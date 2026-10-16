# Evidencia del issue #162 — estados del bulto y custodia Bodega → Conductor → Receptor

Fecha: 2026-10-15 (America/Guayaquil). Rama: `fix/162-estados-bulto-custodia` (desde `origin/develop`, base `dab3429`).
Entorno: worktree `/home/bryan/orca/workspaces/ISTPETDEV/fix-162-estados-bulto-custodia`; PostgreSQL/PostGIS efímero por Testcontainers; sin base compartida, sin cloud y sin despliegue.

> Este documento registra **pruebas locales**. No afirma ejecución física en teléfono ni despliegue. La revisión de prelaunch aplicada es de alcance local aislado (ver «Prelaunch y límites»).

## Alcance implementado

- Máquina de estados explícita del bulto y de la entrega: `Sealed` → `Dispatched` → `InTransit` → `Delivered` → `Received`/`Partial`, con responsables por turno (Bodega, Conductor asignado, Receptor con QR).
- Comandos nuevos `POST /api/v1/deliveries/{id}/pickup` y `POST /api/v1/deliveries/{id}/deliver` (Conductor asignado, idempotentes, solo custodia, sin movimiento de stock).
- Recepción blindada antes de cualquier efecto: `NOT_DISPATCHED`, `NOT_DELIVERED`, `ALREADY_RECEIVED`, validación de **todos** los bultos, replay idempotente tras avanzar el estado y exclusión mutua real por bloqueo de organización + transacción.
- `POST /api/v1/deliveries/{id}/receipts` (conductor) responde 409 `RECEIPT_REQUIRES_RECEIVER`; el recibo final lo registra el Receptor con el QR del bulto.
- PWA Conductor: «Esperando a Bodega» / «Marcar Recogido» / «Marcar Entregado» por parada, con comando en el outbox durable (`commandKind`), estados honestos y sin presentar la captura local como confirmación del servidor. Entrada directa a `/receipt/:deliveryId` restringida al rol Conductor.
- PWA Receptor: «Aún en bodega» / «El Conductor aún no entrega este bulto» / «Recibir» / «Ya recibido» según el estado real; solo habilita el guardado con el bulto `Delivered`.
- Traza pública con hitos reales: Preparada → Despachada → En ruta → Entregada → Recibida.
- Compatibilidad legada documentada: `Delivered` + recibo previo = recepción final (`ALREADY_RECEIVED`); bulto `Prepared` = alias de `Sealed`.
- Migración `20261015234714_CustodyChainStates`: quita la unicidad del índice `CustodyEvent(OrganizationId, OperationId)` porque una operación con varios bultos emite un evento por bulto (antes fallaba con `23505` al despachar más de un bulto).

## Archivos principales

| Capa | Archivos |
|---|---|
| Dominio | `src/Domain/Common/CustodyStates.cs` (nuevo) |
| Aplicación | `src/Application/Field/FieldService.cs`, `IFieldService.cs`, `Dtos/FieldContracts.cs`, `src/Application/PublicTrace/Services/PublicTraceService.cs` |
| API | `src/Api/Controllers/DeliveriesController.cs` |
| Persistencia | `LeanSchema.cs` (índice), migración `CustodyChainStates`, `AccountSeeder.cs` (deduplicación de cuentas demo) |
| Contratos | `libs/shared-core/src/contracts/field.contract.ts`, `apps/mobile/.../field-api.interface.ts`, `http-field-api.service.ts` |
| PWA | `apps/mobile/.../outbox/field-outbox.service.ts`, `pages/receipt/receipt.page.ts`, `pages/sync/sync.page.ts`, `home/home.page.ts`, `app.routes.ts`, mock `mock-field-api.service.ts` y fixture |
| Pruebas | `tests/IstpetDev.Tests/Integration/Field/CustodyChainHttpTests.cs` (nuevo), `E2E/DemoAdvanceTests.cs` (nuevo), ajustes en pruebas de campo, recepción y migraciones |
| Bóveda | `03 Arquitectura/Contratos API y eventos.md`, `03 Arquitectura/Movil offline y sincronizacion.md`, `02 Producto/Usuarios y flujos.md`, `00 Inicio/Hechos canonicos.md` |

## Criterios de aceptación del issue → evidencia

