---
id: 015
titulo: Abrir la PWA de campo sin señal
estado: en curso
creado: 2026-10-15
actualizado: 2026-10-15
aprobado_por: PENDIENTE
depende_de: [004, 013]
boveda: ["03 Arquitectura/Identidad OIDC y sesiones.md"]
---

# 015 — Abrir la PWA de campo sin señal

## Contexto

La PWA de campo ya guarda las capturas en el teléfono: el outbox (`FieldOutboxService`) escribe la recepción, la foto y la firma en IndexedDB antes de enviar nada, y la ruta del conductor queda en caché. Eso funciona **mientras la app siga abierta**.

El hueco está en el arranque en frío. `MobileAuthService.bootstrap()` necesita red dos veces: para renovar el access token (15 min) con el refresh token y para pedir `GET /api/v1/me`. Sin señal, cualquiera de las dos falla, la app nunca fija el dueño del outbox y `saveReceiptAtomic` responde «Inicia sesión para registrar una recepción». Si el sistema cierra la app (o el conductor la cierra) en una zona sin señal, no puede registrar nada hasta volver a tener red.

La bóveda ya lo prohíbe: «no exigir token vigente para conservar una captura ya autorizada localmente» ([Identidad OIDC y sesiones](../../boveda/03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md), sección «Sesión web, móvil y offline»). Este spec cierra la brecha entre esa regla y el código.

## Objetivo y aporte a la nota

El conductor que ya inició sesión una vez con señal puede cerrar la app, quedarse sin red, volver a abrirla y registrar recepciones; al volver la señal la app renueva la sesión sola y envía la cola.

| Criterio | Peso | Cómo lo sostiene este spec |
|---|---:|---|
| Demo / prototipo funcional | 20% | Guion «cierra la app, quita el wifi, ábrela, entrega» frente al jurado sin que se rompa |
| Viabilidad técnica | — | Respuesta a «¿y si no hay señal en la ruta?» con un comportamiento real, no una promesa |

## Historias de usuario

### H1 — Abrir la app sin señal y capturar (P1)

**Como** conductor, **quiero** abrir la app sin señal y registrar la entrega, **para** no depender de la cobertura del punto de entrega.

1. **Dado** que inicié sesión con señal al menos una vez y la sesión sigue vigente (refresh token de 7 días), **cuando** abro la app sin red, **entonces** entro a mi inicio con mi ruta en caché y un aviso «Sin conexión: tus capturas se guardan en el teléfono».
2. **Dado** el modo sin conexión, **cuando** confirmo una recepción, **entonces** queda en la cola como «guardada en el teléfono» y no se intenta enviar.
3. **Dado** que nunca inicié sesión en este teléfono, **cuando** abro la app sin red, **entonces** veo «No pudimos verificar tu cuenta. Revisa la señal e intenta de nuevo.» (sin datos locales no hay identidad que restaurar).

### H2 — Reconectar y enviar solo (P1)

**Como** conductor, **quiero** que al volver la señal la app renueve la sesión y envíe lo pendiente, **para** no tener que hacer nada.

1. **Dado** el modo sin conexión con capturas pendientes, **cuando** vuelve la red, **entonces** la app renueva el token con el refresh token, confirma la cuenta con `/me`, sale del modo sin conexión y envía la cola.
2. **Dado** que el refresh token ya no sirve (revocado o vencido), **cuando** vuelve la red, **entonces** la app pide iniciar sesión y la cola se conserva para la misma cuenta.

## Requisitos

### Funcionales

| ID | Requisito | Traza a bóveda |
|---|---|---|
| FR-01 | Tras un `/me` exitoso de plataforma `field`, la PWA DEBE guardar en el teléfono la última cuenta (`accountId`, `displayName`, `role`, `platform`) y el `sub` del ID token, nunca tokens. Solo se usa si su `sub` coincide con el de la sesión OIDC local. | Identidad OIDC y sesiones |
| FR-02 | Si al arrancar no se puede llegar a Cognito o a la API (sin red, timeout, 5xx) y existe la última cuenta guardada y una sesión OIDC local (refresh token), la PWA DEBE entrar autenticada en modo sin conexión con esa cuenta como dueña del outbox. | «no exigir token vigente…» |
| FR-03 | Si Cognito responde y rechaza el refresh token, la PWA NO DEBE usar la cuenta guardada: queda anónima. | Revocación operativa |
| FR-04 | En modo sin conexión el outbox DEBE guardar capturas y NO DEBE intentar enviarlas hasta que la sesión se renueve. | Sincronización: «obtener sesión válida» |
| FR-05 | Al volver la red, la PWA DEBE reintentar la sesión; si renueva, sale del modo sin conexión y envía la cola; si no, queda anónima sin borrar la cola. | Sincronización |
| FR-06 | Cerrar sesión DEBE borrar la última cuenta guardada. | Logout |
| FR-07 | La PWA DEBE pedir almacenamiento persistente (`navigator.storage.persist()`) para que el navegador no borre la cola. | RNF offline |

### No funcionales

| ID | Requisito |
|---|---|
| NFR-01 | Sin red, el arranque no espera el timeout de 10 s de renovación si el navegador ya sabe que está desconectado (`navigator.onLine === false`). |
| NFR-02 | Ningún token se escribe fuera del almacenamiento de la librería OIDC (D-013-S1). |

## Entidades y contratos

Sin cambios de API. Nuevo estado en el cliente: `MobileAuthService.offline` (señal booleana) y `FieldOutboxService.syncHeld` (señal booleana).

## Fuera de alcance

- Primer inicio de sesión sin red (imposible: la identidad y la ruta viven en el servidor).
- Envío en segundo plano con la app cerrada (Background Sync / Capacitor).
- Cambiar la vigencia de los tokens (15 min / 7 días).

## Contradicciones con la bóveda

Ninguna detectada. Revisadas: Identidad OIDC y sesiones (D-013-S1). La última cuenta va a `localStorage` junto a la sesión OIDC, no a IndexedDB ni al outbox, y no contiene tokens.

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Cuenta desactivada mientras el conductor está sin señal sigue capturando localmente | Baja | Bajo | Al enviar, la API revalida y responde 403 → la captura queda en «requiere revisión», no se pierde (spec 004) |
| Wifi sin internet (`navigator.onLine` dice true) | Media | Medio | Errores de red, timeout y 5xx también activan el modo sin conexión |

## Criterios de éxito

- CE-01: Arranque en frío sin red con sesión previa → inicio autenticado en < 2 s y captura guardada en la cola.
- CE-02: Al volver la red, la cola llega a «Completo» sin iniciar sesión de nuevo.
- CE-03: Pruebas unitarias de `MobileAuthService` y `FieldOutboxService` en verde.

## Preguntas abiertas

Ninguna.
