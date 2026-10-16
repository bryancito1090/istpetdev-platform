---
id: "004"
titulo: Campo — QR, custodia, despacho, recepción y PWA offline
estado: concluido
creado: 2026-10-12
actualizado: 2026-10-13
aprobado_por: Bryan
depende_de: ["001", "002", "013 (parcial: H1–H3, solo FR-17)"]   # la API de campo no espera a 013; solo la PWA conectada a la API real
boveda:
  - 02 Producto/Requisitos y aceptacion.md
  - 03 Arquitectura/Movil offline y sincronizacion.md
  - 03 Arquitectura/Contratos API y eventos.md
  - 03 Arquitectura/Seguridad y evidencias.md
  - 03 Arquitectura/Decisiones de arquitectura.md
  - 04 Datos y algoritmos/Dataset y escenarios.md
  - 06 Equipo/Definiciones de producto y plan hasta el pitch.md
  - 07 Demo/Plus para ganar.md
---

# 004 — Campo: QR, custodia, despacho/recepción y PWA offline

## Contexto

El Reto 1 pide trazabilidad del abastecimiento. El momento físico de la demo une el cálculo y la ruta de 002 con una caja real: bodega prepara una unidad QR, registra el despacho, el conductor recibe sin señal y el jurado consulta el resumen público desde su teléfono. La recepción offline con reinicio y reintento sostiene S-05 y S-06 de [Dataset y escenarios](../../boveda/04%20Datos%20y%20algoritmos/Dataset%20y%20escenarios.md).

El código aún no realiza este recorrido. `apps/mobile` tiene una sola pantalla de estado y afirma que el flujo offline está preparado, aunque no persiste recepciones. La página pública de `apps/web` consulta una `Delivery` por código e incluye un registro de demostración fijo; no lee la custodia de una `HandlingUnit` ni exige token. El esquema implementable de 001 aporta las entidades principales y roles fijos; 002 publica `Delivery` y `DeliveryLine` con lotes FEFO, y 003 guarda el aviso al conductor en `AgentDecision.DriverMessage`.

Este spec reemplaza el marcado provisional `POST /route-plans/{id}/stops/{sequence}/complete` de 002: una parada queda completada por una recepción real aceptada, nunca por un clic que fija horas planificadas como reales.

## Objetivo y aporte a la nota

**Objetivo:** demostrar una transferencia de custodia y una recepción verificable aun después de perder señal y reiniciar la PWA, con inventario conciliado y un QR público de alcance limitado.

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| Prototipo/demo | 20% | Caja física, escaneo de despacho, modo avión, reinicio, sincronización y consulta del jurado sobre el mismo registro |
| Viabilidad técnica | 15% | Persistencia de operación y archivos, idempotencia, conflictos visibles y evidencia privada reintentable |
| Innovación | 15% | Custodia conectada a la ruta revisada por el agente y continuidad sin señal, con resultado visible |
| Presentación | 10% | El jurado comprueba el hito final con su propio teléfono y entiende tres estados de custodia |

## Historias de usuario

### H1 — Preparar y despachar una caja QR (P1)

**Como** personal de bodega, **quiero** asociar una caja física a las líneas FEFO de una entrega publicada e imprimir su QR, **para** transferir al conductor cantidades y lotes comprobables.

**Por qué esta prioridad:** abre el recorrido físico y fija la custodia inicial.

**Escenarios de aceptación:**

1. **Dado** una `Delivery` publicada con líneas reservadas, **cuando** bodega prepara una `HandlingUnit` con cantidades por lote que no exceden las líneas, **entonces** obtiene etiqueta imprimible y la unidad queda asociada a esa entrega sin mover stock todavía.
2. **Dado** la caja preparada, **cuando** bodega escanea el QR y confirma despacho, **entonces** un movimiento por línea traslada stock de bodega a vehículo y un `CustodyEvent` registra actor, unidad, entrega, cantidades y hora del servidor.
3. **Dado** un QR de otra entrega o una cantidad superior a la reservada, **cuando** intenta despachar, **entonces** la API rechaza toda la operación sin movimiento parcial.

