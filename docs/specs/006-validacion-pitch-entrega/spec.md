---
id: 006
titulo: Validación, pitch y paquete de entrega
estado: borrador
creado: 2026-10-12
actualizado: 2026-10-12
aprobado_por: PENDIENTE
depende_de: [001, 002, 003, 004, 005]
boveda: ["Evaluacion y entregables", "Cronograma oficial", "Consultas para la organizacion", "Guion del pitch", "Checklist y contingencias", "Plan de validacion", "Modelo de negocio y piloto", "Registro de trabajo previo y del evento"]
---

# 006 — Validación, pitch y paquete de entrega

## Contexto

El jurado evalúa siete criterios; cinco de sus siete integrantes no son perfiles técnicos y tres representan al sector de limpieza. El recorrido debe ser comprensible sin vocabulario de arquitectura y verificable mediante lo que realmente entreguen 001–005. La duración oficial y los formatos de entrega siguen abiertos en O-04/O-06. El 17-oct las presentaciones comienzan a las 16:15, después de la entrega de materiales de 16:00–16:15.

Este spec produce documentos, entrevistas, ensayos y un paquete, no software. Los specs 001–005 son propuestas pendientes de aprobación y ejecución; sus CE describen capacidades esperadas, no hechos. El pitch solo convierte una capacidad en afirmación después de enlazar la ejecución correspondiente. La [bóveda](../../boveda/README.md) prevalece sobre este spec.

## Objetivo y aporte a la nota

Presentar un relato de una caja destinada a un hospital que sea claro, cronometrado, trazable y honesto; entregar los siete componentes oficiales con evidencia y un piloto propuesto.

| Criterio oficial | Peso | Evidencia que prepara 006 |
|---|---:|---|
| Comprensión del problema | 15% | Conversaciones de feria con consentimiento, objeciones y ajustes registrados |
| Innovación | 15% | Diferencia operativa explicada con CE ejecutados de 002–004 |
| Viabilidad | 15% | Recursos, riesgos, límites y ficha de piloto, separados de costos no verificados |
| Impacto | 15% | Rango simulado, tres seeds y escenario adverso de 005 con ZIP importado y manifiesto |
| Prototipo/demo/simulación | 20% | Recorrido en vivo o respaldo rotulado y pruebas del teléfono/QR |
| Calidad de presentación | 10% | Guiones 3/5/7, materiales legibles y ensayos medidos |
| Proceso y equipo | 10% | Cinco papeles visibles, mentorías, tag previo y cambios del evento |

## Historias de usuario

### H1 — Jurado entiende y verifica (P1)

**Como** jurado del sector, **quiero** ver problema, decisión, caja y resultado con su procedencia, **para** valorar el prototipo sin asumir funciones futuras.

**Aceptación:** el guion nombra las siete partes del Anexo 5.7 en orden; cada cifra tiene enlace a ejecución o se omite; cada escena tiene sustitución ensayada.

### H2 — Expositor valida en la feria (P1)

**Como** expositor, **quiero** responder voluntariamente preguntas breves y reaccionar al prototipo, **para** corregir supuestos sin que mis palabras se conviertan en aval comercial.

**Aceptación:** se pide consentimiento antes de registrar; hay al menos tres conversaciones documentadas antes del 17-oct 12:30 y al menos una reacción al prototipo; si no se logran, el pitch lo declara pendiente.

### H3 — Equipo entrega y responde (P1)

**Como** equipo, **quiero** cinco papeles claros, paquete verificable y banco de preguntas, **para** sostener la demo y las respuestas bajo el tiempo oficial.

**Aceptación:** hay diez ensayos completos consecutivos sin fallo, los dos últimos sin wifi del recinto, cada uno bajo el tiempo oficial menos 20 s; veinte respuestas difíciles se ensayan en 30 s o menos. Los nombres de roles quedan [PENDIENTE: asignación del equipo].

## Requisitos del entregable

