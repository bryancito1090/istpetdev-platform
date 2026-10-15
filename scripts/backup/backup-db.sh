#!/usr/bin/env bash
# ==============================================================================
# backup-db.sh
# Respaldo lógico de una base de RDS con la herramienta verificada del repositorio
# (scripts/base_datos/database-task.py backup): pg_dump -Fc con cliente PostgreSQL 16,
# conteo y hash de cada tabla, SHA-256 y carga verificada al bucket de respaldos
# (prefijo daily/ 7 días; weekly/ 28 días los domingos).
#
# Antes respaldaba la base de mantenimiento `postgres` con `istpetadmin` al disco del
# host, que no conserva datos (#15). Procedimiento completo: «Runbook de DevOps y base
# de datos para la demo», §4.
#
# Requisitos: túnel SSM activo (scripts/ops/dev-tunnel.sh), Docker, psql, AWS CLI con
# un perfil que pueda leer el secreto /istpetdev/dev/db/integration/backup y escribir en
# el bucket, y los archivos privados dev-access.json y dev-outputs.json.
#
# Uso: scripts/backup/backup-db.sh [integration|dev_01..dev_05]   (por defecto: integration)
#   ISTPETDEV_AWS_PROFILE   perfil AWS (por defecto istpetdev-admin)
#   ISTPETDEV_PRIVATE_DIR   carpeta privada fuera del repo (por defecto ~/.config/istpetdev/private)
# ==============================================================================

set -euo pipefail

MEMBER="${1:-integration}"
PROFILE="${ISTPETDEV_AWS_PROFILE:-istpetdev-admin}"
PRIVATE_DIR="${ISTPETDEV_PRIVATE_DIR:-$HOME/.config/istpetdev/private}"

for file in dev-access.json dev-outputs.json; do
  [ -f "$PRIVATE_DIR/$file" ] || { echo "ERROR: falta $PRIVATE_DIR/$file (ver docs/aws-dev.md)" >&2; exit 1; }
done

exec python3 "$(dirname "$0")/../base_datos/database-task.py" backup \
  --profile "$PROFILE" \
  --config "$PRIVATE_DIR/dev-access.json" \
  --outputs "$PRIVATE_DIR/dev-outputs.json" \
  --member "$MEMBER"
