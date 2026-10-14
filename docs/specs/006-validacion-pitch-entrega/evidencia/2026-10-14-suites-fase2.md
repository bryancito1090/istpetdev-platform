# Evidencia EJ-07 y EJ-08 — suites que sostienen la matriz de afirmaciones

**Fecha:** 2026-10-14 · **Ejecutó:** Claude Code (Opus 5.5) a pedido de Joseph Baño · **Commit:** `7aed1eb` (`origin/develop`, merge del PR #71) · **Uso:** [matriz de afirmaciones](../matriz-afirmaciones.md).

**Entorno:** Arch Linux x86_64 (kernel 7.2.8), 16 CPU, 14 GB de RAM; .NET SDK 8.0.424; Docker 29.8.2 con PostGIS por Testcontainers (`PostgisContainer`). Sin despliegue, navegador, teléfono, DeepSeek ni n8n.

Son pruebas automatizadas de backend. No son ensayos de la demo ni miden la duración de una escena. Los TRX completos quedaron fuera del repositorio (contienen rutas locales); se anotan sus SHA-256 para identificarlos.

## EJ-07 — 15:23:44 a 15:25:16 (-05:00) · 12/12 pasaron

```bash
dotnet test tests/IstpetDev.Tests --no-build --filter "FullyQualifiedName~LogisticsJourneyTests|FullyQualifiedName~LogisticsPerformanceTests|FullyQualifiedName~SimulationComparisonJourneyTests|FullyQualifiedName~SimulationNationalTests|FullyQualifiedName~DatasetJourneyTests|FullyQualifiedName~PublicTraceHttpTests" --logger "trx;LogFileName=fase2.trx"
```

TRX SHA-256 `a3ccffe59ea2b69e676d69d1d334049ab4f19e850c00f4d1a145f0681f6ad5c5`.

| Prueba | Resultado | Qué comprueba realmente |
|---|---|---|
| `E2E.LogisticsJourneyTests.E2E_01_Reset_regional_and_risks` | pasó | `HOS-N01`: 5,0 días y nivel Alto; hay facts |
| `E2E.LogisticsJourneyTests.E2E_02_Route_determinism_and_fefo_publication` | pasó | Dos rutas, publicación idempotente, reservas activas. **No compara bytes** pese al nombre |
| `E2E.LogisticsJourneyTests.E2E_03_Predefined_incident_autonomy_and_approval` | pasó | Replanificación con decisión y diff. **No verifica** `AutoExecuted` + `Escalated` |
| `E2E.LogisticsJourneyTests.E2E_04_Revert_and_autonomy_mode_control` | pasó | «Solo proponer» → `Escalated`; revertir crea la revisión 3 |
| `E2E.LogisticsJourneyTests.E2E_05_Offline_operation_and_road_cache` | pasó | 240 tramos en caché; plan sin llamadas externas |
| `Performance.LogisticsPerformanceTests.Benchmark_15_points_completes_under_1_second` | pasó | ≤ 1000 ms (la prueba no imprime el valor si pasa) |
| `Performance.LogisticsPerformanceTests.Benchmark_80_points_completes_under_5_seconds` | pasó | ≤ 5000 ms (valor no impreso) |
| `E2E.SimulationComparisonJourneyTests.E2E_FullJourney_SimulationComparison_Deterministic_And_Exportable` | pasó | Recorrido del comparador y exportación |
| `Performance.SimulationNationalTests.National_Run_12_Runs_Finishes_Well_Under_60_Minutes` | pasó | 12 runs con base **en memoria**, sin dataset de 80 puntos |
| `E2E.DatasetJourneyTests.Planner_resets_demo_then_adds_an_external_dataset_without_code_changes` | pasó | Carga `ejemplo-externo` con perfil |
| `Integration.Field.PublicTraceHttpTests.Valid_token_in_header_returns_only_public_fields` | pasó | Vista pública solo con campos públicos |
| `Integration.Field.PublicTraceHttpTests.Invalid_expired_revoked_missing_or_query_tokens_are_uniform_404` | pasó | Token inválido, vencido o revocado → 404 uniforme |

## EJ-08 — 15:28:20 a 15:29:24 (-05:00) · 93/93 pasaron

```bash
dotnet test tests/IstpetDev.Tests --no-build --filter "FullyQualifiedName~DemoRegionTests|FullyQualifiedName~RoutePlanningTests|FullyQualifiedName~AutonomyPolicyTests|FullyQualifiedName~ReplanningTests|FullyQualifiedName~FactsTests|FullyQualifiedName~ExplanationTemplatesTests|FullyQualifiedName~RoutePlannerTests|FullyQualifiedName~AgentRunsApiTests|FullyQualifiedName~AutomationAuthorizationTests|FullyQualifiedName~ImportServiceTests|FullyQualifiedName~Unit.Simulation|FullyQualifiedName~FieldUnitTests|FullyQualifiedName~AuditorRoleTests" --logger "trx;LogFileName=fase2-b.trx"
```

TRX SHA-256 `0c3a07fab18eeb97ccae8b8331597a3704c00c38baa6b2fcd3458eb944c0b037`.

| Clase | Pruebas | Lo relevante para el pitch |
|---|---:|---|
| `Integration.Logistics.DemoRegionTests` | 2 | Riesgos 1 crítico / 4 alto / 3 vigilar / 1 sin datos / 6 normal; `HOS-N01` 5,0 días. El flujo de incidente no verifica el resultado de cada decisión |
| `Integration.Logistics.RoutePlanningTests` | 1 | JSON idéntico con la misma base y reloj |
| `Integration.Logistics.ReplanningTests` | 1 | Conserva paradas completadas e incrementa la revisión |
| `Unit.Logistics.AutonomyPolicyTests` | 8 | Ejecuta sola dentro de límites; escala por costo, retraso, ventana, obligatoria omitida o modo «solo proponer» |
| `Unit.Logistics.RoutePlannerTests` | 5 | Capacidad (peso/volumen), ventana, jornada, punto sin matriz, costo por fórmula |
| `Unit.Logistics.FactsTests`, `ExplanationTemplatesTests` | 8 | Toda cifra de la plantilla coincide con un fact; no inventa cifras |
| `Integration.Agent.AgentRunsApiTests`, `AutomationAuthorizationTests` | 5 | Agente desactivado → `Fallback`; token de automatización no ejecuta decisiones humanas |
| `Integration.Imports.ImportServiceTests` | 9 | Dataset externo con perfil, validación previa, reimportación sin duplicar |
| `Integration.Api.AuditorRoleTests` | 1 | El Auditor lee todo y no escribe nada |
| `Unit.Field.FieldUnitTests` | 4 | Controladores con **servicio falso**: no prueban base de datos |
| `Unit.Simulation.*` | 49 | Calendario por seed, baselines, paridad, invariantes, métricas (evitados y nuevos por separado), exportación/importación con hashes |

## Límites

Ninguna prueba recorre la web o la PWA conectadas, un teléfono real, HTTPS público, DeepSeek real ni una corrida oficial de 005. Cualquier cambio de código posterior a `7aed1eb` obliga a repetir estas suites antes de usar la matriz.
