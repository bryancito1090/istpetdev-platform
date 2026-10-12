#!/usr/bin/env bash
# ==============================================================================
# deploy-local-host.sh
# Despliegue o actualización manual de servicios en el servidor EC2
# ==============================================================================

set -euo pipefail

DEPLOY_BASE="${DEPLOY_DIR:-/var/www/istpetdev}"
cd "$DEPLOY_BASE"

echo "==> 1. Descargando imágenes actualizadas de GHCR..."
docker compose -f infra/compose/docker-compose.prod.yml pull --quiet

echo "==> 2. Levantando servicios de base de datos y cache..."
docker compose -f infra/compose/docker-compose.prod.yml up -d istpetdev-db istpetdev-redis

echo "==> 3. Esperando disponibilidad de PostgreSQL..."
docker compose -f infra/compose/docker-compose.prod.yml exec -T istpetdev-db \
  pg_isready -h localhost -p 5432 -U istpetdev_user -d istpetdev_db --timeout=30 || true

echo "==> 4. Actualizando servicios de aplicación (Backend, Web y Proxy)..."
docker compose -f infra/compose/docker-compose.prod.yml up -d --force-recreate --remove-orphans \
  istpetdev-backend istpetdev-web istpetdev-proxy

echo "==> 5. Ejecutando mantenimiento y rotación de imágenes..."
docker image prune -f
for img in istpetdev-backend istpetdev-web; do
  docker images --filter "reference=*${img}*" --format "{{.ID}}" | sort -u | tail -n +4 | xargs -r docker rmi -f 2>/dev/null || true
done

echo "==> Despliegue completado exitosamente."
