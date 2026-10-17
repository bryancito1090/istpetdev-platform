# Manifiesto del paquete de entrega — v3 (v2 como respaldo)

**Estado:** paquete en preparación. Ningún archivo está congelado ni consta como entregado. La lista oficial contiene siete entregables; los nombres y formatos finales dependen de la respuesta completa a O-06. El canal comunicado es la carpeta de Drive del capitán, pero faltan URL y formatos confirmados.

Los archivos existentes son propuestas editables/exportadas, no validación de que cada entregable oficial esté completo. “Listo” significa archivo provisional disponible y revisable; no significa congelado ni recibido. Las propuestas de responsable deben confirmarse por el equipo. Los SHA-256 se recalculan en cada modificación y al congelar.

## Estado frente a los siete entregables oficiales

| # | Entregable | Archivo editable / PDF actualmente disponible | Responsable propuesto | SHA-256 | Estado y faltante concreto |
|---:|---|---|---|---|---|---|
| 1 | Problema | PPTX: `materiales/pitch-v2.pptx` (lámina 1); PDF: `materiales/pitch-v2.pdf` | Joseph (contenido/pitch), revisión de Bryan | Ver tabla de hashes, PPTX/PDF | **Listo como lámina provisional**. Falta revisión final de contenido/evidencia contra matriz y formato según O-06. |
| 2 | Solución | DOCX/PDF: `materiales/entregable-2/IstpetDev-Entregable-2-Solucion.docx/.pdf`; también lámina 2 en pitch PPTX/PDF | Bryan (contenido técnico), Joseph (edición) | Ver tabla de hashes | **Listo como propuesta provisional**. Falta validar/adjuntar diagrama final si lo requiere la organización y confirmar formato O-06. |
| 3 | Prototipo / demo | PPTX/PDF: láminas 3–4; PNG existentes en `materiales/entregable-2/img/`; video: `guion-video-respaldo.md` (guion, no video) | Alexander (Web/captura); Bryan (#241); Anthony (grabación propuesta) | Ver tabla; no hay hash de captura A/B/C ni video | **Lámina 5 (v3) con A/B/C del motor; video falta grabar.** Cifras del ensayo técnico local de #285 (PR #295, `1585ab7`, `demo-quito-planificacion`): A USD 57,89 · B USD 69,89 · C contratada USD 66,90, recomendada (puntaje 90,8). Solo totales, críticos a tiempo y puntaje; el desglose por componente no se muestra hasta corregirse (#276). Falta la captura de la pantalla A/B/C conectada (Alexander) y la configuración en producción (#198). |
| 4 | Validación | DOCX/PDF de checkpoint e informe disponibles; `hojas-feria.md` y evidencia de suites son apoyos, no resultados sectoriales | Anthony (registro de pruebas), Joseph (consolidación) | Ver tabla; no hay resultados de entrevistas ni corrida oficial | **Falta evidencia de validación**: completar registros de pruebas/entrevistas y revisión humana. La ronda E2E #118 está cerrada y no equivale a validación sectorial. |
| 5 | Viabilidad | PPTX/PDF: lámina 6; no hay ficha editable/PDF independiente final | Jorge (recursos/despliegue), Bryan (costos/riesgos) | Hashes del pitch en tabla; ficha independiente: — | **Falta ficha completa** de tecnología, recursos, implementación y riesgos; falta calibrar y verificar los costos. Lámina provisional disponible. |
| 6 | Modelo de negocio | PPTX/PDF: lámina 7; la nota de modelo/piloto es fuente de contenido, no archivo final exportado | Jorge (propuesta comercial), Joseph (edición) | Hashes del pitch en tabla; ficha independiente: — | **Falta ficha final** de cliente, sostenibilidad, costos y validación de precio; no inventar cliente confirmado ni tarifa. Lámina provisional disponible. |
| 7 | Pitch | Editable `materiales/pitch-v3.pptx` (con animaciones); PDF `materiales/pitch-v3.pdf`; diez PNG en `render-v3/`. Respaldo: `pitch-v2.pptx`/`.pdf` | Joseph (coordinación y pitch), revisión de todos | Ver tabla de hashes | **v3 provisional con identidad ISTPET.** Mismo contenido que v2, más portada y la mascota Chillo en 5 láminas (frases por aprobar). La lámina A/B/C pasa a ser la 5. Faltan ensayos reales, revisión final, congelamiento y entrega. Clics por lámina en [registro de ensayos](registro-ensayos.md#diapositivas-v3-clics-y-animaciones). |

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
| `registro-ensayos.md` | `63a357ad7232bfd0ab97b8ec89e3498d536fe529fcab0deb7c29e813ebbb4e70` |
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

## Diapositivas v3 (identidad ISTPET)

Generadas con `uv run --with python-pptx --with pillow --with numpy --with scipy python docs/specs/006-validacion-pitch-entrega/materiales/generar_v3.py`. El PPTX no incrusta las fuentes: hay que instalar Oswald y Open Sans (`materiales/assets/fuentes/`) en el equipo del escenario. El PDF sí las incrusta. Si se regenera, el PDF y el PPTX cambian de hash aunque el contenido sea el mismo, así que hay que recalcular esta tabla.

| Archivo | SHA-256 |
|---|---|
| `materiales/pitch-v3.pptx` | `b04ea5635ac71eda98aa14b603d355b3d67cf6d64682459753ec4bbabfe8d0ea` |
| `materiales/pitch-v3.pdf` | `8a813f0c3fee7aedc349b9763890370ed244f901b73092797217735b00d8654d` |
| `materiales/generar_v3.py` | `dd66ba2bf5611efaa00c250c710f6c6de3199ca7bd232b337c587bc05e455631` |
| `materiales/render-v3/diapositiva-1.png` | `c7f5b4aa283bf8cfb0b3d8628ef3777b815181fb3498ce229661f8fa4998007a` |
| `materiales/render-v3/diapositiva-2.png` | `7e67c296ff4b73f236a17f69faa47ab43318e8cfd80ea93626b38f1d45619fe2` |
| `materiales/render-v3/diapositiva-3.png` | `ed15770c7f6de985dfc286db09da695bb215028ef602d70a4c75be1520ca7d40` |
| `materiales/render-v3/diapositiva-4.png` | `786d427acc6f31d8354b38feeb94f9b81109fccf1f16373cb1341a8b157fc2b2` |
| `materiales/render-v3/diapositiva-5.png` | `094cda0040b112eae1f8b43e14570941c82f8c0dcf7f5f201031e7faf61a8e42` |
| `materiales/render-v3/diapositiva-6.png` | `f54c75ac1890ef1b46b9093dad18d3ded5108e0ff545acdf1b77ed2333680639` |
| `materiales/render-v3/diapositiva-7.png` | `221f6ce278f50bb7f0362175a33c2d1cbb7f1140e24d340bf0576189db420982` |
| `materiales/render-v3/diapositiva-8.png` | `88f8fd8634f237d57a8ea176ef3ab0e4fcb680756479bb074623498f8c99648e` |
| `materiales/render-v3/diapositiva-9.png` | `dd71117cccf7db5bcc2475d75cc44063dd44fcff026b7fdea0f8f475f2fe4cc7` |
| `materiales/render-v3/diapositiva-10.png` | `745284a6e7cb824ec571186b0944792d03aec1b6baee9fb5d20d1f9a252c9588` |
| `materiales/assets/chillo-el-gallo.png` | `c8b5eb20e2fea7724d4647a215b98290f6d59eedfb09ab5a2ba193ac0f741e75` |
| `materiales/assets/ISTPET-ORIGINAL.png` | `12ffa1c0d59a2bfe0ffa65be0d489f9d07b9bb98be15f3e8c5051c4af93fd0ae` |
| `materiales/assets/ISTPET-FONDO-AZUL.jpg` | `eed1dd9fb9f543f388c1ea0454ffbaf1b1043ac8a13ba80494017ab705eeefa1` |
| `materiales/assets/ISTPET-ORIGINAL.jpg` | `225b5ee9a8658dbfd05ecdbb20ea58a85aeceb6e3bb541314c5c4010f4703808` |
| `materiales/assets/derivados/captura-rutas-recorte.png` | `6f0232c512ebd59dd74f2d8fa14c63be912876a127ebe6936be5b5f542dfaffd` |
| `materiales/assets/derivados/chillo-cuerpo-entero.png` | `b8f630596941838873fc6de128162e519edfed3471d3ebc66afd27eef10e64af` |
| `materiales/assets/derivados/chillo-medio-cuerpo.png` | `015c1e702381ff9376b9f2e9219e69bc6a57c71ba78416d4ff88c93ff379a23f` |
| `materiales/assets/derivados/istpet-logo-blanco.png` | `1b04b2e0ba1bc98f91cca5f591ceab1e83afe2d562677af02ee53d4f2f844e30` |
| `materiales/assets/derivados/istpet-logo.png` | `52c7076d8f159a01729cc92d61b9b532853b1cecb6bcfc7a06fc3e939628abcd` |
| `materiales/assets/fuentes/OpenSans-Italic.ttf` | `fe269381e992f32e135801740998544d6235061e37c93ec067ad2be3edd5b17b` |
| `materiales/assets/fuentes/OpenSans.ttf` | `36643644f318a812aab2d2ed3bb98f8cf0872527f835fe9398d95fe6b9adb878` |
| `materiales/assets/fuentes/Oswald.ttf` | `5b38c246e255a12f5712d640d56bcced0472466fc68983d2d0410ec0457c2817` |
| `materiales/assets/fuentes/README.md` | `6e9c618c8a3e48edbb251667aeb8eb34b6e7a422fd9c3a6499e2c074fec008b0` |

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
