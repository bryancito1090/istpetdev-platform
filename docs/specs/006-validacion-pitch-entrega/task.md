# 006 — Validación, pitch y paquete de entrega: tareas

No son tareas de código. Cada casilla produce un documento, una entrevista, una prueba de escena o un paquete verificable. Ninguna está marcada como ejecutada por esta redacción. [Plan](plan.md) · [Ensayos](test-e2e.md).

## Actualización 17-oct — pitch v8 y acceso Auditor del jurado (#371)

- [x] Generador v8 reutilizando v7, diez diapositivas, QR grande al dominio y explicación de seguridad en diez líneas; editable/PDF con credencial disponibles localmente fuera de Git y copia de revisión sin contraseña para el PR.
- [x] [Guion v8](guion-pitch-v8.md), con versiones DOCX/PDF y notas del presentador: meta propuesta de 290 s (4:50), sin ensayo cronometrado atribuido.
- [x] **Producción real:** usuario dedicado `jurado@istpetdev.doicela.dev` confirmado en Cognito y cuenta humana activa `Auditor` en RDS `istpetdev_integration`. Sin migraciones ni cambios de grants, IAM, MFA o configuración del pool. Contraseña gestionada por Cognito, ausente de `Account` y de los archivos versionables.
- [x] **Verificación real:** inicio de sesión desde el dominio en navegador; `/api/v1/me` devuelve `Auditor`/`web`, rutas/riesgos/entregas devuelven 200; seis escrituras devuelven 403; sin botones de escritura comprobados en Rutas. [Evidencia y límites](evidencia/pitch-v8-acceso-jurado.md).
- [ ] Ensayar 4:50 con el equipo, escanear desde el teléfono del jurado y desactivar la cuenta cuando termine la evaluación.

Esto no cierra T3–T6 ni acredita entrega oficial, auditoría integral de seguridad o nuevo despliegue de la aplicación. Integración vía PR a `develop` y CI se verifican en GitHub para el HEAD final.

| ID | Entregable | Depende de | Fecha límite | Estado |
|---|---|---|---|---|
| T1 | Kit previo y revisión humana | — | 15-oct | hecho: revisión de 15 min y aprobación de Joseph el 14-oct; CE-01 registrado |
| T2 | Guion y matriz de afirmaciones | T1 | antes de slides | borrador aprobado por Joseph el 14-oct; segundo revisor pedido el 14-oct (fase 3), sin realizar; O-04/O-06 y ensayos pendientes |
| T3 | Evidencia sectorial y simulada | T1, 001–005 | 17-oct 13:15 | pendiente: [hojas](hojas-feria.md) preparadas el 14-oct; sin conversaciones ni corrida oficial de 005 |
| T4 | Demostración, roles y respaldo | T2, CE ejecutados | 15-oct video; 17-oct escena | pendiente: [escena y contingencias](escena-y-contingencias.md) preparadas el 14-oct; papeles sin nombres, sin ensayo funcional ni video |
| T5 | Diez ensayos y veinte respuestas | T2–T4 | 17-oct 15:45 | pendiente: [registro](registro-ensayos.md) preparado el 14-oct; racha 0/10, ninguna ronda de respuestas |
| T6 | Paquete oficial y congelamiento | T3–T5 | 17-oct 15:45 | pendiente: [manifiesto](manifiesto-paquete.md) abierto y editable/PDF **provisionales** el 14-oct; sin congelar ni entregar |

## Avisos orales del 16-oct — comprobaciones antes de cambiar el 006

**Actualización 16-oct, agenda oficial** (`areas-menotres.xlsx`, [Cronograma oficial](../../boveda/01%20Oficial/Cronograma%20oficial.md)): no hay entrega el viernes. El vie 18:30–19:00 es Checkpoint de avances; la única entrega es el sáb 16:00–16:15. La agenda no cita los cortes de 13:00 ni de 18:00. Pitch de 5 min (oral). Siguen pendientes canal/formato (O-06) y turno/cubículo (O-13). Entregable 2 en borrador: [Entregable Solucion y propuesta de valor](../../boveda/07%20Demo/Entregable%20Solucion%20y%20propuesta%20de%20valor.md). Tareas del evento en el issue #200.

