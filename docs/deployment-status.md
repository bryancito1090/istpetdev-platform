# Estado de Despliegue AWS — IstpetDev

Fecha de corte: 2026-10-08T10:45:00-05:00
Entorno: `dev` (`us-east-1`, cuenta `747456040427`)
Identidad ejecutora: `istpetdev-bryan` (Perfil: `istpetdev-admin`)

---

## 1. Resumen Ejecutivo del Estado Actual

- **Bootstrap de Estado S3**: Completado con éxito. Estado migrado al bucket privado `istpetdev-tfstate-747456040427-us-east-1` cifrado con SSE-KMS (`alias/istpetdev-tfstate`) y bloqueo con `use_lockfile = true`.
- **Infraestructura Base `dev` (89 recursos en remote state)**: Totalmente aprovisionada y operativa en AWS.
- **Resolución de RDS y Alternativa Free Tier**:
  - Instancia RDS PostgreSQL 16.13 (`db.t3.micro`, gp3 20GB, cifrada con KMS) creada y en estado `available` (`istpetdev-rds-dev`).
  - PITR configurado a 1 día continuo (compatible con Free Tier).
  - AWS Backup con bóveda `istpetdev-dev` y plan programado con snapshots diarios a las 03:00 ECT (08:00 UTC) retenidos por 7 días.
  - Alertas de fallo de backup en EventBridge conectadas al tópico SNS.
  - Suscripción a eventos de RDS (`istpetdev-dev-rds-events`) conectada al tópico SNS.
  - 4 alarmas CloudWatch configuradas y activas para RDS (CPU, memoria, almacenamiento y conexiones).
- **Bootstrap de Base de Datos (Completado al 100%)**:
  - 6 bases de datos creadas y verificadas: `istpetdev_dev_01` a `istpetdev_dev_05` e `istpetdev_integration`.
  - Extensión PostGIS 3.4 habilitada en todas las 6 bases.
  - 17 secretos generados con contraseñas seguras y almacenados en AWS Secrets Manager (`/istpetdev/dev/db/...`).
  - Roles de migrador con DDL en esquema `public` y roles de aplicación estrictamente limitados a DML (`SELECT, INSERT, UPDATE, DELETE`) y sin permisos DDL.
  - Revocado acceso público a bases de mantenimiento (`postgres`, `template1`).
- **Túnel SSM de Conexión a Base de Datos**:
  - Documento SSM `istpetdev-dev-postgres-tunnel` activo.
  - Túnel operativo reenviando puerto local 15432 hacia RDS 5432 vía nodo EC2 `i-0bba84773e794de36`.
- **Identidad, MFA y Políticas IAM del Equipo**:
  - `istpetdev-bryan`: Activo (Administrador).
  - `istpetdev-alexander`: ✅ MFA Activo — Política `IstpetDevIndividualDevAccess` aplicada en IAM.
  - `istpetdev-jorge`: ✅ MFA Activo — Política `IstpetDevIndividualDevAccess` aplicada en IAM.
  - `istpetdev-andres`: ❌ MFA Pendiente (Sin dispositivo).
  - `istpetdev-anthony`: ❌ MFA Pendiente (Sin dispositivo).

---

## 2. Inventario Real en AWS

### A. Almacenamiento y Cifrado
- **KMS**: `arn:aws:kms:us-east-1:747456040427:key/bdb56749-22df-4a14-a7ec-3b515066ca69` (`alias/istpetdev-tfstate`, habilitada).
- **S3 State**: `istpetdev-tfstate-747456040427-us-east-1` (versionado, SSE-KMS, bloqueo público total, TLS forzado).
- **S3 Evidencias**: `istpetdev-evidence-dev-747456040427-us-east-1` (versionado, SSE-S3, TLS forzado, CORS configurado).
- **S3 Backups**: `istpetdev-backups-dev-747456040427-us-east-1` (versionado, SSE-S3, TLS forzado, lifecycle de 7 y 28 días).

### B. Red y Cómputo
- **VPC**: `vpc-0217f06f82247cd0b` (`10.42.0.0/16`).
  - Subredes públicas: `subnet-0753b32e4f7c99913` (AZ1), `subnet-020f8f9fc2abdb4a1` (AZ2).
  - Subredes datos (privadas): `subnet-07d4c124c2e31db9f` (AZ1), `subnet-08db173463653d6d3` (AZ2).
  - Internet Gateway: Asociado; sin costos de NAT Gateway.
