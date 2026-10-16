---
tipo: analisis-producto
estado: propuesta
actualizado: 2026-10-16
tags: [producto, reto-1, planificacion, rutas, costos, riesgos, pitch]
---

# Planificación potenciada — propuesta del evento

**Origen:** ideas de Joseph del 16-oct-2026, tras la [[Charla de logistica integral - transcripcion recibida]] y su análisis en [[Logistica integral para el Reto 1 - hallazgos y evidencia]]. El código se revisó en `develop` (commit `db9284b`). Todo número de esta nota es **sintético o ilustrativo** salvo que diga otra cosa. Nada de lo que aquí se marca «propuesto» existe todavía en el software.

## El dolor que elegimos

Vamos a atacar **el módulo de planificación**. La charla dejó un concepto clave: una empresa puede **contratar el servicio de logística cuando su flota propia no alcanza o no conviene**. Esa decisión, **¿flota propia o contratada, y con qué ruta?**, se toma hoy a ojo. Nuestro módulo debe tomarla con números y explicarla en palabras simples.

> **Frase para el pitch:** «Para cada día de entregas, el sistema compara rutas con flota propia y contratada, suma todo lo que cuesta servir (kilómetros, combustible, personas que descargan y mermas), revisa horarios, tráfico y restricciones de Quito, y recomienda una opción con su justificación. El planificador decide.»

## Qué tenemos hoy y cuánto crece cada idea

Tamaño: **S** = horas, sin migración; **M** = un día, toca motor o pantalla; **L** = más de un día o necesita datos reales. Decisión: **Sábado** (entra antes del congelamiento de las 11:30), **Dataset** (se resuelve con datos, sin código), **Pitch** (se narra como hoja de ruta).

