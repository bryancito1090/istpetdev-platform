# 007 — Inventario cotidiano y conciliación: tareas

> Ejecución de [plan.md](plan.md), con aceptación en [test-e2e.md](test-e2e.md). Todos los pasos están pendientes. Los fragmentos son código propuesto para archivos futuros, no pruebas ya existentes ni resultados ejecutados. Trabajar desde la raíz y conservar los cambios ajenos del workspace.

## Secuencia y trazabilidad

| ID | Entrega | Depende de | Estado |
|---|---|---|---|
| T0 | Reglas, base integrada y evidencia de entrada | 001/002; contratos reales 004 para integración | pendiente |
| T1 | Modelo, migración, fixture y soporte de pruebas | T0 | pendiente |
| T2 | Autorización por ubicación y lectura de sesión | T1 | pendiente |
| T3 | Consumo y reversión atómicos e idempotentes | T2 | pendiente |
| T4 | Día completo y demanda sin doble conteo | T3 | pendiente |
| T5 | Importaciones seguras y cortes | T3 | pendiente |
| T6 | Consultas conciliadas, páginas y riesgo autorizado | T4, T5 | pendiente |
| T7 | Contratos compartidos y Web de consulta | T6 | pendiente |
| T8 | Integración de reservas, recepción/devolución y reloj | T3, T6, backend 004 | pendiente |
| T9 | PWA y cola offline de consumo | T2, T3, T4, T7 | pendiente |
| T10 | Entradas, demanda no atendida y reposición | MVP verificado y ampliación activada | pendiente |
| T11 | Conteo, caso investigado y cierre humano | T10 | pendiente |
| T12 | Regresión, rendimiento y cierre documental | Alcance entregado de T1–T11 | pendiente |

| Requisito/criterio | Tareas | E2E |
|---|---|---|
| FR-01, NFR-01, CE-06 | T2/T3/T6/T9 | 01, 06, 09 |
| FR-02, NFR-02, CE-01/02 | T1/T3/T8 | 01, 02, 08 |
| FR-03/12, FR-07, CE-03/05/10 | T4/T6/T8 | 03, 04 |
| FR-04, NFR-03/04, CE-04 | T5/T6/T7/T12 | 05, 10, 13 |
| FR-05, CE-04 | T3/T9 | 05 |
| FR-06 | T8 | 08 |
| FR-08, CE-11 | T10 | 11 |
| FR-09, CE-07 | T11 | 12 |
| FR-10, CE-08 | T5 | 07 |
| FR-11, CE-09 | T9 | 09 |

Para cada tarea de código: añadir primero el caso, ejecutar filtro y observar el fallo esperado; implementar el bloque pequeño, repetir el filtro y registrar resultado. No contar error de Docker/restauración como rojo funcional. Hacer commit solo de las rutas de esa tarea después de revisar `git diff --check`; el usuario autoriza aquí redactar documentos, no ejecutar esas tareas o crear commits ahora.

## T0 — Fijar entrada y reglas del ensayo

**Archivos:** leer `src/Infrastructure/Persistence/LeanSchema.cs`, `src/Domain/IstpetDev.Domain.csproj`, `docs/specs/004-campo-qr-custodia-offline/task.md`; crear en ejecución `docs/specs/007-inventario-cotidiano-conciliacion/verification.md`.

- [ ] Registrar commit, última migración integrada y si los endpoints de recepción/despacho de 004 usan PostgreSQL real. Separar prueba sintética local de pase del piloto.
- [ ] Registrar adopción propuesta de Receptor/PWA, lote obligatorio, reversión propia, cierre diario, captura pasada máxima y retención de claves. Si no hay contraparte, usar reglas del fixture y mantener aprobación empresarial pendiente, sin inventar nombres de responsables.
- [ ] Comprobar herramientas y fixture con comandos reales:

```bash
git rev-parse HEAD
dotnet --version
dotnet tool restore
dotnet ef --version
docker info --format '{{.ServerVersion}}'
node --version
npm --version
python3 -c 'from pathlib import Path; import json; p=Path("data/fixtures/demo-base"); j=json.loads((p/"mapping.json").read_text()); assert all((p/v["file"]).is_file() for v in j["files"].values())'
```

- [ ] Aplicar la revisión prelaunch local antes de iniciar hosts/pruebas. Ejecutar la base de regresión `dotnet test tests/IstpetDev.Tests --filter 'Category=Unit'`; registrar cualquier fallo previo por su spec dueño. No suponer que 001–006 están cerrados por existir documentos.

**Salida:** versión y entorno identificados, baseline y reglas de ensayo registradas; E2E de campo identificado como dependiente si falta backend 004.

## T1 — Modelo, migración y fixture

**Files:** modificar `src/Domain/Entities/InventoryEntities.cs`, `src/Domain/Common/MovementKinds.cs`, `src/Infrastructure/Persistence/{LeanSchema.cs,ApplicationDbContext.cs,DemoGrants.cs}`, `src/Application/Common/Interfaces/IApplicationDbContext.cs`, `src/Application/Imports/MappingProfile.cs`; crear `src/Domain/Entities/InventoryControlEntities.cs`, migración `OperationalInventory`, `tests/IstpetDev.Tests/Integration/Inventory/InventorySchemaTests.cs`, `tests/IstpetDev.Tests/Support/InventoryScenario.cs` y `data/fixtures/spec007/`; ajustar pruebas existentes de persistencia que esperan un número fijo de migraciones.

**Produces:** campos/modelos/índices de la tabla «Modelo del MVP» del plan, DbSets `AccountLocationAssignments`, `ConsumptionDays`, `InventoryStockCuts`; helper de tests definido abajo.

