using IstpetDev.Application.Common;
using System.Text.Json;
using System.Text.Json.Nodes;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Application.Logistics.Policy;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record ReplanResult(
    Guid PreviousRoutePlanId,
    int PreviousRevision,
    Guid ProposedRoutePlanId,
    int ProposedRevision,
    string Status,
    ReplanDiff Diff,
    PolicyDecision Decision,
    IReadOnlyList<EscalationOption>? Options,
    IReadOnlyList<Fact> Facts,
    string Explanation,
    Guid? AgentDecisionId = null);

public sealed record ReplanDiff(
    IReadOnlyList<string> MovedStops,
    IReadOnlyList<string> DroppedStops,
    IReadOnlyDictionary<string, int> EtaChangesMinutes,
    decimal CostDeltaPct);

public sealed record ApproveOptionRequest(
    Guid AgentDecisionId,
    string OptionKey, // "late_service", "prioritize_critical"
    Guid? DecidedByAccountId = null);

public sealed record RevertPlanRequest(
    Guid RoutePlanId,
    Guid? RequestedByAccountId = null);

public sealed class ReplanConflictException(string message) : Exception(message);

public interface IReplanner
{
    Task<ReplanResult> ReplanAsync(Guid routePlanId, Guid incidentId, CancellationToken ct = default);
    Task<RoutePlan> ApproveDecisionOptionAsync(ApproveOptionRequest request, CancellationToken ct = default);
    Task<AgentDecision> RejectDecisionAsync(Guid agentDecisionId, Guid? decidedByAccountId = null, CancellationToken ct = default);
    Task<RoutePlan> RevertPlanAsync(RevertPlanRequest request, CancellationToken ct = default);
}

