using System.Text.Json;
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
                .Where(r => r.OrganizationId == DemoOrganization.Id && r.ComparisonId == comparisonId)
                .OrderBy(r => r.Seed)
                .ThenBy(r => r.PolicyCode)
                .ToListAsync(ct);

            if (runs.Count == 0)
            {
                _logger.LogWarning("Comparison {ComparisonId} has no runs in database.", comparisonId);
                return;
            }

            var first = runs[0];
            SimulationDataset dataset;
            try
            {
                dataset = await SimulationDatasetReader.LoadAsync(
                    db, first.DatasetVersion, first.DatasetHash, ct);
            }
            catch (Exception ex)
            {
                foreach (var run in runs)
                {
                    run.Status = "Failed";
                    run.FailureReason = ex.Message;
                    run.CompletedAtUtc = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(ct);
                _logger.LogError(ex, "Dataset load failed for comparison {ComparisonId}", comparisonId);
                return;
            }

            var options = scope.ServiceProvider.GetService<IOptions<LogisticsOptions>>()?.Value ?? new LogisticsOptions();
            var stockoutKeysByRun = new Dictionary<Guid, HashSet<string>>();

            foreach (var run in runs)
            {
                run.Status = "Running";
                run.StartedAtUtc = DateTime.UtcNow;
                run.ProgressPercentage = 5;
                run.DatasetVersion = dataset.Version;
                run.DatasetHash = dataset.Hash;
                run.ManifestHash = dataset.Hash;
                await db.SaveChangesAsync(ct);

                try
                {
                    var calendar = BuildCalendar(dataset, run, request.CustomDisruptions);
                    run.CalendarHash = calendar.Sha256;
                    run.ParametersJson = JsonSerializer.Serialize(new
                    {
                        dataset = dataset.Version,
                        datasetHash = dataset.Hash,
                        startDate = dataset.StartDate.ToString("yyyy-MM-dd"),
                        horizonDays = dataset.HorizonDays,
                        points = dataset.Points.Count,
                        products = dataset.Products.Count,
                        vehicles = dataset.Vehicles.Count,
                        distanceMode = dataset.HasRoadMatrix ? "Road" : "Approximation",
                        retiredVehicle = run.ScenarioCode == "adverse" ? dataset.AdverseRetiredVehicle : null,
                        leadTimeExtraDays = run.ScenarioCode == "adverse" ? 1 : 0,
                        demandSource = "Consumptions por punto, SKU y día de la semana",
                        punctuality = "líneas de demanda atendidas / líneas solicitadas",
                        stockouts = "clave punto|SKU|día"
                    });
                    await db.SaveChangesAsync(ct);

                    var result = SimulateRun(dataset, calendar, run, options);
                    stockoutKeysByRun[run.Id] = result.StockoutKeys;

                    foreach (var metric in result.Metrics)
                    {
                        db.SimulationMetrics.Add(new SimulationMetric
                        {
                            OrganizationId = DemoOrganization.Id,
                            SimulationRunId = run.Id,
                            Code = metric.Code,
                            PolicyCode = run.PolicyCode,
                            ProductId = metric.ProductId,
                            Numerator = metric.Numerator,
                            Denominator = metric.Denominator,
                            Value = metric.Value,
                            Unit = metric.Unit,
                            NotApplicableReason = metric.NotApplicableReason
                        });
                    }

                    var eventsToSave = result.Events
                        .Where(e => e.Kind == "disruption_applied")
                        .Concat(result.Events.Where(e => e.Kind != "disruption_applied"))
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Run {RunId} failed", run.Id);
                    run.Status = "Failed";
                    run.FailureReason = ex.Message;
                    run.CompletedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
            }

            AddComparisonMetrics(db, runs, stockoutKeysByRun);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Comparison {ComparisonId} execution finished.", comparisonId);
        }
        finally
        {
            _executionLock.Release();
        }
    }

    private static ScenarioCalendar BuildCalendar(
        SimulationDataset dataset,
        SimulationRun run,
        IReadOnlyList<DisruptionEvent>? customDisruptions = null)
    {
        if (run.ScenarioCode == "adverse")
        {
            var baseCalendar = ExogenousCalendar.Build(
                new ScenarioKey(dataset.Version, run.Seed, "base"),
                dataset.DemandProfiles,
                dataset.StartDate,
                dataset.HorizonDays,
                customDisruptions);
            return ExogenousCalendar.DeriveAdverseScenario(baseCalendar, dataset.AdverseRetiredVehicle ?? "");
        }

        return ExogenousCalendar.Build(
            new ScenarioKey(dataset.Version, run.Seed, run.ScenarioCode),
            dataset.DemandProfiles,
            dataset.StartDate,
            dataset.HorizonDays,
            customDisruptions);
    }

    private static RunSimulationResult SimulateRun(
        SimulationDataset dataset,
        ScenarioCalendar calendar,
        SimulationRun run,
        LogisticsOptions options)
    {
        var isAdverse = run.ScenarioCode == "adverse";
        var leadTimeExtra = isAdverse ? 1m : 0m;
        var activeVehicles = dataset.Vehicles
            .Where(v => !isAdverse || !string.Equals(v.Code, dataset.AdverseRetiredVehicle, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (activeVehicles.Count == 0)
            throw new SimulationDatasetException("El escenario no deja vehículos disponibles para operar.");

        var pointByCode = dataset.Points.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var productBySku = dataset.Products.ToDictionary(p => p.Sku, StringComparer.OrdinalIgnoreCase);
        var profilesByPoint = dataset.DemandProfiles
            .GroupBy(p => p.PointCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var fixedAssignments = FixedCalendarPolicy.AssignWeeklyDays(dataset.Points.Select(p => p.Code).ToList());
        var pointStock = new Dictionary<(string PointCode, string Sku), decimal>(dataset.InitialPointStock);
        var warehouseStock = new Dictionary<(string WarehouseCode, string Sku), decimal>(dataset.InitialWarehouseStock);
        var pending = new List<PendingDelivery>();
        var events = new List<SimulationEventDto>();
        var stockoutKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unmet = new Dictionary<(string Sku, string Unit), decimal>();
        var scheduledLeadTimes = new List<int>();
        var clock = new SimulationClock(dataset.StartDate);
        var reactivePolicy = new ReactivePolicy();
        var fixedPolicy = new FixedCalendarPolicy();
        long sequence = 0;
        decimal totalKm = 0m;
        decimal totalCost = 0m;
        int deliveredLines = 0;

        var productEntities = dataset.Products.ToDictionary(
            p => p.Id,
            p => new Product
            {
                Id = p.Id,
                Sku = p.Sku,
                BaseUnit = p.Unit,
                WeightPerBaseUnit = p.WeightPerUnit,
                VolumePerBaseUnit = p.VolumePerUnit,
                UnitValue = p.UnitValue,
                UnitsPerPackage = p.UnitsPerPackage,
                IsActive = true
            });
        var serviceMinutes = dataset.Points.ToDictionary(p => p.Id, p => (int?)p.ServiceMinutes);
        var calculator = new ServiceCostCalculator(options, productEntities, serviceMinutes);

        for (var dayOffset = 0; dayOffset < dataset.HorizonDays; dayOffset++)
        {
            var currentDate = dataset.StartDate.AddDays(dayOffset);
            var planningUtc = clock.PlanningTimeUtc(currentDate);
            var dayDemands = calendar.Demand
                .Where(d => DateOnly.FromDateTime(d.OccurredAtUtc) == currentDate)
                .ToList();
            var due = pending.Where(p => p.ArrivalDate <= currentDate).ToList();
            pending.RemoveAll(p => p.ArrivalDate <= currentDate);

            var inTransit = due.Concat(pending)
                .GroupBy(d => (d.PointCode, d.Sku))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            void Schedule(string pointCode, string sku, decimal requested, decimal leadTimeDays, string policy)
            {
                if (requested <= 0m) return;
                if (!dataset.PointWarehouse.TryGetValue(pointCode, out var warehouse) ||
                    !pointByCode.TryGetValue(pointCode, out var point))
                {
                    events.Add(new SimulationEventDto(
                        ++sequence, planningUtc, "delivery_pending", "REG-ALL", pointCode, sku,
                        requested, null, "warehouse_not_assigned"));
                    return;
                }

                var stockKey = (warehouse, sku);
                var available = warehouseStock.GetValueOrDefault(stockKey, 0m);
                var quantity = Math.Min(requested, Math.Max(0m, available));
                if (quantity <= 0m)
                {
                    events.Add(new SimulationEventDto(
                        ++sequence, planningUtc, "delivery_pending", point.Region, pointCode, sku,
                        requested, null, "warehouse_stock"));
                    return;
                }

                warehouseStock[stockKey] = available - quantity;
                if (quantity < requested)
                {
                    events.Add(new SimulationEventDto(
                        ++sequence, planningUtc, "delivery_partial", point.Region, pointCode, sku,
                        requested, quantity, "warehouse_stock"));
                }

                var arrivalDate = SimulationClock.AddWorkdays(
                    currentDate, Math.Max(0, (int)Math.Ceiling(leadTimeDays)));
                scheduledLeadTimes.Add(SimulationClock.WorkdaysBetween(currentDate, arrivalDate));
                var arrivalUtc = clock.WorkdayStartUtc(arrivalDate).AddHours(4);
                pending.Add(new PendingDelivery(pointCode, sku, quantity, arrivalDate, arrivalUtc, policy));
                events.Add(new SimulationEventDto(
                    ++sequence, planningUtc, "delivery_ordered", point.Region, pointCode, sku,
                    quantity, null, policy));
            }

            foreach (var point in dataset.Points)
            {
                if (!profilesByPoint.TryGetValue(point.Code, out var profiles)) continue;
                var leadTime = point.LeadTimeDays + leadTimeExtra;

                foreach (var profile in profiles)
                {
                    var currentStock = pointStock.GetValueOrDefault((point.Code, profile.Sku), 0m);
                    var transit = inTransit.GetValueOrDefault((point.Code, profile.Sku), 0m);
                    var mean = profile.AverageDailyDemand;
                    if (mean <= 0m) continue;

                    if (run.PolicyCode == ReactivePolicy.Version)
                    {
                        var decision = reactivePolicy.EvaluatePointDemand(
                            point.Code, profile.Sku, currentStock, transit, mean, leadTime,
                            targetCoverageDays: 14);
                        if (decision.Triggered && decision.RequestedQuantity > 0m)
                            Schedule(point.Code, profile.Sku, decision.RequestedQuantity, leadTime, run.PolicyCode);
                    }
                    else if (run.PolicyCode == FixedCalendarPolicy.Version)
                    {
                        if (!fixedAssignments.TryGetValue(point.Code, out var assignedDay) ||
                            assignedDay != currentDate.DayOfWeek) continue;
                        var qty = fixedPolicy.CalculateFixedDemand(point.Code, profile.Sku, currentStock, mean);
                        Schedule(point.Code, profile.Sku, qty, leadTime, run.PolicyCode);
                    }
                    else
                    {
                        // La política preventiva se adelanta al umbral reactivo, pero usa el mismo
                        // stock, la misma demanda histórica y el mismo lead time declarado.
                        var threshold = mean * (leadTime + 1m);
                        if (currentStock + transit < threshold)
                        {
                            var qty = Math.Max(0m, mean * 14m - currentStock - transit);
                            Schedule(point.Code, profile.Sku, qty, leadTime, run.PolicyCode);
                        }
                    }
                }
            }

            var engine = new DailySimulationEngine("REG-ALL", clock);
            foreach (var kvp in pointStock)
                engine.SetInitialPointStock(kvp.Key.PointCode, kvp.Key.Sku, kvp.Value);

            var dayDisruptions = calendar.Disruptions
                .Where(d => DateOnly.FromDateTime(d.OccurredAtUtc) == currentDate)
                .ToList();
            var dayResult = engine.ExecuteDay(
                currentDate,
                dayDemands,
                due.Select(d => new SimulatedDelivery(d.PointCode, d.Sku, d.Quantity, d.ArrivalUtc)).ToList(),
                dayDisruptions);

            foreach (var point in dataset.Points)
                foreach (var profile in profilesByPoint.GetValueOrDefault(point.Code) ?? [])
                    pointStock[(point.Code, profile.Sku)] = engine.GetPointStock(point.Code, profile.Sku);

            foreach (var ev in dayResult.Events)
                events.Add(ev with { Sequence = ++sequence });

            foreach (var stockout in dayResult.Stockouts)
            {
                var key = $"{stockout.PointCode}|{stockout.Sku}|{DateOnly.FromDateTime(stockout.OccurredAtUtc):yyyy-MM-dd}";
                stockoutKeys.Add(key);
                if (productBySku.TryGetValue(stockout.Sku, out var product))
                {
                    var unmetKey = (stockout.Sku, product.Unit);
                    unmet[unmetKey] = unmet.GetValueOrDefault(unmetKey, 0m) + stockout.UnservedQuantity;
                }
            }

            var routeResult = RouteDeliveries(
                dataset, due, activeVehicles, calculator, pointByCode, productBySku,
                dayDisruptions, options, warehouseStock, events, ref sequence);
            totalKm += routeResult.Km;
            totalCost += routeResult.Cost;
            deliveredLines += routeResult.DeliveredLines;

            run.ProgressPercentage = 10 + (int)(85.0 * (dayOffset + 1) / dataset.HorizonDays);
        }

        var totalDemandLines = events.Count(e => e.Kind is "demand_satisfied" or "stockout");
        var satisfiedDemandLines = events.Count(e => e.Kind == "demand_satisfied");
        var metrics = new List<MetricValue>
        {
            new("km_total", MetricCalculator.DefinitionVersion, null, totalKm, null, totalKm, "km", null),
            new("costo_transporte", MetricCalculator.DefinitionVersion, null, totalCost, null, totalCost, "USD", null),
            new("entregas_completadas", MetricCalculator.DefinitionVersion, null, deliveredLines, null, deliveredLines, "entregas", null),
            MetricCalculator.ComputeRatio(totalKm, deliveredLines, "km/entrega", "km_por_entrega"),
            MetricCalculator.ComputeRatio(totalCost, deliveredLines, "USD/entrega", "costo_por_entrega"),
            MetricCalculator.ComputePercentage(satisfiedDemandLines, totalDemandLines, "puntualidad"),
            new("lineas_demanda_solicitadas", MetricCalculator.DefinitionVersion, null, totalDemandLines, null, totalDemandLines, "líneas", null),
            new("lineas_demanda_atendidas", MetricCalculator.DefinitionVersion, null, satisfiedDemandLines, null, satisfiedDemandLines, "líneas", null),
            new("quiebres_total", MetricCalculator.DefinitionVersion, null, stockoutKeys.Count, null, stockoutKeys.Count, "quiebres", null),
            MetricCalculator.BuildTraceabilityMetric(isCustodySimulated: false)
        };

        if (scheduledLeadTimes.Count > 0)
        {
            var minLeadTime = scheduledLeadTimes.Min();
            var averageLeadTime = Math.Round((decimal)scheduledLeadTimes.Average(), 3, MidpointRounding.AwayFromZero);
            metrics.Add(new MetricValue("lead_time_minimo_dias", MetricCalculator.DefinitionVersion, null, minLeadTime, null, minLeadTime, "días", null));
            metrics.Add(new MetricValue("lead_time_promedio_dias", MetricCalculator.DefinitionVersion, null, averageLeadTime, null, averageLeadTime, "días", null));
        }
        else
        {
            metrics.Add(new MetricValue("lead_time_minimo_dias", MetricCalculator.DefinitionVersion, null, 0m, null, null, "días", "zero_denominator"));
            metrics.Add(new MetricValue("lead_time_promedio_dias", MetricCalculator.DefinitionVersion, null, 0m, null, null, "días", "zero_denominator"));
        }

        foreach (var ((sku, unit), quantity) in unmet.OrderBy(x => x.Key.Sku))
        {
            var product = productBySku.GetValueOrDefault(sku);
            metrics.Add(new MetricValue(
                "demanda_no_atendida",
                MetricCalculator.DefinitionVersion,
                sku,
                quantity,
                null,
                quantity,
                unit,
                null,
                product?.Id));
        }

        return new RunSimulationResult(metrics, events, stockoutKeys, totalKm, totalCost, deliveredLines);
    }

    private static RouteResult RouteDeliveries(
        SimulationDataset dataset,
        IReadOnlyList<PendingDelivery> due,
        IReadOnlyList<SimulationVehicle> activeVehicles,
        ServiceCostCalculator calculator,
        IReadOnlyDictionary<string, SimulationPoint> pointByCode,
        IReadOnlyDictionary<string, SimulationProduct> productBySku,
        IReadOnlyList<DisruptionEvent> dayDisruptions,
        LogisticsOptions options,
        Dictionary<(string WarehouseCode, string Sku), decimal> warehouseStock,
        List<SimulationEventDto> events,
        ref long sequence)
    {
        decimal km = 0m;
        decimal cost = 0m;
        var deliveredLines = 0;
        var delayFactor = dayDisruptions
            .Where(d => d.Factor > 1m)
            .Select(d => d.Factor)
            .DefaultIfEmpty(1m)
            .Max();

        foreach (var warehouseGroup in due.GroupBy(d => dataset.PointWarehouse.GetValueOrDefault(d.PointCode, "")))
        {
            var warehouse = warehouseGroup.Key;
            var vehicles = activeVehicles
                .Where(v => string.Equals(v.DepotCode, warehouse, StringComparison.OrdinalIgnoreCase))
                .OrderBy(v => v.Code, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (vehicles.Count == 0)
            {
                foreach (var delivery in warehouseGroup)
                {
                    ReleaseStock(warehouseStock, warehouse, delivery);
                    events.Add(new SimulationEventDto(
                        ++sequence, delivery.ArrivalUtc, "delivery_pending", "REG-ALL",
                        delivery.PointCode, delivery.Sku, delivery.Quantity, null, "vehicle_unavailable"));
                }
                continue;
            }

            var stops = BuildStops(warehouseGroup, pointByCode, productBySku);
            var states = vehicles.Select(v => new VehicleState(v)).ToList();
            foreach (var stop in stops.OrderBy(s => s.WindowEnd).ThenBy(s => s.Code, StringComparer.OrdinalIgnoreCase))
            {
                var weight = Math.Abs(stop.DeltaWeight);
                var volume = Math.Abs(stop.DeltaVolume);
                var state = states
                    .Where(s => s.RemainingWeight >= weight && s.RemainingVolume >= volume)
                    .OrderByDescending(s => s.RemainingWeight)
                    .ThenBy(s => s.Vehicle.Code, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (state is null)
                {
                    foreach (var load in stop.Loads ?? [])
                        ReleaseStock(warehouseStock, warehouse, new PendingDelivery(stop.Code, load.Sku, load.Quantity, default, default, ""));
                    events.Add(new SimulationEventDto(
                        ++sequence, due.First().ArrivalUtc, "delivery_pending", "REG-ALL",
                        stop.Code, "", weight, null, "vehicle_capacity"));
                    continue;
                }
                state.RemainingWeight -= weight;
                state.RemainingVolume -= volume;
                state.Stops.Add(stop);
            }

            if (!dataset.WarehousePositions.TryGetValue(warehouse, out var depot))
                depot = stops.FirstOrDefault()?.Position ?? new GeoPoint(0m, 0m);

            foreach (var state in states.Where(s => s.Stops.Count > 0))
            {
                var ordered = state.Stops
                    .OrderBy(s => s.WindowEnd)
                    .ThenBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
                    .Select((s, index) => s with { Sequence = index + 1 })
                    .ToList();
                var vehicleEntity = new Vehicle
                {
                    Id = state.Vehicle.Id,
                    Code = state.Vehicle.Code,
                    DepotLocationId = state.Vehicle.DepotLocationId,
                    MaxWeight = state.Vehicle.MaxWeight,
                    MaxVolume = state.Vehicle.MaxVolume,
                    CostPerKm = state.Vehicle.CostPerKm,
                    VehicleType = state.Vehicle.VehicleType,
                    IsActive = true
                };

                var routeKm = 0m;
                var routeMinutes = 0m;
                var previous = depot;
                foreach (var stop in ordered)
                {
                    var leg = Distance(dataset, previous, stop.Position, options);
                    routeKm += leg.Km;
                    routeMinutes += leg.Minutes;
                    previous = stop.Position;
                }
                var returnLeg = Distance(dataset, previous, depot, options);
                routeKm += returnLeg.Km;
                routeMinutes += returnLeg.Minutes;
                routeMinutes *= delayFactor;
                routeMinutes += ordered.Sum(s => calculator.ServiceMinutes(s, vehicleEntity));

                var maxWeight = ordered.Sum(s => Math.Abs(s.DeltaWeight));
                var breakdown = calculator.Calculate(routeKm, routeMinutes, vehicleEntity, maxWeight, ordered);
                km += routeKm;
                cost += breakdown.Total;
                deliveredLines += ordered.Sum(s => s.Loads?.Count ?? 0);

                events.Add(new SimulationEventDto(
                    ++sequence, due.First().ArrivalUtc,
                    "route_completed", "REG-ALL", vehicleEntity.Code, "",
                    Math.Round(routeKm, 3), Math.Round(breakdown.Total, 2), vehicleEntity.Code));
            }
        }

        return new RouteResult(km, cost, deliveredLines);
    }

    private static List<RouteStopCandidate> BuildStops(
        IEnumerable<PendingDelivery> deliveries,
        IReadOnlyDictionary<string, SimulationPoint> pointByCode,
        IReadOnlyDictionary<string, SimulationProduct> productBySku)
    {
        var result = new List<RouteStopCandidate>();
        foreach (var pointGroup in deliveries.GroupBy(d => d.PointCode))
        {
            if (!pointByCode.TryGetValue(pointGroup.Key, out var point)) continue;
            var loads = new List<StopLoadDto>();
            decimal weight = 0m;
            decimal volume = 0m;
            foreach (var delivery in pointGroup)
            {
                if (!productBySku.TryGetValue(delivery.Sku, out var product)) continue;
                loads.Add(new StopLoadDto(product.Id, product.Sku, delivery.Quantity, product.Unit));
                weight += delivery.Quantity * product.WeightPerUnit;
                volume += delivery.Quantity * product.VolumePerUnit;
            }
            if (loads.Count == 0) continue;
            result.Add(new RouteStopCandidate(
                Sequence: 0,
                Kind: RouteStopKind.Delivery,
                ServicePointId: point.Id,
                LocationId: point.LocationId,
                Code: point.Code,
                Name: point.Code,
                Position: point.Position,
                DeltaWeight: -weight,
                DeltaVolume: -volume,
                WindowStart: point.WindowStart,
                WindowEnd: point.WindowEnd,
                ServiceMinutes: point.ServiceMinutes,
                Loads: loads));
        }
        return result;
    }

    private static (decimal Km, decimal Minutes) Distance(
        SimulationDataset dataset,
        GeoPoint from,
        GeoPoint to,
        LogisticsOptions options)
    {
        var fromRounded = from.Round();
        var toRounded = to.Round();
        if (fromRounded == toRounded) return (0m, 0m);
        if (dataset.HasRoadMatrix)
        {
            if (!dataset.RoadMatrix.TryGetValue(
                    (fromRounded.Lat, fromRounded.Lon, toRounded.Lat, toRounded.Lon), out var edge))
            {
                throw new SimulationMatrixException(
                    $"Matriz vial incompleta para el tramo {fromRounded.Lat},{fromRounded.Lon} → {toRounded.Lat},{toRounded.Lon}.");
            }
            return (edge.DistanceKm, edge.DurationMinutes);
        }

        var km = SimulationGeo.Haversine(fromRounded, toRounded) * options.ApproxDetourFactor;
        var speed = options.ApproxSpeedKmh > 0 ? options.ApproxSpeedKmh : 40m;
        return (km, km / speed * 60m);
    }

    private static void ReleaseStock(
        Dictionary<(string WarehouseCode, string Sku), decimal> warehouseStock,
        string warehouse,
        PendingDelivery delivery)
    {
        var key = (warehouse, delivery.Sku);
        warehouseStock[key] = warehouseStock.GetValueOrDefault(key, 0m) + delivery.Quantity;
    }

    private static void AddComparisonMetrics(
        IApplicationDbContext db,
        IReadOnlyList<SimulationRun> runs,
        IReadOnlyDictionary<Guid, HashSet<string>> stockoutKeysByRun)
    {
        foreach (var group in runs
                     .Where(r => r.Status == "Completed")
                     .GroupBy(r => (r.Seed, r.ScenarioCode)))
        {
            var platform = group.FirstOrDefault(r => r.PolicyCode == IstpetDevPolicy.Version);
            if (platform is null || !stockoutKeysByRun.TryGetValue(platform.Id, out var platformKeys)) continue;

            foreach (var baselineCode in new[] { ReactivePolicy.Version, FixedCalendarPolicy.Version })
            {
                var baseline = group.FirstOrDefault(r => r.PolicyCode == baselineCode);
                if (baseline is null || !stockoutKeysByRun.TryGetValue(baseline.Id, out var baselineKeys)) continue;

                var (avoided, newlyIntroduced, netDifference) =
                    MetricCalculator.CalculateStockoutComparison(baselineKeys, platformKeys);

                foreach (var (code, value) in new[]
                         {
                             ($"quiebres_evitados_{baselineCode}", (decimal)avoided),
                             ($"quiebres_nuevos_{baselineCode}", (decimal)newlyIntroduced),
                             ($"quiebres_diferencia_neta_{baselineCode}", (decimal)netDifference)
                         })
                {
                    db.SimulationMetrics.Add(new SimulationMetric
                    {
                        OrganizationId = DemoOrganization.Id,
                        SimulationRunId = platform.Id,
                        Code = code,
                        PolicyCode = IstpetDevPolicy.Version,
                        Numerator = value,
                        Value = value,
                        Unit = "quiebres",
                        NotApplicableReason = null
                    });
                }
            }
        }
    }

    private sealed record PendingDelivery(
        string PointCode,
        string Sku,
        decimal Quantity,
        DateOnly ArrivalDate,
        DateTime ArrivalUtc,
        string Policy);

    private sealed record RunSimulationResult(
        List<MetricValue> Metrics,
        List<SimulationEventDto> Events,
        HashSet<string> StockoutKeys,
        decimal Km,
        decimal Cost,
        int DeliveredLines);

    private sealed record RouteResult(decimal Km, decimal Cost, int DeliveredLines);

    private sealed class VehicleState(SimulationVehicle vehicle)
    {
        public SimulationVehicle Vehicle { get; } = vehicle;
        public decimal RemainingWeight { get; set; } = vehicle.MaxWeight;
        public decimal RemainingVolume { get; set; } = vehicle.MaxVolume ?? decimal.MaxValue;
        public List<RouteStopCandidate> Stops { get; } = [];
    }
}
