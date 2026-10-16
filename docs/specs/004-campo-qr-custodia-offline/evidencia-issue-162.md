# Evidencia del issue #162 — estados del bulto y custodia Bodega → Conductor → Receptor

Fecha: 2026-10-15 (America/Guayaquil). Rama: `fix/162-estados-bulto-custodia` (desde `origin/develop`, base `dab3429`).
Entorno: worktree de la rama; PostgreSQL/PostGIS efímero por Testcontainers; sin base compartida, sin cloud y sin despliegue.

> Este documento registra **pruebas locales**. No afirma ejecución física en teléfono ni despliegue. La revisión de prelaunch aplicada es de alcance local aislado (ver «Prelaunch y límites»).

## Commits de esta revisión

| Commit | Contenido |
|---|---|
| `fb4d877` | `fix(api): un solo recibo final y despacho atómico por entrega` (core, consumidores, pruebas backend) |
| `ed3a925` | `fix(mobile): estado monotónico y guardas de custodia por turno` (PWA y contratos compartidos) |
| `45d73e5` | `fix(web): atribuir la recepción al receptor en la bandeja` |
| (este commit) | `docs(campo): evidencia y contrato de un recibo final para el 162` |

Correcciones bloqueantes atendidas en esta ronda: ALREADY_RECEIVED antes de versión, validación de recurso en replay de hash legado y matriz completa de transiciones inválidas (prepare/dispatch incluidos). Ver también los commits de la primera entrega (`2f4953f`, `0c7c431`, `af9344f`, `b8045c4`).

## Alcance implementado (semántica final)

- Máquina de estados explícita del bulto y de la entrega: `Sealed` → `Dispatched` → `InTransit` → `Delivered` → `Received`/`Partial`, con responsables por turno (Bodega, Conductor asignado, Receptor con QR).
- **Un solo recibo final por entrega:** `Received` y `Partial` son terminales; cualquier recibo previo, incluso con estado legado `Delivered` o inconsistente, responde 409 `ALREADY_RECEIVED` **antes** de validar `expectedDeliveryVersion`; el replay exacto de la misma operación sigue devolviendo 200. Lo rechazado permanece bajo custodia del conductor hasta su devolución explícita.
- **Cobertura exacta pre-recibo:** conjunto exacto de líneas despachadas (sin omisiones ni duplicados), `LotId` igual al de la `DeliveryLine`, cantidades no negativas y `aceptada + rechazada = DispatchedQuantity` por línea. `GET /receiver/handling-units/{unitId}` muestra todos los bultos y todas las líneas de la entrega. Transferir lo aceptado exige saldo suficiente en el vehículo (409 `INSUFFICIENT_VEHICLE_STOCK`) sin `Math.Max`.
- **Preparación y despacho atómicos:** mismo lock de organización; el despacho exige exactamente todos los bultos de la entrega y cobertura exacta de lo planificado (409 `INCOMPLETE_HANDLING_UNITS`), con ledger y saldos agrupados por línea-lote y caché en memoria.
- **Idempotencia:** índice por `(organización, cuenta, operationId)` sin `ActionCode`; UUID reutilizada en otra acción, entrega o cuerpo → 409 `IDEMPOTENCY_CONFLICT` (sin 23505/500); `Guid.Empty` → 422 `INVALID_OPERATION_ID`; el replay de hash legado valida el `deliveryId` de la respuesta. Límite documentado: el cuerpo histórico no se puede revalidar (no se guardó).
- **Comandos nuevos:** `POST /deliveries/{id}/pickup` y `/deliver` (Conductor asignado, idempotentes, sin movimiento de stock). `POST /deliveries/{id}/receipts` responde 409 `RECEIPT_REQUIRES_RECEIVER`.
- **Consumidores alineados:** `RiskCalculator` cuenta `Delivered` pendiente y excluye recibidas/legado con recibo; `RoutePublisher` y `Replanner` bloquean `InTransit`/`Delivered`; la bandeja web atribuye la recepción al Receptor (`registeredByName`, campo legado `driverName` conservado).
- **PWA:** outbox durable con `commandKind` para Recoger/Entregar; estado monotónico (snapshot + comandos confirmados + bultos, incluido el legado `Delivered` con bultos `Received`/`Partial`); guards antes de guardar comandos; `/receipt/:deliveryId` solo para Conductor; el receptor ve el alcance de entrega completa; `resolveRequiresReview` no toca `receiptPayload` ausente en comandos.
- **Migración** `20261015234714_CustodyChainStates`: índice `CustodyEvent(OrganizationId, OperationId)` no único (una operación con varios bultos emite un evento por bulto). No se editó ninguna migración histórica ni el snapshot a mano.