Fuente: [apunte de Joseph sobre agenda y pitch](../../boveda/NotasEvento/2026-10-16-1102-jb-agenda-entregas-y-pitch.md). Es una transcripción de lo oído durante el evento, sin enlace al Drive ni instrucción escrita. No cierra O-04 (duración del pitch) ni O-06 (canal y formato de entrega).

| Tema oído | Impacto operativo provisional | Confirmación necesaria |
|---|---|---|
| Drive para subir la idea hoy 16-oct a las 13:00 y otra carpeta o entrega a las 18:00 | Preparar una versión breve de la idea sin sustituir el paquete final previsto para el 17-oct. Evitar perder el posible primer plazo. | Enlace, acceso, contenido, formato, responsable y si la entrega de las 18:00 es distinta. Guardar aviso o acuse. |
| Bloque de diseño oído como 14:00–15:00 | Revisar la agenda del equipo; el manual sitúa diseño entre 14:00 y 16:00. | Horario vigente y si el bloque de 14:00–15:00 es una subactividad. |
| Cinco jurados, cuatro equipos simultáneos y un cubículo por reto | Ensayar una escena autosuficiente en espacio reducido solo si se confirma; revisar proyección, audio, conectividad y relevo. No cambiar todavía la descripción de siete jurados del manual. | Número y reparto real de jurados, cubículo del Reto 1, turno, duración de pitch/demo/preguntas y sentido de la frase sobre ingreso 20 minutos antes. |

- [ ] Conseguir confirmación de O-04/O-06 y registrar quién informó, cuándo y con qué soporte; adaptar los cortes 3/5/7 y el manifiesto solo después de conocer tiempo y canal.
- [ ] Si hay pitch simultáneo, ensayar una pasada en el cubículo o en un espacio equivalente con el equipo y dispositivos reales; registrar la prueba en [Ensayos](registro-ensayos.md).
- [ ] Mantener la entrega final del 17-oct 16:00–16:15 como referencia documental hasta que la organización publique un cambio. El Drive de hoy se trata como hito adicional, aún por confirmar.

## T1 — Kit previo sin horas del equipo

- [x] Imprimir o descargar hoja de entrevista de [Registro de mentorias](../../boveda/06%20Equipo/Registro%20de%20mentorias.md); comprobar casillas independientes de notas y cita.
- [x] Revisar el esqueleto 3/5/7 y los huecos `[CITA DE LA FERIA]`, `[RESULTADO DEL SIMULADOR]` y `[PRECIO]` de [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md).
- [x] Revisar cliente, valor, ingresos y costos de [Modelo de negocio y piloto](../../boveda/02%20Producto/Modelo%20de%20negocio%20y%20piloto.md), sin precio inventado.
- [x] Revisar veinte preguntas y fuentes de [SERCOP](../../boveda/99%20Fuentes/SERCOP%20limpieza%20hospitalaria%20y%20sanciones.md); registrar quién dedicó unos 15 min a la revisión y qué corrigió. El kit lo prepara IA; no atribuir horas de construcción al equipo.

