#!/usr/bin/env python3
"""Focused tests for the isolated n8n workflow generator."""

import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "build_workflows", ROOT / "scripts/agent/build-workflows.py"
)
builder = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(builder)


def sample_workflow():
    return {
        "name": "sample",
        "nodes": [
            {
                "name": "AI Agent",
                "type": "@n8n/n8n-nodes-langchain.agent",
                "parameters": {"options": {"systemMessage": "placeholder"}},
            },
            {
                "name": "Send Result",
                "type": "n8n-nodes-base.httpRequest",
                "parameters": {"body": {"promptVersion": "{{PROMPT_VERSION}}"}},
            },
        ],
        "connections": {},
    }


class BuildWorkflowsTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.prompt = self.root / "prompt.md"
        self.templates = self.root / "templates"
        self.output = self.root / "output"
        self.templates.mkdir()
        self.prompt.write_text("Prompt de prueba", encoding="utf-8")
        for name in builder.REQUIRED_WORKFLOWS:
            (self.templates / f"{name}.template.json").write_text(
                json.dumps(sample_workflow()), encoding="utf-8"
            )

    def tearDown(self):
        self.temp.cleanup()

    def test_build_is_reproducible_and_injects_prompt_and_version(self):
        args = (self.prompt, self.templates, self.output)
        self.assertEqual(0, builder.build_workflows(*args))
        first = {p.name: p.read_bytes() for p in self.output.glob("*.json")}
        self.assertEqual(0, builder.build_workflows(*args))
        second = {p.name: p.read_bytes() for p in self.output.glob("*.json")}
        self.assertEqual(first, second)
        for content in first.values():
            workflow = json.loads(content)
            agent = next(n for n in workflow["nodes"] if "agent" in n["type"])
            self.assertEqual("Prompt de prueba", agent["parameters"]["options"]["systemMessage"])
            result = next(n for n in workflow["nodes"] if n["name"] == "Send Result")
            self.assertEqual("v1", result["parameters"]["body"]["promptVersion"])
            self.assertEqual("v1", workflow["meta"]["promptVersion"])
            self.assertEqual("2.39.5", workflow["meta"]["n8nVersion"])
        self.assertEqual(0, builder.build_workflows(*args, check_only=True))

    def test_missing_template_fails_before_creating_outputs(self):
        (self.templates / "incidente.template.json").unlink()
        with self.assertRaises(FileNotFoundError):
            builder.build_workflows(self.prompt, self.templates, self.output)
        self.assertFalse(self.output.exists())

    def test_secret_fields_are_rejected(self):
        workflow = sample_workflow()
        workflow["nodes"][1]["parameters"]["apiKey"] = "must-not-be-exported"
        with self.assertRaisesRegex(ValueError, "Campo sensible literal"):
            builder.assert_no_embedded_secrets(workflow)

    def test_jwt_values_are_rejected(self):
        workflow = sample_workflow()
        workflow["nodes"][1]["parameters"]["url"] = (
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ0ZXN0In0.signatureplaceholder"
        )
        with self.assertRaisesRegex(ValueError, "Posible credencial literal"):
            builder.assert_no_embedded_secrets(workflow)


if __name__ == "__main__":
    unittest.main()
