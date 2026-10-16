# 015 — Abrir la PWA de campo sin señal: pruebas de punta a punta

## Preparación del entorno

- PWA de campo desplegada (build de producción: el service worker solo se activa fuera de `isDevMode()`).
- Teléfono con la PWA **instalada en la pantalla de inicio**, cuenta Conductor con una ruta asignada.

## Escenarios

| ID | Escenario | Cubre | Tipo | Obligatorio |
|---|---|---|---|---|
| E2E-01 | Cerrar la app, quitar la señal, abrir y capturar | H1, FR-02, FR-04 | manual | sí |
| E2E-02 | Volver la señal y ver la cola enviada sin iniciar sesión | H2, FR-05 | manual | sí |
| E2E-03 | Primer uso sin señal | H1.3 | manual | no |

### E2E-01 — Abrir sin señal y capturar

1. Con señal: iniciar sesión como Conductor y abrir el inicio (descarga la ruta).
2. Cerrar la app desde el gestor de tareas. Activar modo avión.
3. Esperar más de 15 minutos (o borrar el access token desde las DevTools remotas) para forzar el token vencido.
4. Abrir la app.

**Resultado esperado:** inicio del Conductor con su ruta y el aviso «Sin conexión: tus capturas se guardan en el teléfono». Registrar una recepción → en «Cola» aparece como guardada en el teléfono.

### E2E-02 — Volver la señal

1. Desde E2E-01, desactivar el modo avión con la app abierta.

**Resultado esperado:** el aviso desaparece y la recepción pasa a «Completo» sin pasar por la pantalla de inicio de sesión.

### E2E-03 — Primer uso sin señal

1. Teléfono sin sesión previa, modo avión, abrir la app.

**Resultado esperado:** «No pudimos verificar tu cuenta. Revisa la señal e intenta de nuevo.»

## Registro de ejecuciones

| Fecha | Commit | Escenario | Resultado | Evidencia | Quién |
|---|---|---|---|---|---|
| 2026-10-15 | — | Unitarias (`npm run test:mobile`, 87/87) | PASS | salida local | Claude |
