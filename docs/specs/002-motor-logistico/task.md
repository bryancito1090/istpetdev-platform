# 002 — Motor logístico determinista: tareas

> Pasos ejecutables de [spec.md](spec.md) y [plan.md](plan.md). Se implementan **después** de integrar 001: esta rama todavía tiene Schema95 y no contiene `src/Application/Imports` ni `tests/IstpetDev.Tests`. Cada tarea se cierra con prueba roja, cambio mínimo, prueba verde y commit; los comandos se ejecutan desde la raíz. No marcar como terminada ninguna tarea solo por crear este documento.

## Resumen

| ID | Entrega comprobable | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T0 | Base de 001 y contrato de 002 confirmados | 001 | — | pendiente |
| T1 | Configuración, reloj y facts | T0 | — | pendiente |
| T2 | Riesgos, snapshots y asignación de bodega | T1 | [P] con T3 | pendiente |
| T3 | Importación y caché de tramos, OSRM y aproximación | T1 | [P] con T2 | pendiente |
| T4 | Planificador de rutas y costos | T2, T3 | — | pendiente |
| T5 | Publicación FEFO, reservas y paradas completadas | T4 | [P] con T6 | pendiente |
| T6 | Política de autonomía y explicaciones por facts | T4 | [P] con T5 | pendiente |
| T7 | Incidentes, revisiones, opciones y decisiones | T5, T6 | — | pendiente |
| T8 | API, autorización e inyección de dependencias | T7 | — | pendiente |
| T9 | Fixture regional y calibración del incidente | T8 | — | pendiente |
| T10 | Recorrido E2E, rendimiento y registro en bóveda | T9 | — | pendiente |