- [ ] Escribir pruebas `Migration_preserves_imports_balances_and_movement_ids`, `Two_operations_same_day_do_not_overwrite_import`, `Reservation_cannot_exceed_on_hand`, `Cross_organization_foreign_key_is_rejected`, `Runtime_cannot_edit_operational_history`. Migrar desde el head anterior registrado en T0 con `IMigrator.MigrateAsync(previousMigration)` en base descartable. Sembrar las columnas anteriores con SQL parametrizado o fixture de respaldo sintético de esa versión; no usar el DbContext nuevo contra columnas todavía ausentes. Migrar a la nueva versión y afirmar IDs, cantidades y relaciones antes/después; el test de reservas intenta `reserved=61` sobre `onHand=60` y espera rechazo PostgreSQL.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~InventorySchemaTests'`. Exigir fallo del caso ausente, no una prueba vacía.
- [ ] Añadir propiedades del plan y checks. Para asignaciones usar esta entidad; mantener `Version` heredada:

```csharp
using IstpetDev.Domain.Common;
namespace IstpetDev.Domain.Entities;

public sealed class AccountLocationAssignment : OrganizationEntity
{
    public Guid AccountId { get; set; }
    public Guid LocationId { get; set; }
    public bool IsActive { get; set; }
}
```

- [ ] Conservar índice histórico con `HasFilter("\"SourceKind\" = 'import'")`; añadir únicos de operación y reversión solo para referencias no nulas. Agregar check `QuantityReserved <= QuantityOnHand`. No usar SQL literal interpolado para referencias o cantidades del usuario.
- [ ] Crear los cortes de migración y secuencias según plan, abortando si hay reservas inválidas. Aplicar grants también a las tablas/secuencias nuevas y verificar rol runtime. Proteger reset con datos piloto; no retirar historial de migraciones.

```bash
dotnet ef migrations add OperationalInventory --project src/Infrastructure --startup-project src/Api
dotnet ef migrations script --idempotent --project src/Infrastructure --startup-project src/Api --output /tmp/spec007-migrations.sql
dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~InventorySchemaTests'
```

- [ ] Añadir ahora `DateTimeOffset? StockObservedAtUtc` a `MappingProfile`, serializable y validado, para que el parser estricto acepte el fixture antes de T5; T5 aplicará su semántica de cortes/guard. Crear fixture `spec007`: copiar los siete CSV de `demo-base` y conservar códigos solo en datos/tests. En `mapping.json` fijar nombre `spec007`, `stockObservedAtUtc=2026-10-12T00:00:00Z`; reloj `FixedDate=2026-10-13`, lote `L-DES-2610`, producto `DES-01` (L), ubicación `HOS-001`, onHand 60 y reserva 0. Historia: 84 L en `[2026-10-05,2026-10-12)`. Crear `expected.json` con valores y origen sintético. Verificar `git check-ignore data/fixtures/spec007/inventario.csv`; incluir excepción acotada si los CSV del fixture se ignoran.
- [ ] Añadir soporte siguiente. `CreateAsync` necesita T2 para completar las asignaciones; T1 solo comprueba migración. No utilizarlo antes de crear los tipos propuestos.

```csharp
using System.Globalization;
using System.Net.Http.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Support;

public sealed class InventoryScenario : IAsyncDisposable
{
    public const string ReceiverSubject = "receiver-007";
    public string ConnectionString { get; }
    public ApiFactory Api { get; }
    public HttpClient Receiver { get; }
    public HttpClient Planner { get; }
    public Guid PointId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid LotId { get; private set; }
    public Guid ReceiverId { get; private set; }

    private InventoryScenario(string connection, DateOnly today)
    {
        ConnectionString = connection;
        Api = new ApiFactory(connection, true, new Dictionary<string, string?>
        {
            ["Demo:FixturePath"] = Fixtures.Folder("spec007"),
            ["Demo:Accounts:2:Subject"] = ReceiverSubject,
            ["Demo:Accounts:2:DisplayName"] = "Receptor sintético 007",
            ["Demo:Accounts:2:Role"] = Roles.Receptor,
            ["Inventory:PilotEnabled"] = "true",
            ["Inventory:MaxPastCaptureDays"] = "14",
            ["Logistics:Clock:Mode"] = "Fixed",
            ["Logistics:Clock:FixedDate"] = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["Logistics:DemandWindowDays"] = "7"
        });
        Receiver = Api.ClientFor(ReceiverSubject);
        Planner = Api.ClientFor(TestTokens.Planner);
    }

    public static async Task<InventoryScenario> CreateAsync(PostgisContainer postgres, DateOnly? today = null)
    {
        var scenario = new InventoryScenario(postgres.NewDatabase(), today ?? new DateOnly(2026, 10, 13));
        using var reset = await scenario.Planner.PostAsync("/api/v1/demo/reset", null);
        reset.EnsureSuccessStatusCode(); // Solo base nueva sintética, antes de operaciones piloto.
        await using var db = scenario.OpenDb();
        scenario.PointId = await db.ServicePoints.Where(x => x.Code == "HOS-001").Select(x => x.Id).SingleAsync();
        scenario.ProductId = await db.Products.Where(x => x.Sku == "DES-01").Select(x => x.Id).SingleAsync();
        scenario.LocationId = await db.Locations.Where(x => x.ServicePointId == scenario.PointId).Select(x => x.Id).SingleAsync();
        scenario.LotId = await db.Lots.Where(x => x.Code == "L-DES-2610").Select(x => x.Id).SingleAsync();
        scenario.ReceiverId = await db.Accounts.Where(x => x.Subject == ReceiverSubject).Select(x => x.Id).SingleAsync();
        db.Set<AccountLocationAssignment>().Add(new AccountLocationAssignment
        {
            OrganizationId = DemoOrganization.Id, AccountId = scenario.ReceiverId,
            LocationId = scenario.LocationId, IsActive = true
        });
        await db.SaveChangesAsync();
        return scenario;
    }

    public ApplicationDbContext OpenDb() => TestDb.Context(ConnectionString);
    public async Task<(decimal Quantity, long Version)> BalanceAsync()
    {
        await using var db = OpenDb();
        var row = await db.StockBalances.AsNoTracking().SingleAsync(x => x.LocationId == LocationId && x.LotId == LotId);
        return (row.QuantityOnHand, row.Version);
    }
    public object ConsumptionBody(Guid operation, decimal quantity, long version, DateTimeOffset? occurredAt = null) => new
    {
        operationId = operation, servicePointId = PointId, locationId = LocationId, lotId = LotId,
        quantity = quantity.ToString("0.000", CultureInfo.InvariantCulture), unit = "L",
        occurredAtDeviceUtc = (occurredAt ?? DateTimeOffset.Parse("2026-10-12T15:00:00Z", CultureInfo.InvariantCulture)).ToString("O"),
        expectedBalanceVersion = version
    };
    public static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, Guid operation, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", operation.ToString());
        return SendAndDisposeAsync(client, request);
    }
    private static async Task<HttpResponseMessage> SendAndDisposeAsync(HttpClient client, HttpRequestMessage request)
    {
        using (request) return await client.SendAsync(request);
    }
    public async ValueTask DisposeAsync()
    {
        Receiver.Dispose(); Planner.Dispose(); await Api.DisposeAsync();
    }
}
```

