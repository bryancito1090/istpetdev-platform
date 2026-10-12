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

echo "==> Generando respaldo de PostgreSQL en ${BACKUP_FILE}..."
docker compose -f /var/www/istpetdev/infra/compose/docker-compose.prod.yml exec -T istpetdev-db \
  pg_dump -U istpetdev_user istpetdev_db | gzip > "$BACKUP_FILE"

echo "==> Respaldo generado exitosamente:"
ls -lh "$BACKUP_FILE"
