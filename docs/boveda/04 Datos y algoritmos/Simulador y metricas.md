---
tipo: especificacion-datos
estado: propuesta
actualizado: 2026-10-12
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

## Complemento propuesto — 12 de octubre de 2026 (spec 005)

El [spec 005](../../specs/005-panel-simulador/spec.md) concreta D7 **antes de la primera corrida**. La decisión de calendario fijo fue confirmada en el brainstorming: cada siete días, cantidad histórica fija por punto y SKU. Las reglas se versionan; si el equipo las modifica, repite todas las políticas y seeds y conserva el manifiesto anterior. Deciders formales pendientes, por lo que la nota sigue como propuesta.

| Política | Regla propuesta congelada | Límite de información |
|---|---|---|
| `reactive-v1` | Aviso si saldo disponible cae bajo `leadTimeDays × media histórica conocida`; pedir hasta catorce días de cobertura, sujeto a stock, flota y ventanas | Saldo y consumo ya revelado; ningún evento futuro |
| `fixed7-v1` | Cada punto tiene un día fijo de visita en la semana laborable (dentro de su región, puntos ordenados por código repartidos en 5 grupos, índice mod 5) y se visita cada siete días en ese día; cantidad por punto/SKU igual a siete veces la media diaria del histórico previo al horizonte, fijada una vez | Calendario y media previa; no adapta cantidades según resultados |
| `istpetdev-v1` | Riesgo, cantidades y rutas del motor 002 cada día | Solo observaciones conocidas a la hora simulada |

Modelo propuesto de demanda: media previa por punto/SKU, factor de día laborable 1,0 y fin de semana 0,8, ruido determinista por seed de ±15 % y picos etiquetados en 5 % de claves punto–SKU–día. El calendario exógeno se crea una vez por seed y se revela por hora a las tres políticas; no equivale a consumo observado cuando hay quiebre. El escenario adverso aplica demanda ×1,20, un vehículo menos y lead time +1 día a **las tres** políticas. Sus resultados se muestran aparte de las tres seeds base.

La red nacional se particiona en grupos regionales con una bodega y flota local por grupo: 002 planifica cada grupo/día sin pernocta ni transferencias entre regiones. La cantidad de grupos, flota y vehículo retirado en el escenario adverso son parámetros versionados del dataset, no decisiones elegidas después de ver resultados.

Costo de transporte común: km con regreso × costo/km del vehículo + conducción/servicio × costo/hora; recargos solo si se definen para todas las políticas antes de correr. El costo de plataforma se muestra separado. Para un jurado no técnico, el panel prioriza cuatro tarjetas: quiebres evitados **y nuevos**, demanda no atendida por SKU/unidad, costo total de transporte y puntualidad de obligaciones comunes; km, servicio completo y trazabilidad se abren en detalle. Cada tasa presenta numerador, denominador, unidad, rango y seed. La exportación incluye manifiesto, calendario, métricas y eventos con versiones/hashes.

El objetivo de rendimiento de la corrida completa (1080 jornadas de política con tres seeds base y una adversa, cada una particionada por región) es ≤60 minutos en una **máquina dedicada**, sujeto a medición (no en la instancia que sirve la demo; ver el complemento siguiente). Con seis regiones de ≤15 puntos habría hasta 6480 oportunidades de planificación; a 1 s por propuesta de 15 puntos/2 vehículos de 002 serían 108 minutos solo para planificar. Por eso la cota por llamada no acredita H4. El trabajo es asíncrono en la API y sus estados se guardan; el simulador no tiene sustituto de cifras defendibles si falla. Esta propuesta no afirma una medición empresarial ni una corrida ejecutada.

## Corrección previa a la primera corrida — revisión del 12 de octubre de 2026

La revisión del spec 005 corrigió dos aspectos **antes de ejecutar ninguna simulación**, por lo que no constituyen un ajuste según resultados:

- **Ruteo común:** las tres políticas secuencian sus paradas del día con el mismo planificador de 002 (inserción + 2-opt, mismas restricciones y costo). La comparación mide la política de reposición (qué puntos, cuánto y cuándo), no el algoritmo de ruteo; un baseline ordenado por código habría sumado km sin relación con la reposición.
- **Calendario escalonado:** `fixed7-v1` reparte los puntos en días laborables fijos en lugar de visitarlos todos el mismo día, lo que habría saturado la flota ese día y debilitado artificialmente el baseline.

Además, la corrida oficial se ejecuta en una máquina dedicada y su exportación se importa en la demo con verificación de los SHA-256 del manifiesto: la plantilla de Terraform define la instancia de la demo como `t3.micro`, con CPU por créditos, lo que hace incierto el plazo y degradaría la API del pitch.
