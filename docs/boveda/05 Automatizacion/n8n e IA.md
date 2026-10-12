---
tipo: automatizacion
estado: propuesta
actualizado: 2026-10-06
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