## Criterios del issue → evidencia (pruebas automáticas)

| Criterio | Prueba |
|---|---|
| Cada rol solo actúa en su turno | `CustodyChainHttpTests.Full_journey_respects_each_role_turn_and_moves_stock_once`; matriz de `CustodyGuardsHttpTests` con actor correcto |
| Traza Despachada → En ruta → Entregada → Recibida | Mismo recorrido: `events` públicos = `Preparada, Despachada, En ruta, Entregada, Recibida` |
| Conductor no recibe | `receipts` → 409 `RECEIPT_REQUIRES_RECEIVER` sin efectos |
| Receptor no recibe antes de la entrega | `NOT_DISPATCHED` / `NOT_DELIVERED` en recorrido y matriz |
| Sin asignación no hay permiso | `Missing_or_foreign_assignment_forbids_driver_transitions` (403) |
| Un solo recibo final y parcial terminal | `Exact_replay_after_advancing…` (clave nueva con versión previa real → `ALREADY_RECEIVED`), `Partial_receipt_keeps_rejected_in_the_vehicle…`, legado con recibo |
| Multibulto sin perder carga | `Multi_box_delivery_is_received_once_with_full_coverage` (2 cajas, 1 recibo, 20 L en punto, 0 en vehículo) |
| Cobertura exacta sin efectos | `Receipt_requires_exact_full_coverage_without_effects` (omisión, duplicado, línea/lote ajeno, negativo, reporte menor/mayor) |
| Saldo de vehículo real | `Receipt_rejects_insufficient_vehicle_stock_without_effects` |
| Despacho atómico y completo | `Dispatch_requires_all_boxes_and_full_planned_coverage_without_effects`, `Dispatch_requires_full_planned_coverage_per_line`, `Dispatch_of_two_lines_sharing_a_lot_creates_one_vehicle_balance` |
| Idempotencia por operación | `Reusing_an_operation_uuid_in_another_action_delivery_or_body…`, `Empty_operation_guid_is_rejected_with_422`, `Exact_replay_after_advancing…`, `Legacy_idempotency_hash_replays_without_new_effects` (incluye otra entrega → 409) |
| Matriz completa de transiciones inválidas | `CustodyGuardsHttpTests` genera 30 combinaciones desde la tabla de origen permitido (prepare/dispatch→Draft, pickup→Dispatched, deliver→InTransit, receipt→Delivered), cada una con 409 y snapshot de efectos inmutable |
| Concurrencia entre cuentas/claves | `Receivers_with_distinct_keys_cannot_duplicate_stock` (1 aceptado, 1 conflicto, 1 recibo) |
| Consumidores de estado | `RiskPendingDeliveriesTests`, `ReplanningTests.Revert_is_blocked…`, `RoutePublisherTests.Revision_cannot_remove_an_in_transit_delivery` |
| Atribución correcta en web | `rutas.page.spec.ts` (DOM: «Registrado por: Laura Receptor», sin «Conductor:») |
| PWA por turnos y estado monotónico | `receipt.page.spec.ts` (jsdom), `custody-state.spec.ts`, `home.page.spec.ts` |
| Demo sin salto de estado | `DemoAdvanceTests` (2 paradas, recibos reales, replay idempotente; simulación atribuida a la cuenta de demo) |

## Comandos y resultados (batería final secuencial, 15-oct-2026)

Ejecutada en serie (sin suites pesadas concurrentes), sobre el código de `fb4d877`/`ed3a925`/`45d73e5`:

| Comando | Exit | Resultado |
|---|---|---|
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/IstpetDev.Tests -c Release` | 0 | 505/505 aprobadas, 0 fallos (PostGIS en Testcontainers) |
| `npm run check` | 0 | lint FSD + .NET + shared + web + mobile + PWA `/campo` |
| `npm run test:frontend` | 0 | shared 24/24, web 75/75, mobile 87/87 |
| `npm run audit:check` | 0 | 7 avisos autorizados por excepciones vigentes |
| `python3 -I scripts/db/check-additive-migrations.py` | 0 | «Migraciones aditivas: OK» |
| `python3 docs/boveda/.github/scripts/check_vault.py` | 0 | 87 notas, 932 enlaces, 0 errores (4 avisos preexistentes) |
| `bash .github/scripts/branch-policy.sh pr fix/162-estados-bulto-custodia develop …` | 0 | PR permitido hacia `develop` |

TRX crudo de la suite .NET final (505 aprobadas) sanitizado, sin rutas personales: `docs/specs/004-campo-qr-custodia-offline/evidencia-trx/full-final.trx`. Salidas completas de la batería: `battery-final.txt` en el directorio de trabajo temporal de la sesión.

### Nota sobre una corrida intermedia fallida de frontend

La primera corrida combinada de `npm run test:frontend` de esta ronda terminó con exit 1: 74 pruebas ejecutadas aprobadas y **un error del pool de Vitest** («Worker forks emitted error» / «Worker exited unexpectedly» / timeout al terminar el fork de `auth.service.spec.ts`), sin ninguna aserción fallida. Comprobaciones posteriores: ese archivo no pertenece a esta rama (último cambio en PR #42), no usa timers, y corridas separadas quedaron verdes — el archivo aislado 5/5 (`--include`, exit 0) y la suite completa dos veces más (mobile 13/87 y después shared 24 + web 75 + mobile 87, exit 0). La explicación **compatible** es presión transitoria de recursos del entorno (la máquina mostraba ~8.9 GiB de swap en uso tras suites .NET/PostGIS y builds concurrentes); no se demostró un OOM/kill ni un defecto de la prueba, y no se cambió código de autenticación. Mitigación operativa aplicada en esta batería: ejecutar las suites pesadas en serie (CI también separa frontend y backend).

## Prelaunch y límites (alcance local aislado)

Se leyó la skill `docs/boveda/08 Skills/istpetdev-prelaunch.md`; no se generó copia local porque no se levantó API expuesta ni navegador (la ejecución fue el host de pruebas .NET con PostgreSQL efímero y las compilaciones/tests de Angular en jsdom).

- RLS: no aplica a la demo (ADR-21, núcleo sin RLS); se verificó autorización por rol fijo y por asignación en las pruebas HTTP.
- No se mutó base compartida, S3, Cognito ni infraestructura; no se usaron secretos reales.
- No hay ensayo físico en teléfono, modo avión ni QR con datos móviles (S-05/S-06/S-07 siguen pendientes de ensayo real). Las pruebas PWA son jsdom, no navegador real.
- `advance` es una **simulación de recorrido** que respeta las guardas de estado y atribuye las transiciones a la cuenta de demo; no acredita acciones físicas del conductor.
- El replay de un registro legado solo puede validar el recurso de la respuesta, no el cuerpo histórico completo (no se almacenó).
- Dependencia #160/PR #159: el recorrido del planificador (publicar/asignar desde el panel) sigue en ese PR abierto; aquí se verificó con asignación explícita por API (el test de demo ya usa el `Account.Id` interno).

## Pendientes para el coordinador

- Revisar el diff, las pruebas y este informe; devolver cualquier falta antes de integrar.
- Actualizar desde `origin/develop`, resolver conflictos preservando cambios del equipo y repetir las verificaciones afectadas.
- Publicar la rama y abrir PR hacia `develop` con `Closes #162`; aplicar metadatos y solicitar revisión.
- No se abrió PR, no se mergeó y no se cerró el issue: quedan a cargo del coordinador.