### H2 — Recibir sin señal y sin perder la captura (P1)

**Como** conductor asignado, **quiero** registrar cantidades, foto y firma voluntaria del receptor en la PWA instalada durante el modo avión, **para** completar la entrega cuando regrese la señal.

**Escenarios de aceptación:**

1. **Dado** que descargué mis entregas y la revisión vigente, **cuando** activo modo avión, escaneo una caja conocida, registro cantidades, foto y firma, y reinicio la PWA, **entonces** veo la captura y ambos archivos como «Guardado en el teléfono».
2. **Dado** que vuelve la red, **cuando** pulso sincronizar, **entonces** la API acepta una recepción, transfiere solo lo aceptado al punto y la PWA muestra «Aceptado por el servidor»; foto y firma pasan a «Evidencia pendiente» hasta confirmarse en S3.
3. **Dado** que se pierde la respuesta HTTP y repito la misma operación, **cuando** reenvío el mismo `operationId` y contenido, **entonces** obtengo el resultado previo y no aparece un segundo movimiento.

### H3 — Resolver parciales, rechazos y revisiones de ruta (P1)

**Como** conductor, **quiero** que una recepción parcial o una ruta revisada se concilie sin ocultar conflictos, **para** saber qué sigue bajo mi custodia y qué debe revisar el equipo.

**Escenarios de aceptación:**

1. **Dado** 12 L despachados, **cuando** se aceptan 9 L y se rechazan 3 L con motivo, **entonces** el punto gana 9 L y los 3 L siguen en el vehículo bajo custodia del conductor, pendientes de devolución física a bodega.
2. **Dado** que la ruta cambia mientras estoy offline, **cuando** sincronizo, **entonces** la recepción se acepta si la entrega, mi asignación y las cantidades despachadas siguen vigentes, aunque el número de revisión haya cambiado.
3. **Dado** dos dispositivos que intentan recibir la misma cantidad con claves distintas, **cuando** llega el segundo, **entonces** el servidor rechaza el exceso con un conflicto visible y conserva la captura local para revisión.

### H4 — Mostrar avisos de ruta al conductor (P1)

**Como** conductor, **quiero** ver en la PWA el aviso breve del agente cuando detecta una revisión nueva de mi ruta, **para** seguir el plan vigente sin usar chat ni otra aplicación.

**Escenarios de aceptación:**

1. **Dado** una nueva revisión publicada con `DriverMessage`, **cuando** la PWA consulta mis rutas con red, **entonces** muestra el mensaje, el número de revisión y las paradas que cambiaron, sin repetir el aviso después de marcarlo leído.
2. **Dado** que sigo sin señal, **cuando** abro la PWA, **entonces** veo la última revisión descargada, su hora de descarga y una indicación de que puede haber cambios pendientes.

### H5 — Verificar la caja desde un teléfono ajeno (P1)

**Como** jurado, **quiero** escanear el QR de la caja y ver un resumen entendible, **para** comprobar los hitos de custodia sin acceder a datos privados.

**Escenarios de aceptación:**

