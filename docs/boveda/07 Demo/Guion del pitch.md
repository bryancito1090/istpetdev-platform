---
tipo: demo
estado: propuesta
actualizado: 2026-10-16
tags: [demo, validacion, istpetdev, planificacion]
---

# Guion del pitch — T4 (alineado a diapositivas v3)

Versión para el Reto 1 de Hackathon Expo Clean 2026. Sigue los siete bloques del Anexo 5.7. El guion maestro dura **280 s (4:40)**; el margen restante del turno anunciado de cinco minutos es operativo. Los cortes de **160 s** y **400 s** son metas de ensayo para turnos de tres y siete minutos, respectivamente. Son tiempos planificados, no resultados de ensayo. No hay pasadas registradas en [registro de ensayos](../../specs/006-validacion-pitch-entrega/registro-ensayos.md).

## Norte y regla de evidencia

El problema elegido es la planificación diaria: cuándo atender con flota propia y cuándo evaluar un operador contratado, considerando el costo total de servir. La frase central es: **«compara rutas con flota propia y contratada, suma todo lo que cuesta servir… La IA explica; el planificador decide»**. La primera parte expresa el objetivo del módulo, no una capacidad integrada de punta a punta.

El panel A/B/C se presenta con la **lámina 5 de `pitch-v3`**, con cifras del motor en el **ensayo técnico local de #285** (PR #295, `1585ab7`; escenario sintético `demo-quito-planificacion`; `evidencia/ensayo-planificacion-local-20261016/02-suggest-pitch-abc.json`): **A** flota propia USD 57,89, 5 de 6 críticos a tiempo, puntaje 81,7; **B** propia con críticos primero USD 69,89, 6 de 6, puntaje 89,1; **C** operador contratado USD 66,90, 6 de 6, puntaje 90,8, **recomendada por el motor**. Se leen solo totales, críticos a tiempo, puntaje y recomendación: el desglose por componente de esa corrida no suma el total y queda fuera de la lámina hasta corregirse (#276). Decir «ensayo técnico» y «escenario sintético»; no decir que corre en producción hasta que #198 aplique la configuración. Las cifras de `demo-base` de #240 (USD 19,94) ya no se usan en esta lámina.

Separar siempre tres capas: (1) funciona y está verificado según la [matriz de afirmaciones](../../specs/006-validacion-pitch-entrega/matriz-afirmaciones.md); (2) diseño o integración en curso; (3) hoja de ruta, sujeta a validación. Los horarios, tráfico y restricciones del escenario son supuestos sintéticos; no representan tráfico en vivo ni certifican normativa municipal. No describir cronologías de construcción del prototipo.

## Guion maestro — 280 s

Presentación: `docs/specs/006-validacion-pitch-entrega/materiales/pitch-v3.pptx` (10 láminas, con animaciones). La columna **Lámina y clics** indica qué lámina está en pantalla y cuántos clics de animación tiene, además del que pasa a la siguiente; el detalle de cada animación está en «Diapositivas v3: clics y animaciones» del [registro de ensayos](../../specs/006-validacion-pitch-entrega/registro-ensayos.md).

**Chillo es solo visual:** nadie lee ni dice sus frases. Su globo aparece con el último clic de las láminas 1, 2, 5, 9 y 10; quien presenta hace una pausa breve (1 s) para que el jurado lo vea y sigue. Las frases de los globos están aprobadas por Joseph tal como figuran en el registro de ensayos.

