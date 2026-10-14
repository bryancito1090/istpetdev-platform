#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
env_file="$repo_root/.env"
compose_file="$repo_root/infra/compose/docker-compose.local.yml"
container="istpetdev-local-n8n"
credential_file=""
trap '[[ -z "$credential_file" ]] || rm -f -- "$credential_file"' EXIT

if [[ ! -f "$env_file" ]]; then
  printf 'Falta %s; cópialo desde .env.example y completa las variables locales.\n' "$env_file" >&2
  exit 1
fi

umask 077
credential_file="$(mktemp "${TMPDIR:-/tmp}/istpetdev-n8n-credentials.XXXXXX")"
python3 - "$env_file" "$credential_file" <<'PY'
import json
import sys
from pathlib import Path

env_path, output_path = map(Path, sys.argv[1:])
values = {}
for line in env_path.read_text(encoding="utf-8").splitlines():
    stripped = line.strip()
    if not stripped or stripped.startswith("#") or "=" not in line:
        continue
    key, value = line.split("=", 1)
    value = value.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
        value = value[1:-1]
    values[key.strip()] = value

required = (
    "N8N_IMAGE",
    "N8N_ENCRYPTION_KEY",
    "ISTPETDEV_API_BASE_URL",
    "ISTPETDEV_WEBHOOK_SECRET",
    "Agent__N8n__WebhookUrl",
    "COGNITO_TOKEN_URL",
    "COGNITO_M2M_CLIENT_ID",
    "COGNITO_M2M_CLIENT_SECRET",
    "DEEPSEEK_API_KEY",
)
missing = [name for name in required if not values.get(name)]
if missing:
    print("Faltan variables locales: " + ", ".join(missing), file=sys.stderr)
    sys.exit(1)
if values["N8N_IMAGE"] != "docker.n8n.io/n8nio/n8n:2.39.5":
    print("N8N_IMAGE debe ser docker.n8n.io/n8nio/n8n:2.39.5", file=sys.stderr)
    sys.exit(1)
if len(values["N8N_ENCRYPTION_KEY"]) < 32 or len(values["ISTPETDEV_WEBHOOK_SECRET"]) < 32:
    print("N8N_ENCRYPTION_KEY e ISTPETDEV_WEBHOOK_SECRET deben tener al menos 32 caracteres.", file=sys.stderr)
    sys.exit(1)

credentials = [
    {
        "id": "8e186867-4916-54b1-b968-c1c273f79a11",
        "name": "IstpetDev M2M",
        "type": "oAuth2Api",
        "data": {
            "useDynamicClientRegistration": False,
            "grantType": "clientCredentials",
            "accessTokenUrl": values["COGNITO_TOKEN_URL"],
            "clientId": values["COGNITO_M2M_CLIENT_ID"],
            "clientSecret": values["COGNITO_M2M_CLIENT_SECRET"],
            "scope": "istpetdev-api/automation",
            "authentication": "header",
        },
    },
    {
        "id": "3b8794f0-9241-5c0c-9e19-c531bed733c8",
        "name": "IstpetDev DeepSeek",
        "type": "deepSeekApi",
        "data": {
            "apiKey": values["DEEPSEEK_API_KEY"],
            "url": "https://api.deepseek.com",
        },
    },
]
output_path.write_text(json.dumps(credentials, ensure_ascii=False), encoding="utf-8")
output_path.chmod(0o600)
PY

python3 -I "$repo_root/scripts/agent/build-workflows.py"
docker compose --env-file "$env_file" -f "$compose_file" config --quiet
docker compose --env-file "$env_file" -f "$compose_file" --profile agent config --quiet
docker compose --env-file "$env_file" -f "$compose_file" --profile agent up -d n8n

for attempt in $(seq 1 60); do
  if curl --fail --silent http://127.0.0.1:5678/healthz >/dev/null; then break; fi
  sleep 1
done
if ! curl --fail --silent http://127.0.0.1:5678/healthz >/dev/null; then
  printf 'n8n local no respondió en http://127.0.0.1:5678/healthz.\n' >&2
  exit 1
fi

docker exec -i "$container" n8n import:credentials --input=/dev/stdin < "$credential_file"
docker exec "$container" n8n import:workflow --separate --input=/workflows
while IFS= read -r workflow_id; do
  [[ -z "$workflow_id" ]] || docker exec "$container" n8n publish:workflow --id="$workflow_id"
done < <(python3 - "$repo_root/automation/n8n/workflows" <<'PY'
import json
import sys
from pathlib import Path

for path in sorted(Path(sys.argv[1]).glob("*.json")):
    print(json.loads(path.read_text(encoding="utf-8"))["id"])
PY
)
docker compose --env-file "$env_file" -f "$compose_file" --profile agent restart n8n

python3 - <<'PY'
import json
import time
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

paths = ("watch", "incident")
for attempt in range(60):
    ready = True
    for path in paths:
        request = Request(
            f"http://127.0.0.1:5678/webhook/istpetdev/agent/{path}",
            data=b"{}",
            method="POST",
            headers={
                "Content-Type": "application/json",
                "X-IstpetDev-Timestamp": "0",
                "X-IstpetDev-Signature": "sha256=" + "0" * 64,
            },
        )
        try:
            with urlopen(request, timeout=2) as response:
                status = response.status
        except HTTPError as error:
            status = error.code
        except (URLError, TimeoutError, ConnectionError):
            status = 0
        if status != 401:
            ready = False
            break
    if ready:
        print("n8n 2.39.5 listo en http://localhost:5678; webhooks rechazando firmas inválidas.")
        break
    time.sleep(0.5)
else:
    raise SystemExit("Los webhooks locales no quedaron listos; revisa docker logs istpetdev-local-n8n.")
PY