1. **Dado** un token público vigente en el fragmento de la URL (`#t=…`), **cuando** abro la URL del QR, **entonces** la página envía el token por cabecera y veo código de caja, producto y unidad, estado e hitos generales «preparada», «despachada» y «recibida» con horas del servidor; no veo cantidades, firmas, fotos, teléfonos, nombres personales ni token en el cuerpo de respuesta.
2. **Dado** un token revocado, vencido o alterado, **cuando** consulto la URL, **entonces** la respuesta no revela si la unidad existe ni su inventario.

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | Bodega DEBE crear `HandlingUnit` y sus líneas de lote desde una `Delivery` publicada antes del despacho; una entrega admite varias unidades, cada unidad pertenece a una sola entrega activa y sus cantidades no exceden las líneas reservadas. | RF-05, Modelo de datos |
| FR-02 | La API DEBE emitir una URL QR de la forma `https://<web>/public-trace/{unitId}#t=<token>`, con token público aleatorio en el **fragmento** (el navegador no lo envía al servidor ni queda en logs de acceso), almacenado solo como hash, con vencimiento y revocación; la etiqueta se imprime desde el navegador al crearla. Para reimprimir después se emite otro token y se revoca el anterior. El QR no autoriza escrituras. | RF-05, RF-13, Seguridad y evidencias |
| FR-03 | Despachar DEBE exigir rol Bodega, unidad preparada y entrega publicada; en una transacción valida lote/cantidad y la reserva activa de 002, la marca consumida, descuenta el agregado reservado, registra movimientos bodega→vehículo, `CustodyEvent` y estado de entrega. Reintentar la misma operación no duplica efectos. | RF-05, RNF-02 |
| FR-04 | El conductor DEBE descargar solo rutas, revisiones, unidades y entregas asignadas a `Delivery.AssignedAccountId`, con líneas, unidades y hora de actualización suficientes para operar offline. | RF-06, RNF-01 |
| FR-05 | La PWA DEBE persistir en IndexedDB operación, líneas, foto y PNG de `signature_pad` en una transacción local antes de mostrar «Guardado en el teléfono»; reiniciar la app conserva todo. | RF-06, RF-09, RNF-04, ADR-17 |
| FR-06 | La recepción DEBE enviar `operationId` UUID v7 también en `Idempotency-Key`, `expectedDeliveryVersion`, `occurredAtDevice`, líneas por `deliveryLineId`/lote y cantidades aceptadas/rechazadas en unidad base; el actor se toma del token. | RF-06, RNF-02, Contratos API y eventos |
| FR-07 | La API DEBE guardar en un mismo commit `Receipt`, `ReceiptLine`, movimientos vehículo→punto de lo aceptado, estado de entrega/parada y respuesta idempotente. La misma clave/huella devuelve el resultado previo; misma clave/contenido distinto da 409. **Revisión 15-oct (#162):** el recibo es final y único por entrega y cubre exactamente todas las líneas despachadas; un recibo previo, `Received` o `Partial` (terminales) responde 409 `ALREADY_RECEIVED` antes de validar la versión, y la transferencia exige saldo suficiente en el vehículo. | RNF-02, RF-06, RNF-03 |
| FR-08 | Lo rechazado o pendiente DEBE permanecer en el vehículo y bajo custodia del conductor con motivo de rechazo; la devolución a bodega requiere un evento y movimiento posterior explícitos. | S-07, RF-05 |
| FR-09 | Una revisión de ruta distinta NO DEBE rechazar por sí sola una recepción offline; la API valida entrega, asignación y cantidades vigentes. Si falla, devuelve 409 con código estable y datos permitidos para revisión, sin borrar la captura local. | S-11, Contratos API y eventos |
| FR-10 | La PWA DEBE sincronizar recepción primero y evidencias después; solicitar autorización de carga por archivo, subir a S3 privado y confirmar checksum/tamaño/tipo. Fallas de archivos no revierten la recepción y se reintentan independientemente. | RF-09, ADR-17 |
| FR-11 | La PWA DEBE mostrar los estados «Guardado en el teléfono», «Enviando», «Aceptado por el servidor», «Conflicto» y «Evidencia pendiente», y permitir reintento manual al recuperar conexión o sesión. | ADR-17, RNF-04 |
| FR-12 | La PWA DEBE detectar por consulta periódica una revisión nueva de la ruta asignada y mostrar `AgentDecision.DriverMessage` con revisión y cambios; el acuse local evita repetir el aviso. | D4, FR-11 de 003 |
| FR-13 | La consulta pública `GET /api/v1/public/trace/{unitId}` DEBE recibir el token en la cabecera `X-Trace-Token` (nunca en la query string), resolver `HandlingUnit` y `CustodyEvent` reales con token vigente, devolver solo código, producto/unidad, estado e hitos generales con horas de servidor; revocación, expiración y limitación de consultas se aplican antes de revelar datos. | RF-13, Seguridad y evidencias |
| FR-14 | La página `public-trace` DEBE abrir la URL QR directamente, leer el token del fragmento, enviarlo por `X-Trace-Token`, retirarlo de la barra de direcciones con `history.replaceState` y dejar de usar la `Delivery` como unidad logística o el registro de demo fijo. | RF-13 |
| FR-15 | La finalización de `RouteStop` DEBE derivarse de recepciones aceptadas para sus entregas; el endpoint provisional `/stops/{sequence}/complete` de 002 no puede simular recepción ni modificar inventario. | RF-06, S-04 |
| FR-16 | `POST /api/v1/demo/route-plans/{id}/advance` (Planificación, solo con `Demo:Enabled=true`) DEBE completar las primeras N paradas pendientes ejecutando **preparación, despacho y recepción reales** con los servicios de este spec y las cantidades planificadas, de modo que inventario, custodia y estado de parada queden conciliados. Es idempotente por `operationId`. Reemplaza al endpoint provisional en los recorridos de 002 y 003 y en el pitch. | S-04, Dataset y escenarios (reinicio de demo) |
| FR-17 | La PWA DEBE usar el login, los guards y el interceptor del [spec 013](../013-login-y-cuentas/spec.md) (Authorization Code + PKCE, ADR-18); 004 no implementa otro flujo de autenticación. El token no se persiste en el outbox (NFR-03). En la demo, las sesiones se inician antes del pitch. *(Desde el 13-oct el login es responsabilidad de 013; antes lo asignaba este FR.)* | ADR-18, Identidad OIDC y sesiones, spec 013 |
| FR-18 | La PWA DEBE publicarse por HTTPS en el despliegue de demo (ruta `/campo/` del mismo dominio, servida por nginx) para poder instalarse y usar el service worker; los clientes Cognito web y móvil DEBEN incluir las URL de retorno y cierre HTTPS de la demo además de las de `localhost`. | ADR-04, ADR-18, Perfil A Reforzado |
| FR-19 | `GET /api/v1/deliveries` DEBE listar entregas paginadas por ruta y estado para Gerencia, Planificación y Bodega, con punto, vencimiento, estado operativo y de evidencia, para el panel 005; no expone datos privados de la consulta pública. Dueño: equipo 004; integrar antes de H4. | RF-07, spec 005 |

### No funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| NFR-01 | La PWA instalada DEBE abrir asignaciones ya descargadas y permitir captura tras activar modo avión y reiniciar, en el teléfono usado para el pitch. | RNF-04, S-05 |
| NFR-02 | Las claves de idempotencia DEBEN conservarse al menos 7 días desde la aceptación para cubrir la ventana de trabajo offline de la demo. | RNF-02 |
| NFR-03 | Tokens OIDC NO DEBEN guardarse en el outbox durable; una sesión vencida conserva capturas y exige reautenticación antes de sincronizar. | ADR-18, Movil offline y sincronizacion |
| NFR-04 | Foto y firma DEBEN quedar en S3 privado; solo actores asignados pueden obtener acceso temporal de lectura. El resumen público nunca devuelve URL privada ni metadatos de evidencia. | RF-09, RF-13 |
| NFR-05 | La PWA DEBE avisar si IndexedDB no puede persistir operación o archivos por cuota; no muestra «guardada» en ese caso. | RNF-04 |
| NFR-06 | El QR público DEBE funcionar por HTTPS desde un teléfono ajeno con datos móviles durante el ensayo de nube. | Plus 3, Checklist y contingencias |

## Entidades y contratos

Usa `Delivery`/`DeliveryLine` y lotes FEFO de 002, `RoutePlan`/`RouteStop`, `HandlingUnit`/`HandlingUnitLine`, `CustodyEvent`, `Receipt`/`ReceiptLine`, `Evidence`, `InventoryMovement` e `IdempotencyRecord` del núcleo 001. Una migración posterior a 001 añade asociación unidad–entrega, token público revocable y metadatos privados S3; no reemplaza el esquema base de 001. `Delivery.AssignedAccountId` limita lo descargable y recibible por conductor.

| Método y ruta | Rol | Resultado principal |
|---|---|---|
| `POST /api/v1/deliveries/{id}/handling-units` | Bodega | Unidad preparada, líneas y URL/etiqueta QR |
| `POST /api/v1/deliveries/{id}/dispatch` | Bodega | Movimientos, custodia y entrega despachada |
| `GET /api/v1/mobile/assignments` | Conductor | Snapshot asignado y revisión de ruta |
| `GET /api/v1/deliveries` | Gerencia, Planificación, Bodega | Lista paginada por ruta y estado, con punto, hora comprometida, estado operativo y estado de evidencia para el panel 005; sin datos privados de custodia pública |
| `POST /api/v1/deliveries/{id}/receipts` | Conductor asignado | Recepción idempotente y estado de evidencia |
| `POST /api/v1/receipts/{id}/evidence-uploads` | Conductor asignado | Autorización de carga S3 por archivo |
| `POST /api/v1/evidence/{id}/confirm` | Conductor asignado | Evidencia confirmada tras comprobar objeto |
| `GET /api/v1/handling-units/{id}/trace` | Roles operativos autorizados | Custodia completa permitida |
| `GET /api/v1/public/trace/{unitId}` + cabecera `X-Trace-Token` | Público con token | Resumen limitado de la unidad |
| `POST /api/v1/demo/route-plans/{id}/advance` | Planificación (solo demo) | Completa N paradas con preparación, despacho y recepción reales |
| `POST /api/v1/handling-units/{id}/public-token/revoke` | Bodega, Gerencia | Revocación del enlace público |

JSON camelCase, horas ISO 8601 UTC y errores Problem Details con código estable y `correlationId`. En operaciones reintentables, `Idempotency-Key` y `operationId` deben coincidir. El detalle de cuerpos, estados y migración va en `plan.md`.

## Guardarraíles aplicados

| Guardarraíl | Aplicación |
|---|---|
| G1 | La etiqueta y la interfaz toman códigos, SKU y nombres de datos; el código no incluye el hospital ni cantidades del fixture. |
| G2 | Las unidades y recepciones nacen de entregas operativas; el reset de demo usa el importador de 001 para su escenario inicial. |
| G3 | Si falta asignación descargada o la revisión está antigua, la PWA lo indica; sin señal no inventa una unidad desconocida. |
| G4 | Cada línea conserva unidad base del SKU y valida lote/cantidad por `DeliveryLine`. |
| G7 | `HandlingUnit` y `Receipt` no dependen de la categoría hospitalaria. |
| G8 | El trabajo de 004 queda en el registro previo y el tag del equipo antes del evento. |

## Fuera de alcance

- Build nativo de Capacitor, GPS continuo, geolocalización en segundo plano y push.
- Chat, Telegram, correo, WhatsApp o mensajería bidireccional.
- ~~Recepción por una sesión independiente de Receptor en el recorrido de demo; el receptor firma voluntariamente en el dispositivo del conductor.~~ (Superado por el [spec 014](../014-receptor-recepcion-real/plan.md): el Receptor registra la recepción con su propia cuenta escaneando el QR del bulto).
- Impresora térmica o integración con drivers; se imprime la etiqueta desde el navegador.
- Acceso público a cantidades, firmas, fotos, nombres personales, teléfonos, rutas completas o inventario.
- Conciliación automática de devoluciones rechazadas sin escaneo físico en bodega.

## Contradicciones con la bóveda

> **⚠️ Contradicción detectada** — Hecho nuevo: la recepción de 004 sustituye el endpoint provisional de 002 que marca una parada completa con horas planificadas o indicadas sin recibo. · Hecho establecido: el [plan de 002](../002-motor-logistico/plan.md#contratos-http) propone `/stops/{sequence}/complete`. · Impacto: conservar ambos como caminos equivalentes permitiría una parada «completa» sin inventario recibido. · Opciones: (a) desactivar el marcado provisional cuando 004 se integre; (b) conservarlo solo para fixture de pruebas sin efecto operativo; (c) mantener ambos. · Propuesta: (a), más el endpoint de demo `advance` (FR-16), que completa paradas con operaciones reales. Al integrar 004, el E2E-03 de 002, `calibrate-incident.sh`, el escenario `incident` de la prueba de aceptación de 003 y su E2E pasan a usar `advance`; antes de esa integración siguen usando el endpoint provisional.

> **⚠️ Contradicción detectada** — Hecho nuevo: la página pública exige token y consulta `HandlingUnit`/`CustodyEvent` reales. · Hecho establecido: el código actual de `PublicTraceService` devuelve una `Delivery` o un registro fijo de demo sin token, y la página usa un código precargado. · Impacto: hoy podría presentar hitos que no ocurrieron y exponer una consulta sin el control propuesto en [Seguridad y evidencias](../../boveda/03%20Arquitectura/Seguridad%20y%20evidencias.md). · Opciones: (a) reemplazar servicio y página; (b) ocultar la página hasta completar la API. · Propuesta: (a), sin afirmar que ya está implementado.

> **⚠️ Contradicción detectada** — Hecho nuevo: el token público viaja en el fragmento de la URL del QR y en la cabecera `X-Trace-Token`. · Hecho establecido: [Contratos API y eventos](../../boveda/03%20Arquitectura/Contratos%20API%20y%20eventos.md) propone `GET /api/v1/public/trace/{unitId}?token=...`. · Impacto: con la query string, el token queda en los logs de acceso de nginx y en el historial del navegador. · Opciones: (a) fragmento + cabecera; (b) query string con logs filtrados. · Propuesta: (a).

> **⚠️ Contradicción detectada** — Hecho nuevo: 004 agrega asociación unidad–entrega y token público revocable después del núcleo de 27 tablas. · Hecho establecido: [spec 001](../001-nucleo-delgado-importador/spec.md#criterios-de-éxito) cierra con 27 tablas (desde el 12-oct, su prueba exige ese núcleo y la ausencia de Schema95, no un conteo cerrado, para que 002–004 añadan tablas sin romperla), mientras el [esquema completo de la bóveda](../../boveda/04%20Datos%20y%20algoritmos/Esquema%20completo%20de%20base%20de%20datos.md) prevé `DeliveryHandlingUnit` y `QrAccessToken`. · Impacto: el conteo de 001 debe medirse antes de la migración de 004; el esquema final de la demo crece de forma explícita. · Opciones: (a) migración posterior de 004; (b) ampliar 001 y retrasar su hito. · Propuesta: (a).

> **✅ Resuelta (15-oct-2026, #162)** — Recepción final única por entrega. · Hecho nuevo: el issue #162 exige que el receptor solo reciba un bulto entregado y que exista un único recibo final; FR-07 permitía claves nuevas contra “cantidades restantes”. · Hecho establecido: FR-07 y el texto de H3 presentaban la parcial como conciliable en recepciones posteriores. · Impacto: dos cuentas o claves podían cerrar la entrega en varios recibos y ocultar el alcance multibulto. · Decisión: `Received` y `Partial` son terminales, cualquier recibo previo responde 409 `ALREADY_RECEIVED` antes de la versión, la cobertura es exacta sobre todas las líneas despachadas y lo rechazado permanece bajo custodia del conductor hasta una devolución explícita. Detalle en [Contratos API y eventos](../../boveda/03%20Arquitectura/Contratos%20API%20y%20eventos.md) y en [evidencia-issue-162.md](evidencia-issue-162.md).

Notas revisadas sin otra contradicción: Movil offline y sincronizacion (cola, conflictos), Contratos API y eventos (idempotencia y endpoints), Decisiones de arquitectura (ADR-06, ADR-17, ADR-18), Dataset y escenarios (S-05, S-06, S-07, S-11), Plus para ganar (plus 3).

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| El navegador del teléfono no persiste Blobs tras reinicio o agota cuota | Media | Alto | Ensayo en el dispositivo real; prueba de cuota y bloqueo claro antes de prometer «guardada» |
| El servicio worker o HTTPS no está listo para instalar la PWA | Media | Alto | Probar instalación y modo avión antes de H3; plan B D8: recepción en línea y video |
| El teléfono del jurado no tiene conexión al despliegue | Media | Medio | Ensayo con datos móviles; plan B D8: teléfono del equipo en hotspot |
| Una revisión de ruta o un segundo dispositivo agota la cantidad pendiente | Media | Alto | Bloqueo de líneas, validación de negocio y conflicto persistido en la PWA |
| S3 falla después de aceptar la recepción | Media | Medio | Estados separados, reintento por archivo y acceso privado solo tras confirmación |

## Criterios de éxito

- CE-01: con `demo-region`, una caja preparada en bodega se despacha y queda ligada a la entrega, al lote FEFO, al vehículo y a un `CustodyEvent` real.
- CE-02: en teléfono real, modo avión + foto + firma + reinicio conserva operación y ambos archivos; al recuperar red, una recepción y sus movimientos aparecen una sola vez.
- CE-03: repetir la misma clave y cuerpo devuelve el resultado anterior; la misma clave con otro cuerpo da 409; otra clave que excede el pendiente da 409 sin cambiar stock.
- CE-04: una recepción parcial deja aceptado en el punto y rechazado en el vehículo; la devolución se registra por una operación posterior de bodega.
- CE-05: dos dispositivos y una revisión de ruta producen aceptación solo con entrega, asignación y cantidades vigentes; el resto queda en «Conflicto» con captura recuperable.
- CE-06: foto y firma constan como evidencia privada confirmada; una falla de carga muestra «Evidencia pendiente» y se recupera sin duplicar el recibo.
- CE-07: el teléfono del jurado abre la etiqueta QR por HTTPS y ve solo producto/unidad, estado e hitos reales; token revocado, vencido o alterado no revela datos.
- CE-08: una nueva revisión con `DriverMessage` muestra un aviso en la PWA del conductor asignado y no lo repite después de marcarlo leído.
- CE-09: la PWA se instala desde `https://<dominio de demo>/campo/` en el teléfono del pitch; Bodega y Conductor inician sesión con PKCE y sus llamadas a la API llevan el access token; ningún token aparece en IndexedDB durable.
- CE-10: `POST /api/v1/demo/route-plans/{id}/advance {stops: 2}` deja 2 paradas completadas con recibos, movimientos y custodia reales; repetirlo con el mismo `operationId` no cambia nada; el incidente predefinido de 002 se ejecuta después sobre ese estado.

## Preguntas abiertas

- PA-01: ¿quiénes del equipo figurarán como Deciders de las propuestas D1–D8 y de este diseño? Decide: equipo; registrar nombres y fecha antes de pasar el spec a `aprobado`.
- PA-02: ¿qué teléfono Android, navegador y URL HTTPS se usarán en los ensayos de CE-02 y CE-07? Decide: equipo antes de H3.
- PA-03: ¿la pantalla de Bodega (preparar caja, imprimir etiqueta, escanear y despachar) vive en la web o en la PWA con rol Bodega? El plan solo tiene la página de etiqueta; falta la interfaz del despacho que se muestra en escena. Decide: equipo antes de T2.
- PA-04: ¿la evidencia se sube con URL prefirmadas desde el navegador (requiere CORS del bucket y permisos IAM del EC2, del equipo de infraestructura) o a través de la API, que la guarda en S3 desde el servidor? Recomendación de la revisión del 12-oct: a través de la API. Decide: equipo antes de T6.
- PA-05: ¿el límite de consultas del QR público se aplica por IP o por token? Los jurados comparten IP en la wifi del recinto (NAT); recomendación: por token con umbral holgado y prueba con 7 escaneos simultáneos. Decide: equipo antes de T7.
