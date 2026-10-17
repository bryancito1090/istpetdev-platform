using System.Text.Json;
using System.Text.Json.Nodes;
using IstpetDev.Api.Authentication;
using IstpetDev.Api.Operations;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

public sealed partial class RoutePlansController
{
    private static readonly JsonSerializerOptions DecisionJson = new(JsonSerializerDefaults.Web);

    [HttpPost("decisions")]
    [Authorize(Roles = Roles.Planning)]
    [RetryableOperation("PrepareRouteDecision")]
    public async Task<IActionResult> PrepareDecision([FromBody] PrepareDecisionRequest request, CancellationToken ct)
    {
        var result = await BuildSuggestionAsync(new SuggestRoutesRequest(request.OrderIds, request.PlannedDate,
            IncludeAlternatives: true, PlanRemainingOnly: request.PlanRemainingOnly), ct);
        if (result is not OkObjectResult ok) return result;
        var suggestion = JsonSerializer.SerializeToElement(ok.Value, DecisionJson);
        var today = request.PlannedDate ?? await clock.TodayAsync(ct);
        // Las ventanas del evaluador están expresadas en hora de Ecuador (UTC-5).
        var start = DateTime.SpecifyKind(today.ToDateTime(new TimeOnly(13, 0)), DateTimeKind.Utc);
        var products = await db.Products.Where(p => p.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(p => p.Id, ct);
        var orders = await db.DeliveryOrders.AsNoTracking()
            .Where(o => o.OrganizationId == DemoOrganization.Id && request.OrderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, ct);
        var activeVehicles = await db.Vehicles.AsNoTracking()
            .Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive)
            .OrderBy(v => v.Code)
            .ToListAsync(ct);
        var alternatives = new List<DecisionAlternative>();
        foreach (var option in suggestion.GetProperty("options").EnumerateArray())
        {
            var id = option.GetProperty("optionId").GetString()!;
            var contracted = option.GetProperty("carrierType").GetString() == "Contratada";
            var original = option.GetProperty("stops").Deserialize<List<RouteStopCandidate>>(DecisionJson)!;
            var sources = option.GetProperty("sourcingLines").Deserialize<List<DecisionSourceLine>>(DecisionJson)!;
            var routes = new List<DecisionRoute>();
            string? blocked = contracted ? "El operador contratado no admite confirmación operativa." : null;
            string splitExplanation = contracted
                ? "Operador logístico externo contratado para cubrir la totalidad de las entregas sin emplear vehículos de la flota propia."
                : "";
            if (!contracted)
            {
                var partition = await PartitionFleetOptionAsync(id, original, sources, orders, products, activeVehicles, start, ct);
                routes = partition.Routes;
                blocked = partition.BlockedReason;
                splitExplanation = partition.SplitExplanation;
            }
            var baseTitle = option.GetProperty("title").GetString()!;
            var title = !contracted && routes.Count > 1 && !baseTitle.Contains("vehículos", StringComparison.OrdinalIgnoreCase)
                ? $"{baseTitle} · Plan Conjunto ({routes.Count} de {activeVehicles.Count} vehículos)"
                : baseTitle;
            var baseDesc = option.GetProperty("description").GetString() ?? "";
            var desc = !contracted && !string.IsNullOrWhiteSpace(splitExplanation)
                ? splitExplanation
                : baseDesc;
            alternatives.Add(new DecisionAlternative(id, title,
                desc, option.GetProperty("isRecommended").GetBoolean(),
                contracted ? "Contratada" : "Propia", blocked, routes, sources,
                contracted ? option.GetProperty("totalCost").GetDecimal() : routes.Sum(r => r.Evaluation.TotalCost),
                contracted ? option.GetProperty("totalDistanceKm").GetDecimal() : routes.Sum(r => r.Evaluation.TotalDistanceKm),
                sources.Where(l => l.PendingQuantity > 0).Select(l => l.OrderId).Distinct().Count(),
                activeVehicles.Count,
                routes.Count,
                splitExplanation));
        }
        var lineVersions = await db.DeliveryOrderLines.Where(l => request.OrderIds.Contains(l.DeliveryOrderId)
            && l.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(l => l.Id, l => l.Version, ct);
        var snapshot = new DecisionSnapshot(request.OrderIds, start, alternatives, lineVersions);
        var decision = new RoutePlanningDecision
        {
            CreatedByAccountId = User.AccountId()!.Value,
            SnapshotJson = JsonSerializer.Serialize(snapshot, DecisionJson)
        };
        db.RoutePlanningDecisions.Add(decision);
        await db.SaveChangesAsync(ct);
        return Ok(await DecisionViewAsync(decision, ct));
    }

    internal async Task<(List<DecisionRoute> Routes, string? BlockedReason, string SplitExplanation)> PartitionFleetOptionAsync(
        string optionId,
        IReadOnlyList<RouteStopCandidate> original,
        IReadOnlyList<DecisionSourceLine> sources,
        IReadOnlyDictionary<Guid, DeliveryOrder> orders,
        Dictionary<Guid, Product> products,
        IReadOnlyList<Vehicle> activeVehicles,
        DateTime start,
        CancellationToken ct)
    {
        var rawDeliveries = original
            .Where(s => s.Kind == RouteStopKind.Delivery && s.Loads?.Any(l => l.Quantity > 0) == true)
            .ToList();
        if (rawDeliveries.Count == 0)
            return ([], "No quedan cantidades disponibles para planificar.", "Sin entregas pendientes para asignar a la flota.");

        var deliveries = await ExpandOversizedDeliveriesAsync(original, sources, rawDeliveries, products, activeVehicles, start, ct);
        var greedy = await PackDeliveryBucketsAsync([deliveries], original, sources, products, start, preferRightSized: false, ct);

        List<(Vehicle Vehicle, List<RouteStopCandidate> Deliveries)>? chosenGroups = greedy.Groups;
        string? blocked = greedy.BlockedReason;

        bool isOptionB = !string.Equals(optionId, "A", StringComparison.OrdinalIgnoreCase);
        if (deliveries.Count >= 2 && activeVehicles.Count >= 2)
        {
            if (!isOptionB)
            {
                var twoBuckets = SplitDeliveriesByPriorityAndSize(deliveries, 2, orders, sources, prioritizeStrictTierCut: false);
                if (twoBuckets.Count >= 2)
                {
                    var packedTwo = await PackDeliveryBucketsAsync(twoBuckets, original, sources, products, start, preferRightSized: true, ct);
                    if (packedTwo.BlockedReason is null && (packedTwo.Groups.Count == 2 || chosenGroups is null || chosenGroups.Count < 2))
                    {
                        chosenGroups = packedTwo.Groups;
                        blocked = null;
                    }
                }
            }
            else if (isOptionB)
            {
                int targetBuckets = deliveries.Count >= 3 && activeVehicles.Count >= 3 ? 3 : 2;
                for (int bucketsCount = targetBuckets; bucketsCount >= 2; bucketsCount--)
                {
                    var buckets = SplitDeliveriesByPriorityAndSize(deliveries, bucketsCount, orders, sources, prioritizeStrictTierCut: true);
                    if (buckets.Count < bucketsCount) continue;
                    var packed = await PackDeliveryBucketsAsync(buckets, original, sources, products, start, preferRightSized: true, ct);
                    if (packed.BlockedReason is null && packed.Groups.Count >= bucketsCount)
                    {
                        chosenGroups = packed.Groups;
                        blocked = null;
                        break;
                    }
                }
            }
        }

        if (blocked is not null || chosenGroups is null || chosenGroups.Count == 0)
            return ([], blocked ?? "No quedan cantidades disponibles para planificar.", blocked ?? "");

        var remainingLineQuantities = sources
            .Where(l => l.PlannedQuantity > 0)
            .ToDictionary(l => (l.OrderId, l.DestinationId, l.ProductId), l => l.PlannedQuantity);

        var routes = new List<DecisionRoute>();
        int totalFleet = Math.Max(activeVehicles.Count, chosenGroups.Count);
        for (int idx = 0; idx < chosenGroups.Count; idx++)
        {
            var group = chosenGroups[idx];
            var stops = BuildDecisionStops(original, sources, group.Deliveries, products);
            var lines = AllocateGroupConfirmLines(sources, group.Deliveries, remainingLineQuantities);
            var evaluation = await routeEvaluator.EvaluateSequenceAsync(stops, group.Vehicle, start, ct);

            decimal loadWeight = Math.Round(group.Deliveries.Sum(d => Math.Abs(d.DeltaWeight)), 2);
            decimal loadVolume = Math.Round(group.Deliveries.Sum(d => Math.Abs(d.DeltaVolume)), 3);
            decimal weightPct = group.Vehicle.MaxWeight > 0
                ? Math.Round(loadWeight / group.Vehicle.MaxWeight * 100m, 1)
                : 0m;
            decimal volumePct = group.Vehicle.MaxVolume is > 0
                ? Math.Round(loadVolume / group.Vehicle.MaxVolume.Value * 100m, 1)
                : 0m;

            string prioritySummary = BuildPrioritySummary(group.Deliveries, orders, sources);
            string assignmentReason = BuildVehicleAssignmentReason(
                group.Vehicle, group.Deliveries.Count, loadWeight, weightPct, prioritySummary, totalFleet, idx + 1, chosenGroups.Count);
            string cutReason = BuildVehicleCutReason(
                idx, chosenGroups, orders, sources, loadWeight, weightPct, prioritySummary, totalFleet);

            routes.Add(new DecisionRoute(
                Guid.NewGuid(),
                group.Vehicle.Id,
                group.Vehicle.Code,
                group.Vehicle.MaxWeight,
                group.Vehicle.MaxVolume,
                stops,
                evaluation,
                lines.Select(l => l.DeliveryOrderId).Distinct().ToList(),
                lines,
                loadWeight,
                loadVolume,
                weightPct,
                volumePct,
                prioritySummary,
                assignmentReason,
                cutReason));
        }

        string splitExplanation = BuildPlanSplitExplanation(optionId, routes, deliveries, orders, sources, totalFleet);
        return (routes, null, splitExplanation);
    }

    private async Task<List<RouteStopCandidate>> ExpandOversizedDeliveriesAsync(
        IReadOnlyList<RouteStopCandidate> original,
        IReadOnlyList<DecisionSourceLine> sources,
        IReadOnlyList<RouteStopCandidate> deliveries,
        Dictionary<Guid, Product> products,
        IReadOnlyList<Vehicle> activeVehicles,
        DateTime start,
        CancellationToken ct)
    {
        if (activeVehicles.Count <= 1) return deliveries.ToList();
        var expanded = new List<RouteStopCandidate>();
        foreach (var delivery in deliveries)
        {
            var singleStops = BuildDecisionStops(original, sources, [delivery], products);
            var matches = await vehicleMatcher.MatchVehiclesAsync(singleStops, start, ct);
            if (matches.Any(m => m.IsMatch))
            {
                expanded.Add(delivery);
                continue;
            }

            var remaining = (delivery.Loads ?? []).Where(l => l.Quantity > 0).ToList();
            var parts = new List<RouteStopCandidate>();
            foreach (var v in activeVehicles.OrderByDescending(v => v.MaxWeight).ThenBy(v => v.Code))
            {
                if (remaining.Count == 0) break;
                var fitted = FitStopLoads(remaining, v.MaxWeight > 0 ? v.MaxWeight : null, v.MaxVolume is > 0 ? v.MaxVolume : null, products);
                if (fitted.Count == 0) continue;
                decimal partWeight = fitted.Sum(l => products.TryGetValue(l.ProductId, out var p) ? l.Quantity * p.WeightPerBaseUnit : 0m);
                decimal partVolume = fitted.Sum(l => products.TryGetValue(l.ProductId, out var p) ? l.Quantity * p.VolumePerBaseUnit : 0m);
                parts.Add(delivery with
                {
                    Loads = fitted,
                    DeltaWeight = -partWeight,
                    DeltaVolume = -partVolume
                });
                var taken = fitted.ToDictionary(l => l.ProductId, l => l.Quantity);
                remaining = remaining
                    .Select(l => l with { Quantity = l.Quantity - taken.GetValueOrDefault(l.ProductId) })
                    .Where(l => l.Quantity > 0)
                    .ToList();
            }

            if (remaining.Count == 0 && parts.Count > 1) expanded.AddRange(parts);
            else expanded.Add(delivery);
        }
        return expanded;
    }

    private static List<StopLoadDto> FitStopLoads(
        IEnumerable<StopLoadDto> loads,
        decimal? freeWeight,
        decimal? freeVolume,
        IReadOnlyDictionary<Guid, Product> products)
    {
        var fitted = new List<StopLoadDto>();
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

    private async Task<(List<(Vehicle Vehicle, List<RouteStopCandidate> Deliveries)> Groups, string? BlockedReason)> PackDeliveryBucketsAsync(
        IReadOnlyList<List<RouteStopCandidate>> buckets,
        IReadOnlyList<RouteStopCandidate> original,
        IReadOnlyList<DecisionSourceLine> sources,
        Dictionary<Guid, Product> products,
        DateTime start,
        bool preferRightSized,
        CancellationToken ct)
    {
        var allGroups = new List<(Vehicle Vehicle, List<RouteStopCandidate> Deliveries)>();
        foreach (var bucket in buckets.Where(b => b.Count > 0))
        {
            var bucketGroups = new List<(Vehicle Vehicle, List<RouteStopCandidate> Deliveries)>();
            decimal bucketTotalWeight = bucket.Sum(d => Math.Abs(d.DeltaWeight));
            foreach (var delivery in bucket)
            {
                var assigned = false;
                string? reason = null;
                for (var i = 0; i <= bucketGroups.Count; i++)
                {
                    var deliveries = i < bucketGroups.Count
                        ? bucketGroups[i].Deliveries.Append(delivery).ToList()
                        : new List<RouteStopCandidate> { delivery };
                    var stops = BuildDecisionStops(original, sources, deliveries, products);
                    var used = allGroups.Select(g => g.Vehicle.Id)
                        .Concat(bucketGroups.Where((_, index) => index != i).Select(g => g.Vehicle.Id))
                        .ToHashSet();
                    var matches = await vehicleMatcher.MatchVehiclesAsync(stops, start, ct);
                    var candidates = matches
                        .Where(m => m.IsMatch && !used.Contains(m.Vehicle.Id)
                            && (i == bucketGroups.Count || m.Vehicle.Id == bucketGroups[i].Vehicle.Id || preferRightSized))
                        .ToList();
                    if (preferRightSized && candidates.Count > 1)
                    {
                        decimal targetWeight = i == bucketGroups.Count && bucketGroups.Count == 0
                            ? bucketTotalWeight
                            : deliveries.Sum(d => Math.Abs(d.DeltaWeight));
                        candidates = candidates
                            .OrderBy(m => m.Vehicle.MaxWeight >= targetWeight ? 0 : 1)
                            .ThenBy(m => m.Vehicle.MaxWeight > 0 ? m.Vehicle.MaxWeight : decimal.MaxValue)
                            .ThenBy(m => m.EstimatedTotalCost)
                            .ToList();
                    }

                    VehicleMatchResult? validMatch = null;
                    foreach (var candidate in candidates)
                    {
                        var evaluation = await routeEvaluator.EvaluateSequenceAsync(stops, candidate.Vehicle, start, ct);
                        if (evaluation.IsValid)
                        {
                            validMatch = candidate;
                            break;
                        }
                        reason = evaluation.ValidationErrors.FirstOrDefault();
                    }

                    if (validMatch is null)
                    {
                        reason ??= matches.FirstOrDefault(m => !m.IsMatch)?.DiscardReason;
                        continue;
                    }

                    if (i < bucketGroups.Count) bucketGroups[i] = (validMatch.Vehicle, deliveries);
                    else bucketGroups.Add((validMatch.Vehicle, deliveries));
                    assigned = true;
                    break;
                }

                if (!assigned)
                    return ([], $"No hay un vehículo disponible con capacidad y horario para {delivery.Name}. {reason}");
            }
            allGroups.AddRange(bucketGroups);
        }
        return (allGroups, null);
    }

    private static List<List<RouteStopCandidate>> SplitDeliveriesByPriorityAndSize(
        IReadOnlyList<RouteStopCandidate> deliveries,
        int targetBuckets,
        IReadOnlyDictionary<Guid, DeliveryOrder> orders,
        IReadOnlyList<DecisionSourceLine> sources,
        bool prioritizeStrictTierCut)
    {
        if (targetBuckets <= 1 || deliveries.Count < targetBuckets)
            return [deliveries.ToList()];

        var annotated = deliveries
            .Select(d => (Stop: d, Rank: GetStopPriorityRank(d, orders, sources), Weight: Math.Max(1m, Math.Abs(d.DeltaWeight))))
            .ToList();

        var distinctRanks = annotated.Select(a => a.Rank).Distinct().OrderBy(r => r).ToList();
        if (targetBuckets == 2)
        {
            if (distinctRanks.Count >= 2)
            {
                int splitRank = prioritizeStrictTierCut
                    ? distinctRanks[0]
                    : distinctRanks[Math.Min(distinctRanks.Count - 2, 0)];
                var first = annotated.Where(a => a.Rank <= splitRank).Select(a => a.Stop).ToList();
                var second = annotated.Where(a => a.Rank > splitRank).Select(a => a.Stop).ToList();
                if (first.Count > 0 && second.Count > 0)
                    return [first, second];
            }
            return SplitByBalancedWeight(annotated.Select(a => (a.Stop, a.Weight)).ToList(), 2);
        }

        if (targetBuckets == 3)
        {
            if (distinctRanks.Count >= 3)
            {
                var b1 = annotated.Where(a => a.Rank == distinctRanks[0]).Select(a => a.Stop).ToList();
                var b2 = annotated.Where(a => a.Rank == distinctRanks[1]).Select(a => a.Stop).ToList();
                var b3 = annotated.Where(a => a.Rank >= distinctRanks[2]).Select(a => a.Stop).ToList();
                if (b1.Count > 0 && b2.Count > 0 && b3.Count > 0)
                    return [b1, b2, b3];
            }
            if (distinctRanks.Count == 2)
            {
                var urgent = annotated.Where(a => a.Rank == distinctRanks[0]).ToList();
                var normal = annotated.Where(a => a.Rank > distinctRanks[0]).ToList();
                if (urgent.Count >= 2 && normal.Count >= 1)
                {
                    var urgentSplit = SplitByBalancedWeight(urgent.Select(a => (a.Stop, a.Weight)).ToList(), 2);
                    return [urgentSplit[0], urgentSplit[1], normal.Select(a => a.Stop).ToList()];
                }
                if (urgent.Count >= 1 && normal.Count >= 2)
                {
                    var normalSplit = SplitByBalancedWeight(normal.Select(a => (a.Stop, a.Weight)).ToList(), 2);
                    return [urgent.Select(a => a.Stop).ToList(), normalSplit[0], normalSplit[1]];
                }
            }
            return SplitByBalancedWeight(annotated.Select(a => (a.Stop, a.Weight)).ToList(), 3);
        }

        return [deliveries.ToList()];
    }

    private static List<List<RouteStopCandidate>> SplitByBalancedWeight(
        IReadOnlyList<(RouteStopCandidate Stop, decimal Weight)> items,
        int bucketsCount)
    {
        var result = new List<List<RouteStopCandidate>>();
        int remainingBuckets = bucketsCount;
        int index = 0;
        while (remainingBuckets > 1 && index < items.Count)
        {
            decimal remainingWeight = items.Skip(index).Sum(x => x.Weight);
            decimal target = remainingWeight / remainingBuckets;
            var current = new List<RouteStopCandidate>();
            decimal acc = 0m;
            int maxTake = items.Count - index - (remainingBuckets - 1);
            for (int take = 0; take < maxTake; take++)
            {
                var item = items[index + take];
                if (current.Count > 0 && Math.Abs((acc + item.Weight) - target) > Math.Abs(acc - target))
                    break;
                current.Add(item.Stop);
                acc += item.Weight;
            }
            if (current.Count == 0) current.Add(items[index].Stop);
            index += current.Count;
            result.Add(current);
            remainingBuckets--;
        }
        if (index < items.Count)
            result.Add(items.Skip(index).Select(x => x.Stop).ToList());
        return result.Where(b => b.Count > 0).ToList();
    }

    private static int GetStopPriorityRank(
        RouteStopCandidate stop,
        IReadOnlyDictionary<Guid, DeliveryOrder> orders,
        IReadOnlyList<DecisionSourceLine> sources)
    {
        var destId = stop.ServicePointId ?? stop.LocationId;
        var priorities = sources
            .Where(l => l.DestinationId == destId && l.PlannedQuantity > 0)
            .Select(l => orders.GetValueOrDefault(l.OrderId)?.Priority ?? "Normal")
            .ToList();
        var mostUrgent = priorities.Count > 0 ? DeliveryStopSequencer.MostUrgent(priorities) : "Normal";
        return DeliveryStopSequencer.PriorityRank(mostUrgent);
    }

    private static string TranslatePriorityLabel(int rank) => rank switch
    {
        0 => "Crítica",
        1 => "Alta",
        2 => "Normal",
        _ => "Baja"
    };

    private static string BuildPrioritySummary(
        IReadOnlyList<RouteStopCandidate> deliveries,
        IReadOnlyDictionary<Guid, DeliveryOrder> orders,
        IReadOnlyList<DecisionSourceLine> sources)
    {
        var groups = deliveries
            .GroupBy(d => GetStopPriorityRank(d, orders, sources))
            .OrderBy(g => g.Key)
            .Select(g => $"{TranslatePriorityLabel(g.Key)} ({g.Count()})")
            .ToList();
        return groups.Count > 0 ? string.Join(", ", groups) : "Normal (1)";
    }

    private static string BuildVehicleAssignmentReason(
        Vehicle vehicle,
        int deliveriesCount,
        decimal loadWeight,
        decimal weightPct,
        string prioritySummary,
        int totalFleet,
        int vehicleIndex,
        int usedVehicles)
    {
        string volumeInfo = vehicle.MaxVolume is > 0 ? $" / {vehicle.MaxVolume:0.#} m³" : "";
        return $"Vehículo {vehicleIndex} de {usedVehicles} ({vehicle.Code}, tamaño {vehicle.MaxWeight:0.#} kg{volumeInfo}): "
            + $"elegido entre {totalFleet} activos para llevar {deliveriesCount} entrega(s) de prioridad {prioritySummary} "
            + $"con {loadWeight:0.#} kg ({weightPct:0.#}% de capacidad).";
    }

    private static string BuildVehicleCutReason(
        int index,
        IReadOnlyList<(Vehicle Vehicle, List<RouteStopCandidate> Deliveries)> groups,
        IReadOnlyDictionary<Guid, DeliveryOrder> orders,
        IReadOnlyList<DecisionSourceLine> sources,
        decimal loadWeight,
        decimal weightPct,
        string prioritySummary,
        int totalFleet)
    {
        var current = groups[index];
        var lastStop = current.Deliveries[^1];
        if (index < groups.Count - 1)
        {
            var next = groups[index + 1];
            var nextStop = next.Deliveries[0];
            decimal nextStopWeight = Math.Round(Math.Abs(nextStop.DeltaWeight), 2);
            int curRank = current.Deliveries.Min(d => GetStopPriorityRank(d, orders, sources));
            int nextRank = next.Deliveries.Min(d => GetStopPriorityRank(d, orders, sources));
            string nextPriority = TranslatePriorityLabel(GetStopPriorityRank(nextStop, orders, sources));

            if (current.Vehicle.MaxWeight > 0 && loadWeight + nextStopWeight > current.Vehicle.MaxWeight)
            {
                return $"Corta en {lastStop.Name} con {loadWeight:0.#}/{current.Vehicle.MaxWeight:0.#} kg ({weightPct:0.#}%): "
                    + $"sumar {nextStop.Name} ({nextStopWeight:0.#} kg, prioridad {nextPriority}) superaría el tamaño de {current.Vehicle.Code}; "
                    + $"toma el relevo {next.Vehicle.Code} ({next.Vehicle.MaxWeight:0.#} kg).";
            }

            if (curRank != nextRank)
            {
                return $"Corta tras {lastStop.Name} ({loadWeight:0.#}/{current.Vehicle.MaxWeight:0.#} kg, {weightPct:0.#}%) por prioridad ({prioritySummary}): "
                    + $"despacha sin demora el bloque {TranslatePriorityLabel(curRank)} y delega prioridad {TranslatePriorityLabel(nextRank)} ({nextStop.Name}) "
                    + $"a {next.Vehicle.Code} ({next.Vehicle.MaxWeight:0.#} kg).";
            }

            return $"Corta en {lastStop.Name} ({loadWeight:0.#}/{current.Vehicle.MaxWeight:0.#} kg, {weightPct:0.#}%) para dividir entregas en paralelo "
                + $"según tamaño y ventana horaria con {next.Vehicle.Code} ({next.Vehicle.MaxWeight:0.#} kg).";
        }

        int reserve = Math.Max(0, totalFleet - groups.Count);
        if (groups.Count > 1)
        {
            return $"Cierra el plan conjunto en {lastStop.Name} ({loadWeight:0.#}/{current.Vehicle.MaxWeight:0.#} kg, {weightPct:0.#}%) "
                + $"completando el bloque de prioridad {prioritySummary}; {reserve} vehículo(s) quedan en reserva.";
        }

        return $"Único vehículo requerido ({loadWeight:0.#}/{current.Vehicle.MaxWeight:0.#} kg, {weightPct:0.#}%): "
            + $"cubre todas las entregas ({prioritySummary}) sin requerir corte adicional ({reserve} vehículo(s) en reserva).";
    }

    private static string BuildPlanSplitExplanation(
        string optionId,
        IReadOnlyList<DecisionRoute> routes,
        IReadOnlyList<RouteStopCandidate> deliveries,
        IReadOnlyDictionary<Guid, DeliveryOrder> orders,
        IReadOnlyList<DecisionSourceLine> sources,
        int totalFleet)
    {
        decimal totalWeight = Math.Round(routes.Sum(r => r.LoadWeight), 2);
        string overallPriority = BuildPrioritySummary(deliveries, orders, sources);
        int reserve = Math.Max(0, totalFleet - routes.Count);

        if (routes.Count == 1)
        {
            var r = routes[0];
            return $"Plan con 1 de {totalFleet} vehículos disponibles ({r.VehicleCode}, capacidad {r.MaxWeight:0.#} kg, ocupación {r.WeightUtilizationPct:0.#}%) "
                + $"para {deliveries.Count} entrega(s) ({totalWeight:0.#} kg, prioridad {overallPriority}). "
                + $"No requiere cortar en más vehículos porque el tamaño de {r.VehicleCode} y las ventanas horarias cubren todo el lote; {reserve} vehículo(s) quedan en reserva.";
        }

        var vehicleRoles = string.Join("; ", routes.Select((r, i) =>
            $"{r.VehicleCode} ({r.MaxWeight:0.#} kg, uso {r.WeightUtilizationPct:0.#}%) lleva {r.Stops.Count(s => s.Kind == RouteStopKind.Delivery)} entrega(s) [{r.PrioritySummary}]"));

        var cutsSummary = string.Join(" ", routes.Take(routes.Count - 1).Select(r => r.CutReason));
        string strategyLabel = string.Equals(optionId, "A", StringComparison.OrdinalIgnoreCase)
            ? $"divide la carga en {routes.Count} vehículos optimizando capacidad y tamaño"
            : $"usa {routes.Count} vehículos escalonados por nivel de prioridad y tamaño";

        return $"Plan conjunto que involucra {routes.Count} de {totalFleet} vehículos disponibles trabajando en equipo ({strategyLabel}) "
            + $"para {deliveries.Count} entrega(s) ({totalWeight:0.#} kg totales, prioridades: {overallPriority}): {vehicleRoles}. "
            + $"Motivo de corte: {cutsSummary} Los {reserve} vehículo(s) restantes permanecen en reserva.";
    }

    private static List<ConfirmLineDto> AllocateGroupConfirmLines(
        IReadOnlyList<DecisionSourceLine> sources,
        IReadOnlyList<RouteStopCandidate> deliveries,
        Dictionary<(Guid OrderId, Guid DestinationId, Guid ProductId), decimal> remainingLineQuantities)
    {
        var groupDemandByDestProd = deliveries
            .GroupBy(d => d.ServicePointId ?? d.LocationId)
            .Where(g => g.Key.HasValue)
            .SelectMany(g => g.SelectMany(d => d.Loads ?? [])
                .GroupBy(l => l.ProductId)
                .Select(pg => (DestId: g.Key!.Value, ProductId: pg.Key, Quantity: pg.Sum(x => x.Quantity))))
            .ToDictionary(x => (x.DestId, x.ProductId), x => x.Quantity);

        var result = new List<ConfirmLineDto>();
        foreach (var line in sources.Where(l => l.PlannedQuantity > 0))
        {
            if (!groupDemandByDestProd.TryGetValue((line.DestinationId, line.ProductId), out var neededForStop) || neededForStop <= 0)
                continue;
            var key = (line.OrderId, line.DestinationId, line.ProductId);
            var availableInLine = remainingLineQuantities.GetValueOrDefault(key, 0m);
            if (availableInLine <= 0) continue;

            var take = Math.Min(availableInLine, neededForStop);
            if (take <= 0) continue;

            result.Add(new ConfirmLineDto(line.OrderId, line.ProductId, take));
            remainingLineQuantities[key] = availableInLine - take;
            groupDemandByDestProd[(line.DestinationId, line.ProductId)] = neededForStop - take;
        }
        return result;
    }

    private static List<RouteStopCandidate> BuildDecisionStops(IReadOnlyList<RouteStopCandidate> original,
        IReadOnlyList<DecisionSourceLine> sources, IReadOnlyList<RouteStopCandidate> deliveries, Dictionary<Guid, Product> products)
    {
        var deliveredByDestAndProd = deliveries
            .GroupBy(s => s.ServicePointId ?? s.LocationId)
            .Where(g => g.Key.HasValue)
            .SelectMany(g => g.SelectMany(d => d.Loads ?? [])
                .GroupBy(l => l.ProductId)
                .Select(pg => (DestId: g.Key!.Value, ProductId: pg.Key, Quantity: pg.Sum(x => x.Quantity))))
            .ToDictionary(x => (x.DestId, x.ProductId), x => x.Quantity);

        var allocations = new List<(Guid ProductId, DecisionAllocation Allocation)>();
        foreach (var group in sources.Where(l => deliveredByDestAndProd.ContainsKey((l.DestinationId, l.ProductId)))
                     .GroupBy(l => (l.DestinationId, l.ProductId)))
        {
            decimal remainingToPickup = deliveredByDestAndProd[group.Key];
            foreach (var alloc in group.SelectMany(l => l.Allocations))
            {
                if (remainingToPickup <= 0) break;
                decimal take = Math.Min(remainingToPickup, alloc.Quantity);
                if (take > 0)
                {
                    allocations.Add((group.Key.ProductId, alloc with { Quantity = take }));
                    remainingToPickup -= take;
                }
            }
        }

        var stops = new List<RouteStopCandidate>();
        foreach (var pickup in original.Where(s => s.Kind == RouteStopKind.Pickup))
        {
            var loads = allocations.Where(a => a.Allocation.WarehouseId == pickup.LocationId)
                .GroupBy(a => a.ProductId).Select(g => new StopLoadDto(g.Key, products[g.Key].Sku,
                    g.Sum(a => a.Allocation.Quantity), products[g.Key].BaseUnit)).Where(l => l.Quantity > 0).ToList();
            if (loads.Count > 0) stops.Add(pickup with { Sequence = stops.Count + 1, Loads = loads,
                DeltaWeight = loads.Sum(l => l.Quantity * products[l.ProductId].WeightPerBaseUnit),
                DeltaVolume = loads.Sum(l => l.Quantity * products[l.ProductId].VolumePerBaseUnit) });
        }
        stops.AddRange(deliveries.Select((s, i) => s with { Sequence = stops.Count + i + 1 }));
        return stops;
    }

    [HttpGet("decisions/{id:guid}")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Auditor)]
    public async Task<IActionResult> GetDecision(Guid id, CancellationToken ct)
    {
        var decision = await db.RoutePlanningDecisions.AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == id && d.OrganizationId == DemoOrganization.Id, ct);
        return decision is null ? NotFound() : Ok(await DecisionViewAsync(decision, ct));
    }

