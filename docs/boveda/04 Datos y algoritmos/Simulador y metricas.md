---
tipo: especificacion-datos
estado: propuesta
actualizado: 2026-10-07
tags: [datos, algoritmo, validacion]
---

# Simulador y métricas

## Objetivo

Comparar una política tradicional con la plataforma durante **90 días simulados**, conservando recursos y demanda comparables. Los resultados son simulados hasta que exista validación empresarial.

Cada ejecución identifica datasetVersion/hash, seed, policyVersion, parameters, startedAt, matriz vial, código y supuestos. La pantalla y las exportaciones deben conservar esas referencias.

## Comparación reproducible

1. Generar antes de cada política un calendario exógeno de demanda, incidentes y tiempos.
2. Copiar stock inicial, productos, flota, disponibilidad de bodega y recursos.
3. Ejecutar baseline documentado.
4. Ejecutar la política propuesta sobre el mismo calendario.
5. Cada política conoce únicamente la información disponible en el momento simulado.
6. Validar balance de stock, capacidad y obligaciones de servicio.
7. Exportar resultados, denominadores y eventos.

Precomputar el calendario común impide que las decisiones de una política consuman números aleatorios diferentes y alteren la demanda del otro escenario.

## Flujo diario

Confirmar entradas disponibles → observar registros conocidos → calcular riesgo → planificar/reservar → ejecutar viajes/entregas → satisfacer demanda según horarios → registrar faltantes/consumo → conciliar. La simulación debe definir horas/eventos; entregar al final del día no puede eliminar un faltante ocurrido antes.

La demanda solicitada existe aunque no haya stock. Medir solo consumo observado ocultaría los quiebres y sesgaría el pronóstico.

## Métricas y fórmulas del equipo

| Indicador | Definición |
|---|---|
| Km totales | Suma de tramos realizados, incluido regreso al depósito |
| Ahorro de km | Km baseline − km plataforma |
| Ahorro de transporte | Costo baseline − costo plataforma, con igual modelo de costos |
| Reducción % | Ahorro / valor baseline ×100, si baseline >0 |
| Km/costo por entrega | Total / entregas completadas; mostrar numerador y denominador |
| Quiebres | Número de claves punto–SKU–día con demanda no atendida por falta de stock |
| Quiebres evitados | Claves con quiebre en baseline y sin quiebre en plataforma |
| Quiebres nuevos | Claves sin quiebre en baseline y con quiebre en plataforma |
| Demanda no atendida | Cantidad solicitada menos satisfecha, por SKU/unidad |
| Puntualidad | Entregas de obligaciones comunes completadas en ventana / obligaciones vencidas ×100 |
| Servicio completo | Órdenes comunes atendidas con todas sus líneas / órdenes comunes vencidas ×100 |
| Trazabilidad completa | Líneas elegibles entregadas con custodia y evidencia requerida / líneas elegibles entregadas ×100 |
| Anomalías detectadas | Alertas válidas contra casos etiquetados sintéticos |
| Mermas confirmadas | Casos conciliados con pérdida documentada; distinto de alertas |
| Anticipación | Tiempo entre primera alerta válida y faltante que ocurriría sin esa acción |
| Calidad del pronóstico | MAE y cobertura de puntos/SKU evaluados |

**Obligaciones comunes:** órdenes/compromisos del dataset con misma fecha límite para ambas políticas. Las reposiciones creadas internamente por cada política se informan aparte; no cambiar su SLA para mejorar artificialmente puntualidad.

Para trazabilidad, usar líneas de entrega como unidad inicial. No contar «un lote completo» si solo una caja de ese lote tiene QR y evidencia. Añadir versión de la definición.

## Evitar comparaciones engañosas

- Quiebres evitados no equivale a diferencia neta: mostrar también nuevos quiebres.
- Si el denominador es cero, mostrar N/A con razón.
- Promedios por entrega cambian si una política agrupa pedidos; mostrar ahorro total y nivel de servicio.
- Comparar también costo por cantidad o pedido común atendido; nunca mezclar unidades incompatibles.
- Si no hay stock o vehículo, declarar demanda pendiente.
- «Cero quiebres» es un resultado posible, no garantizado.
- Un evento de scan no demuestra una entrega física por sí solo.
- La captura incompleta puede estar recibida operativamente, pero no tener trazabilidad completa.

## Experimentos

| Experimento | Qué compara |
|---|---|
| EXP-01 | Orden tradicional vs optimizado para las mismas paradas |
| EXP-02 | Reposición reactiva/fija vs cobertura sobre demanda común |
| EXP-03 | Resultado de 90 días con ambas políticas |
| EXP-04 | Sensibilidad: demanda +20%, menos vehículo y lead time mayor |
| EXP-05 | Incidente vial común y reacción de cada política |
| EXP-06 | Semillas múltiples para evitar elegir solo el mejor resultado |

Los porcentajes de sensibilidad son parámetros de prueba del equipo, no observaciones reales. Usar varias seeds y mostrar distribución/rango, no solo la más favorable.

## Estado inicial

**No hay resultados medidos todavía.** El pitch conserva campos pendientes hasta completar una ejecución y validación. Exportaciones previstas: metrics.json, resumen.csv, eventos.csv y manifiesto con parámetros/hash.

Ver [[Dataset y escenarios]], [[Plan de validacion]] y [[Guion del pitch]].
