# 004 — Campo: QR, custodia, despacho/recepción y PWA offline: plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Las tareas detalladas van en `task.md`, que redacta el equipo.

**Goal:** Conectar una caja QR real con despacho, custodia, recepción durable en modo avión, evidencia privada y consulta pública limitada.

**Architecture:** La API .NET aplica transacciones de despacho y recepción sobre las entregas publicadas por 002; un registro idempotente acompaña cada efecto. La PWA Ionic guarda snapshot, operación y archivos en IndexedDB, sincroniza recibo antes de evidencias y consulta revisiones de ruta para avisos. S3 privado conserva foto y firma; la web pública lee una proyección permitida de `HandlingUnit` y `CustodyEvent` con token revocable.

**Tech Stack:** .NET 8, EF Core/Npgsql 8.0.11, PostgreSQL 16, Angular 22.2.1, Ionic 8.8.19, IndexedDB con `idb`, `signature_pad`, Angular service worker, S3 privado y Cognito PKCE.

**Spec:** [`spec.md`](spec.md)

## Global Constraints

- 001 conserva su cierre de 27 tablas **antes** de la migración de 004. Implementar 004 sobre la rama integrada de 001 y el contrato publicado de 002; no copiar las entidades Schema95 actuales como si fueran el núcleo definitivo.
- 002 crea `Delivery` y `DeliveryLine` con lotes FEFO y reserva. 004 no vuelve a elegir lotes ni replanifica rutas.
- El conductor lee y recibe solo `Delivery.AssignedAccountId` propio; bodega despacha, `Automatizacion` no despacha ni recibe, y el resumen QR no autoriza escritura.
- JSON camelCase, UUID v7 en operaciones, `Idempotency-Key`, fechas UTC ISO 8601, cantidades decimales y unidad base del SKU, errores Problem Details con código estable y `correlationId`.
- El valor `operationId` en cuerpo y `Idempotency-Key` debe coincidir. Retención de registro idempotente: **al menos 7 días**; `occurredAtDevice` no determina el orden contable.
- Una captura offline solo aparece como «Guardado en el teléfono» tras confirmar operación y Blobs en IndexedDB; tokens OIDC quedan fuera de la cola durable.
- Recibo y evidencia tienen estados separados. S3 no es público; la API crea object key, limita MIME/tamaño, verifica objeto y checksum antes de confirmar.
- La consulta pública muestra código, producto/unidad, estado e hitos generales con hora de servidor; nunca cantidades, foto, firma, actores, teléfonos, token en JSON ni ruta completa.
- El 15-oct a las 14:00 se congelan funcionalidades. Si offline no llega, se usa recepción en línea y video de modo avión según D8; el producto no debe presentar capturas pendientes como sincronizadas.

## Review Focus

1. **Reintento después de perder respuesta:** la recepción y sus movimientos ya se confirmaron, pero el móvil no recibió el 200; repetir clave/cuerpo debe reproducir la respuesta y no mover stock otra vez → prueba de integración en la tarea de idempotencia del equipo.
2. **Concurrencia entre dos dispositivos:** ambos ven 12 L pendientes y envían claves distintas; el bloqueo de `DeliveryLine` permite aceptar como máximo lo despachado → prueba de integración en la tarea de recepción.
3. **Revisión de ruta mientras no hay señal:** una versión distinta no rechaza un recibo cuya entrega, asignación y cantidades siguen vigentes; una reasignación sí devuelve conflicto recuperable → prueba en la tarea de validación de recepción.
4. **Archivo local que no se guardó:** cuota o fallo de IndexedDB impide mostrar «Guardado en el teléfono»; la operación y sus Blobs se escriben en una sola transacción local → prueba de navegador en la tarea de outbox.
5. **QR sin control:** token revocado/vencido/alterado y URL sin token no revelan existencia ni datos de unidad; el servicio actual de registro fijo desaparece → prueba HTTP en la tarea de consulta pública.

---

## Arquitectura

