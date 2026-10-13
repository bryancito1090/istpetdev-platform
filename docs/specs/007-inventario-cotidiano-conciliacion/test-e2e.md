# 007 — Inventario cotidiano y conciliación: pruebas de punta a punta

> **Estado: sin ejecutar.** Este documento define resultados esperados, no medidos. Implementar los tests de [task.md](task.md), ejecutar sobre el commit integrado y registrar evidencia antes de cerrar criterios de [spec.md](spec.md). Convención de nombre del repositorio: `test-e2e.md`.

## Preparación y límites

### Campaña automatizada local

Requiere .NET SDK del `global.json`, Docker disponible para `PostgisContainer`, restauración NuGet y nuevas clases T1–T12. Cada test crea una base descartable con `postgres.NewDatabase()` y usa `ApiFactory`; no configurar una conexión compartida ni ejecutar `database update` contra integración. `InventoryScenario` de T1 carga el fixture mediante el importador/reset sintético antes de la primera operación piloto. Para E2E-08 integrar primero el backend real de 004.

```bash
git rev-parse HEAD
dotnet tool restore
docker info --format '{{.ServerVersion}}'
dotnet build IstpetDev.sln
dotnet test tests/IstpetDev.Tests --filter 'Spec=007&Category=Unit' --logger 'trx;LogFileName=Unit.trx' --results-directory artifacts/spec007/acceptance
dotnet test tests/IstpetDev.Tests --filter 'Spec=007&Category=Integration' --logger 'trx;LogFileName=Integration.trx' --results-directory artifacts/spec007/acceptance
dotnet test tests/IstpetDev.Tests --filter 'Spec=007&Category=E2E' --logger 'trx;LogFileName=E2E.trx' --results-directory artifacts/spec007/acceptance
python3 scripts/db/check-test-results.py artifacts/spec007/acceptance
npm run build:shared
npm run lint:web
npm run build:web
npm run build:mobile
```

Antes de implementar, esos filtros todavía no encontrarán tests 007 y **no constituyen una verificación aprobada**. El verificador de TRX exige casos ejecutados, todos aprobados y ninguno omitido en cada categoría. Clasificar fallo de Docker/red como error de entorno, no resultado funcional de un escenario.

### Datos y cuentas del fixture

| Dato | Valor sintético base |
|---|---|
| Organización | La organización de ensayo provisionada por 001, nunca un tenant enviado libremente por el cliente |
| Punto / ubicación | `HOS-001`, ubicación del mismo punto; segundo punto `CLI-002` para denegación |
| SKU / unidad / lote | `DES-01` / L / `L-DES-2610`, utilizable según reloj de prueba |
| Saldo inicial punto | 60.000 L físicos, 0.000 L reservados |
| Bodega | `BOD-UIO`, saldo 800.000 L del mismo lote |
| Historia | 84.000 L desde 2026-10-05, período de 7 días (12.000 L/día), fila importada |
| Corte de inventario | 2026-10-12T00:00:00Z declarado por fixture; secuencia generada por servidor |
| Reloj base | Fixed 2026-10-13, zona `America/Guayaquil`; usar configuración explícita de ventana de 7 días en tests de demanda |
| Cuentas | Receptor A asignado a HOS-001, Receptor B asignado a CLI-002, Bodega A asignado a BOD-UIO, Planificador, Conductor y cuenta revocada |

Los UUID/versiones se descubren por consulta, no se escriben constantes que puedan apuntar a otra base. Cada escenario parte de un fixture independiente; los cálculos no se encadenan entre escenarios salvo pasos explícitos. Crear cuentas/asignaciones mediante soporte de tests o provisionamiento autorizado; una claim inventada por el test no sustituye la tabla de cuentas real.

### Campaña PWA/Web y teléfono

