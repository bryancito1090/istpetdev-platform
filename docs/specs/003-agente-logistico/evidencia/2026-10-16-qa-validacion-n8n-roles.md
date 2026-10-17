# Evidencia de Verificación QA — Issue #277 (16-oct-2026)

Validación integral del Agente Logístico n8n (vigía, incidentes, error y revisión de datasets) y matriz de autorización por roles para la demo/producción.

## 1. Verificación de Endpoints Productivos

| Endpoint / Servicio | Entorno | Verificación | Resultado |
|---|---|---|---|
| `GET https://istpetdev.doicela.dev/health/ready` | Producción | Disponibilidad API y base de datos PostGIS | `200 OK` (Healthy) |
| `GET https://n8n.bryan-bano.com` | Producción | Instancia n8n de Bryan activa | `200 OK` |
| `POST https://n8n.bryan-bano.com/webhook/istpetdev/dataset/review` | Producción | Validación de autenticación sin token | `401 Unauthorized` (`token_mismatch`) |
| `POST https://n8n.bryan-bano.com/webhook/istpetdev/agent/watch` | Producción | Requerimiento de firma HMAC M2M | `401 Unauthorized` |
| `POST https://n8n.bryan-bano.com/webhook/istpetdev/agent/incident` | Producción | Requerimiento de firma HMAC M2M | `401 Unauthorized` |

## 2. Validación de Lógica del Agente y Fallbacks (E2E / Integración)

| Suite de Pruebas | Casos | Cobertura | Estado |
|---|---|---|---|
| `AgentJourneyTests` | 9 | • Recorrido vigía (`Watch`) con n8n.<br>• Resistencia a inyección de prompt (`demo-region-injection`).<br>• Detección y bloqueo 403 ante llamadas no permitidas al agente (publicar, aprobar, rechazar, cambiar modo de autonomía).<br>• Fallback seguro a Template determinista ante timeouts, n8n inalcanzable (`n8n_unreachable`), agente deshabilitado o error.<br>• Replanificación de incidentes automáticos y escalados a revisión humana. | **Superado (9/9)** |
| `LogisticsQueriesTests` | 8 | Validación de las 8 herramientas consumidas por el agente en n8n (`get_risks`, `find_stock`, `plan_sourcing`, `vehicle_availability`, `propose_routes`, `get_route_plan`, `get_incident_impact`, `replan`). | **Superado (8/8)** |
| `AutomationAuthorizationTests` | 2 | Permisos técnicos M2M con scope `istpetdev-api/automation`; denegación estricta de acciones humanas. | **Superado (2/2)** |
| `AgentRunPersistenceTests`, `AgentRunsApiTests`, `AgentRunTraceTests` | 15 | Trazas de auditoría (`toolCalls`, fuentes LLM vs Template, endpoints `/api/v1/agent/runs`). | **Superado (15/15)** |

## 3. Revisión y Viabilidad de Datasets

| Suite de Pruebas | Casos | Cobertura | Estado |
|---|---|---|---|
| `FeasibilityReportTests` | 4 | Informe de viabilidad de datasets para el webhook `istpetdev/dataset/review`. | **Superado (4/4)** |
| `ChaosAndBenchmarkImportTests` | 5 | Resistencia a fixtures de caos e inyección. | **Superado (5/5)** |
| `NationalDatasetImportTests` | 2 | Importación del dataset nacional `national-v1-seed-11`. | **Superado (2/2)** |

## 4. Matriz de Autorización por Roles

| Suite / Ámbito | Roles Validados | Criterio Verificado | Estado |
|---|---|---|---|
| `AuthorizationSafetyTests` | Gerencia, Planificador, Bodega, Conductor, Receptor | Matriz fija estricta; rechazo ante roles forjados, tokens expirados o sin permisos. | **Superado (7/7)** |
| `AuditorRoleTests` | Auditor | Acceso de solo lectura; bloqueo 403 inmediato ante cualquier intento de escritura o importación. | **Superado (3/3)** |
| Frontend Web (`apps/web`) | Todos los roles | 26 archivos de prueba; 157 pruebas pasando. Vistas diferenciadas (Auditor solo lectura, Gerencia con acceso a Datos, Planificador con Asistente y A/B/C). | **Superado (157/157)** |
| PWA Móvil (`apps/mobile`) | Bodega, Conductor, Receptor | 14 archivos de prueba; 141 pruebas pasando. Despacho Draft, escaneo de custodia, recepción con firmas y rechazo `ErrorRotulado`. | **Superado (141/141)** |

