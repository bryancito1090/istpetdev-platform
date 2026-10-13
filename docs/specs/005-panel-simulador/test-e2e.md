# 005 — Panel web y simulador comparativo de 90 días: pruebas de punta a punta

> Recorridos que una persona debe poder repetir desde la web y la API una vez implementados 001–005. **Estado actual: no ejecutado.** No presentar cifras como «medidas» ni cerrar el spec por haber redactado este documento. Guardar evidencia del commit, dataset, matriz, seeds y entorno. Los resultados de 90 días son sintéticos, no observaciones de una empresa.

## Preparación del entorno

**Requisitos:** 001/002/003/004 integrados en `develop` para la prueba completa; `Demo:Enabled=true` **solo** en entorno de demo; conexiones PostgreSQL operativa y simulación distintas; matriz homogénea de origen permitido y precargada; `demo-region` para el panel y dataset nacional canónico para la simulación. La web usa el login PKCE de 004. No ejecutar reset/importación contra producción ni escribir secretos en el repositorio.

```bash
# Desde la raíz, en la rama/commit que se ensaya.
git rev-parse HEAD
dotnet --version
npm run check
dotnet test tests/IstpetDev.Tests
python docs/boveda/.github/scripts/check_vault.py
```

Registrar salida, versión de navegador y teléfono, CPU/memoria del equipo, URL del entorno (sin token), modo vial o aproximado, versión/hash de matriz, `datasetVersion`, `policyVersion` de las tres políticas y commit. Iniciar sesión como Gerencia/Planificación antes del ensayo; Conductor y Automatización se usan solo para la matriz de permisos. Variables de shell **locales** para HTTP, sin volcarlas a logs:

```bash
: "${API_BASE:?URL base de la API de demo}"
: "${PLAN_TOKEN:?access token de Planificación, solo en entorno local}"
: "${DATASET_VERSION:?versión del dataset nacional importado}"
curl -fsS -H "Authorization: Bearer $PLAN_TOKEN" "$API_BASE/api/v1/risks"
```

Para la corrida de aceptación usar tres seeds base fijadas **antes** de observar resultados (por ejemplo `11,22,33`) y una adversa (`44`), horizon 90 días y los tres `policyVersion` aprobados. El ejemplo de números de seed es un parámetro de ensayo, no parte del código. Abrir `data/fixtures/simulation-national/scenario.json` y comprobar regiones, flota, vehículo retirado, matriz y supuestos. Exportaciones y capturas se guardan en un directorio privado de evidencia con manifiesto; publicar en la bóveda solo resúmenes sin tokens, datos personales ni fotos/firmas.

## Escenarios

| ID | Escenario | Cubre | Tipo | Obligatorio |
|---|---|---|---|---|
| E2E-01 | Panel vivo sin cifras fijas | H1, FR-01–02, FR-14, FR-16, NFR-05, CE-01–02 | navegador + API | sí |
| E2E-02 | Dos columnas, acciones, modo y traza | H2, FR-03–05, FR-18, CE-03 | navegador + API | sí |
| E2E-03 | Avance real e incidente | H1–H2, FR-14/18, CE-02/08 | navegador + API + 004 | sí |
| E2E-04 | Generador → importador aislado | H3, FR-06/17/19, G1–G4, NFR-03 | integración + BD | sí |
| E2E-05 | Tres políticas y calendario común | H3, FR-07–09/13/20, CE-04 | integración | sí |
| E2E-06 | KPI, rango y denominadores | H3, FR-12/15, CE-05 | API + web | sí |
| E2E-07 | Exportación y replay | H4, FR-10–11, NFR-02, CE-06 | HTTP + archivos | sí |
| E2E-08 | Escenario adverso y límites de recursos | H3, FR-13/19–20, CE-04 | integración | sí |
| E2E-09 | Corrida completa antes de H4, en máquina dedicada, e importación verificada | NFR-01, FR-21, CE-07, CE-09 | benchmark | sí |
| E2E-10 | Error/red/permisos/responsive | FR-02/04–05/10/12/16, NFR-03–05 | navegador + HTTP | sí |

### E2E-01 — Panel regional conectado

