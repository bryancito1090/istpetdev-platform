using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class IdempotencyRecord : OrganizationEntity
{
    public Guid AccountId { get; set; }
    public Guid OperationId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int? ResponseStatus { get; set; }
    public string? ResponseJson { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class ImportBatch : OrganizationEntity
{
    public string ProfileName { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InputHash { get; set; } = string.Empty;
    public string FilesJson { get; set; } = "[]";
    public string ProfileHash { get; set; } = string.Empty;
    public string ProfileJson { get; set; } = "{}";
    public string ReportJson { get; set; } = "{}";
    public Guid? CreatedByAccountId { get; set; }
}

// Propuesta del agente logístico (spec 003). Queda en Proposed hasta que una persona la aprueba.
public sealed class AgentDecision : OrganizationEntity
{
    public string Kind { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TriggerJson { get; set; } = "{}";
    public string ProposalJson { get; set; } = "{}";
    public string ToolCallsJson { get; set; } = "[]";
    public string Rationale { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid? DecidedByAccountId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? ExecutedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public string DriverMessage { get; set; } = string.Empty;
    public string ExplanationSource { get; set; } = "Template";
    public string? ExplanationMismatch { get; set; }
}
