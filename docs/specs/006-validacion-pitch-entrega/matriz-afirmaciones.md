# 006 — Matriz de afirmaciones del pitch (T2, fase 2 del issue #46)

**Preparada:** 14-oct-2026 por Claude Code (Opus 5.5) a pedido de Joseph Baño, sobre `origin/develop` en `7aed1eb`. **Revisión humana:** Joseph informó el 14-oct que aprobó el borrador `8693f73`; E2E-05 exige un segundo revisor y volver a comprobar A-16/A-17/A-27, ajustadas después de esa aprobación. La matriz sigue como borrador de T2; CE-01 quedó registrado en E2E-01. [Spec](spec.md) · [Tareas](task.md) · [Ensayos](test-e2e.md) · [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md).

## Reglas de lectura

- **usable**: hay una ejecución enlazada (fecha, commit, entorno) que sostiene la frase **dentro del límite indicado**. Se puede pronunciar tal cual.
- **propuesta**: falta ejecución, la ejecución no cubre todo el CE o la evidencia no es reproducible. Solo se pronuncia la **frase de sustitución**, que la presenta como propuesta, diseño o pendiente.
- **retirar**: no se pronuncia ni aparece en diapositivas. Se usa la sustitución o se omite.
- Un CE escrito sin ejecución nunca da `usable`. Una casilla `[x]` en un `task.md` tampoco: cuenta la fila del registro de ejecuciones o una ejecución repetida.
- **Modo de escena** es otra dimensión: `vivo previsto` (capacidad probada en pruebas automatizadas; la escena en pantalla **no se ha ensayado** y solo se llamará «en vivo» tras E2E-04 del 006), `grabado` (video del 15-oct, aún no existe), `simulado` (005) y `propuesto` (no existe como capacidad demostrada). Hoy ninguna escena está ensayada.

## Ejecuciones de referencia