**Objetivo:** mostrar el recorrido `demo-region` como estado operativo, sin datos de maqueta.

**Pasos:**

1. Con Planificación, ejecutar `POST /api/v1/demo/reset` con fixture configurado `demo-region`; anotar `ImportBatch`/reporte. `GET /api/v1/service-points` debe listar los puntos del fixture; no usar el dataset nacional en esta vista.
2. Abrir `/dashboard` con sesión PKCE de 004. Comparar las tarjetas de riesgo con `GET /api/v1/risks`, la lista de rutas con `GET /api/v1/route-plans`, el detalle de cada revisión con `GET /api/v1/route-plans/{id}` y las entregas con `GET /api/v1/deliveries?routePlanId=...`. Los IDs se descubren por las listas, no están escritos en la web.
3. Ver el mapa: un leg `Road` usa geometría y línea continua; forzar en entorno de prueba un leg `Approximation` y comprobar línea punteada, etiqueta de fuente y `fitBounds` sobre puntos devueltos. Desactivar carga de mosaicos externos: la lista de paradas/ETA/costo sigue visible.
4. Reiniciar con otro fixture canónico de seis puntos y valores distintos; refrescar sin recompilar. Consultar API vacía: no aparece ahorro, punto, vehículo, incidente ni coordenada del fixture anterior. Inspeccionar la web buscando literales de R-15.

**Resultado esperado:** panel y API coinciden por punto/SKU/ruta/revisión/entrega; recorrido en vivo muestra ≈15 puntos y dos vehículos con `demo-region`, y red nacional solo en `/simulation`. Ninguna cifra fija se muestra como resultado; si no hay datos, aparece estado vacío.

**Evidencia:** capturas de panel/mapa, JSON de las cuatro lecturas sin tokens, lista de leg sources y resultado de `rg -n '418 km|24\.8%|defaultPoints|deliveryStops|Alóag Cerrada' apps/web/src`.

### E2E-02 — Decisiones y control humano

**Objetivo:** comprobar autonomía limitada y correlación con la traza real.

**Pasos:**

1. Restablecer `demo-region`, iniciar `POST /api/v1/agent/runs` tipo `watch` desde el panel y conservar `agentRunId`. Consultar `GET /api/v1/agent/runs/{id}` y `GET /api/v1/agent-decisions`.
2. Provocar una decisión `AutoExecuted` y una `Escalated` mediante el incidente calibrado de 002/003. En el panel, verificar columnas «Ejecutadas solas» y «Esperan tu aprobación», motivos, `facts`, origen `llm`/`template`, opción factible y revisión.
3. Aprobar una opción escalada, rechazar otra en un ensayo restablecido y deshacer una revisión publicada. Comparar cada respuesta con estado de decisión y revisión nueva en 002; comprobar reservas compensadas solo si siguen activas, sin liberar stock despachado.
4. Cambiar `PUT /api/v1/autonomy/mode` a `ProposeOnly` y confirmar por GET que la siguiente propuesta se escala; restaurar modo anterior. Simular 403/409: interruptor/acción vuelve al estado confirmado y muestra error. Abrir traza de 003 por `agentRunId`.

**Resultado esperado:** columna/estado reflejan API; el LLM no publica ni aprueba. No hay explicación numérica fuera de `facts`; el panel no ejecuta cálculos de ruta o clasificación.

**Evidencia:** capturas de ambas columnas y error, IDs de decisiones/runs/revisiones, JSON de `facts`/traza y conteo de reservas antes/después, sin secretos.

### E2E-03 — `advance`, incidente y ruta revisada

**Objetivo:** encadenar el momento de la demo sin el endpoint provisional.

**Pasos:**

1. Restablecer `demo-region`, crear/publicar ruta de dos vehículos. Registrar saldos, entregas, paradas y reservas antes de avanzar.
2. Como Planificación, `POST /api/v1/demo/route-plans/{id}/advance` con `operationId` nuevo y `stops:2`. Verificar dos recibos/custodias/movimientos reales; repetir misma operación: no duplica efectos.
3. Disparar `POST /api/v1/demo/incidents` con preset del fixture. Abrir ruta afectada y no afectada; la revisión nueva conserva las dos paradas completadas y no mueve las del otro vehículo. Panel muestra decisiones y ETA/costo del cálculo real.
4. Intentar `/route-plans/{id}/stops/{sequence}/complete` provisional tras integrar 004: no crea recepción ni saldo del punto.

