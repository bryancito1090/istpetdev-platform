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

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public sealed class RoutePlanningDecisionTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static async Task<Guid[]> SeedAsync(ApiFactory api, HttpClient client)
    {
        (await client.PostAsync("/api/v1/demo/reset", null)).EnsureSuccessStatusCode();
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = await db.Products.SingleAsync(p => p.Sku == "DES-01");
        product.WeightPerBaseUnit = 1m;
        product.VolumePerBaseUnit = .001m;
        var points = await db.ServicePoints.OrderBy(p => p.Code).Take(2).ToListAsync();
        var warehouse = await db.Locations.OrderBy(l => l.Code).FirstAsync(l => l.Kind == LocationKinds.Warehouse);
        foreach (var point in points) { point.Position = warehouse.Position; point.WindowStartLocal = new TimeOnly(7, 0); point.WindowEndLocal = new TimeOnly(20, 0); }
        db.Vehicles.Add(new Vehicle { Code = "VEH-CHOICE-02", DepotLocationId = warehouse.Id, MaxWeight = 10m, MaxVolume = 10m, CostPerKm = .50m, IsActive = true });
        await db.SaveChangesAsync();
        var vehicles = await db.Vehicles.ToListAsync();
        foreach (var vehicle in vehicles) { vehicle.DepotLocationId = warehouse.Id; vehicle.MaxWeight = 10m; vehicle.MaxVolume = 10m; vehicle.IsActive = true; }
        var lotIds = await db.Lots.Where(l => l.ProductId == product.Id).Select(l => l.Id).ToListAsync();
        foreach (var stock in await db.StockBalances.Where(b => lotIds.Contains(b.LotId)).ToListAsync())
        { stock.QuantityOnHand = 1000m; stock.QuantityReserved = 0; }
        var orders = points.Select((point, i) => new DeliveryOrder
        {
            OrderNumber = $"CHOICE-{i}", DestinationServicePointId = point.Id,
            RequiredDateUtc = new DateTime(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc), Priority = "High"
        }).ToList();
        foreach (var order in orders)
        {
            db.DeliveryOrders.Add(order);
            db.DeliveryOrderLines.Add(new DeliveryOrderLine { DeliveryOrderId = order.Id, ProductId = product.Id, RequestedQuantity = 6m });
        }
        await db.SaveChangesAsync();
        return orders.Select(o => o.Id).ToArray();
    }

    private static async Task<JsonElement> PrepareAsync(HttpClient client, Guid[] ids)
    {
        var response = await client.PostAsJsonAsync("/api/v1/route-plans/decisions", new { orderIds = ids, operationId = Guid.NewGuid() });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Assigned_orders_are_reported_before_suggesting_or_creating_another_decision()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var client = api.ClientFor(TestTokens.Planner);
        var ids = await SeedAsync(api, client);
        var run = await PrepareAsync(client, ids);
        var option = run.GetProperty("options").EnumerateArray().First(o => o.GetProperty("blockedReason").ValueKind == JsonValueKind.Null);
        (await client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{run.GetProperty("id").GetGuid()}/accept",
            new { optionId = option.GetProperty("optionId").GetString(), expectedVersion = 1, operationId = Guid.NewGuid() })).EnsureSuccessStatusCode();
        foreach (var endpoint in new[] { "suggest", "decisions" })
        foreach (var planRemainingOnly in new[] { false, true })
        {
            var response = await client.PostAsJsonAsync($"/api/v1/route-plans/{endpoint}", new { orderIds = ids, planRemainingOnly, operationId = Guid.NewGuid() });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var warning = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("orders_already_assigned", warning.GetProperty("code").GetString());
            Assert.Equal(2, warning.GetProperty("assignedOrders").GetArrayLength());
            Assert.All(warning.GetProperty("assignedOrders").EnumerateArray(), o => Assert.False(o.GetProperty("hasPendingQuantity").GetBoolean()));
        }
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.RoutePlanningDecisions.CountAsync());
        Assert.Equal(2, await db.RoutePlans.CountAsync());

        // Una reposición nueva en el mismo destino es otro pedido, no la entrega ya asignada.
        var prior = await db.DeliveryOrders.FirstAsync(o => o.Id == ids[0]);
        var productId = await db.DeliveryOrderLines.Where(l => l.DeliveryOrderId == prior.Id).Select(l => l.ProductId).SingleAsync();
        var fresh = new DeliveryOrder { OrderNumber = "FIELD-NEXT-VISIT", SourceType = "FieldRequest", DestinationServicePointId = prior.DestinationServicePointId };
        db.DeliveryOrders.Add(fresh);
        db.DeliveryOrderLines.Add(new DeliveryOrderLine { DeliveryOrderId = fresh.Id, ProductId = productId, RequestedQuantity = 2m });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/route-plans/suggest", new { orderIds = new[] { fresh.Id } })).StatusCode);
    }

    [Fact]
    public async Task Partial_assignment_requires_explicit_choice_and_calculates_only_the_remainder()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var client = api.ClientFor(TestTokens.Planner);
        var ids = await SeedAsync(api, client);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreach (var line in await db.DeliveryOrderLines.ToListAsync()) line.PlannedQuantity = 2m;
            await db.SaveChangesAsync();
        }
        foreach (var endpoint in new[] { "suggest", "decisions" })
        {
            var blocked = await client.PostAsJsonAsync($"/api/v1/route-plans/{endpoint}", new { orderIds = ids, operationId = Guid.NewGuid() });
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            var allowed = await client.PostAsJsonAsync($"/api/v1/route-plans/{endpoint}", new { orderIds = ids, planRemainingOnly = true, operationId = Guid.NewGuid() });
            Assert.True(allowed.IsSuccessStatusCode, await allowed.Content.ReadAsStringAsync());
            var doc = await allowed.Content.ReadFromJsonAsync<JsonElement>();
            if (endpoint == "suggest")
                Assert.All(doc.GetProperty("options").EnumerateArray().SelectMany(o => o.GetProperty("sourcingLines").EnumerateArray()),
                    line => Assert.InRange(line.GetProperty("plannedQuantity").GetDecimal(), 0m, 4m));
        }
        using var final = api.Services.CreateScope();
        Assert.All(await final.ServiceProvider.GetRequiredService<ApplicationDbContext>().DeliveryOrderLines.ToListAsync(), l => Assert.Equal(2m, l.PlannedQuantity));
    }

    [Fact]
    public async Task Acceptance_keeps_every_vehicle_discards_siblings_and_survives_replay_and_reload()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: new Dictionary<string, string?> { ["Logistics:Osrm:BaseUrl"] = "", ["Logistics:Osrm:RouteGeometryUrl"] = "" });
        var client = api.ClientFor(TestTokens.Planner);
        var ids = await SeedAsync(api, client);
        var run = await PrepareAsync(client, ids);
        var options = run.GetProperty("options").EnumerateArray().ToList();
        var feasible = options.Where(o => o.GetProperty("blockedReason").ValueKind == JsonValueKind.Null).ToList();
        Assert.True(feasible.Count >= 2, run.ToString());
        var option = feasible[0];
        Assert.Equal(2, option.GetProperty("routes").GetArrayLength());
        var runId = run.GetProperty("id").GetGuid();
        var body = new { optionId = option.GetProperty("optionId").GetString(), expectedVersion = run.GetProperty("version").GetInt64(), operationId = Guid.NewGuid() };
        var accepted = await client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{runId}/accept", body);
        Assert.True(accepted.IsSuccessStatusCode, await accepted.Content.ReadAsStringAsync());
        var doc = await accepted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, doc.GetProperty("routeIds").GetArrayLength());
        Assert.Single(doc.GetProperty("options").EnumerateArray(), o => o.GetProperty("status").GetString() == "Accepted");
        Assert.All(doc.GetProperty("options").EnumerateArray().Where(o => o.GetProperty("optionId").GetString() != body.optionId), o => Assert.Equal("Discarded", o.GetProperty("status").GetString()));
        var replay = await client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{runId}/accept", body);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(doc.GetRawText()), JsonNode.Parse(await replay.Content.ReadAsStringAsync())));
        var reload = await client.GetFromJsonAsync<JsonElement>($"/api/v1/route-plans/decisions/{runId}");
        Assert.Equal("Accepted", reload.GetProperty("status").GetString());
        var competitor = await client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{runId}/accept", new
        { optionId = feasible[1].GetProperty("optionId").GetString(), expectedVersion = 1, operationId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, competitor.StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.RoutePlans.CountAsync());
        Assert.All(await db.DeliveryOrderLines.ToListAsync(), l => Assert.Equal(6m, l.PlannedQuantity));
    }

    [Fact]
    public async Task Concurrent_choices_have_one_winner_and_failed_validation_keeps_all_options()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: new Dictionary<string, string?> { ["Logistics:Osrm:BaseUrl"] = "", ["Logistics:Osrm:RouteGeometryUrl"] = "" });
        var client = api.ClientFor(TestTokens.Planner);
        var ids = await SeedAsync(api, client);
        var run = await PrepareAsync(client, ids);
        var runId = run.GetProperty("id").GetGuid();
        var options = run.GetProperty("options").EnumerateArray().Where(o => o.GetProperty("blockedReason").ValueKind == JsonValueKind.Null).Take(2).ToList();
        Assert.Equal(2, options.Count);
        var responses = await Task.WhenAll(options.Select(o => client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{runId}/accept", new
        { optionId = o.GetProperty("optionId").GetString(), expectedVersion = 1, operationId = Guid.NewGuid() })));
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.ClientFor(TestTokens.Driver).PostAsJsonAsync($"/api/v1/route-plans/decisions/{runId}/accept", new { optionId = "A", expectedVersion = 1, operationId = Guid.NewGuid() })).StatusCode);
    }

    [Fact]
    public async Task Stale_quantities_do_not_discard_options_or_create_routes()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: new Dictionary<string, string?> { ["Logistics:Osrm:BaseUrl"] = "", ["Logistics:Osrm:RouteGeometryUrl"] = "" });
        var client = api.ClientFor(TestTokens.Planner);
        var ids = await SeedAsync(api, client);
        var run = await PrepareAsync(client, ids);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.DeliveryOrderLines.FirstAsync()).PlannedQuantity = 1;
            await db.SaveChangesAsync();
        }
        var runId = run.GetProperty("id").GetGuid();
        var response = await client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{runId}/accept", new { optionId = "A", expectedVersion = 1, operationId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var reload = await client.GetFromJsonAsync<JsonElement>($"/api/v1/route-plans/decisions/{runId}");
        Assert.Equal("Open", reload.GetProperty("status").GetString());
        Assert.All(reload.GetProperty("options").EnumerateArray(), o => Assert.Equal("Available", o.GetProperty("status").GetString()));
        using var check = api.Services.CreateScope();
        Assert.Equal(0, await check.ServiceProvider.GetRequiredService<ApplicationDbContext>().RoutePlans.CountAsync());
    }

    [Fact]
    public async Task Publish_is_atomic_and_individual_publication_cannot_split_an_accepted_alternative()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: new Dictionary<string, string?> { ["Logistics:Osrm:BaseUrl"] = "", ["Logistics:Osrm:RouteGeometryUrl"] = "" });
        var client = api.ClientFor(TestTokens.Planner);
        var orderIds = await SeedAsync(api, client);
        var run = await PrepareAsync(client, orderIds);
        var id = run.GetProperty("id").GetGuid();
        var accepted = await client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{id}/accept", new { optionId = "A", expectedVersion = 1, operationId = Guid.NewGuid() });
        Assert.True(accepted.IsSuccessStatusCode, await accepted.Content.ReadAsStringAsync());
        var decision = await accepted.Content.ReadFromJsonAsync<JsonElement>();
        var routeIds = decision.GetProperty("routeIds").EnumerateArray().Select(r => r.GetGuid()).ToList();
        var assignments = new Dictionary<string, string>();
        List<Guid> balanceIds;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var drivers = await db.Accounts.Where(a => a.Role == Roles.Conductor && a.IsActive).OrderBy(a => a.Id).ToListAsync();
            var vehicles = await db.Vehicles.OrderBy(v => v.Code).ToListAsync();
            for (var i = 0; i < vehicles.Count; i++) assignments[vehicles[i].Code] = drivers[i].Id.ToString();
            var product = await db.Products.SingleAsync(p => p.Sku == "DES-01");
            var lots = await db.Lots.Where(l => l.ProductId == product.Id).Select(l => l.Id).ToListAsync();
            var balances = await db.StockBalances.Where(b => lots.Contains(b.LotId)).ToListAsync();
            foreach (var balance in balances) balance.QuantityOnHand = 0;
            balances[0].QuantityOnHand = 10;
            balanceIds = balances.Select(b => b.Id).ToList();
            await db.SaveChangesAsync();
        }
        var direct = await client.PostAsJsonAsync($"/api/v1/route-plans/{routeIds[0]}/publish", new { driverAssignments = assignments });
        Assert.Equal(HttpStatusCode.Conflict, direct.StatusCode);
        var body = new { driverAssignments = assignments, operationId = Guid.NewGuid() };
        var failed = await client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{id}/publish", body);
        Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await db.Deliveries.CountAsync());
            Assert.Equal(0, await db.RouteStockReservations.CountAsync());
            Assert.All(await db.RoutePlans.ToListAsync(), p => Assert.Equal("Proposed", p.Status));
            foreach (var balance in await db.StockBalances.Where(b => balanceIds.Contains(b.Id)).ToListAsync())
                balance.QuantityOnHand = 1000;
            await db.SaveChangesAsync();
        }
        var published = await client.PostAsJsonAsync($"/api/v1/route-plans/decisions/{id}/publish", body);
        Assert.True(published.IsSuccessStatusCode, await published.Content.ReadAsStringAsync());
        Assert.True((await published.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("published").GetBoolean());
        var reloaded = await client.GetFromJsonAsync<JsonElement>($"/api/v1/route-plans/decisions/{id}");
        Assert.True(reloaded.GetProperty("published").GetBoolean());
    }
}
