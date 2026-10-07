#!/usr/bin/env bash
# ==============================================================================
# Script: export-dev-config.sh
# Descripción: Extrae metadata no confidencial de terraform output -json y genera
#              el archivo de configuración de acceso dev (dev-access.json).
#              NO exporta credenciales maestras ni valores de secretos.
# ==============================================================================

set -euo pipefail

show_help() {
  cat << 'EOF'
Uso: scripts/ops/export-dev-config.sh --outputs <dev-outputs.json> --output-dir <directorio-privado>

Argumentos requeridos:
  --outputs <archivo>       Ruta al archivo JSON generado por 'terraform output -json'.
  --output-dir <directorio> Directorio privado donde se guardará dev-access.json
                            (ejemplo: $HOME/.config/istpetdev/private).

Opciones:
  -h, --help                Muestra esta ayuda y termina.

Ejemplo:
  terraform -chdir=infra/terraform/environments/dev output -json > /tmp/dev-outputs.json
  scripts/ops/export-dev-config.sh \
    --outputs /tmp/dev-outputs.json \
    --output-dir "$HOME/.config/istpetdev/private"
EOF
}

OUTPUTS_FILE=""
OUTPUT_DIR=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --outputs)
      OUTPUTS_FILE="${2:-}"
      shift 2
      ;;
    --output-dir)
      OUTPUT_DIR="${2:-}"
      shift 2
      ;;
    -h|--help)
      show_help
      exit 0
      ;;
    *)
      echo "Error: Argumento desconocido: $1" >&2
      echo "Ejecute '$0 --help' para consultar la sintaxis." >&2
      exit 1
      ;;
  esac
done

if [[ -z "$OUTPUTS_FILE" || -z "$OUTPUT_DIR" ]]; then
  echo "Error: --outputs y --output-dir son obligatorios." >&2
  show_help >&2
  exit 1
fi

if [[ ! -f "$OUTPUTS_FILE" ]]; then
  echo "Error: El archivo de outputs '$OUTPUTS_FILE' no existe." >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "Error: Se requiere la utilidad 'jq' para procesar el JSON." >&2
  exit 1
fi

# Validar que sea JSON válido
if ! jq empty "$OUTPUTS_FILE" 2>/dev/null; then
  echo "Error: El archivo '$OUTPUTS_FILE' no contiene un JSON válido." >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"
chmod 700 "$OUTPUT_DIR"

DEST_FILE="$OUTPUT_DIR/dev-access.json"
umask 077

# Extraer metadata de Terraform outputs asegurando NO incluir secretos
jq -n \
  --slurpfile tf "$OUTPUTS_FILE" \
  '{
    environment: "dev",
    aws_region: "us-east-1",
    vpc_id: ($tf[0].vpc_id.value // ""),
    ec2_access_node_id: ($tf[0].ec2_access_node_id.value // ""),
    ec2_access_node_public_ip: ($tf[0].ec2_access_node_public_ip.value // ""),
    rds_instance_id: ($tf[0].rds_instance_id.value // ""),
    rds_endpoint: ($tf[0].rds_endpoint.value // ""),
    rds_address: ($tf[0].rds_address.value // ""),
    rds_port: ($tf[0].rds_port.value // 5432),
    local_tunnel_port: 15432,
    evidence_bucket_name: ($tf[0].evidence_bucket_name.value // "")
  }' > "$DEST_FILE"

chmod 600 "$DEST_FILE"

echo "================================================================="
echo "Configuración generada exitosamente en:"
echo "  $DEST_FILE"
echo ""
echo "Resumen de metadata pública extraída (sin credenciales):"
jq '{aws_region, ec2_access_node_id, rds_address, local_tunnel_port, evidence_bucket_name}' "$DEST_FILE"
echo "================================================================="
echo "El archivo dev-access.json puede compartirse de forma segura con"
echo "los integrantes del equipo mediante canal privado autorizado."
