---
id: 005
titulo: Panel web y simulador comparativo de 90 días con KPIs
estado: borrador
creado: 2026-10-12
actualizado: 2026-10-12
aprobado_por: PENDIENTE
depende_de: [001, 002, 003, 004]
boveda: ["04 Datos y algoritmos/Simulador y metricas", "04 Datos y algoritmos/Dataset y escenarios", "04 Datos y algoritmos/Rutas y sobrecostos", "06 Equipo/Definiciones de producto y plan hasta el pitch"]
---

# 005 — Panel web y simulador comparativo de 90 días con KPIs

## Contexto

El jurado debe entender en pocos segundos un riesgo real de desabastecimiento, la acción que propuso el motor y el efecto medido de esa política. Impacto potencial pesa 15 % y la demo práctica 20 %; cinco de siete jurados no son técnicos. El recorrido en vivo usa `demo-region` (unos 15 puntos y dos vehículos); la red nacional sintética de unos 80 puntos aparece solo en la comparación de 90 días. Las cifras de la simulación son **sintéticas**, no ahorro observado en una empresa.

Hoy el panel web contiene rutas, entregas, coordenadas y cifras fijas, incluidas «418 km» y «Ahorro: 24.8%» en `route-map.widget.ts` (R-15). El spec 005 sustituye esos valores por respuestas de la API. Reutiliza el login PKCE que implementa 004 (FR-17, T1b), los cálculos, `facts`, plantillas y reglas de autonomía de 002, y la traza del agente de 003. No hay resultados medidos todavía.

## Objetivo y aporte a la nota

Mostrar el recorrido operativo de forma comprensible y una comparación reproducible de la política IstpetDev contra **reposición reactiva** y **calendario fijo** sobre los mismos recursos y la misma demanda exógena, con límites y supuestos visibles.

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| Impacto potencial | 15 % | Rango de resultados de al menos tres semillas más un escenario adverso; dos baselines congelados, numeradores, denominadores y costos comparables. |
| Prototipo, demo o simulación práctica | 20 % | Panel conectado a API, mapa de ruta/incidente, decisiones aprobables y simulación ejecutada/exportable. |
| Presentación | Según matriz oficial | Cuatro indicadores en lenguaje cotidiano y detalle auditable para preguntas. |

## Historias de usuario

### H1 — Entender el riesgo y la ruta en vivo (P1)

**Como** gerente, **quiero** ver riesgo explicado, ruta por vehículo y estado de entregas, **para** entender por qué se repone y qué ocurrirá.

**Escenarios de aceptación:**

1. **Dado** `demo-region` importado, **cuando** se abre `/dashboard`, **entonces** riesgos, cantidades, ETA, km, costo, paradas y trazo provienen de la misma revisión de ruta y de la API; no aparecen puntos ni valores de muestra incrustados.
2. **Dado** un tramo aproximado, **cuando** se pinta el mapa, **entonces** es punteado y la leyenda dice «aproximación»; un tramo vial usa el trazado recibido.
3. **Dado** un incidente real del fixture, **cuando** cambia la revisión, **entonces** se conservan paradas completadas y el panel indica qué vehículo cambió y cuál no.

### H2 — Controlar las decisiones del agente (P1)

**Como** planificador, **quiero** distinguir decisiones ejecutadas solas de las que esperan mi aprobación, **para** mantener control sin ocultar la autonomía.

**Escenarios de aceptación:**

1. **Dado** una decisión `AutoExecuted` y otra `Escalated`, **cuando** consulto el panel, **entonces** aparecen en columnas separadas con motivos, versión de ruta, `facts` y origen de la explicación.
2. **Dado** una decisión escalada, **cuando** apruebo una opción factible o rechazo, **entonces** el panel confirma el estado devuelto por 002; deshacer una ruta publicada crea una revisión nueva por el endpoint de 002.
3. **Dado** el modo `ProposeOnly`, **cuando** el motor clasifica una propuesta, **entonces** espera aprobación; el interruptor muestra el valor confirmado por la API y se revierte visualmente si falla el cambio.

