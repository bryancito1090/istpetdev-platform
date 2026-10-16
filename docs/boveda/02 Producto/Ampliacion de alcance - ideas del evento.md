---
tipo: estrategia
estado: propuesta
actualizado: 2026-10-16
tags: [producto, reto-1, evento, brainstorm, rutas, alcance]
---

# Ampliacion de alcance - ideas del evento

> [!warning] Lluvia de ideas, no compromiso
> Opciones que salen de las notas del 16-oct. Nada de esto está aprobado ni implementado por esta nota. Ninguna idea se presenta en el pitch como capacidad hasta que exista en código con prueba. Decide el equipo; el orden sugerido lo propone el asistente (Claude Code, Opus 5.5) a pedido de Joseph Baño.

**Fuentes:** [[Hallazgos de la charla para el Reto 1]], [[2026-10-16-1109-bb-charla-limpieza-logistica]], [[2026-10-16-1102-jb-bodega-inventario-y-rutas]], [[2026-10-16-1115-ap-flexibilidad-roles-rutas-productos]] y [[2026-10-16-1126-bb-agenda-general-y-mentorias]]. La revisión de código se hizo sobre `develop` en el commit `c147d2a`.

## Cómo filtramos

Una idea entra antes del congelamiento (17-oct, 15:45) solo si cumple las cuatro condiciones:

1. Responde a algo que dijo el sector o la organización.
2. Se puede mostrar en menos de 30 s dentro del pitch de 5 min.
3. Reutiliza lo que ya existe: motor, pedidos, ventanas, replanificación.
4. Con la opción apagada, el sistema se comporta igual que hoy y no rompe lo ya ensayado.

## Mapa de ideas

| ID | Idea | De dónde sale | Qué hay hoy en el código | Esfuerzo | Valor en demo | Propuesta |
|---|---|---|---|---|---|---|
| AMP-01 | **Prioridad de entrega con hora límite:** los puntos críticos reciben antes de una hora fijada (p. ej. 11:00) | Pregunta sin respuesta sobre a quién atender primero; quirófano y neonatos no equivalen a una habitación | El motor ya ordena por urgencia quién entra al camión, pero no a qué hora llega (ver análisis abajo) | Bajo: ~20 líneas en el motor, 2 pruebas y un selector en Rutas | Alto: se ve en el mapa y en las horas de llegada | **Hacer hoy si el equipo aprueba** |
| AMP-02 | **Costo de priorizar:** comparar el plan por costo con el plan con hora límite (km, USD, no atendidos) | Viabilidad económica de cada salida (15 entregas, USD 1.000 en el relato) | El mismo motor calcula ambos planes | Bajo-medio: comparar dos resultados en la API o en la UI | Alto: la prioridad se vuelve una decisión con número | Después de AMP-01 |
| AMP-03 | **Replanificar ante cierre vial priorizando críticos** | Pregunta sin respuesta sobre retraso o cierre vial | **Ya existe:** modos `Strict`, `LateService` y `PrioritizeCritical` en la replanificación | Sin código; guion y ensayo | Alto: contesta la pregunta que el sector dejó abierta | **Incluir en el guion** si el ensayo lo confirma |
| AMP-04 | Pedido imprevisto que entra al plan del día | Clientes que no programan compras; ruta de la costa desviada al centro | Pedidos manuales, pedidos de emergencia y asistente `/suggest` ya existen | Sin código para narrarlo | Medio | Narrar con lo que existe |
| AMP-05 | Asistente de pedidos que ordene por prioridad y fecha requerida y use las ventanas reales del punto | Mismo origen que AMP-01 | `DeliveryOrder.Priority` y `RequiredDateUtc` se guardan, pero `/suggest` no los usa; ventana fija 08:00–17:00 | Medio: controlador y pruebas | Medio | Solo si sobra tiempo tras AMP-01 |
| AMP-06 | Aviso de ruta bajo umbral (menos de N entregas o carga menor a X USD) | «No puedo salir con menos de 15 entregas» | El plan ya emite hechos (`route_km`, `route_cost`, `vehicle_idle`) | Bajo: un hecho informativo configurable, sin bloquear | Bajo-medio: el escenario demo tiene unos 15 puntos y 2 vehículos, avisaría casi siempre | Opcional; nunca como restricción |
| AMP-07 | Editar peso/volumen de producto y costo/capacidad de vehículo sin reimportar (HU-EVT-01/02 de Anthony) | Nota de Anthony | No hay endpoint de edición de productos ni vehículos; solo importación | Medio: endpoints, validación, auditoría y pantalla | Medio | Después del evento, salvo que la mentoría lo pida |
| AMP-08 | Zonas de reparto habituales para agrupar paradas (HU-EVT-03) | Rutas por zona y corredor | `ServicePoint.Region` existe; el motor no lo usa | Medio-alto: cambia la asignación de vehículos | Medio | Próximo paso en el pitch |
| AMP-09 | Ajustar a mano la secuencia antes de publicar (parte de HU-EVT-04) | Nota de Anthony | **Ya existe:** botones ▲▼ por parada en el asistente de rutas | Sin código | Medio | Mostrar si cabe en el tiempo |
| AMP-10 | Caminos mínimos «con Dijkstra» (parte de HU-EVT-04) | Nota de Anthony | Las distancias salen de OSRM sobre la red vial real, que ya resuelve caminos mínimos; hay caché y aproximación de respaldo | Un Dijkstra propio duplicaría lo que OSRM ya entrega | Bajo | No implementar; decir «distancias por calles reales» |
| AMP-11 | Empaque de entrada frente a unidad de salida (caja de 24 o 48 → unidades) | Comercializadora que abre cajas | Unidad base por producto; no hay factor de empaque | Alto: modelo de datos y conciliación | Medio | Próximo paso |
| AMP-12 | Promociones 6+1 / 12+2 con unidad bonificada | Comercializadora | No hay precios ni promociones; el modelo es logístico | Alto | Bajo para el Reto 1 | Fuera |
| AMP-13 | Consumo por superficie (litros ↔ m²) o cotizador | «Eso no tenemos en ningún lado para poder cuantificar» | No hay datos de rendimiento por producto ni área | Alto y sin datos | Medio como visión | Próximo paso en el pitch, sin cifras |
| AMP-14 | Dosificador o verificación de lo aplicado | Primer dolor de la charla | Fuera del reto: la custodia termina en la recepción | — | — | Fuera; otro equipo lo trabaja |