| Bloque (Anexo 5.7) | Lámina y clics (v3) | Duración | Papel | Acción / recurso | Texto continuo para ensayar |
|---|---|---:|---|---|---|
| Apertura | **1 · Portada** · 1 clic (globo de Chillo) | 10 s | Narración | La portada entra sola. Al terminar el saludo, clic: aparece el globo de Chillo (no se lee); pausa breve. | «Buenas tardes. Somos IstpetDev, del Instituto Superior Tecnológico Traversari, y venimos a mostrar cómo planificar con el costo completo de servir.» |
| 1. Problema | **2 · ¿Flota propia o apoyo contratado?** · 2 clics | 25 s | Narración | Clic 1 al decir «comparar el servicio completo»; clic 2 al final: globo de Chillo (no se lee), pausa de 1 s. | «Cada día una empresa de limpieza decide qué atender con sus vehículos y cuándo conviene contratar apoyo. Si decide solo mirando kilómetros, deja fuera personas, combustible, descarga y producto que podría dañarse. Nuestra hipótesis: hay que comparar el servicio completo con datos que se puedan revisar.» |
| 2. Solución | **3 · El motor calcula. La persona decide.** · 4 clics | 30 s | Narración → Web | Un clic por paso: Comparar, Desglosar, Explicar, Decidir. Tras el cuarto aparece sola la frase final. | «La propuesta organiza esa decisión en cuatro pasos. Comparar rutas con flota propia y contratada. Desglosar todo lo que cuesta servir. Explicar: la IA comunica los datos calculados. Y decidir: el planificador elige. El motor calcula con reglas y datos; la IA explica; la persona decide. Hoy hay piezas verificadas y otras todavía en integración.» |
| 3. Prototipo / demo (a) | **4 · La pantalla de rutas, en una captura real** · 0 clics | 25 s | Web | Todo entra solo. Señalar la etiqueta «datos sintéticos»: es otro escenario sintético, no Quito ni el panel A/B/C. | «Esta es la pantalla de rutas del prototipo, en una captura real con un escenario sintético: paradas numeradas sobre calles reales, ventanas horarias y hora estimada de llegada. Para la demostración preparamos además `demo-quito-planificacion`, un escenario sintético de Quito con puntos, ventanas, flota, costos e incidentes definidos para el ejercicio. No son clientes ni operaciones medidas.» |
| 3. Prototipo / demo (b) | **5 · Panel A / B / C** · 3 clics | 65 s | Técnica → Negocio | Clic 1: filas Flota, Críticos a tiempo y Puntaje. Clic 2: costo total → marco dorado en C y recomendación del motor. Clic 3: globo de Chillo (no se lee), pausa de 1 s. Leer solo totales, críticos y puntaje; no leer desglose por componente. | «En el panel, A es la flota propia; B, la flota propia con los críticos primero; C, un operador contratado. Son cifras del motor en un ensayo técnico con nuestro escenario sintético de Quito. A es la más barata, 57,89 dólares, pero deja un crítico fuera de ventana. B cumple los seis críticos, pero cuesta 69,89. C también cumple los seis y cuesta 66,90. Por eso el motor recomienda C, con 90,8 puntos: pondera costo y críticos a tiempo por igual, más pedidos pendientes y riesgo. La más barata no siempre es la mejor. Son costos estimados, no mediciones de una empresa. La IA explica la recomendación; el planificador decide qué opción llevar a la operación.» |
| 4. Impacto | **6 · Indicadores que cualquier rol puede entender** · 0 clics | 25 s | Narración | Los seis indicadores entran solos en cascada. | «El impacto que buscamos es que la empresa anticipe faltantes y decida con más contexto entre su flota y un operador contratado. No afirmamos ahorros observados. En un piloto proponemos acordar una línea base y medir indicadores que cualquier rol entiende: entregas a tiempo, pendientes, kilómetros y costo estimado, separando lo medido de lo supuesto.» |
| 5. Viabilidad | **7 · Lo que podemos respaldar hoy** · 0 clics | 25 s | Técnica | Todo entra solo; terminar sobre la banda «Límite». | «La base viable es empezar con datos que la empresa pueda revisar y un escenario controlado. El motor ya tiene un ensayo sintético del desglose de costos, un ensayo técnico de la comparación propia y contratada y pruebas de franjas de hora pico configuradas. Falta llevar la comparación contratada a producción. Antes de usarlo en operación, validaríamos datos, restricciones y resultados con el equipo planificador.» |
| 6. Modelo de negocio | **8 · Una propuesta comercial por validar** · 0 clics | 20 s | Negocio | Todo entra solo. Sin precio. | «Proponemos atender a empresas de limpieza con una implementación e integración inicial y una suscripción. El cliente, el precio y los costos de operación siguen por validar; no presentamos ventas ni acuerdos como hechos. El piloto debe comprobar quién usa la herramienta y qué valor obtiene.» |
| 7. Riesgos y mitigación | **9 · RIESGOS Y ESTRATEGIAS DE MITIGACIÓN** · 1 clic | 35 s | Negocio → Narración | Los riesgos entran solos en cascada; al final, clic: globo de Chillo (no se lee), pausa de 1 s. | «RIESGOS Y ESTRATEGIAS DE MITIGACIÓN. Datos incompletos: validar la importación, mostrar supuestos y decir “sin dato” donde falte. Rutas poco realistas por ventanas, hora pico o restricciones: validar las reglas con el planificador y revisar antes de publicar. Costos mal estimados: mostrar el desglose y calibrarlo con viajes conciliados; la persona conserva la decisión. Adopción y dependencia de un operador: capacitación por rol, revisión del plan y alternativas de respaldo, a acordar en el piloto.» |
| 7. Próximo paso y cierre | **10 · Hoja de ruta: validar antes de prometer** · 2 clics | 20 s | Narración → Web | Clic 1: banda «Próximo paso». Clic 2, al terminar la última frase: globo de Chillo (no se lee); cerrar con «Gracias». | «La hoja de ruta: llevar a producción la comparación propia y contratada con su recomendación, validar tráfico y normativa de Quito, la guía de retorno, medir el error del pronóstico y calibrar costos con viajes reales. Proponemos empezar con una empresa, una línea base e indicadores acordados. Buscamos un equipo del sector para validarlo juntos. Gracias.» |

