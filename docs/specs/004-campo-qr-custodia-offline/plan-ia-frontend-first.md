# 004 — Plan para agente de IA: frontend completo con backend simulado

> **Para el agente de IA que ejecute este plan.** Lee primero [spec.md](spec.md), [plan.md](plan.md), [task.md](task.md) y [test-e2e.md](test-e2e.md). Este documento **no reemplaza** esos cuatro: define *qué construir ahora sin backend*, *cómo dejarlo listo para conectarse* y *qué se documenta*. Responsable humano: Alexander Martínez. Rama: `feature/004-campo-qr-custodia-offline`. Linear: ALK-12 a ALK-23.

## 1. Objetivo y estrategia

Dejar **funcionando y con el diseño terminado** todo lo de 004 que vive en el cliente (PWA del conductor, etiqueta/despacho de Bodega, consulta pública, login), apoyado en un **backend simulado** que respete los contratos del spec. Cuando llegue el backend real, solo se cambia el adaptador HTTP; la UI, el outbox y los estados no se tocan.

**Patrón obligatorio (puertos y adaptadores):**

```text
UI (páginas)  →  servicios de aplicación  →  puerto FieldApi (interfaz)
                                                ├─ HttpFieldApi   (real, apunta a /api/v1/*, se completa al recibir el backend)
                                                └─ MockFieldApi   (simulador in-browser, activo por defecto)
```

- Selección por configuración: `environment.useMockApi` (true por defecto en desarrollo y demo sin backend).
- `MockFieldApi` **replica las reglas del spec** (validación de cantidades, idempotencia, conflictos 409, revocación de token, latencia y fallos inyectables) y persiste su "servidor" en IndexedDB propio (`mock-server`) para que sobreviva recargas. Todo lo mock debe vivir en carpetas `*/mock/` y mostrar una insignia visible «Datos simulados» en la UI.
- Los tipos de request/response salen de `libs/shared-core/src/contracts/field.contract.ts` (se crea aquí, ver T1 de este plan); el backend real deberá cumplirlos.

## 2. Qué se hace ahora y qué queda bloqueado

| Tarea oficial | Ahora (con mock) | Bloqueado por backend/infra (queda en docs oficiales) |
|---|---|---|
| T1 (contratos) | `field.contract.ts`, estados, códigos de error Problem Details, `trace.contract.ts` | Migración EF `FieldOperations`, entidades, índices, `FieldSchemaTests` |
| T1b (login/HTTPS) | Cliente OIDC PKCE configurable, guard, interceptor, proveedor de sesión simulada | Valores reales de Cognito, `identity.tf`, nginx `/campo/`, Dockerfile móvil, `apply` |
| T2 (QR/etiqueta) | Pantalla de Bodega: preparar unidad, etiqueta imprimible con QR | Endpoint real, token hash SHA-256, `HandlingUnitPreparationTests` |
| T3 (despacho) | Pantalla de despacho/escaneo con respuesta simulada idempotente | Transacción real, reservas de 002, `DispatchIntegrationTests` |
| T4 (recepción) | Pantalla de recepción (cantidades aceptado/rechazado, motivo) contra mock | Servicio real, bloqueo de líneas, `advance`, `ReceiptIntegrationTests` |
| T5 (offline) | **Completo**: outbox IndexedDB, foto, firma, estados, reintento, PWA/service worker | Solo la prueba en teléfono real con HTTPS |
| T6 (evidencia) | Cola de archivos, reintento por archivo, estados, contra mock | S3 real, URL presignada/vía API (PA-04), `EvidenceIntegrationTests` |
| T7 (traza pública) | Página pública completa (token en fragmento → cabecera, `replaceState`) contra mock | Endpoint real, límite de tasa, `PublicTraceHttpTests` |
| T8 (avisos) | Polling 15 s, aviso único por revisión, acuse local, contra mock | Productor real de `DriverMessage` (003) y endpoint de asignaciones |
| T9/T10 | Pruebas de navegador del frontend con mock | E2E real, S3, teléfono ajeno, ensayo físico |

> Regla: **si una tarea oficial queda 100 % cubierta por el mock en cliente, se marca como hecha "con mock"**; si tiene parte de backend, esa parte queda pendiente en la documentación oficial.

