# 015 — Abrir la PWA de campo sin señal: plan de implementación

**Goal:** que la PWA arranque autenticada sin red con la última cuenta guardada, capture en el outbox y envíe la cola al renovar la sesión.

**Architecture:** `MobileAuthService.bootstrap()` distingue «sin sesión» de «no se pudo preguntar». En el segundo caso usa la última cuenta guardada (sin tokens) y entra en modo sin conexión; el outbox guarda pero retiene el envío (`syncHeld`) hasta que `reconnect()` renueva la sesión al volver la señal.

**Tech Stack:** Angular 22, angular-auth-oidc-client 22, IndexedDB (`idb`), Vitest.

**Spec:** [`spec.md`](spec.md) · Tareas: [`task.md`](task.md)

## Global Constraints

- Ningún token fuera del almacenamiento de la librería OIDC (D-013-S1). La clave `istpetdev.lastFieldAccount` solo guarda `accountId`, `displayName`, `role`, `platform` y `subject` (`sub` del ID token).
- Cognito que **responde** y rechaza el refresh token gana siempre sobre la cuenta guardada (FR-03).
- Mensajes en español, sin jerga técnica.

## Review Focus

1. Volver la señal con access token vencido no debe disparar un 401 → redirección a Cognito antes de renovar: `syncHeld` bloquea el envío hasta `reconnect()` → T1, T2.
2. Wifi sin internet (`navigator.onLine` true): timeout, error de red o 5xx también entran en modo sin conexión → T2.
3. Otra persona que inicia sesión en el mismo teléfono no hereda la cuenta guardada: `lastAccount()` exige que el `subject` coincida con el `sub` del ID token local, y valida plataforma y rol → T2.
4. Cerrar sesión sin señal borra la cuenta guardada y no deja la cola retenida → T2.
5. Listeners de `online` no se acumulan: uno por servicio, se quita al reconectar o destruir → T2.

## Arquitectura

| Pieza | Cambio |
|---|---|
| `apps/mobile/src/app/auth/auth.service.ts` | `restoreSession()` devuelve `ok \| none \| unreachable`; `enterOffline()`, `reconnect()`, `offline` (señal), guardar/leer la última cuenta |
| `apps/mobile/src/app/field/outbox/field-outbox.service.ts` | `syncHeld` + `setSyncHeld()`; `canSend()` centraliza «puedo enviar»; `navigator.storage.persist()` al iniciar |
| `apps/mobile/src/app/home/home.page.ts` | Aviso «Sin conexión: tus capturas se guardan en el teléfono» |

## Decisiones técnicas

| Decisión | Alternativas descartadas | Motivo |
|---|---|---|
| Última cuenta en `localStorage` | IndexedDB del outbox | La bóveda prohíbe datos de sesión en IndexedDB/outbox; la sesión OIDC de la PWA ya vive en `localStorage` |
| Estado `authenticated` + señal `offline` | Nuevo `FieldSessionStatus` | Guardas, login y páginas siguen funcionando sin cambios |
| Retener el envío en el outbox | Dejar que el 401 pida login | Un 401 al volver la señal redirigía a Cognito y podía perder un formulario a medio llenar |
| Mantener 15 min / 7 días | Alargar el access token | Capturar sin señal no usa el access token; acortarlo limita el daño de un token robado |

## Dependencias nuevas

Ninguna.

## Estrategia de pruebas

Unitarias en Vitest (`npm run test:mobile`): `auth.service.spec.ts` (12 casos nuevos), `field-outbox.service.spec.ts` (2), `home.page.spec.ts` (1). Prueba manual de punta a punta en [`test-e2e.md`](test-e2e.md).
