---
tipo: skill
estado: vigente
actualizado: 2026-10-07
tags: [istpetdev, ia, skills]
---

# IstpetDev — contrato API y eventos

**Cuándo usarla:** Contrato compartido entre backend, web, móvil y n8n de IstpetDev. Usar al crear, cambiar o consumir un endpoint REST, un evento SignalR, un DTO, un código de error, una clave de idempotencia o la especificación OpenAPI, y antes de que dos integrantes implementen lados distintos de la misma interfaz.

> Documentación de la skill `istpetdev-contrato`. El archivo `SKILL.md` no se versiona en la bóveda: quien quiera usarla con su asistente de IA copia este contenido a la carpeta de skills de su herramienta (frontmatter con `name: istpetdev-contrato` y la descripción de arriba). Catálogo y origen en [[Catalogo y plan de skills]].

Rutas relativas a la raíz del repositorio. Leer antes [[istpetdev-contexto]].

## Fuente única

- Contrato vigente: `03 Arquitectura/Contratos API y eventos.md` (REST `/api/v1`, hub `/hubs/operations`, convenciones, errores, idempotencia, eventos).
- Permisos por acción y recurso: `03 Arquitectura/Seguridad y evidencias.md` (matriz RBAC).
- Nombres de entidades: tabla «Nombres en código» de `00 Inicio/Hechos canonicos.md`.
- Archivo OpenAPI: **PENDIENTE** (el repositorio de código está vacío; su ubicación dentro de él no está decidida). Cuando exista, se genera desde la API y los clientes se verifican contra él. Los tipos compartidos van en `libs/shared-core/src/contracts/`.

## Reglas del contrato

- JSON en camelCase; fechas ISO 8601 en UTC del servidor; UUID como texto; cantidades decimales con unidad explícita.
- Toda escritura reintentable lleva `operationId` y la cabecera `Idempotency-Key`. Misma clave y mismo contenido → mismo resultado; misma clave y otro contenido → 409 `IDEMPOTENCY_CONFLICT`.
- El actor y la organización salen de la sesión, nunca del payload.
- `version` / `expectedVersion` en cambios concurrentes.
- Errores con Problem Details: 400 entrada inválida, 401 sin sesión, 403 sin permiso, 404 inexistente o inaccesible, 409 conflicto, 422 regla de negocio; 429 y 503 son reintentables.
- Eventos: envelope con `eventId`, `eventType`, `schemaVersion`, `aggregateId`, `aggregateVersion`, `occurredAtServer`, `correlationId` y payload mínimo. Se emiten después de persistir; el cliente compara versión y consulta REST.

## Cambiar el contrato

1. Proponer el cambio en `03 Arquitectura/Contratos API y eventos.md` (y en el OpenAPI cuando exista) **antes** de implementarlo.
2. Citar el requisito que lo motiva (RF/RNF de `02 Producto/Requisitos y aceptacion.md`).
3. Abrir PR con el cambio de contrato solo; lo aprueba al menos una persona que consuma ese contrato (web, móvil o n8n).
4. Un cambio incompatible sube la versión (`/api/v2` o `schemaVersion`) y se registra en `03 Arquitectura/Decisiones de arquitectura.md`.

## Documentar cada endpoint

- `summary` en una frase y `description` con el RF/RNF que implementa.
- Parámetros con descripción y ejemplo.
- Respuestas relevantes: 200/201, 400, 401, 403, 404, 409, 422 y 500.
- Esquemas de request y response; ejemplos con marcadores, nunca datos o tokens reales.

## Prohibido inventar

- Endpoints, campos, eventos o códigos de error que no estén en el contrato. Si hacen falta, primero se proponen (paso 1).
- Que un payload conceda roles, actor u organización.
- Que n8n o el LLM cambien stock, prioridad calculada o fechas de entrega.

## Verificar

```bash
python .github/scripts/check_vault.py
```

Cuando exista código: generar el OpenAPI desde la API y comprobar los clientes contra él (comando PENDIENTE en `00 Inicio/Hechos canonicos.md`).

## Origen

Adaptada de la skill personal `tech-writer` (OpenAPI documentado por endpoint, no duplicar definiciones) y de las convenciones ya registradas en la bóveda.
