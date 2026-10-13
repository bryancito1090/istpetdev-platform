using System.Diagnostics;
using System.Text;
using IstpetDev.Application.Demo;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Demo;
using IstpetDev.Infrastructure.Imports;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace IstpetDev.Tests.Integration.Imports;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class ImportSafetyTests(PostgisContainer postgres, ITestOutputHelper output)
{
    private static ImportRequest Csv(ImportEntity entity, string rows, bool partial = false, bool dryRun = false, Guid? actor = null) =>
        new([new ImportFile(entity, "data.csv", new MemoryStream(Encoding.UTF8.GetBytes(rows)))], MappingProfile.Auto(), dryRun, partial, actor);
    private static async Task<ImportReport> Import(ApplicationDbContext db, ImportRequest request)
    {
        try { return await new ImportService(db, new TabularReader()).RunAsync(request); }
        finally { db.ChangeTracker.Clear(); foreach (var file in request.Files) await file.Content.DisposeAsync(); }
    }
    private static string Stock(decimal quantity) => $"location_kind,location_code,sku,lot_code,quantity\nservice_point,HOS-001,DES-01,L-DES-2610,{quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n";

    [Fact]
    public async Task Duplicate_stock_is_aggregated_before_delta_and_reimport_is_stable()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        await Import(db, Fixtures.Request("demo-base"));
        var rows = Stock(20) + "service_point,HOS-001,DES-01,L-DES-2610,30\n";
        var report = await Import(db, Csv(ImportEntity.Stock, rows));
        Assert.True(report.Applied); Assert.Equal(1, report.Entities.Single().Updated);
        Assert.Equal(1, report.Entities.Single().Duplicates);
        var movement = await db.InventoryMovements.OrderByDescending(m => m.CreatedAtUtc).FirstAsync();
        Assert.Equal(10m, movement.Quantity); Assert.NotNull(movement.FromLocationId); Assert.Null(movement.ToLocationId);
        var count = await db.InventoryMovements.CountAsync();
        await Import(db, Csv(ImportEntity.Stock, rows));
        Assert.Equal(count, await db.InventoryMovements.CountAsync());
        await Import(db, Csv(ImportEntity.Stock, Stock(70)));
        var net = await db.InventoryMovements.Where(m => m.LotId == movement.LotId && (m.FromLocationId == movement.FromLocationId || m.ToLocationId == movement.FromLocationId))
            .SumAsync(m => m.ToLocationId == movement.FromLocationId ? m.Quantity : -m.Quantity);
        Assert.Equal(70m, net);
    }

    [Fact]
    public async Task Partial_import_does_not_create_auxiliary_rows_for_invalid_parents()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var request = new ImportRequest([
            Csv(ImportEntity.ServicePoints, "code,name\nBAD,\n").Files[0],
            Csv(ImportEntity.Products, "sku,name,base_unit\nS,Producto,L\n").Files[0],
            Csv(ImportEntity.Stock, "location_code,sku,quantity\nBAD,S,10\n").Files[0]], MappingProfile.Auto(), false, true, null);
        var report = await Import(db, request);
        Assert.True(report.Applied); Assert.True(report.HasErrors);
        Assert.Equal(1, await db.Products.CountAsync());
        Assert.Equal(0, await db.ServicePoints.CountAsync()); Assert.Equal(0, await db.Locations.CountAsync());
        Assert.Equal(0, await db.Lots.CountAsync()); Assert.Equal(0, await db.StockBalances.CountAsync());
    }

    [Fact]
    public async Task Invalid_dry_run_and_partial_with_no_valid_rows_are_rejected_and_recorded()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        const string bad = "code,name\nP,\n";
        foreach (var dry in new[] { true, false })
        {
            var report = await Import(db, Csv(ImportEntity.ServicePoints, bad, partial: true, dryRun: dry));
            Assert.False(report.Applied); Assert.True(report.HasErrors);
            Assert.Equal("Rejected", (await db.ImportBatches.SingleAsync(b => b.Id == report.BatchId)).Status);
        }
        Assert.Equal(0, await db.ServicePoints.CountAsync());
    }

    [Fact]
    public async Task Explicit_unknown_lot_and_incompatible_periods_do_not_change_stock_or_consumption()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        await Import(db, Fixtures.Request("demo-base"));
        var unknown = await Import(db, Csv(ImportEntity.Stock, Stock(12).Replace("L-DES-2610", "UNKNOWN"), partial: true));
        Assert.False(unknown.Applied); Assert.Equal(2, await db.Lots.CountAsync());
        var periods = await Import(db, Csv(ImportEntity.Consumptions,
            "point_code,sku,date,quantity,period_days\nHOS-001,DES-01,2026-10-01,5,7\nHOS-001,DES-01,2026-10-01,8,30\n", partial: true));
        Assert.False(periods.Applied); Assert.Contains(periods.Issues, i => i.Code == "incompatible_periods");
        Assert.Equal(7, await db.Consumptions.CountAsync());
    }

    [Fact]
    public async Task Simultaneous_imports_serialize_the_absolute_balance_and_ledger()
    {
        var connection = postgres.NewDatabase();
        await using (var seed = await TestDb.CreateMigratedAsync(connection)) await Import(seed, Fixtures.Request("demo-base"));
        async Task Apply(decimal quantity)
        { await using var db = TestDb.Context(connection); Assert.True((await Import(db, Csv(ImportEntity.Stock, Stock(quantity)))).Applied); }
        await Task.WhenAll(Apply(73), Apply(11));
        await using var check = TestDb.Context(connection);
        var location = await check.Locations.SingleAsync(l => l.Kind == LocationKinds.ServicePoint && l.Code == "HOS-001");
        var lot = await check.Lots.SingleAsync(l => l.Code == "L-DES-2610");
        var balance = await check.StockBalances.SingleAsync(b => b.LocationId == location.Id && b.LotId == lot.Id);
        Assert.Contains(balance.QuantityOnHand, new[] { 73m, 11m });
        var net = await check.InventoryMovements.Where(m => m.LotId == lot.Id && (m.FromLocationId == location.Id || m.ToLocationId == location.Id))
            .SumAsync(m => m.ToLocationId == location.Id ? m.Quantity : -m.Quantity);
        Assert.Equal(balance.QuantityOnHand, net);
    }

    [Fact]
    public async Task Failed_reset_rolls_back_data_and_retains_rejected_batch_and_actor()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var actor = new Account { Issuer = "test", Subject = "planner", Role = Roles.Planificador, IsActive = true };
        db.Add(actor); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await Import(db, Fixtures.Request("demo-base"));
        var before = await db.StockBalances.SumAsync(b => b.QuantityOnHand);
        var folder = Path.Combine(Path.GetTempPath(), $"istpetdev-fixture-{Guid.NewGuid():N}"); Directory.CreateDirectory(folder);
        try
        {
            foreach (var file in Directory.GetFiles(Fixtures.Folder("demo-base"))) File.Copy(file, Path.Combine(folder, Path.GetFileName(file)));
            File.WriteAllText(Path.Combine(folder, "inventario.csv"), "location_code,sku,quantity\nUNKNOWN,DES-01,12\n");
            var reset = new DemoResetService(db, new ImportService(db, new TabularReader()), Options.Create(new DemoOptions { Enabled = true, FixturePath = folder }));
            var report = await reset.ResetAsync(actorAccountId: actor.Id);
            Assert.False(report.Applied); Assert.Equal(before, await db.StockBalances.SumAsync(b => b.QuantityOnHand));
            Assert.Equal(2, await db.ImportBatches.CountAsync());
            var rejected = await db.ImportBatches.SingleAsync(b => b.Id == report.BatchId);
            Assert.Equal("Rejected", rejected.Status); Assert.Equal(actor.Id, rejected.CreatedByAccountId);
            Assert.Equal(1, await db.Accounts.CountAsync());
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public async Task Natural_keys_preserve_codes_containing_delimiters()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var request = new ImportRequest([
            Csv(ImportEntity.Products, "sku,name,base_unit\nA|B,Uno,L\nA,Dos,L\n").Files[0],
            Csv(ImportEntity.Lots, "sku,lot_code\nA|B,C\nA,B|C\n").Files[0]], MappingProfile.Auto(), false, false, null);
        var report = await Import(db, request);
        Assert.True(report.Applied); Assert.Equal(2, await db.Lots.CountAsync());
        Assert.Equal(2, report.Entities.Single(e => e.Entity == "lots").Created);
    }

    [Fact]
    public async Task Performance_targets_and_file_hashes_are_measured()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var watch = Stopwatch.StartNew(); var fixture = await Import(db, Fixtures.Request("demo-base")); watch.Stop();
        Assert.True(fixture.Applied); Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5));
        output.WriteLine($"demo_fixture_ms={watch.Elapsed.TotalMilliseconds:F1}");
        var rows = new StringBuilder("point_code,sku,date,quantity\n");
        for (var i = 0; i < 10_000; i++) rows.AppendLine($"HOS-001,DES-01,{new DateOnly(1990, 1, 1).AddDays(i):yyyy-MM-dd},1");
        watch.Restart(); var report = await Import(db, Csv(ImportEntity.Consumptions, rows.ToString())); watch.Stop();
        Assert.True(report.Applied); Assert.True(watch.Elapsed < TimeSpan.FromSeconds(30)); Assert.Equal(10_007, await db.Consumptions.CountAsync());
        output.WriteLine($"consumption_rows=10000 import_ms={watch.Elapsed.TotalMilliseconds:F1}");
        var batch = await db.ImportBatches.SingleAsync(b => b.Id == report.BatchId);
        Assert.Equal(64, batch.InputHash.Length); Assert.Equal(64, batch.ProfileHash.Length); Assert.Contains("sha256", batch.FilesJson);
    }
}
