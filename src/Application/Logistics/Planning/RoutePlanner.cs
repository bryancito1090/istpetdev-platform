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

        // Bodega principal de salida
        var depotLoc = warehouseLocations.FirstOrDefault();
        var depotPos = depotLoc?.Position != null
            ? new GeoPoint((decimal)depotLoc.Position.Y, (decimal)depotLoc.Position.X)
            : new GeoPoint(-0.2295m, -78.5243m); // Default demo

        // 5. Preparar candidatos y clasificar en obligatorios y consolidación
        var candidates = new List<StopCandidate>();
        var unserved = new List<Unserved>();
        var facts = new List<Fact>();

        bool capacityDimensionEvaluated = true;

        foreach (var (servicePointId, alloc) in allocationResult.Allocations)
        {
            if (alloc.AllocatedQuantity <= 0) continue;
            if (!servicePoints.TryGetValue(servicePointId, out var sp)) continue;

            if (sp.Position == null)
            {
                unserved.Add(new Unserved(alloc.PointCode, "unreachable", Array.Empty<Fact>()));
                continue;
            }

            var pos = new GeoPoint((decimal)sp.Position.Y, (decimal)sp.Position.X);
            var riskItem = risks.FirstOrDefault(r => r.ServicePointId == servicePointId && r.ProductId == alloc.ProductId);
            var level = riskItem?.Level ?? RiskLevel.Normal;
            bool isConsolidation = level == RiskLevel.Vigilar;

            if (!request.IncludeConsolidation && isConsolidation) continue;

            // Peso y Volumen
            decimal weight = 0m;
            decimal volume = 0m;
            if (products.TryGetValue(alloc.ProductId, out var product))
            {
                if (product.WeightPerBaseUnit == 0 || product.VolumePerBaseUnit == 0)
                {
                    capacityDimensionEvaluated = false;
                }
                weight = alloc.AllocatedQuantity * product.WeightPerBaseUnit;
                volume = alloc.AllocatedQuantity * product.VolumePerBaseUnit;
            }

            var stopLoad = new StopLoad(alloc.ProductId, alloc.Sku, alloc.AllocatedQuantity, product?.BaseUnit ?? "U");
            var windowStart = sp.WindowStartLocal ?? TimeOnly.Parse(_options.DefaultWindowStart);
            var windowEnd = sp.WindowEndLocal ?? TimeOnly.Parse(_options.DefaultWindowEnd);
            var serviceMin = sp.ServiceMinutes ?? _options.DefaultServiceMinutes;

            candidates.Add(new StopCandidate(
                sp.Id, sp.Code, pos, windowStart, windowEnd, serviceMin,
                weight, volume, new[] { stopLoad }, isConsolidation,
                level, sp.Criticality, riskItem?.CoverageDays ?? 999m, riskItem?.LeadTimeDays ?? 2m));
        }

        // Agregar los unserved por falta de stock en bodega
        foreach (var unservedAlloc in allocationResult.Unserved)
        {
            unserved.Add(new Unserved(unservedAlloc.PointCode, unservedAlloc.Reason, unservedAlloc.Facts));
        }

        if (!capacityDimensionEvaluated)
        {
            facts.Add(new Fact("capacity_dimension_not_evaluated", "fleet", 0, "dimension"));
        }

        // 6. Matriz de distancias
        var allPoints = new List<GeoPoint> { depotPos };
        allPoints.AddRange(candidates.Select(c => c.Position));
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

        // Insertar obligatorios
        foreach (var candidate in mandatory)
        {
            bool inserted = TryInsertCheapest(vehicles, vehicleRoutes, candidate, depotPos, workdayStartUtc, distanceMatrix, out var bestVehicleCode, out var bestPos);
            if (inserted && bestVehicleCode != null)
            {
                vehicleRoutes[bestVehicleCode].Insert(bestPos, candidate);
            }
            else
            {
                // Determinar razón de fallo de inserción
                var reason = DetermineFailureReason(vehicles, vehicleRoutes, candidate, depotPos, workdayStartUtc, distanceMatrix);
                unserved.Add(new Unserved(candidate.PointCode, reason, Array.Empty<Fact>()));
            }
        }

        // 8. 2-opt intrarruta sobre obligatorios
        foreach (var v in vehicles)
        {
            vehicleRoutes[v.Code] = RouteImprover.TwoOpt(
                v, depotPos, vehicleRoutes[v.Code], workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix);
        }

        // 9. Consolidación de candidatos Vigilar si desvío <= MaxDetourMinutes
        foreach (var candidate in consolidation)
        {
            bool inserted = TryInsertConsolidation(
                vehicles, vehicleRoutes, candidate, depotPos, workdayStartUtc, distanceMatrix,
                _options.MaxDetourMinutes, out var bestVehicleCode, out var bestPos);

            if (inserted && bestVehicleCode != null)
            {
                vehicleRoutes[bestVehicleCode].Insert(bestPos, candidate);
                // 2-opt post inserción
                var v = vehicles.First(veh => veh.Code == bestVehicleCode);
                vehicleRoutes[bestVehicleCode] = RouteImprover.TwoOpt(
                    v, depotPos, vehicleRoutes[bestVehicleCode], workdayStartUtc, _options.WorkdayHours,
                    _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix);
            }
        }

        // 10. Construir resultado final
        var routes = new List<VehicleRoute>();
        foreach (var v in vehicles)
        {
            var stops = vehicleRoutes[v.Code];
            var eval = RouteFeasibility.EvaluateRoute(
                v, depotPos, stops, workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix);

            var plannedStops = new List<PlannedStop>();
            DateTime curTimeUtc = workdayStartUtc;
            GeoPoint curPos = depotPos;

            for (int i = 0; i < stops.Count; i++)
            {
                var stop = stops[i];
                var leg = distanceMatrix[(curPos, stop.Position)];
                curTimeUtc = curTimeUtc.AddMinutes((double)leg.Minutes);
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

                plannedStops.Add(new PlannedStop(
                    i + 1, stop.PointCode, stop.ServicePointId, arrUtc, depUtc, wait, stop.Loads, stop.IsConsolidation, late));
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
        var depot = await db.Locations.FirstAsync(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse, ct);
        var depotPos = new GeoPoint((decimal)depot.Position!.Y, (decimal)depot.Position.X).Round();
        var workdayStart = await clock.WorkdayStartUtcAsync(ct);
        var workdayEnd = workdayStart.AddHours(_options.WorkdayHours);
        var routes = new List<VehicleRoute>();
        var unserved = new List<Unserved>();
        var facts = new List<Fact>();
        foreach (var prefix in input.Prefixes.OrderBy(p => p.VehicleCode, StringComparer.Ordinal))
        {
            if (!vehicles.TryGetValue(prefix.VehicleCode, out var vehicle))
                throw new InvalidOperationException("El vehículo del prefijo no existe o está inactivo.");
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
                _options.DefaultCostPerKm, matrix, input.Effect, input.Mode, depotPos, workdayEnd);
            var selected = RouteImprover.TwoOpt(vehicle, position, candidates, prefix.ReadyAtUtc,
                _options.WorkdayHours, _options.CostPerHour, _options.DefaultCostPerKm, matrix,
                input.Effect, input.Mode, depotPos, workdayEnd);
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
                var minutes = matrix[(from, stop.Position)].Minutes *
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
                    stop.PointCode, stop.ServicePointId, arrival, time, wait, stop.Loads, stop.IsConsolidation, late));
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

    private bool TryInsertCheapest(
        List<Vehicle> vehicles,
        Dictionary<string, List<StopCandidate>> currentRoutes,
        StopCandidate candidate,
        GeoPoint depotPos,
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
                v, depotPos, route, workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix);

            for (int pos = 0; pos <= route.Count; pos++)
            {
                var candidateRoute = new List<StopCandidate>(route);
                candidateRoute.Insert(pos, candidate);

                var newEval = RouteFeasibility.EvaluateRoute(
                    v, depotPos, candidateRoute, workdayStartUtc, _options.WorkdayHours,
                    _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix);

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
        GeoPoint depotPos,
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
                v, depotPos, route, workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix);

            for (int pos = 0; pos <= route.Count; pos++)
            {
                var candidateRoute = new List<StopCandidate>(route);
                candidateRoute.Insert(pos, candidate);

                var newEval = RouteFeasibility.EvaluateRoute(
                    v, depotPos, candidateRoute, workdayStartUtc, _options.WorkdayHours,
                    _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix);

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

    private string DetermineFailureReason(
        List<Vehicle> vehicles,
        Dictionary<string, List<StopCandidate>> currentRoutes,
        StopCandidate candidate,
        GeoPoint depotPos,
        DateTime workdayStartUtc,
        IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg> distanceMatrix)
    {
        foreach (var v in vehicles)
        {
            var route = currentRoutes[v.Code];
            var candidateRoute = new List<StopCandidate>(route) { candidate };
            var eval = RouteFeasibility.EvaluateRoute(
                v, depotPos, candidateRoute, workdayStartUtc, _options.WorkdayHours,
                _options.CostPerHour, _options.DefaultCostPerKm, distanceMatrix);

            if (!eval.IsFeasible && eval.FailureReason != null)
            {
                return eval.FailureReason;
            }
        }

        return "capacity";
    }
}
