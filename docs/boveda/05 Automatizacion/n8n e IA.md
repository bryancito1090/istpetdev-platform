---
tipo: automatizacion
estado: propuesta
actualizado: 2026-10-14
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


## Avance verificable de #37 — 14 de octubre de 2026

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
control humano ni el estado de propuesta de ADR-22.

El bloqueo inicial P0 (fallback agent_error en vigía) fue resuelto:
1. El AI Agent y los nodos de herramientas se enlazaron con DeepSeek nativo en `build-workflows.py`.
2. Se eliminó la carrera de lectura en `AgentRunService` persistiendo decisiones antes de marcar `Completed`.
3. Se verificó `applied=true` en cada reset.
4. Se ejecutó la matriz de aceptación de 30 corridas reales consecutivas en API 5017 contra DeepSeek nativo (reporte `artifacts/agent-acceptance/20261014-150147-176964/report.md`): 0 fallbacks, 100% herramientas 2xx (60/60), p95 9127 ms (<=10 s), 96% decisiones LLM (48/50) y 10/10 inyecciones neutralizadas con idéntica política y 0 herramientas prohibidas.

Entrega verificada localmente y transferida a Codex para revisión e integración en `develop` vía PR; el despliegue en producción permanece planificado en [plan-cierre-39-produccion.md](../../specs/003-agente-logistico/plan-cierre-39-produccion.md).
