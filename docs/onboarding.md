# Guía de Incorporación del Equipo (Onboarding) — IstpetDev

Esta guía describe el procedimiento para que cada integrante del equipo prepare su entorno local y comience a desarrollar en el proyecto **IstpetDev**.

---

## 1. Requisitos del Sistema

Antes de clonar o ejecutar el proyecto, verifique que su estación de trabajo cuente con las siguientes herramientas instaladas:

| Herramienta | Versión Requerida | Comprobación |
|---|---|---|
| **.NET SDK** | **8.0.x** (fijado en `global.json`) | `dotnet --version` |
| **Node.js** | **22.23.3 LTS** | `node -v` |
| **npm** | **10.9.9** | `npm -v` |
| **Docker Engine & Compose** | **Engine >= 28, Compose v2** | `docker --version && docker compose version` |
| **AWS CLI** | **v2** | `aws --version` |
| **Session Manager Plugin** | **Última versión oficial** | `session-manager-plugin --version` |
| **PostgreSQL Client (psql)** | **16+** | `psql --version` |

---

## 2. Clonar el Repositorio e Instalar Dependencias

```bash
git clone --branch develop https://github.com/bryancito1090/istpetdev-platform.git
cd istpetdev-platform

# 1. Instalar dependencias npm del monorepo (web, móvil, shared-core)
npm install

# 2. Restaurar dependencias .NET (Api, Application, Domain, Infrastructure)
dotnet restore

# 3. Compilar y verificar el estado general de todas las aplicaciones
npm run check
```

---

## 3. Modos de Ejecución Local

### Modo A: Desarrollo Local con Dependencias Docker (Sin AWS)
Si desea trabajar de manera completamente local y desconectada de AWS:

1. Inicie los contenedores de dependencias locales (PostgreSQL 16 con PostGIS y Redis 7):
   ```bash
   docker compose -f infra/compose/docker-compose.local.yml up -d
   ```
2. Configure la cadena de conexión local en .NET user-secrets:
   ```bash
   dotnet user-secrets set "ConnectionStrings:Main" "Host=localhost;Port=15432;Database=postgres;Username=postgres;Password=postgres_dev_password_local;" --project src/Api
   ```
3. Inicie los servicios:
   - **Backend API**: `npm run dev:api` (disponible en `http://localhost:5000`)
   - **Frontend Web**: `npm run dev:web` (disponible en `http://localhost:4200`)
   - **Móvil PWA**: `npm run dev:mobile` (disponible en `http://localhost:8100`)

---

### Modo B: Desarrollo Colaborativo con AWS Dev (RDS por Túnel SSM)

Cada integrante cuenta con una identidad individual en **AWS IAM Identity Center** con MFA obligatorio y su propio conjunto de permisos (`IstpetDevDev01` .. `IstpetDevDev05`).

#### Paso 1: Configurar AWS SSO
```bash
aws configure sso --profile istpetdev-dev
aws sso login --profile istpetdev-dev
```
- Seleccione la URL del portal SSO provista por Bryan.
- Elija su Permission Set asignado (`IstpetDevDev01`, etc.).
- Región predeterminada de la CLI: `us-east-1`.

#### Paso 2: Recibir Configuración de Acceso
Bryan le compartirá por canal privado el archivo `dev-access.json` (metadata pública sin contraseñas). Guárdelo en su carpeta privada:
```bash
mkdir -p "$HOME/.config/istpetdev/private"
chmod 700 "$HOME/.config/istpetdev/private"
cp /ruta/compartida/dev-access.json "$HOME/.config/istpetdev/private/dev-access.json"
chmod 600 "$HOME/.config/istpetdev/private/dev-access.json"
```

#### Paso 3: Configurar Resolución TLS VerifyFull en /etc/hosts
Para conectar a Amazon RDS a través del túnel loopback manteniendo la validación estricta de certificados TLS (`SSL Mode=VerifyFull`), agregue el endpoint real a su archivo de hosts:
```bash
# Obtener el endpoint real desde dev-access.json
RDS_ADDRESS=$(jq -r '.rds_address' "$HOME/.config/istpetdev/private/dev-access.json")

# Agregar mapeo local a 127.0.0.1
echo "127.0.0.1 $RDS_ADDRESS" | sudo tee -a /etc/hosts
```
*(Al finalizar la jornada puede retirar la línea con `sudo sed -i "/$RDS_ADDRESS/d" /etc/hosts`)*.

#### Paso 4: Abrir el Túnel SSM
En una terminal dedicada (Terminal A), mantenga abierto el túnel:
```bash
scripts/ops/dev-tunnel.sh \
  --profile istpetdev-dev \
  --config "$HOME/.config/istpetdev/private/dev-access.json"
```
El script descargará automáticamente el certificado raíz oficial de Amazon RDS (`global-bundle.pem`) y abrirá el reenvío de puertos `localhost:15432 -> RDS:5432`.

#### Paso 5: Cargar Credenciales de Base de Datos
En otra terminal (Terminal B), recupere su secreto autorizado de AWS Secrets Manager directamente a `.NET user-secrets`:
```bash
# Para su base personal aislada:
scripts/ops/load-dev-secrets.sh \
  --profile istpetdev-dev \
  --member dev_01 \
  --database personal \
  --config "$HOME/.config/istpetdev/private/dev-access.json"

# O para colaborar en la base de integración compartida:
scripts/ops/load-dev-secrets.sh \
  --profile istpetdev-dev \
  --member dev_01 \
  --database integration \
  --config "$HOME/.config/istpetdev/private/dev-access.json"
```
*(Reemplace `dev_01` por su identificador asignado)*. Este comando construye la conexión segura con TLS VerifyFull y la almacena en `dotnet user-secrets` sin imprimir contraseñas ni escribir archivos de configuración en el repositorio.

---

## 4. Scripts del Repositorio

El proyecto cuenta con comandos unificados en la raíz:

| Script | Acción |
|---|---|
| `npm run dev:api` | Inicia el backend .NET 8 en `http://localhost:5000` |
| `npm run dev:web` | Inicia la aplicación web Angular en `http://localhost:4200` |
| `npm run dev:mobile` | Inicia la aplicación móvil Ionic en `http://localhost:8100` |
| `npm run build:web` | Compila el bundle de producción de Angular Web en `apps/web/dist/web` |
| `npm run build:mobile` | Compila el bundle de producción de Ionic Móvil en `apps/mobile/dist/mobile` |
| `npm run build:shared` | Compila la librería TypeScript `@istpetdev/shared-core` |
| `npm run check` | Ejecuta la suite de verificación completa (.NET build, shared, web, móvil) |

---

## 5. Endpoints de Comprobación y Diagnóstico

- `GET http://localhost:5000/health/live`: Comprobación de salud de proceso (retorna 200 OK si la API está viva).
- `GET http://localhost:5000/health/ready`: Comprobación de disponibilidad de base de datos PostgreSQL (retorna 200 OK si la DB responde, o 503 Service Unavailable si no está conectada).
- `GET http://localhost:5000/api/v1/ping`: Comprobación liviana con metadatos de versión y hora.
- `GET http://localhost:5000/api/v1/public/trace/{unitId}`: Consulta pública de trazabilidad de insumos.
- `GET http://localhost:5000/api/v1/service-points`: Endpoint de negocio protegido (requiere token JWT de Cognito).
