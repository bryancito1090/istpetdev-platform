using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Logistics;

// #300: una ruta del asistente de pedidos (recogida en bodega + entregas) se replanifica por incidente sin perder la recogida.
[Trait("Category", "Integration")]
public class ReplanAssistantRouteTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private sealed record Point(Guid Id, string Code, decimal Lat, decimal Lon);
    private sealed record Seed(Guid WarehouseId, decimal WarehouseLat, decimal WarehouseLon, Guid ProductId, Guid VehicleId, string VehicleCode,
        IReadOnlyDictionary<string, Point> Points);

    private static async Task<Seed> ResetAsync(ApiFactory api, HttpClient planner, string vehicleCode)
    {
        (await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null)).EnsureSuccessStatusCode();
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var warehouse = await db.Locations.SingleAsync(l => l.Kind == LocationKinds.Warehouse);
        var product = await db.Products.SingleAsync(p => p.Sku == "DES-01");
        var vehicle = await db.Vehicles.SingleAsync(v => v.Code == vehicleCode);
        var points = await db.ServicePoints.ToDictionaryAsync(p => p.Code,
            p => new Point(p.Id, p.Code, Math.Round((decimal)p.Position!.Y, 5), Math.Round((decimal)p.Position.X, 5)));
        return new Seed(warehouse.Id, Math.Round((decimal)warehouse.Position!.Y, 5), Math.Round((decimal)warehouse.Position.X, 5),
            product.Id, vehicle.Id, vehicle.Code, points);
    }

    private static async Task<Guid> OrderAsync(ApiFactory api, Seed s, string pointCode, decimal quantity)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = new DeliveryOrder { OrderNumber = $"ORD-R-{Guid.NewGuid():N}"[..16], DestinationServicePointId = s.Points[pointCode].Id,
                                        RequiredDateUtc = DateTime.UtcNow.AddDays(1), SourceType = "Manual" };
        db.AddRange(order, new DeliveryOrderLine { DeliveryOrderId = order.Id, ProductId = s.ProductId, RequestedQuantity = quantity });
        await db.SaveChangesAsync();
        return order.Id;
    }

    // Confirma y publica una ruta del asistente: recogida en BOD-SUR y una entrega por punto, en ese orden.
    private static async Task<Guid> ConfirmAndPublishAsync(HttpClient planner, Seed s, IReadOnlyList<Guid> orderIds, DateTime? plannedStartUtc,
        params string[] pointCodes)
    {
        var stops = new List<object>
        {
            new { sequence = 1, kind = "Pickup", locationId = s.WarehouseId, code = "BOD-SUR", name = "Bodega Sur",
                  position = new { lat = s.WarehouseLat, lon = s.WarehouseLon }, deltaWeight = 0m, deltaVolume = 0m,
                  windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 20 }
        };
        for (var i = 0; i < pointCodes.Length; i++)
        {
            var p = s.Points[pointCodes[i]];
            stops.Add(new { sequence = i + 2, kind = "Delivery", servicePointId = p.Id, code = p.Code, name = p.Code,
                            position = new { lat = p.Lat, lon = p.Lon }, deltaWeight = 0m, deltaVolume = 0m,
                            windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15 });
        }
        var confirm = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = $"WIZ-{Guid.NewGuid():N}"[..12], vehicleId = s.VehicleId, totalDistanceKm = 1m, totalDurationMinutes = 1m, totalCost = 0.01m,
            plannedStartUtc, orderIds, stops
        });
        var body = await confirm.Content.ReadAsStringAsync();
        Assert.True(confirm.StatusCode == HttpStatusCode.Created, body);
        using var json = JsonDocument.Parse(body);
        var planId = json.RootElement.GetProperty("id").GetGuid();
        var publish = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/publish", new { });
        Assert.True(publish.StatusCode == HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());
        return planId;
    }

    private static async Task<Guid> IncidentAsync(HttpClient planner, object request)
    {
        var incident = await planner.PostAsJsonAsync("/api/v1/incidents", request);
        Assert.True(incident.StatusCode == HttpStatusCode.Created, await incident.Content.ReadAsStringAsync());
        return (await incident.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("incidentId").GetGuid();
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
    public async Task Road_closure_on_a_delivery_replans_an_assistant_route_keeping_the_pending_pickup()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true,
            overrides: new Dictionary<string, string?> { ["Agent:AutoOnIncident"] = "false" });
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner, "VEH-01");
        var blockedOrder = await OrderAsync(api, s, "CLI-S09", 10m);
        var keptOrder = await OrderAsync(api, s, "IND-S13", 20m);
        var planId = await ConfirmAndPublishAsync(planner, s, [blockedOrder, keptOrder], null, "CLI-S09", "IND-S13");

        // Cierre vial sobre la Clínica Sur (preset cierre-norte de demo-region).
        var incidentId = await IncidentAsync(planner, new
        {
            kind = "BloqueoVial", severity = "ALTA", description = "Cierre en la zona Clínica Sur",
            lat = -0.24973m, lon = -78.51721m, radiusKm = 1.5m, isSynthetic = true
        });

        var replan = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/replan", new { incidentId });
        var body = await replan.Content.ReadAsStringAsync();
        Assert.True(replan.StatusCode == HttpStatusCode.Created, body);
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var revisionId = root.GetProperty("proposedRoutePlanId").GetGuid();
        Assert.Contains(root.GetProperty("diff").GetProperty("droppedStops").EnumerateArray(), d => d.GetString() == "CLI-S09");

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stops = await db.RouteStops.Where(st => st.RoutePlanId == revisionId).OrderBy(st => st.SequenceNumber).ToListAsync();
            // La recogida pendiente sigue al inicio, con su bodega; la entrega bloqueada sale y la otra se conserva.
            Assert.True(stops.Count == 2, string.Join(", ", stops.Select(st => $"{st.SequenceNumber}:{st.Kind}:{st.Status}")) + " " + body);
            Assert.Equal((1, RouteStopKind.Pickup, (Guid?)s.WarehouseId, RouteStopStatus.Pending),
                (stops[0].SequenceNumber, stops[0].Kind, stops[0].LocationId, stops[0].Status));
            Assert.Equal((2, RouteStopKind.Delivery, s.Points["IND-S13"].Id), (stops[1].SequenceNumber, stops[1].Kind, stops[1].ServicePointId));
            // El InputJson de la revisión conserva tipo, bodega y cargas de las paradas.
            var revision = await db.RoutePlans.SingleAsync(p => p.Id == revisionId);
            using var input = JsonDocument.Parse(revision.InputJson);
            var inputStops = input.RootElement.GetProperty("stops").EnumerateArray().ToList();
            Assert.Equal("Pickup", inputStops[0].GetProperty("kind").GetString());
            Assert.Equal(s.WarehouseId, inputStops[0].GetProperty("locationId").GetGuid());
            var delivery = inputStops.Single(e => e.GetProperty("kind").GetString() == "Delivery");
            Assert.Equal(20m, delivery.GetProperty("loads").EnumerateArray().Single().GetProperty("quantity").GetDecimal());
        }

        // Publicar la revisión (aprobar): la entrega retirada se cancela y su pedido vuelve a Pending (FR-28).
        if (root.GetProperty("status").GetString() != "Published")
        {
            var publish = await planner.PostAsJsonAsync($"/api/v1/route-plans/{revisionId}/publish", new { });
            Assert.True(publish.StatusCode == HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());
        }
        Assert.Equal((DeliveryOrderStatus.Pending, 0m), await OrderStateAsync(api, blockedOrder));
        Assert.Equal((DeliveryOrderStatus.Planned, 20m), await OrderStateAsync(api, keptOrder));
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deliveries = await db.Deliveries.Where(d => db.RouteStops.Any(st => st.Id == d.RouteStopId && st.RoutePlanId == revisionId)).ToListAsync();
            Assert.Equal(s.Points["IND-S13"].Id, Assert.Single(deliveries).ServicePointId);
            Assert.Equal(20m, await db.RouteStockReservations.Where(r => r.RoutePlanId == revisionId && r.Status == RouteStockReservationStatus.Active).SumAsync(r => r.Quantity));
        }

        // Revertir restaura la recogida y la entrega retirada, y vuelve a sumar lo descontado.
        var revert = await planner.PostAsync($"/api/v1/route-plans/{revisionId}/revert", null);
        Assert.True(revert.StatusCode == HttpStatusCode.OK, await revert.Content.ReadAsStringAsync());
        Assert.Equal((DeliveryOrderStatus.Planned, 10m), await OrderStateAsync(api, blockedOrder));
        Assert.Equal((DeliveryOrderStatus.Planned, 20m), await OrderStateAsync(api, keptOrder));
    }

    [Fact]
    public async Task After_the_pickup_and_a_delivery_the_revision_keeps_both_done_and_replans_from_there()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true,
            overrides: new Dictionary<string, string?> { ["Agent:AutoOnIncident"] = "false" });
        using var planner = api.ClientFor(TestTokens.Planner);
        // VEH-02 (no VEH-01): la ruta previa reconstruida usaba el código fijo "VEH-01".
        var s = await ResetAsync(api, planner, "VEH-02");
        DateTime workdayStart;
        using (var scope = api.Services.CreateScope())
            workdayStart = await scope.ServiceProvider.GetRequiredService<IstpetDev.Application.Logistics.IOperationalClock>().WorkdayStartUtcAsync();
        var doneOrder = await OrderAsync(api, s, "IND-S13", 5m);
        var pendingOrder = await OrderAsync(api, s, "EDU-S10", 5m);
        var planId = await ConfirmAndPublishAsync(planner, s, [doneOrder, pendingOrder], workdayStart, "IND-S13", "EDU-S10");
        Guid pendingDeliveryId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pendingDeliveryId = await db.Deliveries
                .Where(d => db.RouteStops.Any(st => st.Id == d.RouteStopId && st.RoutePlanId == planId && st.ServicePointId == s.Points["EDU-S10"].Id))
                .Select(d => d.Id).SingleAsync();
        }

        // El avance de demo no marca la recogida (no tiene entrega); entrega en IND-S13.
        (await planner.PostAsJsonAsync($"/api/v1/demo/route-plans/{planId}/advance", new { operationId = Guid.NewGuid(), stops = 2 }))
            .EnsureSuccessStatusCode();
        var incidentId = await IncidentAsync(planner, new { kind = "ROAD_CLOSURE", lat = -1m, lon = -79m, radiusKm = .1m, isSynthetic = true });

        var replan = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/replan", new { incidentId });
        var body = await replan.Content.ReadAsStringAsync();
        Assert.True(replan.StatusCode == HttpStatusCode.Created, body);
        using var json = JsonDocument.Parse(body);
        var revisionId = json.RootElement.GetProperty("proposedRoutePlanId").GetGuid();
        Assert.Empty(json.RootElement.GetProperty("diff").GetProperty("droppedStops").EnumerateArray());
        if (json.RootElement.GetProperty("status").GetString() != "Published")
        {
            var publish = await planner.PostAsJsonAsync($"/api/v1/route-plans/{revisionId}/publish", new { });
            Assert.True(publish.StatusCode == HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());
        }

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stops = await db.RouteStops.Where(st => st.RoutePlanId == revisionId).OrderBy(st => st.SequenceNumber).ToListAsync();
            Assert.Equal(
                new[] { (RouteStopKind.Pickup, (Guid?)s.WarehouseId, RouteStopStatus.Completed),
                        (RouteStopKind.Delivery, null, RouteStopStatus.Completed),
                        (RouteStopKind.Delivery, null, RouteStopStatus.Pending) },
                stops.Select(st => (st.Kind, st.LocationId, st.Status)));
            Assert.Equal(new[] { s.Points["IND-S13"].Id, s.Points["EDU-S10"].Id }, stops.Skip(1).Select(st => st.ServicePointId));
            // La entrega pendiente es la misma (no se recrea) y ahora apunta a la parada de la revisión.
            var delivery = await db.Deliveries.SingleAsync(d => d.Id == pendingDeliveryId);
            Assert.Equal(stops[2].Id, delivery.RouteStopId);
            // La simulación prepara/despacha toda la carga antes de recorrer las paradas (#320).
            // Replanificar conserva ese despacho y la entrega existente.
            Assert.Equal(DeliveryStates.Dispatched, delivery.Status);
            // Sale de la última parada hecha (IND-S13), no de la bodega: la llegada a EDU-S10 es posterior a esa entrega.
            Assert.True(stops[2].PlannedArrivalUtc > stops[1].ActualArrivalUtc);
        }
        Assert.Equal((DeliveryOrderStatus.Planned, 5m), await OrderStateAsync(api, pendingOrder));
    }

    [Fact]
    public async Task Vehicle_breakdown_on_an_assistant_route_drops_the_deliveries_and_the_pending_pickup()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true,
            overrides: new Dictionary<string, string?> { ["Agent:AutoOnIncident"] = "false" });
        using var planner = api.ClientFor(TestTokens.Planner);
        var s = await ResetAsync(api, planner, "VEH-01");
        var order = await OrderAsync(api, s, "IND-S13", 5m);
        var planId = await ConfirmAndPublishAsync(planner, s, [order], null, "IND-S13");

        var incident = await planner.PostAsJsonAsync("/api/v1/incidents", new { kind = "VEHICLE_BREAKDOWN", vehicleCode = "VEH-01", isSynthetic = true });
        Assert.Equal(HttpStatusCode.Created, incident.StatusCode);
        using var incidentJson = JsonDocument.Parse(await incident.Content.ReadAsStringAsync());
        var affected = incidentJson.RootElement.GetProperty("affectedRoutes").EnumerateArray().Single();
        Assert.Equal(planId, affected.GetProperty("routePlanId").GetGuid());
        // La recogida se nombra por su bodega, no por el punto de relleno de la parada.
        Assert.Equal(new[] { "BOD-SUR", "IND-S13" },
            affected.GetProperty("affectedStops").EnumerateArray().Select(st => st.GetProperty("pointCode").GetString()));

        var replan = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/replan",
            new { incidentId = incidentJson.RootElement.GetProperty("incidentId").GetGuid() });
        var body = await replan.Content.ReadAsStringAsync();
        Assert.True(replan.StatusCode == HttpStatusCode.Created, body);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("Escalated", json.RootElement.GetProperty("decision").GetProperty("outcome").GetString());
        Assert.Equal(new[] { "IND-S13" }, json.RootElement.GetProperty("diff").GetProperty("droppedStops").EnumerateArray().Select(d => d.GetString()));
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var revisionId = json.RootElement.GetProperty("proposedRoutePlanId").GetGuid();
        // Sin entregas que hacer, la revisión no conserva una recogida que ya no sirve.
        Assert.False(await db.RouteStops.AnyAsync(st => st.RoutePlanId == revisionId));
    }
}