**Resultado esperado:** CE-08; stock y custodia concilian, y el panel corresponde a la revisión vigente de 002/004.

**Evidencia:** saldos antes/después, IDs de recibos/movimientos, paradas, decisiones y capturas del mapa revisado.

### E2E-04 — Una puerta de datos y aislamiento

**Objetivo:** probar G2 y que el simulador no contamina la demo.

**Pasos:**

1. Tomar hash/consulta de saldos, entregas y decisiones de la BD operativa con `demo-region`. Identificar nombre de base operativa y `ConnectionStrings:Simulation` sin imprimir credenciales; deben diferir.
2. Ejecutar el generador con `N=6,D=3,seed=11` y revisar siete CSV canónicos de 001, `road_segments` de 002, `mapping.json`, manifiesto y SHA-256. Importar mediante el servicio canónico de 001 en la base de simulación; verificar `ImportBatch` y reporte de supuestos. Repetir seed/versión: bytes y hashes iguales.
3. Ejecutar una política y forzar un fallo a mitad; cancelar/reiniciar worker. Comparar el hash operativo del paso 1. Intentar configurar ambas cadenas a la misma base en entorno desechable: POST de comparación rechaza antes de importar.
4. Volver a cargar `demo-region` y usar `advance`; las métricas/ledger de una comparación completada permanecen. Una limpieza explícita de resultados de simulación se ensaya aparte, fuera de este reset operativo.

**Resultado esperado:** ningún dato canónico del generador entra por inserción privada, la base operativa conserva su huella tras ejecución/fallo y los resultados completados no se mezclan con el reset.

**Evidencia:** nombres de bases sin credenciales, manifiesto/hashes, reporte de importación y hashes operativos antes/después.

### E2E-05 — Reglas congeladas y calendario común

**Objetivo:** detectar cualquier ventaja artificial por conocer el futuro o cambiar recursos.

**Pasos:**

1. Antes de correr, guardar `reactive-v1`, `fixed7-v1`, `istpetdev-v1`, costos, matriz, seis regiones/flota y seeds en manifest. Iniciar comparación de seed 11 con tres políticas.
2. Comprobar mismo `calendarSha256`, stock/bodega/flota/ventanas/costos iniciales para los tres runs. Los pedidos fijos ocurren en días 0,7,14… y mantienen cantidad `7 × media previa`; un pico entre visitas no cambia el pedido. El reactivo avisa al bajar de `leadTimeDays × media conocida` y pide hasta 14 días sujeto a restricciones.
3. Insertar un evento exógeno a las 10:00 con entrega a las 12:00: quiebre a las 10:00 permanece; entrega a las 09:00 en ensayo nuevo lo evita. Un incidente conocido a las 11:00 no modifica decisión de las 09:00. En el adverso, aplicar igual calendario base transformado, vehículo retirado y lead time a las tres políticas.
4. Revisar balance de lote/ubicación, capacidad/jornada/ventanas y `requested = satisfied + unmet` por día. Provocar violación deliberada: run `Failed`, sin KPI comparativo de esa seed.


**Paridad de ruteo y calendario escalonado (CE-10):** con una red de prueba, comprobar que `fixed7-v1` visita cada punto una vez por semana en su día laborable asignado, y que para un mismo conjunto de paradas de un día y región las tres políticas obtienen la misma secuencia del planificador de 002.

**Resultado esperado:** las políticas comparten demanda y recursos por seed/escenario, no revelan futuro ni mezclan fuentes viales/aproximadas dentro de un run; una regla cambiada exige nueva versión y nueva corrida de las tres políticas.

**Evidencia:** tres manifest/runs, hashes de calendario/recursos, eventos horarios y reporte de invariantes.

### E2E-06 — Indicadores comprensibles y auditables