**Commit:** `feat(inventory): add operational consumption schema and fixtures`; incluir migración/snapshot/tests/fixture concretos revisados, no `git add .`.

## T2 — Cuenta, asignación y permisos

**Files:** crear `src/Application/Inventory/InventoryAccess.cs`, `InventoryOptions.cs`; `src/Api/Controllers/InventoryController.cs` y `AccountController.cs` solo si 004 no implementó `/api/v1/me`; modificar DI/Program y `tests/IstpetDev.Tests/Integration/Inventory/InventoryAuthorizationTests.cs`.

**Interfaces:** `InventoryAccess.RequireAsync(Guid accountId, Guid locationId, string action, CancellationToken ct)` devuelve cuenta/organización autorizadas o error tipado; `InventoryAccess.AllowedLocationIdsAsync(Guid accountId, CancellationToken ct)` devuelve solo asignaciones activas. Validar cuenta existente/activa también en llamadas Application; no pasar rol desde cliente.

- [ ] Añadir clase de prueba completa abajo. Ejecutar filtro y observar 404/ruta ausente o exposición indebida antes de implementar. Registrar `InventoryOptions` con validación al arranque de `MaxPastCaptureDays` cuando se habilita piloto. La tercera comprobación usa cuenta real Receptor sin asignación a bodega; no un header que simula un rol.

```csharp
using System.Net;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Inventory;

[Collection(PostgisCollection.Name), Trait("Category", "Integration"), Trait("Spec", "007")]
public sealed class InventoryAuthorizationTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Inventory_requires_session_and_hides_other_locations()
    {
        await using var s = await InventoryScenario.CreateAsync(postgres);
        using var anon = s.Api.ClientFor(null);
        using var denied = await anon.GetAsync("/api/v1/inventory");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var own = await s.Receiver.GetAsync($"/api/v1/inventory?locationId={s.LocationId}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        await using var db = s.OpenDb();
        var warehouse = await db.Locations.Where(x => x.Code == "BOD-UIO").Select(x => x.Id).SingleAsync();
        using var other = await s.Receiver.GetAsync($"/api/v1/inventory?locationId={warehouse}");
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
    }
}
```

- [ ] Implementar matriz: Receptor lee/consume/revierte propio/cierra días solo de sus puntos; Bodega lee y (T10) ingresa en su almacén; Planificador/Gerencia leen organización y (T11) cierran casos; Conductor/Automatizacion no escriben inventario 007. Endpoint `/me` retorna datos de cuenta del servidor y ninguna credencial.
- [ ] Añadir casos para asignación revocada, cuenta inactiva, UUID de otra organización, lote de otro SKU y roles inyectados en JWT/payload. Denegar sin revelar datos; 401 anónimo, 403 sin capacidad, 404 referencia ajena.
- [ ] Probar un punto con dos ubicaciones: asignación parcial permite operar su ubicación, pero no leer/cerrar el agregado diario completo del punto.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~InventoryAuthorizationTests'`; completar lectura mínima para pasar sin devolver todo el historial. Integrar el resto de lectura en T6.

**Commit:** `feat(inventory): authorize operations by account and location`.

## T3 — Consumo, replay y reversión

**Files:** crear `src/Application/Inventory/{InventoryContracts.cs,InventoryOperationRunner.cs,ConsumptionService.cs}`, `src/Api/Controllers/ConsumptionsController.cs`, tests `ConsumptionApiTests.cs`, `InventoryConcurrencyTests.cs`, `ConsumptionReversalTests.cs` bajo `Integration/Inventory/`.

**Interfaces:** `ConsumptionService.RecordAsync(Guid accountId, CreateConsumption command, CancellationToken ct)`; `ReverseAsync(Guid accountId, Guid consumptionId, ReverseConsumption command, CancellationToken ct)`; devuelven `InventoryWriteResult` con campos HTTP del plan. `CreateConsumption(Guid OperationId,Guid ServicePointId,Guid LocationId,Guid LotId,decimal Quantity,string Unit,DateTimeOffset OccurredAtDeviceUtc,long ExpectedBalanceVersion)`; `ReverseConsumption(Guid OperationId,string Reason,long ExpectedBalanceVersion)`. Convertir cadena decimal en frontera HTTP; no cambiar semántica de decimal de 004.

- [ ] Añadir prueba completa de replay antes de controladores/servicio:

