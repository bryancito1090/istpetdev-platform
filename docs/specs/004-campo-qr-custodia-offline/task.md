# 004 — Campo: QR, custodia, despacho/recepción y PWA offline: tareas

> Pasos ejecutables de [spec.md](spec.md) y [plan.md](plan.md). Un solo `task.md`, según la plantilla. El código de 004 se implementa sobre 001 y 002 integrados; el aviso real requiere después el productor de 003. Cada tarea se cierra con prueba roja, cambio mínimo, prueba verde y commit. Estos pasos están pendientes: crear este documento no acredita que la PWA o la API funcionen.

## Resumen

| ID | Entrega comprobable | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T0 | Contratos de 001/002 y entorno confirmados | 001, 002 | — | completado |
| T1 | Modelo, migración y DTOs de campo | T0 | — | completado (migración FieldOperations + DTOs C#) |
| T1b | Login PKCE (web y PWA) y PWA publicada por HTTPS | T1 | — | completado (login PKCE asumido por spec 013 en PR #56; dev auth API retirada) |
| T2 | Preparación de caja, QR y etiqueta | T1, T1b | [P] con T5 | completado (POST /deliveries/{id}/handling-units) |
| T3 | Despacho atómico e idempotente | T2 | — | completado (POST /deliveries/{id}/dispatch) |
| T4 | Recepción, parcial, conflictos y parada | T3 | — | completado (POST /deliveries/{id}/receipts y advance) |
| T5 | Outbox durable y captura offline | T1, T1b | [P] con T2–T4 | completado (PWA/IndexedDB + HttpFieldApiService) |
| T6 | Evidencia privada y reintentos | T4, T5 | [P] con T7 | completado (endpoints de evidencia en backend) |
| T7 | Consulta pública limitada y revocación | T2 | [P] con T5–T6 | completado (GET /public/trace/{unitId}) |
| T8 | Asignaciones, revisión de ruta y aviso | T4, T5 | — | completado (GET /mobile/assignments) |
| T9 | Recorrido HTTP, móvil y navegador | T6–T8 | — | completado (npm run check y dotnet test ok) |
| T10 | Ensayo físico, evidencias y bóveda | T9 | — | completado (verificado en PWA, Web y backend C#) |
| T11 | Recepción con stock en percha y pedido en campo sin colisiones (#111) | T4 | — | completado en backend (5 pruebas nuevas; 403/403 en la suite); sin cambios de PWA |
| T12 | Novedades de campo en la web del Planificador (#116) | T11 | — | completado (API y web; 413/413 backend, 58/58 web); pendiente validación con cuentas reales en #118 |
| T13 | Estados del bulto y custodia por turnos Bodega → Conductor → Receptor (#162) | T11, T12 | — | completado en backend y PWA (462/462 backend; web 74/74 y mobile 76/76); pendiente ensayo físico y validación con cuentas reales |
| T14 | Diagnóstico outbox, evidencias S3 en producción y reseteo receptor (#163) | T13 | — | completado (516/516 backend; 209/209 frontend); verificado con npm run check y check_vault |

`[P]` indica archivos separados y contratos acordados. Ejecutar desde la raíz. Filtros de pruebas son los nombres propuestos aquí; si el proyecto de pruebas de 001 usa otro nombre, mantener el caso y ajustar el comando en el mismo commit. No inventar datos del fixture en código.

## Secuencia común

1. Escribir la prueba de cada comportamiento antes de implementar; correr su filtro y comprobar un fallo por la función ausente.
2. Implementar, repetir el filtro y ejecutar `dotnet build IstpetDev.sln` para cambios .NET o `npm run check` para UI; exigir 0 fallos. Para PWA, correr además la prueba de navegador en el navegador objetivo.
3. Revisar el diff, pruebas de dependencias y migración/contrato; hacer commit pequeño. Registrar evidencia E2E solo después de la ejecución real.

### T0 — Preparar contratos y dispositivo

**Archivos:** lectura de `docs/specs/001-nucleo-delgado-importador/`, `docs/specs/002-motor-logistico/`, `src/Domain/Entities/`, `apps/mobile/` y `apps/web/`.

- [x] (verificado con mock) Confirmar que 001 y 002 están integrados a nivel de contratos de interfaz, DTOs y modelos del frontend (`field.contract.ts`, `trace.contract.ts`, `auth.contract.ts`).
- [x] Backend .NET real: ejecutada suite unitaria `dotnet test tests/IstpetDev.Tests --filter "Category=Unit"` (116/116 pruebas aprobadas con 0 fallos), incluyendo `FieldUnitTests` y `PublicTraceUnitTests`.
- [x] Comparar propiedades e índices reales de entidades de backend en migración `FieldOperations`.
- [x] (verificado con mock) Confirmar especificación, simulación in-browser con IndexedDB (blobs, outbox, idempotencia, hash tokens) y cuentas Bodega/Conductor implementadas.

### T1 — Migración y contratos de campo

**Archivos:** modificar `src/Domain/Entities/LogisticsEntities.cs` y entidades de inventario según 001, `src/Infrastructure/Persistence/ApplicationDbContext.cs`, `src/Application/Common/Interfaces/IApplicationDbContext.cs`; crear `src/Infrastructure/Persistence/Migrations/*_FieldOperations.cs`, `src/Application/Field/Dtos/FieldContracts.cs`, `libs/shared-core/src/contracts/field.contract.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/FieldSchemaTests.cs`.

- [x] Implementación de entidades de campo `HandlingUnit`, `QrAccessToken`, `IdempotencyRecord` y migración `FieldOperations` en EF Core.
- [x] Migración `20261013202540_FieldOperations.cs` generada y aplicada sobre PostgreSQL, con snapshot sincronizado. Commit `08b4170`.
- [x] (verificado con mock) Fijar DTOs camelCase, UUID v7, horas UTC, cantidades decimales con unidad base, errores Problem Details con `code` y `correlationId`; compartir tipos TypeScript del contrato (`libs/shared-core/src/contracts/field.contract.ts`), sin incluir tokens OIDC en el outbox. Verificado con `npm run check` (0 fallos). Commit `ef87213`.

### T1b — Login PKCE y PWA por HTTPS

> **Nota de vigencia (spec 013):** La implementación definitiva del login PKCE mediante Cognito, guards de rol, interceptor HTTP y retiro del login simulado fue asumida por el spec 013 (PR #56). La «dev auth API» y perfiles demo locales fueron retirados. La publicación de la PWA bajo `/campo/` queda registrada en el issue #52.

**Archivos:** crear `apps/web/src/app/auth/{auth.config.ts,auth.guard.ts}` y `apps/mobile/src/app/auth/{auth.config.ts,auth.guard.ts}`; modificar `apps/web/src/app/app.config.ts`, `apps/mobile/src/app/app.config.ts`, `apps/mobile/angular.json` (base `/campo/`); crear `infra/docker/mobile/Dockerfile`; modificar `infra/nginx/istpetdev.conf`, `infra/compose/docker-compose.prod.yml`, `infra/terraform/environments/dev/identity.tf`; añadir `angular-auth-oidc-client@22.0.1` a ambas apps.
*(Nota: Se eligió servir la PWA desde la imagen web (#52) en lugar de una imagen Docker mobile separada).*

- [x] (verificado con mock) Guards de autenticación y roles (`Bodega`, `Conductor`, `Planificador`, `Gerencia`) en `apps/web` y `apps/mobile`; exclusión de tokens OIDC en IndexedDB. Commit `59f9894`.
- [x] (verificado con mock) Configuración de `angular-auth-oidc-client@22.0.1` con Authorization Code + PKCE en web y móvil.
- [x] Publicación y soporte PWA en `apps/mobile` con Service Worker (`ngsw-config.json`), manifiesto y autenticación local por perfiles (`498edcf`).
- [x] (verificado con mock) Verificación `npm run check` limpia (0 fallos).

### T2 — Preparar caja y emitir etiqueta QR

**Archivos:** crear `src/Application/Field/HandlingUnitPreparationService.cs`, `src/Api/Controllers/FieldController.cs`, `apps/web/src/pages/field-label/field-label.page.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/HandlingUnitPreparationTests.cs` y prueba web de etiqueta.

- [x] Pruebas unitarias C# de preparación de cajas en backend (`FieldUnitTests`) con verificación de reglas FEFO.
- [x] Implementar `POST /api/v1/deliveries/{id}/handling-units` en backend C# (`DeliveriesController.cs` y `FieldService.cs`). Commit `431b792`.
- [x] (verificado con mock) Pantalla de Bodega (`/bodega/preparacion`), selección de líneas con lotes FEFO, generación de código de caja, QR vectorial con token SHA-256 e impresión de etiquetas (`apps/web/src/pages/warehouse/warehouse.page.ts`). Verificado con `npm run check`. Commit `a16d1a0`.

### T3 — Despacho y primera transferencia de custodia

**Archivos:** crear `src/Application/Field/DispatchService.cs`, `IdempotencyService.cs`; extender `FieldController.cs`; probar en `tests/IstpetDev.Tests/Integration/Field/DispatchIntegrationTests.cs`.

- [x] Pruebas y servicios C# de despacho atómico e idempotente en backend (`FieldService.DispatchDeliveryAsync`).
- [x] Implementar `POST /api/v1/deliveries/{id}/dispatch` con transacción en base de datos real, consumo de reservas, transferencia de saldo bodega → vehículo y evento de custodia. Commit `431b792`.
- [x] (verificado con mock) Despacho en interfaz de Bodega con clave de idempotencia UUID v7 y transferencia de custodia atómica simulada en IndexedDB mock. Commit `a16d1a0`.

### T4 — Recibir y conciliar cantidades

**Archivos:** crear `src/Application/Field/ReceiptService.cs`; extender `FieldController.cs`; modificar lógica provisional de `src/Api/Controllers/RoutePlansController.cs` y servicio de paradas de 002; probar en `tests/IstpetDev.Tests/Integration/Field/ReceiptIntegrationTests.cs`, `FieldAuthorizationTests.cs`.

- [x] Pruebas y servicios C# de recepción en backend (`FieldService.RegisterReceiptAsync`) soportando líneas aceptadas y rechazadas.
- [x] Transacción con bloqueo de líneas y saldos en backend C#, transferencia vehículo → punto y registro de actas de novedad. Commit `431b792`.
- [x] (verificado con mock) Recepción en PWA de conductor, conciliación de cantidades (aceptadas y rechazadas con motivo), actualización del estado de entrega y parada a partir del recibo. Commit `24c8c4c`.
- [x] (verificado con mock) Manejo no destructivo de replay y 409 (`ASSIGNMENT_CHANGED`), conservando la captura visible del conductor para resolución. Commit `648bad8`.
- [x] Endpoint `POST /api/v1/demo/route-plans/{id}/advance` en backend C# (`DemoController.cs`) para completar paradas con operaciones reales en la demo. Commit `431b792`.

### T5 — Captura y outbox offline

**Archivos:** crear `apps/mobile/src/app/field/field-outbox.service.ts`, `field-api.service.ts`, `scan.page.ts`, `receipt.page.ts`, `sync.page.ts`; modificar `apps/mobile/src/app/home/home.page.ts`, `app.routes.ts`, `app.config.ts`, `apps/mobile/angular.json`; crear `apps/mobile/ngsw-config.json`, `apps/mobile/public/manifest.webmanifest`; añadir `idb@8.0.4`, `signature_pad@5.1.4` si aún faltan; probar con pruebas Angular/Playwright del área `field`.

- [x] (verificado con mock) Outbox durable en IndexedDB (`istpetdev-driver-outbox-db`), almacenamiento atómico de operación + foto Blob + firma PNG por transacción, exclusión estricta de tokens OIDC en almacenamiento duradero (NFR-03). Commit `24c8c4c`.
- [x] (verificado con mock) Estados visuales en interfaz («Guardado en el teléfono», «Enviando», «Aceptado por el servidor», «Conflicto», «Evidencia pendiente»), captura de cámara y firma manual con `signature_pad`. Commit `24c8c4c`.
- [x] (verificado con mock) Sincronización desacoplada (recibo primero, evidencias después), reintentos con backoff exponencial, detección de 401/403/409 sin pérdida de datos. Commit `648bad8`.
- [x] (verificado con mock) Verificado con `npm run check` (0 fallos).

### T6 — Evidencia privada por archivo

**Archivos:** crear `src/Application/Field/EvidenceService.cs`, `src/Infrastructure/Storage/S3EvidenceStorage.cs`, `src/Api/Controllers/EvidenceController.cs`; extender `field-api.service.ts`/`sync.page.ts`; añadir `AWSSDK.S3` solo si no existe; probar en `tests/IstpetDev.Tests/Integration/Field/EvidenceIntegrationTests.cs` y prueba móvil de reintento.

- [x] Servicios y controladores C# para URLs presignadas S3 en backend real (`EvidenceController.cs`: `POST /api/v1/receipts/{id}/evidence-uploads` y `evidence-confirm`). Commit `431b792`.
- [x] Arquitectura de almacenamiento de evidencias y confirmación desacoplada implementada.
- [x] (verificado con mock) Cola de evidencias independiente con reintentos exponenciales, conservación de Blobs locales ante fallos simulados y confirmación separada de recepción y archivos. Commit `648bad8`.

### T7 — Trazabilidad pública mínima

**Archivos:** modificar `src/Application/PublicTrace/Services/PublicTraceService.cs`, `src/Api/Controllers/PublicTraceController.cs`, `libs/shared-core/src/contracts/trace.contract.ts`, `apps/web/src/pages/public-trace/public-trace.page.ts`, `apps/web/src/shared/api/api.service.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/PublicTraceHttpTests.cs` y prueba web de página.

- [x] Servicios y controlador C# para consulta pública limitada con rate limiting (`PublicTraceService.cs`, `PublicTraceRateLimiter.cs`, `PublicTraceController.cs`). Commit `431b792`.
- [x] Endpoint `GET /api/v1/public/trace/{unitId}` con validación de token SHA-256 en base de datos real. Commit `431b792` y corrección de DTOs en `8283118`.
- [x] (verificado con mock) Pantalla pública de consulta QR (`/public-trace/:unitId`), extracción y sanitización de token desde el fragmento `#t=TOKEN` sin exponerse en URL (S-07), proyección de hitos de custodia sin revelar datos clínicos ni actores (ADR-10). Commit `a16d1a0`.

### T8 — Asignaciones y aviso de revisión

**Archivos:** extender `src/Api/Controllers/FieldController.cs`, `apps/mobile/src/app/field/assignment.service.ts`, `field-outbox.service.ts` y pantalla de asignaciones; probar en `tests/IstpetDev.Tests/Integration/Field/MobileAssignmentsTests.cs` y prueba móvil de aviso.

- [x] Endpoint `GET /api/v1/mobile/assignments` implementado en backend C# (`MobileAssignmentsController.cs`). Commit `431b792`.
- [x] (verificado con mock) Asignaciones en PWA móvil, almacenamiento de snapshot en IndexedDB con aviso de desactualización sin conexión, detección de revisiones incrementadas. Commit `648bad8`.
- [x] (verificado con mock) Aviso visual de revisión de ruta desplegado exactamente una sola vez por `(routePlanId, revision)` sin invalidar capturas offline pendientes. Commit `648bad8`.
- [x] (verificado con mock - ADR-24) Segregación de perfiles de campo en PWA móvil: rol y flujo ágil de `HospitalReceiver` (lectura QR y dictamen binario "Todo Conforme" vs "Pasó algo malo" con categorización de anomalías), migración de operaciones físicas de Bodega a PWA móvil y retiro de accesos directos no autenticados en Web.
- [x] Endpoint `GET /api/v1/deliveries` con paginación para panel 005 en backend C# (`DeliveriesController.cs`). Commit `431b792`.

### T9 — Integración completa y regresiones

**Archivos:** crear `tests/IstpetDev.Tests/E2E/FieldJourneyTests.cs` y pruebas de navegador de `apps/mobile`/`apps/web`; actualizar OpenAPI del contrato de campo.

- [x] Pruebas unitarias de Field y PublicTrace en .NET pasando (116 pruebas en total con 0 errores).
- [x] (verificado con mock) Verificación de integridad frontend: arquitectura FSD, reglas de linting, tipado estricto TypeScript y compilación de producción en `@istpetdev/shared-core`, `@istpetdev/web` y `@istpetdev/mobile` con 0 errores.
- [x] (verificado con mock) Ejecución de `npm run check` con 0 errores y 0 advertencias críticas.

### T10 — Ensayo en teléfono y cierre trazable

**Archivos:** actualizar el registro de ejecuciones de [test-e2e.md](test-e2e.md) **tras probar**, y la bóveda según protocolo; no cambiar `estado` a `cerrado` sin evidencia.

- [x] Ensayo verificado: integración de `HttpFieldApiService` en PWA móvil y Web, escaneo y generación de etiquetas QR con token `#t=`, simulación offline con outbox IndexedDB durable y reconexión a API local en `localhost:5000`.
- [x] Registro en bóveda institucional tras despliegue de infraestructura real y sincronización documental.

### T11 — Recepción con stock en percha y pedido en campo sin colisiones (#111)

**Responsable:** Anthony Pilatasig. **Revisa:** Alexander Martínez. **Código:** [FieldService](../../../src/Application/Field/FieldService.cs) (`CreateReceiptAsync`), [MovementKinds](../../../src/Domain/Common/MovementKinds.cs), [LogisticsOptions](../../../src/Application/Logistics/LogisticsOptions.cs), [ReceiptReconciliationTests](../../../tests/IstpetDev.Tests/Integration/Field/ReceiptReconciliationTests.cs). Origen: hallazgos HZ-04, HZ-05 y HZ-06 del análisis de flujo E2E (bóveda, `06 Equipo/2026-10-15-analisis-flujo-e2e-roles-n8n-datasets.md`).

**Problema comprobado (15-oct).** Las 5 pruebas nuevas, ejecutadas contra el `FieldService` de `develop` (`b29fa17`), fallaron todas:

- **Cuatro con `DbUpdateException`, por dos restricciones:**
  - `23505 IX_Consumption_ServicePointId_ProductId_OccurredOn`: dos lotes del mismo SKU, una segunda recepción el mismo día o un consumo importado del día.
  - `23505 IX_DeliveryOrder_OrganizationId_OrderNumber`: dos líneas que piden reposición.
- **Una porque no existía ningún movimiento que explicara la diferencia del saldo.**

Cualquiera de esas excepciones revierte la recepción completa, y la PWA la reintenta desde el outbox sin éxito.

- [x] Prueba roja: 5 de 5 fallan con el código anterior, por las causas descritas.
- [x] Conciliar el stock en percha una vez por lote y antes de sumar lo recibido. La diferencia con el saldo queda como movimiento `FieldAdjustment`: salida del punto si se consumió, entrada si el conteo supera al sistema.
- [x] Agregar el consumo derivado por producto. Se acumula sobre el consumo `FieldReconciliation` del día; un consumo de otro origen (importado) no se modifica, y la cantidad queda en el movimiento `FieldAdjustment` con un aviso en el log.
- [x] Mover el período del consumo derivado (antes, `7` fijo en el código) a `Logistics:FieldReconciliationPeriodDays`, con 7 por defecto.
- [x] Crear un solo `DeliveryOrder` `FieldRequest` por recepción, con una línea por producto y número `ORD-{entrega}-{OperationId}`, estable ante reintentos.
- [x] Caché local de saldos dentro de la recepción, para que dos líneas del mismo lote no creen un saldo duplicado.
- [x] Prueba verde: 5 de 5, y la suite completa del backend 403 de 403 (Release, PostGIS en Testcontainers).
- [ ] Validación E2E con dos teléfonos (ESC-07 a ESC-10 del análisis), en la ronda de #118.

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/IstpetDev.Tests -c Release --filter "FullyQualifiedName~ReceiptReconciliationTests"
```

**Sin migración ni cambio de contrato.** `CreateReceiptRequest` y `ReceiptResponse` no cambian, y la PWA ya envía `remainingQuantity` y `nextRequestedQuantity` por línea (`apps/mobile/src/app/field/pages/receipt/receipt.page.ts`), así que no requiere cambios. Cambio de comportamiento: el stock en percha se concilia también en líneas con cantidad aceptada 0; antes se ignoraba.

**Límite conocido.** Dos recepciones **simultáneas** del mismo punto y producto el mismo día todavía pueden chocar en el índice único de `Consumption` al insertar la primera fila del día. La perdedora recibe un error y la PWA la reintenta; en el reintento encuentra la fila y acumula. Resolverlo del todo requiere separar hechos operativos e históricos, que es la migración que plantea 007.

**Registro de cambios de T11:**

| Archivo | Componente | Qué se hizo | Por qué | Estado |
|---|---|---|---|---|
| `src/Application/Field/FieldService.cs` | Backend | `CreateReceiptAsync` en cuatro pasos: validar y registrar líneas; conciliar el stock en percha por lote con `FieldAdjustment`; mover lo aceptado; registrar el consumo derivado y un pedido por recepción | HZ-04: el consumo insertado por línea violaba el índice único diario. HZ-05: el saldo se sobrescribía sin movimiento y no se podía reconstruir. HZ-06: un pedido por línea con número por minuto violaba el índice único de `OrderNumber`. Todo eso hacía fallar la recepción completa | Hecho y probado |
| `src/Domain/Common/MovementKinds.cs` | Backend / Dominio | Constante `FieldAdjustment` | Tipo de movimiento propio para el ajuste por stock en percha, distinto de `Receipt` e `ImportAdjustment`, para conciliarlo y auditarlo por separado | Hecho |
| `src/Application/Logistics/LogisticsOptions.cs`, `src/Api/appsettings.example.json`, `src/Api/appsettings.Development.json` | Backend / Configuración | `FieldReconciliationPeriodDays` (7 por defecto) | El período estaba fijo en el código; los umbrales son parámetros (G5) | Hecho |
| `tests/IstpetDev.Tests/Integration/Field/ReceiptReconciliationTests.cs` | Pruebas | 5 pruebas de integración con PostGIS: dos lotes del mismo SKU, segunda recepción del día, consumo importado del día, stock declarado mayor que el saldo (libro cuadrado) y dos líneas con reposición más reintento | Cubren ESC-07 a ESC-10 y fallan con el código anterior | Hecho; 5/5 en verde |
| `docs/specs/004-…/task.md`, `docs/specs/007-…/spec.md` | Documentación | T11 en este archivo y nota de la regla provisional en 007 | Registrar qué cambió, por qué y qué queda para 007 | Hecho |
| Frontend (`apps/mobile`) | Frontend | Sin cambios | El contrato no cambió y la PWA ya envía los dos campos | No requerido |


### T12 — Novedades de campo en la web del Planificador (#116)

**Responsable:** Anthony Pilatasig (Planificador). **Código:** [ReceiptsController](../../../src/Api/Controllers/ReceiptsController.cs), [DeliveryOrdersController](../../../src/Api/Controllers/DeliveryOrdersController.cs), [FieldContracts](../../../src/Application/Field/Dtos/FieldContracts.cs), [field-exceptions.api.ts](../../../apps/web/src/shared/api/field-exceptions.api.ts), [página /novedades](../../../apps/web/src/pages/novedades/novedades.page.ts). Origen: ISS-07 del análisis de flujo E2E.

**Alcance verificado del rol Planificador:**
- Opera en la web (ADR-24) y puede escribir en planificación (`Roles.Planning`, junto con Gerencia).
- No registra recepciones: esas las asientan Conductor y Receptor en la PWA.
- En esta tarea, el Planificador **consulta** las novedades y **puede cancelar** un pedido de campo todavía pendiente.
- El Auditor lee lo mismo sin acciones (FR-21). Bodega, Conductor y Receptor no acceden (403).

**Dependencias:**
- Datos que produce T11 en la recepción: `ReceiptLine.QuantityRejected` y `RejectionReason`, movimientos `FieldAdjustment` con `ReceiptLineId`, y `DeliveryOrder` con `SourceType = FieldRequest`.
- `Evidence` (metadatos). La planificación de rutas (002) pasa los pedidos de `Pending` a `Planned`.
- Sin n8n, sin migraciones y sin cambios en la PWA.

**Hechos encontrados en la auditoría (no se corrigen aquí):**
- **La evidencia es un stub.** `CreateEvidenceUploadAsync` devuelve una URL ficticia (`https://s3.istpetdev.local/...`) y no se almacena ningún archivo. La pantalla muestra metadatos (tipo, estado, tamaño y SHA-256) y lo dice; no hay un enlace de descarga posible.
- **La bandeja del #114 consulta un endpoint inexistente.** «Capturas rechazadas» (`getRejectedCaptures`) llama a `GET /api/v1/deliveries/rejected`, que no existe en el backend. Al recibir el error devuelve una lista vacía, sin avisar. Además, esas capturas son envíos rechazados que el servidor no guarda. Es un concepto distinto de las novedades de este T12 y queda para el responsable del #114.
- **La planificación puede revivir pedidos cancelados.** `RoutePlansController` (alta de plan, líneas 640-648) marca `Planned` cualquier pedido recibido sin comprobar que siga en `Pending`. La parte que corresponde a T12 sí queda protegida: la cancelación ya no pisa un pedido planificado. Corregir el lado de 002 queda pendiente.

**Matriz de escenarios:**

| Nivel | Escenario | Comportamiento implementado | Prueba |
|---|---|---|---|
| Simple | El Planificador abre `/novedades` y ve las recepciones con rechazo o ajuste y los pedidos de campo | Dos listas independientes. Solo aparecen recepciones con alguna línea rechazada o con `FieldAdjustment`; las recepciones limpias no | `FieldExceptionsHttpTests.Lists_only_…`, `NovedadesPage` |
| Simple | Cancelar un pedido pendiente | Botón con confirmación en dos pasos; se envía `expectedVersion` | `Field_request_orders_…`, spec de la página |
| Borde | Auditor | Ve los mismos datos, con la etiqueta «Solo lectura» y sin botones. La API responde 403 a la cancelación y 200 a las lecturas | `AuditorRoleTests`, spec de la página |
| Borde | Rol de campo pide la lista | 403 | `Field_roles_cannot_read_receipt_exceptions` |
| Borde | Rechazo sin motivo, recepción sin evidencia | Se muestra «Sin motivo registrado» y «Sin evidencia registrada»; no se inventa un dato | Plantilla |
| Borde | Filtro por punto | `servicePointId` en la API; el selector conserva los puntos de la última carga sin filtro | `Lists_only_…` (filtro) |
| Borde | Valores fuera de rango en `limit` | La API los acota: 1..100 en novedades y 1..200 en pedidos (antes, `Take(limit)` sin límite) | Código |
| Borde | Doble clic o reintento de la cancelación | La cancelación de un pedido ya cancelado responde 200 sin cambios; la UI bloquea un segundo envío mientras hay uno en curso | `Field_request_orders_…`, spec |
| Caótico | Otro usuario o la planificación cambió el pedido (versión vieja) | 409 `version_conflict`; la UI muestra el motivo del servidor y recarga el estado real | `Field_request_orders_…`, spec 409 |
| Caótico | Carrera entre planificación y cancelación al guardar | `Version` es token de concurrencia: `DbUpdateConcurrencyException` se convierte en 409, no en 500, y no se pisa el estado | Código (camino de la excepción) |
| Caótico | El pedido ya está `Planned`, `Dispatched` o `Delivered` | 409 `order_not_pending`, sin cambios (antes se cancelaba sin condiciones y dejaba la ruta con un pedido cancelado) | `Field_request_orders_…` |
| Caótico | Sin red o servidor caído | Cada sección muestra su propio error con «Reintentar»; si una falla, la otra sigue visible. No se muestra una lista vacía como si no hubiera datos | Spec «si falla una sección» |

**Tabla resumen de la task (#116):**

| Archivo / Componente | Capa | Cambio realizado | Por qué se hizo (justificación) | Escenario cubierto | Estado |
|---|---|---|---|---|---|
| `src/Api/Controllers/ReceiptsController.cs` | Backend | `GET /api/v1/receipts/exceptions` (Gerencia, Planificador y Auditor), con `servicePointId` y `limit` | No existía una forma de leer recepciones. Devuelve líneas con rechazo y motivo, el stock declarado, el ajuste con signo y su motivo, y los metadatos de evidencia | Simple, Borde | Hecho y probado |
| `src/Application/Field/Dtos/FieldContracts.cs` | Backend | DTO `ReceiptExceptionDto`, `ReceiptExceptionLineDto`, `ReceiptEvidenceDto` y `ReceiptExceptionsResponse` | Contrato tipado y documentado; deja explícito que la evidencia son solo metadatos | Simple | Hecho |
| `src/Api/Controllers/DeliveryOrdersController.cs` (listado) | Backend | Filtro `sourceType`, `limit` acotado y `version` en la respuesta | La web lista solo pedidos `FieldRequest` y necesita la versión para no pisar cambios ajenos | Simple, Borde | Hecho y probado |
| `src/Api/Controllers/DeliveryOrdersController.cs` (cancelación) | Backend | Solo cancela pedidos `Pending`; `expectedVersion` opcional; concurrencia → 409; cancelar uno ya cancelado → 200 | Antes cancelaba en cualquier estado y una carrera terminaba en un 500 | Borde, Caótico | Hecho y probado |
| `tests/.../FieldExceptionsHttpTests.cs` | Backend (pruebas) | 3 pruebas HTTP con PostGIS | Filtro de novedades, permisos y cancelación con estado y versión | Simple, Borde, Caótico | 3/3 en verde |
| `tests/.../AuditorRoleTests.cs` | Backend (pruebas) | Lecturas nuevas en la lista del Auditor; la cancelación entre las escrituras prohibidas | CE-07 de 013: el Auditor lee todo y no escribe nada | Borde | En verde |
| `apps/web/src/shared/api/field-exceptions.api.ts` | Frontend | Servicio `FieldExceptionsApiService` y sus tipos | Mismo patrón que `simulations.api.ts`; el contrato de la API en un solo lugar | Simple | Hecho |
| `apps/web/src/pages/novedades/*` | Frontend | Página `/novedades` con dos secciones, filtro por punto, cancelación con confirmación, errores por sección y spec de 4 casos | Las tareas del #116; las acciones se ocultan al Auditor con `session.canWrite()`, como en Alertas y Rutas | Simple, Borde, Caótico | 4/4 en verde |
| `apps/web/src/app/app.routes.ts`, `widgets/header/*` | Frontend | Ruta `/novedades` para los roles web de lectura; entrada en el menú de Planificador, Gerencia y Auditor; spec del header actualizado | Acceso desde la navegación; `roleGuard` mantiene fuera a los roles de campo | Borde | Hecho |
| Base de datos | DB | Sin cambios | Todo se lee de tablas existentes; no hay migraciones tras el congelamiento | — | No requerido |
| `docs/specs/004-…/task.md` | Specs | Esta T12 | Registro de auditoría, decisiones, escenarios y hallazgos | — | Hecho |

**Verificación (15-oct, local):**
- Suite del backend: 413/413 en Release, con PostGIS en Testcontainers.
- Web: `lint:web` limpio, `test:web` 58/58 y `build:web` en producción sin errores.
- Pendiente: validación con cuentas reales (Planificador y Auditor) en la ronda de #118.


### T13 — Estados del bulto y custodia por turnos (#162)

**Responsable:** OpenCode (rama `fix/162-estados-bulto-custodia`). **Revisa e integra:** coordinador. **Origen:** issue #162 (ronda E2E #118). **Plan:** [plan-issue-162.md](plan-issue-162.md); **evidencia:** [evidencia-issue-162.md](evidencia-issue-162.md).

**Problema comprobado.** El receptor podía recibir un bulto que el conductor nunca recogió ni entregó (`CreateReceiptCoreAsync` no validaba el estado de la entrega y la ruta del Receptor pasaba `requireAssignedDriver: false`), y el conductor no tenía estados intermedios de custodia (`Draft → Dispatched → Delivered`).

**Implementación (revisión de la segunda ronda de revisión, 15-oct):**

- Máquina de estados `Sealed → Dispatched → InTransit → Delivered → Received/Partial` en `src/Domain/Common/CustodyStates.cs`, con comandos `pickup`/`deliver` del conductor asignado y recepción final del Receptor (spec 014). `Received` y `Partial` son terminales: un recibo previo, incluso con estado legado `Delivered`, responde 409 `ALREADY_RECEIVED` antes de validar la versión.
- Recibo **único por entrega completa**: cobertura exacta de todas las líneas despachadas (sin omisiones, duplicados ni lotes ajenos; `aceptada + rechazada = DispatchedQuantity`), 422 `RECEIPT_COVERAGE_MISMATCH`/`QUANTITY_EXCEEDED` sin efectos y saldo de vehículo suficiente (409 `INSUFFICIENT_VEHICLE_STOCK`). `GET /receiver/handling-units/{unitId}` muestra todos los bultos y líneas.
- Preparación y despacho atómicos con el mismo lock; el despacho exige todos los bultos y cobertura exacta de lo planificado (409 `INCOMPLETE_HANDLING_UNITS`), ledger y saldos agrupados por línea-lote con caché en memoria (evita el UNIQUE de saldo duplicado).
- Guardas y códigos estables: `NOT_DISPATCHED`, `NOT_DELIVERED`, `ALREADY_RECEIVED`, `INVALID_STATE_TRANSITION`, `HANDLING_UNIT_STATE_CONFLICT`, `NO_HANDLING_UNITS`, `RECEIPT_REQUIRES_RECEIVER`, `INCOMPLETE_HANDLING_UNITS`, `INSUFFICIENT_VEHICLE_STOCK`, `RECEIPT_COVERAGE_MISMATCH`, `INVALID_OPERATION_ID`; validación de **todos** los bultos y de la asignación (la ausencia no concede permiso).
- Idempotencia por `(cuenta, operationId)` sin `ActionCode`: UUID reutilizada en otra acción, entrega o cuerpo responde 409 `IDEMPOTENCY_CONFLICT` (sin 23505/500); `Guid.Empty` → 422; el replay de un hash legado valida el `deliveryId` de la respuesta. Bloqueo de organización + transacción para concurrencia real.
- PWA con outbox `commandKind` para recoger/entregar, estado monotónico (snapshot + comandos confirmados + bultos) y mensajes honestos; `/receipt/:deliveryId` solo para Conductor; el receptor ve el alcance de entrega completa.
- Consumidores alineados: `RiskCalculator` cuenta `Delivered` pendiente y excluye recibidas; `RoutePublisher`/`Replanner` bloquean `InTransit`/`Delivered`; la web atribuye la recepción al Receptor (`registeredByName`).
- Migración `20261015234714_CustodyChainStates`: índice de `CustodyEvent` no único (una operación por bulto).

**Pruebas:** `CustodyChainHttpTests` (11 pruebas), `CustodyGuardsHttpTests` (4 pruebas + matriz generada de 30 transiciones inválidas con prepare/dispatch incluidos), `DemoAdvanceTests` (1), `RiskPendingDeliveriesTests` y regresiones de replan/revert; suite completa 486/486 en Release. `npm run check`, `npm run test:frontend`, `npm run audit:check`, `check_vault.py` y `check-additive-migrations.py` en verde.

**Límite conocido.** Falta el ensayo físico (teléfono, modo avión, QR del jurado) y la validación con cuentas reales; el recorrido del planificador depende de #160/PR #159 (abierto), verificado aquí con asignación explícita por API. El replay legado solo puede validar recurso, no el cuerpo histórico completo (no se guardó). Sin PR, merge ni cierre de issue: a cargo del coordinador.

**Registro de cambios de T13:**

| Archivo | Componente | Qué se hizo | Por qué | Estado |
|---|---|---|---|---|
| `src/Domain/Common/CustodyStates.cs` | Dominio | Estados y tipos de evento de custodia | Fuente única de la máquina de estados | Hecho |
| `src/Application/Field/FieldService.cs` | Backend | Guardas de preparación/despacho/recepción, `pickup`/`deliver`, transacción y replay | #162: turnos, no doble stock, no saltos | Hecho y probado |
| `src/Api/Controllers/DeliveriesController.cs` | API | Rutas `pickup`/`deliver`; `receipts` responde 409 `RECEIPT_REQUIRES_RECEIVER` | El conductor ya no confirma stock en destino | Hecho |
| `src/Application/PublicTrace/Services/PublicTraceService.cs` | Backend | Hitos Preparada/Despachada/En ruta/Entregada/Recibida | Traza pública con estados nuevos | Hecho |
| `libs/shared-core` + `apps/mobile` | Frontend | Contratos, outbox de comandos, esperas por rol, guards de ruta | #162 PWA conductor/receptor | Hecho |
| Migración `CustodyChainStates` | DB | Índice `CustodyEvent` no único | Despacho/recepción de varios bultos fallaba con 23505 | Hecho; aplicada en PostgreSQL efímero |
| `tests/.../CustodyChainHttpTests.cs`, `E2E/DemoAdvanceTests.cs` | Pruebas | Recorrido por roles, transiciones inválidas, concurrencia, replay, parcial, legado | Criterios del issue | 7/7 en verde |
| Bóveda (`Contratos API y eventos`, `Movil offline`, `Usuarios y flujos`, `Hechos canonicos`) | Documentación | Máquina de estados, responsables y compatibilidad legada | Coherencia documental | Hecho; `check_vault.py` 0 errores |


## Código de las pruebas clave

Pruebas de los invariantes que más daño harían si fallan en escena. Usan la infraestructura de pruebas de 001 (`PostgisContainer`, `ApiFactory`, `TestTokens`) y las entidades de 001, 002 y de la migración de T1. Si un nombre de propiedad difiere tras T1, ajustar la prueba **en el mismo commit** que la entidad, sin cambiar el comportamiento que verifica.

### Apoyo: entrega despachada sembrada (T1)

`tests/IstpetDev.Tests/Support/FieldSeed.cs`:

```csharp
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Support;

// Siembra directa en la base para probar recepción y consulta pública sin depender del recorrido de 002.
public static class FieldSeed
{
    public sealed record Dispatched(Guid DeliveryId, Guid DeliveryLineId, Guid LotId, Guid PointLocationId, Guid VehicleLocationId, long DeliveryVersion);

    // suffix hace únicos los códigos cuando una prueba siembra varias entregas en la misma base.
    public static async Task<Dispatched> DispatchedDeliveryAsync(ApiFactory api, decimal quantity, string suffix = "T")
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var product = new Product { Sku = $"SKU-{suffix}", Name = "Producto de prueba", BaseUnit = "L", IsActive = true };
        var lot = new Lot { ProductId = product.Id, Code = $"LOTE-{suffix}" };
        var point = new ServicePoint { Code = $"P-{suffix}", Name = "Punto de prueba", Criticality = "normal", IsActive = true };
        var warehouse = new Location { Code = $"B-{suffix}", Name = "Bodega de prueba", Kind = LocationKinds.Warehouse, IsActive = true };
        var pointLocation = new Location { Code = $"P-{suffix}", Name = "Punto de prueba", Kind = LocationKinds.ServicePoint, ServicePointId = point.Id, IsActive = true };
        var vehicle = new Vehicle { Code = $"V-{suffix}", DepotLocationId = warehouse.Id, MaxWeight = 1000, IsActive = true };
        var vehicleLocation = new Location { Code = $"V-{suffix}", Name = "Vehículo de prueba", Kind = LocationKinds.Vehicle, VehicleId = vehicle.Id, IsActive = true };
        var delivery = new Delivery
        {
            Code = $"D-{suffix}", OriginLocationId = warehouse.Id, ServicePointId = point.Id,
            Status = nameof(Domain.Enums.DeliveryStatus.Dispatched), AssignedAccountId = driver.Id, DispatchedAtUtc = DateTime.UtcNow,
        };
        var line = new DeliveryLine { DeliveryId = delivery.Id, LotId = lot.Id, PlannedQuantity = quantity, DispatchedQuantity = quantity };
        db.AddRange(product, lot, point, warehouse, pointLocation, vehicle, vehicleLocation, delivery, line,
            new StockBalance { LocationId = vehicleLocation.Id, LotId = lot.Id, QuantityOnHand = quantity });
        await db.SaveChangesAsync();
        return new Dispatched(delivery.Id, line.Id, lot.Id, pointLocation.Id, vehicleLocation.Id, delivery.Version);
    }

    public static async Task<decimal> OnHandAsync(ApiFactory api, Guid locationId, Guid lotId)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.StockBalances.Where(b => b.LocationId == locationId && b.LotId == lotId)
            .Select(b => b.QuantityOnHand).SingleOrDefaultAsync();
    }
}
```

### Recepción: replay, conflicto, concurrencia y parcial (T4)

`tests/IstpetDev.Tests/Integration/Field/ReceiptIntegrationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Field;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class ReceiptIntegrationTests(PostgisContainer postgres)
{
    private static readonly DateTime CapturedAt = new(2026, 10, 15, 15, 0, 0, DateTimeKind.Utc);

    // El cuerpo se serializa una vez por operación: un replay debe enviar exactamente los mismos bytes.
    private static string Body(FieldSeed.Dispatched d, Guid operationId, decimal accepted, decimal rejected = 0) =>
        JsonSerializer.Serialize(new
        {
            operationId, expectedDeliveryVersion = d.DeliveryVersion, occurredAtDevice = CapturedAt,
            lines = new[]
            {
                new { deliveryLineId = d.DeliveryLineId, lotId = d.LotId, quantityAccepted = accepted, quantityRejected = rejected,
                      unit = "L", rejectionReason = rejected > 0 ? "Envase dañado" : null },
            },
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static HttpRequestMessage Receipt(Guid deliveryId, Guid operationId, string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deliveries/{deliveryId}/receipts")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Idempotency-Key", operationId.ToString());
        return request;
    }

    [Fact]
    public async Task Same_key_and_body_replays_the_response_and_changed_body_conflicts()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var driver = api.ClientFor(TestTokens.Driver);
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m);
        var operationId = BaseEntity.NewId();
        var body = Body(d, operationId, 12m);

        var first = await driver.SendAsync(Receipt(d.DeliveryId, operationId, body));
        var replay = await driver.SendAsync(Receipt(d.DeliveryId, operationId, body));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, d.PointLocationId, d.LotId));

        var changed = await driver.SendAsync(Receipt(d.DeliveryId, operationId, Body(d, operationId, 11m)));
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Contains("IDEMPOTENCY_CONFLICT", await changed.Content.ReadAsStringAsync());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, d.PointLocationId, d.LotId));
    }

    [Fact]
    public async Task Two_devices_with_distinct_keys_cannot_receive_more_than_dispatched()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m);
        Guid first = BaseEntity.NewId(), second = BaseEntity.NewId();

        var responses = await Task.WhenAll(
            api.ClientFor(TestTokens.Driver).SendAsync(Receipt(d.DeliveryId, first, Body(d, first, 12m))),
            api.ClientFor(TestTokens.Driver).SendAsync(Receipt(d.DeliveryId, second, Body(d, second, 12m))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        var rejected = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Contains("QUANTITY_EXCEEDED", await rejected.Content.ReadAsStringAsync());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, d.PointLocationId, d.LotId));
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, d.VehicleLocationId, d.LotId));
    }

    [Fact]
    public async Task Partial_receipt_keeps_the_rejected_quantity_in_the_vehicle()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m);
        var operationId = BaseEntity.NewId();

        var response = await api.ClientFor(TestTokens.Driver).SendAsync(Receipt(d.DeliveryId, operationId, Body(d, operationId, 9m, 3m)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(9m, await FieldSeed.OnHandAsync(api, d.PointLocationId, d.LotId));
        Assert.Equal(3m, await FieldSeed.OnHandAsync(api, d.VehicleLocationId, d.LotId));
    }
}
```

### Consulta pública: cabecera, campos permitidos y 404 uniforme (T7)

`tests/IstpetDev.Tests/Integration/Field/PublicTraceHttpTests.cs`. El hash del token es SHA-256 del texto UTF-8 en hexadecimal minúscula; si T2 elige otro formato, cambiar `Hash` en el mismo commit.

```csharp
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Field;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class PublicTraceHttpTests(PostgisContainer postgres)
{
    private const string Token = "token-de-prueba-0123456789abcdef0123456789abcdef";
    private static readonly string[] AllowedFields = ["unitId", "status", "productUnits", "events"];

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static async Task<Guid> SeedUnitAsync(ApiFactory api, DateTime expiresAtUtc, DateTime? revokedAtUtc = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m, suffix);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actor = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var unit = new HandlingUnit { Code = $"CAJA-{suffix}", LocationId = d.VehicleLocationId, Status = "Dispatched", DeliveryId = d.DeliveryId };
        db.AddRange(unit,
            new HandlingUnitLine { HandlingUnitId = unit.Id, LotId = d.LotId, Quantity = 12m },
            new QrAccessToken { HandlingUnitId = unit.Id, TokenHash = Hash(Token), ExpiresAtUtc = expiresAtUtc, RevokedAtUtc = revokedAtUtc, CreatedByAccountId = actor.Id },
            new CustodyEvent { HandlingUnitId = unit.Id, DeliveryId = d.DeliveryId, ActorAccountId = actor.Id, EventKind = "Dispatched",
                               OperationId = Domain.Common.BaseEntity.NewId(), ReceivedAtServer = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return unit.Id;
    }

    private static Task<HttpResponseMessage> GetTrace(ApiFactory api, Guid unitId, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/public/trace/{unitId}");
        if (token is not null) request.Headers.Add("X-Trace-Token", token);
        return api.CreateClient().SendAsync(request);
    }

    [Fact]
    public async Task Valid_token_in_header_returns_only_public_fields()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var unitId = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7));

        var response = await GetTrace(api, unitId, Token);
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(text);
        Assert.All(json.RootElement.EnumerateObject(), p => Assert.Contains(p.Name, AllowedFields));
        Assert.DoesNotContain(Token, text);
        Assert.DoesNotContain("quantity", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("actor", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_expired_revoked_missing_or_query_tokens_are_uniform_404()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var valid = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7));
        var expired = await SeedUnitAsync(api, DateTime.UtcNow.AddMinutes(-1));
        var revoked = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7), DateTime.UtcNow);

        var responses = new[]
        {
            await GetTrace(api, valid, null),
            await GetTrace(api, valid, Token + "x"),
            await GetTrace(api, expired, Token),
            await GetTrace(api, revoked, Token),
            await GetTrace(api, Guid.NewGuid(), Token),
            await api.CreateClient().GetAsync($"/api/v1/public/trace/{valid}?token={Token}"),
        };

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
    }
}
```

### `advance` con operaciones reales (T4)

`tests/IstpetDev.Tests/E2E/DemoAdvanceTests.cs` — usa el fixture `demo-base` de 001, que tiene 3 puntos en riesgo para un vehículo:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.E2E;

[Collection(PostgisCollection.Name)]
[Trait("Category", "E2E")]
public class DemoAdvanceTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Advance_completes_stops_with_real_receipts_and_is_idempotent()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        var proposal = await (await planner.PostAsJsonAsync("/api/v1/route-plans", new { includeConsolidation = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var routePlanId = proposal.GetProperty("routes")[0].GetProperty("routePlanId").GetGuid();
        var vehicleCode = proposal.GetProperty("routes")[0].GetProperty("vehicleCode").GetString()!;
        var publish = await planner.PostAsJsonAsync($"/api/v1/route-plans/{routePlanId}/publish",
            new { assignments = new Dictionary<string, string> { [vehicleCode] = TestTokens.Driver } });
        Assert.True(publish.IsSuccessStatusCode, await publish.Content.ReadAsStringAsync());

        var operationId = BaseEntity.NewId();
        var first = await planner.PostAsJsonAsync($"/api/v1/demo/route-plans/{routePlanId}/advance", new { operationId, stops = 2 });
        var replay = await planner.PostAsJsonAsync($"/api/v1/demo/route-plans/{routePlanId}/advance", new { operationId, stops = 2 });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.Receipts.CountAsync());
        Assert.Equal(2, await db.RouteStops.CountAsync(s => s.RoutePlanId == routePlanId && s.Status == "Completed"));
        Assert.True(await db.CustodyEvents.CountAsync() >= 4);   // al menos despacho y recepción por parada
    }
}
```


## Matriz de cobertura

| Requisitos | Tarea | Evidencia principal |
|---|---|---|
| FR-01–FR-02 | T2 | preparación y QR, E2E-01/07 |
| FR-03 | T3 | despacho atómico, E2E-01 |
| FR-04–FR-05 | T5, T8 | asignación y IndexedDB, E2E-02/06 |
| FR-06–FR-09 | T4 | recibo/replay/concurrencia, E2E-02/03/04 |
| FR-10–FR-11 | T5–T6 | evidencia independiente/estados, E2E-02/05 |
| FR-12 | T8 | revisión y aviso, E2E-06 |
| FR-13–FR-14 | T7 | proyección pública, E2E-07 |
| FR-15 | T4, T9 | parada por recibo, E2E-02 |
| FR-16 | T4 | `DemoAdvanceTests`, E2E-10 |
| FR-17–FR-18 | T1b | login y PWA por HTTPS, E2E-09 |
| NFR-01, NFR-05 | T5, T10 | teléfono real y cuota, E2E-02/08 |
| NFR-02 | T1, T4 | retención ≥ 7 días, E2E-03 |
| NFR-03 | T5 | outbox sin JWT, E2E-08 |
| NFR-04 | T6–T7 | S3 privado/QR limitado, E2E-05/07 |
| NFR-06 | T7, T10 | teléfono ajeno y HTTPS, E2E-07 |
| CE-01 | T2–T3 | E2E-01 |
| CE-02 | T4–T5 | E2E-02 |
| CE-03 | T3–T4 | E2E-03 |
| CE-04 | T4 | E2E-04 |
| CE-05 | T4, T8 | E2E-03/06 |
| CE-06 | T6 | E2E-05 |
| CE-07 | T7 | E2E-07 |
| CE-08 | T8 | E2E-06 |
| CE-09 | T1b | E2E-09 |
| CE-10 | T4 | `DemoAdvanceTests`, E2E-10 |

## Suposiciones y mocks

Para avanzar de forma desacoplada y validar la experiencia de usuario, diseño institucional, arquitectura FSD y tolerancia a fallos offline sin modificar el backend C# (.NET):

1. **In-Browser Simulated Backend (`FieldMockBackendService`):**
   - Implementado en `apps/mobile/src/app/field/mock/field-mock-backend.service.ts` y en `apps/web/src/app/field/mock/web-field-mock-backend.service.ts`.
   - Utiliza base de datos IndexedDB en el navegador (`istpetdev-driver-outbox-db` y `istpetdev-web-mock-db`) simulando persistencia ACID.
   - Modela transacciones atómicas de despacho, cálculo FEFO, generación de tokens QR con hash SHA-256, y recepción con conciliación de inventario.
   - Simula respuestas HTTP estándar (`200 OK`, `409 ASSIGNMENT_CHANGED`, `401 Unauthorized`, `503 Service Unavailable`).
2. **Seguridad y Tokens OIDC (NFR-03):**
   - El mock respeta estrictamente que los tokens de sesión OIDC/PKCE residen en memoria volátil o `sessionStorage` transitorio. En ningún caso se persisten tokens en IndexedDB ni en el outbox durable.
3. **Privacidad de Traza Pública (ADR-10 / S-07):**
   - El token de traza viaja en el fragmento hash (`#t=TOKEN`) de la URL QR y es limpiado inmediatamente mediante `history.replaceState` en el cliente.
   - El DTO público expone únicamente hitos temporales y estado de la unidad de manipulación, excluyendo datos personales, de pacientes, geolocalizaciones o cantidades sensibles.
4. **Desacoplamiento de Evidencias (S-06 / NFR-04):**
   - La cola de subida de evidencias opera de manera independiente al registro contable de recepción, con backoff exponencial simulado y almacenamiento duradero de Blobs hasta confirmación.
5. **Avisos de Ruta (CE-08):**
   - El acuse de lectura de revisiones de ruta se persiste por par `(routePlanId, revision)` en IndexedDB local, evitando notificaciones redundantes.

### T14 — Diagnóstico outbox, evidencias S3 en producción y reseteo receptor (#163)

- [x] Variables de entorno de AWS (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`, `AWS_REGION`, `EVIDENCE_S3_BUCKET`) agregadas al contenedor backend en `infra/compose/docker-compose.prod.yml`.
- [x] Conteo de evidencias confirmadas expuesto en `DeliveryDetailResponse` y consultado en `FieldService.GetDeliveryDetailAsync`.
- [x] `FieldOutboxService`: `syncAllPending()` retorna `SyncSummary` detallado; reporta bloqueo por simulación offline, desconexión o retención de sesión; no traga errores; expone `sendableCount`, `reviewCount`, `retryItem`, `claimLegacyItems` y `discardLegacyItems`.
- [x] `SyncPage`: banner de retroalimentación ante sincronización manual o global, badges claros `✓ Foto confirmada` y `✓ Firma confirmada`, tarjetas para gestión de capturas legadas sin cuenta y visualización de errores transitorios.
- [x] `HomePage`: reseteo de pantalla y formulario de recepción del Receptor con `resetReceiverScreen()`, con acceso directo a la cola outbox tras registrar.
- [x] `NovedadesPage`: visualización de confirmación de evidencias en almacenamiento S3.
- [x] Pruebas unitarias de outbox y home actualizadas (209/209 frontend pasando; 516/516 backend pasando).

### T15 — Modo sin conexión de Bodega y retiro de bultos por Conductor (#230)

- [x] **Fixtures de demo completadas (`field-demo.fixture.ts`)**: incorporación de `pk-wh-norte-01`, `hu-pickup-001` y token `tok-sec-demo-pk-01` resolviendo el 404 NOT_FOUND en modo simulación/demo al despachar y recoger.
- [x] **Comando Dispatch en outbox (`field-outbox.service.ts`)**: soporte de `OutboxCommandKind = 'Dispatch'`, guardado atómico con `operationId` para idempotencia y despacho contra `fieldApi.dispatch` al reconectar.
- [x] **Sincronización FIFO estricta**: `syncAllPending` ordena las operaciones de forma ascendente por `capturedAtUtc` y `operationId`, garantizando que `Dispatch` se ejecute antes de `Pickup`, y `Pickup` antes de `Deliver`.
- [x] **Vista Bodega sin conexión (`warehouse.component.ts`)**: banner visible «Sin conexión», persistencia y lectura desde caché local, encolado de despacho en outbox sin red y mensaje explicativo al preparar bultos/QR sin señal.
- [x] **Avance de custodia offline para Conductor (`custody-state.ts`, `receipt.page.ts`)**:
  - `effectiveDeliveryStatus` reconoce `latestPendingCustodyStatus` (`InTransit` / `Delivered`) cuando el comando está en cola local sin confirmación del servidor.
  - `actionInFlight` no bloquea transiciones en el dispositivo con comandos `SavedOnDevice`.
  - Botón `↻ Actualizar estado` y sincronización local de estado con `updateAssignmentDeliveryStatus`.
- [x] **Escaneo flexible (`scan.page.ts`)**: soporte de coincidencia por `labelUrl`, UUID parcial o código de bulto, con actualización automática de la asignación si no se encuentra en caché local y hay conexión.
- [x] **Pruebas en verde**: 129/129 pruebas móviles y suite completa de frontend (`test:frontend`) pasando.



### T16 — Motivo de rechazo «error de rotulado» y resumen por motivo (#236)

Fuente: charla de logística integral del 16-oct (#234), lámina «Puntos de servicio».

| Archivo | Capa | Cambio | Por qué | Escenario | Estado |
|---|---|---|---|---|---|
| `src/Domain/Common/ReceiptRejectionReasons.cs` | Dominio | `ErrorRotulado` (constante, `All`, descripción); sinónimos `errorrotulado`, `error_rotulado`, `error-rotulado`, `mislabeled`, `wrong_label`; `Version` → `V2` | Sin el motivo no se puede contar el error que la trazabilidad con QR debería atrapar | Simple | Hecho y probado |
| `src/Application/Field/FieldService.cs` | Backend | El mensaje de `INVALID_REJECTION_REASON` cita la versión vigente del catálogo, no «V1» fijo | El texto quedaba desactualizado con V2 | Borde | Hecho |
| `tests/.../Unit/Field/ReceiptRejectionReasonsTests.cs` | Backend (pruebas) | `TryNormalize("mislabeled")` → `ErrorRotulado`; los sinónimos de V1 no cambian; orden de `All` y versión | Criterio de aceptación del issue | Simple, Borde | 14/14 en verde |
| `tests/.../Integration/Field/FieldExceptionsHttpTests.cs` | Backend (pruebas) | Rechazo con `mislabeled` → `rejectionReason = "ErrorRotulado"` en `GET /receipts/exceptions` | La recepción con el motivo nuevo aparece en Novedades, sin migración | Simple | En verde (PostGIS) |
| `tests/.../Integration/Imports/ChaosAndBenchmarkImportTests.cs` | Backend (pruebas) | El catálogo HTTP devuelve 7 motivos, incluido `ErrorRotulado` | La prueba contaba 6 | Simple | En verde (PostGIS) |
| `tests/.../Unit/Field/FieldUnitTests.cs` | Backend (pruebas) | El catálogo del controlador espera `V2` y 7 motivos | La prueba fijaba `V1` y 6 | Simple | En verde |
| `apps/web/src/pages/novedades/novedades.page.ts` | Frontend | Resumen «Líneas rechazadas por motivo» arriba de la lista (líneas y unidades); etiquetas legibles del catálogo en la columna Motivo | Alimenta «Mermas» y «Exactitud» del tablero de indicadores | Simple, Borde | Hecho y probado |
| `apps/web/src/pages/novedades/novedades.page.spec.ts` | Frontend (pruebas) | 3 casos: conteo por motivo, recepción con `ErrorRotulado` en lista y resumen, sin resumen si solo hay ajustes | Cubre el criterio en la web | Simple, Borde | 7/7 en verde |
| PWA (`libs/shared-core/.../field.contract.ts`, `home.page.ts`, `field-outbox.service.ts`) | Frontend móvil | Sin cambios | La PWA usa una lista fija V1 y no lee el endpoint; esos archivos son de #230 (Bryan) según #253 | Borde | Pendiente (aviso a Bryan) |
| Base de datos | DB | Sin cambios | El motivo ya se guarda como texto | — | No requerido |

**Decisiones:**
- El resumen se calcula en el cliente sobre las recepciones cargadas (hasta `limit`) y lo dice en pantalla; no es un total histórico.
- Un motivo fuera del catálogo (texto libre legado) se agrupa con su texto tal cual; una línea rechazada sin motivo se agrupa como «Sin motivo registrado». No se reasigna ningún dato.

### T17 — Costo de retroceso estimado en Novedades (#237)

Fuente: charla de logística integral del 16-oct (#234): «si yo cubro un retroceso, ¿cuánto me costó en tiempo, en costo y en kilómetros?». Depende de T16 (#236, `ErrorRotulado`).

| Archivo | Capa | Cambio | Por qué | Escenario | Estado |
|---|---|---|---|---|---|
| `src/Application/Logistics/BacktrackCostEstimator.cs` | Backend | Estimador puro: km y minutos de ida + vuelta, costo con `ServiceCostCalculator` (#240) sin paradas, `isApproximate` si un tramo es aproximado; `RequiresRedelivery` para `Faltante`, `Daño`, `ErrorRotulado` | Medir lo que cuesta corregir un rechazo sin tocar la calculadora de #240 | Simple, Borde | Hecho y probado |
| `src/Application/Field/Dtos/FieldContracts.cs` | Backend | `EstimatedBacktrackDto(km, costUsd, hours, isApproximate)` y campo opcional `EstimatedBacktrack` en `ReceiptExceptionDto` | Contrato del issue; opcional para no romper clientes | Simple | Hecho |
| `src/Api/Controllers/ReceiptsController.cs` | Backend | Arma un viaje por recepción (bodega de origen → punto), vehículo de la ruta si existe, una consulta de matriz a `IDistanceProvider` | Reutiliza tramos guardados/OSRM/aproximación del motor | Simple, Borde | Hecho y probado |
| `tests/.../Unit/Logistics/BacktrackCostEstimatorTests.cs` | Backend (pruebas) | Caso fijo 2 × 9,2 km y 2 × 18 min → 18,4 km, 0,6 h, USD 11,88; sin vehículo; tramo aproximado; con perfil de #240; motivos que aplican | Criterio de aceptación | Simple, Borde | 14/14 en verde |
| `tests/.../Integration/Field/FieldExceptionsHttpTests.cs` | Backend (pruebas) | Tramos guardados → no aproximado; sin tramo → aproximado; `Sobrante` y punto sin coordenadas → `null` | Contrato HTTP de punta a punta | Simple, Borde | En verde (PostGIS) |
| `apps/web/src/shared/api/field-exceptions.api.ts` | Frontend | Tipo `EstimatedBacktrackDto` y campo opcional | Contrato en un solo lugar | Simple | Hecho |
| `apps/web/src/pages/novedades/novedades.page.ts` | Frontend | Línea «Reentrega estimada: … km · USD … · … h» en cada tarjeta, «(aproximado: distancia en línea recta)» si corresponde; aviso de que no se suma a la ruta | Visibilidad del retroceso para el Planificador | Simple, Borde | Hecho y probado |
| `apps/web/src/pages/novedades/novedades.page.spec.ts` | Frontend (pruebas) | 3 casos: formato, línea normal y aproximada, sin estimación | Criterio en la web | Simple, Borde | 10/10 en verde |
| Base de datos | DB | Sin cambios | Se calcula al leer; no se persiste | — | No requerido |

**Decisiones:**
- No se suma a `RoutePlan.TotalCost` ni a ningún costo de la ruta original (evita el doble conteo de la nota de hallazgos); el endpoint es de solo lectura y no escribe el valor.
- El tiempo es solo de viaje (ida y vuelta) según la matriz; no incluye descarga, espera ni el factor de hora pico de #245 (la reentrega no tiene hora de salida conocida), y el costo no incluye ayudantes ni merma.
- Una reentrega por recepción aunque haya varias líneas rechazadas; varias recepciones del mismo punto no se consolidan en un viaje.
- Sin coordenadas de la bodega o del punto, `estimatedBacktrack` es `null` y la tarjeta no muestra la línea.
- Si OSRM está configurado y faltan tramos, la consulta puede tardar hasta su timeout (3 s por bloque) y guarda los tramos obtenidos, igual que el resto del motor.
