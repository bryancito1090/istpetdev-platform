# Guía de Desarrollo Local y Catálogo de Endpoints API

Esta guía contiene las instrucciones operativas para levantar el entorno de desarrollo local y el catálogo de endpoints de la API de **IstpetDev**, incluyendo ejemplos de ejecución con `cURL` y `PowerShell`, roles requeridos y contratos de carga útil (payloads).

---

## 1. Entorno de Desarrollo Local

El desarrollo se realiza de forma **aislada y offline** en tu máquina local, sin interactuar con los recursos de AWS RDS ni la nube.

### 1.1. Levantar Contenedores Base (PostgreSQL PostGIS y Redis)
Asegúrate de que Docker Desktop esté en ejecución:
```bash
# Levantar PostGIS (puerto 15432) y Redis (puerto 6379)
docker compose -f infra/compose/docker-compose.local.yml up -d
```

### 1.2. (Opcional) Levantar n8n Localmente para el Agente Logístico
Para probar los flujos del agente logístico con n8n en local:
```bash
docker compose -f infra/compose/docker-compose.local.yml --profile agent up -d
```
* **Interfaz web de n8n:** [http://localhost:5678](http://localhost:5678)

### 1.3. Levantar la API Backend (.NET 8)
```bash
# Opción A: Desde npm en la raíz
npm run dev:api

# Opción B: Directamente con dotnet
dotnet run --project src/Api
```
* **URL Base:** `http://localhost:5000`
* **Swagger UI Interactivo:** [http://localhost:5000/swagger](http://localhost:5000/swagger)
* **Health Check Liveness:** [http://localhost:5000/health/live](http://localhost:5000/health/live)

---

## 2. Autenticación y Matriz de Roles (ADR-10)

Todos los endpoints privados exigen la cabecera HTTP:
```http
Authorization: Bearer <token>
```

Los roles autorizados en el sistema son:
* `Planificador`: Planifica rutas, publica planes, reasigna incidentes y solicita ejecuciones al agente.
* `Conductor`: Consulta sus paradas asignadas y marca entregas/recepciones como completadas.
* `Gerencia`: Monitorea paneles y es el **único rol autorizado** para modificar el modo de autonomía (`PUT /autonomy/mode`).
* `Automatizacion`: Rol exclusivo para clientes máquina a máquina (M2M) como n8n (`istpetdev-api/automation`). Puede disparar replanificaciones, registrar incidentes y consultar riesgos/inventario. **Tiene prohibido explícitamente (403 Forbidden)**:
  1. `POST /api/v1/route-plans/{id}/publish` (publicación de planes)
  2. `POST /api/v1/agent-decisions/{id}/approve` (aprobación humana)
  3. `POST /api/v1/agent-decisions/{id}/reject` (rechazo de decisiones)
  4. `POST /api/v1/route-plans/{id}/revert` (reversión de revisiones)
  5. `PUT /api/v1/autonomy/mode` (cambio de modo de autonomía)
* `Administrador`: Acceso de gestión y mantenimiento.

---

## 3. Catálogo de Endpoints y Ejemplos de Invocación

---

### 3.1. Health Checks (Monitoreo de Proceso y Base de Datos)
Verifica que la API esté viva (no requiere token):
```bash
# cURL
curl -X GET http://localhost:5000/health/live

# PowerShell
Invoke-RestMethod -Uri "http://localhost:5000/health/live" -Method Get
```
**Respuesta (200 OK):**
```json
{
  "status": "Live",
  "service": "IstpetDev API",
  "timestamp": "2026-10-13T18:34:29Z"
}
```

---

### 3.2. Cargar Fixture Regional (`POST /api/v1/demo/reset?fixture=demo-region`)
Inicializa la base de datos con 1 bodega, 15 puntos, 2 vehículos, 3 SKUs, consumos históricos y 240 tramos viales dirigidos.
* **Rol requerido:** `Planificador` o `Administrador`
```bash
curl -X POST "http://localhost:5000/api/v1/demo/reset?fixture=demo-region" \
  -H "Authorization: Bearer $PLANNER_TOKEN"
```
**Respuesta (200 OK):**
```json
{
  "applied": true,
  "importedEntities": 8,
  "rows": 320
}
```

---

### 3.3. Consultar Riesgos de Abastecimiento (`GET /api/v1/risks`)
Calcula coberturas de stock hospitalario y niveles de severidad (`Critico`, `Alto`, `Vigilar`, `Normal`, `SinDatos`) con sus hechos asociados.
* **Rol requerido:** `Planificador`, `Gerencia`, `OperadorLogistico` o `Automatizacion`
* **Filtros query opcionales:** `?level=Alto` o `?level=Critico`
```bash
curl -X GET "http://localhost:5000/api/v1/risks?level=Alto" \
  -H "Authorization: Bearer $PLANNER_TOKEN"
```
**Respuesta (200 OK):**
```json
{
  "items": [
    {
      "servicePointId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "servicePointCode": "HOS-N01",
      "productId": "8bb1b827-0466-4158-b64d-9a6d8591ee89",
      "sku": "DES-01",
      "availableQuantity": 10.0,
      "dailyDemand": 2.0,
      "coverageDays": 5.0,
      "leadTimeDays": 2,
      "safetyMarginDays": 3,
      "suggestedQuantity": 25.0,
      "level": "Alto"
    }
  ],
  "facts": [
    { "code": "stock_coverage_days", "subject": "HOS-N01:DES-01", "value": 5.0, "unit": "dias" }
  ]
}
```

---

### 3.4. Generar Propuesta de Rutas (`POST /api/v1/route-plans`)
Calcula las rutas deterministas asignando vehículos, ventanas horarias y capacidad (kg y m³).
* **Rol requerido:** `Planificador` o `Automatizacion`
```bash
curl -X POST "http://localhost:5000/api/v1/route-plans" \
  -H "Authorization: Bearer $PLANNER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "date": "2026-10-12",
    "includeWatchlist": true
  }'
```
**Respuesta (201 Created):**
```json
{
  "routes": [
    {
      "routePlanId": "7f1e6040-cfc6-43bf-bb2b-1a52e9efdb78",
      "code": "RP-20261012-VEH01",
      "revision": 1,
      "vehicleCode": "VEH-01",
      "stopsCount": 8,
      "totalKm": 42.5,
      "totalMinutes": 185,
      "totalCost": 37.62,
      "hasApproximations": false
    }
  ]
}
```

---

### 3.5. Publicar Ruta y Reservar Inventario FEFO (`POST /api/v1/route-plans/{id}/publish`)
Pasa el plan a estado `Published`, genera las entregas (`Delivery`) e incrementa `StockBalance.QuantityReserved` asociando `RouteStockReservation.Active` ligadas a la revisión.
* **Rol requerido:** `Planificador`
```bash
curl -X POST "http://localhost:5000/api/v1/route-plans/7f1e6040-cfc6-43bf-bb2b-1a52e9efdb78/publish" \
  -H "Authorization: Bearer $PLANNER_TOKEN"
```
**Respuesta (200 OK):**
```json
{
  "routePlanId": "7f1e6040-cfc6-43bf-bb2b-1a52e9efdb78",
  "status": "Published",
  "deliveriesCount": 8
}
```

---

### 3.6. Marcar Parada como Completada (`POST /api/v1/route-plans/{id}/stops/{sequence}/complete`)
Actualiza el avance del vehículo. Congela el prefijo completado antes de cualquier replanificación ante contingencias.
* **Rol requerido:** `Conductor` o `Planificador`
```bash
curl -X POST "http://localhost:5000/api/v1/route-plans/7f1e6040-cfc6-43bf-bb2b-1a52e9efdb78/stops/1/complete" \
  -H "Authorization: Bearer $DRIVER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "completedAt": "2026-10-12T08:45:00Z"
  }'
```
**Respuesta (200 OK):**
```json
{
  "sequenceNumber": 1,
  "status": "Completed"
}
```

---

### 3.7. Registrar Incidente (`POST /api/v1/incidents`)
Registra una contingencia vial o mecánica con georreferenciación y radio de afectación.
* **Rol requerido:** `Planificador` o `Automatizacion`
```bash
curl -X POST "http://localhost:5000/api/v1/incidents" \
  -H "Authorization: Bearer $PLANNER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "kind": "BloqueoVial",
    "lat": -0.1300,
    "lon": -78.4800,
    "radiusKm": 2.5,
    "severity": "ALTA",
    "description": "Derrumbe en tramo norte"
  }'
```
**Respuesta (201 Created):**
```json
{
  "incidentId": "a988d447-e17f-4b09-b6b6-d249fefea174",
  "affectedRoutes": [
    "RP-20261012-VEH01"
  ]
}
```

---

### 3.8. Replanificar Ruta ante Incidente (`POST /api/v1/route-plans/{id}/replan`)
Evalúa el incidente, preserva las paradas ya completadas y genera una nueva revisión de ruta con 3 estrategias posibles (`Strict`, `LateService`, `PrioritizeCritical`).
* **Rol requerido:** `Planificador` o `Automatizacion`
```bash
curl -X POST "http://localhost:5000/api/v1/route-plans/7f1e6040-cfc6-43bf-bb2b-1a52e9efdb78/replan" \
  -H "Authorization: Bearer $PLANNER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "incidentId": "a988d447-e17f-4b09-b6b6-d249fefea174",
    "strategy": "Strict"
  }'
```
**Respuesta (200 OK):**
```json
{
  "routePlanId": "c0182b79-58b9-4a94-81ae-281b3693bb44",
  "code": "RP-20261012-VEH01",
  "revision": 2,
  "autonomyClassification": "AutoExecuted",
  "status": "Published"
}
```

---

### 3.9. Revertir Plan a Revisión Previa (`POST /api/v1/route-plans/{id}/revert`)
Restaura de forma atómica una versión previa creando una tercera revisión (`R3`) y ajustando reservas sin duplicar.
* **Rol requerido:** `Planificador`
```bash
curl -X POST "http://localhost:5000/api/v1/route-plans/c0182b79-58b9-4a94-81ae-281b3693bb44/revert" \
  -H "Authorization: Bearer $PLANNER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "targetRevision": 1
  }'
```
**Respuesta (200 OK):**
```json
{
  "routePlanId": "e2270921-b4f0-4d43-9831-294713c7bb99",
  "code": "RP-20261012-VEH01",
  "revision": 3,
  "status": "Published"
}
```

---

### 3.10. Listado Paginado de Planes para Panel Web (`GET /api/v1/route-plans`)
Consulta de lectura estable para el panel de operaciones (Spec 005 / FR-23) con cursor no mutante.
* **Rol requerido:** Cualquier usuario autenticado
* **Filtros query:** `?date=2026-10-12&status=Published&limit=10`
```bash
curl -X GET "http://localhost:5000/api/v1/route-plans?date=2026-10-12&limit=10" \
  -H "Authorization: Bearer $PLANNER_TOKEN"
```
**Respuesta (200 OK):**
```json
{
  "items": [
    {
      "routePlanId": "7f1e6040-cfc6-43bf-bb2b-1a52e9efdb78",
      "code": "RP-20261012-VEH01",
      "revision": 1,
      "status": "Published",
      "vehicleCode": "VEH-01",
      "km": 42.5,
      "minutes": 185,
      "cost": 37.62,
      "hasApproximations": false
    }
  ],
  "nextCursor": null
}
```

---

### 3.11. Consultar y Cambiar Modo de Autonomía (`GET` / `PUT /api/v1/autonomy/mode`)
* **Consultar:** Cualquier rol autenticado.
* **Modificar:** **Exclusivo del rol `Gerencia`**.
```bash
# Consultar modo actual
curl -X GET "http://localhost:5000/api/v1/autonomy/mode" \
  -H "Authorization: Bearer $PLANNER_TOKEN"

# Cambiar a ProposeOnly (Solo propuesta, requiere token de Gerencia)
curl -X PUT "http://localhost:5000/api/v1/autonomy/mode" \
  -H "Authorization: Bearer $MANAGER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "mode": "ProposeOnly" }'
```

---

## 4. Nuevos Endpoints del Agente Logístico (Spec 003 / Issue #36)

Estos endpoints comunican la API con el agente n8n y su red de seguridad fail-safe:

| Método y Ruta | Rol | Descripción |
|---|---|---|
| `POST /api/v1/agent/runs` | `Planificador`, `Automatizacion` | Inicia una ejecución (`Watch` o `Incident`), dispara el webhook HMAC a n8n y espera hasta 10 s; si n8n no responde, ejecuta el **fallback determinista** en ≤ 11 s. |
| `GET /api/v1/agent/runs/{id}` | Cualquier rol | Consulta la traza auditable (herramientas usadas, latencia en ms, modelo, versión de prompt y decisiones). |
| `GET /api/v1/agent/runs?limit=20` | Cualquier rol | Historial de ejecuciones del agente. |
| `POST /api/v1/agent/runs/{id}/result` | `Automatizacion` (exclusivo) | Invocado por n8n para enviar la explicación validada y el mensaje al conductor. Rechaza si la ejecución ya cayó en fallback (409) o es inválida (422). |
| `GET /api/v1/incidents/{id}` | Cualquier rol | Retorna el detalle e impacto evaluado del incidente sobre las rutas activas. |

### 4.1. Iniciar Ejecución del Agente (`POST /api/v1/agent/runs`)

```bash
# Ejecutar monitoreo preventivo (Watch)
curl -X POST "http://localhost:5000/api/v1/agent/runs" \
  -H "Authorization: Bearer $PLANNER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "kind": "Watch" }'

# Ejecutar resolución de contingencia ante incidente (Incident)
curl -X POST "http://localhost:5000/api/v1/agent/runs" \
  -H "Authorization: Bearer $PLANNER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "kind": "Incident", "incidentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }'
```

Respuesta (200 OK):
```json
{
  "runId": "4c9e89d1-37d4-4e4b-97e3-0d5108b6bc37",
  "kind": "Watch",
  "status": "Completed",
  "latencyMs": 1420,
  "model": "gpt-4o",
  "promptVersion": "v1.2",
  "n8nExecutionId": "exec-1049",
  "fallbackReason": null,
  "toolCalls": [
    {
      "method": "GET",
      "path": "/api/v1/risks",
      "status": 200,
      "milliseconds": 45,
      "atUtc": "2026-10-13T20:00:01Z"
    }
  ],
  "decisions": [
    {
      "agentDecisionId": "...",
      "vehicleCode": "",
      "outcome": "Evaluated",
      "explanation": "El puesto HOS-N01 presenta riesgo crítico con cobertura de 5,0 días...",
      "driverMessage": "Alerta de inventario en HOS-N01",
      "source": "Llm",
      "mismatchReason": null,
      "facts": []
    }
  ]
}
```

---

### 4.2. Consultar Detalle de Incidente (`GET /api/v1/incidents/{id}`)

```bash
curl -X GET "http://localhost:5000/api/v1/incidents/{incidentId}" \
  -H "Authorization: Bearer $PLANNER_TOKEN"
```

Respuesta (200 OK):
```json
{
  "incidentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "kind": "BloqueoVial",
  "status": "Active",
  "severity": "ALTA",
  "description": "Derrumbe en tramo norte",
  "occurredAtUtc": "2026-10-12T10:15:00Z",
  "radiusKm": 2.5,
  "affectedRoutes": [
    {
      "routePlanId": "...",
      "vehicleCode": "VEH-01",
      "affectedLegs": [{ "from": "DEP-01", "to": "HOS-N01" }],
      "affectedStops": [1, 2]
    }
  ]
}
```

---

### 4.3. Enviar Resultado del Agente (`POST /api/v1/agent/runs/{id}/result`)
* **Rol requerido:** Exclusivo de `Automatizacion` (`istpetdev-api/automation`).

```bash
curl -X POST "http://localhost:5000/api/v1/agent/runs/{runId}/result" \
  -H "Authorization: Bearer $AUTOMATION_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "decisions": [
      {
        "agentDecisionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "explanation": "Desvío aplicado por derrumbe. Demora estimada de 15 minutos.",
        "driverMessage": "Ruta desviada por derrumbe (+15 min). Continuar con precaución."
      }
    ],
    "summary": "Replanificación evaluada y explicada",
    "model": "gpt-4o",
    "promptVersion": "v1.2",
    "n8nExecutionId": "exec-1049",
    "agentError": null
  }'
```

Respuestas:
* `202 Accepted`: Resultado recibido dentro del plazo y validado con éxito.
* `409 Conflict`: La ejecución ya venció el timeout de 10 s y el fallback ya fue ejecutado.
* `422 Unprocessable Entity`: La decisión o carga útil referenciada es inválida.

---

### 4.4. Cabecera de Observabilidad `X-Agent-Run-Id`
Cualquier llamada que realice n8n (rol `Automatizacion`) a endpoints de la API puede incluir:
```http
X-Agent-Run-Id: <Guid-del-Run>
```
El middleware `AgentRunTraceMiddleware` captura automáticamente el método, ruta, status code y tiempo en milisegundos y lo adjunta de forma duradera a `AgentRun.ToolCallsJson` para auditoría total en el panel de control.

