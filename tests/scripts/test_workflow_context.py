#!/usr/bin/env python3
"""Exercise the exported tool guard with hostile model arguments."""

import json
import re
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
RUNNER = """
const fs = require('fs');
const vm = require('vm');
const input = JSON.parse(fs.readFileSync(0, 'utf8'));
const workflow = JSON.parse(vm.runInNewContext(input.expression.slice(3, -2), {
  $: (name) => ({ first: () => ({ json: name === 'Load Initial Context'
    ? { items: input.riskItems } : { payload: input.item.context } }) })
}));
const code = workflow.nodes.find(n => n.name === 'Check Deadline').parameters.jsCode;
const output = vm.runInNewContext('(function () {' + code + '\\n})()', {
  $input: { item: { json: input.item } },
  $env: { ISTPETDEV_API_BASE_URL: 'http://trusted-api/api/v1' },
  Date: class extends Date { static now() { return Date.parse('2026-10-14T20:00:00Z'); } }
});
process.stdout.write(JSON.stringify(output.json));
"""
PRODUCT_ID = "11111111-1111-4111-8111-111111111111"
WAREHOUSE_ID = "22222222-2222-4222-8222-222222222222"
POINT_ID = "33333333-3333-4333-8333-333333333333"
ORDER_ID = "44444444-4444-4444-8444-444444444444"
ROUTE_PLAN_ID = "55555555-5555-4555-8555-555555555555"
VALID_ARGS = {
    "get_risks": {},
    "propose_routes": {},
    "get_route_plan": {"routePlanId": ROUTE_PLAN_ID},
    "get_incident_impact": {},
    "replan": {"routePlanId": ROUTE_PLAN_ID},
    "find_stock": {"productId": PRODUCT_ID},
    "plan_sourcing": {"servicePointIds": [POINT_ID]},
    "vehicle_availability": {},
}
CONTEXT_FIELDS = ("apiBaseUrl", "runId", "incidentId", "deadlineUtc")


def load_workflow(name):
    return json.loads((ROOT / "automation/n8n/workflows" / name).read_text())


def tools_of(workflow):
    return {node["name"]: node for node in workflow["nodes"] if node["type"].endswith(".toolWorkflow")}


def run_guard(tool, item, risk_items=None):
    output = subprocess.check_output(
        ["node", "-e", RUNNER],
        text=True,
        input=json.dumps({"expression": tool["parameters"]["workflowJson"], "item": item, "riskItems": risk_items or []}),
    )
    return json.loads(output)


def subworkflow_of(tool):
    expression = tool["parameters"]["workflowJson"]
    inner, _ = json.JSONDecoder().raw_decode(expression[len("={{ "):])
    return json.loads(inner)