| ID | Fecha | Commit | Entorno | Qué se ejecutó | Resultado | Enlace |
|---|---|---|---|---|---|---|
| EJ-01 | 2026-10-12 | `06f2911` (local) | PostGIS descartable, Release | 001 E2E-01/02 (`DatasetJourneyTests`, `ImportServiceTests`) | pasó | [registro 001](../001-nucleo-delgado-importador/test-e2e.md#registro-de-ejecuciones) |
| EJ-02 | 2026-10-13 | `f06c072` | Docker PostGIS, WSL2/Windows 11 | 002 E2E-01–07 | pasó | [registro 002](../002-motor-logistico/test-e2e.md#registro-de-ejecuciones) |
| EJ-03 | 2026-10-13 | `55764c4` | Navegador/PWA contra `FieldMockBackendService` | 004 E2E-01–09 | pasó **simulado** (sin backend, sin teléfono) | [registro 004](../004-campo-qr-custodia-offline/test-e2e.md#registro-de-ejecuciones) |
| EJ-04 | 2026-10-13 | `33bd6ab` | «.NET 8 Backend + EF Core» | 004 E2E-01/03/04/07/10 | declarado «pasó (backend real)»; **sin artefacto** y sin prueba reproducible en `develop` (ver nota 2) | [registro 004](../004-campo-qr-custodia-offline/test-e2e.md#registro-de-ejecuciones) |
| EJ-05 | 2026-10-14 | `7018e49` (rama `feature/45-…`, fusionada en `255ac60`) | no declarado | 005 E2E-01–10 por pruebas automatizadas | «Superado»; sin `comparisonId`, ZIP, hashes, host ni cifras | [evidencia 005](../../superpowers/evidence/005-e2e.md) |
| EJ-06 | 2026-10-14 | `29572c3` | `npm run check`, Vitest, `dotnet test` | 013 E2E-00 (suites automáticas) | pasó; Cognito real (E2E-04 de 001/013) pendiente | [e2e-00 013](../013-login-y-cuentas/evidencia/e2e-00-resultados.txt) |
| EJ-07 | 2026-10-14 15:23–15:25 (-05:00) | `7aed1eb` | Arch Linux x86_64, .NET SDK 8.0.424, Docker 29.8.2 + PostGIS (Testcontainers), 16 CPU | 12 pruebas E2E/rendimiento/traza pública | 12/12 pasaron | [evidencia fase 2](evidencia/2026-10-14-suites-fase2.md) |
| EJ-08 | 2026-10-14 15:28–15:29 (-05:00) | `7aed1eb` | igual que EJ-07 | 93 pruebas de CE puntuales (riesgos, determinismo, autonomía, plantillas, agente, importador, métricas, campo, auditor) | 93/93 pasaron | [evidencia fase 2](evidencia/2026-10-14-suites-fase2.md) |

EJ-07 y EJ-08 son ejecuciones de esta fase, repetibles con el comando del archivo de evidencia. Las demás son registros del equipo; no se repitieron salvo donde EJ-07/08 lo indican.

**Nota 1 — qué no prueba ninguna ejecución:** ninguna fila anterior recorre la demo completa en pantalla, con la web y la PWA conectadas al backend desplegado, ni usa un teléfono real, HTTPS público, DeepSeek real o una corrida oficial de 005. Eso es exactamente lo que falta para cualquier frase en `vivo`.

**Nota 2 — 004 en `develop`:** las pruebas de despacho, recepción e idempotencia que existen (`Unit/Field/FieldUnitTests`) usan `FakeFieldService` a nivel de controlador; no tocan PostgreSQL. La única prueba de integración de campo es `PublicTraceHttpTests` (pasó en EJ-07). Por eso EJ-04 no habilita `usable` hasta adjuntar su evidencia o repetir el recorrido.

**Nota 3 — prueba «nacional» de 005:** `SimulationNationalTests` usa `UseInMemoryDatabase` y no importa el dataset de unos 80 puntos; demuestra que el orquestador completa 12 runs, no una corrida nacional medida.

## Matriz

Bloque = parte del Anexo 5.7 donde aparece la frase (P problema, S solución, D demo, I impacto, V viabilidad, N negocio, X próximo paso, Q solo respuesta a preguntas).

| # | Bl. | Texto exacto candidato | Spec y CE | Ejecución | Límite de lo demostrado | Estado | Frase de sustitución para la escena |
|---|---|---|---|---|---|---|---|
| A-01 | P | «En contratos públicos de limpieza hospitalaria, el proveedor está obligado a mantener los químicos de desinfección y los insumos fungibles con frecuencias de reposición definidas por el hospital» | Fuente documental, no CE: [SERCOP](../../boveda/99%20Fuentes/SERCOP%20limpieza%20hospitalaria%20y%20sanciones.md) | URL y texto comprobados el 14-oct (por IA); kit T1 aprobado por Joseph en revisión de 15 min | Dos fichas técnicas; no cuantifica costo ni multa | usable (documental) | — (si la fuente deja de poder comprobarse: retirar y abrir con A-02) |
| A-02 | P | «Cuando el hospital llama porque falta desinfectante, la emergencia ya ocurrió» | Hipótesis del equipo; 006 CE-02 (feria) | ninguna | Sin entrevista ni dato | propuesta | «Nuestra hipótesis es que hoy la reposición empieza cuando el hospital ya avisó la falta». |
| A-03 | P | «Una empresa de limpieza atiende 80 puntos del país» (texto base) | Contradicción ya registrada en el guion; D6 | ninguna | El vivo es `demo-region`; 80 puntos solo en simulación | retirar | «En nuestro escenario sintético: una bodega, 15 puntos y dos vehículos». |
| A-04 | D | «El hospital norte tiene cinco días de cobertura de desinfectante: riesgo alto» | 002 CE-01 | EJ-02; EJ-07 (`E2E_01`); EJ-08 (`DemoRegionTests`) | Fixture sintético `demo-region`; probado por API, no en pantalla | usable | (plan B si la pantalla falla) «El cálculo da cinco días de cobertura; lo mostramos en la lista de riesgos». |
| A-05 | D | «De 15 puntos, uno es crítico y cuatro tienen riesgo alto» | 002 CE-01 | EJ-08 (`DemoRegionTests`: 1/4/3/1/6) | Igual que A-04 | usable | — (opcional; omitir en 3 min) |
| A-06 | S/D | «Cada alerta muestra los datos que la explican: stock, consumo y días de cobertura; las explicaciones solo usan cifras calculadas por el motor» | 002 CE-07 | EJ-02 (E2E-07); EJ-08 (`FactsTests`, `ExplanationTemplatesTests`) | Explicación por plantilla; no la del LLM | usable | — |
| A-07 | D | «Propone una ruta para cada vehículo respetando capacidad, horarios de atención y jornada» | 002 CE-02 | EJ-07 (`E2E_02`: dos rutas); EJ-08 (`RoutePlannerTests`: capacidad, ventana, jornada) | Las pruebas comprueban factibilidad; ninguna comprueba «5 obligatorios y ≥2 de 3 de consolidación» del CE-02 | usable (sin «atiende todo») | — ; nunca «ruta óptima» |
| A-08 | Q | «Con los mismos datos, la propuesta sale idéntica» | 002 CE-03 | EJ-08 (`RoutePlanningTests`, JSON igual) | `E2E_02` no compara bytes pese a su nombre; lo cubre la prueba de integración | usable | — (solo en preguntas) |
| A-09 | D | «Ante un cierre de vía, se conservan las paradas ya hechas y se crea una nueva versión de la ruta» | 002 CE-04 (parte) | EJ-07 (`E2E_03`); EJ-08 (`ReplanningTests`) | Incidente creado en prueba; no comprueba qué vehículo cambia | usable | — |
| A-10 | D | «Una decisión se ejecuta sola por política y la otra espera aprobación» | 002 CE-04; 003 CE-03; 005 CE-03 | EJ-02 lo declara; EJ-07 `E2E_03` **no** verifica `AutoExecuted` + `Escalated` sobre `demo-region` | Solo se prueba la regla aislada y el modo «solo proponer» | propuesta | «Si el cambio supera los límites de costo o retraso, el sistema no lo ejecuta: lo deja esperando aprobación de una persona» (EJ-07 `E2E_04`, EJ-08 `AutonomyPolicyTests`). |
| A-11 | S/D | «La IA detecta el riesgo y propone / explica / redacta el aviso al conductor» | 003 CE-01/03 | ninguna (003 T7–T10 pendientes; sin DeepSeek real) | — | retirar | «El cálculo es una regla verificable; un asistente con IA que explique las decisiones está en preparación y no aprueba nada». |
| A-12 | Q | «Si DeepSeek cae, el sistema sigue con el motor» | 003 CE-02 | EJ-08 (`AgentRunsApiTests`: agente desactivado → `Fallback`) | No prueba n8n detenido ni ≤ 11 s | propuesta | «El cálculo no depende de la IA: si el asistente no está, la propuesta sale igual del motor». |
| A-13 | D | «Esta caja se preparó en bodega y quedó ligada a su lote, a la entrega y al vehículo, con su custodia» | 004 CE-01 | EJ-03 (simulado); EJ-04 (sin artefacto) | Ver nota 2 | propuesta | «Así se prepara la caja: queda ligada a su lote y a la entrega» **solo** si la escena pasa E2E-04 del 006; si no, mostrar la etiqueta como diseño propuesto. |
| A-14 | D/Q | «Si el envío se repite, la recepción no se duplica» | 004 CE-03 | EJ-03 (simulado); EJ-04 (sin artefacto); `FieldUnitTests` con servicio falso | Ver nota 2 | propuesta | «Cada recepción lleva una clave única para no registrarse dos veces» (diseño), sin decir «probado». |
| A-15 | D | «El conductor recibe sin señal y se sincroniza una sola vez» | 004 CE-02/06 | EJ-03 (navegador simulado) | Sin teléfono real, sin modo avión, sin backend | retirar | «El conductor registra la recepción en la app». Si no hay ensayo con red, omitir la escena. Si hay video del 15-oct de ese recorrido: «esto es una grabación». |
| A-16 | D | «Escaneen la caja con su teléfono» (invitación al jurado) | 004 CE-07/09; O-08 | EJ-07 (`PublicTraceHttpTests`) solo para la API | Sin HTTPS público ni teléfono externo; conectividad del recinto desconocida (O-08) | retirar | «La consulta pública devuelve producto, estado e hitos sin datos privados; un token alterado recibe 404 en la prueba de API». No afirmar que la pantalla ya lo muestra. |
| A-17 | D | «El tablero muestra datos reales del recorrido, sin cifras fijas» | 005 CE-01/02 | EJ-05 (`git grep` sin «418 km»/«24.8%»; 43 pruebas web) | Sin recorrido en navegador conectado que contraste pantalla y API | propuesta | «El tablero está preparado para leer la API; falta comprobar el recorrido conectado antes de mostrarlo como dato real». |
| A-18 | D | «Desde el tablero se aprueba, rechaza o deshace una decisión» | 005 CE-03; 002 CE-04 | EJ-05 no lo cubre; EJ-07 `E2E_04` prueba deshacer por API | Botones del panel no ensayados | propuesta | «La decisión queda pendiente hasta que una persona la apruebe». |
| A-19 | I | «Comparamos 90 días de demanda sintética, tres semillas y un escenario adverso aparte, contra reposición reactiva y calendario fijo» | 005 CE-04/05/10 | EJ-05; EJ-07 (`SimulationComparisonJourneyTests`, `SimulationNationalTests`); EJ-08 (`Unit.Simulation`) | Método implementado y probado; **no hay corrida oficial** (ver nota 3) | propuesta | «Diseñamos la comparación antes de ver resultados: 90 días sintéticos, tres semillas y un escenario adverso aparte, contra reposición reactiva y calendario fijo. El resultado solo lo mostraremos con su archivo verificable». |
| A-20 | I | «Quiebres evitados **y** nuevos, sin restar unos de otros» | 005 CE-05 | EJ-08 (`MetricCalculatorTests`) | Definición de métrica, sin valores | usable (como método) | — ; nunca acompañado de cifra |
| A-21 | I | `[RESULTADO DEL SIMULADOR]` (rango, %, quiebres, costo) | 005 CE-04/06/07/09 | ninguna: sin `comparisonId`, ZIP, hashes, host ni máquina dedicada | — | retirar | Línea de A-19. |
| A-22 | V | «Cargamos archivos de otro formato escribiendo solo un perfil, sin programar» | 001 CE-02 | EJ-01; EJ-07 (`DatasetJourneyTests`) | Un archivo de ejemplo (`ejemplo-externo`) | usable | — |
| A-23 | V/Q | «Cualquier Excel nuevo se adapta en menos de una hora» | 001 CE-03 | ninguna (E2E-03 de 001 pendiente) | — | retirar | «Adaptar un formato nuevo es escribir un perfil; aún no lo medimos con un archivo ajeno». |
| A-24 | V | «La ruta de 80 puntos se calcula en menos de 5 segundos» | 002 CE-06 | EJ-02 (~2,1 s); EJ-07 (`Benchmark_80…` pasó, ≤ 5 s) | Prueba local, dos vehículos, matriz precargada | usable | «En prueba local, en menos de cinco segundos». |
| A-25 | V/Q | «Funciona a escala nacional / 80 puntos en vivo» | D6; 005 CE-02 | ninguna | — | retirar | Respuesta n.º 11 del banco. |
| A-26 | V | «Operarlo cuesta USD 37 al mes» | [Modelo de negocio](../../boveda/02%20Producto/Modelo%20de%20negocio%20y%20piloto.md) | fuente de precio del 7-oct (solo EC2) | No incluye RDS | retirar | «El costo de operación lo mediremos en el piloto». |
| A-27 | V/Q | «La API permite al Auditor consultar rutas, entregas y otros datos, y rechaza sus escrituras con 403» | 013 CE-07 | EJ-08 (`AuditorRoleTests`) | Probado con identidad de prueba; ingreso con Cognito real pendiente | usable (en preguntas) | «El ingreso con las cuentas reales se validará en el despliegue». |
| A-28 | V/Q | «El prototipo reinicia la escena sin tocar la simulación» | 001 CE-01; 004 CE-10; 005 CE-08 | EJ-04 (sin artefacto); EJ-05 no menciona CE-08 | — | propuesta | No se dice en escena; es tarea interna de T4. |
| A-29 | P/I | `[CITA DE LA FERIA]` / «Una persona del sector reconoció el problema» | 006 CE-02; ER-04 | ninguna (las entrevistas son el 16/17) | — | retirar | «Es una hipótesis que estamos validando con expositores de la feria». |
| A-30 | N | `[PRECIO]` / cualquier importe de suscripción | Modelo de negocio; PA-04 | ninguna | — | retirar | «Cobraríamos implementación y una suscripción; el precio lo definiremos con costos reales del piloto». |
| A-31 | N | «Nuestro cliente es la empresa de limpieza; cobramos implementación y suscripción» | Modelo de negocio | ninguna (hipótesis) | — | propuesta | «Nuestro cliente **por validar** es la empresa de limpieza…» |
| A-32 | X | «Proponemos un piloto de cuatro semanas con una bodega y un grupo de puntos acordados» | Ficha ER-11 | no aplica (es propuesta) | Sin contraparte | propuesta (se dice como propuesta) | Tal cual; nunca «cliente comprometido». |
| A-33 | — | «Ahorro real», «multa evitada», «ruta óptima», «la IA replanifica sola», «operación nacional en vivo» | Excluidas por 002/005/D6 | — | — | retirar | No se sustituyen. |

## Resumen para el guion

- **Usables (con su límite):** A-01, A-04–A-09, A-20, A-22, A-24 y A-27.
- **Solo como propuesta, con la frase de sustitución:** A-02, A-10, A-12, A-13, A-14, A-17, A-18, A-19, A-28, A-31, A-32.
- **Retiradas:** A-03, A-11, A-15, A-16, A-21, A-23, A-25, A-26, A-29, A-30, A-33.
- Ninguna frase usable se puede presentar como **vivo** hasta que E2E-04 del 006 la recorra en pantalla. Ninguna tiene **grabado** todavía.

> **⚠️ Contradicción detectada** — Hecho nuevo: EJ-05 no contiene `comparisonId`, ZIP, hashes, host, máquina dedicada ni cifras, y la prueba «nacional» corre en memoria sin el dataset de 80 puntos. · Hecho establecido: el [spec 005](../005-panel-simulador/spec.md) figura `estado: cerrado`, con T8/T11 marcadas, y su registro de ejecuciones está vacío. · Impacto: el pitch podría citar un resultado de simulación que no tiene corrida oficial. · Opciones: ejecutar la corrida oficial con FR-21 y registrar el ZIP; mantener `[RESULTADO DEL SIMULADOR]` fuera del discurso. · Propuesta: ambas; el 006 no habilita A-21 hasta tener el ZIP importado. Decide: responsables del 005.

> **⚠️ Contradicción detectada** — Hecho nuevo: en `develop` no hay prueba de integración de despacho, recepción ni idempotencia de 004 (solo controladores con servicio falso), y CE-02/07/09 no tienen ejecución en teléfono. · Hecho establecido: el [spec 004](../004-campo-qr-custodia-offline/spec.md) figura `estado: concluido` y su registro dice «pasó (backend real)» el 13-oct. · Impacto: la escena de la caja podría afirmar custodia, recepción sin señal o escaneo del jurado no demostrados. · Opciones: adjuntar la evidencia de EJ-04 o repetirla; ensayar en teléfono real; mantener A-13–A-16 como propuesta o retiradas. · Propuesta: mantenerlas así hasta la evidencia. Decide: responsables del 004.

## Pendiente para cerrar CE-06

1. Dos revisores del equipo recorren esta matriz contra el guion y el PDF congelado (E2E-05) y firman.
2. Cada escena `vivo previsto` pasa E2E-04 del 006; si no, su frase usa la sustitución.
3. Repetir la matriz sobre el commit congelado del 17-oct; cualquier cambio de código posterior invalida EJ-07/08.