| ID | Requisito | Traza |
|---|---|---|
| ER-01 | Guion maestro de 5 min y versiones 3/7 con los siete bloques oficiales en orden, cada una a tiempo oficial −20 s | O-04, Anexo 5.7 |
| ER-02 | Kit previo generado con IA y revisado por el equipo en unos 15 min: entrevista, esqueleto de pitch, negocio, veinte preguntas y fuente SERCOP | D2, B-45 |
| ER-03 | Dos personas entrevistan expositores el 16 en las pausas y vuelven con prototipo el 17 de 11:00–12:30; evitan JIMCORPSERVI/FEDELIMP y LACIF como cautela del equipo | D2, B-46 |
| ER-04 | Cada entrevista conserva consentimiento, rol/organización si se ofrece, pregunta, respuesta fiel, objeción, cambio y autorización separada para cita atribuible/anónima | Convenciones y estados, Plantilla evidencia |
| ER-05 | Cinco integrantes aparecen con los papeles narración, web, móvil, técnica y negocio; suplencias se ensayan sin asignar nombres por suposición | Matriz oficial, Proceso y equipo |
| ER-06 | Guion y diapositivas usan `[CITA DE LA FERIA]`, `[RESULTADO DEL SIMULADOR]`, `[PRECIO]` hasta verificar cada hueco; ningún número de resultado sin ejecución enlazada | Convenciones y estados, 005 CE-04/06/09 |
| ER-07 | Diez ensayos completos consecutivos sin fallo; reinicia conteo ante fallo; dos últimos sin wifi; se registra versión, duración, dispositivo, incidencia y enlace | Métricas de listo para ganar |
| ER-08 | Video de respaldo se graba el 15-oct tras un ensayo funcional y se etiqueta con fecha/commit; materiales se congelan el 17-oct a las 15:45 | D8, cronograma |
| ER-09 | Tag `pre-evento` identifica último commit previo al 16; registro diferencia preparación y cambios del evento | G8, O-01/02 |
| ER-10 | Paquete incluye los siete entregables de la sección 9 del manual, manifiesto de evidencias, fuente de cada resultado, versión, recursos/autoría y copia local; se adapta al formato confirmado en O-06 | Evaluacion y entregables |
| ER-11 | Piloto de 4 semanas con una bodega y grupo acordado de puntos; ficha separa hipótesis, métricas, recursos, criterio de avance y precio por validar | Modelo de negocio y piloto |

## Contratos documentales

- **Hoja de entrevista:** fecha/hora y contexto; consentimiento para notas y para cita por separado; cinco preguntas; respuesta textual o paráfrasis marcada; objeción; reacción observada; decisión de producto; enlace a evidencia anonimizada. Sin firma, teléfono, foto ni audio por defecto.
- **Tarjeta de afirmación:** frase exacta → spec/CE → ejecución enlazada (commit, entorno, fixture, fecha, resultado) → estado `usable`/`retirar` → plan B verbal. Un CE escrito sin ejecución no habilita `usable`.
- **Ficha de piloto:** comprador y usuario por validar; alcance; semanas 1–4; baseline antes de intervención; cuatro KPI con numerador/denominador; recursos/costos con fuente; precio `[PENDIENTE: disposición de pago y costo de soporte]`; decisión final de continuar/cambiar/parar.
- **Manifiesto final:** archivo, versión/hash, origen, etiqueta `en vivo`/`grabado`/`simulado`/`propuesto`, vínculo a evidencia y responsable de revisión. No se añaden datos personales ni credenciales.

## Guardarraíles aplicados

G1–G7: el relato del hospital se etiqueta como caso sintético, el fixture vivo no se confunde con la escala nacional simulada, unidades y supuestos aparecen junto a resultados, y el dataset sorpresa se menciona solo con su E2E aprobado. G8: tag y registro con doble columna preparación/evento. La hoja del jurado no presenta la heurística de 002 como óptimo global ni el LLM como calculadora o aprobador.

## Comprobación cruzada de afirmaciones del pitch

