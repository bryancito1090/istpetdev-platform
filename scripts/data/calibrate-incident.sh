#!/usr/bin/env bash
# Calibración del escenario reproducible demo-region con incidente cierre-norte.
# Flujo: Reset -> Plan -> Publish -> Complete 2 paradas -> Incident -> Replan -> Decisiones

set -euo pipefail

BASE_URL="${API_URL:-http://localhost:5000}"
TOKEN="${AUTH_TOKEN:-}"

AUTH_HEADER=""
if [ -n "$TOKEN" ]; then
  AUTH_HEADER="Authorization: Bearer $TOKEN"
fi

echo "=== 1. Reset con demo-region ==="
curl -s -X POST "$BASE_URL/api/v1/demo/reset?fixture=demo-region" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json"

echo -e "\n=== 2. Calcular Riesgos ==="
curl -s -X GET "$BASE_URL/api/v1/risks" \
  -H "$AUTH_HEADER"

echo -e "\n=== 3. Crear Propuesta de Planificación ==="
PLAN_RESP=$(curl -s -X POST "$BASE_URL/api/v1/route-plans" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json" \
  -d '{"date":"2026-10-12","startWarehouseCode":"BOD-SUR","endWarehouseCode":"BOD-SUR"}')
echo "$PLAN_RESP"

echo -e "\n=== 4. Disparar Incidente cierre-norte ==="
INC_RESP=$(curl -s -X POST "$BASE_URL/api/v1/demo/incidents" \
  -H "$AUTH_HEADER" \
  -H "Content-Type: application/json" \
  -d '{"preset":"cierre-norte"}')
echo "$INC_RESP"

echo -e "\n=== Calibración completada ==="
