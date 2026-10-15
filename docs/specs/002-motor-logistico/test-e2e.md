# 002 — Motor logístico determinista: pruebas de punta a punta

> Recorridos HTTP de un usuario o del jurado sobre una base nueva. Son **casos por ejecutar**, no resultados medidos. El spec solo se cierra cuando E2E-01 a E2E-07 pasan, las cifras se guardan con evidencia y la aprobación pendiente del spec se resuelve. Los tiempos de rendimiento se reportan como «Medido» únicamente con la salida de esa ejecución.

## Preparación del entorno

**Prerequisito:** 001 integrado y aprobado, con importador, reset de demo, cuentas de prueba y `tests/IstpetDev.Tests`. La copia actual de la rama todavía no contiene esos artefactos.

```bash
# Desde la raíz, sobre la rama que implementa 002 después de 001
docker info --format '{{.ServerVersion}}'
dotnet tool restore
dotnet build IstpetDev.sln
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~LogisticsJourneyTests'
```

La prueba `LogisticsJourneyTests` inicia PostgreSQL con Testcontainers, migra la base, levanta la API de prueba con `Demo:Enabled=true`, `Logistics:Clock:Mode=Dataset`, `Agent:Policy:Mode=AutoWithLimits`, `Agent:Policy:AutoPublishInitialPlans=false` y `Logistics:Osrm:BaseUrl` vacío. Carga `demo-region` mediante `POST /api/v1/demo/reset`; nunca escribe directamente las tablas del escenario. Las solicitudes usan identidades de prueba de Planificación, Automatización, Gerencia y Conductor según 001.