Requiere instancia de ensayo autorizada con datos sintéticos, API y PWA publicadas por HTTPS, Cognito/PKCE efectivo, cuentas/asignaciones reales y API de inventario sin mocks. Aplicar prelaunch del repositorio a ese entorno y comprobar `/health/ready`. No iniciar despliegue como efecto de leer este plan. Anotar origen web/móvil, commit de API y frontend, dispositivo, navegador, reloj y red; configurar `Inventory:PilotEnabled=true`, rol/asignaciones del servidor y `Demo:Enabled=false` después de cargar el fixture.

Para simulación offline instalar la PWA antes, abrirla con red y cargar catálogo/saldo propios. `ng serve` y el botón que simula desconexión no sustituyen el modo avión y reinicio real. Las evidencias contienen códigos sintéticos e IDs de operaciones, sin access tokens, cadenas de conexión, fotos de personas ni URLs privadas.

## Matriz de escenarios

| ID | Recorrido | Cubre | Medio | Obligatoriedad |
|---|---|---|---|---|
| E2E-01 | Captura válida, replay y error de payload | FR-01/02, CE-01 | HTTP + PostgreSQL | MVP |
| E2E-02 | Dos consumos compiten por saldo/reservas | NFR-02, CE-02 | HTTP concurrente + PostgreSQL | MVP |
| E2E-03 | Historia y dos consumos del mismo día | FR-03/12, CE-03/10 | HTTP + cálculo real de 002 | MVP |
| E2E-04 | Cero, día parcial, cierre tardío y riesgo | FR-07/12, CE-05/10 | HTTP + Web | MVP |
| E2E-05 | Reversión y conciliación temporal | FR-04/05, NFR-03, CE-04 | HTTP + Web/PWA | MVP |
| E2E-06 | Cuenta, asignación y revocación | NFR-01, CE-06 | JWT real de tests + API | MVP |
| E2E-07 | Reimportación protegida | FR-10, CE-08 | Importador HTTP + PostgreSQL | MVP |
| E2E-08 | Recepción/devolución de 004 y reservas de 002 | FR-06, CE-02/04 | HTTP real de 001/002/004/007 | MVP; dependiente de 004 |
| E2E-09 | PWA sin red, reinicio y cambio de cuenta | FR-11, CE-09 | Teléfono instalado + API real | MVP |
| E2E-10 | Web, paginación y fechas/fuentes | FR-04/07, NFR-03 | Navegador + API | MVP |
| E2E-11 | Entrada, faltante y solicitud | FR-08, CE-11 | HTTP + PWA/Web | Ampliación |
| E2E-12 | Diferencia investigada y cierre humano | FR-09, CE-07 | HTTP + PWA/Web | Ampliación |
| E2E-13 | Rendimiento, migración y regresión | NFR-04 y conservación | PostgreSQL + suite base | Piloto; volumen por acordar |

## E2E-01 — Captura y replay

1. Como Receptor A, consultar saldo 60/reserva 0 y su versión. Capturar 10.000 L del lote utilizable con `operationId=Idempotency-Key` nuevos y hora 2026-10-12T15:00:00Z.
2. Esperar 201 y guardar IDs del consumo/movimiento, saldo 50.000, versión nueva y secuencia. Ver un consumo operativo, un movimiento de salida y un registro idempotente.
3. Repetir tres veces clave/cuerpo originales, incluso con la versión original ya vieja: mismo 201/cuerpo/IDs, saldo 50 y conteos sin cambio.
4. Misma clave con cantidad 11.000: 409 `IDEMPOTENCY_CONFLICT`; nueva clave con cantidad 61.000 y versión vigente: 422 `INSUFFICIENT_STOCK`. Probar cero, cuatro decimales y unidad incorrecta: 400. Ninguna escritura parcial.

**Evidencia:** `artifacts/spec007/E2E-01/` con respuestas JSON sin tokens, saldos/conteos SQL de test y TRX de `ConsumptionApiTests`/`InventoryJourneyTests`.

## E2E-02 — Concurrencia y reservas

