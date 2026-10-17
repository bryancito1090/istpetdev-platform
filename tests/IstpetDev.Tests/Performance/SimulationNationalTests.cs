using System.Diagnostics;
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

namespace IstpetDev.Tests.Performance;

[Trait("Category", "Performance")]
public sealed class SimulationNationalTests
{
    [Fact]
    public async Task National_Run_12_Runs_Finishes_Well_Under_60_Minutes()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        await SimulationSeed.SeedAsync(db, pointCount: 6, pointStock: 100m);

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

        Assert.Equal(12, runs.Count);

        db.SimulationRuns.AddRange(runs);
        await db.SaveChangesAsync();

        var request = new SimulationComparisonRequest(Seeds: seeds, IncludeAdverse: true);

        var stopwatch = Stopwatch.StartNew();
        await worker.ExecuteComparisonAsync(comparisonId, request, CancellationToken.None);
        stopwatch.Stop();

        // CE-07: Tiempo total <= 60 minutos (3600 segundos)
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMinutes(60), $"Elapsed {stopwatch.Elapsed} exceeded 60 minutes limit");

        db.ChangeTracker.Clear();
        var updatedRuns = await db.SimulationRuns.Where(r => r.ComparisonId == comparisonId).ToListAsync();

        Assert.Equal(12, updatedRuns.Count);
        Assert.All(updatedRuns, r =>
        {
            Assert.Equal("Completed", r.Status);
            Assert.Equal(100, r.ProgressPercentage);
            Assert.NotNull(r.CompletedAtUtc);
        });

        var metricsCount = await db.SimulationMetrics.CountAsync();
        Assert.True(metricsCount >= 12 * 5, $"Expected at least 60 metrics, found {metricsCount}");

        var eventsCount = await db.SimulationEvents.CountAsync();
        Assert.True(eventsCount > 0, $"Expected recorded events, found {eventsCount}");
    }
}
