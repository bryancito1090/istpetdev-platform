---
tipo: registro-ejecucion
estado: pendiente
actualizado: 2026-10-14
tags: [equipo, seguimiento, pruebas, hallazgos]
---

# Registro de seguimiento y hallazgos

Tablero editable para ejecutar [[Seguimiento de implementacion y pruebas]] desde el 13-oct. **La campaña integrada todavía no se ha ejecutado.** La evidencia local anterior de 001 se conserva como referencia; cada responsable actualiza aquí resultados reales. Los criterios completos pertenecen a los specs y [[Plan de validacion]].

## Cabecera de la campaña

| Campo | Valor a completar |
|---|---|
| Fecha/hora y zona | PENDIENTE; America/Guayaquil |
| Commit común y PR/checks | PENDIENTE |
| Imagen/digest, si aplica | PENDIENTE |
| Coordinación / revisor de salida | PENDIENTE / PENDIENTE |
| Entorno y alcance | PENDIENTE: local aislado / ensayo autorizado expuesto |
| Base operativa / simulación | PENDIENTE: nombres, sin cadenas de conexión |
| Fixture vivo / dataset simulado / matriz | PENDIENTE: versiones y hashes |
| Seeds / políticas / parámetros | PENDIENTE |
| Teléfono / navegador / URL sin token | PENDIENTE |
| Máquina dedicada del simulador | PENDIENTE |
| Revisión prelaunch | PENDIENTE: enlace, commit y límites |
| Racha de ensayos / material | PENDIENTE: contador y versión |

## Entrega de módulos

Estados operativos para este tablero: pendiente, entregado, integrado, verificado localmente, verificado en entorno de ensayo, bloqueado. El estado formal de cada spec se actualiza en su documento; «cerrado» requiere evidencia de sus criterios.

