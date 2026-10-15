#!/usr/bin/env bash
# ==============================================================================
# Runner de escenarios de caos para Importador (#117)
# Escenarios cubiertos:
#   - ESC-21: CSV corruptos (codificación mixta, columnas desplazadas, sin encabezado)
#   - ESC-22: Archivo masivo (10 000 filas < 30 s) y rechazo > 10 MiB (413)
#   - ESC-28: Reimportación de corte de inventario anterior a recepción (007 FR-10)
# ==============================================================================

set -uo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:5000/api/v1}"
API_BASE_URL="${API_BASE_URL%/}"
ISTPETDEV_PLANNER_TOKEN="${ISTPETDEV_PLANNER_TOKEN:-}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
FIXTURES_CAOS_DIR="$ROOT_DIR/data/fixtures/demo-region-caos"
CORRUPTOS_DIR="$FIXTURES_CAOS_DIR/corruptos"
GEN_SCRIPT="$SCRIPT_DIR/generate-mass-consumos.py"

TMP_FILES=()
cleanup() {
  for f in "${TMP_FILES[@]}"; do
    rm -f "$f" 2>/dev/null || true
  done
}
trap cleanup EXIT

echo "=================================================================="
echo "IstpetDev Platform - Runner de Escenarios de Caos (#117)"
echo "API Base URL: $API_BASE_URL"
if [ -n "$ISTPETDEV_PLANNER_TOKEN" ]; then
  echo "Autenticación: Token de Planificador presente"
else
  echo "Autenticación: [AVISO] ISTPETDEV_PLANNER_TOKEN no definido"
fi
echo "=================================================================="
echo ""

AUTH_HEADER=()
if [ -n "$ISTPETDEV_PLANNER_TOKEN" ]; then
  AUTH_HEADER=(-H "Authorization: Bearer $ISTPETDEV_PLANNER_TOKEN")
fi

OVERALL_STATUS=0

# ------------------------------------------------------------------------------
# ESC-21: CSV corruptos con dryRun=true
# Espera 422 Unprocessable Entity con fila y campo indicados; applied=false.
# ------------------------------------------------------------------------------
echo "--- Ejecutando ESC-21: Archivos CSV corruptos (dryRun=true) ---"
ESC21_PASS=true

if [ ! -d "$CORRUPTOS_DIR" ]; then
  echo "[FAIL] ESC-21: No se encontró el directorio $CORRUPTOS_DIR"
  ESC21_PASS=false
