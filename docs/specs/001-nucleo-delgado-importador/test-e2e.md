# 001 — Núcleo delgado e importador genérico: pruebas de punta a punta

> Recorridos completos del spec. Se cierra cuando E2E-01, E2E-02 y E2E-03 pasan con evidencia guardada. E2E-04 y E2E-05 se ejecutan cuando el entorno de Cognito y el despliegue estén disponibles; si no lo están el 15-oct, se registra el motivo.

> **Revisión pendiente antes de ejecutar (12-oct-2026):** ajustar estos recorridos al [plan revisado](plan.md#cambios-obligatorios-en-taskmd-y-test-e2emd-antes-de-ejecutarlos), aplicar prelaunch antes del arranque y separar pruebas locales de operaciones en integración o entorno expuesto. El plan requiere conservar migraciones e historial; recrear una base compartida no es un prerrequisito implícitamente autorizado. No se han ejecutado estos escenarios durante la planificación.

## Preparación del entorno

```bash
# Desde la raíz del repositorio, en la rama del spec o en develop tras el merge
docker info --format '{{.ServerVersion}}'      # Docker activo (Testcontainers)
dotnet tool restore
dotnet build
```

Para los escenarios manuales contra la API local:

```bash
docker compose -f infra/compose/docker-compose.local.yml up -d db
export ConnectionStrings__Main="Host=localhost;Port=15432;Database=istpetdev_local;Username=postgres;Password=postgres_dev_password_local"
export ApplyMigrations=true
dotnet run --project src/Api                     # http://localhost:5000, entorno Development (Demo:Enabled=true)
```

## Escenarios

| ID | Escenario | Cubre | Tipo | Obligatorio |
|---|---|---|---|---|
| E2E-01 | Reiniciar la demo en una base nueva | H3, H5, FR-15, FR-16, CE-01 | automatizado | sí |
| E2E-02 | Cargar un dataset ajeno con perfil, en seco y aplicado, sin duplicar | H1, H2, FR-03, FR-06, FR-11, CE-02 | automatizado | sí |
| E2E-03 | Simulacro de dataset sorpresa (guardarraíl G6) | H1, H2, CE-03 | manual cronometrado | sí |
| E2E-04 | API local con token real de Cognito | H4, FR-17, FR-18 | manual | si hay pool de Cognito dev |
| E2E-05 | Imagen Docker y reset en el entorno desplegado | FR-15, despliegue | manual | antes del ensayo del 15-oct |

### E2E-01 — Reiniciar la demo en una base nueva

**Objetivo:** demostrar que el escenario de demo se restaura en un paso, desde una base vacía y a través del mismo importador que usa la API.

**Pasos:**

1. `dotnet test tests/IstpetDev.Tests --filter "Category=E2E"`.
2. La prueba `DatasetJourneyTests.Planner_resets_demo_then_adds_an_external_dataset_without_code_changes` arranca la API sobre una base nueva, llama a `POST /api/v1/demo/reset` como Planificador y consulta `GET /api/v1/service-points`.

**Resultado esperado:** reset `200` con `applied: true`; 6 puntos; `HOS-001` con categoría `hospital` y latitud `-0.17`. Las pruebas de `ImportServiceTests` confirman 1 bodega, 2 productos, 1 vehículo, 2 lotes, 9 saldos y 7 consumos, con 60 L de desinfectante en `HOS-001`.

**Evidencia a guardar:** salida de `dotnet test` con el número de pruebas y la duración, más el hash del commit.

### E2E-02 — Dataset ajeno con perfil

**Objetivo:** demostrar que un formato distinto (separador `;`, decimales con coma, encabezados con tildes, galones por mes) entra sin cambios de código.

**Pasos:** los ejecuta la misma prueba de E2E-01, en este orden:

1. `POST /api/v1/imports` con `data/fixtures/ejemplo-externo` (sin `dryRun` explícito, así que se hace en seco): 0 errores y siguen 6 puntos.
2. `POST /api/v1/imports?dryRun=false`: `applied: true` y 8 puntos.
3. Repetir el paso 2: todas las entidades con `created: 0` y siguen 8 puntos.
4. `POST /api/v1/demo/reset`: vuelven a quedar 6 puntos.

**Resultado esperado:** los cuatro pasos como se describen. `ImportServiceTests.External_dataset_loads_with_its_mapping_profile` confirma 359,614 L para `C-100` con `period_days = 30` y fecha `2026-09-01`.

**Evidencia a guardar:** salida de `dotnet test`. Para el pitch, una captura del reporte JSON en seco donde se vean `resolvedColumns`, `assumptions` e `issues`.

### E2E-03 — Simulacro de dataset sorpresa

**Objetivo:** comprobar que el guardarraíl G6 funciona con personas reales y bajo presión de tiempo, como el 16-oct a las 09:30.

**Preparación (la hace alguien que no implementó el importador):**

- Un dataset de una red de abastecimiento con al menos puntos, productos e inventario o consumo, en **otro formato** que el nuestro: por ejemplo, un `.xlsx` con encabezados en español distintos de los sinónimos, otra unidad (cajas, galones) o consumo agregado por semana o mes, alguna fila con errores y alguna columna que no usamos.
- Entregarlo sin aviso a una persona del equipo. Iniciar el cronómetro.

**Pasos de quien integra** (con un token de Planificador obtenido como en E2E-04; si aún no hay pool de Cognito, copiar los archivos y el perfil a `data/fixtures/simulacro/` y ejecutar los mismos pasos con una prueba temporal que llame a `ImportService` mediante `Fixtures.Request("simulacro", dryRun: true)`):

1. Subir los archivos sin perfil y en seco:

   ```bash
   curl -s -X POST "http://localhost:5000/api/v1/imports" -H "Authorization: Bearer $TOKEN" \
     -F "puntos=@ruta/clientes.xlsx" -F "consumos=@ruta/consumo.xlsx" | jq '.entities, .issues[0:10]'
   ```

2. Leer `resolvedColumns`, `ignoredColumns` e `issues`, y escribir `mapping.json` solo para lo que no se asoció.
3. Repetir en seco con el perfil hasta tener `errorCount: 0`, o errores explicados y aceptados para `allowPartial`:

   ```bash
   curl -s -X POST "http://localhost:5000/api/v1/imports" -H "Authorization: Bearer $TOKEN" \
     -F "profile=<ruta/mapping.json" -F "files=@ruta/clientes.xlsx" -F "files=@ruta/consumo.xlsx" | jq '.errorCount, .entities'
   ```

   (`profile=<archivo` envía el contenido como campo de texto; `profile=@archivo` lo enviaría como archivo y la API lo rechazaría.)

4. Aplicar con `?dryRun=false` y comparar los conteos con los del archivo origen.
5. Detener el cronómetro.

**Resultado esperado:** datos aplicados en **≤ 60 minutos** sin modificar código. Si hace falta cambiar código (una entidad o un formato no soportado), se anota qué y cuánto tiempo costó: es información para decidir el alcance del 16-oct.

**Evidencia a guardar:** el dataset (si no contiene datos reales de terceros), el `mapping.json` final, el reporte JSON, el tiempo total y la lista de fricciones. Guardarla en `docs/boveda/99 Fuentes/Evidencias/` con la [Plantilla evidencia](../../boveda/09%20Plantillas/Plantilla%20evidencia.md).

### E2E-04 — API local con token real de Cognito

**Objetivo:** confirmar que la autorización por rol funciona con tokens reales y no solo con los de prueba.

**Pasos:**

1. Obtener un access token del usuario de prueba según [Identidad OIDC y sesiones](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md) y leer su `sub`.
2. Registrar la cuenta con user-secrets (`Demo:Accounts:0:Subject`, `DisplayName`, `Role = Planificador`) y reiniciar la API.
3. `curl -s -X POST http://localhost:5000/api/v1/demo/reset -H "Authorization: Bearer $TOKEN" | jq '.applied'`.
4. Cambiar el rol a `Conductor`, reiniciar la API y repetir el paso 3.

**Resultado esperado:** `true` con Planificador; `403` con Conductor; `401` con un ID token en lugar del access token.

**Evidencia a guardar:** códigos de respuesta, sin tokens ni identificadores personales.

### E2E-05 — Imagen Docker y reset en el entorno desplegado

**Objetivo:** que el entorno de la demo pueda restaurarse antes de cada ensayo y el día del pitch.

**Pasos:**

1. `docker build -f infra/docker/backend/Dockerfile -t istpetdev-backend:e2e .` y `docker run --rm --entrypoint ls istpetdev-backend:e2e /app/data/fixtures/demo-base`.
2. Confirmar que la base de integración se recreó con `scripts/db/reset-to-demo-core.sql` **antes** del despliegue, con aprobación de quien la administra.
3. Tras desplegar: `GET /health/ready` responde `Healthy` sin migraciones pendientes.
4. `POST /api/v1/demo/reset` con una cuenta Planificador configurada en el `.env` del host.

**Resultado esperado:** los 8 archivos del fixture en la imagen; `Healthy`; reset `200` en menos de 5 s.

**Evidencia a guardar:** salida de los comandos y de `/health/ready`, con fecha y versión de imagen.

## Registro de ejecuciones

| Fecha | Commit | Escenario | Resultado | Evidencia | Quién |
|---|---|---|---|---|---|
| 2026-10-12 | `06f2911` (local) | E2E-01 | Aprobado | `artifacts/spec001/E2E.trx` (`DatasetJourneyTests`), PostGIS descartable | Antigravity / Bryan |
| 2026-10-12 | `06f2911` (local) | E2E-02 | Aprobado | `artifacts/spec001/E2E.trx`, `ImportServiceTests` en Release | Antigravity / Bryan |
| — | — | E2E-03 | Pendiente | Requiere simulacro humano con dataset sorpresa | Pendiente de asignación |
| — | — | E2E-04 | Pendiente | Requiere pool real de AWS Cognito (no simulado con RSA de test) | Pendiente de Cognito dev |
| — | — | E2E-05 | Pendiente | Requiere ensayo en host/RDS desplegado antes del 15-oct | Pendiente de despliegue |
