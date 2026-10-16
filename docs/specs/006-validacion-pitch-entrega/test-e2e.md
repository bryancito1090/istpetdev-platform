# 006 — Validación, pitch y paquete de entrega: ensayos de punta a punta

Estos E2E son ensayos documentales y cronometrados ante personas, no pruebas automatizadas de código. Una casilla de criterio de 001–005 no sustituye una ejecución enlazada. No hay ensayos registrados al redactar este archivo.

## Preparación

1. Confirmar O-04/O-06 con respuesta y fuente; si siguen pendientes, ensayar las tres versiones con metas de 160/280/400 s y no declarar tiempo oficial cumplido.
2. Reunir material editable/PDF, teléfono web, teléfono móvil, caja QR, hotspot, video local, tag `pre-evento`, tarjetas de afirmación y manifiesto. Todos llevan commit/versión.
3. Crear registro por ensayo con: fecha/hora local, responsables, corte, versión de slides/guion, commit, conexión, duración, fallos, cambio de escena, evidencia y contador de racha. Copiar [Plantilla evidencia](../../boveda/09%20Plantillas/Plantilla%20evidencia.md).
4. Antes de citar números del simulador: exigir ZIP importado con hashes/commit/host/seeds; antes de escena QR/offline: exigir evidencias de 004 CE-02/07. Si faltan, usar guion de contingencia sin la afirmación.

## Escenarios

| ID | Recorrido | Cubre | Obligatorio |
|---|---|---|---|
| E2E-01 | Revisión del kit y consentimiento | CE-01 | sí |
| E2E-02 | Tres conversaciones y retorno con prototipo | CE-02 | sí |
| E2E-03 | Cortes 3/5/7 con cinco relevos | CE-03 | sí |
| E2E-04 | Escena completa y contingencias | CE-03/04 | sí |
| E2E-05 | Auditoría de cada cifra y cita | CE-06 | sí |
| E2E-06 | Racha de diez y dos sin wifi | CE-04/05 | sí |
| E2E-07 | Congelamiento y paquete | CE-07/08 | sí |
| E2E-08 | Cadena E2E: pedido → stock bodega → vehículo → ruta con hora límite → costo → n8n | Issue #207 | sí |

### E2E-01 — Kit utilizable

**Pasos:** otra persona del equipo lee en unos 15 min guion/hoja, esqueleto 3/5/7, negocio, banco de veinte y nota SERCOP. Marca cualquier pregunta que presuponga respuesta o costo.

**Resultado:** cinco componentes presentes; precio, cita y resultado quedan como huecos; revisión, corrección y nombre del revisor anotados. **Evidencia:** revisión fechada en el registro previo y versión del kit.

**Estado al 14-oct-2026:** Joseph informó que revisó y aprobó todos los componentes del kit del PR #71 durante 15 minutos. No comunicó las horas de inicio y fin. E2E-01 y CE-01 se registran como cumplidos con esa declaración del revisor ([T1](task.md)); no se atribuye tiempo de construcción del kit al equipo.

### E2E-02 — Feria sin fabricar validación

**Pasos:** dos integrantes piden permiso en las pausas del 16; preguntan cinco minutos, sin grabar por defecto; el 17 presentan el prototipo a al menos una persona y piden interpretación antes de explicar. Separan permiso de notas y permiso de cita; evitan las organizaciones cauteladas.

**Resultado:** ≥3 hojas antes de 12:30 del 17, ≥1 reacción al prototipo, al menos una objeción/ajuste o pendiente trazado. Rechazos cuentan como intento, no conversación. Si no se alcanza, CE-02 queda pendiente y el guion no dice «validado por el sector». **Evidencia:** notas anonimizadas con fecha, contexto, consentimiento y vínculo a cambio.

**Estado al 14-oct-2026 (fase 3):** sin ejecutar; las conversaciones son el 16 y 17-oct. Hojas, registro de intentos y lista de la corrida oficial de 005 preparados en [hojas de feria](hojas-feria.md). CE-02 pendiente.

### E2E-03 — Siete bloques y cinco voces

**Pasos:** hacer una pasada por cada corte y marcar inicio/fin de cada bloque. Observar cinco intervenciones sin solaparse. La persona de negocio cierra con piloto de cuatro semanas; técnica nombra un límite real.

**Resultado:** orden Anexo 5.7 exacto; 160/280/400 s o tiempo oficial −20 s tras O-04; nombres solo si el equipo los asignó. **Evidencia:** hoja de cronometraje y video interno permitido.

**Estado al 14-oct-2026 (fase 2):** sin ejecutar. Los cortes 3/5/7 y el reparto por bloque y papel están redactados en la «Versión de trabajo T2» del [Guion del pitch](../../boveda/07%20Demo/Guion%20del%20pitch.md), pero nadie los ha leído en voz alta ni cronometrado. Los segundos del guion son reparto planificado.

