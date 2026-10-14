using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class SimulationExportTests
{
    [Fact]
    public async Task Export_Builds_Zip_With_All_Required_Files_And_Matching_Hashes()
    {
        var comparisonId = Guid.NewGuid();
        var key = new ScenarioKey("national-v1", 11, "base");
        var calendar = ExogenousCalendar.Build(
            key,
            points: [("REG-01", "P01")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 3);

        var metrics = new List<MetricValue>
        {
            new("saving_km", "metric-v1", null, 25m, 100m, 25m, "km", null),
            new("puntualidad", "metric-v1", null, 95m, 100m, 95m, "%", null)
        };

        var events = new List<SimulationEventDto>
        {
            new(1, new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc), "demand_satisfied", "REG-01", "P01", "SKU-01", 10m, 10m, null)
        };

        var exportPackage = await ExportBuilder.CreateZipPackageAsync(
            comparisonId: comparisonId,
            calendar: calendar,
            metrics: metrics,
            events: events,
            datasetVersion: "national-v1",
            commitHash: "commit123",
            hostName: "dedicated-host-1");

        Assert.NotEmpty(exportPackage.ZipBytes);
        Assert.NotEmpty(exportPackage.ETag);

        // Abrir ZIP y verificar los 5 archivos requeridos
        using var zipStream = new MemoryStream(exportPackage.ZipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        string[] requiredFiles = ["manifest.json", "metrics.json", "resumen.csv", "eventos.csv", "calendar.json"];
        foreach (var req in requiredFiles)
        {
            var entry = archive.GetEntry(req);
            Assert.NotNull(entry);
        }

        // Validar integridad contra manifest.json
        var validation = await ExportBuilder.ValidateZipPackageAsync(exportPackage.ZipBytes);
        Assert.True(validation.IsValid);
        Assert.Equal(comparisonId, validation.ComparisonId);
    }

    [Fact]
    public async Task Altered_Zip_File_Fails_Validation_Without_Writing_Anything()
    {
        var comparisonId = Guid.NewGuid();
        var key = new ScenarioKey("national-v1", 11, "base");
        var calendar = ExogenousCalendar.Build(
            key,
            points: [("REG-01", "P01")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 2);

        var exportPackage = await ExportBuilder.CreateZipPackageAsync(
            comparisonId: comparisonId,
            calendar: calendar,
            metrics: [],
            events: [],
            datasetVersion: "national-v1",
            commitHash: "c1",
            hostName: "h1");

        // Alterar 1 byte en metrics.json creando un ZIP modificado
        byte[] tamperedZipBytes;
        using (var sourceStream = new MemoryStream(exportPackage.ZipBytes))
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
                        var tampered = Encoding.UTF8.GetBytes("[{\"tampered\": true}]");
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

        var validation = await ExportBuilder.ValidateZipPackageAsync(tamperedZipBytes);
        Assert.False(validation.IsValid);
        Assert.Contains("Hash mismatch for metrics.json", validation.ErrorMessage);
    }
}
