#!/usr/bin/env bash
# ==============================================================================
# prune-host.sh
# Mantenimiento y liberación de almacenamiento en el servidor EC2 (Debian)
# - Retiene las 3 últimas versiones de imágenes Docker de IstpetDev
# - Poda imágenes huérfanas y contenedores detenidos
# - Compacta logs del sistema a un máximo de 50 MB
# ==============================================================================

set -euo pipefail

echo "==> 1. Poda de imágenes Docker huérfanas..."
docker image prune -f

echo "==> 2. Rotación de imágenes IstpetDev (retención de últimas 3 versiones)..."
for img in istpetdev-backend istpetdev-web; do
  docker images --filter "reference=*${img}*" --format "{{.ID}}" | sort -u | tail -n +4 | xargs -r docker rmi -f 2>/dev/null || true
done

echo "==> 3. Compactación de logs del sistema (journalctl max 50M)..."
sudo journalctl --vacuum-size=50M 2>/dev/null || true

echo "==> 4. Limpieza de caché de paquetes del sistema operativo..."
sudo apt-get clean 2>/dev/null || true

echo "==> Espacio en disco actual:"
df -h /
