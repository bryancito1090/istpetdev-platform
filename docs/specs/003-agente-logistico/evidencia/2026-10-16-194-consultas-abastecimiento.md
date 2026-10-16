# Evidencia de implementación y validación local — Issue #194

Revisión final de Codex solicitada por Bryan (16-oct): se detectó que la consulta
por pedidos aún enviaba cantidades completas después de integrar #199 y que los
ejemplos documentales no coincidían con los DTOs. Se añade regresión de pedido
parcial/completo y se ajusta el contrato antes de volver a ejecutar CI y cerrar.
Prelaunch para esta corrida: API de pruebas y PostGIS descartables sobre el
checkout #194; organización fijada por servidor, consultas EF parametrizadas,
sin cambios de auth, migraciones, lockfiles, proxy o secretos. Se reutilizan las
pruebas de RBAC/solo lectura y la auditoría ya registradas; se repetirán los
caminos afectados. RLS/configuración administrable, QR, archivos, cifrado de datos
personales y cabeceras de un despliegue expuesto no aplican a este cambio local.
Verificación de regresión nueva y cierre remoto: pendientes en esta revisión.

- **Fecha:** 2026-10-16
- **Issue:** [#194 — Consultas para el agente de stock por bodega, origen sugerido y vehículos disponibles](https://github.com/bryancito1090/istpetdev-platform/issues/194)
- **Rama:** `feature/194-consultas-abastecimiento-agente`
- **Base de partida:** `origin/develop` (`1dcfb51`)
- **Plan de referencia:** `docs/specs/003-agente-logistico/plan-194-consultas-abastecimiento.md`

## 1. Registro de revisión `istpetdev-prelaunch`

| Dimensión | Estado | Detalle |
|---|---|---|
| **Alcance del ensayo** | Local aislado | `WebApplicationFactory<Program>` (`ApiFactory`) sobre contenedor descartable PostgreSQL 16 + PostGIS (`postgis/postgis:16-3.4`) con fixture `demo-region` (DemoCore, 36 tablas núcleo). Sin conexiones a Amazon RDS, Cognito real, OSRM público ni n8n/LLM. |
| **Evidencia vigente reutilizada** | Reutilizada | Suite existente de autenticación M2M (`AutomationAuthorizationTests`), trazabilidad (`AgentRunTraceTests`) y ruteo por pedidos (`OrderBasedRoutingValidationTests`) ejecutada y aprobada en la corrida completa (`528/528`). |
| **RBAC y scopes M2M** | Verificado (PASS) | `[Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]` en `LogisticsQueriesController`. Probados `Gerencia` (`200`), `Planificador` (`200`), `Automatizacion` M2M con `istpetdev-api/automation` y `token_use=access` (`200`), anónimo (`401`), roles restantes (`Bodega`, `Conductor`, `Receptor`, `Auditor` → `403`), M2M sin scope / cliente no permitido / `id_token` / cuenta inactiva o rol distinto (`403`). |
| **Solo lectura logística y trazabilidad** | Verificado (PASS) | Consultas con `AsNoTracking()` y `IRiskCalculator.CalculateAsync(persistSnapshots: false)`. Comprobada invariancia exacta de conteos, IDs y valores de `DeliveryOrder`, `DeliveryOrderLine`, `RoutePlan`, `RouteStop`, `RouteStockReservation`, `InventoryBalance`, `StockMovement`, `Delivery` y `RiskSnapshot` antes y después de consultar y repetir. `X-Agent-Run-Id` añade trazas en `AgentRun.ToolCallsJson` solo para `Automatizacion`. |
| **Aislamiento de cambios** | Verificado (PASS) | `src/Api/Controllers/RoutePlansController.cs` permanece intacto (reservado a #201). Sin archivos de configuración sensibles ni secretos en el árbol de Git. |
| **No aplica en este alcance** | N/A | TLS VerifyFull contra RDS, CORS/CSP/HSTS de frontal público, rate limiting de endpoint público QR y despliegue en EC2/ECS no aplican a esta validación local en contenedor descartable. |
| **Pendientes fuera de #194** | Pendiente en #195/#196 | Cableado de herramientas HTTP en workflows de n8n (`#195`) y prueba E2E con n8n/DeepSeek real (`#196`). |

## 2. Cobertura de la matriz de aceptación (`LogisticsQueriesTests`)

| Prueba de integración | Casos verificados | Resultado |
|---|---|---|
| `GetStock_AppliesFormulaSafetyOnceFefoAndFilters` | Fórmula `netUsable = sum(max(0, onHand - reserved))`, `safetyStockPct = 10%` aplicado **una sola vez por bodega y producto**, orden FEFO (`ExpiresOn` ascendente, `null` al final), exclusión de lotes bloqueados y vencidos hoy (`ExpiresOn <= operationalDate`), inclusión de bodega activa vacía (`BOD-SUR`) con totales `0` y filtro por `warehouseId` | PASS |
| `PostSourcing_ByOrderIds_SharesSafetyStockBudget_HandlesShortages_AndPreservesSuggest` | Sourcing de 1 y varios pedidos a través de múltiples bodegas (`BOD-SUR` más cercana a `HOS-S01`, `BOD-CEN` más cercana a `HOS-N01`), presupuesto compartido por `(bodega, producto)` cuando dos pedidos compiten por el mismo lote sin sobreasignar, faltante parcial (`shortageQuantity > 0`, `fullySourced: false`) con `200 OK`, y no regresión de `POST /api/v1/route-plans/suggest` | PASS |
| `PostSourcing_ByServicePointIds_UsesInMemoryRisks_PreservesMultipleProducts_WithoutCreatingOrders` | Cálculo en memoria (`persistSnapshots: false`), preservación de múltiples productos en riesgo para un mismo punto (`HOS-N01`), orden de urgencia (`Critico` antes que `Alto`), filtro opcional `productId`, punto sin riesgo devuelve `demands: []`, y cero `DeliveryOrder` o `RiskSnapshot` creados | PASS |
| `GetAvailableVehicles_UsesOrganizationTimezone_FiltersPublishedOnly_AndAppliesFallbackCost` | Ventana local `[00:00, 24:00)` en `America/Guayaquil` (`[05:00Z, 05:00Z + 1d)`), exclusión de vehículos con `RoutePlan` `Published` en ese día local, borde `04:59:59Z` (día previo en Quito) no bloquea, `Proposed`/`Cancelled`/`Superseded` no bloquean, inactivos excluidos, `CostPerKm` fallback (`0.45 USD/km`), `MaxVolume` nullable y `EstimatedTotalCost: null` | PASS |
| `Validation_RejectsInvalidInputs_MissingEntities_AndCrossOrganizationIds` | `400` en UUID/fecha/JSON mal formados; `422` en `productId` faltante o vacío, ambos/ninguno de `orderIds` y `servicePointIds`, listas vacías, `Guid.Empty`, `productId` con `orderIds`, y `> 100` IDs únicos; deduplicación de IDs repetidos (`200`); `404` en IDs inexistentes, de otra organización o ubicación que no es bodega activa | PASS |
| `AccessControl_EnforcesPlanningAndM2MAutomation acrossAllThreeEndpoints` | Verificación de los 3 endpoints para `Gerencia` (`200`), `Planificador` (`200`), M2M válido (`200`), anónimo (`401`), `Bodega`/`Conductor`/`Receptor`/`Auditor` (`403`), M2M sin scope (`403`), `client_id` no permitido (`403`), `token_use=id` (`403`) y cuenta M2M degradada a rol no autorizado (`403`) | PASS |
| `ReadOnlyAndTraceability_NeverMutatesLogisticsState_AndTracesOnlyForAutomation` | Firma completa del estado logístico idéntica antes y después de invocar y repetir las 3 consultas; `X-Agent-Run-Id` registra los 3 llamados en `AgentRun.ToolCallsJson` con actor `Automatizacion` y es ignorado con actor humano (`Planificador`) | PASS |
| `VehiclesAvailable_RespectsFixedClockMode_AndVehicleMatcherHandlesEmptyStops` | Fecha por defecto tomada de `IOperationalClock` en modo `Fixed` (`2026-10-20`) y `MatchVehiclesAsync` con lista de paradas vacía devuelve distancia y costo `0` sin excepción | PASS |

## 3. Comandos de validación ejecutados

1. `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~LogisticsQueriesTests'`
   - Resultado: `Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 14 s`
2. `dotnet build IstpetDev.sln -c Release`
   - Resultado: `Build succeeded. 0 Warning(s), 0 Error(s)`
3. `dotnet test tests/IstpetDev.Tests -c Release --no-build`
   - Resultado: `Passed! - Failed: 0, Passed: 528, Skipped: 0, Total: 528, Duration: 3 m 59 s`
4. `npm run check`
   - Resultado: `PASS` (`eslint`, `check-no-mock-login`, `dotnet build`, `shared-core`, `web`, `mobile`, `check-campo-build`)
5. `npm run audit:check`
   - Resultado: `PASS` (`Auditoría superada con éxito. Hallazgos cubiertos por excepciones aprobadas vigentes (7 avisos autorizados).`)
6. `python3 docs/boveda/.github/scripts/check_vault.py`
   - Resultado: `Resumen: 105 notas Markdown, 1113 enlaces internos, 0 errores, 4 avisos.`
7. `git diff --check`
   - Resultado: limpio (sin salida, código `0`).

## 4. PR, integración con `develop` y resolución de conflictos

- **PR:** [#215](https://github.com/bryancito1090/istpetdev-platform/pull/215) (`feature/194-consultas-abastecimiento-agente` → `develop`, etiqueta `type::feature`).
- **Corridas de CI remoto previas:**
  - `5af0eb3`: `7/7` checks en `SUCCESS` (run [`38944148815`](https://github.com/bryancito1090/istpetdev-platform/actions/runs/38944148815)).
  - `f913cff`: `7/7` checks en `SUCCESS` (run [`38952810160`](https://github.com/bryancito1090/istpetdev-platform/actions/runs/38952810160): `Pruebas Backend (Integración)`, `Pruebas Backend (Unit y E2E)`, `QA Backend (.NET 8)`, `QA Frontend (Angular 22)`, `Ensayo de respaldo y restauración`, `check-vault` y `Validar rama de origen`).
- **Revisión y límite de autoaprobación en GitHub:** se verificó cada fila de la matriz de aceptación; al invocar `gh pr review 215 --approve`, GitHub responde `GraphQL: Review Can not approve your own pull request (addPullRequestReview)` por coincidir la identidad autenticada (`bryancito1090`) con el autor del PR. La protección de `develop` (`reviewDecision: ""`) no bloquea el merge por falta de aprobador externo cuando `mergeable == MERGEABLE` y todos los checks están en verde.
- **Conflictos resueltos contra avances concurrentes en `origin/develop`:**
  1. `origin/develop` (`621fdc0`, PR #206 / issue #197): conflicto aditivo al final de `docs/boveda/03 Arquitectura/Contratos API y eventos.md`. Se conservaron íntegras ambas secciones (`#197` y `#194`).
  2. `origin/develop` (`5dc3470`, PRs #211 y #214 / issues #196 y #201): auto-merge limpio en `src/Application/Logistics/VehicleMatcher.cs` (conservando `StopLoadDto` y `Loads` de #196/#201 junto con `GetAvailableVehiclesAsync` de #194) y resolución aditiva al final de `docs/boveda/99 Fuentes/Fuente y control documental.md` (conservando las entradas de PR #192 e issue #194).
  3. `origin/develop` (`0c296bf`, PR #216 / issue #199): resolución en `src/Application/Logistics/SourcingService.cs` conservando el cálculo del remanente sin planificar (`Math.Max(0m, line.RequestedQuantity - line.PlannedQuantity)`) de `#199` dentro de `PlanSourcingAsync` conectado a `AllocateDemandsCoreAsync` de `#194`, y resolución aditiva en `docs/boveda/03 Arquitectura/Contratos API y eventos.md` conservando las secciones de `#199` y `#194`. `src/Api/Controllers/RoutePlansController.cs` mantiene únicamente los cambios de `origin/develop` (#197/#201/#199), sin modificaciones propias de #194.
- **Verificación local tras integrar `0c296bf`:** `LogisticsQueriesTests`, `ConfirmCapacityTests`, `PartialCapacityPlanningTests`, `PartialLoadTests`, `RouteSuggestPriorityTests`, `DeliveryStopSequencerTests` y `OrderBasedRoutingValidationTests` en verde.

## 5. Revisión final de Codex antes del merge

- Regresión reproducida contra `786732a`: una línea con `requested=80` y
  `planned=60` seguía ofreciendo 80; una línea completamente planificada seguía
  apareciendo. La prueba dirigida falló por devolver dos demandas en vez de una.
- Corrección: `QueryOrderSourcingAsync` y `PlanSourcingAsync` comparten
  `LoadOrderDemandsAsync`, que resta PlannedQuantity y omite líneas completas.
  El controlador nuevo deja de reconstruir por su cuenta los pedidos. No se
  modifica RoutePlansController.
- La misma prueba comprueba remanente 20 en ambos endpoints, líneas completas
  omitidas, demanda/opciones vacías al planificar todo y PlannedQuantity intacta
  tras consultar. Con la corrección, 21/21 pruebas dirigidas aprobadas; build
  Release con 0 warnings/0 errores. TRX local: `review-194.trx`.
- Contrato cotejado con DTOs y controlador: sourceKind Order/Risk, nombres
  quantityOnHand/quantityReserved/netUsableQuantity/safetyStock, destinos,
  allocations con lots y opciones A/B. Se retiran campos inexistentes, duración
  que no se devuelve, códigos de error no implementados, orden de riesgos y
  redondeos documentados incorrectamente.
- Reglas de GitHub consultadas: develop es la rama por defecto en esta revisión;
  endpoint de reglas devuelve `[]`, protección clásica devuelve Branch not
  protected (404). Se verifica CI y revisión técnica sin atribuir aprobación
  formal a un comentario del autor.
- Pendiente: integrar #193 recién incorporado a develop, regresión y CI finales,
  merge y estado CLOSED de #194.
