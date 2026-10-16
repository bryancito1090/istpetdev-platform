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
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

// #199: el asistente por pedidos no confía en el panel; confirmar revalida vehículo, capacidad y cantidades.
[Trait("Category", "Integration")]
public class ConfirmCapacityTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private sealed record Seed(Guid PointId, string PointCode, Guid ProductId, Guid VehicleId, string VehicleCode, Guid WarehouseId, Guid DriverId);

    // Producto de 1 kg, vehículo de 100 kg y stock suficiente en bodega.
    private static async Task<Seed> ResetAsync(ApiFactory api, HttpClient planner, decimal unitWeight = 1m)
    {
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var point = await db.ServicePoints.OrderBy(p => p.Code).FirstAsync();
        var product = await db.Products.OrderBy(p => p.Sku).FirstAsync();
        var vehicle = await db.Vehicles.OrderBy(v => v.Code).FirstAsync();
        var warehouse = await db.Locations.FirstAsync(l => l.Kind == LocationKinds.Warehouse);
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        product.WeightPerBaseUnit = unitWeight;
        product.VolumePerBaseUnit = 0.001m;
        vehicle.MaxWeight = 100m;
        vehicle.MaxVolume = 10m;
        var lot = await db.Lots.Where(l => l.ProductId == product.Id && !l.IsBlocked).OrderByDescending(l => l.ExpiresOn).FirstAsync();
        var balance = await db.StockBalances.FirstOrDefaultAsync(b => b.LocationId == warehouse.Id && b.LotId == lot.Id);
        if (balance is null) db.StockBalances.Add(new StockBalance { LocationId = warehouse.Id, LotId = lot.Id, QuantityOnHand = 1000m });
        else balance.QuantityOnHand += 1000m;
        await db.SaveChangesAsync();
        return new Seed(point.Id, point.Code, product.Id, vehicle.Id, vehicle.Code, warehouse.Id, driver.Id);
    }

    private static async Task<Guid> OrderAsync(ApiFactory api, Seed s, decimal quantity)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = new DeliveryOrder { OrderNumber = $"ORD-T-{Guid.NewGuid():N}"[..16], DestinationServicePointId = s.PointId,
                                        RequiredDateUtc = DateTime.UtcNow.AddDays(1), SourceType = "Manual" };
        db.AddRange(order, new DeliveryOrderLine { DeliveryOrderId = order.Id, ProductId = s.ProductId, RequestedQuantity = quantity });
        await db.SaveChangesAsync();
        return order.Id;
    }

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient planner, Seed s, IReadOnlyList<Guid> orderIds, Guid? vehicleId,
        bool includeDelivery = true, decimal? plannedQuantity = null)
    {
        var stop = (int seq, string kind, Guid? point, Guid? location, string code) => new
        {
            sequence = seq, kind, servicePointId = point, locationId = location, code, name = code,
            position = new { lat = -0.2m, lon = -78.5m }, deltaWeight = 0m, deltaVolume = 0m,
            windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15
        };
        var stops = new List<object> { stop(1, "Pickup", null, s.WarehouseId, "BOD") };
        if (includeDelivery) stops.Add(stop(2, "Delivery", s.PointId, null, s.PointCode));
        // Totales manipulados: la API no los usa para la carga.
        return planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = $"WIZ-{Guid.NewGuid():N}"[..12], vehicleId, totalDistanceKm = 1m, totalDurationMinutes = 1m, totalCost = 0.01m,
            orderIds, stops,
            lines = plannedQuantity is { } qty
                ? orderIds.Select(id => new { deliveryOrderId = id, productId = s.ProductId, plannedQuantity = qty }).ToArray()
                : null
        });
    }

    private static async Task<(string Status, decimal Planned)> OrderStateAsync(ApiFactory api, Guid orderId)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var status = await db.DeliveryOrders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync();
        var planned = await db.DeliveryOrderLines.Where(l => l.DeliveryOrderId == orderId).SumAsync(l => l.PlannedQuantity);
        return (status, planned);
    }

    [Fact]
    public async Task Confirm_rejects_150kg_in_a_100kg_vehicle_and_plans_an_explicit_partial_keeping_the_rest_pending()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var orderId = await OrderAsync(api, s, 150m);

        // Todo el pedido no cabe: se rechaza (#201) y el pedido no cambia.
        Assert.Equal(HttpStatusCode.Conflict, (await ConfirmAsync(planner, s, [orderId], s.VehicleId)).StatusCode);
        Assert.Equal((DeliveryOrderStatus.Pending, 0m), await OrderStateAsync(api, orderId));
        // No se puede pedir más de lo pendiente.
        Assert.Equal(HttpStatusCode.Conflict, (await ConfirmAsync(planner, s, [orderId], s.VehicleId, plannedQuantity: 200m)).StatusCode);

        // Atención parcial explícita: 100 de 150.
        var response = await ConfirmAsync(planner, s, [orderId], s.VehicleId, plannedQuantity: 100m);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.True(root.GetProperty("partial").GetBoolean());
        Assert.True(root.GetProperty("loadWeight").GetDecimal() <= 100m);
        var line = root.GetProperty("lines").EnumerateArray().Single();
        Assert.Equal(150m, line.GetProperty("requiredQuantity").GetDecimal());
        Assert.Equal(100m, line.GetProperty("plannedQuantity").GetDecimal());
        Assert.Equal(50m, line.GetProperty("pendingQuantity").GetDecimal());
        Assert.Equal((DeliveryOrderStatus.Pending, 100m), await OrderStateAsync(api, orderId));

        // La publicación crea y reserva exactamente lo planificado.
        var planId = root.GetProperty("id").GetGuid();
        var publish = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/publish",
            new { driverAssignments = new Dictionary<string, string> { [s.VehicleCode] = s.DriverId.ToString() } });
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deliveryIds = await db.Deliveries.Where(d => db.RouteStops.Any(st => st.Id == d.RouteStopId && st.RoutePlanId == planId)).Select(d => d.Id).ToListAsync();
            Assert.Equal(100m, await db.DeliveryLines.Where(l => deliveryIds.Contains(l.DeliveryId)).SumAsync(l => l.PlannedQuantity));
            Assert.Equal(100m, await db.RouteStockReservations.Where(r => r.RoutePlanId == planId).SumAsync(r => r.Quantity));
        }

        // Segunda planificación: solo se ofrece y planifica el remanente; entonces el pedido queda Planned.
        var second = await ConfirmAsync(planner, s, [orderId], s.VehicleId);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var secondLine = secondJson.RootElement.GetProperty("lines").EnumerateArray().Single();
        Assert.Equal(50m, secondLine.GetProperty("plannedQuantity").GetDecimal());
        Assert.Equal(0m, secondLine.GetProperty("pendingQuantity").GetDecimal());
        Assert.False(secondJson.RootElement.GetProperty("partial").GetBoolean());
        Assert.Equal((DeliveryOrderStatus.Planned, 150m), await OrderStateAsync(api, orderId));

        // Ya no queda nada que planificar.
        Assert.Equal(HttpStatusCode.Conflict, (await ConfirmAsync(planner, s, [orderId], s.VehicleId)).StatusCode);
    }

    [Fact]
    public async Task Confirm_without_vehicle_or_with_an_unknown_one_is_rejected_and_creates_nothing()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var orderId = await OrderAsync(api, s, 10m);

        Assert.Equal(HttpStatusCode.Conflict, (await ConfirmAsync(planner, s, [orderId], null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await ConfirmAsync(planner, s, [orderId], Guid.NewGuid())).StatusCode);

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.RoutePlans.AnyAsync(p => p.AlgorithmVersion == "order-based-routing-1.0"));
        Assert.Equal((DeliveryOrderStatus.Pending, 0m), await OrderStateAsync(api, orderId));
    }

    [Fact]
    public async Task Confirm_rejects_an_order_without_delivery_stop_or_when_not_even_one_unit_fits()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        // Una unidad pesa 200 kg: no cabe ninguna en el vehículo de 100 kg.
        var s = await ResetAsync(api, planner, unitWeight: 200m);
        var orderId = await OrderAsync(api, s, 5m);

        var noStop = await ConfirmAsync(planner, s, [orderId], s.VehicleId, includeDelivery: false);
        Assert.Equal(HttpStatusCode.Conflict, noStop.StatusCode);
        Assert.Contains("no tienen cantidades pendientes", await noStop.Content.ReadAsStringAsync());

        var tooHeavy = await ConfirmAsync(planner, s, [orderId], s.VehicleId, plannedQuantity: 1m);
        Assert.Equal(HttpStatusCode.Conflict, tooHeavy.StatusCode);
        Assert.Equal((DeliveryOrderStatus.Pending, 0m), await OrderStateAsync(api, orderId));
    }
}
