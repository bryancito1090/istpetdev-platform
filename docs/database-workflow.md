# Flujo de Trabajo Colaborativo en Base de Datos (`istpet-dbflow`)

> **Complemento del 12-oct-2026.** Las referencias a Schema95 y RLS describen el estado previo. Con el spec 001 (ADR-21) las bases se vacían una vez con `scripts/db/reset-to-demo-core.sql`, con respaldo previo, y pasan al núcleo `DemoCore` sin RLS. Los roles `user_*` y `migrator_*`, el túnel SSM y `VerifyFull` siguen igual. Procedimiento en `docs/boveda/03 Arquitectura/Runbook de DevOps y base de datos para la demo.md`.

Guía operativa y estándares de arquitectura y desarrollo colaborativo para la base de datos **PostgreSQL 16 + PostGIS 3.4** alojada en Amazon RDS para el proyecto **IstpetDev** (Reto 1 — Expo Clean 2026).

---

## 1. ¿Por qué trabajamos con Bases de Datos Aisladas y Migraciones en Código?

En el desarrollo profesional de software en equipo, usar una **única base de datos compartida en desarrollo es un antipatrón** que genera caos:
- Si un compañero altera una columna a medio terminar, **rompe inmediatamente la API de los otros 4 integrantes**.
- Si alguien borra datos para probar o revierte una migración, **deja sin datos o sin tablas a todo el equipo**.
- Si alguien bloquea una tabla en una transacción o prueba índices pesados, **congela el trabajo de todos**.

### La Solución: Isolated Development Databases + Migrations as Code
Adoptamos el estándar de la industria:
1. **Tu base personal (`dev_XX`) es como tu rama local de Git (`feature/*`)**: Tu propio "sandbox" o patio de recreo. Puedes crear datos de prueba, experimentar y aplicar cambios sin riesgo de afectar a tus compañeros.
2. **Las migraciones de Entity Framework Core son como los commits de la base de datos**: Toda modificación de esquema queda guardada en código C# reproducible (`src/Infrastructure/Persistence/Migrations/`), versionada en Git.
3. **La base `istpetdev_integration` es como la rama `develop`**: Refleja el estado unificado y validado del sistema. Solo se actualiza cuando un PR/MR ha sido revisado y fusionado en `develop`.

---

## 2. Asignación de Integrantes y Recursos

Amazon RDS (`istpetdev-rds-dev`) corre en subredes privadas. Cada miembro tiene su propia base de datos física, sus credenciales aisladas en AWS Secrets Manager y permisos de acceso controlados por IAM con MFA.

| Integrante | Código | Usuario IAM | Base Personal | Rol App (DML) | Rol Migrador (DDL) |
|---|---|---|---|---|---|
| Bryan | `dev_01` | `istpetdev-bryan` | `istpetdev_dev_01` | `user_dev_01` | `migrator_dev_01` |
| Alexander | `dev_02` | `istpetdev-alexander` | `istpetdev_dev_02` | `user_dev_02` | `migrator_dev_02` |
| Andrés | `dev_03` | `istpetdev-andres` | `istpetdev_dev_03` | `user_dev_03` | `migrator_dev_03` |
| Anthony | `dev_04` | `istpetdev-anthony` | `istpetdev_dev_04` | `user_dev_04` | `migrator_dev_04` |
| Jorge | `dev_05` | `istpetdev-jorge` | `istpetdev_dev_05` | `user_dev_05` | `migrator_dev_05` |
| **Común** | `integration` | *(Todos)* | `istpetdev_integration` | `user_integ_dev_XX` | `migrator_integration` |

### Separación Estricta de Privilegios (Mínimo Privilegio)
- **Rol de Aplicación (`user_dev_XX`)**: Solo tiene permisos de lectura y escritura de datos (`SELECT, INSERT, UPDATE, DELETE`). **Tiene revocado el permiso DDL en `public`**, impidiendo que la aplicación modifique accidentalmente la estructura de tablas en ejecución.
- **Rol Migrador (`migrator_dev_XX`)**: Tiene permisos DDL (`CREATE, USAGE` en `public`). Solo se usa puntualmente para ejecutar `dotnet ef database update`.

---

## 3. Preparación Local Rápida (Requisitos Previos)

