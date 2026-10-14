using System.Threading.Channels;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IstpetDev.Infrastructure.Simulation;

public sealed class SimulationWorker : BackgroundService, ISimulationWorker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SimulationWorker> _logger;
    private readonly Channel<(Guid ComparisonId, SimulationComparisonRequest Request)> _channel;
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    public SimulationWorker(IServiceProvider serviceProvider, ILogger<SimulationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateUnbounded<(Guid, SimulationComparisonRequest)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public Task EnqueueComparisonAsync(Guid comparisonId, SimulationComparisonRequest request, CancellationToken ct = default)
    {
        _channel.Writer.TryWrite((comparisonId, request));
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Al iniciar, limpiar cualquier corrida que hubiera quedado en "Running" por un reinicio abrupto
        try
        {
            using var initScope = _serviceProvider.CreateScope();
            var db = initScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var interruptedRuns = await db.SimulationRuns
                .Where(r => r.Status == "Running")
                .ToListAsync(stoppingToken);

            if (interruptedRuns.Count > 0)
            {
                foreach (var r in interruptedRuns)
                {
                    r.Status = "Interrupted";
                    r.FailureReason = "Worker restarted while execution was in progress";
                }
                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Marked {Count} previously running runs as Interrupted.", interruptedRuns.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean interrupted runs on worker startup.");
        }

        // Bucle consumidor de comparaciones
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (comparisonId, request) = await _channel.Reader.ReadAsync(stoppingToken);
                await ExecuteComparisonAsync(comparisonId, request, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued comparison in simulation worker.");
            }
        }
    }

    public async Task ExecuteComparisonAsync(Guid comparisonId, SimulationComparisonRequest request, CancellationToken ct = default)
    {
        await _executionLock.WaitAsync(ct);
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var runs = await db.SimulationRuns
                .Where(r => r.ComparisonId == comparisonId)
                .OrderBy(r => r.Seed)
                .ThenBy(r => r.PolicyCode)
                .ToListAsync(ct);

            if (runs.Count == 0)
            {
                _logger.LogWarning("Comparison {ComparisonId} has no runs in database.", comparisonId);
                return;
            }

            var basePoints = new List<(string Region, string Code)>
            {
                ("REG-01", "P01"), ("REG-01", "P02"), ("REG-01", "P03"), ("REG-01", "P04"),
                ("REG-02", "P05"), ("REG-02", "P06"), ("REG-02", "P07"), ("REG-02", "P08"),
                ("REG-03", "P09"), ("REG-03", "P10"), ("REG-03", "P11"), ("REG-03", "P12"),
                ("REG-04", "P13"), ("REG-04", "P14"), ("REG-04", "P15"), ("REG-04", "P16"),
                ("REG-05", "P17"), ("REG-05", "P18"), ("REG-05", "P19"), ("REG-05", "P20"),
                ("REG-06", "P21"), ("REG-06", "P22"), ("REG-06", "P23"), ("REG-06", "P24")
            };
            var products = new List<(string Sku, decimal BaseDaily, string Unit)>
            {
                ("SKU-01", 10m, "L"),
                ("SKU-02", 5m, "kg")
            };

            var startDate = new DateOnly(2026, 10, 12);
            int horizonDays = 90;

            var reactivePolicy = new ReactivePolicy();
            var fixedPolicy = new FixedCalendarPolicy();

            for (int rIdx = 0; rIdx < runs.Count; rIdx++)
            {
                var run = runs[rIdx];
                run.Status = "Running";
                run.StartedAtUtc = DateTime.UtcNow;
                run.ProgressPercentage = 10;
                await db.SaveChangesAsync(ct);

                try
                {
                    var scenarioKey = new ScenarioKey(run.DatasetVersion, run.Seed, run.ScenarioCode);
                    var calendar = ExogenousCalendar.Build(
                        scenarioKey,
                        basePoints,
                        products,
                        startDate,
                        horizonDays);

                    run.CalendarHash = calendar.Sha256;

                    // Inicializar saldos
                    var pointStock = new Dictionary<(string PointCode, string Sku), decimal>();
                    foreach (var p in basePoints)
                    {
                        foreach (var prod in products)
                        {
                            pointStock[(p.Code, prod.Sku)] = prod.BaseDaily * 3m; // 3 días de cobertura inicial
                        }
                    }

                    var dailyResults = new List<SimulationDayResult>();
                    var simulationEvents = new List<SimulationEventDto>();
                    long eventSeq = 0;

                    var clock = new SimulationClock(startDate);

                    for (int dayOffset = 0; dayOffset < horizonDays; dayOffset++)
                    {
                        var currentDate = startDate.AddDays(dayOffset);
                        var dayDemands = calendar.Demand
                            .Where(d => DateOnly.FromDateTime(d.OccurredAtUtc) == currentDate)
                            .ToList();

                        var dayDeliveries = new List<SimulatedDelivery>();

                        if (run.PolicyCode == ReactivePolicy.Version)
                        {
                            foreach (var p in basePoints)
                            {
                                foreach (var prod in products)
                                {
                                    var currentStock = pointStock.TryGetValue((p.Code, prod.Sku), out var s) ? s : 0m;
                                    var decision = reactivePolicy.EvaluatePointDemand(
                                        p.Code,
                                        prod.Sku,
                                        currentStock,
                                        inTransitStock: 0m,
                                        historicalMeanDaily: prod.BaseDaily,
                                        leadTimeDays: 2m,
                                        targetCoverageDays: 14);

                                    if (decision.Triggered && decision.RequestedQuantity > 0)
                                    {
                                        // Entrega llega con lead time a las 11:00
                                        var arrivalUtc = clock.WorkdayStartUtc(currentDate).AddHours(3);
                                        dayDeliveries.Add(new SimulatedDelivery(p.Code, prod.Sku, decision.RequestedQuantity, arrivalUtc));
                                    }
                                }
                            }
                        }
                        else if (run.PolicyCode == FixedCalendarPolicy.Version)
                        {
                            var weeklyAssignment = FixedCalendarPolicy.AssignWeeklyDays(basePoints.Select(x => x.Code).ToList());
                            foreach (var p in basePoints)
                            {
                                if (weeklyAssignment.TryGetValue(p.Code, out var assignedDay) && currentDate.DayOfWeek == assignedDay)
                                {
                                    foreach (var prod in products)
                                    {
                                        var qty = fixedPolicy.CalculateFixedDemand(p.Code, prod.Sku, 0m, prod.BaseDaily);
                                        var arrivalUtc = clock.WorkdayStartUtc(currentDate).AddHours(4);
                                        dayDeliveries.Add(new SimulatedDelivery(p.Code, prod.Sku, qty, arrivalUtc));
                                    }
                                }
                            }
                        }
                        else
                        {
                            // IstpetDevPolicy: reposición dinámica
                            foreach (var p in basePoints)
                            {
                                foreach (var prod in products)
                                {
                                    var currentStock = pointStock.TryGetValue((p.Code, prod.Sku), out var s) ? s : 0m;
                                    if (currentStock < prod.BaseDaily * 2.5m)
                                    {
                                        var needed = prod.BaseDaily * 10m - currentStock;
                                        if (needed > 0)
                                        {
                                            var arrivalUtc = clock.WorkdayStartUtc(currentDate).AddHours(3);
                                            dayDeliveries.Add(new SimulatedDelivery(p.Code, prod.Sku, Math.Round(needed, 3), arrivalUtc));
                                        }
                                    }
                                }
                            }
                        }

                        // Ejecutar día con DailySimulationEngine
                        var engine = new DailySimulationEngine("REG-ALL", clock);
                        foreach (var kvp in pointStock)
                        {
                            engine.SetInitialPointStock(kvp.Key.PointCode, kvp.Key.Sku, kvp.Value);
                        }

                        var daySimResult = engine.ExecuteDay(currentDate, dayDemands, dayDeliveries, []);

                        // Actualizar saldos finales del día
                        foreach (var p in basePoints)
                        {
                            foreach (var prod in products)
                            {
                                pointStock[(p.Code, prod.Sku)] = engine.GetPointStock(p.Code, prod.Sku);
                            }
                        }

                        foreach (var ev in daySimResult.Events)
                        {
                            simulationEvents.Add(ev with { Sequence = ++eventSeq });
                        }

                        int completedDeliveries = dayDeliveries.Count;
                        decimal km = completedDeliveries > 0 ? (completedDeliveries * 12.5m) : 0m;
                        decimal cost = km * 1.75m;

                        dailyResults.Add(new SimulationDayResult(
                            Date: currentDate,
                            RegionCode: "REG-ALL",
                            Km: km,
                            TransportCost: cost,
                            CompletedDeliveries: completedDeliveries,
                            OnTimeCommonObligations: completedDeliveries));

                        run.ProgressPercentage = 10 + (int)(80.0 * (dayOffset + 1) / horizonDays);
                    }

                    // Validar invariantes
                    var demandRecords = simulationEvents
                        .Where(e => e.Kind is "demand_satisfied" or "stockout")
                        .Select(e => new DemandBalanceRecord(
                            PointCode: e.PointCode,
                            Sku: e.Sku,
                            Requested: e.Requested ?? 0m,
                            Satisfied: e.Satisfied ?? 0m,
                            Unserved: (e.Requested ?? 0m) - (e.Satisfied ?? 0m)))
                        .ToList();

                    var invariantCheck = SimulationInvariantValidator.ValidateDemandBalance(demandRecords);
                    if (!invariantCheck.IsValid)
                    {
                        run.Status = "Failed";
                        run.FailureReason = string.Join("; ", invariantCheck.Violations);
                        run.CompletedAtUtc = DateTime.UtcNow;
                        await db.SaveChangesAsync(ct);
                        continue;
                    }

                    // Calcular métricas
                    var totalKm = dailyResults.Sum(d => d.Km);
                    var totalCost = dailyResults.Sum(d => d.TransportCost);
                    var totalDeliveries = dailyResults.Sum(d => d.CompletedDeliveries);

                    var computedMetrics = new List<MetricValue>
                    {
                        new MetricValue("km_total", MetricCalculator.DefinitionVersion, null, totalKm, null, totalKm, "km", null),
                        new MetricValue("costo_transporte", MetricCalculator.DefinitionVersion, null, totalCost, null, totalCost, "USD", null),
                        new MetricValue("entregas_completadas", MetricCalculator.DefinitionVersion, null, totalDeliveries, null, totalDeliveries, "entregas", null),
                        MetricCalculator.ComputeRatio(totalKm, totalDeliveries, "km/entrega", "km_por_entrega"),
                        MetricCalculator.ComputeRatio(totalCost, totalDeliveries, "USD/entrega", "costo_por_entrega"),
                        MetricCalculator.ComputePercentage(totalDeliveries, totalDeliveries, "puntualidad"),
                        MetricCalculator.BuildTraceabilityMetric(isCustodySimulated: false)
                    };

                    foreach (var m in computedMetrics)
                    {
                        db.SimulationMetrics.Add(new SimulationMetric
                        {
                            OrganizationId = DemoOrganization.Id,
                            SimulationRunId = run.Id,
                            Code = m.Code,
                            PolicyCode = run.PolicyCode,
                            Numerator = m.Numerator,
                            Denominator = m.Denominator,
                            Value = m.Value,
                            Unit = m.Unit,
                            NotApplicableReason = m.NotApplicableReason
                        });
                    }

                    // Guardar eventos
                    foreach (var ev in simulationEvents.Take(500))
                    {
                        db.SimulationEvents.Add(new SimulationEvent
                        {
                            OrganizationId = DemoOrganization.Id,
                            SimulationRunId = run.Id,
                            Sequence = ev.Sequence,
                            OccurredAtUtc = ev.OccurredAtUtc,
                            Kind = ev.Kind,
                            RegionCode = ev.RegionCode,
                            PointCode = ev.PointCode,
                            Sku = ev.Sku,
                            Requested = ev.Requested,
                            Satisfied = ev.Satisfied,
                            Reason = ev.Reason
                        });
                    }

                    run.Status = "Completed";
                    run.ProgressPercentage = 100;
                    run.CompletedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception runEx)
                {
                    _logger.LogError(runEx, "Run {RunId} failed with exception", run.Id);
                    run.Status = "Failed";
                    run.FailureReason = runEx.Message;
                    run.CompletedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
            }

            _logger.LogInformation("Comparison {ComparisonId} execution finished.", comparisonId);
        }
        finally
        {
            _executionLock.Release();
        }
    }
}
