# Revisión de seguridad de endpoints — #286

Corte revisado: `origin/develop` en `33387ca`, 16-oct-2026, America/Guayaquil.
Ventana: desde el 16-oct a las 17:00 UTC-05:00 hasta ese corte.

```bash
git log origin/develop --since="2026-10-16 17:00:00 -0500" -- src/Api/Controllers src/Application
```

El historial devuelve 12 commits sin merges que modifican las zonas indicadas. Los cuatro controladores cambiados son Incidents, Receipts, ReturnGuides y RoutePlans. Se siguieron sus servicios y los consumidores de los cambios en Field, Imports, RiskCalculator y Sourcing. No se modificaron PWA, contratos de retorno ni scripts del ensayo #285.

## Hallazgos y estado

Rutas relativas a `/api/v1`. G/P = Gerencia/Planificador; A = Automatizacion; B = Bodega; C = Conductor; R = Receptor; Au = Auditor. `Any` se mantiene únicamente en lecturas. Se contrastó RBAC con la advertencia ADR-21 de `RBAC y configuracion del sistema.md`, los roles fijos vigentes en `Roles.cs` y ADR-24/26; el RBAC configurable descrito como propuesta no se presenta como implementado.

| Endpoint | Roles | Hallazgo / protección comprobada | Gravedad | Estado |
|---|---|---|---|---|
| POST `incidents` | G/P/A | Faltaba validar radio negativo, pares de coordenadas, rangos geográficos y tipo vacío; slowdown ya exige (0,1]. VehicleCode se compara exactamente dentro de organización. Reintento puede duplicar incidente/run. | Media | Validación corregida; replay pendiente #287 |
| GET `incidents/{id}` | Any | Evaluación no escribe; faltaban filtros de organización en paradas y puntos auxiliares. | Media | Corregido |
| GET `receipts/exceptions` | G/P/Au | Cálculo de retroceso podía guardar caché RoadMatrixEdge desde un GET. Faltaban filtros de organización en líneas, actores, evidencia y costo. | Media | Corregido: OSRM calcula sin añadir entidades ni llamar SaveChanges; mismos valores de costo |
| POST `return-guides/generate` | G/P/B/C | Transacción y clave natural guía/bulto/tipo de evento evitan duplicar custodia; no se exige nuevo OperationId al cliente. C no verifica asignación de ruta. | Media | Idempotencia existente verificada; ámbito por conductor pendiente #287 |
| GET `return-guides/{guideNumber}` | G/P/B/C/Au | Proyección pura; formato exacto GRT + ocho hexadecimales; colisión devuelve 409; filtros OrganizationId. Ámbito del conductor solo por rol. | Media | #280 verificado; ámbito pendiente #287 |
| POST `return-guides/receive` | G/B | OperationId + FindReplayAsync/fingerprint + transacción serializada por organización. Mismo payload repite respuesta; nueva operación sobre guía recibida devuelve 409. Retorno parcial usa cantidad completa del bulto y saldo se limita a cero. | Alta | Duplicados protegidos por #280; conciliación parcial pendiente #287 |
| GET `route-plans` | Any | Consultas auxiliares sin organización; límite sin cota. | Media | Corregido: filtros y límite 1–100 |
| GET `route-plans/{id}` | Any | Lectura pura; faltaban filtros en paradas, vehículo, ubicación y cargas de pedidos. | Media | Corregido |
| GET `route-plans/{id}/geometry` | Any | Solo caché en memoria; faltaban filtros en vehículo, paradas, puntos y ubicaciones. | Media | Corregido |
| POST `route-plans` | G/P/A | Crea propuestas; no mueve existencias ni dinero. Filtro auxiliar de capacidades incompleto; reintentos generan revisiones. | Baja | Filtro corregido; replay persistente pendiente #287 |
| POST `route-plans/{id}/publish` | G/P | Reserva stock; replay por estado Published. Filtro auxiliar de organización incompleto; check de estado no serializa carrera entre solicitudes. | Alta | Filtros corregidos; idempotencia concurrente pendiente #287 |
| PUT `route-plans/{id}/driver` | G/P | Asignación explícita por id; conductor activo y organización verificados; faltaba organización en subconsulta de paradas. | Media | Corregido |
| POST `route-plans/{id}/stops/{sequence}/complete` | G/P/C | Conductor requiere entrega asignada; transición Completed se ignora si ya completada. No mueve stock/dinero; organización filtrada. | Sin hallazgo material | Revisado |
| POST `route-plans/{id}/replan` | G/P/A | Lock de ruta + conflicto si ya tiene revisión; transacción. Faltaban filtros auxiliares de vehículo/decisión. Puede publicar por política. | Media | Filtros corregidos; publicación subyacente incluida en #287 |
| POST `route-plans/{id}/revert` | G/P | Replay secuencial por revisión restaurada; libera reservas. Faltaban filtros en revisión, entregas, saldos, reservas y paradas. | Alta | Filtros corregidos; concurrencia/replay persistente pendiente #287 |
| POST `route-plans/{id}/cancel` | G/P | Estado Cancelled evita descontar dos veces secuencialmente. Faltaban filtros en saldos, productos y subconsulta de paradas. | Alta | Filtros corregidos; carreras cruzadas pendientes #287 |
| POST `route-plans/suggest` | G/P/A | No mueve stock/custodia/dinero. Organización filtrada en sourcing; caché estática por RequestId sin fingerprint ni caducidad. | Media | Pendiente #287 |
| POST `route-plans/evaluate` | G/P/A | Faltaba filtro de organización del vehículo y rechazo de cargas/servicio negativos. DeltaWeight/DeltaVolume son firmados por contrato y se conservan. | Media | Corregido |
| POST `route-plans/confirm` | G/P/A | Negativos podían convertirse en pedido completo. ProposalId opcional y replay por Contains(InputJson); cambia planificación, sin reserva física hasta publish. | Media | Negativos corregidos; replay exacto/concurrente pendiente #287 |
| POST `route-plans/emergency` | G/P/A | Crea pedidos; no mueve existencias/custodia/dinero; no tiene clave persistente de reintento. | Media | Pendiente #287 |
| POST `route-plans/manual` | G/P/A | Aceptaba distancia/duración negativas. Crea propuestas sin replay. | Media | Negativos corregidos; replay pendiente #287 |
| GET `risks` (impacto indirecto #199) | Any | CalculateAsync(false) no persiste snapshots; faltaban filtros de organización en lotes/líneas en camino. | Media | Corregido |
| GET `logistics/stock`, `logistics/vehicles/available` (impacto indirecto #241/#244) | G/P/A | Consultas puras, ids/fecha validados y consultas de organización; códigos exactos. | Sin hallazgo material | Revisado |
| POST `logistics/sourcing` (impacto indirecto #241) | G/P/A | Cálculo sin movimientos, ids validados. Puede cachear OSRM en un POST; no requiere OperationId por movimiento. | Sin hallazgo material | Revisado |
| POST `receiver/handling-units/{id}/receipt` (impacto indirecto #236/#237) | R/G | QR vigente, validación de cantidades y motivo, OperationId/fingerprint, serialización; negativo rechazado. | Sin hallazgo material | Revisado con suites existentes |
| GET `deliveries/rejection-reasons` (impacto indirecto #236) | Autenticado | Catálogo puro en memoria; sin consulta ni mutación. | Sin hallazgo material | Revisado |
| POST `imports` (impacto indirecto #240/#244) | G/P; A solo dry-run | Validación de números/restricciones en RowParser y upsert por claves canónicas bajo lock de organización; no movimiento aditivo de stock. | Sin hallazgo material en cambios revisados | Revisado con suites existentes; no es auditoría completa del controlador histórico |

No hay endpoints de movimiento de dinero en este corte: los costos son estimaciones. El aislamiento corresponde a la organización fija de la demo, no a una implementación completa de multitenencia/RLS.

## Evidencia y límites

- `MergedEndpointsSecurityTests`: GET reales de los cuatro controladores, interceptando intentos de SaveChanges y con OSRM simulado exitoso; snapshots de inventario/custodia/rutas/caché; entradas negativas y coordenadas; Auditor/Receptor denegados en todas las escrituras; paginación acotada.
- `MergedEndpointsIsolationTests`: relaciones de otra organización sembradas expresamente en EF InMemory (sin restricciones FK), para comprobar que los filtros auxiliares excluyen líneas, actores, evidencia, paradas, vehículos, lotes, planificación y saldos ajenos. No sustituye una prueba de RLS.
- `ReturnGuideHttpTests`: GET por todos los roles autorizados sin mutación; códigos inválidos/ambiguos; replay idéntico y cambiado; dos solicitudes concurrentes con igual y distinto OperationId; generación sin duplicar eventos. PostgreSQL/PostGIS desechable.
- Suites existentes de Field, Imports, Logistics y autorización verifican el flujo válido y la normativa de rechazo.

Aviso previo a editar archivos compartidos y a Joseph: cuerpo inicial de #287, conforme a #253. No se cambian respuestas del recorrido válido, UI ni orden de demo; las entradas inválidas pasan a 400. Los cambios grandes quedan en el issue verde #287 para después del evento, sin bloquear la demo. Integrar solo después del ensayo #285, según coordinación del usuario.

Para #200: «roles por función, retorno con operación idempotente y trazabilidad de movimientos». No afirmar idempotencia concurrente universal mientras sigan pendientes las carreras de publicación/reversión de #287, ni presentar esta revisión local como ensayo en producción.

## Resultado local

- Build Release: correcto.
- `dotnet test tests/IstpetDev.Tests -c Release --filter 'FullyQualifiedName~MergedEndpoints|FullyQualifiedName~RiskPendingDeliveriesTests'`: **28/28**, sin omitidas.
- `dotnet test tests/IstpetDev.Tests -c Release --no-build --filter 'Category=E2E'`: **24/24**, sin omitidas.
- `npm run check`: correcto (lint, ausencia de login simulado, build API/shared/web/mobile y artefactos de Campo).
- La ejecución conjunta de Unit/Integration/E2E produjo **701/705 aprobadas**, con cuatro fallos por timeout/carrera en `AgentJourneyTests`. El grupo E2E separado pasa completo. CI separa Integración de Unit/E2E; se exige su resultado verde sobre el HEAD final. La concurrencia observada queda como evidencia adicional en #287.
