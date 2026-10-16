using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Agent;

public sealed class AgentRunService(
    IApplicationDbContext db,
    IN8nTrigger n8nTrigger,
    IRiskCalculator riskCalculator,
    IRoutePlanner routePlanner,
    IReplanner replanner,
    IIncidentEvaluator incidentEvaluator,
    IExplanationTemplates explanationTemplates,
    IExplanationValidator explanationValidator,
    IOptions<AgentOptions> options,
    ILogger<AgentRunService> logger) : IAgentRunService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<AgentRunView> StartAndWaitAsync(StartRunRequest request, Guid? actorAccountId, CancellationToken ct)
    {
        logger.LogInformation("Starting agent run {Kind} for incident {IncidentId}", request.Kind, request.IncidentId);

        var run = new AgentRun
        {
            OrganizationId = DemoOrganization.Id,
            Kind = request.Kind.ToString(),
            Status = "Running",
            TriggerJson = JsonSerializer.Serialize(request, JsonOpts),
            StartedAtUtc = DateTime.UtcNow,
            ToolCallsJson = "[]"
        };

        db.AgentRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return await ResumeAndWaitAsync(run.Id, ct);
    }

    public async Task<AgentRunView> ResumeAndWaitAsync(Guid runId, CancellationToken ct)
    {
        var run = await db.AgentRuns.AsNoTracking().FirstAsync(r => r.Id == runId && r.OrganizationId == DemoOrganization.Id, ct);
        var request = ParseTrigger(run.TriggerJson) ?? throw new InvalidOperationException("Missing persisted agent trigger");
        if (run.Status != "Running") return await BuildViewAsync(run.Id, ct);
        if (!options.Value.Enabled)
        {
            await CompleteFallbackAsync(run, "agent_disabled", ct);
            return await BuildViewAsync(run.Id, ct);
        }

        var timeoutSeconds = options.Value.TimeoutSeconds > 0 ? options.Value.TimeoutSeconds : 10;
        var deadline = run.StartedAtUtc.AddSeconds(timeoutSeconds);

        var triggerResult = await n8nTrigger.TriggerAsync(run.Id, request.Kind, request.IncidentId, deadline, ct);
        if (triggerResult.Outcome == TriggerOutcome.Error)
        {
            await CompleteFallbackAsync(run, "n8n_unreachable", ct);
            return await BuildViewAsync(run.Id, ct);
        }
        if (!string.IsNullOrWhiteSpace(triggerResult.ExecutionId))
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"AgentRun\" SET \"N8nExecutionId\" = {triggerResult.ExecutionId} WHERE \"Id\" = {run.Id} AND \"OrganizationId\" = {DemoOrganization.Id} AND \"Status\" = 'Running' AND (\"N8nExecutionId\" IS NULL OR \"N8nExecutionId\" = {triggerResult.ExecutionId})",
                ct);
        }

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            await Task.Delay(250, ct);

            var current = await db.AgentRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == run.Id, ct);
            if (current != null && current.Status != "Running")
            {
                return await BuildViewAsync(run.Id, ct);
            }
        }

        await CompleteFallbackAsync(run, "timeout", ct);

        return await BuildViewAsync(run.Id, ct);
    }

    public async Task<ResultOutcome> SubmitResultAsync(Guid runId, AgentResult result, CancellationToken ct)
    {
        var run = await db.AgentRuns.FirstOrDefaultAsync(r => r.Id == runId && r.OrganizationId == DemoOrganization.Id, ct);
        if (run == null)
        {
            return ResultOutcome.Invalid;
        }

        if (run.Status != "Running") return await RecordLateResultAsync(runId, ct);
        if (!string.IsNullOrWhiteSpace(result.AgentError))
        {
            var won = await CompleteFallbackAsync(run, "agent_error", ct);
            return won ? ResultOutcome.Accepted : ResultOutcome.Late;
        }
        if (result.Decisions.Count == 0 || result.Decisions.Select(d => d.AgentDecisionId).Distinct().Count() != result.Decisions.Count)
        {
            await CompleteFallbackAsync(run, "invalid_result", ct);
            return ResultOutcome.Invalid;
        }

        // Validate that referenced decisions exist in DB
        foreach (var item in result.Decisions)
        {
            var exists = await db.AgentDecisions.AnyAsync(
                d => d.Id == item.AgentDecisionId && d.OrganizationId == DemoOrganization.Id && d.CorrelationId == runId,
                ct);
            if (!exists)
            {
                await CompleteFallbackAsync(run, "invalid_result", ct);
                return ResultOutcome.Invalid;
            }
        }

        if (run.Status != "Running")
        {
            return ResultOutcome.Late;
        }

        // The conditional transition and decision updates commit together. A losing
        // callback must not modify decisions, and readers must see complete results.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var completedAt = DateTime.UtcNow;
        var latencyMs = (int)(completedAt - run.StartedAtUtc).TotalMilliseconds;
        var rows = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AgentRun\" SET \"Status\" = 'Completed', \"Model\" = {result.Model}, \"PromptVersion\" = {result.PromptVersion}, \"N8nExecutionId\" = {result.N8nExecutionId}, \"CompletedAtUtc\" = {completedAt}, \"LatencyMs\" = {latencyMs} WHERE \"Id\" = {runId} AND \"Status\" = 'Running'",
            ct);
        if (rows == 0)
        {
            await transaction.RollbackAsync(ct);
            return await RecordLateResultAsync(runId, ct);
        }

        // Update decisions with validated LLM text
        foreach (var item in result.Decisions)
        {
            var decision = await db.AgentDecisions.FirstOrDefaultAsync(d => d.Id == item.AgentDecisionId, ct);
            if (decision == null) continue;

            decision.CorrelationId = run.Id;
            var facts = ExtractFacts(decision.ProposalJson);
            var validation = explanationValidator.Validate(item.Explanation, facts);

            var driverMessage = item.DriverMessage ?? string.Empty;
            var driverValidation = explanationValidator.Validate(driverMessage, facts);
            var driverMsgValid = driverMessage.Length <= 160 && driverValidation.IsValid;

            if (validation.IsValid && driverMsgValid)
            {
                decision.Rationale = item.Explanation;
                decision.DriverMessage = item.DriverMessage ?? string.Empty;
                decision.ExplanationSource = "Llm";
                decision.ExplanationMismatch = null;
            }
            else
            {
                decision.ExplanationSource = "Template";
                decision.DriverMessage = "Consulta la propuesta de ruta antes de continuar.";
                decision.ExplanationMismatch = !validation.IsValid
                    ? validation.MismatchReason
                    : !driverValidation.IsValid ? driverValidation.MismatchReason : "driver_message_too_long";
                // Keep existing template explanation if invalid
            }
            decision.Model = result.Model;
            decision.PromptVersion = result.PromptVersion;
        }

        await db.SaveChangesAsync(ct);
        await SnapshotDecisionEvidenceAsync(runId, ct);
        await transaction.CommitAsync(ct);

        return ResultOutcome.Accepted;
    }

    public Task<Guid?> GetRunIdForExecutionAsync(string executionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(executionId)) return Task.FromResult<Guid?>(null);
        return db.AgentRuns.AsNoTracking()
            .Where(r => r.OrganizationId == DemoOrganization.Id && r.N8nExecutionId == executionId)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ExecutionRegistrationOutcome> RegisterExecutionAsync(Guid runId, string executionId, CancellationToken ct)
    {
        var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "AgentRun"
            SET "N8nExecutionId" = {executionId}
            WHERE "Id" = {runId}
              AND "OrganizationId" = {DemoOrganization.Id}
              AND "Status" = 'Running'
              AND ("N8nExecutionId" IS NULL OR "N8nExecutionId" = {executionId})
            """, ct);
        if (updated > 0) return ExecutionRegistrationOutcome.Accepted;

        var run = await db.AgentRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId && r.OrganizationId == DemoOrganization.Id, ct);
        if (run is null) return ExecutionRegistrationOutcome.NotFound;
        return string.Equals(run.N8nExecutionId, executionId, StringComparison.Ordinal)
            ? ExecutionRegistrationOutcome.Accepted
            : ExecutionRegistrationOutcome.Conflict;
    }

    public async Task<FailureOutcome> SubmitFailureAsync(Guid runId, AgentFailureRequest failure, CancellationToken ct)
    {
        var run = await db.AgentRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId && r.OrganizationId == DemoOrganization.Id, ct);
        if (run is null) return FailureOutcome.NotFound;
        if (!string.Equals(run.N8nExecutionId, failure.ExecutionId, StringComparison.Ordinal))
            return FailureOutcome.ExecutionMismatch;

        var trace = JsonSerializer.Serialize(new[]
        {
            new ToolCallTrace(
                "POST",
                $"/api/v1/agent/runs/{runId}/failure",
                500,
                0,
                DateTime.UtcNow,
                $"falló el nodo {failure.Node}",
                failure.ExecutionId,
                failure.Workflow,
                failure.Node,
                failure.Message)
        }, JsonOpts);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "AgentRun"
            SET "ToolCallsJson" = ("ToolCallsJson"::jsonb || {trace}::jsonb)
            WHERE "Id" = {runId}
              AND "OrganizationId" = {DemoOrganization.Id}
              AND "N8nExecutionId" = {failure.ExecutionId}
              AND NOT EXISTS (
                  SELECT 1
                  FROM jsonb_array_elements("ToolCallsJson"::jsonb) AS entries(trace)
                  WHERE trace->>'executionId' = {failure.ExecutionId})
            """, ct);
        return FailureOutcome.Accepted;
    }

    public async Task<AgentRunView?> GetAsync(Guid runId, CancellationToken ct)
    {
        var run = await db.AgentRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId && r.OrganizationId == DemoOrganization.Id, ct);
        if (run == null) return null;

        return await BuildViewAsync(runId, ct);
    }

    public async Task<IReadOnlyList<AgentRunView>> ListAsync(int limit, AgentRunKind? kind, AgentRunStatus? status, CancellationToken ct)
    {
        var query = db.AgentRuns.AsNoTracking().Where(r => r.OrganizationId == DemoOrganization.Id);

        if (kind.HasValue)
        {
            var kindStr = kind.Value.ToString();
            query = query.Where(r => r.Kind == kindStr);
        }

        if (status.HasValue)
        {
            var statusStr = status.Value.ToString();
            query = query.Where(r => r.Status == statusStr);
        }

        var runIds = await query
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(limit)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var views = new List<AgentRunView>();
        foreach (var id in runIds)
        {
            views.Add(await BuildViewAsync(id, ct));
        }

        return views;
    }

    private async Task<bool> CompleteFallbackAsync(AgentRun run, string reason, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var completedAt = DateTime.UtcNow;
        var latencyMs = (int)(completedAt - run.StartedAtUtc).TotalMilliseconds;
        var rows = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AgentRun\" SET \"Status\" = 'Fallback', \"FallbackReason\" = {reason}, \"CompletedAtUtc\" = {completedAt}, \"LatencyMs\" = {latencyMs} WHERE \"Id\" = {run.Id} AND \"Status\" = 'Running'", ct);
        if (rows == 0) return false;
        db.ChangeTracker.Clear();
        await ExecuteFallbackLogicAsync(run, reason, ParseTrigger(run.TriggerJson)?.IncidentId, ct);
        await SnapshotDecisionEvidenceAsync(run.Id, ct);
        completedAt = DateTime.UtcNow;
        latencyMs = (int)(completedAt - run.StartedAtUtc).TotalMilliseconds;
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AgentRun\" SET \"CompletedAtUtc\" = {completedAt}, \"LatencyMs\" = {latencyMs} WHERE \"Id\" = {run.Id}", ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private async Task<ResultOutcome> RecordLateResultAsync(Guid runId, CancellationToken ct)
    {
        var trace = JsonSerializer.Serialize(new[] { new ToolCallTrace("POST", $"/api/v1/agent/runs/{runId}/result", 409, 0, DateTime.UtcNow, "late_result_ignored") }, JsonOpts);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AgentRun\" SET \"ToolCallsJson\" = (\"ToolCallsJson\"::jsonb || {trace}::jsonb) WHERE \"Id\" = {runId} AND \"OrganizationId\" = {DemoOrganization.Id}", ct);
        return ResultOutcome.Late;
    }

    private async Task ExecuteFallbackLogicAsync(AgentRun run, string reason, Guid? incidentId, CancellationToken ct)
    {
        if (string.Equals(run.Kind, AgentRunKind.Watch.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var existing = await db.AgentDecisions.Where(d => d.CorrelationId == run.Id).ToListAsync(ct);
            if (existing.Count > 0)
            {
                foreach (var decision in existing)
                {
                    decision.ExplanationSource = "Template";
                    decision.DriverMessage = "Consulta la propuesta de ruta antes de continuar.";
                }
                await db.SaveChangesAsync(ct);
                return;
            }
            var risks = await riskCalculator.CalculateAsync(persistSnapshots: false, ct);
            var plan = await routePlanner.PlanAsync(new PlanRequest(null, null, true), ct);
            if (plan.Routes.Count == 0)
            {
                var facts = Facts.Sort(risks.SelectMany(r => r.Facts).ToList());
                db.AgentDecisions.Add(new AgentDecision
                {
                    OrganizationId = DemoOrganization.Id, Kind = "RISK_WATCH", Status = "Evaluated",
                    ProposalJson = JsonSerializer.Serialize(new { facts, unserved = plan.Unserved }, JsonOpts),
                    Rationale = explanationTemplates.Explain("risk", facts),
                    DriverMessage = "No hay rutas propuestas; consulta al planificador antes de salir.",
                    ExplanationSource = "Template", CorrelationId = run.Id, CreatedAtUtc = DateTime.UtcNow
                });
            }
            foreach (var route in plan.Routes)
            {
                var facts = Facts.Sort(plan.Facts.Concat(risks.SelectMany(r => r.Facts)).Concat([
                    new Fact("total_km", route.VehicleCode, route.Km, "km"),
                    new Fact("stops_count", route.VehicleCode, route.Stops.Count, "stops")]).ToList());
                db.AgentDecisions.Add(new AgentDecision
                {
                    OrganizationId = DemoOrganization.Id,
                    Kind = "WATCH_ROUTE_PROPOSAL",
                    Status = "Proposed",
                    ProposalJson = JsonSerializer.Serialize(new { vehicleCode = route.VehicleCode, route, facts }, JsonOpts),
                    Rationale = explanationTemplates.Explain("plan", facts),
                    DriverMessage = "Consulta la propuesta de ruta antes de iniciar el recorrido.",
                    ExplanationSource = "Template",
                    CorrelationId = run.Id,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync(ct);
        }
        else if (string.Equals(run.Kind, AgentRunKind.Incident.ToString(), StringComparison.OrdinalIgnoreCase) && incidentId.HasValue)
        {
            var existingDecisions = await db.AgentDecisions.Where(d => d.CorrelationId == run.Id).ToListAsync(ct);
            foreach (var existingDecision in existingDecisions)
                if (string.IsNullOrWhiteSpace(existingDecision.DriverMessage))
                    existingDecision.DriverMessage = "Consulta la revisión vigente de la ruta antes de continuar.";
            var existing = existingDecisions.Select(d => d.ProposalJson).ToList();
            var replannedRouteIds = existing
                .Select(ReadPreviousRoutePlanId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            var impact = await incidentEvaluator.EvaluateAsync(incidentId.Value, ct);
            foreach (var affected in impact.AffectedRoutes)
            {
                if (replannedRouteIds.Contains(affected.RoutePlanId)) continue;

                ReplanResult result;
                try { result = await replanner.ReplanAsync(affected.RoutePlanId, incidentId.Value, ct); }
                catch (ReplanConflictException) { continue; }
                var dec = await db.AgentDecisions
                    .OrderByDescending(d => d.CreatedAtUtc)
                    .FirstOrDefaultAsync(d => d.CorrelationId == result.ProposedRoutePlanId, ct);
                if (dec != null)
                {
                    dec.CorrelationId = run.Id;
                    dec.ExplanationSource = "Template";
                    dec.DriverMessage = "Consulta la revisión vigente de la ruta antes de continuar.";
                }
            }
            await db.SaveChangesAsync(ct);
        }
    }

    private static Guid? ReadPreviousRoutePlanId(string proposalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(proposalJson);
            return document.RootElement.TryGetProperty("previousRoutePlanId", out var id)
                && id.TryGetGuid(out var routePlanId)
                ? routePlanId
                : null;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private async Task<AgentRunView> BuildViewAsync(Guid runId, CancellationToken ct)
    {
        var run = await db.AgentRuns.AsNoTracking().FirstAsync(r => r.Id == runId, ct);
        var decisions = await db.AgentDecisions.AsNoTracking().Where(d => d.CorrelationId == runId).ToListAsync(ct);

        var toolTraces = JsonSerializer.Deserialize<List<ToolCallTrace>>(run.ToolCallsJson, JsonOpts) ?? [];

        // La evidencia se deriva por servidor del plan correlacionado (bodega base del
        // vehículo y secuencia real de paradas), nunca de texto libre del LLM.
        var proposals = decisions.ToDictionary(d => d.Id, d => ParseProposal(d.ProposalJson));
        var planIds = proposals.Values
            .Where(p => p.Evidence is null && p.ProposedRoutePlanId.HasValue)
            .Select(p => p.ProposedRoutePlanId!.Value)
            .Distinct()
            .ToList();
        var plans = planIds.Count == 0
            ? new Dictionary<Guid, RoutePlan>()
            : await db.RoutePlans.AsNoTracking().Where(p => planIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var vehicleIds = plans.Values.Select(p => p.VehicleId)
            .Concat(proposals.Values.Where(p => p.Evidence is null && p.VehicleId.HasValue).Select(p => p.VehicleId!.Value))
            .Distinct()
            .ToList();
        var vehicles = vehicleIds.Count == 0
            ? new Dictionary<Guid, Vehicle>()
            : await db.Vehicles.AsNoTracking().Where(v => vehicleIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);

        var depotIds = vehicles.Values.Select(v => v.DepotLocationId).Where(id => id != Guid.Empty).Distinct().ToList();
        var depots = depotIds.Count == 0
            ? new Dictionary<Guid, Location>()
            : await db.Locations.AsNoTracking().Where(l => depotIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);

        var storedStops = planIds.Count == 0
            ? new List<RouteStop>()
            : await db.RouteStops.AsNoTracking().Where(s => planIds.Contains(s.RoutePlanId)).ToListAsync(ct);
        var pointIds = storedStops.Select(s => s.ServicePointId).Distinct().ToList();
        var pointCodes = pointIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.ServicePoints.AsNoTracking().Where(sp => pointIds.Contains(sp.Id)).ToDictionaryAsync(sp => sp.Id, sp => sp.Code, ct);

        var decisionViews = new List<AgentDecisionView>();
        foreach (var decision in decisions)
        {
            var proposal = proposals[decision.Id];
            var vehicleCode = proposal.VehicleCode ?? string.Empty;
            var vehicleId = proposal.VehicleId;

            if (proposal.ProposedRoutePlanId.HasValue && plans.TryGetValue(proposal.ProposedRoutePlanId.Value, out var plan))
            {
                vehicleId ??= plan.VehicleId;
                if (vehicles.TryGetValue(plan.VehicleId, out var planVehicle)) vehicleCode = planVehicle.Code;
            }
            else if (vehicleId.HasValue && vehicles.TryGetValue(vehicleId.Value, out var directVehicle))
            {
                vehicleCode = directVehicle.Code;
            }

            string? originCode = null;
            string? originName = null;
            if (vehicleId.HasValue && vehicles.TryGetValue(vehicleId.Value, out var vehicle)
                && vehicle.DepotLocationId != Guid.Empty && depots.TryGetValue(vehicle.DepotLocationId, out var depot))
            {
                originCode = depot.Code;
                originName = depot.Name;
            }

            IReadOnlyList<DecisionStopView>? orderedStops = null;
            if (proposal.ProposedRoutePlanId.HasValue)
            {
                orderedStops = storedStops
                    .Where(s => s.RoutePlanId == proposal.ProposedRoutePlanId.Value)
                    .OrderBy(s => s.SequenceNumber)
                    .Select(s => new DecisionStopView(s.SequenceNumber, pointCodes.TryGetValue(s.ServicePointId, out var code) ? code : string.Empty))
                    .ToList();
            }
            else if (proposal.RouteStops is { Count: > 0 })
            {
                orderedStops = proposal.RouteStops;
            }

            Enum.TryParse<ExplanationSource>(decision.ExplanationSource, true, out var source);

            decisionViews.Add(new AgentDecisionView(
                decision.Id,
                proposal.Evidence?.VehicleCode ?? vehicleCode,
                decision.Status,
                decision.Rationale,
                decision.DriverMessage,
                source,
                decision.ExplanationMismatch,
                ExtractFacts(decision.ProposalJson),
                proposal.Evidence is null ? originCode : proposal.Evidence.OriginWarehouseCode,
                proposal.Evidence is null ? originName : proposal.Evidence.OriginWarehouseName,
                proposal.Evidence is null ? orderedStops : proposal.Evidence.OrderedStops));
        }

        Enum.TryParse<AgentRunKind>(run.Kind, true, out var kind);
        Enum.TryParse<AgentRunStatus>(run.Status, true, out var status);

        return new AgentRunView(
            run.Id,
            kind,
            status,
            run.LatencyMs,
            run.Model,
            run.PromptVersion,
            run.N8nExecutionId,
            run.FallbackReason,
            toolTraces,
            decisionViews);
    }

    private async Task SnapshotDecisionEvidenceAsync(Guid runId, CancellationToken ct)
    {
        var view = await BuildViewAsync(runId, ct);
        var decisions = await db.AgentDecisions.Where(d => d.CorrelationId == runId).ToListAsync(ct);
        foreach (var decision in decisions)
        {
            var proposal = JsonNode.Parse(decision.ProposalJson)!.AsObject();
            if (proposal.ContainsKey("evidence")) continue;
            var item = view.Decisions.Single(d => d.AgentDecisionId == decision.Id);
            proposal["evidence"] = JsonSerializer.SerializeToNode(new DecisionEvidence(
                item.VehicleCode, item.OriginWarehouseCode, item.OriginWarehouseName, item.OrderedStops), JsonOpts);
            decision.ProposalJson = proposal.ToJsonString(JsonOpts);
        }
        await db.SaveChangesAsync(ct);
    }

    private sealed record DecisionEvidence(string VehicleCode, string? OriginWarehouseCode,
        string? OriginWarehouseName, IReadOnlyList<DecisionStopView>? OrderedStops);

    private sealed record ProposalView(
        Guid? ProposedRoutePlanId,
        Guid? VehicleId,
        string? VehicleCode,
        IReadOnlyList<DecisionStopView>? RouteStops,
        DecisionEvidence? Evidence = null);

    private static ProposalView ParseProposal(string proposalJson)
    {
        if (string.IsNullOrWhiteSpace(proposalJson)) return new ProposalView(null, null, null, null);
        try
        {
            using var doc = JsonDocument.Parse(proposalJson);
            var root = doc.RootElement;
            Guid? planId = root.TryGetProperty("proposedRoutePlanId", out var planProp) && planProp.TryGetGuid(out var parsedPlanId)
                ? parsedPlanId
                : null;

            Guid? vehicleId = null;
            string? vehicleCode = null;
            if (root.TryGetProperty("vehicleCode", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
            {
                vehicleCode = codeProp.GetString();
            }

            IReadOnlyList<DecisionStopView>? stops = null;
            if (root.TryGetProperty("route", out var route) && route.ValueKind == JsonValueKind.Object)
            {
                if (route.TryGetProperty("vehicleId", out var vehicleProp) && vehicleProp.TryGetGuid(out var parsedVehicleId))
                {
                    vehicleId = parsedVehicleId;
                }
                if (vehicleCode is null && route.TryGetProperty("vehicleCode", out var routeCode) && routeCode.ValueKind == JsonValueKind.String)
                {
                    vehicleCode = routeCode.GetString();
                }
                if (route.TryGetProperty("stops", out var stopsProp) && stopsProp.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<DecisionStopView>();
                    foreach (var stop in stopsProp.EnumerateArray())
                    {
                        var sequence = stop.TryGetProperty("sequence", out var sequenceProp) && sequenceProp.TryGetInt32(out var parsedSequence)
                            ? parsedSequence
                            : list.Count + 1;
                        var pointCode = stop.TryGetProperty("pointCode", out var pointProp) ? pointProp.GetString() ?? string.Empty : string.Empty;
                        list.Add(new DecisionStopView(sequence, pointCode));
                    }
                    stops = list.Count > 0 ? list : null;
                }
            }

            var evidence = root.TryGetProperty("evidence", out var evidenceProp)
                ? evidenceProp.Deserialize<DecisionEvidence>(JsonOpts) : null;
            return new ProposalView(planId, vehicleId, vehicleCode, stops, evidence);
        }
        catch (JsonException)
        {
            return new ProposalView(null, null, null, null);
        }
    }

    private static IReadOnlyList<Fact> ExtractFacts(string proposalJson)
    {
        if (string.IsNullOrWhiteSpace(proposalJson)) return [];
        try
        {
            using var doc = JsonDocument.Parse(proposalJson);
            if (doc.RootElement.TryGetProperty("facts", out var factsProp))
            {
                return JsonSerializer.Deserialize<List<Fact>>(factsProp.GetRawText(), JsonOpts) ?? [];
            }
        }
        catch
        {
            // ignore
        }
        return [];
    }

    private static StartRunRequest? ParseTrigger(string triggerJson)
    {
        if (string.IsNullOrWhiteSpace(triggerJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<StartRunRequest>(triggerJson, JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}
