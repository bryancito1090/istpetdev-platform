using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

// #287: las operaciones reintentables se serializan por organización y guardan su respuesta.
[Trait("Category", "Integration")]
public sealed class OperationIdempotencyTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private sealed record Seed(Guid PointId, string PointCode, Guid ProductId, Guid VehicleId, string VehicleCode,
        Guid WarehouseId, Guid DriverId);

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
        product.WeightPerBaseUnit = 1m;
        product.VolumePerBaseUnit = 0.001m;
        vehicle.MaxWeight = 100m;
        vehicle.MaxVolume = 10m;
        var lot = await db.Lots.Where(l => l.ProductId == product.Id && !l.IsBlocked)
            .OrderByDescending(l => l.ExpiresOn).FirstAsync();
        var balance = await db.StockBalances.FirstOrDefaultAsync(b => b.LocationId == warehouse.Id && b.LotId == lot.Id);
        if (balance is null)
            db.StockBalances.Add(new StockBalance { LocationId = warehouse.Id, LotId = lot.Id, QuantityOnHand = 1000m });
        else
            balance.QuantityOnHand += 1000m;
        await db.SaveChangesAsync();
        return new Seed(point.Id, point.Code, product.Id, vehicle.Id, vehicle.Code, warehouse.Id, driver.Id);
    }

    private static async Task<Guid> CreateOrderAsync(ApiFactory api, Seed seed, decimal quantity)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var order = new DeliveryOrder
        {
            OrganizationId = DemoOrganization.Id,
            OrderNumber = $"ORD-IDEM-{Guid.NewGuid():N}"[..20],
            DestinationServicePointId = seed.PointId,
            RequiredDateUtc = DateTime.UtcNow.AddDays(1),
            SourceType = "Manual",
            Status = DeliveryOrderStatus.Pending
        };
        db.Add(order);
        db.Add(new DeliveryOrderLine
        {
            OrganizationId = DemoOrganization.Id, DeliveryOrderId = order.Id,
            ProductId = seed.ProductId, RequestedQuantity = quantity
        });
        await db.SaveChangesAsync();
        return order.Id;
    }

    private static object ConfirmBody(Seed seed, Guid orderId, string code, Guid operationId) => new
    {
        code,
        vehicleId = seed.VehicleId,
        totalDistanceKm = 1m,
        totalDurationMinutes = 10m,
        totalCost = 1m,
        orderIds = new[] { orderId },
        operationId,
        stops = new object[]
        {
            new
            {
                sequence = 1, kind = "Pickup", locationId = seed.WarehouseId, code = "BOD", name = "Bodega",
                position = new { lat = -0.2m, lon = -78.5m }, deltaWeight = 0m, deltaVolume = 0m,
                windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15
            },
            new
            {
                sequence = 2, kind = "Delivery", servicePointId = seed.PointId, code = seed.PointCode, name = seed.PointCode,
                position = new { lat = -0.21m, lon = -78.49m }, deltaWeight = 0m, deltaVolume = 0m,
                windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15
            }
        }
    };

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid planId, Guid key, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/route-plans/{planId}/publish")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", key.ToString());
        return client.SendAsync(request);
    }

    [Fact]
    public async Task Concurrent_confirm_with_same_operation_creates_one_plan_and_replays()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var seed = await ResetAsync(api, planner);
        var orderId = await CreateOrderAsync(api, seed, 5m);
        var operationId = Guid.NewGuid();
        var code = $"WIZ-IDEM-{Guid.NewGuid():N}";
        var body = ConfirmBody(seed, orderId, code, operationId);

        var responses = await Task.WhenAll(
            planner.PostAsJsonAsync("/api/v1/route-plans/confirm", body),
            planner.PostAsJsonAsync("/api/v1/route-plans/confirm", body));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var bodies = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(bodies[0]), JsonNode.Parse(bodies[1])));
        var planId = JsonDocument.Parse(bodies[0]).RootElement.GetProperty("id").GetGuid();

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await db.RoutePlans.CountAsync(p => p.OrganizationId == DemoOrganization.Id && p.Code == code));
            Assert.Equal(planId, await db.RoutePlans.Where(p => p.Code == code).Select(p => p.Id).SingleAsync());
            Assert.Equal(1, await db.IdempotencyRecords.CountAsync(r => r.OperationId == operationId));
        }

        var changed = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm",
            ConfirmBody(seed, orderId, code + "-X", operationId));
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Contains("IDEMPOTENCY_CONFLICT", await changed.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Concurrent_publish_with_same_key_creates_one_delivery_and_conflicts_on_changed_payload()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        var seed = await ResetAsync(api, planner);
        var orderId = await CreateOrderAsync(api, seed, 5m);
        var confirm = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm",
            ConfirmBody(seed, orderId, $"WIZ-PUB-{Guid.NewGuid():N}", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, confirm.StatusCode);
        var planId = (await confirm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var key = Guid.NewGuid();
        var body = new { driverAssignments = new Dictionary<string, string>() };
        var responses = await Task.WhenAll(PublishAsync(planner, planId, key, body), PublishAsync(planner, planId, key, body));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var bodies = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(bodies[0]), JsonNode.Parse(bodies[1])));

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await db.Deliveries.CountAsync(d => db.RouteStops.Any(s =>
                s.OrganizationId == DemoOrganization.Id && s.RoutePlanId == planId && s.Id == d.RouteStopId)));
            Assert.Equal(1, await db.IdempotencyRecords.CountAsync(r => r.OperationId == key));
            Assert.True(await db.RouteStockReservations.AnyAsync(r => r.RoutePlanId == planId));
        }

        var changed = await PublishAsync(planner, planId, key,
            new { driverAssignments = new Dictionary<string, string> { [seed.VehicleCode] = seed.DriverId.ToString() } });
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Contains("IDEMPOTENCY_CONFLICT", await changed.Content.ReadAsStringAsync());
    }
}
