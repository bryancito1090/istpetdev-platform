#!/usr/bin/env bash
# ==============================================================================
# Script: dev-tunnel.sh
# Descripción: Inicia un túnel seguro AWS Systems Manager Session Manager
#              hacia la base de datos PostgreSQL privada en Amazon RDS.
#              Mapea el puerto local 15432 hacia el puerto 5432 de RDS.
# ==============================================================================

set -euo pipefail

show_help() {
  cat << 'EOF'
Uso: scripts/ops/dev-tunnel.sh --profile <perfil> --config <dev-access.json> [opciones]

Argumentos requeridos:
  --profile <perfil>        Nombre del perfil AWS configurado (ej. istpetdev-dev o istpetdev-admin).
  --config <dev-access.json> Ruta al archivo JSON con metadata de acceso generado por export-dev-config.sh.

Opciones:
  --local-port <puerto>     Puerto local a enlazar (por defecto: 15432 o el valor en dev-access.json).
  -h, --help                Muestra esta ayuda y termina.

Requisitos previos:
  1. AWS CLI v2 y session-manager-plugin instalados en el sistema.
  2. Sesión activa en AWS Identity Center:
       aws sso login --profile <perfil>
  3. Para validación TLS VerifyFull en Npgsql/psql:
       Añadir en /etc/hosts de esta máquina:
       127.0.0.1 <RDS_ADDRESS>
     (El script comprobará y mostrará la instrucción exacta si no está presente).

Ejemplo:
  scripts/ops/dev-tunnel.sh \
    --profile istpetdev-dev \
    --config "$HOME/.config/istpetdev/private/dev-access.json"
EOF
}

PROFILE=""
CONFIG_FILE=""
LOCAL_PORT=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --profile)
      PROFILE="${2:-}"
      shift 2
      ;;
    --config)
      CONFIG_FILE="${2:-}"
      shift 2
      ;;
    --local-port)
      LOCAL_PORT="${2:-}"
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

if [[ -z "$PROFILE" || -z "$CONFIG_FILE" ]]; then
  echo "Error: --profile y --config son obligatorios." >&2
  show_help >&2
  exit 1
fi

if [[ ! -f "$CONFIG_FILE" ]]; then
  echo "Error: El archivo de configuración '$CONFIG_FILE' no existe." >&2
  exit 1
fi

# Validar herramientas requeridas
if ! command -v aws >/dev/null 2>&1; then
  echo "Error: 'aws' CLI no está instalado o no se encuentra en el PATH." >&2
  exit 1
fi

if ! command -v session-manager-plugin >/dev/null 2>&1; then
  echo "Error: 'session-manager-plugin' no está instalado o no se encuentra en el PATH." >&2
  echo "Descargue el plugin oficial desde: https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "Error: Se requiere 'jq' para leer la configuración." >&2
  exit 1
fi

REGION=$(jq -r '.aws_region // "us-east-1"' "$CONFIG_FILE")
INSTANCE_ID=$(jq -r '.ec2_access_node_id // empty' "$CONFIG_FILE")
RDS_ADDRESS=$(jq -r '.rds_address // empty' "$CONFIG_FILE")
CONFIG_LOCAL_PORT=$(jq -r '.local_tunnel_port // 15432' "$CONFIG_FILE")

if [[ -z "$LOCAL_PORT" ]]; then
  LOCAL_PORT="$CONFIG_LOCAL_PORT"
fi

if [[ -z "$INSTANCE_ID" || -z "$RDS_ADDRESS" ]]; then
  echo "Error: dev-access.json debe contener 'ec2_access_node_id' y 'rds_address'." >&2
  exit 1
fi

# 1. Comprobación y descarga de la CA oficial de Amazon RDS
CERTS_DIR="$HOME/.config/istpetdev/certs"
CA_FILE="$CERTS_DIR/global-bundle.pem"
mkdir -p "$CERTS_DIR"
chmod 700 "$CERTS_DIR"

if [[ ! -f "$CA_FILE" ]]; then
  echo "Descargando certificado raíz oficial de Amazon RDS (global-bundle.pem)..."
  if curl -sSL "https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem" -o "$CA_FILE"; then
    chmod 644 "$CA_FILE"
    echo "Certificado guardado en: $CA_FILE"
  else
    echo "Advertencia: No se pudo descargar automáticamente la CA. Descárguela manualmente en $CA_FILE" >&2
  fi
fi

# 2. Comprobación de mapeo DNS local para TLS VerifyFull
echo "================================================================="
echo "Comprobación de resolución local para TLS VerifyFull:"
if grep -q "$RDS_ADDRESS" /etc/hosts 2>/dev/null; then
  echo "  [OK] El Host DNS real '$RDS_ADDRESS' está mapeado en /etc/hosts a loopback."
else
  echo "  [AVISO] Para conectar con SSL Mode=VerifyFull conservando la verificación"
  echo "          del nombre de host en el certificado de RDS, agregue la siguiente"
  echo "          línea a su archivo /etc/hosts:"
  echo ""
  echo "    127.0.0.1  $RDS_ADDRESS"
  echo ""
  echo "  Comando para añadir:"
  echo "    echo '127.0.0.1  $RDS_ADDRESS' | sudo tee -a /etc/hosts"
  echo "  Comando para retirar al terminar la jornada:"
  echo "    sudo sed -i '/$RDS_ADDRESS/d' /etc/hosts"
fi
echo "================================================================="

echo "Iniciando túnel SSM hacia RDS..."
echo "  Perfil AWS:      $PROFILE"
echo "  Región:          $REGION"
echo "  Nodo EC2:        $INSTANCE_ID"
echo "  Destino RDS:     $RDS_ADDRESS:5432"
echo "  Puerto local:    localhost:$LOCAL_PORT"
echo "Presione Ctrl+C para finalizar la sesión del túnel."
echo "-----------------------------------------------------------------"

exec aws ssm start-session \
  --profile "$PROFILE" \
  --region "$REGION" \
  --target "$INSTANCE_ID" \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters "{\"host\":[\"$RDS_ADDRESS\"],\"portNumber\":[\"5432\"],\"localPortNumber\":[\"$LOCAL_PORT\"]}"