**Estado al 14-oct-2026 (issue #46, fase 1):** Claude Code (Opus 5.5) preparó y corrigió el kit sobre `develop` en la rama `feature/46-kit-previo-006`, a pedido de Joseph Baño. Joseph informó que revisó y aprobó todos los componentes durante 15 minutos el 14-oct; no dio horas de inicio y fin. CE-01 queda registrado con esa duración informada por el revisor. No hay entrevistas, ensayos de escena ni cifras nuevas.

| Componente ER-02 | Dónde está | Qué se corrigió el 14-oct |
|---|---|---|
| Hoja de entrevista | [Registro de mentorias](../../boveda/06%20Equipo/Registro%20de%20mentorias.md), «Hoja de campo» | Tres permisos en casillas separadas (notas, cita sin nombre, mención de rol/organización); preguntas 2 y 4 sin presuponer el hecho; enlace a evidencia; lista de datos que no se anotan |
| Esqueleto 3/5/7 | [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md), complemento del 006 | Aviso al inicio: el recorrido de 180 s queda como antecedente. Tiempos contrastados con el [plan](plan.md): coinciden |
| Cliente, valor, ingresos y costos | [Modelo de negocio y piloto](../../boveda/02%20Producto/Modelo%20de%20negocio%20y%20piloto.md), «Resumen para el kit previo» | Resumen según Anexo 5.6. Corrección: USD 37/mes cubre solo EC2, no RDS (contradicción registrada). Fila de mapas alineada con ADR-05/ADR-23. Precio pendiente |
| Banco de veinte preguntas | [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md), «Banco de 20 preguntas» | Escalabilidad (n.º 11) sustituye la pregunta sobre el escaneo del jurado, cuya condición pasa a la n.º 10 |
| Fuente SERCOP | [SERCOP](../../boveda/99%20Fuentes/SERCOP%20limpieza%20hospitalaria%20y%20sanciones.md) | URL y citas comprobadas el 14-oct; la fila de HGDC cita el texto literal |

O-04 y O-06 siguen **pendientes**, sin respuesta con fuente en el repositorio ni en el manual ([Consultas para la organizacion](../../boveda/01%20Oficial/Consultas%20para%20la%20organizacion.md), «Estado de O-04 y O-06»).

### Hoja de revisión humana de T1 (unos 15 min)

Debe hacerla una persona del equipo con la versión del kit indicada en el PR. Al terminar, completar los campos, marcar las casillas de arriba y añadir la fila en [Registro de trabajo previo y del evento](../../boveda/06%20Equipo/Registro%20de%20trabajo%20previo%20y%20del%20evento.md).

| Min. | Qué revisar | Criterio para aceptarlo |
|---|---|---|
| 0–3 | Hoja de entrevista | Se puede imprimir o copiar; los tres permisos son casillas independientes; ninguna de las cinco preguntas presupone respuesta, cifra o costo; no pide datos personales |
| 3–6 | Esqueleto 3/5/7 | Siete bloques del Anexo 5.7 en orden en los tres cortes; `[CITA DE LA FERIA]`, `[RESULTADO DEL SIMULADOR]` y `[PRECIO]` siguen como huecos; ningún corte se presenta como tiempo oficial |
| 6–9 | Resumen de negocio | Cliente, valor, ingresos y costos responden el Anexo 5.6; no hay precio ni ahorro; USD 37/mes no se presenta como costo total de hosting |
| 9–13 | Veinte preguntas | Cada borrador cabe en ≤30 s leído en voz alta; ninguno afirma un CE como ejecutado; las n.º 16 y 17 no pronuncian el marcador; aceptar o revertir el cambio de la n.º 11 |
| 13–15 | Fuente SERCOP | Abrir al menos un PDF y encontrar «se obliga a proveer» y «frecuencias de cambio»; la frase usable no contiene cifra de multa |

| Campo | Registro |
|---|---|
| Revisor | Joseph Baño (Andrés), según mensaje del 14-oct |
| Fecha, hora de inicio y fin | 14-oct-2026; duración informada: 15 min; horas de inicio y fin no registradas |
| Versión revisada (commit o PR) | Kit del PR #71 (commit `228ab72`; contenido de T1 en `92a9e63`) |
| Correcciones pedidas o hechas | Joseph aprobó T1; no informó correcciones adicionales |
| ¿Aprueba el kit para T2/T3? | Sí, según mensaje de Joseph del 14-oct; CE-01 registrado en E2E-01 |

## T2 — Guion y auditoría de cada frase

- [ ] Confirmar O-04 o señalarlo pendiente y elegir corte objetivo. Marcar cada diapositiva con uno de los siete bloques del Anexo 5.7.
- [ ] Crear tarjeta por afirmación: frase, spec/CE, enlace de ejecución, límite y sustitución. Si falta ejecución, cambiar a `[PENDIENTE: evidencia]` en material de trabajo y retirar la frase del guion de escena.
- [ ] Inspeccionar el flujo Artifacts/Slides del entorno antes de crear el archivo; con O-06 resuelto, crear editable y PDF local de la misma versión. En esta sesión no hay sesión PowerPoint conectada; el archivo de slides queda pendiente.
- [ ] Releer la tabla cruzada del [spec](spec.md) y los «fuera de alcance» de 001–005 después de cualquier recorte de alcance.

**Estado al 14-oct-2026 (issue #46, fase 2):** Claude Code (Opus 5.5), a pedido de Joseph Baño, preparó T2 como **borrador** en la rama `feature/46-fase2-guion-auditoria`, desde `develop` en `7aed1eb` (PR #71 ya fusionado). Joseph informó el mismo día que revisó y aprobó T1 y T2; su aprobación se refiere al borrador `8693f73`. Una revisión posterior ajustó A-16/A-17/A-27 para no atribuir a la pantalla ni a Cognito lo probado solo por API. Falta un segundo revisor para E2E-05, además de O-04/O-06, PDF y ensayos. **T2 no está cerrado.** El 14-oct (fase final) se generaron un editable y un PDF **provisionales**, no oficiales ([manifiesto](manifiesto-paquete.md)).

| Casilla | Qué se hizo | Qué falta |
|---|---|---|
| O-04 y corte | O-04 y O-06 revisados de nuevo: sin respuesta con fuente ([Consultas](../../boveda/01%20Oficial/Consultas%20para%20la%20organizacion.md)). Cortes 3/5/7 con metas internas 160/280/400 s y siete bloques en orden en «Versión de trabajo T2» del [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md); estructura de siete diapositivas, una por bloque | Respuesta oficial de O-04; elegir corte; nombres de los cinco papeles (PA-01) |
| Tarjetas de afirmación | [Matriz de afirmaciones](matriz-afirmaciones.md): 33 frases o cifras con spec/CE, ejecución, límite, estado y sustitución. Se ejecutaron 105 pruebas automatizadas sobre `7aed1eb` (12 + 93, todas pasaron) para sostenerla ([evidencia](evidencia/2026-10-14-suites-fase2.md)). Tras la revisión del borrador: 11 usables con límite, 11 solo como propuesta y 11 retiradas | Dos revisores (E2E-05); repetir sobre el commit congelado |
| Slides | No se creó editable ni PDF: O-06 sin respuesta y formato sin confirmar | O-06; elegir herramienta; editable y PDF de la misma versión |
| Releer cruce del spec | La matriz sustituye en la práctica la tabla cruzada del spec para el guion; registra dos contradicciones (005 «cerrado» sin corrida oficial; 004 «concluido» sin prueba de integración ni teléfono) | Respuesta de los responsables de 004/005 |

No hubo ensayos ni cronometrajes: ningún tiempo de los cortes está medido.

## T3 — Entrevistas, mentoría y corrida

- [ ] 16-oct, 11:00–11:30 y 13:00–14:00: dos integrantes abordan expositores fuera de las organizaciones cauteladas. Usan guion de cinco minutos, registran negativas sin presión y anonimizan notas.
- [ ] 17-oct, 11:30–13:15 (Validación integral, agenda oficial): volver con prototipo; pedir interpretación de alerta, ruta y estado de caja antes de explicar; registrar reacción y cambio.
- [ ] Registrar mentorías del 16 y 17 con la [plantilla](../../boveda/09%20Plantillas/Plantilla%20mentoria.md), incluyendo si el mentor podría integrar jurado.
- [ ] Ejecutar o recibir el ZIP de 005 con FR-21 y CE-04/06/09: verificar hashes, host, commit, seeds y escenario adverso antes de escribir un rango. Si falla, retirar cifras.

**Estado al 14-oct-2026 (issue #46, fase 3):** solo preparación. Claude Code (Opus 5.5), a pedido de Joseph Baño, preparó las [hojas de feria](hojas-feria.md): registro de intentos, hoja del 16 y de retorno del 17 con los tres permisos en casillas separadas, códigos anónimos y la lista de comprobación de la corrida oficial de 005. No hubo conversaciones (son el 16 y 17), mentorías nuevas ni corrida oficial: el registro de ejecuciones de 005 sigue vacío, así que A-19 queda como propuesta y A-21 retirada. **Ninguna casilla de T3 se marca.**

## T4 — Escena física y respaldo

- [ ] Identificar por nombre a narrador, operador web, operador móvil, persona técnica y persona de negocio; ensayar un relevo corto por cada papel. [PENDIENTE: asignación del equipo].
- [ ] Probar HTTPS del QR desde teléfono ajeno al equipo solo si 004 CE-07 y conectividad lo permiten; preparar teléfono del equipo y alternativa de pantalla. No mostrar datos privados.
- [ ] Ensayar offline real en teléfono según 004 CE-02 antes de pronunciar «sin señal»; confirmar que reset/advance no cambia simulación según 005 CE-08.
- [ ] El 15-oct, después de un ensayo funcional, grabar video con fecha, commit, dispositivo y partes realmente grabadas; etiquetar «grabado» y conservarlo local.

**Estado al 14-oct-2026 (issue #46, fase 3):** solo preparación, en [escena y contingencias](escena-y-contingencias.md). Joseph informó que el equipo no ha confirmado los nombres de los papeles: quedan `[PENDIENTE: nombre]`, con una sustitución por papel ausente y un relevo y una sustitución para cada parte de la escena. Contraste con la matriz: **ninguna acción visible tiene ensayo conectado**, así que cada parte de la demo usa por ahora su frase de propuesta («en nuestras pruebas…», «es el diseño…») y la vista pública no se invita a escanear. Hay cinco tarjetas de contingencia (agente, mapa, conexión, cámara/QR, ZIP ausente), ninguna ensayada; el mapa depende de teselas de OpenStreetMap y de íconos de `unpkg.com`, es decir, de Internet. Se dejó el protocolo del ensayo funcional y la tabla del video **vacía**: el video no existe. **Ninguna casilla de T4 se marca.**

## T5 — Ensayos cronometrados y respuestas

- [ ] Registrar cada ensayo con corte, segundos, commit, material, conexión, cinco papeles, fallos, enlace y contador de racha. Un fallo reinicia el contador; los dos últimos son sin wifi del recinto.
- [ ] Hacer rondas de veinte preguntas difíciles del banco; cada respuesta ≤30 s, sin precio ni cifra improvisada. Marcar preguntas que exigen evidencia no disponible.
- [ ] Usar el 15-oct de 15:00 a 20:00 como bloque principal de ensayos (ver el plan): completar allí la racha de diez y confirmarla el 16 y 17 con la versión del evento.
- [ ] Repetir hasta diez ensayos completos consecutivos. Verificar al final tiempo oficial −20 s y que la transición al plan B no exceda el bloque asignado.

**Estado al 14-oct-2026 (issue #46, fase 3):** solo preparación. El [registro de ensayos](registro-ensayos.md) tiene columnas de corte, duración medida, meta, cinco papeles, versión, commit, conexión, fallos, enlace y racha; una tabla de ensayos funcionales de la demo; y la ronda de veinte respuestas cronometrada aparte. Joseph informó que no hubo ensayos: **racha 0/10**, ninguna respuesta cronometrada. **Ninguna casilla de T5 se marca.**

## T6 — Congelar y entregar

- [ ] Crear manifiesto de siete entregables de [Evaluacion y entregables](../../boveda/01%20Oficial/Evaluacion%20y%20entregables.md): problema, solución, demo, validación, viabilidad, modelo de negocio y pitch; anotar formato, hash, estado y responsable.
- [ ] Revisar que citas tengan consentimiento, rangos tengan ZIP/run, y que cada costo/precio tenga fuente o siga pendiente fuera del discurso. Registrar tag `pre-evento` y trabajo adaptado.
- [ ] 17-oct 15:45: congelar editable, PDF, video, ficha y evidencias. Abrir copia local sin red. A las 16:00–16:15 entregar por canal confirmado en O-06 y guardar acuse si existe.

**Estado al 14-oct-2026 16:00 (issue #46, fase final, solo lo que cabe antes del evento):** Claude Code (Opus 5.5), a pedido de Joseph Baño, abrió el [manifiesto del paquete](manifiesto-paquete.md) con los siete entregables. Los datos que aún no existen quedan vacíos. También generó una presentación **provisional** de siete bloques, con editable y PDF ([`materiales/`](materiales/)) y su SHA-256. Solo usa frases autorizadas por la matriz y lleva el rótulo «no es formato oficial · no entregado». O-04/O-06 siguen sin respuesta con fuente: se revisaron el repositorio, las issues y el correo de Joseph. Faltan por fecha o por persona: papeles confirmados, tag `pre-evento` (después del congelamiento de funcionalidades del 15-oct, 14:00), ensayo funcional y video (15-oct), entrevistas (16–17), segunda revisión, comparación final con la matriz, congelamiento (17-oct 15:45) y entrega (16:00–16:15). **Ninguna casilla de T6 se marca.**
