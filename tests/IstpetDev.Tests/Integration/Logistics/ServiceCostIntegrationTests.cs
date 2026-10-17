using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Application.Imports;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Imports;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public sealed class ServiceCostIntegrationTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static async Task<ImportReport> Import(ApplicationDbContext db, ImportRequest request)
    {
        try { return await new ImportService(db, new TabularReader()).RunAsync(request); }
        finally { db.ChangeTracker.Clear(); foreach (var file in request.Files) await file.Content.DisposeAsync(); }
    }

    private static ImportRequest Csv(ImportEntity entity, string content) =>
        new([new(entity, entity.Key() + ".csv", new MemoryStream(Encoding.UTF8.GetBytes(content)))], MappingProfile.Auto(), false, false, null);

    private const string Products = "sku,name,base_unit,weight_per_unit,volume_per_unit,unit_value,units_per_package\nDES-01,Desinfectante,L,1.05,0.001,3.50,10\n";
    private const string Vehicles = "code,depot_code,max_weight,max_volume,cost_per_km,vehicle_type,usage_scope\nVEH-01,BOD-UIO,1200,8,0.45,camioneta,urbano\n";

    [Fact]
    public async Task Optional_columns_import_update_and_survive_old_files_without_resetting_values()
    {
        await using var db = TestDb.Context(postgres.NewDatabase());
        Assert.True((await Import(db, Fixtures.Request("demo-base"))).Applied);
        Assert.True((await Import(db, Csv(ImportEntity.Products, Products))).Applied);
        Assert.True((await Import(db, Csv(ImportEntity.Vehicles, Vehicles))).Applied);
        var report = await Import(db, Fixtures.Request("demo-base"));
        Assert.True(report.Applied);
        var product = await db.Products.SingleAsync(p => p.Sku == "DES-01");
        var vehicle = await db.Vehicles.SingleAsync();
        Assert.Equal(3.5m, product.UnitValue);
        Assert.Equal(10m, product.UnitsPerPackage);
        Assert.Equal("camioneta", vehicle.VehicleType);
        Assert.Equal("urbano", vehicle.UsageScope);
        Assert.Contains("unit_value_preserved", report.Entities.Single(e => e.Entity == "products").Assumptions.Keys);
        Assert.True((await Import(db, Csv(ImportEntity.Products, Products.Replace("3.50,10", "0,5")))).Applied);
        Assert.Equal(0m, (await db.Products.SingleAsync(p => p.Sku == "DES-01")).UnitValue);
    }

    [Theory]
    [InlineData("product", "-1,10")]
    [InlineData("product", "3.5,0")]
    [InlineData("product", "3.5,-10")]
    [InlineData("vehicle", "avion,urbano")]
    [InlineData("vehicle", "camioneta,volador")]
    public async Task Invalid_new_fields_reject_transaction_without_changing_existing_data(string entity, string value)
    {
        await using var db = TestDb.Context(postgres.NewDatabase());
        await Import(db, Fixtures.Request("demo-base"));
        var report = entity == "product"
            ? await Import(db, Csv(ImportEntity.Products, Products.Replace("3.50,10", value)))
            : await Import(db, Csv(ImportEntity.Vehicles, Vehicles.Replace("camioneta,urbano", value)));
        Assert.False(report.Applied);
        Assert.True(report.ErrorCount > 0);
        Assert.Null((await db.Products.SingleAsync(p => p.Sku == "DES-01")).UnitValue);
        Assert.Null((await db.Vehicles.SingleAsync()).VehicleType);
    }

    [Fact]
    public async Task Migration_upgrades_populated_demo_preserving_every_existing_row_and_value()
    {
        await using var db = TestDb.Context(postgres.NewDatabase());
        Assert.True((await Import(db, Fixtures.Request("demo-base"))).Applied);
        var vehicle = await db.Vehicles.SingleAsync();
        // El esquema anterior a ServiceCostCatalog tampoco contenía MetadataJson.
        // No poblar columnas futuras al recrear esa base histórica mediante downgrade.
        vehicle.MetadataJson = null;
        foreach (var product in await db.Products.ToListAsync()) product.MetadataJson = null;
        db.RoutePlans.Add(new() { Code = "HIST-240", VehicleId = vehicle.Id, Status = "Proposed", TotalCost = 42.25m,
            PlannedStartUtc = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE \"Product\" SET \"MetadataJson\" = NULL; UPDATE \"Vehicle\" SET \"MetadataJson\" = NULL;");
        var before = await Snapshot(db);
        // Recrear el esquema inmediatamente anterior, con los datos de la demo ya cargados.
        await db.GetService<IMigrator>().MigrateAsync("20261015234714_CustodyChainStates");
        Assert.DoesNotContain("20261016222608_ServiceCostCatalog", await db.Database.GetAppliedMigrationsAsync());
        await DatabaseInitializer.MigrateAsync(db);
        var after = await Snapshot(db);
        Assert.Equal(before.Keys.Order(), after.Keys.Order());
        foreach (var table in before.Keys) Assert.Equal(before[table], after[table]);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Null((await db.Products.AsNoTracking().FirstAsync()).UnitValue);
        Assert.Null((await db.Vehicles.AsNoTracking().SingleAsync()).UsageScope);
    }

    private static async Task<Dictionary<string, string>> Snapshot(ApplicationDbContext db)
    {
        var tables = db.Model.GetEntityTypes().Select(e => e.GetTableName()!).Distinct().Order().ToArray();
        var result = new Dictionary<string, string>();
        await db.Database.OpenConnectionAsync();
        try
        {
            foreach (var table in tables)
            {
                // Identificadores exclusivamente del modelo EF, nunca entrada del cliente.
                // MetadataJson se añadió después de ServiceCostCatalog; no forma parte del
                // contrato histórico que esta prueba de preservación compara.
                await using var command = new NpgsqlCommand($"SELECT coalesce(jsonb_agg(to_jsonb(t) - 'MetadataJson' ORDER BY t.\"Id\"), '[]')::text FROM \"{table}\" t", (NpgsqlConnection)db.Database.GetDbConnection());
                result[table] = (string)(await command.ExecuteScalarAsync())!;
            }
        }
        finally { await db.Database.CloseConnectionAsync(); }
        return result;
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(0, 0)]
    [InlineData(27, 27)]
    public async Task Real_api_and_matcher_expose_breakdown_and_respect_explicit_point_service(int? explicitMinutes, int expectedMinutes)
    {
        var connection = postgres.NewDatabase();
        await using var factory = new ApiFactory(connection, overrides: Settings());
        var client = factory.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True((await Import(db, Fixtures.Request("demo-base"))).Applied);
        Assert.True((await Import(db, Csv(ImportEntity.Products, Products))).Applied);
        Assert.True((await Import(db, Csv(ImportEntity.Vehicles, Vehicles))).Applied);
        var product = await db.Products.SingleAsync(p => p.Sku == "DES-01");
        var vehicle = await db.Vehicles.SingleAsync();
        var warehouse = await db.Locations.SingleAsync(l => l.Kind == LocationKinds.Warehouse);
        var point = await db.ServicePoints.SingleAsync(p => p.Code == "HOS-001");
        point.ServiceMinutes = explicitMinutes;
        await db.SaveChangesAsync();
        var quantity = 200m; // 210 kg, 20 bultos, 1 ayudante
        var loads = new[] { new StopLoadDto(product.Id, product.Sku, quantity, product.BaseUnit) };
        var stops = new RouteStopCandidate[] {
            new(1, RouteStopKind.Pickup, null, warehouse.Id, warehouse.Code, warehouse.Name,
                new((decimal)warehouse.Position!.Y, (decimal)warehouse.Position.X), 210, 0.2m, new(0, 0), new(23, 59), 20, loads),
            new(2, RouteStopKind.Delivery, point.Id, null, point.Code, point.Name,
                new((decimal)point.Position!.Y, (decimal)point.Position.X), -210, -0.2m, new(0, 0), new(23, 59), 15, loads)
        };
        var start = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
        var response = await client.PostAsJsonAsync("/api/v1/route-plans/evaluate", new { stops, vehicleId = vehicle.Id, plannedStartUtc = start });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var evaluated = (await response.Content.ReadFromJsonAsync<RouteEvaluationResult>())!;
        Assert.True(evaluated.IsValid, string.Join(";", evaluated.ValidationErrors));
        var breakdown = Assert.IsType<ServiceCostBreakdown>(evaluated.ServiceCostBreakdown);
        Assert.Equal(evaluated.TotalCost, breakdown.Total);
        Assert.Equal(1, breakdown.HelperCount);
        Assert.Equal(3.5m, breakdown.Shrink); // 200 L x USD 3.5 x 0.5%
        Assert.Equal("estimado", breakdown.Label);
        Assert.Equal(new DateOnly(2026, 10, 16), breakdown.FuelPrice.Date);
        var delivery = evaluated.Stops[1];
        Assert.Equal(explicitMinutes.HasValue ? expectedMinutes : 20,
            (delivery.PlannedDepartureUtc - delivery.PlannedArrivalUtc).TotalMinutes);
        var matcher = scope.ServiceProvider.GetRequiredService<IVehicleMatcher>();
        var matches = await matcher.MatchVehiclesAsync(stops, start);
        var match = Assert.Single(matches);
        Assert.True(match.IsMatch);
        Assert.Equal(1, match.ServiceCostBreakdown!.HelperCount);
        Assert.Equal(match.EstimatedTotalCost, match.ServiceCostBreakdown.Total);
        Assert.Equal(breakdown.Shrink, match.ServiceCostBreakdown.Shrink);
        Assert.Equal(breakdown.FuelPrice, match.ServiceCostBreakdown.FuelPrice);
        // API restringida: el nuevo desglose no abre el endpoint al receptor.
        using var receiver = factory.ClientFor(TestTokens.Receiver);
        Assert.Equal(HttpStatusCode.Forbidden, (await receiver.PostAsJsonAsync("/api/v1/route-plans/evaluate", new { stops })).StatusCode);
        if (!explicitMinutes.HasValue)
        {
            var folder = Path.GetFullPath(Path.Combine(Fixtures.Folder("demo-base"), "../../..", "artifacts/issue240"));
            Directory.CreateDirectory(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, "demo-route.json"), JsonSerializer.Serialize(
                new { dataset = "demo-base", synthetic = true, product = product.Sku, quantity, vehicle = vehicle.Code, evaluated, match },
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_consumers_keep_exact_legacy_cost_without_configuration_or_profile(bool configured)
    {
        await using var db = TestDb.Context(postgres.NewDatabase());
        await Import(db, Fixtures.Request("demo-base"));
        var options = new LogisticsOptions();
        if (configured) options.ServiceCost = Unit.Logistics.ServiceCostCalculatorTests.Configured().ServiceCost;
        var wrapped = Options.Create(options);
        var distance = new DistanceProvider(db, new HttpClient(), wrapped, NullLogger<DistanceProvider>.Instance);
        var vehicle = await db.Vehicles.SingleAsync();
        var stops = new RouteStopCandidate[] {
            new(1, RouteStopKind.Pickup, null, null, "P", "P", new(-0.2m, -78.5m), 500, 0, new(0, 0), new(23, 59), 20),
            new(2, RouteStopKind.Delivery, null, null, "D", "D", new(-0.3m, -78.6m), -500, 0, new(0, 0), new(23, 59), 15)
        };
        var start = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
        var result = await new RouteEvaluator(distance, wrapped, db: db).EvaluateSequenceAsync(stops, vehicle, start);
        Assert.Equal(15, (result.Stops[1].PlannedDepartureUtc - result.Stops[1].PlannedArrivalUtc).TotalMinutes);
        var legacy = new ServiceCostCalculator(new LogisticsOptions());
        var calc = await ServiceCostCalculator.LoadAsync(options, stops, db, default);
        Assert.Equal(legacy.Calculate(12.345m, 123.4m, vehicle, 500, stops).Total, calc.Calculate(12.345m, 123.4m, vehicle, 500, stops).Total);
        var baselineMatcher = new VehicleMatcher(db, distance, Options.Create(new LogisticsOptions()));
        var actual = Assert.Single(await new VehicleMatcher(db, distance, wrapped).MatchVehiclesAsync(stops, start));
        var baseline = Assert.Single(await baselineMatcher.MatchVehiclesAsync(stops, start));
        Assert.Equal(baseline.EstimatedTotalCost, actual.EstimatedTotalCost);
        var baselineEval = await new RouteEvaluator(distance, Options.Create(new LogisticsOptions())).EvaluateSequenceAsync(stops, vehicle, start);
        Assert.Equal(baselineEval.TotalCost, result.TotalCost);
        Assert.Equal(baselineEval.TotalDurationMinutes, result.TotalDurationMinutes);
    }

    private static Dictionary<string, string?> Settings() => new()
    {
        ["Logistics:ServiceCost:FuelPricePerGallonUsd"] = "3.785",
        ["Logistics:ServiceCost:FuelPriceEffectiveDate"] = "2026-10-16",
        ["Logistics:ServiceCost:FuelPriceSource"] = "sintético",
        ["Logistics:ServiceCost:HelperCostPerHour"] = "4",
        ["Logistics:ServiceCost:VehicleProfiles:camioneta:LitersPer100Km"] = "10",
        ["Logistics:ServiceCost:VehicleProfiles:camioneta:WearPerKm"] = "0.1"
    };
}
