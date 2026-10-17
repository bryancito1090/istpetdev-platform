namespace IstpetDev.Application.Simulation;

public sealed record SeedPolicyMetric(int Seed, string Scenario, string Policy, string MetricCode, decimal? Value, string? Sku = null);

public sealed record AggregatedMetricSummary(
    string Policy,
    string MetricCode,
    decimal? Min,
    decimal? Median,
    decimal? Max,
    decimal? AdverseValue,
    string? Sku = null);

public static class ComparisonAggregator
{
    public static AggregatedMetricSummary AggregateMetric(
        IEnumerable<SeedPolicyMetric> runs,
        string policy,
        string metricCode,
        string? sku = null)
    {
        var relevant = runs.Where(r => r.Policy == policy && r.MetricCode == metricCode && r.Sku == sku).ToList();

        var baseValues = relevant
            .Where(r => r.Scenario == "base" && r.Value.HasValue)
            .Select(r => r.Value!.Value)
            .OrderBy(v => v)
            .ToList();

        var adverseValue = relevant
            .FirstOrDefault(r => r.Scenario == "adverse")?.Value;

        if (baseValues.Count == 0)
        {
            return new AggregatedMetricSummary(policy, metricCode, null, null, null, adverseValue, sku);
        }

        var min = baseValues.First();
        var max = baseValues.Last();
        decimal median;

        int mid = baseValues.Count / 2;
        if (baseValues.Count % 2 == 1)
        {
            median = baseValues[mid];
        }
        else
        {
            median = Math.Round((baseValues[mid - 1] + baseValues[mid]) / 2m, 3, MidpointRounding.AwayFromZero);
        }

        return new AggregatedMetricSummary(policy, metricCode, min, median, max, adverseValue, sku);
    }
}
