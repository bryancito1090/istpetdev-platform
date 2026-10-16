---
tipo: automatizacion
estado: propuesta
actualizado: 2026-10-15
tags: [hackathon, istpetdev]
---

# n8n e IA

## Función de cada pieza

La API calcula riesgo y conserva estado; n8n coordina ejecuciones y proveedores; DeepSeek explica entradas/cálculos existentes. Una caída del LLM no debe detener alertas, despacho o recepción.

La propuesta original planteaba enviar inventario a IA para predecir stock. La recomendación es **separar pronóstico numérico y explicación**, evitando que un texto no reproducible sea la fuente del saldo o la fecha de agotamiento.

## WF-01 — riesgo y explicación programados

| Paso / nodo | Entrada | Salida |
|---|---|---|
| Schedule Trigger | Frecuencia/timezone del entorno | runId y corte temporal |
| HTTP Request a API | Identidad técnica y clave idempotente de ejecución | Snapshot de riesgos calculados |
| Filtrado | Cambios/nuevos riesgos, reglas de frecuencia | Casos que necesitan explicación |
| Preparación de contexto | Stock, demanda, lead time, incertidumbre y referencias | Datos mínimos anonimizados |
| HTTP Request a DeepSeek | Modelo/configuración y promptVersion | JSON de explicación |
| Validación | JSON, campos, longitud y coherencia con cifras | Explicación válida o fallback |
| HTTP Request a API | riskId/runId/explicación y proveniencia | Registro durable |
| Notificación | Evento de la API tras commit | Web/móvil refrescan riesgo |

Un cálculo no se duplica por ejecutar dos cron accidentalmente; la API usa runId/periodo y sus controles.

## WF-02 — incidente de demostración

Activación controlada desde una acción demo o webhook autenticado. Registra por API un incidente etiquetado **synthetic/demo**, afecta matriz/restricciones, solicita replanificación y publica nueva versión después de aprobación.

No escribir directamente en tablas operativas. El mapa debe mostrar cambios calculados, no solo un marcador. Limitar el generador al entorno/demo autorizado y hacerlo reproducible por scenarioId.

## WF-03 — recuperación y salud

Detectar fallos de proveedor, registrar executionId/correlationId, limitar reintentos, aplicar espera y activar la explicación determinista. La cola de reintentos no debe reenviar cientos de veces datos repetidos.

Métricas: duración, errores, reintentos, casos explicados, tokens/costo y porcentaje de fallback. No enviar mensajes externos por correo/WhatsApp sin una acción autorizada e implementada.

## Salida de IA

Contrato propuesto:

```json
{
  "riskId": "REFERENCIA_EXISTENTE",
  "explanation": "La cobertura calculada coincide con el horizonte de reposición.",
  "recommendedAction": "Revisar y aprobar el abastecimiento propuesto.",
  "uncertainties": ["El consumo usa un historial corto."]
}
```

Es un ejemplo de estructura, no respuesta ya generada. La API conserva sus cifras deterministas y no acepta valores del LLM como actualización del stock.