- **Nodo de Acceso EC2**: `i-0bba84773e794de36` (`t3.micro`, Ubuntu 24.04, IP pública `32.196.114.105`).
  - Security Group: `sg-0c4c0a0cff4fdc55f` (sin reglas de entrada; salida hacia endpoints SSM).
  - SSM Agent: Online.
  - Alarma CloudWatch de estado: `istpetdev-dev-access-node-status`.

### C. Base de Datos
- **Instancia RDS**: `istpetdev-rds-dev` (`db.t3.micro`, PostgreSQL 16.13, Single-AZ en subredes privadas).
  - Endpoint: `istpetdev-rds-dev.cep4osawemm3.us-east-1.rds.amazonaws.com:5432`.
  - Security Group: `sg-0c7bbe8a898ce393d` (permite puerto 5432 exclusivamente desde `sg-0c4c0a0cff4fdc55f`).
  - Parameter Group: `istpetdev-pg16-params-dev` (`rds.force_ssl = 1`).
  - Secreto Maestro RDS: `arn:aws:secretsmanager:us-east-1:747456040427:secret:rds!db-9bdd47cb-8726-4dce-b257-f23930022c36-JtKyAk`.
  - Suscripción de Eventos: `istpetdev-dev-rds-events` hacia SNS.
  - Backups: PITR 1 día en RDS + AWS Backup plan 7 días en bóveda `istpetdev-dev`.

### D. Identidad, Acceso y Secretos
- **Cognito User Pool**: `us-east-1_4Y2j5DINp` (Dominio: `istpetdev-dev-747456040427.auth.us-east-1.amazoncognito.com`).
  - Cliente Web: `6ejhdau47gad74id7id1l7ugln`.
  - Cliente Móvil: `3k59rv5cov1sinllgd7igmpe1a`.
- **Secrets Manager**: 17 secretos poblados y operativos:
  - Personales de app: `/istpetdev/dev/db/dev_01` a `dev_05`.
  - Personales migradores: `/istpetdev/dev/db/dev_01/migrator` a `dev_05/migrator`.
  - Integración: `/istpetdev/dev/db/integration/dev_01` a `dev_05`.
  - Integración migrador y backup: `/istpetdev/dev/db/integration/migrator` y `/istpetdev/dev/db/integration/backup`.
- **SSM Document**: `istpetdev-dev-postgres-tunnel`.
- **SNS & Monitoreo**:
  - Tópico SNS: `arn:aws:sns:us-east-1:747456040427:istpetdev-dev-operations`.
  - Suscripción: `banobryan1090@gmail.com` (Confirmada y activa).
  - Presupuesto AWS Budgets: $80 USD mensual con alertas al 50%, 80% y 100%.

---

## 3. Estado de Políticas IAM del Equipo

| Integrante | Rol en Equipo | Usuario IAM | Estado MFA | Política IAM Dev |
|---|---|---|---|---|
| Bryan | Admin Infra | `istpetdev-bryan` | ✅ Activo | Administrador |
| Alexander | Desarrollador | `istpetdev-alexander` | ✅ Activo | ✅ `IstpetDevIndividualDevAccess` aplicada |
| Andrés | Desarrollador | `istpetdev-andres` | ✅ Activo | ✅ `IstpetDevIndividualDevAccess` aplicada |
| Anthony | Desarrollador | `istpetdev-anthony` | ✅ Activo | ✅ `IstpetDevIndividualDevAccess` aplicada |
| Jorge | Desarrollador | `istpetdev-jorge` | ✅ Activo | ✅ `IstpetDevIndividualDevAccess` aplicada |

---

## 4. Archivos Privados Locales (`~/.config/istpetdev/private/`)

- `account-plan.json`: Registro de cuota y créditos de la cuenta (Free plan, créditos vigentes).
- `dev-access.json`: Configuración generada de acceso al entorno dev y túnel.
- `dev-outputs.json`: Outputs definitivos completos exportados de Terraform.
- `dev-policies/dev_02.json` a `dev_05.json`: Políticas individuales generadas para Alexander, Andrés, Anthony y Jorge.
- `handoff-status.json`: Registro de estado actualizado (91 recursos en state, RDS disponible, bootstrap completado, equipo 100% habilitado).
- `team-roster.json`: Mapeo de miembros a usuarios IAM.
