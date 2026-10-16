# Plan de implementación y entrega — issue #194

Fecha: 16-oct-2026. Estado: **plan preparado; implementación y pruebas pendientes**.
Responsable de ejecución: agente Antigravity abierto en este proyecto, por encargo
expreso de Bryan. Issue: https://github.com/bryancito1090/istpetdev-platform/issues/194.
Meta indicada en el issue: viernes 18:30, America/Guayaquil. Desbloquea #195 y #196.

## Objetivo y límites

Exponer tres consultas para stock por bodega, origen sugerido y vehículos
disponibles. Deben aceptar Planning (Gerencia y Planificador) o Automatizacion
con la autenticación M2M existente y scope `istpetdev-api/automation`.
No crean pedidos, planes, reservas, entregas ni snapshots de riesgo.

Usar un controlador nuevo: `src/Api/Controllers/LogisticsQueriesController.cs`.
**No editar `RoutePlansController.cs`**, reservado a #201. Mantener compatibles
las firmas que este controlador consume. No implementar #195, #196, prioridades
horarias ni nuevas herramientas n8n en este PR. Sin migraciones ni dependencias
nuevas salvo necesidad demostrada y registrada. La entrega autorizada termina
en PR revisado e integrado a `develop`; no incluye promoción a `main` ni despliegue.

## Evidencia de partida y riesgos concretos

Inspección local en `c147d2a0f703c176076aea9a8286fffdf8173fbf`. Antes de implementar,
actualizar desde `origin/develop` y contrastar estos hallazgos con su HEAD.
El PR #192 ya está fusionado según GitHub; su nota de ampliación se consultó por
API porque aún no existe en este checkout. Es una propuesta, no evidencia de implementación.

- `SourcingService.PlanSourcingAsync(orderIds)` carga pedidos, distribuye FEFO
  por distancia y consume cantidades solo en memoria. Excluye lotes bloqueados
  o con vencimiento <= hoy, pero **no descuenta stock de seguridad**.
- `ReplenishmentAllocator` calcula seguridad con
  `WarehouseSafetyStockPct` sobre stock utilizable neto de reservas; ese es el
  comportamiento existente a reutilizar. `WarehouseStockPolicy.MinStock` y
  `SafetyStockUnits` no son automáticamente equivalentes a ese porcentaje.
- `EmergencyRoutes` llama `CreateEmergencyOrdersAsync`: **no sirve como camino
  para la consulta por riesgos**. `IRiskCalculator.CalculateAsync(false)` sí
  permite calcular demanda sin guardar snapshots.
- `VehicleMatcher` excluye vehículos inactivos y con ruta Published en la fecha
  UTC de inicio. Sin paradas, usa una posición fija para estimar distancia/costo:
  evitar que ese valor se presente como cotización de un viaje real.
- `IOperationalClock` controla Dataset/Fixed/Real. No usar `DateTime.Today` para
  saltarse la fecha del fixture. Revisar límites del día UTC frente al día de
  operación en America/Guayaquil; documentar y probar la conversión elegida.
- `AgentRunTraceMiddleware` ya registra llamadas M2M con `X-Agent-Run-Id`.
  Esa traza (y la caché técnica de distancias existente, si aplica) no es una
  reserva ni una mutación logística. No prometer cero escrituras físicas.

## 1. Preparación y contrato antes del código

1. Leer AGENTS.md, README.md, docs/boveda/README.md, Hechos canónicos,
   CONTRIBUTING.md, este plan, task.md del spec 003, Contratos API y eventos
   y la nota de ampliación de #192 desde develop actualizado.
2. Usar `ponytail` e `istpet-gitflow`, aplicando el flujo GitHub real del repo.
   Crear `feature/194-consultas-abastecimiento-agente` desde develop actualizado.
   Preferir checkout aislado mediante Orca si hay trabajo concurrente; copiar
   allí este plan y únicamente la entrada de seguimiento de #194.
3. Preservar los cambios ajenos que ya existen en
   `docs/specs/006-validacion-pitch-entrega/hojas-feria.md` y `task.md` de ese spec.
   No hacer reset, stash global ni commit indiscriminado. No cambiar la rama del
   checkout compartido mientras otro agente la esté usando.
