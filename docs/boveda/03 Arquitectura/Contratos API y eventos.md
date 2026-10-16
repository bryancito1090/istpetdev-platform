---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-16
tags: [arquitectura, contratos]
---

# Contratos API y eventos

**Versión inicial propuesta:** REST /api/v1 y hub /hubs/operations. Será la fuente compartida para web, móvil, n8n y backend. El catálogo operativo de endpoints implementados con ejemplos de invocación local se encuentra en [Guía de Desarrollo Local y Endpoints API](../../api-guide.md).

## Convenciones

- JSON camelCase; fechas ISO 8601 UTC en servidor.
- UUID como texto; cantidades con precisión decimal y unidad explícita.
- Cliente siempre indica operationId en escrituras reintentables; clave enviada como Idempotency-Key.
- Actor real derivado de autenticación, nunca confiado desde el payload.
- organizationId validado contra sesión, aunque exista en una referencia.
- Cada endpoint privado y acción SignalR exige permiso RBAC para su acción y ámbito del recurso según [[Seguridad y evidencias]]; roles/ámbitos del payload no otorgan acceso. La única excepción de lectura pública es el resumen QR explícito.
- version/expectedVersion para cambios concurrentes.
- Paginación y filtros acotados; no devolver todo el historial nacional.
- correlationId para seguir una operación.
- Entornos de demo y sus datos están identificados.
- Generar OpenAPI desde la API y verificar clientes contra esa definición cuando exista.

## Endpoints propuestos

