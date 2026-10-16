---
tipo: demo
estado: propuesta
actualizado: 2026-10-16
tags: [demo, validacion, istpetdev]
---

# Guion del pitch

**Turno anunciado oralmente el 16-oct:** cinco minutos por equipo, según la [[2026-10-16-1109-bb-charla-limpieza-logistica|charla aportada por Bryan]]. **Meta interna:** 4:40 para el guion maestro; reservar 20 s no es una instrucción de la organización. Falta confirmar si demo, preguntas y transiciones caben en los cinco minutos (O-04 en [[Consultas para la organizacion]]). Los cortes de 3/7 minutos quedan como variantes internas, no turnos anunciados. No se ha ejecutado un ensayo nuevo con esta actualización.

> [!warning] Versión de trabajo T2 (14-oct-2026, fase 2 del issue #46)
> Para ensayar se usa la sección «Versión de trabajo T2», al final de esta nota: aplica la [matriz de afirmaciones](../../specs/006-validacion-pitch-entrega/matriz-afirmaciones.md) al complemento del 006. T1 fue aprobado por Joseph el 14-oct; T2 conserva el carácter de borrador hasta la revisión cruzada, el ensayo y el desglose pendiente de O-04. El anuncio del 16-oct permite seleccionar el corte maestro de cinco minutos, sin acreditar esas verificaciones.

> [!info] Preparación de T3–T5 (14-oct-2026, fase 3 del issue #46)
> Papeles, relevos, sustituciones, frases vigentes sin ensayo conectado y contingencias: [escena y contingencias](../../specs/006-validacion-pitch-entrega/escena-y-contingencias.md). Pasadas y respuestas se anotan en el [registro de ensayos](../../specs/006-validacion-pitch-entrega/registro-ensayos.md). Hoy ninguna parte de la demo tiene ensayo conectado: se dice la «Frase vigente» de esa nota, no la de la tabla de abajo. Los nombres de los papeles siguen sin confirmar.

> [!warning] Lectura del kit previo (14-oct-2026)
> La sección «Complemento propuesto — spec 006, 12-oct-2026», al final de esta nota, propone sustituir el recorrido de 180 s y el texto base por cortes de 3/5/7 min (objetivos 2:40/4:40/6:40), con Deciders pendientes. Al 14-oct O-04 no tenía respuesta. El 16-oct se anuncia un turno de cinco minutos: se usa ese corte con objetivo interno 4:40; los demás quedan como variantes. El recorrido de 180 s se conserva como antecedente.

Protagonista: una empresa de limpieza; objeto conductor: una caja destinada a un hospital. El caso es sintético hasta contar con validación empresarial.

**Estructura oficial (Anexo 5.7, p. 25):** Problema → Solución → Prototipo / demo → Impacto → Viabilidad → Modelo de negocio → Próximo paso. El recorrido de abajo cubre las siete partes; viabilidad, modelo de negocio y próximo paso comparten el cierre de 20 segundos y deben nombrarse los tres.

**Audiencia:** composición actual por confirmar (O-13). El reparto de siete personas del manual es antecedente, frente al anuncio de paneles por reto del 16-oct. Cada tecnología mencionada debe ir unida a lo que cambia para la operación.

## Recorrido de 180 segundos

| Tiempo | Lo que contamos | Lo que mostramos |
|---|---|---|
| 00:00–00:20 | «Cuando el hospital llama por falta de desinfectante, la emergencia ya ocurrió» | Punto y problema operacional |
| 00:20–00:45 | «Detectamos riesgo antes del faltante y sabemos qué datos lo explican» | Cobertura, lead time, fecha/frescura y cantidad propuesta |
| 00:45–01:15 | «Planificamos una ruta viable y respondemos a un cierre de vía» | Tradicional/optimizada; incidente y nueva versión |
| 01:15–02:05 | «Esta caja conserva su custodia aunque el conductor pierda la señal» | QR, despacho, recepción offline y sincronización |
| 02:05–02:40 | «Comparamos ambas políticas bajo la misma demanda durante 90 días simulados» | KPIs, denominadores y manifiesto del run |
| 02:40–03:00 | «Cuesta [costo verificado] operarlo, lo paga [cliente] y el siguiente paso es un piloto acotado con una empresa» | Viabilidad, modelo de negocio y próximo paso (piloto) |

Total: 180 segundos. Ensayar tiempos reales: si QR/firma tarda más, recortar interacción, no fingir el resultado.

## Texto base

«Una empresa de limpieza atiende 80 puntos del país. En nuestro escenario, un hospital tiene cinco días de cobertura. Hoy una operación reactiva espera a la llamada; nuestra plataforma identifica el riesgo, explica stock y consumo, y propone abastecimiento dentro del tiempo necesario.

Aquí vemos la ruta, su capacidad y sus horarios. Si se bloquea una vía, conserva lo ya entregado y propone el cambio para las paradas pendientes.

Esta caja se escanea en bodega y queda asociada a su lote y cantidades. El conductor puede capturar la recepción sin señal. Al conectarse, la operación se registra una sola vez y la evidencia completa la trazabilidad.

Comparamos 90 días de demanda sintética idéntica para ambas políticas. Los resultados de esta ejecución son los que aparecen en pantalla y pueden exportarse.

Buscamos validar el sistema con un piloto de cuatro semanas, empezando por una bodega y un grupo de puntos.»

Personalizar con resultados medidos y tiempo confirmado. Decir «cinco días» solo si S-02 y el run lo sostienen.

## Campos que deben sustituirse antes del pitch

| Campo | Origen |
|---|---|
| Km/costo total y por entrega | Resultado validado del run |
| Diferencia de costo y % | Fórmula con baseline y denominador; solo si existe run enlazado |
| Quiebres evitados/nuevos | Comparación de claves punto–SKU–día |
| Puntualidad | Obligaciones comunes vencidas |
| Trazabilidad completa | Líneas entregadas con evidencia requerida |
| Anomalías/mermas | Alertas etiquetadas y casos conciliados por separado |
| Costos del sistema | Configuración/precios verificados |
| Validación de usuario | Entrevista/mentoría efectivamente documentada |

No usar porcentajes de ejemplo como resultados. Si una métrica está pendiente, cerrar la medición o retirarla del texto.

## Preguntas previsibles

- ¿La predicción viene de la IA? El cálculo numérico es verificable; el LLM explica y contextualiza.
- ¿La ruta es óptima? Es una heurística factible comparada con una política documentada; no prueba óptimo global.
- ¿Qué ocurre sin señal? Se captura localmente y se valida al sincronizar; conflictos quedan visibles.
- ¿Cómo evitan duplicados? operationId, transacción y control de cantidades/duplicados de negocio.
- ¿El ahorro es real? Declarar si corresponde a simulación, entrevista o piloto; no mezclar niveles.
- ¿Qué hace escalable el sistema? Servicios sin estado, límites/pools y diseño de tiempo real; mostrar solo pruebas efectivamente realizadas.
- ¿Cómo tratan privacidad y auditoría? QR limitado, evidencia privada y historial append-only con garantías declaradas.

## Interacción física

El jurado puede consultar el QR en su teléfono si acepta y hay conectividad. La prueba de firma puede hacerse con una persona del equipo y datos ficticios. Preparar consulta ya abierta y un dispositivo de respaldo; no consumir el pitch configurando un teléfono.

Ver [[Checklist y contingencias]], [[Plus para ganar]] y [[Plan de validacion]].

## Complemento propuesto — spec 006, 12-oct-2026

**Deciders PENDIENTE.** Este complemento sustituye para el próximo pitch el recorrido fijo de 180 s y su texto base. Actualización del 16-oct: turno oral de cinco minutos, desglose de O-04 pendiente. **⚠️ Contradicción detectada** — Hecho nuevo: el vivo usa `demo-region` (unos 15 puntos y dos vehículos) y la escala de unos 80 puntos solo aparece en simulación. · Hecho establecido: el texto base arriba abre con «80 puntos del país» sin distinguir ambas escenas. · Impacto: sugeriría una demo nacional en vivo. · Opciones: aclarar el alcance; conservar ambigüedad. · Propuesta: aclararlo en narración y etiqueta de pantalla. El caso del hospital es sintético. La cobertura de cinco días solo se dice si la ejecución de 002 CE-01 lo respalda.

El pitch sigue exactamente el Anexo 5.7: **problema → solución → demo → impacto → viabilidad → modelo de negocio → próximo paso**. La versión maestra dura **hasta 4:40** y reserva 20 s del turno nominal de 5 min. Los cortes de 3/7 duran como máximo 2:40/6:40; el turno de 5 min fue anunciado el 16-oct. Los objetivos de 2:40/4:40/6:40 son reparto interno; los cortes de 3/7 no son turnos anunciados y el desglose de O-04 sigue pendiente. Cinco papeles visibles: narración [PENDIENTE: nombre], web [PENDIENTE: nombre], móvil [PENDIENTE: nombre], técnica [PENDIENTE: nombre], negocio [PENDIENTE: nombre].

| Bloque | 3 min (objetivo 2:40) | 5 min (objetivo 4:40) | 7 min (objetivo 6:40) | Idea que se dice |
|---|---|---|---|---|
| Problema | 0:00–0:18 | 0:00–0:30 | 0:00–0:45 | «Una falta de insumo obliga a reaccionar tarde»; `[CITA DE LA FERIA]` solo con consentimiento. |
| Solución | 0:18–0:38 | 0:30–1:05 | 0:45–1:35 | Riesgo explicado, propuesta de ruta y aprobación humana. |
| Prototipo/demo | 0:38–1:52 | 1:05–3:30 | 1:35–5:00 | Riesgo → ruta → incidente → caja/recepción → QR al final. Solo pasos cuyos CE tengan ejecución. |
| Impacto | 1:52–2:10 | 3:30–3:55 | 5:00–5:35 | `[RESULTADO DEL SIMULADOR]`: rango de tres seeds base y escenario adverso **separado**; quiebres evitados y nuevos frente a reactiva/fija. «90 días sintéticos; no ahorro observado». |
| Viabilidad | 2:10–2:22 | 3:55–4:10 | 5:35–6:00 | Recursos y límite concreto comprobados; fuente de costos pendiente si no existe. |
| Modelo de negocio | 2:22–2:32 | 4:10–4:27 | 6:00–6:22 | Comprador hipotético, ingreso por implementación y suscripción; `[PRECIO]` fuera del discurso hasta validar. |
| Próximo paso | 2:32–2:40 | 4:27–4:40 | 6:22–6:40 | Proponer piloto de cuatro semanas con una bodega y puntos acordados; pedir contraparte para definir baseline y umbrales. |

**Hitos de la demo maestra:** 1:05 riesgo/causa (web), 1:35 ruta/aprobación (web), 2:05 incidente y límite de autonomía (técnica), 2:30 caja/despacho y móvil, 3:10 invitar al jurado a escanear, 3:30 salir a impacto. Si el QR público falla, se usa el teléfono del equipo solo si está probado; de otro modo se muestra captura etiquetada y se avanza. Cada contingencia concreta está en [[Checklist y contingencias]]. En 3 min se omiten interacciones secundarias, no bloques; en 7 min se amplía contexto y límites, no se introduce otra historia.

**Plantilla de voz, aún sin resultados:** «En nuestro escenario sintético una empresa de limpieza necesita anticipar faltantes en un hospital. [CITA DE LA FERIA]. El sistema [capacidad sustentada por CE y ejecución] y propone una decisión que una persona puede aprobar. Aquí seguimos la caja [solo si 004 pasó]. En 90 días sintéticos, la comparación frente a reposición reactiva y calendario fijo dio [RESULTADO DEL SIMULADOR, run enlazado]; mostramos también dónde aparecieron quiebres nuevos. Para operar esto se requieren [recurso comprobado] y un piloto con datos reales. Nuestro cliente por validar es una empresa de limpieza; proponemos implementación y suscripción, con [PRECIO] por definir tras conocer costos y disposición de pago. El siguiente paso es un piloto de cuatro semanas». Los tres huecos permanecen fuera del PDF final si no se llenan con evidencia; nunca se pronuncian como valores.

### Banco de 20 preguntas difíciles (borradores para respuestas de hasta 30 s)

| # | Pregunta | Borrador breve, sujeto a lo realmente ejecutado |
|---:|---|---|
| 1 | ¿La IA predice el consumo? | No; la regla usa consumo conocido y media móvil. El agente puede organizar y explicar la propuesta si pasa sus pruebas. |
| 2 | ¿La IA aprueba rutas sola? | La política determinista decide dentro de umbrales; lo demás espera aprobación humana. El LLM no tiene permiso de aprobar. |
| 3 | ¿Qué ocurre si DeepSeek cae? | El motor determinista y la explicación por plantilla son el camino de respaldo, sujeto al E2E del agente. |
| 4 | ¿Es la ruta óptima? | Es una heurística factible con capacidad y ventanas; no demostramos óptimo global. |
| 5 | ¿Qué datos requiere? | Puntos, productos, saldos, consumo y flota; los faltantes se reportan como supuestos. |
| 6 | ¿Qué pasa si nos dan otro Excel? | Hay importador con perfiles de mapeo; el tiempo de adaptación solo se afirma con el simulacro G6 aprobado. |
| 7 | ¿Cómo conoce el stock? | Por saldos y movimientos importados/registrados; la frescura del dato se muestra, no se supone tiempo real. |
| 8 | ¿Qué pasa si no hay señal? | La PWA conserva captura y sincroniza al volver la red si el ensayo en teléfono pasa; un conflicto se muestra. |
| 9 | ¿Se duplica una recepción? | La operación usa clave idempotente y control de cantidades; solo afirmarlo al pasar 004 CE-03. |
| 10 | ¿Puede cualquiera escanear la caja? | La vista pública ofrece solo estado e hitos limitados, con token revocable; no incluye firma ni cantidades. Se invita al jurado a escanear solo si HTTPS y 004 CE-07 funcionan en el lugar. |
| 11 | ¿Funciona con 80 puntos o a escala nacional? | En vivo mostramos `demo-region` (unos 15 puntos sintéticos, una bodega y dos vehículos); la escala de unos 80 puntos solo aparece en la simulación de 005. La capacidad de escalar el servicio solo se afirma con una prueba realizada; si no existe, se dice que está pendiente. |
| 12 | ¿Qué prueba la simulación? | Compara políticas bajo demanda sintética común; no demuestra ahorro observado en una empresa. |
| 13 | ¿Por qué esos baselines? | Representan aviso reactivo y calendario fijo, fijados antes de mirar resultados; sus reglas y supuestos quedan exportados. |
| 14 | ¿Qué significan quiebres evitados y nuevos? | Se muestran ambos por punto, producto y día; no escondemos perjuicios tras una resta neta. |
| 15 | ¿Por qué tres seeds y una adversa? | Para mostrar variación base y un caso exigente aparte; no seleccionamos solo el mejor run. |
| 16 | ¿Cuánto ahorra una empresa real? | [PENDIENTE: piloto con costos observados]. Hoy solo podríamos mostrar un rango simulado con run verificable. |
| 17 | ¿Cuánto cuesta y cuál es el precio? | [PENDIENTE: costos medidos y disposición de pago]. Hipótesis: implementación más suscripción por operación/puntos activos. |
| 18 | ¿Evita multas hospitalarias? | No lo afirmamos. Fichas SERCOP muestran obligaciones/sanciones específicas; no prueban multas ni ahorro de este caso. |
| 19 | ¿Qué validaron con el sector? | Solo resumimos conversaciones consentidas de la feria y cambios observados; si faltan, lo declaramos pendiente. |
| 20 | ¿Qué es trabajo previo y qué se hizo aquí? | El tag `pre-evento` y el registro separan preparación de adaptación durante la hackathon; O-01 sigue por confirmar con fuente oficial. |

El banco cubre las «Preguntas previsibles» de arriba (escalabilidad en la 11, privacidad del QR en la 10); la parte de auditoría append-only sigue solo en esa lista. La pregunta 11 sustituyó el 14-oct a «¿Por qué el jurado escanea y no solo mira pantalla?», cuya condición pasó a la 10; queda sujeto a la revisión humana del kit.

La pregunta 16/17 no se responde pronunciando el marcador: se dice «aún no tenemos una medición/precio validado» y se explica el paso de piloto. Ensayar cada respuesta con cronómetro, ≤30 s; no convertir un borrador en hecho técnico hasta enlazar CE y ejecución.

## Versión de trabajo T2 — 14-oct-2026 (fase 2 del issue #46)

**Borrador aprobado por Joseph el 14-oct; revisión cruzada pendiente.** Redactado con IA (Claude Code, Opus 5.5) a pedido de Joseph Baño. T1 tiene revisión humana registrada; la matriz requiere un segundo revisor y volver a comprobar los ajustes A-16/A-17/A-27. Cada frase sale de la [matriz de afirmaciones](../../specs/006-validacion-pitch-entrega/matriz-afirmaciones.md) (columna «#»). Actualización del 16-oct: se anuncia turno de 5 min y se selecciona el maestro; **160/280/400 s siguen siendo metas internas, no cronometrajes ni duración del turno**. Falta el desglose de preguntas de O-04. No hay ensayos cronometrados todavía; los segundos de abajo son reparto planificado, no mediciones.

**Etiquetas de escena.** `VIVO PREVISTO`: capacidad probada en pruebas automatizadas (EJ-07/08); la escena en pantalla no se ha ensayado y no se anuncia como «en vivo» hasta pasar E2E-04 del 006; si falla, se dice la sustitución. `GRABADO`: video del 15-oct, aún no existe; si se usa, se dice «esto es una grabación». `SIMULADO`: 005, datos sintéticos. `PROPUESTO`: diseño o plan, no demostrado.

**Papeles visibles:** narración [PENDIENTE: nombre], web [PENDIENTE: nombre], móvil [PENDIENTE: nombre], técnica [PENDIENTE: nombre], negocio [PENDIENTE: nombre]. Cada uno habla al menos una vez en los tres cortes.

### Guion maestro de 5 min (meta 280 s)

| Bloque (Anexo 5.7) | Reparto | Papel | Etiqueta | Lo que se dice | Matriz |
|---|---|---|---|---|---|
| 1. Problema | 0–30 | Narración | PROPUESTO + fuente | «En contratos públicos de limpieza hospitalaria, el proveedor está obligado a mantener los químicos de desinfección y los insumos con frecuencias de reposición definidas por el hospital. Nuestra hipótesis es que hoy la reposición empieza cuando el hospital ya avisó la falta. Les mostramos un caso sintético: una caja de desinfectante que va a un hospital.» | A-01, A-02 |
| 2. Solución | 30–65 | Narración → web | PROPUESTO | «Nuestra plataforma calcula qué punto se va a quedar sin producto y por qué, propone la ruta y deja a una persona la decisión cuando un cambio supera los límites. El cálculo es una regla verificable; la IA no aprueba nada.» | A-06, A-10 (sustitución), A-11 (sustitución) |
| 3. Demo — riesgo | 65–95 | Web | VIVO PREVISTO | «En nuestro escenario sintético hay una bodega, 15 puntos y dos vehículos. El hospital norte tiene cinco días de cobertura de desinfectante: riesgo alto. Aquí están los datos que lo explican: stock, consumo y días.» | A-03 (sustitución), A-04, A-06 |
| 3. Demo — ruta | 95–125 | Web | VIVO PREVISTO | «Propone una ruta para cada vehículo respetando capacidad, horarios de atención y jornada.» | A-07 |
| 3. Demo — incidente | 125–150 | Técnica | VIVO PREVISTO | «Cerramos una vía. Las paradas ya hechas se conservan y se crea una nueva versión de la ruta. Si el cambio supera los límites de costo o retraso, no se ejecuta: espera la aprobación de una persona.» | A-09, A-10 (sustitución) |
| 3. Demo — caja | 150–190 | Móvil | PROPUESTO (VIVO solo si E2E-04 pasa) | «Así se prepara la caja: queda ligada a su lote y a la entrega. El conductor registra la recepción en la app.» Si E2E-04 no pasa: mostrar la etiqueta como diseño propuesto y no simular despacho. | A-13, A-15 (sustitución) |
| 3. Demo — trazabilidad | 190–210 | Móvil → web | VIVO PREVISTO (API) | «La consulta pública devuelve producto, estado e hitos sin datos privados; un token alterado recibe 404 en la prueba de API.» Mostrar una pantalla solo después de comprobarla conectada. Sin invitar al jurado a escanear. | A-16 (sustitución) |
| 4. Impacto | 210–235 | Web → narración | SIMULADO | «Diseñamos la comparación antes de ver resultados: 90 días sintéticos, tres semillas y un escenario adverso aparte, contra reposición reactiva y calendario fijo. Contamos los quiebres evitados y también los nuevos. El resultado está respaldado por su archivo verificable (ver [[Cifras del simulador 90 dias]]): IstpetDev reduce un 9,8% los kilómetros frente al calendario fijo manteniendo la reposición preventiva.» | A-19, A-20, A-21 |
| 5. Viabilidad | 235–250 | Técnica | VIVO PREVISTO (pruebas) | «Usa datos que una empresa ya tiene; un archivo con otro formato se carga escribiendo solo un perfil. En prueba local, la ruta de 80 puntos se calcula en menos de cinco segundos. No prometemos la ruta óptima.» | A-22, A-24, A-33 |
| 6. Modelo de negocio | 250–267 | Negocio | PROPUESTO | «Nuestro cliente por validar es la empresa de limpieza. Cobraríamos implementación y una suscripción; el precio lo definiremos con costos reales del piloto.» `[PRECIO]` no se pronuncia. | A-26, A-30, A-31 |
| 7. Próximo paso | 267–280 | Negocio → narración (cierre) | PROPUESTO | «Proponemos un piloto de cuatro semanas con una bodega y un grupo de puntos acordados. Buscamos una empresa que quiera medirlo con nosotros.» | A-32 |

### Corte de 3 min (meta 160 s)

Mismo orden y mismos cinco papeles; una acción por sistema.

| Bloque | Reparto | Papel | Qué se conserva |
|---|---|---|---|
| 1. Problema | 0–18 | Narración | A-01 + hipótesis A-02 en una frase |
| 2. Solución | 18–38 | Narración → web | Calcula, propone, una persona aprueba; la IA no aprueba |
| 3. Demo | 38–112 | Web (riesgo + ruta), técnica (incidente, una frase), móvil (caja o vista pública, solo una) | A-04, A-07, A-09/A-10 sustitución, A-13 o A-16 sustitución |
| 4. Impacto | 112–130 | Web | A-19 sustitución, sin cifras |
| 5. Viabilidad | 130–142 | Técnica | A-22 o A-24, una frase, + «no prometemos la ruta óptima» |
| 6. Modelo de negocio | 142–152 | Negocio | A-31 sustitución, sin precio |
| 7. Próximo paso | 152–160 | Negocio → narración | A-32 |

### Corte de 7 min (meta 400 s)

Mismo relato; se amplían supuestos y límites, no se añade otra historia.

| Bloque | Reparto | Papel | Qué se añade al maestro |
|---|---|---|---|
| 1. Problema | 0–45 | Narración | `[CITA DE LA FERIA]` **solo** si CE-02 del 006 y el permiso de cita existen; si no, «es una hipótesis que estamos validando con expositores de la feria» (A-29) |
| 2. Solución | 45–95 | Narración → web | Qué decide la regla y qué decide una persona |
| 3. Demo | 95–300 | Web, técnica, móvil | Distribución de riesgos 1/4/3/1/6 (A-05); los límites de autonomía (A-10 sustitución); vista pública con token alterado (A-16 sustitución) |
| 4. Impacto | 300–335 | Web → narración | Por qué dos baselines y por qué quiebres nuevos (A-19, A-20); sin cifras |
| 5. Viabilidad | 335–360 | Técnica | Un límite real: heurística, no óptimo; agente con IA en preparación (A-11 sustitución) |
| 6. Modelo de negocio | 360–382 | Negocio | Costos por medir en el piloto (A-26 sustitución) |
| 7. Próximo paso | 382–400 | Negocio → narración | Semanas 1–4 de la ficha de piloto (plan del 006) |

### Frases que salen del texto anterior

Del recorrido de 180 s, del texto base y del complemento del 12-oct se retiran: «80 puntos del país» (A-03), «la IA detecta/propone» (A-11), «el conductor puede capturar la recepción sin señal» y «sincroniza una sola vez» (A-15), la invitación al jurado a escanear (A-16), «una decisión se ejecuta sola y otra espera aprobación» (A-10, pasa a sustitución), «los resultados de esta ejecución son los que aparecen en pantalla» (A-21), «Cuesta [costo verificado]» (A-26) y «lo paga [cliente]» (A-31). Esas secciones quedan como antecedente.

### Estructura de las siete diapositivas (sin archivo)

Al 14-oct no había respuesta de O-06 y esta sección solo definía la estructura; después se generaron archivos provisionales según el manifiesto del 006. El 16-oct se anuncia Drive para el capitán, pero URL, acceso efectivo y formatos siguen pendientes. Esta actualización no genera materiales finales ni acredita su entrega.

| # | Bloque | Título de trabajo | Contenido | Rótulo visible |
|---|---|---|---|---|
| 1 | Problema | Reponer antes de la llamada | Frase SERCOP con fuente; hipótesis del equipo; ícono de caja → hospital | «Caso sintético» |
| 2 | Solución | Calcula, propone, una persona decide | Tres pasos: riesgo explicado → ruta → aprobación humana; «la IA no aprueba» | — |
| 3 | Demo | Una caja hacia el hospital norte | Pantalla de la demo; si falla, captura con su rótulo; orden riesgo → ruta → incidente → caja | «Demo» o «Captura» / «Grabación» según lo que se muestre |
| 4 | Impacto | Cómo mediremos el impacto | Diseño de la comparación (90 días, tres semillas + adverso, dos baselines, evitados y nuevos); cifras verificables documentadas en [[Cifras del simulador 90 dias]] | «Simulación — 12 corridas verificables» |
| 5 | Viabilidad | Qué ya funciona y qué no | Dos capacidades usables con su límite (A-22, A-24); un límite honesto | «Prueba local» |
| 6 | Modelo de negocio | Quién paga y por qué | Cliente por validar, implementación + suscripción; sin precio | «Hipótesis» |
| 7 | Próximo paso | Piloto de cuatro semanas | Semanas 1–4 y lo que pedimos | «Propuesta» |
