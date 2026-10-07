#!/usr/bin/env bash
# ==============================================================================
# Script: bootstrap-dev.sh
# Descripción: Inicialización segura e idempotente de bases de datos PostgreSQL
#              y secretos en Amazon RDS para el entorno de desarrollo.
#              Crea 5 bases personales (istpetdev_dev_01..05) y 1 de integración
#              (istpetdev_integration), con roles separados y PostGIS.
#              Solo debe ser ejecutado por el Administrador con túnel activo.
# ==============================================================================

set -euo pipefail

show_help() {
  cat << 'EOF'
Uso: scripts/base_datos/bootstrap-dev.sh --profile <perfil-admin> --config <dev-access.json> --outputs <dev-outputs.json> [opciones]

Argumentos requeridos:
  --profile <perfil-admin>    Perfil administrativo de AWS (ej. istpetdev-admin).
  --config <dev-access.json>  Ruta al archivo dev-access.json generado por export-dev-config.sh.
  --outputs <dev-outputs.json> Ruta al archivo dev-outputs.json generado por terraform output.

Opciones:
  --port <puerto>             Puerto local del túnel hacia RDS (por defecto: 15432).
  --local-test-host <host>    Host para prueba local aislada (omite Secrets Manager).
  --local-test-password <pw>  Contraseña master para prueba local aislada.
  -h, --help                  Muestra esta ayuda y termina.

Seguridad:
  - Requiere tener el túnel SSM abierto en otra terminal (scripts/ops/dev-tunnel.sh).
  - Recupera la contraseña maestra de RDS desde AWS Secrets Manager sin imprimirla.
  - Almacena contraseñas generadas en Secrets Manager (/istpetdev/dev/db/...).
  - Las contraseñas se manejan estrictamente en memoria o mediante archivo .pgpass temporal 0600.
  - Es IDEMPOTENTE: no elimina bases ni rota contraseñas existentes al reejecutar.

Ejemplo de ejecución real:
  scripts/base_datos/bootstrap-dev.sh \
    --profile istpetdev-admin \
    --config "$HOME/.config/istpetdev/private/dev-access.json" \
    --outputs "$HOME/.config/istpetdev/private/dev-outputs.json"
EOF
}

PROFILE=""
CONFIG_FILE=""
OUTPUTS_FILE=""
TUNNEL_PORT="15432"
LOCAL_TEST_HOST=""
LOCAL_TEST_PASSWORD=""

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
    --outputs)
      OUTPUTS_FILE="${2:-}"
      shift 2
      ;;
    --port)
      TUNNEL_PORT="${2:-}"
      shift 2
      ;;
    --local-test-host)
      LOCAL_TEST_HOST="${2:-}"
      shift 2
      ;;
    --local-test-password)
      LOCAL_TEST_PASSWORD="${2:-}"
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

if [[ -z "$LOCAL_TEST_HOST" ]]; then
  if [[ -z "$PROFILE" || -z "$CONFIG_FILE" || -z "$OUTPUTS_FILE" ]]; then
    echo "Error: --profile, --config y --outputs son obligatorios para ejecución AWS." >&2
    show_help >&2
    exit 1
  fi
fi

# Validar utilidades
for cmd in psql jq openssl; do
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "Error: Se requiere la herramienta '$cmd' instalada." >&2
    exit 1
  fi
done

# Preparar archivo .pgpass temporal
PGPASS_FILE=$(mktemp)
chmod 600 "$PGPASS_FILE"
export PGPASSFILE="$PGPASS_FILE"

cleanup() {
  rm -f "$PGPASS_FILE"
  unset PGPASSWORD DB_MASTER_PASS
}
trap cleanup EXIT INT TERM

generate_secure_password() {
  openssl rand -base64 24 | tr -dc 'a-zA-Z0-9' | head -c 24
}

MEMBERS=("dev_01" "dev_02" "dev_03" "dev_04" "dev_05")

