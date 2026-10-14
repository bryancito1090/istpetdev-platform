using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class ComparisonAggregatorTests
{
    [Fact]
    public void Base_Seeds_Aggregate_Min_Median_Max_And_Exclude_Adverse_Scenario()
    {
        var runs = new List<SeedPolicyMetric>
        {
            new(Seed: 11, Scenario: "base", Policy: "reactive-v1", MetricCode: "saving_km", Value: 10m),
            new(Seed: 12, Scenario: "base", Policy: "reactive-v1", MetricCode: "saving_km", Value: 20m),
            new(Seed: 13, Scenario: "base", Policy: "reactive-v1", MetricCode: "saving_km", Value: 30m),
            new(Seed: 11, Scenario: "adverse", Policy: "reactive-v1", MetricCode: "saving_km", Value: 50m) // Adverso
        };

        var summary = ComparisonAggregator.AggregateMetric(runs, "reactive-v1", "saving_km");

        Assert.Equal(10m, summary.Min);
        Assert.Equal(20m, summary.Median);
        Assert.Equal(30m, summary.Max);
        Assert.Equal(50m, summary.AdverseValue);
    }
}
