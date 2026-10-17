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
| POST /api/v1/route-plans                     | Calcular propuesta, 202 si es asíncrono; `criticalDeadlineLocal` opcional (#193) |
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

## Carga parcial según capacidad (#199 — 16 de octubre de 2026)

Cuando la carga sugerida no cabe en peso o volumen, el motor planifica lo factible y conserva el remanente pendiente. Detalle en el FR-24 del spec 002 (`docs/specs/002-motor-logistico/spec.md`) y en su `plan.md`.

- **`POST /api/v1/route-plans`** (lo consume n8n con `propose_routes`): cada ruta trae `vehicleCode`, `loadWeight`, `loadVolume`, `maxWeight`, `maxVolume` y `stops[].loads[]` con `quantity` (planificada; es lo que se publica y reserva), `requiredQuantity` y `pendingQuantity`. Cada no atendido trae `lines[]` con `requiredQuantity`, `plannedQuantity` y `pendingQuantity`. Una entrega parcial es `reason: "capacity"` con `plannedQuantity > 0`. Los hechos `required_quantity`, `planned_quantity` y `pending_quantity` (sujeto `PUNTO/SKU`) permiten explicarla sin inventar cifras.
- **`POST /api/v1/route-plans/confirm`:** mantiene la validación de #201: vehículo obligatorio y 409 si la ruta no cabe; la parcial se pide con `lines`. Con #199:
  - La cantidad de cada línea no puede superar lo pendiente (`RequestedQuantity − PlannedQuantity − DeliveredQuantity`); si lo supera, 409.
  - Lo planificado se suma a `PlannedQuantity`.
  - Un pedido sin nada pendiente no genera otra ruta: 409.
  - La respuesta añade `lines[]` (requerida, planificada y pendiente), `loadWeight`, `loadVolume`, `maxWeight`, `maxVolume` y `partial`.
- **`POST /api/v1/route-plans/suggest`:** ofrece solo el remanente sin planificar de cada línea.
- **`GET /api/v1/route-plans/{id}`:** cada parada añade `loadQuantities[]` con las tres cantidades por SKU (rutas del motor).

## Consultas de abastecimiento para planificación y agente (#194 — 16 de octubre de 2026)

Las tres consultas usan `LogisticsQueriesController`, `SourcingService` y
`VehicleMatcher`. Acceso común: `Roles.Planning` (Gerencia/Planificador) o
`Roles.Automatizacion`; la identidad M2M exige `istpetdev-api/automation` y cuenta
activa con rol técnico. No existe un rol literal llamado Planning.

No crean pedidos, planes, reservas, movimientos, entregas ni snapshots de riesgo.
`X-Agent-Run-Id` registra la llamada en la traza de AgentRun solo para
Automatizacion. La disponibilidad es puntual: consultar no garantiza ni reserva
stock o vehículos para una publicación posterior.

| Método y ruta | Entrada | Respuesta 200 |
|---|---|---|
| `GET /api/v1/logistics/stock` | `productId` obligatorio (UUID), `warehouseId` opcional | `WarehouseStockQueryResult` |
| `POST /api/v1/logistics/sourcing` | exactamente una lista `orderIds` o `servicePointIds` (1–100 UUIDs únicos); `productId` opcional solo para puntos | `DemandSourcingResult` |
| `GET /api/v1/logistics/vehicles/available` | `date` opcional ISO `yyyy-MM-dd`; por defecto reloj operativo Dataset/Fixed/Real | `AvailableVehiclesResult` |

### Stock por producto y bodega

`WarehouseStockQueryResult` contiene `operationalDate`, `safetyStockPct`,
`productId`, `sku`, `productName`, `baseUnit` y `warehouses[]`.
Cada bodega contiene `warehouseId`, `warehouseCode`, `warehouseName`,
`quantityOnHand`, `quantityReserved`, `netUsableQuantity`, `safetyStock`,
`availableQuantity` y `lots[]`. Cada lote contiene `lotId`, `lotCode`,
`expiresOn` (fecha ISO o null), `quantityOnHand`, `quantityReserved` y
`netUsableQuantity`.

Se incluyen solo lotes no bloqueados cuyo vencimiento es null o posterior a
`operationalDate`; vencer hoy ya los excluye. Las cantidades usan la unidad
base del producto. Por bodega y producto:

- `netUsableQuantity = sum(max(0, quantityOnHand - quantityReserved))` por lote.
- `safetyStock = netUsableQuantity * WarehouseSafetyStockPct / 100`, sin redondeo
  adicional en el cálculo.
- `availableQuantity = max(0, netUsableQuantity - safetyStock)`.

La seguridad se descuenta una vez del total; el neto de cada lote aún incluye
esa porción protegida. Lotes en orden FEFO, null al final, desempate por código
e ID. Las bodegas activas vacías aparecen con totales cero y `lots: []`.

### Origen sugerido

Ejemplos de selección (reemplazar los UUIDs por IDs de la API):

```json
{ "orderIds": ["00000000-0000-4000-8000-000000000001"] }
```

```json
{ "servicePointIds": ["00000000-0000-4000-8000-000000000002"], "productId": "00000000-0000-4000-8000-000000000003" }
```

Son ejemplos de forma; esos IDs no identifican registros del fixture ni datos
reales. El método POST permite listas en el cuerpo y mantiene la consulta sin
persistencia logística.

Para pedidos se ofrece solo el remanente `max(0, requested - planned)`, conforme
a #199; las líneas completas no se vuelven a ofrecer. La consulta y
`PlanSourcingAsync` comparten la carga de demandas y el mismo asignador.
Para puntos se usa `IRiskCalculator.CalculateAsync(persistSnapshots: false)`,
filtrando Critico/Alto/Vigilar con cantidad sugerida positiva. El orden es nivel
de riesgo, menor holgura (`coverageDays - leadTimeDays`), mayor criticidad del
punto, código del punto, SKU e IDs para desempate. No crea pedidos de emergencia.

`DemandSourcingResult` contiene `operationalDate`, `sourceKind` (`Order` o `Risk`),
`safetyStockPct`, `fullySourced`, `demands[]` y `options[]`. Sin demanda elegible
(o pedidos completamente planificados), devuelve ambas listas vacías.

Cada elemento de `demands` contiene:

- `orderId`, `orderNumber` (null cuando procede de riesgo), `sourceKind`.
- `destinationId`, `isDestinationWarehouse`, `destinationCode`, `destinationName`,
  `destinationPosition: { lat, lon }`.
- `productId`, `sku`, `productName`, `baseUnit`, `requestedQuantity` (remanente
  para pedidos), `allocatedQuantity`, `totalAllocated` (igual a allocatedQuantity),
  `shortageQuantity`, `fullySourced`, `riskLevel` y `coverageDays` (null para pedidos).
- `allocations[]`: `warehouseId`, `warehouseCode`, `warehouseName`, `distanceKm`,
  `warehousePosition: { lat, lon }`, `quantity` y `lots[]`.
- Cada lote asignado: `lotId`, `lotCode`, `expiresOn` nullable y `quantity`.

Cada opción contiene `optionId`, `title`, `description`, `isRecommended`,
`fullySourced`, `lines[]` (misma estructura que demands) e `involvedWarehouseIds[]`.
Se conserva A (consolidada), y B cuando alguna demanda se divide entre bodegas.
La descripción de una alternativa no garantiza horario, ruta factible o vehículo.

La asignación respeta FEFO y presupuesto compartido por bodega/producto, descontando
seguridad una vez entre todas las demandas. Ordena bodegas por distancia de
`IDistanceProvider`, con el respaldo aproximado existente si falta un tramo;
la consulta no calcula ni devuelve `durationMin`. Si falta stock responde 200
con asignación parcial, faltante y `fullySourced: false`.

### Vehículos disponibles

`AvailableVehiclesResult` contiene `operationalDate`, `date`, `timeZone`,
`dayStartUtc`, `dayEndUtc` y `vehicles[]`.
Cada vehículo contiene `vehicleId`, `code`, `maxWeight` (kg), `maxVolume` (m³,
nullable), `depotLocationId`, `depotCode`, `depotName`, `costPerKm`,
`costUnit: "USD/km"` y `estimatedTotalCost: null`.

El día consultado es el intervalo local desde medianoche hasta la siguiente,
convertido a UTC en la zona de la organización (por defecto America/Guayaquil;
respaldo configurado UTC-5 si no se resuelve la zona). Solo se incluyen activos
sin un RoutePlan Published que empiece en `[dayStartUtc, dayEndUtc)`.
Proposed/Cancelled/Superseded no bloquean. La tarifa efectiva es
`Vehicle.CostPerKm ?? DefaultCostPerKm`; sin recorrido no se cotiza costo total.

### Errores y autorización

- 400: UUID, fecha ISO o JSON mal formados (validación de modelo de ASP.NET),
  o `title: invalid_date` para la fecha validada por el controlador.
- 422 con `title` específico: `invalid_product_id`, `invalid_warehouse_id`,
  `invalid_sourcing_selector`, `product_filter_not_allowed`, `empty_order_ids`,
  `invalid_order_id`, `empty_service_point_ids`, `invalid_service_point_id` o
  `limit_exceeded`. Los límites se aplican después de deduplicar los IDs.
- 404 con `title`: `product_not_found`, `warehouse_not_found`, `order_not_found`
  o `service_point_not_found`. No acepta silenciosamente un subconjunto de IDs.
- 401 para anónimo/token inválido; 403 para cuenta o rol no autorizado.

Los errores de negocio usan Problem Details (`status`, `title`, `detail`);
este controlador no añade una extensión `code` ni garantiza códigos en mayúsculas.

Evidencia y regresiones: [validación de #194](../../specs/003-agente-logistico/evidencia/2026-10-16-194-consultas-abastecimiento.md).

## Hora límite para puntos críticos en `POST /api/v1/route-plans` (#193 — 16 de octubre de 2026)

Responde AMP-01 de [[Ampliacion de alcance - ideas del evento]]: la prioridad ya decidía quién entra al camión, pero no a qué hora llega.

- **Campo nuevo, opcional:** `criticalDeadlineLocal` en el cuerpo (`"HH:mm"`, p. ej. `"11:00"`, hora local). Si se omite o es `null`, el plan es el mismo de antes.
- **A quién aplica:** a las paradas con riesgo `CRITICO` o con criticidad del punto `alta`. Su fin de ventana pasa a ser el mínimo entre la ventana del punto y la hora límite. Si la ventana del punto empieza después de la hora límite, se respeta la del punto. Las demás paradas conservan su ventana.
- **Resultado:** la parada crítica llega antes de la hora límite o queda en `unserved` con motivo `time_window`; no hay motivo nuevo.
- **Error:** un valor que no sea `HH:mm` o `HH:mm:ss` responde 400 `invalid_critical_deadline`.
- **Alcance:** solo la propuesta inicial. Replanificar no recibe hora límite.

## Evidencia determinista del agente y herramientas de abastecimiento (#195, en revisión)

El issue #195 agrega tres herramientas `toolWorkflow` a `vigia`/`incidente`
(prompt v2): `find_stock`, `plan_sourcing` y `vehicle_availability`. Son
consumidores de las consultas de solo lectura `/api/v1/logistics/*` de #194; su
contrato final se rige por el PR de #194 y no se duplica aquí. Los wrappers
validan UUIDs, listas acotadas a 100 únicas y campos de allowlist, y envían el
`X-Agent-Run-Id` firmado en los ocho tools.

`GET /api/v1/agent/runs/{id}` (y `GET /api/v1/agent/runs?limit=`) amplía
`AgentDecisionView` con evidencia derivada por servidor del plan correlacionado,
sin aceptar valores del callback LLM. Al completar la ejecución se conserva una
copia en `ProposalJson.evidence` (vehículo, bodega y paradas), también en
el respaldo; para decisiones históricas sin copia guardada se conserva la proyección
compatible desde el plan. Una renombración posterior no altera nuevas decisiones:

| Campo nuevo | Origen determinista | Uso |
|---|---|---|
| `originWarehouseCode` / `originWarehouseName` | `RoutePlan.VehicleId → Vehicle.DepotLocationId → Location` (bodega base real) | Acredita la bodega de origen de la propuesta |
| `orderedStops: [{ sequence, pointCode }]` | `RouteStop` del plan propuesto, ordenado por `SequenceNumber` (en propuestas de fallback, del `ProposalJson.route` persistido) | Acredita la secuencia real de visita, distinta de la prioridad de asignación del motor |
| `vehicleCode` (ya existía) | Plan/`ProposalJson` → `Vehicle.Code` | Acredita el vehículo |

No se expone el `summary` del LLM ni respuestas HTTP completas de herramientas;
la traza de llamadas sigue registrando método, ruta, código y milisegundos.
Compatible hacia atrás: los campos nuevos son opcionales y las decisiones sin
plan correlacionado pueden devolverlos nulos. Ensayo local y de n8n real en
`docs/specs/003-agente-logistico/evidencia/2026-10-16-195-herramientas-n8n.md`.


## Revisión de viabilidad de datasets (#209 — 16 de octubre de 2026)

Amplía el reporte del importador del spec 001 sin cambiar su validación estricta
(ADR-22: la API calcula y decide; n8n coordina y explica).

- **`POST /api/v1/imports`** devuelve en el reporte `status`
  (`Applied`/`DryRun`/`Rejected`) y `feasibility`, calculada en el plan en
  memoria del dry-run sin persistir datos operativos. La sección contiene
  `verdict` (`Viable`/`ViableWithLimitations`/`Inviable`) y las listas
  `unroutablePoints` (código, nombre y fila de puntos sin GPS),
  `warehousesWithoutFleet`, `productsWithoutCapacity` (motivo
  `weight`/`volume`/`both`) y `stockShortages` (demanda del dataset vs. stock
  físico neto usable de bodegas, sin lotes bloqueados ni vencidos).
- **Veredicto determinista:** `Inviable` si hay déficit de stock, si hay demanda
  y no existe vehículo activo con `maxWeight > 0`, o si todos los puntos
  involucrados carecen de coordenadas; `ViableWithLimitations` si hay hallazgos
  sin déficit; `Viable` sin hallazgos.
- **Autorización M2M acotada:** `Roles.Automatizacion` puede ejecutar
  `POST /api/v1/imports` **solo** con `dryRun=true&allowPartial=false`; aplicar
  o descartar filas sigue siendo exclusivo de `Roles.Planning` (403 en caso
  contrario). Los GET de `data/{entity}` siguen siendo humanos.
- **n8n:** el workflow `revision-dataset` (webhook `istpetdev/dataset/review`)
  ejecuta el dry-run con su credencial M2M, arma el dictamen con las secciones
  de viabilidad, balance, puntos sin GPS, stock vs demanda y acciones separadas
  en seguras/humanas, y usa plantilla determinista si el LLM falla o inventa
  cifras. Nunca envía `allowPartial=true`.

## Costo estimado de servir una ruta (#240 — 16 de octubre de 2026)

`RouteEvaluationResult` (`POST /api/v1/route-plans/evaluate`, y `evaluation` de
`/suggest`) y `VehicleMatchResult` agregan `serviceCostBreakdown`:

| Campo | Unidad / significado |
|---|---|
| `fuel`, `wear`, `driver`, `helpers`, `shrink` | USD estimados por componente; `helpers` es costo |
| `total` | USD; coincide con `totalCost`/`estimatedTotalCost` del resultado |
| `loadPct` | Peso máximo simultáneo / capacidad × 100; no se limita al 100 |
| `helperCount` | Número de ayudantes de la ruta, además del conductor |
| `fuelPrice` | `{value, date, source, unit: "USD/gal"}`; fecha ISO y fuente de servidor |
| `notes` | Supuestos, datos faltantes y fallback; siempre aclara que son estimaciones sintéticas |
| `legacyTransport` | USD por km del cálculo anterior, solo en fallback; no se presenta como combustible |
| `label`, `currency` | `"estimado"`, `"USD"` |

La configuración se enlaza desde `Logistics:ServiceCost` a `ServiceCostOptions`.
El consumo usa la fracción de carga (`loadPct / 100`) y galón = 3,785 litros.
Los componentes se redondean a centavos y luego se suman. La tasa de merma cambia
solo **por encima** del umbral (90% por defecto); el valor se cuenta en recogidas,
sin sumarlo otra vez en entregas. Las reglas de operarios se evalúan en orden
sobre cada parada (peso y máximo peso de un bulto); la ruta lleva el máximo de
ayudantes, y el tiempo de cada descarga usa esa cuadrilla completa.
Sin tiempo definido en el punto, se usa `ceil(10 + bultos / (1 + ayudantes))`
minutos; un tiempo explícito de cero se conserva. Sin bultos conocidos se conserva
el tiempo anterior y se indica una cuadrilla conservadora. Sin valor unitario,
la merma se marca parcial. El conteo de bultos redondea hacia arriba por SKU.

Sin ServiceCost, sin perfil del tipo de vehículo o con configuración incompleta
(fecha/fuente ausente, tasas negativas o reglas inválidas), se conservan el costo
**y los tiempos anteriores**. El desglose indica «perfil no configurado» y el
precio no informado o sin fecha/fuente viaja con los tres campos nulos; no se
inventa una cotización vigente.
Los defaults numéricos de ServiceCost son sintéticos, no precios medidos.

Importación: `products` admite `unit_value` (USD/unidad base, no negativo) y
`units_per_package` (positivo); `vehicles` admite `vehicle_type`
(`camioneta`, `camion_liviano`, `camion_pesado`) y `usage_scope`
(`urbano`, `extraurbano`). Campos ausentes/vacíos conservan valores anteriores;
se registra `*_preserved` en supuestos. La migración `ServiceCostCatalog` agrega
las cuatro columnas nullable, con escala decimal (18,3) del DemoCore actual.
No altera históricos ni significa que haya sido aplicada en producción.

Coordinación: `RoutePlansController` se conserva por #237/#245. Anthony debe
alinear su `costBreakdown` anterior y `/confirm` con este nuevo calculador si se
requiere presentar/persistir el desglose completo allí. `serviceCostBreakdown`
en los DTO es la fuente del costo de servir; el desglose legado de `/suggest`
sigue describiendo km/hora. La pantalla es #242; la calibración de Quito es #243.
La planificación general del motor y la calibración histórica (#204) mantienen
su contrato actual. #244/#245 rebasan después de #240.

## Hora pico por franja (#245 — 16 de octubre de 2026)

Una ruta que sale a las 07:00 no tarda lo mismo que una que sale a las 11:00. OSRM y la aproximación (`ApproxSpeedKmh`) no consideran el tráfico, así que el motor corrige la duración de cada tramo con un factor de velocidad por franja horaria.

- **Configuración:** `Logistics:TrafficBands`, lista de `{ "Start": "HH:mm", "End": "HH:mm", "SpeedFactor": 0.64 }` en hora local (`TrafficBandOptions`). `SpeedFactor` es la fracción de la velocidad normal: 0,5 = el tramo dura el doble. La franja incluye `Start` y excluye `End`. Una franja mal formada, con `End <= Start` o con factor fuera de (0, 1] se ignora; si dos se solapan, gana la más lenta. **Sin franjas (valor por defecto) las duraciones no cambian.** Los valores de la demo están en `appsettings.demo-quito.json` (#243) y son supuestos documentados en el README del fixture.
- **Regla:** la duración del tramo se divide entre el factor de la franja que contiene la **hora local de inicio del tramo** (no se reparte entre franjas). Se aplica en `RouteFeasibility` (propuesta, inserción, 2-opt y replanificación), en el cálculo de llegadas de `RoutePlanner` y en `RouteEvaluator` (`/evaluate` y `evaluation` de `/suggest`). Las ventanas, la espera, la jornada y el costo por hora usan la ETA corregida. Si además hay un incidente que ralentiza el tramo, ambos factores se multiplican.
- **`POST /api/v1/route-plans`:** cada `routes[].stops[]` agrega `trafficFactorApplied` (número o `null`): el factor aplicado al tramo que llega a esa parada; `null` fuera de hora pico. Se guarda en `InputJson.stops[]` del plan (también en las revisiones por incidente).
- **`GET /api/v1/route-plans/{id}`:** cada parada agrega `trafficFactorApplied` leído del plan guardado; `null` en planes anteriores, en rutas del asistente o fuera de hora pico.
- **`RouteEvaluationResult.stops[]`:** agrega `trafficFactorApplied` con la misma regla; `legDurationMinutes` ya viene corregido (redondeado a 0,1 min solo cuando hay factor).
- **Fuera de alcance:** `VehicleMatcher` estima la duración total para descartar por jornada sin horas por tramo; no aplica el factor (lo toca #244 en paralelo). `DeliveryStopSequencer` ordena por prioridad y distancia, sin duraciones, y no cambia.

## Motivo de rechazo «error de rotulado» (#236 — 16 de octubre de 2026)

`GET /api/v1/deliveries/rejection-reasons` responde `version: "V2"`. El catálogo
agrega `ErrorRotulado` («El rótulo o la identificación del empaque no coincide
con el producto o el despacho») al final de la lista; los seis códigos de V1 y
sus sinónimos no cambian. El servidor normaliza `error_rotulado`,
`error-rotulado`, `mislabeled` y `wrong_label` a `ErrorRotulado` al registrar la
recepción, y `GET /api/v1/receipts/exceptions` devuelve el código canónico en
`rejectionReason`. La forma de las respuestas no cambia y no hay migración: el
motivo ya se guarda como texto.

La PWA no lee este endpoint: usa la lista fija `RECEIPT_REJECTION_REASONS`
(versión `V1`) de `libs/shared-core/src/contracts/field.contract.ts`, y el
outbox valida el motivo contra esa lista. Hasta que se actualice (PWA de #230,
Bryan), el receptor no puede elegir `ErrorRotulado` desde el teléfono.

La página web de Novedades muestra arriba de la lista las líneas y unidades
rechazadas por motivo, calculadas en el cliente sobre las recepciones cargadas
(máximo `limit`), no sobre el total histórico.

## Incidentes con retraso parcial (#246 — 16 de octubre de 2026)

Un accidente, una congestión o una manifestación casi nunca cierran del todo la vía: la vuelven lenta. Antes un incidente solo podía bloquear un tramo (`BloqueoVial`/`ROAD_CLOSURE`) o demorarlo con un factor fijo (`DELAY`, minutos × 2).

- **Tipos nuevos de `POST /api/v1/incidents`:** `Congestion`, `Manifestacion` y `Accidente` (sin distinguir mayúsculas). Exigen el factor de velocidad en `slowdownFactor` (nombre de `incidentes.json`) o en `factor` (campo que ya existía); si llegan los dos, manda `slowdownFactor`. Debe estar en (0, 1]: 0,3 = la velocidad cae al 30 % y el tramo dura 1 / 0,3 veces más. Sin factor o fuera de rango responde 400 `invalid_slowdown_factor`. Los demás tipos ignoran el factor y se comportan como antes.
- **Respuesta:** `POST` y `GET /api/v1/incidents/{id}` agregan `slowdownFactor` (`null` en los demás tipos).
- **Dónde se guarda (sin migración):** la única migración de la noche era #240, así que el factor se agrega al final de `Incident.Description` como texto legible, p. ej. «Marcha desde la Av. 10 de Agosto… (velocidad al 30 %)», y se vuelve a leer de ahí (`IncidentSlowdown`). Es provisional: cuando se coordine una migración, pasa a una columna `SlowdownFactor` y la marca deja de escribirse.
- **Efecto:** `IncidentEvaluator` marca en `IncidentEffect.SlowFactors` los tramos dentro del radio, tanto de las rutas publicadas como de la caché de tramos, con el multiplicador de minutos 1 / factor. `IncidentEffect` ya tenía `SlowFactors` y `RouteFeasibility` ya lo aplicaba; se reutiliza en lugar de crear un `SlowedLegs` paralelo con la misma información. Las rutas afectadas se informan en `affectedRoutes` como antes.
- **Replanificación:** sin cambios en `Replanner`. Si la ETA corregida respeta las ventanas, la revisión se acepta según la política; si un obligatorio sale de su ventana, la revisión estricta lo deja como no atendido (`time_window`) y se escala con `late_service` y `prioritize_critical`.
- **Sin cambios:** `BloqueoVial`/`ROAD_CLOSURE` siguen bloqueando, `DELAY` sigue con minutos × 2 y `VEHICLE_BREAKDOWN` deja el vehículo no disponible. Un incidente de tipo nuevo guardado sin marca (por ejemplo, insertado a mano) se trata como `DELAY`.