DeepSeek dispone de modo JSON, pero la documentación advierte de respuestas vacías; JSON válido tampoco equivale a validación de nuestro esquema. Comprobar parseo, campos y consistencia. [JSON Output de DeepSeek](https://api-docs.deepseek.com/guides/json_mode/).

## Prompt base

Indicar que el modelo es un asistente explicativo. Presentar datos como datos, no instrucciones. Pedir español breve, referencias al riesgo, incertidumbres y acción permitida. Prohibir inventar cantidades/fechas, diagnosticar fraude o afirmar merma confirmada sin evidencia.

Versionar prompt y modelo seleccionado en la implementación. El nombre/modelo y costos están pendientes de verificación al configurar la cuenta.

Fallback ejemplo: «Cobertura estimada: 5 días; lead time: 2 días; margen: 3 días. Se requiere decidir ahora para cubrir el horizonte configurado». Sale del backend, sin proveedor externo.

## Hosting de n8n

Persistir workflows/ejecuciones en PostgreSQL separado por base/esquema y usar una clave de cifrado estable protegida en secretos. No perder la clave al reemplazar el contenedor. Exportar workflows versionados sin credenciales.

Instancia principal única para demo; no autoescalar varios principales independientes. Si se adopta queue mode: Redis y workers con la misma clave, almacenamiento y capacidades de la edición verificados. La disponibilidad de la instancia principal requiere diseño separado; añadir workers no la vuelve redundante. [Queue mode de n8n](https://docs.n8n.io/hosting/scaling/queue-mode).

## Criterios de cierre

- Workflow exportado, versionado y reproducible.
- Credenciales referenciadas, no incrustadas.
- Reejecución no duplica incidentes ni explicación.
- Timeout, respuesta vacía/incorrecta y caída de n8n no bloquean P0.
- Datos sintéticos y operativos separados.
- Modelo, prompt y ejecución enlazados a cada explicación.

Ver [[Contratos API y eventos]] y [[Catalogo y plan de skills]].

## Hosting confirmado y configuración posterior — 7 de octubre de 2026

Bryan ya tiene servidor **https://n8n.bryan-bano.com/** y cargará las credenciales. Esta integración usa ese servidor y no añade una segunda instancia obligatoria al host demo. Workflows exportados sin credenciales en `automation/n8n/workflows/`; la DB/clave de cifrado del servidor permanece administrada por Bryan.

API técnica autenticada con Cognito M2M/rol Automatización, cron `America/Guayaquil`, límites/reintentos y pausa cuando AWS esté apagado: [[Identidad OIDC y sesiones]] y [[Entornos y operacion acordados]], sección 7. Un n8n externo no alcanza el localhost del portátil: integración contra API demo HTTPS o túnel temporal autenticado; el cálculo/fallback local funciona sin n8n. Modelo/prompt/credenciales se cargan por Bryan; no se ha accedido ni modificado el servidor en esta entrega.

## Agente con herramientas — propuesta del 12 de octubre de 2026

La sesión de definición propone que WF-01 y WF-02 evolucionen a dos workflows de agente, `vigia` e `incidente`, con un nodo AI Agent cuyas herramientas son endpoints del motor logístico (ADR-22, [[Definiciones de producto y plan hasta el pitch]]). Se conservan los principios de esta nota: la API calcula y conserva el estado; n8n coordina; el LLM no cambia stock, prioridades ni fechas; una caída del LLM no detiene la operación (la API responde a los 10 s con el camino determinista). Cambian tres cosas: el LLM elige qué herramientas llamar; la política determinista del motor decide qué se publica solo; y cada cifra del texto se valida contra los datos calculados. Proveedor: DeepSeek, con prueba de aceptación. Workflows y prompt versionados en `automation/n8n/`; detalle en el spec 003 del repositorio (`docs/specs/003-agente-logistico/`).


## Histórico de la entrega inicial de #37 — 14 de octubre de 2026

La rama `feature/37-n8n-workflows-local` contiene prompt, plantillas, workflows
DeepSeek nativos, generador, perfil Docker `agent`, arranque y aceptación local.
Todavía no está integrada en `develop`. La versión confirmada por Bryan es
`2.39.5`; la configuración de su servidor sigue en `latest` y #39 debe fijarla.

Se importaron y publicaron los workflows en el n8n local. En el servidor de
Bryan se crearon borradores de `vigia` e `incidente` en su proyecto personal,
ambos inactivos y con `DeepSeek account` enlazada. Parámetros y conexiones
coinciden con el repositorio; la instalación oficial seguirá el SHA integrado
en #39. El Schedule de vigía permanece deshabilitado.

Bryan revisó y autorizó el plan dirigido de #38; se publicó su resumen y se
aplicó: 1 recurso añadido, 1 actualizado, 0 destruidos. El cliente M2M ya existe
y emite token con scope `istpetdev-api/automation`; el secreto está en el `.env`
local ignorado. La configuración y validación de la API de producción siguen
pendientes en #38. Bryan confirmó la clave DeepSeek local; su uso real se está
verificando. No existe aún informe de 30 ejecuciones reales ni
ensayo CE-06 registrado en la laptop (drill Plan B: fallback en 21 ms con n8n caído, Completed en 6661 ms con n8n levantado).

El seguimiento y las instrucciones de obtención de credenciales se mantienen
en [task.md](../../specs/003-agente-logistico/task.md) y
[guia-cierre-37.md](../../specs/003-agente-logistico/guia-cierre-37.md).
No se cambian el papel determinista de la API, las cinco herramientas, el
control humano. La aprobación posterior de ADR-22 y el estado vigente se
registran a continuación.

El bloqueo inicial P0 (fallback agent_error en vigía) fue resuelto:
1. El AI Agent y los nodos de herramientas se enlazaron con DeepSeek nativo en `build-workflows.py`.
2. Se eliminó la carrera de lectura en `AgentRunService` persistiendo decisiones antes de marcar `Completed`.
3. Se verificó `applied=true` en cada reset.
4. Se ejecutó la matriz de aceptación de 30 corridas reales consecutivas en API 5017 contra DeepSeek nativo (reporte `artifacts/agent-acceptance/20261014-150147-176964/report.md`): 0 fallbacks, 100% herramientas 2xx (60/60), p95 9127 ms (<=10 s), 96% decisiones LLM (48/50) y 10/10 inyecciones neutralizadas con idéntica política y 0 herramientas prohibidas.

Entrega verificada localmente y transferida a Codex para revisión e integración en `develop` vía PR; el despliegue en producción permanece planificado en [plan-cierre-39-produccion.md](../../specs/003-agente-logistico/plan-cierre-39-produccion.md).


## Estado vigente de preparación #39 — 14 de octubre de 2026, 20:13 Guayaquil

Workflows de #37 integrados en `8c99a71` (PR #74), transporte #81 y M2M #85
integrados en develop/main por #86; CD `38658331163` exitoso para `a45a548`.
ADR-22 aceptada; la matriz histórica local no sustituye la productiva.

n8n de Bryan: Compose/runtime fijados a 2.39.5, DeepSeek nativo, vigía/incidente
publicados del SHA oficial, credenciales remapeadas también en herramientas,
Schedule apagado. HTTPS/readiness 200 desde servidor/contenedor; HMAC inválido,
expirado y alterado rechazado 401; positivo 202 en probe sin modelo. Presupuesto
DeepSeek confirmado por Bryan, quien terminó las ediciones de Antigravity.

Bryan completó contraseña/MFA de su cuenta propia y autorizó registrar dos
Account en RDS productivo `istpetdev_integration`: Planificador humano y
Automatizacion técnica. Cuenta humana: /me y /risks 200; **nodo HTTP n8n con
IstpetDev M2M: risks 200**; prueba eliminada. Las cinco acciones de decisión
humana responden 403 con M2M. No se publican identidades privadas ni JWT.

Ventana exclusiva autorizada. Persisten gates: Demo.Enabled false (probe sin
escritura 404); Agent.Enabled false; Completed/Fallback/recuperación, matriz
oficial de 30 corridas y CE-03 pendientes. La regresión local detectó que el
preset solo afecta un vehículo y que ReplanAsync ignoraba prefijos/efectos/modos;
la corrección pasa regresiones locales; calibración autorizada: VEH-01 automático, VEH-02 escalado. Spec/plan 002 y fixture actualizados, sin promoción productiva acreditada.
Intento de reaplicación `38665762599` falló antes de recrear servicios por
permiso GHCR ausente en rollback.yml. Se prepara packages:read y transporte
explícito DEMO_ENABLED=false por defecto en ambos pipelines. #39 sigue abierta.

Fuentes: [evidencia de preparación](../../specs/003-agente-logistico/evidencia/2026-10-14-preparacion-39.md),
[task.md](../../specs/003-agente-logistico/task.md), [[Runbook de DevOps y base de datos para la demo]]
y [[Registro de seguimiento y hallazgos]]. Las secciones anteriores son registros
históricos fechados, no el estado de cierre actual.

## Cierre productivo verificado — 15 de octubre de 2026

En la ventana exclusiva 08:00–10:00 (Guayaquil) se completó la aceptación de
#39 con evidencia real: matriz oficial 30/30 (herramientas 72/72, p95 7804 ms,
decisiones LLM 58/60, inyección 10/10), Completed/Fallback/recuperación sobre
la API productiva y CE-03 con dos decisiones (VEH-01 AutoExecuted y VEH-02
Escalated por CLI-S09, revisión escalada sin ejecutar). Los workflows publicados
incorporan el tuning de tiempos/reintentos sincronizado desde el repo
(`5c8a5a77…` vigía, `209bd29b…` incidente) con credenciales remapeadas y
Schedule apagado. Estado final: agente activo, incidente automático apagado
(`AGENT_AUTO_ON_INCIDENT=false`), demo apagada; runbook y evidencia en
[evidencia/2026-10-15-cierre-39.md](../../specs/003-agente-logistico/evidencia/2026-10-15-cierre-39.md).
Las secciones anteriores son registros históricos fechados.

## Manejo de fallos HZ-07 / issue #113 — 15 de octubre de 2026

`error-agente.json` inicia con Error Trigger y usa la misma referencia de
credencial OAuth2 M2M que los workflows de agente. El Error Trigger aporta
`execution.id`, workflow, nodo que falló y mensaje; no entrega el `runId` del
webhook original. Por eso la respuesta 202 de `vigia` e `incidente` incluye el
ID de ejecución. Tras validar HMAC, los workflows lo registran con M2M en la API
antes de continuar al nodo de IA, evitando que un fallo inmediato alcance al
handler antes de que exista la asociación. La API también lo toma de la respuesta
202 como respaldo. El handler consulta únicamente el `runId` asociado y envía
`{ runId, executionId, workflow, node, message }` a
`POST /api/v1/agent/runs/{id}/failure`; el callback agrega una entrada
idempotente a la traza sin adjuntar el payload del escenario ni el stack.

`settings.errorWorkflow` usa `{{ERROR_WORKFLOW_ID}}` en los exports. El ID se
configura en la instancia objetivo y no se guarda en Git. El editor/API de n8n
puede crearlo; **el CLI 2.39.5 exige un ID al importar**. `local-up.sh` consulta
primero el workflow existente por nombre; si no existe, genera un UUID dentro
del contenedor y lo aplica solamente al JSON temporal de importación y a los
ajustes de `vigia` e `incidente`. Las importaciones posteriores reutilizan ese
ID. En el runtime 2.39.5 se comprobó que el workflow de error también debe
publicarse: sin ello n8n lo rechaza como inactivo. El arranque publica los tres
workflows y reinicia la instancia local. Se conserva la espera/fallback de
10 s de ESC-16/ESC-17.

El cierre inicial por #147 se reabrió porque faltaba probar un Error Trigger
real. La validación complementaria utiliza Docker Linux, n8n 2.39.5 y
API/PostGIS aislados, con identidad OAuth sintética y la misma referencia M2M;
no cambia la instancia productiva. Resultados en
[task.md](../../specs/003-agente-logistico/task.md).

El formato de Error Trigger y el vínculo por configuración de workflow siguen
la [documentación oficial de n8n](https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.errortrigger/).

## Herramientas de abastecimiento del agente — issue #195, 16 de octubre de 2026

El agente pasa de cinco a **ocho herramientas** `toolWorkflow` en `vigia` e
`incidente` (prompt **v2** en `automation/n8n/prompts/agente-logistico-v2.md`):
se suman `find_stock`, `plan_sourcing` y `vehicle_availability`, que consumen
las consultas de solo lectura de #194 bajo `/api/v1/logistics`. El LLM sigue
eligiendo herramientas y explicando; cantidades, asignaciones, prioridades y
rutas las calcula el motor (ADR-22).

Recorrido de Watch en el prompt: `get_risks` → `find_stock` por producto →
`plan_sourcing` con puntos/pedidos reales → `vehicle_availability` una sola vez
→ `propose_routes` → explicación. Incident conserva `get_incident_impact` y
`replan` por ruta afectada, y usa las consultas nuevas solo cuando evalúa
reposición o sustitución; no reabastece carga reservada/en tránsito.

Reglas de seguridad de los wrappers (las ocho herramientas):

- `apiBaseUrl` desde `$env.ISTPETDEV_API_BASE_URL`; `runId`, `kind`,
  `incidentId` y `deadlineUtc` siempre del payload firmado. Los argumentos del
  modelo pasan por allowlist por herramienta (UUIDs, listas acotadas a 100 y
  deduplicadas); no se reenvía el objeto arbitrario.
- Misma credencial OAuth2 M2M, cabecera `X-Agent-Run-Id` del contexto firmado y
  HMAC, ACK 202, registro de ejecución y Error Trigger sin cambios.
- Timeout HTTP dinámico `min(15000, restante − 1200)` con mínimo 1000 ms en las
  ocho herramientas y margen para el callback; se conserva el guard de 3200 ms
  restantes y la omisión con motivo (`skipped`) cuando la llamada no cabe.
- `vehicle_availability` no acepta fecha: el contexto firmado no autoriza otro
  día, así que consulta el reloj operativo del servidor. La extensión opcional
  `criticalDeadline` de #193 queda fuera mientras ese campo no exista en
  `PlanRequest` publicado.

Evidencia determinista en `GET /api/v1/agent/runs/{id}`: por decisión se
exponen `originWarehouseCode`/`originWarehouseName` (bodega base real del
vehículo, `Vehicle.DepotLocationId`), `vehicleCode` y `orderedStops` (secuencia
real de paradas del plan propuesto), derivados por servidor del plan
correlacionado; nunca de texto libre del LLM. La secuencia final de visita se
distingue de la prioridad de asignación del motor. Detalle de contratos y
evidencia en `docs/specs/003-agente-logistico/evidencia/2026-10-16-195-herramientas-n8n.md`.
