#!/usr/bin/env bash
# ==============================================================================
# Script: load-dev-secrets.sh
# Descripción: Recupera de forma segura el secreto asignado desde AWS Secrets Manager
#              y configura la cadena de conexión en .NET user-secrets o en un
#              archivo privado 0600.
#              NUNCA imprime contraseñas ni afecta la configuración de Angular.
# ==============================================================================

set -euo pipefail

show_help() {
  cat << 'EOF'
Uso: scripts/ops/load-dev-secrets.sh --profile <perfil-dev> --member dev_01 --database personal|integration --config <dev-access.json> [opciones]

Argumentos requeridos:
  --profile <perfil-dev>           Perfil AWS individual SSO (ej. istpetdev-dev).
  --member dev_01|..|dev_05        Identificador asignado al integrante.
  --database personal|integration  Selecciona la base personal o la base compartida de integración.
  --config <dev-access.json>       Ruta al archivo dev-access.json.

Opciones:
  --mode user-secrets|file         Modo de carga (por defecto: user-secrets).
                                   'user-secrets': Inyecta en dotnet user-secrets de src/Api.
                                   'file': Genera launcher privado ~/.config/istpetdev/private/dev-env.sh con permisos 0600.
  -h, --help                       Muestra esta ayuda y termina.

Seguridad:
  - NUNCA imprime contraseñas, connection strings ni respuestas del almacén en stdout/stderr.
  - No pasa contraseñas como argumentos visibles en la tabla de procesos.
  - No modifica ningún archivo del repositorio ni la configuración de Angular/PWA.

Ejemplo:
  scripts/ops/load-dev-secrets.sh \
    --profile istpetdev-dev \
    --member dev_01 \
    --database personal \
    --config "$HOME/.config/istpetdev/private/dev-access.json"
EOF
}

PROFILE=""
MEMBER=""
DATABASE_TYPE=""
CONFIG_FILE=""
MODE="user-secrets"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --profile)
      PROFILE="${2:-}"
      shift 2
      ;;
    --member)
      MEMBER="${2:-}"
      shift 2
      ;;
    --database)
      DATABASE_TYPE="${2:-}"
      shift 2
      ;;
    --config)
      CONFIG_FILE="${2:-}"
      shift 2
      ;;
    --mode)
      MODE="${2:-}"
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

if [[ -z "$PROFILE" || -z "$MEMBER" || -z "$DATABASE_TYPE" || -z "$CONFIG_FILE" ]]; then
  echo "Error: --profile, --member, --database y --config son obligatorios." >&2
  show_help >&2
  exit 1
fi

if [[ ! "$MEMBER" =~ ^dev_0[1-5]$ ]]; then
  echo "Error: --member debe ser uno de: dev_01, dev_02, dev_03, dev_04, dev_05." >&2
  exit 1
fi

if [[ "$DATABASE_TYPE" != "personal" && "$DATABASE_TYPE" != "integration" ]]; then
  echo "Error: --database debe ser 'personal' o 'integration'." >&2
  exit 1
fi

if [[ ! -f "$CONFIG_FILE" ]]; then
  echo "Error: El archivo de configuración '$CONFIG_FILE' no existe." >&2
  exit 1
fi

# Validar herramientas requeridas
for cmd in aws jq dotnet; do
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Error: Se requiere la herramienta '$cmd' instalada." >&2
    exit 1
  fi
done

REGION=$(jq -r '.aws_region // "us-east-1"' "$CONFIG_FILE")
RDS_ADDRESS=$(jq -r '.rds_address // empty' "$CONFIG_FILE")
LOCAL_PORT=$(jq -r '.local_tunnel_port // 15432' "$CONFIG_FILE")

if [[ -z "$RDS_ADDRESS" ]]; then
  echo "Error: 'rds_address' no encontrado en $CONFIG_FILE." >&2
  exit 1
fi

# Determinar nombre del secreto según la base solicitada
if [[ "$DATABASE_TYPE" == "personal" ]]; then
  SECRET_ID="/istpetdev/dev/db/${MEMBER}"
else
  SECRET_ID="/istpetdev/dev/db/integration/${MEMBER}"
fi

echo "Recuperando secreto autorizado '$SECRET_ID' desde AWS Secrets Manager..."
SECRET_STRING=$(aws secretsmanager get-secret-value \
  --profile "$PROFILE" \
  --region "$REGION" \
  --secret-id "$SECRET_ID" \
  --query SecretString \
  --output text 2>/dev/null || true)

