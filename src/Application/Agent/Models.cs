using IstpetDev.Application.Logistics;
using System.Text.Json.Serialization;

namespace IstpetDev.Application.Agent;

public enum AgentRunKind { Watch, Incident }
public enum AgentRunStatus { Running, Completed, Fallback, Failed }
public enum ExplanationSource { Llm, Template }

public sealed record StartRunRequest(AgentRunKind Kind, Guid? IncidentId);

public sealed record AgentRunView(
    Guid RunId,
    AgentRunKind Kind,
    AgentRunStatus Status,
    int LatencyMs,
    string? Model,
    string? PromptVersion,
    string? N8nExecutionId,
    string? FallbackReason,
    IReadOnlyList<ToolCallTrace> ToolCalls,
    IReadOnlyList<AgentDecisionView> Decisions);

public sealed record ToolCallTrace(
    string Method,
    string Path,
    int Status,
    int Milliseconds,
    DateTime AtUtc,
    string? Event = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ExecutionId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Workflow = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Node = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message = null);

public sealed record AgentFailureRequest(
    Guid RunId,
    string ExecutionId,
    string Workflow,
    string Node,
    string Message);

public sealed record AgentExecutionRequest(Guid RunId, string ExecutionId);

public sealed record AgentDecisionView(
    Guid AgentDecisionId,
    string VehicleCode,
    string Outcome,
    string Explanation,
    string DriverMessage,
    ExplanationSource Source,
    string? MismatchReason,
    IReadOnlyList<Fact> Facts,
    string? OriginWarehouseCode = null,
    string? OriginWarehouseName = null,
    IReadOnlyList<DecisionStopView>? OrderedStops = null);

public sealed record DecisionStopView(int Sequence, string PointCode);

public sealed record AgentResult(
    IReadOnlyList<DecisionText> Decisions,
    string? Summary,
    string Model,
    string PromptVersion,
    string? N8nExecutionId,
    string? AgentError);

public sealed record DecisionText(Guid AgentDecisionId, string Explanation, string DriverMessage);

public enum ResultOutcome { Accepted, Late, Invalid }
public enum TriggerOutcome { Success, Error }
public sealed record N8nTriggerResult(TriggerOutcome Outcome, string? ExecutionId = null);
public enum ExecutionRegistrationOutcome { Accepted, NotFound, Conflict }
public enum FailureOutcome { Accepted, NotFound, ExecutionMismatch }

public sealed record ValidationResult(bool IsValid, string? MismatchReason, IReadOnlyList<string> UnmatchedNumbers);

public interface IExplanationValidator
{
    ValidationResult Validate(string text, IReadOnlyList<Fact> facts);
}

public interface IAgentRunService
{
    Task<AgentRunView> StartAndWaitAsync(StartRunRequest request, Guid? actorAccountId, CancellationToken ct);
    Task<ResultOutcome> SubmitResultAsync(Guid runId, AgentResult result, CancellationToken ct);
    Task<ExecutionRegistrationOutcome> RegisterExecutionAsync(Guid runId, string executionId, CancellationToken ct);
    Task<Guid?> GetRunIdForExecutionAsync(string executionId, CancellationToken ct);
    Task<FailureOutcome> SubmitFailureAsync(Guid runId, AgentFailureRequest failure, CancellationToken ct);
    Task<AgentRunView> ResumeAndWaitAsync(Guid runId, CancellationToken ct);
    Task<AgentRunView?> GetAsync(Guid runId, CancellationToken ct);
    Task<IReadOnlyList<AgentRunView>> ListAsync(int limit, AgentRunKind? kind, AgentRunStatus? status, CancellationToken ct);
}

public interface IN8nTrigger
{
    Task<N8nTriggerResult> TriggerAsync(Guid runId, AgentRunKind kind, Guid? incidentId, CancellationToken ct);
    Task<N8nTriggerResult> TriggerAsync(Guid runId, AgentRunKind kind, Guid? incidentId, DateTime? deadlineUtc, CancellationToken ct)
        => TriggerAsync(runId, kind, incidentId, ct);
}
