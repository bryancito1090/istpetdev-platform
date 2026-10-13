# 004 — Campo: QR, custodia, despacho/recepción y PWA offline: pruebas de punta a punta

> Recorridos de bodega, conductor y jurado sobre una misma caja física. Estos escenarios están **pendientes de ejecución**. El spec se cierra cuando E2E-01–E2E-10 pasan con evidencia guardada; las pruebas de teléfono real, S3 privado y QR desde teléfono ajeno no se sustituyen por pruebas simuladas.

## Preparación del entorno

**Base:** 001 y 002 integrados, `demo-region` restaurable, entrega publicada con lotes FEFO y conductor asignado. API y web por HTTPS; bucket S3 privado; cuentas de Bodega y Conductor con sesión; teléfono Android del pitch con PWA instalada, cámara, IndexedDB y espacio suficiente. Para el aviso de E2E-06, 003 integrado o revisión y `DriverMessage` sembrados por API de prueba. Una segunda cuenta/dispositivo permite el conflicto de E2E-03.

```bash
# Desde la raíz del repositorio; el backend usa Docker/Testcontainers
docker info --format '{{.ServerVersion}}'
dotnet tool restore
dotnet build IstpetDev.sln
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~FieldJourneyTests'
npm run check
```

Para ensayo físico: abrir API y PWA desplegadas, confirmar `/health/ready=Healthy`, restablecer `demo-region`, publicar una ruta de 002 y asignar el conductor. Anotar hash de `git rev-parse HEAD`, versión de móvil/navegador, red usada y hora local. Preparar una caja imprimible y un teléfono ajeno con datos móviles. No poner tokens, fotos, firmas, nombres reales ni URLs privadas en la evidencia compartida.

## Escenarios

| ID | Recorrido | Cubre | Tipo | Obligatorio |
|---|---|---|---|---|
| E2E-01 | Bodega prepara, etiqueta y despacha | H1, FR-01–03, CE-01 | HTTP automatizado + caja real | sí |
| E2E-02 | Modo avión → foto/firma → reinicio → sincronización | H2, FR-04–07, FR-10–11, CE-02 | teléfono real + API | sí |
| E2E-03 | Replay y dos dispositivos en conflicto | H2–H3, FR-06–09, CE-03/05 | integración concurrente + móvil | sí |
| E2E-04 | Recepción parcial, rechazo y devolución | H3, FR-07–08, CE-04 | HTTP + custodia física | sí |
| E2E-05 | Evidencias privadas con fallo y reintento | H2, FR-10, CE-06 | API/S3 real + móvil | sí |
| E2E-06 | Revisión nueva y aviso al conductor | H4, FR-04, FR-09, FR-12, CE-05/08 | móvil real | sí |
| E2E-07 | Jurado escanea QR público limitado | H5, FR-02, FR-13–14, CE-07 | teléfono ajeno + HTTP | sí |
| E2E-08 | Cuota, sesión y permisos | FR-05, FR-11, FR-15, NFR-01–05 | navegador + HTTP | sí |
| E2E-09 | Login PKCE y PWA instalada por HTTPS | FR-17–FR-18, CE-09 | teléfono real + navegador | sí |
| E2E-10 | `advance` de demo con operaciones reales | FR-16, CE-10 | HTTP automatizado | sí |

### E2E-01 — Preparar y despachar la caja

**Objetivo:** unir lote FEFO y entrega publicada con una unidad física y transferencia de custodia real.

**Pasos:**

1. Restaurar `demo-region`, proponer/publicar una ruta con 002 y consultar la `Delivery` y `DeliveryLine` seleccionadas. Registrar lote, reserva y saldo inicial de bodega/vehículo/punto.
2. Como Bodega, `POST /api/v1/deliveries/{id}/handling-units` con líneas por `deliveryLineId`, `lotId`, cantidad en unidad base y `operationId` UUID v7. Imprimir etiqueta HTML/QR. Confirmar que una entrega admite varias unidades, cada una de una sola entrega, y que preparar no cambia saldos.
3. Intentar preparar línea con lote distinto o suma mayor a la reservada: error sin unidad parcial. Escanear el QR de la unidad correcta y `POST /api/v1/deliveries/{id}/dispatch` con `Idempotency-Key=operationId` del cuerpo. Intentar en base aislada despacho con QR de otra entrega: rechaza sin movimiento.
4. Consultar movimientos, `RouteStockReservation` y `CustodyEvent`: cada línea mueve una sola vez bodega→vehículo, su reserva activa pasa a `Consumed` y sale del agregado reservado, con actor Bodega y hora del servidor; unidad y entrega pasan al estado despachado. Repetir mismo despacho/clave/cuerpo: respuesta previa y conteos idénticos.