Cada fila es **condicional**: la frase se permite únicamente después de enlazar una ejecución aprobada del CE citado. Los CE documentados de 001–005 aún no son evidencia. Si falla un CE, se usa el plan B de la última columna y se retira el verbo o cifra correspondiente.

| Afirmación candidata | Spec y CE que deben sostenerla | Plan B si no está listo |
|---|---|---|
| «En hospitales públicos, el proveedor de limpieza está obligado a mantener los químicos y los insumos de reposición» (bloque Problema) | Fuente documental, no CE: [SERCOP limpieza hospitalaria y sanciones](../../boveda/99%20Fuentes/SERCOP%20limpieza%20hospitalaria%20y%20sanciones.md), con cita textual verificada | No requiere plan B; nunca añadir un monto de multa ni un «costo de quiebre» a esta frase |
| «Una carga nueva cambia el escenario sin reprogramar» | 001 CE-02/03; 005 CE-01 para panel | Mostrar fixture conocido; no hablar de adaptación a dataset sorpresa. |
| «El hospital tiene riesgo antes del faltante» | 002 CE-01; lectura en 005 CE-01/02 | Describir hipótesis con captura rotulada, sin fecha ni cobertura numérica. |
| «La ruta considera capacidad y horarios» | 002 CE-02/03 | Mostrar orden propuesto, no llamarlo ruta factible. |
| «Ante el cierre de vía, una decisión se ejecuta por política y otra espera aprobación» | 002 CE-04; 003 CE-03 para explicación/aviso; 005 CE-03 para columnas | Mostrar solo replanificación determinista que pase 002, o retirar la escena. No decir que el LLM aprueba ni calcula. |
| «La IA detecta y propone» | 003 CE-01/03, cifras contrastadas con 002 CE-07 | Mostrar tarjeta de reglas del 002 y decir que el agente está pendiente. |
| «La caja se despacha con lote y custodia» | 004 CE-01/10 | Mostrar etiqueta como propuesta; no simular despacho real. |
| «El conductor recibe sin señal y se sincroniza una sola vez» | 004 CE-02/03/06 | Recepción en línea si se verificó; video del recorrido solo con etiqueta de grabación. |
| «El jurado escanea la caja y ve hitos reales» | 004 CE-07/09; acceso externo probado en el lugar | Escaneo por teléfono del equipo en hotspot si está probado; si no, captura rotulada. |
| «El tablero muestra el estado y la aprobación» | 005 CE-01/02/03; 002 CE-04 | Lista de paradas o vista de API verificada, sin cifras fijas del widget previo. |
| «El rango simulado de [RESULTADO DEL SIMULADOR] compara 90 días, tres seeds y un escenario adverso» | 005 CE-04/05/06/09/10; ZIP importado con FR-21 | Explicar metodología y dejar resultado pendiente; ninguna cifra de ahorro. |
| «Quiebres evitados y nuevos frente a reposición reactiva y calendario fijo» | 005 CE-04/05/06/10 | Presentar las dos políticas como diseño congelado antes de resultados. |
| «El prototipo reinicia su escena sin perder la simulación» | 001 CE-01; 004 CE-10; 005 CE-08 | Usar video grabado y explicar qué parte fue grabada. |
| «Una persona del sector reconoció este problema» | 006 CE-02 y consentimiento de cita de ER-04 | Decir «hipótesis del equipo»; sin cita ni atribución. |
| «Proponemos un piloto de cuatro semanas con una empresa por acordar» | Ficha ER-11 y 006 CE-02 para ajustar hipótesis con feria; un acuerdo comercial requiere evidencia posterior | Presentar **propuesta** de piloto; `[PRECIO]` fuera de discurso. Nunca decir cliente comprometido. |

No se incluye «ahorro real», «multa evitada», «ruta óptima», «IA replanifica sola» ni «operación nacional en vivo»: 005 las excluye, 002 usa una heurística y D6 reserva la escala nacional al simulador. El rango de una simulación no se convierte en promesa contractual.

## Fuera de alcance

