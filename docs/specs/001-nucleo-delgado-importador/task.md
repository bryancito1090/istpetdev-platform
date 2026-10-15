# 001 — Núcleo delgado e importador genérico: tareas

> Seguimiento actualizado el 12-oct-2026 durante la implementación autorizada en esta sesión.
> `[x]` significa trabajo realizado; `[ ]` significa pendiente. Implementar una prueba no equivale a ejecutarla con éxito.
> Código y pruebas viven en las rutas enlazadas; se retiraron los ejemplos anteriores que eliminaban migraciones o contradecían el plan revisado.
> Evidencia y límites: [verification.md](verification.md). Todos los comandos se ejecutan desde la raíz.

## Resumen

| ID | Tarea | Depende de | Estado actual |
|---|---|---|---|
| T0 | Alcance local, rama y conservación histórica | — | Completado; rama, hashes y notas fuente registrados |
| T1 | Pruebas y primitivas canónicas | T0 | Implementado; unitarias verificadas |
| T2 | Perfil, resolución, filas y reporte | T1 | Implementado; unitarias verificadas |
| T3 | Lectores CSV/XLSX | T1 | Implementado; unitarias verificadas |
| T4 | Núcleo de 27 tablas y transición DemoCore | T0/T1 | Implementado; catálogo, historia completa y runtime verificados |
| T5 | Importador, ledger y fixtures | T2/T3/T4 | Implementado; integración y mediciones consolidadas en Release |
| T6 | Roles, cuentas y JWT | T4 | Implementado; seguridad verificada en Release |
| T7 | HTTP, reset, límites e imagen | T5/T6 | Implementado; imagen construida y límites verificados en Release |
| T8 | E2E, CI y evidencia | T7 | Verificado en Release (114 pruebas); CI, cotejo y builds aprobados |
| T9 | Bóveda, contratos y cierre | T0/T8 | En curso (documentación y evidencia registradas; ADR/cierre final pendiente de decisión) |
| T10 | Respaldo y restauración verificables (#15) | T4/T8 | Ensayo local verificado y en CI; volcado y restauración contra RDS pendientes de acceso a AWS |
| T11 | Importador que se adapta al dataset (entidad por nombre o columnas) | T5/T7 | Implementado y verificado con `docs/dataset`; 426/426 backend |

Los estados se actualizan después de cada resultado. Las pruebas compartidas pueden editarse de forma independiente; migraciones, snapshot, DI y Program se integran por un solo responsable.

### T0 — Preparación y alcance

- [x] Leer README, bóveda, hechos canónicos, spec y plan revisado; preservar los cambios documentales existentes.
- [x] Recibir autorización expresa para implementar y verificar, incluyendo la sustitución local de Schema95 por 27 tablas.
- [x] Crear `feature/001-nucleo-delgado-importador` y actualizarla por fast-forward a `origin/develop` (`06f2911`).
- [x] Comprobar .NET SDK 8.0.424, EF tools 8.0.11 y Docker 29.8.1.
- [x] Aplicar prelaunch antes de hosts de prueba; registrar alcance local sintético en [verification.md](verification.md).
- [x] Conservar migraciones InitialSchema/CompleteSystemSchema95, sus SQL y herramientas históricas; guardar sus SHA-256 para cotejo final.
- [x] Registrar archivo recuperable de Schema95 con SHA/tag local y comprobar hashes finales.
- [x] Registrar las diferencias D1–D9 y el alcance autorizado en las notas fuente. No inventar aprobación del equipo ni fuente oficial de O-01.

La tarea no incluye recrear RDS, modificar cuentas cloud, publicar tags ni desplegar. El guard de DemoCore detiene bases con datos distintos de los seeds históricos, sin borrarlos.

### T1 — Primitivas y proyecto de pruebas

**Código:** [proyecto de pruebas](../../../tests/IstpetDev.Tests/IstpetDev.Tests.csproj), [HeaderNormalizer](../../../src/Application/Imports/HeaderNormalizer.cs), [ImportEntity](../../../src/Application/Imports/ImportEntity.cs), [CanonicalSchema](../../../src/Application/Imports/CanonicalSchema.cs), [ValueParser](../../../src/Application/Imports/ValueParser.cs).

- [x] Crear proyecto net8.0, añadirlo a la solución y clasificar Unit/Integration/E2E.
- [x] Implementar siete entidades importables y alias españoles; reutilizar `BaseEntity.NewId()`.
- [x] Separar normalización de encabezados y códigos; probar tildes, mayúsculas, espacios, signos y colisiones de sinónimos.
- [x] Interpretar decimales, agrupación de miles válida, fechas comunes/Excel y horas sin ICU.
- [x] Rechazar enteros fraccionarios, overflow, números mal agrupados y capacidades/cantidades inválidas.
- [x] Verificar las unitarias anteriores y los casos adicionales de `ImportEdgeTests` (66 pruebas).

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/IstpetDev.Tests --filter 'Category=Unit'
```

### T2 — Perfiles, columnas, validación y reporte

**Código:** [MappingProfile](../../../src/Application/Imports/MappingProfile.cs), [ColumnResolver](../../../src/Application/Imports/ColumnResolver.cs), [RowParser](../../../src/Application/Imports/RowParser.cs), [ImportReport](../../../src/Application/Imports/ImportReport.cs), [ImportLimits](../../../src/Application/Imports/ImportLimits.cs).

- [x] Validar JSON cerrado, entidades/campos conocidos, escalas positivas numéricas, perfil nulo, constantes y rutas de archivo.
- [x] Rechazar encabezados equivalentes, asociación ambigua y mapeo a una columna inexistente.
- [x] Aplicar constante → columna declarada → sinónimo inequívoco → defecto canónico.
- [x] Conservar tipo de celda, archivo, hoja y número real de fila.
- [x] Validar par lat/lon, ventanas completas/coherentes, períodos positivos y longitudes.
- [x] Reportar constantes, conversiones, redondeo, valores ausentes y conservación de datos existentes.
- [x] Limitar incidencias a 500 manteniendo todos los contadores; distinguir `Planned`, `Committed` y `None`.
- [x] Probar 1 200 errores, perfil incorrecto y XLSX mixto bajo globalización invariante.
- [x] Confirmar corrida Release después de añadir conservación explícita de campos opcionales.

```bash
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~MappingAndParsingTests|FullyQualifiedName~ImportEdgeTests'
```

### T3 — CSV y XLSX

**Código:** [lectores de Infrastructure](../../../src/Infrastructure/Imports/TabularReader.cs), [TabularData](../../../src/Application/Imports/TabularData.cs).

- [x] Fijar CsvHelper 33.0.1 y ClosedXML 0.104.1.
- [x] Leer CSV coma/semicolon/tab/pipe, BOM, UTF-8 y fallback Windows-1252; conservar comillas y filas reales.
- [x] Rechazar ancho incorrecto, binarios, formatos no admitidos y exceso de filas/columnas/celdas.
- [x] Leer hoja seleccionada/primera hoja de XLSX y validar su estructura ZIP antes de abrirlo.
- [x] Distinguir números nativos de texto decimal; probar fechas 1904 y rechazar fórmulas.
- [x] Acotar tamaño comprimido/descomprimido y liberar streams.
- [x] Verificar unitarias de lectura y libro mixto sin ICU.

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~TabularReaderTests|FullyQualifiedName~ImportEdgeTests'
```

### T4 — Esquema y migración conservando historia

**Código:** [entidades](../../../src/Domain/Entities/IdentityEntities.cs), [LeanSchema](../../../src/Infrastructure/Persistence/LeanSchema.cs), [DbContext](../../../src/Infrastructure/Persistence/ApplicationDbContext.cs), [DemoCore](../../../src/Infrastructure/Persistence/Migrations/20261012223838_DemoCore.cs), [preflight](../../../src/Infrastructure/Persistence/Migrations/Sql/DemoCorePreflight.sql), [DatabaseInitializer](../../../src/Infrastructure/Persistence/DatabaseInitializer.cs).

- [x] Mapear las 27 tablas del spec con UUID v7, organización, timestamps, versión, unicidad y FKs por organización.
- [x] Mantener exactamente la organización demo histórica; reconocer solo los seeds de Schema95 al comprobar la transición.
- [x] Generar DemoCore posterior al snapshot vigente, manteniendo ambas migraciones históricas.
- [x] Bloquear cualquier dato ajeno a los seeds; no usar DROP SCHEMA, no borrar historial EF y no generar un script de reset de RDS.
- [x] Revisar retiro de políticas/triggers y conversión explícita geography → geometry; cantidades numeric(18,3).
- [x] Verificar catálogo real de 27 tablas, saldo no negativo y geometrías en PostGIS descartable.
- [x] Implementar recarga de tipos con conexión ya abierta/cerrada y modo `--migrate` que termina al finalizar.
- [x] Limitar grants de runtime a tablas del núcleo, preservando historial de imports y excluyendo DDL.
- [x] Añadir pruebas de historia completa, rechazo de datos existentes y rol runtime limitado.
- [x] Verificar historia completa y rol runtime después de corregir la recarga asíncrona de tipos.
- [x] Cotejar hashes históricos y ausencia de cambios de modelo sin migración.

```bash
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~LeanSchemaTests|FullyQualifiedName~MigrationSafetyTests'
```

DemoCore solo acepta un Schema95 sin datos operativos ni cuentas, con seeds originales intactos. Una base poblada requiere reconciliación separada. `Down` se rechaza: recuperar Schema95 requiere restaurar un backup verificado y una imagen compatible.

### T5 — Importación, ledger y fixtures

**Código:** [ImportService](../../../src/Application/Imports/ImportService.cs), [CanonicalWriter](../../../src/Application/Imports/CanonicalWriter.cs), [ImportContext](../../../src/Application/Imports/ImportContext.cs), [fixtures](../../../data/fixtures/README.md).

- [x] Crear `demo-base`: bodega, seis puntos, dos SKU, vehículo, dos lotes, nueve saldos y siete consumos.
- [x] Crear `ejemplo-externo` y perfil para galones/mes con encabezados distintos.
- [x] Resolver referencias contra registros existentes y filas válidas del envío; impedir auxiliares de filas inválidas.
- [x] Agrupar duplicados antes de aplicar: última maestra válida, suma de stock/consumo compatible.
- [x] Reemplazar saldos absolutos y consumo histórico; registrar ajuste positivo/negativo por delta con actor/batch.
- [x] Registrar dry run/rechazo/aplicación, perfil efectivo, hashes por archivo y hash de manifiesto ordenado.
- [x] Serializar importación/reset por organización mediante bloqueo transaccional PostgreSQL.
- [x] Verificar carga, reimportación, duplicados, referencias, parcial sin filas válidas y períodos incompatibles.
- [x] Verificar dos importaciones simultáneas y conciliación del ledger.
- [x] Medir fixture y 10 000 consumos contra objetivos <5 s/<30 s; tiempos en la evidencia de pruebas.
- [x] Consolidar la nueva corrida Release y sus mediciones en verification.md.

```bash
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~ImportServiceTests|FullyQualifiedName~ImportSafetyTests'
```

### T6 — Identidad, cuentas y roles

**Código:** [CognitoJwtExtensions](../../../src/Api/Authentication/CognitoJwtExtensions.cs), [transformación de roles](../../../src/Api/Authentication/AccountRoleClaimsTransformation.cs), [AccountSeeder](../../../src/Infrastructure/Persistence/AccountSeeder.cs), [DemoOptions](../../../src/Application/Demo/DemoOptions.cs).

- [x] Conservar firma RS256, issuer, client_id, expiración, access token y scope.
- [x] Resolver configuración al crear opciones JWT; retirar el cambio global de variables de entorno de ApiFactory.
- [x] Resolver identidad humana por issuer/sub y técnica por issuer/client_id registrado, con scope técnico.
- [x] Eliminar roles/account_id del token; conceder solo rol válido desde una cuenta y organización activas.
- [x] Sembrar cuentas únicamente en demo, de forma idempotente y sin reactivar ni elevar cuentas existentes.
- [x] Aplicar Gerencia/Planificador a imports/reset; lectura de puntos para roles reconocidos.
- [x] Verificar rol falsificado, revocación con mismo JWT, issuer/client/scope/firma/expiración ajenos y M2M.
- [x] Confirmar resultado consolidado Release después de los cambios de inicialización (106 pruebas aprobadas).
- [ ] Ejecutar E2E-04 con Cognito real cuando exista el entorno autorizado; los tokens de prueba no lo sustituyen.

```bash
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~AuthorizationTests|FullyQualifiedName~AuthorizationSafetyTests'
```

### T7 — Endpoints, reset y empaquetado

**Código:** [ImportsController](../../../src/Api/Controllers/ImportsController.cs), [DemoController](../../../src/Api/Controllers/DemoController.cs), [reset](../../../src/Infrastructure/Demo/DemoResetService.cs), [Program](../../../src/Api/Program.cs), [Dockerfile](../../../infra/docker/backend/Dockerfile).

- [x] Implementar multipart, asociación por entidad/perfil, dryRun=true y allowPartial=false.
- [x] Rechazar archivos sin entidad, entidad duplicada, perfil enviado como archivo y mapeo contradictorio.
- [x] Fijar límites: 7 archivos, 10 MiB/archivo, 50 MiB/solicitud, perfil 64 KiB, 100 000 filas y 128 columnas.
- [x] Aplicar presupuesto de 20 peticiones/minuto por cuenta a imports/reset y devolver Retry-After.
- [x] Dejar demo desactivada por defecto y retornar 404 al planificador autorizado cuando está desactivada.
- [x] Reset transaccional por organización: conservar cuentas/batches, pasar actor y guardar rechazo fuera del rollback.
- [x] Verificar reset repetido y reset fallido con datos/historial/actor conservados.
- [x] Mantener contrato JSON de service-points y exigir readiness con esquema compatible.
- [x] Empaquetar fixtures, añadir dockerignore y construir `istpetdev-backend:spec001`.
- [x] Comprobar usuario no root y lectura de los ocho archivos demo en la imagen.
- [x] Añadir `ImportBoundaryTests` para 400/413/422/429, campos protegidos y cuota por cuenta.
- [x] Ejecutar nuevas pruebas HTTP de límites y reconstruir imagen tras los últimos cambios.
- [ ] Registrar por separado un ensayo expuesto; no ejecutar E2E-05 contra RDS/host como parte implícita de esta tarea.

```bash
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~ImportsApiTests|FullyQualifiedName~ImportBoundaryTests'
docker build -f infra/docker/backend/Dockerfile -t istpetdev-backend:spec001 .
```

### T8 — E2E, CI y evidencia

**Código:** [DatasetJourneyTests](../../../tests/IstpetDev.Tests/E2E/DatasetJourneyTests.cs), [CI](../../../.github/workflows/ci.yml), [QA CD](../../../.github/workflows/deploy.yml), [comprobación TRX](../../../scripts/db/check-test-results.py).

- [x] Verificar reset → seco externo → aplicar → reimportar → reset mediante HTTP real de ApiFactory/PostGIS.
- [x] Incluir fixtures/tests/solución/configuración/scripts en filtros CI y backend de CD.
- [x] Reemplazar Schema95Check activo por Unit/Integration/E2E y exigir pruebas no vacías, sin omitidas ni fallidas.
- [x] Conservar TRX como artifact de CI y exigir readiness al verificar un despliegue.
- [x] Corregir dependencias transitivas detectadas: System.IO.Packaging 8.0.1 y SSH.NET 2026.0.0 (solo tests).
- [x] Repetir scan NuGet: sin vulnerabilidades reportadas en los cinco proyectos.
- [x] Ejecutar y registrar las tres categorías en Release después de completar los casos HTTP adicionales.
- [x] Ejecutar `npm run check`, validador de bóveda, diff y cotejo de migraciones.
- [ ] Ejecutar E2E-03 cronometrado con otra persona y un dataset que no haya preparado quien implementó el importador.
- [x] Registrar E2E-04/05 como medidos únicamente con Cognito/entorno real; documentar pendientes.

```bash
dotnet build IstpetDev.sln -c Release
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/IstpetDev.Tests -c Release --no-build --no-restore --logger trx
npm run check
python docs/boveda/.github/scripts/check_vault.py
```

### T9 — Documentación y cierre demostrado

- [x] Actualizar spec, plan, E2E y verificación con los contratos finales y resultados reales.
- [x] Registrar ADR-21/alcance local y diferencias en las notas fuente antes de Hechos canónicos.
- [ ] Registrar `ImportBatch`/`AgentDecision`, consumo histórico y precisión/espacial del núcleo como alcance de 001.
- [ ] Mantener RNF-14/B-40 y declarar solo el alcance probado de B-29; no cerrar login web/PWA, offline o SignalR.
- [x] Conservar constancia de Schema95 aplicado externamente; no decir que RDS ya tiene DemoCore.
- [x] Registrar trabajo previo y fuente/control documental sin fechas futuras ni resultados de CI inventados.
- [x] Cambiar estado solo con evidencia: implementar/verificar localmente permite `en curso`; `cerrado` requiere los E2E obligatorios, incluido el simulacro humano.

No hay una publicación, commit, PR, migración externa ni despliegue acreditados por este registro.

### T10 — Respaldo y restauración verificables (#15)

**Responsable:** Anthony Pilatasig (procedimiento, scripts y ensayo). **Ejecución en RDS:** Jorge Doicela. **Código:** [database-task.py](../../../scripts/base_datos/database-task.py), [test-bootstrap.py](../../../scripts/base_datos/test-bootstrap.py), [backup-db.sh](../../../scripts/backup/backup-db.sh), [CI](../../../.github/workflows/ci.yml). **Procedimiento:** runbook §4 en la bóveda (`03 Arquitectura/Runbook de DevOps y base de datos para la demo.md`).

**Auditoría previa (15-oct, solo con evidencia del repositorio):**

- **Base de producción:** sigue en Amazon RDS (`istpetdev-rds-dev`, base `istpetdev_integration`). Desarrollo usa un PostgreSQL en EC2 o Docker local.
- **Respaldos de AWS:** RDS tiene PITR de 7 días (o 1 día, según `free_plan_backup_workaround`) y snapshots diarios opcionales de AWS Backup. No se pudo comprobar qué valor está aplicado ni si existen snapshots.
- **La herramienta correcta ya existía:** `database-task.py backup` y `restore-check`, con S3, SHA-256 y hash por tabla. En cambio, `backup-db.sh` respaldaba la base `postgres` con `istpetadmin` al disco del host, sin hash ni S3.
- **Dos defectos que nadie había detectado:**
  - El ensayo `test-bootstrap.py` estaba roto en `develop` y no corría en CI.
  - `database-task.py migrate` fallaba con un error de sintaxis de psql.

- [x] Auditar dónde viven los datos y qué respaldos existen; registrar lo no verificable.
- [x] Reemplazar `backup-db.sh` por un atajo de la herramienta verificada (base `istpetdev_integration`, rol `backup_integration`, S3 con checksum).
- [x] Corregir `database-task.py migrate` para usar el migrador de producción (`IstpetDev.Api.dll --migrate`).
- [x] Reparar `test-bootstrap.py` (lista exacta de migraciones y tabla de prueba que bloqueaba readiness) y ejecutarlo: pasó el 15-oct 16:47 UTC en 76 s.
- [x] Agregar el ensayo a CI (job «Ensayo de respaldo y restauración»).
- [x] Reescribir el runbook §4 con el procedimiento, los responsables y el estado verificado.
- [ ] Snapshot `pre-democore`: sin evidencia; no se presenta como realizado.
- [ ] Volcado de `istpetdev_integration` en S3 y `restore-check` contra RDS, con evidencia (Jorge Doicela, requiere AWS).
- [ ] Respaldo automático en `deploy.yml` antes de migrar: requiere permisos de S3 y Secrets Manager en el rol del host, más `pg_dump` y AWS CLI en el host.

```bash
python3 scripts/base_datos/test-bootstrap.py   # Linux con Docker, psql, openssl y la API compilada en Debug
```

**Registro de cambios de T10:**

| Archivo | Componente | Qué se hizo | Por qué | Estado |
|---|---|---|---|---|
| `scripts/backup/backup-db.sh` | DB / Operación | Ahora es un atajo de `database-task.py backup --member <base>` que valida los archivos privados | Respaldaba la base de mantenimiento `postgres` con `istpetadmin`, en SQL plano, al disco de un host que no conserva datos, sin hash ni S3. Además duplicaba una herramienta que ya hacía todo eso correctamente | Hecho; ejecución contra RDS pendiente |
| `scripts/base_datos/database-task.py` | DB / Backend | `migrate()` ejecuta `IstpetDev.Api.dll --migrate` con la cadena del migrador, en vez de `dotnet ef migrations script --idempotent` vía psql | El script idempotente falla con `syntax error at or near "END"` sobre las migraciones actuales. Producción migra con el migrador de EF de la API (`ApplyMigrations=true`), así que se usa el mismo camino. El error de la API se oculta, igual que los demás, porque puede contener la cadena de conexión | Hecho y verificado |
| `scripts/base_datos/test-bootstrap.py` | DB / Pruebas | Compara las migraciones aplicadas con la lista exacta del repositorio (antes esperaba 1) y borra `isolation_probe` antes de migrar | La aserción databa del 9-oct, cuando había una sola migración (hoy hay 8). Además, `/health/ready` exige exactamente las tablas de DemoCore (`DatabaseHealthCheck`), y la tabla de prueba lo dejaba en «Unhealthy» | Hecho; pasó en 76 s |
| `.github/workflows/ci.yml` | CI | Job `db-backup-rehearsal` y filtros para `scripts/base_datos/**`, `scripts/backup/**` y `scripts/ops/dev.py` | El ensayo de respaldo y restauración se había roto sin que nadie lo notara porque no corría en CI | Hecho; primera ejecución en el PR |
| Bóveda: runbook §4 | Documentación | Inventario verificado de respaldos, herramienta oficial, procedimiento por pasos con responsables, evidencia del ensayo y pendientes | El runbook usaba un snapshot de ejemplo, no tenía la restauración de prueba y no decía qué está verificado | Hecho |

### T11 — Importador que se adapta al dataset (15-oct)

**Responsable:** Anthony Pilatasig. **Código:** [ImportEntityDetector](../../../src/Application/Imports/ImportEntityDetector.cs), [ImportsController](../../../src/Api/Controllers/ImportsController.cs), [página Datos](../../../apps/web/src/pages/datos/datos.page.ts). **Pruebas:** [ImportEntityDetectorTests](../../../tests/IstpetDev.Tests/Unit/Imports/ImportEntityDetectorTests.cs), [AdaptiveImportTests](../../../tests/IstpetDev.Tests/Integration/Imports/AdaptiveImportTests.cs).

**Problema comprobado.** Se subieron los archivos de `docs/dataset/test/national-v1-seed-11` desde la carga masiva de la web y la pantalla quedó vacía.

- **Por qué fallaba:** la web envía cada archivo con su nombre completo (`consumptions.csv`) como campo, y el servidor solo aceptaba la clave sin extensión. Respondía `400 «Archivo sin entidad»`, e `importData` descartaba el error y devolvía `null`.
- **El dataset no tenía errores:** con la clave canónica se importaba con 0 errores.
- **La interfaz prometía otra cosa:** «el nombre del archivo o sus encabezados detectarán la entidad», pero el servidor no lo hacía.

**Regla nueva (FR-04 llevado a la entidad).** La entidad de cada archivo se resuelve en este orden:

1. Perfil de mapeo (`file`).
2. Nombre del campo.
3. **Nombre del archivo**, sin extensión, con claves canónicas y alias en español (`Consumos Septiembre.xlsx`, `bodegas-quito.csv`).
4. **Columnas del archivo**: gana la entidad que tiene todos sus campos obligatorios y explica más columnas.

**Sin adivinanzas:**
- **Empate:** si dos entidades encajan igual (por ejemplo, código, nombre y coordenadas sirven para una bodega y para un punto), el servidor responde 400 y explica cómo resolverlo.
- **Nada encaja:** responde 400 con la entidad más cercana, las columnas que le faltan y la indicación de usar un perfil de mapeo.

**En la web:**
- Un `.json` incluido en la selección se envía como perfil (por ejemplo, `mapping.json`).
- Un rechazo sin reporte (400, 409, 413 o falta de red) se muestra con el motivo del servidor, en lugar de dejar la pantalla vacía.

**Datasets de `docs/dataset`:**
- `test/national-v1-seed-11`, enviado como lo envía la web, **se aplica** con 0 errores: 6 bodegas, 80 puntos, 12 vehículos y 2240 consumos.
- `reales/acess-2026-09` **no es un dato operativo:** es un agregado de permisos por provincia, cantón y tipo, sin códigos ni coordenadas, y su README prohíbe insertarlo. El sistema lo rechaza explicando por qué y qué columnas le faltan para ser, por ejemplo, un punto de servicio.

| Archivo / Componente | Capa | Cambio | Por qué | Estado |
|---|---|---|---|---|
| `src/Application/Imports/ImportEntityDetector.cs` | Backend | Detección por nombre de archivo y por columnas, con explicación | Aceptar los archivos como vienen | Hecho; 12 pruebas unitarias |
| `src/Application/Imports/ImportEntity.cs` | Backend | `Names()`: claves y alias | Para detectar por nombre | Hecho |
| `src/Api/Controllers/ImportsController.cs` | Backend | Cadena perfil → campo → nombre → columnas; 400 con el motivo | Antes: «Archivo sin entidad» para `consumptions.csv` | Hecho |
| `apps/web/src/shared/api/api.service.ts` | Frontend | `importDataOrExplain`: el error llega con el motivo del servidor | `importData` ocultaba los 400 | Hecho |
| `apps/web/src/pages/datos/datos.page.ts` | Frontend | Acepta `.json` como perfil; aviso de error visible | Cargar el dataset con su `mapping.json` y saber por qué falla | Hecho |
| `tests/.../AdaptiveImportTests.cs` | Pruebas | Dataset nacional real aplicado, archivo con nombre desconocido y ACESS rechazado con explicación | Verificar con los datos reales del repositorio | 3/3 en verde |

Verificación (15-oct): backend 426/426; web 54/54 y `build:web` de producción sin errores.

