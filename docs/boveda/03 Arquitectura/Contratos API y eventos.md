---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-09
tags: [arquitectura, contratos]
---

# Contratos API y eventos

**Versión inicial propuesta:** REST /api/v1 y hub /hubs/operations. Será la fuente compartida para web, móvil, n8n y backend. No son endpoints ya implementados.

## Convenciones

- JSON camelCase; fechas ISO 8601 UTC en servidor.
- UUID como texto; cantidades con precisión decimal y unidad explícita.
- Cliente siempre indica operationId en escrituras reintentables; clave enviada como Idempotency-Key.
- Actor real derivado de autenticación, nunca confiado desde el payload.
- organizationId validado contra sesión, aunque exista en una referencia.
- Cada endpoint privado y acción SignalR exige permiso RBAC para su acción y ámbito del recurso según [[Seguridad y evidencias]]; roles/ámbitos del payload no otorgan acceso. La única excepción de lectura pública es el resumen QR explícito.
- version/expectedVersion para cambios concurrentes.
- Paginación y filtros acotados; no devolver todo el historial nacional.
- correlationId para seguir una operación.
- Entornos de demo y sus datos están identificados.
- Generar OpenAPI desde la API y verificar clientes contra esa definición cuando exista.

## Endpoints propuestos

| Método y ruta                                | Función                                 |
| -------------------------------------------- | --------------------------------------- |
| GET /api/v1/service-points                   | Puntos y filtros                        |
| GET /api/v1/inventory                        | Saldos por ubicación/SKU/lote           |
| POST /api/v1/lots                            | Registrar lote                          |
| POST /api/v1/handling-units                  | Crear caja/unidad y líneas de lote      |
| POST /api/v1/consumptions                    | Registrar uso de insumo                 |
| GET /api/v1/risks                            | Riesgos con cálculo                     |
| POST /api/v1/replenishment-requests          | Solicitar cantidad y fecha              |
| POST /api/v1/route-plans                     | Calcular propuesta, 202 si es asíncrono |
| GET /api/v1/jobs/{id}                        | Estado y resultado de trabajo           |
| POST /api/v1/route-plans/{id}/publish        | Aprobar/publicar versión                |
| POST /api/v1/deliveries/{id}/dispatch        | Registrar salida y custodia             |
| POST /api/v1/deliveries/{id}/receipts        | Recepción por líneas, idempotente       |
| POST /api/v1/receipts/{id}/evidence-uploads  | Obtener autorización de carga           |
| POST /api/v1/evidence/{id}/confirm           | Confirmar objeto y metadatos            |
| GET /api/v1/handling-units/{id}/trace        | Historial autorizado                    |
| GET /api/v1/public/trace/{unitId}?token=...  | Resumen público deliberado              |
| POST /api/v1/incidents                       | Incidente operativo autorizado          |
| POST /api/v1/demo/incidents                  | Incidente sintético, solo demo          |
| POST/GET /api/v1/conversations/{id}/messages | Envío/historial durable                 |
| POST /api/v1/simulations                     | Iniciar escenario comparativo           |
| GET /api/v1/simulations/{id}/metrics         | Resultados y denominadores              |
| POST /api/v1/automation/risk-runs            | Solicitar cálculo programado            |
| POST /api/v1/automation/risk-explanations    | Adjuntar explicación validada           |

## Recepción offline: ejemplo de contrato

```json
{
  "operationId": "UUID_UNICO_DEL_DISPOSITIVO",
  "expectedDeliveryVersion": 4,
  "occurredAtDevice": "2026-10-17T16:05:00Z",
  "lines": [
    {
      "deliveryLineId": "UUID_LINEA",
      "lotId": "UUID_LOTE",
      "quantityReceived": "12.000",
      "unit": "L"
    }
  ],
  "evidenceLocalRefs": ["FOTO_LOCAL_1", "FIRMA_LOCAL_1"]
}
```

El ejemplo usa placeholders, no UUIDs válidos. El servidor valida unidad, cantidades restantes y lote. Las referencias locales no prueban que ya exista evidencia en S3. Respuesta: receiptId, acceptedAtServer, deliveryVersion, estado operativo y estado de evidencia.

## Idempotencia

| Situación                          | Resultado                                      |
| ---------------------------------- | ---------------------------------------------- |
| Clave nueva + operación válida     | Registrar efecto y resultado en un commit      |
| Misma clave + misma huella         | Devolver resultado previo sin nuevo movimiento |
| Misma clave + contenido distinto   | 409 con código IDEMPOTENCY_CONFLICT            |
| Clave nueva + duplicado de negocio | Rechazo/conflicto según cantidades restantes   |
| Validación fallida                 | Error claro; no registrar cambio parcial       |

Definir período de conservación que cubra reintentos offline; no expirar claves antes de la ventana de trabajo del dispositivo.

## Errores

Usar Problem Details con código estable, correlationId y errores de campos. HTTP 400 para entrada inválida, 401 sin sesión válida, 403 sin permiso, 404 para referencia inaccesible/inexistente, 409 para conflicto y 422 para restricción de negocio acordada. 429/503 son reintentables con espera; no reintentar ciegamente errores definitivos.

El conflicto devuelve información permitida para resolverlo, sin exponer datos de otra organización.

## Eventos de dominio y SignalR

