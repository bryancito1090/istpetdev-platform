# Issue #287 — concurrencia y conciliación

Base: `origin/develop` (`5352336`). Rama: `chore/287-concurrencia-conciliacion`.
Entorno: pruebas locales con PostgreSQL/PostGIS descartable, sin datos reales ni despliegue.

## Implementación

- `RetryableOperationAttribute` persiste en `IdempotencyRecord` la respuesta y el fingerprint de las operaciones reintentables: creación de planes, publicación, cancelación, reversión, confirmación, rutas manuales/emergencia e incidentes. Acepta `operationId` en el cuerpo o `Idempotency-Key`; una clave reutilizada con otro contenido responde 409 `IDEMPOTENCY_CONFLICT`.
- `OrganizationTransaction.BeginAsync` serializa las operaciones por organización con el bloqueo existente y lo reutilizan publicación, cancelación, reversión, replanificación y custodia.
- Se elimina la caché estática de `route-plans/suggest`: `RequestId` se conserva por compatibilidad, pero ya no se reutilizan propuestas posiblemente obsoletas.
- Las guías de retorno verifican que el conductor esté asignado a la ruta; un conductor ajeno recibe 403. Los rechazos parciales se distribuyen una sola vez entre bultos, se reciben en una ubicación `Quarantine` y generan una demanda por destino original.
- La recepción rechaza con 409 `InsufficientVehicleStock` si la cantidad supera el saldo del vehículo, sin mover stock ni crear pedidos.
- Los contratos nuevos son aditivos: `ReturnGuideLineDto` agrega destino y disposición de stock; `ReceiveReturnGuideResponse` agrega la lista de pedidos de rebalanceo.

## Verificación local

| Control | Resultado |
|---|---|
| `dotnet build IstpetDev.sln -c Debug` | OK, 0 warnings |
| `Category=Integration` | 365/365 |
| `Category=Unit\|Category=E2E` | 371/371 |
| `OperationIdempotencyTests` | confirmación y publicación concurrentes con una sola creación/reserva; cambio de payload con la misma clave responde 409 |
| `PartialReturnReconciliationTests` | cuarentena, saldo del vehículo, demanda por destino y rechazo por saldo insuficiente |
| `ReturnGuideHttpTests` | conductor asignado puede leer; conductor ajeno recibe 403 y no se escriben datos |

La comparación de `AgentJourney` bajo una sola ejecución con todas las categorías no se repitió: el CI separa Unit, Integration y E2E y las tres terminaron en verde. No se ejecutaron operaciones cloud ni se modificó RDS.

## Coordinación

Los cambios visibles se documentan en el PR para revisión de Joseph: nuevos códigos 403/409, campos aditivos de guía de retorno y la ubicación de cuarentena. El issue se cierra cuando el PR queda integrado; la coordinación de producto queda registrada en la revisión solicitada.
