# 006 — Validación, pitch y paquete de entrega: tareas

No son tareas de código. Cada casilla produce un documento, una entrevista, una prueba de escena o un paquete verificable. Ninguna está marcada como ejecutada por esta redacción. [Plan](plan.md) · [Ensayos](test-e2e.md).

| ID | Entregable | Depende de | Fecha límite | Estado |
|---|---|---|---|---|
| T1 | Kit previo y revisión humana | — | 15-oct | pendiente |
| T2 | Guion y matriz de afirmaciones | T1 | antes de slides | pendiente |
| T3 | Evidencia sectorial y simulada | T1, 001–005 | 17-oct 12:30 | pendiente |
| T4 | Demostración, roles y respaldo | T2, CE ejecutados | 15-oct video; 17-oct escena | pendiente |
| T5 | Diez ensayos y veinte respuestas | T2–T4 | 17-oct 15:45 | pendiente |
| T6 | Paquete oficial y congelamiento | T3–T5 | 17-oct 15:45 | pendiente |

## T1 — Kit previo sin horas del equipo

- [ ] Imprimir o descargar hoja de entrevista de [Registro de mentorias](../../boveda/06%20Equipo/Registro%20de%20mentorias.md); comprobar casillas independientes de notas y cita.
- [ ] Revisar el esqueleto 3/5/7 y los huecos `[CITA DE LA FERIA]`, `[RESULTADO DEL SIMULADOR]` y `[PRECIO]` de [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md).
- [ ] Revisar cliente, valor, ingresos y costos de [Modelo de negocio y piloto](../../boveda/02%20Producto/Modelo%20de%20negocio%20y%20piloto.md), sin precio inventado.
- [ ] Revisar veinte preguntas y fuentes de [SERCOP](../../boveda/99%20Fuentes/SERCOP%20limpieza%20hospitalaria%20y%20sanciones.md); registrar quién dedicó unos 15 min a la revisión y qué corrigió. El kit lo prepara IA; no atribuir horas de construcción al equipo.

## T2 — Guion y auditoría de cada frase

- [ ] Confirmar O-04 o señalarlo pendiente y elegir corte objetivo. Marcar cada diapositiva con uno de los siete bloques del Anexo 5.7.
- [ ] Crear tarjeta por afirmación: frase, spec/CE, enlace de ejecución, límite y sustitución. Si falta ejecución, cambiar a `[PENDIENTE: evidencia]` en material de trabajo y retirar la frase del guion de escena.
- [ ] Inspeccionar el flujo Artifacts/Slides del entorno antes de crear el archivo; con O-06 resuelto, crear editable y PDF local de la misma versión. En esta sesión no hay sesión PowerPoint conectada; el archivo de slides queda pendiente.
- [ ] Releer la tabla cruzada del [spec](spec.md) y los «fuera de alcance» de 001–005 después de cualquier recorte de alcance.

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