### H3 — Comparar 90 días sin sesgo de información (P1)

**Como** jurado, **quiero** comparar IstpetDev con dos prácticas razonables, **para** valorar impacto y reconocer sus límites.

**Escenarios de aceptación:**

1. **Dado** un dataset nacional canónico y una seed, **cuando** se ejecuta la comparación, **entonces** las tres políticas parten del mismo stock, vehículos, costos, matriz, obligaciones y calendario exógeno precalculado; ninguna conoce demanda o incidente futuros.
2. **Dado** tres seeds y un escenario adverso, **cuando** termina el trabajo, **entonces** veo rango mínimo–máximo y mediana por baseline, además de quiebres evitados **y nuevos**, demanda no atendida, costo y puntualidad.
3. **Dado** un denominador cero, **cuando** se calcula una tasa, **entonces** aparece «N/A» con causa y no un porcentaje inventado.

### H4 — Defender y reproducir un resultado (P1)

**Como** integrante del equipo, **quiero** exportar calendario, eventos, métricas y manifiesto de una comparación, **para** responder cómo se obtuvo cada cifra.

**Escenarios de aceptación:**

1. **Dado** un resultado visible, **cuando** descargo la exportación, **entonces** sus IDs, seed, hashes, versiones y métricas coinciden con la pantalla.
2. **Dado** el mismo manifiesto y archivos, **cuando** repito la corrida en un entorno aislado, **entonces** produce las mismas métricas dentro de la escala decimal declarada.

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | El panel DEBE consultar `GET /api/v1/risks`, `GET /api/v1/route-plans`, `GET /api/v1/route-plans/{id}`, `GET /api/v1/deliveries`, `GET /api/v1/agent-decisions`, `GET /api/v1/agent/runs` y `GET /api/v1/autonomy/mode`. Riesgo, ruta y entrega se correlacionan por punto, SKU, ruta/revisión y entrega. | RF-07, D5 |
| FR-02 | El mapa Leaflet/OSM DEBE representar `legs` de 002: trazo vial si `source=Road` y geometría recibida; si `source=Approximation`, segmento punteado y etiqueta visible. Centro y zoom se derivan de los puntos visibles (`fitBounds`), sin coordenadas iniciales del fixture. Si faltan mosaicos por falta de red, la lista de paradas, ETA y costos sigue disponible; los iconos necesarios se sirven localmente. | ADR-05, Rutas y sobrecostos |
| FR-03 | Las decisiones DEBEN aparecer en dos columnas: «Ejecutadas solas» y «Esperan tu aprobación». Cada tarjeta indica acción, motivo, `facts`, opción factible, revisión y enlace a la traza de 003. Estados posteriores (aprobada, rechazada, revertida) quedan en historial filtrable. | D5, RF-07 |
| FR-04 | Aprobar, rechazar y deshacer DEBEN usar los endpoints de 002; el interruptor «solo proponer» DEBE leer y escribir el modo de 002. El frontend no recalcula ni clasifica decisiones. | D5 |
| FR-05 | El panel DEBE usar el login PKCE y el interceptor del spec 004, sin otro flujo de autenticación. La API impone roles fijos de 001; controles de escritura solo para roles autorizados. | ADR-18, FR-17 de 004 |
| FR-06 | El generador DEBE emitir los archivos canónicos de 001 y `road_segments` de 002, además de manifiesto con seed, versión, parámetros y hash. DEBE cargarlos mediante el importador de 001 en un entorno de simulación aislado de `demo-region`; no se permite escritura directa de datos operativos desde el generador. | G1, G2, Dataset y escenarios |
| FR-07 | Antes de evaluar una política, la API DEBE precalcular por seed y escenario un calendario exógeno común de demanda solicitada, incidentes y tiempos. Cada política ve solo registros conocidos hasta la hora simulada. El flujo diario ordena observación → riesgo → plan/reserva → viajes/entregas → demanda → faltantes/conciliación; una entrega tardía no borra un faltante anterior. | RF-08, Simulador y metricas |
| FR-08 | La política IstpetDev DEBE reutilizar el cálculo determinista de riesgo, asignación, planificador y costo de 002 mediante servicios de aplicación, sin HTTP interno, LLM ni n8n. Para una corrida se elige **solo matriz vial completa** o **solo aproximación declarada**; nunca tramos mezclados. | D4, ADR-23, G5 |
| FR-09 | Los dos baselines de la tabla «Políticas congeladas» DEBEN ejecutarse con igual calendario, stock inicial, bodega, flota, capacidad, ventanas, matriz y costo. La configuración se versiona antes de la primera corrida; cualquier cambio crea nueva `policyVersion` y no reemplaza resultados previos. | D7, R-04 |
| FR-10 | `POST /api/v1/simulations/comparisons` DEBE iniciar trabajo asíncrono y devolver `202 {comparisonId,status}`. `GET /api/v1/simulations/comparisons/{id}` DEBE devolver progreso por seed, escenario y política, estado terminal, supuestos y métricas; un fallo conserva causa y permite nueva ejecución sin corromper el resultado previo. | RF-14 |
| FR-11 | `GET /api/v1/simulations/comparisons` DEBE listar corridas recientes y `GET /api/v1/simulations/comparisons/{id}/export` DEBE descargar ZIP con `manifest.json`, `metrics.json`, `resumen.csv`, `eventos.csv` y el calendario exógeno; el manifiesto incluye SHA-256 de cada archivo de datos (no de sí mismo) y su propia huella va en `ETag`. | RF-14, Simulador y metricas |
| FR-12 | La vista `/simulation` DEBE ofrecer comparación por baseline, rango mínimo–máximo y mediana de ≥3 seeds, escenario adverso separado, numerador/denominador, unidades, supuestos y enlaces a exportación. Debe distinguir «simulado» de «observado». | Plus 4, D7 |
| FR-13 | El escenario adverso DEBE aplicar demanda ×1,20, un vehículo menos y lead time +1 día, con las mismas perturbaciones para las tres políticas y sin variar semillas, calendario base ni reglas tras ver resultados. | EXP-04, EXP-06 |
| FR-14 | La vista en vivo DEBE usar `demo-region`; la red nacional solo se visualiza en `/simulation`. El control de escena usa `POST /api/v1/demo/route-plans/{id}/advance` del 004, nunca el marcado provisional de parada de 002. | D6, FR-16 de 004 |
| FR-15 | El resultado DEBE registrar km totales con regreso a depósito, costo de transporte, ahorro absoluto y reducción porcentual por baseline, km/costo por entrega completada, quiebres evitados y nuevos por clave punto–SKU–día, demanda no atendida por SKU/unidad, puntualidad de obligaciones comunes y servicio completo; tasas con denominador cero devuelven valor nulo y razón. La trazabilidad operativa se lee de 004 por líneas elegibles con evidencia confirmada; si la corrida no simula custodia, la métrica simulada se informa N/A (`not_simulated`), nunca como 100 % por scans. | Simulador y metricas |
| FR-16 | El panel DEBE consultar la API periódicamente cada 5 s mientras está visible, con pausa al ocultarse, indicador de última actualización y botón «Actualizar». Un error conserva el último snapshot etiquetado con su hora, sin presentar datos viejos como actuales. | D3, RF-07 |
| FR-17 | El generador DEBE partir de la media histórica previa por punto/SKU; aplicar factor semanal 1,0 en laborables y 0,8 en fin de semana, ruido determinista por seed en ±15 % y picos etiquetados en 5 % de las claves punto–SKU–día; el manifiesto guarda parámetros y calendario resultante. El escenario adverso multiplica demanda por 1,20 sobre el calendario base. | Dataset y escenarios, EXP-04 |
| FR-18 | El panel DEBE permitir a Planificación iniciar `POST /api/v1/agent/runs` (`watch`) y disparar el incidente de demo mediante `POST /api/v1/demo/incidents`; tras el avance de 004, debe mostrar la nueva revisión y las decisiones correlacionadas con el `AgentRun`. El botón se deshabilita mientras la petición está activa para evitar dobles disparos. | D4, S-04, H4 |
| FR-19 | El dataset nacional DEBE agrupar los ≈80 puntos en regiones operables con una bodega por grupo y vehículos locales; cada planificación región/día usa el planificador de 002 con su límite de una bodega y sin pernocta ni transferencias entre regiones. El número de regiones y vehículos es parámetro del dataset; el escenario adverso retira un vehículo identificado previamente en el manifiesto. | D6, Rutas y sobrecostos |
| FR-20 | Cada política DEBE validar al cierre diario balance de stock por lote/ubicación, capacidad/jornada/ventanas, demanda solicitada frente a satisfecha y pendientes; una violación marca la corrida `Failed` con evento y no publica KPI comparativos de esa seed. | Simulador y metricas, G4 |