4. #194 tiene `type::feature` y prioridad alta, sin milestone. El único milestone
   abierto observado es H5 del 15-oct, ya pasado. No asignarlo por suposición:
   comprobar metadatos vigentes y registrar pendiente si no hay uno aplicable.
   Las reglas institucionales GitLab no inventan jobs obligatorios en GitHub.
5. Documentar los tres contratos propuestos abajo en
   `docs/boveda/03 Arquitectura/Contratos API y eventos.md` antes de implementarlos,
   inicialmente como pendientes. Son decisiones de este plan, no endpoints existentes.

## 2. Contratos propuestos

Prefijo común: `/api/v1/logistics`. JSON camelCase, DTOs explícitos, organización
determinada por servidor, `CancellationToken`, filtros acotados, sin entidades EF
serializadas directamente. Reutilizar Problem Details y convenciones del repo.

| Método/ruta | Entrada | Respuesta 200 |
|---|---|---|
| `GET /stock` | `productId` obligatorio; `warehouseId` opcional | `{ operationalDate, productId, sku, baseUnit, warehouses: [...] }` |
| `POST /sourcing` | Exactamente uno de `orderIds` o `servicePointIds`, lista no vacía; `productId` opcional solo con puntos | `{ operationalDate, options: [...] }`, asignaciones y faltantes por demanda |
| `GET /vehicles/available` | `date` opcional ISO yyyy-MM-dd; por defecto reloj operativo | `{ operationalDate, date, timeZone, vehicles: [...] }` |

POST sourcing es una **consulta sin persistencia logística**: el método permite
enviar listas en un cuerpo JSON; no es una orden de creación.
La fecha alternativa de vehículos no cambia la fecha de stock ni de sourcing.

Validación: 400 para binding/formato inválido; 422 para combinación inválida,
listas vacías, Guid.Empty o exceso de límites; 404 si algún ID solicitado no
existe dentro de la organización (no aceptar silenciosamente un subconjunto).
Aplicar los códigos existentes equivalentes si el repo los centraliza y dejar
documentada esa adaptación. Acotar listas a 100 IDs únicos; deduplicar antes de
procesar para impedir consumo duplicado. Una consulta válida sin resultados
devuelve 200 con colección vacía. Un origen insuficiente devuelve demanda,
asignación parcial, faltante y `fullySourced: false`, no un error HTTP.

### Stock

- Cada bodega devuelve ID/código/nombre, cantidades en mano y reservadas de
  lotes utilizables, stock de seguridad, cantidad disponible y lotes.
- Cada lote: ID/código, vencimiento nullable, en mano, reservado y neto utilizable;
  ordenar FEFO (sin vencimiento al final) con desempate por código/ID.
- Para cada bodega/producto: `netUsable = sum(max(0, onHand - reserved))` de
  lotes no bloqueados y con `expiresOn == null || expiresOn > operationalDate`;
  `safetyStock = netUsable * WarehouseSafetyStockPct / 100`;
  `available = max(0, netUsable - safetyStock)`. Aplicar seguridad **una sola vez
  por bodega/producto**, nunca una deducción completa por lote o pedido.
- Mostrar bodegas activas sin saldo con disponibilidad cero. No incluir lotes
  vencidos/bloqueados como abastecibles. Especificar que los totales de la
  respuesta representan stock utilizable; el neto de lotes todavía incluye
  la porción protegida como seguridad. No inventar distribución de seguridad por lote.
- Contrastar la fórmula con la política documentada en develop actualizado;
  si cambió, registrar el ajuste y mantener una sola regla para stock y sourcing.

### Origen sugerido

- Por pedidos: validar existencia y reutilizar `PlanSourcingAsync(orderIds)`.
  Mantener trazabilidad de pedido, destino y producto en cada línea.
- Por puntos: `IRiskCalculator.CalculateAsync(persistSnapshots: false)`, filtrar
  puntos/producto pedidos y niveles Critico/Alto/Vigilar con cantidad sugerida > 0.
  Sin riesgos elegibles, devolver opciones vacías. La cantidad proviene del
  servidor; no aceptar stock, prioridades, organización ni cantidades del LLM.