```csharp
using System.Net;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Inventory;

[Collection(PostgisCollection.Name), Trait("Category", "Integration"), Trait("Spec", "007")]
public sealed class ConsumptionApiTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Accepted_consumption_replayed_three_times_moves_stock_once()
    {
        await using var s = await InventoryScenario.CreateAsync(postgres);
        var before = await s.BalanceAsync();
        var operation = Guid.NewGuid();
        var body = s.ConsumptionBody(operation, 10m, before.Version);
        string? original = null;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var response = await InventoryScenario.PostAsync(s.Receiver, "/api/v1/consumptions", operation, body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            original ??= text;
            Assert.Equal(original, text);
        }
        Assert.Equal(50m, (await s.BalanceAsync()).Quantity);
        await using var db = s.OpenDb();
        Assert.Equal(1, await db.InventoryMovements.CountAsync(x => x.OperationId == operation));
        Assert.Equal(1, await db.Consumptions.CountAsync(x => x.OperationId == operation));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.OperationId == operation));
    }
}
```

- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~ConsumptionApiTests'`; observar 404 antes de implementar, luego comprobar que falla cualquier versión que descuente dos veces.
- [ ] Implementar transacción del plan. Regla mínima reusable en `ConsumptionService` antes de mutar:

```csharp
private static void RequireAvailable(decimal onHand, decimal reserved, decimal requested)
{
    if (requested <= 0m || decimal.Round(requested, 3) != requested)
        throw new ArgumentException("INVALID_QUANTITY");
    if (reserved < 0m || onHand < reserved || requested > onHand - reserved)
        throw new InvalidOperationException("INSUFFICIENT_STOCK");
}
```

- [ ] Mapear errores con clase de excepción tipada en `InventoryContracts.cs`; no exponer mensajes SQL. Hash SHA-256 de contrato normalizado, con método/ruta/cuerpo; replay guarda código/cuerpo, no recalcula IDs/fechas. Contenido distinto con misma clave rechaza 409 antes de probar stock. Crear respuesta después de `SaveChanges` para devolver `Version` realmente persistida, dentro de la misma transacción.
- [ ] Reversión recupera lote/ubicación/cantidad del original, exige propietario y motivo; un único evento inverso y movimiento hacia ubicación original. No acepta cantidad elegida por el cliente. Lote bloqueado puede recuperar existencia física por reversión, pero sigue no utilizable. Prohibir reversión de histórico importado o reversión de otra reversión.
- [ ] Probar consumo de un día anterior y reversión aceptada hoy: neto y cierre afectados son los del día original; el saldo físico cambia hoy. La reversión nunca crea demanda negativa en el día actual.
- [ ] Añadir casos: misma clave con 11 en lugar de 10 (409, saldo 50); nueva clave con versión vieja (409, saldo 50); consumo 61 (422, saldo 60); dos contextos HTTP intentan 40 y 40 con versión inicial (solo uno 201, otro 409, saldo 20); saldo 60/reserva 55 pide 10 (422); cuenta revocada tras éxito intenta replay (denegado, no filtra respuesta).
- [ ] Inyectar falla de persistencia antes de guardar `IdempotencyRecord` mediante interceptor EF de tests: no queda consumo/movimiento/saldo parcial. Perder respuesta después del commit: replay devuelve aceptación. Reversión de 10 devuelve saldo 60; misma clave repite sin efecto; otra clave al mismo original rechaza `ALREADY_REVERSED`.
- [ ] Ejecutar filtro `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~ConsumptionApiTests|FullyQualifiedName~InventoryConcurrencyTests|FullyQualifiedName~ConsumptionReversalTests'`.

**Commit:** `feat(inventory): persist consumption and compensation atomically`.

## T4 — Componer demanda por día y cerrar captura

**Files:** crear `src/Application/Inventory/{DemandComposer.cs,ConsumptionDayService.cs}`, tests `Unit/Inventory/DemandComposerTests.cs`, `Integration/Inventory/ConsumptionDayTests.cs`; modificar `src/Application/Logistics/{RiskCalculator.cs,OperationalClock.cs}` y controladores/DI.

**Interfaces:** tipos y método puro siguientes; `ConsumptionDayService.CloseAsync(Guid accountId,CloseConsumptionDay command,CancellationToken ct)`, donde `CloseConsumptionDay(Guid OperationId,Guid ServicePointId,Guid ProductId,DateOnly Date,long ExpectedVersion)`; getters de días según plan.

- [ ] Escribir prueba roja con tipos propuestos:

```csharp
using IstpetDev.Application.Inventory;
namespace IstpetDev.Tests.Unit.Inventory;

[Trait("Category", "Unit"), Trait("Spec", "007")]
public sealed class DemandComposerTests
{
    [Fact]
    public void Closed_day_replaces_history_and_is_counted_once()
    {
        var start = new DateOnly(2026, 10, 5);
        var result = DemandComposer.Compose(start, start.AddDays(7),
            [new HistoricalPeriod(start, 7, 84m)],
            [new OperationalDay(start, 10m, true)]);
        Assert.Equal(82m, result.TotalQuantity);
        Assert.Equal(7, result.ObservedDays);
        Assert.Equal(11.714m, result.DailyDemand);
    }
}
```

- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~DemandComposerTests'`; crear tipos y algoritmo puro mínimo:

```csharp
namespace IstpetDev.Application.Inventory;

public sealed record HistoricalPeriod(DateOnly Start, int PeriodDays, decimal Quantity);
public sealed record OperationalDay(DateOnly Date, decimal NetQuantity, bool IsClosed);
public sealed record DemandSummary(decimal TotalQuantity, int ObservedDays,
    decimal? DailyDemand, IReadOnlyList<string> Assumptions);

public static class DemandComposer
{
    public static DemandSummary Compose(DateOnly from, DateOnly toExclusive,
        IReadOnlyList<HistoricalPeriod> history, IReadOnlyList<OperationalDay> days)
    {
        if (toExclusive <= from || history.Any(x => x.PeriodDays < 1 || x.Quantity < 0m)
            || days.Any(x => x.NetQuantity < 0m)) throw new ArgumentException("INVALID_PERIOD");
        var indexed = days.ToDictionary(x => x.Date); // El agregador entrega un día único.
        var notes = new HashSet<string>(StringComparer.Ordinal);
        decimal total = 0m;
        var observed = 0;
        for (var date = from; date < toExclusive; date = date.AddDays(1))
        {
            indexed.TryGetValue(date, out var day);
            if (day is { IsClosed: true })
            {
                total += day.NetQuantity;
                observed++;
                continue;
            }
            if (day is not null) notes.Add("partial_operational_day");
            var sources = history.Where(x => date >= x.Start && date < x.Start.AddDays(x.PeriodDays)).ToArray();
            if (sources.Length != 1)
            {
                notes.Add(sources.Length == 0 ? "missing_day" : "overlapping_history");
                continue;
            }
            total += sources[0].Quantity / sources[0].PeriodDays;
            observed++;
            if (sources[0].PeriodDays > 1) notes.Add("prorated_history");
        }
        return new(total, observed, observed == 0 ? null : decimal.Round(total / observed, 3),
            notes.OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }
}
```

