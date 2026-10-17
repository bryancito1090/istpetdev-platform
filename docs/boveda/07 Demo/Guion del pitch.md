---
tipo: demo
estado: propuesta
actualizado: 2026-10-17
tags: [demo, validacion, istpetdev, planificacion]
---

# Guion del pitch — T4

## Versión v8 (17-oct): acceso del jurado — 10 láminas, meta 4:50

Por solicitud de Bryan, la v8 conserva las nueve láminas de la v7 y añade una última con un QR grande a `https://istpetdev.doicela.dev/`, una cuenta dedicada al jurado con rol Auditor y una explicación de seguridad en diez líneas. El [guion v8](../../specs/006-validacion-pitch-entrega/guion-pitch-v8.md) es la fuente de esta versión; incluye tiempos propuestos y las instrucciones para regenerar el editable y el PDF. El inicio de sesión real y los permisos se registran en la [evidencia de acceso](../../specs/006-validacion-pitch-entrega/evidencia/pitch-v8-acceso-jurado.md).

La copia de revisión `pitch-v8-revision.*` omite la contraseña y puede versionarse. La copia local `pitch-v8.*`, destinada al jurado, la muestra en la última lámina y queda fuera de Git. `Entregable/` conserva la v7 sin credenciales. La meta de 4:50 aún requiere ensayo cronometrado; no equivale a congelamiento ni entrega oficial.

## Propuesta v7 (17-oct): un solo caso, de la alerta a la decisión — 9 láminas, 4:35

