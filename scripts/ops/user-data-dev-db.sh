#!/usr/bin/env bash
# ==============================================================================
# user-data-dev-db.sh
# Aprovisionamiento desatendido para la Base de Datos Compartida de Desarrollo
# Sistema Operativo: Ubuntu 24.04 LTS o Debian 12/13 en AWS EC2
# Pega este script en: "Detalles avanzados -> Datos de usuario" (User Data)
# ==============================================================================

set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

# 1. Actualización de paquetes base
apt-get update -y
apt-get install -y ca-certificates curl gnupg ufw

# 2. Instalación de Docker CE
if ! command -v docker &>/dev/null; then
  curl -fsSL https://get.docker.com | sh
fi
systemctl enable --now docker

# 3. Usuario del sistema
HOST_USER="ubuntu"
if ! id "ubuntu" &>/dev/null; then
  HOST_USER="$(id -un 1000 2>/dev/null || echo 'admin')"
fi
usermod -aG docker "$HOST_USER"

# 4. Parámetros de la Base de Datos de Desarrollo
DB_NAME="istpetdev_dev_01"
DB_USER="migrator_dev_01"
# Contraseña estándar de desarrollo de equipo (inyectar por variable o reemplazar en consola AWS)
DB_PASS="${DB_PASSWORD:-<PASSWORD_DEV_DB>}"

# 5. Directorio de persistencia y Compose
mkdir -p /opt/istpetdev-dev-db
cat <<EOF > /opt/istpetdev-dev-db/docker-compose.yml
services:
  dev-db:
    image: postgis/postgis:16-3.4-alpine
    container_name: istpetdev-team-db
    restart: always
    environment:
      POSTGRES_DB: ${DB_NAME}
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASS}
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d ${DB_NAME}"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  pgdata:
    name: istpetdev_team_pgdata
EOF

# 6. Levantar la base de datos de desarrollo
cd /opt/istpetdev-dev-db
docker compose up -d

# 7. Configuración de Firewall UFW
ufw default deny incoming
ufw default allow outgoing
ufw limit 22/tcp comment 'SSH Administrativo'
ufw allow 5432/tcp comment 'PostgreSQL PostGIS Desarrollo Equipo'
ufw --force enable

echo "=== Base de datos de desarrollo IstpetDev lista en puerto 5432 ==="
