---
tipo: ejecucion
estado: vigente
actualizado: 2026-10-10
tags: [istpetdev, equipo, migraciones, coordinacion]
---

# Coordinacion migracion schema95

Bryan pidió que el agente de la bóveda y el agente de `~/Projects/ISTPETDEV` trabajen juntos para preparar la incorporación del esquema a `develop`. Este archivo contiene la distribución de trabajo y el estado compartido. El envío automático por Orca falló por falta de FUSE; Bryan transmitió el encargo y el agente de software devolvió `/tmp/istpetdev-schema95-respuesta.md`. El agente de la bóveda leyó esa respuesta y revisó el commit local y las evidencias indicadas. Las operaciones remotas del PR se registran según el informe del agente de software.

## Estado y evidencia disponible

La evidencia privada `~/.config/istpetdev/private/schema95-rds-verification.json` fue leída por el agente de la bóveda. Registra la verificación del agente de software a las `2026-10-10T03:19:14.174965+00:00` (9 de octubre, hora de Ecuador):

| Comprobación | Resultado registrado |
|---|---|
| Base y motor | `istpetdev_dev_01`, PostgreSQL 16.13 en RDS |
| Ejecución | `bash scripts/db/migrate-personal.sh istpetdev-admin dev_01`, código 0 |
| Historial EF | InitialSchema y `20261010023200_CompleteSystemSchema95` |
| Catálogo y RLS | 95 tablas de dominio; 95 ENABLE RLS y 95 FORCE RLS |
| Conexión | TLS 1.3 con VerifyFull |
| Credenciales de API | Restauradas a `user_dev_01` y verificadas por el agente de software |
| API | Detenida al cerrar la verificación |
| Respaldo previo | Snapshot registrado por el agente de software |

La bóveda registra esta evidencia; su agente no repitió las consultas en RDS. La aplicación de la migración y las banderas RLS no sustituyen pruebas de denegación, aislamiento ni flujos HTTP. No se acredita aplicación de schema95 en `istpetdev_integration` ni en las bases de los compañeros.

## Resultado de la preparación del PR

