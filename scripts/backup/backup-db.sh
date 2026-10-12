#!/usr/bin/env bash
# ==============================================================================
# backup-db.sh
# Generación de respaldo preventivo comprimido de la base de datos PostgreSQL
# ==============================================================================

set -euo pipefail

BACKUP_DIR="${DEPLOY_DIR:-/var/www/istpetdev}/backups"
mkdir -p "$BACKUP_DIR"

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/istpetdev_db_${TIMESTAMP}.sql.gz"

DB_HOST="${DB_HOST:-istpetdev-rds-dev.cep4osawemm3.us-east-1.rds.amazonaws.com}"
DB_PORT="${DB_PORT:-5432}"
DB_USER="${DB_USER:-istpetadmin}"
DB_NAME="${DB_NAME:-postgres}"

echo "==> Generando respaldo de Amazon RDS (${DB_HOST}) en ${BACKUP_FILE}..."
PGPASSWORD="${DB_PASSWORD:-}" pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" "$DB_NAME" | gzip > "$BACKUP_FILE"

echo "==> Respaldo generado exitosamente:"
ls -lh "$BACKUP_FILE"
