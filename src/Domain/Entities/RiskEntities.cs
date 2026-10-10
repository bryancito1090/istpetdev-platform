using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class RiskCalculationRun : OrganizationEntity
{
    public Guid JobExecutionId { get; set; }
    public Guid InventorySnapshotId { get; set; }
    public Guid ConfigurationVersionId { get; set; }
    public string FormulaCode { get; set; } = string.Empty;
    public string FormulaVersion { get; set; } = string.Empty;
    public DateTime CutoffUtc { get; set; }
    public string InputHash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class RiskSnapshot : OrganizationEntity
{
    public Guid RiskCalculationRunId { get; set; }
    public Guid ServicePointId { get; set; }
    public Guid ProductId { get; set; }
    public decimal? CoverageDays { get; set; }
    public decimal? LeadTimeDays { get; set; }
    public decimal? SafetyMarginDays { get; set; }
    public string RiskCode { get; set; } = string.Empty;
    public string InputSnapshotJson { get; set; } = "{}";
    public string? UncertaintyJson { get; set; }
}

public sealed class RiskExplanation : OrganizationEntity
{
    public Guid RiskSnapshotId { get; set; }
    public Guid? IntegrationRunId { get; set; }
    public Guid TemplateVersionId { get; set; }
    public string ExplanationText { get; set; } = string.Empty;
    public string? RecommendedAction { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string ModelCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class AnomalyCase : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? RiskSnapshotId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DetectedAtUtc { get; set; }
    public Guid? ClosedByAccountId { get; set; }
    public string? Resolution { get; set; }
    public Guid? LossMovementId { get; set; }
}

public sealed class Incident : OrganizationEntity
{
    public Guid? ServicePointId { get; set; }
    public Guid? RoutePlanId { get; set; }
    public Guid ReportedByAccountId { get; set; }
    public Guid SeverityOptionId { get; set; }
    public Point? Position { get; set; }
    public bool IsSynthetic { get; set; }
    public Guid? ScenarioEventId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
}

public sealed class IncidentRoadRestriction : OrganizationEntity
{
    public Guid IncidentId { get; set; }
    public Guid RoadMatrixEdgeId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsClosed { get; set; }
    public int AddedDurationSeconds { get; set; }
}

public sealed class RoutePlanIncident : OrganizationEntity
{
    public Guid RoutePlanId { get; set; }
    public Guid IncidentId { get; set; }
}

public sealed class IntegrationConnection : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public string EndpointReference { get; set; } = string.Empty;
    public string SecretReference { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid ConfigurationVersionId { get; set; }
}

public sealed class JobExecution : OrganizationEntity
{
    public string Kind { get; set; } = string.Empty;
    public Guid RequestedByAccountId { get; set; }
    public Guid OperationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int Progress { get; set; }
    public string? ErrorCode { get; set; }
}

public sealed class IntegrationRun : OrganizationEntity
{
    public Guid IntegrationConnectionId { get; set; }
    public Guid? JobExecutionId { get; set; }
    public string RunKey { get; set; } = string.Empty;
    public string? ProviderExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public int? TokenCount { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? Currency { get; set; }
    public string? ErrorCode { get; set; }
}
