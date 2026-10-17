# 006 — Registro de ensayos y respuestas (T6)

**Banco actualizado para T4. No se registran pasadas nuevas: las tablas de ensayo siguen vacías y los tiempos del [guion T4](../../boveda/07%20Demo/Guion%20del%20pitch.md) son metas planificadas, no mediciones. [Tareas](task.md) · [E2E-03/04/06](test-e2e.md) · [Escena y contingencias](escena-y-contingencias.md).

## Reglas

- Una fila por pasada real, escrita por quien cronometra, incluso si falla. Esta IA no completa filas.
- **Duración medida:** de la primera palabra al cierre, con cronómetro. Meta: tiempo oficial menos 20 s; las metas internas actuales son 160, 280 y 400 s.
- **Pasada completa sin fallo:** siete bloques en orden, cinco papeles hablan, ninguna frase fuera del guion y duración dentro de la meta.
- **Racha:** suma 1 por pasada completa sin fallo; cualquier fallo la deja en 0. Un cambio de versión obliga a ensayar de nuevo esa versión.
- CE-04 se cierra solo con diez pasadas consecutivas del corte oficial y las dos últimas sin wifi del recinto.
- Papeles: anotar quién ocupó cada uno (N/W/M/T/G = narración, web, móvil, técnica, negocio).

## Pasadas del pitch

| # | Fecha/hora | Corte | Duración medida (s) | Meta (s) | N / W / M / T / G | Versión guion/diapositivas | Commit | Conexión | Fallos / contingencias usadas | Enlace (video interno o nota) | Racha |
|---:|---|---|---:|---:|---|---|---|---|---|---|---:|
| — | — | — | — | — | — | — | — | — | — | — | 0 |

**Racha actual: 0 / 10.** Pasadas sin wifi del recinto al final de la racha: 0 / 2.

## Ensayos funcionales de la demo

| Fecha/hora | Commit | Entorno (nube/local) | Dispositivos | Partes recorridas conectadas | Contingencias y resultado | Resultado | Enlace | Quién |
|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — |

No hay ensayo de escena registrado. Las cifras A/B/C de la lámina 5 salen del ensayo técnico local de #285 (`demo-quito-planificacion`); no anotarlas como recorrido en pantalla ni como producción.

## Ronda de preguntas

Se cronometra aparte del pitch. Una persona lee la pregunta y quien responde no la conoce de antemano. Límite: 30 s por respuesta. La preparación escrita no cuenta como pasada de ensayo; no se agregan resultados ni tiempos sin una sesión real.

| Ronda | Fecha/hora | Versión del banco (commit) |
|---|---|---|
| — | — | — |