- [ ] Añadir pruebas cero cerrado → demanda 0 con un día observado; día ausente → null; día abierto con historia → historia+aviso; período parcial; dos períodos superpuestos → día excluido+aviso; dos operaciones 4+6 agregadas en un único `OperationalDay`; corrección resta a neto sin editar historia.
- [ ] Integrar agregación de `Consumption` por punto/SKU/día: historia aparte, operaciones positivas menos reversión validada. Cerrar solo día pasado, atomizar versión+actor+idempotencia; creación sin fila usa versión esperada 0. Agregar tarde o revertir reabre la fila y sube versión; un replay antiguo de cierre retorna su resultado histórico sin cerrar la nueva versión.
- [ ] Adaptar `RiskCalculator`: saldo disponible cambia al aceptar; demanda usa composición, preserva salida/facts de 002 y agrega supuestos. No dividir stock por cero. `OperationalClock` en modo piloto Real usa zona de organización; Dataset/Fixed conservan ensayos y no deducen «hoy real» de operaciones futuras.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~DemandComposerTests|FullyQualifiedName~ConsumptionDayTests|FullyQualifiedName~RiskCalculatorTests|FullyQualifiedName~OperationalClockTests'` y registrar cambios de snapshots/expectativas justificados.

**Commit:** `feat(inventory): compose daily demand with explicit completeness`.

## T5 — Proteger importación de historia y saldos

**Files:** crear `src/Application/Inventory/InventoryImportGuard.cs`, `tests/IstpetDev.Tests/Integration/Inventory/InventoryImportGuardTests.cs`; modificar `src/Application/Imports/{MappingProfile.cs,ImportContext.cs,CanonicalWriter.cs,ImportService.cs}`.

**Interfaces:** `InventoryImportGuard.ValidateStockAsync(Guid locationId,Guid lotId,decimal target,DateTimeOffset? observedAt,CancellationToken ct)` retorna lista de problemas por fila; ejecutarlo antes de mutar el par. Añadir `MappingProfile.StockObservedAtUtc` nullable; al escribir corte conservar secuencia y lote/perfil. No convertir `ImportContext` en almacén de operaciones físicas.

- [ ] Crear prueba roja (método en clase con mismos atributos y constructor Postgis que T3):

```csharp
[Fact]
public async Task Old_stock_import_cannot_erase_accepted_consumption()
{
    await using var s = await InventoryScenario.CreateAsync(postgres);
    var before = await s.BalanceAsync();
    var op = Guid.NewGuid();
    using var accepted = await InventoryScenario.PostAsync(s.Receiver, "/api/v1/consumptions", op,
        s.ConsumptionBody(op, 10m, before.Version));
    accepted.EnsureSuccessStatusCode();
    await using var db = s.OpenDb();
    var request = Fixtures.Request("spec007");
    try
    {
        var service = new IstpetDev.Application.Imports.ImportService(db,
            new IstpetDev.Infrastructure.Imports.TabularReader());
        var result = await service.RunAsync(request);
        Assert.False(result.Applied);
        Assert.Contains(result.Issues, x => x.Code == "STOCK_IMPORT_CONFLICT");
    }
    finally { foreach (var file in request.Files) await file.Content.DisposeAsync(); }
    Assert.Equal(50m, (await s.BalanceAsync()).Quantity);
}
```

- [ ] Conservar constructor público actual de `ImportService(db,reader)` o actualizar explícitamente todas sus llamadas de tests si se introduce otra dependencia; la prueba anterior presupone guard creado internamente o parámetro opcional compatible. Ejecutar filtro, comprobar que implementación anterior devuelve `Applied` o cambia saldo y falla.
- [ ] Añadir `SourceKind == "import"` al cargar el diccionario de historia; no usar `ToDictionary` sobre todas las operaciones de un día. Al importar historia de fecha compartida, operaciones y reversión permanecen intactas. Revisar también `CanonicalWriter.Consumption` para conservar `SourceKind=import` y reportar origen.
- [ ] Comprobar guard tanto dry-run como commit, bajo bloqueo común; fallo de la fila no deja delta, corte ni asignación mutados. `allowPartial=true` deja otras filas válidas pero nunca aplica la protegida. Fecha reciente o misma cantidad no hacen bypass; `reserved > target` rechaza en todas las filas.
- [ ] Registrar corte inicial seguro para filas sin actividad piloto. Mantener importación existente cuando `PilotEnabled=false`, sin exigir fecha nueva a fixtures 001; migración y piloto guardan «fecha de observación desconocida» cuando corresponde.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~InventoryImportGuardTests|FullyQualifiedName~ImportSafetyTests|FullyQualifiedName~ImportServiceTests'`.

**Commit:** `fix(inventory): preserve operational stock across reimports`.

## T6 — Consultas conciliadas y riesgo con alcance

**Files:** crear `src/Application/Inventory/InventoryQueries.cs`, `tests/IstpetDev.Tests/Integration/Inventory/{InventoryQueryTests.cs,InventoryRiskTests.cs}`; completar `InventoryController.cs` y `src/Api/Controllers/RisksController.cs`.

