using System.Text.Json;
using System.Text.Json.Serialization;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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

        if (!options.Value.Enabled)
        {
            run.Status = "Fallback";
            run.FallbackReason = "agent_disabled";
            run.CompletedAtUtc = DateTime.UtcNow;
            run.LatencyMs = 0;
            db.AgentRuns.Add(run);
            await db.SaveChangesAsync(ct);

            await ExecuteFallbackLogicAsync(run, "agent_disabled", request.IncidentId, ct);
            return await BuildViewAsync(run.Id, ct);
        }

        db.AgentRuns.Add(run);
        await db.SaveChangesAsync(ct);

        var timeoutSeconds = options.Value.TimeoutSeconds > 0 ? options.Value.TimeoutSeconds : 10;
        var deadline = run.StartedAtUtc.AddSeconds(timeoutSeconds);

        var triggerOutcome = await n8nTrigger.TriggerAsync(run.Id, request.Kind, request.IncidentId, deadline, ct);
        if (triggerOutcome == TriggerOutcome.Error)
        {
            run.Status = "Fallback";
            run.FallbackReason = "n8n_unreachable";
            run.CompletedAtUtc = DateTime.UtcNow;
            run.LatencyMs = (int)(run.CompletedAtUtc.Value - run.StartedAtUtc).TotalMilliseconds;
            await db.SaveChangesAsync(ct);

            await ExecuteFallbackLogicAsync(run, "n8n_unreachable", request.IncidentId, ct);
            return await BuildViewAsync(run.Id, ct);
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

        // Timeout reached: atomically transition from Running to Fallback
        var now = DateTime.UtcNow;
        var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"AgentRun\" SET \"Status\" = 'Fallback', \"FallbackReason\" = 'timeout', \"CompletedAtUtc\" = {now} WHERE \"Id\" = {run.Id} AND \"Status\" = 'Running'",
            ct);

        if (rowsAffected > 0)
        {
            db.ChangeTracker.Clear();
            run.Status = "Fallback";
            run.FallbackReason = "timeout";
            run.CompletedAtUtc = now;
            run.LatencyMs = (int)(now - run.StartedAtUtc).TotalMilliseconds;
            await ExecuteFallbackLogicAsync(run, "timeout", request.IncidentId, ct);
        }

        return await BuildViewAsync(run.Id, ct);
    }

    public async Task<ResultOutcome> SubmitResultAsync(Guid runId, AgentResult result, CancellationToken ct)
    {
        var run = await db.AgentRuns.FirstOrDefaultAsync(r => r.Id == runId && r.OrganizationId == DemoOrganization.Id, ct);
        if (run == null)
        {
            return ResultOutcome.Invalid;
        }

        if (!string.IsNullOrWhiteSpace(result.AgentError))
        {
            logger.LogWarning("Agent run {RunId} reported agent_error fallback", runId);
            var affected = await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"AgentRun\" SET \"Status\" = 'Fallback', \"FallbackReason\" = 'agent_error', \"CompletedAtUtc\" = {DateTime.UtcNow} WHERE \"Id\" = {runId} AND \"Status\" = 'Running'",
                ct);

            if (affected == 0) return ResultOutcome.Late;

            db.ChangeTracker.Clear();
            run.Status = "Fallback";
            run.FallbackReason = "agent_error";
            run.CompletedAtUtc = DateTime.UtcNow;
            run.LatencyMs = (int)(run.CompletedAtUtc.Value - run.StartedAtUtc).TotalMilliseconds;

            var triggerReq = ParseTrigger(run.TriggerJson);
            await ExecuteFallbackLogicAsync(run, "agent_error", triggerReq?.IncidentId, ct);
            return ResultOutcome.Accepted;
        }

        // Validate that referenced decisions exist in DB
        foreach (var item in result.Decisions)
        {
            var exists = await db.AgentDecisions.AnyAsync(
                d => d.Id == item.AgentDecisionId && d.OrganizationId == DemoOrganization.Id && d.CorrelationId == runId,
                ct);
            if (!exists)
            {
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
        if (rows == 0) return ResultOutcome.Late;

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
                decision.ExplanationMismatch = !validation.IsValid
                    ? validation.MismatchReason
                    : !driverValidation.IsValid ? driverValidation.MismatchReason : "driver_message_too_long";
                // Keep existing template explanation if invalid
            }
            decision.Model = result.Model;
            decision.PromptVersion = result.PromptVersion;
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return ResultOutcome.Accepted;
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

    private async Task ExecuteFallbackLogicAsync(AgentRun run, string reason, Guid? incidentId, CancellationToken ct)
    {
        if (string.Equals(run.Kind, AgentRunKind.Watch.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var risks = await riskCalculator.CalculateAsync(persistSnapshots: false, ct);
            var facts = Facts.Sort(risks.SelectMany(r => r.Facts).ToList());
            var explanation = explanationTemplates.Explain("risk", facts);

            var decision = new AgentDecision
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "RISK_WATCH",
                Status = "Evaluated",
                ProposalJson = JsonSerializer.Serialize(new { risksCount = risks.Count, facts }, JsonOpts),
                Rationale = explanation,
                ExplanationSource = "Template",
                CorrelationId = run.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.AgentDecisions.Add(decision);
            await db.SaveChangesAsync(ct);
        }
        else if (string.Equals(run.Kind, AgentRunKind.Incident.ToString(), StringComparison.OrdinalIgnoreCase) && incidentId.HasValue)
        {
            var existing = await db.AgentDecisions
                .Where(d => d.CorrelationId == run.Id)
                .Select(d => d.ProposalJson)
                .ToListAsync(ct);
            var replannedRouteIds = existing
                .Select(ReadPreviousRoutePlanId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            var impact = await incidentEvaluator.EvaluateAsync(incidentId.Value, ct);
            foreach (var affected in impact.AffectedRoutes)
            {
                if (replannedRouteIds.Contains(affected.RoutePlanId)) continue;

                var result = await replanner.ReplanAsync(affected.RoutePlanId, incidentId.Value, ct);
                var dec = await db.AgentDecisions
                    .OrderByDescending(d => d.CreatedAtUtc)
                    .FirstOrDefaultAsync(d => d.CorrelationId == result.ProposedRoutePlanId, ct);
                if (dec != null)
                {
                    dec.CorrelationId = run.Id;
                    dec.ExplanationSource = "Template";
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

        var decisionViews = new List<AgentDecisionView>();
        var vehicles = await db.Vehicles.AsNoTracking().ToDictionaryAsync(v => v.Id, v => v.Code, ct);

        foreach (var d in decisions)
        {
            var vehicleCode = "";
            var facts = ExtractFacts(d.ProposalJson);

            // Try to extract vehicle from proposed plan if present
            try
            {
                using var doc = JsonDocument.Parse(d.ProposalJson);
                if (doc.RootElement.TryGetProperty("proposedRoutePlanId", out var planIdProp))
                {
                    var planId = planIdProp.GetGuid();
                    var plan = await db.RoutePlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, ct);
                    if (plan != null && vehicles.TryGetValue(plan.VehicleId, out var vCode))
                    {
                        vehicleCode = vCode;
                    }
                }
            }
            catch
            {
                // ignore parsing failure
            }

            Enum.TryParse<ExplanationSource>(d.ExplanationSource, true, out var source);

            decisionViews.Add(new AgentDecisionView(
                d.Id,
                vehicleCode,
                d.Status,
                d.Rationale,
                d.DriverMessage,
                source,
                d.ExplanationMismatch,
                facts));
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
