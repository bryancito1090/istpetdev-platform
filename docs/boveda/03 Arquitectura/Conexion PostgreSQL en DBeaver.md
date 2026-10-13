---
tipo: guia-operativa
estado: vigente
actualizado: 2026-10-12
tags: [postgresql, dbeaver, desarrollo, acceso]
---

# Conexión PostgreSQL en DBeaver

> [!important] Entorno consolidado — 13 de octubre de 2026
> La conexión principal para el equipo de desarrollo se realiza directamente al **Servidor del Laboratorio (LAN)** o al **Docker Local (Casa)**. Se archiva el uso rutinario de túneles SSM hacia bases personales en Amazon RDS para desarrollo diario ([[Estrategia de tres entornos y base de datos]]).

Guía operativa para conectar DBeaver a la base de datos de desarrollo `istpetdev_dev_01` (27 tablas de `DemoCore`). El esquema objetivo es [[Esquema completo de base de datos]]; el flujo de migraciones sigue [[istpetdev-dbflow]].

## 1. Conexión de Desarrollo (Laboratorio y Casa)

Para los 5 integrantes del equipo, los parámetros de conexión son estandarizados:

| Parámetro | Opción A: Laboratorio (LAN) | Opción B: Casa (Docker Local) |
|---|---|---|
| **Host** | `192.168.7.50` | `localhost` (o `127.0.0.1`) |
| **Port** | `15432` | `15432` |
| **Database** | `istpetdev_dev_01` | `istpetdev_dev_01` |
| **Username** | `migrator_dev_01` (o `user_dev_01`) | `migrator_dev_01` (o `user_dev_01`) |
| **Password** | `12345` | `12345` |
| **SSL Mode** | disabled / allow (sin certificados) | disabled / allow |

### Configuración paso a paso en DBeaver

1. Menú superior: **Database** → **New Database Connection** → seleccionar **PostgreSQL** → **Next**.
2. Pestaña **Main**:
   - **Host:** `192.168.7.50` (en el laboratorio) o `localhost` (en casa).
   - **Port:** `15432`.
   - **Database:** `istpetdev_dev_01`.
   - **Username:** `migrator_dev_01`.
   - **Password:** `12345`.
3. Pestaña **SSL**: Deshabilitada o por defecto (no se requiere certificado CA para la LAN del laboratorio o contenedor local).
4. Hacer clic en **Test Connection**: Debe mostrar `Connected` exitosamente.
5. Hacer clic en **Finish**.
6. En el árbol de navegación: `Databases` → `istpetdev_dev_01` → `Schemas` → `public` → `Tables`. Clic derecho → **Refresh** para visualizar las 27 tablas de `DemoCore`.

## 2. Acceso a Amazon RDS en Producción (Solo Administración)

Amazon RDS (`istpetdev-rds-dev`) aloja la base de integración/producción `istpetdev_integration` y está restringida a subredes privadas en AWS. Solo se accede administrativamente mediante túnel SSM para auditar el despliegue en la nube:

| Parámetro | Conexión RDS Producción (Vía Túnel SSM) |
|---|---|
| **Host** | Mapeo local del endpoint RDS a `127.0.0.1` |
| **Puerto** | `15432` (túnel SSM activo) |
| **Database** | `istpetdev_integration` |
| **Username** | `migrator_integration` |
| **Password** | Valor obtenido desde AWS Secrets Manager |
| **SSL Mode** | `verify-full` con CA bundle oficial de RDS |

> [!note] Histórico archivado
> Las bases personales previas (`istpetdev_dev_01` a `05`) en RDS fueron sustituidas por el Servidor de Laboratorio para suprimir la latencia y la dependencia de credenciales IAM/MFA durante el hackathon.

## 3. Verificar identidad y capacidad en DBeaver

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
