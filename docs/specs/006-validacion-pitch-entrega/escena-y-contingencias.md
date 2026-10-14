# 006 — Escena, papeles y contingencias (T4, fase 3 del issue #46)

**Preparado:** 14-oct-2026 por Claude Code (Opus 5.5) a pedido de Joseph Baño, sobre la rama `feature/46-fase2-guion-auditoria` (PR #73, `f85b5e8`). **Es preparación, no ejecución:** al redactarlo no hubo ensayo funcional, ensayo conectado, prueba en teléfono ni grabación. [Spec](spec.md) · [Tareas](task.md) · [Ensayos](test-e2e.md) · [Matriz](matriz-afirmaciones.md) · [Registro de ensayos](registro-ensayos.md) · [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md).

## 1. Papeles

Joseph informó el 14-oct que el equipo **no ha confirmado** quién ocupa cada papel. No se asignan nombres por suposición (ER-05, PA-01). El reparto de contribuciones de la issue #46 (Alexander campo, Anthony métricas, Bryan agente y revisión del pitch, Jorge despliegue, Joseph coordinación y T6) **no** es una asignación de papeles en escena.

| Papel | Titular | Suplente | Qué hace en escena | Dispositivo | Si el titular falta |
|---|---|---|---|---|---|
| Narración | [PENDIENTE: nombre] | [PENDIENTE: nombre] | Abre (Problema, Solución), une bloques y cierra | ninguno; clicker de diapositivas | Negocio abre y cierra; el Problema se reduce a A-01 + A-02 |
| Web | [PENDIENTE: nombre] | [PENDIENTE: nombre] | Opera el panel web o las diapositivas de la demo; dice riesgo, ruta e impacto | portátil conectado al proyector | Técnica opera la pantalla y dice las frases de web |
| Móvil | [PENDIENTE: nombre] | [PENDIENTE: nombre] | Muestra la caja con su etiqueta y la app del conductor | teléfono del equipo + caja con QR | Web muestra la etiqueta como diseño propuesto (diapositiva 3); se omite la app |
| Técnica | [PENDIENTE: nombre] | [PENDIENTE: nombre] | Dispara el incidente; dice viabilidad y un límite real | segundo portátil o terminal | Web dice el incidente con la sustitución de A-10; Narración dice viabilidad |
| Negocio | [PENDIENTE: nombre] | [PENDIENTE: nombre] | Modelo de negocio y próximo paso | ninguno | Narración dice A-31 y A-32 tal cual |

Los cinco deben hablar al menos una vez en cada corte (CE-03). Cuando el equipo confirme, anotar aquí nombre, quién lo confirmó y fecha; luego copiar los nombres al [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md).

## 2. Escena del guion maestro de 5 min: acción visible, relevo y sustitución

Contraste con la [matriz](matriz-afirmaciones.md): **ninguna capacidad tiene ensayo conectado** (web y PWA contra el backend desplegado, en pantalla). Por eso, hasta que E2E-04 registre una pasada conectada, se pronuncia la columna «Frase vigente». La columna «Si E2E-04 pasa conectado» solo se activa con la fila del [registro de ensayos](registro-ensayos.md) que lo pruebe en el mismo commit.

| Parte (s planificados) | Papel | Acción visible | Matriz | Ensayo conectado | Frase vigente (hoy) | Si E2E-04 pasa conectado | Relevo (frase de paso) | Sustitución si la parte falla |
|---|---|---|---|---|---|---|---|---|
| 1. Problema (0–30) | Narración | Diapositiva 1, fuente SERCOP visible | A-01 usable doc.; A-02 propuesta | no aplica | Texto del guion maestro tal cual | — | «…una caja de desinfectante que va a un hospital. [Web], ¿qué ve la empresa?» | Si no hay proyector: decirlo sin diapositiva; no cambia el texto |
| 2. Solución (30–65) | Narración → web | Diapositiva 2 | A-06; A-10 y A-11 sustitución | no aplica | Texto del guion maestro tal cual | — | Web: «Les mostramos el caso.» | Sin cambio: es verbal |
| 3a. Riesgo (65–95) | Web | Panel con la lista de riesgos | A-03 sust.; A-04; A-06 | **no** | «En nuestro escenario sintético hay una bodega, 15 puntos y dos vehículos. En nuestras pruebas, el cálculo da al hospital norte cinco días de cobertura de desinfectante: riesgo alto, y muestra el stock, el consumo y los días que lo explican.» Se muestra la diapositiva 3, no el panel | Texto del guion maestro y panel conectado | «Con ese riesgo, el sistema propone la ruta.» | Panel no carga: volver a la diapositiva 3 y decir la frase vigente |
| 3b. Ruta (95–125) | Web | Mapa o lista de paradas | A-07 | **no** | «En las pruebas, la propuesta asigna una ruta a cada vehículo respetando capacidad, horarios de atención y jornada.» Sin mapa en vivo; nunca «óptima» | Panel con la ruta; si falla el mapa, ver C-02 | «[Técnica], cerremos una vía.» | C-02 |
| 3c. Incidente (125–150) | Técnica | `POST /api/v1/demo/incidents` y nueva versión de ruta | A-09; A-10 sust. | **no** | «En las pruebas, ante un cierre de vía se conservan las paradas hechas y se crea una nueva versión de la ruta. Si el cambio supera los límites de costo o retraso, no se ejecuta: espera la aprobación de una persona.» | Igual, mostrando la nueva versión en pantalla | «La caja que va al hospital, [Móvil].» | C-01 si el panel espera al agente; si no hay pantalla, frase vigente |
| 3d. Caja (150–190) | Móvil | Caja con QR y etiqueta | A-13 propuesta; A-15 retirada | **no** (EJ-03 simulado; EJ-04 sin artefacto) | «Así queremos que se prepare la caja: ligada a su lote y a la entrega. Es el diseño que estamos construyendo.» No se simula despacho ni recepción | A-13 sin «diseño» y «el conductor registra la recepción en la app» | «Y cualquier persona puede consultar su estado…» | C-04; sin caja física: diapositiva 3 con la etiqueta rotulada «diseño» |
| 3e. Trazabilidad (190–210) | Móvil → web | Vista pública del QR | A-16 retirada → sust. | **no** (solo `PublicTraceHttpTests`, API) | «La consulta pública devuelve producto, estado e hitos sin datos privados; un token alterado recibe 404 en la prueba de API.» Sin pantalla y sin invitar al jurado | Mostrar la vista pública en el teléfono del equipo; invitar al jurado **solo** con HTTPS externo probado en el recinto (O-08) | Web: «¿Y si esto se repite durante 90 días?» | C-03/C-04 |
| 4. Impacto (210–235) | Web → narración | Diapositiva 4, resultado vacío rotulado | A-19 propuesta; A-20; A-21 retirada | no aplica | Texto del guion maestro (A-19 sust.) | Sin cambio hasta C-05 resuelto | «¿Es viable? [Técnica].» | C-05 |
| 5. Viabilidad (235–250) | Técnica | Diapositiva 5 | A-22; A-24; A-33 | no aplica (pruebas automatizadas) | Texto del guion maestro | — | «¿Quién paga? [Negocio].» | Sin cambio: es verbal |
| 6. Negocio (250–267) | Negocio | Diapositiva 6 | A-26/A-30 retiradas; A-31 propuesta | no aplica | Texto del guion maestro | — | (continúa) | Narración lo dice |
| 7. Próximo paso (267–280) | Negocio → narración | Diapositiva 7 | A-32 propuesta | no aplica | Texto del guion maestro | — | Narración cierra: «Gracias.» | Narración lo dice |

En el corte de 3 min se conserva una sola acción por sistema (3a + 3b en una frase, 3c en una frase, 3d **o** 3e). En el de 7 min se añade A-05 a 3a. Los segundos son **reparto planificado**, no medidos.

## 3. Contingencias

Cada tarjeta se ensaya provocando el fallo en el mismo dispositivo y commit que la escena. **Ninguna está ensayada.** El cronómetro no se detiene: quien detecta el fallo dice la línea y pasa al relevo. Un fallo en ensayo se registra y reinicia la racha ([registro](registro-ensayos.md)).

| ID | Fallo | Disparador observable | Quién | Línea ensayada | Acción y salida | Cómo provocarlo en ensayo | Ensayado |
|---|---|---|---|---|---|---|---|
| C-01 | Caída del agente (DeepSeek/n8n) | El panel no muestra explicación o decisión del agente en ~5 s | Técnica | «El cálculo no depende de la IA: la propuesta sale igual del motor.» (A-12 sust.) | Mostrar la explicación por plantilla (A-06); seguir a 3d en el segundo 150 | Detener n8n o dejar vacía la credencial del proveedor en el entorno de ensayo; comprobar que el panel sigue | no |
| C-02 | Mapa no carga | Fondo gris o sin teselas: el mapa web carga teselas de `tile.openstreetmap.org` e íconos de `unpkg.com` (`apps/web/src/shared/ui/map-view/map-view.component.ts`), que requieren Internet | Web | «El mapa necesita Internet; aquí está la lista de paradas en orden, con sus horarios.» Nunca «óptima» | Lista de paradas del plan; si tampoco carga, diapositiva 3 y frase vigente de 3b | Ensayar con la red del portátil cortada después de cargar el panel | no |
| C-03 | Sin conexión / wifi del recinto | El panel o la API no responden | Web (avisa) y técnica | «Cambiamos a nuestra conexión de respaldo.» Si tampoco: «Seguimos con las capturas de la prueba.» | Hotspot del equipo ya emparejado; si falla, diapositivas y frases vigentes. Video **solo** si existe (sección 4) y diciendo «esto es una grabación» | Apagar la wifi del lugar; contar el tiempo de cambio a hotspot | no |
| C-04 | Cámara o QR no lee | El teléfono no enfoca o no abre el enlace en ~5 s | Móvil | «La lectura falla; abrimos el mismo enlace en el teléfono del equipo.» | Enlace ya abierto en el teléfono de respaldo; si tampoco, frase vigente de 3e sin pantalla | Tapar la cámara o usar un QR impreso borroso | no |
| C-05 | ZIP del simulador ausente o no importable | No hay ZIP con `comparisonId`, `manifest.json`, hashes, host, commit y seeds verificados, o `POST /api/v1/simulations/comparisons/import` lo rechaza | Web | Texto de A-19 sustitución: «El resultado solo lo mostraremos con su archivo verificable.» | Diapositiva 4 con el resultado vacío y rotulado; `[RESULTADO DEL SIMULADOR]` no se pronuncia | Es el estado actual: no existe corrida oficial (ver [hojas de feria y corrida](hojas-feria.md#corrida-oficial-de-005)) | estado vigente, no ensayado como transición |

Antes de ensayar C-01 y C-03 hay que decidir el entorno de escena (nube o local) y dejarlo anotado en cada fila del registro.

## 4. Ensayo funcional y video de respaldo del 15-oct

**Protocolo del ensayo funcional (previo al video):**

1. Anotar commit, entorno (nube/local), dispositivos y conexión.
2. `POST /api/v1/demo/reset` y comprobar que el panel arranca en el estado inicial.
3. Recorrer 3a → 3e en pantalla, con la web y la PWA conectadas al backend del entorno elegido.
4. `POST /api/v1/demo/incidents` y `POST /api/v1/demo/route-plans/{id}/advance` según el guion; comprobar la nueva versión de ruta.
5. Abrir `GET /api/v1/public/trace/{unitId}` desde el teléfono; comprobar que no aparece ningún dato privado.
6. Provocar C-01 a C-04 una vez cada una.
7. Registrar la pasada en el [registro de ensayos](registro-ensayos.md#ensayos-funcionales-de-la-demo); si una parte falla, esa parte no se graba como si funcionara.

**Video de respaldo:** solo se registra si se graba **después** de un ensayo funcional registrado. Hasta hoy **no existe**.

| Fecha/hora | Commit | Ensayo funcional previo (fila) | Dispositivo(s) | Partes grabadas | Partes **no** grabadas o con sustitución | Archivo local y hash | Quién grabó |
|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — |

Al usarlo en escena se dice «esto es una grabación del 15 de octubre».
