#!/usr/bin/env bash
# ==============================================================================
# init-debian-host.sh
# Script de preparación para la instancia EC2 Debian 13 (t8i.small)
# Ejecutar en el servidor una única vez si no se usó user-data:
#   sudo bash init-debian-host.sh
# ==============================================================================

set -euo pipefail

export DEBIAN_FRONTEND=noninteractive

echo "==> 1. Actualizando paquetes del sistema..."
apt-get update -y
apt-get install -y ca-certificates curl gnupg lsb-release ufw

echo "==> 2. Instalando Docker CE y Docker Compose plugin oficial..."
if ! command -v docker &>/dev/null; then
  curl -fsSL https://get.docker.com | sh
fi

systemctl enable --now docker

echo "==> 3. Configurando usuario operativo..."
CURRENT_USER="${SUDO_USER:-admin}"
usermod -aG docker "$CURRENT_USER"

echo "==> 4. Creando estructura de directorios de despliegue en /var/www/istpetdev..."
mkdir -p /var/www/istpetdev/{backups,releases,infra/compose,infra/nginx/certs}

if [ ! -f /var/www/istpetdev/infra/nginx/certs/istpetdev.crt ]; then
  openssl req -x509 -nodes -days 3650 -newkey rsa:2048 \
    -keyout /var/www/istpetdev/infra/nginx/certs/istpetdev.key \
    -out /var/www/istpetdev/infra/nginx/certs/istpetdev.crt \
    -subj "/C=EC/ST=Pichincha/L=Quito/O=IstpetDev/CN=istpetdev.doicela.dev" 2>/dev/null || true
fi

chown -R "$CURRENT_USER":"$CURRENT_USER" /var/www/istpetdev
chmod 755 /var/www/istpetdev

echo "==> 5. Configurando reglas de Firewall (UFW) conforme a Hardening..."
ufw default deny incoming
ufw default allow outgoing
ufw limit 22/tcp comment 'SSH Administrativo'
ufw allow 80/tcp comment 'HTTP Nginx / Cloudflare'
ufw allow 443/tcp comment 'HTTPS Nginx / Cloudflare'
ufw --force enable

echo "======================================================================"
echo "Host preparado exitosamente para recibir despliegues de GitHub Actions."
echo "Directorio de despliegue: /var/www/istpetdev"
echo "Usuario con permisos Docker: $CURRENT_USER"
echo "======================================================================"
