---
id: "002"
titulo: Motor logístico determinista
estado: borrador
creado: 2026-10-12
actualizado: 2026-10-13
responsable: Anthony Pilatasig (motor logístico) y Bryan Doicela (revisión)   # asignación de Joseph Baño, 13-oct
aprobado_por: PENDIENTE     # diseño aprobado en sesión de brainstorming del 12-oct; registrar nombre del integrante y confirmación del equipo
depende_de: ["001"]
boveda:
  - 04 Datos y algoritmos/Inventario y prediccion.md
  - 04 Datos y algoritmos/Rutas y sobrecostos.md
  - 04 Datos y algoritmos/Dataset y escenarios.md
  - 02 Producto/Requisitos y aceptacion.md
  - 03 Arquitectura/Contratos API y eventos.md
  - 03 Arquitectura/Decisiones de arquitectura.md
  - 06 Equipo/Definiciones de producto y plan hasta el pitch.md
---

# 002 — Motor logístico determinista

## Contexto

El Reto 1 pide tres capacidades: **predecir puntos de reabastecimiento crítico**, **planificar rutas eficientes** y **garantizar la trazabilidad** ([Reto 1 oficial](../../boveda/01%20Oficial/Reto%201%20oficial.md)). Las dos primeras son este spec. Hoy no existen en el código: el panel web muestra cifras de ruta escritas a mano (`apps/web/src/widgets/route-map/route-map.widget.ts`), lo que el spec 005 debe reemplazar por los datos de este motor.

El motor es también **la caja de herramientas del agente** (spec 003). Las definiciones D4 y D5 ([Definiciones de producto y plan hasta el pitch](../../boveda/06%20Equipo/Definiciones%20de%20producto%20y%20plan%20hasta%20el%20pitch.md)) exigen que el LLM nunca calcule rutas, cantidades ni fechas: todo número sale de aquí. Y la autonomía del agente la decide un **motor de reglas determinista** que también vive aquí, para que ni un error del modelo ni una instrucción inyectada puedan saltarse la política.

El diseño se aprobó en la sesión de brainstorming del 12-oct: motor en .NET dentro de `Application` (enfoque A), fuente de distancias en cadena con caché, selección de puntos por riesgo más consolidación, tres tipos de incidente, política de autonomía con opciones calculadas y fixture regional `demo-region`.

## Objetivo y aporte a la nota

**Objetivo:** que, con los datos que haya cargado el importador, el sistema diga qué puntos necesitan reposición y por qué, arme rutas factibles para dos vehículos, reaccione a un incidente conservando lo ejecutado y decida —con reglas visibles— qué cambios se aplican solos y cuáles necesitan a una persona.

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| Prototipo/demo | 20% | Riesgo → ruta → incidente → replanificación funcionando en vivo sobre `demo-region`, siempre igual gracias al incidente predefinido |
| Impacto potencial | 15% | El simulador (005) llama a este planificador 90 veces por política: sin él no hay cifras de km, costo ni quiebres |
| Innovación | 15% | Autonomía con límites verificable: una parte se ejecuta sola, otra se escala con dos opciones calculadas |
| Viabilidad técnica | 15% | Heurística explicable, determinista y medida (≤ 1 s con 15 puntos); funciona sin internet gracias a la caché de distancias |

## Historias de usuario

### H1 — Saber qué puntos están en riesgo y por qué (P1)

**Como** planificador, **quiero** ver cada punto y SKU con su cobertura en días, su nivel de riesgo y los datos que lo producen, **para** decidir con evidencia y explicársela a gerencia.

**Escenarios de aceptación:**

1. **Dado** el punto `HOS-N01` con 60 L disponibles y 84 L consumidos en 7 días, lead time 2 y margen 3, **cuando** consulto los riesgos, **entonces** veo cobertura 5,0 días, nivel `ALTO` y los valores de entrada.
2. **Dado** un punto sin consumos en la ventana, **cuando** consulto los riesgos, **entonces** su nivel es `SIN_DATOS` y no se muestra ninguna cobertura.
3. **Dado** un dataset con fechas históricas, **cuando** el reloj operativo está en modo `Dataset`, **entonces** «hoy» es el día siguiente al último periodo de consumo importado y los riesgos se calculan respecto de esa fecha.

### H2 — Obtener rutas factibles para dos vehículos (P1)

**Como** planificador, **quiero** que el sistema proponga una ruta por vehículo que atienda los puntos en riesgo y aproveche el viaje para los que pronto lo estarán, **para** reducir viajes sin incumplir ventanas ni capacidad.

