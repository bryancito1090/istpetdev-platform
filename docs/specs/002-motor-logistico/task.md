# 002 — Motor logístico determinista: tareas

> Pasos ejecutables de [spec.md](spec.md) y [plan.md](plan.md). Se implementan **después** de integrar 001: esta rama todavía tiene Schema95 y no contiene `src/Application/Imports` ni `tests/IstpetDev.Tests`. Cada tarea se cierra con prueba roja, cambio mínimo, prueba verde y commit; los comandos se ejecutan desde la raíz. No marcar como terminada ninguna tarea solo por crear este documento.

## Resumen

| ID | Entrega comprobable | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T0 | Base de 001 y contrato de 002 confirmados | 001 | — | completada |
| T1 | Configuración, reloj y facts | T0 | — | completada |
| T2 | Riesgos, snapshots y asignación de bodega | T1 | [P] con T3 | completada |
| T3 | Importación y caché de tramos, OSRM y aproximación | T1 | [P] con T2 | completada |
| T4 | Planificador de rutas y costos | T2, T3 | — | completada |
| T5 | Publicación FEFO, reservas y paradas completadas | T4 | [P] con T6 | completada |
| T6 | Política de autonomía y explicaciones por facts | T4 | [P] con T5 | completada |
| T7 | Incidentes, revisiones, opciones y decisiones | T5, T6 | — | revalidación parcial #39; ver registro posterior |
| T8 | API, autorización e inyección de dependencias | T7 | — | completada |
| T9 | Fixture regional y calibración del incidente | T8 | — | completada |
| T10 | Recorrido E2E, rendimiento y registro en bóveda | T9 | — | completada |
| T11 | Rutas con cualquier dataset: bodega propia por vehículo y coordenadas de alta precisión | T4 | — | completada (15-oct; 444/444) |
| T12 | Rutas por calles (mapa y distancias del motor con OSRM) y un color por vehículo | T11 | — | completada (15-oct; verificada en vivo; demo-region sin cambios) |
| T13 | Las rutas publicadas llegan al conductor (asignación, entregas del asistente y bodega de origen) | T5 | — | completada (15-oct; 6 pruebas nuevas) |