| # | Idea | Qué existe en `develop` | Qué falta | Tamaño | Decisión |
|---|---|---|---|---|---|
| 1 | Costo de la ruta propia con más datos | `LogisticsCostCalculator`: `km × CostPerKm + horas × CostPerHour` (0,45 USD/km y 6 USD/h por defecto) | Combustible separado, mano de obra por descarga y merma esperada | M | **Sábado 🔴** |
| 2 | Paquetes y operarios: 1, 2 o más personas para descargar | `ServiceMinutes` fijo por punto; peso y volumen por producto | Regla de operarios por peso o bulto y su costo de mano de obra | S | **Sábado**, dentro de la idea 1 |
| 3 | Sobrecarga sube el consumo de combustible | `VehicleMatcher` descarta si excede peso o volumen | Factor de consumo según el porcentaje de carga y aviso de riesgo de daño de empaque | S | **Sábado**, dentro de la idea 1 |
| 4 | Flota contratada como alternativa | No existe | Operador externo con tarifa (base + por km + por kg) comparado con la flota propia | M | **Sábado 🟡** |
| 5 | La IA compara rutas y elige con justificación | Opciones A/B en `SourcingService`, pero la A está marcada `IsRecommended: true` fija; DeepSeek explica en n8n (prompt v2) | Puntaje calculado (costo, tiempo, riesgo, críticos) y justificación con las cifras | S–M | **Sábado 🟡** |
| 6 | Horas pico de Quito | Velocidad de respaldo fija de 40 km/h; OSRM sin tráfico | Factor de velocidad por franja horaria | M | **Sábado 🟢** si sobra tiempo; si no, **Pitch** |
| 7 | Restricción horaria para camiones | No existe | Tipo de vehículo y franjas prohibidas; nuevo motivo de descarte `restriccion_horaria` | M | **Pitch** (con el dataset preparado) |
| 8 | Ventanas de centros comerciales | **Ya existe:** `WindowStartLocal` y `WindowEndLocal` por punto; el secuenciador las respeta | Solo datos | — | **Dataset** (#238) |
| 9 | Accidente, tráfico o manifestación | **Ya existe:** incidente `BloqueoVial` con radio, replanificación con modos `Strict`, `LateService` y `PrioritizeCritical` | Tipos «Manifestación» y «Congestión» y un retraso parcial (hoy solo bloqueo total) | S datos / M motor | **Dataset** ahora; retraso parcial al **Pitch** |
| 10 | Entregas no realizadas en el día: guía o ruta de retorno | Estado `NotDelivered` en campo y demanda `Unserved` en el plan; #199 conserva la demanda pendiente | Guía de retorno de bultos a la bodega y reinyección con prioridad al día siguiente | M | **Pitch**, salvo la parte que ya cubre #199 |
| 11 | Merma en el transporte: ¿conviene la ruta? | Rechazos por `Daño` en recepción (Novedades); #236 agrega error de rotulado | Merma esperada por ruta en USD; los productos no tienen valor unitario | M | **Dataset** (valor unitario sintético) + idea 1 |
| 12 | Costo por km del vehículo asignado | **Ya existe:** `CostPerKm` por vehículo | Desglosarlo en combustible + desgaste | S | Dentro de la idea 1 |
| 13 | Tipos de vehículo: urbano o extraurbano, capacidades | Peso, volumen y costo por vehículo | Tipo y uso (urbano o extraurbano) | S config / M entidad | **Dataset** + config; entidad al **Pitch** |
| 14 | Precio del combustible variable en Ecuador | No existe | Parámetro fechado con fuente: precio por galón y fecha de vigencia | S | **Sábado**, dentro de la idea 1 |
| 15 | Reabastecimiento predictivo | **Ya existe:** media móvil de 14 días, cobertura frente a lead time + margen, riesgo por punto | Estacionalidad y medición del error (MAE) con más historia | L | **Pitch** (hoja de ruta) |
| 16 | Inventario en tiempo real con sincronización | **Ya existe:** movimientos por cada despacho y recepción; PWA offline con bandeja de envío (bug abierto en #230) | Cerrar #230 | — | Ya en curso |
| 17 | Indicadores en lenguaje sencillo | #235: tarjeta con los 6 indicadores de la charla | Textos para cada rol | S | **Sábado**, dentro de #235 |
| 18 | Protocolos de capacitación por rol | No aplica a código | Guía por rol | — | **Pitch** |
| 19 | Riesgos y mitigación | Ver la sección de riesgos | — | — | **Pitch**, con énfasis |

**Veredicto de tamaño:** el módulo **crece poco en código y mucho en narrativa**. Cuatro piezas caben antes del congelamiento (1–3, 12 y 14 como un solo cambio; 4; 5; y el dataset). El resto ya existe y solo hay que mostrarlo (8, 9, 16), o se presenta como hoja de ruta con datos preparados (6, 7, 10, 13, 15). No conviene tocar la entidad `Vehicle` ni hacer migraciones antes del sábado: todo lo nuevo entra como **configuración** (`appsettings` o un JSON del dataset).

## Cómo se calcula el costo total de servir una ruta

Propuesta para la idea 1. Las fórmulas son simples a propósito, para que cualquiera de la empresa pueda revisarlas.

| Componente | Fórmula propuesta | Dato de entrada (sintético) |
|---|---|---|
| Combustible | `km × (consumo L/100 km ÷ 100) × (1 + k × % de carga) × precio por litro` | Consumo por tipo de vehículo; `k = 0,3`; precio con fecha y fuente |
| Desgaste | `km × desgaste por km` | Por tipo de vehículo |
| Conductor | `horas de ruta × costo por hora` | `CostPerHour` actual |
| Ayudantes de descarga | `n.º de ayudantes × horas de ruta × costo por hora del ayudante` | Regla de operarios (abajo) |
| Merma esperada | `valor de la carga × % de merma esperado` (sube si la carga pasa del 90 %) | Valor unitario y % de merma sintéticos |
| **Total** | Suma de lo anterior, mostrada desglosada | — |

**Regla de operarios (sintética, ajustable):**

| Carga en la parada | Personas | Tiempo de descarga |
|---|---|---|
| Hasta 150 kg y ningún bulto de más de 25 kg | Solo el conductor | 10 min + 1 min por bulto |
| De 150 a 400 kg | Conductor + 1 ayudante | 10 min + 1 min por bulto ÷ 2 |
| Más de 400 kg, o algún bulto de más de 25 kg | Conductor + 2 ayudantes | 10 min + 1 min por bulto ÷ 3 |

La ruta lleva el máximo de ayudantes que pida cualquiera de sus paradas: si una parada necesita dos, la cuadrilla viaja completa.

**Flota contratada (idea 4):** se modela como un operador externo con `tarifa base + tarifa por km + tarifa por kg`, sin límite de vehículos y con un tiempo de respuesta fijo (por ejemplo, 4 h). El sistema calcula el costo de la misma ruta con flota propia y contratada. Si la flota propia no alcanza, muestra cuánto cuesta cubrir el faltante con el operador frente a dejar la demanda pendiente.

**Recomendación con justificación (idea 5):** cada opción recibe un puntaje con pesos visibles: costo total, entregas críticas a tiempo, demanda que queda pendiente y riesgo de la ruta (incidentes, hora pico, carga mayor al 90 %). La IA (DeepSeek en n8n) **no calcula**: recibe las cifras y las explica. La explicación debe citar las cifras, por ejemplo: «Se recomienda B: cuesta USD 12 más que A, pero entrega a tiempo los 3 hospitales; A deja 1 hospital fuera de su ventana». Si DeepSeek no responde, se usa la explicación determinista que ya existe.

## Panel de planificación para el pitch

Lo que debe verse en la demo, en una sola pantalla del Planificador:

```
┌ Planificación · viernes 17-oct · Bodega Sur ─────────────────────────────┐
│ Riesgo: 6 puntos en riesgo (2 críticos) · Combustible: USD x,xx/gal (fecha)│
├──────────────┬─────────────┬─────────────┬──────────────────────────────┤
│              │ A · Propia   │ B · Propia   │ C · Contratada               │
│              │ más barata   │ críticos 1ro │ (operador externo)           │
├──────────────┼─────────────┼─────────────┼──────────────────────────────┤
│ Km           │ 48           │ 55           │ 48                           │
│ Combustible  │ $ ...        │ $ ...        │ incluido en la tarifa        │
│ Personas     │ 1 + 1 ayud.  │ 1 + 1 ayud.  │ del operador                 │
│ Merma esper. │ $ ...        │ $ ...        │ $ ...                        │
│ Costo total  │ $ ...        │ $ ...        │ $ ...                        │
│ Críticos OK  │ 2 de 3       │ 3 de 3       │ 3 de 3                       │
│ Pendientes   │ 1 pedido     │ 0            │ 0                            │
│ Riesgos      │ hora pico    │ —            │ respuesta en 4 h             │
├──────────────┴─────────────┴─────────────┴──────────────────────────────┤
│ ★ Recomendada: B — «Cuesta $12 más que A, pero cumple los 3 hospitales…»│
│ [Publicar B]  [Elegir otra]  [Ver en mapa]                              │
└─────────────────────────────────────────────────────────────────────────┘
```

Los valores `...` se llenan con la corrida del dataset propio, nunca a mano. Si una celda no tiene dato, dice «sin dato».

## Dataset propio del equipo

La organización **no entregará un dataset**: lo generamos nosotros y lo documentamos. Eso nos da libertad para crear y alimentar los datos que hagan visible cada decisión del módulo, siempre que se presenten como **sintéticos** y sean reproducibles. El detalle de campos está en [[Dataset y escenarios]].

Fixture propuesto: **`demo-quito-planificacion`**, construido sobre `demo-region-caos` (que ya tiene puntos con ventanas, vehículos, tramos e incidente `BloqueoVial`). Agrega:

| Archivo | Contenido sintético | Para qué idea |
|---|---|---|
| `puntos.csv` | Un centro comercial con ventana 07:00–09:30, hospitales con ventanas de mañana y puntos en el centro histórico | 8, 9 |
| `vehiculos.csv` | Camioneta urbana (≈800 kg), camión liviano (≈2 t) y camión extraurbano (≈5 t), con `cost_per_km` distinto | 12, 13 |
| `perfiles-vehiculo.json` | Tipo, uso urbano o extraurbano, consumo L/100 km, desgaste por km y franjas restringidas | 1, 3, 7, 13 |
| `costos.json` | Precio del combustible con fecha y fuente («sintético»), costo por hora del ayudante, % de merma esperado y regla de operarios | 1, 2, 11, 14 |
| `operador-externo.json` | Tarifa base, por km y por kg, y tiempo de respuesta | 4 |
| `franjas-trafico.json` | Horas pico con factor de velocidad (p. ej. 06:30–09:30 y 16:00–19:30, factor 0,5) | 6 |
| `incidentes.json` | Bloqueo (ya existe), manifestación en el centro histórico y accidente en una vía rápida, cada uno con `isSynthetic: true` | 9 |
| `productos.csv` | Valor unitario sintético y peso por bulto (para la regla de operarios) | 2, 11 |

Las franjas, restricciones y precios de este fixture **no son normativa vigente de Quito** ni precios oficiales. Antes de citarlos en el pitch como reales hay que verificar la fuente (ordenanza municipal o publicación oficial del precio del combustible) y anotarla en [[Hechos canonicos]].

## Indicadores en lenguaje de toda la empresa

Complementa #235 (los seis indicadores de la charla). Cada indicador responde una pregunta que cualquier persona de la empresa entiende.

| Indicador | Pregunta sencilla | Quién la usa | Cómo se calcula |
|---|---|---|---|
| Costo por km | ¿Cuánto nos cuesta cada kilómetro que recorre el camión? | Gerencia, Planificador | Costo total de la ruta ÷ km |
| Costo por entrega | ¿Cuánto nos cuesta llevar un pedido? | Gerencia | Costo total ÷ entregas completadas |
| Faltantes | ¿Cuántos clientes se quedaron sin producto? | Todos | Puntos con stock en cero o pedidos no cubiertos en el período |
| Entregas a tiempo | ¿Llegamos cuando el cliente nos esperaba? | Planificador, Conductor | Entregas dentro de la ventana ÷ entregas |
| Entregas completas | ¿Llegó todo lo que se pidió? | Bodega, Receptor | Líneas recibidas sin rechazo ÷ líneas despachadas |
| Mermas | ¿Cuánto producto se dañó o se perdió en el camino? | Bodega, Gerencia | Cantidad y valor rechazados por daño o rotulado |
| Exactitud de inventario | ¿Lo que dice el sistema es lo que hay en la percha? | Bodega, Auditor | Conteos sin diferencia ÷ conteos |
| Tiempo de ruta | ¿Cuánto tarda el recorrido completo? | Planificador, Conductor | Hora de regreso − hora de salida |
| Propia frente a contratada | ¿Nos convino usar nuestro camión o contratar? | Gerencia | Costo propio − costo contratado para las mismas entregas |

## Protocolos de capacitación por rol

Para el pitch: la herramienta no basta si la operación no la adopta. La charla nombra la resistencia al cambio como riesgo; la respuesta es capacitar por rol, en poco tiempo y con práctica.

| Rol | Qué aprende | Formato propuesto | Cómo se comprueba |
|---|---|---|---|
| Planificador | Leer el riesgo, comparar opciones A/B/C y publicar o elegir otra | 2 h con el dataset de práctica | Planifica un día del dataset y justifica la opción elegida |
| Bodega | Preparar bultos, sellar, registrar conteos y diferencias | 1 h en la bodega | Prepara una ruta sin errores de rótulo |
| Conductor | Escanear QR, registrar entrega o no entrega y trabajar sin conexión | 30 min con el celular | Completa una ruta de prueba en modo avión |
| Receptor | Recibir, rechazar con motivo y firmar | 20 min | Registra una recepción con un rechazo |
| Gerencia | Leer los indicadores y la comparación propia frente a contratada | 30 min | Explica el indicador de la semana con sus palabras |
| Auditor | Seguir la trazabilidad de un bulto de punta a punta | 30 min | Reconstruye la historia de un bulto |

Cada protocolo incluye una hoja de una página por rol y un responsable de dudas («referente») en cada turno.

## Riesgos y estrategias de mitigación

Este es el punto que más valoró la charla (lámina «Riesgos y estrategias de mitigación»). Las seis primeras filas son las de la lámina; el resto son riesgos propios del módulo de planificación. Para cada uno se indica cómo lo detectamos a tiempo y cómo lo mitigamos en el producto y en el proceso.

### Riesgos de la lámina

| Riesgo | Señal temprana | Mitigación en el producto | Mitigación en el proceso |
|---|---|---|---|
| Datos incompletos o inconsistentes | El importador reporta filas rechazadas; puntos sin ventana o productos sin peso | Validación al importar (ya existe), valores por defecto visibles y marca «sin dato» en vez de inventar | Catálogo estandarizado, responsable de cada tabla y conteos físicos de validación por muestreo |
| Resistencia al cambio | Rutas planificadas fuera del sistema; uso bajo de la app en campo | Pantallas simples por rol, modo sin conexión, el planificador siempre puede elegir otra opción | Capacitación por rol (sección anterior), referentes por turno y metas comunicadas en cascada hasta quien estiba |
| Rutas poco realistas | Rutas que no se cumplen; muchas entregas fuera de ventana | Validar capacidad, ventanas, jornada, horas pico y restricciones antes de publicar; motivos de descarte visibles | Revisión del planificador antes de publicar y retroalimentación del conductor sobre cada ruta |
| Pronósticos imprecisos | Faltantes en puntos que el sistema no marcó en riesgo | Fórmula simple y explicable (media móvil, cobertura, lead time) con margen de seguridad | Empezar con modelos de referencia, medir el error (MAE) cada mes y ajustar los parámetros |
| Falta de integración tecnológica | Datos copiados a mano entre sistemas | Importador por archivos con plantilla y API documentada | Integración gradual: definir interfaces, responsables de datos y una operación piloto primero |
| Costos de implementación mayores a lo previsto | El piloto se alarga sin resultados medibles | MVP enfocado en planificación, con indicadores desde el primer día | Priorizar funciones que muestren beneficios medibles; piloto de 30 días con línea base |

### Riesgos propios de la planificación

| Riesgo | Señal temprana | Mitigación en el producto | Mitigación en el proceso |
|---|---|---|---|
| Hora pico y tráfico de Quito retrasan la ruta | Llegadas tardías concentradas en ciertas franjas | Factor de velocidad por franja y salida temprana sugerida; ventanas respetadas por el secuenciador | Programar salidas fuera de la hora pico; registrar los tiempos reales para recalibrar |
| Restricción de circulación para camiones | Multas o desvíos; rutas rechazadas por el conductor | Tipo de vehículo con franjas restringidas; elegir un vehículo urbano en esas franjas | Mantener actualizada la normativa vigente con fuente y fecha |
| Manifestaciones o cierres viales | Alertas de noticias o del conductor | Incidente con radio, replanificación con modos (estricto, servicio tardío, críticos primero) y aprobación humana | Protocolo de contingencia: quién aprueba, rutas alternas conocidas y aviso al cliente |
| Accidente del vehículo o del personal | El conductor reporta el incidente desde la app | Reasignar entregas a otro vehículo o al operador contratado; las críticas primero | Seguro, contacto de emergencia y procedimiento de traspaso de carga |
| Combustible con precio variable | El costo por km sube sin cambiar la ruta | Precio del combustible como parámetro fechado; el costo se recalcula y se ve el impacto | Revisar el precio cada mes; negociar con operadores sobre un costo promedio, como relató la charla |
| Sobrecarga del vehículo | Carga mayor al 90 %; rechazos por daño de empaque | Aviso de riesgo y merma esperada mayor; descarte si excede la capacidad (ya existe) | Normas de estiba y peso máximo por bulto |
| Flota propia insuficiente | Demanda pendiente al cerrar el plan | Opción con operador contratado y su costo comparado | Contratos marco con operadores y tiempos de respuesta acordados |
| Operador contratado incumple | Retrasos o entregas incompletas del tercero | Trazabilidad con QR también para la carga contratada | Calificar a los operadores por cumplimiento; tener un operador de respaldo |
| Entregas no realizadas | Bultos que vuelven a la bodega | Demanda pendiente conservada (#199) y prioridad al día siguiente | Guía de retorno firmada y conciliación del inventario al cierre del día |
| Mermas y errores de rotulado | Rechazos por daño o rotulado en recepción | Motivos de rechazo (#236), merma en USD por ruta y trazabilidad del bulto | Revisión de empaque y rótulo antes de cargar; lista de verificación en bodega |
| Inventario desfasado por falta de conexión | Recepciones pendientes de sincronizar | Captura sin conexión con sincronización posterior y aviso de datos atrasados | Cierre diario: no se planifica con datos de más de N horas sin revisar |
| La IA explica mal o se equivoca | La justificación no coincide con las cifras | La IA solo explica: el cálculo es determinista; respaldo con explicación fija si falla | El planificador aprueba toda publicación; se auditan las decisiones del agente |
| Personal insuficiente para descargar | Paradas con tiempos de descarga mucho mayores al plan | Regla de operarios por peso y bulto con su costo | Asignar ayudantes al planificar, no en el camino |

### Riesgos del día de la demo

| Riesgo | Mitigación |
|---|---|
| Falla la conexión del recinto | Datos móviles probados (#198) y video de respaldo (#200) |
| Falla un servicio externo (OSRM o DeepSeek) | Distancia aproximada y explicación fija, ya implementadas |
| La demo muestra cifras inconsistentes | Usar solo el dataset propio ya ensayado; las celdas sin dato dicen «sin dato» |

## Hoja de ruta para mencionar en el pitch

Ideas valiosas que **no** se implementan antes del sábado y se presentan como el plan del proyecto:

1. **Pronóstico con estacionalidad**: con más historia, pasar de la media móvil a modelos que reconozcan temporadas y días de la semana, midiendo siempre el error.
2. **Tráfico en vivo y restricciones municipales** conectadas a fuentes oficiales.
3. **Guía de retorno digital** para lo no entregado, con reinyección automática al plan del día siguiente.
4. **Tipos de vehículo en el catálogo** (urbano, extraurbano, refrigerado) y flota eléctrica, que la charla mencionó como plan de una operadora.
5. **Marketplace de operadores contratados**: cotizar con varios operadores y calificar su cumplimiento.
6. **Calibrar el costo con viajes reales** y su margen de error (#204).

## Reparto aprobado — issues del 16-oct

Joseph aprobó el reparto con carga equilibrada. Cada issue trae su lugar en la cola de su responsable y una sección «Para el pitch»; la guía para el pitch quedó como comentario en #200. La migración de base de datos está permitida y se marca con la etiqueta `🗄️ migración-db`.

| Persona | Cola (en orden) |
|---|---|
| Bryan | #230 🔴 → #240 🔴 costo total de servir (🗄️ migración: `Product.UnitValue`, `UnitsPerPackage`, `Vehicle.VehicleType`, `UsageScope`) → #241 🟡 operador contratado y recomendación → #247 🟢 guía de retorno |
| Anthony | #199 → #236 🟡 → #237 🟡 → #245 🟢 horas pico → #246 🟢 congestión y manifestación |
| Alexander | #210 🟡 → #242 🟡 pantalla A/B/C → #231 🟢 → #167 🟢 → #249 🟢 hojas de capacitación e indicadores |
| Jorge | #198 🔴 → #235 🔴 → #244 🟢 restricción horaria por tipo de vehículo → #166 🟢 |
| Joseph | #200 🔴 → #243 🔴 dataset `demo-quito-planificacion` (absorbe #238) → #248 🟢 error del pronóstico → #204 🟢 |

Dependencias: #243 publica primero los valores de configuración; #241, #244 y #237 usan el desglose de #240; #242 consume el contrato de #241; #245 y #246 comparten `RouteFeasibility.cs` y van en secuencia.

Relacionado: [[Rutas y sobrecostos]], [[Inventario y prediccion]], [[Simulador y metricas]], [[Guion del pitch]], [[Plan de validacion]] y [[Ampliacion de alcance - ideas del evento]].
