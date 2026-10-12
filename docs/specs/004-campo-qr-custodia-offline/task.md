# 004 — Campo: QR, custodia, despacho/recepción y PWA offline: tareas

> Pasos ejecutables de [spec.md](spec.md) y [plan.md](plan.md). Un solo `task.md`, según la plantilla. El código de 004 se implementa sobre 001 y 002 integrados; el aviso real requiere después el productor de 003. Cada tarea se cierra con prueba roja, cambio mínimo, prueba verde y commit. Estos pasos están pendientes: crear este documento no acredita que la PWA o la API funcionen.

## Resumen

| ID | Entrega comprobable | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T0 | Contratos de 001/002 y entorno confirmados | 001, 002 | — | pendiente |
| T1 | Modelo, migración y DTOs de campo | T0 | — | pendiente |
| T2 | Preparación de caja, QR y etiqueta | T1 | [P] con T5 | pendiente |
| T3 | Despacho atómico e idempotente | T2 | — | pendiente |
| T4 | Recepción, parcial, conflictos y parada | T3 | — | pendiente |
| T5 | Outbox durable y captura offline | T1 | [P] con T2–T4 | pendiente |
| T6 | Evidencia privada y reintentos | T4, T5 | [P] con T7 | pendiente |
| T7 | Consulta pública limitada y revocación | T2 | [P] con T5–T6 | pendiente |
| T8 | Asignaciones, revisión de ruta y aviso | T4, T5 | — | pendiente |
| T9 | Recorrido HTTP, móvil y navegador | T6–T8 | — | pendiente |
| T10 | Ensayo físico, evidencias y bóveda | T9 | — | pendiente |

`[P]` indica archivos separados y contratos acordados. Ejecutar desde la raíz. Filtros de pruebas son los nombres propuestos aquí; si el proyecto de pruebas de 001 usa otro nombre, mantener el caso y ajustar el comando en el mismo commit. No inventar datos del fixture en código.

## Secuencia común

1. Escribir la prueba de cada comportamiento antes de implementar; correr su filtro y comprobar un fallo por la función ausente.
2. Implementar, repetir el filtro y ejecutar `dotnet build IstpetDev.sln` para cambios .NET o `npm run check` para UI; exigir 0 fallos. Para PWA, correr además la prueba de navegador en el navegador objetivo.
3. Revisar el diff, pruebas de dependencias y migración/contrato; hacer commit pequeño. Registrar evidencia E2E solo después de la ejecución real.

### T0 — Preparar contratos y dispositivo

**Archivos:** lectura de `docs/specs/001-nucleo-delgado-importador/`, `docs/specs/002-motor-logistico/`, `src/Domain/Entities/`, `apps/mobile/` y `apps/web/`.