### Políticas congeladas antes de la primera simulación

Estas reglas son **propuesta de equipo del 12-oct-2026**, pendiente de Deciders. Se fijan en el spec antes de observar resultados; si el equipo decide cambiarlas, debe versionarlas y repetir **las tres** políticas en todas las seeds.

| Política | Disparador, cantidad y orden | Información permitida |
|---|---|---|
| Reactiva (`reactive-v1`) | El punto avisa cuando el saldo disponible cae por debajo de la demanda histórica conocida de `leadTimeDays` días. Se pide cantidad para volver a 14 días de esa media, descontando saldo y tránsito conocidos, limitada por stock/capacidad; urgencias siguen orden de aviso, criticidad y código. Si no hay historial, declara supuesto y usa el consumo base del dataset. | Solo saldo, avisos y consumo revelado hasta el instante; mismo lead time y ventanas. |
| Calendario fijo (`fixed7-v1`) | Visita cada 7 días desde el inicio; la cantidad por punto–SKU es **fija**: 7 × media diaria del histórico previo al horizonte, redondeada a unidad base. No se cambia por resultado, saldo observado ni urgencias; stock/capacidad insuficiente deja pendiente explícito. Orden estable por región y código; misma flota y ventanas. | Calendario, histórico previo y stock disponible al cargar; no mira demanda futura. |
| IstpetDev (`istpetdev-v1`) | Riesgo, cantidad y ruta de 002 cada día con datos revelados, media móvil declarada y restricciones de stock, vehículo y ventanas. | Datos conocidos hasta el instante; sin oráculo de calendario ni LLM. |