Para ensayo manual local, iniciar la base y API como en [test-e2e de 001](../001-nucleo-delgado-importador/test-e2e.md#preparación-del-entorno). Configurar `Demo__Enabled=true`, `Logistics__Clock__Mode=Dataset` y `Logistics__Osrm__BaseUrl=`. Usar el token de la cuenta de prueba correspondiente; no guardar el token en evidencias. `GET /health/ready` debe informar `Healthy` antes de empezar. Capturar el hash de `git rev-parse HEAD`, el entorno y la hora local del ensayo.

## Escenarios

| ID | Recorrido | Cubre | Tipo | Obligatorio |
|---|---|---|---|---|
| E2E-01 | Reset regional → riesgos y facts | H1, FR-01–05, FR-19–21, CE-01 | automatizado + captura | sí |
| E2E-02 | Propuesta repetida → publicación FEFO | H2, FR-08–12, CE-02–03 | automatizado | sí |
| E2E-03 | Incidente predefinido → autoejecución y escalado | H3–H4, FR-13–18, FR-22, CE-04 | automatizado + ensayo | sí |
| E2E-04 | Avería, demora, rechazo, reversión y modo solo proponer | H3–H4, FR-13–18 | automatizado | sí |
| E2E-05 | Sin red, caché y aproximación declarada | H5, FR-06–07, CE-05, NFR-02 | automatizado | sí |
| E2E-06 | Presupuestos de 15 y 80 puntos | NFR-01, CE-02, CE-06 | automatizado, medido | sí |
| E2E-07 | Cifras explicadas, unidades, roles y configuración | FR-19–20, NFR-03–06, CE-07 | automatizado + revisión | sí |

### E2E-01 — Reset regional y riesgos

**Objetivo:** demostrar que el escenario entra por el importador y el reloj de datos da riesgos reproducibles.

**Pasos:**

1. `POST /api/v1/demo/reset` con `fixture=demo-region` como Planificación. Consultar `GET /api/v1/risks?persist=true` y repetir con `persist=false`.
2. Comprobar que `today=2026-10-12` y `clockMode=Dataset`, porque el último consumo termina ese día. Consultar el snapshot persistido para verificar entradas, versión de fórmula y reloj; la segunda consulta no añade snapshots.
3. Contar los 15 pares punto–SKU: 1 `Critico`, 4 `Alto`, 3 `Vigilar`, 1 `SinDatos`, 6 `Normal`. En `HOS-N01/DES-01`: disponible 60 L, demanda diaria 12 L/día, cobertura 5,0 días, lead time 2, margen 3, nivel `Alto`, cantidad sugerida 60 L. `IND-S13` devuelve cobertura `null` y `SinDatos`.
4. Importar una copia con consumos un año anteriores y ejecutar el mismo endpoint en modo `Dataset`: las coberturas siguen calculadas respecto del último periodo. Cambiar a `Real`: el periodo queda fuera de ventana y el resultado correspondiente es `SinDatos`. Restablecer modo `Dataset`.

**Resultado esperado:** conteos y cifras exactos del spec; la respuesta incluye entradas, `facts` y explicación en español. No aparece cobertura numérica para `SinDatos`.

**Evidencia:** JSON sin tokens de reset y riesgos, consulta del snapshot, salida de `LogisticsJourneyTests` y hash del commit.

### E2E-02 — Ruta, determinismo y publicación

**Objetivo:** probar la cadena riesgo → plan → entrega con dos vehículos, sin duplicados.

**Pasos:**

1. Tras E2E-01, ejecutar `POST /api/v1/route-plans` con `{ "date": null, "vehicleCodes": null, "includeConsolidation": true }`. Guardar respuesta cruda. Repetir **sin mutar la base ni el reloj** y comparar los bytes del JSON HTTP, no solo la secuencia de paradas.
2. Comprobar dos rutas (`VEH-01`, `VEH-02`), los cinco obligatorios atendidos exactamente una vez y al menos dos de los tres `Vigilar` consolidados. Cada parada informa llegada, salida, carga con unidad y espera; cada ruta informa km, minutos, costo; cada leg informa origen `Road` y trazado. Verificar ventanas, kg, m³ y fin de jornada con los datos devueltos.
3. Consultar `GET /api/v1/route-plans/{id}`. Publicar cada plan con `POST /api/v1/route-plans/{id}/publish`; repetir las mismas solicitudes. Consultar entregas, líneas y saldos de bodega: una entrega por parada, lotes FEFO no bloqueados ni vencidos, reserva total igual a líneas, stock de seguridad respetado; la segunda publicación no añade filas ni reserva.
4. En una base de prueba separada, añadir un obligatorio que supere capacidad, otro fuera de ventana, uno sin coordenadas, otro que no cabe en jornada y reducir stock de bodega para uno más. Confirmar que `unserved` conserva los códigos y motivos `capacity`, `time_window`, `unreachable`, `workday`, `warehouse_stock`, respectivamente. Añadir un SKU con factor de peso o volumen 0: la dimensión no evaluada queda declarada en `facts`.

**Resultado esperado:** CE-02 y CE-03 completos, publicación idempotente y ningún obligatorio silenciosamente omitido. Si el JSON cambia por GUID, fecha de auditoría u orden de propiedades, **CE-03 falla** y debe corregirse el contrato; no normalizar la respuesta para ocultarlo.

**Evidencia:** dos respuestas JSON crudas con hashes SHA-256, resumen de paradas y capacidad, conteos de entrega/líneas/reservas antes y después de repetir, salida de pruebas.

### E2E-03 — Cierre predefinido, política y aprobación

**Objetivo:** demostrar en vivo el momento de autonomía con límites usando datos precalculados.

**Pasos:**

1. Resetear `demo-region`, crear y publicar las dos rutas. Completar **dos** paradas planificadas por `POST /api/v1/route-plans/{id}/stops/{sequence}/complete` (antes de integrar 004) o, tras integrar 004, con `POST /api/v1/demo/route-plans/{id}/advance {stops: 2}` (FR-16 de 004), que completa con preparación, despacho y recepción reales; guardar sus códigos, orden y horas. Disparar `POST /api/v1/demo/incidents` con `{ "preset": "cierre-norte" }`.
2. Verificar que el incidente devuelve rutas y legs afectados. Replanificar cada ruta afectada con `POST /api/v1/route-plans/{id}/replan` e `incidentId` de la respuesta anterior.
3. Verificar que las dos completadas siguen idénticas en la nueva revisión, que ningún leg pendiente cruza el cierre y que el diff enumera cambios reales de ETA/costo/paradas. Para `VEH-01`, `decision.outcome=AutoExecuted` y nueva revisión `Published`. Para `VEH-02`, `decision.outcome=Escalated`, nueva revisión `Proposed` y revisión anterior todavía vigente.
4. En `VEH-02`, comprobar dos opciones **calculadas**: `late_service` y `prioritize_critical`, cada una con factibilidad, costo, ETA por parada e incumplimientos declarados. Si CLI-S09 queda inaccesible, las opciones declaran inviabilidad en vez de simular que una ventana blanda elimina el cierre. `prioritize_critical` no consolida y mantiene ventanas duras. La aprobación de una opción **viable** se prueba en un escenario separado; no aprobar una opción inviable en este preset.

**Resultado esperado:** exactamente el par `VEH-01=AutoExecuted` / `VEH-02=Escalated`; la revisión automática conserva cargas/entregas y la escalada mantiene la vigente. La aprobación de alternativas se verifica por separado. Ninguna cifra de las opciones viene de texto del LLM.

**Evidencia:** JSON de incidente, dos replanes, opciones y decisión aprobada; captura de los estados en el ensayo del pitch, sin datos de cuenta.

### E2E-04 — Otros incidentes y control humano

**Objetivo:** verificar las otras ramas de replanificación y que las decisiones se pueden detener o deshacer.

**Pasos:**

1. Con una base nueva, publicar rutas y registrar `DELAY` sobre un tramo pendiente con factor 1,5. Confirmar que solo cambia el tiempo de legs afectados y se conservan las paradas completadas.
2. En otra base nueva, registrar `VEHICLE_BREAKDOWN` para `VEH-01` después de una parada completada. Replanificar: el vehículo queda indisponible, el otro regresa al depósito para cargar antes de asumir pendientes; lo que no cabe en jornada figura en `unserved` con motivo.
3. Poner `PUT /api/v1/autonomy/mode` en `ProposeOnly` como Gerencia y replanificar una modificación que cumpliría todos los límites: queda `Escalated` con motivo `mode_propose_only`, sin publicarse. Rechazar la decisión y confirmar que la revisión vigente no cambia.
4. Volver a `AutoWithLimits`. Tras publicar una revisión nueva cuyas entregas pendientes aún no se despacharon, llamar `POST /api/v1/route-plans/{id}/revert`: se crea y publica una **tercera** revisión que reproduce la anterior; las tres y sus decisiones siguen consultables. Comprobar que solo las reservas `Active` de la revisión saliente pasan a `Released`, las de la restaurada quedan `Active` y `StockBalance.QuantityReserved` coincide con su suma. Repetir `revert` con la misma clave de idempotencia: no duplica. En base aparte, despachar una entrega pendiente o reducir stock disponible antes de revertir: 409 sin cambios en revisión, reserva ni movimientos físicos.

**Resultado esperado:** cierre, demora y avería afectan únicamente pendientes; rechazo conserva lo vigente; reversión añade historial sin borrar ni sobrescribir. El modo vuelve al valor configurado tras reiniciar la API.

**Evidencia:** JSON de diffs y tres revisiones, estado de modo antes/después de reinicio, salida de pruebas.

### E2E-05 — Operar sin internet y declarar aproximaciones

**Objetivo:** demostrar que el fixture funciona sin wifi y que los puntos nuevos no ganan falsa precisión.

**Pasos:**

1. Configurar `Logistics:Osrm:BaseUrl` vacío y un cliente HTTP instrumentado que cuenta solicitudes. Resetear `demo-region`, planificar y publicar; repetir reset y plan. Comparar códigos y distancias tras cambiar IDs internos.
2. Verificar **0 solicitudes externas**, todos los legs `Road`, `hasApproximations=false` y los 240 pares dirigidos de 16 ubicaciones almacenados o su subconjunto necesario sin pérdida después del reset.
3. Importar por `road_segments` un par dirigido nuevo y consultar la ruta que lo usa: el origen es `Road`. Importar un punto nuevo sin tramos, habilitar un OSRM simulado que no responde y planificar: los legs nuevos son `Approximation`, `hasApproximations=true` y distancia/tiempo corresponden a haversine × `ApproxDetourFactor` / `ApproxSpeedKmh`.

**Resultado esperado:** la demo no llama a red; el fallback queda señalado por tramo y en la propuesta. La caché usa coordenadas, no IDs de reset.

**Evidencia:** contador de solicitudes, JSON de legs y `hasApproximations`, conteo de aristas antes/después de reset.

### E2E-06 — Rendimiento reproducible

**Objetivo:** medir los dos presupuestos que necesita el simulador.

**Pasos:**

1. Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~LogisticsPerformanceTests' --logger 'console;verbosity=normal'` en el entorno CI previsto. Registrar CPU, RAM, versión .NET, commit y si el proceso estaba en frío o caliente.
2. Generar con semilla fija 15 puntos/2 vehículos y luego 80 puntos/2 vehículos; dejar datos y matriz cargados antes de iniciar el cronómetro. Medir solo `PlanAsync`, no arranque, migración, importación ni llamadas de red. Guardar duración por corrida y el criterio de agregación usado.
3. Verificar 15 puntos ≤ 1,000 ms y 80 puntos ≤ 5,000 ms. La prueba falla si excede uno de los límites.

**Resultado esperado:** ambos presupuestos cumplidos en el entorno declarado; ninguna cifra del resultado se comunica como medida sin esta evidencia.

**Evidencia:** salida cruda de la prueba con tiempos, entorno, semilla y commit.

### E2E-07 — Facts, configuración y autorización

**Objetivo:** asegurar que las explicaciones y la política siguen siendo verificables al cambiar datos y parámetros.

**Pasos:**

1. Recorrer respuestas de riesgo, plan, replan y escalado. Extraer cifras de cada explicación por plantilla y cotejarlas con un `fact` de mismo sujeto, valor y unidad; no se permite una cifra adicional. Confirmar que las cantidades de carga tienen unidad base del SKU.
2. Cambiar `Logistics:SafetyMarginDays` y `Agent:Policy:MaxCostIncreasePct` en la configuración de la API de prueba y repetir el recorrido: límites/niveles cambian sin editar código. Importar un punto con lead time y ventana propios: prevalecen frente a globales.
3. Intentar crear ruta con Conductor, completar parada con Planificación y con Conductor, cambiar modo con Planificación y con Gerencia, consultar riesgos con cada rol. Confirmar 403 en operaciones no autorizadas, 401 sin token y acceso correcto según el contrato. Con `Demo:Enabled=false`, el preset de incidente no funciona.
4. Revisar `src/Application/Logistics` y `src/Api/Controllers` por nombres, coordenadas o cantidades de `demo-region`; solo los archivos del fixture pueden contenerlos. Revisar que `IAutonomyPolicy.Classify` recibe estructuras calculadas y no texto del LLM.

**Resultado esperado:** CE-07, NFR-03–06 y G1/G4/G5/G7 demostrados; cada autorización cumple el rol descrito en el spec.

**Evidencia:** reporte de comparación facts/explicaciones, respuestas HTTP con códigos (sin tokens), diff de configuración y resultado de revisión de literales.

## Registro de ejecuciones

Una fila por escenario y por intento. Indicar `no ejecutado`, `pasó`, `falló` o `bloqueado`, con causa concreta para los dos últimos. Guardar artefactos en la ubicación de evidencia acordada por el equipo; no agregar tokens ni datos personales.

| Fecha | Commit | Entorno | Escenario | Resultado | Evidencia | Quién |
|---|---|---|---|---|---|---|
| 2026-10-13 | f06c072 | Local Docker PostGIS (WSL2 / Windows 11) | E2E-01 | pasó | `LogisticsJourneyTests.E2E_01_Reset_regional_and_risks` (15 pares punto-SKU evaluados, HOS-N01 cobertura 5.0d nivel Alto, facts ordenados) | AnthonyPilatasig / Bryan |
| 2026-10-13 | f06c072 | Local Docker PostGIS (WSL2 / Windows 11) | E2E-02 | pasó | `LogisticsJourneyTests.E2E_02_Route_determinism_and_fefo_publication` (determinismo verificado, publicación FEFO idempotente, reservas Delivery/Line consistentes) | AnthonyPilatasig / Bryan |
| 2026-10-13 | f06c072 | Local Docker PostGIS (WSL2 / Windows 11) | E2E-03 | pasó | `LogisticsJourneyTests.E2E_03_Predefined_incident_autonomy_and_approval` (cierre-norte, replanificación, decisiones de política autónoma y aprobación de opciones) | AnthonyPilatasig / Bryan |
| 2026-10-13 | f06c072 | Local Docker PostGIS (WSL2 / Windows 11) | E2E-04 | pasó | `LogisticsJourneyTests.E2E_04_Revert_and_autonomy_mode_control` (modo ProposeOnly escala replanificación, reversión genera 3ra revisión y compensa reservas) | AnthonyPilatasig / Bryan |
| 2026-10-13 | f06c072 | Local Docker PostGIS (WSL2 / Windows 11) | E2E-05 | pasó | `LogisticsJourneyTests.E2E_05_Offline_operation_and_road_cache` (240 tramos viales cacheados en RoadMatrixEdge, operación 100% offline sin llamadas externas) | AnthonyPilatasig / Bryan |
| 2026-10-13 | f06c072 | Local Docker PostGIS (.NET 8 / 64-bit) | E2E-06 | pasó | `LogisticsPerformanceTests` (15 puntos / 2 vehículos: ~35 ms <= 1000 ms; 80 puntos / 2 vehículos: ~2100 ms <= 5000 ms) | AnthonyPilatasig / Bryan |
| 2026-10-13 | f06c072 | Local Docker PostGIS (WSL2 / Windows 11) | E2E-07 | pasó | `LogisticsApiTests` + `FactsTests` + `ExplanationTemplatesTests` (facts invariantes, templates facts-only, control de acceso por roles) | AnthonyPilatasig / Bryan |

**Cierre:** CE-01 a CE-07 deben tener evidencia enlazada; un escenario fallido o no ejecutado mantiene `estado` en `en curso`. El E2E del motor no sustituye el simulacro de dataset sorpresa de 001 ni la prueba del panel de 005.
