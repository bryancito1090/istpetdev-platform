using System.Security.Cryptography;
using System.Text.Json;
using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class ScenarioGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public ScenarioGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "istpetdev_sim_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private static string ResolveScenarioPath()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, "data", "fixtures", "simulation-national", "scenario.json");
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(current);
            if (parent is null) break;
            current = parent.FullName;
        }
        throw new FileNotFoundException("scenario.json no encontrado");
    }

    [Fact]
    public async Task Same_Seed_Generates_Identical_Bytes_And_Sha256_In_Two_Executions()
    {
        var run1Dir = Path.Combine(_tempDir, "run1");
        var run2Dir = Path.Combine(_tempDir, "run2");
        var scenarioPath = ResolveScenarioPath();

        var gen1 = await ScenarioGenerator.GenerateFromScenarioFileAsync(scenarioPath, run1Dir, seed: 11);
        var gen2 = await ScenarioGenerator.GenerateFromScenarioFileAsync(scenarioPath, run2Dir, seed: 11);

        Assert.Equal(gen1.ManifestSha256, gen2.ManifestSha256);
        Assert.Equal(gen1.CalendarSha256, gen2.CalendarSha256);

        // Comprobar que los bytes de cada CSV son exactamente idénticos
        string[] files =
        [
            "warehouses.csv",
            "service_points.csv",
            "products.csv",
            "vehicles.csv",
            "lots.csv",
            "stock.csv",
            "consumptions.csv",
            "road_segments.csv"
        ];

        foreach (var file in files)
        {
            var bytes1 = await File.ReadAllBytesAsync(Path.Combine(run1Dir, file));
            var bytes2 = await File.ReadAllBytesAsync(Path.Combine(run2Dir, file));

            Assert.Equal(bytes1, bytes2);
        }
    }

    [Fact]
    public async Task Different_Seed_Changes_Calendar_Hash_Without_Changing_Entity_Ids()
    {
        var run1Dir = Path.Combine(_tempDir, "seed11");
        var run2Dir = Path.Combine(_tempDir, "seed12");
        var scenarioPath = ResolveScenarioPath();

        var gen1 = await ScenarioGenerator.GenerateFromScenarioFileAsync(scenarioPath, run1Dir, seed: 11);
        var gen2 = await ScenarioGenerator.GenerateFromScenarioFileAsync(scenarioPath, run2Dir, seed: 12);

        // Entidades canónicas idénticas
        var pts1 = await File.ReadAllTextAsync(Path.Combine(run1Dir, "service_points.csv"));
        var pts2 = await File.ReadAllTextAsync(Path.Combine(run2Dir, "service_points.csv"));
        Assert.Equal(pts1, pts2);

        var veh1 = await File.ReadAllTextAsync(Path.Combine(run1Dir, "vehicles.csv"));
        var veh2 = await File.ReadAllTextAsync(Path.Combine(run2Dir, "vehicles.csv"));
        Assert.Equal(veh1, veh2);

        // Calendario y hash cambian
        Assert.NotEqual(gen1.CalendarSha256, gen2.CalendarSha256);
    }

    [Fact]
    public async Task Small_Fixture_N6_D3_Two_Skus_Generates_36_Demand_Keys_And_No_Future_Consumptions()
    {
        var outputDir = Path.Combine(_tempDir, "small");
        var startDate = new DateOnly(2026, 10, 12);

        var result = await ScenarioGenerator.GenerateSyntheticAsync(
            outputDirectory: outputDir,
            datasetVersion: "national-v1",
            seed: 11,
            pointCount: 6,
            days: 3,
            skuCount: 2,
            startDate: startDate);

        // 6 puntos * 3 días * 2 SKUs = 36 claves de demanda
        Assert.Equal(36, result.Calendar.Demand.Count);

        // consumptions.csv solo contiene historial previo, ninguna fila >= startDate
        var consumptionsPath = Path.Combine(outputDir, "consumptions.csv");
        Assert.True(File.Exists(consumptionsPath));

        var lines = await File.ReadAllLinesAsync(consumptionsPath);
        Assert.True(lines.Length > 1); // Encabezado + filas históricas

        foreach (var line in lines.Skip(1))
        {
            var parts = line.Split(',');
            var dateStr = parts[2]; // date column
            var date = DateOnly.Parse(dateStr);
            Assert.True(date < startDate, $"La fila de consumo {dateStr} no debe ser futura (startDate: {startDate})");
        }
    }
}