## 3. Suposiciones (todas deben quedar registradas)

Se copian tal cual a la sección «Suposiciones y mocks» de `task.md` (ver §7). Cada una tiene ID y una forma de verificarla.

| ID | Suposición | Cómo se valida al llegar el backend |
|---|---|---|
| S-01 | Los DTO de [plan.md → Contratos HTTP](plan.md#contratos-http-y-estados) son definitivos: rutas, campos camelCase, UUID v7, UTC ISO 8601, cantidades decimales como `number` con unidad base. | Contrastar `field.contract.ts` con OpenAPI real; ajustar solo el adaptador/tipos |
| S-02 | Códigos de error estables: `IDEMPOTENCY_CONFLICT`, `ASSIGNMENT_CHANGED`, `QUANTITY_EXCEEDED`, más 401/403/404/422/429/503 con Problem Details (`code`, `correlationId`). | Revisar catálogo de errores del backend |
| S-03 | `GET /api/v1/mobile/assignments` devuelve `{serverTimeUtc, routes[{routePlanId, revision, downloadedAtUtc, driverMessage, stops, deliveries, handlingUnits}]}` filtrado por conductor. | Contrato real |
| S-04 | **PA-03:** la pantalla de Bodega vive en `apps/web`, no en la PWA. | Decisión del equipo |
| S-05 | **PA-04:** la evidencia se sube **a través de la API** (no URL presignada). El puerto `FieldApi.uploadEvidence()` es agnóstico; el mock simula ambas. | Decisión del equipo |
| S-06 | **PA-05:** el límite del QR público es por token. El mock devuelve 429 tras N consultas/token (N configurable, por defecto 30/min). | Decisión del equipo |
| S-07 | URL del QR: `https://<web>/public-trace/{unitId}#t=<token>`; el token solo viaja en `X-Trace-Token` y se borra de la barra con `history.replaceState`. | Prueba de navegador |
| S-08 | Token OIDC **nunca** se guarda en IndexedDB durable (NFR-03); solo en memoria/storage de la librería OIDC. | Inspección de IndexedDB en prueba |
| S-09 | Cognito: `authority`, `clientId`, `scope` (`openid istpetdev-api/access`) y URLs de retorno son **placeholders** en `environment.*.ts`. En mock se usa una sesión simulada con roles Bodega/Conductor. | Valores de Terraform |
| S-10 | El escaneo usa `BarcodeDetector` (Chrome/Android) y, si no existe, **ingreso manual del código** de la unidad. No se añade librería de escaneo sin verificarla. | Prueba en el Android del pitch |
| S-11 | `uuid v7` se genera en cliente con función propia (sin dependencia) usando `crypto.getRandomValues`. | Validar con regex v7 del contrato |
| S-12 | Las versiones fijadas del plan (`idb` 8.0.4, `signature_pad` 5.1.4, `qrcode` 1.5.4, `angular-auth-oidc-client` 22.0.1) **existen y son compatibles**; verificar en el registro antes de instalar y registrar la versión realmente usada. | `npm view` + `package-lock.json` |
| S-13 | `@ionic/angular` 8.8.19 ya está en `apps/mobile`; se usa para el layout móvil con el sistema de diseño de la bóveda (ver [Sistema de diseno y colores](../../boveda/03%20Arquitectura/Sistema%20de%20diseno%20y%20colores.md)). | Revisión visual |
| S-14 | La hora contable es la del servidor; `occurredAtDevice` solo es informativa. En mock, el "servidor" usa `Date.now()` del navegador. | Backend real |
| S-15 | Retención de idempotencia ≥ 7 días; el mock guarda registros 7 días. | Contrato backend |
| S-16 | Polling de revisiones cada 15 s con red y en primer plano, configurable. | Prueba E2E-06 |
| S-17 | Reglas de recepción simuladas: suma aceptado + rechazado ≤ pendiente despachado; misma clave+cuerpo = replay; misma clave + otro cuerpo = 409 `IDEMPOTENCY_CONFLICT`; exceso con clave nueva = 409 `QUANTITY_EXCEEDED`; entrega reasignada = 409 `ASSIGNMENT_CHANGED`. | `ReceiptIntegrationTests` reales |
| S-18 | Datos de ejemplo del mock salen de un fixture JSON propio (`mock/fixtures/field-demo.json`), **sin hardcodear el escenario en código** (G1). Es dato sintético y debe etiquetarse. | N/A |
| S-19 | `advance`, preparación y despacho de Bodega no tienen UI de conductor; en la web de Bodega se ofrece botón «Avanzar demo» solo con `Demo:Enabled`. | Backend |
| S-20 | Si 002 no está integrado, los lotes FEFO/reservas son del fixture; **no se afirma** consumo de reserva real. | Integración con 002 |

## 4. Fases de trabajo para el agente

Cada fase termina con `npm run check` (lint FSD + builds) en verde y un commit pequeño con asunto de 10 a 72 caracteres (p. ej. `feat(field): add field contracts and mock api`).

### F0 — Verificación y andamiaje (≈1 h)
1. `git status`, confirmar rama `feature/004-campo-qr-custodia-offline`; `npm ci`; `npm run check` base.
2. Verificar en el registro cada paquete de S-12 (`npm view <pkg> version`); instalar con versión fija solo lo que exista y compile con Angular 22.2.1.
3. Crear carpetas: `apps/mobile/src/app/{field,auth,shared}/…`, `apps/mobile/src/app/field/mock/`, `apps/web/src/features/{dispatch-unit,print-label}/`, `apps/web/src/entities/handling-unit/`.

### F1 — Contratos y puertos (≈2 h)
- Crear `libs/shared-core/src/contracts/field.contract.ts` (request/response de todos los endpoints del spec, enums de estados de PWA y custodia, códigos de error) y ampliar `trace.contract.ts` al contrato público de 004 (sin cantidades, sin actores).
- Definir `FieldApi` (puerto) y tokens de DI en móvil y web.
- Entregable: tipos compilados y exportados desde `shared-core`.

### F2 — Backend simulado (≈3 h)
- `MockFieldApi` + `MockServerStore` (IndexedDB) con reglas S-17, latencia configurable y **interruptores de fallo** (red caída, 401, 429, 503, S3 falla) accesibles desde una pantalla oculta `/dev/mock`.
- Fixture `field-demo.json` (entregas, líneas, lotes, unidades, ruta con revisiones, `driverMessage`).
- Pruebas unitarias del mock (idempotencia, conflictos, exceso, replay).

### F3 — Sesión, roles y guards (≈2 h)
- Configuración OIDC PKCE con `angular-auth-oidc-client` (placeholders S-09) + **proveedor de sesión simulada** (Bodega/Conductor) conmutable.
- Guard por rol leído de la API (`GET account/me` simulado), interceptor que adjunta Bearer solo al origen de la API.
- Prueba: ruta protegida redirige sin sesión; ningún token en IndexedDB (S-08).

### F4 — PWA del conductor: outbox y captura (≈6 h) — **núcleo del spec**
- Descarga/almacenamiento del snapshot de asignaciones; vista offline con hora de descarga y aviso «puede haber cambios».
- `FieldOutbox` (idb): una transacción por identidad con operación + líneas + foto Blob + firma PNG; estados de la máquina de [plan.md](plan.md#máquina-de-estados-de-la-pwa). «Guardado en el teléfono» solo tras commit; cuota/fallo → error claro.
- Pantallas: `home` real (sustituye textos ficticios), `scan` (BarcodeDetector + entrada manual), `receipt` (aceptado/rechazado/motivo, cámara, firma con `signature_pad`), `sync` (estados, reintento, conflictos).
- Sincronización: recibo primero, evidencias después; 401 pausa, 403/409 conservan, red/429/503 reintentan con espera; al abrir, al recuperar red y botón manual.
- Service worker: `ngsw-config.json`, `manifest.webmanifest`, íconos; registrar en `app.config.ts`.

### F5 — Evidencia, avisos y revisión (≈3 h)
- Cola de archivos independiente del recibo, reintento por archivo, estado «Evidencia pendiente».
- Polling de revisión (S-16), aviso `DriverMessage` único por ruta/revisión con acuse local; revisión nueva no pisa capturas pendientes.

### F6 — Web: Bodega y consulta pública (≈4 h)
- Páginas web (FSD, API pública por slice): preparar unidad por línea/lote, etiqueta imprimible con QR (`qrcode`), reimprimir (rota token), revocar, despachar (escaneo/entrada manual), lista de entregas (consume `GET deliveries` simulado).
- `public-trace` real: lee `#t=`, llama con `X-Trace-Token`, `history.replaceState`, respuesta limitada, 404 uniforme, 429 con mensaje; **eliminar el registro fijo de demo**.

### F7 — Diseño final y accesibilidad (≈3 h)
- Aplicar tokens/colores de la bóveda a móvil y web; estados vacíos, carga, error y offline en cada pantalla; tamaños táctiles ≥ 44 px; contraste AA; legible en proyector.
- Insignia «Datos simulados» siempre visible mientras `useMockApi=true`.

### F8 — Pruebas del frontend y cierre (≈3 h)
- Unitarias (mock, outbox, estados, uuid v7) y de navegador (guardado atómico, reinicio, 401/409/429/503, aviso único) con el ejecutor que ya exista; si no existe ninguno, proponerlo **verificando versiones en el registro** antes de añadirlo.
- `npm run check` completo; revisar que `eslint-plugin-boundaries` (FSD) pase.
- Actualizar documentación (§7) y preparar PR a `develop`.

## 5. Checklist para conectar el backend real (fase posterior)

1. Implementar `HttpFieldApi` contra los endpoints reales y poner `useMockApi=false`.
2. Comparar `field.contract.ts` con el OpenAPI; ajustar tipos, no UI.
3. Reemplazar placeholders OIDC (S-09) y verificar S-08.
4. Ejecutar E2E-01 a E2E-10 con evidencia; revisar cada suposición S-xx y marcarla *confirmada* o *corregida*.
5. Retirar `mock/` (o dejarlo solo para pruebas) y la insignia.

## 6. Reglas para el agente

- No inventar datos reales, tokens, claves ni URLs privadas; todo valor ficticio va en el fixture y se etiqueta como sintético.
- Respetar FSD (importar solo por API pública del slice) y el lint de límites.
- No afirmar funcionalidad que depende del backend; la UI debe decir «simulado» cuando use el mock.
- No guardar tokens OIDC en IndexedDB; no usar `localStorage` para la cola.
- Fijar versiones exactas y actualizar `package-lock.json`.
- Commits en español, 10–72 caracteres, sin mensajes genéricos; no hacer push directo a `main`/`develop`.
- No modificar `spec.md` salvo para registrar decisiones/PA resueltas con Deciders confirmados por humano.

## 7. Qué se documenta (oficial: solo lo pendiente)

1. **`task.md`:** marcar `[x]` los ítems resueltos con mock y anotar «(verificado con mock)»; dejar `[ ]` **solo** lo bloqueado por backend/infra/dispositivo (§2, columna derecha). Añadir al final la sección «Suposiciones y mocks» con la tabla de §3.
2. **`test-e2e.md`:** no marcar ningún escenario como ejecutado; añadir una columna/nota «cubierto con mock / pendiente real».
3. **Linear:** mover a `Done` solo T5 (offline) y las partes de UI; las demás quedan `In Progress` con comentario de qué falta (backend).
4. **Bóveda:** actualizar [Plan de trabajo specs 004 y 005](../../boveda/06%20Equipo/Plan%20de%20trabajo%20specs%20004%20y%20005.md) y correr `python docs/boveda/.github/scripts/check_vault.py` (ajustar conteos en «Fuente y control documental»).

## 8. Criterios de aceptación de este plan

- Un conductor simulado completa **sin red**: descargar → captura (cantidades, foto, firma) → reiniciar → ver «Guardado en el teléfono» → recuperar red → «Aceptado por el servidor» → «Evidencia pendiente» → «Completo».
- Replay de la misma operación no crea duplicados; conflicto 409 conserva la captura y es visible.
- Bodega prepara unidad, imprime etiqueta con QR y despacha; el QR abre la traza pública con datos limitados y token fuera de la URL visible.
- Todo corre con `useMockApi=true`, `npm run check` pasa y la documentación oficial solo lista lo pendiente de backend/infra/dispositivo.
