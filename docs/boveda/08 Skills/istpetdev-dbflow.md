---
tipo: skill
estado: vigente
actualizado: 2026-10-12
tags: [istpetdev, ia, skills, base-de-datos, postgresql, migraciones]
---

# IstpetDev — base de datos colaborativa y migraciones

**Cuándo usarla:** Trabajo colaborativo sobre PostgreSQL 16 y PostGIS 3.4 en el entorno de desarrollo centralizado (AWS EC2 configurado en `.env` o Docker local `localhost:15432`) y despliegue a Amazon RDS (`istpetdev_integration`) en producción. Usar al crear o modificar entidades en C# (`src/Domain/Entities`), generar y aplicar migraciones de Entity Framework Core, conmutar entornos con `switch-db.mjs`, y coordinar cambios de esquema en el repositorio.

> [!important] Estrategia de tres entornos consolidada — 13 de octubre de 2026
> - **Persistencia centralizada en la nube:** El desarrollo colaborativo diario de los 5 integrantes se consolida en la base de datos de desarrollo en AWS EC2 (`istpetdev_dev_01`) configurada a través de `.env`, eliminando la fricción de redes locales o túneles personales. La persistencia opera bajo tres niveles ([[Estrategia de tres entornos y base de datos]]):
>   1. **Desarrollo Centralizado:** AWS EC2 con PostGIS (`istpetdev_dev_01`) para todo el equipo (`npm run db:dev` o `npm run db:cloud`).
>   2. **Casa (Local):** Docker Desktop local (`localhost:15432`) para trabajo offline individual (`npm run db:local`).
>   3. **Producción:** Amazon RDS gestionado (`istpetdev_integration`) exclusivo para el host EC2 en AWS.
> - **Retiro de Schema95 y adopción de DemoCore:** Se retira Schema95 y RLS para la demo; rige el núcleo `DemoCore` de 27 tablas gestionado por EF Core. Ver [[Runbook de DevOps y base de datos para la demo]].

> Documentación de la skill `istpetdev-dbflow`. El archivo `SKILL.md` no se versiona en la bóveda: quien quiera usarla con su asistente de IA copia este contenido a la carpeta de skills de su herramienta (frontmatter con `name: istpetdev-dbflow` y la descripción de arriba). Catálogo y origen en [[Catalogo y plan de skills]].

Rutas relativas a la raíz del repositorio de software `istpetdev-platform`. Leer antes [[istpetdev-contexto]] y [[istpetdev-datos]].

## Leer primero

- `03 Arquitectura/Estrategia de tres entornos y base de datos.md`: especificación técnica de los entornos Desarrollo Centralizado, Casa y Producción ([[Estrategia de tres entornos y base de datos]]).
- `04 Datos y algoritmos/Modelo de datos.md`: entidades, tipos canónicos, relaciones e invariantes ([[Modelo de datos]]).
- `00 Inicio/Hechos canonicos.md`: nombres en código, motor y convenciones canónicas ([[Hechos canonicos]]).
- `03 Arquitectura/Conexion PostgreSQL en DBeaver.md`: inspección visual en DBeaver para la base de datos de desarrollo y Docker ([[Conexion PostgreSQL en DBeaver]]).
- `03 Arquitectura/Runbook de DevOps y base de datos para la demo.md`: procedimientos operativos de despliegue y demo ([[Runbook de DevOps y base de datos para la demo]]).

## Hechos consolidados

