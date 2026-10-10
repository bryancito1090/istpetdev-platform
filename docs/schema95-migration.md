# Migración del esquema de 95 tablas

Estado: aplicada y verificada directamente en RDS en la base personal `istpetdev_dev_01` el 9 de octubre de 2026 (hora de Ecuador). La API permanece detenida. No acredita lanzamiento de la app ni aplicación en integración o en las bases de los compañeros. Conserva `20261008131600_InitialSchema` y añade `20261010023200_CompleteSystemSchema95`, su Designer y el snapshot de EF Core. Ambas migraciones están aplicadas: no editarlas ni regenerarlas, incluso antes del merge; corregir la estructura con una migración posterior revisada.

## Antes de aplicar

1. Detener la API local. Tener sesión AWS vigente y túnel SSM en 15432.
2. Revisar el diff y las instrucciones SQL incrustadas en `src/Infrastructure/Persistence/Migrations/Sql`.
3. Ejecutar en DBeaver, conectado al migrador de la base personal:

```sql
SELECT current_database(), current_user;
SELECT 'Products' AS tabla, count(*) AS registros FROM public."Products"
UNION ALL SELECT 'ServicePoints', count(*) FROM public."ServicePoints"
UNION ALL SELECT 'InventoryItems', count(*) FROM public."InventoryItems"
UNION ALL SELECT 'Deliveries', count(*) FROM public."Deliveries"
UNION ALL SELECT 'RiskAssessments', count(*) FROM public."RiskAssessments";
```

La transición exige que las cinco tablas estén vacías. No elimina registros para conseguirlo. Si existen registros, se detiene antes del cambio de estructura y exige una reconciliación adicional: origen de movimientos iniciales de stock, lotes, ubicaciones, workflow y proveniencia de riesgo. No borrar tablas, historial EF ni datos para saltarse esta protección. La migración bloquea las cinco tablas durante el preflight para impedir inserciones concurrentes entre comprobación y DDL.

## Aplicación personal de Bryan

Desde la raíz de ISTPETDEV:

```bash
bash scripts/db/migrate-personal.sh istpetdev-admin dev_01
```

El script comprueba el modelo, carga `migrator_dev_01`, aplica las migraciones y restaura `user_dev_01` aun cuando la actualización falla. No levanta la app ni modifica integración. Si falla AWS, renovar sesión con `aws login --profile istpetdev-admin-console --region us-east-1`. No iniciar la API si la restauración de credenciales falla.

### Resultado comprobado en RDS

La ejecución anterior terminó con código 0. No es necesario volver a aplicarla en la base personal de Bryan para preparar el PR.

- Sesión `istpetdev-admin` y túnel SSM en `127.0.0.1:15432` verificados. Conexión con VerifyFull y TLS 1.3 a RDS PostgreSQL 16.13, con PostGIS 3.4.6.
- Las cinco tablas iniciales tenían cero registros antes de la transición. Se creó y comprobó disponible un snapshot previo; no se borraron datos para superar la guarda.
- Consultas al servidor comprobaron `InitialSchema` y `20261010023200_CompleteSystemSchema95` en `public."__EFMigrationsHistory"`.
- Los nombres de las 95 tablas de dominio coincidieron exactamente con `docs/schema95.json`. Las 95 tenían `relrowsecurity = true`, `relforcerowsecurity = true` y propietario `migrator_dev_01`. El conteo excluyó el historial EF y las tablas de extensión PostGIS.
- La conexión local de la API se comparó con el secreto personal autorizado y se autenticó en RDS como `user_dev_01`. Ese rol no tiene SUPERUSER, BYPASSRLS ni membresía del migrador. Los secretos y la evidencia detallada permanecen fuera del repositorio.

En DBeaver: conexión de migraciones → Databases → istpetdev_dev_01 → Schemas → public → Tables → Refresh. Se esperan 95 tablas de dominio, más `__EFMigrationsHistory` y las tablas propias de PostGIS (por ejemplo, `spatial_ref_sys`). Product, ServicePoint y Delivery pasan al nombre canónico singular; InventoryItems y RiskAssessments se sustituyen por el catálogo nuevo, únicamente estando vacías.

```sql
SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId";
SELECT count(*) AS tablas_con_rls
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public' AND c.relkind = 'r'
  AND c.relrowsecurity AND c.relforcerowsecurity;
```

## Verificación de aislamiento

