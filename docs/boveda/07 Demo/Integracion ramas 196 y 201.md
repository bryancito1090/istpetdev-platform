---
tipo: registro-de-integracion
estado: integrado-localmente
actualizado: 2026-10-16
tags: [rutas, prioridad, stock, capacidad, issue-196, issue-201]
---

# Integración de las ramas #196 y #201

Este registro resume los cambios incorporados desde las ramas locales y sirve como índice de revisión para la integración hacia `develop`.

## Rama `feat/196-prioridad-stock-costo-rutas` — Issue #196

**Commit integrado:** `0d82043` (`feat(web): prioridad de entrega, stock por bodega y costo de priorizar rutas`).

En la pantalla de Rutas se añadió la selección opcional de hora límite para entregas críticas, la comparación del costo entre el plan base y el priorizado, y la consulta del stock disponible y reservado por bodega para un producto o una parada. El cliente API envía la hora límite cuando se define y consulta el inventario con el endpoint de stock, con alternativa al análisis de bodegas.

Archivos principales:

- `apps/web/src/pages/rutas/rutas.page.ts`: selector, comparación de planes y panel de stock.
- `apps/web/src/shared/api/api.service.ts`: parámetros de hora límite y consulta de stock.
- `apps/web/src/pages/rutas/rutas-prioridad-stock.spec.ts`: pruebas de UI para prioridad, costo y stock.
- `docs/boveda/07 Demo/Evidencia-Prioridad-Entrega-y-Stock-Rutas-196.md`: evidencia funcional y de demo.

## Rama `fix/201-suggest-confirm-capacidad-prioridad` — Issue #201

**Commit integrado:** `e3673be` (`fix(api): /suggest y /confirm validan capacidad y cantidades por pedido`).

La API de sugerencia de rutas incorpora prioridad y fecha requerida al ordenar entregas, calcula cargas por producto en las paradas de recogida y valida cantidades y capacidad por pedido al sugerir y confirmar. Se ajustó la evaluación logística y la asignación de vehículos para aplicar esas validaciones de forma coherente.

Archivos principales:

- `src/Api/Controllers/RoutePlansController.cs`: orden de prioridad, cargas de recogida y validaciones de sugerencia/confirmación.
- `src/Application/Logistics/OrderStopLoads.cs`, `RouteEvaluator.cs` y `VehicleMatcher.cs`: soporte de cargas y evaluación de capacidad.
- `tests/IstpetDev.Tests/Unit/Logistics/OrderBasedRoutingValidationTests.cs`: casos de validación de cantidades y capacidad por pedido.

## Integración y revisión

Las dos ramas se fusionaron localmente en `develop` mediante commits de merge separados y sin conflictos. El PR propuesto tiene como base `develop`; al fusionarlo, las referencias `Closes #196` y `Closes #201` solicitan a GitHub cerrar ambos issues automáticamente.

La cobertura automatizada añadida está en los archivos de prueba indicados arriba. Las pruebas no se ejecutaron como parte de esta integración.
