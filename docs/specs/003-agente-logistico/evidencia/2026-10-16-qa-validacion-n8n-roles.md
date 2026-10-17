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