`scripts/db/schema95-check.sql` es una prueba con dos organizaciones y cuentas sintéticas, puntos visibles/ocultos, cambio de contexto en una misma sesión, FK entre organizaciones, escritura no autorizada, permisos de historial y manipulación de RBAC. Todo se ejecuta dentro de una transacción revertida. Usar una base personal/disposable; no integración. El archivo usa comandos de psql (`\set`, `\gset`) y no se pega directamente en DBeaver.

El migrador tiene NOINHERIT y no puede asumir el usuario API por sí solo. La prueba de psql exige una sesión de prueba autorizada para `SET ROLE user_dev_XX` (por ejemplo, el administrador del entorno disposable). No conceder al usuario API el rol de migrador ni ampliar permisos en RDS para ejecutar una prueba.

Para una sesión ADMIN de pruebas cuyo rol no hereda migrador, establecer explícitamente el rol migrador al comienzo de una copia del script y volver al administrador antes del SET LOCAL ROLE del usuario API; conceder temporalmente solo los memberships necesarios en el entorno disposable y revocarlos después. La prueba automática local usa SET ROLE en un cluster descartable, nunca modifica memberships del RDS.

`scripts/db/schema95-integrity-check.sql` complementa esos casos con identidad inmutable, Version, historial, publicación de plantillas/campos/opciones y activaciones solapadas. Se ejecuta con el migrador personal usando psql y revierte todos sus datos sintéticos.

Comprobación del modelo sin conectar a RDS:

```bash
dotnet run --project tools/Schema95Check -- "$PWD"
```

Comprueba 95 tablas/90 tenant, tipos y nulabilidad del catálogo `docs/schema95.json`, FK compuestas, borrados RESTRICT, concurrencia, UUID v7, snapshot sin cambios pendientes, historial inicial conservado y SQL con guardas/FORCE RLS.

## Alcance de seguridad y acceso inicial

Las 95 tablas activan ENABLE/FORCE RLS. El migrador conserva un permiso explícito de mantenimiento. El usuario API es no propietario y no debe ser superusuario ni BYPASSRLS. Las políticas verifican contexto, membresía activa y permisos/ámbitos; no se considera válida la lectura de un migrador para demostrar RLS.

Helpers invoker y políticas específicas de Account/OrganizationAccess evitan recursión. La función de resolución de cuenta es SECURITY DEFINER con propietario migrador, search_path fijo y EXECUTE restringido; solo devuelve el Id activo que coincide con issuer/subject. La API resuelve esa cuenta desde el JWT validado, recibe X-Organization-Id como selección, valida membresía y fija el contexto parametrizado con SET LOCAL dentro de una transacción. Se adapta el endpoint existente de puntos de servicio y sus coordenadas anulables.

El schema crea una organización demo y siete roles iniciales sin asignar cuentas ni capacidades por nombre. Solo siembra el permiso `service-point.read`, porque ese endpoint ya existe. Los demás códigos propuestos se incorporarán junto con handlers y pruebas reales. No se conceden privilegios futuros automáticamente. Para empezar a leer datos de aplicación hacen falta una cuenta vinculada a Cognito, OrganizationAccess, AccessRole, RolePermission y ámbitos explícitos; ese bootstrap requiere issuer/subject reales y no se inventa con nombres de usuarios PostgreSQL.

Por RLS, la conexión DATOS puede mostrar cero filas sin contexto. DBeaver requiere una transacción con `app.account_id` y `app.organization_id` de una cuenta autorizada. Tener el password SQL permite cambiar contexto; no entregarlo al navegador. RBAC, DTOs y comandos transaccionales siguen siendo necesarios. Tablas de identidad/permisos son de lectura para la conexión API; su administración necesita un camino privilegiado acotado, aún no implementado. Outbox/auditoría/sync quedan sin escrituras genéricas del usuario API hasta entregar sus comandos acotados. RLS de rutas/flota y campos sensibles requiere pruebas de cada caso de uso al implementarlo; no cerrar las validaciones del sistema completo por contar tablas.

La base incluye FK/versiones, cantidades/rangos, grants, historial insert-only, identidad/organización inmutables, versiones publicadas congeladas, opciones/campos congelados y rechazo de activaciones solapadas. Las reglas que agregan múltiples filas (stock, recepciones acumuladas, capacidad/rutas), clasificación/cifrado de contenido, publicación/validación JSON, retención y autorizaciones por campo necesitan handlers y validaciones adicionales. No se incluye un cifrado ficticio ni cuentas de usuario final con password en PostgreSQL.

## Pruebas realizadas y pendientes

Resultados de la aplicación personal y preparación del PR, con la revisión de código conservada:

| Verificación | Entorno y resultado |
|---|---|
| Compilación .NET 8 y `tools/Schema95Check` | Reutilizados de la ejecución personal vigente: build con cero errores/advertencias; PASS de 95 tablas, 90 tenant, tipos/nulabilidad, FK RESTRICT y compuestas, concurrencia, UUID v7, snapshot y guardas/FORCE RLS. Las huellas de los archivos de código no cambiaron después de esa ejecución. |
| Contrato TypeScript afectado | `npm run build:shared`: código 0. No hay consumidores de las coordenadas de ServicePoint en las apps actuales. |
| Historial y catálogo/RLS | Comprobados directamente en RDS PostgreSQL 16.13/PostGIS 3.4.6: ambas migraciones registradas y 95/95 tablas con ENABLE/FORCE RLS. |
| `schema95-integrity-check.sql` | Ejecutado en RDS personal con `migrator_dev_01`: código 0 y `schema95_integrity_checks_passed`. Identidad, Version, historial, publicación y solapamientos comprobados. Conteos y hashes de las 97 tablas de public, incluidos historial EF y PostGIS, iguales antes/después del rollback. |
| `schema95-check.sql` | Ejecutado en contenedor descartable PostgreSQL 16.4/PostGIS 3.4.3: `schema95_security_checks_passed`. Dos organizaciones, ámbito de puntos, contexto vacío/no miembro, cambio de organización en la misma sesión, FK entre organizaciones, denegación de escritura/RBAC/historial y lectura de las 95 políticas comprobados como `user_dev_01`, sin SUPERUSER/BYPASSRLS. |
| Integridad en motor descartable | También pasó `schema95-integrity-check.sql`. Ambos scripts conservaron las huellas de las 95 tablas de dominio y del historial tras el rollback de sus datos sintéticos. |
| SQL EF idempotente y catálogo físico | SQL generado sin modificar las migraciones, aplicado dos veces en ese contenedor: dos entradas EF sin duplicación. Las 604 columnas del catálogo coincidieron en tipo y nulabilidad con el catálogo físico PostgreSQL/PostGIS. |
| PostGIS por SQL | En ese contenedor: inserción/lectura de geography(Point,4326), SRID, coordenadas X/Y y ST_DWithin comprobados dentro de una transacción revertida. |
| Pool Npgsql y filtro real | `python3 scripts/db/check-schema95-runtime.py`: código 0, `schema95_runtime_checks_passed`. En PostgreSQL 16.4/PostGIS 3.4.3 descartable con VerifyFull, 16 aperturas de la conexión API reutilizaron el mismo PID de backend entre cuentas/organizaciones. Contexto vacío antes y después del filtro, incluso antes de cerrar la conexión. Commit conservó una escritura; respuesta 400, excepción SQL y cancelación activa de pg_sleep revirtieron las escrituras. El pool mantuvo su reset normal. |
| EF/Npgsql/NTS y controlador real | En ese ensayo: escritura/materialización geography(Point,4326) y null mediante EF; Npgsql rechazó SRID 3857 y LineString para geography(Point,4326). ST_Distance se tradujo y ejecutó en PostgreSQL respetando el ámbito. ServicePoints devolvió solo puntos asignados del tenant, Type del catálogo, coordenadas correctas y nulls, enumerando el DTO después de terminar el filtro/transacción. |
| Denegaciones con cliente .NET | En ese ensayo: sesión SQL de user_dev_01 sin SUPERUSER/BYPASSRLS; UPDATE de una cuenta sin permiso afectó cero filas. El filtro rechazó tenant ajeno, cuenta sin membresía, cuenta sin permiso, cuenta inactiva, claims ausentes y header ausente sin ejecutar la acción. Contexto vacío denegó lecturas incluso en la conexión todavía abierta. Se comprobaron resultados MVC directos; no respuestas del pipeline HTTP. |

El contenedor usó red deshabilitada, sin puertos publicados, y se eliminó junto con sus datos al terminar. El primer intento coincidió con el reinicio del servidor temporal de inicialización de la imagen; se corrigió la espera para comprobar el servidor TCP final y el ensayo completo pasó. No se inició la API ni se ejecutó el instalador del parche o el bootstrap del proyecto.

El ensayo .NET posterior usa un contenedor distinto con un puerto PostgreSQL dinámico publicado exclusivamente en 127.0.0.1, certificado y passwords efímeros, VerifyFull y roles limitados. Su launcher elimina el contenedor y los datos al terminar, incluso si una prueba falla. Solo crea la base personal simulada; no consulta AWS ni carga user-secrets. Rechaza el puerto 15432 y destinos distintos de localhost. Invoca `AddInfrastructureServices`, `OrganizationContextFilter` y `ServicePointsController` reales sin arrancar la API. El primer pase se detuvo por una aserción del verificador que trataba ForbidResult como IStatusCodeActionResult; se corrigió únicamente el verificador y el pase completo terminó con código 0.

