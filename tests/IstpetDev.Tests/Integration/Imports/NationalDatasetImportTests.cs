using System.Text.Json;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Imports;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IstpetDev.Tests.Integration.Imports;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class NationalDatasetImportTests(PostgisContainer postgres)
{
    private static string GetDatasetFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "IstpetDev.sln")))
            directory = directory.Parent;
        if (directory is null)
            throw new InvalidOperationException("No se encontró IstpetDev.sln");
        return Path.Combine(directory.FullName, "docs", "dataset", "test", "national-v1-seed-11");
    }

    private static ImportRequest BuildRequest(string folder, bool dryRun = false)
    {
        var profileJson = File.ReadAllText(Path.Combine(folder, "mapping.json"));
        var profile = MappingProfile.Parse(profileJson);
        var files = new List<ImportFile>();
        foreach (var (key, mapping) in profile.Files)
        {
            if (ImportEntityKeys.TryParse(key, out var entity))
            {
                var filePath = Path.Combine(folder, mapping.File!);
                if (File.Exists(filePath))
                {
                    files.Add(new ImportFile(entity, mapping.File!, File.OpenRead(filePath)));
                }
            }
        }
        return new ImportRequest(files, profile, dryRun, false, null);
    }

    private static async Task<ImportReport> Import(ApplicationDbContext db, ImportRequest request)
    {
        try
        {
            return await new ImportService(db, new TabularReader()).RunAsync(request);
        }
        finally
        {
            db.ChangeTracker.Clear();
            foreach (var file in request.Files) await file.Content.DisposeAsync();
        }
    }

    [Fact]
    public async Task National_v1_seed_11_dry_run_and_apply_pass_without_errors()
    {
        var folder = GetDatasetFolder();
        Assert.True(Directory.Exists(folder), $"Directory does not exist: {folder}");

        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        // 1. Dry-run
        var dryRequest = BuildRequest(folder, dryRun: true);
        var dryReport = await Import(db, dryRequest);

        Assert.False(dryReport.Applied);
        Assert.Equal(0, dryReport.ErrorCount);

        // 2. Real Apply
        var applyRequest = BuildRequest(folder, dryRun: false);
        var report = await Import(db, applyRequest);

        var issues = string.Join("\n", report.Issues.Select(i => $"{i.Severity} {i.Entity}:{i.Row}:{i.Field} {i.Message}"));
        Assert.True(report.Applied, $"Import failed: {issues}");
        Assert.Equal(0, report.ErrorCount);

        // Verify entity counts in DB
        Assert.Equal(6, await db.Locations.CountAsync(l => l.Kind == LocationKinds.Warehouse));
        Assert.Equal(80, await db.ServicePoints.CountAsync());
        Assert.Equal(12, await db.Vehicles.CountAsync());
        Assert.Equal(2, await db.Products.CountAsync());
        Assert.Equal(2, await db.Lots.CountAsync());
        Assert.Equal(172, await db.StockBalances.CountAsync());
        Assert.Equal(2240, await db.Consumptions.CountAsync());
        Assert.Equal(1148, await db.RoadMatrixEdges.CountAsync());
    }
}
