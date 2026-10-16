# Sincronización productiva de workflows n8n — 16-oct-2026

Release `develop → main` (#251, merge `438c2d6`), CD «Despliegue en Producción»
exitoso y sincronización de los cuatro workflows en la instancia n8n de Bryan
(2.39.5). Los IDs de workflow y credenciales pertenecen a la instancia y no se
versionan; los valores de los secretos no se imprimen ni se guardan aquí.

## Release y API

| Verificación | Resultado |
|---|---|
| PR #251 (`develop → main`) | Fusionado; CD de producción en verde |
| `GET /health/ready` | 200 Healthy, PostgreSQL disponible |
| Token M2M (client_credentials) | Emitido y aceptado por la API |
| `GET /api/v1/logistics/vehicles/available` con M2M | 200 (endpoint nuevo en producción) |
| `POST /api/v1/imports?dryRun=true&allowPartial=false` con M2M sin cuerpo | 400 (autorizado; deja de ser 403) |

## Sincronización n8n

Método: API pública de n8n con respaldo privado 0600 previo (fuera del
repositorio, dos instantáneas: estado original y estado intermedio). El remapeo
de credenciales cubrió también los sub-workflows embebidos de cada herramienta
(`workflowJson`), que conservaban el identificador local.

| Workflow | Antes | Después |
|---|---|---|
| `IstpetDev Agent - vigia` | 5 herramientas, prompt v1 | 8 herramientas, prompt **v2**, Schedule apagado, `errorWorkflow` conservado |
| `IstpetDev Agent - incidente` | 5 herramientas, prompt v1 | 8 herramientas, prompt **v2**, `errorWorkflow` conservado |
| `IstpetDev Agent - error` | Publicado | Sin cambios |
| `IstpetDev Dataset - revision de viabilidad` | No existía | Creado y publicado (webhook `istpetdev/dataset/review`) |

Herramientas nuevas conectadas en ambos agentes: `find_stock` (`GET
/api/v1/logistics/stock`), `plan_sourcing` (`POST /api/v1/logistics/sourcing`)
y `vehicle_availability` (`GET /api/v1/logistics/vehicles/available`).

## Verificación

| Prueba | Resultado |
|---|---|
| Firma inválida en `watch` e `incident` | 401 en ambos |
| Token inválido en `dataset/review` | 401 (`token_mismatch`) |
| Watch real n.º 1 (ejecución n8n `292`) | Fallback `agent_error`; causa: credenciales locales dentro de los sub-workflows embebidos; se corrigió el remapeo y se reaplicó la sincronización |
| **Watch real n.º 2** (run `01a146dc-7748-7ecf-b5fb-890740270c7f`, ejecución n8n `297`) | **Completed**, `deepseek-chat`, `promptVersion v2`, 7091 ms; traza: `GET /risks` 200, `GET /logistics/stock` 200, `POST /logistics/sourcing` 200, `GET /logistics/vehicles/available` 200, `POST /route-plans` 201; 6/6 decisiones explicadas por LLM |
| Revisión de datasets con token válido | Publicación verificada; el rechazo con token inválido da 401. La ejecución con el token compartido de producción queda para el cierre de esta evidencia (el valor no se imprime ni se versiona) |
| Instantánea del grafo vigía | `AI Agent` con `systemMessage` v2 (find_stock, plan_sourcing, vehicle_availability presentes en el prompt) |

Sin incidentes inducidos y sin resets de datos productivos. El Schedule del
vigía permanece apagado; `AGENT_AUTO_ON_INCIDENT` sigue en `false`.

## Pendiente

- Nada bloqueante para el uso del agente en producción. Cualquier cambio futuro
  de prompt o herramientas requiere repetir esta sincronización con respaldo.
