# Verificación JWT/HTTP de schema95

El 10 de octubre de 2026 se ejecutó el pipeline HTTP completo con **JWT sintéticos firmados**, no con Cognito real. Comando reproducible, únicamente con autorización para iniciar la API local:

```bash
python3 scripts/db/check-schema95-runtime.py --http
```

Resultado: `schema95_http_checks_passed`, código 0. PostgreSQL 16.4/PostGIS 3.4.3 descartable, TLS VerifyFull y login `user_dev_01` limitado; Kestrel y PostgreSQL publicados exclusivamente en loopback, en puertos dinámicos distintos del túnel RDS. Al terminar se detuvo la API y se eliminaron contenedor, datos, certificados y passwords efímeros. No se consultó AWS/RDS ni se cargaron/modificaron user-secrets.

`Schema95HttpCheck` invoca el entrypoint real de `IstpetDev.Api`, su configuración base y entorno `Schema95SyntheticTest`. Un IHostingStartup del proyecto de pruebas suministra metadata/clave RSA públicas estáticas del issuer sintético, instrumentación y un controlador de fallos. Conserva JwtBearer, validación de firma/issuer/lifetime/algoritmo y eventos de CognitoJwtExtensions. Producción no referencia ese proyecto y permaneció sin cambios. Los tokens y claves privadas viven en memoria; los logs no pueden contener los tokens generados.

## Matriz ejecutada con autenticación sintética

Recurso real: GET /api/v1/service-points. Se comprobaron estado, cuerpo, media type y filas después de middleware, routing, filtros y serialización MVC.

| Caso | Resultado comprobado |
|---|---|
| Sin bearer, JWT corrupto, firma/issuer ajenos, expirado fuera del ClockSkew, not-before futuro o sin firma | 401, cuerpo vacío y challenge Bearer. |
| token_use=id, client_id ajeno/ausente, scope ausente o prefijo distinto del scope requerido | 401, cuerpo vacío y challenge Bearer; rechazo por los eventos de producción. |
| Subject ausente en JWT firmado | 401 del filtro; JSON ProblemDetails con status=401 y application/problem+json. |
| Header de organización ausente, no UUID, ambiguo o con texto de inyección SQL | 400; JSON code=organization_context_required. |
| Cuenta desconocida/inactiva, sin membresía/permiso, membresía revocada/inactiva/futura/caducada, rol revocado/inactivo u organización ajena | 403, cuerpo vacío; resolución por el helper real de issuer/subject. |
| Subject con texto de inyección SQL y firma válida | 403; la consulta parametrizada no concede acceso. |
| Ámbito revocado, con membresía y permiso válidos | 200; array JSON vacío. |
| A y B de organizaciones distintas, más otra cuenta de A con puntos diferentes | 200; solo los códigos asignados a cada cuenta, incluidos ámbitos diferentes del mismo tenant. |
| Catálogo y geografía | Type=clinic y coordenadas (-2.2,-79.9) para A; (-0.2,-78.5) para B. Sin posición/opción: latitude/longitude=null y Type="". |
| Identidad enviada en query string | No reemplaza la organización del header ni el actor del JWT; el DTO omite identidades administradas por servidor. |

Los 401 del challenge y 403 de Forbid **no contienen JSON** en el contrato actual. Se verificó el cuerpo vacío; no se añadió un formato de error para hacer pasar las pruebas.

## Concurrencia y fallos del pool

150 peticiones en total. La tanda principal de 60 peticiones concurrentes alternó tres cuentas en dos organizaciones con MaxPoolSize=10, diez PIDs PostgreSQL y pico de diez conexiones. Se comprobó reutilización de PIDs entre cuentas, ausencia de leases simultáneos del mismo PID y ámbito exacto. Todas las conexiones utilizables tenían contexto vacío y cero puntos visibles sin contexto antes de cerrar/devolver la conexión; reset normal de Npgsql habilitado.

La instrumentación mantiene abierta la conexión del mismo DbContext desde antes del pipeline hasta comprobar el cleanup del filtro. Eso permite observar SET LOCAL antes del cierre, pero retiene conexiones más tiempo que una petición normal; este ensayo acredita aislamiento/recuperación, no capacidad ni latencia de producción.