**Objetivo:** cotejar las fórmulas de [Simulador y métricas](../../boveda/04%20Datos%20y%20algoritmos/Simulador%20y%20metricas.md) con datos/eventos exportados.

**Pasos:**

1. Abrir `/simulation` con tres seeds base terminadas; seleccionar reactivo y calendario fijo por separado. La portada muestra quiebres evitados **y nuevos**, demanda no atendida por SKU/unidad, costo de transporte y puntualidad. Cada tarjeta abre numerador, denominador, fórmula/versión, rango min–mediana–max y seed.
2. En un fixture controlado: baseline quiebra claves A/B y plataforma B/C. Debe mostrar 1 evitado, 1 nuevo, diferencia neta 0. Con 3 obligaciones en ventana de 4 vencidas, puntualidad 3/4 = 75 %. Con baseline 100 km y plataforma 80 km, ahorro 20 km y reducción 20 %.
3. Forzar baseline de 0 km, 0 entregas y 0 obligaciones: tasas y km/costo por entrega muestran N/A con `zero_denominator`; no hay porcentaje infinito ni 100 % por convención. Forzar 1 L y 1 kg de demanda no atendida: dos líneas por SKU/unidad, sin total mezclado.
4. Confirmar km incluye regreso, costo usa mismo modelo para las tres políticas y plataforma no es descrita como óptimo global. Trazabilidad simulada sin custodia aparece N/A `not_simulated`; la operativa depende de evidencia confirmada en 004.

**Resultado esperado:** CE-05; las cifras de pantalla se recalculan desde eventos/metrics del run, no del widget ni de texto del LLM.

**Evidencia:** captura de cuatro tarjetas y detalles, cálculo manual de ejemplo, `metrics.json` y `eventos.csv`.

### E2E-07 — Exportación y repetición

**Objetivo:** probar que un jurado puede rastrear la cifra hasta el run.

**Pasos:**

1. Con comparación en `Running`, solicitar `/api/v1/simulations/comparisons/{id}/export`: 409. Al quedar `Completed`, descargar ZIP como Gerencia/Planificación. Sin JWT y como Conductor: 401/403.
2. Confirmar archivos `manifest.json`, `metrics.json`, `resumen.csv`, `eventos.csv` y calendario; verificar SHA-256 de cada archivo de datos contra manifest. Confirmar `ETag` igual al SHA-256 de `manifest.json` (el manifest no incluye su propia huella). `comparisonId`, seed, dataset/matriz/policyVersion/código/definiciones y supuestos coinciden con pantalla.
3. Repetir corrida con mismos archivos, parámetros, seeds y código en base aislada; ordenar eventos canónicamente y comparar hashes/métricas. Repetir con seed distinta: calendario cambia. Comparar `quiebres evitados` por claves, no solo diferencia de conteos.

**Resultado esperado:** CE-06; misma entrada reproduce eventos/métricas, otra seed altera calendario, sin archivos con datos privados del teléfono/QR.

**Evidencia:** ZIP privado, manifest/hashes, ETag, comparación de dos ejecuciones y capturas con IDs.

### E2E-08 — Escenario adverso con los mismos límites

**Objetivo:** mostrar un límite honesto de la propuesta sin elegir el mejor resultado.

**Pasos:**

1. Correr seed adversa fijada (44) sobre calendario base con demanda ×1,20, `leadTimeDays +1` y un vehículo retirado por código/región predeterminados. Las tres políticas reciben exactamente esas alteraciones.
2. Verificar que ninguna planifica vehículo retirado ni cruza regiones/pernocta, y que faltantes/capacidad/ventanas inviables quedan con motivo. Un run sin matriz vial completa falla o se ejecuta como escenario íntegramente aproximado y etiquetado, nunca mezcla legs.
3. Abrir `/simulation`: adverso aparece apartado del rango de las tres seeds base; no se ocultan quiebres nuevos, demandas pendientes ni costo mayor. El manifest registra el vehículo retirado, matriz y parámetros.

**Resultado esperado:** CE-04 y FR-13/19/20; se puede explicar por qué cambia el servicio/costo sin afirmar un ahorro garantizado.

**Evidencia:** manifest adverso, lista de no atendidos, detalle de KPI y captura de separación base/adverso.

