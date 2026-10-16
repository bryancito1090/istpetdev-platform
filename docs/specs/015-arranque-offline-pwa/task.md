# 015 — Abrir la PWA de campo sin señal: tareas

> Pasos del [`plan.md`](plan.md). TDD: prueba que falla → implementación mínima → prueba que pasa.

## Resumen

| ID | Tarea | Depende de | Estado |
|---|---|---|---|
| T1 | Outbox: retener el envío (`setSyncHeld`) y pedir almacenamiento persistente | — | hecho |
| T2 | Sesión: última cuenta guardada, modo sin conexión y `reconnect()` | T1 | hecho |
| T3 | Inicio: aviso de modo sin conexión | T2 | hecho |
| T4 | Prueba de punta a punta en teléfono real | T1–T3 | pendiente |

### Task 1: Outbox retiene el envío

- Test: `apps/mobile/src/app/field/outbox/field-outbox.service.spec.ts` — «015: con la sincronización en espera…» y «015: pide almacenamiento persistente…».
- Modify: `field-outbox.service.ts` — `syncHeld`, `setSyncHeld()`, `canSend()`, `requestPersistentStorage()`.
- Run: `npm run test:mobile` → PASS.

### Task 2: Sesión sin señal

- Test: `apps/mobile/src/app/auth/auth.service.spec.ts` — bloque «arranque sin señal (015)».
- Modify: `auth.service.ts` — `restoreSession(): Promise<'ok' | 'none' | 'unreachable'>`, `enterOffline()`, `reconnect()`, `offline`, `rememberAccount()`, `lastAccount()`; `logout()` borra la cuenta guardada.
- Run: `npm run test:mobile` → PASS.

### Task 3: Aviso en el inicio

- Test: `apps/mobile/src/app/home/home.page.spec.ts` — «015: en modo sin conexión avisa…».
- Modify: `home.page.ts` — bloque `@if (authService.offline())`.
- Run: `npm run test:mobile` → PASS.

### Task 4: Prueba de punta a punta

Ejecutar E2E-01 y E2E-02 de [`test-e2e.md`](test-e2e.md) en un teléfono con la PWA instalada y registrar la evidencia.