Eventos iniciales: StockChanged, RiskCalculated, RoutePublished, IncidentCreated, DeliveryDispatched, ReceiptAccepted, EvidenceCompleted, MessageCreated.

Envelope: eventId, eventType, schemaVersion, aggregateId, aggregateVersion, occurredAtServer, correlationId y payload mínimo autorizado. Las notificaciones se originan después de persistir. Puede haber duplicados y reconexiones; el cliente compara versión y consulta REST.

El hub ofrece notificaciones autorizadas; el historial y las escrituras críticas siguen en API.

## Contrato n8n

La API devuelve riskId, stockSnapshotId, valores calculados y referencias de entrada. n8n devuelve riskId, runId, explicación, provider/model/promptVersion y estado. No permite que DeepSeek cambie stock, prioridad oficial calculada ni fechas de entrega.

Ver [[n8n e IA]], [[Modelo de datos]] y [[Movil offline y sincronizacion]].

## Administración configurable — propuesta del 9 de octubre

ADR-16 agrega los siguientes contratos para RF-17/18/19. Tablas, invariantes y delegación en [[Esquema completo de base de datos]] y [[RBAC y configuracion del sistema]]. Rutas propuestas; no afirmar que ya existen controllers. Todos los GET se filtran/paginan; escrituras mutables usan expectedVersion, y publicaciones/concesiones reintentables usan Idempotency-Key. Actor y pertenencia se resuelven en servidor.

| Método y ruta | Función y límite |
|---|---|
| GET /api/v1/permissions | Catálogo de acciones implementadas concedibles por quien consulta |
| GET/POST /api/v1/roles | Listar/crear roles de organización autorizada |
| PATCH /api/v1/roles/{id} | Nombre/estado permitido, sin alterar organización ni generar acciones |
| PUT /api/v1/roles/{id}/permissions | Reemplazar asociaciones delegables con diff/versión/motivo y auditoría |
| PUT /api/v1/roles/{id}/field-permissions | Administrar allowlist de campos conocidos, conservando campos de servidor protegidos |
| GET/POST /api/v1/organization-accesses | Consultar/conceder membresía a identidad existente, con autoridad de delegación |
| PUT /api/v1/organization-accesses/{id}/roles | Asignar/revocar roles, sin autoescalación ni cruce de organización |
| PUT /api/v1/organization-accesses/{id}/scopes | Concesiones de ubicación/punto existentes y delegables |
| POST /api/v1/deliveries/{id}/assignments | Asignar conductor/receptor activo a una entrega autorizada |
| GET /api/v1/configuration-definitions | Definiciones/tipos/rangos admitidos, sin modificar garantías protegidas |
| GET/POST /api/v1/configuration-versions | Listar/crear valores versionados validados contra definición |
| POST /api/v1/configuration-versions/{id}/publish | Congelar contenido/hash de versión válida |
| POST /api/v1/configuration-activations | Activar versión publicada por ámbito/vigencia sin solapamiento |
| GET/POST /api/v1/catalogs | Catálogos editables por organización |
| POST/PATCH /api/v1/catalogs/{id}/options | Gestionar opciones, preservando valores ya referenciados |
| GET/POST /api/v1/workflows | Procesos admitidos, sin crear handlers desde texto |
| POST /api/v1/workflows/{id}/versions | Borrador con estados/transiciones válidos |
| POST /api/v1/workflow-versions/{id}/publish | Publicar versión íntegra, congelando estados/transiciones |
| GET/POST /api/v1/templates | Listar/crear plantillas de tipo/módulo admitido |
| POST /api/v1/templates/{id}/versions | Nueva revisión en borrador |
| PATCH /api/v1/template-versions/{id} | Editar borrador, validaciones/fields/layout declarativos; jamás una versión publicada |
| POST /api/v1/template-versions/{id}/publish | Validar/compilar y congelar versión/hash |
| PUT /api/v1/template-versions/{id}/field-access | Administrar lectura/escritura de campos con permiso específico y auditoría |
| POST /api/v1/template-bindings | Activar versión publicada por módulo/acción/ámbito |
| GET /api/v1/template-bindings/effective | Resolver plantilla/versión efectiva para contexto autorizado |
| POST /api/v1/form-submissions | Captura con templateVersionId, contexto concreto, data validada y operationId |
| GET /api/v1/form-submissions/{id} | Lectura de campos/recurso autorizados, incluida versión histórica |
| POST /api/v1/form-submissions/{id}/corrections | Nueva captura con motivo y enlace a la anterior, sin sobrescribir aceptación |
| GET /api/v1/audit-events | Auditoría mínima autorizada, sin credenciales ni datos sensibles en claro |

Eventos adicionales propuestos: AccessChanged, ConfigurationActivated, TemplatePublished, TemplateBindingChanged y FormSubmitted. Payload mínimo contiene IDs/versiones y organización autorizada; no distribución global de concesiones o contenido sensible. AccessChanged invalida autorización/canales; no obliga al cliente a aceptar roles enviados como autoridad.

La actualización de plantillas requiere texto/rich content del tipo admitido; JSON válido por sí solo no permite propiedades adicionales, expressions ejecutables ni campos de dominio protegidos. Si la versión offline dejó de admitir nueva captura, responder 409 con código TEMPLATE_VERSION_CONFLICT e información suficiente para corrección dentro del ámbito permitido.