- PR [#1 — schema95](https://github.com/bryancito1090/istpetdev-platform/pull/1), como borrador desde `feature/esquema95-rbac` hacia `develop`, según la respuesta del agente de software.
- Commit `bc85792f5ae94eeda79f93482f61cb77ced3b5dd`: 36 rutas, confirmado en el repositorio local. Árbol de trabajo limpio en la rama publicada al revisarlo.
- Comparación del agente de la bóveda: las 35 rutas ajenas a `docs/schema95-migration.md` coinciden con el paquete preparado; las migraciones aplicadas y su SQL no se reescribieron. InitialSchema no cambió en el commit. Esta comprobación de cambios y evidencia no equivale a aprobar el PR ni a validar el flujo HTTP.
- La evidencia privada se conserva fuera de Git. El hash del script de integridad coincide con el informe: `f75727c0b7dabe9c4ebb9f69b1429a2c27fb5de4419033e13c49dc273c535f8b`.

| Prueba | Entorno | Resultado informado y evidencia leída |
|---|---|---|
| Compilación .NET y Schema95Check | Ejecución personal vigente | Resultados reutilizados sin cambios de código; modelo de 95 tablas aprobado |
| Contrato TypeScript | Repositorio de software | `npm run build:shared`, código 0 según respuesta |
| Integridad, inmutabilidad y publicación | RDS personal, PostgreSQL 16.13/PostGIS 3.4.6, migrador | PASS; rollback conservó conteos y hashes de las 97 tablas de public; `schema95-rds-integrity.json` |
| Aislamiento RLS y permisos | PostgreSQL 16.4/PostGIS 3.4.3 descartable | PASS como usuario API sin SUPERUSER/BYPASSRLS; datos sintéticos revertidos; `schema95-disposable-verification.json` |
| Integridad y SQL idempotente | Mismo entorno descartable | PASS; SQL aplicado dos veces sin duplicación; 604 columnas comprobadas en tipo/nulabilidad |
| PostGIS por SQL | Mismo entorno descartable | PASS de geography, SRID 4326 y ST_DWithin con rollback; no valida EF/Npgsql |
| Revisión de secretos | Cambios preparados para commit | Gitleaks y diff check aprobados según respuesta |

El entorno descartable no publicó puertos y se eliminó al terminar, según la evidencia. La API quedó detenida con `user_dev_01`. No se modificó integración ni se fusionó el PR.

## Continuación del 10 de octubre: pruebas del cliente .NET

Commit adicional `8c6791cc6bf5c730b0447d343897fef4fae0e4e7`, confirmado localmente: añade el ejecutable `tools/Schema95RuntimeCheck`, el lanzador `scripts/db/check-schema95-runtime.py`, una matriz JWT/HTTP y documentación de resultados. El diff desde bc85792 no modifica código de producción, migraciones ni sus SQL. El agente de software informó que actualizó el PR #1 y lo conservó como borrador.

La bóveda leyó `~/.config/istpetdev/private/schema95-runtime-verification.json` y comprobó sus cuatro huellas contra las fuentes actuales: lanzador, ejecutable de pruebas, filtro de organización y controlador ServicePoints. Todas coinciden. El registro tiene código 0, `schema95_runtime_checks_passed`, 16 aperturas, PID PostgreSQL 78, API no iniciada, RDS no ensayado y contenedor eliminado. La bóveda revisó los casos del verificador, sin repetir su ejecución.

| Caso | Resultado y límite |
|---|---|
| Pool Npgsql y contexto | PASS secuencial con MaxPoolSize=1: misma conexión física entre cuentas/organizaciones, contexto vacío antes/después del filtro y al abrir la siguiente conexión. Commit conserva una escritura; respuesta 400, excepción SQL y cancelación durante consulta revierten las escrituras. No comprueba concurrencia ni fallos durante commit/devolución al pool. |
| Filtro y permisos | PASS invocando clases reales con claims sintéticos: denegaciones no ejecutan la acción, cuenta sin permiso de escritura no actualiza filas y contexto vacío no lee puntos. Se comprueban resultados MVC directos, no códigos HTTP del middleware. |
| EF/Npgsql/NTS y PostGIS | PASS en PostgreSQL 16.4/PostGIS 3.4.3: geography con SRID 4326 y null, rechazo de SRID/tipo inválidos, traducción de ST_Distance y consulta real de ServicePoints respetando organización/ámbito. No acredita este recorrido en RDS. |
| JWT/HTTP | Matriz preparada en `docs/schema95-jwt-http-checks.md`; sin ejecución HTTP, firma Cognito ni autenticación real. |

El ensayo del cliente usa un puerto PostgreSQL dinámico publicado únicamente en loopback, TLS VerifyFull y contraseñas efímeras; se distingue del ensayo SQL anterior sin puertos publicados. No consultó AWS ni cambió user-secrets o bases reales. La API, integración y migraciones aplicadas quedaron sin cambios según el informe.

## Pendientes antes de aprobar

1. **Aislamiento en RDS:** las autorizaciones existentes no permiten ejecutar la prueba con sus dos roles. No se ampliaron permisos. El PASS local se conserva por separado; no se declara PASS de aislamiento en RDS. Evaluar una prueba mediante conexiones separadas ya autorizadas, con limpieza comprobada, antes de proponer cualquier cambio de permisos.
2. **Pool Npgsql:** la comprobación secuencial de tamaño uno pasó en el entorno descartable. Faltan concurrencia con el pool habitual, conexiones rotas y cancelación/fallo durante commit o devolución al pool, además del recorrido contra RDS.
3. **Cognito/JWT y HTTP:** probar validación del token, resolución de cuenta/membresía y respuestas 401/400/403/éxito del endpoint. Las pruebas deben respetar el alcance autorizado y la revisión prelaunch cuando levanten la aplicación.
4. **PostGIS mediante EF/Npgsql/NTS:** escritura, materialización, tipos/SRID y consulta real del controlador pasaron en el entorno descartable; falta el recorrido contra RDS y la respuesta por HTTP.
5. **Revisión del PR:** adjuntar resultados, resolver observaciones y comprobar checks disponibles. El agente de software informó que no había checks automáticos registrados al abrirlo. El PR sigue como borrador; no se autoriza su merge ni la migración compartida con esta nota.

Las funciones futuras de rutas/flota, administración RBAC, campos sensibles, formularios y reglas agregadas se verifican junto con sus casos de uso. No se exige implementar todo el sistema para revisar este PR, ni se da por implementado por tener sus tablas.

## Parte del agente de la bóveda

- Actualizar [[Esquema completo de base de datos]], [[Hechos canonicos]] y [[Registro de trabajo previo y del evento]] con el estado personal comprobado en el informe.
- Corregir [[istpetdev-dbflow]] para no reescribir migraciones aplicadas, incluso antes del merge, y usar el perfil AWS correcto de Bryan.
- Mantener B-37 en curso y conservar las pruebas pendientes en [[Backlog]].
- Verificar enlaces y formato con `python3 .github/scripts/check_vault.py`.
- Dejar los cambios de documentación disponibles para revisión, sin commit ni push desde esta sesión.

## Parte del agente de ISTPETDEV

Continúa en `feature/esquema95-rbac`; conserva todos los cambios existentes. Trabaja en el repositorio de software y deja al agente de la bóveda las notas compartidas.

1. Lee las instrucciones del repositorio, `docs/schema95-migration.md` y esta nota. Revisa el diff completo, incluidos archivos nuevos.
2. Actualiza `docs/schema95-migration.md` con la aplicación y verificación en RDS. Incluye un resumen sin secretos; no publiques el archivo privado de evidencia.
3. Comprueba compilación, catálogo/modelo y pruebas afectadas. Reutiliza resultados vigentes si la revisión no ha cambiado. Las pruebas `scripts/db/schema95-check.sql` y `scripts/db/schema95-integrity-check.sql` validan casos distintos de contar tablas. Ejecútalas únicamente con roles ya autorizados, en la base personal o en un entorno descartable, y con rollback de datos sintéticos. No amplíes permisos en RDS para hacerlas funcionar. Registra por separado los resultados y cualquier impedimento.
4. Documenta los casos todavía pendientes de pool Npgsql, Cognito/JWT, HTTP y PostGIS. No declares listo el sistema completo por tener 95 tablas. Si vas a levantar la app para pruebas, aplica la skill prelaunch y respeta el alcance autorizado; este encargo no autoriza levantarla ni exponerla.
5. Si aparece un defecto en la estructura, no cambies ni regeneres InitialSchema ni CompleteSystemSchema95: ambas están aplicadas. Propón una migración posterior y explica su impacto antes de una nueva operación de base.
6. Comprueba que el commit excluya secretos, archivos privados y artefactos. Haz commit de los cambios revisados, sube únicamente la rama de feature y abre un PR hacia `develop`. Si quedan comprobaciones necesarias pendientes, abre el PR como borrador y detalla sus pendientes.
7. No fusiones el PR, no hagas push directo a `develop`/`main`, no modifiques integración ni las bases personales de otros integrantes. No ejecutes nuevamente la incorporación del parche.

Para devolver el resultado, escribe `/tmp/istpetdev-schema95-respuesta.md` con: SHA del commit, enlace del PR, archivos cambiados, verificaciones realizadas con su evidencia, pendientes y cuestiones que deba reflejar la bóveda. No incluyas secretos. Indica también el resultado a Bryan: el canal automático de Orca no está disponible en la sesión de la bóveda.

## Después del PR

La revisión y el merge anteceden a la migración compartida. Una sola persona aplicará la revisión aprobada a `istpetdev_integration` con `migrator_integration`, respaldo verificado y sin migradores concurrentes. Los compañeros recibirán las migraciones por Git y aplicarán esas mismas migraciones a su base personal; no regenerarán schema95. Ver [[istpetdev-dbflow]].

## Cierre y Despliegue en Entornos Contenerizados — 12 de octubre de 2026

- **Merge de PR #1 en develop:** Realizado por Joseph Baño (`93fc508`), integrando el esquema canónico de 95 tablas, PostGIS geography y políticas de aislamiento RLS.
- **Orquestación de despliegue contenerizado en EC2:**
  - Se configuró la API .NET (`src/Api/Program.cs`) para soportar la ejecución automática de `db.Database.Migrate()` bajo la variable de entorno `ApplyMigrations: "true"`, garantizando que nuevas instancias aprovisionadas con `user-data.sh` o `docker-compose.prod.yml` levanten y apliquen automáticamente las 95 tablas sobre la base local `istpetdev-db` sin intervención manual ni túneles.
  - Se corrigió el comando `pg_isready` en el workflow de despliegue (`deploy.yml`) especificando `-d istpetdev_db`.
  - La verificación de salud en despliegue valida la disponibilidad de la base de datos contra `/health/ready` previo a habilitar el tráfico.
  - En el frontend, se blindó el consumo de coordenadas en `route-map.widget.ts` filtrando valores nulos procedentes del nuevo esquema PostGIS.