```mermaid
sequenceDiagram
  participant B as Bodega
  participant API as API .NET
  participant DB as PostgreSQL
  participant C as PWA conductor
  participant IDB as IndexedDB
  participant S3 as S3 privado
  participant J as Jurado
  B->>API: Preparar unidad desde Delivery publicada
  API->>DB: HandlingUnit + líneas + token hash
  API-->>B: Etiqueta URL QR
  B->>API: Escanear y despachar
  API->>DB: Movimiento bodega→vehículo + custodia (un commit)
  C->>API: Descargar asignaciones y revisión
  C->>IDB: Snapshot asignado
  Note over C,IDB: Modo avión, foto, firma y reinicio
  C->>IDB: Operación + Blobs (una transacción)
  C->>API: Recepción + Idempotency-Key
  API->>DB: Recibo + movimiento vehículo→punto + resultado (un commit)
  API-->>C: Aceptado; evidencia pendiente
  C->>API: Solicitar carga por archivo
  API-->>C: URL presignada limitada
  C->>S3: Subir Blob
  C->>API: Confirmar checksum/objeto
  API->>S3: Comprobar objeto
  API->>DB: Evidencia completa
  J->>API: URL QR con token
  API->>DB: Token vigente + proyección de custodia
  API-->>J: Resumen público limitado
```

### Límites de componentes

| Componente | Responsabilidad | Depende de |
|---|---|---|
| `HandlingUnitPreparation` (Application) | Validar composición por `DeliveryLine`/lote y emitir token; no mueve stock | Entregas de 002, repositorios EF |
| `DispatchService` (Application) | Validar rol, unidad y reserva activa por línea/lote de 002; marcarla consumida al mover bodega→vehículo; escribir custodia y respuesta idempotente | `RouteStockReservation`, `InventoryMovement`, `IdempotencyRecord` |
| `ReceiptService` (Application) | Autorizar asignación, bloquear líneas, validar cantidades, aceptar recibo y mover vehículo→punto | `Delivery.AssignedAccountId`, 002, transacción EF |
| `EvidenceService` (Application + Infrastructure) | Autorizar carga/lectura, crear objeto esperado, confirmar S3 y metadatos | S3 y recepción aceptada |
| `PublicTraceService` (Application) | Resolver token y proyectar únicamente campos públicos | `QrAccessToken`, `HandlingUnit`, `CustodyEvent` |
| `FieldOutbox` (mobile) | Guardar snapshot, operación y archivos por identidad; reintentar y preservar conflictos | `idb`, API móvil |
| `RouteRevisionNotice` (mobile) | Comparar revisión descargada con vigente y mostrar `DriverMessage` una vez | Endpoint de asignaciones, 003 |

## Modelo y migración

Crear **una migración de 004 después de integrar 001 y 002**, sin modificar migraciones anteriores. Si 001 ya incorporó alguna columna o tabla, usarla en vez de duplicarla. Cambios previstos:

| Entidad | Cambio de 004 | Invariante |
|---|---|---|
| `HandlingUnit` | `DeliveryId`, código único por organización, estado `Prepared/Dispatched/Received/Partial` | Unidad asociada a una entrega activa; composición inmóvil tras despacho |
| `HandlingUnitLine` | relación a `DeliveryLineId`, `LotId`, `Quantity` | Mismo lote de línea; suma por entrega/lote ≤ planificado/reservado |
| `QrAccessToken` | tabla nueva si no existe: `HandlingUnitId`, `TokenHash`, `ExpiresAtUtc`, `RevokedAtUtc`, `CreatedByAccountId` | Hash único; token claro solo al emitir etiqueta; revocación efectiva |
| `Receipt`/`ReceiptLine` | operación, versión observada, actor, fechas de dispositivo/servidor y cantidades aceptadas/rechazadas | Suma aceptado + rechazado ≤ despachado pendiente bajo bloqueo |
| `Evidence` | estado y vínculo al objeto privado; si 001 no incluye `StoredFile`, guardar `ObjectKey`, checksum, MIME y tamaño en una extensión mínima | No exponer clave/URL en consulta pública |
| `CustodyEvent` | `HandlingUnitId`, entrega/recibo, actor, ubicación origen/destino, tipo, cantidades y dos fechas | Solo inserción; escaneo no equivale a movimiento sin commit |
| `IdempotencyRecord` | actor, acción, operación, huella, código/cuerpo de respuesta y `RetainUntilUtc` | UNIQUE por organización, actor, acción y operación |