## 5. Matriz T1–T6 del issue #277 (estado real)

Las suites de las secciones 2 a 4 prueban la lógica en un entorno de pruebas; el issue pide verificar **en producción**. Esta tabla separa lo comprobado en producción de lo que sigue pendiente.

| ID | Qué se prueba | Estado | Evidencia o bloqueo |
|---|---|---|---|
| T1 | Vigía desde la web (obligatorio) | **Pendiente** | Requiere iniciar sesión con MFA en `istpetdev.doicela.dev` y pulsar «🤖 Pedir al Agente IA» en Rutas. Falta runId, estado, latencia, nº de decisiones y captura |
| T2 | Ejecución real en n8n (8 herramientas, executionId) | **Bloqueada** | Requiere acceso al portal n8n de Bryan |
| T3 | Error Workflow enlazado y Schedule apagado | **Bloqueada** | Requiere acceso al portal n8n de Bryan |
| T4 | Traza por API (opcional) | **Pendiente** | Depende del runId de T1. `GET /api/v1/agent/runs` sin token responde 401, como corresponde |
| T5 | Incidente del 15-oct (observar) | **Bloqueada** | Requiere acceso al portal n8n de Bryan |
| T6 | Revisión de datasets publicada y rechazo 401 | **Comprobada** | `POST /webhook/istpetdev/dataset/review` sin token → 401 `token_mismatch` |

Referencia previa (no sustituye a T1): smoke de la sincronización del 16-oct, run `01a146dc-7748-7ecf-b5fb-890740270c7f`, ejecución n8n `297`: Completed, `deepseek-chat`, prompt v2, 7091 ms, 6/6 decisiones explicadas por LLM (`2026-10-16-sincronizacion-productiva.md`).

## 6. Comprobación repetida en producción (16-oct-2026, 22:26 America/Guayaquil; 03:26 UTC del 17-oct)

Solo peticiones sin credenciales, que no crean runs ni modifican nada.

| Petición | Resultado |
|---|---|
| `GET https://istpetdev.doicela.dev/health/ready` | 200 (0,26 s) |
| `GET https://istpetdev.doicela.dev/health/live` | 200 (0,14 s) |
| `GET https://n8n.bryan-bano.com/healthz` | 200 (0,39 s) |
| `POST /webhook/istpetdev/agent/watch` sin firma | 401 `{"status":"unauthorized"}` |
| `POST /webhook/istpetdev/agent/incident` sin firma | 401 `{"status":"unauthorized"}` |
| `POST /webhook/istpetdev/dataset/review` sin token | 401 `{"status":"unauthorized","reason":"token_mismatch"}` |
| `GET https://istpetdev.doicela.dev/api/v1/agent/runs` sin token | 401 |

## 7. Corrección a la sección 4

La fila de la PWA dice que la PWA cubre el «rechazo `ErrorRotulado`». La API acepta ese motivo desde #236 (PR #260), pero la PWA todavía usa la lista fija `RECEIPT_REJECTION_REASONS` (V1) de `libs/shared-core/src/contracts/field.contract.ts`. Hoy el receptor no puede elegir «Error de rotulado» desde el teléfono. Queda anotado para Bryan en #260.

## 8. Réplica local completa de los workflows de Bryan (16-oct-2026, 22:33–22:50 America/Guayaquil)

Para cubrir todo lo que no se puede ver en producción sin sesión ni portal, se levantó en local la misma lógica: **n8n 2.39.5** (la versión de Bryan), con los cuatro workflows del repositorio (`automation/n8n/workflows/*.json`), credencial M2M de Cognito de desarrollo y DeepSeek real. La API local corrió sobre una base **aislada** `istpetdev_dev_05` en el Docker local (roles `migrator_dev_05`/`user_dev_05` sin privilegios especiales, como exige `Schema95`). Los runs se dispararon con el token M2M (rol Automatización), igual que lo hace n8n en producción.

