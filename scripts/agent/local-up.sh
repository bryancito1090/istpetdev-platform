#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
env_file="$repo_root/.env"
compose_file="$repo_root/infra/compose/docker-compose.local.yml"
container="istpetdev-local-n8n"
credential_file=""
workflow_export=""
workflow_stage_dir=""
trap '[[ -z "$credential_file" ]] || rm -f -- "$credential_file"; [[ -z "$workflow_export" ]] || rm -f -- "$workflow_export"; [[ -z "$workflow_stage_dir" ]] || rm -rf -- "$workflow_stage_dir"' EXIT

if [[ ! -f "$env_file" ]]; then
  printf 'Falta %s; cópialo desde .env.example y completa las variables locales.\n' "$env_file" >&2
  exit 1
fi

umask 077
credential_file="$(mktemp "${TMPDIR:-/tmp}/istpetdev-n8n-credentials.XXXXXX")"
workflow_export="$(mktemp "${TMPDIR:-/tmp}/istpetdev-n8n-workflows.XXXXXX")"
workflow_stage_dir="$(mktemp -d "${TMPDIR:-/tmp}/istpetdev-n8n-import.XXXXXX")"
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

find_error_workflow_id() {
  python3 - "$workflow_export" <<'PY'
import json
import sys
from pathlib import Path

text = Path(sys.argv[1]).read_text(encoding="utf-8").strip()
workflows = json.loads(text) if text else []
if isinstance(workflows, dict):
    workflows = [workflows]
matches = [workflow for workflow in workflows if workflow.get("name") == "IstpetDev Agent - error"]
if len(matches) > 1:
    raise SystemExit("Hay más de un workflow IstpetDev Agent - error en n8n local.")
workflow_id = matches[0].get("id", "") if matches else ""
print(workflow_id)
PY
}

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
docker exec "$container" n8n export:workflow --all > "$workflow_export"
error_workflow_id="$(find_error_workflow_id)"

if [[ -z "$error_workflow_id" ]]; then
  # CLI import requires an ID. Generate it on the target instance, never in Git.
  error_workflow_id="$(docker exec "$container" node -e 'process.stdout.write(require("crypto").randomUUID())')"
  if [[ -z "$error_workflow_id" ]]; then
    printf 'No se pudo generar el ID del workflow de error en n8n.\n' >&2
    exit 1
  fi
fi

python3 - "$repo_root/automation/n8n/workflows" "$workflow_stage_dir" "$error_workflow_id" <<'PY'
import json
import sys
from pathlib import Path

source, destination = map(Path, sys.argv[1:3])
error_id = sys.argv[3]
for path in source.glob("*.json"):
    workflow = json.loads(path.read_text(encoding="utf-8"))
    if path.name == "error-agente.json":
        workflow["id"] = error_id
    else:
        workflow.setdefault("settings", {})["errorWorkflow"] = error_id
    (destination / path.name).write_text(
        json.dumps(workflow, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
PY

docker exec "$container" rm -rf /tmp/istpetdev-agent-workflows
docker exec "$container" mkdir -p /tmp/istpetdev-agent-workflows
docker cp "$workflow_stage_dir/." "$container:/tmp/istpetdev-agent-workflows"
docker exec "$container" n8n import:workflow --separate --input=/tmp/istpetdev-agent-workflows
while IFS= read -r workflow_id; do
  [[ -z "$workflow_id" ]] || docker exec "$container" n8n publish:workflow --id="$workflow_id"
done < <(python3 - "$workflow_stage_dir" <<'PY'
import json
import sys
from pathlib import Path

for path in sorted(Path(sys.argv[1]).glob("*.json")):
    workflow = json.loads(path.read_text(encoding="utf-8"))
    print(workflow["id"])
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