1. Desde saldo 60 y una misma versión, dos clientes de la cuenta autorizada generan claves distintas y piden 40 cada uno. Disparar solicitudes concurrentes con contextos distintos, sin secuencia artificial.
2. Debe haber exactamente una aceptación 201 y un conflicto 409; saldo 20.000 y un solo movimiento de 40. Repetir ambas peticiones: la aceptada se reproduce; la rechazada no se convierte en segunda salida.
3. Nuevo fixture con saldo 60/reserva 55; pedir 10 con versión vigente: 422, ambos saldos intactos. Publicación de reserva 30 y consumo 40 compitiendo requieren resultado que preserve `reserved <= onHand`; probarlo con el escritor real de 002 en E2E-08.
4. Falla de persistencia inyectada antes del commit: ningún consumo/movimiento/respuesta idempotente nuevo y saldo original. Pérdida de respuesta posterior al commit: replay recupera el resultado original.

**Evidencia:** operaciones, barrera de arranque de tests, códigos, balance final y TRX de `InventoryConcurrencyTests`. Un único proceso serial de prueba no acredita concurrencia.

## E2E-03 — Fuentes que comparten un día

1. Variación de fixture: reloj Fixed 2026-10-12, ventana de 7 días `[2026-10-05,2026-10-12)`. Mantener historia 84/7 y saldo 60. Capturar 4 y 6 con fecha de negocio 2026-10-05 en dos claves/versiones sucesivas; la ventana de captura de ensayo permite esas fechas.
2. Consultar: historia intacta, dos consumos operativos, saldo 50. Antes de cierre, demanda 12.000 con aviso de día parcial y fuente histórica; las capturas físicas ya restaron 10.
3. Cerrar 2026-10-05 con versión actual del día. Para demanda, ese día vale 10 y los otros seis 12: total 82, siete días observados, media 11.714. No sumar 94, no contar ocho o nueve días ni borrar el histórico.
4. Replay del cierre no duplica estado. Registrar otro consumo 1 del día 5: saldo 49 y día abierto. El replay del cierre anterior devuelve su aceptación histórica, pero no vuelve a cerrar el día modificado. Cerrar con nueva clave y nueva versión recalcula total 83/7=11.857.

**Evidencia:** filas por fuente, estados/versiones de día, respuesta/facts de riesgos y TRX `DemandComposerTests`/`ConsumptionDayTests`. `inputCutoff` y zona se anotan en el informe.

## E2E-04 — Frescura, cero y dato incompleto

1. Con fixture base y día 12 abierto, consumo 12 aceptado deja 48. La historia observada sigue estimando 12/día: cobertura 5→4. Captura local no enviada deja saldo 60 y cobertura 5 en servidor.
2. En otro punto/SKU sin historia, un día sin registros produce demanda desconocida, no 0. Cerrar explícitamente un día pasado con cero produce cero observado; cobertura no calculable con motivo «sin consumo observado», sin división por cero.
3. Consumir en un día ya cerrado lo reabre; la UI muestra dato parcial y última operación. La fecha de corte de stock no se reemplaza por la hora del consumo.
4. Cierre del día actual o futuro se rechaza; un dispositivo con fecha futura no adelanta el reloj real de piloto. Probar conversión cerca de las 00:00 en la zona de la organización.

**Evidencia:** estado antes/después, fuentes, cobertura, textos de cero/desconocido, capturas sintéticas Web y TRX `InventoryRiskTests`.

## E2E-05 — Corrección que conserva historia

1. Desde 60 aceptar consumo 10 →50. Como Receptor A enviar reversión del consumo propio con motivo «cantidad capturada erróneamente» y versión vigente.
2. Esperar otro consumo de fuente `reversal`, movimiento hacia ubicación original y saldo 60. El consumo 10 original sigue legible y vinculado a su compensación; no se modifica su cantidad ni se elimina fila.
3. Replay de reversión mantiene 60; otra clave contra el mismo original devuelve 409 `ALREADY_REVERSED`. Reversión del histórico, de una reversión o por otro actor se deniega.
4. Registrar consumo correcto 8 con clave nueva →52. La consulta concilia `60 − 10 + 10 − 8 = 52`. Revisar corte y apertura de intervalo, incluyendo uno que empiece después de la primera salida: no debe comenzar artificialmente en 0.
5. Si el lote se bloqueó después del consumo, reversión devuelve existencia física pero disponible utilizable sigue 0; no desbloquea lote. Un caso de conteo físico se tramita en E2E-12, no como reversión de consumo ajeno.
6. Revertir hoy un consumo de ayer: se reabre y corrige el neto de ayer; el movimiento compensatorio cambia el saldo a la hora de aceptación de hoy. Consultar antes del primer corte reconstruible devuelve límite disponible y `HISTORY_BEFORE_BASELINE`, sin apertura inventada.

