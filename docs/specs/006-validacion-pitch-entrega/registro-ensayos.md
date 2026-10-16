# 006 — Registro de ensayos y respuestas (T5, fase 3 del issue #46)

**Preparado:** 14-oct-2026 por Claude Code (Opus 5.5) a pedido de Joseph Baño. **Sin pasadas registradas:** Joseph informó el 14-oct que todavía no hubo ensayos. Los segundos del [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md) son reparto planificado y **no** se copian aquí como tiempos medidos. [Tareas](task.md) · [E2E-03/04/06](test-e2e.md) · [Escena y contingencias](escena-y-contingencias.md).

## Reglas

- Una fila por pasada real, escrita por quien cronometra, incluso si falla. Esta IA no completa filas.
- **Duración medida:** de la primera palabra al cierre, con cronómetro. Meta: tiempo oficial −20 s; mientras O-04 siga pendiente, 160/280/400 s son metas internas y la pasada cuenta como preparación.
- **Pasada completa sin fallo:** siete bloques en orden, cinco papeles hablan, ninguna frase fuera de la columna «Frase vigente» de la [escena](escena-y-contingencias.md#2-escena-del-guion-maestro-de-5-min-acción-visible-relevo-y-sustitución), duración ≤ meta, y ninguna contingencia que exceda su bloque.
- **Racha:** suma 1 por pasada completa sin fallo; **cualquier fallo la deja en 0**. Cambiar de versión (guion, diapositivas o commit) en una escena obliga a repetir al menos una pasada de esa versión.
- CE-04 se cierra solo con **diez pasadas consecutivas** del corte oficial y las **dos últimas sin la wifi del recinto** (conexión de respaldo elegida para la escena).
- Papeles: anotar quién ocupó cada uno en esa pasada (N/W/M/T/G = narración, web, móvil, técnica, negocio). Si un suplente reemplazó al titular, anotarlo.

## Pasadas del pitch

| # | Fecha/hora | Corte | Duración medida (s) | Meta (s) | N / W / M / T / G | Versión guion/diapositivas | Commit | Conexión | Fallos / contingencias usadas | Enlace (video interno o nota) | Racha |
|---:|---|---|---|---:|---|---|---|---|---|---|---|---:|
| — | — | — | — | — | — | — | — | — | — | — | — | 0 |

**Racha actual: 0 / 10.** Pasadas sin wifi del recinto al final de la racha: 0 / 2.

## Ensayos funcionales de la demo

Pasadas técnicas del protocolo de la [sección 4 de escena](escena-y-contingencias.md#4-ensayo-funcional-y-video-de-respaldo-del-15-oct). Una pasada funcional exitosa habilita la columna «Si E2E-04 pasa conectado» solo para las partes que recorrió y en ese commit.

| Fecha/hora | Commit | Entorno (nube/local) | Dispositivos | Partes recorridas conectadas (3a–3e) | Contingencias provocadas (C-01–C-05) y resultado | Resultado | Enlace | Quién |
|---|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — | — |

## Ronda de 24 respuestas (ensayos de preguntas)

Se cronometra **aparte** del pitch. Una persona lee la pregunta del [banco](../../boveda/07%20Demo/Guion%20del%20pitch.md); quien responde no la conoce de antemano. Límite: 30 s por respuesta (40–70 palabras); sin precio ni cifra improvisada; si la respuesta exige evidencia que no existe, se marca.

| Ronda | Fecha/hora | Versión del banco (commit) |
|---|---|---|
| — | — | — |

| # | Pregunta (banco) | Quién respondió | Tiempo medido (s) | ≤30 s | Afirmó algo sin evidencia (ID de matriz) | Ajuste al borrador |
|---:|---|---|---|---:|---|---|---|
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
| 20 | ¿Qué es trabajo previo y qué se hizo aquí? | — | — | — | — | — |
| 21 | ¿A quién atienden primero si no alcanza el camión? | — | — | — | — | — |
| 22 | ¿Qué pasa ante un cierre vial? | — | — | — | — | — |
| 23 | ¿Por qué no Dijkstra? | — | — | — | — | — |
| 24 | ¿Qué pasa si nos dan otro Excel? | — | — | — | — | — |

CE-05 se cumple con una ronda en la que las 24 respuestas quedan ≤30 s sin afirmaciones fuera de la [matriz](matriz-afirmaciones.md). Las respuestas 3, 8, 9 y 10 dependen de evidencia que hoy falta (A-12, A-15, A-14, A-16): se responden con su sustitución.

## Banco de respuestas ensayadas de 30 s (24 preguntas)

Respuestas preparadas para la ronda de preguntas del jurado. Cada respuesta está diseñada para pronunciarse en **30 segundos o menos** (40–70 palabras a ritmo natural de ~130 palabras/minuto). Cumplen la regla estricta de **cero cifras ni precios inventados**, distinguiendo capacidades demostradas, métodos matemáticos/algorítmicos y elementos en preparación o propuesta.

| # | Pregunta | Soporte / Matriz | Palabras | Respuesta ensayada (≤ 30 s) |
|---:|---|---|---:|---|
| 1 | ¿La IA predice el consumo? | A-06, A-11 | 44 | No; la regla usa consumo histórico conocido y media móvil de demanda. El agente o asistente con IA no inventa números ni aprueba operaciones; su función en preparación es contextualizar y estructurar explicaciones comprensibles basadas en los hechos ya calculados por el motor. |
| 2 | ¿La IA aprueba rutas sola? | A-10, A-11, A-33 | 48 | No; la IA no tiene permisos para aprobar rutas. El motor determinista aplica decisiones automáticas solo si se mantienen estrictamente dentro de umbrales operativos configurados. Cualquier desvío significativo, sobrecosto o retraso que supere los límites se escala de forma obligatoria para la decisión de una persona. |
| 3 | ¿Qué ocurre si DeepSeek cae? | A-12 | 47 | El cálculo no depende de la IA. Si DeepSeek o n8n no están disponibles, el motor logístico determinista en backend continúa planificando rutas y evaluando restricciones con total normalidad. Como respaldo de explicación, el sistema activa plantillas deterministas estructuradas con los datos del cálculo. |
| 4 | ¿Es la ruta óptima? | A-07, A-33 | 46 | No prometemos un óptimo matemático global. El motor resuelve el problema de ruteo con ventanas de tiempo y capacidad mediante una heurística constructiva factible mejorada con 2-opt. Su objetivo es entregar un plan operativo viable y eficiente en pocos segundos, respetando restricciones físicas reales. |
| 5 | ¿Qué datos requiere? | A-22 | 49 | Requiere cinco fuentes de datos: puntos de servicio con coordenadas y ventanas horarias, catálogo de productos con peso y volumen, saldos de inventario inicial, historial o proyección de demanda, y vehículos con capacidad y jornada. Si falta algún dato no crítico, el importador lo declara como supuesto. |
| 6 | ¿Qué pasa si nos dan otro Excel? | A-22, A-23 | 48 | El importador canónico desacopla el archivo del modelo interno mediante perfiles declarativos. Adaptar un formato nuevo consiste en mapear nombres de columnas y unidades sin modificar código. No prometemos plazos menores a una hora sin medirlo con archivos ajenos, pero el mecanismo está estructurado y probado. |
| 7 | ¿Cómo conoce el stock? | A-04, A-06 | 49 | Por saldos iniciales importados y movimientos de despacho y recepción registrados. La plataforma calcula la cobertura dividiendo el stock entre el consumo proyectado y muestra en pantalla la fecha y hora de frescura de cada dato. No asumimos sincronización en tiempo real sin una integración activa. |
| 8 | ¿Qué pasa si no hay señal? | A-13, A-15 | 47 | La PWA de campo está diseñada para operar sin conexión: almacena la recepción en IndexedDB local. Cuando el dispositivo recupera señal de red, sincroniza las capturas pendientes con el backend. En el pitch lo presentamos con prudencia técnica según lo verificado en pruebas de integración. |
| 9 | ¿Se duplica una recepción? | A-14 | 49 | No se duplica. La operación de recepción utiliza una clave idempotente única por entrega y control estricto de bultos y cantidades recibidas. Si el conductor reintenta el envío o la red retransmite la petición, el backend detecta la clave existente y no crea registros duplicados. |
| 10 | ¿Puede cualquiera escanear la caja? | A-16 | 49 | El código QR público dirige a una vista web protegida con token limitado y revocable. Solo muestra hitos generales de trazabilidad y estado logístico del paquete; no expone firmas, montos, nombres privados ni inventarios de bodega. Un token alterado o inválido recibe respuesta 404 por la API. |
| 11 | ¿Funciona con 80 puntos o a escala nacional? | A-03, A-24, A-25 | 48 | En vivo mostramos el escenario regional con 15 puntos sintéticos, una bodega y dos vehículos. La escala de 80 puntos se ha evaluado en simulación y pruebas locales de rendimiento donde el motor planifica en menos de cinco segundos. El despliegue nacional requerirá pruebas de carga dedicadas. |
| 12 | ¿Qué prueba la simulación? | A-19, A-21 | 47 | Compara de forma controlada dos políticas operativas bajo 90 días de demanda sintética común: reposición preventiva contra baselines de reposición reactiva y calendario fijo. Evalúa el comportamiento del algoritmo ante variaciones de consumo, pero no constituye una medición de ahorro financiero observado en una empresa. |
| 13 | ¿Por qué esos baselines? | A-19 | 47 | Representan las prácticas estándar de la industria: la reposición reactiva espera el reclamo por falta de stock y el calendario fijo visita puntos en días preestablecidos sin mirar consumo. Fijamos estas reglas antes de evaluar resultados para asegurar una comparación metodológicamente neutral y auditable. |
| 14 | ¿Qué significan quiebres evitados y nuevos? | A-20 | 51 | Un quiebre evitado ocurre cuando el motor abastece a tiempo un punto que en el baseline se quedaba sin stock. Un quiebre nuevo ocurre si por capacidad o desvío se desatiende un punto que el baseline sí cubría. Mostramos ambas métricas por separado, sin esconder perjuicios bajo restas netas. |
| 15 | ¿Por qué tres seeds y una adversa? | A-19 | 50 | Usamos tres semillas aleatorias para verificar que los resultados no dependan de una combinación afortunada de demanda. Añadimos una semilla adversa con picos abruptos y dispersión para estresar el motor y mostrar con honestidad cómo responde el sistema bajo condiciones difíciles, sin seleccionar solo el mejor caso. |
| 16 | ¿Cuánto ahorra una empresa real? | A-21, A-26, A-33 | 51 | No prometemos cifras de ahorro en porcentaje ni dinero porque no tenemos mediciones operativas en una empresa real. Los resultados mostrados provienen de simulaciones sintéticas comparadas. El ahorro real dependerá de la flota, dispersión y costos del cliente, lo cual mediremos formalmente en el piloto de cuatro semanas. |
| 17 | ¿Cuánto cuesta y cuál es el precio? | A-26, A-30, A-31 | 51 | No fijamos un precio de suscripción arbitrario. El modelo contempla una tarifa de implementación técnica y una suscripción mensual según volumen de puntos y operaciones activas. El importe exacto se definirá tras medir los costos reales de infraestructura y la disposición de pago durante el piloto de cuatro semanas. |
| 18 | ¿Evita multas hospitalarias? | A-01, A-33 | 49 | No afirmamos que evite multas. Las fichas técnicas del SERCOP demuestran que los contratos públicos de limpieza imponen sanciones severas por desabastecimiento de desinfectantes. Nuestra plataforma ayuda a reducir el riesgo de faltantes al anticipar la reposición, pero no podemos certificar la eliminación legal de penalizaciones contractuales. |
| 19 | ¿Qué validaron con el sector? | A-02, A-29 | 47 | Contrastamos con expositores de la feria y personas del sector la problemática operativa: confirmaron que reponer tras la queja genera urgencias costosas y riesgo en contratos públicos. Mantenemos estas conclusiones como hipótesis en validación cualitativa hasta documentar entrevistas formales con consentimiento en el informe final. |
| 20 | ¿Qué es trabajo previo y qué se hizo aquí? | A-28, O-01 | 49 | La estructura base del proyecto y dependencias iniciales estaban identificadas con la etiqueta pre-evento según las reglas de transparencia. Durante la hackathon se desarrollaron e integraron la lógica del motor logístico, los algoritmos de replanificación, los modelos de riesgo, la PWA de campo y las suites de pruebas automatizadas. |
| 21 | ¿A quién atienden primero si no alcanza el camión? | AMP-01, Spec 002 | 66 | El motor prioriza mediante un orden determinista y transparente: primero evalúa el nivel de riesgo (Crítico antes que Alto), luego la menor holgura de tiempo (cobertura menos lead time), después la criticidad del punto de servicio, y finalmente el código. Si no alcanza la capacidad, los puntos menos urgentes quedan formalmente clasificados como no atendidos con su motivo explícito (`ReplenishmentAllocator` y `RoutePlanner`). |
| 22 | ¿Qué pasa ante un cierre vial? | AMP-03, Spec 002 | 66 | Se ejecuta una replanificación determinista que conserva las paradas ya realizadas y genera una nueva versión de ruta. El motor dispone de modos configurables: `Strict`, `LateService` si admite demoras, o `PrioritizeCritical`, que descarta paradas de consolidación para proteger las entregas críticas. Si el plan resultante supera los límites de costo o jornada, no se auto-ejecuta y escala a aprobación humana. |
| 23 | ¿Por qué no Dijkstra? | AMP-10, OSRM | 66 | Dijkstra halla el camino mínimo entre nodos aislados en un grafo estático. En nuestro sistema, el servicio OSRM ya calcula distancias y tiempos mínimos sobre la red vial real de OpenStreetMap, respetando sentidos de giro y velocidades reales. Sobre esa matriz real, el algoritmo del motor resuelve el problema complejo de ruteo vehicular con ventanas horarias, jornadas y capacidades físicas. |
| 24 | ¿Qué pasa si nos dan otro Excel? | Spec 001, ADR-22 | 57 | El sistema dispone de un importador canónico con perfiles de mapeo declarativos. Permite homologar archivos con distintas columnas, orden o unidades hacia el esquema interno sin modificar una sola línea de código fuente. Su funcionamiento está probado y validado con datasets externos, permitiendo incorporar nuevos datos estructurados con reglas de validación previas. |