- Extender mínimamente `ISourcingService`/`SourcingService` con un camino para
  demandas en memoria que comparta el mismo algoritmo de asignación con el
  método existente. Conservar la firma usada por `/suggest` y `/emergency`.
  No insertar pedidos temporales ni crearlos y revertirlos. Para demandas de
  riesgos, `orderId` será nullable o se usará un DTO externo que distinga el
  origen; no emitir IDs ficticios como si fueran pedidos persistidos.
- Aplicar el presupuesto de seguridad compartido entre todas las demandas de
  una consulta. No sobreasignar un lote cuando dos pedidos/puntos compiten.
  Conservar FEFO, distancias y opciones A/B existentes, sin convertir la
  descripción de una alternativa en garantía de horario o de vehículo.
- Orden estable de demandas y desempates; reutilizar el orden de urgencia
  existente para riesgos. No incorporar los cambios de prioridad de #201.
- Incluir bodegas/lotes/cantidades, solicitado, asignado, faltante y factibilidad.
  Probar regresión de `/suggest` al corregir la seguridad en el servicio compartido.

### Vehículos

- Reutilizar `VehicleMatcher`; extraer o ampliar mínimamente su consulta de
  disponibilidad para compartir activos, fecha y exclusión de Published.
  No duplicar la regla en el controlador. Conservar la firma actual del matcher.
- Devolver ID/código, `maxWeight` (kg), `maxVolume` (m³, nullable), bodega base
  ID/código/nombre y tarifa `costPerKm` efectiva (configuración de respaldo si
  falta en vehículo). Documentar USD/km; no llamar costo total a una tarifa.
- Sin recorrido no existe cotización total: omitirla o devolver null explícito.
  No usar la posición fija de fallback como destino de una consulta de disponibilidad.
- Resolver el día consultado en la zona operativa y traducir el intervalo
  `[inicio, inicio del día siguiente)` a UTC para Published. Compartir esa
  semántica en el matcher, con regresión para sus consumidores y límites de medianoche.
  Devolver zona y fecha usadas para que n8n no dependa de la zona del servidor.
- Proposed, Cancelled y Superseded no bloquean bajo el criterio del issue.
  Un vehículo publicado varias veces aparece excluido una sola vez; vehículos
  disponibles aparecen una vez. Orden estable por código/ID; sin filtro de carga
  ni evaluación de ventanas/jornada porque esta consulta no recibe una ruta.

## 3. RBAC y solo lectura

Usar `[Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]` en el
controlador nuevo y la validación JWT/Account existente; no añadir un rol literal
"Planning" ni exigir scope automation a humanos. Verificar rechazo M2M sin
scope correcto, cliente no permitido, cuenta inactiva o rol incompatible.
Aplicar organización fija DemoCore y `AsNoTracking()` en las nuevas lecturas;
no habilitar Schema95/RLS ni introducir organizaciones suministradas por cliente.

No llamar `SaveChanges`, publicación, creación de pedidos ni snapshots desde
los nuevos caminos. Mantener la traza existente de herramientas y probar que
su encabezado solo funciona con identidad de Automatizacion.

## 4. Pruebas y evidencia de aceptación

Crear `tests/IstpetDev.Tests/Integration/Agent/LogisticsQueriesTests.cs` usando
`ApiFactory`, `PostgisContainer` y reset **de la base descartable** a `demo-region`.
Extender datos solo en la base de prueba para segunda bodega, lotes y pedidos;
no recalibrar el fixture canónico del equipo. No depender de n8n/LLM, Cognito
real ni OSRM público para CI; reutilizar los dobles/caché de prueba existentes.

| Área | Casos que deben pasar |
|---|---|
| Stock | fórmula con reservas y porcentaje no cero; seguridad una vez; FEFO; vencido hoy; bloqueado; sin vencimiento; bodega vacía; filtro por bodega/producto |
| Sourcing pedidos | uno y varios pedidos; una y varias bodegas; faltante; dos demandas por el mismo lote; suma asignada <= disponible; regresión de `/suggest` |
| Sourcing riesgos | cantidades desde RiskCalculator; filtros; sin riesgos; demanda sin pedido guardado; múltiples productos de un punto conservados |
| Vehículos | activo libre incluido; inactivo excluido; Published mismo día excluido; otro día y Proposed no bloquean; límites medianoche/UTC; fecha por reloj Dataset/Fixed; tarifa fallback y volumen nullable |
| Validación | IDs inexistentes/ajenos, vacíos/duplicados, listas mixtas, cuerpo inválido, fechas mal formadas y límites |
| Acceso | los tres endpoints: Gerencia/Planificador 200, M2M válido 200, restantes roles 403, anónimo 401 y scope/cliente/cuenta inválidos rechazados |
| Sin efectos logísticos | comparar IDs, conteos y valores antes/después de cada consulta y su repetición: pedidos/líneas, planes/paradas, reservas, saldos, movimientos, entregas y snapshots sin cambios |
| Trazabilidad | llamada M2M con X-Agent-Run-Id registra endpoint/resultado; cabecera humana no registra traza; ninguna altera stock/pedidos |