**v7 = v6 + cambios de la revisión del 17-oct:** (1) portada con el rótulo del reto «OPTIMIZACIÓN Y TRAZABILIDAD LOGÍSTICA» y el globo de Chillo que dice que el motor calcula y la IA explica (antes decía «usamos IA para planificar rutas», que contradecía la tesis); (2) el problema en rojo: banda «El problema: hoy se decide mirando kilómetros» en la lámina 2 y filete rojo en la 6; (3) la columna «Falta» pasa a **«Siguiente fase»**: se resalta usarlo en una empresa real con sus datos y **se mantiene el ensayo del recorrido sin señal en un teléfono real**, porque sigue sin hacerse (el flujo móvil de 3 roles en [[Entregable Solucion y propuesta de valor]] muestra la captura concurrente, no el corte de señal; ver matriz A-15); (4) los indicadores alimentan informes ejecutivos (sección Reportes); (5) cierre con «Su empresa». La v6 (PR #359) queda como antecedente de esta versión.

**Estado: propuesta para aprobar; la v5 sigue vigente hasta entonces (en `Entregable/` ya está la v7 por pedido del equipo).** Cambia la historia, no el sistema: mismas capturas, mismas cifras, misma matriz de afirmaciones. Corrige tres fallas de la v5: (1) de la lámina 7 a la 8 se saltaba a las cifras A/B/C sin explicar qué eran; (2) se contaba un recorrido por módulos en vez de una razón para elegirnos; (3) la innovación se defendía («no es otro inventario con alertas») en lugar de afirmarse.

**Tesis:** una buena cotización de limpieza suma el costo completo del servicio. Nosotros hacemos lo mismo con la entrega: ponerle costo real antes de decidir y dejar la prueba. Conecta con la charla de rentabilidad del 17-oct ([[2026-10-17-1050-bb-charla-pitch-jurado-rentabilidad]]).

**Reglas que se mantienen:** el caso es el escenario sintético de Quito (P-04, P-05) y se dice; no se nombra ningún hospital real; no se afirma que «nadie» cotiza la entrega (no está validado, A-29/A-33), solo lo que escuchamos (A-02, hipótesis); ninguna cifra fuera de P-02; sin ahorro prometido (A-21).

| # | Lámina | Voz | s | Texto para ensayar | Respaldo |
|---|---|---|---:|---|---|
| 1 | Portada con la pregunta y el rótulo del reto | Joseph | 15 | «¿Cómo se entera hoy una empresa de limpieza de que a un hospital le falta desinfectante? Cuando el hospital llama. Ese día ya es tarde.» | A-02 (hipótesis, no cifra). Globo de Chillo (clic 1, no se lee): «el motor calcula y la IA explica» (A-11: la IA explica, no calcula). |
| 2 | **El caso**: cuatro puntos, tres maneras de atenderlos | Joseph | 40 | «Pongámonos en un día cualquiera, en nuestro escenario de Quito, que es sintético. Cuatro pedidos, con seis entregas críticas, entre hospitales y otros clientes, necesitan insumos de limpieza hoy. Hay tres maneras de atenderlos. A: un solo viaje con flota propia. B: también flota propia, pero los críticos primero, en el primer turno. C: contratar un operador externo. Hoy esa decisión se toma mirando kilómetros. Pero lo que cuesta de verdad es el personal, el combustible, la descarga y el producto que se daña. Y lo que importa: que lo crítico llegue a tiempo.» | P-02, P-04, P-05 (4 pedidos y 6 entregas críticas del ensayo #285; A = «Viaje Multi-Recogida Consolidado», B = «Dos Entregas Escalonadas», C = operador contratado). Diagrama en la lámina 2. Lo de «mirando kilómetros» es lo que escuchamos (A-02). Banda roja «El problema…» (clic 1): decir «el problema» al llegar a «mirando kilómetros». |
| 3 | **La alerta**: monitoreo de cobertura (`alertas.png`) | Alexander | 15 | «Todo empieza antes de que el cliente llame: el sistema revisa inventario y consumo, y avisa qué punto entra en riesgo y cuánto reponer.» | A-04. Sin cifras de la captura. |
| 4 | **La decisión**: A / B / C, solo tres datos por opción | Alexander | 55 | «Y esta es la decisión, con costo completo y no solo kilómetros. A es la más barata: 57,89 dólares. Pero solo llega a tiempo a 5 de las 6 entregas críticas. B y C llegan al 100 %. Entre ellas, C, el operador contratado, cuesta 66,90 contra 69,89 de B. El motor recomienda C, con 90,8 puntos, y la inteligencia artificial le explica al planificador por qué, en palabras simples. La más barata no fue la mejor. Y la persona decide.» | P-02: A 57,89 / B 69,89 / C 66,90, puntaje 90,80; 5 de 6 críticos en A (83 %). Ensayo técnico sintético, no producción (#198). A-10, A-11. |
| 5 | **La ruta y la prueba**: ruta, seguimiento, QR y novedades | Alexander | 30 | «Elegida la opción, el sistema arma la ruta con capacidad, horarios y jornada, y el planificador la aprueba. En el camino queda registro: cada caja viaja con QR y los daños o errores de rotulado se anotan con su motivo. Así, cuando alguien pregunta qué se entregó, hay respuesta.» | A-07, A-13, A-16; P-08 en curso: decir «errores de rotulado» solo si la captura `novedades.png` los muestra. No decir «ensayado en teléfono». |
| 6 | **Por qué es nuevo** | Bryan | 35 | «¿Qué cambia? Lo que escuchamos en el sector es que se repone por calendario o por urgencia, y la entrega se decide por kilómetros. Nosotros hacemos con la entrega lo que se hace con una buena cotización: ponerle el costo completo antes de decidir. Un motor de reglas calcula, siempre igual con los mismos datos. La inteligencia artificial explica, no inventa cifras, y si falla hay una explicación de respaldo. La alerta no se queda en alerta: termina en una decisión con costo y en una entrega comprobable.» | A-02, A-06, A-08, A-11, A-12. «n8n» no se dice en voz alta; queda en la lámina. |
| 7 | **Construido / Ensayado / Siguiente fase** | Anthony | 25 | «Construido: de la alerta a la prueba de entrega. Ensayado: la decisión A, B y C en un ensayo técnico sobre un escenario sintético de Quito de 40 puntos, y una corrida del agente de inteligencia artificial, incluida su caída al respaldo. Siguiente fase: usarlo en una empresa real, cargar sus datos y medir contra su línea base, probar el recorrido sin señal en un teléfono real, y llevar la comparación A, B y C a producción.» | A-03, A-11, A-12, A-15, #198. El recorrido sin señal en teléfono real sigue pendiente: se dice como siguiente fase, no como hecho. Etiqueta «Ensayado», no «Probado»: es un ensayo sintético. |
| 8 | **Impacto y modelo** | Jorge | 40 | «En un piloto queremos medir tres cosas: menos faltantes descubiertos tarde, más entregas críticas a tiempo y cuánto cuesta realmente cada entrega. No prometemos un ahorro: lo medimos contra una línea base. No necesita hardware nuevo: funciona en la web y en el teléfono del conductor. Proponemos implementación inicial más suscripción, que validaremos. Y si falta un dato, el sistema dice “sin dato”. Esos indicadores alimentan informes ejecutivos: ya tenemos la sección Reportes.» | A-21, A-26, A-30, A-31; pantalla Reportes (`reportes.page.ts`, informe con OTIF, cobertura y costo por km; imprimir o exportar CSV/JSON). Decir «ya tenemos la sección», no «ya mide estos tres». |
| 9 | **Cierre** | Joseph | 20 | «Una buena cotización de limpieza suma el costo completo. Nosotros hacemos lo mismo con la entrega: la decidimos con su costo real, antes de hacerla, y dejamos la prueba. Buscamos una empresa de limpieza para un piloto: con su línea base, indicadores acordados y sus datos. Gracias.» | A-32. Sin cliente comprometido. |

**Suma:** 15 + 40 + 15 + 55 + 30 + 35 + 25 + 40 + 20 = **275 s** (4:35), con 25 s de margen sobre los 5 min. Si el ensayo se pasa, se recorta primero la lámina 6, luego la 8.

**Qué se quita respecto de la v5:** las láminas 5, 6 y 7 sueltas (pasan a ser 3 y 5 dentro del caso), el recorrido «Detectar → Decidir → Entregar → Probar» como lámina propia (queda implícito en la secuencia 3 → 4 → 5), la lámina 11 aparte (se fusiona con 8) y las frases defensivas.

**Láminas v7:** [`pitch-v7.pptx`](../../specs/006-validacion-pitch-entrega/materiales/pitch-v7.pptx) y [`pitch-v7.pdf`](../../specs/006-validacion-pitch-entrega/materiales/pitch-v7.pdf) (PNG en `render-v7/`), generadas con `materiales/generar_v7.py`; copias en `Entregable/7-Pitch.*`. La v6 sigue en `generar_v6.py`. La lámina 2 es el diagrama del caso; el panel A/B/C de la 4 es el de la v5 con título nuevo. `Entregable/` contiene la v7.

**Pendiente (equipo):** ensayar los 275 s en voz alta y revisar el guion del video 9, que sigue el recorrido de la v5.

---

## Versión vigente v5 (17-oct): 12 láminas, 4:40

Reemplaza a la v4 (conservada más abajo como antecedente). Los PDF y el PPTX del pitch (`7-Pitch`) salen de `materiales/generar_v5.py`. Mismo texto que el PDF [`8-Guion-del-pitch.pdf`](../Entregable/8-Guion-del-pitch.pdf) de la carpeta `Entregable`; **esta nota es la fuente**: se edita aquí y se regenera el PDF. Las láminas 5, 6 y 7 usan capturas reales (cobertura, rutas, novedades) y sustituyen a la primera versión v5 de `develop`, que reutilizaba imágenes ilustrativas de `entregable-2/img/` (consola sin alertas activas, mapa de Ambato, escáner QR). Estar fusionado en `develop` no prueba despliegue ni ensayo en teléfono.

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

### Guion v5 — 280 s (turno de 5 min de exposición + 1 min de preguntas; 20 s de margen)

| # (mentor) | Lámina vigente | s | Texto para ensayar | Respaldo |
|---|---|---:|---|---|
| 1. Problema (gancho) | 1 · Portada con la pregunta | 15 | «¿Cómo se entera hoy una empresa de limpieza de que a un hospital le falta desinfectante? Cuando el hospital llama. Ese día ya es tarde.» | Dolor recogido en la charla del sector ([[2026-10-16-1109-bb-charla-limpieza-logistica]]); es hipótesis (A-02), no cifra. |
| 2. Usuario | 2 · Tres dolores | 30 | «Lo escuchamos ayer en la charla del sector. El planificador repone por calendario o por urgencia; bodega pierde de vista el insumo cuando sale; y nadie puede probar qué se entregó. Cuando se apura una entrega, se decide mirando kilómetros y no el costo real: personal, combustible, descarga y producto que se daña. Y el que paga es el cliente final: un hospital o una clínica sin insumos de limpieza.» | [[Historia y propuesta de valor]] |
| 3. Solución | 3 · Detectar → Decidir → Entregar → Probar | 30 | «IstpetDev une eso en una sola cadena. Detecta: revisa el inventario y avisa qué punto se quedará sin producto. Decide: compara cómo atenderlo con flota propia o con un operador contratado, sumando todo lo que cuesta servir. Entrega: arma la ruta respetando capacidad, horarios y jornada. Y prueba: cada caja viaja con QR. Bodega y conductor pueden guardar registros sin señal y sincronizarlos después; la recepción final del cliente requiere conexión.» | A-04, A-07, P-02, A-13 |
| 4. Diferenciación (IA + n8n) | 4 · Cómo funciona por dentro | 35 | «¿Qué es lo nuevo? Que no es otro inventario con alertas: la alerta termina en una decisión con costo y en una entrega comprobable. Lo calcula un motor con reglas, siempre igual con los mismos datos. Encima trabaja un agente de inteligencia artificial, orquestado con n8n, que lee esos resultados y le explica al planificador, en lenguaje simple, por qué conviene cada opción. La IA no inventa cifras: solo explica las del motor, y si falla, hay una explicación de respaldo. La persona aprueba. Y como n8n está separado del motor, podemos cambiar de modelo o agregar automatizaciones sin tocar el cálculo.» | A-08, A-11, A-12, A-10 |
| 5. Demo (a1) | 5 · Consola de alertas y rutas | 9 | «Así se ve: una consola reúne el estado del stock, las alertas y las rutas para revisar qué requiere atención.» | Captura existente, rol Auditor; cero alertas activas. Entrada automática. |
| 5. Demo (a2) | 6 · Mapa de rutas | 9 | «En el mapa vemos los recorridos y sus paradas. Esta vista usa un escenario sintético de Ambato, distinto de la comparación de Quito.» | Captura de Ambato; no atribuirle las cifras A/B/C. Entrada automática. |
| 5. Demo (a3) | 7 · Escáner QR | 7 | «Y el QR identifica cada bulto para consultar su trazabilidad. Aquí mostramos el escáner.» | Captura de identificación; no afirmar recepción ni ensayo offline. Entrada automática. |
| 5. Demo (b) | 8 · Panel A/B/C | 50 | «Y esta es la decisión. A, flota propia: la más barata, 57,89 dólares, pero llega a tiempo solo al 83 % de los críticos. B llega al 100 %, pero cuesta un 20,7 % más. C, un operador contratado, también llega al 100 % y cuesta un 4,3 % menos que B. El motor recomienda C con 90,8 puntos y la IA explica por qué. La más barata no siempre es la mejor: pondera costo y entregas críticas a tiempo por igual.» | P-02, P-05 (#285) |
| 6. Validación | 9 · Construido / Probado / Falta | 35 | «Seamos claros con lo que hay. Construido: inventario con alertas, motor de rutas y costos, agente de IA con n8n y custodia con QR. Probado: el motor y la comparación A/B/C en un ensayo técnico con un escenario sintético de Quito de 40 puntos, y una corrida registrada del agente, incluida su caída al respaldo. Falta: ensayar el recorrido en teléfono sin señal, llevar la comparación a producción y medir con datos de una empresa real. Preferimos mostrarles lo que funciona de verdad que prometer lo que no hemos medido.» | P-04, P-05, A-03, A-11, A-12, A-15, #198 |
| 7. Impacto | 10 · Indicadores | 25 | «El impacto que buscamos: menos faltantes que se descubren tarde, entregas críticas a tiempo y saber cuánto cuesta realmente cada entrega. No prometemos un ahorro: lo mediremos en un piloto contra una línea base. Cada indicador está escrito para que lo entienda cualquier rol, de bodega a gerencia.» | A-21 |
| 8–9. Viabilidad y modelo | 11 · Modelo y riesgos | 15 | «No necesita hardware nuevo: funciona en la web y en el teléfono del conductor. Proponemos implementación inicial más suscripción. Y si faltan datos, el sistema dice “sin dato” y no inventa.» | A-22, [[Modelo de negocio y piloto]] |
| 10. Próximo paso | 12 · Hoja de ruta | 20 | «El siguiente paso: un piloto con una empresa de limpieza, una línea base e indicadores acordados. Buscamos ese aliado aquí. Detectar antes, decidir con el costo real y probar cada entrega. Gracias.» | — |

**Suma:** 15 + 30 + 30 + 35 + (9 + 9 + 7) + 50 + 35 + 25 + 15 + 20 = **280 s** (4:40). Si en el ensayo se pasa de 4:50, se recortan primero las frases añadidas al final de los bloques 2, 4 y 6 para volver a la meta de 280 s.

Las tres capturas entran automáticamente, sin clics internos. Los dos avances adicionales se incluyen en los 25 s del bloque; la duración de 4:40 sigue siendo una meta pendiente de ensayo.

### Correspondencia v4 → v5

| v4 | v5 | Cambio |
|---|---|---|
| 1–4 | 1–4 | Mismo argumento y diseño. |
| 5 | 5–7 | Tres capturas con descripción breve; 9 + 9 + 7 s. |
| 6–10 | 8–12 | Mismo contenido; numeración desplazada dos posiciones. |

### Histórico: cambios de v3 a v4

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

La lista de capturas (C1–C7) y el guion del video v4, plano por plano y con la ruta web de cada pantalla, están en el [guion del video de respaldo](../../specs/006-validacion-pitch-entrega/guion-video-respaldo.md). La v5 usa las tres capturas existentes indicadas arriba. C1–C7 siguen siendo la lista de tomas deseadas para renovar el material y grabar el video; las láminas 3 y 4 son esquemas y no dependen de esas imágenes.

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

