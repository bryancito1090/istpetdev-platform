using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public sealed class RouteProposalAcceptanceTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static async Task<Guid[]> SeedAsync(ApiFactory api, bool legacyReceipt)
    {
        (await api.ClientFor(TestTokens.Planner).PostAsync("/api/v1/demo/reset", null)).EnsureSuccessStatusCode();
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var vehicle = await db.Vehicles.FirstAsync();
        var second = new Vehicle { Code = "ACCEPT-02", DepotLocationId = vehicle.DepotLocationId, MaxWeight = 1000, IsActive = true };
        db.Vehicles.Add(second);
        var batch = Guid.NewGuid();
        RoutePlan Plan(string code, Guid vehicleId, int revision, string status = "Proposed", string input = "{}") => new()
        { Code = code, VehicleId = vehicleId, Revision = revision, Status = status, AlgorithmVersion = "2.0-deterministic",
            PlannedStartUtc = new DateTime(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc), InputJson = input };
        var chosen = new[] { Plan("PLAN-ONE", vehicle.Id, 2), Plan("PLAN-TWO", second.Id, 2) };
        if (!legacyReceipt) foreach (var plan in chosen) plan.InputJson = JsonSerializer.Serialize(new { proposalBatchId = batch });
        var others = new[] { Plan("PLAN-ONE", vehicle.Id, 1), Plan("PLAN-TWO", second.Id, 1),
            Plan("PLAN-ONE", vehicle.Id, 0, "Published"), Plan("UNRELATED", vehicle.Id, 1) };
        var operationalRevision = Plan("PLAN-ONE", vehicle.Id, 3);
        operationalRevision.SupersedesRoutePlanId = others[2].Id;
        db.RoutePlans.AddRange(chosen.Concat(others).Append(operationalRevision));
        if (legacyReceipt) db.IdempotencyRecords.Add(new IdempotencyRecord { AccountId = await db.Accounts.Select(a => a.Id).FirstAsync(),
            OperationId = batch, ActionCode = "CreateRoutePlans", RequestHash = "fixture", ResponseStatus = 201,
            ResponseJson = JsonSerializer.Serialize(new { routes = chosen.Select(p => new { id = p.Id }) }) });
        await db.SaveChangesAsync();
        return chosen.Concat(others).Append(operationalRevision).Select(p => p.Id).ToArray();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Discard_persists_after_reload_preserves_every_chosen_vehicle_and_cannot_be_published(bool legacyReceipt)
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var ids = await SeedAsync(api, legacyReceipt);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var selected = await db.RoutePlans.SingleAsync(p => p.Id == ids[0]);
            var batchIds = await RouteProposalAcceptance.ResolveBatchAsync(db, selected, default);
            Assert.Equal(ids.Take(2).Order(), batchIds.Order());
            Assert.Equal(2, await RouteProposalAcceptance.AcceptAsync(db, batchIds, default));
            Assert.Equal(0, await RouteProposalAcceptance.AcceptAsync(db, batchIds, default));
        }
        using (var fresh = api.Services.CreateScope())
        {
            var db = fresh.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(2, await db.RoutePlans.CountAsync(p => p.Status == "Discarded"));
            Assert.Equal("Proposed", (await db.RoutePlans.FindAsync(ids[1]))!.Status);
            Assert.Equal("Published", (await db.RoutePlans.FindAsync(ids[4]))!.Status);
            Assert.Equal("Proposed", (await db.RoutePlans.FindAsync(ids[5]))!.Status);
            Assert.Equal("Proposed", (await db.RoutePlans.FindAsync(ids[6]))!.Status);
        }
        var client = api.ClientFor(TestTokens.Planner);
        for (var reload = 0; reload < 2; reload++)
        {
            var list = await client.GetFromJsonAsync<JsonElement>("/api/v1/route-plans");
            var visible = list.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToList();
            Assert.DoesNotContain(ids[2], visible); Assert.DoesNotContain(ids[3], visible);
            Assert.All(ids.Take(2), id => Assert.Contains(id, visible));
        }
        var rejected = await client.PostAsJsonAsync($"/api/v1/route-plans/{ids[2]}/publish", new { });
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
    }

    [Fact]
    public async Task Failed_publication_does_not_discard_competing_routes()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var ids = await SeedAsync(api, legacyReceipt: false);
        // Sin paradas/cargas: el publicador debe rechazar antes de comprometer la elección.
        var failed = await api.ClientFor(TestTokens.Planner).PostAsJsonAsync($"/api/v1/route-plans/{ids[0]}/publish", new { });
        Assert.Equal(HttpStatusCode.Conflict, failed.StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await db.RoutePlans.CountAsync(p => p.Status == "Discarded"));
        Assert.Equal(0, await db.RouteStockReservations.CountAsync());
        Assert.Equal(0, await db.Deliveries.CountAsync());
    }
}
