using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public sealed class VehicleRestrictionIntegrationTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static Dictionary<string, string?> Settings() => new ConfigurationBuilder()
        .AddJsonFile(Path.GetFullPath(Path.Combine(Fixtures.Folder("demo-base"), "../../../docs/evidence/244-restricciones/appsettings.RestriccionesSinteticas.json")))
        .Build().AsEnumerable().ToDictionary(p => p.Key, p => p.Value);
    private static DateTime Start(int localHour) => new(2026, 10, 16, localHour + 5, 0, 0, DateTimeKind.Utc);

    private static async Task<(Vehicle Heavy, Vehicle Urban, RouteStopCandidate[] Stops, Guid Order)> Seed(ApplicationDbContext db)
    {
        var request = Fixtures.Request("demo-base");
        try { Assert.True((await new IstpetDev.Application.Imports.ImportService(db, new IstpetDev.Infrastructure.Imports.TabularReader()).RunAsync(request)).Applied); }
        finally { foreach (var file in request.Files) await file.Content.DisposeAsync(); }
        var heavy = await db.Vehicles.SingleAsync(); heavy.VehicleType = "camion_pesado"; heavy.UsageScope = "extraurbano";
        var urban = new Vehicle { Code = "URB-244", DepotLocationId = heavy.DepotLocationId, VehicleType = "camioneta", UsageScope = "urbano", MaxWeight = 1000, MaxVolume = 20, IsActive = true };
        db.Vehicles.Add(urban);
        var warehouse = await db.Locations.SingleAsync(l => l.Id == heavy.DepotLocationId);
        var point = await db.ServicePoints.FirstAsync(); point.Region = "Valle sintético";
        var product = await db.Products.FirstAsync();
        var order = new DeliveryOrder { OrderNumber = "PED-244", DestinationServicePointId = point.Id, RequiredDateUtc = Start(7), Status = "Pending", Priority = "Normal", SourceType = "Manual" };
        db.DeliveryOrders.Add(order);
        db.DeliveryOrderLines.Add(new() { DeliveryOrderId = order.Id, ProductId = product.Id, RequestedQuantity = 1 });
        await db.SaveChangesAsync();
        return (heavy, urban, [
            new(1, RouteStopKind.Pickup, null, warehouse.Id, warehouse.Code, warehouse.Name, new((decimal)warehouse.Position!.Y, (decimal)warehouse.Position.X), 10, 0.01m, new(0,0), new(23,59), 10),
            new(2, RouteStopKind.Delivery, point.Id, null, point.Code, point.Name, new((decimal)point.Position!.Y, (decimal)point.Position.X), -10, -0.01m, new(8,0), new(17,0), 15)
        ], order.Id);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(10, true)]
    public async Task Configured_matcher_rejects_at_0700_accepts_at_1000_and_keeps_urban_warning_selectable(int hour, bool accepted)
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: Settings());
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var data = await Seed(db);
        var options = scope.ServiceProvider.GetRequiredService<IOptions<LogisticsOptions>>().Value;
        Assert.Single(options.VehicleRestrictions); Assert.NotEmpty(options.UrbanRegions);
        var result = await scope.ServiceProvider.GetRequiredService<IVehicleMatcher>().MatchVehiclesAsync(data.Stops, Start(hour));
        var heavy = Assert.Single(result, m => m.Vehicle.Id == data.Heavy.Id);
        Assert.Equal(accepted, heavy.IsMatch);
        if (accepted) Assert.Null(heavy.DiscardReason); else Assert.StartsWith("restriccion_horaria", heavy.DiscardReason);
        var urban = Assert.Single(result, m => m.Vehicle.Id == data.Urban.Id);
        Assert.True(urban.IsMatch); Assert.Null(urban.DiscardReason);
        Assert.StartsWith("uso_extraurbano", Assert.Single(urban.Warnings));
        // Evidencia del motor real con PostGIS descartable; nunca producción.
        var directory = Path.GetFullPath(Path.Combine(Fixtures.Folder("demo-base"), "../../../artifacts/issue244")); Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, $"matches-{hour:00}00.json"), JsonSerializer.Serialize(new {
            synthetic = true, localHour = hour, plannedStartUtc = Start(hour),
            vehicleMatches = result.Select(m => new { vehicleId = m.Vehicle.Id, vehicleCode = m.Vehicle.Code, m.IsMatch, m.DiscardReason, m.Warnings,
                m.Vehicle.MaxWeight, m.Vehicle.MaxVolume, m.WeightUtilizationPct, estimatedCost = m.EstimatedTotalCost, m.DistanceToFirstPickupKm })
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
    }

    [Fact]
    public async Task Http_suggest_exposes_reason_and_warning_and_confirm_cannot_bypass_the_restriction()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: Settings());
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var data = await Seed(db); var planner = api.ClientFor(TestTokens.Planner);
        var response = await planner.PostAsJsonAsync("/api/v1/route-plans/suggest", new { orderIds = new[] { data.Order }, plannedDate = "2026-10-16" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var matches = json.GetProperty("options")[0].GetProperty("vehicleMatches").EnumerateArray().ToArray();
        var heavy = Assert.Single(matches, m => m.GetProperty("vehicleId").GetGuid() == data.Heavy.Id);
        Assert.False(heavy.GetProperty("isMatch").GetBoolean());
        Assert.Contains("restriccion_horaria", heavy.GetProperty("discardReason").GetString());
        var urban = Assert.Single(matches, m => m.GetProperty("vehicleId").GetGuid() == data.Urban.Id);
        Assert.True(urban.GetProperty("isMatch").GetBoolean());
        Assert.Contains("uso_extraurbano", urban.GetProperty("warnings")[0].GetString());
        var confirm = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new { vehicleId = data.Heavy.Id, plannedStartUtc = Start(7), stops = data.Stops });
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Contains("restriccion_horaria", await confirm.Content.ReadAsStringAsync());
        Assert.False(await db.RoutePlans.AnyAsync());
        var good = await planner.PostAsJsonAsync("/api/v1/route-plans/evaluate", new { vehicleId = data.Heavy.Id, plannedStartUtc = Start(10), stops = data.Stops });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode); Assert.True((await good.Content.ReadFromJsonAsync<RouteEvaluationResult>())!.IsValid);
        using var receiver = api.ClientFor(TestTokens.Receiver);
        Assert.Equal(HttpStatusCode.Forbidden, (await receiver.PostAsJsonAsync("/api/v1/route-plans/evaluate", new { stops = data.Stops })).StatusCode);
    }

    [Fact]
    public async Task No_configuration_preserves_original_eligibility_cost_and_empty_warnings()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var data = await Seed(db); var distance = scope.ServiceProvider.GetRequiredService<IDistanceProvider>();
        var defaults = new LogisticsOptions();
        var baseline = await new VehicleMatcher(db, distance, Options.Create(defaults)).MatchVehiclesAsync(data.Stops, Start(7));
        var explicitEmpty = new LogisticsOptions { VehicleRestrictions = [], UrbanRegions = [] };
        var actual = await new VehicleMatcher(db, distance, Options.Create(explicitEmpty)).MatchVehiclesAsync(data.Stops, Start(7));
        Assert.All(actual, m => { Assert.True(m.IsMatch); Assert.Null(m.DiscardReason); Assert.Empty(m.Warnings); });
        Assert.Equal(baseline.Select(m => (m.Vehicle.Id, m.EstimatedTotalCost)), actual.Select(m => (m.Vehicle.Id, m.EstimatedTotalCost)));
    }

    [Fact]
    public async Task Waiting_for_a_delivery_window_can_enter_a_later_restriction()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: Settings());
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var data = await Seed(db);
        var result = await scope.ServiceProvider.GetRequiredService<IVehicleMatcher>().MatchVehiclesAsync(data.Stops, Start(5));
        Assert.StartsWith("restriccion_horaria", result.Single(m => m.Vehicle.Id == data.Heavy.Id).DiscardReason);
    }
}