`[P]` implica archivos distintos y commits independientes. Las firmas entre tareas son las de [Componentes e interfaces](plan.md#componentes-e-interfaces); cambiar una exige actualizar consumidores y pruebas en el mismo commit. No poner códigos, nombres o coordenadas de `demo-region` en C#.

## Secuencia común por tarea

1. Escribir primero la prueba indicada en la tarea. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~<clase>"`: debe fallar por el comportamiento faltante; un error de compilación por clase inexistente también es rojo válido.
2. Implementar el comportamiento y volver a ejecutar el filtro: salida con `Failed: 0`. Ejecutar `dotnet build IstpetDev.sln` antes del commit: salida `Build succeeded`.
3. Ejecutar al final de cada tarea las pruebas de sus dependencias directas; registrar el comando y resultado en el PR. Hacer commit con los archivos de esa tarea. Las pruebas de integración necesitan Docker/Testcontainers según 001.

### T0 — Preparar la base

**Archivos:** `docs/specs/002-motor-logistico/spec.md`, `docs/specs/002-motor-logistico/plan.md` (lectura); esquema de 001 (lectura).

- [ ] Confirmar que 001 está integrado: existen `src/Application/Imports/CanonicalWriter.cs`, `src/Infrastructure/Persistence/LeanSchema.cs`, `tests/IstpetDev.Tests/IstpetDev.Tests.csproj`, el fixture `demo-base` y `POST /api/v1/demo/reset`. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "Category=E2E"`; esperado: 0 fallos.
- [ ] Registrar quién aprueba el spec y resolver las decisiones aún `PENDIENTE` en el frontmatter antes de iniciar cambios de dominio; no elevar el estado a `planificado` por la mera presencia de tareas.
- [ ] Confirmar en el código integrado los nombres y propiedades de `RiskSnapshot`, `RoutePlan`, `RouteStop`, `Incident`, `Delivery`, `DeliveryLine`, `StockBalance`, `AgentDecision` y `DemoResetService`. Anotar diferencias frente a [plan.md](plan.md) en el PR antes de tocar persistencia.
- [ ] Crear la rama de implementación desde el `develop` que contiene 001 y ejecutar `dotnet build IstpetDev.sln`; esperado: 0 errores. Este paso no publica ni modifica datos remotos.

### T1 — Configuración, reloj operativo y facts

**Archivos:** crear `src/Application/Logistics/LogisticsOptions.cs`, `OperationalClock.cs`, `Facts.cs`; modificar `src/Api/appsettings.json`, `src/Api/appsettings.Development.json`, `src/Application/DependencyInjection.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/OperationalClockTests.cs` y `FactsTests.cs`.

- [ ] Prueba roja `OperationalClockTests`: con consumos de periodo `[2025-09-01, 2025-09-08)` el modo `Dataset` devuelve `2025-09-08`; `Fixed` devuelve la fecha configurada; `Real` usa fecha local de la zona; sin consumos, `Dataset` cae a `Fixed` o `Real`. Para `2026-10-12 07:00` en `America/Guayaquil`, `WorkdayStartUtcAsync` devuelve `2026-10-12T12:00:00Z`.
- [ ] Implementar `ClockMode`, `IOperationalClock`, opciones tipadas `Logistics:*` y `Agent:Policy:*`, validación de valores positivos, y `Fact(string Code, string Subject, decimal Value, string Unit)` según el plan. Inyectar `TimeProvider` para probar `Real` sin depender del reloj de CI; usar `TimeZoneInfo` y emitir `timezone_fallback` cuando aplique.
- [ ] Prueba roja y verde `FactsTests`: orden estable por `(Code, Subject, Unit)`, formato invariante en JSON y sin cifras generadas fuera de la lista. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~OperationalClockTests|FullyQualifiedName~FactsTests"`; esperado: 0 fallos. Commit `feat(logistics): add operational clock and facts`.

### T2 — Riesgos, cantidades y asignación

**Archivos:** crear `src/Application/Logistics/RiskCalculator.cs`, `ReplenishmentAllocator.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/RiskCalculatorTests.cs`, `ReplenishmentAllocatorTests.cs` y `tests/IstpetDev.Tests/Integration/Logistics/RiskSnapshotTests.cs`.

- [ ] Pruebas rojas: consumo prorrateado al recortar una ventana de 14 días; disponible = existencia menos reserva de lotes válidos; en camino = despachado menos aceptado; cobertura 5,0 con lead time 2 + margen 3 da `Alto`, límites exactos de `Critico` y `Vigilar`; consumo ausente/cero o stock más antiguo que `StaleStockDays` da `SinDatos` con cobertura `null`.
- [ ] Implementar `IRiskCalculator.CalculateAsync(bool persistSnapshots, CancellationToken)` y el cálculo de `SuggestedQuantity` en unidad base, redondeado hacia arriba a 3 decimales. Respetar lead time por punto antes del global. `persistSnapshots=true` guarda entradas, versión de fórmula y reloj en `RiskSnapshot`; `false` no escribe. La prueba de integración consulta la fila persistida.
- [ ] Pruebas rojas de `ReplenishmentAllocator`: 2 obligatorios del mismo SKU compiten por bodega; gana nivel/prioridad/código, la suma asignada no supera stock menos seguridad y el resto recibe `warehouse_stock`; consolidación usa solo remanente. No tomar lotes bloqueados o vencidos.
- [ ] Implementar asignación FEFO estable por vencimiento y código de lote, con cantidad no negativa. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RiskCalculatorTests|FullyQualifiedName~ReplenishmentAllocatorTests|FullyQualifiedName~RiskSnapshotTests"`; esperado: 0 fallos. Commit `feat(logistics): calculate risk and allocate replenishment`.

### T3 — Tramos viales e importador

**Archivos:** modificar `src/Domain/Entities/LogisticsEntities.cs`, `src/Infrastructure/Persistence/LeanSchema.cs`, `src/Infrastructure/Persistence/ApplicationDbContext.cs`, `src/Application/Common/Interfaces/IApplicationDbContext.cs`, `src/Application/Imports/ImportEntity.cs`, `CanonicalSchema.cs`, `CanonicalWriter.cs`, `ImportContext.cs`; crear `src/Application/Logistics/Distances/GeoPoint.cs`, `Leg.cs`, `DistanceProvider.cs`, `Polyline.cs`, `src/Infrastructure/Logistics/OsrmDistanceProvider.cs` y una migración `*_RoadMatrixCache.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/DistanceTests.cs`, `tests/IstpetDev.Tests/Integration/Logistics/RoadSegmentsImportTests.cs`, `DistanceProviderTests.cs`.

- [ ] Pruebas rojas de importación: `road_segments` es la octava entidad, admite alias `tramos`, resuelve `from_code`/`to_code` de bodega o punto, exige `*_kind` ante ambigüedad, valida km/minutos positivos y hace upsert dirigido por coordenadas redondeadas a 5 decimales. Reimportar no crea duplicados.
- [ ] Crear `RoadMatrixEdge` e índice único `(OrganizationId, FromLat, FromLon, ToLat, ToLon)` y `RouteStockReservation` con vínculo a revisión/entrega/línea/lote/saldo, cantidad, estado e índices del plan; generar **una** migración sobre el esquema de 001, con `DemoGrants.Sql` al final. Excluir solo la caché de `DemoResetService`; borrar reservas con las entregas. Probar que reset + reimportación conservan las aristas y su origen `road`.
- [ ] Pruebas rojas de cadena: arista guardada evita HTTP; par faltante usa OSRM simulado, persiste distancia y tiempo, y usa trazado `polyline` cuando se obtiene; OSRM caído o sin URL produce haversine × 1,3 a 40 km/h con `source=approximation`; A→B no implica B→A. Decodificar una polilínea conocida y comprobar extremos.
- [ ] Implementar proveedores por lote, `HttpClient` con timeout de 3 s y fallback explícito sin guardar aproximaciones. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~DistanceTests|FullyQualifiedName~RoadSegmentsImportTests|FullyQualifiedName~DistanceProviderTests"`; esperado: 0 fallos. Commit `feat(logistics): import and resolve road distances`.

### T4 — Planificador determinista

**Archivos:** crear `src/Application/Logistics/Planning/PlanModels.cs`, `RoutePlanner.cs`, `RouteFeasibility.cs`, `RouteImprover.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/RoutePlannerTests.cs`, `RouteQualityTests.cs` y `tests/IstpetDev.Tests/Integration/Logistics/RoutePlanningTests.cs`.

- [ ] Pruebas rojas de factibilidad: dos vehículos activos; obligatorios `Critico`/`Alto` antes de `Vigilar`; inserción con espera antes de ventana, tiempo de servicio, regreso al depósito dentro de jornada, topes independientes de kg y m³, costo = km × costo/km + horas × `CostPerHour`.
- [ ] Probar razones exactas `capacity`, `time_window`, `workday`, `warehouse_stock`, `unreachable` (incluido punto sin coordenadas). Factor de peso o volumen 0 no divide por cero ni rechaza por esa dimensión y añade fact `capacity_dimension_not_evaluated`.
- [ ] Implementar inserción de menor costo con desempate por código de vehículo y posición, 2-opt intrarruta con factibilidad, consolidación `Vigilar` con `MaxDetourMinutes`, legs y `HasApproximations`. `PlanAsync` recibe `PlanRequest`; `ReplanAsync` reserva el prefijo fijo para T7. Todas las colecciones expuestas se ordenan antes de serializar; IDs aleatorios no forman parte de la comparación de rutas.
- [ ] Probar dos cálculos de la **misma base y reloj**: comparar JSON de resultados canónicos tras excluir IDs de persistencia; prueba de integración compara respuestas HTTP completas una vez que T8 asigne IDs deterministas o estabilice la representación. En instancias de ≤ 8 puntos con semilla fija, costo ≤ 110% del óptimo por fuerza bruta. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RoutePlannerTests|FullyQualifiedName~RouteQualityTests|FullyQualifiedName~RoutePlanningTests"`; esperado: 0 fallos. Commit `feat(logistics): plan deterministic feasible routes`.

### T5 — Publicación, FEFO y reservas

**Archivos:** crear `src/Application/Logistics/RoutePublisher.cs`; modificar mapeos de `RoutePlan`, `RouteStop`, `Delivery`, `DeliveryLine`, `StockBalance` solo si 001 lo requiere; probar en `tests/IstpetDev.Tests/Integration/Logistics/RoutePublisherTests.cs`.

- [ ] Prueba roja: publicar una propuesta crea una `Delivery` por parada y líneas con lotes FEFO disponibles al día operativo; por cada línea/lote crea `RouteStockReservation.Active` ligada a la revisión y suma exactamente esa cantidad en `StockBalance.QuantityReserved`, conserva seguridad y no toca saldo físico. Repetir publicación devuelve las mismas entregas e igual reserva; dos solicitudes concurrentes tampoco duplican.
- [ ] Implementar transacción con restricción única/idempotencia, elegir lotes por vencimiento y código, y devolver conflicto explicable si cambió el stock entre propuesta y publicación. No aceptar un lote bloqueado ni vencido. Probar invariante `QuantityReserved = Σ reservas Active` por saldo.
- [ ] Prueba roja y verde de completar parada: una secuencia se marca `Completed` una sola vez; hora real provista en el cuerpo o, en modo demo, de la hora planificada; completar dos veces no retrocede el estado. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RoutePublisherTests"`; esperado: 0 fallos. Commit `feat(logistics): publish FEFO deliveries idempotently`.

### T6 — Política y explicaciones

**Archivos:** crear `src/Application/Logistics/Policy/AutonomyPolicy.cs`, `AutonomyState.cs`, `src/Application/Logistics/ExplanationTemplates.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/AutonomyPolicyTests.cs`, `ExplanationTemplatesTests.cs`.

- [ ] Prueba roja tabular: `AutoWithLimits` solo ejecuta si conserva obligatorios y ventanas, costo sube ≤ 15% y ningún ETA se retrasa > 60 min; los límites exactos se aceptan. Cada incumplimiento produce motivo y fact; `ProposeOnly` siempre escala. Plan inicial exige aprobación si `AutoPublishInitialPlans=false`.
- [ ] Implementar `IAutonomyPolicy.Classify` sin recibir texto libre, y `AutonomyState` en memoria con lectura/escritura de modo; reinicio vuelve al valor configurado. Prueba de reinicio y de concurrencia de lectura/escritura.
- [ ] Pruebas rojas de cuatro plantillas (`risk`, `plan`, `replan`, `escalation`): toda cifra renderizada coincide con un `Fact` por sujeto y unidad; si falta fact, la plantilla no inventa valor. Implementar `IExplanationTemplates` en español y formateo estable. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AutonomyPolicyTests|FullyQualifiedName~ExplanationTemplatesTests"`; esperado: 0 fallos. Commit `feat(logistics): classify autonomy and explain facts`.

### T7 — Incidentes, revisiones y opciones

**Archivos:** crear `src/Application/Logistics/Incidents/IncidentEvaluator.cs`, `Replanner.cs`, `src/Application/Logistics/Policy/EscalationOptions.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/IncidentEvaluatorTests.cs`, `tests/IstpetDev.Tests/Integration/Logistics/ReplanningTests.cs`.

- [ ] Pruebas rojas: `ROAD_CLOSURE` bloquea solo legs cuyo trazado cruza el círculo; `DELAY` multiplica minutos de legs afectados; `VEHICLE_BREAKDOWN` deja indisponible ese vehículo desde el incidente. Un trazado aproximado usa su segmento recto. La respuesta enumera rutas y paradas afectadas.
- [ ] Implementar `IncidentEvaluator` con distancia punto–segmento local y `IncidentEffect`, sin modificar una ruta ya publicada. Añadir prueba con un tramo apenas fuera del radio y otra con extremos fuera pero trazado dentro.
- [ ] Pruebas rojas de replan: completadas idénticas y en el mismo orden, comienzo en última parada completada, solo pendientes se reordenan, revisión `n+1` apunta a `SupersedesRoutePlanId=n`, diff incluye cambios de ETA, costo, movidas y no atendidas. Avería: otro vehículo vuelve al depósito, recarga y recibe pendientes factibles; resto `workday` o capacidad.
- [ ] Implementar `ReplanAsync(Strict)`, `LateService` (ventana blanda solo de obligatorios) y `PrioritizeCritical` (sin consolidación, ventana dura); opción inviable declara `feasible=false` y razón. Guardar `AgentDecision` por vehículo con origen y estado; autoejecución publica, escalado conserva la revisión vigente; aprobar opción, rechazar y revertir son idempotentes. Revertir bloquea reservas/saldos, cancela solo entregas pendientes no despachadas, libera sus reservas `Active`, revalida stock, reserva para la revisión restaurada y publica una **tercera** revisión en un commit. `Consumed` y completadas quedan históricas; entrega pendiente ya despachada o stock insuficiente → 409 sin efectos. Probar replay de reversión y el invariante de reservas por saldo.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~IncidentEvaluatorTests|FullyQualifiedName~ReplanningTests"`; esperado: 0 fallos. Commit `feat(logistics): replan incidents with auditable decisions`.

### T8 — Endpoints y roles

**Archivos:** crear `src/Api/Controllers/RisksController.cs`, `RoutePlansController.cs`, `IncidentsController.cs`, `AgentDecisionsController.cs`, `AutonomyController.cs`; modificar `src/Api/Controllers/DemoController.cs`, `src/Api/Program.cs`, `src/Infrastructure/DependencyInjection.cs`; probar en `tests/IstpetDev.Tests/Integration/Logistics/LogisticsApiTests.cs`.

- [ ] Pruebas HTTP rojas de todos los endpoints de [Entidades y contratos](spec.md#entidades-y-contratos): métodos/códigos, JSON camelCase, `facts`, Problem Details, filtro de riesgos, consulta de revisión y decisiones, publicar/completar/revertir, registrar/replanificar, aprobar/rechazar, GET/PUT del modo, preset de demo.
- [ ] Probar roles: lectura para cualquier cuenta autorizada; planificación y automatización crean planes/incidentes y replanifican; conductor solo completa; gerencia cambia modo; `POST /demo/incidents` devuelve 404 o 403 si `Demo:Enabled=false` y no ejecuta incidente. Los endpoints de escritura rechazan token ausente/rol incorrecto.
- [ ] Conectar servicios y persistencia. `POST /route-plans` responde 201 síncrono; propuesta inicial queda pendiente de aprobación por defecto. Los errores de conflictos de stock y opciones inviables tienen códigos y motivos estables. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~LogisticsApiTests"`; esperado: 0 fallos. Commit `feat(api): expose deterministic logistics endpoints`.

### T9 — Fixture regional y calibración

**Archivos:** crear `data/fixtures/demo-region/{bodegas,puntos,productos,vehiculos,lotes,inventario,consumos,tramos}.csv`, `mapping.json`, `incidentes.json`, `scripts/data/build-road-segments.py`, `scripts/data/calibrate-incident.sh`; modificar el selector de fixture del reset de 001; probar en `tests/IstpetDev.Tests/Integration/Logistics/DemoRegionTests.cs`.

- [ ] Prueba roja: reset de `demo-region` importa por el importador canónico 1 bodega, 15 puntos, 2 vehículos, 3 SKU, consumos y tramos dirigidos; reloj `Dataset=2026-10-12`; 1 `Critico`, 4 `Alto`, 3 `Vigilar`, 1 `SinDatos`, 6 `Normal`; `HOS-N01/DES-01` tiene 5,0 días.
- [ ] Crear los 7 CSV de 001 más `tramos.csv`, perfil e incidentes del plan. El generador consulta OSRM a ≤ 1 petición/s para 240 pares dirigidos y falla con salida clara si no obtiene datos; el CSV versionado permite correr sin red. Verificar `source=road` para todos los legs en el recorrido de demo y 0 solicitudes HTTP externas.
- [ ] Calibrar `cierre-norte` **después** de ejecutar el planificador real mediante `scripts/data/calibrate-incident.sh`: reset, propuesta, publicación, dos paradas completadas, incidente, replan de rutas afectadas. Ajustar solo datos de `incidentes.json` o el fixture hasta obtener `VEH-02=AutoExecuted`, `VEH-01=Escalated`, con `late_service` y `prioritize_critical` calculadas. Fijar ese resultado en `DemoRegionTests`. Si 004 ya está integrado, el script completa las paradas con `POST /api/v1/demo/route-plans/{id}/advance` en vez del endpoint provisional.
- [ ] Repetir reset y recorrido; los códigos del preset siguen resolviendo con IDs nuevos y la caché conserva tramos. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~DemoRegionTests"`; esperado: 0 fallos. Commit `feat(demo): add reproducible regional scenario`.

### T10 — Recorrido E2E, rendimiento y bóveda

**Archivos:** crear `tests/IstpetDev.Tests/E2E/LogisticsJourneyTests.cs`, `tests/IstpetDev.Tests/Performance/LogisticsPerformanceTests.cs`; actualizar `docs/boveda/03 Arquitectura/Decisiones de arquitectura.md` según protocolo y `docs/specs/002-motor-logistico/test-e2e.md` con evidencias **solo tras ejecutarlo**.

- [ ] Automatizar [E2E-01 a E2E-05](test-e2e.md) como recorridos HTTP con Testcontainers: riesgos, plan, publicación/FEFO, incidente, clasificación y opciones, aprobación/rechazo/reversión, modo `ProposeOnly`, sin red y reset. Fijar las respuestas observables sin depender de GUID generados.
- [ ] Medir propuesta de 15 puntos/2 vehículos ≤ 1 s y 80 puntos/2 vehículos ≤ 5 s con datos sintéticos y semilla fija; excluir arranque, migración e importación del cronómetro. Registrar hardware/CI y tiempos. Probar determinismo byte a byte del JSON HTTP sobre una misma base y reloj.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests`, `dotnet build IstpetDev.sln` y los pasos de `test-e2e.md`; registrar hash, fecha, duración y evidencias sin tokens. Revisar los 22 FR, 6 NFR y 7 CE en la matriz de abajo; marcar gaps explícitos, no cerrar el spec con faltantes.
- [ ] Registrar las contradicciones de mezcla de distancias, respuesta síncrona y autonomía en la bóveda; proponer ADR-22 conforme al protocolo, sin presentar aprobación implícita. Commit `test(logistics): verify end-to-end demo and performance`.

## Matriz de cobertura

| Requisitos | Tarea responsable | Evidencia principal |
|---|---|---|
| FR-01 | T1 | `OperationalClockTests`, E2E-01 |
| FR-02–FR-05 | T2 | `RiskCalculatorTests`, `RiskSnapshotTests`, E2E-01 |
| FR-06–FR-07 | T3 | `DistanceProviderTests`, `RoadSegmentsImportTests`, E2E-05 |
| FR-08–FR-11 | T4 | `RoutePlannerTests`, `RouteQualityTests`, E2E-02/06 |
| FR-12 | T5 | `RoutePublisherTests`, E2E-02 |
| FR-13–FR-15 | T7 | `IncidentEvaluatorTests`, `ReplanningTests`, E2E-03/04 |
| FR-16–FR-18 | T6–T7 | `AutonomyPolicyTests`, `ReplanningTests`, E2E-03/04 |
| FR-19–FR-20 | T1, T6 | `FactsTests`, `ExplanationTemplatesTests`, E2E-01/03 |
| FR-21–FR-22 | T9, T8 | `DemoRegionTests`, `LogisticsApiTests`, E2E-01/03 |
| NFR-01, CE-02, CE-06 | T10 | `LogisticsPerformanceTests`, E2E-06 |
| NFR-02, CE-05 | T3, T9 | `DistanceProviderTests`, E2E-05 |
| NFR-03, CE-07 | T6 | `ExplanationTemplatesTests`, E2E-07 |
| NFR-04–NFR-06 | T2, T4, T6, T10 | revisión G1, pruebas de unidades y política |
| CE-01 | T9 | `DemoRegionTests`, E2E-01 |
| CE-03 | T4, T10 | JSON HTTP canónico, E2E-02 |
| CE-04 | T7, T9 | `DemoRegionTests`, E2E-03 |

**Nota de contrato:** CE-03 exige JSON idéntico byte a byte. La prueba de T4 sobre objetos no basta: T8/T10 deben estabilizar también los identificadores y campos de auditoría de la respuesta de propuestas repetidas, o excluirlos del contrato público mediante una misma representación canónica. No relajar CE-03 en la prueba E2E.
