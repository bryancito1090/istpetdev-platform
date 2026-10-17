# Evidencia de Medición y Aceptación — Issue #291 (17-oct-2026)

Validación de la corrección del timeout del vigía (`Watch`) con el fixture `demo-quito-planificacion` y preservación de herramientas/permisos en IstpetDev.

---

## 1. Contexto y Estado del Entorno

- **Commit base:** `f8f85e9a32e04942ca716cbad63b8b65ffea5852` (origin/develop)
- **Rama:** `JosephBano/pickerel`
- **Versión de prompt:** `v2` ([automation/n8n/prompts/agente-logistico-v2.md](../../../automation/n8n/prompts/agente-logistico-v2.md))
- **Workflows regenerados:** [automation/n8n/workflows/vigia.json](../../../automation/n8n/workflows/vigia.json), [automation/n8n/workflows/incidente.json](../../../automation/n8n/workflows/incidente.json)
- **Configuración de timeout:** `Agent:TimeoutSeconds=10` (invariable en backend; no se modificó para falsear la prueba)

---

## 2. Comprobaciones Estructurales y Pruebas Automatizadas Ejecutadas

### 2.1 Comprobaciones Estructurales

1. **Sincronización de workflows y verificación de secretos:**
   - **Comando:** `python3 scripts/agent/build-workflows.py --check`
   - **Resultado:** `Exit Code 0`
   - **Detalle:** Los workflows JSON coinciden de forma determinista con las plantillas y el prompt v2. Se confirmó la ausencia de credenciales o tokens literales (`assert_no_embedded_secrets`).

2. **Inspección del grafo de herramientas en n8n:**
   - **Verificación:** Inspección programática de nodos y conexiones en `vigia.json` e `incidente.json`.
   - **Resultado:** Las 8 herramientas (`get_risks`, `find_stock`, `plan_sourcing`, `vehicle_availability`, `propose_routes`, `get_route_plan`, `get_incident_impact`, `replan`) continúan presentes como nodos `toolWorkflow` y conectadas como `ai_tool` al `AI Agent` (8/8 en ambos workflows).

### 2.2 Pruebas Automatizadas de la API y Agente

1. **Pruebas de consultas logísticas (`LogisticsQueriesTests`):**
   - **Comando:** `dotnet test --filter "FullyQualifiedName~LogisticsQueriesTests"`
   - **Resultado:** `Passed! 8/8 superados (12 s)`
   - **Cobertura real de la suite:**
     - Consulta de stock (`/api/v1/logistics/stock`): aplicación de stock de seguridad, orden FEFO, exclusión de vencidos/bloqueados y presencia de bodegas vacías.
     - Consulta de sourcing (`/api/v1/logistics/sourcing`): resolución por pedidos (ordenes individuales y múltiples bodegas, faltantes) y por puntos de servicio (cálculo de riesgos sin persistir pedidos).
     - Consulta de vehículos disponibles (`/api/v1/logistics/vehicles/available`): respeto de zona horaria operativa, frontera de medianoche, estado publicado y tarifa base.
     - Validaciones de entrada: códigos 400, 404, 422, límites de IDs y aislamiento por organización.
     - Control de acceso: permitido para rol Planificación y M2M técnico (`istpetdev-api/automation`); rechazo 401 a anónimos y 403 a roles no autorizados.
     - Inmutabilidad y auditoría: lectura estricta sin mutación de estado operativo y registro de traza en `AgentRun` exclusivamente ante peticiones M2M con `X-Agent-Run-Id`.
   - *Nota de alcance:* Esta suite valida las consultas de logística y su control de acceso; no cubre los endpoints de generación de rutas ni replanificación (`/route-plans`, `/incidents`), los cuales se cubren en `AgentJourneyTests`.

