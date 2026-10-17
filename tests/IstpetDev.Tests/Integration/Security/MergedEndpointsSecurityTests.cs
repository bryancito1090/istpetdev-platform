using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace IstpetDev.Tests.Integration.Security;

[Trait("Category", "Integration")]
public sealed class MergedEndpointsSecurityTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    // Any SaveChanges during the GET fails the HTTP request, including incidental cache writes.
    private sealed class WriteGuard : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public int Attempts { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (Enabled) Attempts++;
            Assert.False(Enabled, "A GET attempted SaveChanges.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class OsrmHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { code = "Ok", distances = new[] { new[] { 0, 4500 }, new[] { 4600, 0 } },
                    durations = new[] { new[] { 0, 600 }, new[] { 660, 0 } } })
            });
        }
    }

    private sealed class GuardedApi(string connection) : ApiFactory(connection, overrides: new Dictionary<string, string?>
    {
        ["Logistics:Osrm:BaseUrl"] = "http://osrm.test",
        ["Logistics:Osrm:RouteGeometryUrl"] = "",
        ["Agent:AutoOnIncident"] = "false"
    })
    {
        public WriteGuard Guard { get; } = new();
        public OsrmHandler Osrm { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddDbContext<ApplicationDbContext>(options => options.AddInterceptors(Guard));
                services.AddHttpClient<IDistanceProvider, DistanceProvider>().ConfigurePrimaryHttpMessageHandler(() => Osrm);
            });
        }
    }

    [Fact]
    public async Task All_reviewed_GETs_are_pure_even_when_backtrack_needs_OSRM()
    {
        await using var api = new GuardedApi(postgres.NewDatabase());
        using var client = api.ClientFor(TestTokens.Planner);
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 10m, "SEC286");
        Guid planId, incidentId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var delivery = await db.Deliveries.SingleAsync(x => x.Id == d.DeliveryId);
            (await db.Locations.SingleAsync(x => x.Id == delivery.OriginLocationId)).Position = new Point(-78.5, -0.2) { SRID = 4326 };
            (await db.ServicePoints.SingleAsync(x => x.Id == delivery.ServicePointId)).Position = new Point(-78.51, -0.21) { SRID = 4326 };
            var vehicleLocation = await db.Locations.SingleAsync(x => x.Id == d.VehicleLocationId);
            var plan = new RoutePlan { Id = Guid.Parse("286abcde-0000-4000-8000-000000000001"),
                Code = "PLAN-SEC286", Revision = 1, Status = "Published", VehicleId = vehicleLocation.VehicleId!.Value,
                PlannedStartUtc = DateTime.UtcNow, InputJson = "{}" };
            var stop = new RouteStop { RoutePlanId = plan.Id, ServicePointId = delivery.ServicePointId, SequenceNumber = 1 };
            delivery.RouteStopId = stop.Id;
            var receipt = new Receipt { DeliveryId = delivery.Id, ActorAccountId = d.DriverAccountId,
                OperationId = Guid.NewGuid(), ReceivedAtServer = DateTime.UtcNow };
            var incident = new Incident { Kind = "VEHICLE_BREAKDOWN", Status = "Active", RoutePlanId = plan.Id, OccurredAtUtc = DateTime.UtcNow };
            db.AddRange(plan, stop, receipt, incident, new ReceiptLine { ReceiptId = receipt.Id,
                DeliveryLineId = d.DeliveryLineId, QuantityAccepted = 8, QuantityRejected = 2, RejectionReason = "Faltante" });
            await db.SaveChangesAsync();
            planId = plan.Id; incidentId = incident.Id;
        }
        api.Guard.Enabled = true;
        var before = await SnapshotAsync(api);
        for (var repeat = 0; repeat < 2; repeat++)
        {
            foreach (var path in new[] { $"incidents/{incidentId}", "receipts/exceptions", "return-guides/GRT-286ABCDE",
                "route-plans", $"route-plans/{planId}", $"route-plans/{planId}/geometry" })
            {
                var response = await client.GetAsync($"/api/v1/{path}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                if (path == "receipts/exceptions")
                {
                    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var backtrack = body.GetProperty("items")[0].GetProperty("estimatedBacktrack");
                    Assert.Equal(9.1m, backtrack.GetProperty("km").GetDecimal());
                    Assert.False(backtrack.GetProperty("isApproximate").GetBoolean());
                }
            }
        }
        Assert.Equal(2, api.Osrm.Calls);
        Assert.Equal(0, api.Guard.Attempts);
        Assert.Equal(before, await SnapshotAsync(api));
    }

    private static async Task<string> SnapshotAsync(ApiFactory api)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return JsonSerializer.Serialize(new
        {
            Edges = await db.RoadMatrixEdges.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Units = await db.HandlingUnits.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Events = await db.CustodyEvents.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Balances = await db.StockBalances.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Movements = await db.InventoryMovements.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Plans = await db.RoutePlans.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
        });
    }

    [Theory]
    [InlineData("incidents", "{\"kind\":\"DELAY\",\"radiusKm\":-1}")]
    [InlineData("incidents", "{\"kind\":\"DELAY\",\"lat\":91,\"lon\":0}")]
    [InlineData("incidents", "{\"kind\":\"DELAY\",\"lat\":0,\"lon\":181}")]
    [InlineData("incidents", "{\"kind\":\"DELAY\",\"lat\":0}")]
    [InlineData("incidents", "{\"kind\":\" \"}")]
    [InlineData("route-plans/manual", "{\"totalDistanceKm\":-1,\"stops\":[{}]}")]
    [InlineData("route-plans/manual", "{\"totalDurationMinutes\":-1,\"stops\":[{}]}")]
    [InlineData("route-plans/confirm", "{\"totalCost\":-1,\"stops\":[{}]}")]
    [InlineData("route-plans/confirm", "{\"totalDistanceKm\":-1,\"stops\":[{}]}")]
    [InlineData("route-plans/confirm", "{\"totalDurationMinutes\":-1,\"stops\":[{}]}")]
    [InlineData("route-plans/confirm", "{\"lines\":[{\"plannedQuantity\":-1}],\"stops\":[{}]}")]
    [InlineData("route-plans/confirm", "{\"stops\":[{\"loads\":[{\"quantity\":-1}]}]}")]
    [InlineData("route-plans/evaluate", "{\"stops\":[{\"serviceMinutes\":-1}]}")]
    [InlineData("route-plans/evaluate", "{\"stops\":[{\"loads\":[{\"quantity\":-1}]}]}")]
    public async Task Invalid_input_is_rejected_before_writes(string endpoint, string json)
    {
        await using var api = new GuardedApi(postgres.NewDatabase());
        using var client = api.ClientFor(TestTokens.Planner);
        api.Guard.Enabled = true;
        var body = JsonNode.Parse(json)!;
        if (body["stops"] is JsonArray stops)
        {
            foreach (var stop in stops)
            {
                stop!["kind"] = "Delivery";
                stop["code"] = "P-286";
                stop["name"] = "Punto de prueba";
                if (stop["loads"] is JsonArray loads)
                    foreach (var load in loads)
                    {
                        load!["sku"] = "SKU-286";
                        load["baseUnit"] = "U";
                    }
            }
        }
        var response = await client.PostAsync($"/api/v1/{endpoint}",
            new StringContent(body.ToJsonString(), System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(endpoint == "incidents" ? "invalid_incident_input" : "negativ", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, api.Guard.Attempts);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.RoutePlans.ToListAsync());
        Assert.Empty(await db.Incidents.ToListAsync());
    }

    [Theory]
    [InlineData(Roles.Auditor)]
    [InlineData(Roles.Receptor)]
    public async Task Read_only_roles_cannot_mutate_reviewed_endpoints(string role)
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, role);
        foreach (var path in new[] { "incidents", "return-guides/generate", "return-guides/receive", "route-plans",
            "route-plans/suggest", "route-plans/evaluate", "route-plans/confirm", "route-plans/manual", "route-plans/emergency",
            $"route-plans/{Guid.NewGuid()}/publish", $"route-plans/{Guid.NewGuid()}/cancel", $"route-plans/{Guid.NewGuid()}/revert",
            $"route-plans/{Guid.NewGuid()}/replan", $"route-plans/{Guid.NewGuid()}/stops/1/complete" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/v1/{path}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/v1/route-plans/{Guid.NewGuid()}/driver", new { })).StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Route_list_rejects_unbounded_limits(int limit)
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var client = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/route-plans?limit={limit}")).StatusCode);
    }
}