if [[ -n "$LOCAL_TEST_HOST" ]]; then
  # Modo prueba local
  DB_HOST="$LOCAL_TEST_HOST"
  DB_PORT="$TUNNEL_PORT"
  DB_MASTER_USER="postgres"
  DB_MASTER_PASS="$LOCAL_TEST_PASSWORD"
  REGION="us-east-1"
  RDS_ADDRESS="localhost"
  echo ">>> EJECUTANDO EN MODO PRUEBA LOCAL CONTRA $DB_HOST:$DB_PORT <<<"
else
  # Modo AWS real con túnel activo
  if ! command -v aws >/dev/null 2>&1; then
    echo "Error: Se requiere 'aws' CLI." >&2
    exit 1
  fi

  REGION=$(jq -r '.aws_region // "us-east-1"' "$CONFIG_FILE")
  RDS_ADDRESS=$(jq -r '.rds_address' "$CONFIG_FILE")
  DB_HOST="127.0.0.1"
  DB_PORT="$TUNNEL_PORT"
  DB_MASTER_USER="istpetadmin"

  echo "Recuperando secreto maestro de RDS desde AWS Secrets Manager..."
  MASTER_SECRET_ARN=$(jq -r '.rds_master_user_secret_arn.value // empty' "$OUTPUTS_FILE")
  if [[ -z "$MASTER_SECRET_ARN" ]]; then
    echo "Error: No se encontró 'rds_master_user_secret_arn' en $OUTPUTS_FILE." >&2
    exit 1
  fi

  MASTER_SECRET_JSON=$(aws secretsmanager get-secret-value \
    --profile "$PROFILE" \
    --region "$REGION" \
    --secret-id "$MASTER_SECRET_ARN" \
    --query SecretString \
    --output text)

  DB_MASTER_PASS=$(echo "$MASTER_SECRET_JSON" | jq -r '.password')
  if [[ -z "$DB_MASTER_PASS" ]]; then
    echo "Error: No se pudo extraer la contraseña maestra del secreto." >&2
    exit 1
  fi
fi

# Configurar PGPASSWORD para conexión administrativa
export PGPASSWORD="$DB_MASTER_PASS"

echo "Verificando conectividad inicial con PostgreSQL en $DB_HOST:$DB_PORT..."
if ! psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_MASTER_USER" -d "postgres" -c "SELECT 1;" >/dev/null 2>&1; then
  echo "Error: No se pudo conectar a PostgreSQL en $DB_HOST:$DB_PORT." >&2
  echo "Asegúrese de que el túnel SSM esté activo en otra terminal (scripts/ops/dev-tunnel.sh)." >&2
  exit 1
fi
echo "[OK] Conexión establecida con el servidor PostgreSQL."

# Función auxiliar para ejecutar SQL con master
run_master_sql() {
  local db="${1:-postgres}"
  local sql="$2"
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_MASTER_USER" -d "$db" -v ON_ERROR_STOP=1 -q -c "$sql"
}

# Función para obtener o crear secreto en Secrets Manager
get_or_create_secret() {
  local secret_name="$1"
  local dbname="$2"
  local username="$3"

  if [[ -n "$LOCAL_TEST_HOST" ]]; then
    # En modo prueba local se genera una contraseña en memoria para el test
    generate_secure_password
    return
  fi

  # Comprobar si ya existe el secreto en AWS Secrets Manager
  local existing_secret
  if existing_secret=$(aws secretsmanager get-secret-value \
      --profile "$PROFILE" \
      --region "$REGION" \
      --secret-id "$secret_name" \
      --query SecretString \
      --output text 2>/dev/null); then
    local pass
    pass=$(echo "$existing_secret" | jq -r '.password // empty')
    if [[ -n "$pass" ]]; then
      echo "$pass"
      return
    fi
  fi

  # Generar nueva contraseña y registrar en Secrets Manager
  local new_pass
  new_pass=$(generate_secure_password)

  local payload
  payload=$(jq -n \
    --arg host "$RDS_ADDRESS" \
    --arg port "15432" \
    --arg dbname "$dbname" \
    --arg username "$username" \
    --arg password "$new_pass" \
    '{host: $host, port: ($port | tonumber), dbname: $dbname, username: $username, password: $password}')

  aws secretsmanager create-secret \
    --profile "$PROFILE" \
    --region "$REGION" \
    --name "$secret_name" \
    --description "Credenciales de acceso dev para $username en $dbname" \
    --secret-string "$payload" >/dev/null

  echo "$new_pass"
}