    [HttpPost("decisions/{id:guid}/accept")]
    [Authorize(Roles = Roles.Planning)]
    [RetryableOperation("AcceptRouteDecision")]
    public async Task<IActionResult> AcceptDecision(Guid id, [FromBody] AcceptDecisionRequest request, CancellationToken ct)
    {
        var decision = await db.RoutePlanningDecisions.SingleOrDefaultAsync(d => d.Id == id && d.OrganizationId == DemoOrganization.Id, ct);
        if (decision is null) return NotFound();
        if (decision.Status == "Accepted") return decision.AcceptedOptionId == request.OptionId
            ? Ok(await DecisionViewAsync(decision, ct)) : Conflict(new { message = "Ya se aceptó otra alternativa de este cálculo. Actualiza su estado." });
        if (decision.Version != request.ExpectedVersion) return Conflict(new { message = "La propuesta cambió. Vuelve a calcular." });
        var snapshot = JsonSerializer.Deserialize<DecisionSnapshot>(decision.SnapshotJson, DecisionJson)!;
        var option = snapshot.Options.FirstOrDefault(o => o.OptionId == request.OptionId);
        if (option is null || option.BlockedReason is not null || option.Routes.Count == 0)
            return Conflict(new { message = option?.BlockedReason ?? "La alternativa no se puede aceptar." });
        var currentVersions = await db.DeliveryOrderLines.Where(l => snapshot.OrderIds.Contains(l.DeliveryOrderId)
            && l.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(l => l.Id, l => l.Version, ct);
        if (currentVersions.Count != snapshot.LineVersions.Count || snapshot.LineVersions.Any(v => currentVersions.GetValueOrDefault(v.Key) != v.Value))
            return Conflict(new { message = "Las cantidades de los pedidos cambiaron. Vuelve a calcular antes de aceptar." });
        var freshSourcing = (await sourcingService.PlanSourcingAsync(snapshot.OrderIds, ct)).FirstOrDefault(o => o.OptionId == option.OptionId);
        if (freshSourcing is null) return Conflict(new { message = "Cambió el abastecimiento. Vuelve a calcular." });
        var available = freshSourcing.Lines.SelectMany(l => l.Allocations.Select(a => new { l.ProductId, a.WarehouseId, a.Quantity }))
            .GroupBy(a => (a.ProductId, a.WarehouseId)).ToDictionary(g => g.Key, g => g.Sum(a => a.Quantity));
        if (option.Sources.SelectMany(l => l.Allocations.Select(a => new { l.ProductId, a.WarehouseId, a.Quantity }))
            .GroupBy(a => (a.ProductId, a.WarehouseId)).Any(g => available.GetValueOrDefault(g.Key) < g.Sum(a => a.Quantity)))
            return Conflict(new { message = "El stock disponible cambió. Vuelve a calcular antes de aceptar." });
        var vehiclesInOption = option.Routes.Select(r => r.VehicleId).ToList();
        var committed = await db.RoutePlans.Where(p => p.OrganizationId == DemoOrganization.Id && p.Status == "Proposed"
            && vehiclesInOption.Contains(p.VehicleId) && p.PlannedStartUtc.Date == snapshot.PlannedStartUtc.Date).ToListAsync(ct);
        if (committed.Any(p => JsonNode.Parse(p.InputJson)?["planningDecisionId"] is not null))
            return Conflict(new { message = "Un vehículo ya está comprometido en otra alternativa aceptada. Vuelve a calcular." });
        // Validar todo antes de guardar; la transacción de #287 incluye cantidades, elección e idempotencia.
        foreach (var route in option.Routes)
        {
            var matches = await vehicleMatcher.MatchVehiclesAsync(route.Stops, snapshot.PlannedStartUtc, ct);
            var match = matches.FirstOrDefault(m => m.IsMatch && m.Vehicle.Id == route.VehicleId);
            if (match is null) return Conflict(new { message = $"El vehículo {route.VehicleCode} ya no está disponible. Vuelve a calcular." });
            var evaluation = await routeEvaluator.EvaluateSequenceAsync(route.Stops, match.Vehicle, snapshot.PlannedStartUtc, ct);
            if (!evaluation.IsValid || Math.Round(evaluation.TotalCost, 2) != Math.Round(route.Evaluation.TotalCost, 2))
                return Conflict(new { message = "La evaluación o el costo cambió. Revisa un nuevo cálculo antes de aceptar." });
        }
        var ids = new List<Guid>();
        foreach (var route in option.Routes)
        {
            var result = await ConfirmRoutePlanCoreAsync(new ConfirmRoutePlanRequest(null, route.VehicleId,
                route.Evaluation.TotalDistanceKm, route.Evaluation.TotalDurationMinutes, route.Evaluation.TotalCost,
                snapshot.PlannedStartUtc, route.OrderIds, route.Stops, null, route.Evaluation.Coordinates,
                route.Lines, route.Id, 1), ct, preservePickupLoads: true);
            if (result is not ObjectResult saved || saved.StatusCode != 201) return result;
            var routeId = JsonSerializer.SerializeToElement(saved.Value, DecisionJson).GetProperty("id").GetGuid();
            ids.Add(routeId);
            var plan = await db.RoutePlans.SingleAsync(p => p.Id == routeId, ct);
            var input = JsonNode.Parse(plan.InputJson)!.AsObject();
            input["planningDecisionId"] = id;
            input["proposalBatchId"] = id;
            input["alternativeId"] = option.OptionId;
            input["totalAvailableVehicles"] = option.TotalAvailableVehicles;
            input["usedVehiclesCount"] = option.Routes.Count;
            input["splitExplanation"] = option.SplitExplanation;
            input["assignmentReason"] = route.AssignmentReason;
            input["cutReason"] = route.CutReason;
            input["prioritySummary"] = route.PrioritySummary;
            input["loadWeight"] = route.LoadWeight;
            input["loadVolume"] = route.LoadVolume;
            input["maxWeight"] = route.MaxWeight;
            input["maxVolume"] = route.MaxVolume;
            input["weightUtilizationPct"] = route.WeightUtilizationPct;
            input["volumeUtilizationPct"] = route.VolumeUtilizationPct;
            plan.InputJson = input.ToJsonString();
        }
        await RouteProposalAcceptance.AcceptAsync(db, ids, ct);
        decision.Status = "Accepted";
        decision.AcceptedOptionId = option.OptionId;
        decision.RouteIdsJson = JsonSerializer.Serialize(ids);
        await db.SaveChangesAsync(ct);
        return Ok(await DecisionViewAsync(decision, ct));
    }

    [HttpPost("decisions/{id:guid}/publish")]
    [Authorize(Roles = Roles.Planning)]
    [RetryableOperation("PublishRouteDecision")]
    public async Task<IActionResult> PublishDecision(Guid id, [FromBody] PublishDecisionRequest request, CancellationToken ct)
    {
        var decision = await db.RoutePlanningDecisions.SingleOrDefaultAsync(d => d.Id == id && d.OrganizationId == DemoOrganization.Id, ct);
        if (decision?.Status != "Accepted") return Conflict(new { message = "Primero acepta una alternativa de este cálculo." });
        var ids = JsonSerializer.Deserialize<List<Guid>>(decision.RouteIdsJson)!;
        var plans = await db.RoutePlans.Where(p => ids.Contains(p.Id) && p.OrganizationId == DemoOrganization.Id).ToListAsync(ct);
        var vehicles = await db.Vehicles.Where(v => plans.Select(p => p.VehicleId).Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.Code, ct);
        if (plans.Count != ids.Count || plans.Any(p => p.Status is not ("Proposed" or "Published")))
            return Conflict(new { message = "El conjunto cambió de estado. Revisa sus rutas antes de publicar." });
        if (plans.All(p => p.Status == "Published")) return Ok(await DecisionViewAsync(decision, ct));
        foreach (var route in JsonSerializer.Deserialize<DecisionSnapshot>(decision.SnapshotJson, DecisionJson)!.Options.Single(o => o.OptionId == decision.AcceptedOptionId).Routes)
        {
            var matches = await vehicleMatcher.MatchVehiclesAsync(route.Stops, plans[0].PlannedStartUtc, ct);
            if (!matches.Any(m => m.IsMatch && m.Vehicle.Id == route.VehicleId))
                return Conflict(new { message = $"El vehículo {route.VehicleCode} dejó de estar disponible. No se publicó ninguna ruta." });
        }
        foreach (var code in plans.Select(p => vehicles[p.VehicleId]))
            if (!request.DriverAssignments.TryGetValue(code, out var driver) || !Guid.TryParse(driver, out var driverId)
                || !await db.Accounts.AnyAsync(a => a.Id == driverId && a.OrganizationId == DemoOrganization.Id && a.Role == Roles.Conductor && a.IsActive, ct))
                return Conflict(new { message = $"Asigna un conductor activo a {code} antes de publicar el conjunto." });
        if (request.DriverAssignments.Where(a => plans.Any(p => vehicles[p.VehicleId] == a.Key)).Select(a => a.Value).Distinct().Count() != plans.Count)
            return Conflict(new { message = "Los vehículos que operan en paralelo necesitan conductores distintos." });
        try
        {
            foreach (var plan in plans) await publisher.PublishAsync(new PublishRequest(plan.Id, request.DriverAssignments, User.AccountId()), ct);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        { return Conflict(new { message = ex.Message }); }
        return Ok(await DecisionViewAsync(decision, ct));
    }

    private async Task<object> DecisionViewAsync(RoutePlanningDecision decision, CancellationToken ct)
    {
        var snapshot = JsonSerializer.Deserialize<DecisionSnapshot>(decision.SnapshotJson, DecisionJson)!;
        var ids = JsonSerializer.Deserialize<List<Guid>>(decision.RouteIdsJson)!;
        var published = ids.Count > 0 && await db.RoutePlans.CountAsync(p => ids.Contains(p.Id) && p.Status == "Published" && p.OrganizationId == DemoOrganization.Id, ct) == ids.Count;
        return new { published, id = decision.Id, version = decision.Version, status = decision.Status, decision.AcceptedOptionId,
            routeIds = JsonSerializer.Deserialize<List<Guid>>(decision.RouteIdsJson), snapshot.PlannedStartUtc,
            snapshot.OrderIds, options = snapshot.Options.Select(o => new { o.OptionId, o.Title, o.Description,
                o.IsRecommended, o.CarrierType, o.BlockedReason, o.Routes, o.TotalCost, o.TotalDistanceKm, o.PendingOrdersCount,
                o.TotalAvailableVehicles, o.UsedVehiclesCount, o.SplitExplanation,
                status = decision.Status == "Accepted" ? decision.AcceptedOptionId == o.OptionId ? "Accepted" : "Discarded" : "Available" }) };
    }
}

public sealed record PrepareDecisionRequest(IReadOnlyList<Guid> OrderIds, DateOnly? PlannedDate = null, Guid? OperationId = null,
    bool PlanRemainingOnly = false);
public sealed record AcceptDecisionRequest(string OptionId, int ExpectedVersion, Guid OperationId);
public sealed record PublishDecisionRequest(IReadOnlyDictionary<string, string> DriverAssignments, Guid OperationId);
public sealed record DecisionAllocation(Guid WarehouseId, decimal Quantity);
public sealed record DecisionSourceLine(Guid OrderId, Guid DestinationId, Guid ProductId, decimal PlannedQuantity,
    decimal PendingQuantity, IReadOnlyList<DecisionAllocation> Allocations);
public sealed record DecisionRoute(Guid Id, Guid VehicleId, string VehicleCode, decimal MaxWeight, decimal? MaxVolume,
    IReadOnlyList<RouteStopCandidate> Stops, RouteEvaluationResult Evaluation, IReadOnlyList<Guid> OrderIds, IReadOnlyList<ConfirmLineDto> Lines,
    decimal LoadWeight = 0m, decimal LoadVolume = 0m, decimal WeightUtilizationPct = 0m, decimal VolumeUtilizationPct = 0m,
    string PrioritySummary = "", string AssignmentReason = "", string CutReason = "");
public sealed record DecisionAlternative(string OptionId, string Title, string Description, bool IsRecommended,
    string CarrierType, string? BlockedReason, IReadOnlyList<DecisionRoute> Routes, IReadOnlyList<DecisionSourceLine> Sources,
    decimal TotalCost, decimal TotalDistanceKm, int PendingOrdersCount,
    int TotalAvailableVehicles = 0, int UsedVehiclesCount = 0, string SplitExplanation = "");
public sealed record DecisionSnapshot(IReadOnlyList<Guid> OrderIds, DateTime PlannedStartUtc,
    IReadOnlyList<DecisionAlternative> Options, IReadOnlyDictionary<Guid, long> LineVersions);
