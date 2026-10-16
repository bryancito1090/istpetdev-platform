# Evidencia — issue #195: herramientas n8n de abastecimiento

Fecha de inicio: 16-oct-2026. Rama: `feature/195-herramientas-n8n-abastecimiento`,
checkout aislado Orca `/home/bryan/orca/workspaces/ISTPETDEV/195-herramientas-n8n-abastecimiento`
desde `origin/develop` `1dcfb51`. Ejecutor: OpenCode, por encargo de Bryan
(transferencia de Codex). Plan: [plan-195-herramientas-n8n.md](../plan-195-herramientas-n8n.md).

Estado: **en ejecución; continuación por Codex tras detener OpenCode por petición de Bryan**. Código, pruebas locales, ejecución real, CI y merge se
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

## Estado de ejecución — 16-oct-2026 (tarde)

### Código y pruebas locales (verificado)

- Commits en `feature/195-herramientas-n8n-abastecimiento`; PR
  [#213](https://github.com/bryancito1090/istpetdev-platform/pull/213) abierto en
  **draft** hacia `develop` (las dependencias aún no están integradas). El PR
  permanece draft a propósito; el CI de PR no se ha disparado todavía (eventos
  perdidos durante la caída de red + condición de draft); se marcará listo y se
  forzará un `synchronize` cuando el ensayo real y las dependencias estén.
- Integrados a la rama `origin/develop` `621fdc0` y `0c296bf` sin perder
  funcionalidades ajenas: regla 8 de carga parcial (#199) portada del prompt v1
  al **v2**, workflows regenerados desde plantillas fusionadas y secciones de
  #192/#197/#199 conservadas junto a las de #195 en la bóveda.
- `dotnet build IstpetDev.sln -c Release`: 0 errores, 0 warnings.
- `dotnet test tests/IstpetDev.Tests -c Release --no-build`: **545/545** en el
  árbol fusionado (incluye `N8nSupplyToolsTests` y las pruebas de #199/#197).
  Nota de fallo conservado: una corrida intermedia de la suite falló por
  timeouts transitorios de PostGIS con carga alta del host (Npgsql operation
  timed out, 4 pruebas); las mismas pruebas pasaron 36/36 en aislamiento y la
  suite completa volvió a pasar 545/545. No es regresión funcional.
- `python3 -I -m unittest discover -s tests/scripts -p 'test_*.py'`: 32/32;
  `build-workflows.py --check` limpio; `npm run check`, `npm run audit:check`,
  `check_vault.py` (0 errores, 4 avisos preexistentes) y `git diff --check`
  limpios antes de publicar el PR.
- `N8nSupplyToolsTests` (E2E real, n8n 2.39.5, host networking, loopback):
  ejecuta los subworkflows de `find_stock`, `plan_sourcing` y
  `vehicle_availability` generados desde las plantillas y verifica URL, método,
  query/cuerpo deduplicado, cabecera `X-Agent-Run-Id`, token OAuth2 M2M y la
  omisión por `invalid_arguments` y `deadline_too_short` sin llamar a la API.
  El bridge de referencia no reenviaba esa cabecera; el ensayo nuevo la
  conserva y la acredita.

### Prelaunch — alcance local aislado (registro previo al runtime)

Alcance: ensayo local para el issue #195, sin despliegue ni datos reales.
Aplicado `istpetdev-prelaunch` leyendo su fuente
`docs/boveda/08 Skills/istpetdev-prelaunch.md` y
`03 Arquitectura/Verificacion de seguridad antes del lanzamiento.md`.

| Control | Estado | Justificación |
|---|---|---|
| RLS | No aplica | ADR-21: demo sin RLS; una organización fija `DemoCore` |
| Campos/plantillas | No aplica al cambio | No se tocan plantillas administrables, DTO de campos ni RBAC; los esquemas nuevos son de herramientas n8n, con allowlist y validación runtime |
| Cifrado/archivos/QR/cabeceras | No aplica | El alcance no expone proxy, storage, QR ni web pública; ensayo en loopback |
| SQL manual | No aplica | No se modifican consultas manuales; el enriquecimiento usa EF con `AsNoTracking` |
| Dependencias | Verificado | `npm run audit:check` (7 avisos con excepción vigente) y lockfiles sin cambios; `dotnet list package --vulnerable` se ejecuta en el prelaunch del ensayo |
| Identidad/M2M/HMAC | Verificado por pruebas | Guard de HMAC intacto; OAuth2 M2M y `X-Agent-Run-Id` solo desde contexto firmado; pruebas de contexto y de runtime real |
| Secretos | Verificado | `WorkflowSecretsTests` inspecciona exports, plantillas y subworkflows embebidos; sin `pinData` con datos reales; `.env` privado fuera del repo |

Riesgo práctico del ensayo: n8n/API escuchan en loopback y la base es
descartable; no se presenta como lanzamiento seguro ni se ejecuta ningún
despliegue.

### Dependencias (bloqueo real registrado)

- **#194**: PR [#215](https://github.com/bryancito1090/istpetdev-platform/pull/215)
  abierto, CI en curso al 19:15 UTC; contratos documentados
  (`/api/v1/logistics/{stock,sourcing,vehicles/available}`) compatibles con los
  wrappers implementados. Sin merge todavía: no se acredita integración ni se
  declara el ensayo real con esos endpoints.
- **#193**: PR [#205](https://github.com/bryancito1090/istpetdev-platform/pull/205)
  en conflicto, sin merge; contrato conocido (`criticalDeadlineLocal`, `"HH:mm"`).
  La extensión del `propose_routes` queda fuera de #195 porque exigiría que la
  hora viaje en el contexto firmado/configuración autorizada (no existe hoy) y
  el propio issue la marca como opcional. Hueco documentado; no se promete
  prioridad horaria.

### Pendiente para salir de draft y cerrar

1. Integrar el contrato final de #194 cuando su PR se fusione y repetir pruebas.
2. Ensayo real local con n8n 2.39.5 + DeepSeek sobre `demo-region` (Watch
   Completed, Incident de regresión, fallo controlado del proveedor), guardando
   `runId`, `executionId`, SHA, versión de prompt, latencia, traza ordenada y
   respuesta saneada del `AgentRun` con origen/vehículo/prioridad.
3. Revalidación del harness de aceptación con prompt v2 cuando #194 esté en
   `develop` (los tres endpoints deben responder 2xx).
4. Marcar listo el PR, CI del HEAD final, revisión/aprobación según protecciones,
   merge a `develop`, verificación de SHA de merge y cierre explícito de #195.

## Revisión de Codex (16-oct, desde 20:00 UTC)

- Dependencias desbloqueadas: #193 fusionado en `5cfc657`, #194 en `a602edd`; ambos incluidos por el merge local `64ec1c3`. Las menciones de bloqueo de las secciones anteriores describen el estado histórico.
- La suite de OpenCode produjo 562/563: su nuevo ensayo manual exigía una clave privada aun al ejecutar la suite normal. Ahora se descubre como omitido explícito sin clave; con clave se ejecuta contra DeepSeek real.
- Corregido el comparador de políticas (comparaba referencias de listas), y separado el fallo deliberado del proveedor del cálculo de aceptación del proveedor disponible. Se exige además >=90% de explicaciones validadas, herramientas no vacías y las tres consultas en Watch.
- Primera corrida reproducida: 8/8 llamadas 2xx; Watch/Injection agotaron tiempo; Incident terminó pero solo 1/2 explicaciones fue aceptada; caída deliberada dio respaldo en 1363 ms. Este resultado NO acredita aceptación.
- Ajuste mínimo en prompt v2: agrupa consultas independientes después de riesgos y antes de rutas, conserva ocho herramientas y ocho iteraciones, no cambia el modelo ni el plazo. `get_risks` proyecta filas con demanda positiva Critico/Alto/Vigilar, sin duplicar facts ni recalcular valores. Sin reintentos del modelo que excedan el presupuesto interactivo.
- Evidencia histórica: los campos de vehículo/bodega/paradas se guardan al completar el run en `ProposalJson.evidence`, también con respaldo, sin migración ni aceptar evidencia del LLM. Regresión de renombración cubierta.
- Verificación focalizada: 15/15 (AgentRunTrace, AgentJourney, WorkflowSecrets). Auditoría .NET sin paquetes vulnerables; npm audit aceptada con 7 excepciones vigentes.
- HMAC y registro de ejecución comparados contra develop: nodos idénticos en ambos workflows.
- Ensayo optimizado, matriz final, suite completa, CI y merge: pendientes de registrar.