Para reproducir el ensayo .NET, con Docker, openssl, psql y el SDK .NET del repositorio disponibles:

```bash
python3 scripts/db/check-schema95-runtime.py
```

El resumen y el log del launcher se guardan en la carpeta privada de ISTPETDEV, fuera de Git. Las cuentas/claims, roles y permisos de negocio de este ensayo son sintéticos y se destruyen con el contenedor; no son identidades Cognito ni autorizan un bootstrap real.

**Impedimento específico en RDS:** el migrador no puede asumir `user_dev_01`; la sesión administrativa disponible tampoco tiene autorización existente para asumir los dos roles de la prueba. Por eso `schema95-check.sql` pasó en el entorno descartable, no en RDS. No se concedieron membresías ni se ampliaron permisos en RDS. El PASS de integridad con migrador no demuestra por sí solo aislamiento de la conexión API en RDS.

Los ensayos anteriores documentados en PostgreSQL 18.6 standalone incluyeron rechazo de la transición con registros legados y conservación del historial inicial. No se repitieron en esta preparación del PR. Su sustituto espacial `point` no acredita comportamiento PostGIS; las comprobaciones SQL espaciales nuevas usan la extensión real.

### Casos todavía pendientes

- **Pool Npgsql en condiciones reales:** el ensayo .NET secuencial con MaxPoolSize=1 y reset normal pasó en el motor descartable. Quedan RDS, concurrencia/tamaño de pool de la configuración habitual, conexión rota y cancelación/fallo durante commit o devolución al pool. El ensayo no ejecuta middleware HTTP ni usuarios JWT reales.
- **Cognito/JWT:** tokens reales con firma, issuer, caducidad, token_use, client_id y scope; resolución de issuer/subject a Account y rechazo de cuentas inactivas o sin membresía vigente. No se crearon cuentas Cognito ni un bootstrap de identidad ficticio.
- **HTTP:** matriz preparada en [schema95-jwt-http-checks.md](schema95-jwt-http-checks.md), con 401/400/403/éxito, ámbitos y nulls. Los resultados del filtro/controlador directo con claims sintéticos no validan autenticación, routing ni serialización del pipeline HTTP. No hay autorización para levantar la API en este encargo.
- **PostGIS mediante Npgsql/EF en RDS:** materialización/escritura NTS, rechazos de SRID/tipo, ST_Distance y recorrido del controlador ya pasaron con el cliente real en el motor descartable. Queda repetir la ruta autorizada contra RDS y comprobar el pipeline HTTP; motor/extensión locales 16.4/3.4.3 difieren de RDS 16.13/3.4.6. No se acredita esta prueba en RDS por el PASS local.
- **Seguridad por caso de uso:** denegaciones con roles autorizados en RDS, rutas/flota, campos sensibles y comandos privilegiados acotados para RBAC/outbox/auditoría/sync; reglas agregadas de stock/recepciones/capacidad y validaciones JSON/cifrado/retención siguen requiriendo handlers y pruebas.

El PR hacia develop debe permanecer como borrador mientras falten las comprobaciones necesarias para aprobarlo. No declarar listo el sistema completo por contar tablas. Antes de pruebas futuras que levanten o expongan la app, aplicar la skill prelaunch y obtener el alcance de ejecución correspondiente; esta sesión mantiene la API detenida.

## Base compartida y siguientes cambios

Primero revisar/probar la migración personal, ejecutar las verificaciones pendientes y fusionar el PR hacia develop. Una sola persona aplica después exactamente esa revisión a `istpetdev_integration` con `migrator_integration`, verificando respaldo y sin migradores concurrentes. Restaurar siempre las credenciales personales al terminar. Si la base común tiene registros en las tablas iniciales, la misma guarda detendrá la operación: requiere una migración de reconciliación, no una excepción a la guarda.

El downgrade también se detiene si existe cualquier registro en el nuevo catálogo, incluidos los seeds iniciales. Esta entrega se trata como forward-only una vez creada: corregir con otra migración, no borrar datos para revertir. El SQL de rollback generado no autoriza eliminar datos.

Los compañeros reciben los modelos, migraciones, SQL incrustado y snapshot por Git. Después de pull, aplican `database update` con su migrador personal y restauran su usuario de datos. No vuelven a generar esta migración. Nuevas entidades/cambios actualizan el catálogo/modelo, generan otra migración y prueban los casos afectados antes del PR.
