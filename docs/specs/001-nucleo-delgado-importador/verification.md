# Verificación de 001

## Alcance autorizado — 12-oct-2026

Bryan pidió «Implementar y verificar el spec» y confirmó que incluye sustituir
Schema95 por el núcleo de 27 tablas. Se implementa localmente en
`feature/001-nucleo-delgado-importador`; se conservan las migraciones aplicadas.
La instrucción no autoriza modificar RDS, publicar ni desplegar.

## Prelaunch previo a los hosts de prueba

Skill aplicada: `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md`.
Referencias: verificación de seguridad, esquema, RBAC, validación y reglas
backend/dbflow/revisión de la bóveda. Entorno: localhost y PostGIS descartable,
datos sintéticos; JWT de prueba exclusivo del proyecto de tests.

| Control | Estado inicial | Trabajo previsto |
|---|---|---|
| Aislamiento | Excepción de demo autorizada; una organización, sin RLS | Probar filtros/actor de servidor y FKs; conservar historial Schema95 |
| Identidad y roles | Pendiente | JWT válido/ajeno, cuenta revocada, claim falsificado, scopes |
| Campos protegidos | Pendiente | Perfil cerrado; actor/organización derivados en servidor |
| Archivos y consultas | Pendiente | Límites, tipos XLSX/CSV, perfiles inválidos, SQL parametrizado |
| Datos clasificados/plantillas | No aplica al nuevo núcleo | Sin formularios ni datos personales de terceros en los fixtures |
| Dependencias | Pendiente | Scan NuGet y dependencias efectivas |
| Proxy/imagen/Cognito/RDS | Pendiente | No acreditar con pruebas locales ni modificar esos entornos |

El entorno local aislado se inicia para implementar y verificar estos controles.
Los pendientes no constituyen una declaración de demo expuesta lista.

## Resultados de verificación (12-oct-2026)

### 1. Pruebas automatizadas .NET (Release)
- **Compilación**: `dotnet build IstpetDev.sln -c Release` (0 errores, 0 advertencias).
- **Unitarias (`Category=Unit`)**: 66 pasadas, 0 fallidas, 0 omitidas en 1 s. Artifact: `artifacts/spec001/Unit.trx`.
- **Integración (`Category=Integration`)**: 48 pasadas, 0 fallidas, 0 omitidas en 1 m 46 s con contenedor PostGIS descartable (`postgis/postgis:16-3.4-alpine`). Artifact: `artifacts/spec001/Integration.trx`.
- **E2E (`Category=E2E`)**: 1 pasada (`DatasetJourneyTests`), 0 fallidas, 0 omitidas. Artifact: `artifacts/spec001/E2E.trx`.
- **Total ejecutado y aprobado**: **115/115 pruebas aprobadas (100%)**, verificado con `scripts/db/check-test-results.py`.

### 2. Calidad de código y builds de la plataforma (`npm run check`)
- `check:lint`: ESLint sin errores en web y apps.
- `check:api`: Compilación .NET sin errores ni advertencias.
- `check:shared`: Compilación TypeScript `@istpetdev/shared-core` sin errores.
- `check:web`: Build de producción Angular Web completado (bundle inicial total: 152.15 kB transfer).
- `check:mobile`: Build de producción Angular Mobile completado (bundle inicial total: 152.32 kB transfer).

### 3. Integridad documental de la bóveda
- `check_vault.py`: 74 notas Markdown, 615 enlaces internos resueltos, 0 errores, 3 avisos de lenguaje preexistentes.

### 4. Integridad de migraciones e historial
- `has-pending-model-changes`: Sin cambios pendientes en el modelo EF Core respecto a `DemoCore`.
- **Hashes SHA-256 de Schema95**: 7 archivos históricos cotejados byte a byte contra `artifacts/spec001/historical-hashes.json` (7/7 coincidentes).
- **Tag local de respaldo**: `schema95-archivo` en commit base `06f29111a0835adbc222f23b982ffd6337db0cf0`.

### 5. Imagen Docker
- `docker build -f infra/docker/backend/Dockerfile -t istpetdev-backend:spec001 .`: Construcción exitosa.
- **Usuario de ejecución**: verificado como no root (`uid=1654(app)`).
- **Fixtures integrados**: 8 archivos de `demo-base` y 4 archivos de `ejemplo-externo` leídos con éxito.

### 6. Pruebas y entornos pendientes (requieren ambiente autorizado)
- **E2E-03**: Medición humana de importación guiada con dataset ajeno.
- **E2E-04**: Validación de tokens reales contra AWS Cognito (no simulados con clave RSA efímera).
- **E2E-05**: Ensayo expuesto contra instancia real de AWS RDS / host de despliegue.