- **Motor y hosting de desarrollo centralizado:** PostgreSQL 16 con PostGIS 3.4 corriendo en contenedor Docker (`istpetdev_postgres` con imagen `postgis/postgis:16-3.4-alpine`) en instancia dedicada de AWS EC2. Ofrece conectividad directa a todo el equipo desde cualquier red (laboratorio, casa u oficina).
- **Entorno alternativo local:** Contenedor local (`localhost:15432` para trabajo individual offline vía Docker Compose).
- **Hosting de producción:** PostgreSQL 16.13 con PostGIS 3.4 en Amazon RDS (`istpetdev-rds-dev`, base `istpetdev_integration`) en subred privada de AWS, consumida exclusivamente por el host de producción EC2 ([https://istpetdev.doicela.dev](https://istpetdev.doicela.dev)).
- **Base de desarrollo compartida:** `istpetdev_dev_01` en AWS EC2 aloja las 27 tablas canónicas de `DemoCore`.
- **Política de usuario único para desarrollo:** Todos los desarrolladores utilizan **`migrator_dev_01`** (contraseña protegida en `.env`), propietario del esquema `public` con permisos completos de DDL y DML para evitar fricciones operativas entre miembros del equipo.
- **Centralización de configuración y no exposición de secretos:** 
  - Archivo `.env`: Fuente única de verdad para el host, puerto, base de datos y credenciales en desarrollo (ignorado por Git).
  - Backend .NET: `src/Api/appsettings.json` mantiene `"ConnectionStrings:Main": ""` limpio para no exponer IPs ni contraseñas en Git. `Program.cs` carga automáticamente `.env` en desarrollo.
  - Scripts: `scripts/ops/switch-db.mjs` lee directamente de `.env` y gestiona `dotnet user-secrets` sin quemar IPs.

### Conexión de Desarrollo de los Integrantes
| Entorno | Host / IP | Puerto | Base de Datos | Usuario | Password |
|---|---|---|---|---|---|
| **AWS Cloud EC2 (Desarrollo)** | Configurado en `.env` | `5432` | `istpetdev_dev_01` | `migrator_dev_01` (Único) | Configurada en `.env` |
| **Casa (Local Offline)** | `localhost` | `15432` | `istpetdev_dev_01` | `migrator_dev_01` | Configurada en Compose local |
| **Producción (AWS RDS)** | *Endpoint RDS privado* | `5432` | `istpetdev_integration` | `migrator_integration` | *Secrets Manager* |

## Reglas del flujo de trabajo

1. **El código C# es la única fuente de la verdad:** La estructura de la base se gestiona exclusivamente mediante migraciones de Entity Framework Core en `src/Infrastructure/Persistence/Migrations/`. Nunca ejecutar sentencias DDL manuales (`CREATE TABLE`, `ALTER TABLE`) en clientes gráficos como DBeaver o pgAdmin.
2. **Ciclo de vida de una migración:**
   - Diseñar la entidad C# en `src/Domain/Entities/` y registrarla en `ApplicationDbContext.cs`.
   - Generar la migración con:
     `dotnet ef migrations add <NombrePascalCase> --project src/Infrastructure --startup-project src/Api`
   - Aplicar a la base de desarrollo (laboratorio o local):
     `npm run db:migrate` o `dotnet ef database update --project src/Infrastructure --startup-project src/Api`
   - Validar que la API responda exitosamente en `/health/ready` (HTTP 200).
   - Subir el cambio en una rama `feature/*` y abrir Pull Request hacia `develop`.
3. **Propagación a producción (integración):**
   - La base `istpetdev_integration` en Amazon RDS se actualiza automáticamente mediante el pipeline de despliegue inmutable en GitHub Actions cuando el código se promueve a `main`.
4. **Resolución de conflictos en Git (Snapshot de EF Core):**
   - Si dos integrantes generan migraciones concurrentes y surge conflicto en `ApplicationDbContextModelSnapshot.cs`: **NUNCA editar el snapshot a mano**.
   - Si una migración nunca fue aplicada en el servidor compartido ni compartida con el equipo, puede removerse con `dotnet ef migrations remove --project src/Infrastructure --startup-project src/Api`, sincronizar la rama con `develop` y regenerarse sobre el snapshot actualizado.
   - Si ya fue aplicada en el servidor de desarrollo, coordinar la secuencia con el equipo y generar las modificaciones de estructura como migraciones complementarias hacia adelante.

### Histórico archivado: Bases individuales RDS y túneles SSM
El esquema previo donde cada desarrollador (`dev_01` a `dev_05`) mantenía una base remota en AWS RDS mediante túneles SSM, perfiles IAM individuales y credenciales en AWS Secrets Manager queda archivado debido a la latencia, dependencias de conectividad WAN e impedimentos operativos durante el hackathon. Los scripts asociados (`load-dev-secrets.sh`, `migrate-personal.sh`) se conservan únicamente como referencia histórica.

Para Bryan, el comando `bash scripts/db/migrate-personal.sh istpetdev-admin dev_01` comprueba el modelo, carga el migrador, actualiza su base y restaura el usuario API. Fue ejecutado por el agente de software con resultado 0; evidencia y alcance en [[Coordinacion migracion schema95]]. El script requiere API detenida, sesión AWS vigente y túnel activo. La transición exige las cinco tablas iniciales vacías; no borrar registros para superar la guarda. Después de aplicar, las tablas renombradas usan nombres singulares.

## Prohibido inventar

- Nombres de tablas, columnas o entidades que no provengan de [[Hechos canonicos]] o [[Modelo de datos]] (usar nombres canónicos en inglés: `RoutePlan`, `ServicePoint`, `HandlingUnit`, etc.).
- Modificar directamente la base de datos `istpetdev_integration` desde una máquina local sin que el PR esté fusionado en `develop`.
- Desactivar la verificación TLS (`VerifyFull`) o abrir puertos de entrada en el Security Group de RDS.
- Modificar o reescribir archivos de migración ya aplicados, aunque todavía no estén fusionados en ramas principales.

## Verificar

- Con la API autorizada y ejecutándose, `GET http://localhost:5000/health/ready` devuelve `HTTP 200 OK` demostrando conectividad a la base de datos. No acredita autorización, aislamiento RLS ni funciones de negocio.
- Tipos de datos correctos: identificadores con UUIDv7, fechas en UTC (`DateTime.UtcNow`) y dinero/cantidades en `decimal` con escala explícita.
- Comprobación de integridad con `python docs/boveda/.github/scripts/check_vault.py` antes de fusionar documentación.

## Origen

Creada para la hackathon para coordinar los cinco desarrolladores sobre Amazon RDS sin bloqueos de esquema. Adapta principios de `data-architect-dba` (código como fuente de la verdad, migraciones reproducibles y verificadas) y los estándares de `istpet-gitflow` (rebase limpio, ramas de características y sincronización sobre rama principal).