El bloqueo de `DeliveryLine` y `StockBalance` se hace dentro de transacción PostgreSQL; ordenar bloqueos por ID para reducir interbloqueos. El hash idempotente usa una serialización canónica del método, ruta y cuerpo, sin token ni cabeceras volátiles. La restricción de duplicado de negocio se valida incluso con clave nueva. Una recepción aceptada puede ser parcial: el punto recibe solo `QuantityAccepted`; `QuantityRejected` permanece en vehículo. Una devolución posterior desde vehículo a bodega es operación separada de Bodega con su propio movimiento y `CustodyEvent`.

El despacho bloquea también `RouteStockReservation.Active` de 002 por entrega/línea/lote; en el mismo commit la marca `Consumed`, resta su importe de `StockBalance.QuantityReserved` y mueve el stock físico al vehículo. Una revisión posterior nunca libera esa reserva consumida ni revierte el movimiento físico por sí sola.

## Contratos HTTP y estados

Todos los endpoints privados exigen JWT validado por la API. El actor viene de `(issuer, subject)` y `Account.Role`; nunca del cuerpo. Usar roles fijos de 001. En este spec, `Receptor` no opera la PWA de la demo.

| Ruta | Solicitud/resultado | Errores relevantes |
|---|---|---|
| `POST /api/v1/deliveries/{id}/handling-units` | `{ lines:[{deliveryLineId,lotId,quantity}], operationId }` → 201 `{handlingUnitId,code,labelUrl,lines}` | 403 rol, 409 entrega no publicada/composición duplicada, 422 cantidad |
| `POST /api/v1/deliveries/{id}/dispatch` | `{operationId,handlingUnitIds,occurredAtDevice}` + `Idempotency-Key` → 200 entrega/custodia | 403, 409 duplicado o unidad ajena, 422 stock |
| `GET /api/v1/mobile/assignments` | `{serverTimeUtc,routes:[{routePlanId,revision,downloadedAtUtc,driverMessage,stops,deliveries,handlingUnits}]}`; solo asignadas | 401, 403 |
| `POST /api/v1/deliveries/{id}/receipts` | `{operationId,expectedDeliveryVersion,occurredAtDevice,lines:[{deliveryLineId,lotId,quantityAccepted,quantityRejected,unit,rejectionReason}]}` + misma clave → 200 `{receiptId,acceptedAtServer,deliveryVersion,deliveryStatus,evidenceStatus}` | 409 `IDEMPOTENCY_CONFLICT`, `ASSIGNMENT_CHANGED`, `QUANTITY_EXCEEDED`; 422 lote/unidad/estado |
| `POST /api/v1/receipts/{id}/evidence-uploads` | `{operationId,kind,mimeType,byteSize,sha256}` → `{evidenceId,uploadUrl,requiredHeaders,expiresAtUtc}` | 403, 409 archivo ya confirmado, 422 tipo/tamaño |
| `POST /api/v1/evidence/{id}/confirm` | `{operationId,sha256,byteSize}` → estado `Confirmed` tras `HEAD`/comprobación S3 | 403, 409 huella diferente, 503 S3 inaccesible |
| `GET /api/v1/handling-units/{id}/trace` | Custodia operativa autorizada | 403, 404 |
| `GET /api/v1/public/trace/{unitId}?token=…` | `{unitId,status,productUnits:[{sku,unit}],events:[{stage,timestamp}]}`; sin evidencia ni cantidades | 404 uniforme para token ausente/inválido/vencido/revocado; 429 límite |
| `POST /api/v1/handling-units/{id}/public-token/revoke` | 204; emisión de nuevo token requiere operación de Bodega o Gerencia | 403, 404 |