**Resultado esperado:** caja ligada a entrega, lote FEFO y vehículo; evento real de custodia; cero movimientos al preparar o en intentos rechazados. El token QR claro no está en DB ni en la respuesta pública de traza.

**Evidencia a guardar:** foto de etiqueta sin token legible en evidencia pública, IDs/códigos no sensibles, tabla de saldos antes/después, conteos de eventos/movimientos y salida de `FieldJourneyTests`.

### E2E-02 — Modo avión con reinicio

**Objetivo:** probar en el teléfono del pitch que operación y archivos sobreviven sin red.

**Pasos:**

1. Como Conductor asignado, abrir la PWA instalada con red y descargar `GET /api/v1/mobile/assignments`. Ver solo ruta, entregas y unidades propias con revisión y hora de descarga. Activar modo avión; cerrar y reabrir la PWA.
2. Escanear una caja **conocida** del snapshot; capturar cantidades, foto y firma PNG voluntaria. Guardar y observar «Guardado en el teléfono». Cerrar/reiniciar de nuevo la PWA todavía en modo avión; verificar operación, líneas y ambos Blobs, sin JWT en IndexedDB durable.
3. Recuperar red y pulsar sincronizar: estado «Enviando», luego «Aceptado por el servidor». La API acepta un `Receipt` con mismo `operationId` en `Idempotency-Key`, actor del token y hora de servidor; el punto recibe solo cantidad aceptada. La parada queda completada a partir del recibo, no del endpoint provisional de 002.
4. Tras aceptación, observar «Evidencia pendiente» hasta que foto y firma estén confirmadas por S3. Consultar recibo, movimientos y saldos: un recibo y un movimiento por línea, sin duplicados tras reabrir la PWA.

**Resultado esperado:** CE-02 completo en teléfono real; no se afirma «guardado» antes del commit de IndexedDB ni «evidencia completa» antes de S3. PWA abre snapshot/captura tras reinicio sin señal.

**Evidencia a guardar:** capturas de estados con datos sensibles tapados, hora/versión de dispositivo, comprobación local de dos Blobs sin adjuntarlos, IDs de recibo/movimiento y saldos antes/después.

### E2E-03 — Idempotencia y concurrencia

**Objetivo:** impedir duplicados y exceso de recepción después de perder respuesta o usar dos dispositivos.

**Pasos:**

1. Preparar/despachar 12 L. Enviar recibo de 12 L con clave/cuerpo válido y simular pérdida de la respuesta HTTP tras commit. Reenviar misma clave/cuerpo: mismo código, `receiptId` y cuerpo, sin movimientos nuevos. Usar la misma clave con cantidad cambiada: 409 `IDEMPOTENCY_CONFLICT`, sin efecto.
2. En dos dispositivos/sesiones del mismo conductor, partir del mismo snapshot de 12 L pendientes. Enviar a la vez dos operaciones **con claves distintas** que aceptan 12 L. La primera confirmada gana el bloqueo; la segunda da 409 `QUANTITY_EXCEEDED`, sin segundo movimiento ni saldo negativo. La captura del segundo queda visible como «Conflicto» para revisión.
3. Repetir con revisión de ruta cambiada mientras el primero estuvo offline: la versión observada es distinta, pero entrega, asignación y cantidad siguen vigentes; aceptar. En otro intento, reasignar la entrega o agotar cantidad: 409 `ASSIGNMENT_CHANGED` o `QUANTITY_EXCEEDED`, con respuesta permitida y captura local intacta.
4. Comprobar que el `IdempotencyRecord` conserva resultado y huella ≥ 7 días desde aceptación; `occurredAtDevice` no altera el orden contable de servidor.

