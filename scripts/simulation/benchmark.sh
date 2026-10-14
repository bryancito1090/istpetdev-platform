#!/usr/bin/env bash
set -euo pipefail

# Benchmark de la corrida nacional completa (Spec 005 - T8)
# Nunca ejecutar sobre la instancia de la demo en vivo.

API_BASE="${API_BASE:-http://localhost:5000}"
DATASET_VERSION="${DATASET_VERSION:-national-v1}"
ACCESS_TOKEN="${ACCESS_TOKEN:-}"
OP_DB="${OPERATIONAL_DB:-istpetdev_demo}"
SIM_DB="${SIMULATION_DB:-istpetdev_simulation}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-3600}" # 60 minutos límite CE-07
OUTPUT_DIR="${OUTPUT_DIR:-docs/evidence/simulation-benchmark}"

echo "=== INICIANDO BENCHMARK NACIONAL (Spec 005) ==="
echo "API Base: $API_BASE"
echo "Dataset Version: $DATASET_VERSION"

# Validación de aislamiento de bases de datos
if [ "$OP_DB" = "$SIM_DB" ]; then
    echo "ERROR: SIMULATION_DATABASE_NOT_ISOLATED: La base de simulación ($SIM_DB) debe ser diferente de la operativa ($OP_DB)." >&2
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

AUTH_HEADER=""
if [ -n "$ACCESS_TOKEN" ]; then
    AUTH_HEADER="Authorization: Bearer $ACCESS_TOKEN"
fi

echo "Lanzando corrida de simulación comparativa de 90 días (3 base + 1 adversa × 3 políticas)..."
START_TIME=$(date +%s)

PAYLOAD='{
  "datasetVersion": "'"$DATASET_VERSION"'",
  "seeds": [42, 101, 2024],
  "includeAdverse": true
}'

RESP=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE/api/v1/simulations/comparisons" \
    -H "Content-Type: application/json" \
    ${AUTH_HEADER:+-H "$AUTH_HEADER"} \
    -d "$PAYLOAD")

HTTP_CODE=$(echo "$RESP" | tail -n1)
BODY=$(echo "$RESP" | head -n -1)

if [ "$HTTP_CODE" != "202" ]; then
    echo "ERROR: Falló el lanzamiento de la simulación. HTTP $HTTP_CODE: $BODY" >&2
    exit 1
fi

COMPARISON_ID=$(echo "$BODY" | grep -o '"comparisonId":"[^"]*' | cut -d'"' -f4)
echo "Comparación encolada con ID: $COMPARISON_ID"

# Polling de estado cada 10 segundos
while true; do
    CURRENT_TIME=$(date +%s)
    ELAPSED=$((CURRENT_TIME - START_TIME))

    if [ "$ELAPSED" -ge "$TIMEOUT_SECONDS" ]; then
        echo "ERROR: Tiempo límite superado (> $TIMEOUT_SECONDS s). Bloqueo medido registrado." >&2
        exit 1
    fi

    STATUS_RESP=$(curl -s -X GET "$API_BASE/api/v1/simulations/comparisons/$COMPARISON_ID" \
        ${AUTH_HEADER:+-H "$AUTH_HEADER"})

    STATUS=$(echo "$STATUS_RESP" | grep -o '"status":"[^"]*' | cut -d'"' -f4 || echo "Unknown")
    PROGRESS=$(echo "$STATUS_RESP" | grep -o '"progressPercentage":[0-9]*' | cut -d':' -f2 || echo "0")

    echo "[$ELAPSED s] Estado: $STATUS, Progreso: $PROGRESS%"

    if [ "$STATUS" = "Completed" ]; then
        echo "=== CORRIDA COMPLETADA EXITOSAMENTE en $ELAPSED segundos ==="
        echo "$STATUS_RESP" > "$OUTPUT_DIR/comparison-$COMPARISON_ID.json"
        break
    elif [ "$STATUS" = "Failed" ] || [ "$STATUS" = "Interrupted" ]; then
        echo "ERROR: La corrida terminó con estado: $STATUS" >&2
        echo "$STATUS_RESP" > "$OUTPUT_DIR/failure-$COMPARISON_ID.json"
        exit 1
    fi

    sleep 10
done

# Descargar exportación reproducible
echo "Descargando paquete ZIP reproducible..."
curl -s -X GET "$API_BASE/api/v1/simulations/comparisons/$COMPARISON_ID/export" \
    ${AUTH_HEADER:+-H "$AUTH_HEADER"} \
    -o "$OUTPUT_DIR/simulation-$COMPARISON_ID.zip"

echo "Evidencia guardada en $OUTPUT_DIR/simulation-$COMPARISON_ID.zip"
