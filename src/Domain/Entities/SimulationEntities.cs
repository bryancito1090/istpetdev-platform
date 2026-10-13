using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class SimulationRun : OrganizationEntity
{
    public int Seed { get; set; }
    public string DatasetHash { get; set; } = string.Empty;
    public string PolicyCode { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "{}";
    public string Status { get; set; } = string.Empty;
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
