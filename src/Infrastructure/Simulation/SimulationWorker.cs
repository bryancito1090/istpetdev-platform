using System.Threading.Channels;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IstpetDev.Infrastructure.Simulation;

public sealed class SimulationWorker : BackgroundService, ISimulationWorker
{
    private static readonly GeoPoint DefaultDepotPos = new(-0.1800m, -78.4800m);

    private static readonly Dictionary<string, GeoPoint> DefaultPointCoordinates = new()
    {
        ["P01"] = new(-0.148m, -78.459m),
        ["P02"] = new(-0.124m, -78.492m),
        ["P03"] = new(-0.164m, -78.491m),
        ["P04"] = new(-0.108m, -78.438m),
        ["P05"] = new(-0.278m, -78.519m),
        ["P06"] = new(-0.254m, -78.552m),
        ["P07"] = new(-0.294m, -78.551m),
        ["P08"] = new(-0.238m, -78.498m),
        ["P09"] = new(-2.188m, -79.879m),
        ["P10"] = new(-2.164m, -79.912m),
        ["P11"] = new(-2.204m, -79.911m),
        ["P12"] = new(-2.148m, -79.858m),
        ["P13"] = new(-2.908m, -78.999m),
        ["P14"] = new(-2.884m, -79.032m),
        ["P15"] = new(-2.924m, -79.031m),
        ["P16"] = new(-2.868m, -78.978m),
        ["P17"] = new(-0.968m, -80.699m),
        ["P18"] = new(-0.944m, -80.732m),
        ["P19"] = new(-0.984m, -80.731m),
        ["P20"] = new(-0.928m, -80.678m),
        ["P21"] = new(-1.258m, -78.619m),
        ["P22"] = new(-1.234m, -78.652m),
        ["P23"] = new(-1.274m, -78.651m),
        ["P24"] = new(-1.218m, -78.598m)
    };

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

            var logisticsOptions = scope.ServiceProvider.GetService<IOptions<LogisticsOptions>>()?.Value ?? new LogisticsOptions();

            var vehicles = await db.Vehicles
                .Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive)
                .ToListAsync(ct);

            decimal effectiveCostPerKm = logisticsOptions.DefaultCostPerKm;
            var vehiclesWithCost = vehicles.Where(v => v.CostPerKm.HasValue && v.CostPerKm.Value > 0).ToList();
            if (vehiclesWithCost.Count > 0)
            {
                effectiveCostPerKm = vehiclesWithCost.Average(v => v.CostPerKm!.Value);
            }

            var servicePointsInDb = await db.ServicePoints
                .Where(sp => sp.OrganizationId == DemoOrganization.Id && sp.IsActive && sp.Position != null)
                .ToListAsync(ct);

            var servicePointCoords = servicePointsInDb.ToDictionary(
                sp => sp.Code,
                sp => new GeoPoint((decimal)sp.Position!.Y, (decimal)sp.Position.X));

            var depotLocation = await db.Locations
                .FirstOrDefaultAsync(l => l.OrganizationId == DemoOrganization.Id && l.Kind == "Warehouse" && l.Position != null, ct);
            var depotPos = depotLocation?.Position != null
                ? new GeoPoint((decimal)depotLocation.Position.Y, (decimal)depotLocation.Position.X)
                : DefaultDepotPos;

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
                    var disruptionsToPass = request.CustomDisruptions;
                    if (run.ScenarioCode == "adverse" && (disruptionsToPass == null || disruptionsToPass.Count == 0))
                    {
                        var adverseDisruptions = new List<DisruptionEvent>();
                        for (int day = 0; day < horizonDays; day += 7)
                        {
                            var adverseDate = startDate.AddDays(day);
                            adverseDisruptions.Add(new DisruptionEvent(
                                OccurredAtUtc: adverseDate.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc),
                                KnownAtUtc: adverseDate.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc),
                                RegionCode: "REG-ALL",
                                Kind: "road_delay",
                                SubjectCode: "ALL",
                                Factor: 1.5m));
                        }
                        disruptionsToPass = adverseDisruptions;
                    }

                    var scenarioKey = new ScenarioKey(run.DatasetVersion, run.Seed, run.ScenarioCode);
                    var calendar = ExogenousCalendar.Build(
                        scenarioKey,
                        basePoints,
                        products,
                        startDate,
                        horizonDays,
                        disruptionsToPass);

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

                        var dayDisruptions = calendar.Disruptions
                            .Where(d => DateOnly.FromDateTime(d.OccurredAtUtc) == currentDate)
                            .ToList();

                        var daySimResult = engine.ExecuteDay(currentDate, dayDemands, dayDeliveries, dayDisruptions);

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
                        decimal km = 0m;
                        decimal cost = 0m;

                        if (completedDeliveries > 0)
                        {
                            var visitedPoints = dayDeliveries.Select(d => d.PointCode).Distinct().OrderBy(p => p).ToList();

                            var detourFactor = dayDisruptions
                                .Where(d => (d.Kind is "road_closure" or "detour") && d.Factor > 1.0m)
                                .Select(d => d.Factor)
                                .DefaultIfEmpty(1.0m)
                                .Max();

                            var routePoints = new List<GeoPoint> { depotPos };
                            foreach (var pt in visitedPoints)
                            {
                                var ptCoord = servicePointCoords.TryGetValue(pt, out var spCoord)
                                    ? spCoord
                                    : (DefaultPointCoordinates.TryGetValue(pt, out var defCoord) ? defCoord : depotPos);
                                routePoints.Add(ptCoord);
                            }
                            routePoints.Add(depotPos);

                            for (int i = 0; i < routePoints.Count - 1; i++)
                            {
                                var from = routePoints[i];
                                var to = routePoints[i + 1];
                                var legKm = DistanceProvider.HaversineDistanceKm(from, to) * logisticsOptions.ApproxDetourFactor * detourFactor;
                                km += legKm;
                            }

                            km = Math.Round(km, 3, MidpointRounding.AwayFromZero);

                            var activeDelayFactor = dayDisruptions
                                .Where(d => d.Factor > 1.0m)
                                .Select(d => d.Factor)
                                .DefaultIfEmpty(1.0m)
                                .Max();

                            var speedKmh = logisticsOptions.ApproxSpeedKmh > 0 ? logisticsOptions.ApproxSpeedKmh : 40m;
                            var drivingMinutes = (km / speedKmh) * 60m * activeDelayFactor;
                            var serviceMinutes = (decimal)visitedPoints.Count * logisticsOptions.DefaultServiceMinutes;
                            var totalDurationMinutes = drivingMinutes + serviceMinutes;

                            cost = LogisticsCostCalculator.CalculateCost(
                                km,
                                totalDurationMinutes,
                                effectiveCostPerKm,
                                logisticsOptions.CostPerHour);
                        }

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
                    var eventsToSave = simulationEvents
                        .Where(e => e.Kind == "disruption_applied")
                        .Concat(simulationEvents.Where(e => e.Kind != "disruption_applied"))
                        .Take(500);

                    foreach (var ev in eventsToSave)
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
