using System.Globalization;
using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics.Planning;

public sealed class RoutePlanner(
    IApplicationDbContext db,
    IOperationalClock clock,
    IRiskCalculator riskCalculator,
    ReplenishmentAllocator allocator,
    IDistanceProvider distanceProvider,
    IOptions<LogisticsOptions> options) : IRoutePlanner
{
    private readonly LogisticsOptions _options = options.Value;

    public async Task<PlanResult> PlanAsync(PlanRequest request, CancellationToken ct = default)
    {
        var today = request.Date ?? await clock.TodayAsync(ct);
        var criticalDeadline = ParseCriticalDeadline(request.CriticalDeadlineLocal);
        var workdayStartUtc = await clock.WorkdayStartUtcAsync(ct);

        // 1. Calcular riesgos
        var risks = await riskCalculator.CalculateAsync(persistSnapshots: false, ct);

        // 2. Cargar lotes de bodega y calcular asignaciones FEFO
        var warehouseLocations = await db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse)
            .ToListAsync(ct);
        var warehouseLocationIds = warehouseLocations.Select(w => w.Id).ToHashSet();

        var balances = await db.StockBalances
            .Where(b => b.OrganizationId == DemoOrganization.Id && warehouseLocationIds.Contains(b.LocationId))
            .ToListAsync(ct);

        var lotIds = balances.Select(b => b.LotId).Distinct().ToHashSet();
        var lots = await db.Lots
            .Where(l => l.OrganizationId == DemoOrganization.Id && lotIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l, ct);

        var warehouseLotStocks = balances.Select(b =>
        {
            lots.TryGetValue(b.LotId, out var lot);
            return new WarehouseLotStock(
                b.LotId,
                lot?.Code ?? string.Empty,
                lot?.ProductId ?? Guid.Empty,
                b.QuantityOnHand - b.QuantityReserved,
                lot?.ExpiresOn,
                lot?.IsBlocked ?? false);
        }).ToList();

        var allocationResult = allocator.Allocate(risks, warehouseLotStocks, _options.WarehouseSafetyStockPct, today);

        // 3. Obtener vehículos activos
        var vehiclesQuery = db.Vehicles
            .Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive);

        if (request.VehicleCodes != null && request.VehicleCodes.Count > 0)
        {
            vehiclesQuery = vehiclesQuery.Where(v => request.VehicleCodes.Contains(v.Code));
        }

        var vehicles = await vehiclesQuery.OrderBy(v => v.Code).ToListAsync(ct);

        // 4. Cargar puntos de servicio y productos para dimensiones
        var servicePoints = await db.ServicePoints
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive)
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        var products = await db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        // Bodega de salida por vehículo (ADR-28, varias bodegas): cada vehículo sale y vuelve a su DepotLocationId.
        // Antes todos salían de la primera bodega y, con varias, cruzaban el país o quedaban vacíos.
        var depotLoc = warehouseLocations.FirstOrDefault();
        // Posiciones redondeadas como las claves de la matriz de distancias (DistanceProvider usa GeoPoint.Round()):
        // con más de 5 decimales en los datos, la búsqueda fallaba y todos los puntos quedaban «unreachable».
        var depotPos = depotLoc?.Position != null
            ? new GeoPoint((decimal)depotLoc.Position.Y, (decimal)depotLoc.Position.X).Round()
            : new GeoPoint(-0.2295m, -78.5243m); // Default demo
        var depots = vehicles.ToDictionary(v => v.Code, v => DepotOf(v, warehouseLocations) ?? depotPos);

        // 5. Preparar candidatos y clasificar en obligatorios y consolidación
        var candidates = new List<StopCandidate>();
        var unserved = new List<Unserved>();
        var facts = new List<Fact>();

        bool capacityDimensionEvaluated = true;

        // #199: una parada por punto (obligatoria o de consolidación) con todas sus líneas.
        // Las líneas quedan en orden de reparto: nivel de riesgo y luego SKU (ordinal).
        var pointGroups = allocationResult.Lines
            .Where(a => a.AllocatedQuantity > 0)
            .Select(a => (Alloc: a, Risk: risks.FirstOrDefault(r => r.ServicePointId == a.ServicePointId && r.ProductId == a.ProductId)))
            .GroupBy(x => (x.Alloc.ServicePointId, IsConsolidation: (x.Risk?.Level ?? RiskLevel.Normal) == RiskLevel.Vigilar))
            .OrderBy(g => g.First().Alloc.PointCode, StringComparer.Ordinal)
            .ThenBy(g => g.Key.IsConsolidation);

        foreach (var group in pointGroups)
        {
            var first = group.First().Alloc;
            if (!servicePoints.TryGetValue(group.Key.ServicePointId, out var sp)) continue;

            if (sp.Position == null)
            {
                unserved.Add(new Unserved(first.PointCode, "unreachable", Array.Empty<Fact>()));
                continue;
            }

            bool isConsolidation = group.Key.IsConsolidation;
            if (!request.IncludeConsolidation && isConsolidation) continue;

            var lines = group
                .OrderBy(x => x.Risk?.Level ?? RiskLevel.Normal)
                .ThenBy(x => x.Alloc.Sku, StringComparer.Ordinal)
                .ToList();
            var loads = new List<StopLoad>();
            foreach (var (alloc, _) in lines)
            {
                products.TryGetValue(alloc.ProductId, out var product);
                if (product != null && (product.WeightPerBaseUnit == 0 || product.VolumePerBaseUnit == 0))
                {
                    capacityDimensionEvaluated = false;
                }
                loads.Add(new StopLoad(alloc.ProductId, alloc.Sku, alloc.AllocatedQuantity, product?.BaseUnit ?? "U")
                {
                    RequiredQuantity = alloc.RequestedQuantity
                });
            }

            var pos = new GeoPoint((decimal)sp.Position.Y, (decimal)sp.Position.X).Round();
            var riskItem = lines[0].Risk;
            var level = riskItem?.Level ?? RiskLevel.Normal;
            var windowStart = sp.WindowStartLocal ?? TimeOnly.Parse(_options.DefaultWindowStart);
            var windowEnd = sp.WindowEndLocal ?? TimeOnly.Parse(_options.DefaultWindowEnd);
            // Hora límite para críticos (#193): RouteFeasibility rechaza con time_window si no se llega a tiempo.
            bool isCritical = level == RiskLevel.Critico || sp.Criticality == "alta";
            windowEnd = ApplyCriticalDeadline(windowStart, windowEnd, isCritical, criticalDeadline);
            var serviceMin = sp.ServiceMinutes ?? _options.DefaultServiceMinutes;

            candidates.Add(new StopCandidate(
                sp.Id, sp.Code, pos, windowStart, windowEnd, serviceMin,
                WeightOf(loads, products), VolumeOf(loads, products), loads, isConsolidation,
                level, sp.Criticality, riskItem?.CoverageDays ?? 999m, riskItem?.LeadTimeDays ?? 2m));
        }

        // Agregar los unserved por falta de stock en bodega, con la cantidad requerida de la línea
        foreach (var unservedAlloc in allocationResult.Unserved)
        {
            var line = allocationResult.Lines.FirstOrDefault(l => l.PointCode == unservedAlloc.PointCode && l.Sku == unservedAlloc.Sku);
            var baseUnit = line != null && products.TryGetValue(line.ProductId, out var lineProduct) ? lineProduct.BaseUnit : "U";
            unserved.Add(new Unserved(unservedAlloc.PointCode, unservedAlloc.Reason, unservedAlloc.Facts)
            {
                Lines = line == null ? [] :
                    [new PendingLine(line.ProductId, line.Sku, baseUnit, line.RequestedQuantity, 0m, line.RequestedQuantity)]
            });
        }

        if (!capacityDimensionEvaluated)
        {
            facts.Add(new Fact("capacity_dimension_not_evaluated", "fleet", 0, "dimension"));
        }

        // 6. Matriz de distancias
        var allPoints = new List<GeoPoint> { depotPos };
        allPoints.AddRange(depots.Values);
        allPoints.AddRange(candidates.Select(c => c.Position));
        allPoints = allPoints.Distinct().ToList();
        var distanceMatrix = await distanceProvider.MatrixAsync(allPoints, ct);

        // 7. Algoritmo de Inserción
        // Separar obligatorios y consolidación
        var mandatory = candidates
            .Where(c => !c.IsConsolidation)
            .OrderBy(c => c.Level) // Critico antes de Alto
            .ThenBy(c => c.CoverageDays - c.LeadTimeDays)
            .ThenByDescending(c => c.Criticality == "alta" ? 3 : c.Criticality == "media" ? 2 : 1)
            .ThenBy(c => c.PointCode, StringComparer.Ordinal)
            .ToList();

        var consolidation = candidates
            .Where(c => c.IsConsolidation)
            .OrderBy(c => c.CoverageDays)
            .ToList();

        var vehicleRoutes = vehicles.ToDictionary(v => v.Code, v => new List<StopCandidate>());

        // Obligatorios que no entraron completos y su motivo (#199: las líneas se calculan al final)
        var shortfalls = new List<(StopCandidate Candidate, string Reason)>();

        // Insertar obligatorios
        foreach (var candidate in mandatory)
        {
            bool inserted = TryInsertCheapest(vehicles, vehicleRoutes, candidate, depots, workdayStartUtc, distanceMatrix, out var bestVehicleCode, out var bestPos);
            if (inserted && bestVehicleCode != null)
            {
                vehicleRoutes[bestVehicleCode].Insert(bestPos, candidate);
            }
            else
            {
                // Determinar razón de fallo de inserción
                var reason = DetermineFailureReason(vehicles, vehicleRoutes, candidate, depots, workdayStartUtc, distanceMatrix);
                // #199: si no cabe completa, se planifica lo que cabe y el remanente queda pendiente por capacidad.
                if (SplitByCapacity(vehicles, vehicleRoutes, candidate, depots, workdayStartUtc, distanceMatrix, products) > 0)
                {
                    reason = "capacity";
                }
                shortfalls.Add((candidate, reason));
            }
        }

        // 8. 2-opt intrarruta sobre obligatorios
        foreach (var v in vehicles)
        {
            vehicleRoutes[v.Code] = RouteImprover.TwoOpt(
                v, depots[v.Code], vehicleRoutes[v.Code], workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);
        }

        // 9. Consolidación de candidatos Vigilar si desvío <= MaxDetourMinutes
        foreach (var candidate in consolidation)
        {
            bool inserted = TryInsertConsolidation(
                vehicles, vehicleRoutes, candidate, depots, workdayStartUtc, distanceMatrix,
                _options.MaxDetourMinutes, out var bestVehicleCode, out var bestPos);

            if (inserted && bestVehicleCode != null)
            {
                vehicleRoutes[bestVehicleCode].Insert(bestPos, candidate);
                // 2-opt post inserción
                var v = vehicles.First(veh => veh.Code == bestVehicleCode);
                vehicleRoutes[bestVehicleCode] = RouteImprover.TwoOpt(
                    v, depots[v.Code], vehicleRoutes[bestVehicleCode], workdayStartUtc, _options.WorkdayHours,
                    _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);
            }
        }

        // #199: cantidades por línea (punto y producto): requerida, planificada en todas las rutas y pendiente.
        var plannedByLine = vehicleRoutes.Values.SelectMany(r => r)
            .SelectMany(c => c.Loads.Select(l => (c.ServicePointId, l.ProductId, l.Quantity)))
            .GroupBy(x => (x.ServicePointId, x.ProductId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        decimal PendingOf(Guid pointId, StopLoad load) =>
            Math.Max(0m, (load.RequiredQuantity ?? load.Quantity) - plannedByLine.GetValueOrDefault((pointId, load.ProductId)));
        List<PendingLine> PendingLines(StopCandidate c) => c.Loads
            .Select(l => new PendingLine(l.ProductId, l.Sku, l.BaseUnit, l.RequiredQuantity ?? l.Quantity,
                plannedByLine.GetValueOrDefault((c.ServicePointId, l.ProductId)), PendingOf(c.ServicePointId, l)))
            .Where(l => l.PendingQuantity > 0)
            .ToList();

        foreach (var (candidate, reason) in shortfalls)
        {
            // Repartida por completo entre varios vehículos: no queda nada pendiente.
            var pendingLines = PendingLines(candidate);
            if (pendingLines.Count == 0 && plannedByLine.Keys.Any(k => k.ServicePointId == candidate.ServicePointId)) continue;
            unserved.Add(new Unserved(candidate.PointCode, reason, Array.Empty<Fact>()) { Lines = pendingLines });
        }
        // Obligatorios planificados completos pero con faltante de bodega: ese remanente también queda pendiente.
        foreach (var candidate in mandatory.Where(c => shortfalls.All(s => s.Candidate != c)))
        {
            var pendingLines = PendingLines(candidate);
            if (pendingLines.Count > 0)
            {
                unserved.Add(new Unserved(candidate.PointCode, "warehouse_stock", Array.Empty<Fact>()) { Lines = pendingLines });
            }
        }
        // Hechos por línea pendiente para que la explicación (plantilla o agente) no invente cifras.
        foreach (var u in unserved)
        {
            foreach (var line in u.Lines)
            {
                var subject = $"{u.PointCode}/{line.Sku}";
                facts.Add(new Fact("required_quantity", subject, line.RequiredQuantity, line.BaseUnit));
                facts.Add(new Fact("planned_quantity", subject, line.PlannedQuantity, line.BaseUnit));
                facts.Add(new Fact("pending_quantity", subject, line.PendingQuantity, line.BaseUnit));
            }
        }

        // 10. Construir resultado final
        var routes = new List<VehicleRoute>();
        foreach (var v in vehicles)
        {
            var stops = vehicleRoutes[v.Code];
            // Un vehículo sin paradas no genera plan: antes se guardaban rutas vacías de 0 km.
            if (stops.Count == 0)
            {
                facts.Add(new Fact("vehicle_idle", v.Code, 0, "stops"));
                continue;
            }
            var eval = RouteFeasibility.EvaluateRoute(
                v, depots[v.Code], stops, workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);

            var plannedStops = new List<PlannedStop>();
            DateTime curTimeUtc = workdayStartUtc;
            GeoPoint curPos = depots[v.Code];

            for (int i = 0; i < stops.Count; i++)
            {
                var stop = stops[i];
                var leg = distanceMatrix[(curPos, stop.Position)];
                // #245: misma corrección por hora pico que RouteFeasibility
                var speedFactor = TrafficBands.SpeedFactorAt(_options.TrafficBands, TimeOnly.FromDateTime(curTimeUtc.AddHours(-5)));
                curTimeUtc = curTimeUtc.AddMinutes((double)TrafficBands.Apply(leg.Minutes, speedFactor));
                var arrUtc = curTimeUtc;

                var localArr = TimeOnly.FromDateTime(curTimeUtc.AddHours(-5));
                TimeSpan wait = TimeSpan.Zero;
                int? late = null;
                if (localArr < stop.WindowStartLocal)
                {
                    wait = stop.WindowStartLocal.ToTimeSpan() - localArr.ToTimeSpan();
                    curTimeUtc = curTimeUtc.Add(wait);
                }
                else if (localArr > stop.WindowEndLocal)
                {
                    late = (int)Math.Ceiling((localArr.ToTimeSpan() - stop.WindowEndLocal.ToTimeSpan()).TotalMinutes);
                }

                var depUtc = curTimeUtc.AddMinutes(stop.ServiceMinutes);
                curTimeUtc = depUtc;
                curPos = stop.Position;

                var stopLoads = stop.Loads.Select(l => l with { PendingQuantity = PendingOf(stop.ServicePointId, l) }).ToList();
                plannedStops.Add(new PlannedStop(
                    i + 1, stop.PointCode, stop.ServicePointId, arrUtc, depUtc, wait, stopLoads, stop.IsConsolidation, late)
                { TrafficFactorApplied = TrafficBands.Reported(speedFactor) });
            }

            routes.Add(new VehicleRoute(
                v.Code, v.Id, plannedStops, eval.CumulativeKm, eval.CumulativeMinutes,
                eval.CumulativeCost, eval.TotalWeight, eval.TotalVolume, eval.HasApproximations, eval.Legs));

            facts.Add(new Fact("route_km", v.Code, eval.CumulativeKm, "km"));
            facts.Add(new Fact("route_cost", v.Code, eval.CumulativeCost, "USD"));
        }

        return new PlanResult(
            routes.OrderBy(r => r.VehicleCode).ToList(),
            unserved.OrderBy(u => u.PointCode).ToList(),
            facts);
    }

    public async Task<PlanResult> ReplanAsync(ReplanInput input, CancellationToken ct = default)
    {
        // Cargo already belongs to the published route. Reallocating warehouse stock
        // here would lose reserved cargo and repeat deliveries from the fixed prefix.
        var vehicles = await db.Vehicles.Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive)
            .ToDictionaryAsync(v => v.Code, ct);
        var points = await db.ServicePoints.Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Code, ct);
        var products = await db.Products.Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, ct);
        var risks = await riskCalculator.CalculateAsync(false, ct);
        var warehouseLocations = await db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse).ToListAsync(ct);
        var firstDepot = warehouseLocations.First();
        var defaultDepotPos = new GeoPoint((decimal)firstDepot.Position!.Y, (decimal)firstDepot.Position.X).Round();
        var workdayStart = await clock.WorkdayStartUtcAsync(ct);
        var workdayEnd = workdayStart.AddHours(_options.WorkdayHours);
        var routes = new List<VehicleRoute>();
        var unserved = new List<Unserved>();
        var facts = new List<Fact>();
        foreach (var prefix in input.Prefixes.OrderBy(p => p.VehicleCode, StringComparer.Ordinal))
        {
            if (!vehicles.TryGetValue(prefix.VehicleCode, out var vehicle))
                throw new InvalidOperationException("El vehículo del prefijo no existe o está inactivo.");
            var depotPos = DepotOf(vehicle, warehouseLocations) ?? defaultDepotPos;
            var published = await db.RoutePlans.Where(p => p.OrganizationId == DemoOrganization.Id &&
                    p.VehicleId == vehicle.Id && p.Status == "Published")
                .OrderByDescending(p => p.Revision).ThenByDescending(p => p.CreatedAtUtc).FirstAsync(ct);
            using var stored = JsonDocument.Parse(published.InputJson);
            var loads = stored.RootElement.GetProperty("stops").EnumerateArray().ToDictionary(
                s => s.GetProperty("pointCode").GetString()!,
                s => JsonSerializer.Deserialize<List<StopLoad>>(s.GetProperty("loads").GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);
            var consolidationCodes = stored.RootElement.GetProperty("stops").EnumerateArray()
                .Where(s => s.TryGetProperty("isConsolidation", out var value) && value.ValueKind == JsonValueKind.True)
                .Select(s => s.GetProperty("pointCode").GetString()!).ToHashSet();
            var previousLegs = stored.RootElement.TryGetProperty("legs", out var savedLegs)
                ? JsonSerializer.Deserialize<List<Leg>>(savedLegs.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
                : new List<Leg>();
            var completedLegs = previousLegs.Take(prefix.CompletedPointCodes.Count).ToList();
            var completedKm = completedLegs.Sum(l => l.Km);
            var completedMinutes = completedLegs.Sum(l => l.Minutes) + prefix.CompletedPointCodes
                .Sum(code => points[code].ServiceMinutes ?? _options.DefaultServiceMinutes);
            var completedCost = prefix.CompletedPointCodes.Count == 0 ? 0m : Math.Round(
                completedKm * (vehicle.CostPerKm ?? _options.DefaultCostPerKm) +
                Math.Max(0m, (decimal)(prefix.ReadyAtUtc - workdayStart).TotalHours) * _options.CostPerHour,
                3, MidpointRounding.AwayFromZero);
            var candidates = new List<StopCandidate>();
            foreach (var code in prefix.PendingPointCodes.Except(prefix.CompletedPointCodes).Distinct())
            {
                var point = points[code];
                var cargo = loads[code];
                var risk = risks.Where(r => r.ServicePointId == point.Id && cargo.Any(l => l.ProductId == r.ProductId))
                    .OrderBy(r => r.Level).FirstOrDefault();
                // Preserve the classification at publication; receipts elsewhere
                // must not turn a mandatory stop into disposable consolidation.
                var consolidation = consolidationCodes.Contains(code);
                if (input.Mode == ReplanMode.PrioritizeCritical && consolidation)
                {
                    unserved.Add(new Unserved(code, "consolidation_excluded",
                        new[] { new Fact("is_consolidation", code, 1, "flag") }));
                    continue;
                }
                if (!prefix.VehicleAvailable || input.Effect.UnavailableVehicles.Contains(vehicle.Code))
                {
                    unserved.Add(new Unserved(code, "vehicle_unavailable", Array.Empty<Fact>()));
                    continue;
                }
                if (point.Position is null)
                {
                    unserved.Add(new Unserved(code, "unreachable", Array.Empty<Fact>()));
                    continue;
                }
                candidates.Add(new StopCandidate(point.Id, code,
                    new GeoPoint((decimal)point.Position.Y, (decimal)point.Position.X).Round(),
                    point.WindowStartLocal ?? TimeOnly.Parse(_options.DefaultWindowStart),
                    point.WindowEndLocal ?? TimeOnly.Parse(_options.DefaultWindowEnd),
                    point.ServiceMinutes ?? _options.DefaultServiceMinutes,
                    cargo.Sum(l => l.Quantity * products[l.ProductId].WeightPerBaseUnit),
                    cargo.Sum(l => l.Quantity * products[l.ProductId].VolumePerBaseUnit), cargo,
                    consolidation, risk?.Level ?? RiskLevel.Alto, point.Criticality,
                    risk?.CoverageDays ?? 999m, risk?.LeadTimeDays ?? 2m));
            }
            var position = prefix.Position.Round();
            var matrix = await distanceProvider.MatrixAsync(
                new[] { position, depotPos }.Concat(candidates.Select(c => c.Position)).Distinct().ToList(), ct);
            FeasibilityCheck Evaluate(IReadOnlyList<StopCandidate> stops) => RouteFeasibility.EvaluateRoute(
                vehicle, position, stops, prefix.ReadyAtUtc, _options.WorkdayHours, _options.CostPerHour,
                _options.DefaultCostPerKm, matrix, input.Effect, input.Mode, depotPos, workdayEnd, _options.TrafficBands);
            var selected = RouteImprover.TwoOpt(vehicle, position, candidates, prefix.ReadyAtUtc,
                _options.WorkdayHours, _options.CostPerHour, _options.DefaultCostPerKm, matrix,
                input.Effect, input.Mode, depotPos, workdayEnd, _options.TrafficBands);
            if (!Evaluate(selected).IsFeasible)
            {
                selected.Clear();
                foreach (var candidate in candidates.OrderBy(c => c.IsConsolidation).ThenBy(c => c.Level)
                             .ThenBy(c => c.CoverageDays).ThenBy(c => c.PointCode, StringComparer.Ordinal))
                {
                    List<StopCandidate>? best = null;
                    decimal bestCost = decimal.MaxValue;
                    string failure = "unreachable";
                    for (var index = 0; index <= selected.Count; index++)
                    {
                        var proposal = new List<StopCandidate>(selected);
                        proposal.Insert(index, candidate);
                        var evaluated = Evaluate(proposal);
                        if (evaluated.IsFeasible && evaluated.CumulativeCost < bestCost)
                        { best = proposal; bestCost = evaluated.CumulativeCost; }
                        else if (!evaluated.IsFeasible) failure = evaluated.FailureReason!;
                    }
                    if (best is not null) selected = best;
                    else unserved.Add(new Unserved(candidate.PointCode, failure,
                        new[] { new Fact("is_consolidation", candidate.PointCode, candidate.IsConsolidation ? 1 : 0, "flag") }));
                }
            }
            var evaluation = Evaluate(selected);
            var planned = new List<PlannedStop>();
            var time = prefix.ReadyAtUtc;
            var from = position;
            foreach (var stop in selected)
            {
                var speedFactor = TrafficBands.SpeedFactorAt(_options.TrafficBands, TimeOnly.FromDateTime(time.AddHours(-5)));
                var minutes = TrafficBands.Apply(matrix[(from, stop.Position)].Minutes, speedFactor) *
                    input.Effect.SlowFactors.GetValueOrDefault((from, stop.Position), 1m);
                time = time.AddMinutes((double)minutes);
                var arrival = time;
                var local = TimeOnly.FromDateTime(time.AddHours(-5));
                var wait = local < stop.WindowStartLocal
                    ? stop.WindowStartLocal.ToTimeSpan() - local.ToTimeSpan() : TimeSpan.Zero;
                int? late = local > stop.WindowEndLocal
                    ? (int)Math.Ceiling((local.ToTimeSpan() - stop.WindowEndLocal.ToTimeSpan()).TotalMinutes) : null;
                time = time.Add(wait).AddMinutes(stop.ServiceMinutes);
                planned.Add(new PlannedStop(prefix.CompletedPointCodes.Count + planned.Count + 1,
                    stop.PointCode, stop.ServicePointId, arrival, time, wait, stop.Loads, stop.IsConsolidation, late)
                { TrafficFactorApplied = TrafficBands.Reported(speedFactor) });
                from = stop.Position;
            }
            routes.Add(new VehicleRoute(vehicle.Code, vehicle.Id, planned, completedKm + evaluation.CumulativeKm,
                completedMinutes + evaluation.CumulativeMinutes, completedCost + evaluation.CumulativeCost, evaluation.TotalWeight,
                evaluation.TotalVolume, evaluation.HasApproximations, completedLegs.Concat(evaluation.Legs).ToList()));
            facts.Add(new Fact("route_km", vehicle.Code, completedKm + evaluation.CumulativeKm, "km"));
            facts.Add(new Fact("route_cost", vehicle.Code, completedCost + evaluation.CumulativeCost, "USD"));
        }
        return new PlanResult(routes, unserved.OrderBy(u => u.PointCode, StringComparer.Ordinal).ToList(), facts);
    }

    private static decimal WeightOf(IEnumerable<StopLoad> loads, IReadOnlyDictionary<Guid, Product> products) =>
        loads.Sum(l => products.TryGetValue(l.ProductId, out var p) ? l.Quantity * p.WeightPerBaseUnit : 0m);

    private static decimal VolumeOf(IEnumerable<StopLoad> loads, IReadOnlyDictionary<Guid, Product> products) =>
        loads.Sum(l => products.TryGetValue(l.ProductId, out var p) ? l.Quantity * p.VolumePerBaseUnit : 0m);

    /// <summary>
    /// #199: toma de las cargas, en su orden, lo que cabe en el peso y el volumen libres (null = sin límite).
    /// Una línea que no cabe completa se recorta a unidades base enteras.
    /// </summary>
    public static List<StopLoad> FitLoads(IEnumerable<StopLoad> loads, decimal? freeWeight, decimal? freeVolume,
        IReadOnlyDictionary<Guid, Product> products)
    {
        var fitted = new List<StopLoad>();
        foreach (var load in loads.Where(l => l.Quantity > 0))
        {
            products.TryGetValue(load.ProductId, out var product);
            var unitWeight = product?.WeightPerBaseUnit ?? 0m;
            var unitVolume = product?.VolumePerBaseUnit ?? 0m;
            var max = load.Quantity;
            if (freeWeight.HasValue && unitWeight > 0) max = Math.Min(max, Math.Max(0m, freeWeight.Value) / unitWeight);
            if (freeVolume.HasValue && unitVolume > 0) max = Math.Min(max, Math.Max(0m, freeVolume.Value) / unitVolume);
            var take = max >= load.Quantity ? load.Quantity : Math.Floor(max);
            if (take <= 0) continue;
            if (freeWeight.HasValue) freeWeight -= take * unitWeight;
            if (freeVolume.HasValue) freeVolume -= take * unitVolume;
            fitted.Add(load with { Quantity = take });
        }
        return fitted;
    }

    /// <summary>
    /// #199: reparte una parada obligatoria que no cabe completa entre los vehículos con capacidad libre.
    /// En cada vuelta gana el vehículo que lleva más unidades, luego el de menor costo adicional y luego el de
    /// menor código; un punto no se repite en la misma ruta. Devuelve cuántas partes insertó.
    /// </summary>
    private int SplitByCapacity(
        List<Vehicle> vehicles,
        Dictionary<string, List<StopCandidate>> currentRoutes,
        StopCandidate candidate,
        IReadOnlyDictionary<string, GeoPoint> depots,
        DateTime workdayStartUtc,
        IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg> distanceMatrix,
        IReadOnlyDictionary<Guid, Product> products)
    {
        var remaining = candidate.Loads.ToList();
        var parts = 0;
        while (remaining.Any(l => l.Quantity > 0))
        {
            (string Code, int Pos, StopCandidate Part, decimal Units, decimal Delta)? best = null;
            foreach (var v in vehicles)
            {
                var route = currentRoutes[v.Code];
                if (route.Any(s => s.ServicePointId == candidate.ServicePointId)) continue;

                decimal? freeWeight = v.MaxWeight > 0 ? v.MaxWeight - route.Sum(s => s.Weight) : null;
                decimal? freeVolume = v.MaxVolume is > 0 ? v.MaxVolume.Value - route.Sum(s => s.Volume) : null;
                var loads = FitLoads(remaining, freeWeight, freeVolume, products);
                if (loads.Count == 0) continue;

                var part = candidate with { Loads = loads, Weight = WeightOf(loads, products), Volume = VolumeOf(loads, products) };
                var units = loads.Sum(l => l.Quantity);
                var currentCost = RouteFeasibility.EvaluateRoute(
                    v, depots[v.Code], route, workdayStartUtc, _options.WorkdayHours,
                    _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands).CumulativeCost;

                for (int pos = 0; pos <= route.Count; pos++)
                {
                    var proposal = new List<StopCandidate>(route);
                    proposal.Insert(pos, part);
                    var eval = RouteFeasibility.EvaluateRoute(
                        v, depots[v.Code], proposal, workdayStartUtc, _options.WorkdayHours,
                        _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);
                    if (!eval.IsFeasible) continue;

                    var delta = eval.CumulativeCost - currentCost;
                    // Vehículos y posiciones se recorren en orden: con empate total gana el primero.
                    if (best is null || units > best.Value.Units || (units == best.Value.Units && delta < best.Value.Delta))
                    {
                        best = (v.Code, pos, part, units, delta);
                    }
                }
            }

            if (best is null) break;
            currentRoutes[best.Value.Code].Insert(best.Value.Pos, best.Value.Part);
            var taken = best.Value.Part.Loads.ToDictionary(l => l.ProductId, l => l.Quantity);
            remaining = remaining.Select(l => l with { Quantity = l.Quantity - taken.GetValueOrDefault(l.ProductId) }).ToList();
            parts++;
        }
        return parts;
    }

    // Posición de la bodega propia del vehículo; null si no tiene o la bodega no tiene coordenadas.
    private static GeoPoint? DepotOf(Vehicle vehicle, IReadOnlyList<Location> warehouses)
    {
        var depot = warehouses.FirstOrDefault(w => w.Id == vehicle.DepotLocationId);
        return depot?.Position is { } position ? new GeoPoint((decimal)position.Y, (decimal)position.X).Round() : null;
    }

    private bool TryInsertCheapest(
        List<Vehicle> vehicles,
        Dictionary<string, List<StopCandidate>> currentRoutes,
        StopCandidate candidate,
        IReadOnlyDictionary<string, GeoPoint> depots,
        DateTime workdayStartUtc,
        IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg> distanceMatrix,
        out string? bestVehicleCode,
        out int bestPos)
    {
        bestVehicleCode = null;
        bestPos = -1;
        decimal minCostDelta = decimal.MaxValue;

        foreach (var v in vehicles)
        {
            var route = currentRoutes[v.Code];
            var currentEval = RouteFeasibility.EvaluateRoute(
                v, depots[v.Code], route, workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);

            for (int pos = 0; pos <= route.Count; pos++)
            {
                var candidateRoute = new List<StopCandidate>(route);
                candidateRoute.Insert(pos, candidate);

                var newEval = RouteFeasibility.EvaluateRoute(
                    v, depots[v.Code], candidateRoute, workdayStartUtc, _options.WorkdayHours,
                    _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);

                if (newEval.IsFeasible)
                {
                    var delta = newEval.CumulativeCost - currentEval.CumulativeCost;
                    if (delta < minCostDelta)
                    {
                        minCostDelta = delta;
                        bestVehicleCode = v.Code;
                        bestPos = pos;
                    }
                    else if (delta == minCostDelta && bestVehicleCode != null)
                    {
                        // Empate: menor código de vehículo -> menor posición
                        if (string.CompareOrdinal(v.Code, bestVehicleCode) < 0 ||
                            (v.Code == bestVehicleCode && pos < bestPos))
                        {
                            bestVehicleCode = v.Code;
                            bestPos = pos;
                        }
                    }
                }
            }
        }

        return bestVehicleCode != null;
    }

    private bool TryInsertConsolidation(
        List<Vehicle> vehicles,
        Dictionary<string, List<StopCandidate>> currentRoutes,
        StopCandidate candidate,
        IReadOnlyDictionary<string, GeoPoint> depots,
        DateTime workdayStartUtc,
        IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg> distanceMatrix,
        int maxDetourMinutes,
        out string? bestVehicleCode,
        out int bestPos)
    {
        bestVehicleCode = null;
        bestPos = -1;
        decimal minDetourMinutes = decimal.MaxValue;

        foreach (var v in vehicles)
        {
            var route = currentRoutes[v.Code];
            var currentEval = RouteFeasibility.EvaluateRoute(
                v, depots[v.Code], route, workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);

            for (int pos = 0; pos <= route.Count; pos++)
            {
                var candidateRoute = new List<StopCandidate>(route);
                candidateRoute.Insert(pos, candidate);

                var newEval = RouteFeasibility.EvaluateRoute(
                    v, depots[v.Code], candidateRoute, workdayStartUtc, _options.WorkdayHours,
                    _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);

                if (newEval.IsFeasible)
                {
                    var detour = newEval.CumulativeMinutes - currentEval.CumulativeMinutes;
                    if (detour <= maxDetourMinutes && detour < minDetourMinutes)
                    {
                        minDetourMinutes = detour;
                        bestVehicleCode = v.Code;
                        bestPos = pos;
                    }
                }
            }
        }

        return bestVehicleCode != null;
    }

    /// <summary>Convierte "HH:mm" (o "HH:mm:ss") en hora local; null o vacío = sin hora límite.</summary>
    public static TimeOnly? ParseCriticalDeadline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (TimeOnly.TryParseExact(value.Trim(), new[] { "HH:mm", "HH:mm:ss" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline))
            return deadline;
        throw new ArgumentException("criticalDeadlineLocal debe tener formato HH:mm (p. ej. \"11:00\").",
            nameof(PlanRequest.CriticalDeadlineLocal));
    }

    /// <summary>
    /// Fin de ventana de una parada: para críticos es el mínimo entre la ventana del punto y la hora límite.
    /// Si la ventana del punto empieza después de la hora límite, se respeta la del punto.
    /// </summary>
    public static TimeOnly ApplyCriticalDeadline(TimeOnly windowStart, TimeOnly windowEnd, bool isCritical, TimeOnly? deadline)
    {
        if (!isCritical || deadline is not { } limit) return windowEnd;
        if (windowStart > limit) return windowEnd;
        return windowEnd < limit ? windowEnd : limit;
    }

    private string DetermineFailureReason(
        List<Vehicle> vehicles,
        Dictionary<string, List<StopCandidate>> currentRoutes,
        StopCandidate candidate,
        IReadOnlyDictionary<string, GeoPoint> depots,
        DateTime workdayStartUtc,
        IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg> distanceMatrix)
    {
        foreach (var v in vehicles)
        {
            var route = currentRoutes[v.Code];
            var candidateRoute = new List<StopCandidate>(route) { candidate };
            var eval = RouteFeasibility.EvaluateRoute(
                v, depots[v.Code], candidateRoute, workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix, trafficBands: _options.TrafficBands);

            if (!eval.IsFeasible && eval.FailureReason != null)
            {
                return eval.FailureReason;
            }
        }

        return "capacity";
    }
}