**Evidencia:** cadena de referencias, sumatoria por lote, capturas de movimiento original/corrección y TRX `ConsumptionReversalTests`/`InventoryQueryTests`.

## E2E-06 — Recursos ajenos y revocación

1. Anónimo consulta/escribe: 401. Conductor o Automatizacion intenta consumo: 403. Un rol inventado en payload/claim no sustituye `Account.Role` de servidor.
2. Receptor B de CLI-002 solicita saldo, movimientos, consumo y reversión de HOS-001:404 para referencias ajenas; lista general solo devuelve su alcance. Probar UUID sintético de otra organización en lote, ubicación y punto.
3. Receptor A registra 10; revocar asignación en servidor y repetir clave/cuerpo antes aceptados. Denegar sin devolver IDs/saldos privados de la respuesta original; no cambiar la operación aceptada.
4. Reactivar cuenta/asignación mediante provisionamiento autorizado y repetir: recuperar aceptación original sin descuento nuevo. Repetir caso con cuenta inactiva. `GET /risks` filtra tanto `items` como `facts`.
5. Variante con dos ubicaciones del mismo punto y asignación a una: permite consumo local, pero deniega consulta/cierre del agregado diario completo.

**Evidencia:** matriz rol/recurso/código y conteos; no guardar tokens. TRX `InventoryAuthorizationTests`; prueba de UI no reemplaza este control.

## E2E-07 — Reimportar no deshace operación

1. Desde fixture base aceptar 10 →50. Reenviar snapshot inicial de 60 con fecha de corte anterior al consumo a `/api/v1/imports?dryRun=true`.
2. Informe señala fila y `STOCK_IMPORT_CONFLICT`, sin cambiar 50 ni crear corte/movimiento. `dryRun=false&allowPartial=false` rechaza el lote de importación, conservando toda la operación.
3. Probar fecha de corte aparentemente posterior y cantidad igual a 50: el MVP también exige conciliación para ese par con actividad piloto. Desactivar rutas piloto no habilita un bypass de importación.
4. Con `allowPartial=true` y otro par sin actividad válido, el informe enumera aplicado/rechazado; el par protegido sigue 50. Reimportar solo historia para el día compartido no reemplaza ni borra consumos operativos y no descuenta stock.
5. Otra fila intenta bajar cantidad por debajo de reserva: rechazar incluso si no tiene consumo 007.

**Evidencia:** perfil/manifiesto, reportes de dry-run/commit/parcial, cortes y saldos. TRX `InventoryImportGuardTests`/regresión de 001.

## E2E-08 — Un mismo movimiento para campo e inventario

**Dependencia real:** backend de 001/002/004 integrado. Si falta, registrar `bloqueado por 004`, no sustituir por escritura directa de la base de datos ni mock.

1. Publicar una entrega válida de 12 L del mismo lote con reserva 002; usar despacho y recepción reales de 004 hasta aceptar 12 en HOS-001. Saldo del punto 60→72.
2. Consultar 007: aparecen movimientos 004 con sus referencias y actores, sin copia 007. Repetir recibo original: saldo 72, mismos recibos/movimientos.
3. Receptor captura 10 por 007 →62; repetir mantiene 62. Si hay rechazo/devolución en un fixture aparte, 007 muestra el movimiento 004 en la ubicación correcta, sin sumar producto rechazado al punto.
4. Competir publicación/reserva/replanificación de 002 y consumo/ajuste de ubicación afectada; comprobar saldos/reservas y ausencia de commits parciales. Reset de demo con operación piloto se deniega sin borrar nada.

