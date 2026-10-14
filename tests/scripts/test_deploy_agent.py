#!/usr/bin/env python3
"""Exercise the production Compose and both remote scripts without a deployment."""

import json
import os
import re
import subprocess
import tempfile
import textwrap
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOWS = ("deploy.yml", "rollback.yml")
SETTINGS = {
    "COGNITO_M2M_CLIENT_ID": "test-machine-client",
    "AUTHENTICATION_TECHNICAL_READ_SCOPE": "istpetdev-api/automation",
    "AGENT_ENABLED": "true",
    "AGENT_AUTO_ON_INCIDENT": "false",
    "AGENT_TIMEOUT_SECONDS": "10",
    "AGENT_POLL_MILLISECONDS": "250",
    "AGENT_N8N_WEBHOOK_URL": "https://n8n.example.invalid/webhook/istpetdev/agent",
    "AGENT_N8N_WEBHOOK_SECRET": "synthetic-test-value",
    "AGENT_N8N_API_BASE_URL": "https://api.example.invalid/api/v1",
}


def isolated_environment():
    # Do not use the operator's .env or deployment credentials during this check.
    return {key: value for key, value in os.environ.items()
            if not key.startswith(("AGENT_", "COGNITO_", "AUTHENTICATION_", "DB_"))}


class AgentDeploymentTests(unittest.TestCase):
    def compose_environment(self, overrides):
        env = isolated_environment() | {
            "DB_PASSWORD": "synthetic-db-value",
            "COGNITO_AUTHORITY": "https://issuer.example.invalid/pool",
            "COGNITO_WEB_CLIENT_ID": "test-web-client",
            "COGNITO_MOBILE_CLIENT_ID": "test-mobile-client",
        } | overrides
        with tempfile.TemporaryDirectory() as directory:
            env_file = Path(directory, "empty.env")
            env_file.touch()
            result = subprocess.run(
                ["docker", "compose", "--env-file", str(env_file), "-f",
                 str(ROOT / "infra/compose/docker-compose.prod.yml"),
                 "config", "--format", "json"],
                cwd=directory, env=env, text=True, capture_output=True, check=True,
            )
        return json.loads(result.stdout)["services"]["istpetdev-backend"]["environment"]

    def remote_block(self, name):
        text = (ROOT / ".github/workflows" / name).read_text()
        match = re.search(
            r"# Inicio de configuración del agente\n(.*?)\n\s*# Fin de configuración del agente",
            text, re.S,
        )
        self.assertIsNotNone(match, name)
        return textwrap.dedent(match.group(1))

    def test_unconfigured_compose_keeps_agent_disabled_and_human_clients(self):
        env = self.compose_environment({})
        self.assertEqual(env["Agent__Enabled"], "false")
        self.assertEqual(env["Agent__N8n__WebhookSecret"], "")
        self.assertEqual(env["Authentication__TechnicalClientIds__0"], "")
        self.assertEqual(env["Authentication__AllowedClientIds__0"], "test-web-client")
        self.assertEqual(env["Authentication__AllowedClientIds__1"], "test-mobile-client")

    def test_configured_compose_maps_machine_identity_and_callback_origin(self):
        env = self.compose_environment(SETTINGS)
        self.assertEqual(env["Authentication__AllowedClientIds__2"], SETTINGS["COGNITO_M2M_CLIENT_ID"])
        self.assertEqual(env["Authentication__TechnicalClientIds__0"], SETTINGS["COGNITO_M2M_CLIENT_ID"])
        self.assertEqual(env["Agent__Enabled"], "true")
        self.assertEqual(env["Agent__AutoOnIncident"], "false")
        self.assertEqual(env["Agent__N8n__WebhookSecret"], SETTINGS["AGENT_N8N_WEBHOOK_SECRET"])
        self.assertEqual(env["Agent__N8n__ApiBaseUrl"], SETTINGS["AGENT_N8N_API_BASE_URL"])

    def test_both_remote_scripts_reject_activation_with_missing_credentials(self):
        for name in WORKFLOWS:
            block = self.remote_block(name)
            for missing in ("COGNITO_M2M_CLIENT_ID", "AGENT_N8N_WEBHOOK_SECRET"):
                with self.subTest(workflow=name, missing=missing):
                    env = isolated_environment() | SETTINGS | {missing: ""}
                    result = subprocess.run(["sh", "-c", block], env=env, text=True, capture_output=True)
                    self.assertNotEqual(result.returncode, 0)
                    self.assertNotIn(SETTINGS["AGENT_N8N_WEBHOOK_SECRET"], result.stdout + result.stderr)
            for settings, expected in (({}, 0), (SETTINGS, 0), ({"AGENT_ENABLED": "invalid"}, 1)):
                with self.subTest(workflow=name, settings=settings):
                    result = subprocess.run(["sh", "-c", block], env=isolated_environment() | settings,
                                            text=True, capture_output=True)
                    self.assertEqual(result.returncode, expected)

    def test_actions_pass_all_agent_settings_to_ssh_in_deploy_and_rollback(self):
        for name in WORKFLOWS:
            text = (ROOT / ".github/workflows" / name).read_text()
            envs = next(line.split("envs:", 1)[1].strip().split(",")
                        for line in text.splitlines() if "envs:" in line and "COGNITO_M2M_CLIENT_ID" in line)
            for key in SETTINGS:
                with self.subTest(workflow=name, variable=key):
                    self.assertIn(key, envs)
                    scope = "secrets" if key == "AGENT_N8N_WEBHOOK_SECRET" else "vars"
                    self.assertRegex(text, rf"(?m)^\s+{key}: \$\{{\{{ {scope}\.{key}(?: | \|\|)")


if __name__ == "__main__":
    unittest.main()