### E2E-09 — Presupuesto completo y H4

**Objetivo:** medir la corrida que realmente necesita el pitch.

**Pasos:**

1. En la **máquina dedicada** de la corrida oficial (FR-21; nunca en la instancia que sirve la demo), con matriz precargada y red externa deshabilitada, ejecutar `bash scripts/simulation/benchmark.sh` para tres seeds base y una adversa; guardar commit/CPU/memoria y hora de inicio/fin. Verificar 12 runs (4 seeds/escenarios ×3 políticas), 1080 jornadas de política y número real de llamadas regionales.
2. Registrar p50/p95 por llamada, tiempo por importación, planificación, persistencia y exportación, y total. La cota de 002 por llamada no sustituye este resultado. Si supera 60 min o falla, perfilar y repetir tras corregir; conservar tanto el fallo como el nuevo intento.
3. Importar el ZIP en el entorno de demo con `POST /api/v1/simulations/comparisons/import`: se acepta y el panel muestra host, commit y fecha de cálculo. Repetir con una copia alterada (un byte cambiado en `metrics.json`): 422 sin escribir nada (CE-09). Confirmar exportación y vista listas antes de H4 (15-oct 11:00). Conservar un run verificado para el pitch sin afirmar que se acaba de ejecutar en escenario; cualquier cambio de política/dataset/código después exige nueva corrida y manifest.

**Resultado esperado:** CE-07 solo si total medido ≤60 min, doce runs completos y evidencia antes de H4. Si no, registrar bloqueo real; el simulador no tiene plan B de cifras.

**Evidencia:** salida íntegra del benchmark, IDs, tiempos, hardware, manifest y marca horaria.

### E2E-10 — Sesión, fallos, frescura y proyector

**Objetivo:** evitar una pantalla convincente con datos obsoletos o permisos incorrectos.

**Pasos:**

1. En `/dashboard` y `/simulation`, simular API 401/403/409/503 y pestaña oculta: no se duplica acción, la consulta de 5 s se pausa oculta y se reanuda visible; último snapshot conserva hora y etiqueta «desactualizado». Un `PUT` fallido de `ProposeOnly` revierte el interruptor.
2. Vencer sesión PKCE: se usa el guard/interceptor de 004, sin segundo login ni token en almacenamiento del simulador. Como Automatización/Conductor, intentar POST/GET de comparaciones según matriz de roles: 403; sin token: 401.
3. Abrir a 1280 px en proyector y a 390 px móvil. Comprobar cuatro KPI, unidades, comparador, N/A, foco visible y nombres de botones. Navegar con teclado y sin distinguir colores; estado de ruta/decisión sigue entendible.
4. Bloquear OSM y red del recinto: el mapa puede perder mosaicos, pero lista de paradas y resultados persistidos siguen accesibles en el stack/hotspot de ensayo; no atribuir tráfico actual a la matriz guardada.

**Resultado esperado:** FR/NFR de sesión, error y accesibilidad sin datos falsos; el panel indica cuándo se actualizó por última vez.

**Evidencia:** capturas 1280/390, matriz de códigos HTTP, registro de polls y lista de control accesible sin tokens.

## Registro de ejecuciones

Una fila **por escenario e intento**, con `no ejecutado`, `pasó`, `falló` o `bloqueado`; si falla, consignar causa y commit de corrección en nueva fila. Usar [Plantilla evidencia](../../boveda/09%20Plantillas/Plantilla%20evidencia.md). No subir tokens, URLs privadas, fotos, firmas ni dumps completos de datos personales. Los porcentajes de la simulación se citan como «simulados» y solo como «medidos en la corrida» si enlazan manifest/ejecución.

| Fecha/hora (America/Guayaquil) | Commit | Dataset/matriz/seed | Escenario | Resultado | Evidencia | Responsable |
|---|---|---|---|---|---|---|

**Cierre:** E2E-01–E2E-10 y CE-01–CE-08 requieren evidencia. El spec permanece `borrador` hasta Deciders; redactar E2E no cambia su estado. El equipo solo lo marca `cerrado` después de pruebas pasadas y revisión del manifiesto de impacto.
