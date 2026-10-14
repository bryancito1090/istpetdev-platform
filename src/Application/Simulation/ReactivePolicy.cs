namespace IstpetDev.Application.Simulation;

public sealed record ReactiveNotice(string PointCode, string Criticality, DateTime NoticeTimeUtc)
{
    public int CriticalityRank => Criticality.ToLowerInvariant() switch
    {
        "critica" or "critico" => 3,
        "alta" or "alto" => 2,
        "media" or "medio" => 1,
        _ => 0
    };
}

public sealed record ReactiveOrderDecision(
    string PointCode,
    string Sku,
    bool Triggered,
    decimal RequestedQuantity,
    string? Assumption = null);

public sealed class ReactivePolicy
{
    public const string Version = "reactive-v1";

    public ReactiveOrderDecision EvaluatePointDemand(
        string pointCode,
        string sku,
        decimal availableStock,
        decimal inTransitStock,
        decimal? historicalMeanDaily,
        decimal leadTimeDays,
        int targetCoverageDays = 14,
        decimal fallbackBaseDaily = 10m)
    {
        string? assumption = null;
        decimal dailyDemand = historicalMeanDaily ?? fallbackBaseDaily;
        if (historicalMeanDaily is null)
        {
            assumption = "assumed_dataset_base_consumption";
        }

        var threshold = dailyDemand * leadTimeDays;

        if (availableStock < threshold)
        {
            var target = dailyDemand * targetCoverageDays;
            var needed = target - availableStock - inTransitStock;
            var requested = Math.Max(0m, Math.Round(needed, 3, MidpointRounding.AwayFromZero));

            return new ReactiveOrderDecision(pointCode, sku, true, requested, assumption);
        }

        return new ReactiveOrderDecision(pointCode, sku, false, 0m, assumption);
    }
}