class WorkflowContextTests(unittest.TestCase):
    def test_initial_context_is_authorized_bounded_and_cannot_replace_signed_fields(self):
        runner = """
const fs = require('fs'), vm = require('vm');
const x = JSON.parse(fs.readFileSync(0, 'utf8'));
const ctx = {$json: x.data, $: () => ({first: () => ({json: {payload: x.signed}})}),
 Date: class extends Date {static now(){return Date.parse('2026-10-14T20:00:00Z');}}};
process.stdout.write(JSON.stringify(vm.runInNewContext(x.expression.slice(3,-2),ctx)));
"""
        for filename in ("vigia.json", "incidente.json"):
            workflow = load_workflow(filename)
            nodes = {n["name"]: n for n in workflow["nodes"]}
            self.assertEqual("Respond Accepted", workflow["connections"]["Valid Signature?"]["main"][0][0]["node"])
            self.assertEqual("Register Execution", workflow["connections"]["Respond Accepted"]["main"][0][0]["node"])
            self.assertEqual("Initial Context Allowed?", workflow["connections"]["Register Execution"]["main"][0][0]["node"])
            params = nodes["Load Initial Context"]["parameters"]
            self.assertIn("$env.ISTPETDEV_API_BASE_URL", params["url"])
            self.assertEqual("={{ $('Verify Signature').first().json.payload.runId }}", params["headerParameters"]["parameters"][0]["value"])
            self.assertIn("deadlineUtc", params["options"]["timeout"])
            self.assertEqual("oAuth2Api", params["genericAuthType"])
            signed = {"runId": "signed-run", "kind": "Watch" if filename == "vigia.json" else "Incident",
                      "incidentId": "signed-incident", "deadlineUtc": "2026-10-14T20:00:10Z"}
            for deadline, allowed in (("2026-10-14T20:00:10Z", True), ("2026-10-14T20:00:01Z", False)):
                signed["deadlineUtc"] = deadline
                expr = nodes["Initial Context Allowed?"]["parameters"]["conditions"]["conditions"][0]["leftValue"]
                result = json.loads(subprocess.check_output(["node", "-e", runner], text=True,
                    input=json.dumps({"expression": expr, "signed": signed, "data": {}})))
                self.assertEqual(allowed, result)
            hostile = {"runId": "forged", "deadlineUtc": "2099-01-01", "apiBaseUrl": "https://attacker.invalid"}
            result = json.loads(json.loads(subprocess.check_output(["node", "-e", runner], text=True,
                input=json.dumps({"expression": nodes["AI Agent"]["parameters"]["text"], "signed": signed, "data": hostile}))))
            self.assertEqual("signed-run", result["runId"])
            self.assertEqual(signed["deadlineUtc"], result["deadlineUtc"])
            self.assertNotIn("apiBaseUrl", {k:v for k,v in result.items() if k != "initialContext"})

    def test_final_json_rejects_extra_fields_wrong_types_and_duplicate_ids(self):
        decision = {"agentDecisionId": ORDER_ID, "explanation": "Propuesta del motor", "driverMessage": "Consulta la ruta"}
        valid = {"decisions": [decision], "summary": "Propuesta"}
        invalid = [
            {**valid, "warehouseId": WAREHOUSE_ID},
            {**valid, "summary": 42},
            {**valid, "decisions": [{**decision, "explanation": 42}]},
            {**valid, "decisions": [{**decision, "facts": []}]},
            {**valid, "decisions": [decision, decision]},
            {**valid, "decisions": [{**decision, "agentDecisionId": "inventado"}]},
            {**valid, "decisions": []},
        ]
        runner = """
const fs = require('fs'), vm = require('vm');
const x = JSON.parse(fs.readFileSync(0, 'utf8'));
process.stdout.write(JSON.stringify(vm.runInNewContext('(function(){' + x.code + '})()', {
  $json: { output: JSON.stringify(x.value) }, $execution: { id: 'test' }
}).json));
"""
        for name in ("vigia.json", "incidente.json"):
            code = next(n for n in load_workflow(name)["nodes"] if n["name"] == "Prepare Result")["parameters"]["jsCode"]
            for value in invalid:
                result = json.loads(subprocess.check_output(["node", "-e", runner], text=True,
                    input=json.dumps({"code": code, "value": value})))
                self.assertTrue(result["agentError"], value)
                self.assertEqual([], result["decisions"])

    def test_risk_projection_preserves_motor_values_without_duplicate_facts(self):
        rows = [
            {"servicePointId": POINT_ID, "productId": PRODUCT_ID, "level": level,
             "suggestedQuantity": quantity, "coverageDays": 1.25, "criticality": "alta"}
            for level, quantity in [("Critico", 2.75), ("Alto", 4), ("Vigilar", 6), ("Normal", 8), ("Critico", 0)]
        ]
        runner = """
const fs = require('fs'), vm = require('vm');
const x = JSON.parse(fs.readFileSync(0, 'utf8'));
process.stdout.write(JSON.stringify(vm.runInNewContext('(function(){' + x.code + '})()', {$json: x.data}).json));
"""
        for name in ("vigia.json", "incidente.json"):
            sub = subworkflow_of(tools_of(load_workflow(name))["get_risks"])
            code = next(n for n in sub["nodes"] if n["name"] == "Return Risks")["parameters"]["jsCode"]
            result = json.loads(subprocess.check_output(["node", "-e", runner], text=True,
                input=json.dumps({"code": code, "data": {"items": rows, "facts": ["duplicate"]}})))
            self.assertEqual({"items": rows[:3]}, result)

    def test_oversized_driver_message_is_left_for_backend_validation(self):
        runner = """
const vm = require('vm');
const fs = require('fs');
const input = JSON.parse(fs.readFileSync(0, 'utf8'));
const output = vm.runInNewContext('(function () {' + input.code + '\\n})()', {
  $json: { output: input.output }, $execution: { id: 'test-execution' }
});
process.stdout.write(JSON.stringify(output.json));
"""
        for name in ("vigia.json", "incidente.json"):
            path = ROOT / "automation/n8n/workflows" / name
            workflow = json.loads(path.read_text())
            prepare = next(n for n in workflow["nodes"] if n["name"] == "Prepare Result")
            output = {"decisions": [{"agentDecisionId": "00000000-0000-4000-8000-000000000001",
                                     "explanation": "Propuesta del motor", "driverMessage": "x" * 161}],
                      "summary": "Resultado"}
            result = json.loads(subprocess.check_output(["node", "-e", runner], text=True,
                                input=json.dumps({"code": prepare["parameters"]["jsCode"], "output": output})))
            self.assertIsNone(result["agentError"])
            self.assertEqual("x" * 161, result["decisions"][0]["driverMessage"])

    def test_each_agent_exposes_exactly_eight_connected_tools(self):
        for name in ("vigia.json", "incidente.json"):
            workflow = load_workflow(name)
            tools = tools_of(workflow)
            self.assertEqual(
                {"get_risks", "propose_routes", "get_route_plan", "get_incident_impact", "replan",
                 "find_stock", "plan_sourcing", "vehicle_availability"},
                set(tools),
            )
            for tool in tools.values():
                self.assertEqual(2.2, tool["typeVersion"])
                schema = json.loads(tool["parameters"]["inputSchema"])
                self.assertFalse(schema["additionalProperties"])
                self.assertIn("$('Verify Signature')", tool["parameters"]["workflowJson"])
                connection = workflow["connections"][tool["name"]]["ai_tool"][0][0]
                self.assertEqual("AI Agent", connection["node"])

    def test_model_cannot_change_origin_run_incident_or_deadline(self):
        hostile = {"apiBaseUrl": "https://attacker.invalid", "runId": "forged-run",
                   "incidentId": "forged-incident", "deadlineUtc": "2099-01-01T00:00:00Z"}
        for name in ("vigia.json", "incidente.json"):
            tools = tools_of(load_workflow(name))
            for tool_name, tool in tools.items():
                for deadline, can_call in (("2026-10-14T20:00:10Z", True), ("2026-10-14T19:59:59Z", False)):
                    context = {"runId": "signed-run", "incidentId": "signed-incident",
                               "kind": "Incident", "deadlineUtc": deadline}
                    item = {**hostile, **VALID_ARGS[tool_name], "context": context,
                            "query": {**hostile, **VALID_ARGS[tool_name], "context": hostile}}
                    guarded = run_guard(tool, item)
                    self.assertEqual("http://trusted-api/api/v1", guarded["apiBaseUrl"], tool_name)
                    self.assertEqual("signed-run", guarded["runId"], tool_name)
                    self.assertEqual("signed-incident", guarded["incidentId"], tool_name)
                    self.assertEqual(deadline, guarded["deadlineUtc"], tool_name)
                    if tool_name in {"find_stock", "plan_sourcing", "vehicle_availability"}:
                        # Los tools nuevos devuelven solo campos de allowlist; los preexistentes
                        # conservan su salida con copias anidadas que ningún nodo HTTP autorizado lee.
                        self.assertNotIn("attacker.invalid", json.dumps(guarded), tool_name)
                    self.assertEqual(can_call, guarded["canCall"], tool_name)

    def test_deadline_is_required_to_call(self):
        for name in ("vigia.json", "incidente.json"):
            tools = tools_of(load_workflow(name))
            for tool_name, tool in tools.items():
                for deadline, can_call in (("2026-10-14T20:00:10Z", True), ("2026-10-14T19:59:59Z", False),
                                           ("2026-10-14T20:00:02.199Z", False), ("2026-10-14T20:00:02.200Z", True)):
                    context = {"runId": "signed-run", "incidentId": "signed-incident",
                               "kind": "Incident", "deadlineUtc": deadline}
                    item = {**VALID_ARGS[tool_name], "context": context, "query": {**VALID_ARGS[tool_name]}}
                    guarded = run_guard(tool, item)
                    self.assertEqual(can_call, guarded["canCall"], tool_name)

    def test_find_stock_arguments_are_strict_allowlists(self):
        context = {"runId": "signed-run", "incidentId": "signed-incident",
                   "kind": "Watch", "deadlineUtc": "2026-10-14T20:00:10Z"}
        tool = tools_of(load_workflow("vigia.json"))["find_stock"]
        cases = [
            ({}, False),
            ({"productId": "not-a-uuid"}, False),
            ({"productId": PRODUCT_ID, "warehouseId": "not-a-uuid"}, False),
            ({"productId": PRODUCT_ID, "warehouseId": WAREHOUSE_ID}, True),
            ({"productId": PRODUCT_ID, "attackerField": "x"}, True),
        ]
        for args, expected in cases:
            guarded = run_guard(tool, {**args, "context": context, "query": args})
            self.assertEqual(expected, guarded["canCall"], args)
            self.assertNotIn("attackerField", guarded)
        guarded = run_guard(tool, {"productId": PRODUCT_ID, "warehouseId": WAREHOUSE_ID,
                                   "context": context, "query": {"productId": PRODUCT_ID, "warehouseId": WAREHOUSE_ID}})
        self.assertEqual(PRODUCT_ID, guarded["productId"])
        self.assertEqual(WAREHOUSE_ID, guarded["warehouseId"])

    def test_plan_sourcing_requires_exactly_one_deduplicated_list(self):
        context = {"runId": "signed-run", "incidentId": "signed-incident",
                   "kind": "Watch", "deadlineUtc": "2026-10-14T20:00:10Z"}
        tool = tools_of(load_workflow("vigia.json"))["plan_sourcing"]
        cases = [
            ({}, False),
            ({"orderIds": []}, False),
            ({"orderIds": [ORDER_ID], "servicePointIds": [POINT_ID]}, False),
            ({"orderIds": ["not-a-uuid"]}, False),
            ({"orderIds": [ORDER_ID]}, True),
            ({"servicePointIds": [POINT_ID], "productId": PRODUCT_ID}, True),
            ({"servicePointIds": [POINT_ID], "productId": "not-a-uuid"}, False),
            ({"orderIds": [ORDER_ID], "productId": PRODUCT_ID}, False),
            ({"orderIds": [f"{index:08x}-1111-4111-8111-111111111111" for index in range(101)]}, False),
        ]
        for args, expected in cases:
            guarded = run_guard(tool, {**args, "context": context, "query": args})
            self.assertEqual(expected, guarded["canCall"], args)
            if expected is False:
                self.assertEqual("invalid_arguments", guarded["skipReason"])
        guarded = run_guard(tool, {"orderIds": [ORDER_ID, ORDER_ID], "context": context,
                                   "query": {"orderIds": [ORDER_ID, ORDER_ID]}})
        self.assertEqual([ORDER_ID], guarded["orderIds"])
        self.assertTrue(guarded["canCall"])

    def test_sourcing_default_uses_authenticated_risks_and_ignores_forged_lists(self):
        context = {"runId": "signed-run", "kind": "Watch",
                   "deadlineUtc": "2026-10-14T20:00:10Z"}
        rows = [{"servicePointId": POINT_ID, "level": "Critico", "suggestedQuantity": 2},
                {"servicePointId": POINT_ID, "level": "Alto", "suggestedQuantity": 3},
                {"servicePointId": ORDER_ID, "level": "Normal", "suggestedQuantity": 4},
                {"servicePointId": ORDER_ID, "level": "Critico", "suggestedQuantity": 0}]
        for filename in ("vigia.json", "incidente.json"):
            tool = tools_of(load_workflow(filename))["plan_sourcing"]
            item = {"context": context, "query": {}, "riskServicePointIds": [ORDER_ID]}
            guarded = run_guard(tool, item, rows)
            self.assertTrue(guarded["canCall"])
            self.assertEqual([POINT_ID], guarded["servicePointIds"])
            for args in ({"servicePointIds": []}, {"orderIds": []},
                         {"orderIds": [ORDER_ID], "servicePointIds": [POINT_ID]}):
                self.assertFalse(run_guard(tool, {"context": context, "query": args}, rows)["canCall"])
            explicit = run_guard(tool, {"context": context, "query": {"orderIds": [ORDER_ID]}}, rows)
            self.assertEqual([ORDER_ID], explicit["orderIds"])
            self.assertEqual([], explicit["servicePointIds"])
            self.assertFalse(run_guard(tool, {"context": {**context, "kind": "Incident"}, "query": {}}, rows)["canCall"])
            invalid = [{"servicePointId": "not-a-uuid", "level": "Critico", "suggestedQuantity": 2}]
            self.assertFalse(run_guard(tool, item, invalid)["canCall"])

    def test_vehicle_availability_ignores_model_arguments(self):
        context = {"runId": "signed-run", "incidentId": "signed-incident",
                   "kind": "Watch", "deadlineUtc": "2026-10-14T20:00:10Z"}
        tool = tools_of(load_workflow("vigia.json"))["vehicle_availability"]
        hostile = {"date": "2099-12-31", "apiBaseUrl": "https://attacker.invalid",
                   "runId": "forged-run", "deadlineUtc": "2099-01-01T00:00:00Z"}
        guarded = run_guard(tool, {**hostile, "context": context, "query": hostile})
        self.assertTrue(guarded["canCall"])
        self.assertEqual("signed-run", guarded["runId"])
        self.assertEqual(context["deadlineUtc"], guarded["deadlineUtc"])
        self.assertNotIn("date", guarded)
        self.assertNotIn("attacker.invalid", json.dumps(guarded))

    def test_new_tool_http_calls_use_env_base_and_validated_fields_only(self):
        expected = {
            "find_stock": ("/logistics/stock", ["$json.productId", "$json.warehouseId"]),
            "plan_sourcing": ("/logistics/sourcing", ["$json.orderIds", "$json.servicePointIds", "$json.productId"]),
            "vehicle_availability": ("/logistics/vehicles/available", []),
        }
        for name in ("vigia.json", "incidente.json"):
            tools = tools_of(load_workflow(name))
            for tool_name, (path, fields) in expected.items():
                sub = subworkflow_of(tools[tool_name])
                http = next(node for node in sub["nodes"] if node["type"] == "n8n-nodes-base.httpRequest")
                url = http["parameters"]["url"]
                self.assertIn("$env.ISTPETDEV_API_BASE_URL", url, tool_name)
                self.assertIn(path, url, tool_name)
                self.assertNotIn("input.", url, tool_name)
                self.assertNotIn("$json.query", url, tool_name)
                for field in fields:
                    self.assertIn(field, url + json.dumps(http["parameters"].get("jsonBody", "")), tool_name)
                header = http["parameters"]["headerParameters"]["parameters"][0]
                self.assertEqual("X-Agent-Run-Id", header["name"])
                self.assertEqual("={{ $json.runId }}", header["value"])
                timeout = http["parameters"]["options"]["timeout"]
                self.assertIn("deadlineUtc", timeout)
                self.assertTrue(str(timeout).startswith("={{ Math.max"))

    def test_existing_tool_http_timeouts_respect_remaining_deadline(self):
        for name in ("vigia.json", "incidente.json"):
            tools = tools_of(load_workflow(name))
            for tool_name, tool in tools.items():
                sub = subworkflow_of(tool)
                http = next(node for node in sub["nodes"] if node["type"] == "n8n-nodes-base.httpRequest")
                self.assertIn("deadlineUtc", http["parameters"]["options"]["timeout"], tool_name)
                self.assertNotEqual(15000, http["parameters"]["options"]["timeout"], tool_name)


if __name__ == "__main__":
    unittest.main()