**Costo común:** por viaje completado, km de ida, tramos y regreso × costo/km del vehículo, más horas de conducción/servicio × `CostPerHour` de 002; recargo de urgencia solo si se define y aplica igual a las tres políticas antes de correr. No se suma personal dos veces si costo/km ya lo incluye. Costo de plataforma, si se estima, se muestra separado del ahorro de transporte. Ninguna política recibe viajes imposibles ni stock negativo gratis.

### No funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| NFR-01 | La corrida de 3 seeds × 3 políticas × 90 días más 1 seed adversa × 3 políticas × 90 días suma 1080 jornadas de política y hasta `1080 × regiones` oportunidades de planificación. DEBE registrar llamadas reales, tiempos por región/política y total; objetivo de aceptación ≤60 min en el entorno de demo con matriz precargada y sin red. Con 6 regiones de ≤15 puntos son hasta 6480 llamadas: el límite ≤1 s de 002 para 15 puntos permitiría hasta 108 min, sin contar importación/IO. Perfilar y optimizar antes de H4; no se fabrican resultados. | H4, NFR-01 de 002 |
| NFR-02 | La comparación DEBE ser reproducible: hashes de entrada, matriz y calendario, versión de generador, código/política, seed, parámetros, horas simuladas y definición de cada métrica. Misma entrada da los mismos eventos/métricas. | RF-14 |
| NFR-03 | La simulación DEBE aislar datos y efectos de los operativos; iniciar, fallar, cancelar o reiniciar una corrida no cambia `demo-region` ni sus entregas, inventario o decisiones. | G2, D6 |
| NFR-04 | La pantalla de proyector a ≥1280 px y la móvil a 390 px DEBEN mantener legibles estado, comparador, unidades y etiquetas; color no es el único indicador, acciones tienen nombre y foco visible. | RNF-09, Presentación |
| NFR-05 | Ninguna cifra, nombre, coordenada, vehículo ni texto que afirme un evento operativo del fixture queda incrustado en el frontend; estados vacíos muestran «sin datos»/error y no una demostración simulada. | G1, R-15 |