echo ""
echo "================================================================="
echo "PASO 1: Configuración de bases personales (istpetdev_dev_01..05)"
echo "================================================================="

for member in "${MEMBERS[@]}"; do
  dbname="istpetdev_${member}"
  app_user="user_${member}"
  migrator_user="migrator_${member}"

  echo "Procesando $member ($dbname)..."

  # 1. Obtener o crear secretos
  app_secret_name="/istpetdev/dev/db/${member}"
  migrator_secret_name="/istpetdev/dev/db/${member}/migrator"

  app_pass=$(get_or_create_secret "$app_secret_name" "$dbname" "$app_user")
  migrator_pass=$(get_or_create_secret "$migrator_secret_name" "$dbname" "$migrator_user")

  # 2. Crear base de datos si no existe
  db_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_MASTER_USER" -d "postgres" -tAc "SELECT 1 FROM pg_database WHERE datname = '$dbname';")
  if [[ "$db_exists" != "1" ]]; then
    run_master_sql "postgres" "CREATE DATABASE $dbname;"
    echo "  - Creada base de datos $dbname"
  else
    echo "  - Base de datos $dbname ya existe (conservada)"
  fi

  # Revocar CONNECT de PUBLIC sobre la base de datos para aislamiento estricto
  run_master_sql "postgres" "REVOKE CONNECT ON DATABASE $dbname FROM PUBLIC;"

  # 3. Crear o actualizar rol de aplicación
  app_role_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_MASTER_USER" -d "postgres" -tAc "SELECT 1 FROM pg_roles WHERE rolname = '$app_user';")
  if [[ "$app_role_exists" != "1" ]]; then
    run_master_sql "postgres" "CREATE ROLE $app_user WITH LOGIN PASSWORD '$app_pass' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;"
    echo "  - Creado rol de aplicación $app_user"
  fi

  # 4. Crear o actualizar rol de migrador
  migrator_role_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_MASTER_USER" -d "postgres" -tAc "SELECT 1 FROM pg_roles WHERE rolname = '$migrator_user';")
  if [[ "$migrator_role_exists" != "1" ]]; then
    run_master_sql "postgres" "CREATE ROLE $migrator_user WITH LOGIN PASSWORD '$migrator_pass' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;"
    echo "  - Creado rol migrador $migrator_user"
  fi

  # 5. Permisos de conexión y esquemas
  run_master_sql "postgres" "GRANT CONNECT ON DATABASE $dbname TO $app_user, $migrator_user;"

  # Configurar permisos dentro de la base específica
  # Instalar PostGIS
  run_master_sql "$dbname" "CREATE EXTENSION IF NOT EXISTS postgis;"

  # El migrador puede crear tablas en public
  run_master_sql "$dbname" "GRANT USAGE, CREATE ON SCHEMA public TO $migrator_user;"
  # La aplicación solo puede usar y manipular datos (DML)
  run_master_sql "$dbname" "GRANT USAGE ON SCHEMA public TO $app_user;"
  run_master_sql "$dbname" "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO $app_user;"
  run_master_sql "$dbname" "GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO $app_user;"
  run_master_sql "$dbname" "ALTER DEFAULT PRIVILEGES FOR ROLE $migrator_user IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $app_user;"
  run_master_sql "$dbname" "ALTER DEFAULT PRIVILEGES FOR ROLE $migrator_user IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO $app_user;"
  run_master_sql "$dbname" "ALTER DEFAULT PRIVILEGES FOR ROLE $DB_MASTER_USER IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $app_user;"

  echo "  - Permisos y extensión PostGIS configurados para $dbname."
