# Revisión e integración de #37 — 14-oct-2026

La entrega de Antigravity se conservó en `1b98954`, sobre `dfd4507`.
La conciliación con `origin/develop` (`255ac60`) está en `089bbf8`:
se conservaron login real, seguridad, ruteo por pedidos y simulación;
los defaults configurables de PostgreSQL se trasladaron al Compose de
dependencias para mantener el arranque sin variables de n8n.

## Hallazgos y correcciones en revisión

- Las herramientas aceptaban del modelo `apiBaseUrl`, `deadlineUtc`, `runId`
  e `incidentId`. Ahora reciben el contexto firmado desde el payload del nodo `Verify Signature` al evaluar
  el JSON del subworkflow, separado de `query`; el origen HTTP viene de
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

La entrega no incluía `AgentJourneyTests.cs` ni `LocalWorkflowSmokeTests.cs`.
Codex añadió el primer archivo: vigía con traza, cinco rechazos 403, fixture de
inyección, fallbacks por desactivación/red/timeout/error/resultado inválido,
callback tardío, incidente automático y replanificación parcial sin repetición.
`LocalWorkflowSmokeTests.cs` se sustituye por el ensayo real CE-06 documentado.
La revisión dirigida pasó 20/20 pruebas de agente y ruteo, incluidas carrera de
callback, append concurrente de traza, conflicto de replan y aprobación humana.

## Pendientes completados tras la comprobación de casillas

Bryan confirmó el diseño el 14-oct: `aprobado_por`, ADR-22 y hechos canónicos
se actualizan con esa confirmación. Se corrigió el incidente automático para
continuar el run persistido con scope independiente del request y ejecutar
fallback; se completaron propuestas por vehículo y mensajes por plantilla,
`invalid_result`, traza de 403 y `late_result_ignored`, validación HTTP y
conflicto 409 ante replan concurrente. El cron apagado se conecta a un paso
fijo autenticado que inicia Watch por la API cuando el operador lo habilite.

Se conservó el intento de aceptación fallido
`artifacts/agent-acceptance/20261014-152313-927145/report.md` (0 llamadas).
El paso de contexto y compatibilidad del nodo ToolWorkflow se validan primero
con el runtime real; no se reemplaza un resultado fallido por evidencia anterior.

Suite completa local: **388/388**, 0 fallos ni omitidas, en 6 min 16 s,
`artifacts/agent-review/delivery-final.trx`. Incluye las 3 pruebas de rendimiento
del simulador integrado. Prueba real inicial con exports corregidos: Completed,
2/2 herramientas 2xx, 2/2 decisiones LLM y 7456 ms. La matriz final sigue en curso.
Auditoría NuGet sin paquetes vulnerables; `npm run audit:check` pasa con las
7 excepciones vigentes del spec 000. No se acredita escaneo de imagen n8n.
