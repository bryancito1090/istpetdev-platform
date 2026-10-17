# 006 — Manifiesto del paquete de entrega (T6, fase final del issue #46)

**Estado:** paquete provisional, aún no congelado ni entregado. Las celdas «—» son datos que todavía no existen. Coordina y mantiene el manifiesto: Joseph Baño (issue #46). [Spec](spec.md) · [Tareas](task.md) · [E2E-07](test-e2e.md#e2e-07--paquete-que-se-entrega) · [Matriz](matriz-afirmaciones.md) · [Evaluacion y entregables](../../boveda/01%20Oficial/Evaluacion%20y%20entregables.md).

**Formato y canal:** O-06 sigue **pendiente** ([Consultas](../../boveda/01%20Oficial/Consultas%20para%20la%20organizacion.md)). Ningún archivo de esta tabla es formato oficial. Si O-06 pide otro formato, se cambia la columna «Formato» y se recalculan los hashes.

## Siete entregables oficiales (manual, §9, p. 8)

Estado: `vivo`, `grabado`, `simulado` o `propuesto`, según lo que **realmente** respalde el archivo hoy.

| # | Entregable | Archivo | Formato | Versión / commit | SHA-256 | Responsable | Estado | Evidencia |
|---|---|---|---|---|---|---|---|---|
| 1 | Problema | Diapositiva 1 de [`pitch-v2.pptx`](materiales/pitch-v2.pptx) / [`pitch-v2.pdf`](materiales/pitch-v2.pdf) | pptx + pdf provisional | v2, no congelado | ver fila 7 | [PENDIENTE] | propuesto; hipótesis de problema | matriz A-02 |
| 2 | Solución | Diapositiva 2; [`materiales/entregable-2/IstpetDev-Entregable-2-Solucion.pdf`](materiales/entregable-2/IstpetDev-Entregable-2-Solucion.pdf) y `.docx` editable | pptx + docx + pdf | v2, no congelado | ver fila 7 y hashes de Entregable 2 | [PENDIENTE] | propuesto; comparación A/B/C en integración | [Nota de la bóveda](../../boveda/07%20Demo/Entregable%20Solucion%20y%20propuesta%20de%20valor.md) |
| 3 | Prototipo / demo | Diapositivas 3–4 de `pitch-v2`; captura de rutas sintética y diseño A/B/C con marcador de captura pendiente | pptx + pdf + PNG | v2, sin ensayo conectado | ver fila 7 | [PENDIENTE] | propuesto (A/B/C sin datos ni recomendación atribuida al motor) | [Registro de ensayos funcionales](registro-ensayos.md#ensayos-funcionales-de-la-demo): vacío |
| 4 | Validación | Hojas de feria completadas —; corrida oficial de 005 — | — | — | — | [PENDIENTE] | — (sin entrevistas ni corrida) | [Hojas de feria](hojas-feria.md): sin filas; [evidencia de suites](evidencia/2026-10-14-suites-fase2.md) (pruebas automatizadas, no validación sectorial) |
| 5 | Viabilidad | Diapositiva 6; ficha de recursos y riesgos — | pptx/pdf provisional | v2, no congelado | ver fila 7 | [PENDIENTE] | propuesto; costo estimado separado de A/B/C | matriz P-01/P-04/P-11, A-22 |
| 6 | Modelo de negocio | Diapositiva 7; ficha de piloto — | pptx/pdf provisional | v2, no congelado | ver fila 7 | [PENDIENTE] | propuesto (hipótesis; sin precio) | matriz A-26/A-30/A-31 |
| 7 | Pitch | [`materiales/pitch-v2.pptx`](materiales/pitch-v2.pptx), [`pitch-v2.pdf`](materiales/pitch-v2.pdf) y nueve PNG en [`materiales/render-v2/`](materiales/render-v2/) | pptx editable + PDF provisional + PNG | v2, rama `docs/006-pitch-v2`; no congelado | PPTX `52706080cebea4b4cb63c7d6f9f761085fefed7555dce79ad34aebc7e3ddbeec`; PDF `808033ae26dc26a3ce3e2a5bc7fa4984d4d5e41557ff8de255d37fec7454c91f` | [PENDIENTE] | propuesto, revisión visual local; no ensayado ni entregado | Anexo 5.7, guion T4 y matriz de afirmaciones |

**Sobre el editable y el PDF provisionales:** nueve diapositivas alineadas con Anexo 5.7 y el guion T4. Incluyen el panel A/B/C como diseño con celdas «sin dato», indicadores en lenguaje sencillo, riesgos y mitigaciones, y hoja de ruta. El panel no presenta resultados del motor. La captura real de rutas se rotula como otro escenario sintético; el marcador visible indica que falta la captura A/B/C. El PDF se convirtió a nueve PNG con `pdftoppm` y se inspeccionó cada página. Los archivos siguen siendo provisionales, no ensayados, no congelados y no entregados.

## Apoyos del paquete

| Elemento | Archivo | SHA-256 | Estado |
|---|---|---|---|
| Matriz de afirmaciones final | [matriz-afirmaciones.md](matriz-afirmaciones.md) | — (se calcula al congelar) | borrador; segunda revisión pendiente |
| Guion T4 | [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md) | `de3d136396d6055a84655d3cbee1870729d2e02554e307d192829818ce03dec5` | fuente narrativa de pitch-v2 |
| Generador v2 | [`materiales/generar_v2.py`](materiales/generar_v2.py) | `e422660bacdd80e43069f5cbca2467bfef7085da77d010b675b6358fc44dd609` | genera PPTX y PDF |
| Render PNG 1 | [`materiales/render-v2/diapositiva-1.png`](materiales/render-v2/diapositiva-1.png) | `c45a83e5b12f337ee2586fbfa54b6d8abd3eb3f3f57b10cc9477913ebf02741c` | inspeccionado |
| Render PNG 2 | [`materiales/render-v2/diapositiva-2.png`](materiales/render-v2/diapositiva-2.png) | `47dd35a5c1a2dc04d254f85b0a919aed728cbd43e1231d77ad83b14a7849c707` | inspeccionado |
| Render PNG 3 | [`materiales/render-v2/diapositiva-3.png`](materiales/render-v2/diapositiva-3.png) | `e13f6a97a02e6656e5d4fc9f61a894d1f3d1e3a6e7716e1684d294cf65177da8` | inspeccionado |
| Render PNG 4 | [`materiales/render-v2/diapositiva-4.png`](materiales/render-v2/diapositiva-4.png) | `d35edcbd6f12c00494eed186329f22d47d5c6f3853501d757d6f953f62c6b751` | inspeccionado |
| Render PNG 5 | [`materiales/render-v2/diapositiva-5.png`](materiales/render-v2/diapositiva-5.png) | `58976af1530177150988faa728d439a4a7c789de7fccd42da4724620e88ffe3b` | inspeccionado |
| Render PNG 6 | [`materiales/render-v2/diapositiva-6.png`](materiales/render-v2/diapositiva-6.png) | `11b659f5251d0708c042faa0b96a9ba6cbb62b013ef601f42bdf6d7ef02209bd` | inspeccionado |
| Render PNG 7 | [`materiales/render-v2/diapositiva-7.png`](materiales/render-v2/diapositiva-7.png) | `ab2366cf6dc137261f2ef8b8f99ba67bc17274cb1788e9bc8091c6fb88345782` | inspeccionado |
| Render PNG 8 | [`materiales/render-v2/diapositiva-8.png`](materiales/render-v2/diapositiva-8.png) | `a5591839413e6de59d982e120f39d1c52348d936929deb2b905398a1157c247a` | inspeccionado |
| Render PNG 9 | [`materiales/render-v2/diapositiva-9.png`](materiales/render-v2/diapositiva-9.png) | `88c38723222ff71d76c7b371052c84f2cdcea5df4f8083fa018a13c07bd0ebdb` | inspeccionado |
| ZIP de 005 y `manifest.json` | — | — | no existe corrida oficial |
| Video de respaldo | — | — | no grabado |

## Congelamiento (17-oct, 15:45)

| Hora real | Commit congelado | Archivos y SHA-256 | Abierto sin red (quién, dispositivo) | Comparación con la matriz final (quién) | Retirado antes de congelar |
|---|---|---|---|---|---|
| — | — | — | — | — | — |

## Entrega (17-oct, 16:00–16:15)

Solo por el canal que la organización confirme en O-06.

| Hora real | Canal confirmado (fuente O-06) | Archivos entregados (SHA-256) | Quién entregó | Acuse o nota fiel de recepción |
|---|---|---|---|---|
| — | — | — | — | — |

## Capturas pendientes para pitch-v2

| Captura | Responsable sugerido | Condición para tomarla |
|---|---|---|
| `[CAPTURA PENDIENTE: pantalla A/B/C]` | Equipo Web que integre #241/#242; Técnica registra la corrida y el Planificador valida las cifras y la justificación. | Capturar solo cuando la comparación esté conectada al escenario `demo-quito-planificacion`, las cifras tengan fuente/configuración y se haya ensayado la pantalla. |