**Suma:** 10 + 25 + 30 + 25 + 65 + 25 + 25 + 20 + 35 + 20 = **280 s**, con **13 clics de animación y 9 avances de lámina**. Frente a T4 original: el Problema y la demo ceden 5 s cada uno para la portada; los 55 s de riesgos y cierre se reparten entre las láminas 9 y 10. Ensayar con clics: los tiempos de animación todavía no están medidos en una pasada real. La sección de riesgos da énfasis explícito a la valoración hecha en la charla de logística. Las mitigaciones descritas son diseño o propuesta; no se atribuyen todas a funciones implementadas.

## Cortes de ensayo

### Corte de 3 minutos — meta 160 s

| Bloque | Duración | Papel | Texto para ensayar |
|---|---:|---|---|
| Problema y solución | 30 s | Narración | «Cada día una empresa decide qué atender con su flota y cuándo contratar apoyo. Mirar solo kilómetros puede ocultar combustible, descarga y producto expuesto a merma. Nuestra propuesta compara rutas con flota propia y contratada y el costo total de servir. La IA explica; el planificador decide.» |
| Demo A/B/C | 55 s | Web → Técnica | «En nuestro escenario sintético de Quito, el motor compara tres opciones en un ensayo técnico: A, flota propia, 57,89 dólares pero un crítico fuera de ventana; B, propia con críticos primero, 69,89; C, operador contratado, 66,90 y los seis críticos a tiempo. El motor recomienda C, con 90,8 puntos. La más barata no siempre es la mejor. Son costos estimados, no mediciones de una empresa.» |
| Impacto y viabilidad | 25 s | Narración → Técnica | «Buscamos ayudar a anticipar faltantes y comparar opciones con contexto. El impacto se medirá en un piloto con línea base acordada; todavía no afirmamos ahorro observado. El motor cuenta con pruebas de factores de hora pico configurados, que son supuestos y no tráfico en vivo. La escena A/B/C requiere integración y validación.» |
| Riesgos y mitigación | 30 s | Negocio | «RIESGOS Y ESTRATEGIAS DE MITIGACIÓN: ante datos incompletos, validar y mostrar “sin dato”; ante rutas poco realistas, revisar ventanas, restricciones y plan antes de publicar; ante costos inciertos, enseñar el desglose y calibrarlo con viajes conciliados; ante baja adopción, capacitar por rol. Son medidas de diseño por validar en el piloto.» |
| Modelo y próximo paso | 20 s | Negocio → Narración | «Proponemos implementación e integración más suscripción para empresas de limpieza; cliente, precio y costos siguen por validar. El siguiente paso es llevar a producción la comparación propia/contratada, acordar una línea base y validar el flujo con un equipo del sector. La persona planificadora mantiene la decisión.» |