**Escenarios de aceptación:**

1. **Dado** `demo-region`, **cuando** pido una propuesta, **entonces** recibo dos rutas que atienden los 5 puntos obligatorios, con hora de llegada, carga, km, costo y origen de cada distancia.
2. **Dado** un punto obligatorio que no cabe en ningún vehículo o cuya ventana no se alcanza, **cuando** pido la propuesta, **entonces** aparece en «no atendidos» con su motivo (`capacity`, `time_window`, `warehouse_stock` o `unreachable`).
3. **Dado** la misma base y el mismo reloj, **cuando** pido la propuesta dos veces, **entonces** las rutas son idénticas.
4. **Dado** una propuesta aprobada, **cuando** la publico, **entonces** se crean las entregas por parada con lotes elegidos por vencimiento más próximo (FEFO) y se reserva el stock de la bodega; publicar dos veces no duplica nada.

### H3 — Reaccionar a un incidente conservando lo ejecutado (P1)

**Como** planificador, **quiero** registrar un cierre de vía, una demora o una avería y obtener una nueva revisión de las rutas afectadas, **para** reaccionar sin rehacer el día ni perder lo ya entregado.

**Escenarios de aceptación:**

1. **Dado** una ruta con 2 paradas completadas, **cuando** se registra un cierre de vía que cruza un tramo pendiente, **entonces** la nueva revisión conserva las 2 paradas completadas y no usa el tramo cerrado.
2. **Dado** una avería del vehículo 1, **cuando** se replanifica, **entonces** sus paradas pendientes se intentan atender con el vehículo 2 tras volver al depósito a cargar; las que no caben en la jornada quedan como no atendidas.
3. **Dado** una revisión publicada por error, **cuando** la deshago antes del despacho de sus entregas pendientes, **entonces** se liberan solo sus reservas activas, se publica la revisión anterior como una revisión nueva y el historial conserva las tres. Si alguna entrega pendiente ya se despachó, el sistema conserva su movimiento y exige conciliación en vez de liberar stock físico.

### H4 — Reglas visibles que deciden qué se ejecuta solo (P1)

**Como** gerente, **quiero** que los cambios rutinarios se apliquen solos y que lo excepcional me llegue con opciones, **para** confiar en la automatización sin perder el control.

**Escenarios de aceptación:**

1. **Dado** una replanificación que no retira paradas obligatorias, cumple todas las ventanas, sube el costo ≤ 15% y no retrasa ninguna llegada más de 60 min, **cuando** el modo es «autónomo con límites», **entonces** se publica sola y queda registrada como ejecutada automáticamente con sus motivos.
2. **Dado** una replanificación en la que el hospital perdería su ventana, **cuando** se evalúa, **entonces** queda escalada con dos opciones calculadas —«atender tarde» y «priorizar críticos»— cada una con costo, horas de llegada y lo que incumple.
3. **Dado** el modo «solo proponer», **cuando** se evalúa cualquier propuesta, **entonces** ninguna se ejecuta sola.
4. **Dado** el incidente predefinido de `demo-region`, **cuando** se dispara, **entonces** la ruta del vehículo 2 se ajusta sola y la del vehículo 1 se escala.

### H5 — Funcionar sin la wifi del recinto (P1)

**Como** equipo, **queremos** que el ruteo use distancias viales guardadas y solo pida a internet las que falten, **para** que la demo no dependa de la conexión.

**Escenarios de aceptación:**

