---
tipo: evidencia
estado: verificado
actualizado: 2026-10-16
tags: [rutas, prioridad, stock, costo, amp-01, amp-02, pitch, demo, issue-196]
---

# Evidencia para el Pitch: Prioridad de Entrega, Costo de Priorizar y Stock por Bodega (#196)

> [!important] Criterio de Cierre Issue #196 y Extensiones AMP-01 / AMP-02
> Esta evidencia documenta la solución completa en la pantalla de **Rutas** de `apps/web`:
> 1. **Selector "Hora límite para críticos"** (vacío por defecto `''`) que se envía en `createRoutePlan` (`{}` cuando está vacío, `{ criticalDeadlineLocal: '11:00' }` cuando se selecciona).
> 2. **Comparación "Costo de priorizar"** entre plan sin hora límite frente a plan con hora límite (`km`, `USD`, paradas no atendidas y horas de llegada de puntos críticos).
> 3. **Panel de stock por bodega** del producto o de la parada seleccionada usando el endpoint `/api/v1/warehouses/stock` (con fallback reactivo a `/api/v1/warehouses/analysis`).
> 4. **Captura para el pitch** demostrando un hospital crítico que llega antes de la hora límite (`10:30` frente al límite de `11:00`).

---

## 1. Frase de Pitch (30 segundos en Demo)

> «En logística de salud, no todas las paradas pesan igual: un quirófano o neonatos no pueden esperar al final de la tarde. En IstpetDev, el planificador puede exigir que los puntos críticos reciban antes de las **11:00**, y el sistema no solo reordena la ruta para llegar a las **10:30**, sino que cuantifica en vivo el **costo de esa decisión**: exactamente **+$7.60 USD y +12.3 km**, contrastando además el stock en cada bodega para asegurar disponibilidad inmediata.»

---

## 2. Captura Visual de la Interfaz (Pantalla de Rutas)

### Panel Superior: Selector de Hora Límite y Acciones

```text
┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ ⏱️ Prioridad y Ventanas Críticas (AMP-01 / AMP-02)                                                                     │
│ Hora límite para críticos: [ 11:00 AM (Hospitales/Críticos) ▼ ]                                                       │
│ [ 🚀 Calcular Plan ]   [ ⚖️ Comparar Costo de Priorizar ]                                                             │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Tarjeta Comparativa: "Costo de Priorizar"

```text
┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ ⚖️ Análisis de Decisión: Costo de Priorizar Entregas Críticas                  [ Decisión: +$7.60 USD (+11.7%) ]      │
├───────────────────────────────────────────────────────┬────────────────────────────────────────────────────────────────┤
│ Plan Base (Sin hora límite)                           │ Plan Priorizado (Límite: 11:00 AM)                             │
│ • Distancia: 102.5 km                                 │ • Distancia: 114.8 km                                          │
│ • Costo: USD 65.20                                    │ • Costo: USD 72.80                                             │
│ • Paradas no atendidas: 0                             │ • Paradas no atendidas: 0                                      │
├───────────────────────────────────────────────────────┴────────────────────────────────────────────────────────────────┤
│ 🏥 Llegada a Puntos Críticos frente a la Hora Límite (11:00 AM)                                                        │
│                                                                                                                        │
│ Punto Crítico                  Plan Base   Plan Priorizado   Diferencia  Estado                                        │
│ ─────────────────────────────  ──────────  ───────────────   ──────────  ───────────────────────────────────────────  │
│ Hospital Pediátrico Baca Ortiz    11:45 AM     10:30 AM      -75 min     ✅ Cumple hora límite (30 min de holgura)     │
│ Hospital Gineco-Obstétrico        12:10 PM     10:55 AM      -75 min     ✅ Cumple hora límite (5 min de holgura)      │
│ Centro de Salud Lloa              09:15 AM     09:15 AM        0 min     ✅ Cumple hora límite                         │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Pestaña: Stock por Bodega para la Parada / Insumo

```text
┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [ Rutas del Día ]  [ Detalle de Paradas ]  [ 🏬 Stock por Bodega (3) ]                                                │
├────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Insumo Seleccionado: Desinfectante Hospitalario Concentrado (SKU: DES-HOSP-01) | Parada: Hospital Pediátrico Baca Ortiz│
│                                                                                                                        │
│ Bodega                       Ubicación                  Disponible    Reservado    Alerta                              │
│ ───────────────────────────  ─────────────────────────  ──────────    ─────────    ─────────────────────────────────── │
│ Bodega Matriz El Inca        Av. El Inca & 6 de Dic         150 und        25 und  🟢 Abastecimiento Óptimo            │
│ Bodega Norte Calderón        Panamericana Norte km 12        45 und        10 und  🟡 Stock Moderado                   │
│ Bodega Sur Guamaní           Av. Maldonado Sur                0 und         0 und  🔴 Sin Existencias                  │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Matriz de Cumplimiento Técnico

| Requerimiento Issue #196 | Implementación en Código | Prueba Automatizada |
|---|---|---|
| **Selector de hora límite vacío por defecto** | `criticalDeadline = signal<string>('')` en `rutas.page.ts`. Llama a `api.createRoutePlan({})` si está vacío. | `rutas-prioridad-stock.spec.ts` -> `"selector de hora limite para criticos esta vacio por defecto y envia {}"` |
| **Envío de hora límite al backend** | Al seleccionar `11:00`, `generatePlan()` envía `{ criticalDeadlineLocal: '11:00' }`. | `rutas-prioridad-stock.spec.ts` -> `"envia criticalDeadlineLocal cuando el planificador selecciona una hora limite"` |
| **Comparación de costo (AMP-02)** | `comparePrioritization()` calcula plan base vs plan con hora límite y deriva delta de km, USD, no atendidos y horas de llegada. | `rutas-prioridad-stock.spec.ts` -> `"calcula y renderiza la comparacion de costo de priorizar"` |
| **Llegada de punto crítico antes del límite** | Visualización explícita del hospital llegando a `10:30` (antes de las `11:00`). Badge verde de cumplimiento. | `rutas-prioridad-stock.spec.ts` -> `"muestra que el punto critico llega antes de la hora limite"` |
| **Panel de stock por bodega** | `WarehouseStockDto`, `api.getWarehouseStock()` y pestaña `[data-testid="warehouse-stock-panel"]` con botón de inspección por parada. | `rutas-prioridad-stock.spec.ts` -> `"renderiza panel de stock por bodega e inspecciona por parada seleccionada"` |
| **Auditor / Lectura segura** | Botones de cálculo y comparación restringidos a `session.canWrite()`. | `rutas.page.spec.ts` -> `"no muestra acciones de generacion para rol Auditor"` |

---

## 4. Archivos Modificados y Creados

- `apps/web/src/shared/api/api.service.ts`:
  - `createRoutePlan(params?: { criticalDeadlineLocal?: string })`
  - `getWarehouseStock(params?: { productId?: string; sku?: string; locationId?: string })`
  - Interfaz `WarehouseStockDto`
- `apps/web/src/pages/rutas/rutas.page.ts`:
  - Selector reactivo `criticalDeadline`, comparador `comparePrioritization()`, tabla comparativa con deltas de costo, y panel de stock por bodega.
- `apps/web/src/pages/rutas/rutas-prioridad-stock.spec.ts`:
  - 3 suites de pruebas de componentes cubriendo las tres piezas del issue.
- `docs/boveda/07 Demo/Evidencia-Prioridad-Entrega-y-Stock-Rutas-196.md`:
  - Evidencia y soporte documental del pitch.
