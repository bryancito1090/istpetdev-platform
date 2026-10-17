# Issue #287 — concurrencia y conciliación

Base: `origin/develop` (`f8f85e9`). Rama: `chore/287-concurrencia-conciliacion`.
Entorno previsto: pruebas locales con PostgreSQL/PostGIS desechable, sin datos reales ni despliegue.

## Estado

- [x] Lectura de #287, auditoría #286/#294, bóveda, contratos y reglas Git.
- [x] Directorio de trabajo independiente; no se incluyen modificaciones de #290/#293.
- [ ] Contratos persistentes e implementación de concurrencia.
- [ ] Conciliación parcial y autorización por conductor.
- [ ] Evidencia PostgreSQL y regresión de carga comparada con la base.
- [ ] Verificaciones de repositorio y CI del HEAD final.
- [ ] Coordinación con Joseph: **pendiente**. Bryan indicó «Preparar cambios; falta coordinar con Joseph».
- [ ] Merge a develop y cierre del issue: pendientes de coordinación y criterios completos.

Código, pruebas locales, ejecución real y despliegue se acreditan por separado.

## Prelaunch local previo a pruebas

Se aplicó la skill local `istpetdev-prelaunch` del checkout principal (la copia generada no se versiona en el worktree), contrastada con su fuente editable y referencias de seguridad, esquema, RBAC y validación.

| Control | Estado previo / alcance |
|---|---|
| Entorno | Solo TestServer y PostgreSQL/PostGIS Testcontainers desechable, fixtures sintéticos; Docker 29.8.1 y SDK .NET 8.0.424 disponibles |
| Base/esquema | DemoCore y migraciones existentes; no se acredita RLS/multiempresa P2 ni se modifica RDS |
| Identidad/asignación | Implementación modificada; prueba HTTP con identidades distintas pendiente |
| Campos/stock/idempotencia | Validación y serialización implementadas; pruebas PostgreSQL pendientes |
| SQL | Se reutiliza LockOrganizationAsync parametrizado; no se añaden consultas con valores interpolados sin parámetros |
| Archivos/cifrado/plantillas | No se modifican estos caminos; no se acredita verificación nueva |
| Proxy/cabeceras/TLS remoto | No aplica a TestServer local; no se declara demo expuesta ni release listo |
| Dependencias | Sin paquetes nuevos ni cambios de lockfiles; auditoría de esta revisión pendiente |

No se levanta la demo compartida ni se ejecutan operaciones cloud. Los controles pendientes se verifican en el entorno aislado antes de declarar completo el código.
