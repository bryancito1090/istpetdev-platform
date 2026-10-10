# Casos JWT/HTTP de schema95 pendientes de ejecución

Estos casos están preparados para una ventana de pruebas autorizada. No se ejecutaron por HTTP ni se levantó la API en el encargo actual. `Schema95RuntimeCheck` invoca el filtro y el controlador reales con claims sintéticos; no valida firmas JWT, middleware, routing ni serialización HTTP.

La implementación a probar es `CognitoJwtExtensions`, `OrganizationContextFilter` y `ServicePointsController`, con la configuración real de issuer/client_ids/scope. Usar únicamente un entorno personal o descartable y cuentas Cognito existentes que estén autorizadas para pruebas. Obtener privadamente sus tokens; no guardarlos en Git, argumentos de shell, URLs, trazas ni en este documento. No crear identidades ni ampliar permisos de RDS para suplir datos faltantes.

## Matriz de ejecución

Cambiar una sola condición por caso. El recurso es `GET /api/v1/service-points`; cada resultado debe comprobar estado y contenido de la respuesta, además de ausencia de filas de otra organización/ámbito.

| Caso | Preparación | Resultado esperado |
|---|---|---|
| Sin bearer / bearer inválido | Sin Authorization, o JWT corrupto | 401 del middleware; el filtro/controlador no concede acceso. |
| Firma/issuer/caducidad | Token con firma o issuer ajenos; token expirado más allá del ClockSkew de un minuto | 401, sin consulta autorizada de ServicePoints. |
| ID token | Token emitido con token_use=id | 401 aunque issuer y firma sean válidos. |
| Cliente no permitido | Access token de client_id fuera de AllowedClientIds | 401. |
| Scope ausente | Access token sin istpetdev-api/access | 401. |
| Claims mapeados | Access token válido; comprobar sub y su posible mapeo a NameIdentifier | Resolución de la misma Account por issuer/subject; nunca usar el usuario SQL como identidad. |
| Organización inválida | Token válido; X-Organization-Id ausente, no UUID o valor ambiguo | 400, sin ejecutar la acción. |
| Cuenta desconocida/inactiva | JWT válido pero sin Account activa coincidente | 403. |
| Sin membresía vigente | Organización seleccionada ajena; acceso revocado, inactivo, futuro o caducado | 403. Seleccionar un header no concede membresía. |
| Sin permiso | Membresía válida pero sin service-point.read | 403. |
| Lectura autorizada | Account, membresía, rol, permiso y AccessServicePoint explícitos | 200; solo puntos asignados de esa organización. Verificar Code/Type y latitud/longitud, incluidos nulls. |
| Catálogo | Punto con TypeOptionId autorizado y punto sin opción | Código de la opción o cadena vacía según el contrato actual; sin datos de otro tenant. |
| Cuenta A → cuenta B | Peticiones alternadas y concurrentes, con tokens reales distintos | Cada respuesta contiene únicamente su ámbito. Repetir con pool de tamaño real, sin reutilizar claims sintéticos. |
| Error / aborto | Error del handler o cancelación de la petición, seguida de otra cuenta/organización | Sin escrituras parciales ni contexto reutilizado en la siguiente petición; comprobar la conexión devuelta al pool. |

Para tokens inválidos que Cognito no puede emitir (firma/issuer/client_id deliberadamente alterados), usar un fixture de autenticación controlado en un ensayo HTTP autorizado y documentar esa diferencia; no presentar un JWT sintético como token real de Cognito. Para tokens expirados, conservar únicamente material de prueba privado y comprobar el margen de reloj.

## Ejecución adicional necesaria

1. Autorizar expresamente el arranque de la API de pruebas y aplicar prelaunch antes de hacerlo. Limitar el listener al entorno acordado; el encargo actual mantiene la API detenida.
2. Disponer de issuer/client_ids reales y de cuentas de prueba existentes con tokens válidos, así como sus vínculos Account/OrganizationAccess/roles/ámbitos. Los fixtures SQL sintéticos actuales no representan identidades Cognito. Cualquier bootstrap de identidad real necesita su alcance propio.
3. Ejecutar la matriz contra el pipeline HTTP completo, observando los resultados sin registrar tokens. Separar resultados de Cognito real, de autenticación sintética y de invocación directa del filtro/controlador.
4. Registrar entorno, revisión, casos ejecutados, estados/cuerpos sanitizados, cleanup de fixtures y casos que no pudieron prepararse. Dejar pendiente cada caso no ejecutado.

Los ensayos actuales de pool/PostGIS usan PostgreSQL 16/PostGIS descartable con TLS VerifyFull y el cliente EF/Npgsql real. No sustituyen esta matriz HTTP ni el ensayo de aislamiento del usuario API en RDS, cuyo impedimento de roles permanece.