Fijar `PublicTraceResponseDto` y OpenAPI al contrato nuevo. El token QR aleatorio tiene al menos 128 bits de entropía y se guarda como SHA-256; la URL solo usa `unitId` y token, sin datos operativos en la ruta. Vencimiento inicial configurable de **7 días** desde emisión; la etiqueta debe reimprimirse si se rota el token. La respuesta pública usa `Cache-Control: no-store` y límite por IP/tasa en el endpoint. Los hitos se derivan de eventos persistidos, no de tiempos estimados. Horas públicas son las de servidor.

### Máquina de estados de la PWA

```text
Snapshot descargado → Captura local → Guardado en el teléfono
Guardado en el teléfono → Enviando → Aceptado por el servidor → Evidencia pendiente → Completo
Enviando → Conflicto (409 definitivo, conservar operación/Blobs)
Enviando → Guardado en el teléfono (red/429/503, reintentar con espera)
Aceptado por el servidor → Evidencia pendiente (archivo fallido; nunca reenviar recibo)
```

`401` pausa la cola y pide reautenticación; `403` conserva los datos pero bloquea el envío; `409` deja el motivo y la respuesta permitida. La PWA no depende de Background Sync: reintenta al abrirse, al recuperar red y al pulsar «Sincronizar». Compara revisiones de rutas cada 15 s en primer plano con red, además de al abrir; ese intervalo es configuración del cliente. Si detecta revisión mayor, actualiza el snapshot, muestra `DriverMessage` y guarda localmente el último aviso leído por ruta/revisión. La consulta nunca sustituye el acuse HTTP de un recibo.

## Estructura de archivos

Las rutas describen responsabilidades; el equipo escribe el detalle incremental en `task.md` después de aprobar este plan.

| Acción | Ruta | Responsabilidad |
|---|---|---|
| Crear | `src/Application/Field/HandlingUnitPreparationService.cs` | Composición y etiqueta |
| Crear | `src/Application/Field/DispatchService.cs` | Despacho y custodia inicial |
| Crear | `src/Application/Field/ReceiptService.cs` | Validación y aceptación transaccional |
| Crear | `src/Application/Field/IdempotencyService.cs` | Huella, replay y conflicto |
| Crear | `src/Application/Field/EvidenceService.cs` | Autorización y confirmación de archivos |
| Crear | `src/Application/Field/Dtos/FieldContracts.cs` | Contratos de campo; separar si crece |
| Crear | `src/Infrastructure/Storage/S3EvidenceStorage.cs` | URLs presignadas y verificación privada |
| Crear | `src/Api/Controllers/FieldController.cs` | Rutas privadas de unidades, despacho y recepción |
| Crear | `src/Api/Controllers/EvidenceController.cs` | Rutas de archivo privado |
| Modificar | `src/Application/PublicTrace/Services/PublicTraceService.cs` | Proyección de unidad/custodia, sin registro fijo |
| Modificar | `src/Api/Controllers/PublicTraceController.cs` | Token, respuesta uniforme y límite |
| Modificar | `src/Infrastructure/Persistence/ApplicationDbContext.cs` | Entidades, índices y transacciones |
| Crear | `src/Infrastructure/Persistence/Migrations/<timestamp>_FieldOperations.cs` | Única migración después de 001/002 |
| Modificar | `src/Application/Common/Interfaces/IApplicationDbContext.cs` | Acceso usado por servicios de campo |
| Modificar | `src/Application/DependencyInjection.cs` y `src/Infrastructure/DependencyInjection.cs` | Registro de servicios |
| Crear | `libs/shared-core/src/contracts/field.contract.ts` | Tipos compartidos de entrega/recibo y estados |
| Modificar | `libs/shared-core/src/contracts/trace.contract.ts` | DTO público limitado |
| Crear | `apps/mobile/src/app/field/field-outbox.service.ts` | IndexedDB, identidad y Blobs |
| Crear | `apps/mobile/src/app/field/field-api.service.ts` | HTTP privado y sincronización |
| Crear | `apps/mobile/src/app/field/assignment.service.ts` | Snapshot y revisión de ruta |
| Crear | `apps/mobile/src/app/field/scan.page.ts` | Escaneo/unidad conocida |
| Crear | `apps/mobile/src/app/field/receipt.page.ts` | Cantidades, foto y firma |
| Crear | `apps/mobile/src/app/field/sync.page.ts` | Estados, reintento y conflictos |
| Modificar | `apps/mobile/src/app/home/home.page.ts` y `apps/mobile/src/app/app.routes.ts` | Sustituir afirmaciones ficticias y navegación real |
| Modificar | `apps/mobile/src/app/app.config.ts` y `apps/mobile/angular.json` | PWA instalada/service worker e interceptor OIDC |
| Crear | `apps/mobile/ngsw-config.json` y `apps/mobile/public/manifest.webmanifest` | Shell offline y manifiesto instalable |
| Modificar | `apps/web/src/pages/public-trace/public-trace.page.ts` y `apps/web/src/shared/api/api.service.ts` | URL QR directa y respuesta real |
| Crear | `apps/web/src/pages/field-label/field-label.page.ts` | Etiqueta imprimible desde navegador de bodega |