El controlador de fallos solo existe en el hosting de pruebas y exige el mismo JWT/filtro. Actualiza dos filas autorizadas para comprobar atomicidad:

- Excepción SQL: HTTP 500 vacío y ambas escrituras revertidas.
- Aborto de cliente con pg_sleep activo: cancelación y rollback.
- Conexión rota con pg_terminate_backend del administrador descartable: HTTP 500 y rollback. Diez lecturas concurrentes comprobaron recuperación completa del pool, nuevos PIDs y exclusión del PID terminado.
- Aborto con COMMIT activo esperando PgSleep en PostgreSQL: ambas escrituras revertidas en esta corrida. La espera se inyecta mediante trigger diferido exclusivo de la DB descartable, fuera de las migraciones. Se acepta solo commit completo o rollback completo y se registra cuál ocurrió; un aborto HTTP posterior al commit no promete deshacerlo.
- Aborto dentro de Npgsql DisposeAsync, antes de enviar ROLLBACK: callback sincronizado y stack de DisposeAsync comprobados; ambas escrituras revertidas. El hook depende del [código Npgsql 8.0.6](https://github.com/npgsql/npgsql/blob/v8.0.6/src/Npgsql/Internal/NpgsqlConnector.cs#L1453), que debe revisarse al actualizar el cliente. No cubre todos los fallos de red durante disposal.

Cada cancelación se acompañó de doce lecturas concurrentes de B. Todos los fallos fueron seguidos por una lectura saludable de B. Un cliente cancelado carece de respuesta HTTP: no se registra un estado ficticio para ese caso.

Tres intentos iniciales corrigieron exclusivamente instrumentación: cuerpo de UnauthorizedResult, categoría del logger Npgsql y registro diagnóstico de un PID liberado después de devolverlo al pool. La corrida completa posterior pasó; no se requirió modificar producción ni migraciones.

## Prelaunch del entorno local

Se aplicó la fuente vigente de istpetdev-prelaunch de la bóveda antes del arranque. Este resultado no acredita demo expuesta ni release.

| Control | Estado y alcance |
|---|---|
| RLS | Verificado localmente: 95 tablas ENABLE/FORCE, login API sin SUPERUSER/BYPASSRLS, contexto vacío y ámbitos por cuenta bajo concurrencia/fallos. Se reutilizó el ensayo SQL previo para políticas/tablas hijas/denegaciones no afectadas. |
| Campos/plantillas | Proyección del GET y selección de actor/tenant comprobadas. Administración de campos/formularios y publicación/versionado: pendientes al implementar sus handlers. |
| Datos clasificados | Fixtures sin contenido sensible; clasificación/cifrado por campo pendiente. Cifrado de volumen no cierra este control. |
| SQL/caminos especiales | SQL del filtro parametrizado; headers/subjects hostiles denegados. QR, uploads y renderer de plantillas no tienen handlers expuestos en este ensayo. |
| Abuso/cabeceras | Sin frontend/proxy público. Cabeceras finales y límites de aplicación pendientes para demo/release; saturación del pool no acredita rate limiting. |
| Dependencias | dotnet list package --vulnerable --include-transitive: sin paquetes vulnerables reportados en la API. npm audit: 5 altos y 2 moderados en Tailwind/transitivas; no ejecutados por la API .NET. Evaluación/corrección frontend pendiente, sin upgrade mayor automático. Sin trivy/grype disponibles: scan de imagen pendiente. |

Informes detallados y huellas de fuentes efectivas permanecen en la carpeta privada de ISTPETDEV, fuera de Git.

## Pendientes de Cognito real y RDS

No había un juego autorizado de identidades/tokens Cognito reales disponible para la matriz. No se crearon cuentas ni se pidieron tokens por chat. Quedan issuer/JWKS reales, AllowedClientIds/scope efectivos, vínculos de cuentas/membresías reales y repetición de HTTP/aislamiento con esas identidades. La metadata estática no acredita discovery HTTPS, rotación de claves ni el flujo de login Cognito.

La aplicación personal de schema95 en RDS fue comprobada en una sesión anterior; no se repitió ni modificó en esta continuación. El impedimento de roles de schema95-check.sql, el pool/PostGIS por cliente real en RDS y los casos de uso futuros siguen pendientes. Las pruebas locales no se atribuyen a RDS. El PR permanece como borrador.