**Láminas (v3):** Problema y solución → 2 y 3 (mostrar los cuatro pasos sin pausas); Demo A/B/C → 5 (saltar la 4); Impacto y viabilidad → 6 y 7; Riesgos y mitigación → 9; Modelo y próximo paso → 8 y 10. La portada se muestra mientras el equipo se presenta, fuera del tiempo.

**Suma:** 30 + 55 + 25 + 30 + 20 = **160 s**. Este corte conserva los siete puntos del Anexo 5.7, agrupando problema/solución e impacto/viabilidad para ajustar el tiempo.

### Corte de 7 minutos — meta 400 s

| Bloque | Duración | Papel | Texto para ensayar |
|---|---:|---|---|
| 1. Problema | 45 s | Narración | «En una operación de limpieza, planificar un día implica decidir qué entregas caben en la flota disponible y cuáles podrían requerir apoyo externo. Si se compara solo distancia, quedan fuera componentes como combustible, desgaste, tiempo del conductor, ayudantes de descarga y merma esperada. Nuestra hipótesis es que esa decisión necesita una comparación que muestre sus supuestos y permita revisar cada componente. No afirmamos que todas las empresas operen igual; proponemos validarlo con el sector.» |
| 2. Solución | 45 s | Narración → Web | «La propuesta es un módulo de planificación que compara alternativas de ruta y el costo total estimado de servir. La frase central es: compara rutas con flota propia y contratada, suma todo lo que cuesta servir… La IA explica; el planificador decide. La IA recibe cifras calculadas para hacerlas comprensibles; la recomendación, cuando se integre, debe tener su justificación visible y quedar bajo control de la persona. La comparación contratada y el puntaje A/B/C ya se ensayaron en el motor con el escenario sintético.» |
| 3. Prototipo / demo | 130 s | Web → Técnica → Web | «Abrimos `demo-quito-planificacion`. Es un escenario sintético de Quito con referencias geográficas públicas y datos de operación construidos para el ejercicio; no contiene clientes ni viajes medidos. En el panel A/B/C, A representa flota propia; B, flota propia con los críticos primero; C, un operador contratado. En un ensayo técnico con este escenario, el motor calcula las tres: A cuesta 57,89 dólares pero deja un crítico fuera de ventana; B cumple los seis críticos y cuesta 69,89; C, el operador contratado, también cumple los seis y cuesta 66,90. El motor recomienda C, con 90,8 puntos, porque pondera costo y críticos a tiempo por igual, más pendientes y riesgo; su justificación queda visible. La más barata no siempre es la mejor. Son costos estimados, no mediciones de una empresa. El motor también tiene pruebas para factores de hora pico por franja configurada. Son supuestos de escenario; no equivalen a tráfico en vivo, y el recorrido en esta interfaz no está ensayado. La decisión final de operación corresponde al planificador. La IA explica los datos calculados y puede usar una explicación determinista de respaldo cuando falla el agente, según el registro técnico.» |
| 4. Impacto | 40 s | Narración | «El impacto esperado es mejorar la calidad de la decisión diaria: identificar entregas pendientes, entender el costo estimado y reconocer cuándo la flota propia podría no alcanzar. Eso es una meta, no un resultado medido. En el piloto proponemos comparar con una línea base acordada, registrar qué se atendió y qué quedó pendiente, y revisar kilómetros, puntualidad y costo estimado. Solo después de esa medición hablaríamos de resultados de la empresa.» |
| 5. Viabilidad | 40 s | Técnica | «La viabilidad empieza con los datos: puntos y ventanas, productos y cantidades, vehículos y costos que el equipo pueda revisar. El escenario de Quito sirve para mostrar decisiones con datos sintéticos. Un ensayo técnico de costo ya desglosa combustible según carga, desgaste, conductor, ayudantes y merma esperada; el resultado es estimado. La comparación del operador contratado no está integrada. También hay pruebas del motor sobre franjas de hora pico configuradas, pero no datos de tráfico en vivo. Antes de operar, habría que validar procedencia, restricciones, configuración e integración con la empresa.» |
| 6. Modelo de negocio | 35 s | Negocio | «La hipótesis comercial es atender a empresas de servicios de limpieza mediante implementación e integración inicial y una suscripción. No contamos aquí con precio validado, costos mensuales confirmados, ventas ni cliente comprometido. En una validación con el sector acordaríamos quién usaría el planificador, qué sistemas y archivos hay que integrar, qué indicadores importan y cuánto esfuerzo requiere mantener los datos. El precio se definiría con evidencia de costos y disposición de pago.» |
| 7. Próximo paso, riesgos y mitigación | 65 s | Negocio → Narración → Web | «RIESGOS Y ESTRATEGIAS DE MITIGACIÓN. Datos incompletos o inconsistentes: validar al importar, identificar responsables y mostrar “sin dato” en lugar de rellenar con una cifra inventada. Rutas poco realistas: revisar ventanas y capacidad con el planificador, validar las restricciones aplicables y revisar antes de publicar. Costos inexactos: hacer visible cada componente y calibrarlo con viajes conciliados; mientras no exista esa calibración, declararlo estimado. Error de la IA al explicar: basar la explicación en cifras calculadas, ofrecer texto determinista de respaldo y dejar la decisión a una persona. Resistencia al cambio: diseñar capacitación por rol y recoger observaciones de bodega y conductores. Incumplimiento de un operador: plantear trazabilidad y un operador de respaldo, sujeto a acuerdos y validación. La hoja de ruta separada de lo que funciona incluye integrar la alternativa contratada y la recomendación A/B/C; verificar tráfico y normativa de Quito con fuentes vigentes; crear guía de retorno y prioridad para entregas pendientes; evaluar pronóstico con estacionalidad midiendo su error; y calibrar costos con viajes reales. Proponemos un piloto con alcance e indicadores acordados con una empresa. Buscamos validar primero la decisión, los datos y los riesgos junto al sector.» |