- [ ] Confirmar que 001 y 002 están integrados: `Delivery`/`DeliveryLine` publicados con reserva y lotes FEFO, `HandlingUnit`, `CustodyEvent`, `Receipt`, `InventoryMovement`, `IdempotencyRecord`, roles fijos y `demo-region`. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'Category=E2E'`; registrar el resultado base.
- [ ] Comparar propiedades e índices reales de esas entidades con [Modelo y migración](plan.md#modelo-y-migración). Documentar en el PR qué columnas faltan antes de generar **una** migración nueva; no editar migraciones anteriores ni usar Schema95 como núcleo final.
- [ ] Confirmar aprobación del spec, teléfono del pitch, HTTPS, instalación PWA, cámara, IndexedDB con Blobs, bucket S3 privado y cuentas Bodega/Conductor. Si 003 aún no existe, preparar una revisión de prueba con `DriverMessage` para T8 sin fingir que el agente la produjo.

### T1 — Migración y contratos de campo

**Archivos:** modificar `src/Domain/Entities/LogisticsEntities.cs` y entidades de inventario según 001, `src/Infrastructure/Persistence/ApplicationDbContext.cs`, `src/Application/Common/Interfaces/IApplicationDbContext.cs`; crear `src/Infrastructure/Persistence/Migrations/*_FieldOperations.cs`, `src/Application/Field/Dtos/FieldContracts.cs`, `libs/shared-core/src/contracts/field.contract.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/FieldSchemaTests.cs`.

- [ ] Prueba roja de persistencia: una `HandlingUnit` referencia una entrega activa y sus líneas a `DeliveryLine`/lote; código único por organización; `QrAccessToken` guarda hash único, vencimiento y revocación; `IdempotencyRecord` guarda actor, acción, operación, huella, respuesta y retención; recibo/custodia/evidencia tienen las relaciones del plan.
- [ ] Generar una migración `FieldOperations` sobre 001/002, reutilizando columnas ya existentes. Índices para token hash y clave `(OrganizationId, ActorId, Action, OperationId)`; `RetainUntilUtc` ≥ aceptación + 7 días. Añadir concesiones de rol según patrón de migración de 001. Probar `dotnet ef database update` en base de prueba vacía y con entregas existentes; no borrar el historial operativo.
- [ ] Fijar DTOs camelCase, UUID v7, horas UTC, cantidades decimales con unidad base, errores Problem Details con `code` y `correlationId`; compartir tipos TypeScript del contrato, sin incluir tokens OIDC en el outbox. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~FieldSchemaTests'` y `npm run check`; esperado: 0 fallos. Commit `feat(field): add operations schema and contracts`.

### T2 — Preparar caja y emitir etiqueta QR

**Archivos:** crear `src/Application/Field/HandlingUnitPreparationService.cs`, `src/Api/Controllers/FieldController.cs`, `apps/web/src/pages/field-label/field-label.page.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/HandlingUnitPreparationTests.cs` y prueba web de etiqueta.

- [ ] Pruebas rojas: Bodega crea una o más unidades desde `Delivery` publicada; por cada lote, suma de líneas de unidades ≤ cantidad reservada de `DeliveryLine`; unidad de otra entrega, lote distinto, cantidad negativa o exceso se rechazan sin filas parciales. Preparar no mueve stock ni completa ruta.
- [ ] Implementar `POST /api/v1/deliveries/{id}/handling-units`, código único y token aleatorio de ≥ 128 bits: almacenar solo SHA-256, vencimiento configurable inicial 7 días, token claro únicamente en `labelUrl` de creación/reimpresión. Reimprimir revoca el token anterior y emite uno nuevo. El QR no otorga permisos de escritura.
- [ ] Etiqueta HTML imprimible en web: QR de URL HTTPS con `unitId` y token, código de caja, producto/unidad tomados de datos. Probar impresión/escaneo en teléfono; no escribir nombres o cantidades del escenario en código. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~HandlingUnitPreparationTests'` y `npm run check`; esperado: 0 fallos. Commit `feat(field): prepare handling units and print qr labels`.

### T3 — Despacho y primera transferencia de custodia

**Archivos:** crear `src/Application/Field/DispatchService.cs`, `IdempotencyService.cs`; extender `FieldController.cs`; probar en `tests/IstpetDev.Tests/Integration/Field/DispatchIntegrationTests.cs`.

- [ ] Pruebas rojas: solo Bodega despacha unidad preparada de entrega publicada; `Idempotency-Key` coincide con `operationId` UUID v7; unidad ajena, exceso, lote no reservado o stock insuficiente revierten la transacción completa. `Automatizacion` y Conductor reciben 403.
- [ ] Implementar `POST /api/v1/deliveries/{id}/dispatch` con transacción: bloquear líneas, saldos y `RouteStockReservation.Active` de 002 por entrega/lote en orden estable, marcarlas `Consumed`, restarlas del agregado `StockBalance.QuantityReserved`, mover cantidades bodega→vehículo una vez por línea, escribir `CustodyEvent` con actor y hora del servidor, actualizar unidad/entrega, guardar respuesta y huella canónica en `IdempotencyRecord` en el mismo commit.
- [ ] Probar replay con misma clave/cuerpo: mismo status/cuerpo/IDs, ningún movimiento nuevo; misma clave con otro cuerpo: 409 `IDEMPOTENCY_CONFLICT`; dos solicitudes concurrentes no despachan dos veces. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~DispatchIntegrationTests'`; esperado: 0 fallos. Commit `feat(field): dispatch units with atomic custody`.

### T4 — Recibir y conciliar cantidades

**Archivos:** crear `src/Application/Field/ReceiptService.cs`; extender `FieldController.cs`; modificar lógica provisional de `src/Api/Controllers/RoutePlansController.cs` y servicio de paradas de 002; probar en `tests/IstpetDev.Tests/Integration/Field/ReceiptIntegrationTests.cs`, `FieldAuthorizationTests.cs`.

- [ ] Pruebas rojas: conductor solo recibe `Delivery.AssignedAccountId` propia y despachada. Solicitud incluye mismo UUID v7 en cuerpo/cabecera, `expectedDeliveryVersion`, `occurredAtDevice`, líneas por `deliveryLineId`/lote, aceptado/rechazado en unidad base y motivo del rechazo; actor se toma del JWT, nunca del cuerpo.
- [ ] Implementar una transacción con bloqueo de `DeliveryLine` y `StockBalance`: `Receipt`/`ReceiptLine`, movimiento vehículo→punto **solo** por aceptado, custodia de rechazado/pendiente en vehículo, estado de entrega y parada derivado del recibo, respuesta idempotente guardada. Hora contable = servidor; `occurredAtDevice` es evidencia informativa. Desactivar el endpoint provisional `/stops/{sequence}/complete` como camino operativo, sin simular movimiento.
- [ ] Pruebas rojas/verde de 12 L despachados: 9 aceptados elevan punto 9; 3 rechazados siguen en vehículo con motivo. Una devolución posterior hecha por Bodega mueve esos 3 a bodega con nuevo `CustodyEvent`; no ocurre automáticamente al rechazar.
- [ ] Probar replay igual, clave alterada, dos dispositivos con claves distintas y suma que excede pendiente: solo uno confirma, el otro 409 `QUANTITY_EXCEEDED`, sin stock negativo ni duplicado. Cambiar revisión de ruta entre captura y sync: aceptar si entrega/asignación/cantidad siguen vigentes; reasignar entrega: 409 `ASSIGNMENT_CHANGED` con datos permitidos. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~ReceiptIntegrationTests|FullyQualifiedName~FieldAuthorizationTests'`; esperado: 0 fallos. Commit `feat(field): accept idempotent receipts and complete stops`.

### T5 — Captura y outbox offline

**Archivos:** crear `apps/mobile/src/app/field/field-outbox.service.ts`, `field-api.service.ts`, `scan.page.ts`, `receipt.page.ts`, `sync.page.ts`; modificar `apps/mobile/src/app/home/home.page.ts`, `app.routes.ts`, `app.config.ts`, `apps/mobile/angular.json`; crear `apps/mobile/ngsw-config.json`, `apps/mobile/public/manifest.webmanifest`; añadir `idb@8.0.4`, `signature_pad@5.1.4` si aún faltan; probar con pruebas Angular/Playwright del área `field`.

- [ ] Prueba roja de navegador: descargar snapshot asignado y abrirlo instalado sin señal; escanear solo una caja conocida; guardar `operationId`, líneas, foto Blob y firma PNG en **una** transacción IndexedDB por identidad. Cerrar/reabrir la PWA en modo avión conserva operación y ambos archivos; ningún token OIDC se escribe en IndexedDB durable.
- [ ] Implementar captura de cámara y firma voluntaria, UUID v7 estable, sello de dispositivo y estados «Guardado en el teléfono», «Enviando», «Aceptado por el servidor», «Conflicto», «Evidencia pendiente». Mostrar «Guardado» solo al confirmar la transacción; cuota o fallo de Blob deja mensaje claro y no afirma persistencia.
- [ ] Sincronizar al abrir, al recuperar red y al pulsar botón: primero recibo con `Idempotency-Key`, después evidencias; 401 pausa y exige reautenticación conservando cola, 403/409 conservan captura visible, red/429/503 reintentan con espera. Una revisión nueva no borra ni reescribe una captura pendiente.
- [ ] Ejecutar `npm run check` y las pruebas del área móvil; ensayo en navegador/dispositivo real para Service Worker, instalación y reinicio. Commit `feat(mobile): persist offline receipts and evidence`.

### T6 — Evidencia privada por archivo

**Archivos:** crear `src/Application/Field/EvidenceService.cs`, `src/Infrastructure/Storage/S3EvidenceStorage.cs`, `src/Api/Controllers/EvidenceController.cs`; extender `field-api.service.ts`/`sync.page.ts`; añadir `AWSSDK.S3` solo si no existe; probar en `tests/IstpetDev.Tests/Integration/Field/EvidenceIntegrationTests.cs` y prueba móvil de reintento.

- [ ] Pruebas rojas: solo conductor asignado al recibo solicita carga de foto/firma; MIME y tamaño permitidos; API genera object key propio y URL presignada de vida corta para bucket privado. La consulta pública nunca devuelve key, URL ni metadatos de evidencia.
- [ ] Implementar `POST /receipts/{id}/evidence-uploads`: una autorización por archivo con checksum SHA-256/tamaño/tipo; subir Blob directo a S3; `POST /evidence/{id}/confirm` verifica existencia, metadata y huella antes de `Confirmed`. Solo actores asignados obtienen lectura temporal privada.
- [ ] Simular falla S3 después de recibo aceptado: inventario y `Receipt` permanecen; móvil muestra «Evidencia pendiente», conserva Blob y reintenta solo archivo, sin reenviar movimiento. Confirmar cada archivo independientemente y evitar duplicados con replay. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~EvidenceIntegrationTests'` y prueba móvil de sync; esperado: 0 fallos. Commit `feat(field): upload private receipt evidence independently`.

### T7 — Trazabilidad pública mínima

**Archivos:** modificar `src/Application/PublicTrace/Services/PublicTraceService.cs`, `src/Api/Controllers/PublicTraceController.cs`, `libs/shared-core/src/contracts/trace.contract.ts`, `apps/web/src/pages/public-trace/public-trace.page.ts`, `apps/web/src/shared/api/api.service.ts`; probar en `tests/IstpetDev.Tests/Integration/Field/PublicTraceHttpTests.cs` y prueba web de página.

- [ ] Pruebas rojas: token correcto devuelve únicamente código de unidad, producto/unidad, estado e hitos `prepared`, `dispatched`, `received` con horas de **servidor** tomadas de `CustodyEvent`; no hay cantidad, actor, contacto, ruta, foto/firma, URL privada ni token en JSON. El registro fijo de demo y la búsqueda pública por código de Delivery desaparecen.
- [ ] Implementar `GET /api/v1/public/trace/{unitId}?token=…`: comparar hash de token vigente, revocación y caducidad **antes** de proyectar datos; sin token, alterado, expirado, revocado o unidad inexistente → respuesta uniforme 404. `Cache-Control: no-store`; 429 por límite de tasa sin revelar existencia. `POST /handling-units/{id}/public-token/revoke` solo Bodega/Gerencia.
- [ ] La página acepta directamente la URL QR por HTTPS, consulta el endpoint y muestra estados en lenguaje simple; no usa fixture incrustado. Comprobar que reimpresión revoca etiqueta anterior y que el token no entra en analytics/logs de la página. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~PublicTraceHttpTests'` y `npm run check`; esperado: 0 fallos. Commit `feat(field): expose limited revocable qr trace`.

### T8 — Asignaciones y aviso de revisión

**Archivos:** extender `src/Api/Controllers/FieldController.cs`, `apps/mobile/src/app/field/assignment.service.ts`, `field-outbox.service.ts` y pantalla de asignaciones; probar en `tests/IstpetDev.Tests/Integration/Field/MobileAssignmentsTests.cs` y prueba móvil de aviso.

- [ ] Pruebas rojas: `GET /api/v1/mobile/assignments` con Conductor devuelve solo entregas/unidades asignadas, rutas y revisiones vigentes, paradas, líneas en unidad base, hora del servidor y `DriverMessage` si existe. Conductor distinto no puede ver entrega ni evidencia ajena; `Automatizacion` no descarga como conductor.
- [ ] Guardar snapshot por identidad en IndexedDB con hora de descarga. Sin red, mostrar último snapshot con aviso de posible desactualización. Con red, consultar revisión al abrir y cada 15 s en primer plano; revisión mayor muestra mensaje, número y paradas cambiadas; marcar leído localmente por `(routePlanId, revision)` evita repetirlo.
- [ ] Ensayar con revisión/mensaje sembrado por API si 003 aún no está integrado, y repetir con productor real cuando lo esté. La nueva revisión no invalida por sí sola el recibo capturado offline. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~MobileAssignmentsTests'` y prueba móvil; esperado: 0 fallos. Commit `feat(mobile): show assigned route revisions once`.

### T9 — Integración completa y regresiones

**Archivos:** crear `tests/IstpetDev.Tests/E2E/FieldJourneyTests.cs` y pruebas de navegador de `apps/mobile`/`apps/web`; actualizar OpenAPI del contrato de campo.

- [ ] Automatizar [E2E-01–E2E-04](test-e2e.md) sobre API+PostgreSQL: preparación, despacho, recepción, replays, concurrencia, parcial, devolución y permisos. Usar Testcontainers y S3 de ensayo/emulador para casos de evidencia; guardar IDs y conteos observables.
- [ ] Automatizar navegador: outbox en IndexedDB con Blobs, reinicio, cuota, 401/409/429/503, revisión y mensaje una sola vez; consulta QR no muestra datos prohibidos. Probar que `/stops/{sequence}/complete` provisional ya no puede simular una recepción.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests`, `npm run check` y verificación de bóveda del repo; registrar fallos reales. Revisar mapa FR/NFR/CE siguiente y corregir huecos antes del ensayo físico. Commit `test(field): cover custody and offline journeys`.

### T10 — Ensayo en teléfono y cierre trazable

**Archivos:** actualizar el registro de ejecuciones de [test-e2e.md](test-e2e.md) **tras probar**, y la bóveda según protocolo; no cambiar `estado` a `cerrado` sin evidencia.

- [ ] Ensayar S-05/S-06/S-07/S-11 del spec en teléfono Android real instalado: caja/QR impresos, modo avión, foto/firma, reinicio, reconexión, parcial y conflicto. Guardar cantidad antes/después de bodega/vehículo/punto, recibo, movimientos, custodia y estados de cola; no guardar evidencia privada públicamente.
- [ ] Ensayar S3 privado real y QR por HTTPS desde teléfono ajeno con datos móviles. Probar token revocado/expirado y límite de tasa; capturar únicamente respuesta pública permitida. Medir tiempos reales del recorrido.
- [ ] Registrar la sustitución del endpoint provisional de 002, el nuevo esquema posterior al conteo de 27 tablas de 001 y la consulta pública tokenizada en la bóveda. Añadir trabajo de 004 al registro previo/tag del equipo según G8, sin declarar aprobación de una ADR todavía propuesta.
- [ ] Si falta alguna prueba obligatoria, dejar `estado: en curso` y anotar plan B de D8 (recepción en línea/video) como contingencia, no como pase E2E. Commit `docs(field): record physical demo evidence` solo con artefactos permitidos.

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