## Entidades y contratos

`SimulationRun` y `SimulationMetric` son propiedad del esquema base de 001. El 005 añade en **una migración posterior a develop actualizado** metadatos de comparación/escenario/política/progreso/hash a `SimulationRun` y `SimulationEvent` para eventos exportables; la misma migración termina con `migrationBuilder.Sql(DemoGrants.Sql)`. Un `SimulationRun` representa una política, seed y escenario; `comparisonId` agrupa los tres runs por seed/escenario. Las métricas conservan código, versión de fórmula, unidad, numerador, denominador, valor nullable y razón N/A. La demanda exógena y los eventos usan un ledger de simulación separado del inventario operativo.

| Contrato del 005 | Rol | Respuesta/semántica |
|---|---|---|
| `POST /api/v1/simulations/comparisons` | Gerencia, Planificación | `202 {comparisonId,status}`; cuerpo `{datasetVersion,seeds,includeAdverse,policyVersions}`; valida matriz homogénea y recursos |
| `GET /api/v1/simulations/comparisons` | Gerencia, Planificación | Lista paginada de comparaciones y estados |
| `GET /api/v1/simulations/comparisons/{id}` | Gerencia, Planificación | Corridas, progreso, supuestos, métricas y errores |
| `GET /api/v1/simulations/comparisons/{id}/export` | Gerencia, Planificación | ZIP reproducible, solo si terminó; `409` si sigue calculando |

El generador produce datos canónicos y el importador de 001 los carga **en un contexto aislado**. El dueño de esta adaptación es 005; no se modifica el endpoint ni el contrato de importación de 001. El plan debe probar que el mismo escritor/validador canónico opera sobre ese contexto y que el reset no toca resultados archivados salvo selección explícita de limpieza de demo.

## Guardarraíles aplicados

| Guardarraíl | Prueba en 005 |
|---|---|
| G1 | Búsqueda de literales del escenario en `apps/web`; panel vacío sin API, y otro fixture cambia cifras/puntos sin recompilar. |
| G2 | Los archivos del generador pasan por el importador de 001 (y `road_segments` de 002), no por inserts privados. |
| G3 | Supuestos del importador y matriz se muestran en pantalla y manifiesto; faltantes de datos no se ocultan. |
| G4 | Demanda y faltantes se informan por SKU/unidad base; no se suman unidades incompatibles. |
| G5 | Costos, lead time, márgenes, capacidad y ventanas usan parámetros versionados y overrides importados. |
| G6 | Otro perfil canónico se prueba contra el simulador sin modificar el generador ni el motor. |
| G7 | Punto/SKU genéricos; «hospital» es dato narrativo del fixture, no rama de código. |
| G8 | La corrida y exportación previas al evento se registran con commit/tag y seed; la corrida del evento se distingue. |

## Fuera de alcance

- Login, PWA, QR, trazabilidad pública y `advance` (004); datos y explicación del agente (003); cálculo de rutas, `facts` y plantillas (002).
- Pronóstico ML, ajuste de baselines por resultado, selección manual de la mejor seed, costo empresarial observado o afirmación de ahorro real.
- SignalR, streaming, optimización nacional de varios días con pernocta, mapas viales de pago y nivel C del agente.