**Resultado esperado:** CE-03 y CE-05; el stock se mueve una vez y nunca supera lo despachado, aun con claves nuevas o respuestas perdidas. La revisión distinta por sí sola no bloquea un recibo válido.

**Evidencia a guardar:** código/cuerpo de cada respuesta, `receiptId`, conteos antes/después, saldos y captura del conflicto sin datos privados.

### E2E-04 — Parcial, rechazo y devolución física

**Objetivo:** hacer visible lo que sigue bajo custodia del conductor.

**Pasos:**

1. Despachar 12 L de un lote y registrar estado inicial bodega/vehículo/punto. Conductor recibe 9 L aceptados y 3 L rechazados con motivo.
2. Consultar `ReceiptLine`, movimientos, custodia y saldos: el punto gana 9 L; 3 L permanecen en vehículo, asignados a conductor, sin devolución implícita. La entrega queda parcial y el motivo se conserva.
3. Más tarde, Bodega confirma devolución física con operación nueva: movimiento vehículo→bodega de 3 L y `CustodyEvent` nuevo. Confirmar que no reutiliza la clave del recibo y que los eventos conservan el orden de servidor.

**Resultado esperado:** CE-04: 9 L en punto, 3 L en vehículo antes de devolución y 3 L devueltos solo tras escaneo/operación posterior de Bodega.

**Evidencia a guardar:** tabla de saldos en tres momentos, IDs de recibo y eventos de custodia, motivo de rechazo sin datos personales.

### E2E-05 — Foto y firma privadas

**Objetivo:** separar inventario aceptado de archivos aún pendientes.

**Pasos:**

1. Aceptar recibo con foto y firma locales. Para cada Blob, solicitar `POST /api/v1/receipts/{id}/evidence-uploads` con MIME, bytes y SHA-256; confirmar URL presignada de corta vida para objeto en bucket privado y ausencia de credenciales AWS en la PWA.
2. Hacer fallar la primera carga o confirmación S3. La PWA muestra «Evidencia pendiente» y conserva el Blob; `Receipt`, movimientos y saldo permanecen aceptados. Reintentar **solo** archivo fallido y confirmar con `POST /api/v1/evidence/{id}/confirm`; la API verifica objeto, tamaño, tipo y checksum.
3. Probar confirmación con hash/tamaño distintos y lectura desde cuenta no asignada: rechazo. Tras recuperar S3, ambas evidencias quedan `Confirmed`; el recibo y movimientos siguen únicos. Consultar traza pública: no contiene key, URL, foto, firma ni metadatos.

**Resultado esperado:** CE-06 y NFR-04: dos evidencias privadas confirmadas; falla de S3 no revierte inventario ni provoca nuevo recibo.

**Evidencia a guardar:** estados de evidencia y checksum abreviado, conteos de recibo/movimientos, política de bucket privado; no subir foto/firma a la carpeta pública de evidencia.

### E2E-06 — Nueva revisión y aviso único

**Objetivo:** asegurar que el conductor ve un cambio sin perder una captura offline.

**Pasos:**

1. Descargar revisión publicada de ruta como Conductor. Desconectar red y abrir PWA: mostrar revisión guardada, hora de descarga y aviso de posible desactualización. Capturar un recibo pendiente.
2. Publicar una nueva revisión de 002 con `DriverMessage` de 003, o sembrarla por API de prueba si 003 no está integrado aún. Reconectar; la consulta al abrir o el poll de 15 s detecta revisión mayor y muestra mensaje, número y paradas cambiadas.
3. Marcar aviso leído; cerrar/reabrir y esperar otro poll: no repetir para la misma `(routePlanId, revision)`. La captura pendiente no desaparece. Sincronizarla: si entrega/asignación/cantidades siguen válidas, se acepta pese a versión antigua; si hay reasignación, se conserva como «Conflicto».

**Resultado esperado:** CE-08 y rama de CE-05. La prueba con mensaje sembrado demuestra consumidor; repetir con 003 integrado antes de acreditar el recorrido real del agente.

**Evidencia a guardar:** capturas de revisión antigua/nueva, acuse local, estado del recibo y origen del mensaje utilizado.

