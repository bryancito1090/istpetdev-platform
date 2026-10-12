#!/usr/bin/env bash
# ==============================================================================
# init-debian-host.sh
# Script de preparación inicial para la instancia EC2 Debian 13 (t8i.small)
# Ejecutar en el servidor una única vez: sudo bash init-debian-host.sh
# ==============================================================================

set -euo pipefail

echo "==> 1. Actualizando paquetes del sistema..."
apt-get update -y
apt-get install -y ca-certificates curl gnupg lsb-release ufw

echo "==> 2. Instalando Docker y Docker Compose plugin oficial..."
if ! command -v docker &>/dev/null; then
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg --yes
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo \
    "deb [arch="$(dpkg --print-architecture)" signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian \
    "$(. /etc/os-release && echo "$VERSION_CODENAME")" stable" | \
    tee /etc/apt/sources.list.d/docker.list > /dev/null

  apt-get update -y
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi

systemctl enable --now docker

echo "==> 3. Configurando usuario operativo 'admin'..."
CURRENT_USER="${SUDO_USER:-admin}"
usermod -aG docker "$CURRENT_USER"

echo "==> 4. Creando estructura de directorios de despliegue en /var/www/istpetdev..."
mkdir -p /var/www/istpetdev/{backups,releases,infra/compose,infra/nginx}
chown -R "$CURRENT_USER":"$CURRENT_USER" /var/www/istpetdev
chmod 755 /var/www/istpetdev

echo "==> 5. Configurando reglas de Firewall (UFW) conforme a la Capa 1 de Hardening..."
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
