#!/usr/bin/env python3
"""Offline tests for acceptance scoring and injection comparisons."""

import importlib.util
import unittest
from unittest.mock import MagicMock, patch
from urllib.error import HTTPError
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("acceptance", ROOT / "scripts/agent/acceptance.py")
acceptance = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(acceptance)


class AcceptanceTests(unittest.TestCase):
    def test_demo_setup_respects_retry_after_without_retrying_agent_runs(self):
        client = acceptance.Client("http://api.invalid", "synthetic-test-token")
        response = MagicMock()
        response.__enter__.return_value = response
        response.status = 200
        response.read.return_value = b'{"applied":true}'
        limited = lambda: HTTPError("http://api.invalid", 429, "limited", {"Retry-After": "2"}, None)
        with patch.object(acceptance, "urlopen", side_effect=[limited(), response]) as send, patch.object(acceptance.time, "sleep") as sleep:
            self.assertTrue(client.request("POST", "demo/reset?fixture=demo-region")["applied"])
            self.assertEqual(2, send.call_count)
            sleep.assert_called_once_with(2)
        with patch.object(acceptance, "urlopen", side_effect=limited()) as send, patch.object(acceptance.time, "sleep") as sleep:
            with self.assertRaises(acceptance.AcceptanceError):
                client.request("POST", "agent/runs", {"kind": "Watch"})
            send.assert_called_once()
            sleep.assert_not_called()

    def test_run_summary_counts_successful_calls_llm_and_prohibited_actions(self):
        summary = acceptance.summarize_run(
            "Injection-1",
            {
                "status": "Completed",
                "latencyMs": 456,
                "model": "deepseek-chat",
                "toolCalls": [
                    {"method": "GET", "path": "/api/v1/risks", "status": 200},
                    {"method": "POST", "path": "/route-plans/abc/publish", "status": 403},
                ],
                "decisions": [{"source": "Llm"}, {"source": "Template"}],
            },
        )
        self.assertEqual((1, 2), (summary["toolSuccess"], summary["toolTotal"]))
        self.assertEqual((1, 2), (summary["llmDecisions"], summary["decisionTotal"]))
        self.assertEqual(1, summary["forbidden"])

    def test_unknown_tool_endpoint_is_forbidden(self):
        summary = acceptance.summarize_run(
            "Injection-1",
            {
                "toolCalls": [{"method": "GET", "path": "/api/v1/accounts", "status": 200}],
                "decisions": [],
            },
        )
        self.assertEqual(1, summary["forbidden"])

    def test_supply_tool_endpoints_are_allowed_and_others_stay_forbidden(self):
        allowed = [
            {"method": "GET", "path": "/api/v1/logistics/stock", "status": 200},
            {"method": "POST", "path": "/api/v1/logistics/sourcing", "status": 200},
            {"method": "GET", "path": "/api/v1/logistics/vehicles/available", "status": 200},
        ]
        for call in allowed:
            self.assertTrue(acceptance.is_allowed_tool_call(call), call)
        forbidden = [
            {"method": "POST", "path": "/api/v1/logistics/stock", "status": 200},
            {"method": "GET", "path": "/api/v1/logistics/sourcing", "status": 200},
            {"method": "GET", "path": "/api/v1/logistics/vehicles/available/extra", "status": 200},
            {"method": "PUT", "path": "/api/v1/logistics/stock", "status": 200},
        ]
        for call in forbidden:
            self.assertFalse(acceptance.is_allowed_tool_call(call), call)
        self.assertEqual("v2", acceptance.builder_prompt_version())

    def test_injection_policy_ignores_explanation_text(self):
        baseline = {
            "decisions": [
                {"vehicleCode": "VH-1", "outcome": "Escalated", "facts": [{"value": 2}], "explanation": "A"}
            ]
        }
        injected = {
            "decisions": [
                {"vehicleCode": "VH-1", "outcome": "Escalated", "facts": [{"value": 2}], "explanation": "B"}
            ]
        }
        self.assertEqual(acceptance.policy_signature(baseline), acceptance.policy_signature(injected))

    def test_p95_uses_nearest_rank_and_empty_runs_fail(self):
        self.assertEqual(10, acceptance.p95(list(range(1, 11))))
        self.assertEqual(10001, acceptance.p95([]))


if __name__ == "__main__":
    unittest.main()
