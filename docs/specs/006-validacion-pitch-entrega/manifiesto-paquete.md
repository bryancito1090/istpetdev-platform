# 006 — Manifiesto del paquete de entrega (T6, fase final del issue #46)

**Abierto:** 14-oct-2026 16:00 (-05:00) por Claude Code (Opus 5.5), a pedido de Joseph Baño, en la rama `feature/46-fase3-escena-ensayos` (PR #75). **Paquete no congelado ni entregado.** Las celdas «—» son datos que aún no existen. Coordina y mantiene el manifiesto: Joseph Baño (issue #46). [Spec](spec.md) · [Tareas](task.md) · [E2E-07](test-e2e.md#e2e-07--paquete-que-se-entrega) · [Matriz](matriz-afirmaciones.md) · [Evaluacion y entregables](../../boveda/01%20Oficial/Evaluacion%20y%20entregables.md).

**Formato y canal:** O-06 sigue **pendiente** ([Consultas](../../boveda/01%20Oficial/Consultas%20para%20la%20organizacion.md)). Ningún archivo de esta tabla es formato oficial. Si O-06 pide otro formato, se cambia la columna «Formato» y se recalculan los hashes.

## Siete entregables oficiales (manual, §9, p. 8)

Estado: `vivo`, `grabado`, `simulado` o `propuesto`, según lo que **realmente** respalde el archivo hoy.

| # | Entregable | Archivo | Formato | Versión / commit | SHA-256 | Responsable | Estado | Evidencia |
|---|---|---|---|---|---|---|---|---|
| 1 | Problema | Diapositiva 1 del editable provisional; ficha — | pptx/pdf provisional; ficha — | v0, PR #75 | ver «Pitch»; ficha — | [PENDIENTE] | propuesto (A-01 documental, A-02 hipótesis) | [SERCOP](../../boveda/99%20Fuentes/SERCOP%20limpieza%20hospitalaria%20y%20sanciones.md); entrevistas: — |
| 2 | Solución | Diapositiva 2; diagrama final — | pptx/pdf provisional | v0, PR #75 | ver «Pitch» | [PENDIENTE] | propuesto | Matriz A-06/A-10/A-11; EJ-07/EJ-08 |
| 3 | Prototipo / demo | URL o versión del prototipo —; video de respaldo — | — | — | — | [PENDIENTE] | propuesto (sin ensayo conectado ni video) | [Registro de ensayos funcionales](registro-ensayos.md#ensayos-funcionales-de-la-demo): vacío |
| 4 | Validación | Hojas de feria completadas —; corrida oficial de 005 — | — | — | — | [PENDIENTE] | — (sin entrevistas ni corrida) | [Hojas de feria](hojas-feria.md): sin filas; [evidencia de suites](evidencia/2026-10-14-suites-fase2.md) (pruebas automatizadas, no validación sectorial) |
| 5 | Viabilidad | Diapositiva 5; ficha de recursos y riesgos — | pptx/pdf provisional | v0, PR #75 | ver «Pitch» | [PENDIENTE] | propuesto (A-22/A-24 de prueba local) | EJ-01, EJ-02, EJ-07 |
| 6 | Modelo de negocio | Diapositiva 6; ficha de piloto — | pptx/pdf provisional | v0, PR #75 | ver «Pitch» | [PENDIENTE] | propuesto (hipótesis; sin precio) | [Modelo de negocio y piloto](../../boveda/02%20Producto/Modelo%20de%20negocio%20y%20piloto.md) |
| 7 | Pitch | [`materiales/pitch-provisional-v0.pptx`](materiales/pitch-provisional-v0.pptx) y [`.pdf`](materiales/pitch-provisional-v0.pdf) | pptx editable + PDF local (provisional) | v0, PR #75; generado con [`generar_provisional.py`](materiales/generar_provisional.py) | pptx `61c620b0fae7a2481bdeb504e97df484e454ed3d628bbd6b9207f312c228a7be`; pdf `aad85bac3a15a1c571156f651dfe56ba346cf170987dd6bf7dd7f84f991de672` | [PENDIENTE] | propuesto (provisional, sin ensayar) | Frases de la matriz y de la «Frase vigente» de la [escena](escena-y-contingencias.md) |

**Sobre el editable y el PDF provisionales:** siete diapositivas, una por bloque del Anexo 5.7, cada una con el rótulo «PROVISIONAL v0 · no es formato oficial (O-04/O-06 pendientes) · no entregado» y los ID de la matriz que la sostienen. No contienen cita de feria, cifra del simulador, precio ni costo. El PDF se generó con LibreOffice 26.8.1 y el 14-oct se renderizaron localmente sus siete páginas con `pdftoppm`, sin recursos externos. La prueba de abrirlo **sin red** en el dispositivo de escena es parte del congelamiento y está pendiente. Ninguna persona del equipo lo ha revisado ni ensayado.

## Apoyos del paquete

| Elemento | Archivo | SHA-256 | Estado |
|---|---|---|---|
| Matriz de afirmaciones final | [matriz-afirmaciones.md](matriz-afirmaciones.md) | — (se calcula al congelar) | borrador; segunda revisión pendiente |
| Guion | [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md) | — | versión de trabajo T2 |
| ZIP de 005 y `manifest.json` | — | — | no existe corrida oficial |
| Video de respaldo | — | — | no grabado |
| Tag `pre-evento` | — | commit — | no creado |
| Registro preparación / evento | [Registro de trabajo previo y del evento](../../boveda/06%20Equipo/Registro%20de%20trabajo%20previo%20y%20del%20evento.md) | — | sección del evento abierta y vacía |

## Congelamiento (17-oct, 15:45)

| Hora real | Commit congelado | Archivos y SHA-256 | Abierto sin red (quién, dispositivo) | Comparación con la matriz final (quién) | Retirado antes de congelar |
|---|---|---|---|---|---|
| — | — | — | — | — | — |

## Entrega (17-oct, 16:00–16:15)

Solo por el canal que la organización confirme en O-06.

| Hora real | Canal confirmado (fuente O-06) | Archivos entregados (SHA-256) | Quién entregó | Acuse o nota fiel de recepción |
|---|---|---|---|---|
| — | — | — | — | — |