| Método y ruta                                | Función                                 |
| -------------------------------------------- | --------------------------------------- |
| GET /api/v1/service-points                   | Puntos y filtros                        |
| GET /api/v1/inventory                        | Saldos por ubicación/SKU/lote           |
| POST /api/v1/lots                            | Registrar lote                          |
| POST /api/v1/handling-units                  | Crear caja/unidad y líneas de lote      |
| POST /api/v1/consumptions                    | Registrar uso de insumo                 |
| GET /api/v1/risks                            | Riesgos con cálculo                     |
| POST /api/v1/replenishment-requests          | Solicitar cantidad y fecha              |
| POST /api/v1/route-plans                     | Calcular propuesta, 202 si es asíncrono |
| GET /api/v1/jobs/{id}                        | Estado y resultado de trabajo           |
| POST /api/v1/route-plans/{id}/publish        | Aprobar/publicar versión                |
| GET /api/v1/drivers                          | Conductores activos para asignar (#160) |
| PUT /api/v1/route-plans/{id}/driver          | Reasignar conductor de entregas pendientes (#160) |
| POST /api/v1/deliveries/{id}/dispatch        | Registrar salida y custodia             |
| POST /api/v1/deliveries/{id}/receipts        | Recepción por líneas, idempotente       |
| POST /api/v1/receipts/{id}/evidence-uploads  | Obtener autorización de carga           |
| POST /api/v1/evidence/{id}/confirm           | Confirmar objeto y metadatos            |
| GET /api/v1/handling-units/{id}/trace        | Historial autorizado                    |
| GET /api/v1/public/trace/{unitId}?token=...  | Resumen público deliberado              |
| POST /api/v1/incidents                       | Incidente operativo autorizado          |
| POST /api/v1/demo/incidents                  | Incidente sintético, solo demo          |
| POST/GET /api/v1/conversations/{id}/messages | Envío/historial durable                 |
| POST /api/v1/simulations                     | Iniciar escenario comparativo           |
| GET /api/v1/simulations/{id}/metrics         | Resultados y denominadores              |
| POST /api/v1/automation/risk-runs            | Solicitar cálculo programado            |
| POST /api/v1/automation/risk-explanations    | Adjuntar explicación validada           |
| GET /api/v1/logistics/stock                  | Stock disponible por bodega y lote (#194) |
| POST /api/v1/logistics/sourcing              | Origen sugerido sin crear pedidos ni reservas (#194) |
| GET /api/v1/logistics/vehicles/available     | Vehículos activos sin ruta publicada en la fecha (#194) |

## Recepción offline: ejemplo de contrato

**Aplicación propuesta en spec 004:** la recepción real reemplaza el marcado provisional de parada completa de 002. Bodega crea `HandlingUnit` desde una `Delivery` publicada y despacha con transferencia bodega→vehículo. El conductor asignado envía recepción con `operationId` idéntico a `Idempotency-Key`, líneas aceptadas/rechazadas, `expectedDeliveryVersion` y `occurredAtDevice`. Una revisión distinta se valida contra entrega, asignación y cantidades vigentes; la revisión sola no rechaza. El commit de recibo, movimientos y respuesta idempotente es único; las evidencias se suben después a S3 y se confirman por separado. Ver [[Definiciones de producto y plan hasta el pitch]].

```json
{
  "operationId": "UUID_UNICO_DEL_DISPOSITIVO",
  "expectedDeliveryVersion": 4,
  "occurredAtDevice": "2026-10-17T16:05:00Z",
  "lines": [
    {
      "deliveryLineId": "UUID_LINEA",
      "lotId": "UUID_LOTE",
      "quantityReceived": "12.000",
      "unit": "L"
    }
  ],
  "evidenceLocalRefs": ["FOTO_LOCAL_1", "FIRMA_LOCAL_1"]
}
```

El ejemplo usa placeholders, no UUIDs válidos. El servidor valida unidad, cantidades restantes y lote. Las referencias locales no prueban que ya exista evidencia en S3. Respuesta: receiptId, acceptedAtServer, deliveryVersion, estado operativo y estado de evidencia.

## Idempotencia

| Situación                          | Resultado                                      |
| ---------------------------------- | ---------------------------------------------- |
| Clave nueva + operación válida     | Registrar efecto y resultado en un commit      |
| Misma clave + misma huella         | Devolver resultado previo sin nuevo movimiento |
| Misma clave + contenido distinto   | 409 con código IDEMPOTENCY_CONFLICT            |
| Clave nueva + duplicado de negocio | Rechazo/conflicto según cantidades restantes   |
| Validación fallida                 | Error claro; no registrar cambio parcial       |

Definir período de conservación que cubra reintentos offline; no expirar claves antes de la ventana de trabajo del dispositivo.

## Errores

Usar Problem Details con código estable, correlationId y errores de campos. HTTP 400 para entrada inválida, 401 sin sesión válida, 403 sin permiso, 404 para referencia inaccesible/inexistente, 409 para conflicto y 422 para restricción de negocio acordada. 429/503 son reintentables con espera; no reintentar ciegamente errores definitivos.

El conflicto devuelve información permitida para resolverlo, sin exponer datos de otra organización.

## Eventos de dominio y SignalR

Eventos iniciales: StockChanged, RiskCalculated, RoutePublished, IncidentCreated, DeliveryDispatched, ReceiptAccepted, EvidenceCompleted, MessageCreated.

Envelope: eventId, eventType, schemaVersion, aggregateId, aggregateVersion, occurredAtServer, correlationId y payload mínimo autorizado. Las notificaciones se originan después de persistir. Puede haber duplicados y reconexiones; el cliente compara versión y consulta REST.

El hub ofrece notificaciones autorizadas; el historial y las escrituras críticas siguen en API.

## Contrato n8n

La API devuelve riskId, stockSnapshotId, valores calculados y referencias de entrada. n8n devuelve riskId, runId, explicación, provider/model/promptVersion y estado. No permite que DeepSeek cambie stock, prioridad oficial calculada ni fechas de entrega.

Ver [[n8n e IA]], [[Modelo de datos]] y [[Movil offline y sincronizacion]].

## Administración configurable — propuesta del 9 de octubre

ADR-16 agrega los siguientes contratos para RF-17/18/19. Tablas, invariantes y delegación en [[Esquema completo de base de datos]] y [[RBAC y configuracion del sistema]]. Rutas propuestas; no afirmar que ya existen controllers. Todos los GET se filtran/paginan; escrituras mutables usan expectedVersion, y publicaciones/concesiones reintentables usan Idempotency-Key. Actor y pertenencia se resuelven en servidor.

| Método y ruta | Función y límite |
|---|---|
| GET /api/v1/permissions | Catálogo de acciones implementadas concedibles por quien consulta |
| GET/POST /api/v1/roles | Listar/crear roles de organización autorizada |
| PATCH /api/v1/roles/{id} | Nombre/estado permitido, sin alterar organización ni generar acciones |
| PUT /api/v1/roles/{id}/permissions | Reemplazar asociaciones delegables con diff/versión/motivo y auditoría |
| PUT /api/v1/roles/{id}/field-permissions | Administrar allowlist de campos conocidos, conservando campos de servidor protegidos |
| GET/POST /api/v1/organization-accesses | Consultar/conceder membresía a identidad existente, con autoridad de delegación |
| PUT /api/v1/organization-accesses/{id}/roles | Asignar/revocar roles, sin autoescalación ni cruce de organización |
| PUT /api/v1/organization-accesses/{id}/scopes | Concesiones de ubicación/punto existentes y delegables |
| POST /api/v1/deliveries/{id}/assignments | Asignar conductor/receptor activo a una entrega autorizada |
| GET /api/v1/configuration-definitions | Definiciones/tipos/rangos admitidos, sin modificar garantías protegidas |
| GET/POST /api/v1/configuration-versions | Listar/crear valores versionados validados contra definición |
| POST /api/v1/configuration-versions/{id}/publish | Congelar contenido/hash de versión válida |
| POST /api/v1/configuration-activations | Activar versión publicada por ámbito/vigencia sin solapamiento |
| GET/POST /api/v1/catalogs | Catálogos editables por organización |
| POST/PATCH /api/v1/catalogs/{id}/options | Gestionar opciones, preservando valores ya referenciados |
| GET/POST /api/v1/workflows | Procesos admitidos, sin crear handlers desde texto |
| POST /api/v1/workflows/{id}/versions | Borrador con estados/transiciones válidos |
| POST /api/v1/workflow-versions/{id}/publish | Publicar versión íntegra, congelando estados/transiciones |
| GET/POST /api/v1/templates | Listar/crear plantillas de tipo/módulo admitido |
| POST /api/v1/templates/{id}/versions | Nueva revisión en borrador |
| PATCH /api/v1/template-versions/{id} | Editar borrador, validaciones/fields/layout declarativos; jamás una versión publicada |
| POST /api/v1/template-versions/{id}/publish | Validar/compilar y congelar versión/hash |
| PUT /api/v1/template-versions/{id}/field-access | Administrar lectura/escritura de campos con permiso específico y auditoría |
| POST /api/v1/template-bindings | Activar versión publicada por módulo/acción/ámbito |
| GET /api/v1/template-bindings/effective | Resolver plantilla/versión efectiva para contexto autorizado |
| POST /api/v1/form-submissions | Captura con templateVersionId, contexto concreto, data validada y operationId |
| GET /api/v1/form-submissions/{id} | Lectura de campos/recurso autorizados, incluida versión histórica |
| POST /api/v1/form-submissions/{id}/corrections | Nueva captura con motivo y enlace a la anterior, sin sobrescribir aceptación |
| GET /api/v1/audit-events | Auditoría mínima autorizada, sin credenciales ni datos sensibles en claro |

Eventos adicionales propuestos: AccessChanged, ConfigurationActivated, TemplatePublished, TemplateBindingChanged y FormSubmitted. Payload mínimo contiene IDs/versiones y organización autorizada; no distribución global de concesiones o contenido sensible. AccessChanged invalida autorización/canales; no obliga al cliente a aceptar roles enviados como autoridad.

La actualización de plantillas requiere texto/rich content del tipo admitido; JSON válido por sí solo no permite propiedades adicionales, expressions ejecutables ni campos de dominio protegidos. Si la versión offline dejó de admitir nueva captura, responder 409 con código TEMPLATE_VERSION_CONFLICT e información suficiente para corrección dentro del ámbito permitido.

## Implementación e integración de contratos del spec 004 — 13 de octubre de 2026

Los contratos del spec 004 (`docs/specs/004-campo-qr-custodia-offline/`) han sido implementados e integrados en el backend C# .NET y consumidos por las aplicaciones Web y Móvil:
- `POST /api/v1/deliveries/{id}/handling-units`: Creación de unidades de manipulación con generación de token SHA-256 efímero (`DeliveriesController`).
- `POST /api/v1/deliveries/{id}/dispatch`: Despacho atómico e idempotente con bloqueo transaccional, deducción de reservas y transferencia de custodia de bodega a vehículo (`DeliveriesController`, `FieldService`).
- `POST /api/v1/deliveries/{id}/receipts`: **Superado por #162**: la recepción final la registra el Receptor en `POST /api/v1/receiver/handling-units/{unitId}/receipt`; esta ruta responde 409 `RECEIPT_REQUIRES_RECEIVER`.
- `POST /api/v1/receipts/{id}/evidence-uploads` y `POST /api/v1/receipts/{id}/evidence-confirm`: Generación de URLs presignadas y confirmación desacoplada de evidencias fotográficas y firmas (`EvidenceController`).
- `GET /api/v1/public/trace/{unitId}`: Consulta pública de trazabilidad sanitaria con token en cabecera `X-Trace-Token` o query string, limitación de tasa (`PublicTraceRateLimiter`), sanitización del fragmento `#t=` en frontend y protección estricta de datos clínicos (`PublicTraceController`, `PublicTraceService`).
- `GET /api/v1/deliveries`: Consulta paginada de entregas para la torre de control y panel 005 (`DeliveriesController`).
- `GET /api/v1/mobile/assignments`: Asignaciones de conductor y control de revisiones incrementadas (`MobileAssignmentsController`).
- `POST /api/v1/demo/route-plans/{id}/advance`: Avance operativo de paradas para la demo que ejecuta la cadena contable real: preparación, despacho y recepción (`DemoController`).

## Máquina de estados de custodia Bodega → Conductor → Receptor (#162 — 15 de octubre de 2026)

El issue #162 exige que cada rol actúe solo en su turno y que la traza pública muestre Despachada → En ruta → Entregada → Recibida. La cadena se implementa sobre los campos string existentes (`Delivery.Status`, `HandlingUnit.Status`, `CustodyEvent.EventKind`); no hay migración de estados. La migración `CustodyChainStates` solo quita la unicidad del índice `CustodyEvent(OrganizationId, OperationId)`, porque una operación con varios bultos emite un evento de custodia por bulto.

| Paso | Entrega | Bulto | Actor | Efecto contable |
|---|---|---|---|---|
| Preparación | `Draft` | `Sealed` | Bodega | Ninguno; empaque y QR |
| Despacho | `Dispatched` | `Dispatched` | Bodega | Movimiento bodega → vehículo (una sola vez) |
| Recogida | `InTransit` | `InTransit` | Conductor asignado | Evento de custodia; sin movimiento de stock |
| Entrega física | `Delivered` | `Delivered` | Conductor asignado | Evento de custodia; sin movimiento de stock |
| Recepción | `Received` / `Partial` | `Received` / `Partial` | Receptor autorizado (QR) | Recibo único **por entrega completa**; movimiento vehículo → punto solo de lo aceptado |

- **Un solo recibo final por entrega (revisión 15-oct):** `Received` y `Partial` son terminales; cualquier recibo previo, incluso con estado legado `Delivered` o inconsistente, responde 409 `ALREADY_RECEIVED`. El guard terminal precede a la validación de `expectedDeliveryVersion`, y el replay exacto de la misma operación sigue devolviendo 200 antes del guard. Lo rechazado permanece bajo custodia del conductor hasta su devolución explícita a bodega; no se vuelve a recibir.
- **Cobertura exacta:** antes de crear `Receipt`/`ReceiptLine` se exige el conjunto exacto de líneas despachadas (sin omisiones ni duplicados), `LotId` igual al de la `DeliveryLine`, cantidades no negativas y `aceptada + rechazada = DispatchedQuantity` por línea. Omisiones, duplicados o lotes ajenos → 422 `RECEIPT_COVERAGE_MISMATCH`; reportes menor/mayor o negativos → 422 `QUANTITY_EXCEEDED`. El receptor ve todos los bultos y todas las líneas de la entrega en `GET /api/v1/receiver/handling-units/{unitId}`.
- **Saldos reales:** la transferencia de lo aceptado exige saldo suficiente en el vehículo (409 `INSUFFICIENT_VEHICLE_STOCK`) y descuenta el saldo exacto, sin `Math.Max` que oculte faltantes. El despacho agrupa el ledger por línea y lote, exige todos los bultos de la entrega y cobertura exacta de lo planificado (409 `INCOMPLETE_HANDLING_UNITS`), y usa saldos en memoria para no duplicar filas del mismo lote.
- **Ubicación contable vs. custodia física:** la ubicación contable cambia solo en Despacho (bodega → vehículo) y Recepción (vehículo → punto). Recogida y entrega solo agregan `CustodyEvent` (`PickedUp`, `Delivered`) con actor y hora de servidor.
- **Rutas nuevas:** `POST /api/v1/deliveries/{id}/pickup` (Conductor asignado) y `POST /api/v1/deliveries/{id}/deliver` (Conductor asignado), con `operationId` = `Idempotency-Key`. Rechazan sin asignación con 403 `UNAUTHORIZED_ASSIGNMENT` (la ausencia de asignación no concede permiso).
- **Guardas de recepción:** `Draft` → 409 `NOT_DISPATCHED`; `Dispatched`/`InTransit` → 409 `NOT_DELIVERED`; estado que no admite recepción → 409 `INVALID_STATE_TRANSITION`. Todos los bultos de la entrega se validan (`HANDLING_UNIT_STATE_CONFLICT`, `NO_HANDLING_UNITS`).
- **Idempotencia y concurrencia:** el índice idempotente es `(organización, cuenta, operationId)` sin `ActionCode`; reutilizar la UUID en otra acción, entrega o cuerpo responde 409 `IDEMPOTENCY_CONFLICT` (nunca 23505/500), `Guid.Empty` responde 422 `INVALID_OPERATION_ID` y el replay de un hash legado valida el `deliveryId` de la respuesta antes de aceptar (el cuerpo histórico no se puede revalidar). El bloqueo de organización (`LockOrganizationAsync`) más la transacción serializa preparación, despacho y recepciones concurrentes.
- **Compatibilidad legada:** las entregas anteriores al 15-oct usaban `Delivered` como recepción final; con cualquier `Receipt` previo, una clave nueva responde `ALREADY_RECEIVED` y no repite efectos. El bulto `Prepared` se acepta como alias de `Sealed` y se normaliza en la siguiente transición.
- **Traza pública:** `PublicTraceService` deriva hitos persistidos: `Preparada`, `Despachada`, `En ruta`, `Entregada`, `Recibida`/`Recibida Parcial`; los códigos estables de error se documentan en `FieldErrorCodes`.
- **Demo:** `POST /api/v1/demo/route-plans/{id}/advance` es una **simulación de recorrido** que igualmente ejecuta preparación, despacho, recogida, entrega y recepción reales con sus guardas de estado, atribuidas a la cuenta de demo que la invoca (no al conductor real); es idempotente por `operationId` y ya no marca paradas con horas planificadas.

## Asistente de pedidos: orden de entregas y ventanas (#197 — 16 de octubre de 2026)

`POST /api/v1/route-plans/suggest` conserva la forma de su respuesta (`options[].stops`, `evaluation`, `sourcingLines`, `vehicleMatches`); solo cambia cómo se arman las paradas de entrega:

- **Orden:** las recogidas en bodega siguen primero. Las entregas se ordenan por `DeliveryOrder.Priority` (`Critical` → `High` → `Normal` → `Low`; un valor desconocido cuenta como `Normal`) y luego por el día de `RequiredDateUtc`. Dentro de cada nivel se elige el destino más cercano a la parada anterior, empezando en la última recogida. Si varios pedidos van al mismo destino, manda el más urgente y la fecha más temprana. `sequence` se renumera en ese orden.
- **Ventanas:** `windowStart`, `windowEnd` y `serviceMinutes` de cada entrega salen de `WindowStartLocal`, `WindowEndLocal` y `ServiceMinutes` del punto de servicio. Si el punto no los tiene, o el destino es una bodega, se usan 08:00–17:00 y 15 min, los valores fijos anteriores.
- **Fuera de alcance:** validar capacidad por prioridad en `/suggest` y `/confirm` es el issue #201.

## Consultas de abastecimiento para planificación y agente (#194 — implementado y verificado localmente, 16 de octubre de 2026)

El issue #194 expone tres consultas de solo lectura logística bajo `/api/v1/logistics` (`LogisticsQueriesController`), reutilizando `SourcingService` y `VehicleMatcher` sin tocar `RoutePlansController` (reservado a #201).

- **Autorización común:** `[Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]` (`Gerencia`, `Planificador` o `Automatizacion` mediante JWT M2M con scope `istpetdev-api/automation`). Cualquier otro rol recibe `403 Forbidden`; sin token o con cliente/scope/cuenta inválida recibe `401`/`403`.
- **Naturaleza de solo lectura logística y límite de frescura:** ninguna de las tres rutas crea pedidos (`DeliveryOrder`), planes (`RoutePlan`), reservas (`RouteStockReservation`), entregas (`Delivery`) ni snapshots de riesgo (`RiskSnapshot`). La cabecera `X-Agent-Run-Id` solo añade trazas de herramienta en `AgentRun.ToolCallsJson` cuando el actor es `Automatizacion`. Los datos reflejan una lectura puntual sin reserva: una publicación o despacho posterior puede consumir stock o asignar un vehículo después de consultar.

| Método y ruta | Entrada | Respuesta 200 |
|---|---|---|
| `GET /api/v1/logistics/stock` | `productId` obligatorio (`Guid`); `warehouseId` opcional (`Guid`) | `{ operationalDate, safetyStockPct, productId, sku, productName, baseUnit, warehouses: [...] }` |
| `POST /api/v1/logistics/sourcing` | Cuerpo JSON con exactamente uno de `orderIds` o `servicePointIds` (1–100 UUIDs únicos tras deduplicar); `productId` opcional solo con `servicePointIds` | `{ operationalDate, sourceKind, safetyStockPct, demands: [...], options: [...] }` |
| `GET /api/v1/logistics/vehicles/available` | `date` opcional (`yyyy-MM-dd`); por defecto fecha del reloj operativo (`IOperationalClock`) | `{ operationalDate, date, timeZone, dayStartUtc, dayEndUtc, vehicles: [...] }` |

### Reglas de cálculo, unidades y validación (#194)

1. **Stock por bodega (`GET /api/v1/logistics/stock`):**
   - Solo se consideran abastecibles los lotes no bloqueados (`!IsBlocked`) y no vencidos (`ExpiresOn == null || ExpiresOn > operationalDate`).
   - Por cada bodega activa y producto (cantidades en `baseUnit` del producto): `netUsable = sum(max(0, onHand - reserved))` sobre sus lotes utilizables; `safetyStock = round(netUsable * WarehouseSafetyStockPct / 100, 3)`; `availableQuantity = max(0, netUsable - safetyStock)`.
   - El stock de seguridad se aplica **una sola vez por bodega y producto** (nunca por lote ni por pedido). Las bodegas activas sin saldo aparecen con cantidades en `0` y lista de lotes vacía. Lotes ordenados por FEFO (`ExpiresOn` ascendente, `null` al final) y desempate por `lotCode` y `lotId`.
2. **Origen sugerido (`POST /api/v1/logistics/sourcing`):**
   - Usa `POST` únicamente para recibir listas de UUIDs en el cuerpo JSON; no persiste entidades logísticas.
   - Modo `orderIds`: valida que todos los pedidos existan en la organización y reutiliza `SourcingService` conservando `orderId`, `orderNumber`, destino y producto.
   - Modo `servicePointIds`: calcula riesgos en memoria con `IRiskCalculator.CalculateAsync(persistSnapshots: false)`, filtra los puntos solicitados (y `productId` si se indicó) con nivel `Critico`, `Alto` o `Vigilar` y `SuggestedQuantity > 0`, manteniendo múltiples productos por punto y orden de urgencia (`Critico` → `Alto` → `Vigilar`, menor `DaysCover`, mayor `SuggestedQuantity`, código de punto y SKU) sin crear pedidos de emergencia. En las líneas devueltas, `orderId` es `null`.
   - En ambos modos, el presupuesto asignable por `(bodega, producto)` descuenta el stock de seguridad una sola vez y se comparte entre todas las demandas de la consulta, evitando sobreasignar un lote o bodega cuando varias demandas compiten. Distancias en kilómetros (`distanceKm`) y duraciones en minutos (`durationMin`) provienen de `IDistanceProvider` con clave redondeada a 6 decimales. Si el stock no alcanza, devuelve `200 OK` con asignación parcial, `shortageQuantity > 0` y `fullySourced: false`.
3. **Vehículos disponibles (`GET /api/v1/logistics/vehicles/available`):**
   - Resuelve la fecha consultada (`date` o `IOperationalClock.TodayAsync`) en la zona horaria operativa de la organización (`America/Guayaquil`, respaldo UTC-5) y traduce el intervalo local `[00:00:00, día siguiente 00:00:00)` a `[dayStartUtc, dayEndUtc)`.
   - Excluye vehículos inactivos y aquellos con al menos un `RoutePlan` en estado `Published` cuyo `PlannedStartUtc` caiga en `[dayStartUtc, dayEndUtc)`. Planes en `Proposed`, `Cancelled` o `Superseded` no bloquean disponibilidad.
   - Devuelve `vehicleId`, `code`, `maxWeight` (`kg`), `maxVolume` (`m³`, nullable), `depotLocationId`, `depotCode`, `depotName`, `costPerKm` efectivo (`Vehicle.CostPerKm ?? DefaultCostPerKm`), `costUnit: "USD/km"` y `estimatedTotalCost: null` (sin ruta no existe cotización de viaje).
4. **Errores Problem Details (`code` estable en extensiones):**
   - `400 Bad Request` (`INVALID_QUERY_PARAMETER`, `INVALID_REQUEST_BODY`): formato de UUID o fecha `yyyy-MM-dd` inválido, o cuerpo JSON mal formado.
   - `422 Unprocessable Entity` (`VALIDATION_ERROR`): falta `productId` o es `Guid.Empty`; en `/sourcing`, enviar ambos `orderIds` y `servicePointIds` o ninguno, listas vacías, `Guid.Empty`, `productId` combinado con `orderIds`, o más de 100 IDs únicos tras deduplicar.
   - `404 Not Found` (`PRODUCT_NOT_FOUND`, `WAREHOUSE_NOT_FOUND`, `ORDER_NOT_FOUND`, `SERVICE_POINT_NOT_FOUND`): algún ID solicitado no existe en la organización o la ubicación indicada no es una bodega activa.

### Ejemplos de contrato (#194)

```json
// GET /api/v1/logistics/stock?productId=01963b7e-0000-7000-8000-000000000201
{
  "operationalDate": "2026-04-01",
  "safetyStockPct": 10.0,
  "productId": "01963b7e-0000-7000-8000-000000000201",
  "sku": "CLOR-01",
  "productName": "Hipoclorito de sodio 5%",
  "baseUnit": "L",
  "warehouses": [
    {
      "warehouseId": "01963b7e-0000-7000-8000-000000000101",
      "warehouseCode": "BOD-CEN",
      "warehouseName": "Bodega Central Quito",
      "onHandTotal": 120.000,
      "reservedTotal": 20.000,
      "usableBeforeSafetyTotal": 100.000,
      "safetyStockQuantity": 10.000,
      "availableQuantity": 90.000,
      "lots": [
        {
          "lotId": "01963b7e-0000-7000-8000-000000000301",
          "lotCode": "LOT-CLOR-2026A",
          "expiresOn": "2026-06-15",
          "onHandQuantity": 120.000,
          "reservedQuantity": 20.000,
          "netQuantity": 100.000
        }
      ]
    }
  ]
}
```

```json
// POST /api/v1/logistics/sourcing
// Request: { "servicePointIds": ["01963b7e-0000-7000-8000-000000000401"] }
{
  "operationalDate": "2026-04-01",
  "sourceKind": "servicePoints",
  "safetyStockPct": 10.0,
  "demands": [
    {
      "demandIndex": 0,
      "orderId": null,
      "orderNumber": null,
      "servicePointId": "01963b7e-0000-7000-8000-000000000401",
      "servicePointCode": "HOS-N01",
      "servicePointName": "Hospital del Norte",
      "productId": "01963b7e-0000-7000-8000-000000000202",
      "sku": "DES-01",
      "productName": "Desinfectante hospitalario",
      "baseUnit": "L",
      "requestedQuantity": 18.000,
      "allocatedQuantity": 18.000,
      "shortageQuantity": 0.000,
      "fullySourced": true,
      "riskLevel": "Alto",
      "daysCover": 2.40,
      "suggestedWarehouseId": "01963b7e-0000-7000-8000-000000000101",
      "suggestedWarehouseCode": "BOD-CEN",
      "allocations": [
        {
          "warehouseId": "01963b7e-0000-7000-8000-000000000101",
          "warehouseCode": "BOD-CEN",
          "warehouseName": "Bodega Central Quito",
          "lotId": "01963b7e-0000-7000-8000-000000000305",
          "lotCode": "LOT-DES-01",
          "expiresOn": "2026-05-20",
          "quantity": 18.000,
          "distanceKm": 6.42,
          "durationMin": 12.8
        }
      ]
    }
  ],
  "options": [
    {
      "warehouseId": "01963b7e-0000-7000-8000-000000000101",
      "warehouseCode": "BOD-CEN",
      "warehouseName": "Bodega Central Quito",
      "lines": [
        {
          "orderId": null,
          "servicePointId": "01963b7e-0000-7000-8000-000000000401",
          "productId": "01963b7e-0000-7000-8000-000000000202",
          "sku": "DES-01",
          "lotId": "01963b7e-0000-7000-8000-000000000305",
          "lotCode": "LOT-DES-01",
          "quantity": 18.000,
          "distanceKm": 6.42,
          "durationMin": 12.8
        }
      ],
      "totalDistanceKm": 6.42
    }
  ]
}
```

```json
// GET /api/v1/logistics/vehicles/available?date=2026-04-01
{
  "operationalDate": "2026-04-01",
  "date": "2026-04-01",
  "timeZone": "America/Guayaquil",
  "dayStartUtc": "2026-04-01T05:00:00Z",
  "dayEndUtc": "2026-04-02T05:00:00Z",
  "vehicles": [
    {
      "vehicleId": "01963b7e-0000-7000-8000-000000000501",
      "code": "VEH-01",
      "maxWeight": 1200.000,
      "maxVolume": 8.000,
      "depotLocationId": "01963b7e-0000-7000-8000-000000000101",
      "depotCode": "BOD-CEN",
      "depotName": "Bodega Central Quito",
      "costPerKm": 0.85,
      "costUnit": "USD/km",
      "estimatedTotalCost": null
    }
  ]
}
```