## Contradicciones con la bóveda

> **⚠️ Contradicción detectada** — Hecho nuevo: el calendario fijo y la reposición reactiva se concretan aquí con 7 y 14 días y reglas de aviso, antes de correr. · Hecho establecido: [Simulador y metricas](../../boveda/04%20Datos%20y%20algoritmos/Simulador%20y%20metricas.md) y D7 nombran baselines sin parámetros; [Rutas y sobrecostos](../../boveda/04%20Datos%20y%20algoritmos/Rutas%20y%20sobrecostos.md) exige frecuencia y urgencia acordadas. · Impacto: la comparación depende de esos valores. · Opciones: fijarlos ahora y versionar cada cambio; esperar proceso de empresa; ajustar después. · Propuesta: fijarlos como `reactive-v1` y `fixed7-v1`, documentar supuesto y no usarlos como medición empresarial.

> **⚠️ Contradicción detectada** — Hecho nuevo: el plan de pantalla existente habla de ruteo «VRP», geocercas PostGIS, telemetría y valores numéricos fijos sin fuentes reales. · Hecho establecido: [Simulador y metricas](../../boveda/04%20Datos%20y%20algoritmos/Simulador%20y%20metricas.md) exige resultados reproducibles y [Rutas y sobrecostos](../../boveda/04%20Datos%20y%20algoritmos/Rutas%20y%20sobrecostos.md) define una heurística de inserción/2-opt. · Impacto: la pantalla actual puede atribuir capacidades y ahorro no probados. · Opciones: conservarla como maqueta etiquetada; sustituirla por datos/API y lenguaje fiel al motor. · Propuesta: sustituirla en 005 y cerrar R-15 solo al verificar la interfaz.

> **⚠️ Contradicción detectada** — Hecho nuevo: el 005 añade una lectura paginada de rutas a 002 y de entregas a 004 para que el panel recupere el estado sin depender de IDs precargados. · Hecho establecido: los contratos originales de 002/004 solo exponían ruta individual y consulta móvil. · Impacto: sin estas lecturas, el panel requeriría datos fijos o consultas privadas. · Opciones: añadir las lecturas a sus specs productores; crear agregador propio; codificar IDs. · Propuesta: primeras lecturas, documentadas en los specs productores en este PR, con dueños 002 y 004 antes de H4.

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Hasta `1080 × regiones` planificaciones superan el plazo si cada una consume el límite de 002 | Alta | H4 sin resultados | Matriz precargada, perfilado del ciclo completo, trabajo asíncrono, corrida anticipada y objetivo ≤60 min; prioridad sobre el agente. |
| Baselines sintéticos irreales | Media | Impacto poco defendible | Reglas congeladas, recursos iguales, parámetros visibles y validación de sector durante evento. |
| `demo-region` contaminado por simulación | Media | Recorrido físico inconsistente | Contexto aislado y prueba de stock/entregas antes/después. |
| Mapas sin mosaicos en el recinto | Media | Menor claridad visual | Lista y leyenda de rutas con tramos/geometría de API, ensayo sin wifi. |

## Criterios de éxito