Antes de arrancar API/PostGIS de prueba o declarar la implementación lista,
aplicar `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md`, leyendo sus
referencias pertinentes (seguridad, RBAC, esquema DemoCore y plan de validación).
Si falta la copia generada, localizar su mecanismo de generación; fuente editable
única: `docs/boveda/08 Skills/istpetdev-prelaunch.md`. Registrar alcance local
aislado, evidencia reutilizable, fallos, pendientes y no aplica.

Secuencia de validación, conservando salida real y SHA:

```bash
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~LogisticsQueriesTests'
dotnet build IstpetDev.sln -c Release
dotnet test tests/IstpetDev.Tests -c Release --no-build
npm run check
npm run audit:check
python3 docs/boveda/.github/scripts/check_vault.py
git diff --check
```

No afirmar que pasaron pruebas que no se ejecutaron. Actualizar task.md a medida
que se avanza, distinguiendo código, pruebas locales, CI e integración. Añadir
evidencia breve en `evidencia/2026-10-16-194-consultas-abastecimiento.md`.
Cerrar documentación con ejemplos reales de request/response, unidades, fechas,
fórmula, errores, RBAC, ausencia de reservas y límite de frescura (otra operación
puede consumir stock/vehículo después de consultar). Registrar el cambio en
`docs/boveda/99 Fuentes/Fuente y control documental.md`.

## 5. PR, revisión, conflictos e integración a develop

1. Revisar diff completo, sin archivos ajenos, secretos ni cambios a
   `RoutePlansController.cs`. Commits Conventional Commits y rama del issue.
2. Publicar la rama y abrir PR hacia `develop`, plantilla del repositorio,
   `Closes #194`, label `type::feature`, evidencia y vínculos a #195/#196.
   Dejar en el PR los contratos definitivos para sus consumidores.
3. Obtener develop actualizado, integrar y resolver cualquier conflicto
   conservando ambas funcionalidades; no usar ours/theirs indiscriminadamente.
   Repetir las pruebas afectadas y exigir checks verdes para el nuevo HEAD.
4. Verificar en GitHub el destino, SHA de cabeza, mergeabilidad y todos los
   checks aplicables: política de ramas, backend, bóveda y restantes jobs de CI.
   Estado UNKNOWN o checks pendientes no equivalen a limpio/aprobado.
5. Revisar contra cada fila de aceptación y corregir hallazgos antes de aprobar.
   Emitir aprobación formal si la identidad disponible puede revisar ese PR.
   Si GitHub impide autoaprobar por ser el autor, **no simular aprobación ni
   cambiar de identidad**: registrar la limitación y solicitar revisión a un
   colaborador habilitado mediante el PR. No eludir revisiones requeridas.
6. Integrar a develop solo con criterios cumplidos, pruebas verdes, sin
   conflictos y respetando protecciones. No usar merge administrativo para
   saltar checks. Si falta una aprobación requerida, dejar el PR pendiente y
   reportar exactamente ese bloqueo. Verificar SHA de merge y estado final.
7. Informar a Bryan URL del PR, resumen de endpoints, pruebas y resultados,
   conflictos resueltos (o ausencia verificada), estado real de aprobación y
   merge. No describir integración a develop como despliegue ni ejecutar #195.

## Entrega al agente

Antigravity debe ejecutar este plan de principio a fin dentro del alcance
autorizado. Puede ajustar detalles mínimos por cambios reales en develop,
dejando la razón y contrato resultante documentados. No reducir los cuatro
criterios del issue ni detenerse tras escribir código o abrir el PR.
El agente que prepara el plan transfiere la ejecución; este documento no
acredita implementación, pruebas, aprobación, merge ni despliegue.