else
  for csv_file in "$CORRUPTOS_DIR"/*.csv; do
    [ -f "$csv_file" ] || continue
    csv_name="$(basename "$csv_file")"

    HTTP_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/imports?dryRun=true" \
      "${AUTH_HEADER[@]}" \
      -F "consumos=@$csv_file")

    HTTP_CODE=$(tail -n1 <<< "$HTTP_RESPONSE")
    BODY=$(sed '$ d' <<< "$HTTP_RESPONSE")

    if [ "$HTTP_CODE" = "401" ]; then
      echo "  - $csv_name: HTTP 401 Unauthorized (falta ISTPETDEV_PLANNER_TOKEN)"
      ESC21_PASS=false
      break
    fi

    # Verificar código 422 y presencia de fila y campo en las incidencias
    CHECK_RESULT=$(python3 -c "
import sys, json
try:
    data = json.loads(sys.argv[1])
    applied = data.get('applied', True)
    issues = data.get('issues', [])
    has_row_and_field = any(
        i.get('row') is not None and i.get('row') >= 0 and i.get('field')
        for i in issues
    )
    if not applied and has_row_and_field:
        sample = next((f\"fila {i.get('row')}, campo '{i.get('field')}': {i.get('code')}\" for i in issues if i.get('field')), '')
        print(f'OK|{sample}')
        sys.exit(0)
    else:
        print(f'FAIL|applied={applied}, issues={len(issues)}')
        sys.exit(1)
except Exception as ex:
    print(f'ERROR|{ex}')
    sys.exit(2)
" "$BODY" 2>/dev/null || echo "ERROR|parse_error")

    STATUS="${CHECK_RESULT%%|*}"
    DETAIL="${CHECK_RESULT#*|}"

    if [ "$HTTP_CODE" = "422" ] && [ "$STATUS" = "OK" ]; then
      echo "  ✓ $csv_name: 422 detectado ($DETAIL, applied=false)"
    else
      echo "  ✗ $csv_name: HTTP $HTTP_CODE inesperado ($DETAIL)"
      ESC21_PASS=false
    fi
  done
fi

if [ "$ESC21_PASS" = true ]; then
  echo "[PASS] ESC-21: Todos los CSV corruptos fueron rechazados con 422, indicando fila y campo sin aplicar cambios."
else
  echo "[FAIL] ESC-21: Uno o más CSV corruptos no fueron rechazados correctamente con 422."
  OVERALL_STATUS=1
fi
echo ""

# ------------------------------------------------------------------------------
# ESC-22: Archivo masivo (10 000 filas < 30 s) y archivo > 10 MiB (413)
# ------------------------------------------------------------------------------
echo "--- Ejecutando ESC-22: Archivo masivo y límite de tamaño ---"
ESC22_PASS=true

TMP_10K=$(mktemp /tmp/consumos-10k-XXXXXX.csv)
TMP_FILES+=("$TMP_10K")

echo "  Generando 10 000 filas de consumos reproducibles..."
python3 "$GEN_SCRIPT" --rows 10000 --out "$TMP_10K" --seed 42 >/dev/null

START_TIME=$(date +%s%N)
HTTP_RESPONSE_10K=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/imports?dryRun=true" \
  "${AUTH_HEADER[@]}" \
  -F "consumos=@$TMP_10K")
END_TIME=$(date +%s%N)

HTTP_CODE_10K=$(tail -n1 <<< "$HTTP_RESPONSE_10K")
BODY_10K=$(sed '$ d' <<< "$HTTP_RESPONSE_10K")

DURATION_MS=$(( (END_TIME - START_TIME) / 1000000 ))
DURATION_SEC=$(awk "BEGIN {printf \"%.2f\", $DURATION_MS / 1000}")

if [ "$HTTP_CODE_10K" = "401" ]; then
  echo "  - 10 000 filas: HTTP 401 Unauthorized (falta ISTPETDEV_PLANNER_TOKEN)"
  ESC22_PASS=false
elif [ "$HTTP_CODE_10K" = "200" ] && [ "$DURATION_MS" -lt 30000 ]; then
  echo "  ✓ 10 000 filas importadas en seco en ${DURATION_SEC} s (< 30 s) con HTTP 200"
else
  echo "  ✗ 10 000 filas: HTTP $HTTP_CODE_10K en ${DURATION_SEC} s (esperado 200 en < 30 s)"
  ESC22_PASS=false
fi

# Archivo > 10 MiB (10 MiB = 10485760 bytes)
TMP_OVER_10MB=$(mktemp /tmp/consumos-large-XXXXXX.csv)
TMP_FILES+=("$TMP_OVER_10MB")

echo "  Generando archivo > 10 MiB (10.5 MiB)..."
python3 -c "
with open('$TMP_OVER_10MB', 'wb') as f:
    f.write(b'point_code,sku,date,quantity,period_days\n')
    line = b'HOS-N01,DES-001,2026-10-15,10,1\n'
    target = 10 * 1024 * 1024 + 512 * 1024  # 10.5 MiB
    chunk = line * 1000
    written = len(b'point_code,sku,date,quantity,period_days\n')
    while written < target:
        f.write(chunk)
        written += len(chunk)
"

HTTP_RESPONSE_10MB=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/imports?dryRun=true" \
  "${AUTH_HEADER[@]}" \
  -F "consumos=@$TMP_OVER_10MB")
HTTP_CODE_10MB=$(tail -n1 <<< "$HTTP_RESPONSE_10MB")

if [ "$HTTP_CODE_10MB" = "413" ]; then
  echo "  ✓ Archivo de $(ls -lh "$TMP_OVER_10MB" | awk '{print $5}') rechazado con HTTP 413 Payload Too Large"
else
  echo "  ✗ Archivo > 10 MiB: HTTP $HTTP_CODE_10MB (esperado 413 Payload Too Large)"
  ESC22_PASS=false
fi

if [ "$ESC22_PASS" = true ]; then
  echo "[PASS] ESC-22: 10 000 filas importadas en ${DURATION_SEC} s (< 30 s) y archivo > 10 MiB rechazado con 413."
else
  echo "[FAIL] ESC-22: Umbral de tiempo (30 s) o límite de tamaño (10 MiB / 413) no cumplido."
  OVERALL_STATUS=1
fi
echo ""

# ------------------------------------------------------------------------------
# ESC-28: Reimportación de corte de inventario anterior a recepción aceptada
# Hoy se espera FAIL documentado: 007 FR-10 sin implementar. El script lo reporta sin abortar.
# ------------------------------------------------------------------------------
echo "--- Ejecutando ESC-28: Reimportar corte de stock anterior a recepción aceptada ---"

TMP_CORTE=$(mktemp /tmp/inventario-corte-XXXXXX.csv)
TMP_FILES+=("$TMP_CORTE")

cat << 'EOF' > "$TMP_CORTE"
location_kind,location_code,sku,lot_code,quantity
service_point,HOS-N01,DES-01,L-DES-2610,50
EOF

HTTP_RESPONSE_28=$(curl -s -w "\n%{http_code}" -X POST "$API_BASE_URL/imports?dryRun=true" \
  "${AUTH_HEADER[@]}" \
  -F "inventario=@$TMP_CORTE")
HTTP_CODE_28=$(tail -n1 <<< "$HTTP_RESPONSE_28")
BODY_28=$(sed '$ d' <<< "$HTTP_RESPONSE_28")

if [ "$HTTP_CODE_28" = "401" ]; then
  echo "  - Corte de stock: HTTP 401 Unauthorized (falta ISTPETDEV_PLANNER_TOKEN)"
  echo "[FAIL] ESC-28: No autenticado para probar reimportación de stock."
else
  # Si devuelve 200, significa que no rechaza el corte anterior a la recepción (FR-10 sin implementar)
  if [ "$HTTP_CODE_28" = "200" ]; then
    echo "  [INFO] Servidor aceptó el corte con HTTP 200 sin validar recepciones previas (FR-10 pendiente)"
    echo "[FAIL] ESC-28: Reimportar corte de stock anterior a recepción aceptada (esperado: 007 FR-10 sin implementar)"
  elif [ "$HTTP_CODE_28" = "422" ] || [ "$HTTP_CODE_28" = "409" ]; then
    echo "[PASS] ESC-28: Corte anterior rechazado con HTTP $HTTP_CODE_28 con informe de conflicto."
  else
    echo "[FAIL] ESC-28: Respuesta inesperada HTTP $HTTP_CODE_28."
  fi
fi
echo ""

echo "=================================================================="
echo "Resumen final de ejecución:"
if [ "$ESC21_PASS" = true ]; then echo "  ESC-21: PASS"; else echo "  ESC-21: FAIL"; fi
if [ "$ESC22_PASS" = true ]; then echo "  ESC-22: PASS"; else echo "  ESC-22: FAIL"; fi
echo "  ESC-28: FAIL (esperado: 007 FR-10 sin implementar)"
echo "=================================================================="

exit $OVERALL_STATUS
