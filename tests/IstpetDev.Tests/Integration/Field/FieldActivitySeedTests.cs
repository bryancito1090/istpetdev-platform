using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Field;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Field;

[Trait("Category", "Integration")]
public sealed class FieldActivitySeedTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static FieldActivitySeed Seeder(IServiceProvider services) => new(
        services.GetRequiredService<ApplicationDbContext>(), services.GetRequiredService<IFieldService>(),
        services.GetRequiredService<IRoutePublisher>(), services.GetRequiredService<IRouteEvaluator>());

    private static async Task ResetAsync(ApiFactory api) =>
        (await api.ClientFor(TestTokens.Planner).PostAsync("/api/v1/demo/reset?fixture=demo-quito-planificacion", null)).EnsureSuccessStatusCode();

    [Fact]
    public async Task Rehearsal_runs_the_whole_chain_and_rolls_back_every_effect()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        await ResetAsync(api);
        decimal before;
        int accounts;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            before = await db.StockBalances.SumAsync(b => b.QuantityOnHand);
            accounts = await db.Accounts.CountAsync();
            var report = await Seeder(scope.ServiceProvider).RunAsync(false);
            Assert.False(report.Applied);
            Assert.True(report.Receipts >= 6);
            Assert.True(report.ReceiptsWithRejections > 0);
            Assert.True(report.ReceiptsWithAdjustments > 0);
            Assert.True(report.FieldRequests > 0);
        }
        using var fresh = api.Services.CreateScope();
        var persisted = fresh.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await persisted.Receipts.CountAsync());
        Assert.Equal(0, await persisted.RoutePlans.CountAsync());
        Assert.Equal(accounts, await persisted.Accounts.CountAsync());
        Assert.Equal(before, await persisted.StockBalances.SumAsync(b => b.QuantityOnHand));
    }

    [Fact]
    public async Task Apply_exposes_exceptions_and_field_requests_with_balanced_stock_and_idempotent_replay()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        await ResetAsync(api);
        decimal before, reserved;
        FieldSeedReport result;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            before = await db.StockBalances.SumAsync(b => b.QuantityOnHand);
            reserved = await db.StockBalances.SumAsync(b => b.QuantityReserved);
            result = await Seeder(scope.ServiceProvider).RunAsync(true);
            Assert.True(result.Applied);
            Assert.False(result.AlreadyApplied);
            Assert.Equal(result.DispatchedUnits, result.AcceptedUnits + result.RejectedUnits);
            Assert.InRange(result.RejectedUnitPercent, 0.01m, 10m);
        }
        using (var fresh = api.Services.CreateScope())
        {
            var db = fresh.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var adjustments = await db.InventoryMovements.Where(m => m.Kind == MovementKinds.FieldAdjustment).ToListAsync();
            var netAdjustment = adjustments.Sum(m => m.ToLocationId.HasValue ? m.Quantity : -m.Quantity);
            Assert.Equal(before + netAdjustment, await db.StockBalances.SumAsync(b => b.QuantityOnHand));
            Assert.Equal(reserved, await db.StockBalances.SumAsync(b => b.QuantityReserved));
            Assert.False(await db.StockBalances.AnyAsync(b => b.QuantityOnHand < 0 || b.QuantityReserved < 0));
            Assert.False(await db.ReceiptLines.AnyAsync(l => l.QuantityAccepted + l.QuantityRejected !=
                db.DeliveryLines.Where(d => d.Id == l.DeliveryLineId).Select(d => d.DispatchedQuantity).Single()));
            var ids = await db.Receipts.Select(r => r.Id).ToListAsync();
            var replay = await Seeder(fresh.ServiceProvider).RunAsync(true);
            Assert.True(replay.AlreadyApplied);
            Assert.Equal(result.Receipts, replay.Receipts);
            Assert.Equal(ids.Order(), (await db.Receipts.Select(r => r.Id).ToListAsync()).Order());
        }
        var exceptions = await api.ClientFor(TestTokens.Planner).GetFromJsonAsync<JsonElement>("/api/v1/receipts/exceptions");
        Assert.True(exceptions.GetProperty("items").GetArrayLength() >= 2);
        using var final = api.Services.CreateScope();
        var finalDb = final.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(result.FieldRequests, await finalDb.DeliveryOrders.CountAsync(o => o.SourceType == "FieldRequest" && o.Notes!.Contains("SINTÉTICO")));
    }

    [Fact]
    public async Task No_free_stock_rejects_without_creating_demonstration_records()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        await ResetAsync(api);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var balances = await db.StockBalances.ToListAsync();
        foreach (var balance in balances) balance.QuantityReserved = balance.QuantityOnHand;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Seeder(scope.ServiceProvider).RunAsync(true));
        Assert.Equal(0, await db.Receipts.CountAsync());
        Assert.Equal(0, await db.RoutePlans.CountAsync());
    }
}
