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

    private static async Task<(int Draft, int Cancelled, decimal ActiveReserved, decimal BalanceReserved)> PlanStockAsync(ApiFactory api, Guid planId)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deliveries = await db.Deliveries.Where(d => db.RouteStops.Any(st => st.Id == d.RouteStopId && st.RoutePlanId == planId)).ToListAsync();
        var reservations = await db.RouteStockReservations.Where(r => r.RoutePlanId == planId).ToListAsync();
        var balanceIds = reservations.Select(r => r.StockBalanceId).Distinct().ToList();
        return (deliveries.Count(d => d.Status == DeliveryStates.Draft), deliveries.Count(d => d.Status == DeliveryStates.Cancelled),
                reservations.Where(r => r.Status == RouteStockReservationStatus.Active).Sum(r => r.Quantity),
                await db.StockBalances.Where(b => balanceIds.Contains(b.Id)).SumAsync(b => b.QuantityReserved));
    }

    private static Task<HttpResponseMessage> PublishAsync(HttpClient planner, Seed s, Guid planId) =>
        planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/publish",
            new { driverAssignments = new Dictionary<string, string> { [s.VehicleCode] = s.DriverId.ToString() } });

    private static async Task<Guid> PlanIdAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    // #199: el panel reenvía las paradas de suggest (peso y cargas del pedido completo); con lines manda lo planificado.
    [Fact]
    public async Task Confirm_with_panel_totals_and_edited_lines_plans_publishes_and_reserves_only_the_edited_quantity()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var orderId = await OrderAsync(api, s, 150m);

        var fullLoad = new[] { new { productId = s.ProductId, sku = "X", quantity = 150m, baseUnit = "u" } };
        var response = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = $"WIZ-{Guid.NewGuid():N}"[..12], vehicleId = s.VehicleId, totalDistanceKm = 1m, totalDurationMinutes = 1m, totalCost = 0.01m,
            orderIds = new[] { orderId },
            stops = new object[]
            {
                new { sequence = 1, kind = "Pickup", locationId = s.WarehouseId, code = "BOD", name = "BOD", position = new { lat = -0.2m, lon = -78.5m },
                      deltaWeight = 150m, deltaVolume = 0.15m, windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15, loads = fullLoad },
                new { sequence = 2, kind = "Delivery", servicePointId = s.PointId, code = s.PointCode, name = s.PointCode, position = new { lat = -0.2m, lon = -78.5m },
                      deltaWeight = -150m, deltaVolume = -0.15m, windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15, loads = fullLoad }
            },
            lines = new[] { new { deliveryOrderId = orderId, productId = s.ProductId, plannedQuantity = 100m } }
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(100m, json.RootElement.GetProperty("loadWeight").GetDecimal());
        var planId = json.RootElement.GetProperty("id").GetGuid();
        Assert.Equal((DeliveryOrderStatus.Pending, 100m), await OrderStateAsync(api, orderId));

        Assert.Equal(HttpStatusCode.OK, (await PublishAsync(planner, s, planId)).StatusCode);
        Assert.Equal(100m, (await PlanStockAsync(api, planId)).ActiveReserved);
    }

    // #199: cancelar una ruta confirmada libera reservas, cancela entregas y descuenta PlannedQuantity del pedido.
    [Fact]
    public async Task Cancel_releases_reservations_and_returns_the_planned_quantity_to_the_order()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var orderId = await OrderAsync(api, s, 150m);

        // Dos rutas: 100 publicadas y 50 propuestas; el pedido queda Planned con 150.
        var firstId = await PlanIdAsync(await ConfirmAsync(planner, s, [orderId], s.VehicleId, plannedQuantity: 100m));
        Assert.Equal(HttpStatusCode.OK, (await PublishAsync(planner, s, firstId)).StatusCode);
        var secondId = await PlanIdAsync(await ConfirmAsync(planner, s, [orderId], s.VehicleId));
        Assert.Equal((DeliveryOrderStatus.Planned, 150m), await OrderStateAsync(api, orderId));
        var before = await PlanStockAsync(api, firstId);
        Assert.Equal(100m, before.ActiveReserved);

        // Cancelar la publicada: vuelve a Pending con 50 planificadas (las de la segunda ruta).
        var cancel = await planner.PostAsync($"/api/v1/route-plans/{firstId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        using (var json = JsonDocument.Parse(await cancel.Content.ReadAsStringAsync()))
        {
            var root = json.RootElement;
            Assert.Equal("Cancelled", root.GetProperty("status").GetString());
            Assert.Equal(100m, root.GetProperty("releasedReservedQuantity").GetDecimal());
            var line = root.GetProperty("orderLines").EnumerateArray().Single();
            Assert.Equal(100m, line.GetProperty("releasedQuantity").GetDecimal());
            Assert.Equal(50m, line.GetProperty("plannedQuantity").GetDecimal());
            Assert.Equal(100m, line.GetProperty("pendingQuantity").GetDecimal());
            Assert.Equal(DeliveryOrderStatus.Pending, line.GetProperty("orderStatus").GetString());
        }
        Assert.Equal((DeliveryOrderStatus.Pending, 50m), await OrderStateAsync(api, orderId));
        var after = await PlanStockAsync(api, firstId);
        Assert.Equal((0, 1, 0m), (after.Draft, after.Cancelled, after.ActiveReserved));
        Assert.Equal(before.BalanceReserved - 100m, after.BalanceReserved);

        // Idempotente: cancelar otra vez no descuenta de nuevo.
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync($"/api/v1/route-plans/{firstId}/cancel", null)).StatusCode);
        Assert.Equal((DeliveryOrderStatus.Pending, 50m), await OrderStateAsync(api, orderId));

        // Cancelar la propuesta sin publicar deja el pedido sin nada planificado; se puede volver a planificar.
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync($"/api/v1/route-plans/{secondId}/cancel", null)).StatusCode);
        Assert.Equal((DeliveryOrderStatus.Pending, 0m), await OrderStateAsync(api, orderId));
        Assert.Equal(HttpStatusCode.Created, (await ConfirmAsync(planner, s, [orderId], s.VehicleId, plannedQuantity: 100m)).StatusCode);
    }

    [Fact]
    public async Task Cancel_is_rejected_when_a_delivery_was_already_dispatched()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var orderId = await OrderAsync(api, s, 50m);
        var planId = await PlanIdAsync(await ConfirmAsync(planner, s, [orderId], s.VehicleId));
        Assert.Equal(HttpStatusCode.OK, (await PublishAsync(planner, s, planId)).StatusCode);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var delivery = await db.Deliveries.SingleAsync(d => db.RouteStops.Any(st => st.Id == d.RouteStopId && st.RoutePlanId == planId));
            delivery.Status = DeliveryStates.Dispatched;
            delivery.DispatchedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Conflict, (await planner.PostAsync($"/api/v1/route-plans/{planId}/cancel", null)).StatusCode);
        Assert.Equal((DeliveryOrderStatus.Planned, 50m), await OrderStateAsync(api, orderId));
        Assert.Equal(HttpStatusCode.NotFound, (await planner.PostAsync($"/api/v1/route-plans/{Guid.NewGuid()}/cancel", null)).StatusCode);
    }

    // #199 (FR-28): una revisión por incidente que retira una parada con pedidos descuenta lo planificado de ese pedido.
    [Fact]
    public async Task A_revision_that_removes_an_order_stop_returns_its_planned_quantity_and_revert_adds_it_back()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        Guid otherPointId;
        string otherPointCode;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var other = await db.ServicePoints.Where(p => p.Id != s.PointId).OrderBy(p => p.Code).FirstAsync();
            (otherPointId, otherPointCode) = (other.Id, other.Code);
        }
        var removedOrder = await OrderAsync(api, s, 30m);
        var keptOrder = await OrderAsync(api, s with { PointId = otherPointId, PointCode = otherPointCode }, 40m);

        // Ruta confirmada con dos entregas (30 y 40) y publicada.
        var stop = (int seq, string kind, Guid? point, Guid? location, string code) => new
        {
            sequence = seq, kind, servicePointId = point, locationId = location, code, name = code,
            position = new { lat = -0.2m, lon = -78.5m }, deltaWeight = 0m, deltaVolume = 0m,
            windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15
        };
        var planId = await PlanIdAsync(await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = $"WIZ-{Guid.NewGuid():N}"[..12], vehicleId = s.VehicleId, totalDistanceKm = 1m, totalDurationMinutes = 1m, totalCost = 0.01m,
            orderIds = new[] { removedOrder, keptOrder },
            stops = new object[] { stop(1, "Pickup", null, s.WarehouseId, "BOD"), stop(2, "Delivery", s.PointId, null, s.PointCode), stop(3, "Delivery", otherPointId, null, otherPointCode) }
        }));
        Assert.Equal(HttpStatusCode.OK, (await PublishAsync(planner, s, planId)).StatusCode);
        Assert.Equal((DeliveryOrderStatus.Planned, 30m), await OrderStateAsync(api, removedOrder));
        Assert.Equal((DeliveryOrderStatus.Planned, 40m), await OrderStateAsync(api, keptOrder));

        // Revisión como la guarda Replanner: solo queda la entrega del otro punto, con la misma carga.
        Guid revisionId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var plan = await db.RoutePlans.SingleAsync(p => p.Id == planId);
            var revision = new RoutePlan
            {
                Code = plan.Code, Revision = plan.Revision + 1, SupersedesRoutePlanId = plan.Id, VehicleId = plan.VehicleId,
                Status = "Proposed", ProposedBy = "System:IncidentReplanner", AlgorithmVersion = "2.0-deterministic",
                PlannedStartUtc = plan.PlannedStartUtc,
                InputJson = JsonSerializer.Serialize(new
                {
                    stops = new[] { new { sequence = 1, pointCode = otherPointCode, isConsolidation = false,
                                          loads = new[] { new { productId = s.ProductId, sku = "X", quantity = 40m, baseUnit = "u" } } } }
                })
            };
            db.RoutePlans.Add(revision);
            db.RouteStops.Add(new RouteStop { RoutePlanId = revision.Id, ServicePointId = otherPointId, SequenceNumber = 1,
                                              PlannedArrivalUtc = plan.PlannedStartUtc, Status = RouteStopStatus.Pending });
            await db.SaveChangesAsync();
            revisionId = revision.Id;
        }

        var publish = await planner.PostAsJsonAsync($"/api/v1/route-plans/{revisionId}/publish", new { });
        Assert.True(publish.StatusCode == HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());
        // La entrega retirada se cancela, su reserva se libera y su pedido vuelve a Pending sin nada planificado.
        Assert.Equal((DeliveryOrderStatus.Pending, 0m), await OrderStateAsync(api, removedOrder));
        Assert.Equal((DeliveryOrderStatus.Planned, 40m), await OrderStateAsync(api, keptOrder));
        Assert.Equal(40m, (await PlanStockAsync(api, revisionId)).ActiveReserved);

        // Revertir restaura la parada y vuelve a sumar lo descontado.
        var revert = await planner.PostAsync($"/api/v1/route-plans/{revisionId}/revert", null);
        Assert.True(revert.StatusCode == HttpStatusCode.OK, await revert.Content.ReadAsStringAsync());
        Guid restoredId;
        using (var json = JsonDocument.Parse(await revert.Content.ReadAsStringAsync()))
            restoredId = json.RootElement.GetProperty("id").GetGuid();
        Assert.Equal((DeliveryOrderStatus.Planned, 30m), await OrderStateAsync(api, removedOrder));
        Assert.Equal((DeliveryOrderStatus.Planned, 40m), await OrderStateAsync(api, keptOrder));
        var restored = await PlanStockAsync(api, restoredId);
        Assert.Equal((2, 70m), (restored.Draft, restored.ActiveReserved));

        // Cancelar la ruta restaurada descuenta todo una sola vez.
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync($"/api/v1/route-plans/{restoredId}/cancel", null)).StatusCode);
        Assert.Equal((DeliveryOrderStatus.Pending, 0m), await OrderStateAsync(api, removedOrder));
        Assert.Equal((DeliveryOrderStatus.Pending, 0m), await OrderStateAsync(api, keptOrder));
    }

    // #199 (FR-28): un plan confirmado antes de #264 no guarda orderLines; cancelarlo no inventa un descuento.
    [Fact]
    public async Task Cancel_of_a_plan_without_order_lines_releases_stock_but_does_not_change_planned_quantity()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner);
        var orderId = await OrderAsync(api, s, 50m);
        var planId = await PlanIdAsync(await ConfirmAsync(planner, s, [orderId], s.VehicleId));
        Assert.Equal(HttpStatusCode.OK, (await PublishAsync(planner, s, planId)).StatusCode);
        using (var scope = api.Services.CreateScope())
        {
            // Como un plan anterior a #264: sin orderLines en InputJson.
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var plan = await db.RoutePlans.SingleAsync(p => p.Id == planId);
            var input = System.Text.Json.Nodes.JsonNode.Parse(plan.InputJson)!.AsObject();
            input.Remove("orderLines");
            plan.InputJson = input.ToJsonString();
            await db.SaveChangesAsync();
        }

        var cancel = await planner.PostAsync($"/api/v1/route-plans/{planId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        using var json = JsonDocument.Parse(await cancel.Content.ReadAsStringAsync());
        Assert.Equal(50m, json.RootElement.GetProperty("releasedReservedQuantity").GetDecimal());
        Assert.Empty(json.RootElement.GetProperty("orderLines").EnumerateArray());
        Assert.Equal((DeliveryOrderStatus.Planned, 50m), await OrderStateAsync(api, orderId));
    }
}
