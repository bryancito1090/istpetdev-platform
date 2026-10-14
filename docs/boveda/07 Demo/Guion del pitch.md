---
tipo: demo
estado: propuesta
actualizado: 2026-10-14
tags: [demo, validacion, istpetdev]
---

# Guion del pitch

**Meta interna:** tres minutos. Duración oficial y tiempo de preguntas pendientes de confirmar en [[Consultas para la organizacion]]. Ajustar este guion a lo que informe la organización.

> [!warning] Lectura del kit previo (14-oct-2026)
> La sección «Complemento propuesto — spec 006, 12-oct-2026», al final de esta nota, propone sustituir el recorrido de 180 s y el texto base por cortes de 3/5/7 min (objetivos 2:40/4:40/6:40), con Deciders pendientes. O-04 sigue sin respuesta registrada; ninguno de los cortes es tiempo oficial. Para ensayar se usa el complemento; las secciones de arriba quedan como antecedente.

Protagonista: una empresa de limpieza; objeto conductor: una caja destinada a un hospital. El caso es sintético hasta contar con validación empresarial.

**Estructura oficial (Anexo 5.7, p. 25):** Problema → Solución → Prototipo / demo → Impacto → Viabilidad → Modelo de negocio → Próximo paso. El recorrido de abajo cubre las siete partes; viabilidad, modelo de negocio y próximo paso comparten el cierre de 20 segundos y deben nombrarse los tres.

**Audiencia:** cinco de los siete jurados no son perfiles técnicos (§11.3, p. 10). Cada tecnología mencionada debe ir unida a lo que cambia para la operación.

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

**Deciders PENDIENTE.** Este complemento sustituye para el próximo pitch el recorrido fijo de 180 s y su texto base: O-04 sigue pendiente. **⚠️ Contradicción detectada** — Hecho nuevo: el vivo usa `demo-region` (unos 15 puntos y dos vehículos) y la escala de unos 80 puntos solo aparece en simulación. · Hecho establecido: el texto base arriba abre con «80 puntos del país» sin distinguir ambas escenas. · Impacto: sugeriría una demo nacional en vivo. · Opciones: aclarar el alcance; conservar ambigüedad. · Propuesta: aclararlo en narración y etiqueta de pantalla. El caso del hospital es sintético. La cobertura de cinco días solo se dice si la ejecución de 002 CE-01 lo respalda.

El pitch sigue exactamente el Anexo 5.7: **problema → solución → demo → impacto → viabilidad → modelo de negocio → próximo paso**. La versión maestra dura **hasta 4:40** y reserva 20 s del turno nominal de 5 min. Los cortes de 3/7 duran como máximo 2:40/6:40; si O-04 indica otra duración, se sustituye el objetivo por tiempo oficial menos 20 s. Ninguno es tiempo oficial confirmado. Cinco papeles visibles: narración [PENDIENTE: nombre], web [PENDIENTE: nombre], móvil [PENDIENTE: nombre], técnica [PENDIENTE: nombre], negocio [PENDIENTE: nombre].

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
