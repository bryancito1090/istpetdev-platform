#!/usr/bin/env python3
"""Exercise the exported dataset review workflow: structure and deterministic nodes (#209)."""

import json
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / "automation/n8n/workflows/revision-dataset.json"

RUNNER = """
const fs = require('fs');
const vm = require('vm');
const input = JSON.parse(fs.readFileSync(0, 'utf8'));
const sandbox = {
  $json: input.item,
  $: (name) => ({ first: () => ({ json: ((input.nodes || input.item.nodes) || {})[name] || {} }) }),
  $input: { item: { json: input.item, binary: input.binary } },
  $execution: { id: 'test-execution' },
  $env: {
    ISTPETDEV_API_BASE_URL: 'http://trusted-api/api/v1',
    ISTPETDEV_WEBHOOK_SECRET: 'test-secret'
  },
  require: require,
  Buffer: Buffer
};
const output = vm.runInNewContext('(function () {' + input.code + '\\n})()', sandbox);
process.stdout.write(JSON.stringify(input.full ? { json: output.json, binary: output.binary || null } : output.json));
"""

REPORT = {
    "batchId": "00000000-0000-4000-8000-000000000209",
    "profile": "auto",
    "dryRun": True,
    "applied": False,
    "status": "DryRun",
    "countsMode": "Planned",
    "errorCount": 0,
    "warningCount": 1,
    "omittedIssues": 0,
    "entities": [
        {
            "entity": "service_points",
            "file": "puntos.csv",
            "rowsRead": 2,
            "created": 2,
            "updated": 0,
            "skipped": 0,
            "duplicates": 0,
            "assumptions": {"window_start": {"note": "se usara la ventana global por defecto", "rows": 1}},
        },
        {
            "entity": "consumptions",
            "file": "consumos.csv",
            "rowsRead": 2,
            "created": 2,
            "updated": 0,
            "skipped": 0,
            "duplicates": 1,
            "assumptions": {},
        },
    ],
    "issues": [
        {
            "entity": "consumptions",
            "row": 3,
            "field": None,
            "severity": "Warning",
            "code": "duplicate",
            "message": "La clave esta repetida; se suman las cantidades validas antes de aplicar.",
            "file": "consumos.csv",
        }
    ],
    "feasibility": {
        "verdict": "Inviable",
        "unroutablePoints": [{"code": "P-1", "name": "Punto sin gps", "row": 2}],
        "warehousesWithoutFleet": [{"code": "W-1", "name": "Bodega Centro"}],
        "productsWithoutCapacity": [{"sku": "SKU-1", "name": "Producto", "reason": "both"}],
        "stockShortages": [
            {"sku": "SKU-1", "name": "Producto", "baseUnit": "UNIDAD", "demanded": 100, "available": 0}
        ],
    },
}

REJECTED_REPORT = {
    **REPORT,
    "status": "Rejected",
    "errorCount": 1,
    "issues": [
        {
            "entity": "warehouses",
            "row": 3,
            "field": "lat",
            "severity": "Error",
            "code": "out_of_range",
            "message": "Latitud -200 fuera de [-90, 90].",
            "file": "bodegas.csv",
        }
    ],
}


def run_node(workflow: dict, node_name: str, item: dict, full: bool = False, binary: dict | None = None, nodes: dict | None = None) -> dict:
    code = next(n for n in workflow["nodes"] if n["name"] == node_name)["parameters"]["jsCode"]
    output = subprocess.check_output(
        ["node", "-e", RUNNER], text=True,
        input=json.dumps({"code": code, "item": item, "full": full, "binary": binary, "nodes": nodes})
    )
    return json.loads(output)


class DatasetReviewWorkflowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = json.loads(WORKFLOW.read_text(encoding="utf-8"))

    def test_structure_calls_the_api_only_for_dry_runs(self):
        names = {node["name"] for node in self.workflow["nodes"]}
        self.assertTrue({"Webhook", "Check Token", "Import Dry-Run", "Analizar viabilidad", "Prepare Result", "Respond Review"} <= names)
        raw = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("/imports?dryRun=true&allowPartial=false", raw)
        self.assertNotIn("allowPartial=true", raw)
        http = next(n for n in self.workflow["nodes"] if n["name"] == "Import Dry-Run")
        response = http["parameters"]["options"]["response"]["response"]
        self.assertTrue(response["fullResponse"])
        self.assertTrue(response["neverError"])
        self.assertEqual(
            "8e186867-4916-54b1-b968-c1c273f79a11",
            http["credentials"]["oAuth2Api"]["id"],
        )
        self.assertNotIn("errorWorkflow", self.workflow.get("settings", {}))

    def test_check_token_uses_the_shared_secret_and_normalizes_the_upload(self):
        binary = {"file0": {"data": "ZGF0bw==", "fileName": "puntos.csv", "mimeType": "text/csv"}}
        headers = {"headers": {"x-istpetdev-review-token": "test-secret"}}
        authorized = run_node(self.workflow, "Check Token", headers, full=True, binary=binary)
        self.assertTrue(authorized["json"]["authorized"])
        self.assertIsNone(authorized["json"]["reason"])
        self.assertEqual("puntos.csv", authorized["binary"]["file"]["fileName"])
        rejected = run_node(self.workflow, "Check Token", {"headers": {"x-istpetdev-review-token": "wrong"}}, binary=binary)
        self.assertFalse(rejected["authorized"])
        self.assertEqual("token_mismatch", rejected["reason"])
        missing = run_node(self.workflow, "Check Token", headers)
        self.assertFalse(missing["authorized"])
        self.assertEqual("missing_file", missing["reason"])

    def test_analysis_reports_sections_and_separates_safe_from_human_treatments(self):
        result = run_node(self.workflow, "Analizar viabilidad", {"statusCode": 200, "body": REPORT})

        self.assertTrue(result["valid"])
        dictamen = result["dictamen"]
        self.assertEqual("DryRun", dictamen["importacion"]["estado"])
        self.assertEqual("Inviable para rutear el dataset completo", dictamen["veredicto"])
        point = dictamen["puntosSinCoordenadas"][0]
        self.assertEqual({"codigo": "P-1", "nombre": "Punto sin gps", "fila": 2}, point)
        shortage = dictamen["stockVsDemanda"][0]
        self.assertEqual("SKU-1", shortage["sku"])
        self.assertEqual(100, shortage["demanda"])
        self.assertEqual(0, shortage["disponible"])
        self.assertEqual("W-1", dictamen["bodegasSinFlota"][0]["codigo"])
        self.assertEqual("both", dictamen["productosSinCapacidad"][0]["motivo"])
        self.assertEqual(1, dictamen["contadores"]["puntosSinCoordenadas"])
        self.assertEqual(1, dictamen["contadores"]["stockVsDemanda"])
        self.assertTrue(any("Supuesto automatico" in action for action in dictamen["accionesSeguras"]))
        self.assertTrue(any("Duplicados consolidados" in action for action in dictamen["accionesSeguras"]))
        codes = {action["codigo"] for action in dictamen["accionesHumanas"]}
        self.assertTrue({"missing_coordinates", "stock_shortage", "warehouse_without_fleet", "product_without_capacity"} <= codes)
        self.assertEqual(dictamen, json.loads(result["facts"]))

    def test_rejected_preview_keeps_row_issues_for_human_review(self):
        result = run_node(self.workflow, "Analizar viabilidad", {"statusCode": 422, "body": REJECTED_REPORT})

        self.assertTrue(result["valid"])
        dictamen = result["dictamen"]
        self.assertEqual(1, dictamen["contadores"]["errores"])
        issue = next(action for action in dictamen["accionesHumanas"] if action["codigo"] == "out_of_range")
        self.assertEqual("bodegas.csv", issue["archivo"])
        self.assertEqual(3, issue["fila"])
        self.assertEqual("lat", issue["campo"])
        self.assertIn("-200", issue["mensaje"])

    def test_unreadable_api_response_fails_closed(self):
        result = run_node(self.workflow, "Analizar viabilidad", {"statusCode": 200, "body": "<html>bad gateway</html>"})

        self.assertFalse(result["valid"])
        self.assertEqual("invalid_report", result["error"])

    def test_prepare_result_falls_back_when_llm_output_is_missing_or_invented(self):
        analysis = run_node(self.workflow, "Analizar viabilidad", {"statusCode": 200, "body": REPORT})
        base = {"nodes": {"Analizar viabilidad": analysis}}

        missing = run_node(self.workflow, "Prepare Result", {**base, "output": None, "error": "provider down"})
        self.assertEqual("provider down", missing["agentError"])
        self.assertEqual("Inviable para rutear el dataset completo", missing["veredicto"])
        self.assertIn("puntos sin coordenadas", missing["resumen"])
        self.assertNotIn("deepseek", missing["resumen"].lower())

        invented = run_node(self.workflow, "Prepare Result", {**base, "output": {"resumenEjecutivo": "Faltan 987654 unidades en la bodega Z-9."}})
        self.assertEqual("llm_numbers_not_grounded", invented["agentError"])
        self.assertEqual(missing["resumen"], invented["resumen"])

        grounded = run_node(self.workflow, "Prepare Result", {**base, "output": {"resumenEjecutivo": "Revision en seco sin errores; hay puntos sin coordenadas."}})
        self.assertIsNone(grounded["agentError"])
        self.assertEqual("Revision en seco sin errores; hay puntos sin coordenadas.", grounded["resumen"])

    def test_prepare_result_without_analysis_does_not_invent_data(self):
        failed = run_node(self.workflow, "Prepare Result", {"error": "boom", "message": "La API no respondio."})
        self.assertEqual("Sin dictamen", failed["veredicto"])
        self.assertEqual("boom", failed["agentError"])
        self.assertIsNone(failed["dictamen"])

        invalid = run_node(self.workflow, "Prepare Result", {"error": "invalid_report"}, nodes={"Analizar viabilidad": {"valid": False, "error": "invalid_report", "message": "La API no devolvio un reporte de importacion legible."}})
        self.assertEqual("La API no devolvio un reporte de importacion legible.", invalid["resumen"])
        self.assertEqual("invalid_report", invalid["agentError"])

    def test_check_token_accepts_raw_json_body_payload(self):
        item = {
            "headers": {"x-istpetdev-review-token": "test-secret"},
            "body": [{"code": "P-1", "name": "Punto de entrega"}]
        }
        authorized = run_node(self.workflow, "Check Token", item, full=True)
        self.assertTrue(authorized["json"]["authorized"])
        self.assertIsNone(authorized["json"]["reason"])
        self.assertEqual("dataset.json", authorized["binary"]["file"]["fileName"])
        self.assertEqual("application/json", authorized["binary"]["file"]["mimeType"])

    def test_api_problem_details_400_for_unsupported_json_reports_error_cleanly(self):
        problem_400 = {
            "statusCode": 400,
            "body": {
                "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "title": "No se pudo identificar el contenido de dataset.json.",
                "status": 400,
                "detail": "Formato '.json' no soportado. Guarde el archivo como .csv o .xlsx."
            }
        }
        analysis = run_node(self.workflow, "Analizar viabilidad", problem_400)
        self.assertFalse(analysis["valid"])
        self.assertEqual("unsupported_file_format", analysis["error"])
        self.assertEqual("Formato no soportado", analysis["veredicto"])
        self.assertEqual("Formato no soportado", analysis["dictamen"]["veredicto"])
        self.assertEqual(1, analysis["dictamen"]["importacion"]["errores"])
        self.assertEqual("400", analysis["dictamen"]["importacion"]["estado"])
        self.assertIn(".csv o .xlsx", analysis["dictamen"]["error"])

        result = run_node(self.workflow, "Prepare Result", {"output": None}, nodes={"Analizar viabilidad": analysis})
        self.assertEqual("Formato no soportado", result["veredicto"])
        self.assertEqual("unsupported_file_format", result["agentError"])
        self.assertIn("Formato no soportado", result["resumen"])
        self.assertIn("Formato '.json' no soportado. Guarde el archivo como .csv o .xlsx.", result["resumen"])
        self.assertNotIn("Viable con limitaciones", result["resumen"])
        self.assertNotIn("sin errores", result["resumen"])

    def test_api_internal_error_500_does_not_generate_viable_verdict(self):
        error_500 = {
            "statusCode": 500,
            "body": {
                "title": "Error interno al ejecutar el dry-run",
                "status": 500,
                "detail": "Fallo de conexión con el motor de base de datos"
            }
        }
        analysis = run_node(self.workflow, "Analizar viabilidad", error_500)
        self.assertFalse(analysis["valid"])
        self.assertEqual("internal_api_error", analysis["error"])
        self.assertEqual("Error interno del importador", analysis["veredicto"])
        self.assertEqual(1, analysis["dictamen"]["importacion"]["errores"])

        result = run_node(self.workflow, "Prepare Result", {"output": None}, nodes={"Analizar viabilidad": analysis})
        self.assertEqual("Error interno del importador", result["veredicto"])
        self.assertNotIn("Viable con limitaciones", result["resumen"])
        self.assertNotIn("sin errores", result["resumen"])
        self.assertIn("Fallo de conexión con el motor de base de datos", result["resumen"])


if __name__ == "__main__":
    unittest.main()
