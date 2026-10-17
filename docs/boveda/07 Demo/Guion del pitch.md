---
tipo: demo
estado: propuesta
actualizado: 2026-10-17
tags: [demo, validacion, istpetdev, planificacion]
---

# Guion del pitch — T4

## Versión vigente v5 (17-oct): 12 láminas, 4:40

Reemplaza a la v4 (conservada más abajo como antecedente). Los PDF y el PPTX del pitch (`7-Pitch`) salen de `materiales/generar_v5.py`. Mismo texto que el PDF [`8-Guion-del-pitch.pdf`](../Entregable/8-Guion-del-pitch.pdf) de la carpeta `Entregable`; **esta nota es la fuente**: se edita aquí y se regenera el PDF. Las láminas 5, 6 y 7 usan capturas reales (cobertura, rutas, novedades).

### Láminas 5–7: capturas (17-oct)

| Lámina | s | Pantalla | Qué debe verse en la captura | Estado |
|---|---:|---|---|---|
| 5 | 10 | Monitoreo de cobertura (antes «Alertas») | Tabla con centro de salud, SKU, días de cobertura, nivel de riesgo y cantidad sugerida; pestañas con 9 quiebres críticos y 203 en margen preventivo. | Hecha (`alertas.png`) |
| 6 | 10 | Rutas | Vista previa de la ruta Bodega Sur → Clínica Sur (GIF en el PPTX, imagen fija en el PDF), más recortes de VEH-02 con «Aprobar y publicar ruta» y del seguimiento en vivo de paradas. | Hecha |
| 7 | 10 | Novedades | KPIs (rechazos en destino, impacto de reentrada USD 19,62 estimado, ajustes de percha, pedidos de campo) y rechazos por motivo: daño y error de rotulado. | Hecha (`novedades.png`) |

Capturas y GIF en `docs/specs/006-validacion-pitch-entrega/materiales/assets/capturas-v5/`; se montan con `materiales/generar_v5.py`, que produce el PPTX con GIF animado y el PDF con la imagen fija. Si el texto de una lámina deja de coincidir con la pantalla, se corrige el guion o se vuelve a capturar.


### 00:00–00:15 · Joseph · Diapositiva 1

¿Cómo se entera hoy una empresa de limpieza de que a un hospital le falta desinfectante? Cuando el hospital llama. Ese día ya es tarde.

### 00:15–00:45 · Joseph · Diapositiva 2

Lo escuchamos ayer en la charla del sector. El planificador repone por calendario o por urgencia; bodega pierde de vista el insumo cuando sale; y nadie puede probar qué se entregó. Cuando se apura una entrega, se decide mirando kilómetros y no el costo real: personal, combustible, descarga y producto que se daña. Y el que paga es el cliente final: un hospital o una clínica sin insumos de limpieza.

### 00:45–01:15 · Joseph · Diapositiva 3

IstpetDev une eso en una sola cadena. Detecta: revisa el inventario y avisa qué punto se quedará sin producto. Decide: compara cómo atenderlo con flota propia o con un operador contratado, sumando todo lo que cuesta servir. Entrega: arma la ruta respetando capacidad, horarios y jornada. Y prueba: cada caja viaja con QR. Bodega y conductor pueden guardar registros sin señal y sincronizarlos después; la recepción final del cliente requiere conexión.

### 01:15–01:50 · Bryan · Diapositiva 4

¿Qué es lo nuevo? Que no es otro inventario con alertas: la alerta termina en una decisión con costo y en una entrega comprobable. Lo calcula un motor con reglas, siempre igual con los mismos datos. Encima trabaja un agente de inteligencia artificial, orquestado con n8n, que lee esos resultados y le explica al planificador, en lenguaje simple, por qué conviene cada opción. La IA no inventa cifras: solo explica las del motor, y si falla, hay una explicación de respaldo. La persona aprueba. Y como n8n está separado del motor, podemos cambiar de modelo o agregar automatizaciones sin tocar el cálculo.

### 01:50–02:00 · Alexander · Diapositiva 5

Así se ve. Primero, el monitoreo de cobertura: qué punto entra en riesgo y cuánto reponer, antes de que el cliente llame.

### 02:00–02:10 · Alexander · Diapositiva 6

De la alerta a la ruta: el mapa y el seguimiento de paradas, en nuestro escenario sintético de Quito. El planificador la revisa y aprueba.

