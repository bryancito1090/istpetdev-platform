using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class SimulationRun : OrganizationEntity
{
    public Guid ComparisonId { get; set; }
    public int Seed { get; set; }
    public string ScenarioCode { get; set; } = "base";
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = string.Empty;
    public string DatasetVersion { get; set; } = string.Empty;
    public string DatasetHash { get; set; } = string.Empty;
    public string CalendarHash { get; set; } = string.Empty;
    public string ManifestHash { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "{}";
    public string Status { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public string? FailureReason { get; set; }
    public string Origin { get; set; } = "computed";
    public string? HostName { get; set; }
    public string? CommitHash { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class SimulationMetric : OrganizationEntity
{
    public Guid SimulationRunId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string PolicyCode { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public decimal Numerator { get; set; }
    public decimal? Denominator { get; set; }
    public decimal? Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? NotApplicableReason { get; set; }
}

public sealed class SimulationEvent : OrganizationEntity
{
    public Guid SimulationRunId { get; set; }
    public long Sequence { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string PointCode { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal? Requested { get; set; }
    public decimal? Satisfied { get; set; }
    public string? Reason { get; set; }
}
