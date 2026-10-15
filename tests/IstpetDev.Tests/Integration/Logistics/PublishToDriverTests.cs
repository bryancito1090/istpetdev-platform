using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

// Las rutas deben llegar al conductor: asignación al publicar, entregas desde los pedidos del asistente,
// bodega de origen correcta y nada de planes "publicados" sin entregas.
[Trait("Category", "Integration")]
public class PublishToDriverTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private sealed record Seed(Guid PointId, string PointCode, Guid ProductId, Guid VehicleId, Guid WarehouseId, Guid DriverId, Guid PlannerId);

    private static async Task<Seed> ResetAsync(ApiFactory api, HttpClient planner)
    {
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var point = await db.ServicePoints.OrderBy(p => p.Code).FirstAsync();
        var product = await db.Products.OrderBy(p => p.Sku).FirstAsync();
        var vehicle = await db.Vehicles.OrderBy(v => v.Code).FirstAsync();
        var warehouse = await db.Locations.FirstAsync(l => l.Kind == LocationKinds.Warehouse);
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var plannerAccount = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Planner);
        return new Seed(point.Id, point.Code, product.Id, vehicle.Id, warehouse.Id, driver.Id, plannerAccount.Id);
    }

    private static async Task<Guid> OrderAsync(ApiFactory api, Seed s, decimal quantity)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = new DeliveryOrder { OrderNumber = $"ORD-TEST-{Guid.NewGuid():N}"[..20], DestinationServicePointId = s.PointId,
                                        RequiredDateUtc = DateTime.UtcNow.AddDays(1), SourceType = "Manual" };
        db.AddRange(order, new DeliveryOrderLine { DeliveryOrderId = order.Id, ProductId = s.ProductId, RequestedQuantity = quantity });
        await db.SaveChangesAsync();
        return order.Id;
    }

    // Igual que el asistente: recogida en bodega y entrega en el punto, sin cargas en las paradas.
    private static async Task<Guid> ConfirmAsync(HttpClient planner, Seed s, Guid pickupLocationId, IReadOnlyList<Guid> orderIds)
    {
        var stop = (int seq, string kind, Guid? point, Guid? location, string code) => new
        {
            sequence = seq, kind, servicePointId = point, locationId = location, code, name = code,
            position = new { lat = -0.2m, lon = -78.5m }, deltaWeight = 0m, deltaVolume = 0m,
            windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15
        };
        var response = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = $"WIZ-{Guid.NewGuid():N}"[..12], vehicleId = s.VehicleId, totalDistanceKm = 10m, totalDurationMinutes = 30m, totalCost = 5m,
            orderIds, stops = new object[] { stop(1, "Pickup", null, pickupLocationId, "BOD"), stop(2, "Delivery", s.PointId, null, s.PointCode) }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> VehicleCodeAsync(ApiFactory api, Guid vehicleId)
    {
        using var scope = api.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Vehicles.Where(v => v.Id == vehicleId).Select(v => v.Code).SingleAsync();
    }

    private static Task<HttpResponseMessage> PublishAsync(HttpClient planner, Guid planId, string? vehicleCode = null, Guid? driverId = null) =>
        planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/publish",
            vehicleCode is null ? new { } : (object)new { driverAssignments = new Dictionary<string, string> { [vehicleCode] = driverId!.Value.ToString() } });

    [Fact]
    public async Task Wizard_route_with_orders_reaches_the_assigned_driver()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var orderId = await OrderAsync(api, s, 5m);
        var planId = await ConfirmAsync(planner, s, s.WarehouseId, [orderId]);

        var publish = await PublishAsync(planner, planId, await VehicleCodeAsync(api, s.VehicleId), s.DriverId);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var delivery = await db.Deliveries.SingleAsync(d => db.RouteStops.Any(st => st.Id == d.RouteStopId && st.RoutePlanId == planId));
            Assert.Equal(s.DriverId, delivery.AssignedAccountId);
            Assert.Equal(s.WarehouseId, delivery.OriginLocationId);
            Assert.Equal(5m, await db.DeliveryLines.Where(l => l.DeliveryId == delivery.Id).SumAsync(l => l.PlannedQuantity));
        }

        using var driver = api.ClientFor(TestTokens.Driver);
        using var assignments = JsonDocument.Parse(await (await driver.GetAsync("/api/v1/mobile/assignments")).Content.ReadAsStringAsync());
        var route = assignments.RootElement.GetProperty("routes").EnumerateArray().Single(r => r.GetProperty("routePlanId").GetGuid() == planId);
        Assert.Equal(1, route.GetProperty("deliveries").GetArrayLength());
    }

    [Fact]
    public async Task A_route_with_nothing_to_deliver_is_not_published()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var planId = await ConfirmAsync(planner, s, s.WarehouseId, []);

        var publish = await PublishAsync(planner, planId);
        Assert.Equal(HttpStatusCode.Conflict, publish.StatusCode);
        Assert.Contains("no tiene productos para entregar", await publish.Content.ReadAsStringAsync());

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal("Proposed", await db.RoutePlans.Where(p => p.Id == planId).Select(p => p.Status).SingleAsync());
    }

    [Fact]
    public async Task Only_an_active_driver_can_be_assigned()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var planId = await ConfirmAsync(planner, s, s.WarehouseId, [await OrderAsync(api, s, 5m)]);

        var publish = await PublishAsync(planner, planId, await VehicleCodeAsync(api, s.VehicleId), s.PlannerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, publish.StatusCode);
    }

    [Fact]
    public async Task Drivers_list_is_for_planning_roles()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);

        using var json = JsonDocument.Parse(await (await planner.GetAsync("/api/v1/drivers")).Content.ReadAsStringAsync());
        Assert.Contains(json.RootElement.EnumerateArray(), d => d.GetProperty("accountId").GetGuid() == s.DriverId);
        using var driver = api.ClientFor(TestTokens.Driver);
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/drivers")).StatusCode);
    }

    [Fact]
    public async Task A_route_published_empty_before_the_fix_is_repaired_by_publishing_again()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var planId = await ConfirmAsync(planner, s, s.WarehouseId, [await OrderAsync(api, s, 4m)]);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var plan = await db.RoutePlans.SingleAsync(p => p.Id == planId);
            plan.Status = "Published"; // estado que dejaba la versión anterior: publicado y sin entregas
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.OK, (await PublishAsync(planner, planId, await VehicleCodeAsync(api, s.VehicleId), s.DriverId)).StatusCode);

        using var check = api.Services.CreateScope();
        var database = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await database.Deliveries.AnyAsync(d => d.AssignedAccountId == s.DriverId &&
            database.RouteStops.Any(st => st.Id == d.RouteStopId && st.RoutePlanId == planId)));
    }

    [Fact]
    public async Task Deliveries_leave_from_the_warehouse_where_the_route_picks_up()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        Guid secondWarehouse;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var lot = await db.Lots.FirstAsync(l => l.ProductId == s.ProductId);
            var warehouse = new IstpetDev.Domain.Entities.Location { Code = "BOD-SUR-TEST", Name = "Bodega Sur", Kind = LocationKinds.Warehouse, IsActive = true,
                                           Position = new GeometryFactory(new PrecisionModel(), 4326).CreatePoint(new Coordinate(-78.55, -0.30)) };
            db.AddRange(warehouse, new StockBalance { LocationId = warehouse.Id, LotId = lot.Id, QuantityOnHand = 100m });
            await db.SaveChangesAsync();
            secondWarehouse = warehouse.Id;
        }
        var planId = await ConfirmAsync(planner, s, secondWarehouse, [await OrderAsync(api, s, 3m)]);

        Assert.Equal(HttpStatusCode.OK, (await PublishAsync(planner, planId, await VehicleCodeAsync(api, s.VehicleId), s.DriverId)).StatusCode);

        using var check = api.Services.CreateScope();
        var database = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var delivery = await database.Deliveries.SingleAsync(d => database.RouteStops.Any(st => st.Id == d.RouteStopId && st.RoutePlanId == planId));
        Assert.Equal(secondWarehouse, delivery.OriginLocationId);
        var reserved = await database.StockBalances.Where(b => b.LocationId == secondWarehouse).SumAsync(b => b.QuantityReserved);
        Assert.Equal(3m, reserved);
    }
}
