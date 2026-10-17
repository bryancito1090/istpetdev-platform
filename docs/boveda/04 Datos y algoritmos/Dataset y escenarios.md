---
tipo: especificacion-datos
estado: propuesta
actualizado: 2026-10-16
tags: [datos, algoritmo, validacion]
---

# Dataset y escenarios

## Dataset sintético versionado

Propuesta: **80 puntos, 90 días**, una red de productos, bodegas/vehículos y demanda explícita. Son cantidades del equipo. No hay resultados del simulador medidos.

El fixture sintético **national-v1** (seed 11, escenario base, 2026-10-12) ya está generado con `ScenarioGenerator` y la configuración existentes en `docs/dataset/test/national-v1-seed-11/`: 80 puntos, 6 bodegas, 12 vehículos, 2 productos y 90 días. Es salida sintética reproducible, no una observación ni un resultado de simulación; procedencia y hashes en `docs/dataset/README.md`.

Comenzar con fixture pequeño de **6 puntos y 2 SKU** para verificar invariantes; ampliar después. No forzar toda la operación nacional en el mismo vehículo/circuito diario.

## Dataset propio del equipo — 16-oct-2026

La organización no entregará un dataset para el Reto 1: lo **generamos y alimentamos nosotros**. Tenemos libertad para crear o ampliar los datos de modo que cada decisión del módulo de planificación sea visible en la demo (ventanas, horas pico, tipos de vehículo, costos, mermas, operador contratado e incidentes), con dos condiciones: todo se presenta como **sintético** y cada fixture es reproducible y versionado. El fixture propuesto `demo-quito-planificacion` y sus archivos están en [[Planificacion potenciada - propuesta del evento]].

**Fixture generado (#243):** `data/fixtures/demo-quito-planificacion/`, reproducible con `scripts/data/generate-demo-quito-planificacion.py`. Contiene 3 bodegas, 40 puntos (12 establecimientos de salud públicos con ubicación real de OpenStreetMap y 28 negocios sintéticos: bancos, oficinas, locales de comida, colegios, clínicas, hoteles y centros comerciales), 12 productos con valor y bultos, 10 vehículos (Hino Dutro, Chevrolet N400 y BYD T35 eléctrico) con tipo, uso, matrícula ficticia y kilometraje, 60 días de consumo, 1806 tramos con distancias por calles (OSRM) y seis incidentes que reproducen hechos publicados por la AMT y la prensa. Incluye `appsettings.demo-quito.json` con `ServiceCost`, `ContractedCarrier`, `Recommendation`, `TrafficBands` y `VehicleRestrictions`. Las franjas de hora pico y de camiones pesados siguen el pico y placa y la restricción de la Av. Simón Bolívar y la Ruta Viva vigentes en 2026. El README del fixture cita cada fuente, separa lo real de lo sintético y describe 13 escenarios, desde un día normal hasta la avería de un camión a mitad de ruta. La prueba `DemoQuitoPlanificacionTests` lo importa completo y cubre la avería.

## Campos

| Archivo lógico | Campos mínimos |
|---|---|
| Puntos | ID, región, ubicación, ventana, criticidad y fuente |
| Productos | SKU, unidad base, empaque, peso/volumen y condiciones |
| Lotes | SKU, origen, cantidad y vencimiento |
| Inventario inicial | Ubicación, lote y cantidad utilizable |
| Consumo/demanda | Punto, SKU, fecha/hora, solicitado, observado y disponible al sistema |
| Vehículos | Capacidades, depósito, jornada y costos |
| Matrices | Origen/destino, metros/segundos, perfil, proveedor y fecha |
| Obligaciones | Punto, líneas, fecha límite y ventana común |
| Incidentes | Momento de ocurrencia/conocimiento, tramo afectado y impacto |
| Escenario | Seed, horizonte, recursos y parámetros de política |

Se genera calendario común antes de correr políticas. Separar «la demanda real simulada» de «lo que el planificador conoce» para impedir anticipación artificial.

## Escenarios obligatorios

| ID | Caso | Evidencia esperada |
|---|---|---|
| S-01 | Operación estable | Comparación base y balance de existencias |
| S-02 | Hospital con cobertura de cinco días | Riesgo explicado con 60 L, 12 L/día y lead time/margen |
| S-03 | Consumo anómalo etiquetado | Alerta e investigación; no merma automática |
| S-04 | Cierre de vía después del despacho | Replanificación que respeta entregas ya ejecutadas |
| S-05 | Recepción sin señal | Persistencia, reinicio y sincronización |
| S-06 | Reintento de la misma recepción | Una aceptación y un efecto de inventario |
| S-07 | Entrega parcial o rechazada | Cantidades pendientes/devolución y custodia |
| S-08 | Stock cero o agotamiento de bodega | Falta declarada y prioridad; no saldo negativo |
| S-09 | Ventana/capacidad inviable | Motivo de parada no atendida |
| S-10 | IA/mapas temporalmente caídos | Regla determinista y fallback etiquetado |
| S-11 | Dos dispositivos y ruta revisada | Conflicto explícito |
| S-12 | Lote vencido/bloqueado | Exclusión de disponibilidad |

El caso del hospital debe estar dentro de una operación con stock/vehículo suficiente si queremos mostrar prevención. Otro caso deliberadamente inviable prueba límites.

## Geografía

Usar puntos plausibles y ubicaciones verificadas sin representar clientes reales. Etiquetar coordenadas sintéticas o públicas. Probar cobertura vial en las regiones elegidas; no inventar distancias nacionales como si provinieran de la API.

El agregado público de ACESS 2026-09 en `docs/dataset/reales/acess-2026-09/` cuenta establecimientos únicos (2590) y permisos emitidos (2601) por provincia, cantón, tipo y categoría; no ubica clientes ni mide demanda.

Si se almacenan respuestas de mapas para ensayo sin internet, verificar permisos del proveedor. Registrar proveniencia de cada matriz.

## Reinicio de demo

Un comando o endpoint solo de demo restaura el dataset identificado y el recorrido esperado. Debe limpiar/aislar simulación y estado local del dispositivo sin afectar producción. Confirmar visualmente runId y seed antes del ensayo.

## Regla de honestidad

No precargar un dashboard con ahorro fijo. Los valores salen del simulador y se acompañan de etiqueta, parámetros y exportaciones. Ver [[Simulador y metricas]] y [[Cifras del simulador 90 dias]].
