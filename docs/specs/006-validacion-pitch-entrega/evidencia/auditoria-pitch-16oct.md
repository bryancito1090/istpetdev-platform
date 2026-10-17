# Auditoría de capacidades del pitch — 16-oct-2026

Árbol auditado: `origin/develop` en `aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0`. Auditoría documental y estática: no se levantó la aplicación, no se ejecutó una demo, no se alteró ninguna base de datos y no se repitieron las suites del producto. Las ejecuciones de otros PR se atribuyen a sus registros. [Matriz vigente](../matriz-afirmaciones.md).

## Consultas y criterio de corte

Se ejecutaron `gh issue view 200 --comments`, `gh issue view N --json number,title,state,body,comments,url,closedAt,updatedAt` para N = 200, 235–249 y 253, y `gh pr list --state merged --search "merged:>=2026-10-16" --limit 100 --json number,title,mergedAt,mergeCommit,headRefName,baseRefName,url`. Se consultaron también los PR abiertos y el cuerpo/archivos de los PR relevantes. Los estados siguientes son una instantánea, no una consulta permanente.

La búsqueda de GitHub usa fechas UTC; se conserva su alcance literal, aunque no coincida con la medianoche de Ecuador. Solo un commit ancestro del corte cuenta como integrado. Los PR de promoción a `main` no acreditan capacidades adicionales de `develop`.

## Issues y sección «Para el pitch»

