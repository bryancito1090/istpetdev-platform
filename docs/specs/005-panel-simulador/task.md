# 005 — Panel web y simulador comparativo de 90 días: tareas

> Pasos ejecutables de [spec.md](spec.md) y [plan.md](plan.md). **Ninguna tarea está ejecutada.** La rama documental actual todavía contiene Schema95 y no tiene el importador de 001 ni el planificador de 002; iniciar T0 solo después de integrarlos en `develop`. Los fragmentos son contratos y pruebas a materializar, no evidencia de código compilado. Cada tarea sigue rojo → implementación mínima → verde → commit. Comandos desde la raíz del repositorio.

## Resumen y ruta crítica

| ID | Entrega verificable | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T0 | Base, Deciders, baselines y ramas confirmados | 001/002 | — | hecho (desarrollo sobre develop integrado con 001/002/004) |
| T1 | Contratos de lectura de rutas y entregas | T0, 002/004 | [P] con T2 | hecho (integrado en develop vía PR #47 y PR #59) |
| T2 | Generador canónico y calendario exógeno | T0, 001 | [P] con T1 | hecho |
| T3 | Base aislada, importador y migración única | T2, 001/002 | — | pendiente |
| T4 | Políticas reactiva y fija | T2–T3 | [P] con T5 | hecho |
| T5 | Adaptador IstpetDev y ciclo horario regional | T2–T3, 002 | [P] con T4 | pendiente |
| T6 | Métricas, invariantes y agregación | T4–T5 | — | pendiente |
| T7 | Worker, API, progreso y exportación | T3, T6 | — | pendiente |
| T8 | Benchmark de corrida completa y corrección medida | T7 | [P] con T9 | pendiente |
| T9 | Panel en vivo, mapa y acciones | T1, 003/004 | [P] con T8 | en curso (mapa Leaflet, ruteo por pedidos y bodegas integrados en PR #59) |
| T10 | Vista de simulación y exportación | T7, T9 | — | pendiente |
| T11 | Recorrido E2E, evidencia y cierre | T8–T10 | — | pendiente |

`[P]` significa que no se editan los mismos archivos. Los cambios de 002/004 pertenecen a sus equipos; si los endpoints no están integrados, T9 no pasa. El simulador T2–T8 no espera al LLM ni a la PWA. **H4 = 15-oct 11:00** con resultados del simulador y panel; H5 = 15-oct 14:00. No sustituir la corrida por cifras precargadas.

## Contratos internos que fijan las tareas

Estas firmas nuevas pertenecen al 005; sus adaptadores llaman a 001/002 una vez integrados. Las implementaciones pueden dividirse por archivo sin cambiar el significado de estos DTOs.

```csharp
namespace IstpetDev.Application.Simulation;

public sealed record ScenarioKey(string DatasetVersion, int Seed, string ScenarioCode);
public sealed record DemandEvent(DateTime OccurredAtUtc, string RegionCode, string PointCode,
    string Sku, decimal Requested, string Unit, DateTime DueAtUtc, bool IsSpike);
public sealed record DisruptionEvent(DateTime OccurredAtUtc, DateTime KnownAtUtc,
    string RegionCode, string Kind, string SubjectCode, decimal Factor);
public sealed record ScenarioCalendar(ScenarioKey Key, IReadOnlyList<DemandEvent> Demand,
    IReadOnlyList<DisruptionEvent> Disruptions, string Sha256);
public sealed record PolicyResult(string PolicyVersion, ScenarioKey Key,
    IReadOnlyList<SimulationDayResult> Days, IReadOnlyList<SimulationEventDto> Events);
public sealed record SimulationDayResult(DateOnly Date, string RegionCode, decimal Km,
    decimal TransportCost, int CompletedDeliveries, int OnTimeCommonObligations);
public sealed record SimulationEventDto(long Sequence, DateTime OccurredAtUtc,
    string Kind, string RegionCode, string PointCode, string Sku,
    decimal? Requested, decimal? Satisfied, string? Reason);
public sealed record MetricValue(string Code, string DefinitionVersion, string? Sku,
    decimal Numerator, decimal? Denominator, decimal? Value, string Unit,
    string? NotApplicableReason);
public interface IScenarioImporter
{
    Task ImportInitialStateAsync(string canonicalDirectory, string simulationConnection,
        CancellationToken cancellationToken);
}
public interface IRegionalPlanner
{
    Task<RegionalPlan> PlanAsync(RegionalPlanRequest request, CancellationToken cancellationToken);
}
public sealed record RegionalPlanRequest(DateOnly Date, string RegionCode,
    string DistanceMode, IReadOnlyList<string> AvailableVehicleCodes);
public sealed record RegionalPlan(decimal Km, decimal TransportCost,
    IReadOnlyList<string> CompletedPointCodes, IReadOnlyList<string> UnservedPointCodes);
```

`ScenarioCalendar` es inmutable durante las tres políticas. Los IDs internos de base no forman parte del hash de eventos; orden estable: hora UTC, región, punto, SKU y tipo. `SimulationRun` representa una política × seed × escenario; `comparisonId` agrupa los doce runs. El manifiesto captura versiones de dataset/generador/política/definición de métrica/código, seed, matriz, parámetros y hashes.

## Regla de prueba y commit

Para tareas .NET: prueba roja con `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~<Clase>"`; error por clase/servicio ausente es rojo válido. Tras implementar: mismo comando con `Failed: 0`, luego `dotnet build IstpetDev.sln` con `Build succeeded`. Para web: usar la prueba de componente configurada por 004; si todavía no existe ejecutor de pruebas web, añadirlo **solo tras verificar el paquete/versiones en el registro** y registrarlo en el PR, o probar el comportamiento por E2E en navegador real. `npm run lint:web` y `npm run build:web` deben pasar. Commits en español y trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; el ejemplo de comando de cada tarea usa `git commit -m '<tipo>(<ámbito>): <acción en español>' -m 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>'`.

### T0 — Confirmar base, aprobación y límites

**Archivos (lectura):** `docs/specs/{001-nucleo-delgado-importador,002-motor-logistico,003-agente-logistico,004-campo-qr-custodia-offline}/{spec,plan}.md`, `docs/specs/005-panel-simulador/{spec,plan}.md`, `global.json`, `apps/web/package.json`.

- [x] Verificar `git fetch origin` y que la rama de implementación nace de `origin/develop` con 001 y 002 integrados. Confirmar `src/Application/Imports/CanonicalWriter.cs`, el importador de `road_segments`, `tests/IstpetDev.Tests/IstpetDev.Tests.csproj`, el planificador y los endpoints 002. Ejecutar `dotnet build IstpetDev.sln`; esperado `Build succeeded`.
- [x] Registrar Deciders y fecha para el spec 005, baseline `reactive-v1`/`fixed7-v1`, demanda sintética, regiones/flota y matriz permitida. Si se cambia una regla antes de la primera corrida, cambiar `policyVersion`/`datasetVersion` en spec y bóveda **antes** de generar resultados. No aprobar con `PENDIENTE`.
- [x] Confirmar login de 013 (H1–H3) y `advance` de 004, 003 `GET/POST /agent/runs`, y contratos nuevos 002 FR-23 / 004 FR-19. El backend T2–T8 puede avanzar sin 003/004; T9 requiere sus contratos reales.
- [x] Comprobar el origen de la matriz y dos conexiones PostgreSQL distintas; si son la misma base, detener la preparación antes de importar. Registrar el compromiso de H4 en el PR de implementación.

### T1 — Lecturas estables de 002 y 004

**Archivos:** 002 modifica `src/Api/Controllers/RoutePlansController.cs` y `tests/IstpetDev.Tests/Integration/Logistics/LogisticsApiTests.cs`; 004 modifica `src/Api/Controllers/DeliveriesController.cs` y `tests/IstpetDev.Tests/Integration/Field/FieldApiTests.cs` (crear si aún no existen). 005 solo verifica y consume.

- [x] Prueba roja HTTP de `GET /api/v1/route-plans?date=2026-10-12&limit=1`: 200, `items` con revisión vigente ordenada por fecha/código/revisión, `nextCursor` estable; segundo GET con cursor no repite ID. Sin token → 401. GET no aumenta conteo de planes.
- [x] Prueba roja HTTP de `GET /api/v1/deliveries?routePlanId=<id>&limit=1`: 200 con `deliveryId`, `routePlanId`, `pointCode`, `dueAtUtc`, `status`, `evidenceStatus`; paginación sin repetición. Conductor no autorizado → 403; vista pública no recibe datos privados.
- [x] Implementar solo lecturas y filtros en los specs productores, sin hacer nuevas migraciones. Ejecutar los filtros `FullyQualifiedName~LogisticsApiTests` y `FullyQualifiedName~FieldApiTests`; esperado `Failed: 0`.
- [x] Los equipos 002/004 hacen sus commits; 005 registra los hashes integrados. Si falta cualquiera antes de H4, escalar como bloqueo de integración, no fijar IDs en la web.

### T2 — Generar canónicos y calendario exógeno

**Archivos:** crear `src/Application/Simulation/{ScenarioGenerator,ExogenousCalendar}.cs`, `data/fixtures/simulation-national/{scenario.json,mapping.json}`, `tests/IstpetDev.Tests/Unit/Simulation/{ScenarioGeneratorTests,ExogenousCalendarTests}.cs`.

- [x] Prueba roja `ScenarioGeneratorTests`: seed 11 y versión `national-v1` generan los mismos bytes de siete CSV canónicos de 001, `road_segments` de 002, manifiesto y SHA-256 en dos ejecuciones; seed 12 cambia calendario, no IDs de puntos/recursos. Con N=6, D=3, dos SKU: 6 puntos, 3 días, 36 claves de demanda solicitada. Ninguna fila futura se importa como consumo observado inicial.
- [x] Implementar generador con parámetros en `scenario.json`: seis regiones para ≈80 puntos (≤15 por región), bodega y flota por región, histórico previo y matriz de origen declarado. Ordenar CSV por código/fecha/SKU, codificar UTF-8 y cantidades en unidad base. `mapping.json` usa el perfil de 001; no escribir puntos, nombres ni coordenadas en C#.
- [x] Prueba roja `ExogenousCalendarTests`: para una media de 10 L/día, un laborable sin ruido ni pico pide 10 L y un fin de semana 8 L; ruido con seed permanece en ±15 % y las mismas claves reciben picos etiquetados. `VisibleAt(t)` no devuelve demanda ni incidente con `KnownAtUtc > t`; una política que consulta día siguiente recibe cero eventos futuros.
- [x] Implementar PRNG con seed derivada de `(datasetVersion, seed)` y **una única generación antes de las políticas**; ordenar eventos por tiempo UTC/región/punto/SKU, calcular hash sobre serialización canónica. El adverso deriva del calendario base con demanda ×1,20, `leadTimeDays +1` y vehículo retirado declarado en manifest, igual para las tres políticas.
- [x] Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ScenarioGeneratorTests|FullyQualifiedName~ExogenousCalendarTests"`; esperado `Failed: 0`. Commit `feat(simulation): generar demanda común y archivos canónicos` con trailer.

### T3 — Importar en base separada y crear una migración

**Archivos:** crear `src/Infrastructure/Simulation/SimulationDatabase.cs`, `src/Application/Simulation/ScenarioImporter.cs`; modificar `src/Infrastructure/Persistence/ApplicationDbContext.cs`, crear **una** entrada en `src/Infrastructure/Persistence/Migrations/`; pruebas `tests/IstpetDev.Tests/Integration/Simulation/SimulationIsolationTests.cs`.

- [ ] Prueba roja: `ConnectionStrings:Simulation` igual a la conexión operativa devuelve error `SIMULATION_DATABASE_NOT_ISOLATED` antes de crear el run. Con conexiones distintas, importar `demo-region` en operativa, tomar huella de `StockBalance`/`Delivery`/`AgentDecision`, importar N=6 en simulación vía `IScenarioImporter`, repetir/fallar y comparar huella operativa idéntica.
- [ ] Implementar `ScenarioImporter.ImportInitialStateAsync` como adaptador del importador/validador **de 001**, apuntando al `ApplicationDbContext` de simulación; `road_segments` usa la extensión de 002. No insertar canónicos con SQL privado ni llamar el endpoint HTTP desde el worker. Reiniciar el estado de trabajo de cada política por importación, conservando runs/exportaciones terminados en la BD principal.
- [ ] Prueba roja de migración: `SimulationRun` guarda `comparisonId`, seed, escenario, política/versión, hashes, estado, progreso y causa; `SimulationEvent` tiene orden único por run/secuencia; no duplica `SimulationMetric` de 001. La migración termina con `migrationBuilder.Sql(DemoGrants.Sql)`. La prueba de tablas de 001 sigue exigiendo núcleo sin Schema95, sin conteo final fijo.
- [ ] Crear **una** migración sobre `develop` actualizado y coordinar su turno con 003/004. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~SimulationIsolationTests|FullyQualifiedName~SchemaTests"`; esperado `Failed: 0`. Commit `feat(db): aislar corridas y registrar eventos simulados` con trailer.

### T4 — Baselines congelados

**Archivos:** crear `src/Application/Simulation/{PolicyRunner,ReactivePolicy,FixedCalendarPolicy}.cs`; pruebas `tests/IstpetDev.Tests/Unit/Simulation/{ReactivePolicyTests,FixedCalendarPolicyTests,PolicyParityTests}.cs`.

- [x] Prueba roja reactiva: con media histórica 10 L/día, lead time 2 días y saldo 19 L hay aviso; con 20 L exactos no hay aviso hasta que el saldo baje (límite explícito). Si saldo 19 L, en tránsito 1 L y meta 14 días, solicita `max(0,140−19−1)=120 L` antes de límites de bodega/capacidad. Sin historial usa consumo base del dataset y registra supuesto. Orden de avisos: hora, criticidad y código.
- [x] Implementar `reactive-v1` con observaciones disponibles al instante; el pedido no salta lead time ni ventanas, y falta de stock queda pendiente. Secuenciar las paradas del día con el **mismo planificador de 002** (inserción + 2-opt, mismas restricciones y costo) y el mismo servicio de distancia, sin llamar a la política IstpetDev (FR-09).
- [x] Prueba roja fija: en una región de 10 puntos ordenados por código, el punto de índice `i` recibe el día laborable `i mod 5` y se visita cada 7 días en ese día (cada punto una vez por semana; 2 puntos por día laborable); media previa 10 L/día fija 70 L por punto/SKU; un pico o un saldo distinto no cambia 70 L ni adelanta la visita. Stock/capacidad insuficiente registra pendiente, sin viaje imposible. La secuencia del día la arma el planificador de 002 y es idéntica a la que obtendría IstpetDev para las mismas paradas (prueba de paridad de ruteo, CE-10).
- [x] Implementar `fixed7-v1`, `PolicyRunner` y copia profunda del estado inicial por política. Prueba `PolicyParityTests`: los tres runners reciben el mismo `ScenarioCalendar.Sha256`, matriz, stock, flota, ventana y costo; nunca comparten estado mutable.
- [x] Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ReactivePolicyTests|FullyQualifiedName~FixedCalendarPolicyTests|FullyQualifiedName~PolicyParityTests"`; esperado `Failed: 0`. Commit `feat(simulation): fijar baselines reactivo y semanal` con trailer.

### T5 — Política IstpetDev y ciclo regional por hora

**Archivos:** crear `src/Application/Simulation/{IstpetDevPolicy,SimulationClock,DailySimulationEngine}.cs`, `src/Infrastructure/Simulation/LogisticsPlannerAdapter.cs`; pruebas `tests/IstpetDev.Tests/Unit/Simulation/{DailySimulationEngineTests,IstpetDevPolicyTests}.cs`.

- [ ] Prueba roja: demanda de 5 L a las 10:00, entrega a las 12:00 y stock inicial 0 dejan quiebre a las 10:00 aunque luego se entreguen 5 L; entrega a las 09:00 evita ese quiebre. Un incidente ocurrido a las 08:00 pero conocido a las 11:00 no afecta la decisión de las 09:00.
- [ ] Implementar reloj fijo de simulación en `America/Guayaquil`: 06:00 observación/avisos, 06:05 riesgo/plan/reserva, ETA real para entrega y demanda a su hora exógena, conciliación al cierre. Cada evento tiene secuencia determinista. Una región/día usa una bodega y vehículos locales; seis regiones de ≤15 puntos no comparten stock ni rutas y sus métricas se suman después.
- [ ] Prueba roja `IstpetDevPolicyTests`: el adaptador llama exactamente una vez por región/día con pedidos a riesgo, cantidad, planificador y costo **de 002**, y devuelve no atendidos; una matriz incompleta con `Road` falla el run, mientras modo `Approximation` usa todas las aristas aproximadas y etiqueta supuestos. Ninguna llamada HTTP interna, LLM ni n8n.
- [ ] Implementar `LogisticsPlannerAdapter` sobre servicios de aplicación de 002 y pasar reloj/contexto de simulación; las conversiones de respuesta quedan en el adaptador. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~DailySimulationEngineTests|FullyQualifiedName~IstpetDevPolicyTests"`; esperado `Failed: 0`. Commit `feat(simulation): reutilizar motor logístico por región y hora` con trailer.

### T6 — Invariantes y métricas comparables

**Archivos:** crear `src/Application/Simulation/{SimulationInvariantValidator,MetricCalculator,ComparisonAggregator}.cs`; pruebas `tests/IstpetDev.Tests/Unit/Simulation/{SimulationInvariantTests,MetricCalculatorTests,ComparisonAggregatorTests}.cs`.

- [ ] Prueba roja de invariantes: al cierre por lote/ubicación `inicial + entradas − salidas = final`; demanda solicitada = satisfecha + no atendida; capacidad, jornada y ventana de cada viaje son válidas. Inyectar salida adicional de 1 L: run `Failed`, evento de causa, sin KPI publicado para esa seed.
- [ ] Implementar validación diaria antes de persistir métricas. No reducir `Requested` al consumo observado cuando falta stock; faltante conserva SKU/unidad base.
- [ ] Prueba roja de métricas: baseline quiebra claves A y B, plataforma quiebra B y C → evitados=1 (A), nuevos=1 (C), diferencia neta=0. `km=100`, `cost=200`, entregas=4 → 25 km/entrega y 50 costo/entrega. Puntualidad 3/4 = 75 %. Baseline km=100 y plataforma=80 → ahorro 20 km, reducción 20 %. Baseline=0 → `Value=null`, razón `zero_denominator`. L y kg no se suman en una demanda no atendida total.
- [ ] Implementar `MetricCalculator` con `MetricValue`, definición versionada, denominador explícito y redondeo documentado a 3 decimales. Trazabilidad simulada sin custodia → N/A `not_simulated`; la operativa viene de 004.
- [ ] Prueba roja de agregación: tres seeds con ahorro 10, 20, 30 muestran mínimo 10, mediana 20, máximo 30 por baseline; el adverso aparece separado y no entra en ese rango. Implementar `ComparisonAggregator`.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~SimulationInvariantTests|FullyQualifiedName~MetricCalculatorTests|FullyQualifiedName~ComparisonAggregatorTests"`; esperado `Failed: 0`. Commit `feat(simulation): calcular indicadores auditables` con trailer.

### T7 — Trabajo asíncrono, HTTP y exportación

**Archivos:** crear `src/Infrastructure/Simulation/SimulationWorker.cs`, `src/Application/Simulation/ExportBuilder.cs`, `src/Api/Controllers/SimulationsController.cs`; modificar DI de `src/Api/Program.cs`/`src/Infrastructure/DependencyInjection.cs`; pruebas `tests/IstpetDev.Tests/Integration/Simulation/{SimulationWorkerTests,SimulationApiTests,SimulationExportTests}.cs`.

- [ ] Prueba roja HTTP: Gerencia/Planificación `POST /api/v1/simulations/comparisons` válido → 202 con `comparisonId,status=Queued`, sin esperar cálculo; `GET` lista y detalle ordenados; Conductor/Automatización → 403; sin JWT → 401. Dataset sin matriz homogénea → 422 con causa. Misma base operativa/simulación → 422 `SIMULATION_DATABASE_NOT_ISOLATED`.
- [ ] Implementar claim transaccional de **una** comparación activa por instancia y doce `SimulationRun` (3 seeds base +1 adversa ×3 políticas); progreso por run/seed/escenario/política en BD principal. Al reiniciar, run activo pasa `Interrupted`; reintento crea intento nuevo desde estado inicial importado, sin agregar métricas/eventos parciales al run completado. Fallo conserva causa; no publicar rango si una política de la seed falla.
- [ ] Prueba roja export: terminado → ZIP con `manifest.json`, `metrics.json`, `resumen.csv`, `eventos.csv`, calendario; los SHA-256 de archivos de datos coinciden con manifest y `ETag` es SHA-256 del manifest. En progreso → 409; 401/403 según rol. Dos exportaciones del mismo run tienen contenido lógico/hashes idénticos, sin timestamp variable en datos canónicos.
- [ ] Implementar serialización canónica, CSV con escape correcto, metadatos seed/dataset/políticas/matriz/versión de código/fórmulas/supuestos y enlace a cada `SimulationRun`. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~SimulationWorkerTests|FullyQualifiedName~SimulationApiTests|FullyQualifiedName~SimulationExportTests"`; esperado `Failed: 0`. Commit `feat(api): ejecutar y exportar comparaciones asíncronas` con trailer.
- [ ] Prueba roja de importación (FR-21): `POST /api/v1/simulations/comparisons/import` con el ZIP de una corrida acepta y guarda runs, métricas y eventos con origen `imported`, host y commit; el mismo ZIP con un byte cambiado en `metrics.json` da 422 sin escribir nada; importar dos veces el mismo `comparisonId` es idempotente. Implementar con la verificación de hashes de `ExportBuilder`. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~SimulationImportTests"`; esperado `Failed: 0`. Commit `feat(api): importar comparaciones verificadas` con trailer.

### T8 — Medir la corrida nacional completa

**Archivos:** crear `scripts/simulation/benchmark.sh`, `tests/IstpetDev.Tests/Performance/SimulationNationalTests.cs`; no modificar políticas para obtener un porcentaje favorable.

- [ ] El script toma `API_BASE`, `ACCESS_TOKEN`, `DATASET_VERSION` y rutas de dos bases de ensayo por variables de entorno (sin imprimir secretos); valida que los nombres de base difieren. Lanza 3 seeds base y 1 adversa por `POST /comparisons`, consulta estado cada 10 s y corta si excede el plazo de ensayo configurado; guarda `comparisonId`, manifest, tiempos total, por región/política, número de llamadas y p50/p95 por llamada en una carpeta privada de evidencia.
- [ ] Prueba roja de performance: con dataset nacional de seis regiones/≈80 puntos y matriz precargada, la corrida termina con 12 runs completos, 1080 jornadas de política y conteo real de planificaciones; red externa desactivada. Si `elapsed > 60 min`, la prueba falla y guarda perfil de tiempo en importación, cálculo, planificador, persistencia y exportación.
- [ ] Optimizar **solo el cuello medido** (por ejemplo, importaciones repetidas, consultas N+1 o planificar región sin pedido), repetir el benchmark completo y comparar resultados/hashes antes/después para comprobar que la optimización no cambió semántica.
- [ ] Ejecutar `bash scripts/simulation/benchmark.sh` en la **máquina dedicada** de la corrida oficial (FR-21; nunca en la instancia que sirve la demo) y registrar su tipo de CPU y memoria; después importar el ZIP en el entorno de demo con `POST /simulations/comparisons/import`. Ejecutar también `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~SimulationNationalTests"`; registrar tiempos reales, commit, CPU y matriz. No afirmar CE-07 con el límite de 002 por llamada. Commit `perf(simulation): medir corrida nacional completa` con trailer.

### T9 — Panel en vivo y mapa sin cifras fijas

**Archivos:** modificar `apps/web/src/pages/operations-dashboard/operations-dashboard.page.ts`, `apps/web/src/widgets/route-map/route-map.widget.ts`, `apps/web/src/shared/ui/map-view/map-view.component.ts`, `apps/web/src/app/app.routes.ts`, cliente `apps/web/src/shared/api/api.service.ts`; crear modelos de ruta/decisión/entrega bajo `apps/web/src/entities/` solo si se usan. Pruebas de componente bajo `apps/web/src/pages/operations-dashboard/` o E2E web según runner verificado.

- [ ] Prueba roja con API vacía: no aparecen «418 km», «Ahorro: 24.8%», coordenadas, hospital, vehículo, telemetría ni estado de entrega del fixture. Se muestra «Sin datos del recorrido» y hora de última actualización. Otro fixture cambia puntos y cifras sin recompilar.
- [ ] Reemplazar `defaultPoints`, `deliveryStops`, `routeMode` ficticio, porcentajes y relatos de incidente en el widget por `GET /risks`, lista/detalle de rutas, entregas y decisiones de 002/004. Correlacionar `routePlanId/revision`, `deliveryId`, `pointCode/Sku`; no sumar valores de revisiones distintas.
- [ ] Prueba roja de mapa: `source=Road` con polilínea recibida → línea continua; `Approximation` → punteada y leyenda; centro/zoom por `fitBounds`, iconos locales. Bloquear mosaicos/OSM en ensayo: lista ordenada de paradas, ETA, costo y estado siguen visibles.
- [ ] Prueba roja de acciones: `AutoExecuted` a izquierda y `Escalated` a derecha; aprobar opción factible, rechazar y revertir envían exactamente los endpoints de 002, actualizan solo con respuesta confirmada; 403/409 dejan error visible y snapshot consistente. Modo `ProposeOnly` revierte interruptor si PUT falla. `watch` usa POST de 003; incidente POST de 002; `advance` usa 004, nunca `/stops/{sequence}/complete`.
- [ ] Reutilizar guard PKCE/interceptor de [013](../013-login-y-cuentas/spec.md) (`authGuard`, `roleGuard`, `apiAuthInterceptor`; botones de escritura dentro de `session.canWrite()`, el Auditor solo lee). Consulta cada 5 s mientras pestaña visible; al ocultarse para; al error conserva snapshot **etiquetado como desactualizado**. Verificar 1280 px y 390 px con teclado/foco y etiquetas, no solo color. Ejecutar `npm run lint:web` y `npm run build:web`; esperado 0 errores. Commit `feat(web): conectar panel operativo y mapa a la API` con trailer.

### T10 — Vista comparativa de simulación

**Archivos:** crear `apps/web/src/pages/simulation/simulation.page.ts`, `apps/web/src/entities/simulation/model/simulation.model.ts`, `apps/web/src/entities/simulation/index.ts`, `apps/web/src/shared/api/simulations.api.ts`; modificar `apps/web/src/app/app.routes.ts`.

- [ ] Prueba roja de ruta `/simulation` sin sesión → `authGuard` de 013; con sesión muestra `Queued/Running/Completed/Failed`, progreso de doce runs, seed, dataset/matriz/policyVersion, supuestos y etiqueta «Datos sintéticos». No hay números de ahorro antes de `Completed`.
- [ ] Implementar DTOs del endpoint 005 y cliente de consulta/descarga. Cuatro tarjetas: quiebres evitados **y nuevos**, demanda no atendida por SKU/unidad, costo transporte y puntualidad; selector de baseline; rango min–mediana–max de tres seeds base; adverso aparte. Detalle muestra numerador/denominador y N/A con razón; km, servicio y trazabilidad en detalle. Nunca sumar L con kg.
- [ ] Prueba roja de integridad visible: `comparisonId` de pantalla coincide con ZIP descargado; si export está en 409, no habilitar descarga; si falla descarga, mantener resultado en pantalla con error. Panel a 1280/390 conserva unidades y comparador legibles. Ejecutar `npm run lint:web` y `npm run build:web`; esperado 0 errores. Commit `feat(web): mostrar comparación reproducible de 90 días` con trailer.

### T11 — Ensayar el recorrido y registrar evidencia

**Archivos:** `docs/specs/005-panel-simulador/test-e2e.md` (solo registro de ejecuciones), `docs/boveda/99 Fuentes/Evidencias/` (crear al ejecutar), notas de resultado del equipo.

- [ ] Ejecutar E2E-01 a E2E-10 de [test-e2e.md](test-e2e.md) con el commit de implementación; anotar fecha, entorno, operador, resultado, IDs de comparación/runs y evidencia. Los escenarios obligatorios fallidos o bloqueados dejan el spec abierto.
- [ ] Repetir la misma seed/versión y comparar eventos/métricas; otra seed debe cambiar el calendario. Verificar hashes de ZIP y que reset/`advance` no cambian el ledger de simulación. Registrar cifras solo como **simuladas y medidas en esa corrida**, no como beneficio empresarial observado.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests`, `npm run check` y `python docs/boveda/.github/scripts/check_vault.py`; anotar salida completa. Revisar `rg -n '418 km|24\.8%|defaultPoints|deliveryStops|Alóag Cerrada' apps/web/src` y explicar cualquier coincidencia que no sea fixture/prueba.
- [ ] Completar los doce runs y benchmark antes de H4; si algo falla, priorizar simulador sobre pulido del agente. Solo después de evidencia y aprobación de Deciders cambiar estado del spec a `cerrado`. Commit `test(e2e): registrar recorrido y métricas del simulador` con trailer.
