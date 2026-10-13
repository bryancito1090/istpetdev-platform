using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class RiskSnapshot : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
    public string FormulaVersion { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public decimal? DailyDemand { get; set; }
    public decimal? CoverageDays { get; set; }
    public decimal LeadTimeDays { get; set; }
    public decimal SafetyMarginDays { get; set; }
    public decimal SuggestedQuantity { get; set; }
    public string RiskCode { get; set; } = string.Empty;
    public string InputSnapshotJson { get; set; } = "{}";
    public string? UncertaintyJson { get; set; }
}

public sealed class Incident : OrganizationEntity
{
    public string Kind { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ServicePointId { get; set; }
    public Guid? RoutePlanId { get; set; }
    public Point? Position { get; set; }
    public decimal? RadiusKm { get; set; }
    public bool IsSynthetic { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ReportedByAccountId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