**Esto es evidencia local: no sustituye a T1–T5 en producción**, pero demuestra que la lógica desplegada funciona de punta a punta.

| ID | Equivalente local | Resultado |
|---|---|---|
| T1 | `POST /agent/runs {kind: Watch}` con `demo-region` | **Completed** en 5483 ms, `deepseek-chat`, prompt v2, ejecución n8n 38, run `01a147f7-ad37-771b-b1d5-0e4b961762d3`, 7 llamadas a herramientas (todas 200/201) y 2 decisiones con fuente `Llm` |
| T2 | Grafo de la ejecución 38 en el registro de eventos de n8n | Webhook → Load HMAC Key → Verify Signature → Register Execution → Respond Accepted → Load Initial Context → AI Agent → DeepSeek → `find_stock` → `plan_sourcing` → `vehicle_availability` → `propose_routes` → Prepare Result → Send Result; correlación `n8nExecutionId = 38` |
| T3 | Configuración importada | Vigía e incidente con `errorWorkflow` enlazado a «IstpetDev Agent - error»; Schedule del vigía `disabled`; 8 herramientas en cada agente. El workflow de error **se ejecutó de verdad** (ejecuciones 20 y 37: Error Trigger → Resolve AgentRun → Register Failure) cuando los vigías 4 y 21 terminaron por timeout |
| T4 | `GET /agent/runs/{runId}` | Traza saneada en `2026-10-16-local/traza-vigia-local.json`: promptVersion v2, toolCalls con método, ruta, estado y ms, y `source: Llm` en cada decisión |
| T5 | Incidente `cierre-norte` sobre 2 rutas publicadas (una con 2 paradas completadas) | **Completed** en 4654 ms, ejecución n8n 45, run `01a147f9-f0e1-7976-a6a6-987310e7695c`; `get_incident_impact` 200 → `replan` 201; decisión **Escalated** para VEH-01 («Ruta bloqueada al norte; solo HOS-S08. Confirma con operador.») |
| T6 | `POST /webhook/istpetdev/dataset/review` con token válido y `puntos.csv` del fixture de Quito | 200 en 2,2 s, ejecución n8n 47: «Viable para ruteo», importación en seco sin aplicar, 40 filas leídas, 0 errores, 2 acciones seguras para confirmar |
| Plan B | Vigía con el n8n frío y con el dataset de Quito | `n8n_unreachable` (primer webhook > 2 s mientras n8n obtenía el token M2M) y `timeout` (con 40 puntos el agente supera los 10 s); en ambos casos 6 decisiones de plantilla y la API respondió |

### Hallazgos de la réplica local

1. **(#291) El vigía no termina en 10 s con el dataset de Quito (40 puntos):** el agente llama a `find_stock` por cada riesgo (14 llamadas antes del corte) y cae al plan B `timeout`. Con `demo-region` (15 puntos) termina en 5,5 s. Si la demo usa `demo-quito-planificacion`, conviene agrupar la consulta de stock o subir el límite.
2. **(#292) Primer webhook en frío:** la API espera la confirmación de n8n un máximo de 2 s; la primera llamada tras arrancar n8n pidió el token M2M a Cognito y superó ese tiempo (`n8n_unreachable`).
3. **(#290) `POST /demo/reset` falla si hay pedidos:** `DemoResetService.DeleteOperationalDataAsync` no borra `DeliveryOrderLines`, `DeliveryOrders`, `TransferSuggestions`, `RouteStopLines` ni `WarehouseStockPolicies`, que tienen claves foráneas sin cascada hacia puntos, productos, ubicaciones o paradas. En una base con pedidos el reinicio se revierte con `23503`. Riesgo directo para el reinicio de la demo antes del pitch.
4. **(#293) `scripts/agent/local-up.sh` no funciona en una instalación nueva ni en Git Bash para Windows:** `n8n export:workflow --all` devuelve error sin workflows (y `set -e` corta el script), y `docker cp` desde `/tmp` importa 0 workflows por la conversión de rutas. Se completó a mano con los mismos pasos.
