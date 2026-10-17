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

namespace IstpetDev.Tests.Unit.Simulation;

[Trait("Category", "Unit")]
public sealed class SimulationMetricsTests
{
    private static (ServiceProvider Provider, ApplicationDbContext Db) CreateServices()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        var provider = services.BuildServiceProvider();
        return (provider, provider.GetRequiredService<ApplicationDbContext>());
    }

    private static async Task<Guid> AddComparisonAsync(ApplicationDbContext db, int seed = 42)
    {
        var comparisonId = Guid.NewGuid();
        foreach (var policy in new[] { ReactivePolicy.Version, FixedCalendarPolicy.Version, IstpetDevPolicy.Version })
        {
            db.SimulationRuns.Add(new SimulationRun
            {
                OrganizationId = DemoOrganization.Id,
                ComparisonId = comparisonId,
                Seed = seed,
                ScenarioCode = "base",
                PolicyCode = policy,
                PolicyVersion = policy,
                DatasetVersion = "test-dataset",
                DatasetHash = "simulation-test-hash",
                Status = "Queued",
                StartedAtUtc = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        return comparisonId;
    }

    [Fact]
    public async Task Worker_Calculates_Stockouts_Unattended_Demand_And_Punctuality_From_Real_Events()
    {
        var (provider, db) = CreateServices();
        await SimulationSeed.SeedAsync(db, pointStock: 0m, warehouseStock: 1_000_000m);
        var worker = new SimulationWorker(provider, NullLogger<SimulationWorker>.Instance);
        var comparisonId = await AddComparisonAsync(db);

        await worker.ExecuteComparisonAsync(
            comparisonId,
            new SimulationComparisonRequest(Seeds: [42], IncludeAdverse: false),
            CancellationToken.None);

        var runStates = await db.SimulationRuns
            .Where(r => r.ComparisonId == comparisonId)
            .Select(r => new { r.PolicyCode, r.Status, r.FailureReason })
            .ToListAsync();
        Assert.All(runStates, r => Assert.True(r.Status == "Completed", $"{r.PolicyCode}: {r.FailureReason}"));

        var metrics = await db.SimulationMetrics
            .Where(m => m.OrganizationId == DemoOrganization.Id)
            .ToListAsync();

        var stockoutMetric = Assert.Single(metrics, m => m.Code == "quiebres_total" && m.PolicyCode == ReactivePolicy.Version);
        Assert.True(stockoutMetric.Value > 0m, "La política reactiva debe tener quiebres reales con stock inicial cero y lead time.");

        var unattended = metrics.Where(m => m.Code == "demanda_no_atendida").ToList();
        Assert.Contains(unattended, m => string.Equals(m.Unit, "L", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(unattended, m => string.Equals(m.Unit, "kg", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(unattended, m => string.Equals(m.Unit, "L", StringComparison.OrdinalIgnoreCase) && m.Value is null);
        Assert.All(unattended, m => Assert.NotNull(m.ProductId));

        var punctuality = Assert.Single(metrics, m => m.Code == "puntualidad" && m.PolicyCode == ReactivePolicy.Version);
        Assert.NotNull(punctuality.Value);
        Assert.True(punctuality.Value!.Value < 100m, "La puntualidad no debe ser 100 % cuando faltó stock.");
        Assert.True(punctuality.Numerator <= punctuality.Denominator);

        Assert.Contains(metrics, m => m.Code == "quiebres_evitados_reactive-v1");
        Assert.Contains(metrics, m => m.Code == "quiebres_nuevos_reactive-v1");
        Assert.Contains(metrics, m => m.Code == "quiebres_diferencia_neta_reactive-v1");
        Assert.Contains(metrics, m => m.Code == "quiebres_evitados_fixed7-v1");
    }

    [Fact]
    public async Task Same_Seed_And_Dataset_Produce_Same_Calendar_And_Metrics()
    {
        var (provider, db) = CreateServices();
        await SimulationSeed.SeedAsync(db, pointStock: 5m, warehouseStock: 1_000_000m);
        var worker = new SimulationWorker(provider, NullLogger<SimulationWorker>.Instance);
        var firstComparison = await AddComparisonAsync(db);
        await worker.ExecuteComparisonAsync(
            firstComparison,
            new SimulationComparisonRequest(Seeds: [42], IncludeAdverse: false),
            CancellationToken.None);

        var firstRuns = await db.SimulationRuns
            .Where(r => r.ComparisonId == firstComparison)
            .ToListAsync();
        var firstMetrics = await db.SimulationMetrics
            .Where(m => firstRuns.Select(r => r.Id).Contains(m.SimulationRunId))
            .ToListAsync();

        var secondComparison = await AddComparisonAsync(db);
        await worker.ExecuteComparisonAsync(
            secondComparison,
            new SimulationComparisonRequest(Seeds: [42], IncludeAdverse: false),
            CancellationToken.None);

        var secondRuns = await db.SimulationRuns
            .Where(r => r.ComparisonId == secondComparison)
            .ToListAsync();
        var secondMetrics = await db.SimulationMetrics
            .Where(m => secondRuns.Select(r => r.Id).Contains(m.SimulationRunId))
            .ToListAsync();

        foreach (var policy in new[] { ReactivePolicy.Version, FixedCalendarPolicy.Version, IstpetDevPolicy.Version })
        {
            var first = firstRuns.Single(r => r.PolicyCode == policy);
            var second = secondRuns.Single(r => r.PolicyCode == policy);
            Assert.Equal(first.CalendarHash, second.CalendarHash);

            var firstPolicyMetrics = firstMetrics
                .Where(m => m.SimulationRunId == first.Id)
                .ToDictionary(m => (m.Code, m.ProductId), m => m.Value);
            var secondPolicyMetrics = secondMetrics
                .Where(m => m.SimulationRunId == second.Id)
                .ToDictionary(m => (m.Code, m.ProductId), m => m.Value);
            Assert.Equal(firstPolicyMetrics.Count, secondPolicyMetrics.Count);
            foreach (var (key, value) in firstPolicyMetrics)
                Assert.Equal(value, secondPolicyMetrics[key]);
        }
    }
}