| # | Pregunta | Quién respondió | Tiempo medido (s) | ≤30 s | Afirmó algo sin evidencia (ID) | Ajuste |
|---:|---|---|---:|---:|---|---|
| 1 | ¿La IA predice el consumo? | — | — | — | — | — |
| 2 | ¿La IA aprueba rutas sola? | — | — | — | — | — |
| 3 | ¿Qué ocurre si DeepSeek cae? | — | — | — | — | — |
| 4 | ¿Es la ruta óptima? | — | — | — | — | — |
| 5 | ¿Qué datos requiere? | — | — | — | — | — |
| 6 | ¿Qué pasa si nos dan otro Excel? | — | — | — | — | — |
| 7 | ¿Cómo conoce el stock? | — | — | — | — | — |
| 8 | ¿Qué pasa si no hay señal? | — | — | — | — | — |
| 9 | ¿Se duplica una recepción? | — | — | — | — | — |
| 10 | ¿Puede cualquiera escanear la caja? | — | — | — | — | — |
| 11 | ¿Funciona con 80 puntos o a escala nacional? | — | — | — | — | — |
| 12 | ¿Qué prueba la simulación? | — | — | — | — | — |
| 13 | ¿Por qué esos baselines? | — | — | — | — | — |
| 14 | ¿Qué significan quiebres evitados y nuevos? | — | — | — | — | — |
| 15 | ¿Por qué tres seeds y una adversa? | — | — | — | — | — |
| 16 | ¿Cuánto ahorra una empresa real? | — | — | — | — | — |
| 17 | ¿Cuánto cuesta y cuál es el precio? | — | — | — | — | — |
| 18 | ¿Evita multas hospitalarias? | — | — | — | — | — |
| 19 | ¿Qué validaron con el sector? | — | — | — | — | — |
| 20 | ¿Qué parte está implementada y qué queda como hoja de ruta? | — | — | — | — | — |
| 21 | ¿A quién atienden primero si no alcanza el camión? | — | — | — | — | — |
| 22 | ¿Qué pasa ante un cierre vial? | — | — | — | — | — |
| 23 | ¿Por qué no Dijkstra? | — | — | — | — | — |
| 24 | ¿Qué pasa si nos dan otro Excel? | — | — | — | — | — |
| 25 | ¿La flota es propia o contratada? | — | — | — | — | — |
| 26 | ¿Cómo se calcula el costo total de servir? | — | — | — | — | — |
| 27 | ¿Por qué la IA no decide? | — | — | — | — | — |
| 28 | ¿De dónde salen los datos del escenario? | — | — | — | — | — |
| 29 | ¿Cómo considera las horas pico y restricciones de Quito? | — | — | — | — | — |
| 30 | ¿Qué pasa con lo que no se entrega? | — | — | — | — | — |

## Banco de respuestas de hasta 30 segundos

Las respuestas se revisaron contra la [matriz de afirmaciones](matriz-afirmaciones.md). Ninguna respuesta autoriza afirmar que la escena fue ensayada. Los límites técnicos se expresan como tales. El guion no presenta cronologías de construcción del prototipo.

