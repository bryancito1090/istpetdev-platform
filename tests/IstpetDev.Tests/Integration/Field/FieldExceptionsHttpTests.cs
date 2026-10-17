using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

// #116: novedades de recepción y pedidos de campo para la web del Planificador.
[Trait("Category", "Integration")]
public class FieldExceptionsHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static async Task ReceiveAsync(ApiFactory api, FieldSeed.Dispatched d, decimal accepted, decimal rejected,
        string? reason = null, decimal? remaining = null, decimal? next = null)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var receiver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Receiver);
        var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
        var op = BaseEntity.NewId();
        await field.CreateReceiverReceiptAsync(d.UnitId!.Value, d.UnitToken!, new CreateReceiptRequest(op, 0, DateTime.UtcNow,
            [new ReceiptLineRequest(d.DeliveryLineId, d.LotId, accepted, rejected, "L", reason, remaining, next)]), receiver.Id, op.ToString());
    }

    private static async Task<FieldSeed.Dispatched> SeedDeliveredAsync(ApiFactory api, string suffix)
    {
        var token = $"token-{suffix}-{Guid.NewGuid():N}";
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m, suffix, withHandlingUnit: true, unitToken: token);
        await FieldSeed.DeliverUnitAsync(api, d);
        return d;
    }

    private static async Task<JsonElement> GetJsonAsync(System.Net.Http.HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    [Fact]
    public async Task Lists_only_receipts_with_rejections_or_shelf_adjustments_and_filters_by_point()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var rejected = await SeedDeliveredAsync(api, "EXC-R");
        var adjusted = await SeedDeliveredAsync(api, "EXC-A");
        var clean = await SeedDeliveredAsync(api, "EXC-C");
        await ReceiveAsync(api, rejected, 8m, 4m, reason: "Daño");
        await ReceiveAsync(api, adjusted, 12m, 0m, remaining: 3m); // saldo previo 0 → ajuste +3
        await ReceiveAsync(api, clean, 12m, 0m);

        using var planner = api.ClientFor(TestTokens.Planner);
        var body = await GetJsonAsync(planner, "/api/v1/receipts/exceptions");

        Assert.Equal(2, body.GetProperty("total").GetInt32());
        var items = body.GetProperty("items").EnumerateArray().ToList();
        var withRejection = items.Single(i => i.GetProperty("deliveryId").GetGuid() == rejected.DeliveryId);
        Assert.True(withRejection.GetProperty("hasRejections").GetBoolean());
        var rejectedLine = withRejection.GetProperty("lines")[0];
        Assert.Equal(4m, rejectedLine.GetProperty("quantityRejected").GetDecimal());
        Assert.Equal("Daño", rejectedLine.GetProperty("rejectionReason").GetString());

        var withAdjustment = items.Single(i => i.GetProperty("deliveryId").GetGuid() == adjusted.DeliveryId);
        Assert.True(withAdjustment.GetProperty("hasAdjustments").GetBoolean());
        Assert.Equal(3m, withAdjustment.GetProperty("lines")[0].GetProperty("adjustmentQuantity").GetDecimal());
        Assert.DoesNotContain(items, i => i.GetProperty("deliveryId").GetGuid() == clean.DeliveryId);

        Guid pointId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pointId = await db.Deliveries.Where(d => d.Id == rejected.DeliveryId).Select(d => d.ServicePointId).SingleAsync();
        }
        var filtered = await GetJsonAsync(planner, $"/api/v1/receipts/exceptions?servicePointId={pointId}");
        Assert.Equal(1, filtered.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Mislabeled_rejection_is_stored_as_ErrorRotulado_and_listed_in_exceptions()
    {
        // #236: el sinónimo del receptor se guarda con el código canónico, sin migración (columna de texto).
        await using var api = new ApiFactory(postgres.NewDatabase());
        var mislabeled = await SeedDeliveredAsync(api, "EXC-ROT");
        await ReceiveAsync(api, mislabeled, 9m, 3m, reason: "mislabeled");

        using var planner = api.ClientFor(TestTokens.Planner);
        var body = await GetJsonAsync(planner, "/api/v1/receipts/exceptions");

        var item = body.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("deliveryId").GetGuid() == mislabeled.DeliveryId);
        Assert.True(item.GetProperty("hasRejections").GetBoolean());
        var line = item.GetProperty("lines")[0];
        Assert.Equal(3m, line.GetProperty("quantityRejected").GetDecimal());
        Assert.Equal(ReceiptRejectionReasons.ErrorRotulado, line.GetProperty("rejectionReason").GetString());
    }

    // Ubica la bodega de origen y el punto de la entrega sembrada; opcionalmente guarda el tramo por calles en ambos sentidos.
    private static async Task PlaceAsync(ApiFactory api, FieldSeed.Dispatched d, GeoPoint warehouse, GeoPoint point,
        (decimal Km, decimal Minutes)? roadOut = null, (decimal Km, decimal Minutes)? roadBack = null)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var delivery = await db.Deliveries.SingleAsync(x => x.Id == d.DeliveryId);
        (await db.Locations.SingleAsync(l => l.Id == delivery.OriginLocationId)).Position =
            new Point((double)warehouse.Lon, (double)warehouse.Lat) { SRID = 4326 };
        (await db.ServicePoints.SingleAsync(p => p.Id == delivery.ServicePointId)).Position =
            new Point((double)point.Lon, (double)point.Lat) { SRID = 4326 };
        void Edge(GeoPoint from, GeoPoint to, (decimal Km, decimal Minutes) leg) => db.RoadMatrixEdges.Add(new RoadMatrixEdge
        {
            OrganizationId = DemoOrganization.Id, FromLat = from.Lat, FromLon = from.Lon, ToLat = to.Lat, ToLon = to.Lon,
            DistanceKm = leg.Km, DurationMinutes = leg.Minutes, Source = "imported", RetrievedAtUtc = DateTime.UtcNow
        });
        if (roadOut is { } o) Edge(warehouse, point, o);
        if (roadBack is { } b) Edge(point, warehouse, b);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Exceptions_include_estimated_backtrack_only_for_redelivery_reasons_with_coordinates()
    {
        // #237: ida y vuelta bodega → punto; «aproximado» si algún tramo sale de la línea recta.
        await using var api = new ApiFactory(postgres.NewDatabase());
        var road = await SeedDeliveredAsync(api, "BT-ROAD");
        var approx = await SeedDeliveredAsync(api, "BT-APPROX");
        var surplus = await SeedDeliveredAsync(api, "BT-SOBRANTE");
        var noCoords = await SeedDeliveredAsync(api, "BT-SIN-GPS");

        await PlaceAsync(api, road, new GeoPoint(-0.18000m, -78.48000m), new GeoPoint(-0.10000m, -78.45000m), (9.2m, 18m), (9.4m, 19m));
        await PlaceAsync(api, approx, new GeoPoint(-0.25000m, -78.52000m), new GeoPoint(-0.30000m, -78.55000m));
        await PlaceAsync(api, surplus, new GeoPoint(-0.26000m, -78.53000m), new GeoPoint(-0.31000m, -78.56000m));
        await ReceiveAsync(api, road, 8m, 4m, reason: "Daño");
        await ReceiveAsync(api, approx, 10m, 2m, reason: "mislabeled");
        await ReceiveAsync(api, surplus, 10m, 2m, reason: "Sobrante");
        await ReceiveAsync(api, noCoords, 9m, 3m, reason: "Faltante");

        LogisticsOptions options;
        using (var scope = api.Services.CreateScope())
            options = scope.ServiceProvider.GetRequiredService<IOptions<LogisticsOptions>>().Value;

        using var planner = api.ClientFor(TestTokens.Planner);
        var items = (await GetJsonAsync(planner, "/api/v1/receipts/exceptions")).GetProperty("items").EnumerateArray().ToList();
        JsonElement Item(FieldSeed.Dispatched d) => items.Single(i => i.GetProperty("deliveryId").GetGuid() == d.DeliveryId);

        var byRoad = Item(road).GetProperty("estimatedBacktrack");
        Assert.Equal(18.6m, byRoad.GetProperty("km").GetDecimal());
        Assert.Equal(0.62m, byRoad.GetProperty("hours").GetDecimal());
        // Sin ruta ni perfil de costo: tarifa por defecto, con el mismo calculador que el resto del motor.
        Assert.Equal(LogisticsCostCalculator.CalculateCost(18.6m, 37m, options.DefaultCostPerKm, options.CostPerHour),
            byRoad.GetProperty("costUsd").GetDecimal());
        Assert.False(byRoad.GetProperty("isApproximate").GetBoolean());

        var byApprox = Item(approx).GetProperty("estimatedBacktrack");
        Assert.True(byApprox.GetProperty("isApproximate").GetBoolean());
        Assert.True(byApprox.GetProperty("km").GetDecimal() > 0m);

        Assert.Equal(JsonValueKind.Null, Item(surplus).GetProperty("estimatedBacktrack").ValueKind);
        Assert.Equal(JsonValueKind.Null, Item(noCoords).GetProperty("estimatedBacktrack").ValueKind);
    }

    [Fact]
    public async Task Field_roles_cannot_read_receipt_exceptions()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var driver = api.ClientFor(TestTokens.Driver);
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/receipts/exceptions")).StatusCode);
    }

    [Fact]
    public async Task Field_request_orders_are_filtered_and_cancellation_respects_status_and_version()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await SeedDeliveredAsync(api, "EXC-O");
        await ReceiveAsync(api, d, 12m, 0m, next: 20m); // crea un pedido FieldRequest

        Guid planned;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pointId = await db.Deliveries.Where(x => x.Id == d.DeliveryId).Select(x => x.ServicePointId).SingleAsync();
            var manual = new DeliveryOrder { OrderNumber = "ORD-MANUAL-116", DestinationServicePointId = pointId, SourceType = "Manual",
                                             RequiredDateUtc = DateTime.UtcNow.AddDays(2) };
            var plannedOrder = new DeliveryOrder { OrderNumber = "ORD-PLANNED-116", DestinationServicePointId = pointId, SourceType = "FieldRequest",
                                                   Status = DeliveryOrderStatus.Planned, RequiredDateUtc = DateTime.UtcNow.AddDays(2) };
            db.AddRange(manual, plannedOrder);
            await db.SaveChangesAsync();
            planned = plannedOrder.Id;
        }

        using var planner = api.ClientFor(TestTokens.Planner);
        var orders = (await GetJsonAsync(planner, "/api/v1/delivery-orders?sourceType=FieldRequest")).EnumerateArray().ToList();
        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal("FieldRequest", o.GetProperty("sourceType").GetString()));

        var pending = orders.Single(o => o.GetProperty("status").GetString() == DeliveryOrderStatus.Pending);
        var pendingId = pending.GetProperty("id").GetGuid();
        var version = pending.GetProperty("version").GetInt64();

        // Versión vieja: otro usuario lo cambió → 409 sin modificar.
        var stale = await planner.PatchAsync($"/api/v1/delivery-orders/{pendingId}/cancel?expectedVersion={version - 1}", null);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains("version_conflict", await stale.Content.ReadAsStringAsync());

        var cancel = await planner.PatchAsync($"/api/v1/delivery-orders/{pendingId}/cancel?expectedVersion={version}", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        // Reintento o doble clic: sigue cancelado, sin error.
        Assert.Equal(HttpStatusCode.OK, (await planner.PatchAsync($"/api/v1/delivery-orders/{pendingId}/cancel", null)).StatusCode);

        // Ya planificado: no se cancela desde aquí.
        var plannedCancel = await planner.PatchAsync($"/api/v1/delivery-orders/{planned}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, plannedCancel.StatusCode);
        Assert.Contains("order_not_pending", await plannedCancel.Content.ReadAsStringAsync());

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(DeliveryOrderStatus.Cancelled, await db.DeliveryOrders.Where(o => o.Id == pendingId).Select(o => o.Status).SingleAsync());
            Assert.Equal(DeliveryOrderStatus.Planned, await db.DeliveryOrders.Where(o => o.Id == planned).Select(o => o.Status).SingleAsync());
        }
    }
}
