using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

[Trait("Category", "Unit")]
public sealed class SimulationWorkerTests
{
    private static (ServiceProvider Provider, ApplicationDbContext Db) CreateTestServices()
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
    public async Task Worker_Executes_Runs_And_Sets_Completed_With_Metrics()
    {
        var (provider, db) = CreateTestServices();
        var worker = new SimulationWorker(provider, NullLogger<SimulationWorker>.Instance);

        var comparisonId = Guid.NewGuid();
        var runs = new List<SimulationRun>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ComparisonId = comparisonId,
                Seed = 42,
                ScenarioCode = "base",
                PolicyCode = "reactive-v1",
                PolicyVersion = "reactive-v1",
                DatasetVersion = "national-v1",
                Status = "Queued",
                ProgressPercentage = 0,
                OrganizationId = DemoOrganization.Id,
                StartedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                ComparisonId = comparisonId,
                Seed = 42,
                ScenarioCode = "base",
                PolicyCode = "fixed7-v1",
                PolicyVersion = "fixed7-v1",
                DatasetVersion = "national-v1",
                Status = "Queued",
                ProgressPercentage = 0,
                OrganizationId = DemoOrganization.Id,
                StartedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                ComparisonId = comparisonId,
                Seed = 42,
                ScenarioCode = "base",
                PolicyCode = "istpetdev-v1",
                PolicyVersion = "istpetdev-v1",
                DatasetVersion = "national-v1",
                Status = "Queued",
                ProgressPercentage = 0,
                OrganizationId = DemoOrganization.Id,
                StartedAtUtc = DateTime.UtcNow
            }
        };

        db.SimulationRuns.AddRange(runs);
        await db.SaveChangesAsync();

        var request = new SimulationComparisonRequest(Seeds: [42], IncludeAdverse: false);
        await worker.ExecuteComparisonAsync(comparisonId, request, CancellationToken.None);

        db.ChangeTracker.Clear();
        var updatedRuns = await db.SimulationRuns
            .Where(r => r.ComparisonId == comparisonId)
            .ToListAsync();

        Assert.All(updatedRuns, r =>
        {
            Assert.Equal("Completed", r.Status);
            Assert.Equal(100, r.ProgressPercentage);
            Assert.False(string.IsNullOrEmpty(r.CalendarHash));
            Assert.NotNull(r.CompletedAtUtc);
        });

        var metrics = await db.SimulationMetrics.ToListAsync();
        Assert.NotEmpty(metrics);
        Assert.Contains(metrics, m => m.Code == "km_total");
        Assert.Contains(metrics, m => m.Code == "costo_transporte");
        Assert.Contains(metrics, m => m.Code == "entregas_completadas");

        var events = await db.SimulationEvents.ToListAsync();
        Assert.NotEmpty(events);
    }
}
