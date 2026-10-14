# 006 — Validación, pitch y paquete de entrega: tareas

No son tareas de código. Cada casilla produce un documento, una entrevista, una prueba de escena o un paquete verificable. Ninguna está marcada como ejecutada por esta redacción. [Plan](plan.md) · [Ensayos](test-e2e.md).

| ID | Entregable | Depende de | Fecha límite | Estado |
|---|---|---|---|---|
| T1 | Kit previo y revisión humana | — | 15-oct | kit preparado por IA el 14-oct; revisión humana pendiente |
| T2 | Guion y matriz de afirmaciones | T1 | antes de slides | borrador preparado por IA el 14-oct; T1 sin revisión humana, matriz sin revisores, O-04/O-06 pendientes |
| T3 | Evidencia sectorial y simulada | T1, 001–005 | 17-oct 12:30 | pendiente |
| T4 | Demostración, roles y respaldo | T2, CE ejecutados | 15-oct video; 17-oct escena | pendiente |
| T5 | Diez ensayos y veinte respuestas | T2–T4 | 17-oct 15:45 | pendiente |
| T6 | Paquete oficial y congelamiento | T3–T5 | 17-oct 15:45 | pendiente |

## T1 — Kit previo sin horas del equipo

- [ ] Imprimir o descargar hoja de entrevista de [Registro de mentorias](../../boveda/06%20Equipo/Registro%20de%20mentorias.md); comprobar casillas independientes de notas y cita.
- [ ] Revisar el esqueleto 3/5/7 y los huecos `[CITA DE LA FERIA]`, `[RESULTADO DEL SIMULADOR]` y `[PRECIO]` de [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md).
- [ ] Revisar cliente, valor, ingresos y costos de [Modelo de negocio y piloto](../../boveda/02%20Producto/Modelo%20de%20negocio%20y%20piloto.md), sin precio inventado.
- [ ] Revisar veinte preguntas y fuentes de [SERCOP](../../boveda/99%20Fuentes/SERCOP%20limpieza%20hospitalaria%20y%20sanciones.md); registrar quién dedicó unos 15 min a la revisión y qué corrigió. El kit lo prepara IA; no atribuir horas de construcción al equipo.

