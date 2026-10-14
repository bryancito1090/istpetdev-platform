---
tipo: plan-ejecucion
estado: vigente
actualizado: 2026-10-14
tags: [equipo, linear, spec004, spec005, tareas, campo, simulador]
---

# Plan de trabajo e integración: Specs 004 y 005

> [!important] Spec 004 Concluido — Spec 005 con avances clave en Develop (PR #59)
> Este documento centraliza el desglose operativo de los **Specs 004 (Campo, QR y PWA Offline)** y **005 (Panel Web y Simulador 90 Días)**. Las tareas han sido cargadas en el workspace de Linear (equipo **Alkuy - ALK**).
> **Spec 004 ha culminado su implementación fullstack e integración** (PR #30).
> **Spec 005 integró en develop el ruteo multiparada por pedidos, bodegas, rebalanceo y mapa interactivo** (PR #59, commits `370ffa1` y `22d7985`), quedando pendiente el motor de simulación de 90 días (T2 a T8).

---

## 1. Mapeo General de Proyectos en Linear

| Proyecto Linear | Identificador | Tareas Asociadas | Foco Principal | Estado |
|---|---|---|---|---|
| **Spec 004: Campo, QR, Custodia y PWA Offline** | `43d9562c` | [`ALK-12` a `ALK-23`](https://linear.app/alkuy/project/spec-004-campo-qr-custodia-y-pwa-offline-43d9562c-e23e-47cc-85ac-e7f5e5e55356) | Operaciones físicas: etiquetado QR, despacho de bodega, custodia atómica, PWA offline y conciliación de entrega. | **✅ Concluido** (PR #30, migración `FieldOperations`, API C# y PWA) |
| **Spec 005: Panel Web y Simulador 90 Días** | `da2aa954` | [`ALK-24` a `ALK-35`](https://linear.app/alkuy/project/spec-005-panel-web-y-simulador-90-dias-da2aa954-7136-4c01-a6d7-a4ab2c437b56) | Panel web Angular FSD, visualización de rutas/decisiones de agente, simulador de 90 días, baselines y reporte de impacto reproducible. | **En curso** (Ruteo por pedidos, bodegas, rebalanceo y mapas integrados en develop) |

---

## 2. Spec 004 — Campo: QR, custodia, despacho/recepción y PWA offline (✅ Concluido)

* **Especificación base:** [spec.md](../../specs/004-campo-qr-custodia-offline/spec.md)
* **Plan técnico:** [plan.md](../../specs/004-campo-qr-custodia-offline/plan.md)
* **Tareas detalladas:** [task.md](../../specs/004-campo-qr-custodia-offline/task.md)
* **Escenarios E2E:** [test-e2e.md](../../specs/004-campo-qr-custodia-offline/test-e2e.md)

### Matriz de Tareas en Linear (Spec 004)

| Issue Linear | Tarea | Prioridad | Entregable y Criterio de Aceptación | Estado |
|---|---|---|---|---|
| [**ALK-12**](https://linear.app/alkuy/issue/ALK-12) | **T0** — Preparar contratos de 001/002 y entorno | 🟠 Alta (`High`) | Verificar integración con 001 y 002 (`Delivery`, `HandlingUnit`, `CustodyEvent`, `Receipt`, `StockBalance`, roles fijos). Comprobar bucket S3 privado y teléfono para pitch con soporte PWA. | ✅ Concluido |
| [**ALK-13**](https://linear.app/alkuy/issue/ALK-13) | **T1** — Modelo de datos, migración `FieldOperations` y DTOs | 🔴 Urgente (`Urgent`) | Migración sobre EF Core de `HandlingUnit`, `QrAccessToken` (SHA-256), `IdempotencyRecord` con clave compuesta `(OrganizationId, ActorId, Action, OperationId)`. DTOs camelCase con UUID v7. | ✅ Concluido (`08b4170`) |
| [**ALK-14**](https://linear.app/alkuy/issue/ALK-14) | **T1b** — Autenticación PKCE (Web/PWA) y publicación HTTPS | 🔴 Urgente (`Urgent`) | Configurar `angular-auth-oidc-client` con Authorization Code + PKCE contra Cognito. Publicar PWA en `/campo/` en Nginx con Service Worker (`ngsw-config.json`). No almacenar tokens OIDC en almacenamiento local durable. | ✅ Concluido (`498edcf`) |
| [**ALK-15**](https://linear.app/alkuy/issue/ALK-15) | **T2** — Preparación de caja, código QR y emisión de etiquetas | 🟠 Alta (`High`) | Endpoint `POST /api/v1/deliveries/{id}/handling-units`. Tokens de 128 bits guardados como SHA-256 (validez 7 días). Pantalla web imprimible con QR HTTPS. Reimpresión revoca token previo. | ✅ Concluido (`431b792`, `a16d1a0`) |
| [**ALK-16**](https://linear.app/alkuy/issue/ALK-16) | **T3** — Despacho atómico e idempotente con custodia | 🔴 Urgente (`Urgent`) | `POST /api/v1/deliveries/{id}/dispatch` con `Idempotency-Key`. Bloqueo transaccional, consumo de reservas, transferencia de saldo bodega → vehículo, registro de `CustodyEvent` y detección de conflictos 409. | ✅ Concluido (`431b792`) |
| [**ALK-17**](https://linear.app/alkuy/issue/ALK-17) | **T4** — Recepción en punto, conciliación y parada | 🔴 Urgente (`Urgent`) | Conductor y receptor hospitalario asientan entrega. Aceptado mueve vehículo → punto; rechazado queda en vehículo con motivo. Endpoint `POST /api/v1/demo/route-plans/{id}/advance` para la demo. | ✅ Concluido (`431b792`) |
| [**ALK-18**](https://linear.app/alkuy/issue/ALK-18) | **T5** — Outbox durable y captura offline (IndexedDB + PWA) | 🔴 Urgente (`Urgent`) | Almacenamiento local en `apps/mobile` con IndexedDB (`idb`) y `signature_pad`. Persistir snapshot, líneas, foto Blob y firma en una sola transacción sin red. Sincronización ordenada al reanudar conexión. | ✅ Concluido (`24c8c4c`, `33bd6ab`) |
| [**ALK-19**](https://linear.app/alkuy/issue/ALK-19) | **T6** — Evidencia privada (foto/firma) con S3 presigned URLs | 🟠 Alta (`High`) | `POST /receipts/{id}/evidence-uploads` genera URLs presignadas. Si la carga S3 falla, el recibo e inventario permanecen confirmados y el móvil reintenta sólo la evidencia sin duplicar movimientos contables. | ✅ Concluido (`431b792`, `648bad8`) |
| [**ALK-20**](https://linear.app/alkuy/issue/ALK-20) | **T7** — Trazabilidad pública mínima por QR | 🔵 Media (`Medium`) | `GET /api/v1/public/trace/{unitId}`: muestra sólo información pública (producto, hora, hito). No expone firmas, fotos, nombres de personas ni saldos de bodega. Sanitización en `#t=`. | ✅ Concluido (`431b792`, `8283118`) |
| [**ALK-21**](https://linear.app/alkuy/issue/ALK-21) | **T8** — Asignaciones de conductor y avisos de ruta | 🟠 Alta (`High`) | Lectura de paradas asignadas para el conductor conectado (`GET /mobile/assignments`). Detección de revisiones de ruta o avisos emitidos por el motor o agente (`DriverMessage`). | ✅ Concluido (`431b792`, `648bad8`) |
| [**ALK-22**](https://linear.app/alkuy/issue/ALK-22) | **T9** — Recorrido integrado E2E (API, PWA y Navegador) | 🟠 Alta (`High`) | Validación automatizada de endpoints y frontend: 116 pruebas unitarias C# en verde y `npm run check` con 0 errores. | ✅ Concluido |
| [**ALK-23**](https://linear.app/alkuy/issue/ALK-23) | **T10** — Ensayo físico en teléfono móvil y registro | 🟠 Alta (`High`) | Prueba de interfaz PWA móvil conectada a API real (`localhost:5000`), outbox atómico hospitalario y registro en bóveda Obsidian. | ✅ Concluido |

---

## 3. Spec 005 — Panel web y simulador comparativo de 90 días

* **Especificación base:** [spec.md](../../specs/005-panel-simulador/spec.md)
* **Plan técnico:** [plan.md](../../specs/005-panel-simulador/plan.md)
* **Tareas detalladas:** [task.md](../../specs/005-panel-simulador/task.md)
* **Escenarios E2E:** [test-e2e.md](../../specs/005-panel-simulador/test-e2e.md)

### Matriz de Tareas en Linear (Spec 005)

| Issue Linear | Tarea | Prioridad | Entregable y Criterio de Aceptación |
|---|---|---|---|
| [**ALK-24**](https://linear.app/alkuy/issue/ALK-24) | **T0** — Confirmar base, aprobación y baselines | 🟠 Alta (`High`) | Congelar datasets y reglas de baselines (`reactive-v1` y `fixed7-v1`) antes de generar corridas. Validar integración previa de 001/002. |
| [**ALK-25**](https://linear.app/alkuy/issue/ALK-25) | **T1** — Contratos y lecturas paginadas de rutas (002/004) | 🟠 Alta (`High`) | `GET /api/v1/route-plans` y `GET /api/v1/deliveries` con paginación por cursor y orden estable por revisión vigente. |
| [**ALK-26**](https://linear.app/alkuy/issue/ALK-26) | **T2** — Generador canónico y calendario exógeno (~80 puntos) | 🔴 Urgente (`Urgent`) | Implementar `ScenarioGenerator` y `ExogenousCalendar`. Generación determinista con seeds (3 bases + 1 adversa). Cero visibilidad de demanda futura para las políticas evaluadas. |
| [**ALK-27**](https://linear.app/alkuy/issue/ALK-27) | **T3** — BD aislada de simulación, importador y migración | 🔴 Urgente (`Urgent`) | Base de datos PostgreSQL dedicada para no interferir con la base operativa. `ScenarioImporter` carga datos canónicos con hash SHA-256. |
| [**ALK-28**](https://linear.app/alkuy/issue/ALK-28) | **T4** — Políticas de simulación (Reactiva vs Fija) | 🔴 Urgente (`Urgent`) | Baseline reactivo (pedido cuando stock < seguridad) y baseline fijo (reabastecimiento cada 7 días con lote fijo). Ambas con restricciones de vehículos y horarios idénticas. |
| [**ALK-29**](https://linear.app/alkuy/issue/ALK-29) | **T5** — Adaptador IstpetDev y ciclo horario regional | 🔴 Urgente (`Urgent`) | Conectar motor logístico determinista (002) para ejecutar la política proactiva IstpetDev frente al mismo calendario exógeno. |
| [**ALK-30**](https://linear.app/alkuy/issue/ALK-30) | **T6** — Cálculo de métricas e invariantes de 90 días | 🔴 Urgente (`Urgent`) | Cálculo de los 4 KPIs clave: 1) Quiebres de stock evitados, 2) Demanda no satisfecha, 3) Costo y km recorridos, 4) Nivel de servicio en obligaciones comunes. |
| [**ALK-31**](https://linear.app/alkuy/issue/ALK-31) | **T7** — Worker asíncrono, API y exportador ZIP | 🟠 Alta (`High`) | Procesamiento en segundo plano de 90 días x 3 políticas x 4 escenarios. API de progreso y empaquetado ZIP con manifiesto, hashes y CSVs reproducibles. |
| [**ALK-32**](https://linear.app/alkuy/issue/ALK-32) | **T8** — Benchmark de corrida completa (<60 min) | 🟠 Alta (`High`) | Ejecución en máquina dedicada con tiempo objetivo total ≤ 60 minutos y determinismo exacto de hashes por seed. |
| [**ALK-33**](https://linear.app/alkuy/issue/ALK-33) | **T9** — Panel web en vivo (Angular FSD, mapa y agente) | 🔴 Urgente (`Urgent`) | Interfaz en `apps/web`: mapa de rutas interactivas, columna de decisiones del agente (ejecutadas vs escaladas) y control `ProposeOnly`. |
| [**ALK-34**](https://linear.app/alkuy/issue/ALK-34) | **T10** — Vista de simulación comparativa y gráficas | 🟠 Alta (`High`) | Visualización comparativa de impacto con gráficos de barras/líneas (IstpetDev vs Reactivo vs Fijo) y botón para exportar ZIP reproducible. |
| [**ALK-35**](https://linear.app/alkuy/issue/ALK-35) | **T11** — Recorrido E2E completo, evidencia y cierre | 🟠 Alta (`High`) | Ejecución y acreditación de los 10 escenarios de [test-e2e.md](../../specs/005-panel-simulador/test-e2e.md). |

---

## 4. Guía de Ejecución y Flujo TDD para Alexander Martínez

1. **Rama de trabajo:** Crear rama desde `develop`:
   ```bash
   git switch develop
   git pull origin develop
   git switch -c feature/004-campo-qr-custodia-offline
   # o para el spec 005:
   # git switch -c feature/005-panel-simulador
   ```
2. **Ciclo TDD por cada tarea:**
   * Escribir prueba unitaria o de integración (esperando que falle en rojo).
   * Implementar el código mínimo.
   * Ejecutar suite: `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~<ClasePrueba>"` o `npm run check`.
   * Verificar verde sin fallos.
3. **Commit según normativa ISTPET (10 a 72 caracteres):**
   ```bash
   git add -A
   git commit -m "feat(field): implementar despacho atomico y custodia qr"
   ```
4. **Actualización en Linear:** Al iniciar una tarea, mover el estado en Linear de `Todo` a `In Progress`. Al culminarla con tests pasando, marcar como `Done`.