| # | Pregunta | Soporte / matriz | Palabras | Respuesta ensayada |
|---:|---|---|---:|---|
| 1 | ¿La IA predice el consumo? | A-11, P-14 | 32 | No presentamos a la IA como pronosticador. El agente explica una propuesta con base en datos calculados. Incorporar estacionalidad y medir el error del pronóstico es parte de la hoja de ruta. |
| 2 | ¿La IA aprueba rutas sola? | A-10, A-11 | 29 | La IA explica; el planificador decide. Las reglas de autonomía pueden dejar una decisión pendiente para aprobación humana. La matriz no acredita una escena concreta de aprobación en pantalla. |
| 3 | ¿Qué ocurre si DeepSeek cae? | A-12 | 25 | Hay un fallo técnico registrado con explicación determinista de respaldo. Esa evidencia acredita el caso ensayado, no disponibilidad absoluta ni un tiempo de respuesta garantizado. |
| 4 | ¿Es la ruta óptima? | A-07, A-33 | 22 | No afirmamos que sea óptima globalmente. El motor propone rutas sujetas a capacidad, ventanas y jornada; su alcance es una propuesta factible. |
| 5 | ¿Qué datos requiere? | A-04, A-07, A-22 | 32 | Para este escenario usamos inventario, consumo, puntos y datos de planificación. El importador permite mapear formatos y revisar errores antes de aplicar. La integración concreta depende de los datos de cada empresa. |
| 6 | ¿Qué pasa si nos dan otro Excel? | A-22, A-23 | 30 | El importador admite perfiles para mapear formatos y revisar errores antes de aplicar. El tiempo de adaptación con archivos de una empresa aún debe medirse; no prometemos un plazo fijo. |
| 7 | ¿Cómo conoce el stock? | A-04, A-06 | 32 | En el escenario, el motor calcula cobertura y riesgo a partir del inventario y el consumo. La frescura y calidad de los datos de una operación real deben validarse con la empresa. |
| 8 | ¿Qué pasa si no hay señal? | A-15 | 30 | Bodega y conductor tienen captura y cola local; falta ensayar el recorrido en un teléfono sin señal. La recepción final hospitalaria requiere conexión, así que no la ofrecemos como offline. |
| 9 | ¿Se duplica una recepción? | A-13, A-14 | 30 | Las pruebas de API cubren la cadena de custodia y evitan duplicar el recibo al repetir una operación. Esto es evidencia de pruebas, no de un ensayo físico en teléfono. |
| 10 | ¿Puede cualquiera escanear la caja? | A-16 | 28 | La API de consulta pública limita la información visible y rechaza tokens inválidos. No invitamos a escanear desde teléfonos externos sin haber ensayado el acceso en el recinto. |
| 11 | ¿Funciona con 80 puntos o a escala nacional? | A-03, A-24, A-25 | 30 | El escenario de Quito contiene 40 puntos sintéticos, 12 con referencias geográficas públicas. Eso describe el dataset, no demuestra rendimiento nacional; la escala debe validarse en el despliegue del piloto. |
| 12 | ¿Qué prueba la simulación? | A-19, A-21 | 33 | Hay un simulador sintético, pero la corrida disponible no valida una cifra de ahorro nacional ni ahorro real. Para hablar del impacto de una empresa necesitamos una comparación trazable y luego un piloto. |
| 13 | ¿Por qué esos baselines? | A-19, A-21 | 32 | Son políticas definidas para una comparación sintética. La auditoría detectó límites en la generación de resultados, por eso no las usamos para afirmar ahorro observado ni para generalizar sobre todas las empresas. |
| 14 | ¿Qué significan quiebres evitados y nuevos? | A-20, A-21 | 31 | Se reportan por separado los quiebres evitados y los nuevos para no ocultar diferencias en una resta neta. La corrida existente tiene límites documentados y no la presentamos como ahorro real. |
| 15 | ¿Por qué tres seeds y una adversa? | A-19, A-21 | 29 | El diseño contempla semillas base y una condición adversa para explorar variación. La auditoría encontró límites en la corrida; no usamos ese diseño como prueba de un resultado empresarial. |
| 16 | ¿Cuánto ahorra una empresa real? | A-21, A-26, A-33 | 27 | No tenemos una medición de ahorro en una empresa. El impacto se medirá con una comparación trazable y un piloto; no damos un porcentaje ni un monto. |
| 17 | ¿Cuánto cuesta y cuál es el precio? | A-26, A-30, A-31 | 28 | Proponemos implementación e integración más suscripción. El costo de operación y el precio siguen por validar con costos reales y disposición de pago; no presentamos una tarifa fija. |
| 18 | ¿Evita multas hospitalarias? | A-01, A-33 | 21 | No afirmamos que evite multas. La propuesta busca reducir el riesgo de faltantes; cualquier afirmación contractual requiere contrastar la cláusula aplicable. |
| 19 | ¿Qué validaron con el sector? | A-02, A-29 | 22 | La propuesta se está contrastando con el sector. No atribuimos entrevistas, acuerdos ni confirmación general del problema sin registros que lo acrediten. |
| 20 | ¿Qué parte está implementada y qué queda como hoja de ruta? | P-02, P-03, P-11 | 27 | El motor ya compara flota propia y operador contratado y recomienda con puntaje en un ensayo técnico local sobre el escenario sintético de Quito (#285). Falta llevarlo a producción, conciliar el desglose por componente y validarlo con una operación real. |
| 21 | ¿A quién atienden primero si no alcanza el camión? | A-34 | 27 | El motor prioriza entregas y puede conservar una parte pendiente cuando la capacidad no alcanza. No afirmamos que la comparación con operador contratado ya decida esa asignación. |
| 22 | ¿Qué pasa ante un cierre vial? | A-09, A-12 | 28 | Ante incidentes compatibles, el motor conserva lo ya ejecutado y evalúa una nueva versión para lo pendiente. Eso no incluye ralentizaciones parciales ni reasignación a un operador contratado. |
| 23 | ¿Por qué no Dijkstra? | A-07, A-33 | 29 | Para el pitch importa el plan completo: el motor propone rutas con ventanas, capacidad y jornada. No prometemos óptimo global; la matriz no compara algoritmos como afirmación de producto. |
| 24 | ¿Qué pasa si nos dan otro Excel? | A-22, A-23 | 28 | El importador permite mapear formatos con perfiles y revisar errores antes de aplicar. Aún falta medir el esfuerzo con archivos de una empresa; no prometemos adaptación sin trabajo. |
| 25 | ¿La flota es propia o contratada? | P-02 | 30 | La pantalla propone comparar ambas opciones, pero la alternativa contratada y la recomendación calculada siguen en integración. La mostramos como diseño, no como una decisión ya tomada por el motor. |
| 26 | ¿Cómo se calcula el costo total de servir? | P-01 | 32 | En un ensayo sintético del motor se desglosó combustible según carga, desgaste, conductor, ayudantes y merma esperada. Es un costo estimado. El ensayo usa `demo-base`, no los valores de la pantalla A/B/C. |
| 27 | ¿Por qué la IA no decide? | A-10, A-11 | 28 | El cálculo se apoya en datos y reglas verificables; la IA ayuda a explicarlos. El planificador conserva la decisión y algunas decisiones pueden quedar pendientes de aprobación humana. |
| 28 | ¿De dónde salen los datos del escenario? | P-04, A-03 | 27 | El escenario es sintético y generado por el equipo; algunas ubicaciones usan referencias geográficas públicas. Ventanas, demanda, costos e incidentes son supuestos, no clientes ni operaciones medidas. |
| 29 | ¿Cómo considera las horas pico y restricciones de Quito? | P-10, P-11 | 28 | Hay pruebas del motor con franjas horarias configuradas; son supuestos, no tráfico en vivo. Las restricciones de circulación verificadas para cada vehículo siguen en la hoja de ruta. |
| 30 | ¿Qué pasa con lo que no se entrega? | P-13, A-34 | 30 | El motor puede conservar demanda pendiente cuando la capacidad no alcanza. La guía de retorno y la prioridad automática al planificar el día siguiente son propuestas de hoja de ruta. |

## Registro de ensayos del equipo

Completar una fila por sesión real; los horarios siguientes son una propuesta compatible con la agenda, no ensayos ya realizados. En la sesión final, congelar materiales a las 15:45.

| Fecha/hora | Versión guion / slides | Duración real | Papeles (N/W/M/T/G) | Fallos observados | Cambio decidido |
|---|---|---:|---|---|---|
| — | — | — | — | — | — |
| — | — | — | — | — | — |
| — | — | — | — | — | — |

## Reparto propuesto de papeles

Propuesta basada en áreas de contribución registradas en los PR y documentos del equipo; cada integrante confirma su disponibilidad antes de ensayar. La autoría de código orienta la asignación técnica, no acredita práctica escénica.

| Papel | Integrante propuesto | Base de contribución |
|---|---|---|
| Narración | Joseph Baño | Coordinación, documentación y pitch; mantenerlo en el rol no crítico de presentación. |
| Web | Alexander Martínez | Interfaz de rutas, planificador web y campo; responsable de tomar la captura A/B/C con `demo-quito-planificacion`. |
| Móvil | Anthony Pilatasig | Motor de rutas y flujos operativos; coordinar la demostración del recorrido móvil si se incluye. |
| Técnica | Bryan Baño | Motor/agente, datos y costos; responsable de #241 y de explicar límites de integración. |
| Negocio | Jorge Doicela | Infraestructura, despliegue y panel; presentar viabilidad, riesgos y piloto propuesto. |

## Tres ensayos propuestos

La ronda E2E #118 está cerrada y no sustituye estos ensayos del pitch.

| Sesión | Bloque libre | Enfoque | Responsable de cronómetro/registro |
|---|---|---|---|
| 1 | Sábado 11:00–11:30 | Recorrido de papeles, transiciones y riesgos; registrar fallos de la demo y asignar cambios. | Joseph (propuesto; confirmar) |
| 2 | Sábado 15:00–15:20 | Pasada completa de 280 s con cronómetro; verificar legibilidad, demo y corte si falla la conexión. | Anthony (propuesto; confirmar) |
| 3 | Sábado 15:20–15:45 | Pasada final, preguntas breves y revisión de archivos; congelar materiales exactamente a las 15:45. | Jorge (propuesto; confirmar) |

Los cinco integrantes cubren su papel en cada pasada. Registrar duración real y fallos solo después de ejecutarlos.

## Diapositivas v3: clics y animaciones

Guía para quien presenta con `materiales/pitch-v3.pptx`. Todas las láminas entran con un fundido de 0,75 s. «Auto» significa que la entrada ocurre sola al abrir la lámina, sin clic; si se hace clic antes de que termine, PowerPoint completa la entrada. Los clics indicados son **adicionales** al que pasa a la lámina siguiente. En v3, la tabla A/B/C es la **lámina 5**, porque se agregó una portada (en v2 era la 4). El PDF es estático y muestra cada lámina completa.

| # | Lámina | Clics | Orden de aparición | Chillo |
|---:|---|---:|---|---|
| 1 | Portada | 1 | Auto: logo → título (barrido) → subtítulo y equipo → Chillo (zoom). Clic 1: globo. | Entra solo; globo en el clic 1 |
| 2 | Problema | 2 | Auto: título → «Mirar solo kilómetros» (barrido) → Chillo (zoom). Clic 1: «Comparar el servicio completo» → hipótesis. Clic 2: globo. | Entra solo; globo en el clic 2 |
| 3 | Solución | 4 | Auto: título → línea dorada. Clics 1 a 4: Comparar, Desglosar, Explicar, Decidir; tras el 4 aparecen solos el rótulo PROPUESTA y «La IA explica; el planificador decide». | — |
| 4 | Captura de rutas | 0 | Auto: título → captura (barrido) → tarjeta → rótulo «datos sintéticos». | — |
| 5 | Panel A / B / C | 3 | Auto: título y rótulo → encabezado → Chillo (zoom). Clic 1: filas Flota, Críticos a tiempo, Puntaje (en cascada). Clic 2: Costo total → marco dorado en C y recomendación del motor. Clic 3: globo. | Entra solo; globo en el clic 3 |
| 6 | Impacto | 0 | Auto: título → seis indicadores en cascada. | — |
| 7 | Viabilidad | 0 | Auto: título → tarjeta 01 → tarjeta 02 → banda «Límite» (barrido). | — |
| 8 | Modelo de negocio | 0 | Auto: título → tarjeta 01 → tarjeta 02 → frase del piloto. | — |
| 9 | Riesgos y mitigación | 1 | Auto: título → Chillo (zoom) → cinco riesgos en cascada → nota de la charla. Clic 1: globo. | Entra solo; globo en el clic 1 |
| 10 | Hoja de ruta y cierre | 2 | Auto: título → Chillo de cuerpo entero (zoom) → cuatro pasos. Clic 1: banda «Próximo paso» (barrido). Clic 2: globo. | Entra solo; globo en el clic 2 |

**Total: 13 clics de animación + 9 avances de lámina.**

### Frases de Chillo (aprobadas por Joseph, 16-oct)

Solo visuales: nadie las lee ni las dice en el pitch. No agregan hechos ni cifras: solo reformulan lo que ya dice cada lámina.

| Lámina | Frase |
|---|---|
| 1 · Portada | «¡Hola! Soy Chillo y hoy les cuento cómo **decidir mejor** cada ruta.» |
| 2 · Problema | «Si solo miramos kilómetros, la cuenta sale **incompleta**.» |
| 5 · A / B / C | «Una ruta no es solo kilómetros: también es **quien descarga**.» |
| 9 · Riesgos | «Todo plan tiene riesgos; lo clave es saber **mitigarlos**.» |
| 10 · Cierre | «¿Lo validamos juntos en un **piloto**? ¡Kikirikí!» |

Las frases están en `slide_cover`, `slide_problem`, `slide_abc`, `slide_risks` y `slide_roadmap` de `materiales/generar_v3.py`. Para cambiar una, se edita allí y se vuelve a generar.