**Láminas (v3):** 1. Problema → 1 y 2 (la portada al inicio del bloque); 2. Solución → 3; 3. Prototipo / demo → 4 y 5; 4. Impacto → 6; 5. Viabilidad → 7; 6. Modelo de negocio → 8; 7. Próximo paso, riesgos y mitigación → 9 y 10.

**Suma:** 45 + 45 + 130 + 40 + 40 + 35 + 65 = **400 s**. El registro de ensayos conserva solo pasadas realmente realizadas; estos cortes son guiones propuestos.

## Hechos, integración y hoja de ruta

| Estado | Contenido autorizado para el pitch |
|---|---|
| Hecho | Ensayo técnico sintético del motor para desglosar costo estimado de servir según carga, con fixture `demo-base`. Ensayo técnico local de la comparación A/B/C con operador contratado, puntaje y recomendación justificada sobre `demo-quito-planificacion` (#285); no acredita despliegue ni el desglose por componente de esa corrida. Pruebas del motor sobre franjas configuradas, sin tráfico en vivo. Escenario sintético `demo-quito-planificacion` y pruebas de importación documentadas; no afirmar ensayo en pantalla ni paquete íntegramente reproducido. |
| En curso | Configuración del escenario de Quito en producción (#198); desglose por componente que cuadre con el total de cada opción (#276); prueba visible de la ventana estrecha y captura de la pantalla A/B/C conectada. |
| Hoja de ruta | Tráfico en vivo y restricciones de circulación verificadas; guía de retorno y reinyección de pendientes; pronóstico con estacionalidad y medición del error; calibración del costo con viajes reales; marketplace y evaluación de operadores; capacitación por rol y piloto con empresa. Son propuestas a validar. |

## RIESGOS Y ESTRATEGIAS DE MITIGACIÓN

La charla de logística valoró especialmente este análisis. Presentar las mitigaciones como diseño propuesto cuando no sean capacidades verificadas.

| Riesgo | Detección temprana | Mitigación propuesta |
|---|---|---|
| Datos incompletos o inconsistentes | Filas rechazadas, campos faltantes, diferencias con conteos. | Validar la importación, asignar responsables de datos y mostrar «sin dato» o el supuesto explícito. |
| Resistencia al cambio | El equipo planifica fuera del sistema o evita registrar excepciones. | Capacitación práctica por rol, referentes por turno y revisión conjunta de las rutas. |
| Rutas poco realistas | Incumplimientos de ventanas, jornada o capacidad. | Verificar los datos y restricciones, mostrar motivos y pedir revisión del planificador antes de publicar. |
| Pronóstico impreciso | Faltantes que el pronóstico no anticipó. | Mantener una fórmula explicable, medir error con datos suficientes y ajustar solo después de evaluarlo. |
| Integración tecnológica difícil | Copia manual repetida o errores al importar. | Empezar con perfil de importación y acordar interfaces, responsables y alcance de piloto. |
| Costos mayores o mal estimados | Diferencias persistentes entre estimación y viaje conciliado. | Desglosar componentes, declarar incertidumbre y calibrar con datos reales antes de cotizar. |
| Hora pico y restricciones de Quito | Llegadas tardías o rutas incompatibles con el vehículo. | Validar franjas y normativa con fuentes oficiales vigentes; hoy no afirmar cumplimiento municipal ni tráfico en vivo. |
| Operador contratado incumple | Retrasos o entregas incompletas. | Definir acuerdos de servicio, trazabilidad y alternativa de respaldo antes de depender del operador. |
| Entregas no realizadas | Demanda pendiente al cierre. | Conservar el pendiente en la planificación; proponer guía de retorno y prioridad al día siguiente como hoja de ruta. |
| Error de explicación de IA | Justificación no coincide con cifras calculadas. | La IA explica, el cálculo permanece determinista, se ofrece explicación fija de respaldo y la persona decide. |

## Referencias de uso

- Fuente factual exclusiva: [matriz de afirmaciones](../../specs/006-validacion-pitch-entrega/matriz-afirmaciones.md).
- Diseño y riesgos: [[Planificacion potenciada - propuesta del evento]].
- Respuestas para jurado y control de ensayos: [registro de ensayos](../../specs/006-validacion-pitch-entrega/registro-ensayos.md).
- Estructura oficial: Anexo 5.7, según [[Evaluacion y entregables]].

---

## Material de apoyo para preguntas del jurado

- **Protocolos de adopción y mitigación de resistencia al cambio:** [[Protocolos de capacitacion por rol]]. Hojas de una sola página por puesto de trabajo con prácticas sobre el escenario de Quito:
  - [[Capacitacion - Planificador]]: Comparación de opciones A/B/C, ventanas horarias estrictas y relevo ante avería vial.
  - [[Capacitacion - Bodega]]: Preparación, doble verificación de lote/vencimiento, bultos pesados >25 kg y rótulo QR.
  - [[Capacitacion - Conductor]]: Descarga de ruta para trabajo sin red, escaneo de custodia y entrega.
  - [[Capacitacion - Receptor]]: Validación de bulto, rechazo con motivo y foto de evidencia en farmacia/cliente.
  - [[Capacitacion - Gerencia]]: Panel ejecutivo «Hoy», lectura de los 8 KPIs y comparación propia frente a contratada.
  - [[Capacitacion - Auditor]]: Trazabilidad forense de bultos (HU), inspección de firmas y fotos en modo solo lectura.
- **Indicadores en lenguaje sencillo:** [[Indicadores de operacion en lenguaje sencillo]], con los 6 indicadores de la charla técnica más costo por entrega y propia vs. contratada, explicando su pregunta sencilla, fórmula y origen en la app.

---

Notas relacionadas: [[Entregable Solucion y propuesta de valor]], [[Cifras del simulador 90 dias]], [[Checklist y contingencias]], [[Plus para ganar]], [[Plan de validacion]], [[Historia y propuesta de valor]], [[Modelo de negocio y piloto]], [[Protocolos de capacitacion por rol]], [[Indicadores de operacion en lenguaje sencillo]].

