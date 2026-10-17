using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Infrastructure.Simulation;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IstpetDev.Tests.E2E;

[Trait("Category", "Unit")]
public sealed class SimulationComparisonJourneyTests
{
    private static (ServiceProvider Provider, ApplicationDbContext Db) CreateServices()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        return (provider, db);
    }

    [Fact]
    public async Task E2E_FullJourney_SimulationComparison_Deterministic_And_Exportable()
    {
        var (provider, db) = CreateServices();
        await SimulationSeed.SeedAsync(db);
        var worker = new SimulationWorker(provider, NullLogger<SimulationWorker>.Instance);

        var comparisonId = Guid.NewGuid();
        var seeds = new[] { 42, 101, 2024 };

        var runs = new List<SimulationRun>();
        foreach (var seed in seeds)
        {
            foreach (var policy in new[] { "reactive-v1", "fixed7-v1", "istpetdev-v1" })
            {
                runs.Add(new SimulationRun
                {
                    Id = Guid.NewGuid(),
                    ComparisonId = comparisonId,
                    Seed = seed,
                    ScenarioCode = "base",
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

        // Add 1 adverse run for each policy
        foreach (var policy in new[] { "reactive-v1", "fixed7-v1", "istpetdev-v1" })
        {
            runs.Add(new SimulationRun
            {
                Id = Guid.NewGuid(),
                ComparisonId = comparisonId,
                Seed = 9999,
                ScenarioCode = "adverse",
                PolicyCode = policy,
                PolicyVersion = policy,
                DatasetVersion = "national-v1",
                Status = "Queued",
                ProgressPercentage = 0,
                OrganizationId = DemoOrganization.Id,
                StartedAtUtc = DateTime.UtcNow
            });
        }

        Assert.Equal(12, runs.Count);
        db.SimulationRuns.AddRange(runs);
        await db.SaveChangesAsync();

        // 1. Ejecutar la corrida a través del worker
        var request = new SimulationComparisonRequest(Seeds: seeds, IncludeAdverse: true);
        await worker.ExecuteComparisonAsync(comparisonId, request, CancellationToken.None);

        db.ChangeTracker.Clear();
        var completedRuns = await db.SimulationRuns.Where(r => r.ComparisonId == comparisonId).ToListAsync();

        Assert.Equal(12, completedRuns.Count);
        Assert.All(completedRuns, r =>
        {
            Assert.Equal("Completed", r.Status);
            Assert.Equal(100, r.ProgressPercentage);
            Assert.False(string.IsNullOrEmpty(r.CalendarHash));
        });

        // 2. Determinismo: Repetir el calendario para la semilla 42 y verificar que el hash coincide exactamente
        var cal1 = ExogenousCalendar.Build(new ScenarioKey("national-v1", 42, "base"), [("REG-01", "P01")], [("SKU-01", 10m, "L")], new DateOnly(2026, 10, 12), 90);
        var cal2 = ExogenousCalendar.Build(new ScenarioKey("national-v1", 42, "base"), [("REG-01", "P01")], [("SKU-01", 10m, "L")], new DateOnly(2026, 10, 12), 90);
        Assert.Equal(cal1.Sha256, cal2.Sha256);

        // 3. Exportación y Validación de ZIP
        var metrics = await db.SimulationMetrics.ToListAsync();
        var metricValues = metrics.Select(m => new MetricValue(m.Code, "v1", null, m.Numerator, m.Denominator, m.Value, m.Unit, m.NotApplicableReason)).ToList();

        var events = await db.SimulationEvents.Take(100).ToListAsync();
        var eventDtos = events.Select(e => new SimulationEventDto(e.Sequence, e.OccurredAtUtc, e.Kind, e.RegionCode, e.PointCode, e.Sku, e.Requested, e.Satisfied, e.Reason)).ToList();

        var pkg = await ExportBuilder.CreateZipPackageAsync(
            comparisonId,
            cal1,
            metricValues,
            eventDtos,
            "national-v1",
            "test-commit",
            "test-host");

        Assert.NotEmpty(pkg.ZipBytes);
        Assert.StartsWith("\"", pkg.ETag);

        var valResult = await ExportBuilder.ValidateZipPackageAsync(pkg.ZipBytes);
        Assert.True(valResult.IsValid);
        Assert.Equal(comparisonId, valResult.ComparisonId);

        // 4. Agregación sin mezclar el escenario adverso en los rangos base
        var seedMetrics = new List<SeedPolicyMetric>();
        foreach (var r in completedRuns)
        {
            var rM = metrics.Where(m => m.SimulationRunId == r.Id && m.Code == "km_total").FirstOrDefault();
            if (rM != null)
            {
                seedMetrics.Add(new SeedPolicyMetric(r.Seed, r.ScenarioCode, r.PolicyCode, "km_total", rM.Value));
            }
        }

        var istpetAgg = ComparisonAggregator.AggregateMetric(seedMetrics, "istpetdev-v1", "km_total");
        Assert.NotNull(istpetAgg.Min);
        Assert.NotNull(istpetAgg.Median);
        Assert.NotNull(istpetAgg.Max);
        Assert.NotNull(istpetAgg.AdverseValue);
    }
}