## Decisiones técnicas

| Decisión | Alternativas descartadas | Motivo |
|---|---|---|
| Crear y etiquetar unidad en bodega, luego despachar | Crearla al publicar la ruta | Asociación a una caja física existente y a lotes ya reservados |
| Recibo en commit y evidencia después | Multipart único; lote genérico de sincronización | Archivo fallido se reintenta sin tocar inventario |
| Token aleatorio revocable, hash en DB | Código visible como credencial; token firmado sin revocación persistida | El código no es secreto y se necesita revocar una etiqueta publicada |
| Conflicto por validez operativa | Rechazar toda revisión distinta; último escritor gana | Una ruta revisada no invalida una entrega legítima; cantidades no se exceden |
| Conductor registra; receptor firma voluntariamente | Segunda sesión de Receptor en demo | Reduce pasos de escena sin perder evidencia del receptor |
| Poll de revisiones en primer plano | Push/SignalR obligatorio; Telegram | Recupera cambios tras reconexión sin servicio adicional |
| Etiqueta HTML imprimible | Impresora térmica/driver | Funciona con navegador y caja de demo |

## Dependencias nuevas

| Paquete | Versión fijada | Proyecto | Motivo |
|---|---|---|---|
| [`idb`](https://www.npmjs.com/package/idb) | 8.0.4 | `apps/mobile` | Transacciones IndexedDB con Blobs |
| [`signature_pad`](https://www.npmjs.com/package/signature_pad) | 5.1.4 | `apps/mobile` | Firma táctil a PNG |
| [`qrcode`](https://www.npmjs.com/package/qrcode) | 1.5.4 | `apps/web` | Etiqueta QR en navegador |
| [`AWSSDK.S3`](https://www.nuget.org/packages/AWSSDK.S3/4.0.104.2) | 4.0.104.2 | `src/Infrastructure` | Presignar carga privada y verificar objeto |

Antes de añadir un paquete, comprobar si 001 u otra rama ya lo incorporó y usar la versión existente si cumple el contrato. El `package-lock.json` debe fijar las versiones JS. La SDK de S3 usa credenciales del rol de la API; no se envían credenciales AWS al móvil.

## Prerrequisitos de entorno

1. `git status --short` y confirmar que 001 y 002 estén integrados; si no, trabajar sobre sus contratos publicados y rebasar la migración al integrar.
2. `dotnet --version` debe indicar SDK .NET 8; `node --version` y `npm --version` deben cumplir el README del repositorio.
3. `docker compose ps` debe mostrar PostgreSQL 16/PostGIS para integración; la API tiene Cognito demo configurado con dos cuentas ya iniciadas (Bodega y Conductor).
4. El bucket S3 de evidencias del entorno debe estar privado y accesible por el rol de la API; probar URL HTTPS de web y móvil en el teléfono real. No guardar credenciales en el repositorio.
5. En el navegador objetivo, comprobar instalación de PWA, cámara, IndexedDB con Blobs y carga tras reinicio antes de iniciar tareas de UI.

## Estrategia de pruebas

| Nivel | Pruebas necesarias | Comando/filtro previsto |
|---|---|---|
| Dominio/Application | Composición por lote, suma de cantidades, roles/asignación, estados, huella idempotente, proyección pública | `dotnet test --filter FullyQualifiedName~Field` |
| Integración PostgreSQL | Despacho atómico; recibo + movimientos + respuesta en commit; replay; clave alterada; dos dispositivos concurrentes; parcial/rechazo y devolución | `dotnet test --filter FullyQualifiedName~FieldIntegration` |
| Integración S3 | Presignación limitada; confirmación con objeto/tamaño/huella; falla de archivo sin revertir recibo | prueba con bucket de ensayo privado o emulador compatible, más una prueba real antes de H3 |
| HTTP/autorización | Bodega vs Conductor vs Automatización, asignación ajena, token QR vencido/revocado, ausencia de registro fijo | `dotnet test --filter FullyQualifiedName~FieldHttp` |
| Mobile unit/browser | Guardado atómico de operación+Blobs, reinicio, 401/409/429/503, aviso único por revisión, cuota | pruebas Angular/Playwright según infraestructura del equipo |
| E2E de campo | S-05/S-06/S-07/S-11 en Android real y HTTPS; además teléfono ajeno para QR; registrar tiempos y capturas en `test-e2e.md` del equipo | recorrido manual y automatización que el equipo documente |

El ensayo de H3 (14-oct 20:00) debe registrar cantidad antes/después en bodega, vehículo y punto, IDs de recibo/movimientos, evidencia y estado de la cola. El video de respaldo se graba después de un ensayo funcional, no sustituye la prueba. Antes del congelamiento correr `npm run check`, `dotnet test`, la verificación de bóveda y el recorrido de teléfono real.

## Orden y paralelización

1. **Contrato y migración:** integrar 001 y 002, fijar DTOs, estados, índices y migración. Bloquea el resto del backend.
2. **Backend transaccional:** preparación/QR, despacho, recepción e idempotencia. Se implementa con pruebas de integración antes de la UI; recepción bloquea E2E offline.
3. **En paralelo tras contratos:** la PWA puede construir outbox/captura sobre DTOs; la web puede reemplazar consulta pública y etiqueta; S3 puede preparar `EvidenceService` con interfaz estable.
4. **Integración:** conectar PWA a API, S3 y revisión de ruta de 003. Si 003 aún no existe, probar con una revisión/`DriverMessage` sembrada por API y conectar el productor real después.
5. **Ensayo y corte:** ejecutar S-05/S-06/S-07/S-11 en teléfono real; si no cumple antes de H5, aplicar el plan B de D8. `task.md` y `test-e2e.md` los redacta el equipo.

## Reversión

Si falla la PWA offline en integración, conservar API de recepción y permitir recepción en línea desde móvil; el modo avión se muestra solo en el video de respaldo. Si S3 falla, el recibo queda aceptado con «Evidencia pendiente» y el equipo reintenta después; la consulta pública no afirma que la evidencia esté completa. Si la migración de 004 falla antes de recibir datos, revertir el despliegue de API y restaurar la base desde el backup previo; con datos de recibos, no hacer `down` destructivo: corregir con migración compensatoria y conservar el ledger. Revocar tokens QR emitidos por error y reimprimir etiquetas al restablecer el entorno.