**Interfaces:** `InventoryQueries.BalancesAsync(Guid accountId,InventoryFilter filter,CancellationToken ct)` y `MovementsAsync(Guid accountId,MovementFilter filter,CancellationToken ct)`; filtros/campos según tabla HTTP del plan. Retornar `PagedResult<T>(IReadOnlyList<T> Items,string? NextCursor,long AsOfSequence)` más datos de corte para movimientos.

- [ ] Prueba roja numérica: corte 60, consumo 10 y reversión 10 producen cierre 60; mover recepción 004 de 12 produce72 sin un movimiento adicional de 007. Antes de disponer de 004, el caso corte/consumo/reversión corre con API007 y el de recepción se reserva a T8.
- [ ] Prueba paginada: límite 1, consumir 10 y después 5; primera página fija `asOfSequence`; insertar consumo 2 entre páginas y verificar que la segunda lectura histórica mantiene cierre 45 y excluye ese nuevo movimiento. Consulta nueva muestra 43. Cursor manipulado o usado con otra ubicación devuelve 400 y no filtra datos.
- [ ] Calcular apertura para filtro de fecha usando corte+movimientos anteriores al `fromUtc`, no empezando en cero; ordenar por `RecordedSequence`. Usar `AsNoTracking`, proyecciones DTO e índices del plan; no cargar todos los movimientos de organización en memoria para paginar.
- [ ] Pedir intervalo anterior al primer corte reconstruible: 422 `HISTORY_BEFORE_BASELINE` con límite disponible. La UI ofrece antecedentes sin saldo reconstruido; no fabrica una conciliación histórica.
- [ ] Prueba de riesgo:60/12=5 días; consumo 12 aceptado →48/12=4 con día abierto y fuente histórica; cierre del día permite recalcular demanda según T4. Cuenta Receptor no ve `items` ni `facts` de otro punto aunque use `GET /risks` sin filtro.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~InventoryQueryTests|FullyQualifiedName~InventoryRiskTests|FullyQualifiedName~InventoryAuthorizationTests'`.

**Commit:** `feat(inventory): expose reconciled balances and scoped risk`.

## T7 — Tipos compartidos y consulta Web

**Files:** crear `libs/shared-core/src/contracts/inventory.contract.ts`; modificar `contracts/index.ts`; crear archivos del slice `apps/web/src/entities/inventory/` y página `apps/web/src/pages/inventory/` listados en plan; modificar rutas/exports.

**Consumes:** respuestas reales T3/T6. **Produces:** puerto `InventoryApi` de lectura y DTO TypeScript nuevos sin romper `InventoryItem` existente.

- [ ] Capturar respuestas HTTP del fixture de T6 como contratos esperados en tests backend; incluir cadena decimal y zona/fecha. Tipo mínimo en contrato compartido:

```typescript
export type DecimalString = string;
export interface InventoryBalanceDto {
  locationId: string;
  lotId: string;
  productId: string;
  unit: string;
  onHand: DecimalString;
  reserved: DecimalString;
  available: DecimalString;
  usable: boolean;
  version: number;
  observedAtUtc: string | null;
  lastOperationAtUtc: string | null;
}
export interface InventoryWriteResponse {
  consumptionId: string;
  movementId: string;
  operationId: string;
  acceptedAtUtc: string;
  businessDate: string;
  onHand: DecimalString;
  balanceVersion: number;
  sequence: number;
}
```

- [ ] Implementar servicio HTTP de lectura desde `entities/inventory/api`, exportarlo por `index.ts` y consumirlo solo desde capas superiores. Los DTO completos incluyen filtros, lista, movimientos, días y errores del plan, sin `any` ni conversión implícita a números.
- [ ] Crear `/inventory` protegida con sesión real y rol Planificador/Gerencia. Mostrar saldo físico/reservado/disponible, lote/unidad, bloqueo/vencimiento, corte, última operación, apertura/cierre e historia de correcciones. Un dato desconocido lleva texto explícito; una celda vacía no equivale a cero.
- [ ] Ensayo de navegador E2E-10: filtros, paginación, volver desde riesgo, cargar/vacío/error y cursor vencido; cuenta sin permiso no abre consulta ni API. No agregar captura física de Bodega a Web.
- [ ] Ejecutar `npm run build:shared`, `npm run lint:web`, `npm run build:web`; documentar prueba visual. La compilación no reemplaza el recorrido con datos de API.

**Commit:** `feat(web): add inventory balance and movement views`.

## T8 — Unir 002/004 y proteger el reloj/reset

**Files:** modificar `src/Application/Logistics/{RoutePublisher.cs,Replanner.cs,OperationalClock.cs}`, `src/Infrastructure/Demo/DemoResetService.cs`; integrar `src/Application/Field/{DispatchService.cs,ReceiptService.cs}` cuando existan y el servicio real de devolución que publique 004; crear `tests/IstpetDev.Tests/Integration/Inventory/InventoryFieldIntegrationTests.cs` y `E2E/InventoryJourneyTests.cs`.

- [ ] Verificar endpoints de 004 contra base real; si siguen ausentes, registrar dependencia y continuar T9 sin etiquetar E2E-08 como aprobado. No crear un recibo insertando saldo directamente para aparentar integración.
- [ ] Prueba roja de competencia de reservas: saldo 60, consumo 40 y reserva 30 concurrentes; solo puede aceptarse una combinación que cumpla `reserved <= onHand`, sin operaciones parciales. Contextos EF separados; coordinar arranque con barrera de tests, no `Thread.Sleep`.
- [ ] Unificar transacciones/bloqueo del plan antes de leer y validar saldo, manteniendo cálculos costosos fuera. Si se usa concurrencia optimista existente, demostrar el mismo resultado e idempotencia para todos los `SaveChanges`; no mezclar dos dueños de transacción.
- [ ] E2E HTTP de ruta→reserva→despacho→recepción 12→consumo 10: saldo del punto 60→72→62; recepción replay no suma otras 12, consumo replay no resta otras 10. Devolución 004 cambia custodio/ubicación exactamente una vez; consulta 007 solo muestra movimiento existente.
- [ ] Configurar reloj Real con zona de organización para piloto; fixture Fixed. Probar captura futura rechazada; una captura tardía válida no mueve retroactivamente un saldo histórico. Probar `POST /demo/reset` denegado tras primer consumo piloto aunque el endpoint de demo se hubiera habilitado accidentalmente.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~InventoryFieldIntegrationTests|FullyQualifiedName~InventoryJourneyTests|FullyQualifiedName~RoutePublisherTests|FullyQualifiedName~ReplanningTests'`.

