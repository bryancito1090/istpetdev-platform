#!/usr/bin/env python3
"""Check instance workflow IDs, publication and cleanup without services."""

import json
import os
import shlex
import shutil
import subprocess
import sys
import tempfile
import unittest
from itertools import product
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


class LocalUpTests(unittest.TestCase):
    def test_workflow_links_publish_all_reuse_server_id_and_clean_up_secrets(self):
        with tempfile.TemporaryDirectory() as directory:
            repo = Path(directory)
            scripts = repo / "scripts/agent"
            scripts.mkdir(parents=True)
            for name in ("local-up.sh", "build-workflows.py"):
                shutil.copy2(ROOT / "scripts/agent" / name, scripts / name)
            shutil.copytree(ROOT / "automation", repo / "automation")
            compose = repo / "infra/compose"
            compose.mkdir(parents=True)
            for name in ("docker-compose.local.yml", "docker-compose.dependencies.yml"):
                shutil.copy2(ROOT / "infra/compose" / name, compose / name)
            secret = "local-cleanup-test-value-never-print-123456"
            values = {
                "N8N_IMAGE": "docker.n8n.io/n8nio/n8n:2.39.5",
                "N8N_ENCRYPTION_KEY": secret,
                "ISTPETDEV_API_BASE_URL": "http://host.docker.internal:5000/api/v1",
                "ISTPETDEV_WEBHOOK_SECRET": secret,
                "Agent__N8n__WebhookUrl": "http://localhost:5678/webhook/istpetdev/agent",
                "COGNITO_TOKEN_URL": "https://example.invalid/oauth2/token",
                "COGNITO_M2M_CLIENT_ID": "cleanup-test-client",
                "COGNITO_M2M_CLIENT_SECRET": secret,
                "DEEPSEEK_API_KEY": secret,
            }
            (repo / ".env").write_text("".join(f"{k}={v}\n" for k, v in values.items()))
            mock_bin = repo / "mock-bin"
            mock_bin.mkdir()
            (mock_bin / "docker").write_text(
                """#!/usr/bin/env python3
import json, os, sys
from pathlib import Path
args = sys.argv[1:]
server_id = os.environ.get('EXISTING_ERROR_ID') or 'a1130000-0000-4000-8000-000000000001'
if 'export:workflow' in args:
    print(json.dumps([{'name': 'IstpetDev Agent - error', 'id': server_id}] if os.environ.get('EXISTING_ERROR_ID') else []))
elif 'node' in args:
    Path(os.environ['GENERATED_ID_FILE']).touch()
    print(server_id)
elif args[0] == 'cp':
    workflows = [json.loads(path.read_text()) for path in Path(args[1]).glob('*.json')]
    error = next(w for w in workflows if w['name'] == 'IstpetDev Agent - error')
    assert error['id'] == server_id
    assert all(w['settings']['errorWorkflow'] == server_id for w in workflows if w != error)
    Path(os.environ['IMPORT_CHECK_FILE']).write_text(json.dumps({'linked': len(workflows)}))
elif 'publish:workflow' in args:
    with Path(os.environ['PUBLISHED_ID_FILE']).open('a') as output:
        output.write(next(arg[5:] for arg in args if arg.startswith('--id=')) + '\\n')
elif 'import:workflow' in args and os.environ['FAIL_IMPORT'] == '1':
    print('forced workflow import failure', file=sys.stderr)
    sys.exit(42)
"""
            )
            (mock_bin / "curl").write_text("#!/bin/sh\nexit 0\n")
            # The final HTTP readiness probe is covered by the real n8n E2E test.
            (mock_bin / "python3").write_text(
                '#!/bin/sh\nif [ "$#" = 1 ] && [ "$1" = - ]; then cat >/dev/null; exit 0; fi\n'
                f'exec {shlex.quote(sys.executable)} "$@"\n'
            )
            for path in mock_bin.iterdir():
                path.chmod(0o700)
            temporary = repo / "temporary"
            temporary.mkdir()
            for existing_id, fail_import in product(("", "server-existing-error-113"), (True, False)):
                with self.subTest(existing_id=bool(existing_id), fail_import=fail_import):
                    generated = repo / "generated-id"
                    generated.unlink(missing_ok=True)
                    check = repo / "import-check.json"
                    published = repo / "published-ids"
                    published.unlink(missing_ok=True)
                    result = subprocess.run(
                        ["bash", str(scripts / "local-up.sh")], cwd=repo, text=True,
                        capture_output=True,
                        env={**os.environ, "PATH": f"{mock_bin}:{os.environ['PATH']}",
                             "TMPDIR": str(temporary), "EXISTING_ERROR_ID": existing_id,
                             "GENERATED_ID_FILE": str(generated), "IMPORT_CHECK_FILE": str(check),
                             "PUBLISHED_ID_FILE": str(published), "FAIL_IMPORT": "1" if fail_import else "0"},
                    )
                    self.assertEqual(42 if fail_import else 0, result.returncode, result.stderr)
                    self.assertEqual({"linked": 3}, json.loads(check.read_text()))
                    self.assertEqual(not bool(existing_id), generated.exists())
                    if fail_import:
                        self.assertIn("forced workflow import failure", result.stderr)
                    else:
                        expected = {json.loads(path.read_text())["id"] for path in (repo / "automation/n8n/workflows").glob("*.json") if path.name != "error-agente.json"}
                        expected.add(existing_id or "a1130000-0000-4000-8000-000000000001")
                        self.assertEqual(expected, set(published.read_text().splitlines()))
                    self.assertEqual([], list(temporary.iterdir()))
                    self.assertNotIn(secret, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