1. **Resolución DNS para TLS VerifyFull** (solo una vez en tu máquina):
   ```bash
   echo "127.0.0.1  istpetdev-rds-dev.cep4osawemm3.us-east-1.rds.amazonaws.com" | sudo tee -a /etc/hosts
   ```
2. **Iniciar sesión en AWS con MFA**:
   ```bash
   aws login --profile istpetdev-dev-console --region us-east-1
   ```
3. **Abrir el Túnel Seguro SSM** (mantener esta terminal abierta):
   ```bash
   bash scripts/ops/dev-tunnel.sh --profile istpetdev-dev --config ~/.config/istpetdev/private/dev-access.json
   ```
4. **Cargar credenciales a `dotnet user-secrets`**:
   ```bash
   # Carga la conexión de tu base personal dev_XX:
   bash scripts/ops/load-dev-secrets.sh --profile istpetdev-dev --member dev_XX --database personal --config ~/.config/istpetdev/private/dev-access.json
   ```

---

## 4. Ciclo de Vida de una Migración (Paso a Paso)

```
[1. Modificar Entidad C#] 
       │
[2. Registrar en DbContext]
       │
[3. dotnet ef migrations add ...]
       │
[4. Cargar rol migrador (--migrator)]
       │
[5. dotnet ef database update]
       │
[6. Probar API localmente (health/ready = 200)]
       │
[7. Commit & Push a rama feature/*]
       │
[8. Pull Request aprobado y fusionado en develop]
       │
[9. Aplicar migración en istpetdev_integration]
       │
[10. Equipo hace git pull y actualiza su base dev_XX]
```

### Paso 1: Diseñar o modificar entidades en C#
- Ubicación: `src/Domain/Entities/`.
- Usar tipos canónicos acordados en la documentación del reto:
  - Identificadores: UUIDv7.
  - Fechas: `DateTime` siempre en UTC (`DateTime.UtcNow`).
  - Cantidades monetarias o físicas: `decimal` con precisión y escala explícitas.
  - Nombres: Nombres canónicos en inglés (ej. `RoutePlan`, `HandlingUnit`, `ServicePoint`, no inventar sinónimos ni usar español).

### Paso 2: Registrar en el DbContext
- En `src/Infrastructure/Persistence/ApplicationDbContext.cs`, agrega el `DbSet<TuEntidad>` y configura índices y relaciones fluidas en `OnModelCreating`.

### Paso 3: Generar la migración de EF Core
```bash
dotnet ef migrations add <NombreDescriptivoEnPascalCase> \
  --project src/Infrastructure \
  --startup-project src/Api
```
Esto creará un archivo con timestamp en `src/Infrastructure/Persistence/Migrations/` y actualizará automáticamente `ApplicationDbContextModelSnapshot.cs`.

### Paso 4: Cargar temporalmente el rol migrador
El rol de aplicación normal no tiene permisos DDL. Carga el rol migrador en tu configuración local:
```bash
bash scripts/ops/load-dev-secrets.sh --profile istpetdev-dev --member dev_XX --database personal --migrator --config ~/.config/istpetdev/private/dev-access.json
```

### Paso 5: Aplicar la migración a tu base de datos personal
```bash
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Api
```

### Paso 6: Restaurar el rol de aplicación y probar la API
Restaura el usuario normal de aplicación:
```bash
bash scripts/ops/load-dev-secrets.sh --profile istpetdev-dev --member dev_XX --database personal --config ~/.config/istpetdev/private/dev-access.json
```
Inicia la API con `npm run dev:api` y verifica que `http://localhost:5000/health/ready` responda con código `HTTP 200 OK`.

### Paso 7: Subir cambios a Git
Sigue la convención de `istpet-gitflow`:
```bash
git add src/Domain/ src/Infrastructure/Persistence/
git commit -m "feat(db): agregar entidad RoutePlan y configuracion de persistencia"
git push origin feature/<id>-<slug>
```

### Paso 8: Integración a `develop` y base compartida
Una vez que el Pull Request es aprobado y fusionado en `develop`:
1. Se aplica la migración sobre la base de integración `istpetdev_integration`:
   ```bash
   bash scripts/ops/load-dev-secrets.sh --profile istpetdev-admin --member dev_01 --database integration --migrator --config ~/.config/istpetdev/private/dev-access.json
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   ```
2. **Tus compañeros sincronizan sus entornos**:
   ```bash
   git switch develop
   git pull origin develop

   # Aplicar a su propia base personal:
   bash scripts/ops/load-dev-secrets.sh --profile istpetdev-dev --member dev_XX --database personal --migrator --config ~/.config/istpetdev/private/dev-access.json
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   bash scripts/ops/load-dev-secrets.sh --profile istpetdev-dev --member dev_XX --database personal --config ~/.config/istpetdev/private/dev-access.json
   ```

