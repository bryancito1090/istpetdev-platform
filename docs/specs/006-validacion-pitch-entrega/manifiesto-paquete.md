# Manifiesto del paquete de entrega — v2

**Estado:** paquete en preparación. Ningún archivo está congelado ni consta como entregado. La lista oficial contiene siete entregables; los nombres y formatos finales dependen de la respuesta completa a O-06. El canal comunicado es la carpeta de Drive del capitán, pero faltan URL y formatos confirmados.

Los archivos existentes son propuestas editables/exportadas, no validación de que cada entregable oficial esté completo. “Listo” significa archivo provisional disponible y revisable; no significa congelado ni recibido. Las propuestas de responsable deben confirmarse por el equipo. Los SHA-256 se recalculan en cada modificación y al congelar.

## Estado frente a los siete entregables oficiales

| # | Entregable | Archivo editable / PDF actualmente disponible | Responsable propuesto | SHA-256 | Estado y faltante concreto |
|---:|---|---|---|---|---|---|
| 1 | Problema | PPTX: `materiales/pitch-v2.pptx` (lámina 1); PDF: `materiales/pitch-v2.pdf` | Joseph (contenido/pitch), revisión de Bryan | Ver tabla de hashes, PPTX/PDF | **Listo como lámina provisional**. Falta revisión final de contenido/evidencia contra matriz y formato según O-06. |
| 2 | Solución | DOCX/PDF: `materiales/entregable-2/IstpetDev-Entregable-2-Solucion.docx/.pdf`; también lámina 2 en pitch PPTX/PDF | Bryan (contenido técnico), Joseph (edición) | Ver tabla de hashes | **Listo como propuesta provisional**. Falta validar/adjuntar diagrama final si lo requiere la organización y confirmar formato O-06. |
| 3 | Prototipo / demo | PPTX/PDF: láminas 3–4; PNG existentes en `materiales/entregable-2/img/`; video: `guion-video-respaldo.md` (guion, no video) | Alexander (Web/captura); Bryan (#241); Anthony (grabación propuesta) | Ver tabla; no hay hash de captura A/B/C ni video | **Lámina 4 con columna A real; B/C bloqueadas por #241; video falta grabar.** La columna A lleva el costo estimado de #240 (escenario sintético `demo-base`, commit `6adcf53`, USD 19,94); B, C y la recomendación quedan «en integración». La pantalla #242 existe en develop como maqueta. Alexander captura la pantalla A/B/C con `demo-quito-planificacion` cuando Bryan integre #241; sin cifras o recomendación no respaldadas. |
| 4 | Validación | DOCX/PDF de checkpoint e informe disponibles; `hojas-feria.md` y evidencia de suites son apoyos, no resultados sectoriales | Anthony (registro de pruebas), Joseph (consolidación) | Ver tabla; no hay resultados de entrevistas ni corrida oficial | **Falta evidencia de validación**: completar registros de pruebas/entrevistas y revisión humana. La ronda E2E #118 está cerrada y no equivale a validación sectorial. |
| 5 | Viabilidad | PPTX/PDF: lámina 6; no hay ficha editable/PDF independiente final | Jorge (recursos/despliegue), Bryan (costos/riesgos) | Hashes del pitch en tabla; ficha independiente: — | **Falta ficha completa** de tecnología, recursos, implementación y riesgos; falta calibrar y verificar los costos. Lámina provisional disponible. |
| 6 | Modelo de negocio | PPTX/PDF: lámina 7; la nota de modelo/piloto es fuente de contenido, no archivo final exportado | Jorge (propuesta comercial), Joseph (edición) | Hashes del pitch en tabla; ficha independiente: — | **Falta ficha final** de cliente, sostenibilidad, costos y validación de precio; no inventar cliente confirmado ni tarifa. Lámina provisional disponible. |
| 7 | Pitch | Editable `materiales/pitch-v2.pptx`; PDF `materiales/pitch-v2.pdf`; nueve PNG de render | Joseph (coordinación y pitch), revisión de todos | Ver tabla de hashes | **Listo como v2 provisional.** Faltan ensayos reales, revisión final, congelamiento y entrega. |

## SHA-256 de archivos presentes

Los hashes corresponden a los archivos listados, en el estado actual del repositorio. Los espacios “—” indican que el artefacto todavía no existe.

| Archivo | SHA-256 |
|---|---|
| `materiales/pitch-v2.pptx` | `c4945b675c281532a2a353fca6cfdbef536599dfb66aeda19d096d44b3fc0028` |
| `materiales/pitch-v2.pdf` | `90f1bf94c3713110774e041b7336de24a3dc6d1b9f3f1be81bb0a0a373a8e4df` |
| `materiales/generar_v2.py` | `b53e031bb9c33ee58d61797e7a8cf3f7c7c8364ab284fef5dac7b25bd9129c0d` |
| `materiales/entregable-2/IstpetDev-Entregable-2-Solucion.docx` | `407fa5f5862fafbf9e6f427f220ac56a3a630528224edeff897aff303333e75e` |
| `materiales/entregable-2/IstpetDev-Entregable-2-Solucion.pdf` | `fc5f7bda2804e2b0535a2c185c22d020b4595a54676e151ca1cc690ff153585f` |
| `materiales/informe-checkpoint/IstpetDev-Informe-Checkpoint-16oct.docx` | `9263464134311d26f2aa5db5e2ad757db7e9c80e5aa0570f17690594443222f5` |
| `materiales/informe-checkpoint/IstpetDev-Informe-Checkpoint-16oct.pdf` | `e9de41b7fe3a5e08a3ee494e1b309ba9ae907c9ec47451ce56625f4b7362920a` |
| `materiales/capacitacion/IstpetDev-Hojas-Capacitacion-e-Indicadores.docx` | `3bea0777d7b7d6cf456713b842469d89793b0c1d69436d723ce1a9620cf95e92` |
| `materiales/capacitacion/IstpetDev-Hojas-Capacitacion-e-Indicadores.pdf` | `43002cc916b560df28480b34a10b8ffddc14a4d885cc65d3d73af74d213b0f62` |
| `guion-video-respaldo.md` | `94994829161b3020592b40a24a1acc1e620590828f2a800ba64b3c3520f13aad` |
| `registro-ensayos.md` | `9c31d35ee5ac01fe6d71edacdad9bc5210ccdb7fe50b959a0779779a3b8968aa` |
| `checklist-entrega.md` | `6057e2fcc4d377954b3d2881db37bd92bcd710349accbbdeb836c67391bc42f5` |
| `materiales/render-v2/diapositiva-1.png` | `c45a83e5b12f337ee2586fbfa54b6d8abd3eb3f3f57b10cc9477913ebf02741c` |
| `materiales/render-v2/diapositiva-2.png` | `47dd35a5c1a2dc04d254f85b0a919aed728cbd43e1231d77ad83b14a7849c707` |
| `materiales/render-v2/diapositiva-3.png` | `e13f6a97a02e6656e5d4fc9f61a894d1f3d1e3a6e7716e1684d294cf65177da8` |
| `materiales/render-v2/diapositiva-4.png` | `5d96559e79b0ebf9b9c790f8daecebd8e24faa8eb504375b3919c24aa0cb3b08` |
| `materiales/render-v2/diapositiva-5.png` | `58976af1530177150988faa728d439a4a7c789de7fccd42da4724620e88ffe3b` |
| `materiales/render-v2/diapositiva-6.png` | `11b659f5251d0708c042faa0b96a9ba6cbb62b013ef601f42bdf6d7ef02209bd` |
| `materiales/render-v2/diapositiva-7.png` | `ab2366cf6dc137261f2ef8b8f99ba67bc17274cb1788e9bc8091c6fb88345782` |
| `materiales/render-v2/diapositiva-8.png` | `a5591839413e6de59d982e120f39d1c52348d936929deb2b905398a1157c247a` |
| `materiales/render-v2/diapositiva-9.png` | `88c38723222ff71d76c7b371052c84f2cdcea5df4f8083fa018a13c07bd0ebdb` |

## Capturas pendientes

| Captura | Quién | Condición / estado |
|---|---|---|
| Pantalla A/B/C conectada | Alexander Martínez (Web), con apoyo técnico de Bryan Baño | Pendiente: #242 está en develop; el contrato real depende de #241. Tomar con `demo-quito-planificacion` después de integrar y comprobar el recorrido. Mostrar valores solo si la matriz y la fuente de datos los respaldan. |
| Video de respaldo | Anthony Pilatasig (grabación propuesta), equipo en pantalla | El guion está listo; falta grabar y revisar duración, audio y evidencia. |
| Acuse de entrega | Joseph Baño (registro), capitán entrega | Pendiente hasta completar la entrega; anotar hora, receptor, canal y archivos realmente recibidos en `checklist-entrega.md`. |

## Fuentes de estado

- Lista oficial: [Evaluación y entregables](../../boveda/01%20Oficial/Evaluacion%20y%20entregables.md).
- Alcance demostrable: [matriz de afirmaciones](matriz-afirmaciones.md).
- Horario/canal: [Cronograma oficial](../../boveda/01%20Oficial/Cronograma%20oficial.md) y O-06 en [Consultas para la organización](../../boveda/01%20Oficial/Consultas%20para%20la%20organizacion.md).
- Los materiales son candidatos para la carpeta de Drive; hasta recibir el acuse no se reportarán como entregados.
