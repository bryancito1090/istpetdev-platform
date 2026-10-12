---
tipo: especificacion-datos
estado: propuesta
actualizado: 2026-10-06
tags: [datos, algoritmo, validacion]
---

# Dataset y escenarios

## Dataset sintético versionado

Propuesta: **80 puntos, 90 días**, una red de productos, bodegas/vehículos y demanda explícita. Son cantidades del equipo. No se han generado registros ni resultados aún.

Comenzar con fixture pequeño de **6 puntos y 2 SKU** para verificar invariantes; ampliar después. No forzar toda la operación nacional en el mismo vehículo/circuito diario.

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

Si se almacenan respuestas de mapas para ensayo sin internet, verificar permisos del proveedor. Registrar proveniencia de cada matriz.

## Reinicio de demo

Un comando o endpoint solo de demo restaura el dataset identificado y el recorrido esperado. Debe limpiar/aislar simulación y estado local del dispositivo sin afectar producción. Confirmar visualmente runId y seed antes del ensayo.

## Regla de honestidad

No precargar un dashboard con ahorro fijo. Los valores salen del simulador y se acompañan de etiqueta, parámetros y exportaciones. Ver [[Simulador y metricas]].