---

## 5. Prevención y Resolución de Conflictos

### ¿Qué pasa si dos personas crean tablas o modifican el modelo a la vez?
1. **Regla de división de tareas**: Cada integrante tiene tareas claras asignadas en el backlog de la hackathon. Dos desarrolladores no deberían estar creando la misma entidad en paralelo.
2. **Nombres canónicos preestablecidos**: La documentación en `docs/boveda/` (`00 Inicio/Hechos canonicos.md` y `04 Datos y algoritmos/Modelo de datos.md`) define de antemano el nombre y campos de cada tabla. No se inventan nombres sobre la marcha.

### Conflicto de Snapshot en Git (`ApplicationDbContextModelSnapshot.cs`)
Si tú y un compañero crearon migraciones distintas al mismo tiempo en ramas separadas, Git mostrará un conflicto al hacer rebase o merge en el archivo `ApplicationDbContextModelSnapshot.cs`.

> [!CAUTION]
> **NUNCA intentes resolver el snapshot a mano editando el código C# del archivo.** El snapshot es autogenerado por EF Core y editarlo manualmente causará inconsistencias graves en el historial de migraciones.

#### Procedimiento Correcto de Resolución:
1. Cancela el merge temporalmente o inicia el rebase:
   ```bash
   git rebase develop
   ```
2. Si hay conflicto en la migración o el snapshot, revierte tu migración local aún no mergeada:
   ```bash
   dotnet ef migrations remove --project src/Infrastructure --startup-project src/Api
   ```
3. Termina de resolver los conflictos de tus archivos de dominio/código de negocio (si los hubiera) y completa el rebase:
   ```bash
   git add src/Domain/
   git rebase --continue
   ```
4. Ahora que tu rama tiene el código actualizado de `develop`, vuelve a generar tu migración:
   ```bash
   dotnet ef migrations add <TuMigracion> --project src/Infrastructure --startup-project src/Api
   ```
   *EF Core leerá el snapshot actualizado de develop y creará una migración limpia que encaja perfectamente encima.*
5. Aplica la migración a tu base personal con `dotnet ef database update`.
6. Haz push a tu rama (o push forzado seguro con `--force-with-lease`).

---

## 6. Preguntas Frecuentes y Diagnóstico

### ¿Por qué obtengo `permission denied for schema public` al hacer `dotnet ef database update`?
Porque tienes cargado en `dotnet user-secrets` el usuario de aplicación (`user_dev_XX`), el cual solo tiene permisos DML.
- **Solución**: Ejecuta `load-dev-secrets.sh` con el flag `--migrator` antes de correr el comando de EF Core.

### ¿Por qué obtengo `The remote certificate is invalid because of a name mismatch`?
Porque Npgsql está intentando validar el certificado TLS oficial de Amazon RDS, pero tu conexión se dirige a `127.0.0.1` o `localhost`.
- **Solución**: Verifica que hayas agregado la línea correspondiente a tu `/etc/hosts`:
  ```bash
  127.0.0.1 istpetdev-rds-dev.cep4osawemm3.us-east-1.rds.amazonaws.com
  ```

### ¿Puedo conectarme con un cliente gráfico como DBeaver o pgAdmin?
Sí. Mientras el túnel SSM esté abierto en tu terminal, puedes conectar tu cliente gráfico a:
- **Host**: `istpetdev-rds-dev.cep4osawemm3.us-east-1.rds.amazonaws.com` (o `127.0.0.1`)
- **Port**: `15432`
- **Database**: `istpetdev_dev_XX`
- **Username / Password**: Las credenciales obtenidas de AWS Secrets Manager para tu usuario.
- **SSL Mode**: `require` o `verify-full` con el certificado `global-bundle.pem` descargado en `~/.config/istpetdev/private/rds-ca.pem`.
- **Regla de oro**: El cliente gráfico es **solo para consulta e inspección**. Los cambios de estructura se hacen siempre por EF Core.
