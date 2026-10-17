namespace IstpetDev.Application.Logistics;

public sealed class RecommendationOptions
{
    public const string SectionName = "Logistics:Recommendation";

    public decimal WeightCost { get; set; } = 0.35m;
    public decimal WeightCriticalOnTime { get; set; } = 0.35m;
    public decimal WeightPendingDemand { get; set; } = 0.20m;
    public decimal WeightRisk { get; set; } = 0.10m;

    public decimal ActiveIncidentPenalty { get; set; } = 15.0m;
    public decimal OverloadThresholdPct { get; set; } = 90.0m;
    public decimal OverloadPenalty { get; set; } = 10.0m;
    public decimal ResponseTimePenaltyPerHour { get; set; } = 2.5m;
}