| Criterio | Evidencia |
|---|---|
| Cada rol solo actúa en su turno | `CustodyChainHttpTests.Full_journey_respects_each_role_turn_and_moves_stock_once`: receptor antes de despacho → 409; receptor antes de entrega → 409; conductor antes de despacho → 409; pickup/deliver solo por el conductor asignado |
| Traza Despachada → En ruta → Entregada → Recibida | Mismo test: `events` públicos = `["Preparada","Despachada","En ruta","Entregada","Recibida"]` |
| Conductor no recibe | `POST /deliveries/{id}/receipts` → 409 `RECEIPT_REQUIRES_RECEIVER`, sin movimientos ni recibo |
| Receptor no recibe bulto no entregado | `ReceiverReceiptHttpTests` + guardas del test de recorrido (`NOT_DISPATCHED`, `NOT_DELIVERED`) |
| Sin asignación no hay permiso | `Missing_or_foreign_assignment_forbids_driver_transitions` (403 `UNAUTHORIZED_ASSIGNMENT`, asignación nula o ajena) |
| Sin doble stock ni doble recibo | `Receivers_with_distinct_keys_cannot_duplicate_stock` (dos cuentas, claves distintas, uno aceptado y uno 409; 1 recibo, 12 L en punto, 0 en vehículo) |
| Replay idempotente tras avanzar el estado | Test de recorrido: repetir `operationId` del recibo devuelve 200 con el mismo cuerpo; clave nueva → `ALREADY_RECEIVED` |
| Parcial conserva rechazado en vehículo | `Partial_receipt_keeps_rejected_in_the_vehicle_and_blocks_fully_accounted_retries` (9 L punto, 3 L vehículo) |
| Estado legado con recibo | `Legacy_delivered_with_an_existing_receipt_is_already_received` (409 `ALREADY_RECEIVED`, sin movimiento) |
| Demo sin salto de custodia | `DemoAdvanceTests`: 2 paradas con recibos reales, 8+ eventos de custodia, replay idempotente de `advance` |

## Comandos y resultados (15-oct-2026)

```bash
dotnet build src/Api                                             # 0 errores, 0 avisos
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/IstpetDev.Tests -c Release
#   Passed! Failed: 0, Passed: 462, Skipped: 0, Total: 462  (2 m 45 s, PostGIS en Testcontainers)
npm run check                                                    # lint FSD + .NET + shared + web + mobile + PWA /campo: OK
npm run test:frontend                                            # shared 24/24, web 74/74, mobile 76/76
npm run audit:check                                              # 7 avisos autorizados por excepciones vigentes
python3 docs/boveda/.github/scripts/check_vault.py               # 0 errores, 4 avisos preexistentes (87 notas, 928 enlaces)
python3 -I scripts/db/check-additive-migrations.py               # «Migraciones aditivas: OK»
bash .github/scripts/branch-policy.sh pr fix/162-estados-bulto-custodia develop \
     bryancito1090/istpetdev-platform bryancito1090/istpetdev-platform
#   PR `fix/162-estados-bulto-custodia` → `develop` permitido.
```

Pruebas nuevas/afectadas en verde (extracto):

```bash
dotnet test --filter "FullyQualifiedName~CustodyChainHttpTests"   # 6/6
dotnet test --filter "FullyQualifiedName~DemoAdvanceTests"        # 1/1
dotnet test --filter "FullyQualifiedName~IstpetDev.Tests.Integration.Field"  # 26/26
```

## Commits

| Commit | Contenido |
|---|---|
| `2f4953f` | `feat(api): cadena de custodia por turnos y comandos del conductor` |
| `0c7c431` | `feat(mobile): recogida y entrega por turnos con esperas honestas` |
| (este commit) | `docs(campo): evidencia, contrato y seguimiento del issue 162` |

## Prelaunch y límites (alcance local aislado)

Se leyó la skill `docs/boveda/08 Skills/istpetdev-prelaunch.md` antes de usar el runtime de pruebas. No hizo falta generar `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md` porque no se levantó API expuesta ni navegador: la ejecución fue el host de pruebas .NET con PostgreSQL efímero y las compilaciones/tests de Angular.

- RLS: **no aplica** a la demo (ADR-21, núcleo sin RLS); se verificó autorización por rol fijo y por asignación en las pruebas HTTP.
- No se mutó base compartida, ni S3, ni Cognito, ni infraestructura; no se usaron secretos reales.
- No se ejecutó ensayo físico en teléfono ni HTTPS con datos móviles (S-05/S-06/S-07 del spec 004 siguen pendientes de ensayo real).
- La recepción del Receptor requiere conexión (spec 014); el modo avión y el reinicio no se probaron en dispositivo físico en esta rama.
- Dependencia #160/PR #159: el recorrido del planificador (publicar ruta y asignar conductor desde el panel) depende de ese PR, que sigue abierto; #162 se verificó con asignación explícita por API en las pruebas, sin incorporar su código.

## Pendientes para el coordinador

- Revisar el diff, las pruebas y este informe; devolver cualquier falta antes de integrar.
- Actualizar desde `origin/develop`, resolver conflictos preservando cambios del equipo y repetir las verificaciones afectadas.
- Publicar la rama y abrir PR hacia `develop` con `Closes #162`; aplicar metadatos del repositorio y solicitar revisión.
- No se abrió PR, no se mergeó y no se cerró el issue: quedan a cargo del coordinador.
