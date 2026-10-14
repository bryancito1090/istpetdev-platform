#!/usr/bin/env python3
"""Exercise the exported tool guard with hostile model arguments."""

import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
RUNNER = """
const fs = require('fs');
const vm = require('vm');
const input = JSON.parse(fs.readFileSync(0, 'utf8'));
const workflow = JSON.parse(vm.runInNewContext(input.expression.slice(3, -2), {
  $: () => ({ first: () => ({ json: { payload: input.item.context } }) })
}));
const code = workflow.nodes.find(n => n.name === 'Check Deadline').parameters.jsCode;
const output = vm.runInNewContext('(function () {' + code + '\\n})()', {
  $input: { item: { json: input.item } },
  $env: { ISTPETDEV_API_BASE_URL: 'http://trusted-api/api/v1' },
  Date: class extends Date { static now() { return Date.parse('2026-10-14T20:00:00Z'); } }
});
process.stdout.write(JSON.stringify(output.json));
"""


class WorkflowContextTests(unittest.TestCase):
    def test_model_cannot_change_origin_run_incident_or_deadline(self):
        for path in (ROOT / "automation/n8n/workflows").glob("*.json"):
            workflow = json.loads(path.read_text())
            tools = [n for n in workflow["nodes"] if n["type"].endswith(".toolWorkflow")]
            self.assertEqual(5, len(tools))
            for tool in tools:
                self.assertEqual(2.2, tool["typeVersion"])
                params = tool["parameters"]
                self.assertIn("$('Verify Signature')", params["workflowJson"])
                schema = json.loads(params["inputSchema"])
                self.assertFalse(schema["additionalProperties"])
                self.assertLessEqual(set(schema["properties"]), {"routePlanId"})
                hostile = {"apiBaseUrl": "https://attacker.invalid", "runId": "forged-run",
                           "incidentId": "forged-incident", "deadlineUtc": "2099-01-01T00:00:00Z"}
                for deadline, can_call in (("2026-10-14T20:00:10Z", True), ("2026-10-14T19:59:59Z", False)):
                    context = {"runId": "signed-run", "incidentId": "signed-incident",
                               "kind": "Incident", "deadlineUtc": deadline}
                    item = {**hostile, "context": context, "query": {**hostile, "context": hostile}}
                    output = subprocess.check_output(["node", "-e", RUNNER], text=True,
                                                     input=json.dumps({"expression": params["workflowJson"], "item": item}))
                    guarded = json.loads(output)
                    self.assertEqual("http://trusted-api/api/v1", guarded["apiBaseUrl"])
                    self.assertEqual("signed-run", guarded["runId"])
                    self.assertEqual("signed-incident", guarded["incidentId"])
                    self.assertEqual(deadline, guarded["deadlineUtc"])
                    self.assertEqual(can_call, guarded["canCall"])


if __name__ == "__main__":
    unittest.main()
