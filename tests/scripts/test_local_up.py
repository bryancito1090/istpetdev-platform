#!/usr/bin/env python3
"""Check credential cleanup when the local bootstrap fails, without services."""

import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


class LocalUpTests(unittest.TestCase):
    def test_failed_workflow_import_removes_credentials_and_hides_secrets(self):
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
                '#!/bin/sh\ncase "$*" in\n'
                '*import:workflow*) echo "forced workflow import failure" >&2; exit 42;;\n'
                'esac\nexit 0\n'
            )
            (mock_bin / "curl").write_text("#!/bin/sh\nexit 0\n")
            for path in mock_bin.iterdir():
                path.chmod(0o700)
            temporary = repo / "temporary"
            temporary.mkdir()
            result = subprocess.run(
                ["bash", str(scripts / "local-up.sh")], cwd=repo, text=True,
                capture_output=True,
                env={**os.environ, "PATH": f"{mock_bin}:{os.environ['PATH']}",
                     "TMPDIR": str(temporary)},
            )
            self.assertEqual(42, result.returncode, result.stderr)
            self.assertIn("forced workflow import failure", result.stderr)
            self.assertEqual([], list(temporary.glob("istpetdev-n8n-credentials.*")))
            self.assertNotIn(secret, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