- CE-01: ningún número o escenario operativo fijo de `route-map.widget.ts`/dashboard aparece con API vacía; otro fixture cambia el mapa y los KPI sin recompilar.
- CE-02: 15 puntos y 2 vehículos de `demo-region` se muestran en vivo; los ≈80 nacionales solo en simulación; vial sólido y aproximación punteada.
- CE-03: columnas de decisiones, aprobación/rechazo/deshacer y «solo proponer» reflejan estados confirmados por 002; la traza de 003 abre por ID.
- CE-04: tres seeds base y una seed adversa completan comparación de 90 días con tres políticas; mismo calendario y recursos por seed/escenario; ningún tramo vial/estimado mezclado dentro de un run.
- CE-05: las fórmulas oficiales producen numerador, denominador, unidad y N/A; quiebres evitados y nuevos se muestran juntos, sin vender diferencia neta como «evitados».
- CE-06: exportación y pantalla coinciden por `comparisonId`, hashes, seeds y versiones; repetición da mismas métricas/eventos.
- CE-07: corrida completa con matriz local termina en ≤60 min en entorno de demo o queda registrado un bloqueo medido que debe resolverse antes de H4 (15-oct 11:00); una corrida verificada se inicia tan pronto esté integrado 002 y se conserva para el pitch.
- CE-08: reset/avance de la demo en vivo no altera el ledger de simulación ni viceversa; `advance` de 004 concilia stock, custodia y paradas.

## Matriz de bloqueos cruzados (005 consume / 005 produce)

| Spec productor | 005 consume (nombre exacto) | 005 produce o exige | Dueño y hito |
|---|---|---|---|
| 001 | `SimulationRun`, `SimulationMetric`, `POST /api/v1/imports`, `GET /api/v1/service-points`, `POST /api/v1/demo/reset`, roles fijos | Extensión de runs/eventos en una migración 005; generador canónico que usa importador aislado; no cambia el núcleo de tablas ni Schema95 | 001 antes de H2; 005 antes de H4 |
| 002 | `GET /api/v1/risks`, `GET /api/v1/route-plans`, `GET /api/v1/route-plans/{id}`, `GET /api/v1/agent-decisions`, `POST /api/v1/agent-decisions/{id}/approve`, `POST /api/v1/agent-decisions/{id}/reject`, `POST /api/v1/route-plans/{id}/revert`, `GET /api/v1/autonomy/mode`, `PUT /api/v1/autonomy/mode`, `POST /api/v1/demo/incidents`, planificador, costo y `facts` | Lectura `GET /route-plans` añadida explícitamente a 002 en este PR; simulador reutiliza servicios sin duplicarlos | 002 antes de H4; 005 antes de H4 |
| 003 | `POST /api/v1/agent/runs`, `GET /api/v1/agent/runs`, `GET /api/v1/agent/runs/{id}`, `AgentRun` y origen de explicación | Disparo y vista de traza; no implementa LLM ni flujo n8n | 003 antes de H4; panel puede mostrar plantillas de 002 si falla agente |
| 004 | Login PKCE web (FR-17/T1b), `GET /api/v1/deliveries`, `POST /api/v1/demo/route-plans/{id}/advance` y estados de entrega | Lectura `GET /deliveries` añadida explícitamente a 004 en este PR; sin duplicar auth ni usar `/stops/{sequence}/complete` provisional | 004 login y `advance` antes de H3; lectura antes de H4 |

**Secuencia de migraciones:** 001 crea el núcleo; cada spec posterior usa una migración sobre `develop` actualizado, terminada en `DemoGrants.Sql`, y se integra antes de que el siguiente genere la suya. El 005 puede integrar su migración tras 001/002 coordinando el turno con 003/004; el simulador no espera a la PWA ni al agente. La prueba de 001 comprueba núcleo y ausencia de Schema95, no conteo final cerrado. La comparación se perfila antes de H4: 1080 jornadas × 6 regiones de ejemplo (≤15 puntos cada una) × 1 s (cota para 15 puntos/2 vehículos en 002) = 108 min solo para planificación. La meta total ≤60 min requiere menos llamadas efectivas o menor tiempo medio; se verifica con benchmark, no se presume.

## Preguntas abiertas

- PA-01: ¿quiénes son los Deciders que aprueban baselines, demanda sintética y este spec? Equipo, antes de pasar de `borrador` a `aprobado`.
- PA-02: ¿qué consumo base, regiones/flota y matriz vial permitida se documentarán para el dataset nacional? Equipo de datos antes de generar la primera corrida; sin fuente permitida, ejecutar solo aproximación **como escenario separado y etiquetado**.
