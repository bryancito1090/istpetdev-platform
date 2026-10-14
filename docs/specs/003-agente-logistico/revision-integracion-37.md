# Revisión e integración de #37 — 14-oct-2026

La entrega de Antigravity se conservó en `1b98954`, sobre `dfd4507`.
La conciliación con `origin/develop` (`255ac60`) está en `089bbf8`:
se conservaron login real, seguridad, ruteo por pedidos y simulación;
los defaults configurables de PostgreSQL se trasladaron al Compose de
dependencias para mantener el arranque sin variables de n8n.

## Hallazgos y correcciones en revisión

- Las herramientas aceptaban del modelo `apiBaseUrl`, `deadlineUtc`, `runId`
  e `incidentId`. Ahora reciben el contexto firmado mediante `fields.values`
  del nodo ToolWorkflow, separado de `query`; el origen HTTP viene de
  `ISTPETDEV_API_BASE_URL`, configurado por el operador. El modelo solo elige
  `routePlanId`. No puede enviar la credencial OAuth2 a otro origen ni ampliar
  el deadline. Se conserva el contrato de cinco herramientas de #37 / NFR-04.
- `DriverMessage` solo verificaba longitud. Ahora sus cifras se validan contra
  los mismos facts que la explicación; el workflow rechaza exceso de longitud
  sin truncarlo silenciosamente (FR-07 / FR-11).
- Guardar decisiones antes de cambiar el estado no evitaba que un callback
  perdedor las modificase. La transición condicional y el guardado se realizan
  en una sola transacción: los lectores ven `Completed` junto con las decisiones
  y el callback tardío no escribe (FR-06b / FR-08).
- La correlación con un run rompía la aprobación humana de una propuesta:
  `Replanner` interpretaba `CorrelationId` como ID del plan. Ahora resuelve el
  plan desde `ProposalJson.proposedRoutePlanId`, también al repetir la aprobación.

## Prelaunch: alcance y controles antes del ensayo

Revisión local aislada, con fixtures sintéticos y PostgreSQL descartable en
`127.0.0.1:15433`, API de aceptación en `5017` y n8n local `2.39.5` en `5678`.
No se promueve a `main`, se modifica el n8n remoto ni se resetea la base compartida.
La comprobación usa la skill local de prelaunch y el runbook ADR-21.

| Control | Estado antes del ensayo | Evidencia / límite |
|---|---|---|
| RLS y multiempresa | No aplica a demo ADR-21 | Organización fija; RLS diferido a Schema95/P2. |
| Identidad y campos protegidos | Pruebas en ejecución | Autorización M2M/humana y correlación conservadas; aprobación resuelta desde el plan persistido, nunca desde texto LLM. |
| Contenido y consultas | Inspección verificada; regresiones pendientes | Contexto firmado independiente del modelo; SQL de transición parametrizado; cifras y longitud validadas por backend. |
| Datos clasificados / archivos | No aplica al cambio | Fixtures sintéticos; no se añaden campos personales, cifrado ni uploads. |
| Dependencias | Pendiente de reporte final | No se agregan paquetes; se reutiliza compuerta de auditoría del spec 000. No se acredita scan de imagen por build. |
| Proxy, SSH, TLS RDS y producción | Pendiente / fuera de ensayo local | No se declara un release expuesto ni se cambia configuración de producción; #39 conserva su alcance. |

## Evidencia

Informe anterior conservado:
`artifacts/agent-acceptance/20261014-150147-176964/report.md`, 30/30 Completed,
herramientas 60/60, p95 9127 ms, LLM 48/50, inyección 10/10.
Corresponde al árbol entregado antes de las correcciones; no acredita el árbol
conciliado. Se repetirá la aceptación local y CI sobre la revisión final.

Los archivos `AgentJourneyTests.cs` y `LocalWorkflowSmokeTests.cs` mencionados
en `task.md` no existen en la entrega. Las pruebas locales de integración
existentes y el informe real se registran por separado; no se atribuye a CI
un recorrido E2E automatizado inexistente.