`[P]` implica archivos distintos y commits independientes. Las firmas entre tareas son las de [Componentes e interfaces](plan.md#componentes-e-interfaces); cambiar una exige actualizar consumidores y pruebas en el mismo commit. No poner códigos, nombres o coordenadas de `demo-region` en C#.

## Secuencia común por tarea

1. Escribir primero la prueba indicada en la tarea. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~<clase>"`: debe fallar por el comportamiento faltante; un error de compilación por clase inexistente también es rojo válido.
2. Implementar el comportamiento y volver a ejecutar el filtro: salida con `Failed: 0`. Ejecutar `dotnet build IstpetDev.sln` antes del commit: salida `Build succeeded`.
3. Ejecutar al final de cada tarea las pruebas de sus dependencias directas; registrar el comando y resultado en el PR. Hacer commit con los archivos de esa tarea. Las pruebas de integración necesitan Docker/Testcontainers según 001.

### T0 — Preparar la base

**Archivos:** `docs/specs/002-motor-logistico/spec.md`, `docs/specs/002-motor-logistico/plan.md` (lectura); esquema de 001 (lectura).

- [x] Confirmar que 001 está integrado: existen `src/Application/Imports/CanonicalWriter.cs`, `src/Infrastructure/Persistence/LeanSchema.cs`, `tests/IstpetDev.Tests/IstpetDev.Tests.csproj`, el fixture `demo-base` y `POST /api/v1/demo/reset`.
- [x] Registrar quién aprueba el spec y resolver las decisiones aún `PENDIENTE` en el frontmatter antes de iniciar cambios de dominio; no elevar el estado a `planificado` por la mera presencia de tareas.
- [x] Confirmar en el código integrado los nombres y propiedades de `RiskSnapshot`, `RoutePlan`, `RouteStop`, `Incident`, `Delivery`, `DeliveryLine`, `StockBalance`, `AgentDecision` y `DemoResetService`. Anotar diferencias frente a [plan.md](plan.md) en el PR antes de tocar persistencia.
- [x] Crear la rama de implementación desde el `develop` que contiene 001 y ejecutar `dotnet build IstpetDev.sln`; esperado: 0 errores. Este paso no publica ni modifica datos remotos.

### T1 — Configuración, reloj operativo y facts

**Archivos:** crear `src/Application/Logistics/LogisticsOptions.cs`, `OperationalClock.cs`, `Facts.cs`; modificar `src/Api/appsettings.json`, `src/Api/appsettings.Development.json`, `src/Application/DependencyInjection.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/OperationalClockTests.cs` y `FactsTests.cs`.

- [x] Prueba roja `OperationalClockTests`: con consumos de periodo `[2025-09-01, 2025-09-08)` el modo `Dataset` devuelve `2025-09-08`; `Fixed` devuelve la fecha configurada; `Real` usa fecha local de la zona; sin consumos, `Dataset` cae a `Fixed` o `Real`. Para `2026-10-12 07:00` en `America/Guayaquil`, `WorkdayStartUtcAsync` devuelve `2026-10-12T12:00:00Z`.
- [x] Implementar `ClockMode`, `IOperationalClock`, opciones tipadas `Logistics:*` y `Agent:Policy:*`, validación de valores positivos, y `Fact(string Code, string Subject, decimal Value, string Unit)` según el plan. Inyectar `TimeProvider` para probar `Real` sin depender del reloj de CI; usar `TimeZoneInfo` y emitir `timezone_fallback` cuando aplique.
- [x] Prueba roja y verde `FactsTests`: orden estable por `(Code, Subject, Unit)`, formato invariante en JSON y sin cifras generadas fuera de la lista. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~OperationalClockTests|FullyQualifiedName~FactsTests"`; esperado: 0 fallos. Commit `feat(logistics): add operational clock and facts`.

### T2 — Riesgos, cantidades y asignación

**Archivos:** crear `src/Application/Logistics/RiskCalculator.cs`, `ReplenishmentAllocator.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/RiskCalculatorTests.cs`, `ReplenishmentAllocatorTests.cs` y `tests/IstpetDev.Tests/Integration/Logistics/RiskSnapshotTests.cs`.

- [x] Pruebas rojas: consumo prorrateado al recortar una ventana de 14 días; disponible = existencia menos reserva de lotes válidos; en camino = despachado menos aceptado; cobertura 5,0 con lead time 2 + margen 3 da `Alto`, límites exactos de `Critico` y `Vigilar`; consumo ausente/cero o stock más antiguo que `StaleStockDays` da `SinDatos` con cobertura `null`.
- [x] Implementar `IRiskCalculator.CalculateAsync(bool persistSnapshots, CancellationToken)` y el cálculo de `SuggestedQuantity` en unidad base, redondeado hacia arriba a 3 decimales. Respetar lead time por punto antes del global. `persistSnapshots=true` guarda entradas, versión de fórmula y reloj en `RiskSnapshot`; `false` no escribe. La prueba de integración consulta la fila persistida.
- [x] Pruebas rojas de `ReplenishmentAllocator`: 2 obligatorios del mismo SKU compiten por bodega; gana nivel/prioridad/código, la suma asignada no supera stock menos seguridad y el resto recibe `warehouse_stock`; consolidación usa solo remanente. No tomar lotes bloqueados o vencidos.
- [x] Implementar asignación FEFO estable por vencimiento y código de lote, con cantidad no negativa. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RiskCalculatorTests|FullyQualifiedName~ReplenishmentAllocatorTests|FullyQualifiedName~RiskSnapshotTests"`; esperado: 0 fallos. Commit `feat(logistics): calculate risk and allocate replenishment`.

### T3 — Tramos viales e importador

**Archivos:** modificar `src/Domain/Entities/LogisticsEntities.cs`, `src/Infrastructure/Persistence/LeanSchema.cs`, `src/Infrastructure/Persistence/ApplicationDbContext.cs`, `src/Application/Common/Interfaces/IApplicationDbContext.cs`, `src/Application/Imports/ImportEntity.cs`, `CanonicalSchema.cs`, `CanonicalWriter.cs`, `ImportContext.cs`; crear `src/Application/Logistics/Distances/GeoPoint.cs`, `Leg.cs`, `DistanceProvider.cs`, `Polyline.cs`, `src/Infrastructure/Logistics/OsrmDistanceProvider.cs` y una migración `*_RoadMatrixCache.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/DistanceTests.cs`, `tests/IstpetDev.Tests/Integration/Logistics/RoadSegmentsImportTests.cs`, `DistanceProviderTests.cs`.

- [x] Pruebas rojas de importación: `road_segments` es la octava entidad, admite alias `tramos`, resuelve `from_code`/`to_code` de bodega o punto, exige `*_kind` ante ambigüedad, valida km/minutos positivos y hace upsert dirigido por coordenadas redondeadas a 5 decimales. Reimportar no crea duplicados.
- [x] Crear `RoadMatrixEdge` e índice único `(OrganizationId, FromLat, FromLon, ToLat, ToLon)` y `RouteStockReservation` con vínculo a revisión/entrega/línea/lote/saldo, cantidad, estado e índices del plan; generar **una** migración sobre el esquema de 001, con `DemoGrants.Sql` al final. Excluir solo la caché de `DemoResetService`; borrar reservas con las entregas. Probar que reset + reimportación conservan las aristas y su origen `road`.
- [x] Pruebas rojas de cadena: arista guardada evita HTTP; par faltante usa OSRM simulado, persiste distancia y tiempo, y usa trazado `polyline` cuando se obtiene; OSRM caído o sin URL produce haversine × 1,3 a 40 km/h con `source=approximation`; A→B no implica B→A. Decodificar una polilínea conocida y comprobar extremos.
- [x] Implementar proveedores por lote, `HttpClient` con timeout de 3 s y fallback explícito sin guardar aproximaciones. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~DistanceTests|FullyQualifiedName~RoadSegmentsImportTests|FullyQualifiedName~DistanceProviderTests"`; esperado: 0 fallos. Commit `feat(logistics): import and resolve road distances`.

### T4 — Planificador determinista

**Archivos:** crear `src/Application/Logistics/Planning/PlanModels.cs`, `RoutePlanner.cs`, `RouteFeasibility.cs`, `RouteImprover.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/RoutePlannerTests.cs`, `RouteQualityTests.cs` y `tests/IstpetDev.Tests/Integration/Logistics/RoutePlanningTests.cs`.

- [x] Pruebas rojas de factibilidad: dos vehículos activos; obligatorios `Critico`/`Alto` antes de `Vigilar`; inserción con espera antes de ventana, tiempo de servicio, regreso al depósito dentro de jornada, topes independientes de kg y m³, costo = km × costo/km + horas × `CostPerHour`.
- [x] Probar razones exactas `capacity`, `time_window`, `workday`, `warehouse_stock`, `unreachable` (incluido punto sin coordenadas). Factor de peso o volumen 0 no divide por cero ni rechaza por esa dimensión y añade fact `capacity_dimension_not_evaluated`.
- [x] Implementar inserción de menor costo con desempate por código de vehículo y posición, 2-opt intrarruta con factibilidad, consolidación `Vigilar` con `MaxDetourMinutes`, legs y `HasApproximations`. `PlanAsync` recibe `PlanRequest`; `ReplanAsync` reserva el prefijo fijo para T7. Todas las colecciones expuestas se ordenan antes de serializar; IDs aleatorios no forman parte de la comparación de rutas.
- [x] Probar dos cálculos de la **misma base y reloj**: comparar JSON de resultados canónicos tras excluir IDs de persistencia; prueba de integración compara respuestas HTTP completas una vez que T8 asigne IDs deterministas o estabilice la representación. En instancias de ≤ 8 puntos con semilla fija, costo ≤ 110% del óptimo por fuerza bruta. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RoutePlannerTests|FullyQualifiedName~RouteQualityTests|FullyQualifiedName~RoutePlanningTests"`; esperado: 0 fallos. Commit `feat(logistics): plan deterministic feasible routes`.

### T5 — Publicación, FEFO y reservas

**Archivos:** crear `src/Application/Logistics/RoutePublisher.cs`; modificar mapeos de `RoutePlan`, `RouteStop`, `Delivery`, `DeliveryLine`, `StockBalance` solo si 001 lo requiere; probar en `tests/IstpetDev.Tests/Integration/Logistics/RoutePublisherTests.cs`.

- [x] Prueba roja: publicar una propuesta crea una `Delivery` por parada y líneas con lotes FEFO disponibles al día operativo; por cada línea/lote crea `RouteStockReservation.Active` ligada a la revisión y suma exactamente esa cantidad en `StockBalance.QuantityReserved`, conserva seguridad y no toca saldo físico. Repetir publicación devuelve las mismas entregas e igual reserva; dos solicitudes concurrentes tampoco duplican.
- [x] Implementar transacción con restricción única/idempotencia, elegir lotes por vencimiento y código, y devolver conflicto explicable si cambió el stock entre propuesta y publicación. No aceptar un lote bloqueado ni vencido. Probar invariante `QuantityReserved = Σ reservas Active` por saldo.
- [x] Prueba roja y verde de completar parada: una secuencia se marca `Completed` una sola vez; hora real provista en el cuerpo o, en modo demo, de la hora planificada; completar dos veces no retrocede el estado. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RoutePublisherTests"`; esperado: 0 fallos. Commit `feat(logistics): publish FEFO deliveries idempotently`.

### T6 — Política y explicaciones

**Archivos:** crear `src/Application/Logistics/Policy/AutonomyPolicy.cs`, `AutonomyState.cs`, `src/Application/Logistics/ExplanationTemplates.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/AutonomyPolicyTests.cs`, `ExplanationTemplatesTests.cs`.

- [x] Prueba roja tabular: `AutoWithLimits` solo ejecuta si conserva obligatorios y ventanas, costo sube ≤ 15% y ningún ETA se retrasa > 60 min; los límites exactos se aceptan. Cada incumplimiento produce motivo y fact; `ProposeOnly` siempre escala. Plan inicial exige aprobación si `AutoPublishInitialPlans=false`.
- [x] Implementar `IAutonomyPolicy.Classify` sin recibir texto libre, y `AutonomyState` en memoria con lectura/escritura de modo; reinicio vuelve al valor configurado. Prueba de reinicio y de concurrencia de lectura/escritura.
- [x] Pruebas rojas de cuatro plantillas (`risk`, `plan`, `replan`, `escalation`): toda cifra renderizada coincide con un `Fact` por sujeto y unidad; si falta fact, la plantilla no inventa valor. Implementar `IExplanationTemplates` en español y formateo estable. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~AutonomyPolicyTests|FullyQualifiedName~ExplanationTemplatesTests"`; esperado: 0 fallos. Commit `feat(logistics): classify autonomy and explain facts`.

### T7 — Incidentes, revisiones y opciones

**Archivos:** crear `src/Application/Logistics/Incidents/IncidentEvaluator.cs`, `Replanner.cs`, `src/Application/Logistics/Policy/EscalationOptions.cs`; probar en `tests/IstpetDev.Tests/Unit/Logistics/IncidentEvaluatorTests.cs`, `tests/IstpetDev.Tests/Integration/Logistics/ReplanningTests.cs`.

- [x] Pruebas rojas: `ROAD_CLOSURE` bloquea solo legs cuyo trazado cruza el círculo; `DELAY` multiplica minutos de legs afectados; `VEHICLE_BREAKDOWN` deja indisponible ese vehículo desde el incidente. Un trazado aproximado usa su segmento recto. La respuesta enumera rutas y paradas afectadas.
- [x] Implementar `IncidentEvaluator` con distancia punto–segmento local y `IncidentEffect`, sin modificar una ruta ya publicada. Añadir prueba con un tramo apenas fuera del radio y otra con extremos fuera pero trazado dentro.
- [x] Pruebas rojas de replan: completadas idénticas y en el mismo orden, comienzo en última parada completada, solo pendientes se reordenan, revisión `n+1` apunta a `SupersedesRoutePlanId=n`, diff incluye cambios de ETA, costo, movidas y no atendidas. Avería: otro vehículo vuelve al depósito, recarga y recibe pendientes factibles; resto `workday` o capacidad.
- [x] Implementar `ReplanAsync(Strict)`, `LateService` (ventana blanda solo de obligatorios) y `PrioritizeCritical` (sin consolidación, ventana dura); opción inviable declara `feasible=false` y razón. Guardar `AgentDecision` por vehículo con origen y estado; autoejecución publica, escalado conserva la revisión vigente; aprobar opción, rechazar y revertir son idempotentes. Revertir bloquea reservas/saldos, cancela solo entregas pendientes no despachadas, libera sus reservas `Active`, revalida stock, reserva para la revisión restaurada y publica una **tercera** revisión en un commit. `Consumed` y completadas quedan históricas; entrega pendiente ya despachada o stock insuficiente → 409 sin efectos. Probar replay de reversión y el invariante de reservas por saldo.
- [x] Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~IncidentEvaluatorTests|FullyQualifiedName~ReplanningTests"`; esperado: 0 fallos. Commit `feat(logistics): replan incidents with auditable decisions`.

### T8 — Endpoints y roles

**Archivos:** crear `src/Api/Controllers/RisksController.cs`, `RoutePlansController.cs`, `IncidentsController.cs`, `AgentDecisionsController.cs`, `AutonomyController.cs`; modificar `src/Api/Controllers/DemoController.cs`, `src/Api/Program.cs`, `src/Infrastructure/DependencyInjection.cs`; probar en `tests/IstpetDev.Tests/Integration/Logistics/LogisticsApiTests.cs`.

- [x] Pruebas HTTP rojas de todos los endpoints de [Entidades y contratos](spec.md#entidades-y-contratos): métodos/códigos, JSON camelCase, `facts`, Problem Details, filtro de riesgos, consulta de revisión y decisiones, publicar/completar/revertir, registrar/replanificar, aprobar/rechazar, GET/PUT del modo, preset de demo.
- [x] Probar roles: lectura para cualquier cuenta autorizada; planificación y automatización crean planes/incidentes y replanifican; conductor solo completa; gerencia cambia modo; `POST /demo/incidents` devuelve 404 o 403 si `Demo:Enabled=false` y no ejecuta incidente. Los endpoints de escritura rechazan token ausente/rol incorrecto.
- [x] Conectar servicios y persistencia. `POST /route-plans` responde 201 síncrono; propuesta inicial queda pendiente de aprobación por defecto. Los errores de conflictos de stock y opciones inviables tienen códigos y motivos estables. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~LogisticsApiTests"`; esperado: 0 fallos. Commit `feat(api): expose deterministic logistics endpoints`.
- [x] **Lectura para el panel 005 (FR-23):** `GET /api/v1/route-plans?date=&status=&vehicleCode=&limit=50&cursor=` devuelve `{items:[{routePlanId,code,revision,status,vehicleCode,km,minutes,cost,hasApproximations}],nextCursor}` con la revisión vigente, orden estable por fecha, código y revisión, y no crea propuestas. Pruebas en `LogisticsApiTests`: paginación sin repetir IDs, sin token → 401, el GET no cambia el conteo de planes. Integrar antes de H4; el spec 005 (T1) la consume y no la implementa.

### T9 — Fixture regional y calibración

**Archivos:** crear `data/fixtures/demo-region/{bodegas,puntos,productos,vehiculos,lotes,inventario,consumos,tramos}.csv`, `mapping.json`, `incidentes.json`, `scripts/data/build-road-segments.py`, `scripts/data/calibrate-incident.sh`; modificar el selector de fixture del reset de 001; probar en `tests/IstpetDev.Tests/Integration/Logistics/DemoRegionTests.cs`.

- [x] Prueba roja: reset de `demo-region` importa por el importador canónico 1 bodega, 15 puntos, 2 vehículos, 3 SKU, consumos y tramos dirigidos; reloj `Dataset=2026-10-12`; 1 `Critico`, 4 `Alto`, 3 `Vigilar`, 1 `SinDatos`, 6 `Normal`; `HOS-N01/DES-01` tiene 5,0 días.
- [x] Crear los 7 CSV de 001 más `tramos.csv`, perfil e incidentes del plan. El generador consulta OSRM a ≤ 1 petición/s para 240 pares dirigidos y falla con salida clara si no obtiene datos; el CSV versionado permite correr sin red. Verificar `source=road` para todos los legs en el recorrido de demo y 0 solicitudes HTTP externas.
- [x] Calibrar `cierre-norte` **después** de ejecutar el planificador real mediante `scripts/data/calibrate-incident.sh`: reset, propuesta, publicación, dos paradas completadas, incidente, replan de rutas afectadas. Ajustar solo datos de `incidentes.json` o el fixture hasta obtener `VEH-01=AutoExecuted`, `VEH-02=Escalated` (mapeo recalibrado y autorizado en #39), con `late_service` y `prioritize_critical` calculadas. Fijar ese resultado en `DemoRegionTests`. Si 004 ya está integrado, el script completa las paradas con `POST /api/v1/demo/route-plans/{id}/advance` en vez del endpoint provisional.
- [x] Repetir reset y recorrido; los códigos del preset siguen resolviendo con IDs nuevos y la caché conserva tramos. Ejecutar `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~DemoRegionTests"`; esperado: 0 fallos. Commit `feat(demo): add reproducible regional scenario`.

### T10 — Recorrido E2E, rendimiento y bóveda

**Archivos:** crear `tests/IstpetDev.Tests/E2E/LogisticsJourneyTests.cs`, `tests/IstpetDev.Tests/Performance/LogisticsPerformanceTests.cs`; actualizar `docs/boveda/03 Arquitectura/Decisiones de arquitectura.md` según protocolo y `docs/specs/002-motor-logistico/test-e2e.md` con evidencias **solo tras ejecutarlo**.

- [x] Automatizar [E2E-01 a E2E-05](test-e2e.md) como recorridos HTTP con Testcontainers: riesgos, plan, publicación/FEFO, incidente, clasificación y opciones, aprobación/rechazo/reversión, modo `ProposeOnly`, sin red y reset. Fijar las respuestas observables sin depender de GUID generados.
- [x] Medir propuesta de 15 puntos/2 vehículos ≤ 1 s y 80 puntos/2 vehículos ≤ 5 s con datos sintéticos y semilla fija; excluir arranque, migración e importación del cronómetro. Registrar hardware/CI y tiempos. Probar determinismo byte a byte del JSON HTTP sobre una misma base y reloj.
- [x] Ejecutar `dotnet test tests/IstpetDev.Tests`, `dotnet build IstpetDev.sln` y los pasos de `test-e2e.md`; registrar hash, fecha, duración y evidencias sin tokens. Revisar los 22 FR, 6 NFR y 7 CE en la matriz de abajo; marcar gaps explícitos, no cerrar el spec con faltantes.
- [x] Registrar las contradicciones de mezcla de distancias, respuesta síncrona y autonomía en la bóveda; proponer ADR-22 conforme al protocolo, sin presentar aprobación implícita. Commit `test(logistics): verify end-to-end demo and performance`.

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
| FR-24 | T16 | `RoutePlannerTests`, `RoutePlanningTests` |
| NFR-01, CE-02, CE-06 | T10 | `LogisticsPerformanceTests`, E2E-06 |
| NFR-02, CE-05 | T3, T9 | `DistanceProviderTests`, E2E-05 |
| NFR-03, CE-07 | T6 | `ExplanationTemplatesTests`, E2E-07 |
| NFR-04–NFR-06 | T2, T4, T6, T10 | revisión G1, pruebas de unidades y política |
| CE-01 | T9 | `DemoRegionTests`, E2E-01 |
| CE-03 | T4, T10 | JSON HTTP canónico, E2E-02 |
| CE-04 | T7, T9 | `DemoRegionTests`, E2E-03 |

**Nota de contrato:** CE-03 exige JSON idéntico byte a byte. La prueba de T4 sobre objetos no basta: T8/T10 deben estabilizar también los identificadores y campos de auditoría de la respuesta de propuestas repetidas, o excluirlos del contrato público mediante una misma representación canónica. No relajar CE-03 en la prueba E2E.

## Corrección de integración descubierta por #39 — 14-oct-2026

La comprobación previa de T7/T9 no detectaba que ReplanAsync volvía a PlanAsync
sin usar prefijos/efectos/modos ni exigía el par real del preset. La regresión
reforzada falló con una ruta afectada y la otra vacía. Se corrigen carga publicada,
clasificación original de paradas, prefijos, cierres/demoras, ventanas, depósito,
jornada y publicación que conserva entregas/reservas. Demo/advance usa la fecha
operativa para el evento del dispositivo; servidor registra la hora real.

Calibración HTTP local reproducida con 2 rutas afectadas, VEH-01 automático
y VEH-02 escalado; Bryan delegó elegir este mapeo con umbrales intactos. Fixture
VEH-02 180 kg/0,15 USD-km; círculo del preset actualizado. La prueba canónica
exige el par, dos opciones del escalado y dos paradas completadas con recibos.
Resultado de la prueba tras fijar datos todavía en verificación; despliegue y
aceptación productiva se registran exclusivamente en spec 003. No se atribuye
esta corrección como comprobación completa de la reasignación por avería ni de
la aprobación de alternativas de otros escenarios; requieren su propia evidencia.

### T11 — Rutas con cualquier dataset: bodega propia por vehículo y coordenadas de alta precisión (15-oct)

**Responsable:** Anthony Pilatasig. **Código:** [RoutePlanner](../../../src/Application/Logistics/Planning/RoutePlanner.cs). **Pruebas:** [DatasetsJourneyTests](../../../tests/IstpetDev.Tests/E2E/DatasetsJourneyTests.cs) y [LogisticsPerformanceTests](../../../tests/IstpetDev.Tests/Performance/LogisticsPerformanceTests.cs).

**Cómo se encontró.** Se recorrió importar → `GET /risks` → `POST /route-plans` → `POST /agent/runs` con cada dataset del repositorio (`docs/dataset/test/national-v1-seed-11`, `data/fixtures/demo-base`, `demo-region`, `demo-region-injection` y `ejemplo-externo`). Importación, alertas y agente respondieron con todos. Las rutas tenían dos defectos:

1. **Una sola bodega para toda la flota.** `PlanAsync` tomaba `warehouseLocations.FirstOrDefault()` como salida de todos los vehículos, sin mirar su `DepotLocationId`. Con las 6 bodegas del dataset nacional (ADR-28):
   - Todos salían desde Ambato.
   - Cuatro planes quedaban vacíos (0 paradas, 0 km).
   - Los demás recorrían unos 330 km con 3 a 5 paradas.

   `ReplanAsync` tenía el mismo supuesto.
2. **Coordenadas con más de 5 decimales.** `DistanceProvider` guarda la matriz con `GeoPoint.Round()` (5 decimales), pero `PlanAsync` buscaba con las posiciones sin redondear. Con datos de más precisión, como las exportaciones de GPS, todos los puntos quedaban `unreachable` y no se planificaba nada.

   Los benchmarks de rendimiento estaban en ese caso: 0 paradas con 15 y con 80 puntos. Pasaban solo porque contaban rutas vacías.

**Corrección:**
- Cada vehículo sale y vuelve a **su** bodega, al planificar y al replanificar. Si no tiene bodega o esta no tiene coordenadas, se usa la primera, como antes.
- Todas las bodegas entran en la matriz de distancias.
- Las posiciones se redondean igual que las claves de la matriz.
- Un vehículo sin paradas ya no genera un plan vacío; queda el fact `vehicle_idle`.

**Resultado con los datos del repositorio:**

| Dataset | Antes | Después |
|---|---|---|
| `national-v1-seed-11` (6 bodegas) | 12 planes: 4 vacíos y 8 de unos 300 km con 3 a 5 paradas | 6 planes, uno por bodega, con 13 o 14 paradas (80/80 puntos) y 104 a 143 km |
| `demo-base`, `demo-region`, `demo-region-injection` (1 bodega) | 1, 2 y 2 planes | Sin cambios (mismos km y paradas) |
| `ejemplo-externo` (sin bodegas ni stock) | Sin rutas | Sin rutas, con el motivo `warehouse_stock` en `unserved` |
| Benchmarks de 15 y 80 puntos (7 decimales) | 0 paradas | 15/15 paradas; 44 de 80 (36 sin atender por capacidad) |

**Verificación:**
- Suite del backend 444/444 (Release, PostGIS en Testcontainers).
- `AgentJourneyTests.Fallback_proposes_routes_and_rejects_late_callback(partial_watch)` falló una vez con la suite completa y pasó en las 3 repeticiones aisladas y en la corrida completa siguiente. Es sensible al tiempo: el n8n falso crea los planes en segundo plano mientras corre el plazo del agente. Queda registrada como **intermitente**, no como resuelta.
- El agente se probó por el camino determinista (sin n8n). La prueba con n8n y DeepSeek reales requiere un token de Planificador vigente.

| Archivo | Capa | Cambio | Por qué | Estado |
|---|---|---|---|---|
| `src/Application/Logistics/Planning/RoutePlanner.cs` | Backend | Bodega por vehículo al planificar y replanificar; posiciones redondeadas; sin planes vacíos | Defectos 1 y 2 | Hecho |
| `tests/IstpetDev.Tests/E2E/DatasetsJourneyTests.cs` | Pruebas | Recorrido completo con los datasets del repositorio y con coordenadas de 7 decimales | Que los defectos no vuelvan con datos reales | 6/6 en verde |
| `tests/IstpetDev.Tests/Performance/LogisticsPerformanceTests.cs` | Pruebas | El benchmark exige paradas planificadas, no solo rutas | Pasaba sin planificar nada | En verde |

### T12 — Rutas por calles en el mapa y un color por vehículo (15-oct)

**Responsable:** Anthony Pilatasig. **Código:** [RouteGeometryService](../../../src/Application/Logistics/RouteGeometryService.cs), `GET /api/v1/route-plans/{id}/geometry` en [RoutePlansController](../../../src/Api/Controllers/RoutePlansController.cs), [página Rutas](../../../apps/web/src/pages/rutas/rutas.page.ts) y [map-view](../../../apps/web/src/shared/ui/map-view/map-view.component.ts). **Pruebas:** [RouteGeometryTests](../../../tests/IstpetDev.Tests/Integration/Logistics/RouteGeometryTests.cs) y [rutas-map.spec.ts](../../../apps/web/src/pages/rutas/rutas-map.spec.ts).

**Problema.** Las rutas propuestas por el motor o el agente se dibujaban como rectas entre paradas, sin la bodega de salida, y todas con el mismo color fijo (`#00A7C4`).
- `Logistics:Osrm:BaseUrl` estaba vacío.
- Aun configurado, el backend solo pedía la tabla de distancias (`/table`), no la geometría.
- La web unía las paradas con segmentos rectos (`buildMap`).

**Decisiones:**
- **OSRM sobre OpenStreetMap** (ADR-05: sin costo ni API key), elegido por el usuario frente a Google Maps.
- **OSRM solo dibuja.** La opción nueva `Logistics:Osrm:RouteGeometryUrl` no cambia las distancias del plan: activar `BaseUrl` habría cambiado los km de todas las rutas y descalibrado la política del agente validada en #39.
- **Recorrido completo:** bodega propia del vehículo → paradas en orden → bodega. Se guarda en memoria por plan y versión: 12 h si sale bien, 1 min si falla.
- **Sin OSRM:** rectas marcadas `approximate`, que la web dibuja punteadas con un aviso.
- **En la web:**
  - Todas las rutas vigentes a la vez (última revisión por vehículo, sin canceladas, rechazadas ni reemplazadas), cada una con su color.
  - La seleccionada va resaltada y encima.
  - La bodega aparece marcada, y hay una leyenda clicable con vehículo, km y paradas.
- **Seguridad:** los textos de las ventanas emergentes del mapa se escapan; vienen de datos importados (el fixture `demo-region-injection` trae texto malicioso).

**Verificación en vivo** contra `router.project-osrm.org`, con `docs/dataset/test/national-v1-seed-11`:
- La primera prueba falló: el servidor público responde **403 sin `User-Agent`**, que `HttpClient` no envía. Se agregó el encabezado y una prueba que lo exige.
- Después: 6/6 rutas `osrm`, con 3.332 a 7.598 puntos de trazado y entre 0,4 y 2 s por ruta en la primera consulta.

**Diferencia que hay que conocer.** Por calles, las rutas miden **147 a 233 km**; el plan dice **104 a 143 km**, porque el motor usa una aproximación en línea recta. La leyenda indica «por calles» o «aprox.» para no confundir las dos cifras. Llevar las distancias del plan a carretera (`BaseUrl`) queda como decisión posterior al evento, porque obliga a recalibrar la política.

**Datos:** los puntos y bodegas de `docs/dataset` y `data/fixtures` son **sintéticos**, ubicados en zonas plausibles de ciudades reales; solo la red vial (OpenStreetMap) es real.

| Archivo / Componente | Capa | Cambio | Por qué | Estado |
|---|---|---|---|---|
| `RouteGeometryService.cs`, `LogisticsOptions.cs`, `DependencyInjection.cs` | Backend | Recorrido por calles con OSRM, caché y respaldo aproximado, más `User-Agent` | Las rutas se dibujaban rectas | Hecho; 3 pruebas |
| `RoutePlansController.cs` | Backend | `GET /route-plans/{id}/geometry` | La web necesita el trazado y las paradas de cada plan | Hecho |
| `appsettings.*.json`, `docker-compose.prod.yml` | Configuración | `RouteGeometryUrl` (en producción, `OSRM_ROUTE_GEOMETRY_URL` sobrescribible) | Producción tampoco tenía proveedor de rutas | Hecho |
| `rutas.page.ts`, `api.service.ts` | Frontend | Todas las rutas con color por vehículo, resaltado, bodega y leyenda | No se distinguían las rutas | Hecho; 2 pruebas nuevas |
| `map-view.component.ts` | Frontend | Marcadores de color numerados, opacidad, etiqueta y escape de HTML | Soporte de lo anterior y seguridad | Hecho |

Suite del backend 447/447; web 74/74 y `build:web` de producción sin errores.

#### T12b — El motor planifica con distancias por calles (decidido por el usuario el 15-oct)

**Medición previa:** se comparó el orden de paradas del motor con el orden óptimo por calles (OSRM `trip`), con las mismas paradas:
- Dataset nacional: el orden del motor recorre **9 % más de km** en total, y hasta **31 %** en VEH-MA-01.
- `demo-region`: 4 % más.

Causa: el motor ordenaba con distancias en línea recta (`Osrm:BaseUrl` vacío). El trazado de OSRM entre paradas respeta las calles de una vía de OpenStreetMap; lo que fallaba era el orden. Google Maps no lo resolvería sin reordenar las paradas.

**Defectos de `DistanceProvider` al activar OSRM**, corregidos:
1. No enviaba `User-Agent`: el servidor público respondía 403 y se volvía en silencio a la aproximación.
2. No respetaba el límite de 100 coordenadas por tabla: ahora pide por bloques con `sources` y `destinations`.
3. Tiempo límite de 3 s, insuficiente para una tabla nacional: ahora 10 s.

Además, los pares pendientes se buscan en un `HashSet` y se registra cuántos tramos quedaron aproximados.

**Efecto medido con `Osrm:BaseUrl` activo:**

| Dataset | Tramos del motor | Resultado |
|---|---|---|
| `demo-region` (demo) | 240/240 de `tramos.csv` del dataset | **Sin cambios:** mismos km (57,5 y 11,5) y misma política del agente |
| Nacional | 1.148 de `road_segments.csv` + 6.162 de OSRM | Cambian VEH-UN-01 (122,8 → 152,9 km) y VEH-US-01 (103,9 → 132,2 km); planificar tarda 4,1 s la primera vez (luego los tramos quedan en `RoadMatrixEdge`) |

**Prioridad de las distancias:** se mantiene tramos importados > OSRM > aproximación. Si una empresa sube sus propios tiempos, mandan sus datos.
- Los tramos de `docs/dataset` y `data/fixtures` son **sintéticos** (README del dataset nacional).
- Para planificar el nacional con calles reales, súbelo **sin** `road_segments.csv`.

**Verificación:**
- Con `Osrm:BaseUrl` apuntando al OSRM público: 58/58 pruebas de E2E, logística y agente en verde, incluido el incidente `cierre-norte` (VEH-01 automático, VEH-02 escalado).
- Suite completa sin OSRM: 448/449. Falla la intermitente conocida `AgentJourneyTests(partial_watch)`, que pasa aislada.
- Prueba nueva: 120 puntos → peticiones de 100 coordenadas como máximo, con `User-Agent`, y todos los pares por carretera.

**Configuración:** `Logistics:Osrm:BaseUrl` = `https://router.project-osrm.org` y `TimeoutSeconds` = 10 en `appsettings`. En producción, `OSRM_BASE_URL` y `OSRM_TIMEOUT_SECONDS` se pueden sobrescribir.

### T13 — Las rutas publicadas llegan al conductor (15-oct)

**Responsable:** Anthony Pilatasig. **Código:** [RoutePublisher](../../../src/Application/Logistics/RoutePublisher.cs), [DriversController](../../../src/Api/Controllers/DriversController.cs), `Publish` en [RoutePlansController](../../../src/Api/Controllers/RoutePlansController.cs) y [página Rutas](../../../apps/web/src/pages/rutas/rutas.page.ts). **Pruebas:** [PublishToDriverTests](../../../tests/IstpetDev.Tests/Integration/Logistics/PublishToDriverTests.cs) y `rutas-map.spec.ts`.

**Diagnóstico (código y base de desarrollo compartida, solo lectura):** el conductor ve únicamente las entregas con su cuenta en `Delivery.AssignedAccountId` (`FieldService.GetMobileAssignmentsAsync`).
1. **Nadie asignaba el conductor.** No hay relación vehículo–conductor en el esquema: la asignación existe solo al publicar (`PublishRequest.DriverAssignments`). La web publicaba con `{}`, así que las entregas quedaban sin asignar.
2. **Las rutas del asistente (pedidos y emergencia) se publicaban vacías.** `RoutePublisher` crea una entrega por parada solo si `InputJson` trae `loads`, y `confirm` no las guarda. El plan quedaba `Published` con 0 entregas.
   - En la base de desarrollo: **3 planes `Published` (los 3 del asistente) y 0 filas en `Delivery`**; 2 Conductores activos sin entregas.
3. **El stock se reservaba siempre en la primera bodega** encontrada, no en la del vehículo ni en la de recogida.

La base de producción no se consultó: requiere el túnel SSM. Corre el mismo código, por lo que se espera el mismo comportamiento, pero no está verificado.

**Corrección:**
- **Asignar conductor:** `GET /api/v1/drivers` (Gerencia y Planificador) lista los Conductores activos, sin correos. La web exige elegir conductor antes de «Publicar a Conductor» y envía `driverAssignments`. La API valida que la cuenta sea un Conductor activo (422 si no).
- **Cargas desde los pedidos:** cada parada de entrega sin cargas toma las líneas pendientes de los pedidos de su punto (`orderIds` del plan).
- **Origen correcto:** cada entrega sale de la última recogida en bodega previa en la ruta o, si no hay, de la bodega del vehículo; el stock se reserva ahí.
- **Sin publicaciones vacías:** si no hay nada que entregar, responde 409 con el motivo y el plan queda `Proposed`. Los errores de stock también son 409 con motivo; antes eran 500.
- **Reparación:** un plan ya `Published` sin entregas se puede volver a publicar. Así se recuperan los 3 planes de la base de desarrollo.

**Pendiente:** no existe relación fija vehículo–conductor, así que el conductor se elige en cada publicación. Las publicaciones automáticas del agente no asignan conductor, y las revisiones conservan el de la ruta anterior.

| Archivo / Componente | Capa | Cambio | Por qué | Estado |
|---|---|---|---|---|
| `RoutePublisher.cs` | Backend | Conductor validado, cargas desde pedidos, bodega por parada, sin publicaciones vacías, reparación | Defectos 1, 2 y 3 | Hecho |
| `DriversController.cs` | Backend | `GET /api/v1/drivers` | El Planificador necesita elegir conductor sin acceso a `/accounts` | Hecho |
| `RoutePlansController.cs` (`Publish`) | Backend | 404, 409 y 422 con motivo | Antes, error 500 | Hecho |
| `rutas.page.ts`, `api.service.ts` | Frontend | Selector de conductor obligatorio y mensaje del servidor | La web publicaba sin conductor | Hecho |
| `PublishToDriverTests.cs` | Pruebas | 6 escenarios, incluido el conductor que ve su ruta en `/mobile/assignments` | Regresión | 6/6 |

Verificación: backend 455/456. Falla `N8nErrorWorkflowTests`, que también falla sin estos cambios porque necesita un n8n real en local. Web 75/75 y `build:web` sin errores.

#### T13b — Bodega, vehículo y conductor visibles en web y PWA (#160, 15-oct)

**Problema:** al publicar ya se elige conductor (T13), pero:
- Nadie veía después a quién se asignó la ruta.
- Las rutas publicadas antes sin conductor no tenían forma de corregirse, porque publicar es idempotente.
- La Bodega no sabía a qué vehículo iba cada entrega.

**Corrección:**
- **Reasignar conductor:** `PUT /api/v1/route-plans/{id}/driver` (Gerencia y Planificador) cambia el conductor de las entregas **pendientes** de una ruta publicada. Las entregadas, parciales, recibidas y canceladas no se tocan.
  - 404 si el plan no existe.
  - 409 si no está publicado o no tiene entregas pendientes.
  - 422 si la cuenta no es un Conductor activo.
  - Los roles de campo reciben 403.
- **Detalle del plan:** devuelve `originWarehouseName`, `driverName`, `deliveriesCount` y `unassignedDeliveries`. La web muestra Bodega, Vehículo y Conductor, avisa de las entregas sin conductor y permite cambiar el conductor.
- **PWA, Bodega:** el detalle de la entrega muestra vehículo, conductor y bodega de salida.
- **PWA, Conductor:** su ruta muestra «Sale de: {bodega}». Si el vehículo es desconocido ya no se inventa «VEH-01»; se deja vacío.

**Límite del esquema:** `Vehicle` no tiene campo de placa, así que se muestra el código del vehículo.

| Archivo / Componente | Capa | Cambio | Por qué | Escenario | Estado |
|---|---|---|---|---|---|
| `RoutePublisher.AssignDriverAsync` | Backend | Reasignar las entregas pendientes | Publicar no reasigna | Rutas antiguas sin conductor | Hecho |
| `RoutePlansController` | Backend | `PUT …/driver` y campos de asignación en el detalle | La web no mostraba la asignación | Planificador revisa la ruta | Hecho |
| `FieldService`, `FieldContracts` | Backend | Vehículo, conductor y bodega en el detalle y en la ruta móvil | Bodega y Conductor sin contexto | Despacho en bodega | Hecho |
| `rutas.page.ts`, `api.service.ts` | Web | Fila de asignación y «Cambiar conductor» | Visibilidad y corrección | Planificador | Hecho |
| `warehouse.component.ts`, `home.page.ts`, `field.contract.ts` | PWA | Vehículo, conductor y «Sale de» | Visibilidad en campo | Bodega y Conductor | Hecho |
| `PublishToDriverTests.cs` | Pruebas | Asignación vista por Bodega y Conductor, reasignación, 403/404/409/422 | Regresión | Todos | 9/9 |

**Verificación:**
- Backend: 512 de 514. Fallan `N8nErrorWorkflowTests`, que necesita un n8n real en local, y `AgentJourneyTests(partial_watch)`, que es intermitente. Los dos fallaban ya antes de #160.
- Web: 76 de 76; `build:web` y `check:mobile` sin errores.

#### T12c — Rectas en el mapa con el OSRM público saturado (16-oct)

**Síntoma:** algunas rutas seguían saliendo como líneas rectas punteadas, en local y en producción.

**Causa medida el 16-oct contra `router.project-osrm.org`, con los puntos reales de la base dev:**
- **El servidor está saturado.** Es el servidor de demostración de OSRM. Con 6 consultas en paralelo, que es lo que hace la web al abrir Rutas, entre 1 y 3 de cada 6 superaron los 8 s de `RouteGeometryTimeoutSeconds`. El backend las cortaba y devolvía `approximate`.
- **La web no reintentaba.** Guardaba esa respuesta en `geometries` y no volvía a pedirla, así que la ruta quedaba en rectas hasta recargar la página.
- **El backend fijaba las rectas.** Guardaba la respuesta aproximada durante 1 minuto.

**Sobre Dijkstra:** el algoritmo no era el problema. OSRM calcula el camino con Contraction Hierarchies, una variante acelerada de Dijkstra sobre la red vial de OpenStreetMap, con sentidos de vía. Las rectas aparecen solo cuando la consulta no llega a responder.

**Corrección:**
- **Backend (`RouteGeometryService`):**
  - Como máximo 2 consultas simultáneas a OSRM por proceso.
  - Un reintento tras 500 ms cuando el fallo es pasajero: tiempo agotado, error de red, 429 o 5xx. Un 400 no se reintenta.
  - Solo se guarda en caché el trazado por calles; las rectas no.
- **Web (`rutas.page.ts`):** las rutas que llegan en rectas se vuelven a pedir a los 4, 10 y 20 s. El temporizador se cancela al salir de la página.

**Medición con el patrón corregido**, en 3 rondas de 6 rutas contra el OSRM real: **18 de 18 por calles**, 2 de ellas en el reintento.

**Pendiente:** el servidor público no garantiza disponibilidad. La solución definitiva es un OSRM propio con el extracto de Ecuador, configurable con `OSRM_BASE_URL` y `OSRM_ROUTE_GEOMETRY_URL` sin cambiar código.

| Archivo / Componente | Capa | Cambio | Por qué | Escenario | Estado |
|---|---|---|---|---|---|
| `RouteGeometryService.cs` | Backend | Máximo 2 simultáneas, 1 reintento, sin caché de rectas | OSRM público supera el timeout con carga | Abrir Rutas con varias rutas | Hecho |
| `rutas.page.ts` | Web | Volver a pedir las rutas en rectas (4, 10 y 20 s) | La web fijaba la respuesta aproximada | Mapa de todas las rutas | Hecho |
| `RouteGeometryTests.cs`, `rutas-map.spec.ts` | Pruebas | Reintento, sin caché de rectas, 400 sin reintento, reintento en la web | Regresión | — | Pasan |

**Verificación:**
- Backend: 518 de 519. Falla `N8nErrorWorkflowTests`, que necesita un n8n real en local.
- Web: 77 de 77.

#### T13c — «Sin productos registrados para esta parada» en el detalle de la ruta (16-oct)

**Síntoma:** en Planificación → Rutas, las paradas no mostraban qué productos se llevan, y la recogida en bodega no decía qué cargar.

**Causa** (verificada con código y con la base dev, donde 3 planes publicados del asistente tienen `orderIds`, ninguna carga en `InputJson` y 0 entregas):
1. **La web descartaba los productos.** `ApiService.getRoutePlanById` reconstruía cada parada campo por campo y perdía `lines`, `handlingUnits`, `deliveryCode` y `deliveryStatus`, además de `productsSummary` y `driverRoutes`. Aunque el backend enviara los productos, la página nunca los recibía. Las pruebas del #164 simulaban `getRoutePlanById` y por eso no lo detectaron.
2. **Faltaban las cargas de las rutas del asistente.** Esas rutas guardan los pedidos y no las cargas, y el detalle solo leía `InputJson.stops[].loads`. Sin entregas creadas, no había nada que mostrar.
3. **La recogida en bodega no mostraba nada.** La web daba por hecho que la parada 1 era la bodega y no descargaba nada, sin mirar el tipo de parada.

**Corrección:**
- **`OrderStopLoads` (helper nuevo):** deduce las cargas de cada parada de entrega a partir de los pedidos. Lo usan la publicación, que antes tenía su propia copia, y el detalle del plan. Así la ruta muestra antes de publicar lo mismo que se publicará.
- **Detalle del plan:**
  - Cada parada indica su tipo (`kind`).
  - La recogida usa el nombre de su bodega y lista lo que se **carga** en ella: la suma de lo que se entrega hasta la siguiente recogida, con estado `Cargar`.
  - El resumen por producto no cuenta dos veces lo cargado.
- **Web:** la conversión conserva productos, bultos, entrega, tipo de parada, resumen y rutas del conductor. La recogida muestra «Cargar en esta bodega».

**Pendiente, para decidir:** la ruta manual o de emergencia (`/route-plans/manual`) no guarda pedidos ni cargas. Sigue mostrando «Sin productos» y al publicarla responde 409 «La ruta no tiene productos para entregar». Para que lleve productos, habría que decidir de dónde salen: por ejemplo, los pedidos pendientes de cada punto.

| Archivo / Componente | Capa | Cambio | Por qué | Escenario | Estado |
|---|---|---|---|---|---|
| `OrderStopLoads.cs`, `RoutePublisher.cs` | Backend | Cargas desde pedidos en un solo lugar | Detalle y publicación deben coincidir | Ruta del asistente | Hecho |
| `RoutePlansController.GetById` | Backend | `kind`, productos desde pedidos, carga en la recogida, resumen sin duplicar | Causas 2 y 3 | Ver una ruta propuesta o publicada | Hecho |
| `api.service.ts` | Web | No descartar líneas, bultos ni resumen | Causa 1 | Detalle de cualquier ruta | Hecho |
| `rutas.page.ts` | Web | «Cargar en esta bodega» según el tipo de parada | Causa 3 | Recogida en bodega | Hecho |
| `PublishToDriverTests.cs`, `api.service.spec.ts` | Pruebas | Productos por parada antes y después de publicar; conversión web | Regresión | — | Pasan |

**Verificación:**
- Backend: 519 de 520. Falla `N8nErrorWorkflowTests`, que necesita un n8n real en local.
- Web: 78 de 78; lint y `build:web` sin errores.

### T14 — El asistente de pedidos ordena por prioridad y usa las ventanas del punto (#197, 16-oct)

**Síntoma:** `POST /route-plans/suggest` armaba las recogidas y después las entregas en el orden en que salían del abastecimiento. No usaba `DeliveryOrder.Priority` ni `RequiredDateUtc`, y ponía a todas las entregas la ventana fija 08:00–17:00 con 15 min de servicio, aunque el punto tuviera la suya.

**Corrección:**
- **Orden de entregas (`DeliveryStopSequencer`, nuevo):** primero por prioridad (`Critical`, `High`, `Normal`, `Low`; un valor desconocido cuenta como `Normal`) y luego por día requerido. Dentro de cada nivel, vecino más cercano por distancia en línea recta desde la parada anterior, empezando en la última recogida. Si varios pedidos van al mismo destino, manda el más urgente y la fecha más temprana.
- **Ventanas:** cada entrega toma `WindowStartLocal`, `WindowEndLocal` y `ServiceMinutes` del punto. Si el punto no los tiene, o el destino es una bodega, quedan 08:00–17:00 y 15 min.
- **Sin cambio de contrato:** la respuesta conserva su forma; el panel del asistente en `apps/web` no cambia. Las recogidas siguen antes que las entregas y `/confirm` no se tocó.
- **Independiente de #193:** no usa `CriticalDeadlineLocal`. La validación de capacidad por prioridad en `/suggest` y `/confirm` queda para #201.

| Archivo / Componente | Capa | Cambio | Por qué | Escenario | Estado |
|---|---|---|---|---|---|
| `DeliveryStopSequencer.cs` | Application | Orden por prioridad, día requerido y vecino más cercano; ventana y servicio de respaldo | Lógica pura que se prueba sin base de datos | Varios pedidos con distinta prioridad | Hecho |
| `RoutePlansController.SuggestRoutes` | Api | Carga pedidos y puntos destino; ventana y servicio del punto; entregas ordenadas con el secuenciador | El asistente ignoraba prioridad, fecha y ventana | Asistente de pedidos | Hecho |
| `DeliveryStopSequencerTests.cs` | Pruebas unitarias | Prioridad antes que distancia, día requerido, vecino más cercano encadenado, prioridad por destino compartido | Regresión del orden | — | Pasan |
| `RouteSuggestPriorityTests.cs` | Pruebas de integración | Critical lejano antes que Normal cercano; ventana propia del punto y respaldo 08:00–17:00 | Criterios de cierre de #197 | `/suggest` con datos `demo-base` | Pasan |
| `Contratos API y eventos.md` | Bóveda | Regla de orden y ventanas de `/suggest` | Contrato primero | — | Hecho |

**Escenarios:**

| Escenario | Resultado esperado | Estado |
|---|---|---|
| Dos pedidos `Normal` del mismo día a CLI-002 y HOS-001 | Primero CLI-002, el más cercano a BOD-UIO | Pasa |
| El de HOS-001 pasa a `Critical` | HOS-001 antes que CLI-002 aunque esté más lejos | Pasa |
| Punto con ventana 09:15–11:45 y 25 min | La entrega sale con esa ventana y ese servicio | Pasa |
| Punto sin ventana ni servicio | La entrega sale con 08:00–17:00 y 15 min | Pasa |
| Panel del asistente en `apps/web` | Sin cambios; misma forma de respuesta | No verificado en pantalla |

**Verificación:** `dotnet test --filter "DeliveryStopSequencerTests|RouteSuggestPriorityTests|RouteEvaluatorTests"`: 12 de 12. No se corrió la suite completa.

### T16 — Hora límite para puntos críticos (#193, 16-oct)

**Por qué:** la prioridad decidía quién entra al camión, pero no a qué hora llega. Un hospital crítico podía quedar como última parada. Es la pregunta que el sector dejó abierta en la charla del 16-oct (AMP-01).

**Regla (FR-24):** `PlanRequest.CriticalDeadlineLocal` es opcional (`"HH:mm"`). Una parada es crítica si su riesgo es `CRITICO` o la criticidad del punto es `alta`. Su fin de ventana es el mínimo entre la ventana del punto y la hora límite. Si la ventana del punto empieza después de la hora límite, se respeta la del punto. `RouteFeasibility` no cambia: ya rechaza con `time_window` las llegadas fuera de ventana.

**Fuera de alcance:** replanificar (`ReplanAsync`) no recibe hora límite; el selector en la página Rutas y la comparación de costos (AMP-02) van en otros issues.

| Archivo / Componente | Capa | Cambio | Por qué | Escenario | Estado |
|---|---|---|---|---|---|
| `PlanModels.cs` | Application | Campo opcional `CriticalDeadlineLocal` en `PlanRequest` | Recibir la hora límite sin romper llamadas existentes | Plan sin el campo | Hecho |
| `RoutePlanner.cs` | Application | `ParseCriticalDeadline` y `ApplyCriticalDeadline` al crear cada `StopCandidate` | Recortar solo la ventana de los críticos | Plan con `"11:00"` | Hecho |
| `RoutePlansController.cs` | API | Hora mal formada → 400 `invalid_critical_deadline` | Era un 500 por error del cliente | `"once"` | Hecho |
| `RoutePlannerTests.cs` | Pruebas | Recorte, ventana que termina antes, ventana que empieza después, no crítico, sin límite, formato, `time_window` | Regla aislada | — | Pasan |
| `RoutePlanningTests.cs` | Pruebas | Crítico llega antes de las 11:00; con jornada 10:00 y límite 10:00 el crítico queda `time_window` y el no crítico sigue en ruta | Regla con base real (PostGIS) | — | Pasan |

**Escenarios de prueba:**

| Escenario | Esperado | Estado |
|---|---|---|
| Sin `criticalDeadlineLocal` | El plan del fixture no cambia (prueba determinista existente) | Pasa |
| Crítico con límite `"11:00"` y jornada 07:00 | HOS-01 llega antes de las 11:00, sin retraso | Pasa |
| Crítico con límite `"10:00"` y jornada 10:00 | HOS-01 en `unserved` con `time_window` | Pasa |
| No crítico (riesgo `ALTO`, criticidad `media`) con el mismo límite | HOS-02 conserva 08:00–17:00 y sigue en la ruta tras las 10:00 | Pasa |
| Ventana del punto que empieza después del límite | Se respeta la ventana del punto | Pasa (unitaria) |
| Valor `"once"` | `ArgumentException` en el motor; 400 en la API | Pasa la unitaria; el 400 no tiene prueba HTTP |

**Verificación:** `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~RoutePlanner|FullyQualifiedName~RoutePlanning"` → 20 de 20.
