---
tipo: especificacion-datos
estado: propuesta
actualizado: 2026-10-06
tags: [datos, algoritmo, validacion]
---

# Rutas y sobrecostos

## Problema que resolvemos

Seleccionar puntos que necesitan abastecimiento y ordenar entregas con restricciones de carretera, tiempo, capacidad y disponibilidad. El mapa muestra una decisión logística, no solo una línea entre coordenadas.

## Datos necesarios

- Depósito de salida y regreso.
- Puntos y ventanas de atención.
- Cantidades, peso y volumen de cada pedido.
- Vehículo y duración de jornada.
- Matriz vial dirigida de distancia/tiempo.
- Duración de servicio por parada.
- Criticidad, fecha límite, vías restringidas e incidentes.
- Costo por km/hora y recargos verificables.

No asumir que distancia A→B equivale a B→A. Viaje en línea recta solo se permite como aproximación explícita en un escenario separado.

Mapbox Matrix entrega matrices, no geometría completa del mapa ni solución con capacidad. Google Routes ofrece Compute Route Matrix; elegir y probar proveedor antes de fijarlo. [Mapbox Matrix](https://docs.mapbox.com/api/navigation/matrix/), [Google Routes](https://developers.google.com/maps/documentation/routes/compute_route_matrix).

## Algoritmo inicial

1. Priorizar necesidades por riesgo con datos disponibles al planificar.
2. Filtrar stock disponible y compatibilidad de vehículo.
3. Construir ruta por inserción factible, evaluando capacidad y ventanas.
4. Incorporar espera y duración de servicio en cada llegada/salida.
5. Aplicar mejora local 2-opt si mantiene restricciones.
6. Regresar al depósito y validar jornada.
7. Devolver paradas atendidas, pendientes y motivos de inviabilidad.

Con varios vehículos, asignar por zona/capacidad antes de mejorar rutas; reemplazar por un solver especializado solo cuando el caso y sus pruebas lo justifiquen.

**Límite conocido:** la heurística no prueba óptimo global; llamarla «ruta optimizada por la política» y mostrar cómo se compara. Un solver puede mejorarla en P2.

## Planificación nacional

Ochenta puntos distribuidos por Ecuador no implican una única ruta diaria visitando todo el país. Definir agrupaciones regionales, tiempos de desplazamiento y vehículos. P0 demuestra un circuito pequeño; P1 simula la red nacional con recursos declarados.

## Baseline justo

La ruta tradicional se define antes de ejecutar resultados: frecuencia fija, orden histórico/geográfico acordado y regla de urgencia. Debe ser razonable y, si hay información empresarial, reflejar su proceso.

Comparación de ordenamiento: mismas paradas, cantidades, vehículo y matriz. Comparación de políticas de abastecimiento: ambas atienden la misma demanda exógena durante todo el período, aunque sus órdenes/rutas difieran.

No confundir estos dos experimentos. Más km puede justificarse si el servicio mejora; revisar junto a quiebres y puntualidad.

## Costos

Por ruta: distancia × costo/km + conducción/servicio × costo/hora + recargos/urgencias definidos. Si costo/km ya incorpora personal, no sumar ese costo dos veces.

Mostrar total, km, tiempo, pedidos atendidos y demanda no cubierta. Los costos de plataforma se agregan en beneficio neto, separado del ahorro de transporte.

## Incidente y replanificación

Caso demo: cierre de un tramo o demora añadida a aristas de la matriz. Colocar un icono no afecta el algoritmo por sí solo; el incidente debe alterar restricciones o tiempos.

- Mantener recorrido y entregas ya ejecutadas.
- Replanificar desde posición/estado conocido del vehículo.
- Revalidar ventanas, capacidad remanente y tiempos.
- Versionar la nueva ruta y mostrar cambios de costo/ETA.
- Solicitar aprobación si deja puntos pendientes o cambia compromisos.
- Indicar si la incidencia es sintética y si el proveedor no soporta el bloqueo directamente.

## Datos viales y fallback

Guardar fecha, proveedor, perfil y hash de matriz. Revisar condiciones de almacenamiento/reuso antes de preparar matrices para demo. Si no hay red, usar una matriz permitida y etiquetada; no presentarla como tráfico actual.

El fallback mantiene el flujo y muestra antigüedad. La planificación asíncrona deja progreso y error recuperable.

Ver [[Simulador y metricas]], [[Dataset y escenarios]] y [[Plan de validacion]].
