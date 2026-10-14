using System.IO.Compression;
using System.Text;
using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class SimulationImportTests
{
    [Fact]
    public async Task Valid_Zip_Package_Passes_Validation_And_Extracts_Comparison_Metadata()
    {
        var comparisonId = Guid.NewGuid();
        var key = new ScenarioKey("national-v1", 42, "base");
        var calendar = ExogenousCalendar.Build(
            key,
            points: [("REG-01", "P01")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 2);

        var metrics = new List<MetricValue>
        {
            new("saving_km", "metric-v1", null, 15m, 80m, 15m, "km", null)
        };

        var package = await ExportBuilder.CreateZipPackageAsync(
            comparisonId: comparisonId,
            calendar: calendar,
            metrics: metrics,
            events: [],
            datasetVersion: "national-v1",
            commitHash: "abc1234",
            hostName: "benchmarking-box");

        var result = await ExportBuilder.ValidateZipPackageAsync(package.ZipBytes);

        Assert.True(result.IsValid);
        Assert.Equal(comparisonId, result.ComparisonId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task Corrupted_Zip_Entry_Fails_Validation()
    {
        var comparisonId = Guid.NewGuid();
        var key = new ScenarioKey("national-v1", 42, "base");
        var calendar = ExogenousCalendar.Build(
            key,
            points: [("REG-01", "P01")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 2);

        var metrics = new List<MetricValue>
        {
            new("saving_km", "metric-v1", null, 15m, 80m, 15m, "km", null)
        };

        var package = await ExportBuilder.CreateZipPackageAsync(
            comparisonId: comparisonId,
            calendar: calendar,
            metrics: metrics,
            events: [],
            datasetVersion: "national-v1",
            commitHash: "abc",
            hostName: "host");

        // Alterar 1 byte en metrics.json como exige task.md
        byte[] tamperedZipBytes;
        using (var sourceStream = new MemoryStream(package.ZipBytes))
        using (var sourceArchive = new ZipArchive(sourceStream, ZipArchiveMode.Read))
        using (var destStream = new MemoryStream())
        {
            using (var destArchive = new ZipArchive(destStream, ZipArchiveMode.Create, true))
            {
                foreach (var entry in sourceArchive.Entries)
                {
                    var newEntry = destArchive.CreateEntry(entry.FullName);
                    using var s = entry.Open();
                    using var d = newEntry.Open();

                    if (entry.FullName == "metrics.json")
                    {
                        var tampered = Encoding.UTF8.GetBytes("[{\"tampered\": 1}]");
                        d.Write(tampered);
                    }
                    else
                    {
                        s.CopyTo(d);
                    }
                }
            }
            tamperedZipBytes = destStream.ToArray();
        }

        var result = await ExportBuilder.ValidateZipPackageAsync(tamperedZipBytes);
        Assert.False(result.IsValid);
        Assert.Contains("Hash mismatch for metrics.json", result.ErrorMessage);
    }
}
