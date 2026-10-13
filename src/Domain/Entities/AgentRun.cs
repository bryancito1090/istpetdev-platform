using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class AgentRun : OrganizationEntity
{
    public string Kind { get; set; } = string.Empty; // "Watch", "Incident"
    public string Status { get; set; } = string.Empty; // "Running", "Completed", "Fallback", "Failed"
    public string TriggerJson { get; set; } = "{}";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int LatencyMs { get; set; }
    public string? Model { get; set; }
    public string? PromptVersion { get; set; }
    public string? N8nExecutionId { get; set; }
    public string? FallbackReason { get; set; }
    public string ToolCallsJson { get; set; } = "[]";
}
