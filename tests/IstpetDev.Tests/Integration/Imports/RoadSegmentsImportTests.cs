using System.Text;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Imports;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IstpetDev.Tests.Integration.Imports;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RoadSegmentsImportTests(PostgisContainer postgres)
{
    private static ImportRequest Csv(ImportEntity entity, string rows, bool partial = false, bool dryRun = false) =>
        new([new ImportFile(entity, $"{entity.Key()}.csv", new MemoryStream(Encoding.UTF8.GetBytes(rows)))], MappingProfile.Auto(), dryRun, partial, null);

    private static async Task<ImportReport> Import(ApplicationDbContext db, ImportRequest request)
    {
        try { return await new ImportService(db, new TabularReader()).RunAsync(request); }
        finally { db.ChangeTracker.Clear(); foreach (var file in request.Files) await file.Content.DisposeAsync(); }
    }

    [Fact]
    public void Road_segments_is_the_eighth_entity_and_supports_aliases()
    {
        Assert.Equal(ImportEntity.RoadSegments, CanonicalSchema.Order[7]);
        Assert.True(ImportEntityKeys.TryParse("road_segments", out var e1));
        Assert.Equal(ImportEntity.RoadSegments, e1);
        Assert.True(ImportEntityKeys.TryParse("tramos", out var e2));
        Assert.Equal(ImportEntity.RoadSegments, e2);
        Assert.True(ImportEntityKeys.TryParse("matriz", out var e3));
        Assert.Equal(ImportEntity.RoadSegments, e3);
        Assert.True(ImportEntityKeys.TryParse("distancias", out var e4));
        Assert.Equal(ImportEntity.RoadSegments, e4);
    }

    [Fact]
    public async Task Imports_road_segments_resolves_endpoints_and_upserts_by_coordinates()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        await Import(db, Fixtures.Request("demo-base"));

        var tramosCsv =
            "from_code,to_code,distance_km,duration_min,geometry\n" +
            "BOD-UIO,HOS-001,12.450,25.300,_p~iF~ps|U_ulLnnqC\n" +
            "HOS-001,BOD-UIO,13.100,27.000,another_polyline\n";

        var report = await Import(db, Csv(ImportEntity.RoadSegments, tramosCsv));

        Assert.True(report.Applied, string.Join("\n", report.Issues.Select(i => $"{i.Field}: {i.Message}")));
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(2, await db.RoadMatrixEdges.CountAsync());

        var edge = await db.RoadMatrixEdges.FirstAsync(e => e.DistanceKm == 12.450m);
        Assert.Equal("imported", edge.Source);
        Assert.Equal(25.300m, edge.DurationMinutes);
        Assert.Equal("_p~iF~ps|U_ulLnnqC", edge.EncodedPolyline);

        // Reimport does not duplicate
        var reimportReport = await Import(db, Csv(ImportEntity.RoadSegments, tramosCsv));
        Assert.True(reimportReport.Applied);
        Assert.Equal(2, await db.RoadMatrixEdges.CountAsync());
    }

    [Fact]
    public async Task Ambiguity_requires_kind_and_invalid_endpoint_reports_error()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        await Import(db, Fixtures.Request("demo-base"));

        // Unknown endpoint
        var badCsv = "from_code,to_code,distance_km,duration_min\nUNKNOWN,HOS-001,10,15\n";
        var report = await Import(db, Csv(ImportEntity.RoadSegments, badCsv));
        Assert.False(report.Applied);
        Assert.True(report.HasErrors);
        Assert.Contains(report.Issues, i => i.Field == "from_code" && i.Code == "unknown_reference");
    }
}