### 02:10–02:20 · Alexander · Diapositiva 7

Y lo que pasa en el camino queda registrado: novedades de campo, como un daño o un error de rotulado, con su motivo.

### 02:20–03:05 · Alexander · Diapositiva 8

Y esta es la decisión. A, flota propia: la más barata, 57,89 dólares, pero llega a tiempo solo al 83 % de los críticos. B llega al 100 %, pero cuesta un 20,7 % más. C, un operador contratado, también llega al 100 % y cuesta un 4,3 % menos que B. El motor recomienda C con 90,8 puntos y la IA explica por qué. La más barata no siempre es la mejor: pondera costo y entregas críticas a tiempo por igual.

### 03:05–03:40 · Anthony · Diapositiva 9

Seamos claros con lo que hay. Construido: inventario con alertas, motor de rutas y costos, agente de IA con n8n y custodia con QR. Probado: el motor y la comparación A/B/C en un ensayo técnico con un escenario sintético de Quito de 40 puntos, y una corrida registrada del agente, incluida su caída al respaldo. Falta: ensayar el recorrido en teléfono sin señal, llevar la comparación a producción y medir con datos de una empresa real. Preferimos mostrarles lo que funciona de verdad que prometer lo que no hemos medido.

### 03:40–04:05 · Jorge · Diapositiva 10

El impacto que buscamos: menos faltantes que se descubren tarde, entregas críticas a tiempo y saber cuánto cuesta realmente cada entrega. No prometemos un ahorro: lo mediremos en un piloto contra una línea base. Cada indicador está escrito para que lo entienda cualquier rol, de bodega a gerencia.

### 04:05–04:20 · Jorge · Diapositiva 11

No necesita hardware nuevo: funciona en la web y en el teléfono del conductor. Proponemos implementación inicial más suscripción. Y si faltan datos, el sistema dice “sin dato” y no inventa.

### 04:20–04:40 · Joseph · Diapositiva 12

El siguiente paso: un piloto con una empresa de limpieza, una línea base e indicadores acordados. Buscamos ese aliado aquí. Detectar antes, decidir con el costo real y probar cada entrega. Gracias.

---

## Propuesta v4 (antecedente)
 dolor, innovación e IA al frente

Reorganiza el pitch con la [[2026-10-17-1129-jb-presentacion-pitch-perfecto|mentoría «Pitch que Convence»]] y el [[2026-10-17-1050-bb-charla-pitch-jurado-rentabilidad|apunte de audio]]. Resuelve cuatro huecos de la v3: el dolor era abstracto (flota propia o contratada), la innovación no se veía como cadena, la IA solo aparecía para decir lo que no hace y n8n no se nombraba. Además, la apertura «Somos IstpetDev…» rompía la regla de los primeros 30 segundos. **Estado: superada por la v5.** Láminas: [`pitch-v4.pptx`](../../specs/006-validacion-pitch-entrega/materiales/pitch-v4.pptx) (PDF y PNG en `render-v4/`), generadas con `materiales/generar_v4.py`. La lámina 5 muestra «CAPTURA PENDIENTE» hasta que se guarde C1 como `materiales/assets/capturas-v4/c1-alerta.png` y se vuelva a ejecutar el script. La v3 queda debajo como respaldo.

**Frase central:** «Detectamos el faltante antes de que el cliente llame, proponemos cómo atenderlo al costo real y dejamos prueba de cada entrega. El motor calcula, la IA explica y la persona decide.»

**Diferencial frente a un inventario con alertas** (el Modelo 2 del mentor, «StockClean», se parece a nosotros): nosotros no paramos en la alerta, sino que la convertimos en decisión de ruta con el costo completo y en evidencia de entrega. Decirlo en el primer minuto.

**Regla de honestidad:** se dice una sola vez, en el bloque de validación, con el formato del mentor: construido, probado y pendiente. El resto del guion no repite advertencias en cada frase, pero tampoco afirma nada fuera de la [matriz de afirmaciones](../../specs/006-validacion-pitch-entrega/matriz-afirmaciones.md).

### Guion v4 — 280 s (turno de 5 min de exposición + 1 min de preguntas; 20 s de margen)