**Evidencia:** IDs de ruta/entrega/recibo/consumo, tabla de ubicaciones antes/después y TRX `InventoryFieldIntegrationTests`/`InventoryJourneyTests`.

## E2E-09 — Teléfono sin conexión

1. Receptor A inicia sesión real, descarga sus lotes/saldo 60 e instala/abre PWA. Activar modo avión; capturar 10 y comprobar «Guardado en el teléfono» solo después de commit local.
2. Cerrar PWA, reiniciar dispositivo o navegador, reabrir sin red: captura 10 sigue disponible en cola, estado pendiente y saldo de snapshot con fecha; servidor todavía 60.
3. Conectar, cambiar a Receptor B: no puede ver ni enviar la cola A. Volver a A, sincronizar: 201, saldo 50 y un movimiento. Simular pérdida de respuesta luego del commit: repetir conservando clave/cuerpo recupera mismo ID.
4. Repetir con sesión vencida → estado sesión requerida; asignación revocada → rechazo persistido; versión vieja → conflicto que preserva la captura. Corregir una operación rechazada crea clave nueva; no reescribir la petición que ya pudo haberse aceptado.
5. Simular cuota IndexedDB agotada: no anunciar guardado. Inspeccionar almacenamiento sin copiar secretos: no JWT/refresh tokens, separación por identidad y payload inmutable. Confirmar que la cola de recibos 004 conserva sus datos.
6. En un fixture independiente guardar dos consumos offline con la misma versión: el primero se acepta; el segundo conserva su conflicto. Revisar saldo y reenviar el rechazado con nueva clave; comprobar que cada aceptación descontó una sola vez y ninguna captura desapareció.

**Evidencia:** dispositivo/navegador/commit, secuencia de pantallas con datos sintéticos, estados locales, IDs de respuesta y SQL/consulta de saldo. Build móvil o simulador offline no basta para aprobar.

## E2E-10 — Consulta Web y páginas estables

1. Planificador abre `/inventory`, filtra ubicación/SKU/lote y ve cantidades con unidad, stock físico/reservado/disponible, último corte y última operación separados. Un lote no utilizable muestra causa además de color.
2. Con movimientos de 10 y 5 sobre 60, usar límite 1. La primera página fija secuencia; aceptar 2 entre páginas. La vista histórica completa termina 45, una consulta nueva 43.
3. Cambiar filtros conservando cursor anterior produce error controlado; refrescar inicia otra consulta. Probar intervalo sin movimientos, carga fallida y reintento; desconocido no aparece como cero.
4. Seguir enlace al riesgo y volver sin perder contexto. Cuenta sin permiso no obtiene datos por URL directa o API. Probar controles por teclado y mensajes legibles en móvil/escritorio.

**Evidencia:** filtros, cursores sin secretos, saldos 45/43, capturas y TRX `InventoryQueryTests`; anotar revisión manual de UI.

## E2E-11 — Entradas y demanda no atendida (ampliación)

1. Bodega asignada registra entrada 20 L con referencia externa nueva: 800→820. Replay idéntico mantiene 820; otra clave con misma referencia/lote informa duplicado de negocio.
2. Receptor, en fixture independiente, consume 6 sobre 60 →54. Declara solicitadas 10 vinculadas a ese consumo: atendidas 6/no atendidas 4. El déficit no crea otro movimiento. Reutilizar el consumo en otra observación o pedir cantidad 5 provoca rechazo.
3. Receptor solicita reposición 20: nueva solicitud Requested, saldo 54, reservas sin cambio, no tratarla como entrada confirmada. Planificador la consulta con alcance correcto.
4. Si se revierte consumo 6, la observación necesita revisión y conserva el vínculo; el sistema no declara cumplida la demanda por defecto. Déficit abierto figura como motivo de atención en riesgo con fuente explícita.

**Evidencia:** entrada/movimiento/referencia, consumos observados, solicitud y saldos; TRX `InventoryExtensionTests`, pantallas PWA/Web.

