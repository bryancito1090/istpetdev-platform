using System.Text;
using ClosedXML.Excel;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Imports;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Imports;

[Trait("Category", "Integration")]
public class ImportServiceTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static async Task<ImportReport> Import(ApplicationDbContext db, ImportRequest request)
    {
        try { return await new ImportService(db, new TabularReader()).RunAsync(request); }
        finally { db.ChangeTracker.Clear(); foreach (var file in request.Files) await file.Content.DisposeAsync(); }
    }

    private static ImportFile Csv(ImportEntity entity, string content) =>
        new(entity, $"{entity.Key()}.csv", new MemoryStream(Encoding.UTF8.GetBytes(content)));

    private static string Describe(ImportReport report) => string.Join("\n", report.Issues.Select(i => $"{i.Entity}:{i.Row}:{i.Field} {i.Message}"));

    [Fact]
    public async Task Demo_base_fixture_loads_the_expected_network()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, Fixtures.Request("demo-base"));

        Assert.True(report.Applied, Describe(report));
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(6, await db.ServicePoints.CountAsync());
        Assert.Equal(8, await db.Locations.CountAsync());
        Assert.Equal(2, await db.Products.CountAsync());
        Assert.Equal(1, await db.Vehicles.CountAsync());
        Assert.Equal(2, await db.Lots.CountAsync());
        Assert.Equal(9, await db.StockBalances.CountAsync());
        Assert.Equal(7, await db.Consumptions.CountAsync());
        Assert.Equal(9, await db.InventoryMovements.CountAsync(m => m.Kind == MovementKinds.ImportAdjustment));
        var hospitalDisinfectant = await (
            from balance in db.StockBalances
            join location in db.Locations on balance.LocationId equals location.Id
            join lot in db.Lots on balance.LotId equals lot.Id
            where location.Kind == LocationKinds.ServicePoint && location.Code == "HOS-001" && lot.Code == "L-DES-2610"
            select balance.QuantityOnHand).SingleAsync();
        Assert.Equal(60m, hospitalDisinfectant);
        Assert.Equal("Applied", (await db.ImportBatches.SingleAsync()).Status);
    }

    [Fact]
    public async Task Dry_run_validates_everything_without_writing_operational_data()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, Fixtures.Request("demo-base", dryRun: true));

        Assert.False(report.Applied);
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(6, report.Entities.Single(e => e.Entity == "service_points").Created);
        Assert.Equal(0, await db.ServicePoints.CountAsync());
        Assert.Equal("DryRun", (await db.ImportBatches.SingleAsync()).Status);
    }

    [Fact]
    public async Task Reimporting_the_same_files_creates_nothing_new()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        await Import(db, Fixtures.Request("demo-base"));

        var second = await Import(db, Fixtures.Request("demo-base"));

        Assert.True(second.Applied, Describe(second));
        Assert.All(second.Entities, e => Assert.Equal(0, e.Created));
        Assert.Equal(6, await db.ServicePoints.CountAsync());
        Assert.Equal(9, await db.InventoryMovements.CountAsync());
    }

    [Fact]
    public async Task External_dataset_loads_with_its_mapping_profile()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, Fixtures.Request("ejemplo-externo"));

        Assert.True(report.Applied, Describe(report));
        var point = await db.ServicePoints.SingleAsync(p => p.Code == "C-100");
        Assert.Equal(-2.17, point.Position!.Y, 4);
        Assert.Equal("cliente", point.Category);
        Assert.Equal("Guayas", point.Region);
        var consumption = await db.Consumptions.SingleAsync(c => c.ServicePointId == point.Id);
        Assert.Equal(359.614m, consumption.Quantity);
        Assert.Equal(30, consumption.PeriodDays);
        Assert.Equal(new DateOnly(2026, 9, 1), consumption.OccurredOn);
        Assert.Equal("L", (await db.Products.SingleAsync()).BaseUnit);
        Assert.Contains("weight_per_unit", report.Entities.Single(e => e.Entity == "products").Assumptions.Keys);
    }

    [Fact]
    public async Task Errors_reject_the_whole_import_unless_partial_is_allowed()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        const string csv = "code,name,lat,lon\nB-1,Bodega,-0.2,-78.5\nB-2,Bodega mala,-200,-78.5\n";
        ImportRequest Request(bool partial) => new([Csv(ImportEntity.Warehouses, csv)], MappingProfile.Auto(), false, partial, null);

        var rejected = await Import(db, Request(partial: false));
        Assert.False(rejected.Applied);
        Assert.Equal(3, Assert.Single(rejected.Issues).Row);
        Assert.Equal(0, await db.Locations.CountAsync());
        Assert.Equal("Rejected", (await db.ImportBatches.SingleAsync()).Status);

        var partial = await Import(db, Request(partial: true));
        Assert.True(partial.Applied);
        Assert.Equal(1, await db.Locations.CountAsync());
    }

    [Fact]
    public async Task Unknown_references_are_reported_with_row_and_field()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, new ImportRequest(
            [Csv(ImportEntity.Consumptions, "point_code,sku,date,quantity\nNOPE,DES-01,2026-10-05,1\n")],
            MappingProfile.Auto(), true, false, null));

        var issue = Assert.Single(report.Issues, i => i.Severity == IssueSeverity.Error && i.Field == "point_code");
        Assert.Equal("unknown_reference", issue.Code);
        Assert.Equal(2, issue.Row);
        Assert.Equal("point_code", issue.Field);
    }

    [Fact]
    public async Task Codes_match_across_files_regardless_of_case_and_spaces()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, new ImportRequest(
        [
            Csv(ImportEntity.ServicePoints, "code,name\nHOS-001,Hospital\n"),
            Csv(ImportEntity.Products, "sku,name,base_unit\nDES-01,Desinfectante,L\n"),
            Csv(ImportEntity.Consumptions, "point_code,sku,date,quantity\n\" hos-001 \",des-01,2026-10-05,12\n"),
        ], MappingProfile.Auto(), false, false, null));

        Assert.True(report.Applied, Describe(report));
        Assert.Equal(1, await db.Consumptions.CountAsync());
    }

    [Fact]
    public async Task Duplicate_consumption_rows_are_summed_with_a_warning()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var report = await Import(db, new ImportRequest(
        [
            Csv(ImportEntity.ServicePoints, "code,name\nP1,Punto\n"),
            Csv(ImportEntity.Products, "sku,name,base_unit\nS1,Producto,L\n"),
            Csv(ImportEntity.Consumptions, "point_code,sku,date,quantity\nP1,S1,2026-10-05,5\nP1,S1,2026-10-05,7\n"),
        ], MappingProfile.Auto(), false, false, null));

        Assert.True(report.Applied, Describe(report));
        Assert.Equal(12m, (await db.Consumptions.SingleAsync()).Quantity);
        Assert.Contains(report.Issues, i => i.Code == "duplicate" && i.Severity == IssueSeverity.Warning);
    }

    [Fact]
    public async Task Xlsx_numbers_ignore_the_profile_decimal_separator()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Productos");
        sheet.Cell(1, 1).Value = "sku";
        sheet.Cell(1, 2).Value = "name";
        sheet.Cell(1, 3).Value = "base_unit";
        sheet.Cell(1, 4).Value = "weight_per_unit";
        sheet.Cell(2, 1).Value = "DES-01";
        sheet.Cell(2, 2).Value = "Desinfectante";
        sheet.Cell(2, 3).Value = "L";
        sheet.Cell(2, 4).Value = 0.45;
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var profile = MappingProfile.Parse("""{ "name": "coma", "decimalSeparator": "," }""");

        var report = await Import(db, new ImportRequest([new ImportFile(ImportEntity.Products, "productos.xlsx", stream)], profile, false, false, null));

        Assert.True(report.Applied, Describe(report));
        Assert.Equal(0.45m, (await db.Products.SingleAsync()).WeightPerBaseUnit);
    }
}