| Spec | Responsable / revisor | Estado de esta campaña | Commit/PR | Comando y evidencia | Pendientes |
|---|---|---|---|---|---|
| 000 | Anthony Pilatasig / PENDIENTE | Pendiente | PENDIENTE | [spec 000](../../specs/000-gestion-dependencias-seguridad-monorepo/spec.md) | Inventario de dependencias, remediación, CI de auditoría; pnpm es P2 |
| 001 | Anthony Pilatasig y Joseph Baño / PENDIENTE | Pendiente de revalidación integrada | PENDIENTE | Referencia previa: [verification.md](../../specs/001-nucleo-delgado-importador/verification.md), 115 pruebas locales | Simulacro humano, identidad/entorno reales y cierre documentado |
| 002 | AnthonyPilatasig / Bryan | Verificado localmente (T0-T10 completadas) | Issue #24 | `dotnet test` (67/67 pruebas de logística con 0 fallos: suites unitaria, integración, rendimiento y E2E completas) | Ninguno (Listo para PR a develop tras revisión de pares) |
| 003 | AnthonyPilatasig / Bryan | Integrado en develop (T0 a T6 completadas) | PR #40 (8774971) | Suites de Agente (22/22 pruebas con 0 fallos: Persistencia 3/3, Cifras 6/6, Auth 2/2, HMAC 6/6, API 3/3, Traza 2/2) | T7 a T9 (Workflows n8n locales, #37 y #39) |
| 004 | Alexander Martínez / Bryan | Verificado e integrado localmente (T0-T10 completadas) | PR #30, commits `431b792`, `08b4170` | `dotnet test` (116/116 unitarias pasan), `npm run check` (0 errores, builds web y mobile exitosos) | Ninguno (Fullstack completado: API C#, migraciones EF, PWA y Web) |
| 005 | Alexander Martínez y Bryan Doicela / Por asignar | Ruteo multiparada, bodegas y rebalanceo integrados en develop (PR #59) | PR #59 (`d4f35c9`, `370ffa1`, `22d7985`) | 145 unitarias + 111 integración pasan (0 fallos). Páginas `/bodegas` y `/rutas` interactivas en Web. | T2 a T8 (Calendario exógeno y worker de simulación 90d) |
| 006 | Todo el equipo (quien la tome) / revisión cruzada | Pendiente | PENDIENTE | [spec 006](../../specs/006-validacion-pitch-entrega/spec.md) | Preparación/ensayos; entrevistas y entrega en evento |
| 013 | Alexander Martínez y Joseph Baño / revisión cruzada | H1–H3 completadas en branch (#42) | Branch `feature/42-login-y-cuentas` (`fef78be`) | Pruebas de integración de Auditor (200 lectura / 403 escritura), `/me`, provisión Cognito y sesión segura en storage | H4 (Gestión de cuentas por UI en P2) |

## Registro de pruebas

Una fila por ejecución o conjunto automatizado identificable. Usar referencias calificadas, como «004 E2E-03», para no mezclar IDs de diferentes specs. Resultados: aprobado, fallo, pendiente de entorno, no aplica con justificación.

| Fecha | Caso / requisito | Commit y entorno | Ejecutor / revisor | Esperado → observado | Resultado | Evidencia / hallazgo |
|---|---|---|---|---|---|---|
| 2026-10-13 | 001 + 002 (T0 a T7: Núcleo, Importador, Matriz vial, Ruteo determinista, Publicación FEFO, Autonomía, Incidentes y Replanificación) | Issue #24 (Local Docker PostGIS 15432) | AnthonyPilatasig / Bryan | 171 pruebas pasan con 0 fallos (118 núcleo/importador + 53 motor logístico) | Aprobado | IncidentEvaluatorTests, ReplanningTests pasan. Reversión genera 3ra revisión y libera reservas intermedias sin duplicar. |
| 2026-10-13 | 002 (T8: Endpoints API, Autorización, Inyección de dependencias y Lectura FR-23) | Issue #24 (Local Docker PostGIS 15432) | AnthonyPilatasig / Bryan | LogisticsApiTests (5/5 pasan, 57 pruebas logística totales con 0 fallos). Matriz de roles validada: conductor 403 en creación de planes y cambio de modo, gerencia 200 en modo, lectura 200 con facts ordenados y paginación no mutante FR-23. | Aprobado | Controllers expuestos: Risks, RoutePlans, Incidents, AgentDecisions, Autonomy. |
| 2026-10-13 | 002 (T9: Fixture regional y calibración del incidente) | Issue #24 (Local Docker PostGIS 15432) | AnthonyPilatasig / Bryan | DemoRegionTests (2/2 pasan, 60 pruebas logística totales con 0 fallos). Fixture demo-region importado vía canónica: 1 bodega, 15 puntos, 2 vehículos, 3 SKUs, 240 tramos viales; reloj Dataset fija 2026-10-12; riesgos calibrados: 1 Crítico, 4 Alto (HOS-N01 cobertura 5.0d), 3 Vigilar, 1 SinDatos, 6 Normal; flujo completo de plan, publicación, 2 paradas completadas, incidente y replan verificado. | Aprobado | Fixture data/fixtures/demo-region y scripts/data/build-road-segments.py, calibrate-incident.sh |
| 2026-10-13 | 002 (T10: Recorrido E2E, Rendimiento y Bóveda) | Issue #24 (Local Docker PostGIS 15432) | AnthonyPilatasig / Bryan | LogisticsJourneyTests (5/5 pasan) + LogisticsPerformanceTests (2/2 pasan). Total suite logística: 67 pruebas con 0 fallos. Presupuestos de tiempo medidos: 15 puntos / 2 vehículos en ~35 ms (<= 1000 ms), 80 puntos / 2 vehículos en ~2100 ms (<= 5000 ms). E2E-01 a E2E-07 verificados con 100% de requisitos cubiertos. | Aprobado | Registrado en test-e2e.md y ADR-22/23 |
| 2026-10-13 | 003 (T0 y T1: Persistencia AgentRun, columnas de decisión y reset) | Issue #36 (Local Docker PostGIS 15432) | AnthonyPilatasig / Bryan | AgentRunPersistenceTests (3/3 pasan) + LeanSchemaTests (3/3 pasan). Guardado completo de AgentRun, validación de DriverMessage <= 160 caracteres, borrado en DemoResetService sin referencias colgantes y migración AgentRuns sin cambios pendientes. | Aprobado | Migración 20261013203831_AgentRuns.cs |
| 2026-10-13 | 003 (T2: Validador de cifras y explicaciones del LLM) | Issue #36 (Local xUnit) | AnthonyPilatasig / Bryan | ExplanationValidatorTests (6/6 pasan en 17 ms). Regex sin falsos positivos en códigos de entidad (VEH-02, HOS-N01), validación de horas HH:mm, porcentajes, redondeos decimales y conteos contra Facts canónicos. | Aprobado | ExplanationValidator.cs |
| 2026-10-13 | 003 (T3: Separación de identidad M2M y scope de automatización) | Issue #36 (Local Docker PostGIS 15432) | AnthonyPilatasig / Bryan | AutomationAuthorizationTests (2/2 pasan) + AuthorizationSafetyTests (8/8 pasan). 403 Forbidden estricto en las 5 operaciones humanas (publicar, aprobar, rechazar, revertir, cambiar modo) para rol Automatizacion / token n8n; 201 y 200 en operaciones de incidentes, riesgos y replanificación; 401 en scopes no técnicos o faltantes. Recurso M2M Cognito y scope istpetdev-api/automation declarados en Terraform identity.tf. | Aprobado | AutomationAuthorizationTests.cs |
| 2026-10-13 | 003 (T4: Firma HMAC-SHA256 y disparo de webhook n8n) | Issue #36 (Local xUnit) | AnthonyPilatasig / Bryan | N8nTriggerTests (6/6 pasan en 2 s). Cabeceras X-IstpetDev-Timestamp y X-IstpetDev-Signature (sha256=hex), timeout estricto de 2 s ante demoras de red, rutas separadas /watch y /incident, verificación con anti-tampering y rechazo de timestamps con deriva > 60 s sin imprimir secretos en logs. | Aprobado | N8nTrigger.cs |
| 2026-10-13 | 003 (T5: Orquestador de ejecuciones, resolución de carreras y fallback fail-safe) | Issue #36 (Local Docker PostGIS 15432) | AnthonyPilatasig / Bryan | AgentRunsApiTests + AgentRunTraceTests (5/5 pasan). Transiciones de estado atómicas con UPDATE condicional; fallback determinista garantizado en <= 11 s ante agent_disabled, timeout o error de agente; resolución de carreras atómica (ganador único); rechazo con 409 Late a callbacks tardíos; traza append-only persistida. | Aprobado | AgentRunService.cs |
| 2026-10-13 | 003 (T6: Endpoints HTTP de AgentRuns, Middleware de observabilidad y correlación) | Issue #36 (Local Docker PostGIS 15432) | AnthonyPilatasig / Bryan | AgentRunsApiTests (3/3 pasan) + AgentRunTraceTests (2/2 pasan). Endpoints POST /agent/runs, GET /agent/runs/{id}, GET /agent/runs, POST /agent/runs/{id}/result (202/409/422 con RBAC estricto Automatizacion) y GET /incidents/{id} expuestos; cabecera X-Agent-Run-Id trazada en middleware solo para llamadas M2M. | Aprobado | AgentRunsController.cs, AgentRunTraceMiddleware.cs, IncidentsController.cs |
| 2026-10-14 | 003 (#37: T7/T8/T9 parcial) | Rama feature/37-n8n-workflows-local; n8n local 2.39.5 y borradores remotos | Bryan / Codex | Backend: 12 pruebas de integración; Python: 4 generador, 4 aceptación, 1 fixture, 1 limpieza. Borradores remotos vigia/incidente inactivos con DeepSeek nativo; parámetros/conexiones coinciden con repo. | En curso | Plan M2M revisado/autorizado por Bryan y aplicado: 1 añadido/1 actualizado/0 destruidos; token emitido con scope automation. Configuración API pendiente. Aceptación real y CE-06 pendientes; ver [task.md](../../specs/003-agente-logistico/task.md). |
| 2026-10-13 | 004 (T0 a T10: Campo, QR, Custodia, Despacho, Recepción Hospitalaria, Outbox Offline PWA y Trazabilidad Pública) | Commits `431b792`, `08b4170`, `33bd6ab` | Alexander Martínez / Bryan | Endpoints C# (Deliveries, HandlingUnits, Evidence, MobileAssignments, PublicTrace, Demo Advance) integrados; migración FieldOperations aplicada; PWA móvil outbox atómico hospitalario conectado a API local localhost:5000; Web consola bodega conectada; trazabilidad pública con token #t= y rate limiter probada. 116 pruebas unitarias aprobadas, npm run check 0 errores. | Aprobado | Registrado en task.md, informe oficial de entrega y ADR-24/ADR-25/ADR-27 |
| 2026-10-14 | 013 (T1 a T9: Rol Auditor, Endpoint /me, Directorio Cognito, Provisionador H3 y Sesión sin Tokens) | Commits `cfdd22a` a `fef78be` (branch `feature/42-login-y-cuentas`) | Alexander Martínez / Joseph Baño | AuditorRoleTests y RolesTests pasan (Auditor con 200 en lecturas y 403 estricto en escrituras); Endpoint GET /api/v1/auth/me devuelve rol y compatibilidad web/field; script idempotente de provisión de cuentas de demo; sesiones seguras en storage (ADR-24). | Aprobado | Registrado en task.md de spec 013, AuditorRoleTests.cs, RolesTests.cs, token-free-storage.ts |
| 2026-10-14 | Ruteo de Pedidos, Bodegas y Rebalanceo (ADR-28 / Integración PR #59) | Commits `370ffa1` y `22d7985` en `develop` (`d4f35c9`) | Alexander Martínez / Bryan | 145 pruebas unitarias y 111 de integración pasan con 0 fallos. Migración OrderBasedRoutingAndWarehouseStock aplicada; LeanSchemaTests valida exactamente 36 tablas núcleo y MigrationSafetyTests valida 7 migraciones de EF Core. Sourcing multialmacén FEFO, emparejador de vehículos por peso/volumen, rebalanceo inter-bodegas y páginas web `/bodegas` y `/rutas` conectadas con Leaflet. | Aprobado | Registrado en ADR-28, SourcingService.cs, RebalancingAdvisor.cs, VehicleMatcher.cs, RouteEvaluator.cs, bodegas.page.ts, rutas.page.ts |
| 2026-10-14 | Flujo BD dev vs prod, Fail-Fast, CI/CD y Rollback (Issue #35 / PR #70) | Commits `0c13909` y `d2b7850` en `develop` | Jorge Doicela / Joseph Baño | `deploy.yml` y `rollback.yml` exigen `/health/ready` estricto (0 fallbacks a `/live`); `PrepareDatabaseAsync` propaga error crítico (fail-fast); `MigrationSafetyTests` actualizado a 8 migraciones canónicas tras incorporar `SimulationRunsAndEvents`; suite de integración en verde en GitHub Actions; contingencia Plan B documentada. | Aprobado | [[Evidencias y ensayos de base de datos y despliegue]], [[Runbook de DevOps y base de datos para la demo]], PR #70 |


Adjuntar resúmenes con [[Plantilla evidencia]]: datos iniciales, pasos, valores/unidades, IDs de operación y archivos anonimizados. Guardar TRX/logs bajo `artifacts/seguimiento/<sha>/`; los registros compartidos enlazan evidencia accesible sin tokens, firmas, fotos privadas ni secretos. Conservar los archivos privados en almacenamiento autorizado.


## Hallazgos a confirmar al inicio

Estas filas provienen de [[Analisis de cobertura de specs]] y deben contrastarse con lo que entreguen los compañeros. **Son pendientes de revisión documental, no fallos runtime observados.** Cerrar los que ya tengan contrato/implementación/evidencia actualizados y crear hallazgos nuevos solo después de observarlos.

| Referencia local | Pendiente a contrastar | Dueño del tema | Responsable | Estado / resolución |
|---|---|---|---|---|
| SEG-01 | Estados y alcance de ADR-21; conservar RNF-14/B-40 y distinguir roles fijos de administración configurable | Coordinación / 001 | PENDIENTE | Por confirmar |
| SEG-02 | Transición preservando migraciones frente a instrucciones anteriores de recreación de bases | 001 / infraestructura | PENDIENTE | Por confirmar |
| SEG-03 | Interfaz de Bodega para preparación y despacho de la escena | 004 | Alexander Martínez | **Cerrado:** Implementada en `apps/web/src/pages/warehouse/warehouse.page.ts` y PWA móvil con selección de entrega, unidades QR y despacho físico conectado a backend C# `DeliveriesController`. |
| SEG-04 | Contrato e interfaz de devolución física ya exigida por 004 | 004 | Alexander Martínez | **Cerrado:** Flujo de rechazo físico, conciliación de líneas aceptadas/rechazadas, actas de novedades y transferencia contable implementado en `FieldService.cs` y PWA móvil `HomePage`. |
| SEG-05 | Vía de upload de evidencias, límites QR y lectura de cuenta/rol para guards | 004 / infraestructura | Alexander Martínez | **Cerrado:** Endpoints de evidencias presignadas en `EvidenceController.cs`, limitador de tasa `PublicTraceRateLimiter` para escaneos QR y guards OIDC locales en Web y Móvil. |
| SEG-06 | Cliente M2M, credenciales autorizadas y versión de n8n local | 003 / infraestructura | Jorge / Bryan | Parcial: n8n 2.39.5 confirmada y DeepSeek remota enlazada; M2M/scope aplicados con autorización y token emitido; configuración de API y aceptación local/producción pendientes (#37/#38). |
| SEG-07 | Obligaciones comunes, matriz, máquina dedicada y benchmark completo | 005 / datos | PENDIENTE | Por confirmar |
| SEG-08 | Tratamiento explícito de RF-15/V-19: alcance actual o entrega 007 | Producto / coordinación | PENDIENTE | Por confirmar |
| SEG-09 | Evidencia de navegador/PWA y filtros CI de módulos/workflows, además de build web | 003/004/005 / CI | PENDIENTE | Por confirmar |

## Registro de defectos y necesidades

Severidad:

- **Bloqueador:** pérdida/duplicado de inventario o captura, acceso indebido, flujo esencial roto, simulación inválida o evidencia inexistente que impide el pase.
- **Mayor:** un caso obligatorio falla con alternativa limitada; corregir antes del cierre de ese requisito o registrar recorte explícito con su efecto.
- **Menor:** problema de presentación/usabilidad que permite completar el caso; priorizar según uso y dejar responsable/plazo.

| ID local | Tipo / severidad | Caso y spec dueño | Esperado / observado | Commit/entorno y pasos | Responsable / siguiente revisión | Estado | Evidencia / corrección / retest |
|---|---|---|---|---|---|---|---|
| PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE | Sin hallazgos observados en esta campaña aún | PENDIENTE |

Tipos: defecto, contrato incompleto, entorno/acceso, falta de evidencia, mejora futura. Estados: abierto, en corrección, pendiente de revalidación, cerrado con evidencia, diferido por decisión enlazada. Los IDs locales SEG/HAL organizan esta campaña; no sustituyen RF/RNF/V/B ni se incorporan a nombres de código.

## Registro de decisión de salida

| Puerta | Estado inicial | Evidencia / pendientes | Responsable y fecha |
|---|---|---|---|
| Versión integrada y contratos | Pendiente | PENDIENTE | PENDIENTE |
| Suite automática y CI correspondiente | Pendiente | PENDIENTE | PENDIENTE |
| Recorrido sin agente y teléfono real | Pendiente | PENDIENTE | PENDIENTE |
| n8n, incidentes, fallback y aceptación | Pendiente | PENDIENTE | PENDIENTE |
| Fallos, roles fijos/asignación y prelaunch | Pendiente | PENDIENTE | PENDIENTE |
| Simulación completa, invariantes y exportación | Pendiente | PENDIENTE | PENDIENTE |
| Preparación del 006 y ensayos | Pendiente | PENDIENTE | PENDIENTE |
| **Pase técnico del núcleo** | **Pendiente** | Bloqueadores abiertos: PENDIENTE de revisión | PENDIENTE |
| **Cierre final del 006/hackathon** | **Pendiente de criterios y evento** | Entrevistas, validación y entrega 16–17 oct | PENDIENTE |

Con cada salida guardar decisión, evidencia y límites: «continuar correcciones», «verificado localmente», «demo con limitaciones» o «pase técnico aprobado». Un pendiente de entorno no se convierte en aprobado por el paso del tiempo.

## Cierre de jornada y siguiente entrega

Copiar este bloque para cada día:

- Fecha y versión repetible:
- Módulos integrados y casos aprobados:
- Hallazgos abiertos y responsables:
- Pendientes de entorno y fecha/evento:
- Cambios de contrato/alcance registrados:
- Próxima prueba:
- Primera entrega de continuidad y criterio de entrada:
- Responsable de la siguiente revisión:

El orden de continuidad está en [[Continuidad del piloto y specs 007 a 012]]. Las pruebas nuevas y decisiones se enlazan desde los specs y [[Plan de validacion]] al ejecutarse; no mantener estados contradictorios en distintos índices.

### 14 de octubre de 2026 — Entrega y verificación local spec 003 (#37)

- Fecha y versión repetible: 2026-10-14, commit base `dfd4507`, rama `feature/37-n8n-workflows-local`.
- Módulos integrados y casos aprobados: Spec 003 (agente logístico n8n). 227/227 pruebas .NET verdes, 10/10 pruebas Python verdes. Matriz de aceptación con DeepSeek nativo en API local 5017 (30/30 corridas exitosas, 0 fallbacks, 100% herramientas 2xx, p95 9127 ms, 96% decisiones LLM, 10/10 inyecciones neutralizadas; reporte `artifacts/agent-acceptance/20261014-150147-176964/report.md`). Ensayo CE-06 verificado en laptop (Plan B: 21 ms fallback con n8n caído, 6661 ms recuperación con n8n levantado).
- Hallazgos abiertos y responsables: Despliegue en servidor de producción, fijación de versión en Compose y reset de base compartida delegados a #39 (`docs/specs/003-agente-logistico/plan-cierre-39-produccion.md`). Integración en `develop` vía PR asignada a Codex.
- Pendientes de entorno y fecha/evento: Revisión e integración por Codex; prueba oficial en producción (#39) antes del pitch (16-17 oct).
- Cambios de contrato/alcance registrados: Validador estricto con signos respetados y facts semánticos positivos (`cost_reduction_pct`, `advance_minutes`); cálculo de retraso en política sin redondeo prematuro; persistencia atómica de decisiones previa al estado Completed.
- Próxima prueba: Revisión, resolución de conflictos con `origin/develop` (333ce68) y PR por parte de Codex.
- Responsable de la siguiente revisión: Codex (revisión e integración post-entrega).


### 14 de octubre de 2026 — Checkpoint productivo del spec 003 (#39)

- Antigravity entregó preparación de Compose/CD y diagnósticos de producción;
  Codex conservó el checkpoint e incorporó develop @ `51c3b30`, sin conflictos.
- Transporte M2M/agente revisado para deploy y rollback: client ID explícito,
  HMAC desde GitHub Secrets, agente deshabilitado por defecto y rechazo de
  activación sin credenciales. No cambian workflows, prompt ni proveedor.
- Verificación local de Codex: Python 16/16, .NET HMAC/secretos 11/11,
  `npm run check`, generación reproducible y actionlint en verde. Son pruebas
  del software de configuración; no una aceptación del servidor.
- PR #78 fusionado y CD `9d1a3d9` exitoso; este checkpoint todavía requiere
  integración a develop y promoción por el flujo autorizado.
- Pendientes de #38/#39: fijar n8n sin latest, enlazar credenciales, configurar
  variables/cuentas productivas, publicar webhooks, comprobar M2M 200/403,
  Completed/Fallback, 30 muestras y CE-03 de dos decisiones en ventana acordada.
  La última matriz local disponible falló p95 (10001 ms); no se reemplaza por
  el informe inicial aprobado ni se declaran todos los CE cumplidos.
- Detalle y límites: [entrega de #39](../../specs/003-agente-logistico/entrega-39-antigravity.md).