**Estado al 14-oct-2026 (fase 3):** sin ejecutar. Nombres de los cinco papeles sin confirmar por el equipo; relevo y sustitución por parte en [escena y contingencias](escena-y-contingencias.md). CE-03 pendiente.

### E2E-04 — Demo y plan B en escena

**Pasos:** ejecutar reset, riesgo, ruta, incidente, despacho/recepción y QR; luego repetir simulando por separado caída de DeepSeek, ausencia de mapa, falta de wifi, fallo de cámara/QR y ZIP de simulación no importable. La persona responsable dice la línea de contingencia y pasa al siguiente hito sin detener cronómetro.

**Resultado:** ninguna escena afirma capacidad sin CE ejecutado; la recepción grabada se llama grabada, un QR que solo funciona en hotspot se opera con teléfono del equipo, y un resultado sin ZIP se retira. **Evidencia:** matriz de contingencia con video/cronómetro y enlace al CE de 001–005 correspondiente.

**Estado al 14-oct-2026 (fase 3):** sin ejecutar. Protocolo del ensayo funcional, frases vigentes sin ensayo conectado y tarjetas C-01–C-05 preparadas en [escena y contingencias](escena-y-contingencias.md); ninguna contingencia provocada. El video de respaldo no existe. La tabla de pasadas funcionales está vacía en el [registro de ensayos](registro-ensayos.md#ensayos-funcionales-de-la-demo).

### E2E-05 — Trazabilidad de promesas

**Pasos:** dos revisores subrayan cifras, citas y verbos de capacidad en PDF y guion; para cada uno buscan tarjeta de afirmación, ejecución y fuera de alcance del spec productor. Intentan deliberadamente una cita sin consentimiento, un «ahorro» sin ZIP, «IA replanifica sola» y «QR del jurado» sin HTTPS externo.

**Resultado:** cuatro afirmaciones rechazadas o corregidas cuando falta soporte; cero números de resultado huérfanos en versión congelada. Mostrar min/max de seeds solo con denominadores y etiqueta de simulación. **Evidencia:** matriz firmada por revisores y hashes de materiales auditados.

**Estado al 14-oct-2026 (fase 2):** sin ejecutar como recorrido completo. Existe la [matriz de afirmaciones](matriz-afirmaciones.md), preparada por IA sobre `7aed1eb`, con 105 pruebas automatizadas de 001–005 y 013 ejecutadas para sostenerla ([evidencia](evidencia/2026-10-14-suites-fase2.md)). Joseph aprobó el borrador `8693f73`; faltan el segundo revisor humano, la revisión de A-16/A-17/A-27 ajustadas después de su aprobación, el PDF y la versión congelada. Las cuatro afirmaciones del intento deliberado (cita sin consentimiento, «ahorro» sin ZIP, «IA replanifica sola», «QR del jurado») ya están como `retirar` en la matriz (A-29, A-21, A-11/A-33, A-16), pero eso no sustituye el recorrido.

**Actualización 14-oct-2026 (fase 3):** segundo revisor pedido en la issue #46; la revisión no ha ocurrido. Hoja para registrar nombre, versión y correcciones en la [matriz](matriz-afirmaciones.md#hoja-de-segunda-revisión-e2e-05). CE-06 pendiente.

### E2E-06 — Racha real

**Pasos:** diez pasadas consecutivas del corte oficial completo, incluidas preguntas breves y relevos; medir de primera palabra a cierre. Los ensayos 9 y 10 se hacen sin wifi del recinto y con el mismo plan de conexión elegido para escena. Registrar incluso fallos; un fallo vuelve contador a cero. Cronometrar aparte ronda de veinte respuestas, cada una ≤30 s.

**Resultado:** contador llega a diez, dos últimas sin wifi, tiempo ≤ oficial −20 s en cada pasada, veinte respuestas dentro del límite. Si O-04 aún no llega, registrar pasadas como preparación, no CE-04 cerrado. **Evidencia:** diez filas consecutivas con enlaces, cronómetro, conexión, cinco roles, versión y fallos vacíos.

**Estado al 14-oct-2026 (fase 3):** sin ejecutar. Registro de pasadas y ronda de veinte respuestas preparados en [registro de ensayos](registro-ensayos.md); racha 0/10. CE-04 y CE-05 pendientes.

### E2E-07 — Paquete que se entrega

**Pasos:** verificar tag previo y tabla de cambios del evento; abrir PDF, editable, ficha, ZIP/manifiesto y video sin red; contar siete entregables oficiales, etiquetar cada uno y registrar hash. Congelar a 15:45. Entregar de 16:00–16:15 por canal confirmado; conservar acuse o anotación de recepción.

**Resultado:** una sola versión entregable con siete componentes y vínculos de evidencia, copia local legible y hora de congelamiento. Si el formato no fue confirmado, CE-07 permanece pendiente hasta recibir instrucción oficial. **Evidencia:** manifiesto, hashes, registro de entrega y tag.

**Estado al 14-oct-2026 16:00 (fase final):** sin ejecutar. [Manifiesto](manifiesto-paquete.md) abierto; editable y PDF provisionales con SHA-256; no hay tag `pre-evento`, video, congelamiento ni entrega. O-06 está pendiente, así que no hay canal para entregar.

### E2E-08 — Flujo de punta a punta: propuesta única por prioridad, bodega, vehículo, hora límite y explicación verificable (Issue #207)

Lista de verificación técnica y operativa para la integración de punta a punta: **pedido → stock por bodega → vehículo → ruta priorizada con hora límite → costo explicado → n8n explica sin inventar cifras**. Se ejecuta contra `develop` una vez que los PRs de #193 y #194 estén fusionados.

#### 1. Prerrequisitos de integración en `develop`
- [ ] PR de #193 fusionado en `develop` (parámetro opcional `CriticalDeadlineLocal` en motor y controlador).
- [ ] PR de #194 fusionado en `develop` (endpoints de consulta de stock por bodega y disponibilidad de vehículos).
- [ ] Entorno local o de pruebas levantado con backend .NET y base de datos con fixtures `demo-region`.

#### 2. Pasos de la lista de verificación (Checklist)

##### Fase A: Pedido y Prioridad
- [ ] **A.1 Creación/Recepción del pedido:** El pedido (`DeliveryOrder`) contiene punto de destino, SKU, cantidad requerida y nivel de prioridad (`Critical`, `High`, `Normal`).
- [ ] **A.2 Ventana de servicio:** Si el pedido es crítico (ej. hospital o quirófano), cuenta con criticidad alta o ventana horaria matutina exigible.

##### Fase B: Stock por Bodega (FEFO y Seguridad)
- [ ] **B.1 Consulta de stock:** El endpoint `/api/v1/warehouses/stock` (o similar de #194) retorna el inventario real desagregado por bodega física y lote.
- [ ] **B.2 Asignación y frescura:** La asignación respeta política FEFO (vencimiento próximo primero) y reserva el stock de seguridad configurado.
- [ ] **B.3 Manejo de faltante:** Si una bodega no cuenta con inventario suficiente, se propone bodega alternativa o se marca faltante explícito; el sistema **nunca** inventa stock disponible.

##### Fase C: Selección de Vehículo Adecuado
- [ ] **C.1 Filtrado de flota:** Se consultan los vehículos activos en la bodega origen (`GET /api/v1/vehicles`).
- [ ] **C.2 Validación de capacidades:** Se verifica que el peso total (kg) y volumen (m³) de la carga asignada no superen la capacidad máxima del vehículo (`MaxWeightKg`, `MaxVolumeM3`).
- [ ] **C.3 Costo base y jornada:** Se asocian las tarifas de costo por km y costo por hora configuradas para el vehículo, así como el límite máximo de jornada laboral (ej. 8 horas).

##### Fase D: Ruta Priorizada con Hora Límite (#193)
- [ ] **D.1 Invocación con parámetro:** Se solicita la planificación (`POST /api/v1/route-plans`) enviando el campo opcional `CriticalDeadlineLocal` (ej. `"11:00"`).
- [ ] **D.2 Cumplimiento en críticos:** Las paradas con riesgo Crítico o criticidad de punto alta quedan programadas con hora estimada de llegada (`EstimatedArrivalLocal`) anterior o igual a la hora límite especificada.
- [ ] **D.3 Conservación en no críticos:** Las paradas normales o de riesgo bajo/medio conservan sus ventanas horarias habituales sin compresión innecesaria.
- [ ] **D.4 Imposibilidad física:** Si un punto crítico no puede alcanzarse antes de la hora límite sin violar jornada o capacidad, queda clasificado como **no atendido** con motivo explícito `time_window`, sin que el algoritmo corrompa las restricciones.
- [ ] **D.5 Comportamiento base:** Si se omite `CriticalDeadlineLocal`, el motor produce exactamente el plan estándar ordenado por costo y factibilidad.

##### Fase E: Costo Explicado y Hechos Estructurados (`facts`)
- [ ] **E.1 Métricas físicas y monetarias:** El plan generado expone kilómetros totales, tiempo en tránsito, tiempo de servicio y costo total en USD.
- [ ] **E.2 Costo de priorizar:** Se calculan y contrastan las métricas del plan con hora límite frente al plan sin hora límite (diferencial de kilómetros, sobrecosto y paradas atendidas).
- [ ] **E.3 Hechos deterministas:** La respuesta del motor incluye el bloque `facts` estructurado con los valores calculados que alimentan las explicaciones.

##### Fase F: n8n y Asistente con IA sin Cifras Inventadas (#195/#197)
- [ ] **F.1 Consulta de herramientas:** El workflow de n8n / agente IA consume los endpoints autorizados de la API pasando la cabecera `X-Agent-Run-Id`.
- [ ] **F.2 Explicación grounded:** El texto generado por el LLM describe la decisión utilizando **únicamente** los datos y números presentes en `facts` (cobertura, días, horas, kilómetros y costos).
- [ ] **F.3 Validación anti-alucinación:** Cualquier cifra, porcentaje de ahorro o costo no respaldado en `facts` provoca el descarte del texto y la activación inmediata de la plantilla determinista.
- [ ] **F.4 Límite de latencia y fallback:** Si n8n o la IA tardan más de 10 segundos o responden con error HTTP, la API devuelve en fallback la propuesta determinista con explicación por plantilla.

#### 3. Matriz de Asignación ante Fallos o Incidencias

Si durante la ejecución de esta lista algún paso no se cumple, se abre un issue nuevo con etiqueta de semáforo (🔴 crítico, 🟡 medio, 🟢 bajo) y se asigna inmediatamente al responsable técnico correspondiente:

| Componente afectado | Archivos / Rutas clave | Responsable GitHub |
|---|---|---|
| Motor de rutas, hora límite y controladores | `Planning/RoutePlanner.cs`, `Planning/RouteFeasibility.cs`, `RoutePlansController.cs` | `@AnthonyPilatasig` |
| Endpoints de stock, consultas de flota y n8n | `Controllers/WarehousesController.cs`, `automation/n8n/*`, servicios de stock | `@bryancito1090` |
| Interfaz web (Rutas, paneles de stock) y PWA móvil | `src/frontend/src/app/pages/rutas/*`, PWA de campo | `@AlexanderMartinez0410` |
| Infraestructura, Docker, base de datos y despliegue | `infra/*`, migraciones, configuración de entorno | `@JorgeDoicela` |
| Coordinación general, matriz de afirmaciones y pitch | `docs/specs/006-*`, `docs/boveda/07 Demo/*` | `@JosephBano` |

#### 4. Registro de Ejecución
- **Fecha y hora:** [Pendiente a la fusión de PRs #193 y #194]
- **Commit evaluado:** [Registrar commit de develop]
- **Resultado:** [Pasó / Falló con detalle de pasos A.1 a F.4]
- **Issue de seguimiento:** #207

## Estado de los criterios al 14-oct-2026 16:00

| CE | Estado | Motivo | Alternativa honesta si no se cumple |
|---|---|---|---|
| CE-01 | registrado | E2E-01: revisión de Joseph del 14-oct | — |
| CE-02 | pendiente | Entrevistas el 16 y retorno el 17; ninguna conversación | «Es una hipótesis que estamos validando con expositores» (A-29) |
| CE-03 | pendiente | Sin pasadas cronometradas; papeles sin confirmar; O-04 sin respuesta | Ensayar los tres cortes como preparación |
| CE-04 | pendiente | Racha 0/10; O-04 sin respuesta | Pasadas clasificadas como preparación, sin cerrar contra un tiempo inventado |
| CE-05 | pendiente | Ninguna ronda de respuestas cronometrada | Responder con las sustituciones de la matriz |
| CE-06 | pendiente | Segunda revisión pedida, no realizada; sin versión congelada | Mantener retiradas y sustituciones vigentes |
| CE-07 | pendiente | Sin congelamiento, entrega ni canal (O-06) | Entregar solo por un canal oficial confirmado; si no llega, registrar la consulta y no improvisar |
| CE-08 | pendiente | Sin tag `pre-evento` ni video del 15-oct | Sin video: frases vigentes y diapositivas; nunca presentar una grabación como vivo |

## Registro de ejecuciones

| Fecha/hora | Commit/material | Escenario | Duración/conexión | Resultado | Evidencia | Quién |
|---|---|---|---|---|---|---|
| 2026-10-14 (hora no registrada) | PR #71, `228ab72` (T1 en `92a9e63`) | E2E-01 | 15 min, revisión documental | pasó según declaración del revisor; cinco componentes aprobados, sin correcciones adicionales informadas | [hoja de T1](task.md#hoja-de-revisión-humana-de-t1-unos-15-min), [registro previo](../../boveda/06%20Equipo/Registro%20de%20trabajo%20previo%20y%20del%20evento.md) | Joseph Baño (Andrés) |