2. **Pruebas de flujo y fallbacks (`AgentJourneyTests`):**
   - **Comando:** `dotnet test --filter "FullyQualifiedName~AgentJourneyTests"`
   - **Resultado:** `Passed! 10/10 superados (21 s)`
   - **Cobertura real de la suite:**
     - Ciclo de vida de `Watch` e `Incident` con integración de fallbacks deterministas.
     - Resistencia a inyección de prompt con `demo-region-injection`.
     - Denegación estricta (403) ante intentos de acciones humanas desde la cuenta M2M (publicar, aprobar, rechazar, revertir o alterar modo de autonomía).
     - Activación correcta de fallbacks ante `timeout`, `n8n_unreachable`, `agent_disabled` y `agent_error`.
     - Rechazo con código `409 Late` a resultados extemporáneos.

---

## 3. Estado de la Medición en Vivo con n8n y DeepSeek

Para la ejecución de corridas reales con n8n y el modelo `deepseek-chat`, se verificó la disponibilidad de infraestructura:

- **Contenedores de base de datos:** `istpetdev-local-db` (PostGIS 15432) y `istpetdev-local-redis` (Redis 6379) operativos.
- **Bloqueo identificado:** No existe el archivo `.env` local. Faltan las siguientes credenciales requeridas por `scripts/agent/local-up.sh`:
  - `DEEPSEEK_API_KEY`: clave de API para la llamada al modelo DeepSeek.
  - `COGNITO_M2M_CLIENT_ID`, `COGNITO_M2M_CLIENT_SECRET`, `COGNITO_TOKEN_URL`: credenciales M2M para autenticación de n8n frente a la API.
  - `N8N_ENCRYPTION_KEY`, `ISTPETDEV_WEBHOOK_SECRET`: secretos de cifrado y validación de firma HMAC local.
- **Contenedor n8n:** `istpetdev-local-n8n` no desplegado.

Por tanto, las corridas en vivo no se pudieron ejecutar en este ciclo y quedan explícitamente registradas como **Pendientes**.

### Tabla de Corridas de Aceptación