| # (mentor) | Lámina propuesta | s | Texto para ensayar | Respaldo |
|---|---|---:|---|---|
| 1. Problema (gancho) | 1 · Portada con la pregunta | 15 | «¿Cómo se entera hoy una empresa de limpieza de que a un hospital le falta desinfectante? Cuando el hospital llama. Ese día ya es tarde.» | Dolor recogido en la charla del sector ([[2026-10-16-1109-bb-charla-limpieza-logistica]]); es hipótesis (A-02), no cifra. |
| 2. Usuario | 2 · Tres dolores | 30 | «Lo escuchamos ayer en la charla del sector. El planificador repone por calendario o por urgencia; bodega pierde de vista el insumo cuando sale; y nadie puede probar qué se entregó. Cuando se apura una entrega, se decide mirando kilómetros y no el costo real: personal, combustible, descarga y producto que se daña. Y el que paga es el cliente final: un hospital o una clínica sin insumos de limpieza.» | [[Historia y propuesta de valor]] |
| 3. Solución | 3 · Detectar → Decidir → Entregar → Probar | 30 | «IstpetDev une eso en una sola cadena. Detecta: revisa el inventario y avisa qué punto se quedará sin producto. Decide: compara cómo atenderlo con flota propia o con un operador contratado, sumando todo lo que cuesta servir. Entrega: arma la ruta respetando capacidad, horarios y jornada. Y prueba: cada caja viaja con QR hasta que el cliente la recibe, y si no hay señal, el registro se guarda en el teléfono y se sincroniza después.» | A-04, A-07, P-02, A-13 |
| 4. Diferenciación (IA + n8n) | 4 · Cómo funciona por dentro | 35 | «¿Qué es lo nuevo? Que no es otro inventario con alertas: la alerta termina en una decisión con costo y en una entrega comprobable. Lo calcula un motor con reglas, siempre igual con los mismos datos. Encima trabaja un agente de inteligencia artificial, orquestado con n8n, que lee esos resultados y le explica al planificador, en lenguaje simple, por qué conviene cada opción. La IA no inventa cifras: solo explica las del motor, y si falla, hay una explicación de respaldo. La persona aprueba. Y como n8n está separado del motor, podemos cambiar de modelo o agregar automatizaciones sin tocar el cálculo.» | A-08, A-11, A-12, A-10 |
| 5. Demo (a) | 5 · Captura de la alerta en vivo | 25 | «Así se ve: el sistema detecta que un punto entra en riesgo y lo avisa en pantalla, con días de cobertura y la cantidad sugerida para reponer. El planificador ya no espera la llamada: la alerta le dice qué punto atender primero.» | #318 (fusionado el 17-oct; hecho sin ensayo). Mostrar captura real del entorno de demo. |
| 5. Demo (b) | 6 · Panel A/B/C (lámina 5 de v3) | 50 | «Y esta es la decisión. A, flota propia: la más barata, 57,89 dólares, pero llega a tiempo solo al 83 % de los críticos. B llega al 100 %, pero cuesta un 20,7 % más. C, un operador contratado, también llega al 100 % y cuesta un 4,3 % menos que B. El motor recomienda C con 90,8 puntos y la IA explica por qué. La más barata no siempre es la mejor: pondera costo y entregas críticas a tiempo por igual.» | P-02, P-05 (#285) |
| 6. Validación | 7 · Construido / Probado / Falta | 35 | «Seamos claros con lo que hay. Construido: inventario con alertas, motor de rutas y costos, agente de IA con n8n y custodia con QR. Probado: el motor y la comparación A/B/C en un ensayo técnico con un escenario sintético de Quito de 40 puntos, y una corrida registrada del agente, incluida su caída al respaldo. Falta: ensayar el recorrido en teléfono sin señal, llevar la comparación a producción y medir con datos de una empresa real. Preferimos mostrarles lo que funciona de verdad que prometer lo que no hemos medido.» | P-04, P-05, A-03, A-11, A-12, A-15, #198 |
| 7. Impacto | 8 · Indicadores | 25 | «El impacto que buscamos: menos faltantes que se descubren tarde, entregas críticas a tiempo y saber cuánto cuesta realmente cada entrega. No prometemos un ahorro: lo mediremos en un piloto contra una línea base. Cada indicador está escrito para que lo entienda cualquier rol, de bodega a gerencia.» | A-21 |
| 8–9. Viabilidad y modelo | 9 · Modelo y riesgos | 15 | «No necesita hardware nuevo: funciona en la web y en el teléfono del conductor. Proponemos implementación inicial más suscripción. Y si faltan datos, el sistema dice “sin dato” y no inventa.» | A-22, [[Modelo de negocio y piloto]] |
| 10. Próximo paso | 10 · Hoja de ruta | 20 | «El siguiente paso: un piloto con una empresa de limpieza, una línea base e indicadores acordados. Buscamos ese aliado aquí. Detectar antes, decidir con el costo real y probar cada entrega. Gracias.» | — |

**Suma:** 15 + 30 + 30 + 35 + 25 + 50 + 35 + 25 + 15 + 20 = **280 s** (4:40). Si en el ensayo se pasa de 4:50, se recortan primero las frases añadidas al final de los bloques 2, 4 y 6 para volver a 240 s.

### Cambios de láminas respecto de v3

| v4 | Viene de v3 | Cambio |
|---|---|---|
| 1 | 1 · Portada | Añadir la pregunta del gancho como título grande; el nombre del equipo queda pequeño. |
| 2 | 2 · ¿Flota propia o contratada? | Nuevo título «El faltante se descubre cuando el cliente llama» y los tres dolores (planificación reactiva, bodega sin visibilidad, entrega sin prueba). |
| 3 | 3 · Comparar, Desglosar, Explicar, Decidir | Cambiar los cuatro pasos por **Detectar → Decidir → Entregar → Probar**. |
| 4 | — (nueva) | Esquema: datos → **motor** (calcula) → **agente IA en n8n** (explica) → **persona** (aprueba), con la nota «si la IA falla, explicación de respaldo». Al generar la lámina, usar el logo `materiales/assets/n8n-logo.png` en el bloque del agente. |
| 5 | 4 · Captura de rutas | Sustituir por la captura de la alerta en vivo (#318). La captura de rutas puede quedar de fondo o salir. |
| 6 | 5 · Panel A/B/C | Sin cambios. |
| 7 | 7 · Lo que podemos respaldar hoy | Rehacer como tres columnas: **Construido / Probado / Falta**. |
| 8 | 6 · Indicadores | Sin cambios de fondo. |
| 9 | 8 + 9 · Modelo y riesgos | Fusionar: modelo en una línea y los cuatro riesgos con su mitigación en una tabla corta. |
| 10 | 10 · Hoja de ruta | Cerrar con «Buscamos una empresa para el piloto». |

### Capturas y video

La lista de capturas (C1–C7) y el guion del video v4, plano por plano y con la ruta web de cada pantalla, están en el [guion del video de respaldo](../../specs/006-validacion-pitch-entrega/guion-video-respaldo.md). Las láminas 3, 4 y 5 de la v4 dependen de C1, C3, C5 y C6.

### Preguntas probables del jurado (nuevas)

- **«¿En qué se diferencian de un sistema de inventario con alertas?»** — La alerta no es el final: el sistema propone cómo atenderla con el costo completo y deja la cadena de custodia hasta la recepción.
- **«¿Qué hace exactamente la IA?»** — Explica. El cálculo es determinista y reproducible; el agente (DeepSeek, orquestado con n8n) traduce los resultados y no puede cambiar cifras. Si falla, hay un texto de respaldo y la decisión siempre es humana.
- **«¿Por qué n8n?»** — Separa la orquestación del agente del núcleo del sistema: se puede cambiar de modelo o de flujo sin tocar el motor, y cada corrida queda registrada.

---

## Versión v3 (respaldo)

Versión para el Reto 1 de Hackathon Expo Clean 2026. Sigue los siete bloques del Anexo 5.7. El guion maestro dura **280 s (4:40)**; el margen restante del turno anunciado de cinco minutos es operativo. Los cortes de **160 s** y **400 s** son metas de ensayo para turnos de tres y siete minutos, respectivamente. Son tiempos planificados, no resultados de ensayo. No hay pasadas registradas en [registro de ensayos](../../specs/006-validacion-pitch-entrega/registro-ensayos.md).

## Norte y regla de evidencia

El problema elegido es la planificación diaria: cuándo atender con flota propia y cuándo evaluar un operador contratado, considerando el costo total de servir. La frase central es: **«compara rutas con flota propia y contratada, suma todo lo que cuesta servir… La IA explica; el planificador decide»**. La primera parte expresa el objetivo del módulo, no una capacidad integrada de punta a punta.

El panel A/B/C se presenta con la **lámina 5 de `pitch-v3`**, con cifras del motor en el **ensayo técnico local de #285** (PR #295, `1585ab7`; escenario sintético `demo-quito-planificacion`; `evidencia/ensayo-planificacion-local-20261016/02-suggest-pitch-abc.json`): **A** flota propia USD 57,89, 5 de 6 críticos a tiempo, puntaje 81,7; **B** propia con críticos primero USD 69,89, 6 de 6, puntaje 89,1; **C** operador contratado USD 66,90, 6 de 6, puntaje 90,8, **recomendada por el motor**. La lámina agrega porcentajes derivados de esos totales para que se entiendan de un vistazo: críticos a tiempo 83 % / 100 % / 100 %; costo frente a A: B +20,7 %, C +15,6 %; C cuesta 4,3 % menos que B. Se leen totales, porcentajes, puntaje y recomendación: el desglose por componente de esa corrida no suma el total y queda fuera de la lámina hasta corregirse (#276). Decir «ensayo técnico» y «escenario sintético»; no decir que corre en producción hasta que #198 aplique la configuración. Las cifras de `demo-base` de #240 (USD 19,94) ya no se usan en esta lámina.

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
| 3. Prototipo / demo (b) | **5 · Panel A / B / C** · 3 clics | 65 s | Técnica → Negocio | Clic 1: filas Críticos a tiempo, Costo frente a A y Puntaje. Clic 2: costo total → marco dorado en C y recomendación del motor. Clic 3: globo de Chillo (no se lee), pausa de 1 s. Leer solo totales, críticos y puntaje; no leer desglose por componente. | «En el panel, A es la flota propia; B, la flota propia con los críticos primero; C, un operador contratado. Son cifras del motor en un ensayo técnico con nuestro escenario sintético de Quito. A es la más barata, 57,89 dólares, pero solo llega a tiempo al 83 % de los críticos. B llega al 100 %, pero cuesta un 20,7 % más. C también llega al 100 % y cuesta un 15,6 % más que A: un 4,3 % menos que B. Por eso el motor recomienda C, con 90,8 puntos: pondera costo y críticos a tiempo por igual, más pedidos pendientes y riesgo. La más barata no siempre es la mejor. Son costos estimados, no mediciones de una empresa. La IA explica la recomendación; el planificador decide qué opción llevar a la operación.» |
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
| Demo A/B/C | 55 s | Web → Técnica | «En nuestro escenario sintético de Quito, el motor compara tres opciones en un ensayo técnico: A, flota propia, la más barata, pero llega a tiempo al 83 % de los críticos; B llega al 100 % con un 20,7 % más de costo; C, operador contratado, también llega al 100 % y cuesta un 4,3 % menos que B. El motor recomienda C, con 90,8 puntos. La más barata no siempre es la mejor. Son costos estimados, no mediciones de una empresa.» |
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
| 3. Prototipo / demo | 130 s | Web → Técnica → Web | «Abrimos `demo-quito-planificacion`. Es un escenario sintético de Quito con referencias geográficas públicas y datos de operación construidos para el ejercicio; no contiene clientes ni viajes medidos. En el panel A/B/C, A representa flota propia; B, flota propia con los críticos primero; C, un operador contratado. En un ensayo técnico con este escenario, el motor calcula las tres: A cuesta 57,89 dólares pero llega a tiempo al 83 % de los críticos; B llega al 100 % y cuesta un 20,7 % más; C, el operador contratado, también llega al 100 %, cuesta un 15,6 % más que A y un 4,3 % menos que B. El motor recomienda C, con 90,8 puntos, porque pondera costo y críticos a tiempo por igual, más pendientes y riesgo; su justificación queda visible. La más barata no siempre es la mejor. Son costos estimados, no mediciones de una empresa. El motor también tiene pruebas para factores de hora pico por franja configurada. Son supuestos de escenario; no equivalen a tráfico en vivo, y el recorrido en esta interfaz no está ensayado. La decisión final de operación corresponde al planificador. La IA explica los datos calculados y puede usar una explicación determinista de respaldo cuando falla el agente, según el registro técnico.» |
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