## Lo más importante y fácil

**AMP-01 y AMP-03.** Las dos preguntas que el sector dejó sin respuesta (a quién atender primero y qué hacer ante un cierre vial) son las que el motor ya está cerca de contestar. AMP-03 no necesita código. AMP-01 es un parámetro opcional que reutiliza la regla de ventanas horarias que el motor ya respeta.

Frase de pitch propuesta, **solo si AMP-01 se implementa y se ensaya**: «El planificador puede exigir que los puntos críticos reciban antes de las 11:00, y el sistema muestra cuánto cuesta esa decisión.» La segunda mitad depende de AMP-02.

## Análisis: prioridades de entrega en el código actual

### Qué ya hace

- **Orden de urgencia para repartir stock y camión.** La asignación de stock y el motor usan el mismo orden: nivel de riesgo (Crítico antes que Alto) → holgura (cobertura − lead time) → criticidad del punto (alta, media, normal) → código. Ver `src/Application/Logistics/ReplenishmentAllocator.cs` y `src/Application/Logistics/Planning/RoutePlanner.cs` (lista `mandatory`). Si no alcanzan el stock, la capacidad o la jornada, los menos urgentes quedan **no atendidos** con su motivo.
- **Consolidación acotada.** Los puntos en «Vigilar» entran solo si el desvío no supera `MaxDetourMinutes` (20 min).
- **Solo urgentes.** `PlanRequest.IncludeConsolidation = false` deja fuera la consolidación. Existe en la API, pero la web llama a `createRoutePlan({})` y no lo ofrece.
- **Replanificación con modos.** `Strict`; `LateService`, que admite llegar tarde a obligatorios; y `PrioritizeCritical`, que descarta las paradas de consolidación pendientes.
- **Datos de prioridad ya guardados.** `ServicePoint.Criticality` se importa del CSV (columna `criticality`, alias `prioridad`). `DeliveryOrder.Priority` (Critical/High/Normal/Low) y `RequiredDateUtc` existen; los pedidos de emergencia nacen como Critical.

### Qué no hace

- **La prioridad decide quién entra, no a qué hora llega.** La inserción busca la posición más barata y el 2-opt reordena solo por costo (km × USD/km + horas × USD/h). Un hospital crítico puede quedar como última parada si eso es más barato y su ventana lo permite.
- **No hay parámetro de prioridad** en `PlanRequest` ni en la pantalla de Rutas.
- **El asistente de pedidos (`/suggest`)** arma recogidas y luego entregas en el orden del abastecimiento, sin usar `Priority` ni `RequiredDateUtc`, y con ventana fija 08:00–17:00 en lugar de la del punto.