1. **Dado** `demo-region` importado con su archivo de tramos, **cuando** se planifica sin acceso a internet, **entonces** todas las distancias tienen origen `road` y no hay llamadas externas.
2. **Dado** un punto nuevo sin tramos guardados y OSRM inaccesible, **cuando** se planifica, **entonces** sus tramos usan la aproximación declarada, se marcan `approximation` y la propuesta indica que contiene aproximaciones.

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | El sistema DEBE tener un **reloj operativo** con modos `Dataset` (día siguiente al fin del último periodo de consumo), `Real` y `Fixed` (fecha configurada); la hora de inicio de jornada es configurable. | Inventario y prediccion (corte de datos) |
| FR-02 | DEBE calcular por punto y SKU: demanda diaria = Σ cantidad ÷ Σ días de periodo en la ventana `DemandWindowDays` previa al reloj; disponible = existencias − reservado en el punto; cobertura = disponible ÷ demanda diaria. | RF-03, Inventario y prediccion |
| FR-03 | DEBE clasificar en `CRITICO` (cobertura ≤ lead time), `ALTO` (≤ lead time + margen), `VIGILAR` (≤ lead time + margen + días de consolidación), `NORMAL` y `SIN_DATOS` (demanda cero o ausente, o última actualización de stock más antigua que `StaleStockDays`). El lead time por fila del punto prevalece sobre el global. | RF-03, Inventario y prediccion (datos insuficientes) |
| FR-04 | DEBE calcular la cantidad sugerida = máx(0, demanda × `TargetCoverageDays` + `SafetyStockUnits` − posición), con posición = disponible + cantidad en camino, limitada por el stock de bodega que excede su stock de seguridad y por la capacidad en peso y volumen. | RF-03, Inventario y prediccion |
| FR-05 | DEBE guardar cada cálculo como `RiskSnapshot` con entradas, versión de fórmula y reloj usado. | Simulador y metricas (trazabilidad) |
| FR-06 | DEBE obtener distancias y tiempos en cadena: tramos guardados → OSRM (URL configurable, se guarda lo obtenido) → línea recta × `ApproxDetourFactor` a `ApproxSpeedKmh`. Cada tramo informa su origen (`road` o `approximation`). | Rutas y sobrecostos, ADR-05 |
| FR-07 | El importador de 001 DEBE aceptar una octava entidad canónica `road_segments` (origen, destino, km, minutos, trazado opcional), para que el fixture y cualquier matriz que entregue la organización entren por la misma puerta (G2). | — |
| FR-08 | DEBE proponer una ruta por vehículo disponible: obligatorios (`CRITICO`, `ALTO`) ordenados por prioridad; inserción más barata y factible; 2-opt intrarruta; consolidación de `VIGILAR` con desvío ≤ `MaxDetourMinutes`; capacidad en peso y volumen, ventanas con espera, tiempo de servicio y duración de jornada. | RF-04, Rutas y sobrecostos |
| FR-09 | DEBE devolver los obligatorios no atendidos con motivo: `capacity`, `time_window`, `warehouse_stock`, `unreachable`, `workday`. Cada no atendido lleva sus líneas (`lines`) con cantidad requerida, planificada y pendiente; una atención parcial (FR-24) aparece como no atendido `capacity` con cantidad planificada mayor que cero. | RF-04 |
| FR-10 | DEBE ser determinista: misma base, misma configuración y mismo reloj → misma respuesta; los empates se resuelven por código. | Simulador y metricas (reproducibilidad) |
| FR-11 | DEBE calcular costo = km × costo/km (del vehículo o global) + horas × `CostPerHour`, con el mismo modelo que usará el simulador. | Rutas y sobrecostos (costos) |
| FR-12 | Publicar una propuesta DEBE crear `Delivery` y `DeliveryLine` por parada con lotes FEFO no bloqueados ni vencidos, registrar cada reserva por revisión/línea/lote, sumar la reserva en el saldo de bodega y ser idempotente. Deshacer una revisión libera solo sus reservas activas no despachadas en la misma transacción que publica la revisión restaurada; las consumidas se conservan. | RF-02, RNF-02, RNF-03 |
| FR-13 | DEBE registrar incidentes `ROAD_CLOSURE` (zona circular), `DELAY` (zona y factor) y `VEHICLE_BREAKDOWN` (vehículo) y devolver las rutas y paradas afectadas. | RF-11 |
| FR-14 | Replanificar DEBE crear una nueva revisión que conserve las paradas completadas, parta de la última parada completada (o del depósito) y devuelva la diferencia con la revisión anterior. | RF-11, Rutas y sobrecostos (incidente) |
| FR-15 | Un cierre DEBE inutilizar los tramos cuyo trazado cruza la zona; una demora DEBE multiplicar su tiempo; una avería DEBE reasignar las paradas pendientes al otro vehículo tras volver al depósito, si la jornada lo permite. | RF-11 |
| FR-16 | La **política de autonomía** DEBE clasificar cada propuesta por vehículo en `AutoExecuted` o `Escalated` con motivos, según los umbrales `Agent:Policy:*`, y en el modo `ProposeOnly` escalar todo. | D5 |
| FR-17 | Una propuesta escalada DEBE incluir dos opciones calculadas por el planificador: `late_service` (ventanas blandas para los obligatorios, menor costo) y `prioritize_critical` (sin consolidación, reasignación entre vehículos, ventanas duras); si una opción no es factible, lo dice. | D5 |
| FR-18 | Cada clasificación DEBE registrarse como `AgentDecision` (tipo, estado, propuesta, motivos, quién la originó); aprobar, rechazar y deshacer DEBEN actualizarla. | D5 |
| FR-19 | Cada respuesta del motor DEBE incluir `facts`: lista estructurada de cifras con código, sujeto, valor y unidad, de la que salen las explicaciones por plantilla y con la que el spec 003 valida el texto del LLM. | RF-12, D4 |
| FR-20 | DEBE ofrecer explicaciones en español por plantilla para riesgo, propuesta, replanificación y escalado, construidas solo a partir de `facts`. | RF-12 (fallback determinista) |
| FR-21 | DEBE existir el fixture `data/fixtures/demo-region` (≈15 puntos sintéticos de Quito, 1 bodega, 2 vehículos, 3 SKUs, tramos viales precalculados e incidentes predefinidos) y un script que regenera sus tramos con OSRM. | Dataset y escenarios, D6 |
| FR-22 | `POST /api/v1/demo/incidents` DEBE disparar un incidente predefinido del fixture por su nombre, solo con `Demo:Enabled=true`. | Contratos API y eventos |
| FR-23 | `GET /api/v1/route-plans` DEBE listar planes del día de forma paginada, filtrable por vehículo y estado, con revisión vigente, para que el panel 005 descubra IDs sin fijarlos en código; la respuesta no crea propuestas. Dueño: equipo 002; integrar antes de H4. | RF-07, spec 005 |
| FR-24 | **Atención parcial por capacidad (#199).** Si la carga de un obligatorio no cabe completa en ningún vehículo, el motor DEBE planificar la cantidad que cabe en peso **y** volumen (unidades base enteras) y conservar el remanente pendiente con motivo `capacity`; puede repartirlo en otro vehículo con capacidad libre. Por línea se expone `requiredQuantity` (sugerida o pedida), `quantity`/`plannedQuantity` (factible, la que se publica y reserva) y `pendingQuantity` (remanente), y los hechos `required_quantity`, `planned_quantity` y `pending_quantity` (sujeto `PUNTO/SKU`). Política determinista: entre puntos, el orden de prioridad de FR-08 (nivel, holgura de cobertura, criticidad, código); dentro de una parada, nivel de riesgo y luego SKU (ordinal); entre vehículos, el que lleva más unidades, luego menor costo adicional y luego código. `POST /route-plans/confirm` DEBE exigir `vehicleId` de un vehículo activo, recalcular las cargas desde los pedidos (sin confiar en totales del panel), planificar solo lo que cabe (prioridad: criticidad del punto, fecha requerida, número de pedido, SKU), guardar esas cargas por parada para que la publicación reserve exactamente lo planificado, sumar lo planificado en `DeliveryOrderLine.PlannedQuantity` y marcar `Planned` solo los pedidos sin remanente; rechaza con 400/422 si falta el vehículo, si un pedido no tiene parada o saldo pendiente, o si no cabe ninguna unidad. | RF-03, RF-04, #199 |

### No funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| NFR-01 | Propuesta de 15 puntos y 2 vehículos ≤ 1 s; 80 puntos ≤ 5 s (el simulador la llama 90 veces por política). | RNF-10 |
| NFR-02 | Sin llamadas de red cuando todos los tramos están guardados. | D3 (wifi), H5 |
| NFR-03 | Toda cifra que se muestre o explique proviene de `facts`; ninguna explicación inventa números. | RF-12 |
| NFR-04 | Ningún nombre, cantidad, región ni coordenada del escenario en código C# (G1). | G1 |
| NFR-05 | Las cantidades respetan la unidad base del SKU; peso y volumen se calculan con los factores del producto. | Modelo de datos (cantidades) |
| NFR-06 | La política no depende de texto del LLM: recibe solo estructuras calculadas. | D5 |

## Entidades y contratos

**Esquema:** usa las tablas de 001 (`RiskSnapshot`, `RoutePlan`, `RouteStop`, `Incident`, `Delivery`, `DeliveryLine`, `StockBalance`, `AgentDecision`). Añade **una** migración con dos tablas: `RoadMatrixEdge` (nombre ya presente en el diseño de la bóveda) como caché de tramos, indexada por coordenadas redondeadas para sobrevivir al reset de demo, y `RouteStockReservation` para atribuir a cada revisión, entrega, línea y lote su cantidad reservada y su estado (`Active`, `Released`, `Consumed`). El saldo reservado de bodega es el agregado de reservas activas. El despacho de 004 marca la reserva consumida al mover el stock físico.

**Endpoints** (prefijo `/api/v1`; detalle de cuerpos y respuestas en `plan.md`):

| Método y ruta | Rol | Función |
|---|---|---|
| `GET /risks` | Cualquiera | Riesgos con entradas, nivel, cantidad sugerida y `facts` |
| `POST /route-plans` | Planificación, Automatización | Propone una ruta por vehículo (guardadas como `Proposed`) |
| `GET /route-plans/{id}` | Cualquiera | Revisión con paradas, trazados, origen de distancias y `facts` |
| `GET /route-plans` | Cualquiera | Lista paginada de planes del día, vehículo, estado y revisión vigente para el panel 005; no crea propuestas |
| `POST /route-plans/{id}/publish` | Planificación | Crea entregas y reservas; idempotente |
| `POST /route-plans/{id}/stops/{sequence}/complete` | Planificación, Conductor | **Provisional hasta 004.** Marca una parada como completada (004 lo hará con la recepción real) |
| `POST /route-plans/{id}/revert` | Planificación | Vuelve a publicar la revisión anterior como nueva |
| `POST /incidents` | Planificación, Automatización | Registra un incidente y devuelve las rutas afectadas |
| `POST /route-plans/{id}/replan` | Planificación, Automatización | Nueva revisión + clasificación de la política + opciones si se escala |
| `GET /agent-decisions` | Cualquiera | Decisiones por estado (`AutoExecuted`, `Escalated`, `Approved`, `Rejected`, `Reverted`) |
| `POST /agent-decisions/{id}/approve` | Planificación | Publica la opción elegida |
| `POST /agent-decisions/{id}/reject` | Planificación | Mantiene la revisión vigente |
| `GET /autonomy/mode`, `PUT /autonomy/mode` | Cualquiera / Gerencia | Leer o cambiar `AutoWithLimits` ↔ `ProposeOnly` |
| `POST /demo/incidents` | Planificación | Dispara un incidente predefinido del fixture |

## Guardarraíles aplicados

| Guardarraíl | Aplicación |
|---|---|
| G1 | Umbrales, costos, ventanas por defecto y velocidades en `Logistics:*` y `Agent:Policy:*`; el escenario vive en `data/fixtures/demo-region`. |
| G2 | Los tramos viales entran por el importador como `road_segments`. |
| G3 | `SIN_DATOS` en vez de inventar cobertura; tramos aproximados etiquetados; no atendidos con motivo. |
| G4 | Carga calculada en peso y volumen desde la unidad base del SKU. |
| G5 | Lead time, ventana y tiempo de servicio por punto prevalecen sobre los globales. |
| G7 | Ningún tratamiento especial por categoría: el hospital es prioritario por su criticidad y su ventana, que son datos. |

## Fuera de alcance

- Más de una bodega de salida por día, flotas de más de 2 vehículos en vivo (el algoritmo admite N vehículos, pero no se optimiza ni se prueba para más de 2 en la demo).
- Solver de óptimo global (OR-Tools u otro): la heurística se presenta como «ruta optimizada por la política», no como óptima.
- Rutas de varios días, pernocta, recargas múltiples o carga parcial entre vehículos fuera de la avería.
- Geocodificación de direcciones.
- Pronóstico estadístico más allá de la media de la ventana.
- Chat, tiempo real con SignalR y notificaciones push (004 y 005 deciden cómo se entera cada usuario).
- La interfaz del panel (005) y el agente con LLM (003).

## Contradicciones con la bóveda

> **⚠️ Contradicción detectada** — Hecho nuevo: una propuesta mezcla tramos viales y aproximados cuando OSRM no responde, etiquetados por tramo. · Hecho establecido: «Viaje en línea recta solo se permite como aproximación explícita en un escenario separado» ([Rutas y sobrecostos](../../boveda/04%20Datos%20y%20algoritmos/Rutas%20y%20sobrecostos.md)). · Impacto: sin mezcla, un punto nuevo sin red dejaría la propuesta sin ruta. · Opciones: (a) mezclar con etiqueta por tramo y marca global en la propuesta; (b) negar la propuesta. · Propuesta: (a) para la operación; **el simulador (005) no mezcla**: cada corrida usa solo tramos viales o solo aproximación, como pide la bóveda para comparaciones.

> **⚠️ Contradicción detectada** — Hecho nuevo: `POST /api/v1/route-plans` responde de forma síncrona (201) porque la propuesta tarda ≤ 1 s. · Hecho establecido: [Contratos API y eventos](../../boveda/03%20Arquitectura/Contratos%20API%20y%20eventos.md) prevé «202 si es asíncrono» con `GET /jobs/{id}`. · Impacto: ninguno para la demo; el contrato asíncrono queda para volúmenes nacionales. · Propuesta: síncrono ahora; se conserva la ruta del contrato.

> **⚠️ Contradicción detectada** — Hecho nuevo: decisiones de ruta que se publican solas según reglas deterministas (D5). · Hecho establecido: ADR-08 dice «LLM solo explicación y recomendaciones acotadas». · Impacto: no hay contradicción de fondo —quien decide es la política, no el LLM—, pero ADR-08 debe registrar la autonomía con límites. · Propuesta: ADR-22 (propuesta) en [Decisiones de arquitectura](../../boveda/03%20Arquitectura/Decisiones%20de%20arquitectura.md).

Notas revisadas sin contradicción: Inventario y prediccion (fórmulas, niveles, datos insuficientes, prioridad), Modelo de datos (invariantes de stock y reserva), Dataset y escenarios (S-02, S-04, S-08, S-09, S-10), Hechos canonicos (nombres: `RoadMatrixEdge`, `RoutePlan`, `RouteStop`, `Incident`, `AgentDecision`).

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| El incidente predefinido no produce «uno solo, uno escalado» con las rutas reales del algoritmo | Media | Alto: se pierde el momento wow | Se calibra después de implementar el planificador; una prueba E2E fija el resultado esperado y falla si cambia |
| La heurística da rutas visiblemente malas en el mapa | Baja | Medio | 2-opt + prueba que compara con fuerza bruta en instancias de ≤ 8 puntos |
| OSRM público rechaza o limita el script de tramos | Media | Bajo | El script respeta 1 petición/s y se ejecuta una vez; plan B: OSRM en Docker con el extracto de Ecuador en un portátil |
| Rendimiento insuficiente para el simulador | Baja | Alto | Presupuesto de 5 s con 80 puntos y prueba de rendimiento en CI |
| Zona horaria en el contenedor (globalización invariante) | Baja | Medio | Ventanas en hora local de `Organization.TimeZone` con `tzdata` instalado (ya está en la imagen); Ecuador no tiene horario de verano; prueba de conversión |

## Criterios de éxito

- CE-01: sobre `demo-region`, los riesgos dan exactamente 1 `CRITICO`, 4 `ALTO`, 3 `VIGILAR`, 1 `SIN_DATOS` y 6 `NORMAL`; `HOS-N01` tiene cobertura 5,0.
- CE-02: la propuesta atiende los 5 obligatorios y ≥ 2 de los 3 de consolidación, sin violar ventanas ni capacidad, en ≤ 1 s.
- CE-03: dos propuestas con la misma base son idénticas byte a byte (JSON serializado).
- CE-04: el incidente predefinido produce una decisión `AutoExecuted` (vehículo 1) y una `Escalated` (vehículo 2) con las dos opciones calculadas.
- CE-05: sin red, la propuesta sobre `demo-region` no hace llamadas externas.
- CE-06: con 80 puntos sintéticos, la propuesta tarda ≤ 5 s.
- CE-07: toda cifra de las explicaciones por plantilla está en `facts` (prueba automática).

## Preguntas abiertas

- PA-01: umbrales iniciales de la política (15% de costo, 60 min de retraso): ¿se validan con el mentor del día 1? Decide: equipo en el evento; se cambian por configuración.
- PA-02: ¿quién del equipo ejecuta OSRM en su portátil como plan B el día del evento? Decide: equipo.

### Calibración posterior de CE-04 — 14-oct-2026

Bryan delegó escoger la combinación verificada que permita cerrar #39 sin
modificar los límites de autonomía. La calibración HTTP en PostGIS local fija
VEH-01=AutoExecuted y VEH-02=Escalated (CLI-S09 bloqueada); sustituye el mapeo
inverso del borrador. Siguen vigentes 15% de costo, 60 min de demora, ninguna
parada obligatoria omitida en autoejecución y dos opciones calculadas. Fuente:
[registro #39](../003-agente-logistico/task.md). No acredita ejecución productiva.