- Implementar 001–005, construir una app de slides o afirmar que sus CE ya pasan.
- Piloto ejecutado, validación estadísticamente representativa, ahorro observado, precio aceptado y multa evitada.
- Prometer ML entrenado, IA que aprueba por sí sola, varias bodegas o más de dos vehículos en vivo, chat, app nativa, RBAC administrable o escalado multinodo.
- Entrevistar organizaciones con representantes en el jurado; usar mentoría como validación comercial o garantía de puntuación.

## Contradicciones con la bóveda

> **⚠️ Contradicción detectada** — Hecho nuevo: el guion usa `demo-region` de unos 15 puntos en vivo y la escala de unos 80 solo en la simulación. · Hecho establecido: el texto base de [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md) dice que una empresa atiende 80 puntos antes de distinguir escena y simulación. · Impacto: el jurado podría creer que ve operación nacional en vivo. · Opciones: aclarar el contexto en voz y diapositiva; mantener la frase ambigua. · Propuesta: aclarar y dejar el texto anterior superado por el complemento fechado del 006.

> **⚠️ Contradicción detectada** — Hecho nuevo: el 006 promete una duración de tiempo oficial menos 20 s y tres cortes ensayados. · Hecho establecido: [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md) fija un recorrido de 180 s mientras O-04 sigue pendiente. · Impacto: el guion previo podría exceder un turno de 3 min. · Opciones: mantener 180 s; guion maestro de 5 min con cortes de 160/280/400 s; esperar O-04. · Propuesta: segunda opción y sustituir la versión efectiva al resolver O-04.

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Entrevistas sin consentimiento o con cita no autorizada | Media | Alto | Hoja con casillas separadas y anonimización; retirar cita |
| Turno o formato distintos de los previstos | Alta | Medio | O-04/O-06, tres cortes, PDF local y control de versiones |
| CE de una escena sin ejecución al congelar | Alta | Alto | Tarjetas de afirmación; retirada automática y plan B ensayado |
| Simulación sin ZIP verificable | Media | Alto | No mostrar ahorro; explicar método y límite; priorizar 005 |
| Cinco integrantes interfieren en una escena breve | Media | Medio | Un relevo verbal por papel, puestos fijos y ensayo con cronómetro |

## Criterios de éxito

- CE-01: kit previo contiene los cinco componentes de ER-02, con revisión de equipo registrada; ninguna respuesta/cifra inventada.
- CE-02: al 17-oct 12:30 hay ≥3 hojas consentidas, ≥1 reacción al prototipo y vínculo de al menos una objeción a una decisión o pendiente.
- CE-03: versiones 3/5/7 conservan orden de siete bloques, cinco papeles y cierre; el corte elegido dura ≤ tiempo confirmado −20 s.
- CE-04: diez ensayos completos consecutivos pasan sin fallo; dos últimos sin wifi del recinto, cronómetro y versión registrados.
- CE-05: veinte preguntas tienen borrador de ≤30 s y se prueban en ronda cronometrada con límites honestos.
- CE-06: toda afirmación del pitch exhibida en la versión congelada enlaza CE y ejecución o aparece como propuesta/pendiente; auditoría sin números de resultado huérfanos.
- CE-07: paquete de siete entregables, manifiesto y copia local queda congelado a las 15:45 y listo para la ventana 16:00–16:15; recepción oficial se registra cuando ocurra.
- CE-08: tag `pre-evento`, inventario previo y cambios del evento trazan el origen; video del 15-oct está rotulado como grabado y tiene commit/ensayo funcional.

## Preguntas abiertas

- PA-01: nombres para los cinco papeles y suplentes. Decide: equipo; respuesta actual «pendiente».
- PA-02: O-04/O-06: tiempo, turno, preguntas y formato/plataforma de entrega. Decide: organización; registrar fuente.
- PA-03: Deciders que aprueban el 006 y la ficha de piloto. Decide: equipo.
- PA-04: condiciones del piloto, contraparte, costos y precio. Decide: empresa interesada junto con equipo, después de validación.