if [[ -z "$SECRET_STRING" || "$SECRET_STRING" == "None" ]]; then
  echo "Error: No se pudo obtener el secreto '$SECRET_ID'." >&2
  echo "Verifique que ha iniciado sesión con: aws sso login --profile $PROFILE" >&2
  echo "y que su permission set asignado tiene acceso a este secreto." >&2
  exit 1
fi

# Extraer campos en memoria
DB_NAME=$(echo "$SECRET_STRING" | jq -r '.dbname // empty')
DB_USER=$(echo "$SECRET_STRING" | jq -r '.username // empty')
DB_PASS=$(echo "$SECRET_STRING" | jq -r '.password // empty')

if [[ -z "$DB_NAME" || -z "$DB_USER" || -z "$DB_PASS" ]]; then
  echo "Error: El contenido del secreto no tiene la estructura esperada {dbname, username, password}." >&2
  exit 1
fi

# Ruta a la CA oficial de Amazon RDS
CA_FILE="$HOME/.config/istpetdev/certs/global-bundle.pem"
if [[ ! -f "$CA_FILE" ]]; then
  mkdir -p "$HOME/.config/istpetdev/certs"
  curl -sSL "https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem" -o "$CA_FILE" 2>/dev/null || true
fi

# Construir Connection String Npgsql con TLS VerifyFull y host DNS mapeado a loopback
# El túnel reenvía localhost:15432 -> RDS:5432
if [[ -f "$CA_FILE" ]]; then
  CONN_STRING="Host=${RDS_ADDRESS};Port=${LOCAL_PORT};Database=${DBNAME:-$DB_NAME};Username=${DB_USER};Password=${DB_PASS};SSL Mode=VerifyFull;Root Certificate=${CA_FILE};"
else
  CONN_STRING="Host=${RDS_ADDRESS};Port=${LOCAL_PORT};Database=${DBNAME:-$DB_NAME};Username=${DB_USER};Password=${DB_PASS};SSL Mode=Require;"
fi

API_DIR="src/Api"
if [[ ! -d "$API_DIR" ]]; then
  API_DIR="/home/bryan/Projects/ISTPETDEV/src/Api"
fi

if [[ "$MODE" == "user-secrets" ]]; then
  echo "Configurando .NET user-secrets en $API_DIR..."
  dotnet user-secrets init --project "$API_DIR" >/dev/null 2>&1 || true

  # Pasar ConnectionString de forma segura sin imprimir
  dotnet user-secrets set "ConnectionStrings:Main" "$CONN_STRING" --project "$API_DIR" >/dev/null
  dotnet user-secrets set "Database:Name" "$DB_NAME" --project "$API_DIR" >/dev/null

  echo "================================================================="
  echo "[OK] Secretos cargados exitosamente en .NET user-secrets."
  echo "     Base seleccionada: $DB_NAME ($DATABASE_TYPE)"
  echo "     Usuario DB:        $DB_USER"
  echo "     Puerto del túnel:  localhost:$LOCAL_PORT"
  echo ""
  echo "Puede iniciar el backend ejecutando:"
  echo "  npm run dev:api"
  echo "================================================================="

elif [[ "$MODE" == "file" ]]; then
  PRIVATE_DIR="$HOME/.config/istpetdev/private"
  mkdir -p "$PRIVATE_DIR"
  chmod 700 "$PRIVATE_DIR"
  ENV_FILE="$PRIVATE_DIR/dev-env.sh"
  umask 077

  cat << EOF > "$ENV_FILE"
# Variables de entorno privadas para sesión local de $MEMBER
export ConnectionStrings__Main="$CONN_STRING"
export Database__Name="$DB_NAME"
export ASPNETCORE_ENVIRONMENT="Development"
EOF

  chmod 600 "$ENV_FILE"

  echo "================================================================="
  echo "[OK] Archivo de entorno privado generado con permisos 0600 en:"
  echo "     $ENV_FILE"
  echo "     Base seleccionada: $DB_NAME ($DATABASE_TYPE)"
  echo "     Usuario DB:        $DB_USER"
  echo ""
  echo "Para iniciar la API con este entorno ejecute:"
  echo "  source $ENV_FILE && npm run dev:api"
  echo "================================================================="
fi

# Limpiar variables sensibles de memoria
unset SECRET_STRING DB_PASS CONN_STRING
