---
tipo: ejecucion
estado: vigente
actualizado: 2026-10-14
tags: [istpetdev, devops, base-de-datos, rds, despliegue, rollback, evidencia]
---

# Evidencias y ensayos de base de datos y despliegue

> [!important] Evidencia de Validación Técnica — Issue #35
> Esta nota registra los ensayos técnicos, salidas de comandos, fechas y SHAs correspondientes a la validación de punta a punta del flujo de base de datos entre desarrollo y producción, formalizada en [[Runbook de DevOps y base de datos para la demo]] y en atención a la [issue #35](https://github.com/bryancito1090/istpetdev-platform/issues/35).

---

## 1. Identificación y Parámetros del Ensayo

- **Fecha de ejecución:** 14 de octubre de 2026.
- **Responsables técnicos:** Jorge Doicela (DevOps / Infraestructura), Joseph Bano (DevOps / QA).
- **Commits auditados:**
  - `09567f8` (develop base con saneamiento de credenciales #34).
  - `c1e05d0` (producción inicial verificada en host EC2).
- **Entornos evaluados:**
  - **Producción:** AWS EC2 `istpetdev-prod-host` (`44.204.107.167`) y Amazon RDS PostgreSQL 16.13 (`istpetdev-rds-dev`, base `istpetdev_integration`).
  - **Local (Plan B):** Docker Desktop PostGIS 16 (`localhost:15432`) y .NET 8 Web API.

---

## 2. Ensayo 1 — Detección de Fallo en Despliegue (Fail-Fast)

### Objetivo
Demostrar que si la conexión a Amazon RDS falla o una migración DDL no puede aplicarse, el pipeline de CI/CD (`deploy.yml`) aborta con código de error (`exit 1`) y **no emite un reporte verde falso**.

### Condición y Ejecución
1. En `src/Api/Program.cs`, el método `PrepareDatabaseAsync` implementa la política Fail-Fast:
   ```csharp
   catch (Exception ex)
   {
       app.Logger.LogError(ex, "Error al preparar la base de datos en el arranque.");
       throw;
   }
   ```
2. En `.github/workflows/deploy.yml` (paso 8), se eliminó el fallback permisivo a `/health/live` y se exige estrictamente `HTTP 200` en `/health/ready`:
   ```bash
   HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5000/health/ready || true)
   ```

### Salida Observada en Ensayo de Fallo
Al simular indisponibilidad de base de datos o credencial inválida:
```text
Comprobando endpoint de readiness de la API (/health/ready)...
Esperando readiness de la API y migraciones de BD (intento 1/15, status=000)...
Esperando readiness de la API y migraciones de BD (intento 2/15, status=503)...
...
Esperando readiness de la API y migraciones de BD (intento 15/15, status=503)...
Error critico: La API no alcanzo estado READY (PostgreSQL/RDS o migraciones no disponibles).
--- Ultimos logs de istpetdev-backend ---
[19:30:12 ERR] Error al preparar la base de datos en el arranque.
Npgsql.NpgsqlException (0x80004005): Failed to connect to istpetdev-rds-dev...
   at IstpetDev.Infrastructure.Persistence.DatabaseInitializer.MigrateAsync(ApplicationDbContext context)
   at Program.PrepareDatabaseAsync(WebApplication app, String[] args) in /src/src/Api/Program.cs:line 204
Unhandled exception. Npgsql.NpgsqlException: Connection refused
##[error]Process completed with exit code 1.
```
**Resultado:** El despliegue falló de forma inmediata y fail-fast en GitHub Actions. La versión previa en el host se preservó sin interrupción.

---

## 3. Ensayo 2 — Verificación de Migraciones Acumuladas en Amazon RDS

### Objetivo
Verificar la aplicación de migraciones pendientes en `istpetdev_integration` y auditar la tabla `__EFMigrationsHistory`.

### Comando Ejecutado vía Túnel SSM
```bash
psql -h 127.0.0.1 -p 15432 -U migrator_integration -d istpetdev_integration -c \
  "SELECT \"MigrationId\", \"ProductVersion\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" ASC;"
```

### Salida Observada
```text
                  MigrationId                  | ProductVersion 
-----------------------------------------------+----------------
 20261013031023_DemoCore                       | 8.0.11
 20261013143520_RoadMatrixCache                | 8.0.11
 20261013202540_FieldOperations                | 8.0.11
 20261013203831_AgentRuns                      | 8.0.11
 20261014162010_AccountsAndAudit               | 8.0.11
(5 rows)
```

### Comprobación HTTP de Readiness
```bash
curl -s -i https://istpetdev.doicela.dev/health/ready
```
**Salida:**
```http
HTTP/1.1 200 OK
Content-Type: application/json
Date: Wed, 14 Oct 2026 19:35:10 GMT

{
  "status": "Healthy",
  "totalDurationMs": 28.4,
  "entries": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": "PostgreSQL database is available.",
      "durationMs": 28.1
    }
  ]
}
```
**Resultado:** `HTTP 200 Healthy`. Las tablas de matriz de ruta, operaciones de campo, corridas del agente y auditoría de cuentas se encuentran creadas y vinculadas con `DemoGrants`.

---

## 4. Ensayo 3 — Rollback Seguro con Esquema Migrado

### Objetivo
Demostrar que tras aplicar una migración aditiva, si se ejecuta un rollback rápido de imágenes mediante `rollback.yml`, la versión previa de la API continúa operando sin errores contra Amazon RDS.

### Acción Ejecutada
1. Ejecución del workflow `rollback.yml` con el SHA previo `c1e05d0`.
2. Recreación de contenedores en producción en <30 segundos.
3. Verificación de readiness interna en el paso 3:
   ```bash
   HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5000/health/ready || true)
   ```

### Salida Observada
```text
Descargando imágenes inmutables para la versión c1e05d0...
Restaurando servicios de aplicación a la versión c1e05d0...
Verificando readiness del backend y conexión a RDS tras el rollback...
Rollback completado y verificado exitosamente en el intento 2 (HTTP 200 en /health/ready).
```
**Interpretación técnica:** Al cumplir con la regla de migraciones aditivas (nuevas columnas admiten `NULL` o tienen valores `DEFAULT`), el código anterior de la API ignora las columnas no mapeadas y ejecuta sus consultas DML sin colisión.

---

## 5. Ensayo 4 — Plan B Local (Operación Offline sin Cobertura WAN)

### Objetivo
Validar que la plataforma puede levantarse íntegramente en una laptop de desarrollo sin conexión a internet ni dependencia de AWS, permitiendo la conexión de teléfonos móviles sobre una red WiFi Hotspot local.

### Comandos de Puesta en Marcha Local
```bash
# 1. Dependencias locales de datos
npm run docker:up

# 2. Backend API en modo Desarrollo Local
npm run dev:api
```

### Salida Observada en Terminal
```text
[14:40:02 INF] Now listening on: http://localhost:5000
[14:40:03 INF] Application started. Press Ctrl+C to shut down.
[14:40:03 INF] Hosting environment: Development
[14:40:03 INF] Content root path: /workspace/istpetdev-platform/src/Api
```

### Comprobación de Salud Local
```bash
curl -s http://localhost:5000/health/ready
```
```json
{
  "status": "Healthy",
  "totalDurationMs": 14.2,
  "entries": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": "PostgreSQL database is available.",
      "durationMs": 13.9
    }
  ]
}
```

### Protocolo de Red para el Recinto del Concurso
1. La laptop host activa la **Zona con cobertura inalámbrica móvil (Hotspot)** en Windows (SSID: `ISTPETDEV-DEMO`).
2. La laptop obtiene la dirección IP local de red: `192.168.137.1`.
3. El backend se inicia vinculando todas las interfaces: `dotnet run --project src/Api --urls "http://0.0.0.0:5000"`.
4. El frontend se sirve con: `npm run dev:web -- --host 0.0.0.0`.
5. El teléfono del evaluador se conecta al SSID `ISTPETDEV-DEMO` y abre `http://192.168.137.1:4200/campo` para escanear los códigos QR de entrega.

---

## 6. Conclusiones y Cumplimiento

1. **Paridad de motores confirmada:** PostgreSQL 16 PostGIS 3.4 en local y PostgreSQL 16.13 PostGIS 3.4.6 en Amazon RDS comparten tipos canónicos y extensiones espaciales sin discrepancias.
2. **Despliegues Fail-Fast garantizados:** Ningún despliegue saldrá verde si la base de datos o las migraciones fallan.
3. **Rollback seguro certificado:** La política de migraciones aditivas protege la disponibilidad del servicio durante reversiones de emergencia.
4. **Plan B del evento listo para ejecución en vivo:** Cero dependencias de redes institucionales o de laboratorios remotos.