done

echo ""
echo "================================================================="
echo "PASO 2: Configuración de base compartida (istpetdev_integration)"
echo "================================================================="

integ_db="istpetdev_integration"
integ_migrator="migrator_integration"

integ_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_MASTER_USER" -d "postgres" -tAc "SELECT 1 FROM pg_database WHERE datname = '$integ_db';")
if [[ "$integ_exists" != "1" ]]; then
  run_master_sql "postgres" "CREATE DATABASE $integ_db;"
  echo "- Creada base de datos $integ_db"
else
  echo "- Base de datos $integ_db ya existe (conservada)"
fi

run_master_sql "postgres" "REVOKE CONNECT ON DATABASE $integ_db FROM PUBLIC;"

# Migrador de integración exclusivo de administración
integ_migrator_secret="/istpetdev/dev/db/integration/migrator"
integ_migrator_pass=$(get_or_create_secret "$integ_migrator_secret" "$integ_db" "$integ_migrator")

integ_migrator_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_MASTER_USER" -d "postgres" -tAc "SELECT 1 FROM pg_roles WHERE rolname = '$integ_migrator';")
if [[ "$integ_migrator_exists" != "1" ]]; then
  run_master_sql "postgres" "CREATE ROLE $integ_migrator WITH LOGIN PASSWORD '$integ_migrator_pass' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;"
  echo "- Creado rol migrador de integración $integ_migrator"
fi

run_master_sql "postgres" "GRANT CONNECT ON DATABASE $integ_db TO $integ_migrator;"
run_master_sql "$integ_db" "CREATE EXTENSION IF NOT EXISTS postgis;"
run_master_sql "$integ_db" "GRANT USAGE, CREATE ON SCHEMA public TO $integ_migrator;"

# Crear usuarios individuales de integración para cada integrante
for member in "${MEMBERS[@]}"; do
  integ_user="user_integ_${member}"
  integ_user_secret="/istpetdev/dev/db/integration/${member}"
  integ_user_pass=$(get_or_create_secret "$integ_user_secret" "$integ_db" "$integ_user")

  user_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_MASTER_USER" -d "postgres" -tAc "SELECT 1 FROM pg_roles WHERE rolname = '$integ_user';")
  if [[ "$user_exists" != "1" ]]; then
    run_master_sql "postgres" "CREATE ROLE $integ_user WITH LOGIN PASSWORD '$integ_user_pass' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;"
    echo "  - Creado usuario de integración $integ_user"
  fi

  run_master_sql "postgres" "GRANT CONNECT ON DATABASE $integ_db TO $integ_user;"
  run_master_sql "$integ_db" "GRANT USAGE ON SCHEMA public TO $integ_user;"
  run_master_sql "$integ_db" "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO $integ_user;"
  run_master_sql "$integ_db" "GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO $integ_user;"
  run_master_sql "$integ_db" "ALTER DEFAULT PRIVILEGES FOR ROLE $integ_migrator IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $integ_user;"
  run_master_sql "$integ_db" "ALTER DEFAULT PRIVILEGES FOR ROLE $integ_migrator IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO $integ_user;"
  run_master_sql "$integ_db" "ALTER DEFAULT PRIVILEGES FOR ROLE $DB_MASTER_USER IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $integ_user;"
done

echo ""
echo "================================================================="
echo "Bootstrap de bases de datos dev completado exitosamente."
echo "Bases creadas y verificadas:"
echo "  - istpetdev_dev_01 .. istpetdev_dev_05 (personales aisladas)"
echo "  - istpetdev_integration (compartida, migrador restringido)"
echo "Extensión PostGIS habilitada en todas las bases."
echo "Secretos registrados en Secrets Manager bajo /istpetdev/dev/db/."
echo "================================================================="