**Commit:** `fix(inventory): coordinate field movements and reservations`.

## T9 — Captura PWA y cola offline

**Files:** crear archivos de `apps/mobile/src/app/inventory/` del plan; modificar rutas, `app.config.ts`, `home/home.page.ts` y adaptador de sesión; extender contratos de `inventory.contract.ts`.

**Interfaces:** `InventoryApiService.recordConsumption(body,key)` llama API real; `InventoryOutboxService.enqueue(identity,body)`, `sync(identity)` y `list(identity)`; `identity={accountId,organizationId}` obtenida de `/me`. Cola conserva payload inmutable más estados/respuesta/error; no reutiliza `OutboxItem` de recibos.

- [ ] Preparar ensayo rojo E2E-09 con formulario ausente; definir registros IndexedDB y transición mínima antes de UI:

```typescript
export type InventorySyncState = 'Pending' | 'Sending' | 'Accepted'
  | 'Conflict' | 'SessionRequired' | 'Rejected';
export function stateForFailure(status: number): InventorySyncState {
  if (status === 401) return 'SessionRequired';
  if (status === 409) return 'Conflict';
  if (status === 403 || status === 404 || status === 400 || status === 422) return 'Rejected';
  return 'Pending'; // Fallo de transporte, 429 o 5xx reintentables según Problem Details.
}
```

- [ ] Antes de usar la función, tratar `retryable=false` del servidor como `Rejected`; no reintentar indefinidamente un error definitivo. Guardar operación en una transacción IndexedDB antes de mostrar éxito local. Clave compuesta `[organizationId,accountId,operationId]`; `crypto.randomUUID()` no viene de mock.
- [ ] Formulario solo Receptor real, lote del snapshot autorizado, cantidad decimal textual, saldo/hora del snapshot. Aceptar fecha según contrato; guardar `expectedBalanceVersion` observado. Cierre/reversión visibles cuando hay red; pedir confirmación humana para anular consumo propio con motivo.
- [ ] Al recuperar red enviar exactamente la misma clave/cuerpo; reenviar pendientes `Sending` tras reinicio. Si respuesta fue aceptada pero se perdió, replay recupera ID original. Ante conflicto conservar registro, refrescar saldo y crear otra operación solo después de resolver estado de la anterior; nunca editar una clave ya aceptada.
- [ ] Probar dos capturas offline del mismo lote/versión: sincronización secuencial acepta una y conserva el conflicto de la otra. Reenvío autorizado tras revisar saldo usa clave nueva; ambas conservan su historia local y solo las aceptadas afectan stock.
- [ ] Ensayar teléfono: modo avión → capturar 10 → cerrar/reabrir → cola conserva 10 → cuenta distinta no ve/envía → regresar cuenta original → sincronizar una vez → saldo 50. Probar revocación, sesión vencida, falta de espacio local y 422 sin borrar captura. Registrar aceptación únicamente al recibir respuesta del servidor real.
- [ ] Ejecutar `npm run build:shared` y `npm run build:mobile`; verificar PWA instalada por HTTPS en E2E-09 y que la configuración de inventario no inyecta mocks. Conservar funcionamiento del outbox de recibos 004.

**Commit:** `feat(mobile): capture inventory consumption with durable retries`.

## T10 — Entradas, demanda no atendida y solicitudes

**Files:** crear `src/Domain/Entities/InventoryExtensionEntities.cs`, migración `InventoryExceptions`; crear servicios `WarehouseEntryService.cs`, `DemandObservationService.cs`, `ReplenishmentRequestService.cs`, `src/Api/Controllers/InventoryExceptionsController.cs`; modificar DbSets/mapeo/grants/DI y DTO; crear `tests/IstpetDev.Tests/Integration/Inventory/InventoryExtensionTests.cs`; PWA `pages/warehouse-entry/warehouse-entry.page.ts` y controles de faltante/reposición; vista de solicitudes en Web.

**Interfaces:** métodos `CreateAsync(Guid accountId,<Request> request,CancellationToken ct)` con requests definidos en tabla de ampliación del plan; outputs ID, versión, hora y referencias. Entidades nuevas no sustituyen `Receipt`004 ni importador 001.

- [ ] Test rojo de entrada: saldo 800 en bodega +20 →820; replay y otra clave con misma referencia externa/lote no duplican. 0/negativo/unidad incorrecta o lote ajeno rechazan antes de movimiento.
- [ ] Test rojo de faltante: consumo físico 6 sobre 60 deja 54; declaración solicitada 10 enlazada a consumo 6 crea no atendido 4 y mantiene 54. Reutilizar el mismo consumo en otra observación activa o declarar solicitado 5 rechaza. Revertir consumo marca observación para revisión; no inventa demanda cero.
- [ ] Test rojo de solicitud: crear pedido 20 no cambia stock, reserva ni entrada confirmada usada por riesgo; replay devuelve solicitud anterior. Usuario ajeno no crea ni consulta.
- [ ] Implementar ramas `WarehouseInbound`, registro de observación y solicitud mediante runner común; cantidades netas derivadas de consumos aceptados y sus reversos. Faltante abierto aparece como motivo de riesgo/atención de 002; no se suma otra vez como consumo ni cuenta una solicitud como stock en tránsito. Mantener versión/fuente del supuesto.
- [ ] UI Bodega/PWA registra referencia física y lote, confirma aceptación real; Receptor/PWA declara necesidad/faltante; Planificador/Web consulta solicitudes. Si un endpoint está deshabilitado, no mostrar controles activos.
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~InventoryExtensionTests'`, builds Shared/Web/Mobile y E2E-11.