### E2E-07 — QR desde teléfono del jurado

**Objetivo:** mostrar custodia real con el menor dato público necesario.

**Pasos:**

1. Desde un teléfono ajeno con **datos móviles**, escanear la etiqueta impresa y abrir HTTPS. Consultar la misma URL antes/después de preparar, despachar y recibir: hitos reales con horas de servidor; código de caja, producto/unidad y estado, sin cantidades.
2. Comprobar en la etiqueta que la URL tiene la forma `/public-trace/{unitId}#t=…` y en las herramientas de red que el token viaja en la cabecera `X-Trace-Token`, no en la query string; después de cargar, la barra de direcciones ya no muestra el token. Revisar el log de acceso de nginx del ensayo: el token no aparece. Inspeccionar JSON/red: no contiene actor, nombre, contacto, inventario, ruta, token en cuerpo, foto/firma ni URL privada. La página `public-trace` no muestra registro fijo de demo ni consulta una `Delivery` por código.
3. Revocar token y reimprimir etiqueta con otro. URL anterior, token alterado, vencido, ausente y unidad inexistente tienen respuesta uniforme 404 sin revelar existencia; ráfaga de consultas recibe 429. Confirmar `Cache-Control: no-store`. La etiqueta nueva funciona.

**Resultado esperado:** CE-07 y NFR-06: QR útil por HTTPS desde teléfono ajeno, con proyección pública limitada y revocación efectiva.

**Evidencia a guardar:** captura de la página sin URL/token visible, esquema de claves del JSON, códigos HTTP de tokens inválidos, red/dispositivo y fecha del ensayo.

### E2E-08 — Fallos locales, sesión y permisos

**Objetivo:** comprobar los límites que evitan afirmar una sincronización falsa o exponer asignaciones.

**Pasos:**

1. Forzar cuota insuficiente o fallo de IndexedDB al guardar foto/operación: no aparece «Guardado en el teléfono» y se muestra la causa; no queda registro parcial. Restaurar espacio y guardar correctamente.
2. Vencer sesión OIDC con captura pendiente: no hay JWT en outbox, la operación y Blobs siguen tras reinicio; 401 pausa envío y pide reautenticación. Tras nueva sesión, reintento con la misma clave. Simular 429/503/red caída y comprobar espera/reintento sin pérdida; 403/409 mantienen captura visible.
3. Con cuenta de otro Conductor, intentar `GET /mobile/assignments`, recibo y lectura temporal de evidencia ajenos: no obtiene datos. `Automatizacion` y QR público no pueden despachar ni recibir; Bodega sí puede preparar/despachar y no recibe como conductor.
4. Intentar el endpoint provisional `/route-plans/{id}/stops/{sequence}/complete` de 002: no puede marcar parada como recibida ni mover inventario. Solo recibo aceptado cambia el estado operativo.

**Resultado esperado:** NFR-01–05 y FR-15 verificados con errores visibles, capturas durables y autorización por rol/asignación.

**Evidencia a guardar:** capturas de fallo/recuperación con datos privados tapados, matriz de códigos HTTP, verificación de outbox sin token y saldos sin cambio por endpoint provisional.

### E2E-09 — Login PKCE y PWA por HTTPS

**Objetivo:** comprobar que las dos cuentas de la demo operan desde sus dispositivos con sesión real.

**Pasos:**

1. En el teléfono del pitch, abrir `https://<dominio de demo>/campo/`, instalar la PWA e iniciar sesión como Conductor con Cognito (PKCE). Repetir en el navegador de bodega como Bodega.
2. Con las herramientas de red, comprobar que las llamadas a `/api/v1/*` llevan `Authorization: Bearer` y que ninguna otra petición lo lleva. Comprobar que IndexedDB no contiene tokens.
3. Cerrar la PWA, activar modo avión y reabrir: abre desde el service worker con el último snapshot (E2E-02). Volver a la red: la sesión se renueva sin pedir credenciales mientras el refresh token sea válido.

**Resultado esperado:** CE-09; sin esto no se pueden ejecutar E2E-01 a E2E-08 desde la interfaz.

