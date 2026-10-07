# IstpetDev — plataforma logística

Repositorio de software para el Reto 1 de Expo Clean Ecuador 2026.

- GitHub: https://github.com/bryancito1090/istpetdev-platform
- Bóveda de decisiones: https://github.com/bryancito1090/istpetdev-hackathon-2026
- Carpeta local: `/home/bryan/Projects/ISTPETDEV`.
- Rama base de desarrollo: `develop`.

---

## Documentación Principal

- 📘 [**Guía de Incorporación del Equipo (Onboarding)**](docs/onboarding.md): Configuración de máquina local, arranque de aplicaciones, túnel SSM y carga de secretos.
- ☁️ [**Guía de Aprovisionamiento y Operación AWS Dev**](docs/aws-dev.md): Bootstrap manual de state KMS/S3, Terraform Dev, bootstrap de PostgreSQL en RDS y políticas IAM.

---

## Estado del Workspace y Aplicaciones

El workspace ha sido completamente implementado, configurado y validado localmente:

1. **Backend .NET 8** (`src/{Api,Application,Domain,Infrastructure}`):
   - Solución .NET 8 con Clean Architecture, EF Core 8 y Npgsql 8.
   - Soporte espacial PostGIS (`NetTopologySuite`).
   - Separación estricta de salud de proceso (`/health/live`) y salud de base de datos (`/health/ready`).
   - CORS configurado para `http://localhost:4200` y `http://localhost:8100`.
   - Autenticación JWT para Amazon Cognito conforme a ADR-10.
   - Escucha en `http://localhost:5000`.

2. **Frontend Web Angular 22** (`apps/web`):
   - Angular 22.2.1, Angular CLI 22.2.2, TypeScript 6.0.3, RxJS 7.8.2.
   - Arquitectura Feature-Sliced Design (FSD): `app`, `pages`, `widgets`, `features`, `entities`, `shared`.
   - Componentes Standalone, Signals reactivos y Tailwind CSS 3.4.19.
   - Comprobación funcional contra la API en tiempo real.

3. **Móvil Ionic Angular PWA** (`apps/mobile`):
   - Ionic Angular 8.8.19 con Angular 22.2.1 y Tailwind 3.4.19.
   - PWA funcional con soporte de verificación de conectividad API.

4. **Librería de Contratos Compartidos** (`libs/shared-core`):
   - Paquete TypeScript `@istpetdev/shared-core` con modelos, contratos de API, unidades y validaciones sin dependencias de UI.

5. **Infraestructura Terraform Dev** (`infra/terraform/environments/dev` y `modules/`):
   - VPC 10.42.0.0/16 en dos AZs (subredes públicas y privadas de datos).
   - Nodo EC2 de acceso `t3.micro` con Ubuntu 24.04 oficial, sin reglas inbound, rol SSM e IMDSv2.
   - Amazon RDS PostgreSQL 16 Single-AZ con `rds.force_ssl=1`, gp3 cifrado y password maestro administrado por Secrets Manager.
   - S3 de evidencias dev con cifrado, versionado y política estricta de TLS en tránsito.
   - Validado con `terraform fmt`, `terraform init -backend=false` y `terraform validate`.

6. **Scripts de Operación y Datos** (`scripts/ops/` y `scripts/base_datos/`):
   - `scripts/ops/export-dev-config.sh`: Extrae metadata pública a `dev-access.json`.
   - `scripts/ops/dev-tunnel.sh`: Inicia túnel SSM `localhost:15432 -> RDS:5432` con validación de CA oficial y `/etc/hosts`.
   - `scripts/base_datos/bootstrap-dev.sh`: Inicializa de forma idempotente las 5 bases personales y la de integración en RDS, con roles aislados y PostGIS.
   - `scripts/ops/render-dev-policies.sh`: Materializa políticas IAM inline JSON individuales para IAM Identity Center.
   - `scripts/ops/load-dev-secrets.sh`: Carga credenciales autorizadas desde Secrets Manager a `.NET user-secrets` sin imprimir secretos.

7. **Dependencias Locales Compose** (`infra/compose/docker-compose.local.yml`):
   - PostgreSQL 16 con PostGIS y Redis 7 para desarrollo desconectado de AWS.

---

## Versiones Efectivas del Workspace

| Componente | Versión Efectiva |
|---|---|
| .NET SDK | **8.0.424** (fijado en `global.json`) |
| EF Core / Npgsql | **8.0.11** |
| Node.js | **22.23.3 LTS** |
| npm | **10.9.9** |
| Angular Framework | **22.2.1** |
| Angular CLI / Build | **22.2.2** |
| TypeScript | **6.0.3** |
| RxJS | **7.8.2** |
| Ionic Angular | **8.8.19** |
| Tailwind CSS | **3.4.19** |
| Terraform | **1.15.9** (AWS Provider ~> 5.50) |
| Docker Engine / Compose | **Engine 29.8.1, Compose 5.5.1** |
| AWS CLI | **2.37.10** |
| Session Manager Plugin | **1.2.835.0** |
| PostgreSQL Client | **psql 18.6** (servidor RDS / Docker: **PostgreSQL 16**) |

---

## Comandos Rápidos

```bash
# Comprobación de compilación de todos los proyectos:
npm run check

# Levantar servicios en desarrollo:
npm run dev:api     # Backend en http://localhost:5000
npm run dev:web     # Web en http://localhost:4200
npm run dev:mobile  # Móvil PWA en http://localhost:8100
```

---

## Trabajo Pendiente del Equipo

- **Compañero responsable de DevOps**: Implementación de workflows de CI/CD en `.github/workflows/` (build, push GHCR, deploy demo con OIDC) y Compose de despliegue en `infra/compose/`.
- **Bryan (Administrador)**:
  - Crear usuarios y grupos en IAM Identity Center.
  - Crear el bucket KMS/S3 de state persistente por consola según `docs/aws-dev.md`.
  - Ejecutar `terraform plan/apply` para dev y el script `bootstrap-dev.sh`.
- **Servidor n8n**: Servidor existente `https://n8n.bryan-bano.com/` (las credenciales de n8n y DeepSeek permanecen exclusivamente en ese servidor).