**Commit:** `feat(inventory): record warehouse entries and unmet demand`.

## T11 — Investigación y ajustes con evidencia

**Files:** extender `InventoryExtensionEntities.cs`; crear `src/Application/Inventory/InventoryCaseService.cs` y `InventoryCasePolicy.cs`, tests `Unit/Inventory/InventoryCasePolicyTests.cs` y `Integration/Inventory/InventoryCaseTests.cs`; crear `apps/web/src/pages/inventory-cases/{inventory-cases.page.ts,index.ts}` y captura de conteo en página PWA de inventario; completar controlador/contratos.

**Interfaces:** `OpenAsync`, `StartAsync`, `CloseAsync` con cuenta y requests de la tabla HTTP; tipo `InventoryCaseEvent` append-only para transiciones. `CountDifference(decimal counted,decimal onHand)` produce diferencia firmada; stock afectado solo al cerrar con aprobación y versiones vigentes.

- [ ] Prueba roja de regla mínima:

```csharp
[Theory]
[InlineData(50, 54, -4)]
[InlineData(58, 54, 4)]
[InlineData(54, 54, 0)]
public void Count_difference_is_signed(decimal counted, decimal onHand, decimal expected)
{
    Assert.Equal(expected, InventoryCasePolicy.CountDifference(counted, onHand));
}
```

Crear `InventoryCasePolicyTests` con namespace `IstpetDev.Tests.Unit.Inventory`, using `IstpetDev.Application.Inventory` y atributos `Category=Unit`, `Spec=007`. Implementación pura:

```csharp
namespace IstpetDev.Application.Inventory;
public static class InventoryCasePolicy
{
    public static decimal CountDifference(decimal counted, decimal onHand)
    {
        if (counted < 0m || decimal.Round(counted, 3) != counted)
            throw new ArgumentException("INVALID_COUNT");
        return counted - onHand;
    }
}
```

- [ ] Integración: saldo 54, conteo 50 abre caso sin mover saldo. `Open→Investigating→ConfirmedLoss` con motivo/evidencia y versión vigente crea movimiento de 4, saldo 50 y cierre una vez. Caso descartado conserva 54; conteo 58 con resultado `CorrectedCount` crea entrada de 4 sin etiquetarla merma. Diferencia 0 cierra sin movimiento.
- [ ] Añadir rechazos: pérdida sin evidencia; cerrar desde Open; Bodega intentando aprobar; saldo cambió después del conteo; pérdida excede disponible por reservas; versión vieja del caso; misma clave/cuerpo tras éxito devuelve cierre previo; misma clave alterada 409. Reabrir investigación requiere nuevo caso vinculado, no borrar cierre ni revertir saldo automáticamente.
- [ ] Mostrar actor/motivo/referencia del conteo y cierre en Web; evidencia de foto si existe usa acceso privado 004. No aceptar URL remota arbitraria como archivo del sistema. Estados de UI dicen «por investigar», «descartada», «merma confirmada» o «ajuste de conteo».
- [ ] Ejecutar `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~InventoryCasePolicyTests|FullyQualifiedName~InventoryCaseTests'`, builds y E2E-12. RF-15 no se declara completo como detector estadístico: documentar pendiente de definición/calibración en su nota dueña.

**Commit:** `feat(inventory): investigate and close stock discrepancies`.

## T12 — Regresión y entrega verificable

**Files:** completar `tests/IstpetDev.Tests/E2E/InventoryJourneyTests.cs`, `tests/IstpetDev.Tests/Performance/InventoryPerformanceTests.cs`; modificar `.github/workflows/ci.yml` si falta compilación móvil; actualizar `verification.md`, `test-e2e.md`, `docs/specs/README.md` y notas de continuidad/cobertura con estados reales.

- [ ] Ejecutar todos los escenarios del alcance entregado. Pruebas de rendimiento implementan carga/volumen pactados en T0, registran p95/errores; si falta acuerdo, mantener NFR-04 pendiente sin inventar medición.
- [ ] Automatizar CI con las categorías existentes, comprobar que las clases 007 se descubren y no quedan omitidas. Guardar TRX por categoría y revisar counters; no basta exit 0 de filtro vacío.

```bash
dotnet build IstpetDev.sln
dotnet test tests/IstpetDev.Tests --filter 'Category=Unit' --logger 'trx;LogFileName=Unit.trx' --results-directory artifacts/spec007/regression
dotnet test tests/IstpetDev.Tests --filter 'Category=Integration' --logger 'trx;LogFileName=Integration.trx' --results-directory artifacts/spec007/regression
dotnet test tests/IstpetDev.Tests --filter 'Category=E2E' --logger 'trx;LogFileName=E2E.trx' --results-directory artifacts/spec007/regression
python3 scripts/db/check-test-results.py artifacts/spec007/regression
npm run build:shared
npm run lint:web
npm run build:web
npm run build:mobile
git diff --check
```

- [ ] Registrar fallos previos separados de regresiones 007; corregir los introducidos y repetir su tramo. No marcar E2E de teléfono, recepción real o datos autorizados como cubierto por Testcontainers.
- [ ] Revisar correspondencia FR/CE → tarea → E2E → resultado; si solo MVP pasa, estado «MVP verificado; ampliación pendiente». Spec completo exige también CE-07/11 y decisiones pendientes resueltas.
- [ ] Antes de piloto real, comprobar organización, cuentas/ubicaciones autorizadas, modo Real, demo/reset deshabilitados y evidencia de prelaunch para ese entorno. El plan no publica ningún servicio ni despliega automáticamente.

**Commit:** `test(inventory): verify pilot inventory journeys and evidence`.