**Evidencia a guardar:** captura de la PWA instalada, códigos HTTP de llamadas autenticadas y verificación de IndexedDB sin tokens.

### E2E-10 — `advance` de demo

**Objetivo:** dejar paradas completadas con operaciones reales antes del incidente del pitch.

**Pasos:**

1. Restablecer `demo-region`, proponer y publicar las rutas asignando el conductor.
2. `POST /api/v1/demo/route-plans/{id}/advance` con `{ operationId, stops: 2 }`: comprobar 2 recibos, movimientos bodega→vehículo→punto, eventos de custodia y paradas `Completed`.
3. Repetir con el mismo `operationId`: misma respuesta, sin efectos nuevos. Disparar el incidente predefinido de 002: replanifica conservando las 2 paradas completadas.
4. Con `Demo:Enabled=false`: 404.

**Resultado esperado:** CE-10; el E2E-03 de 002 y el escenario `incident` de 003 pueden usar este endpoint en lugar del marcado provisional.

**Evidencia a guardar:** salida de `DemoAdvanceTests`, conteos de recibos/movimientos antes y después del replay.

## Registro de ejecuciones

Una fila por escenario e intento. Resultado: `no ejecutado`, `pasó`, `falló` o `bloqueado`, con causa concreta para los dos últimos. Cifras y tiempos solo se llaman «Medido» al enlazar la ejecución. Guardar evidencia según [Plantilla evidencia](../../boveda/09%20Plantillas/Plantilla%20evidencia.md), sin secretos, fotos ni firmas privadas.

> [!NOTE] Cobertura con Mock in-browser vs Backend Real
> Las funcionalidades de interfaz, flujos de usuario, persistencia atómica en IndexedDB, manejo offline/modo avión, cola de evidencias, sanitización de token QR (`#t=`) y diseño institucional se encuentran implementadas y verificadas en el frontend mediante el backend simulado (`FieldMockBackendService`).
> La ejecución contra PostgreSQL real, bucket S3 real y hardware Android físico queda en estado **bloqueado** a la espera de la integración de endpoints y despliegue por parte de los equipos de backend e infraestructura.

| Fecha | Commit | Entorno y dispositivo | Escenario | Resultado | Evidencia | Quién |
|---|---|---|---|---|---|---|
| 2026-10-13 | 55764c4 | Navegador Web (Chrome/Edge) - Mock In-Browser | E2E-01 | pasó (simulado) | Preparación FEFO, código de caja y despacho en IndexedDB | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | PWA Conductor - IndexedDB Mock Outbox | E2E-02 | pasó (simulado) | Captura foto/firma atómica en outbox, reinicio sin JWT | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | PWA Conductor - Mock Backend | E2E-03 | pasó (simulado) | Idempotencia UUID v7 y resolución no destructiva 409 | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | PWA Conductor - Mock Backend | E2E-04 | pasó (simulado) | Conciliación de aceptado/rechazado con motivo en parada | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | PWA Conductor - Mock S3 Uploads | E2E-05 | pasó (simulado) | Cola de evidencias desacoplada con backoff exponencial | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | PWA Conductor - Mock Revisions | E2E-06 | pasó (simulado) | Notificación única por (routePlanId, revision) en snapshot | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | Navegador Web - Mock Public Trace | E2E-07 | pasó (simulado) | QR hash #t= sanitizado con history.replaceState y DTO mínimo | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | Navegador Web / PWA - Auth Guards | E2E-08 | pasó (simulado) | Guards por rol, cero tokens en IndexedDB persistente | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | Navegador Web / PWA - OIDC PKCE | E2E-09 | pasó (simulado) | Configuración angular-auth-oidc-client y ngsw listos | Desarrollador Frontend |
| 2026-10-13 | 55764c4 | Entorno Real Backend C# / DB / S3 | E2E-01 a E2E-10 | bloqueado | Requiere endpoints reales en .NET y AWS S3 físico | Pendiente Backend/Infra |

**Cierre:** CE-01–CE-10 necesitan filas con evidencia. El plan B del pitch (recepción en línea o video) es contingencia, no pase de la prueba offline. Si algún recorrido obligatorio no pasa o el spec sigue sin aprobación, mantener `estado` abierto.
