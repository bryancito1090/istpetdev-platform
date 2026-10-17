using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
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
    private static (ServiceProvider Provider, ApplicationDbContext Db) CreateTestServices(Action<LogisticsOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

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

    [Fact]
    public async Task Worker_Cost_Depends_On_LogisticsOptions_And_Vehicle_CostPerKm()
    {
        // 1. Corrida con opciones por defecto (DefaultCostPerKm = 0.45, CostPerHour = 6.00)
        var (providerDef, dbDef) = CreateTestServices();
        var workerDef = new SimulationWorker(providerDef, NullLogger<SimulationWorker>.Instance);

        var comparisonIdDef = Guid.NewGuid();
        var runDef = new SimulationRun
        {
            Id = Guid.NewGuid(),
            ComparisonId = comparisonIdDef,
            Seed = 42,
            ScenarioCode = "base",
            PolicyCode = "reactive-v1",
            PolicyVersion = "reactive-v1",
            DatasetVersion = "national-v1",
            Status = "Queued",
            ProgressPercentage = 0,
            OrganizationId = DemoOrganization.Id,
            StartedAtUtc = DateTime.UtcNow
        };
        dbDef.SimulationRuns.Add(runDef);
        await dbDef.SaveChangesAsync();

        var request = new SimulationComparisonRequest(Seeds: [42], IncludeAdverse: false);
        await workerDef.ExecuteComparisonAsync(comparisonIdDef, request, CancellationToken.None);

        var costDef = (await dbDef.SimulationMetrics
            .FirstAsync(m => m.SimulationRunId == runDef.Id && m.Code == "costo_transporte")).Value!.Value;
        var kmDef = (await dbDef.SimulationMetrics
            .FirstAsync(m => m.SimulationRunId == runDef.Id && m.Code == "km_total")).Value!.Value;

        // 2. Corrida con CostPerHour y DefaultCostPerKm incrementados en LogisticsOptions
        var (providerHigh, dbHigh) = CreateTestServices(opt =>
        {
            opt.DefaultCostPerKm = 1.50m;
            opt.CostPerHour = 20.00m;
        });
        var workerHigh = new SimulationWorker(providerHigh, NullLogger<SimulationWorker>.Instance);

        var comparisonIdHigh = Guid.NewGuid();
        var runHigh = new SimulationRun
        {
            Id = Guid.NewGuid(),
            ComparisonId = comparisonIdHigh,
            Seed = 42,
            ScenarioCode = "base",
            PolicyCode = "reactive-v1",
            PolicyVersion = "reactive-v1",
            DatasetVersion = "national-v1",
            Status = "Queued",
            ProgressPercentage = 0,
            OrganizationId = DemoOrganization.Id,
            StartedAtUtc = DateTime.UtcNow
        };
        dbHigh.SimulationRuns.Add(runHigh);
        await dbHigh.SaveChangesAsync();

        await workerHigh.ExecuteComparisonAsync(comparisonIdHigh, request, CancellationToken.None);

        var costHigh = (await dbHigh.SimulationMetrics
            .FirstAsync(m => m.SimulationRunId == runHigh.Id && m.Code == "costo_transporte")).Value!.Value;
        var kmHigh = (await dbHigh.SimulationMetrics
            .FirstAsync(m => m.SimulationRunId == runHigh.Id && m.Code == "km_total")).Value!.Value;

        // Los km son geométricos y deterministas (idénticos para la misma semilla)
        Assert.Equal(kmDef, kmHigh);
        // El costo de transporte es estrictamente mayor debido a los parámetros de costo
        Assert.True(costHigh > costDef, $"costHigh ({costHigh}) debe ser mayor que costDef ({costDef})");

        // 3. Corrida con tarifa de vehículo activo en db.Vehicles que reemplaza DefaultCostPerKm
        var (providerVeh, dbVeh) = CreateTestServices();
        dbVeh.Vehicles.Add(new Vehicle
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "VEH-EXPENSIVE",
            DepotLocationId = Guid.NewGuid(),
            MaxWeight = 1000m,
            CostPerKm = 5.00m,
            IsActive = true
        });
        await dbVeh.SaveChangesAsync();

        var workerVeh = new SimulationWorker(providerVeh, NullLogger<SimulationWorker>.Instance);
        var comparisonIdVeh = Guid.NewGuid();
        var runVeh = new SimulationRun
        {
            Id = Guid.NewGuid(),
            ComparisonId = comparisonIdVeh,
            Seed = 42,
            ScenarioCode = "base",
            PolicyCode = "reactive-v1",
            PolicyVersion = "reactive-v1",
            DatasetVersion = "national-v1",
            Status = "Queued",
            ProgressPercentage = 0,
            OrganizationId = DemoOrganization.Id,
            StartedAtUtc = DateTime.UtcNow
        };
        dbVeh.SimulationRuns.Add(runVeh);
        await dbVeh.SaveChangesAsync();

        await workerVeh.ExecuteComparisonAsync(comparisonIdVeh, request, CancellationToken.None);

        var costVeh = (await dbVeh.SimulationMetrics
            .FirstAsync(m => m.SimulationRunId == runVeh.Id && m.Code == "costo_transporte")).Value!.Value;

        Assert.True(costVeh > costDef, $"costVeh con tarifa $5/km ({costVeh}) debe ser mayor que costDef ({costDef})");
    }

    [Fact]
    public async Task Worker_Applies_CustomDisruptions_And_Increases_Transport_Cost_And_Registers_Disruption_Event()
    {
        var date = new DateOnly(2026, 10, 12);
        var customDisruptions = new List<DisruptionEvent>();
        for (int d = 0; d < 14; d++)
        {
            var dt = date.AddDays(d);
            customDisruptions.Add(new(
                OccurredAtUtc: dt.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc),
                KnownAtUtc: dt.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc),
                RegionCode: "REG-ALL",
                Kind: "road_delay",
                SubjectCode: "ALL",
                Factor: 2.0m));
        }

        // 1. Corrida base sin disrupciones
        var (providerBase, dbBase) = CreateTestServices();
        var workerBase = new SimulationWorker(providerBase, NullLogger<SimulationWorker>.Instance);
        var compIdBase = Guid.NewGuid();
        var runBase = new SimulationRun
        {
            Id = Guid.NewGuid(),
            ComparisonId = compIdBase,
            Seed = 42,
            ScenarioCode = "base",
            PolicyCode = "reactive-v1",
            PolicyVersion = "reactive-v1",
            DatasetVersion = "national-v1",
            Status = "Queued",
            ProgressPercentage = 0,
            OrganizationId = DemoOrganization.Id,
            StartedAtUtc = DateTime.UtcNow
        };
        dbBase.SimulationRuns.Add(runBase);
        await dbBase.SaveChangesAsync();

        await workerBase.ExecuteComparisonAsync(compIdBase, new SimulationComparisonRequest(Seeds: [42], IncludeAdverse: false), CancellationToken.None);
        var costBase = (await dbBase.SimulationMetrics
            .FirstAsync(m => m.SimulationRunId == runBase.Id && m.Code == "costo_transporte")).Value!.Value;

        // 2. Corrida con incidente de demora Factor = 2.0m
        var (providerDisr, dbDisr) = CreateTestServices();
        var workerDisr = new SimulationWorker(providerDisr, NullLogger<SimulationWorker>.Instance);
        var compIdDisr = Guid.NewGuid();
        var runDisr = new SimulationRun
        {
            Id = Guid.NewGuid(),
            ComparisonId = compIdDisr,
            Seed = 42,
            ScenarioCode = "base",
            PolicyCode = "reactive-v1",
            PolicyVersion = "reactive-v1",
            DatasetVersion = "national-v1",
            Status = "Queued",
            ProgressPercentage = 0,
            OrganizationId = DemoOrganization.Id,
            StartedAtUtc = DateTime.UtcNow
        };
        dbDisr.SimulationRuns.Add(runDisr);
        await dbDisr.SaveChangesAsync();

        var disrRequest = new SimulationComparisonRequest(Seeds: [42], IncludeAdverse: false, CustomDisruptions: customDisruptions);
        await workerDisr.ExecuteComparisonAsync(compIdDisr, disrRequest, CancellationToken.None);

        var costDisr = (await dbDisr.SimulationMetrics
            .FirstAsync(m => m.SimulationRunId == runDisr.Id && m.Code == "costo_transporte")).Value!.Value;

        // La demora vial incrementa el tiempo de conducción y por lo tanto el costo de transporte
        Assert.True(costDisr > costBase, $"costDisr ({costDisr}) debe ser mayor que costBase ({costBase})");

        // Verifica que se haya registrado el evento "disruption_applied"
        var events = await dbDisr.SimulationEvents
            .Where(e => e.SimulationRunId == runDisr.Id)
            .ToListAsync();

        Assert.Contains(events, e => e.Kind == "disruption_applied" && e.Reason == "road_delay" && e.Requested == 2.0m);
    }

    [Fact]
    public async Task Worker_Eliminates_Synthetic_Constants_And_All_Three_Policies_Share_Cost_Calculator()
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

        await worker.ExecuteComparisonAsync(comparisonId, new SimulationComparisonRequest(Seeds: [42], IncludeAdverse: false), CancellationToken.None);

        foreach (var run in runs)
        {
            var km = (await db.SimulationMetrics
                .FirstAsync(m => m.SimulationRunId == run.Id && m.Code == "km_total")).Value!.Value;
            var deliveries = (await db.SimulationMetrics
                .FirstAsync(m => m.SimulationRunId == run.Id && m.Code == "entregas_completadas")).Value!.Value;
            var cost = (await db.SimulationMetrics
                .FirstAsync(m => m.SimulationRunId == run.Id && m.Code == "costo_transporte")).Value!.Value;

            Assert.True(km > 0m);
            Assert.True(deliveries > 0m);
            Assert.True(cost > 0m);

            // Las constantes sustitutas 'entregas * 12.5m' y 'km * 1.75m' ya no existen
            var syntheticKm = deliveries * 12.5m;
            var syntheticCost = km * 1.75m;

            Assert.NotEqual(syntheticKm, km);
            Assert.NotEqual(syntheticCost, cost);
        }
    }
}