**Estado al 14-oct-2026 (issue #46, fase 1):** Claude Code (Opus 5.5) revisó y corrigió el kit sobre `develop` en la rama `feature/46-kit-previo-006`, a pedido de Joseph Baño. Las casillas de arriba siguen sin marcar porque son de revisión humana. **CE-01 no está cumplido:** falta la revisión de unos 15 min por una persona del equipo y su registro. No hay entrevistas, ensayos, cifras ni aprobaciones nuevas.

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
| Revisor | [PENDIENTE: nombre del integrante] |
| Fecha, hora de inicio y fin | [PENDIENTE] |
| Versión revisada (commit o PR) | [PENDIENTE] |
| Correcciones pedidas o hechas | [PENDIENTE] |
| ¿Aprueba el kit para T2/T3? | [PENDIENTE: sí / sí con cambios / no] |

## T2 — Guion y auditoría de cada frase

- [ ] Confirmar O-04 o señalarlo pendiente y elegir corte objetivo. Marcar cada diapositiva con uno de los siete bloques del Anexo 5.7.
- [ ] Crear tarjeta por afirmación: frase, spec/CE, enlace de ejecución, límite y sustitución. Si falta ejecución, cambiar a `[PENDIENTE: evidencia]` en material de trabajo y retirar la frase del guion de escena.
- [ ] Inspeccionar el flujo Artifacts/Slides del entorno antes de crear el archivo; con O-06 resuelto, crear editable y PDF local de la misma versión. En esta sesión no hay sesión PowerPoint conectada; el archivo de slides queda pendiente.
- [ ] Releer la tabla cruzada del [spec](spec.md) y los «fuera de alcance» de 001–005 después de cualquier recorte de alcance.

**Estado al 14-oct-2026 (issue #46, fase 2):** Claude Code (Opus 5.5), a pedido de Joseph Baño, preparó T2 como **borrador** en la rama `feature/46-fase2-guion-auditoria`, desde `develop` en `7aed1eb` (PR #71 ya fusionado). Las casillas de arriba siguen sin marcar: T1 no tiene revisión humana registrada (la hoja de T1 sigue con todos los campos en `[PENDIENTE]`, sin comentarios en el issue #46 ni revisiones en el PR #71) y la matriz necesita dos revisores. **T2 no está cerrado.**

| Casilla | Qué se hizo | Qué falta |
|---|---|---|
| O-04 y corte | O-04 y O-06 revisados de nuevo: sin respuesta con fuente ([Consultas](../../boveda/01%20Oficial/Consultas%20para%20la%20organizacion.md)). Cortes 3/5/7 con metas internas 160/280/400 s y siete bloques en orden en «Versión de trabajo T2» del [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md); estructura de siete diapositivas, una por bloque | Respuesta oficial de O-04; elegir corte; nombres de los cinco papeles (PA-01) |
| Tarjetas de afirmación | [Matriz de afirmaciones](matriz-afirmaciones.md): 33 frases o cifras con spec/CE, ejecución, límite, estado y sustitución. Se ejecutaron 105 pruebas automatizadas sobre `7aed1eb` (12 + 93, todas pasaron) para sostenerla ([evidencia](evidencia/2026-10-14-suites-fase2.md)). Resultado: 12 usables con límite, 10 solo como propuesta y 11 retiradas | Dos revisores (E2E-05); repetir sobre el commit congelado |
| Slides | No se creó editable ni PDF: O-06 sin respuesta y formato sin confirmar | O-06; elegir herramienta; editable y PDF de la misma versión |
| Releer cruce del spec | La matriz sustituye en la práctica la tabla cruzada del spec para el guion; registra dos contradicciones (005 «cerrado» sin corrida oficial; 004 «concluido» sin prueba de integración ni teléfono) | Respuesta de los responsables de 004/005 |

No hubo ensayos ni cronometrajes: ningún tiempo de los cortes está medido.

## T3 — Entrevistas, mentoría y corrida

- [ ] 16-oct, 11:00–11:30 y 13:00–14:00: dos integrantes abordan expositores fuera de las organizaciones cauteladas. Usan guion de cinco minutos, registran negativas sin presión y anonimizan notas.
- [ ] 17-oct, 11:00–12:30: volver con prototipo; pedir interpretación de alerta, ruta y estado de caja antes de explicar; registrar reacción y cambio.
- [ ] Registrar mentorías del 16 y 17 con la [plantilla](../../boveda/09%20Plantillas/Plantilla%20mentoria.md), incluyendo si el mentor podría integrar jurado.
- [ ] Ejecutar o recibir el ZIP de 005 con FR-21 y CE-04/06/09: verificar hashes, host, commit, seeds y escenario adverso antes de escribir un rango. Si falla, retirar cifras.

## T4 — Escena física y respaldo

- [ ] Identificar por nombre a narrador, operador web, operador móvil, persona técnica y persona de negocio; ensayar un relevo corto por cada papel. [PENDIENTE: asignación del equipo].
- [ ] Probar HTTPS del QR desde teléfono ajeno al equipo solo si 004 CE-07 y conectividad lo permiten; preparar teléfono del equipo y alternativa de pantalla. No mostrar datos privados.
- [ ] Ensayar offline real en teléfono según 004 CE-02 antes de pronunciar «sin señal»; confirmar que reset/advance no cambia simulación según 005 CE-08.
- [ ] El 15-oct, después de un ensayo funcional, grabar video con fecha, commit, dispositivo y partes realmente grabadas; etiquetar «grabado» y conservarlo local.

## T5 — Ensayos cronometrados y respuestas

- [ ] Registrar cada ensayo con corte, segundos, commit, material, conexión, cinco papeles, fallos, enlace y contador de racha. Un fallo reinicia el contador; los dos últimos son sin wifi del recinto.
- [ ] Hacer rondas de veinte preguntas difíciles del banco; cada respuesta ≤30 s, sin precio ni cifra improvisada. Marcar preguntas que exigen evidencia no disponible.
- [ ] Usar el 15-oct de 15:00 a 20:00 como bloque principal de ensayos (ver el plan): completar allí la racha de diez y confirmarla el 16 y 17 con la versión del evento.
- [ ] Repetir hasta diez ensayos completos consecutivos. Verificar al final tiempo oficial −20 s y que la transición al plan B no exceda el bloque asignado.

## T6 — Congelar y entregar

- [ ] Crear manifiesto de siete entregables de [Evaluacion y entregables](../../boveda/01%20Oficial/Evaluacion%20y%20entregables.md): problema, solución, demo, validación, viabilidad, modelo de negocio y pitch; anotar formato, hash, estado y responsable.
- [ ] Revisar que citas tengan consentimiento, rangos tengan ZIP/run, y que cada costo/precio tenga fuente o siga pendiente fuera del discurso. Registrar tag `pre-evento` y trabajo adaptado.
- [ ] 17-oct 15:45: congelar editable, PDF, video, ficha y evidencias. Abrir copia local sin red. A las 16:00–16:15 entregar por canal confirmado en O-06 y guardar acuse si existe.
