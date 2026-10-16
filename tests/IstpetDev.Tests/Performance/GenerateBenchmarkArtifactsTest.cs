using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace IstpetDev.Tests.Performance;

[Trait("Category", "Performance")]
public sealed class GenerateBenchmarkArtifactsTest(ITestOutputHelper output)
{
    [Fact]
    public async Task Run_Full_12_Runs_Benchmark_And_Export_Canonical_Package()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<ApplicationDbContext>();

        var worker = new SimulationWorker(provider, NullLogger<SimulationWorker>.Instance);

        var comparisonId = Guid.NewGuid();
        var seeds = new[] { 42, 101, 2024 };
        var scenarios = new List<(int Seed, string ScenarioCode)>();

        foreach (var s in seeds)
        {
            scenarios.Add((s, "base"));
        }
        scenarios.Add((9999, "adverse"));

        var policies = new[] { ReactivePolicy.Version, FixedCalendarPolicy.Version, IstpetDevPolicy.Version };

        var runs = new List<SimulationRun>();
        foreach (var (seed, scenario) in scenarios)
        {
            foreach (var policy in policies)
            {
                runs.Add(new SimulationRun
                {
                    Id = Guid.NewGuid(),
                    ComparisonId = comparisonId,
                    Seed = seed,
                    ScenarioCode = scenario,
                    PolicyCode = policy,
                    PolicyVersion = policy,
                    DatasetVersion = "national-v1",
                    Status = "Queued",
                    ProgressPercentage = 0,
                    OrganizationId = DemoOrganization.Id,
                    StartedAtUtc = DateTime.UtcNow
                });
            }
        }

        db.SimulationRuns.AddRange(runs);
        await db.SaveChangesAsync();

        var request = new SimulationComparisonRequest(Seeds: seeds, IncludeAdverse: true);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await worker.ExecuteComparisonAsync(comparisonId, request, CancellationToken.None);
        stopwatch.Stop();

        output.WriteLine($"[BENCHMARK] 12 runs completadas en {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F2} s).");

        db.ChangeTracker.Clear();
        var updatedRuns = await db.SimulationRuns.Where(r => r.ComparisonId == comparisonId).ToListAsync();

        Assert.Equal(12, updatedRuns.Count);
        Assert.All(updatedRuns, r =>
        {
            Assert.Equal("Completed", r.Status);
            Assert.Equal(100, r.ProgressPercentage);
            Assert.NotNull(r.CompletedAtUtc);
            Assert.False(string.IsNullOrEmpty(r.CalendarHash));
        });

        var metrics = await db.SimulationMetrics.ToListAsync();
        var metricValues = metrics.Select(m => new MetricValue(
            Code: m.Code,
            DefinitionVersion: "v1",
            Sku: null,
            Numerator: m.Numerator,
            Denominator: m.Denominator,
            Value: m.Value,
            Unit: m.Unit,
            NotApplicableReason: m.NotApplicableReason)).ToList();

        var events = await db.SimulationEvents.Take(500).ToListAsync();
        var eventDtos = events.Select(e => new SimulationEventDto(
            Sequence: e.Sequence,
            OccurredAtUtc: e.OccurredAtUtc,
            Kind: e.Kind,
            RegionCode: e.RegionCode,
            PointCode: e.PointCode,
            Sku: e.Sku,
            Requested: e.Requested,
            Satisfied: e.Satisfied,
            Reason: e.Reason)).ToList();

        var key = new ScenarioKey("national-v1", 42, "base");
        var calendar = ExogenousCalendar.Build(key, [("REG-01", "P01")], [("SKU-01", 10m, "L")], new DateOnly(2026, 10, 12), 90);

        var exportPackage = await ExportBuilder.CreateZipPackageAsync(
            comparisonId: comparisonId,
            calendar: calendar,
            metrics: metricValues,
            events: eventDtos,
            datasetVersion: "national-v1",
            commitHash: "chore-106",
            hostName: Environment.MachineName);

        Assert.NotEmpty(exportPackage.ZipBytes);
        Assert.StartsWith("\"", exportPackage.ETag);

        // Guardar ZIP y JSON en docs/evidence/simulation-benchmark
        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var outputDir = Path.Combine(rootDir, "docs", "evidence", "simulation-benchmark");
        Directory.CreateDirectory(outputDir);

        var zipPath = Path.Combine(outputDir, $"simulation-{comparisonId}.zip");
        await File.WriteAllBytesAsync(zipPath, exportPackage.ZipBytes);
        output.WriteLine($"[EXPORT] Paquete ZIP generado exitosamente en: {zipPath} ({exportPackage.ZipBytes.Length} bytes)");

        // Validar paquete exportado
        var valResult = await ExportBuilder.ValidateZipPackageAsync(exportPackage.ZipBytes);
        Assert.True(valResult.IsValid);
        Assert.Equal(comparisonId, valResult.ComparisonId);

        // Probar ImportComparisonAsync
        var config = new ConfigurationBuilder().Build();
        var simService = new SimulationService(db, worker, config);
        var importResult = await simService.ImportComparisonAsync(exportPackage.ZipBytes, Environment.MachineName, "chore-106");

        Assert.True(importResult.Success, $"Import failed: {importResult.ErrorMessage}");
        Assert.Equal(comparisonId, importResult.ComparisonId);
        Assert.Equal(12, importResult.ImportedRunsCount);

        output.WriteLine($"[IMPORT] Importación reproducible validada con éxito: {importResult.ImportedRunsCount} runs importadas.");

        // Resumen de métricas agregadas por política
        var seedMetrics = new List<SeedPolicyMetric>();
        foreach (var r in updatedRuns)
        {
            var rMetrics = metrics.Where(m => m.SimulationRunId == r.Id);
            foreach (var rm in rMetrics)
            {
                seedMetrics.Add(new SeedPolicyMetric(r.Seed, r.ScenarioCode, r.PolicyCode, rm.Code, rm.Value));
            }
        }

        output.WriteLine("==================================================================");
        output.WriteLine("RESUMEN DE MÉTRICAS COMPARATIVAS DEL SIMULADOR 90 DÍAS (12 RUNS)");
        output.WriteLine("==================================================================");

        var metricCodes = new[] { "km_total", "costo_transporte", "entregas_completadas", "km_por_entrega", "costo_por_entrega" };
        var summaryDict = new Dictionary<string, object>();

        foreach (var policy in policies)
        {
            output.WriteLine($"\n--- Política: {policy} ---");
            var policyMetrics = new Dictionary<string, object>();
            foreach (var code in metricCodes)
            {
                var agg = ComparisonAggregator.AggregateMetric(seedMetrics, policy, code);
                output.WriteLine($"  {code}: Base [Min: {agg.Min}, Mediana: {agg.Median}, Max: {agg.Max}] | Adverso: {agg.AdverseValue}");
                policyMetrics[code] = new { min = agg.Min, median = agg.Median, max = agg.Max, adverse = agg.AdverseValue };
            }
            summaryDict[policy] = policyMetrics;
        }

        var jsonSummaryPath = Path.Combine(outputDir, $"comparison-{comparisonId}.json");
        var summaryJson = JsonSerializer.Serialize(new
        {
            comparisonId,
            datasetVersion = "national-v1",
            completedAtUtc = DateTime.UtcNow,
            elapsedSeconds = stopwatch.Elapsed.TotalSeconds,
            policies = summaryDict
        }, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(jsonSummaryPath, summaryJson);
        output.WriteLine($"\n[METADATA] Resumen JSON guardado en: {jsonSummaryPath}");
    }
}