### ¿Es adaptable? Sí

Inserción, 2-opt y replanificación pasan por un único evaluador, `RouteFeasibility.EvaluateRoute`. Una regla puesta ahí, o en los datos que recibe, llega a todo el motor. El simulador de 90 días (spec 005) no usa este planificador, así que sus cifras no cambian. El agente (`AgentRunService`) sí lo usa y podría pasar el mismo parámetro.

| Opción | Cómo | Archivos | Esfuerzo | Riesgo |
|---|---|---|---|---|
| A — Hora límite para críticos (recomendada) | Parámetro opcional en `PlanRequest`, p. ej. `CriticalDeadlineLocal = "11:00"`. Al crear cada `StopCandidate`, si el riesgo es Crítico (o la criticidad del punto es alta), el fin de la ventana pasa a ser el mínimo entre la ventana del punto y la hora límite. Si la ventana del punto empieza después de la hora límite, se respeta la del punto. El evaluador ya rechaza llegadas fuera de ventana, y la inserción prueba otra posición u otro vehículo; si no cabe, la parada queda «no atendida: time_window» | `PlanModels.cs`, `RoutePlanner.cs`, `RoutePlansController.cs` y la página de Rutas | Bajo | Bajo: con el parámetro vacío el plan es idéntico y el costo en USD sigue siendo real |
| B — Penalizar la espera según prioridad | Sumar al objetivo peso(prioridad) × horas hasta la llegada. Separar «objetivo» de «costo» en `FeasibilityCheck` para no inflar los USD reportados, y comparar por objetivo en inserción y 2-opt | `RouteFeasibility.cs`, `RouteImprover.cs`, `RoutePlanner.cs` y pruebas | Medio (~medio día) | Medio: cambia la comparación interna y cuesta más explicarlo al jurado |
| C — Prioridad en `/suggest` | Ordenar entregas por `Priority` y `RequiredDateUtc`, aplicar vecino más cercano dentro de cada nivel y usar las ventanas del punto | `RoutePlansController.cs` | Medio | Medio: toca un flujo que el equipo ya validó en pantalla |

Para la hackathon: **A**. B y C quedan como evolución.

### Pruebas mínimas si se implementa A

- Sin parámetro, el plan del fixture no cambia: `RoutePlannerTests` y `RoutePlanningTests` siguen en verde.
- Con hora límite, una parada crítica que llegaba después pasa a llegar antes de esa hora, o queda no atendida con `time_window` si no hay forma de cumplirla.
- Una parada no crítica conserva su ventana.

## Preguntas para la mentoría con Mauro Zurita (16-oct, 16:15–17:00)

- Cuando no alcanza el camión, ¿qué entrega va primero: interrupción del servicio, criticidad del área, ventana o contrato? Sirve para contrastar el orden actual del motor.
- ¿Los hospitales tienen una hora límite de recepción, por ejemplo antes del cambio de turno? Sirve para AMP-01.
- ¿Cuántos días antes necesitan saber que algo se va a agotar? Sigue sin respuesta.
- ¿Una emergencia justifica salir con menos entregas de las habituales? Sirve para AMP-06.

Registrar respuestas y consentimiento según [[Registro de mentorias]].

## Decisiones pendientes

| Decisión | Opciones | Quién decide |
|---|---|---|
| Implementar AMP-01 antes del congelamiento | Sí, con responsable y hora tope; o dejarlo como próximo paso | Equipo |
| Hora límite por defecto en la demo | Valor fijo (p. ej. 11:00) o elegido en pantalla | Equipo, después de la mentoría |
| Qué cuenta como «crítico» para la hora límite | Nivel de riesgo Crítico, criticidad alta del punto, o ambos | Equipo |
| Qué ideas se nombran en el pitch como próximo paso | AMP-08, AMP-11, AMP-13 | Equipo, con [[Guion del pitch]] |

Si una idea se aprueba, pasa a [[Backlog]] o a un spec según [[Convenciones y estados]]; esta nota no amplía specs por sí sola. Relacionadas: [[Alcance y prioridades]], [[Rutas y sobrecostos]], [[Plus para ganar]] y [[Reto 1 oficial]].