## E2E-12 — Diferencia y cierre humano (ampliación)

1. Partir de saldo 54. Bodega/Receptor autorizado constata 50, abre caso con referencia de conteo y versión de saldo 54. Estado Open no cambia stock.
2. Planificador inicia investigación. Sin motivo/evidencia o con rol Bodega intentando aprobar, cierre se deniega. Una alerta no se muestra como merma confirmada.
3. Con evidencia y versiones vigentes, cerrar ConfirmedLoss: movimiento −4, saldo 50 y evento de cierre inmutable. Replay no repite ajuste. Otra clave contra caso cerrado produce conflicto.
4. Casos independientes: Dismissed mantiene 54; conteo 58→CorrectedCount suma 4 y deja 58; conteo 54 cierra sin movimiento. No etiquetar ajuste positivo como merma.
5. Contar 50, luego aceptar consumo 1 antes de cerrar: expectedBalanceVersion queda vieja; cierre 409 sin ajuste obsoleto. Pedir nuevo conteo/investigación; pérdida no invade stock reservado.

**Evidencia:** cronología Open→Investigating→cierre, referencia privada autorizada sin contenidos sensibles, IDs de actor, diferencia y movimientos. Esto prueba investigación/cierre; no mide un detector estadístico aún no definido.

## E2E-13 — Integridad y rendimiento del piloto

1. En base descartable con versión anterior, conservar IDs/counts/saldos, aplicar migración 007 y comprobar los mismos datos más cortes nuevos. Cadena de migraciones preservada; grants runtime sin DDL y sin edición de movimientos operativos.
2. Ejecutar regresión completa de T12 y suites de 004/002 afectadas. Cambios a formato de demanda se explican; tests omitidos/no encontrados no son pase.
3. Con volumen, concurrencia, recursos y política de caché acordados en T0, medir consultas de saldo/movimientos por HTTP: warm-up declarado y al menos 100 consultas válidas por endpoint, sin mezclar cálculos asíncronos. Registrar muestra, errores y p95; objetivo ≤ 2 s. Mantener pendiente si no hay volumen empresarial definido.
4. Confirmar que lectura de conciliación sigue disponible al desactivar escrituras, y que un binario antiguo incapaz de interpretar fuentes operativas no se ofrece como rollback.

**Evidencia:** TRX, informe de migración, medición con entorno/volumen, comando/script de carga versionado en `InventoryPerformanceTests` y limitaciones observadas.

## Registro de ejecuciones

| Escenario | Fecha/commit | Resultado | Evidencia | Responsable |
|---|---|---|---|---|
| E2E-01 | — | Sin ejecutar | — | Por asignar |
| E2E-02 | — | Sin ejecutar | — | Por asignar |
| E2E-03 | — | Sin ejecutar | — | Por asignar |
| E2E-04 | — | Sin ejecutar | — | Por asignar |
| E2E-05 | — | Sin ejecutar | — | Por asignar |
| E2E-06 | — | Sin ejecutar | — | Por asignar |
| E2E-07 | — | Sin ejecutar | — | Por asignar |
| E2E-08 | — | Pendiente backend 004 integrado | — | Por asignar |
| E2E-09 | — | Sin ejecutar en teléfono | — | Por asignar |
| E2E-10 | — | Sin ejecutar en navegador | — | Por asignar |
| E2E-11 | — | Ampliación sin ejecutar | — | Por asignar |
| E2E-12 | — | Ampliación sin ejecutar | — | Por asignar |
| E2E-13 | — | Sin ejecutar; volumen por acordar | — | Por asignar |

Cada ejecución identifica commit de API/UI, modo de reloj, dataset, entorno, IDs de caso y actor revisor. Adjuntar evidencia al registro tras correrla; conservar resultados fallidos y su repetición. MVP verificado requiere E2E-01–10 y controles de migración/regresión; piloto con NFR-04 exige medición acordada. Cierre completo 007 exige también ampliación, decisiones abiertas resueltas y referencia a entregables de 007 en cobertura/bóveda.
