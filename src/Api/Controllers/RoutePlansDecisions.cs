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
            IncludeAlternatives: true), ct);
        if (result is not OkObjectResult ok) return result;
        var suggestion = JsonSerializer.SerializeToElement(ok.Value, DecisionJson);
        var today = request.PlannedDate ?? await clock.TodayAsync(ct);
        // Las ventanas del evaluador están expresadas en hora de Ecuador (UTC-5).
        var start = DateTime.SpecifyKind(today.ToDateTime(new TimeOnly(13, 0)), DateTimeKind.Utc);
        var products = await db.Products.Where(p => p.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(p => p.Id, ct);
        var alternatives = new List<DecisionAlternative>();
        foreach (var option in suggestion.GetProperty("options").EnumerateArray())
        {
            var id = option.GetProperty("optionId").GetString()!;
            var contracted = option.GetProperty("carrierType").GetString() == "Contratada";
            var original = option.GetProperty("stops").Deserialize<List<RouteStopCandidate>>(DecisionJson)!;
            var sources = option.GetProperty("sourcingLines").Deserialize<List<DecisionSourceLine>>(DecisionJson)!;
            var routes = new List<DecisionRoute>();
            string? blocked = contracted ? "El operador contratado no admite confirmación operativa." : null;
            if (!contracted)
            {
                var groups = new List<(Vehicle Vehicle, List<RouteStopCandidate> Deliveries)>();
                foreach (var delivery in original.Where(s => s.Kind == RouteStopKind.Delivery && s.Loads?.Any(l => l.Quantity > 0) == true))
                {
                    var assigned = false;
                    string? reason = null;
                    for (var i = 0; i <= groups.Count; i++)
                    {
                        var deliveries = i < groups.Count ? groups[i].Deliveries.Append(delivery).ToList() : new List<RouteStopCandidate> { delivery };
                        var stops = BuildDecisionStops(original, sources, deliveries, products);
                        var used = groups.Where((_, index) => index != i).Select(g => g.Vehicle.Id).ToHashSet();
                        var matches = await vehicleMatcher.MatchVehiclesAsync(stops, start, ct);
                        var match = matches.FirstOrDefault(m => m.IsMatch && !used.Contains(m.Vehicle.Id)
                            && (i == groups.Count || m.Vehicle.Id == groups[i].Vehicle.Id));
                        if (match is null) { reason = matches.FirstOrDefault(m => !m.IsMatch)?.DiscardReason; continue; }
                        var evaluation = await routeEvaluator.EvaluateSequenceAsync(stops, match.Vehicle, start, ct);
                        if (!evaluation.IsValid) { reason = evaluation.ValidationErrors.FirstOrDefault(); continue; }
                        if (i < groups.Count) groups[i] = (match.Vehicle, deliveries);
                        else groups.Add((match.Vehicle, deliveries));
                        assigned = true;
                        break;
                    }
                    if (!assigned) { blocked = $"No hay un vehículo disponible con capacidad y horario para {delivery.Name}. {reason}"; break; }
                }
                if (blocked is null)
                    foreach (var group in groups)
                    {
                        var stops = BuildDecisionStops(original, sources, group.Deliveries, products);
                        var destinations = group.Deliveries.Select(s => s.ServicePointId ?? s.LocationId).ToHashSet();
                        var lines = sources.Where(l => destinations.Contains(l.DestinationId) && l.PlannedQuantity > 0).ToList();
                        var evaluation = await routeEvaluator.EvaluateSequenceAsync(stops, group.Vehicle, start, ct);
                        routes.Add(new DecisionRoute(Guid.NewGuid(), group.Vehicle.Id, group.Vehicle.Code,
                            group.Vehicle.MaxWeight, group.Vehicle.MaxVolume, stops, evaluation,
                            lines.Select(l => l.OrderId).Distinct().ToList(),
                            lines.Select(l => new ConfirmLineDto(l.OrderId, l.ProductId, l.PlannedQuantity)).ToList()));
                    }
                if (routes.Count == 0 && blocked is null) blocked = "No quedan cantidades disponibles para planificar.";
            }
            alternatives.Add(new DecisionAlternative(id, option.GetProperty("title").GetString()!,
                option.GetProperty("description").GetString() ?? "", option.GetProperty("isRecommended").GetBoolean(),
                contracted ? "Contratada" : "Propia", blocked, routes, sources,
                contracted ? option.GetProperty("totalCost").GetDecimal() : routes.Sum(r => r.Evaluation.TotalCost),
                contracted ? option.GetProperty("totalDistanceKm").GetDecimal() : routes.Sum(r => r.Evaluation.TotalDistanceKm),
                sources.Where(l => l.PendingQuantity > 0).Select(l => l.OrderId).Distinct().Count()));
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

    private static List<RouteStopCandidate> BuildDecisionStops(IReadOnlyList<RouteStopCandidate> original,
        IReadOnlyList<DecisionSourceLine> sources, IReadOnlyList<RouteStopCandidate> deliveries, Dictionary<Guid, Product> products)
    {
        var destinations = deliveries.Select(s => s.ServicePointId ?? s.LocationId).ToHashSet();
        var allocations = sources.Where(l => destinations.Contains(l.DestinationId))
            .SelectMany(l => l.Allocations.Select(a => new { l.ProductId, Allocation = a })).ToList();
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
            input["alternativeId"] = option.OptionId;
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
                status = decision.Status == "Accepted" ? decision.AcceptedOptionId == o.OptionId ? "Accepted" : "Discarded" : "Available" }) };
    }
}

public sealed record PrepareDecisionRequest(IReadOnlyList<Guid> OrderIds, DateOnly? PlannedDate = null, Guid? OperationId = null);
public sealed record AcceptDecisionRequest(string OptionId, int ExpectedVersion, Guid OperationId);
public sealed record PublishDecisionRequest(IReadOnlyDictionary<string, string> DriverAssignments, Guid OperationId);
public sealed record DecisionAllocation(Guid WarehouseId, decimal Quantity);
public sealed record DecisionSourceLine(Guid OrderId, Guid DestinationId, Guid ProductId, decimal PlannedQuantity,
    decimal PendingQuantity, IReadOnlyList<DecisionAllocation> Allocations);
public sealed record DecisionRoute(Guid Id, Guid VehicleId, string VehicleCode, decimal MaxWeight, decimal? MaxVolume,
    IReadOnlyList<RouteStopCandidate> Stops, RouteEvaluationResult Evaluation, IReadOnlyList<Guid> OrderIds, IReadOnlyList<ConfirmLineDto> Lines);
public sealed record DecisionAlternative(string OptionId, string Title, string Description, bool IsRecommended,
    string CarrierType, string? BlockedReason, IReadOnlyList<DecisionRoute> Routes, IReadOnlyList<DecisionSourceLine> Sources,
    decimal TotalCost, decimal TotalDistanceKm, int PendingOrdersCount);
public sealed record DecisionSnapshot(IReadOnlyList<Guid> OrderIds, DateTime PlannedStartUtc,
    IReadOnlyList<DecisionAlternative> Options, IReadOnlyDictionary<Guid, long> LineVersions);
