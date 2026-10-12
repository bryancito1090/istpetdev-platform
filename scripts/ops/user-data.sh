#!/usr/bin/env bash
# ==============================================================================
# user-data.sh
# Script de aprovisionamiento desatendido (Cloud-Init / User Data para AWS EC2)
# Compatible con: Debian 12/13 y Ubuntu 22.04/24.04
# Pega este script en "Detalles avanzados -> Datos de usuario" al lanzar la instancia
# y el servidor quedará 100% listo para recibir despliegues sin intervención manual.
# ==============================================================================

set -euo pipefail

# 1. Configurar variables de entorno no interactivas
export DEBIAN_FRONTEND=noninteractive

# 2. Actualizar repositorios e instalar paquetes base
apt-get update -y
apt-get install -y ca-certificates curl gnupg lsb-release ufw

# 3. Instalar Docker CE y Docker Compose Plugin oficial
if ! command -v docker &>/dev/null; then
  curl -fsSL https://get.docker.com | sh
fi

# 4. Asegurar arranque inmediato de Docker
systemctl enable --now docker

# 5. Detectar usuario del sistema (admin en Debian, ubuntu en Ubuntu)
HOST_USER="admin"
if ! id "admin" &>/dev/null; then
  if id "ubuntu" &>/dev/null; then
    HOST_USER="ubuntu"
  else
    HOST_USER="$(id -un 1000 2>/dev/null || echo 'admin')"
  fi
fi

# Asignar usuario al grupo docker
usermod -aG docker "$HOST_USER"

# 6. Crear estructura de directorios de producción para IstpetDev
DEPLOY_BASE="/var/www/istpetdev"
mkdir -p "${DEPLOY_BASE}"/{backups,releases,infra/compose,infra/nginx/certs}

# Generar certificado SSL autofirmado de 10 años para Nginx (Cloudflare Full / Direct HTTPS)
if [ ! -f "${DEPLOY_BASE}/infra/nginx/certs/istpetdev.crt" ]; then
  openssl req -x509 -nodes -days 3650 -newkey rsa:2048 \
    -keyout "${DEPLOY_BASE}/infra/nginx/certs/istpetdev.key" \
    -out "${DEPLOY_BASE}/infra/nginx/certs/istpetdev.crt" \
    -subj "/C=EC/ST=Pichincha/L=Quito/O=IstpetDev/CN=istpetdev.doicela.dev" 2>/dev/null || true
fi

chown -R "${HOST_USER}:${HOST_USER}" "${DEPLOY_BASE}"
chmod -R 755 "${DEPLOY_BASE}"

# 7. Configuración de Firewall perimetral UFW (Capa 1 de Hardening)
ufw default deny incoming
ufw default allow outgoing
ufw limit 22/tcp comment 'SSH Administrativo'
ufw allow 80/tcp comment 'HTTP Nginx / Cloudflare'
ufw allow 443/tcp comment 'HTTPS Nginx / Cloudflare'
ufw --force enable

echo "==> Instancia EC2 inicializada exitosamente para IstpetDev Platform."
