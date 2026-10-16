# Evidencia #209 — revisión de viabilidad de datasets

Fecha: 16-oct-2026. Rama `feature/209-revision-viabilidad-datasets` desde
`origin/develop` (`0e8ffef`, merge #226). Entorno local Linux; sin despliegue ni
cambios en n8n productivo.

## Alcance entregado

- **API (ADR-22):** el reporte del importador gana `status`
  (`Applied`/`DryRun`/`Rejected`) y `feasibility`, calculada en el plan en
  memoria del dry-run (`FeasibilityAnalyzer`): puntos sin coordenadas, bodegas
  sin flota activa con capacidad, productos sin peso/volumen y déficit de stock
  por SKU; veredicto determinista `Viable`/`ViableWithLimitations`/`Inviable`.
  Sin mutar tablas operativas.
- **Autorización M2M acotada:** `Roles.Automatizacion` puede `POST /imports`
  solo con `dryRun=true&allowPartial=false` (403 en cualquier otro caso);
  aplicar y descartar filas sigue siendo de `Roles.Planning`.
- **n8n:** workflow nuevo `revision-dataset` (webhook con token compartido →
  dry-run M2M → análisis determinista → resumen LLM con fallback → respuesta).
  Nunca envía `allowPartial=true`. No se enlaza al workflow de error porque no
  existe AgentRun que resolver; `local-up.sh` solo resuelve el marcador cuando
  está presente.
- **Contrato:** `ImportReport` con `feasibility` documentado en Contratos API,
  n8n e IA y FR-10 del spec 001.

## Pruebas automáticas (locales, reales)

| Verificación | Resultado |
|---|---|
| Suite backend completa (`dotnet test -c Release`) | **566/566**, 0 fallos (4 m); incluye 6 pruebas nuevas de `FeasibilityReportTests` |
| `AuthorizationSafetyTests` | M2M: dry-run sin cuerpo 400; `dryRun=false` y `allowPartial=true` 403; reset 403 |
| Scripts n8n (`tests/scripts/test_*.py`, 8 archivos) | Todos OK; `test_dataset_review_workflow.py` 7/7 ejecuta los Code nodes reales del export con Node |
| `python3 -I scripts/agent/build-workflows.py --check` | Limpio (4 workflows reproducibles) |
| `npm run check` | OK (lint, no-mock-login, API, shared, web, mobile, campo) |
| `npm run audit:check` | OK (7 avisos autorizados vigentes) |
| `check_vault.py` | 0 errores, 4 avisos previos |
| `git diff --check` | Limpio |

Criterios cubiertos por `FeasibilityReportTests` (PostGIS descartable):

1. Dry-run limpio devuelve `status="DryRun"`, `applied=false`, punto sin GPS
   contado con código, nombre y fila, y no persiste puntos.
2. Dataset completo (bodega, flota, producto con peso/volumen, stock y consumo
   cubierto) responde `Viable` sin hallazgos.
3. Consumos sin stock: `Inviable` con el déficit por SKU (demanda 100,
   disponible 0).
4. Bodega sin flota y producto sin peso ni volumen quedan listados con motivo.
5. Fila inválida: HTTP 422, `status="Rejected"`, incidencia con
   `entity/file/row/field/code/message`, y base sin cambios ni lote parcial.
6. M2M: dry-run 200 con `DryRun`; aplicar y `allowPartial` 403; sin persistencia.

## Ejecución real en n8n 2.39.5

Contenedor `docker.n8n.io/n8nio/n8n:2.39.5` (red host, loopback) con
`revision-dataset.json` importado y publicado desde el repositorio. Puente HTTP
local que simula `POST /oauth/token`, `POST /api/v1/imports` (respondió el
reporte de prueba con `feasibility`) y un DeepSeek sin credencial válida.

- Token inválido en el webhook → **401** `{status:"unauthorized", reason:"token_mismatch"}`.
- Subida multipart válida → la API recibió **1** llamada `multipart/form-data`
  con `dryRun=true&allowPartial=false`; OAuth emitido 1 vez; DeepSeek 1 intento
  fallido.
- Resultado: `veredicto="Inviable para rutear el dataset completo"`,
  `agentError="Authorization failed - please check your credentials"` y
  **resumen determinista** íntegro (no inventó cifras ni coordenadas):
  «Inviable para rutear el dataset completo. Revision en seco DryRun sin
  errores. 1 puntos sin coordenadas no seran ruteables. Hay deficit de stock en
  1 SKUs. 1 bodegas no tienen flota activa con capacidad. 4 correcciones
  requieren revision humana.»
- Secciones presentes en la respuesta: `dictamen.importacion` (DryRun, 0
  errores), `contadores`, `balance` (service_points y consumptions),
  `puntosSinCoordenadas` (P-1, fila 2), `stockVsDemanda` (SKU-1 100 vs 0),
  `bodegasSinFlota`, `productosSinCapacidad` y acciones seguras/humanas.

## Pendientes y no aplica

- Llamada real a DeepSeek no ejecutada en este recorrido (puente sin credencial
  válida): el criterio validado es el fallback determinista. La ruta LLM ya se
  ejerce con el mismo patrón en `vigia`/`incidente`.
- No se levantó la instancia n8n productiva ni se promovió a `main`; sin
  despliegue.
- El webhook acepta un archivo por revisión (campo `file`); multiarchivo queda
  fuera de alcance y se registra para seguimiento si el operador lo necesita.
