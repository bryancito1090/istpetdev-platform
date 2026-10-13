---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-12
tags: [arquitectura, devops, base-de-datos, migraciones, despliegue, runbook]
---

# Runbook de DevOps y base de datos para la demo

> [!warning] Borrador del 12-oct
> Este documento está en borrador y no se ha ejecutado ningún paso. El responsable de infraestructura lo valida y completa en la [issue #16](https://github.com/bryancito1090/istpetdev-platform/issues/16); el procedimiento de respaldo depende de la [issue #15](https://github.com/bryancito1090/istpetdev-platform/issues/15); las correcciones de TLS y Terraform, de la [issue #14](https://github.com/bryancito1090/istpetdev-platform/issues/14). Hasta que se acepte, ADR-21 sigue con Deciders PENDIENTE ([[Decisiones de arquitectura]]).

Es el procedimiento único para cambiar la base y el despliegue entre el 13 y el 15 de octubre. Cualquier otro procedimiento que lo contradiga queda reemplazado. Lo que no aparece aquí no se ejecuta sin acordarlo antes con el equipo.

## 1. Cómo se manejará la base

ADR-21 (núcleo delgado) **retira Schema95 y RLS de la demo**. Por la dinámica del evento, en el que el dataset se conoce el mismo día, se usa un esquema de 27 tablas, una sola organización fija (`OrganizationId` se conserva) y seis roles funcionales fijos guardados en `Account.Role`. No hay RBAC administrable, concesiones, plantillas ni configuración desde la base: RF-17, RF-18, RF-19 y RNF-13 pasan a P2. El diseño de 95 tablas con RLS se conserva como referencia para la versión multiempresa en el tag `schema95-archivo` y en [[Esquema completo de base de datos]].

Que no haya RLS no significa que no haya roles **de PostgreSQL**. Los usuarios de conexión siguen existiendo porque los crea el bootstrap de RDS y Secrets Manager:

| Rol de PostgreSQL | Uso en la demo |
|---|---|
| `migrator_dev_0N`, `migrator_integration` | Dueños de las tablas; aplican migraciones de EF Core. En producción, la API también se conecta con `migrator_integration` porque aplica las migraciones al arrancar (`ApplyMigrations=true`). Sin RLS se acepta para la hackathon; separar el rol de ejecución es una mejora posterior (spec 001). |
| `user_dev_0N`, `user_integ_dev_0N` | Lectura y escritura sin DDL para desarrollo. Cada migración termina con `DemoGrants.Sql`, que les vuelve a conceder permisos sobre las tablas nuevas. |

## 2. Estado actual del despliegue (12-oct)

| Pieza | Situación | Fuente |
|---|---|---|
| Pipeline | Desde `main`, con un PR de `develop` o de `hotfix/*`: QA, imágenes en GHCR, SSH al EC2 y `docker compose up`. `rollback.yml` vuelve a la imagen anterior. | [[CI-CD y automatizacion de despliegue]], `CONTRIBUTING.md` |
| Host de la app | `i-0e0a979f8c0885a4f`, Debian 13, clave `login`, usuario `admin`, creado **a mano** | [[Perfil A Reforzado - Despliegue Stateless y Amazon RDS]] |
| Terraform `dev` | Gestiona VPC, RDS, S3, Cognito, secretos y el **nodo del túnel SSM** `i-0bba84773e794de36` (Ubuntu 24.04, sin key pair). **No gestiona el host de la app.** | `infra/terraform/environments/dev` |
| Base desplegada | `istpetdev_integration` con Schema95 (95 tablas con RLS), a la que la API conecta como `migrator_integration` | `deploy.yml`, compose de producción |
| QA del backend | `tools/Schema95Check` | `deploy.yml`, job `backend-qa` |

### Choques entre el despliegue y Terraform

Se resuelven en la [issue #14](https://github.com/bryancito1090/istpetdev-platform/issues/14):

1. **Comparten security group.** El host de la app y el nodo del túnel usan `sg-0c4c0a0cff4fdc55f`. Al abrir 22, 80 y 443 para la app (commit 01aaf2d), el nodo del túnel, diseñado sin entradas, queda expuesto también (R-19).
2. **La salida del SG difiere.** La nota Perfil A registra «todo el tráfico», pero el código Terraform solo declara 443, 53 y 5432. **No ejecutar `terraform apply` en `dev` sin revisar antes `terraform plan`**: eliminaría la salida total y el paso «Preparar Host» de `deploy.yml` (apt por HTTP) fallaría en un host nuevo.
3. **El host de la app no está en Terraform.** Si alguien recrea la infraestructura, el host no vuelve. El procedimiento de recreación está en la sección 6 de la nota Perfil A y es manual.
4. **Credenciales de despliegue.** `deploy.yml` usa una clave SSH estática y SSH abierto a internet, aunque ya existe el rol OIDC `istpetdev-github-demo` sin usar.
5. **TLS hacia RDS sin verificar.** El compose usa `SSL Mode=Require;Trust Server Certificate=true`, contra la regla `VerifyFull` de [[istpetdev-dbflow]]. Además, Swagger está público.

## 3. Secuencia de migraciones

Regla: **una migración por spec, en serie sobre `develop`**. Cada una se genera sobre `develop` actualizado, después de que la anterior esté integrada, y termina con `migrationBuilder.Sql(DemoGrants.Sql)`. Nunca hay dos migraciones abiertas en paralelo porque chocan en `ApplicationDbContextModelSnapshot`.

| Orden | Spec | Migración | Contenido | Prerrequisito | Plazo |
|---|---|---|---|---|---|
| 0 | — | Tag `schema95-archivo` | Último commit con Schema95 | — | Antes de fusionar 001 |
| 1 | 001 | `DemoCore` (desde cero) | 27 tablas del núcleo, organización demo, PostGIS, `DemoGrants` | Reset de bases (sección 4) | H2: 13-oct 20:00 |
| 2 | 002 | `RoadMatrixCache` | `RoadMatrixEdge`, `RouteStockReservation` | 001 en `develop` | Antes de H3 (14-oct 20:00) |
| 3 | 003 | `AgentRuns` | Registro de ejecuciones del agente | 002 en `develop` | Antes de H4 (15-oct 11:00) |
| 4 | 004 | `FieldOperations` | `QrAccessToken` y operación de campo | La anterior en `develop` | Antes de H3 (recorrido con teléfonos) |
| 5 | 005 | Migración de simulación | Metadatos de `SimulationRun`, `SimulationEvent` | 001 y 002; en serie con 003 y 004 | Antes de H4 |

Si 004 está lista antes que 003, se cambia el orden de la tabla y se avisa en el canal del equipo antes de generar la migración: el orden lo marca el momento de integración, no el número del spec. **Después de H5 (15-oct 14:00) no entra ninguna migración.**

## 4. Transición de Schema95 a DemoCore (una sola vez)

`DemoCore` crea el esquema desde cero, así que las bases que tienen Schema95 deben vaciarse antes. Si se despliega 001 sin este paso, la API no arranca porque las migraciones fallan.

1. **Respaldo obligatorio** según la [issue #15](https://github.com/bryancito1090/istpetdev-platform/issues/15): snapshot manual `istpetdev-rds-dev-pre-democore-20261013`, `pg_dump -Fc` de cada base subido al bucket de backups con su SHA-256 y una restauración de prueba. Sin respaldo verificado no se ejecuta el paso 3.
2. **Congelar despliegues.** Nadie fusiona a `main` mientras dura la transición.
3. **Reset** con `scripts/db/reset-to-demo-core.sql` usando el rol migrador de cada base, por el túnel SSM con `VerifyFull`, y con confirmación del dueño de la base:
   - Bases personales `istpetdev_dev_0N`: cada integrante la suya. Luego `scripts/db/migrate-personal.sh` aplica `DemoCore`.
   - `istpetdev_integration`: una sola persona, la responsable del runbook.
4. **Promover 001** de `develop` a `main`. El despliegue arranca la API, que aplica `DemoCore` y los grants.
5. **Verificar:**
   - `/health/ready` responde 200.
   - `POST /api/v1/demo/reset` carga el fixture.
   - La prueba `LeanSchemaTests` y el E2E del spec 001 pasan.
6. **Reversión:** volver al tag `schema95-archivo`, recrear la base y restaurar el volcado del paso 1.

## 5. Propagación en integración durante la hackathon

Propuesta para que la valide la [issue #16](https://github.com/bryancito1090/istpetdev-platform/issues/16): `istpetdev_integration` **solo la cambia el despliegue**. Cada vez que se fusiona en `develop` un spec con migración, se abre de inmediato el PR de `develop` a `main`. La API aplica la migración al arrancar. Así se evita que la base de integración vaya por delante del código desplegado. La aplicación manual con `migrator_integration` queda solo para el reset de la sección 4.

Las bases personales se actualizan al traer `develop`: se carga el rol migrador, se ejecuta `dotnet ef database update` y se restaura el rol de aplicación ([[istpetdev-dbflow]]).

La base de simulación del spec 005 (`ConnectionStrings:Simulation`) es **una base distinta** de la operativa. Hay que crearla, con sus roles y secretos, en RDS antes de H4; hoy no existe en Terraform ni en el bootstrap.

## 6. Cambios de infraestructura por spec

| Spec | Cambio | Archivo | Responsable | Plazo |
|---|---|---|---|---|
| 001 T4 | Reemplazar Schema95Check por `dotnet test` | `.github/workflows/deploy.yml`, `scripts/db/migrate-personal.sh` | Quien implemente 001 | H2 |
| 001 | Copiar `data/fixtures` y activar `Demo__Enabled` y `Demo__FixturePath` | `infra/docker/backend/Dockerfile`, compose de producción | Quien implemente 001, con revisión de infraestructura | H2 |
| 003 | Cliente M2M de Cognito (`client_credentials`) | Terraform `identity.tf` | Infraestructura (R-22) | H3 |
| 003 | Perfil `agent` de n8n en el compose local (plan B) | `infra/compose/docker-compose.local.yml` | Quien implemente 003 | H4 |
| 004 T1b | PWA servida en `/campo/` y callbacks HTTPS de Cognito | `infra/nginx/istpetdev.conf`, Terraform `identity.tf` | Infraestructura (R-24) | H3 |
| 005 | Base de simulación y su cadena de conexión | Bootstrap de RDS, secretos, compose | Infraestructura | H4 |
| 005 FR-21 | Corrida oficial en una máquina dedicada, no en el t3.micro; el ZIP se importa con verificación SHA-256 | Procedimiento del spec 005 | Quien implemente 005 | H4 |
| Transversal | `VerifyFull`, Swagger restringido, SSH cerrado, host de la app en Terraform | [issue #14](https://github.com/bryancito1090/istpetdev-platform/issues/14) | Infraestructura | Antes de H5 |

## 7. Checklist por día

**13-oct**
- [ ] Coordinación con infraestructura: este runbook, issues #14 a #16, R-22 y R-24.
- [ ] Respaldo verificado (#15).
- [ ] Tag `schema95-archivo`.
- [ ] Reset de las bases.
- [ ] 001 en `develop` y en `main` (H2, 20:00).
- [ ] `/health/ready` responde 200 en `https://istpetdev.doicela.dev`.

**14-oct**
- [ ] Migraciones de 002 y 004, cada una promovida a `main`.
- [ ] Cliente M2M y callbacks HTTPS aplicados.
- [ ] PWA en `/campo/`.
- [ ] Recorrido de H3 (20:00) en teléfonos.

**15-oct**
- [ ] Migraciones de 003 y 005, con la base de simulación creada.
- [ ] H4 (11:00).
- [ ] Correcciones de #14 desplegadas.
- [ ] H5 (14:00): no entran más migraciones ni cambios de infraestructura.
- [ ] Snapshot posterior al congelamiento para el día del evento.

## 8. Riesgos

R-19, R-22, R-24, R-28, R-32 y R-33 en [[Riesgos y decisiones pendientes]].

## Relacionado

[[Perfil A Reforzado - Despliegue Stateless y Amazon RDS]] · [[CI-CD y automatizacion de despliegue]] · [[AWS y Terraform]] · [[istpetdev-dbflow]] · [[Definiciones de producto y plan hasta el pitch]] · [[Decisiones de arquitectura]]