public sealed class Replanner(
    IApplicationDbContext db,
    IOperationalClock clock,
    IIncidentEvaluator incidentEvaluator,
    IRoutePlanner routePlanner,
    IAutonomyPolicy autonomyPolicy,
    AutonomyState autonomyState,
    IRoutePublisher routePublisher,
    IExplanationTemplates explanationTemplates,
    IOptions<LogisticsOptions> options) : IReplanner
{
    private readonly LogisticsOptions _options = options.Value;

    public async Task<ReplanResult> ReplanAsync(Guid routePlanId, Guid incidentId, CancellationToken ct = default)
    {
        await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
        var plan = await db.RoutePlans
            .FirstOrDefaultAsync(p => p.Id == routePlanId && p.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"RoutePlan {routePlanId} no encontrado.");
        if (await db.RoutePlans.AnyAsync(p => p.SupersedesRoutePlanId == plan.Id && p.OrganizationId == DemoOrganization.Id, ct))
            throw new ReplanConflictException("La ruta ya tiene una revisión; consulta la revisión vigente antes de replanificar.");

        var impact = await incidentEvaluator.EvaluateAsync(incidentId, ct);
        var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId && i.OrganizationId == DemoOrganization.Id, ct);
        var today = await clock.TodayAsync(ct);

        // 1. Construir prefijo fijo para cada ruta publicada
        var stops = await db.RouteStops
            .Where(s => s.RoutePlanId == plan.Id && s.OrganizationId == DemoOrganization.Id)
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync(ct);

        var spMap = await db.ServicePoints
            .Where(s => s.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(s => s.Id, s => s, ct);

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.OrganizationId == DemoOrganization.Id && v.Id == plan.VehicleId, ct)
            ?? throw new InvalidOperationException($"Vehículo {plan.VehicleId} no encontrado.");

        // #300: las recogidas en bodega (y entregas a bodega) del asistente no son puntos de servicio y no se reoptimizan.
        // Una recogida cuenta como hecha si se marcó o si ya se completó una parada posterior.
        var locationIds = stops.Where(s => s.LocationId.HasValue).Select(s => s.LocationId!.Value).Distinct().ToList();
        var locations = await db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, ct);
        var lastCompletedSequence = stops.Where(s => s.Status == RouteStopStatus.Completed)
            .Select(s => s.SequenceNumber).DefaultIfEmpty(int.MinValue).Max();
        bool IsDone(RouteStop s) => IsDoneStatus(s.Status) ||
            (s.Kind == RouteStopKind.Pickup && s.SequenceNumber < lastCompletedSequence);
        GeoPoint? LocationPosition(RouteStop s) =>
            s.LocationId is { } id && locations.TryGetValue(id, out var l) && l.Position != null
                ? new GeoPoint((decimal)l.Position.Y, (decimal)l.Position.X) : null;
        string LocationCode(RouteStop s) =>
            s.LocationId is { } id && locations.TryGetValue(id, out var l) ? l.Code : "BOD";

        var completedStops = stops.Where(IsDone).ToList();
        var pendingStops = stops.Where(s => !IsDone(s)).ToList();
        var pendingPickups = pendingStops.Where(s => s.Kind == RouteStopKind.Pickup).ToList();
        var pendingLocationDeliveries = pendingStops.Where(s => s.Kind != RouteStopKind.Pickup && IsLocationStop(s)).ToList();
        var pendingServiceStops = pendingStops.Where(s => !IsLocationStop(s)).ToList();

        var depotLoc = await db.Locations
            .FirstOrDefaultAsync(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse, ct);
        var depotPos = depotLoc?.Position != null
            ? new GeoPoint((decimal)depotLoc.Position.Y, (decimal)depotLoc.Position.X)
            : new GeoPoint(-0.2295m, -78.5243m);

        GeoPoint lastPos = depotPos;
        DateTime readyAtUtc = await clock.WorkdayStartUtcAsync(ct);

        if (completedStops.Count > 0 && IsLocationStop(completedStops.Last()))
        {
            // #300: la última parada hecha es una bodega: el vehículo parte desde ella.
            var lastStop = completedStops.Last();
            lastPos = LocationPosition(lastStop) ?? lastPos;
            var arrival = lastStop.ActualArrivalUtc ?? lastStop.PlannedArrivalUtc ?? DateTime.UtcNow;
            readyAtUtc = arrival.AddMinutes(ServiceMinutes(lastStop));
        }
        else if (completedStops.Count > 0)
        {
            var lastStop = completedStops.Last();
            var sp = spMap[lastStop.ServicePointId];
            if (sp.Position != null)
            {
                lastPos = new GeoPoint((decimal)sp.Position.Y, (decimal)sp.Position.X);
            }
            var arrival = lastStop.ActualArrivalUtc ?? lastStop.PlannedArrivalUtc ?? DateTime.UtcNow;
            var windowStart = arrival.AddHours(-5).Date
                .Add((sp.WindowStartLocal ?? TimeOnly.Parse(_options.DefaultWindowStart)).ToTimeSpan()).AddHours(5);
            readyAtUtc = (arrival < windowStart ? windowStart : arrival)
                .AddMinutes(sp.ServiceMinutes ?? _options.DefaultServiceMinutes);
        }

        bool vehicleAvailable = !impact.Effect.UnavailableVehicles.Contains(vehicle.Code);

        var prefix = new FixedPrefix(
            vehicle.Code,
            completedStops.Where(s => !IsLocationStop(s)).Select(s => spMap[s.ServicePointId].Code).ToList(),
            lastPos,
            readyAtUtc,
            pendingServiceStops.Select(s => spMap[s.ServicePointId].Code).ToList(),
            vehicleAvailable)
        {
            RoutePlanId = plan.Id,
            LeadingStops = pendingPickups
                .Select(s => new FixedStop(LocationCode(s), LocationPosition(s) ?? lastPos, ServiceMinutes(s))).ToList(),
            CompletedStopCount = completedStops.Count
        };

        // 2. Ejecutar ReplanAsync(Strict)
        var replanInput = new ReplanInput(new[] { prefix }, impact.Effect, ReplanMode.Strict);
        var strictResult = await routePlanner.ReplanAsync(replanInput, ct);

        var proposedRoute = strictResult.Routes.FirstOrDefault(r => r.VehicleCode == vehicle.Code)
            ?? strictResult.Routes.FirstOrDefault()
            ?? new VehicleRoute(vehicle.Code, vehicle.Id, Array.Empty<PlannedStop>(), 0m, 0m, 0m, 0m, 0m, false, Array.Empty<Leg>());

        // Reconstruir VehicleRoute previa para comparar
        var prevRoute = ReconstructPreviousVehicleRoute(plan, vehicle.Code, stops.Where(s => !IsLocationStop(s)).ToList(), spMap);

        // #300: las recogidas pendientes se conservan si quedan entregas; las entregas a bodega, si el vehículo sigue disponible.
        var keepPickups = proposedRoute.Stops.Count > 0;
        var keptLocationDeliveries = vehicleAvailable ? pendingLocationDeliveries : [];

        // 3. Evaluar política de autonomía
        var mode = autonomyState.CurrentMode;
        var decision = autonomyPolicy.Classify(prevRoute, proposedRoute, MandatoryUnserved(strictResult), mode);

        // 4. Calcular diff
        var prevArrivals = prevRoute.Stops.ToDictionary(s => s.PointCode, s => s.ArrivalUtc);
        var etaChanges = new Dictionary<string, int>();
        foreach (var ps in proposedRoute.Stops)
        {
            if (prevArrivals.TryGetValue(ps.PointCode, out var pa))
            {
                int diffMin = (int)Math.Round((ps.ArrivalUtc - pa).TotalMinutes);
                if (diffMin != 0) etaChanges[ps.PointCode] = diffMin;
            }
        }

        decimal costDeltaPct = 0m;
        if (prevRoute.Cost > 0)
        {
            costDeltaPct = Math.Round(((proposedRoute.Cost - prevRoute.Cost) / prevRoute.Cost) * 100m, 2);
        }

        var movedStops = proposedRoute.Stops
            .Where(s => prevArrivals.ContainsKey(s.PointCode) && etaChanges.ContainsKey(s.PointCode))
            .Select(s => s.PointCode)
            .ToList();

        var droppedStops = strictResult.Unserved.Select(u => u.PointCode)
            .Concat(pendingLocationDeliveries.Except(keptLocationDeliveries).Select(LocationCode)).ToList();
        var diff = new ReplanDiff(movedStops, droppedStops, etaChanges, costDeltaPct);

        // 5. Opciones si escala
        List<EscalationOption>? optionsList = null;
        if (decision.Outcome == PolicyOutcome.Escalated)
        {
            optionsList = new List<EscalationOption>();

            // Opción late_service
            var lateInput = new ReplanInput(new[] { prefix }, impact.Effect, ReplanMode.LateService);
            var lateResult = await routePlanner.ReplanAsync(lateInput, ct);
            var lateRoute = lateResult.Routes.FirstOrDefault();
            var lateStopsMap = lateRoute?.Stops
                .Where(s => s.LateMinutes.HasValue && s.LateMinutes > 0)
                .ToDictionary(s => s.PointCode, s => s.LateMinutes!.Value) ?? new Dictionary<string, int>();

            optionsList.Add(new EscalationOption(
                "late_service",
                lateResult.Routes.Count > 0 && MandatoryUnserved(lateResult).Count == 0,
                lateResult.Routes.Count == 0 || MandatoryUnserved(lateResult).Count > 0 ? "infeasible" : null,
                lateResult.Routes,
                lateStopsMap,
                Array.Empty<string>(),
                lateRoute?.Cost ?? 0m));

            // Opción prioritize_critical
            var critInput = new ReplanInput(new[] { prefix }, impact.Effect, ReplanMode.PrioritizeCritical);
            var critResult = await routePlanner.ReplanAsync(critInput, ct);
            var critRoute = critResult.Routes.FirstOrDefault();
            var droppedConsolidation = critResult.Unserved.Where(u => u.Facts.Any(f => f.Code == "is_consolidation" && f.Value == 1))
                .Select(u => u.PointCode).ToList();

            optionsList.Add(new EscalationOption(
                "prioritize_critical",
                critResult.Routes.Count > 0 && MandatoryUnserved(critResult).Count == 0,
                critResult.Routes.Count == 0 || MandatoryUnserved(critResult).Count > 0 ? "infeasible" : null,
                critResult.Routes,
                new Dictionary<string, int>(),
                droppedConsolidation,
                critRoute?.Cost ?? 0m));
        }

        // 6. Guardar nueva revisión RoutePlan (Revision + 1, SupersedesRoutePlanId = plan.Id)
        var existingMaxRevision = await db.RoutePlans
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.Code == plan.Code)
            .MaxAsync(p => (int?)p.Revision, ct) ?? plan.Revision;
        var nextRevision = Math.Max(plan.Revision + 1, existingMaxRevision + 1);

        var newPlan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = plan.Code,
            Revision = nextRevision,
            SupersedesRoutePlanId = plan.Id,
            VehicleId = vehicle.Id,
            Status = "Proposed",
            ProposedBy = "System:IncidentReplanner",
            PlannedStartUtc = plan.PlannedStartUtc,
            PlannedEndUtc = plan.PlannedEndUtc,
            TotalKm = proposedRoute.Km,
            TotalMinutes = proposedRoute.Minutes,
            TotalCost = proposedRoute.Cost,
            AlgorithmVersion = "2.0-deterministic"
        };
        db.RoutePlans.Add(newPlan);

        // Guardar paradas de la nueva revisión. #300: cada parada conserva tipo, bodega y cargas; el InputJson
        // guarda las pendientes con la misma secuencia que su RouteStop (la publicación busca las cargas por secuencia).
        var originalStops = OriginalStopsBySequence(plan.InputJson);
        var newRouteStops = new List<RouteStop>();
        var inputStops = new List<object>();
        int nextSeq = 1;
        RouteStop AddStop(RouteStop source, string status, DateTime? arrival, DateTime? departure)
        {
            var stop = new RouteStop
            {
                OrganizationId = DemoOrganization.Id,
                RoutePlanId = newPlan.Id,
                ServicePointId = source.ServicePointId,
                LocationId = source.LocationId,
                Kind = source.Kind,
                SequenceNumber = nextSeq++,
                PlannedArrivalUtc = arrival,
                PlannedDepartureUtc = departure,
                ActualArrivalUtc = status == RouteStopStatus.Pending ? null : source.ActualArrivalUtc,
                Status = status
            };
            newRouteStops.Add(stop);
            return stop;
        }
        void AddKeptInput(RouteStop source, RouteStop stop) => inputStops.Add(new
        {
            sequence = stop.SequenceNumber,
            kind = stop.Kind,
            pointCode = LocationCode(source),
            code = LocationCode(source),
            servicePointId = (Guid?)null,
            locationId = stop.LocationId,
            isConsolidation = false,
            loads = originalStops.GetValueOrDefault(source.SequenceNumber)?["loads"]?.DeepClone()
        });

        foreach (var cs in completedStops)
        {
            AddStop(cs, IsDoneStatus(cs.Status) ? cs.Status : RouteStopStatus.Completed, cs.PlannedArrivalUtc, cs.PlannedDepartureUtc);
        }

        if (keepPickups)
        {
            for (var i = 0; i < pendingPickups.Count; i++)
            {
                var pickup = pendingPickups[i];
                var times = i < proposedRoute.LeadingTimes.Count
                    ? proposedRoute.LeadingTimes[i]
                    : (ArrivalUtc: pickup.PlannedArrivalUtc ?? DateTime.UtcNow, DepartureUtc: pickup.PlannedDepartureUtc ?? DateTime.UtcNow);
                AddKeptInput(pickup, AddStop(pickup, RouteStopStatus.Pending, times.ArrivalUtc, times.DepartureUtc));
            }
        }

        var completedServicePointIds = completedStops.Where(c => !IsLocationStop(c)).Select(c => c.ServicePointId).ToHashSet();
        foreach (var ps in proposedRoute.Stops.Where(s => !completedServicePointIds.Contains(s.ServicePointId)))
        {
            var stop = AddStop(new RouteStop { ServicePointId = ps.ServicePointId, Kind = RouteStopKind.Delivery },
                RouteStopStatus.Pending, ps.ArrivalUtc, ps.DepartureUtc);
            inputStops.Add(new
            {
                sequence = stop.SequenceNumber,
                kind = stop.Kind,
                pointCode = ps.PointCode,
                code = ps.PointCode,
                servicePointId = (Guid?)ps.ServicePointId,
                locationId = (Guid?)null,
                isConsolidation = ps.IsConsolidation,
                loads = (object?)ps.Loads,
                trafficFactorApplied = ps.TrafficFactorApplied
            });
        }

        // Las entregas a bodega no pasan por el optimizador: se mantienen al final con sus cargas.
        foreach (var delivery in keptLocationDeliveries)
        {
            AddKeptInput(delivery, AddStop(delivery, RouteStopStatus.Pending, delivery.PlannedArrivalUtc, delivery.PlannedDepartureUtc));
        }
        db.RouteStops.AddRange(newRouteStops);
        newPlan.InputJson = JsonSerializer.Serialize(new { stops = inputStops, legs = proposedRoute.Legs },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var replanFacts = decision.Facts.ToList();
        foreach (var missing in strictResult.Unserved)
        {
            replanFacts.AddRange(missing.Facts);
            replanFacts.Add(new Fact("unserved_reason", missing.PointCode, 1, missing.Reason));
        }
        replanFacts.Add(new Fact("cost_increase_pct", "route", diff.CostDeltaPct, "%"));
        if (diff.CostDeltaPct < 0)
        {
            replanFacts.Add(new Fact("cost_reduction_pct", "route", Math.Abs(diff.CostDeltaPct), "%"));
        }
        replanFacts.Add(new Fact("max_delay_minutes", "route", diff.EtaChangesMinutes.Values.DefaultIfEmpty(0).Max(), "min"));
        foreach (var (stopCode, etaMin) in diff.EtaChangesMinutes)
        {
            replanFacts.Add(new Fact("eta_change_minutes", stopCode, etaMin, "min"));
            if (etaMin < 0)
            {
                replanFacts.Add(new Fact("advance_minutes", stopCode, Math.Abs(etaMin), "min"));
            }
            else
            {
                replanFacts.Add(new Fact("delay_minutes", stopCode, etaMin, "min"));
            }
        }
        replanFacts.Add(new Fact("total_km", proposedRoute.VehicleCode, Math.Round(proposedRoute.Km, 2), "km"));
        replanFacts.Add(new Fact("total_minutes", proposedRoute.VehicleCode, Math.Round(proposedRoute.Minutes, 2), "min"));
        replanFacts.Add(new Fact("total_cost", proposedRoute.VehicleCode, Math.Round(proposedRoute.Cost, 2), "USD"));
        foreach (var stop in proposedRoute.Stops)
        {
            replanFacts.Add(new Fact("arrival_time", stop.PointCode, (decimal)stop.ArrivalUtc.TimeOfDay.TotalHours, "hora"));
            replanFacts.Add(new Fact("arrival_time", stop.PointCode, (decimal)stop.ArrivalUtc.TimeOfDay.TotalMinutes, "min"));
        }
        if (incident?.RadiusKm.HasValue == true)
        {
            replanFacts.Add(new Fact("incident_radius_km", "incident", incident.RadiusKm.Value, "km"));
        }
        if (incident?.Position != null)
        {
            replanFacts.Add(new Fact("incident_lat", "incident", Math.Round((decimal)incident.Position.Y, 4), "deg"));
            replanFacts.Add(new Fact("incident_lon", "incident", Math.Round((decimal)incident.Position.X, 4), "deg"));
        }
        replanFacts.Add(new Fact("revision", proposedRoute.VehicleCode, newPlan.Revision, "revision"));
        replanFacts.Add(new Fact("previous_revision", proposedRoute.VehicleCode, plan.Revision, "revision"));
        replanFacts.Add(new Fact("moved_stops_count", proposedRoute.VehicleCode, diff.MovedStops.Count, "stops"));
        replanFacts.Add(new Fact("dropped_stops_count", proposedRoute.VehicleCode, diff.DroppedStops.Count, "stops"));
        replanFacts.Add(new Fact("stops_count", proposedRoute.VehicleCode, newRouteStops.Count, "stops"));
        for (int i = 1; i <= newRouteStops.Count + 4; i++)
        {
            replanFacts.Add(new Fact("stop_sequence", $"stop_{i}", i, "sequence"));
        }

        // Guardar AgentDecision
        var agentDecision = new AgentDecision
        {
            OrganizationId = DemoOrganization.Id,
            Kind = "REPLAN_CLASSIFICATION",
            Status = decision.Outcome.ToString(),
            ProposalJson = JsonSerializer.Serialize(new
            {
                previousRoutePlanId = plan.Id,
                proposedRoutePlanId = newPlan.Id,
                revision = newPlan.Revision,
                options = optionsList,
                facts = replanFacts
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true }),
            Rationale = string.Join("; ", decision.Reasons),
            CorrelationId = newPlan.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.AgentDecisions.Add(agentDecision);

        await db.SaveChangesAsync(ct);

        // Si es AutoExecuted, publicar automáticamente
        if (decision.Outcome == PolicyOutcome.AutoExecuted)
        {
            await routePublisher.PublishAsync(new PublishRequest(newPlan.Id), ct);
        }

        var explanation = explanationTemplates.Explain(
            decision.Outcome == PolicyOutcome.AutoExecuted ? "replan" : "escalation",
            replanFacts);

        if (transaction is not null) await transaction.CommitAsync(ct);

        return new ReplanResult(
            plan.Id,
            plan.Revision,
            newPlan.Id,
            newPlan.Revision,
            newPlan.Status,
            diff,
            decision,
            optionsList,
            replanFacts,
            explanation,
            agentDecision.Id);
    }

    private static IReadOnlyList<Unserved> MandatoryUnserved(PlanResult result) => result.Unserved
        .Where(u => !u.Facts.Any(f => f.Code == "is_consolidation" && f.Value == 1)).ToList();

    public async Task<RoutePlan> ApproveDecisionOptionAsync(ApproveOptionRequest request, CancellationToken ct = default)
    {
        await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
        var result = await ApproveDecisionOptionAsyncCore(request, ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return result;
    }

    private async Task<RoutePlan> ApproveDecisionOptionAsyncCore(ApproveOptionRequest request, CancellationToken ct)
    {
        var decision = await db.AgentDecisions
            .FirstOrDefaultAsync(d => d.Id == request.AgentDecisionId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"AgentDecision {request.AgentDecisionId} no encontrada.");

        using var proposal = JsonDocument.Parse(decision.ProposalJson);
        var proposedRoutePlanId = proposal.RootElement.GetProperty("proposedRoutePlanId").GetGuid();

        if (decision.Status == "Approved")
        {
            // Idempotencia: ya aprobada, devolver el plan asociado
            var existingPlan = await db.RoutePlans.FirstAsync(p => p.OrganizationId == DemoOrganization.Id && p.Id == proposedRoutePlanId, ct);
            return existingPlan;
        }

        decision.Status = "Approved";
        decision.DecidedByAccountId = request.DecidedByAccountId;
        decision.DecidedAtUtc = DateTime.UtcNow;

        var plan = await db.RoutePlans.FirstAsync(p => p.OrganizationId == DemoOrganization.Id && p.Id == proposedRoutePlanId, ct);
        await routePublisher.PublishAsync(new PublishRequest(plan.Id, null, request.DecidedByAccountId), ct);

        await db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<AgentDecision> RejectDecisionAsync(Guid agentDecisionId, Guid? decidedByAccountId = null, CancellationToken ct = default)
    {
        var decision = await db.AgentDecisions
            .FirstOrDefaultAsync(d => d.Id == agentDecisionId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"AgentDecision {agentDecisionId} no encontrada.");

        if (decision.Status == "Rejected")
        {
            return decision;
        }

        decision.Status = "Rejected";
        decision.DecidedByAccountId = decidedByAccountId;
        decision.DecidedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return decision;
    }

    public async Task<RoutePlan> RevertPlanAsync(RevertPlanRequest request, CancellationToken ct = default)
    {
        await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
        var result = await RevertPlanAsyncCore(request, ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return result;
    }

    private async Task<RoutePlan> RevertPlanAsyncCore(RevertPlanRequest request, CancellationToken ct)
    {
        var currentPlan = await db.RoutePlans
            .FirstOrDefaultAsync(p => p.Id == request.RoutePlanId && p.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"RoutePlan {request.RoutePlanId} no encontrado.");

        if (!currentPlan.SupersedesRoutePlanId.HasValue)
        {
            throw new InvalidOperationException("No se puede revertir un plan inicial sin revisión previa.");
        }

        var previousPlan = await db.RoutePlans
            .FirstOrDefaultAsync(p => p.OrganizationId == DemoOrganization.Id && p.Id == currentPlan.SupersedesRoutePlanId.Value, ct)
            ?? throw new InvalidOperationException("Revisión previa a restaurar no encontrada.");

        // Idempotencia: Verificar si ya existe una revisión restaurada que apunte a este plan
        var existingRestored = await db.RoutePlans
            .FirstOrDefaultAsync(p => p.OrganizationId == DemoOrganization.Id && p.SupersedesRoutePlanId == currentPlan.Id && p.Code == currentPlan.Code && p.Status == "Published", ct);
        if (existingRestored != null)
        {
            return existingRestored;
        }

        if (currentPlan.Status != "Published")
            throw new InvalidOperationException("Solo se revierte la revisión publicada vigente.");

        // 1. Validar que ninguna entrega pendiente esté despachada (incluye la entregada sin recibir)
        var currentDeliveries = await db.Deliveries
            .Where(d => d.OrganizationId == DemoOrganization.Id && db.RouteStops.Any(s => s.OrganizationId == DemoOrganization.Id && s.RoutePlanId == currentPlan.Id && s.Id == d.RouteStopId))
            .ToListAsync(ct);

        if (currentDeliveries.Any(d => d.Status is not (DeliveryStates.Draft or DeliveryStates.Cancelled)))
        {
            throw new InvalidOperationException("409: Conflicto. No se puede revertir: existen entregas ya despachadas o en tránsito.");
        }

        // 2. Liberar reservas Active no despachadas de la revisión actual
        var activeReservations = await db.RouteStockReservations
            .Where(r => r.OrganizationId == DemoOrganization.Id && r.RoutePlanId == currentPlan.Id && r.Status == RouteStockReservationStatus.Active)
            .ToListAsync(ct);

        var balanceIds = activeReservations.Select(r => r.StockBalanceId).Distinct().ToList();
        var balances = await db.StockBalances.Where(b => b.OrganizationId == DemoOrganization.Id && balanceIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b, ct);

        foreach (var res in activeReservations)
        {
            res.Status = RouteStockReservationStatus.Released;
            res.ClosedAtUtc = DateTime.UtcNow;

            if (balances.TryGetValue(res.StockBalanceId, out var bal))
            {
                bal.QuantityReserved = Math.Max(0m, bal.QuantityReserved - res.Quantity);
            }
        }

        // 3. Cancelar entregas pendientes
        foreach (var del in currentDeliveries.Where(d => d.Status == nameof(Domain.Enums.DeliveryStatus.Draft)))
        {
            del.Status = nameof(Domain.Enums.DeliveryStatus.Cancelled);
        }

        // #199 (FR-28): la revisión pudo descontar pedidos de paradas retiradas; restaurar la anterior los vuelve a sumar.
        var restoredInput = previousPlan.InputJson;
        var previousLines = await RoutePlanCancellation.EffectiveOrderLinesAsync(db, previousPlan, ct);
        if (previousLines is not null)
        {
            var currentLines = await RoutePlanCancellation.EffectiveOrderLinesAsync(db, currentPlan, ct) ?? [];
            var restore = previousLines
                .Select(kv => (kv.Key, Quantity: kv.Value - currentLines.GetValueOrDefault(kv.Key)))
                .Where(x => x.Quantity > 0)
                .ToDictionary(x => x.Key, x => x.Quantity);
            await RoutePlanCancellation.AdjustPlannedAsync(db, restore, ct);
            restoredInput = RoutePlanCancellation.WithOrderLines(previousPlan.InputJson, previousLines);
        }

        // 4. Crear tercera revisión restaurada
        var restoredPlan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = currentPlan.Code,
            Revision = currentPlan.Revision + 1,
            SupersedesRoutePlanId = currentPlan.Id,
            VehicleId = previousPlan.VehicleId,
            Status = "Proposed", // Se publicará de inmediato
            ProposedBy = $"System:RevertToRev{previousPlan.Revision}",
            PlannedStartUtc = previousPlan.PlannedStartUtc,
            PlannedEndUtc = previousPlan.PlannedEndUtc,
            TotalKm = previousPlan.TotalKm,
            TotalMinutes = previousPlan.TotalMinutes,
            TotalCost = previousPlan.TotalCost,
            AlgorithmVersion = previousPlan.AlgorithmVersion,
            InputJson = restoredInput
        };
        db.RoutePlans.Add(restoredPlan);

        var prevStops = await db.RouteStops
            .Where(s => s.OrganizationId == DemoOrganization.Id && s.RoutePlanId == previousPlan.Id)
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync(ct);

        var restoredStops = prevStops.Select(s => new RouteStop
        {
            OrganizationId = DemoOrganization.Id,
            RoutePlanId = restoredPlan.Id,
            ServicePointId = s.ServicePointId,
            // #199: la recogida en bodega sigue siendo recogida; sin esto se publicaba como entrega.
            LocationId = s.LocationId,
            Kind = s.Kind,
            SequenceNumber = s.SequenceNumber,
            PlannedArrivalUtc = s.PlannedArrivalUtc,
            PlannedDepartureUtc = s.PlannedDepartureUtc,
            ActualArrivalUtc = s.ActualArrivalUtc,
            Status = s.Status
        }).ToList();
        db.RouteStops.AddRange(restoredStops);

        await db.SaveChangesAsync(ct);

        // 5. Publicar la tercera revisión en la misma operación
        await routePublisher.PublishAsync(new PublishRequest(restoredPlan.Id, null, request.RequestedByAccountId), ct);

        return restoredPlan;
    }

    // #300: recogida en bodega o entrega a bodega (rutas del asistente): no es un punto de servicio.
    private static bool IsLocationStop(RouteStop s) => s.Kind == RouteStopKind.Pickup || s.LocationId.HasValue;

    private static bool IsDoneStatus(string status) =>
        status is RouteStopStatus.Completed or RouteStopStatus.PickedUp or RouteStopStatus.PartiallyPickedUp;

    private int ServiceMinutes(RouteStop s) =>
        s.PlannedArrivalUtc is { } arrival && s.PlannedDepartureUtc is { } departure && departure > arrival
            ? (int)Math.Round((departure - arrival).TotalMinutes)
            : _options.DefaultServiceMinutes;

    // #300: paradas del InputJson original por secuencia, para copiar las cargas de las que no cambian.
    private static Dictionary<int, JsonNode> OriginalStopsBySequence(string? inputJson)
    {
        var result = new Dictionary<int, JsonNode>();
        if (string.IsNullOrWhiteSpace(inputJson) || JsonNode.Parse(inputJson)?["stops"] is not JsonArray stops) return result;
        foreach (var stop in stops)
        {
            if (stop?["sequence"] is JsonValue value && value.TryGetValue<int>(out var sequence)) result.TryAdd(sequence, stop);
        }
        return result;
    }

    private static VehicleRoute ReconstructPreviousVehicleRoute(
        RoutePlan plan,
        string vehicleCode,
        List<RouteStop> stops,
        Dictionary<Guid, ServicePoint> spMap)
    {
        // #300: solo puntos de servicio, una vez cada uno (las diferencias se indexan por código).
        var plannedStops = stops.Select(s =>
        {
            var code = spMap.TryGetValue(s.ServicePointId, out var sp) ? sp.Code : "";
            return new PlannedStop(
                s.SequenceNumber,
                code,
                s.ServicePointId,
                s.PlannedArrivalUtc ?? DateTime.UtcNow,
                s.PlannedDepartureUtc ?? DateTime.UtcNow,
                TimeSpan.Zero,
                Array.Empty<StopLoad>(),
                false,
                null);
        }).Where(s => s.PointCode != "").DistinctBy(s => s.PointCode).ToList();

        return new VehicleRoute(
            vehicleCode,
            plan.VehicleId,
            plannedStops,
            plan.TotalKm ?? 0m,
            plan.TotalMinutes ?? 0m,
            plan.TotalCost ?? 0m,
            0m,
            0m,
            false,
            Array.Empty<Leg>());
    }
}