| Referencia | Estado consultado | Texto para el pitch | Observación |
|---|---|---|---|
| [#235](https://github.com/bryancito1090/istpetdev-platform/issues/235) | OPEN | Sin sección literal; cuerpo revisado | Clasificación y frase acotada en la matriz. |
| [#236](https://github.com/bryancito1090/istpetdev-platform/issues/236) | OPEN | Sin sección literal; cuerpo revisado | Clasificación y frase acotada en la matriz. |
| [#237](https://github.com/bryancito1090/istpetdev-platform/issues/237) | OPEN | Sin sección literal; cuerpo revisado | Clasificación y frase acotada en la matriz. |
| [#238](https://github.com/bryancito1090/istpetdev-platform/issues/238) | CLOSED | Sin sección literal; cuerpo revisado | Cerrado por absorción en #243; no por ensayo. |
| [#239](https://github.com/bryancito1090/istpetdev-platform/pull/239) | MERGED | Sin sección literal; cuerpo revisado | El número corresponde al PR documental #239. |
| [#240](https://github.com/bryancito1090/istpetdev-platform/issues/240) | CLOSED | Sí; revisado | Clasificación y frase acotada en la matriz. |
| [#241](https://github.com/bryancito1090/istpetdev-platform/issues/241) | OPEN | Sí; revisado | Clasificación y frase acotada en la matriz. |
| [#242](https://github.com/bryancito1090/istpetdev-platform/issues/242) | CLOSED | Sí; revisado | Código presente por commit 5d0e326; cierre no acredita integración del backend. |
| [#243](https://github.com/bryancito1090/istpetdev-platform/issues/243) | CLOSED | Sí; revisado | Cerrado aunque el PR #257 deja criterios pendientes. |
| [#244](https://github.com/bryancito1090/istpetdev-platform/issues/244) | OPEN | Sí; revisado | Clasificación y frase acotada en la matriz. |
| [#245](https://github.com/bryancito1090/istpetdev-platform/issues/245) | CLOSED | Sí; revisado | Clasificación y frase acotada en la matriz. |
| [#246](https://github.com/bryancito1090/istpetdev-platform/issues/246) | OPEN | Sí; revisado | Clasificación y frase acotada en la matriz. |
| [#247](https://github.com/bryancito1090/istpetdev-platform/issues/247) | OPEN | Sí; revisado | Clasificación y frase acotada en la matriz. |
| [#248](https://github.com/bryancito1090/istpetdev-platform/issues/248) | OPEN | Sí; revisado | Clasificación y frase acotada en la matriz. |
| [#249](https://github.com/bryancito1090/istpetdev-platform/issues/249) | OPEN | Sí; revisado | Clasificación y frase acotada en la matriz. |

En #200 se revisaron los comentarios sobre materiales, el nuevo norte de planificación y el ensayo técnico de #240. #235–#238 no contienen la sección literal «Para el pitch» en el cuerpo consultado; se revisaron sus requisitos y la tabla equivalente del comentario de #200. El #239 es documentación fusionada. #253 mantiene pendientes el ensayo y las dependencias del módulo.

## PR fusionados desde el 16-oct

| PR | Título | Base | Commit | En el corte de develop |
|---|---|---|---|---|
| [#262](https://github.com/bryancito1090/istpetdev-platform/pull/262) | feat(web): seguimiento de la ruta en el Planificador — bodega, retiro… | develop | `aae1d61` | Sí |
| [#261](https://github.com/bryancito1090/istpetdev-platform/pull/261) | feat(logistica): hora pico por franja en la duración de cada tramo (#245) | develop | `5148395` | Sí |
| [#258](https://github.com/bryancito1090/istpetdev-platform/pull/258) | feat(web): seleccion y priorizacion de pedidos pendientes en el asist… | develop | `f5c4b5f` | Sí |
| [#257](https://github.com/bryancito1090/istpetdev-platform/pull/257) | chore(datos): dataset demo-quito-planificacion con ubicaciones, flota e incidentes reales (#243) | develop | `544711d` | Sí |
| [#256](https://github.com/bryancito1090/istpetdev-platform/pull/256) | docs(006): informe de avance del checkpoint del 16-oct — evolución de la idea | develop | `3445185` | Sí |
| [#255](https://github.com/bryancito1090/istpetdev-platform/pull/255) | docs(agent): registrar la sincronizacion productiva de workflows n8n | develop | `bf774eb` | Sí |
| [#254](https://github.com/bryancito1090/istpetdev-platform/pull/254) | feat(logística): costo total estimado de servir una ruta (#240) | develop | `6adcf53` | Sí |
| [#252](https://github.com/bryancito1090/istpetdev-platform/pull/252) | fix(campo): modo sin conexión de bodega y retiro de conductor | develop | `c45c894` | Sí |
| [#251](https://github.com/bryancito1090/istpetdev-platform/pull/251) | release: develop → main (agente con abastecimiento, revisión de datasets y propuesta única) | main | `438c2d6` | No; no se usa como prueba de integración |
| [#250](https://github.com/bryancito1090/istpetdev-platform/pull/250) | feat(web): estandarizar diseno ejecutivo stripe dashboard y arquitectura ui-ux (#166) | develop | `617680a` | Sí |
| [#239](https://github.com/bryancito1090/istpetdev-platform/pull/239) | docs(boveda): planificación potenciada y alineación de la bóveda con el norte del 16-oct | develop | `82c4f65` | Sí |
| [#234](https://github.com/bryancito1090/istpetdev-platform/pull/234) | docs(boveda): transcribir diapositivas de la charla de logística integral | develop | `d77a153` | Sí |
| [#233](https://github.com/bryancito1090/istpetdev-platform/pull/233) | docs(boveda): incorporar charla de logística integral para el Reto 1 | develop | `db9284b` | Sí |
| [#229](https://github.com/bryancito1090/istpetdev-platform/pull/229) | feat(logística): propuesta única por prioridad, vehículo adecuado y costo con incertidumbre verificable (#207) | develop | `b2cc9bd` | Sí |
| [#228](https://github.com/bryancito1090/istpetdev-platform/pull/228) | feat(importador,n8n): revisión de viabilidad de datasets coordinada por n8n | develop | `09f0f25` | Sí |
| [#227](https://github.com/bryancito1090/istpetdev-platform/pull/227) | feat(mobile): rediseñar dashboard con interfaz estilo Stripe y paleta… | develop | `1171afe` | Sí |
| [#226](https://github.com/bryancito1090/istpetdev-platform/pull/226) | docs(207): verificación de integración E2E y brechas por criterio | develop | `0e8ffef` | Sí |
| [#225](https://github.com/bryancito1090/istpetdev-platform/pull/225) | release: develop → main (consultas del agente, rediseño de inicio, login en español y materiales del pitch) | main | `9d5f856` | No; no se usa como prueba de integración |
| [#224](https://github.com/bryancito1090/istpetdev-platform/pull/224) | feat(web): Rediseño ejecutivo de inicio con maquetación asimétrica, sidebar y guía UI-UX | develop | `9a0e822` | Sí |
| [#223](https://github.com/bryancito1090/istpetdev-platform/pull/223) | docs(qa): guion estructurado para ronda corta E2E por roles (#118) | develop | `ad29e52` | Sí |
| [#222](https://github.com/bryancito1090/istpetdev-platform/pull/222) | docs(spec-006): lista de verificación E2E de punta a punta (#207) | develop | `f7843d4` | Sí |
| [#221](https://github.com/bryancito1090/istpetdev-platform/pull/221) | docs(pitch): diapositivas provisionales v1 y manifiesto | develop | `9b1ebfe` | Sí |
| [#220](https://github.com/bryancito1090/istpetdev-platform/pull/220) | release: develop → main (hora límite de críticos, carga parcial por capacidad, prioridad en el asistente y productos por parada) | main | `16d4a69` | No; no se usa como prueba de integración |
| [#219](https://github.com/bryancito1090/istpetdev-platform/pull/219) | docs(pitch): banco de respuestas de 30s (24 preguntas) | develop | `04b8056` | Sí |
| [#218](https://github.com/bryancito1090/istpetdev-platform/pull/218) | docs(pitch): actualizar guion maestro de 5 min (T3) | develop | `66006ff` | Sí |
| [#217](https://github.com/bryancito1090/istpetdev-platform/pull/217) | docs(006): entregable 2 Solución como documento formal (Word + PDF) | develop | `42bd1b0` | Sí |
| [#216](https://github.com/bryancito1090/istpetdev-platform/pull/216) | fix(logistica): planificar carga parcial según capacidad y conservar el remanente | develop | `0c296bf` | Sí |
| [#215](https://github.com/bryancito1090/istpetdev-platform/pull/215) | feat(api): consultas para el agente de stock por bodega, origen sugerido y vehiculos disponibles (#194) | develop | `a602edd` | Sí |
| [#214](https://github.com/bryancito1090/istpetdev-platform/pull/214) | Integrar prioridad, stock y validaciones de rutas (#196, #201) | develop | `5dc3470` | Sí |
| [#213](https://github.com/bryancito1090/istpetdev-platform/pull/213) | feat(agent): herramientas n8n de abastecimiento para el agente logístico | develop | `233b653` | Sí |
| [#211](https://github.com/bryancito1090/istpetdev-platform/pull/211) | docs(boveda): entregable 2 Solución y plan del evento alineado a la agenda oficial | develop | `f2219d5` | Sí |
| [#206](https://github.com/bryancito1090/istpetdev-platform/pull/206) | feat(api): el asistente ordena entregas por prioridad y usa las ventanas del punto | develop | `621fdc0` | Sí |
| [#205](https://github.com/bryancito1090/istpetdev-platform/pull/205) | feat(motor): hora límite para puntos críticos en el planificador | develop | `5cfc657` | Sí |
| [#203](https://github.com/bryancito1090/istpetdev-platform/pull/203) | docs(boveda): documentar contingencia de red y validacion movil #198 | develop | `4053ed2` | Sí |
| [#192](https://github.com/bryancito1090/istpetdev-platform/pull/192) | docs(boveda): cronograma oficial con convocatorias y brainstorm de ampliación del evento | develop | `1dcfb51` | Sí |
| [#191](https://github.com/bryancito1090/istpetdev-platform/pull/191) | docs(boveda): completar charla y consolidar hallazgos del reto 1 | develop | `c147d2a` | Sí |
| [#190](https://github.com/bryancito1090/istpetdev-platform/pull/190) | docs(boveda): registrar agenda general oficial y matriz de mentorias | develop | `ea29ddc` | Sí |
| [#186](https://github.com/bryancito1090/istpetdev-platform/pull/186) | docs(boveda): registrar charla de limpieza y logistica del evento | develop | `cb1022f` | Sí |
| [#185](https://github.com/bryancito1090/istpetdev-platform/pull/185) | fix(rutas): mostrar los productos de cada parada y lo que se carga en bodega | develop | `0ab4bd9` | Sí |
| [#183](https://github.com/bryancito1090/istpetdev-platform/pull/183) | release: develop → main (rediseño PWA de campo y notas colaborativas del evento) | main | `9621ec4` | No; no se usa como prueba de integración |
| [#182](https://github.com/bryancito1090/istpetdev-platform/pull/182) | feat(campo): actualizar identidad visual, tipografia local y experiencia de login/home en PWA | develop | `0c7581d` | Sí |
| [#181](https://github.com/bryancito1090/istpetdev-platform/pull/181) | docs(evento): registrar primera intervención comercial | develop | `faa3d22` | Sí |
| [#180](https://github.com/bryancito1090/istpetdev-platform/pull/180) | docs(boveda): organizar notas colaborativas del evento | develop | `c63d17e` | Sí |
| [#179](https://github.com/bryancito1090/istpetdev-platform/pull/179) | release: develop → main (mapa sin rectas con OSRM saturado) | main | `9bf640c` | No; no se usa como prueba de integración |
| [#178](https://github.com/bryancito1090/istpetdev-platform/pull/178) | fix(rutas): el mapa ya no se queda en rectas cuando el OSRM público se satura | develop | `4e0b273` | Sí |
| [#177](https://github.com/bryancito1090/istpetdev-platform/pull/177) | docs(boveda): registrar notas y ficha de participante de Bryan Baño | develop | `7c164d4` | Sí |
| [#176](https://github.com/bryancito1090/istpetdev-platform/pull/176) | docs(boveda): plantilla de notas y contexto del participante | develop | `7f9e140` | Sí |
| [#175](https://github.com/bryancito1090/istpetdev-platform/pull/175) | release: develop → main (rutas al conductor, PWA sin señal, custodia y simulador) | main | `f178b3d` | No; no se usa como prueba de integración |
| [#174](https://github.com/bryancito1090/istpetdev-platform/pull/174) | docs(boveda): contrato del #160 y registro de la promoción a main del 16-oct | develop | `ce95ca2` | Sí |
| [#173](https://github.com/bryancito1090/istpetdev-platform/pull/173) | fix(campo): el título de la etiqueta no se restaura por temporizador (#161) | develop | `97a0f11` | Sí |
| [#172](https://github.com/bryancito1090/istpetdev-platform/pull/172) | feat(web): detalle de la planificacion por ruta, producto y conductor… | develop | `05a85e4` | Sí |
| [#171](https://github.com/bryancito1090/istpetdev-platform/pull/171) | fix(campo): sincronizacion con diagnostico y evidencias en receptor | develop | `3c1bcf4` | Sí |
| [#170](https://github.com/bryancito1090/istpetdev-platform/pull/170) | fix(campo): nombre único del PDF de la etiqueta QR (#161) | develop | `8614971` | Sí |
| [#169](https://github.com/bryancito1090/istpetdev-platform/pull/169) | feat(simulation): dataset reproducible y cifras canónicas para el simulador de 90 días | develop | `7abd96c` | Sí |
| [#168](https://github.com/bryancito1090/istpetdev-platform/pull/168) | fix(campo): estados del bulto y validación de custodia Bodega → Conductor → Receptor | develop | `015b4a9` | Sí |
| [#159](https://github.com/bryancito1090/istpetdev-platform/pull/159) | fix(publicación): las rutas publicadas llegan al conductor | develop | `e26deec` | Sí |
| [#158](https://github.com/bryancito1090/istpetdev-platform/pull/158) | fix(campo): abrir la PWA sin señal y capturar (spec 015) | develop | `cfa5512` | Sí |

## PR abiertos pertinentes al corte

| PR | Capacidad | Base |
|---|---|---|
| [#265](https://github.com/bryancito1090/istpetdev-platform/pull/265) | fix(campo): UUID v7 monótono dentro del mismo milisegundo (prueba intermitente de la cola) | develop |
| [#264](https://github.com/bryancito1090/istpetdev-platform/pull/264) | fix(logistica): reserva antes del despacho, cancelar ruta y cantidad por línea en el asistente (#199) | develop |
| [#263](https://github.com/bryancito1090/istpetdev-platform/pull/263) | feat(campo): costo de retroceso estimado por recepción con rechazo en Novedades (#237) | develop |
| [#260](https://github.com/bryancito1090/istpetdev-platform/pull/260) | feat(campo): motivo de rechazo ErrorRotulado y resumen por motivo en Novedades (#236) | develop |
| [#259](https://github.com/bryancito1090/istpetdev-platform/pull/259) | release: develop → main (costo total de servir, comparación A/B/C, priorización de pedidos y dataset de Quito) | main |

## Integridad del fixture

Dataset `demo-quito-planificacion`, semilla `20261016` declarada en el manifiesto y el generador. SHA-256 recalculado leyendo los bytes del árbol auditado. No se regeneraron archivos: la llamada a OSRM puede cambiar la matriz y el generador no fija una instantánea externa.

| Archivo | SHA-256 esperado por manifest.json | SHA-256 observado | Resultado |
|---|---|---|---|
| `bodegas.csv` | `3f9eb3f6f70f42e268a91665bb1fae86782dba82f32a80c719e1fc4d3c3a980a` | `3f9eb3f6f70f42e268a91665bb1fae86782dba82f32a80c719e1fc4d3c3a980a` | Coincide |
| `puntos.csv` | `1453e6a29344db1e23e3200ee753a425c0c61239ef96f512584d2ced4a1ab711` | `1453e6a29344db1e23e3200ee753a425c0c61239ef96f512584d2ced4a1ab711` | Coincide |
| `productos.csv` | `110f7a6a8993b46526e2cf4293eb1b0dc9fe01cef99a9e50c3140fceba3738e6` | `110f7a6a8993b46526e2cf4293eb1b0dc9fe01cef99a9e50c3140fceba3738e6` | Coincide |
| `lotes.csv` | `bc18a5f320e7f98778140424266eb15cad18d9980cd6cb55a34488cc23fc5484` | `bc18a5f320e7f98778140424266eb15cad18d9980cd6cb55a34488cc23fc5484` | Coincide |
| `vehiculos.csv` | `f56918cd13f04bc76ce5c5fc11ffc5eb57a446f5e664674840ab18b09e127cdb` | `f56918cd13f04bc76ce5c5fc11ffc5eb57a446f5e664674840ab18b09e127cdb` | Coincide |
| `consumos.csv` | `89ebebf22ec703391666651ca90967f565c770ded12bf76f14f94d137b32a1ce` | `89ebebf22ec703391666651ca90967f565c770ded12bf76f14f94d137b32a1ce` | Coincide |
| `inventario.csv` | `76f8f827b9ee178759abd84abad014f9901a9be26ebc6ea7a7a17dc83c76c98d` | `76f8f827b9ee178759abd84abad014f9901a9be26ebc6ea7a7a17dc83c76c98d` | Coincide |
| `tramos.csv` | `006e688449e38e3457bb0efea830b267a92851aabecd2e86037711ceb8c4e777` | `006e688449e38e3457bb0efea830b267a92851aabecd2e86037711ceb8c4e777` | Coincide |
| `incidentes.json` | `f1e55c426bf7b700c7c4ad4ba595f80d958f605a6c26e502aca61f8b56aa2b0b` | `fe6deabec55eda09714e982ff357c1361f4fb60dd5a118076a0d0f0db69f86a9` | DIFIERE |
| `mapping.json` | `3f8fba950b5cf529c8b71a5948e55d0530683ecedd80c9c616356595111d21a2` | `cbd20f51daf9d70132d82e72e61a2504152478a34331fbfbbcc972af1d9d0e31` | DIFIERE |

Hash del propio `manifest.json`: `031edbc121e27ca628141eb6d5769e2f77b3988ecacfec3b6cd925e4a43d625d`.

`git ls-tree -r --name-only aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0` no contiene `appsettings.demo-quito.json`. El generador tampoco escribe ese archivo. `TrafficBandsQuitoTests` introduce franjas mediante `ExtraConfig`: sus pruebas no demuestran que una demo desplegada cargue esa configuración.

## Evidencia de las discrepancias

- [src/Application/Logistics/SourcingService.cs:452](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/src/Application/Logistics/SourcingService.cs#L452): AllocateDemandsCoreAsync construye A con IsRecommended: true y B condicional con false; no ofrece C.
- [src/Application/Logistics/LogisticsOptions.cs:1](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/src/Application/Logistics/LogisticsOptions.cs#L1): Opciones de ServiceCost y TrafficBands presentes; no hay ContractedCarrier, Recommendation ni VehicleRestrictions.
- [apps/web/src/pages/rutas/rutas.page.ts:2479](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/apps/web/src/pages/rutas/rutas.page.ts#L2479): el estado de modo iniciaba en maqueta; la ausencia de opciones también activaba el ejemplo. El mapeo API completaba costos, críticos y pendientes con constantes.
- [apps/web/src/pages/rutas/route-comparison.model.ts:175](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b1c1ed1c248918eea64bcc683e3ecf0/apps/web/src/pages/rutas/route-comparison.model.ts#L175): las opciones y referencia de combustible de Quito eran literales, no una corrida identificada. La etiqueta Petroecuador del precio de ejemplo no era una fuente verificada.
- [tests/IstpetDev.Tests/Performance/GenerateBenchmarkArtifactsTest.cs:26](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/tests/IstpetDev.Tests/Performance/GenerateBenchmarkArtifactsTest.cs#L26): Usa base en memoria y no importa el dataset; el export lleva un marcador de commit, no un SHA.
- [src/Infrastructure/Simulation/SimulationWorker.cs:103](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/src/Infrastructure/Simulation/SimulationWorker.cs#L103): Lista fija de puntos; más adelante deriva km de entregas y costo de km mediante constantes.
- [tests/IstpetDev.Tests/Integration/Logistics/DemoQuitoPlanificacionTests.cs:22](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/tests/IstpetDev.Tests/Integration/Logistics/DemoQuitoPlanificacionTests.cs#L22): Comprueba importación, columnas, riesgo, publicación y avería; no comprueba la comparación contratada ni primera parada de ventana estrecha.
- [tests/IstpetDev.Tests/Integration/Logistics/TrafficBandsQuitoTests.cs:15](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/tests/IstpetDev.Tests/Integration/Logistics/TrafficBandsQuitoTests.cs#L15): Configuración inyectada en pruebas; no archivo de appsettings desplegado.
- [scripts/data/generate-demo-quito-planificacion.py:31](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/scripts/data/generate-demo-quito-planificacion.py#L31): Semilla fija, fuentes OSM por punto, bodegas/negocios y operación sintéticos; dependencia externa para tramos.
- [src/Domain/Common/ReceiptRejectionReasons.cs:13](https://github.com/bryancito1090/istpetdev-platform/blob/aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0/src/Domain/Common/ReceiptRejectionReasons.cs#L13): Catálogo V1 sin ErrorRotulado en el corte.

La matriz conserva el ensayo de costo de #254 como ensayo técnico con fixture `demo-base` y el run del agente de #255 como ejecución productiva registrada por el equipo. No extrapola esos resultados a la UI comparativa, al fixture Quito ni a un ensayo del pitch.

## Comprobaciones de esta auditoría

- Todas las fusiones citadas como implementación se comprobaron con `git merge-base --is-ancestor` contra el corte; `5d0e326` también es ancestro.
- Se inspeccionaron las fuentes y contratos indicados, los registros de ensayos y los artefactos JSON de costo/simulación.
- Los hashes y la ausencia de la configuración son hallazgos reproducidos por esta auditoría, no inferencias del estado de los issues.
- Las referencias públicas consultadas y sus límites están en la tabla de procedencia de la matriz. OSM no fue accesible mediante la consulta web: se conserva la trazabilidad por IDs, sin fingir revalidación de cada coordenada.
- La comprobación documental final se registra en el PR; no equivale a pruebas funcionales del producto.

Resultado documental: `python3 -I docs/boveda/.github/scripts/check_vault.py` → **109 notas, 1201 enlaces internos, 0 errores y 4 avisos preexistentes**. `git diff --check` sin errores. Comprobación adicional: enlaces locales existentes, identificadores A-01–A-35 y P-01–P-16 presentes y commits citados ancestros del corte. La consulta final `git ls-remote origin refs/heads/develop` seguía devolviendo `aae1d61b8b8c1ed1c248918eea64bcc683e3ecf0`.
