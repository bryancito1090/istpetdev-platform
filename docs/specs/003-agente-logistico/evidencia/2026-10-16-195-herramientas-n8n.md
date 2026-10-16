# Evidencia — issue #195: herramientas n8n de abastecimiento

Fecha de inicio: 16-oct-2026. Rama: `feature/195-herramientas-n8n-abastecimiento`,
checkout aislado Orca `/home/bryan/orca/workspaces/ISTPETDEV/195-herramientas-n8n-abastecimiento`
desde `origin/develop` `1dcfb51`. Ejecutor: OpenCode, por encargo de Bryan
(transferencia de Codex). Plan: [plan-195-herramientas-n8n.md](../plan-195-herramientas-n8n.md).

Estado: **en ejecución**. Código, pruebas locales, ejecución real, CI y merge se
registran por separado a medida que se verifican; nada se acredita sin salida real.

## Matriz de contratos (propuesta antes de implementar)

Contratos de #194 propuestos por su plan (Antigravity aún no implementa; sin commit
al 16-oct 12:30). Se implementan los wrappers contra estos contratos y se
reconciliarán contra el contrato final cuando el PR de #194 esté disponible.
Ningún endpoint se presume existente ni se integra cierre con ellos sin verificar.

| Tool | HTTP | Argumentos del LLM | Respuesta esperada |
|---|---|---|---|
| `find_stock` | `GET /api/v1/logistics/stock` | `productId` (UUID, requerido); `warehouseId` (UUID, opcional) | `{ operationalDate, productId, sku, baseUnit, warehouses: [...] }` |
| `plan_sourcing` | `POST /api/v1/logistics/sourcing` | exactamente una de `orderIds` / `servicePointIds` (1..100 UUID únicos); `productId` opcional solo con puntos | `{ operationalDate, options: [...] }` con asignaciones y faltantes |
| `vehicle_availability` | `GET /api/v1/logistics/vehicles/available` | objeto vacío `{}` (sin fecha libre: el contexto firmado no autoriza otro día) | `{ operationalDate, date, timeZone, vehicles: [...] }` |
| `propose_routes` (sin cambio de contrato) | `POST /api/v1/route-plans` | `{}` | `routes[]` con `agentDecisionId` |

Reglas de wrapper comunes a los ocho tools:

- `apiBaseUrl` de `$env.ISTPETDEV_API_BASE_URL`; `runId`, `kind`, `incidentId` y
  `deadlineUtc` siempre del payload firmado (`Verify Signature`); argumentos,
  query ni objetos anidados pueden sobrescribirlos.
- Solo se reenvían campos de allowlist (validados en el `Check Deadline` de cada
  subworkflow); el objeto arbitrario del modelo no se propaga.
- UUID validado con el mismo patrón del parser; listas deduplicadas y acotadas a
  100; URL con `encodeURIComponent`; cuerpo construido por allowlist.
- `X-Agent-Run-Id` del contexto firmado en los ocho tools; OAuth2 M2M existente.
- Timeout HTTP dinámico: `min(15000, restante - 1200)`, mínimo 1000 ms; el guard
  preexistente de 3200 ms restantes se conserva. La llamada se omite si no cabe
  (`skipped` con motivo) y el callback conserva margen.
- `criticalDeadline` (#193) queda **fuera de este PR**: el campo publicado aún no
  existe en develop (`PlanRequest` solo tiene `Date`, `VehicleCodes`,
  `IncludeConsolidation`). Se documenta el hueco; no se promete prioridad horaria
  ni se inventa el nombre del campo. Al integrar #193 se re-evalúa el cambio mínimo.

## Decisión de no expuesto: fecha de `vehicle_availability`

El plan admite fecha opcional "solo si existe contexto autorizado para otro día".
El contexto firmado actual solo contiene `runId`, `kind`, `incidentId`,
`apiBaseUrl` y `deadlineUtc`; no hay fecha operativa autorizada para el modelo.
Por eso el schema del tool no acepta `date` y el wrapper rechaza cualquier
argumento: la consulta usa el reloj operativo del servidor (`operationalDate`).

## Evidencia determinista en AgentRun (mínima, capa Application)

`GET /api/v1/agent/runs/{id}` expone por decisión, derivado por servidor del plan
correlacionado (no de texto del LLM): `vehicleCode` (preexistente),
`originWarehouseCode`/`originWarehouseName` (bodega base real del vehículo vía
`Vehicle.DepotLocationId`) y `orderedStops` (secuencia real de `RouteStop` del
plan propuesto; en propuestas de fallback, del `ProposalJson.route`). Se mantiene
la instantánea del plan propuesto; no se reconstruye con stock/riesgo actual.
Cambio en `src/Application/Agent` únicamente; `RoutePlansController` no se toca.
