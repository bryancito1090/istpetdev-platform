---
tipo: arquitectura
estado: aceptado
actualizado: 2026-10-13
deciders: [Jorge Doicela, Bryan Doicela, Joseph Bano]
tags: [arquitectura, devops, base-de-datos, migraciones, despliegue, runbook]
---

# Runbook de DevOps y base de datos para la demo

> [!important] Procedimiento Vinculante Aceptado — 13 de octubre de 2026
> Este runbook resuelve y formaliza la [issue #16](https://github.com/bryancito1090/istpetdev-platform/issues/16), consolidando el **único procedimiento oficial de base de datos y despliegue para la demo del 13 al 15 de octubre**. ADR-21 queda formalmente **aceptado** con Deciders confirmados (Jorge Doicela como Infraestructura/DevOps, Bryan Doicela como Tech Lead y Joseph Bano como DevOps/Tech Writer). Cualquier procedimiento anterior en conflicto queda reemplazado.

Es el procedimiento único para cambiar la base y el despliegue entre el 13 y el 15 de octubre. Lo que no aparece aquí no se ejecuta sin acordarlo antes con el equipo.

## 1. Manejo de la Base de Datos

ADR-21 (núcleo delgado) **retira Schema95 y RLS de la demo**. Por la dinámica del evento, en el que el dataset se conoce el mismo día, se utiliza un esquema de 27 tablas gestionadas por Entity Framework Core, una sola organización fija (`OrganizationId` se conserva) y seis roles funcionales fijos guardados en `Account.Role`. No hay RBAC administrable, concesiones, plantillas ni configuración desde la base: RF-17, RF-18, RF-19 y RNF-13 pasan a P2. El diseño previo de 95 tablas con RLS se conserva como referencia en el tag `schema95-archivo` y en [[Esquema completo de base de datos]].

Que no haya RLS no significa que no existan roles de PostgreSQL:

| Rol de PostgreSQL | Uso en la demo |
|---|---|
| `migrator_dev_01`, `migrator_integration` | Dueños de las tablas en el esquema `public`; aplican migraciones de EF Core. En producción, la API se conecta con `migrator_integration` y ejecuta las migraciones al arrancar (`ApplyMigrations=true`). |
| `user_dev_01`, `user_integ_dev_01` | Roles de lectura y escritura (DML) sin DDL. Cada migración termina con `DemoGrants.Sql`, que concede permisos sobre las tablas del núcleo. |

## 2. Estado Actual del Despliegue (13-oct)

| Pieza | Situación | Fuente |
|---|---|---|
| Pipeline CI/CD | Desde `main`, con un PR de `develop`: validación de política de ramas, QA Backend (.NET 8 con `LeanSchemaTests`), QA Frontend (Angular 22 con ESLint FSD), imágenes Docker inmutables en GHCR asociadas al SHA, y despliegue atómico por SSH al EC2. | `.github/workflows/deploy.yml` |
| Host de la aplicación | `istpetdev-prod-host` (`i-0e0a979f8c0885a4f`, IP `44.204.107.167`), Debian 13, stateless. | [[Perfil A Reforzado - Despliegue Stateless y Amazon RDS]] |
| Base de datos en producción | Amazon RDS PostgreSQL 16.13 (`istpetdev-rds-dev`, base `istpetdev_integration`) en subred privada de AWS. **27 tablas de `DemoCore` aplicadas y verificadas.** | [[Estrategia de tres entornos y base de datos]] |
| Entorno de desarrollo | Base compartida en AWS EC2 (puerto `5432`) con usuario único `migrator_dev_01` para todo el equipo (laboratorio, casa u oficina), configurada centralizadamente en `.env`, y Docker local (`localhost:15432`) como alternativa individual. `appsettings.json` mantiene secretos limpios para no exponer credenciales en Git. | [[Estrategia de tres entornos y base de datos]] |
| Verificación de salud | `https://istpetdev.doicela.dev/health/ready` responde `HTTP 200 Healthy` (`PostgreSQL database is available.`). | Verificación en vivo |

## 3. Secuencia Oficial de Migraciones (13–15 oct)

Regla estricta: **una migración por spec, en serie sobre `develop`**. Cada migración se genera sobre `develop` actualizado, después de que la anterior esté integrada en el repositorio, y termina invocando `migrationBuilder.Sql(DemoGrants.Sql)`. Prohibido abrir migraciones concurrentes en paralelo para evitar conflictos de snapshot en `ApplicationDbContextModelSnapshot`.

| Orden | Spec | Migración EF Core | Contenido Principal | Responsable | Plazo / Hito | Estado |
|---|---|---|---|---|---|---|
| 0 | — | Tag `schema95-archivo` | Punto de congelamiento y referencia de Schema95 | Bryan / Jorge | 12-oct | **Completado** |
| 1 | 001 | `DemoCore` | 27 tablas del núcleo, organización demo, PostGIS, cuentas y `DemoGrants` | Bryan / Jorge | H2 (13-oct 20:00) | **Completado y Desplegado** |
| 2 | 002 | `RoadMatrixCache` | `RoadMatrixEdge`, caché de distancias y `RouteStockReservation` | Asignado spec 002 | Antes de H3 (14-oct 20:00) | Pendiente |
| 3 | 004 | `FieldOperations` | `QrAccessToken`, estado y sincronización offline | Alexander Martínez | Antes de H3 (14-oct 20:00) | Pendiente |
| 4 | 003 | `AgentRuns` | Registro de ejecuciones y trazabilidad de decisiones del agente | Asignado spec 003 | Antes de H4 (15-oct 11:00) | Pendiente |
| 5 | 005 | `SimulationRuns` | Metadatos y eventos de corridas de simulación (`SimulationRun`, `SimulationEvent`) | Alexander Martínez | Antes de H4 (15-oct 11:00) | Pendiente |

> [!important] Congelamiento total de base de datos (H5)
> **A partir de H5 (15 de octubre a las 14:00) queda terminantemente prohibido generar, fusionar o aplicar cualquier nueva migración DDL.** Toda la persistencia quedará congelada para la sesión de demostración y pitch.

## 4. Procedimiento de Respaldo y Restauración (Issue #15)

Antes de cualquier intervención destructiva o cambio crítico de esquema sobre Amazon RDS:

1. **Snapshot Manual en Amazon RDS:**
   ```bash
   aws rds create-db-snapshot \
     --db-instance-identifier istpetdev-rds-dev \
     --db-snapshot-identifier istpetdev-rds-dev-pre-democore-20261013
   ```
   Verificar que el estado cambie a `available` antes de continuar.
2. **Volcado Lógico (`pg_dump -Fc`):**
   - Ejecutado desde el nodo administrativo o túnel SSM sobre `istpetdev_integration`.
   - Carga del archivo al bucket `istpetdev-backups-dev-...` registrando su hash SHA-256.
3. **Plan de Reversión Probado:**
   - En caso de fallo mayor, restaurar el volcado mediante `pg_restore -d istpetdev_integration` o recuperar el snapshot en una base temporal para reconciliación.

## 5. Regla de Promoción a Producción (Acordada y Ratificada)

De las dos alternativas planteadas en la [issue #16](https://github.com/bryancito1090/istpetdev-platform/issues/16), se ratifica como **regla única y obligatoria**:

> **Regla de Promoción:** Toda migración integrada en `develop` se promueve a `main` mediante Pull Request. El pipeline de CI/CD despliega la imagen inmutable y la API .NET aplica la migración automáticamente al iniciar en el host EC2 (`ApplyMigrations=true`).

- **Justificación técnica:** Se descarta la ejecución manual por túnel SSM con `migrator_integration` para el despliegue rutinario porque introduce fricción de autenticación AWS/MFA, requiere conexión WAN interactiva del operador y genera desalineación temporal entre el código desplegado y la base de datos.
- **En desarrollo local:** Cada desarrollador actualiza su copia de `develop` y ejecuta `npm run db:migrate` sobre la base de desarrollo centralizada configurada en `.env` o Docker local (`localhost:15432`).

## 6. Matriz de Cambios de Infraestructura por Spec

| Spec | Requerimiento de Infraestructura | Archivos Afectados | Responsable | Plazo | Estado |
|---|---|---|---|---|---|
| **001 T4** | Retirar `Schema95Check` y validar con `dotnet test` (`LeanSchemaTests`) | `.github/workflows/deploy.yml` | Jorge Doicela | H2 (13-oct) | **Completado** |
| **001** | Incluir fixtures (`demo-base`) y variables `Demo__*` en Docker y compose | `infra/docker/backend/Dockerfile`, `infra/compose/docker-compose.prod.yml` | Jorge Doicela | H2 (13-oct) | **Completado** |
| **004 T1b** | Servir PWA en ruta `/campo/` y configurar cabeceras de caché | `infra/nginx/istpetdev.conf` | Alexander Martínez / Jorge Doicela | H3 (14-oct) | En curso |
| **004 T1b** | Callbacks HTTPS en Amazon Cognito (`https://istpetdev.doicela.dev/auth/callback`) | Terraform `identity.tf` | Jorge Doicela | H3 (14-oct) | Pendiente |
| **003** | Cliente M2M en Cognito (`client_credentials`) para integraciones | Terraform `identity.tf` | Jorge Doicela | H4 (15-oct) | Pendiente |
| **005** | Base aislada de simulación (`istpetdev_simulation`) y cadena `ConnectionStrings:Simulation` | RDS / Docker Compose / Secretos | Jorge Doicela | H4 (15-oct) | Pendiente |
| **Seguridad (#14)** | TLS con `VerifyFull` + CA oficial de RDS y Swagger restringido por defecto. SSH 22 mitigado con UFW/fail2ban (cierre y Terraform diferidos a P2) | Dockerfile / Compose / Nginx / Program.cs / Deploy workflow | Jorge Doicela | Antes de H4 (14-oct) | **Completado** |

## 7. Checklist de Control Día por Día

### 13 de octubre (Día 1) — Núcleo y Estabilización
- [x] Coordinación de infraestructura y formalización del Runbook (resolución de issue #16).
- [x] Respaldo y validación de bases de datos.
- [x] Tag de archivo `schema95-archivo`.
- [x] Spec 001 (`DemoCore`) integrado en `develop` y promovido a `main`.
- [x] Despliegue en AWS verificado: `/health/ready` responde `HTTP 200 Healthy` contra Amazon RDS.
- [x] Persistencia de desarrollo centralizada en AWS EC2 y Docker local operativa para los integrantes.

### 14 de octubre (Día 2) — Motor Logístico y Operación Móvil
- [ ] Integración de migración 002 (`RoadMatrixCache`) en `develop` y promoción a `main`.
- [ ] Callbacks HTTPS de Cognito y PWA en `/campo/` en Nginx listos.
- [ ] Integración de migración 004 (`FieldOperations`) en `develop` y promoción a `main`.
- [ ] Ensayo del hito H3 (20:00): Recorrido operativo completo en teléfonos móviles sobre terreno.

### 15 de octubre (Día 3) — Agente, Simulación y Congelamiento
- [ ] Integración de migración 003 (`AgentRuns`) y cliente M2M de Cognito.
- [ ] Base de datos de simulación aprovisionada e integración de migración 005 (`SimulationRuns`).
- [ ] Ensayo del hito H4 (11:00): Corrida completa del simulador y agente logístico.
- [ ] Aplicación de ajustes transversales de seguridad (issue #14).
- [ ] **Hito H5 (14:00): Congelamiento total de código e infraestructura.**
- [ ] Snapshot final de Amazon RDS posterior al congelamiento para la demo oficial.

## 8. Gestión de Riesgos y Reversión

- **Riesgo de bloqueo de EF Core:** Si una migración falla al arrancar en el EC2, el pipeline detiene el tráfico inmediatamente al no pasar el check de `/health/ready`.
- **Procedimiento de Rollback Rápido:** Ejecutar el workflow `rollback.yml` indicando el SHA previo estable para restaurar los contenedores en <30 segundos.
- **Riesgo de red en el evento:** Si la conectividad WAN o AWS falla durante la presentación, el sistema cuenta con el respaldo de persistencia local mediante Docker en las laptops del equipo.

## Referencias

[[Perfil A Reforzado - Despliegue Stateless y Amazon RDS]] · [[CI-CD y automatizacion de despliegue]] · [[Estrategia de tres entornos y base de datos]] · [[AWS y Terraform]] · [[istpetdev-dbflow]] · [[Definiciones de producto y plan hasta el pitch]] · [[Decisiones de arquitectura]]