| Corrida | Fixture | runId | n8nExecutionId | Estado Final | fallbackReason | Latencia AgentRun | Tiempo HTTP Cliente | Herramientas Invocadas | Consultas `/logistics/stock` | Decisiones / Fuente | n8n / Modelo / Prompt | Estado de Medición |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Calentamiento** | `demo-quito-planificacion` | — | — | — | — | — | — | — | — | — | 2.39.5 / deepseek-chat / v2 | **Pendiente** (corrida de warm-up para descartar cold start #292) |
| **Quito #1** | `demo-quito-planificacion` | — | — | — | — | — | — | — | — | — | 2.39.5 / deepseek-chat / v2 | **Pendiente** |
| **Quito #2** | `demo-quito-planificacion` | — | — | — | — | — | — | — | — | — | 2.39.5 / deepseek-chat / v2 | **Pendiente** |
| **Quito #3** | `demo-quito-planificacion` | — | — | — | — | — | — | — | — | — | 2.39.5 / deepseek-chat / v2 | **Pendiente** |
| **demo-region** | `demo-region` | — | — | — | — | — | — | — | — | — | 2.39.5 / deepseek-chat / v2 | **Pendiente** |

> **Referencia histórica previa:** La corrida de `demo-region` documentada el 16 de octubre (5483 ms, Completed, ejecución 38) se ejecutó con el prompt anterior que aún ordenaba `find_stock` por producto; sirve como antecedente de funcionamiento del pipeline, pero no constituye validación del cambio actual.

---

## 4. Estado de los Criterios de Aceptación

| Criterio | Exigencia | Estado | Evidencia / Justificación |
|---|---|---|---|
| **Criterio 1** | Tres corridas consecutivas de Quito en estado `Completed`, sin fallback, en ≤ 11 s cada una | **Pendiente de medición en vivo** | Requiere entorno local con n8n y DeepSeek configurados en `.env`. El cambio de prompt elimina la instrucción que provocaba las consultas reiteradas a `find_stock` por producto; el comportamiento real del modelo y la latencia observada siguen pendientes de medición en vivo en el cronómetro. |
| **Criterio 2** | Al menos una corrida de `demo-region` en estado `Completed`, sin fallback, en ≤ 11 s | **Pendiente de medición en vivo** | Requiere el mismo entorno real. La medición previa del 16-oct corresponde al prompt anterior. |
| **Criterio 3** | Conservar las 8 herramientas disponibles y con los mismos permisos | **Cumplido** | • **Inspección de grafo:** 8/8 herramientas presentes y conectadas como `ai_tool` al `AI Agent` en `vigia.json` e `incidente.json`.<br>• **Permisos técnicos:** `LogisticsQueriesTests` (8/8) y `AgentJourneyTests` (10/10) pasando satisfactoriamente con scope `istpetdev-api/automation`. |

---

## 5. Instrucciones para Ejecutar la Medición en Vivo

Cuando se disponga de las credenciales de desarrollo, seguir estos pasos para completar la matriz:

1. **Configurar el archivo `.env`:**
   Copiar `.env.example` a `.env` y completar los valores requeridos (`N8N_IMAGE=docker.n8n.io/n8nio/n8n:2.39.5`, claves de cifrado/webhook, endpoints y credenciales de Cognito y `DEEPSEEK_API_KEY`).
2. **Levantar el contenedor de n8n e importar workflows:**
   ```bash
   bash scripts/agent/local-up.sh
   ```
   Verificar que `curl -f http://localhost:5678/healthz` responda correctamente.
3. **Ejecutar la corrida de calentamiento (excluir de la aceptación):**
   - Resetear fixture: `POST /api/v1/demo/reset?fixture=demo-quito-planificacion`
   - Iniciar Watch: `POST /api/v1/agent/runs {"kind": "Watch"}`
   - Descartar esta corrida (permite que n8n obtenga el token inicial M2M de Cognito y mitiga #292).
4. **Gestión del fixture y riesgo de reinicio (#290):**
   - El endpoint `POST /api/v1/demo/reset?fixture=demo-quito-planificacion` puede fallar con error HTTP 500 / código PostgreSQL `23503` (`foreign_key_violation`) si en la base existen pedidos operativos (`DeliveryOrders`, `DeliveryOrderLines`, etc.) debido a claves foráneas sin eliminación en cascada.
   - **Detección del fallo:** Si el llamado a `demo/reset` responde con código distinto a `200 OK` o error `23503`:
     1. Detener inmediatamente la medición (no continuar las corridas con estado inconsistente).
     2. No ejecutar borrados manuales en bases de datos compartidas ni en otros entornos.
     3. Preparar una base local aislada y limpia en PostgreSQL local (por ejemplo, recreando una base de datos local aislada como `istpetdev_dev_05` y aplicando migraciones con rol migrador local sin privilegios especiales).
     4. Cargar nuevamente el fixture limpio antes de reanudar las corridas.
5. **Ejecutar las tres corridas consecutivas de Quito:**
   - Asegurar que la base esté en el estado inicial de `demo-quito-planificacion`.
   - Por cada una de las 3 corridas consecutivas (sin intercalar otro dataset):
     - Iniciar Watch cronometrando el tiempo de respuesta HTTP desde el cliente: `POST /api/v1/agent/runs {"kind": "Watch"}`.
     - Consultar `GET /api/v1/agent/runs/{runId}` para verificar:
       - `status == "Completed"` y `fallbackReason == null`.
       - Duración reportada (`latencyMs`) y tiempo cliente ≤ 11 s.
       - Herramientas en `toolCalls` y confirmar ausencia de llamadas rutinarias a `/logistics/stock`.
       - Número de decisiones y `source: "Llm"` en cada una.
6. **Ejecutar la corrida de `demo-region`:**
   - Cargar `demo-region` en la base local aislada.
   - Iniciar Watch cronometrando el tiempo cliente: `POST /api/v1/agent/runs {"kind": "Watch"}`.
   - Consultar `GET /api/v1/agent/runs/{runId}` y registrar los mismos campos.
7. **Actualizar la tabla de resultados en este archivo de evidencia sin incluir tokens ni secretos.**
