---
tipo: guia-operativa
estado: vigente
actualizado: 2026-10-09
tags: [postgresql, dbeaver, desarrollo, acceso]
---

# Conexión PostgreSQL en DBeaver

Guía para la base personal de Bryan; sustituir `dev_01` por la asignación correspondiente para cada integrante. El esquema objetivo es [[Esquema completo de base de datos]]; las migraciones siguen [[istpetdev-dbflow]].

## Lo que ya existe y lo que falta

Las capturas de Bryan del 9 de octubre muestran una conexión a `istpetdev_dev_01` con `migrator_dev_01`, contraseña guardada y puerto 15432. Esa conexión sirve para inspeccionar el esquema y aplicar migraciones con el rol correcto en .NET. La captura no acredita RLS, TLS verify-full, existencia de todas las tablas ni permisos RBAC de la aplicación.

El otro usuario ya fue creado por el bootstrap documentado: `user_dev_01`. No crear un usuario nuevo con el mismo nombre ni rotar su password para añadirlo a DBeaver. Su contraseña está en el secreto `/istpetdev/dev/db/dev_01`; la del migrador en `/istpetdev/dev/db/dev_01/migrator`. RBAC de usuarios finales se administra mediante la API/Cognito, no mediante el menú Roles de PostgreSQL.

## Prerequisitos

Túnel SSM abierto en 15432, autenticación AWS vigente, certificado oficial RDS y mapeo local del endpoint real a 127.0.0.1. El archivo privado `dev-access.json` contiene endpoint/nodo/documento; no passwords. Arranque automático puede abrir el túnel al iniciar sesión en el equipo, pero no elimina la renovación de autenticación AWS/MFA. No abrir RDS al público para evitar el túnel.

## Dos conexiones con nombres claros

| Campo | Conexión de migración | Conexión de datos |
|---|---|---|
| Nombre visible | IstpetDev — esquema dev_01 | IstpetDev — datos dev_01 |
| Host | Endpoint RDS real de dev-access.json, mapeado localmente | El mismo |
| Puerto | 15432 | 15432 |
| Database | istpetdev_dev_01 | istpetdev_dev_01 |
| Username | migrator_dev_01 | user_dev_01 |
| Password | Guardada o secreto personal de migrador | Secreto personal de aplicación |
| SSL Mode | verify-full | verify-full |
| CA Certificate | ~/.config/istpetdev/certs/global-bundle.pem, expandido por selector | El mismo |

## Clic por clic

1. Conexión existente → clic derecho → Edit Connection → General: asignar el nombre de esquema de la tabla anterior.
2. Connection settings → Main: Host con endpoint real, port 15432 y database `istpetdev_dev_01`. Conservar username/password del migrador guardados.
3. Botón `+ SSH, SSL, …` → SSL; abrir pestaña SSL, habilitar SSL si la versión muestra esa casilla, seleccionar verify-full y elegir el archivo CA. Mantener vacíos certificado/clave de cliente; no omitir validación de hostname.
4. Test Connection → OK. Si aparece connection refused, verificar túnel/puerto; si aparece certificado inválido, revisar hostname/CA; si password authentication failed, verificar que contraseña y usuario correspondan al mismo secreto.
5. Para el usuario de datos: menú Database → New Database Connection → PostgreSQL → Next. Usar los campos de la segunda columna y la misma configuración SSL.
6. Obtener la contraseña: consola AWS → región us-east-1 → Secrets Manager → Secrets → `/istpetdev/dev/db/dev_01` → Retrieve secret value → copiar `password` directamente al campo Password de DBeaver. Guardarla allí según política local; no pegarla en la bóveda ni en conversaciones.
7. General: nombre de datos → Test Connection → Finish. Cada conexión conserva su password propio; la contraseña guardada del migrador no es la del usuario de datos.
8. En el navegador DB: Databases → istpetdev_dev_01 → Schemas → public → Tables; clic derecho → Refresh para ver tablas creadas por migraciones.
9. Para SQL de inspección: clic derecho en la conexión deseada → SQL Editor → New SQL Script. Verificar en la barra del editor conexión y base antes de ejecutar. No reutilizar scripts MySQL con `USE`, `DATABASE()` o columnas `ENGINE`.

Los nombres y controles SSL están documentados por [DBeaver](https://dbeaver.com/docs/dbeaver/SSL-Configuration/). Las etiquetas pueden variar con el idioma/versión; SSL también se puede configurar mediante propiedades del driver.

## Verificar identidad y capacidad

Ejecutar por separado en cada conexión:

```sql
SELECT current_database() AS database_name,
       current_user AS database_user,
       has_schema_privilege(current_user, 'public', 'CREATE') AS can_create_tables;

SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
ORDER BY table_name;
```

Se espera CREATE permitido al migrador y denegado al usuario de datos. No prueba todo el aislamiento. Cuando se implemente RLS, la conexión API sin contexto de organización/cuenta debe devolver cero filas o denegar; no «solucionarlo» desactivando RLS. Para trabajar con datos tenant desde DBeaver se debe usar una transacción y el contexto documentado por la migración, con una cuenta/organización sintética autorizada. El contexto manual de un desarrollador con password DB no equivale a autenticación de usuario final.

La configuración del proyecto gobierna cómo cambiar datos; no editar saldos, concesiones o eventos append-only desde un editor genérico. RLS y grants no sustituyen los comandos transaccionales de inventario o la administración auditada de RBAC. Las tablas se crean desde EF Core, permanecen versionadas y aparecen después de Refresh.
