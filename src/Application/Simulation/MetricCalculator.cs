namespace IstpetDev.Application.Simulation;

public sealed record UnattendedDemandItem(string PointCode, string Sku, string Unit, decimal Quantity);
public sealed record GroupedUnattendedDemand(string Sku, string Unit, decimal TotalQuantity);

public static class MetricCalculator
{
    public const string DefinitionVersion = "metric-v1";

    public static (int Avoided, int NewlyIntroduced, int NetDifference) CalculateStockoutComparison(
        IReadOnlySet<string> baselineStockouts,
        IReadOnlySet<string> platformStockouts)
    {
        // Claves con quiebre en baseline y sin quiebre en plataforma
        int avoided = baselineStockouts.Count(k => !platformStockouts.Contains(k));

        // Claves sin quiebre en baseline y con quiebre en plataforma
        int newlyIntroduced = platformStockouts.Count(k => !baselineStockouts.Contains(k));

        int netDifference = baselineStockouts.Count - platformStockouts.Count;

        return (avoided, newlyIntroduced, netDifference);
    }

    public static MetricValue ComputeRatio(decimal numerator, decimal denominator, string unit, string code = "ratio")
    {
        if (denominator == 0m)
        {
            return new MetricValue(code, DefinitionVersion, null, numerator, denominator, null, unit, "zero_denominator");
        }

        var val = Math.Round(numerator / denominator, 3, MidpointRounding.AwayFromZero);
        return new MetricValue(code, DefinitionVersion, null, numerator, denominator, val, unit, null);
    }

    public static MetricValue ComputePercentage(decimal numerator, decimal denominator, string code)
    {
        if (denominator == 0m)
        {
            return new MetricValue(code, DefinitionVersion, null, numerator, denominator, null, "%", "zero_denominator");
        }

        var val = Math.Round((numerator / denominator) * 100m, 3, MidpointRounding.AwayFromZero);
        return new MetricValue(code, DefinitionVersion, null, numerator, denominator, val, "%", null);
    }

    public static (MetricValue Saving, MetricValue Reduction) ComputeSavings(decimal baseline, decimal platform, string unit)
    {
        var savingVal = Math.Round(baseline - platform, 3, MidpointRounding.AwayFromZero);
        var savingMetric = new MetricValue("saving_" + unit, DefinitionVersion, null, savingVal, null, savingVal, unit, null);

        if (baseline == 0m)
        {
            var zeroReduction = new MetricValue("reduction_" + unit, DefinitionVersion, null, savingVal, baseline, null, "%", "zero_denominator");
            return (savingMetric, zeroReduction);
        }

        var reductionPct = Math.Round((savingVal / baseline) * 100m, 3, MidpointRounding.AwayFromZero);
        var reductionMetric = new MetricValue("reduction_" + unit, DefinitionVersion, null, savingVal, baseline, reductionPct, "%", null);

        return (savingMetric, reductionMetric);
    }

    public static IReadOnlyList<GroupedUnattendedDemand> GroupUnattendedDemand(IEnumerable<UnattendedDemandItem> items)
    {
        return items
            .GroupBy(i => (i.Sku, i.Unit))
            .OrderBy(g => g.Key.Sku)
            .Select(g => new GroupedUnattendedDemand(
                g.Key.Sku,
                g.Key.Unit,
                Math.Round(g.Sum(x => x.Quantity), 3, MidpointRounding.AwayFromZero)))
            .ToList();
    }

    public static MetricValue BuildTraceabilityMetric(bool isCustodySimulated)
    {
        if (!isCustodySimulated)
        {
            return new MetricValue("trazabilidad_completa", DefinitionVersion, null, 0m, 0m, null, "%", "not_simulated");
        }

        return new MetricValue("trazabilidad_completa", DefinitionVersion, null, 100m, 100m, 100m, "%", null);
    }
}
