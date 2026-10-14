using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class MetricCalculatorTests
{
    [Fact]
    public void Avoided_And_New_Stockouts_Are_Calculated_Separately_Without_Netting()
    {
        // Baseline quiebra en A y B
        var baselineStockouts = new HashSet<string> { "P01:SKU-01:2026-10-12", "P02:SKU-01:2026-10-12" };
        // Plataforma quiebra en B y C
        var platformStockouts = new HashSet<string> { "P02:SKU-01:2026-10-12", "P03:SKU-01:2026-10-12" };

        var (avoided, newlyIntroduced, netDifference) = MetricCalculator.CalculateStockoutComparison(baselineStockouts, platformStockouts);

        // Claves con quiebre en baseline y sin quiebre en plataforma = A (1)
        Assert.Equal(1, avoided);
        // Claves sin quiebre en baseline y con quiebre en plataforma = C (1)
        Assert.Equal(1, newlyIntroduced);
        // Diferencia neta = 0 (2 - 2)
        Assert.Equal(0, netDifference);
    }

    [Fact]
    public void Delivery_Averages_And_Punctuality_Compute_Expected_Ratios()
    {
        // km = 100, cost = 200, deliveries = 4
        var kmPerDelivery = MetricCalculator.ComputeRatio(numerator: 100m, denominator: 4m, unit: "km/entrega");
        var costPerDelivery = MetricCalculator.ComputeRatio(numerator: 200m, denominator: 4m, unit: "$/entrega");

        Assert.Equal(25m, kmPerDelivery.Value);
        Assert.Equal(50m, costPerDelivery.Value);

        // Puntualidad: 3 a tiempo de 4 obligaciones vencidas = 75%
        var punctuality = MetricCalculator.ComputePercentage(numerator: 3m, denominator: 4m, code: "puntualidad");
        Assert.Equal(75m, punctuality.Value);
    }

    [Fact]
    public void Savings_And_Percentage_Reduction_With_Zero_Denominator_Returns_Null_And_Reason()
    {
        // Baseline = 100, Plataforma = 80 -> Ahorro 20 km, reducción 20%
        var (saving, reduction) = MetricCalculator.ComputeSavings(baseline: 100m, platform: 80m, unit: "km");
        Assert.Equal(20m, saving.Value);
        Assert.Equal(20m, reduction.Value);

        // Baseline = 0 -> Denominador cero
        var (_, reductionZero) = MetricCalculator.ComputeSavings(baseline: 0m, platform: 0m, unit: "km");
        Assert.Null(reductionZero.Value);
        Assert.Equal("zero_denominator", reductionZero.NotApplicableReason);
    }

    [Fact]
    public void Unattended_Demand_Keeps_Skus_And_Units_Strictly_Separate()
    {
        var records = new List<UnattendedDemandItem>
        {
            new("P01", "DES-01", "L", 5m),
            new("P02", "DES-01", "L", 3m),
            new("P01", "PAP-01", "UN", 10m)
        };

        var grouped = MetricCalculator.GroupUnattendedDemand(records);

        Assert.Equal(2, grouped.Count);

        var des = grouped.First(g => g.Sku == "DES-01");
        Assert.Equal(8m, des.TotalQuantity);
        Assert.Equal("L", des.Unit);

        var pap = grouped.First(g => g.Sku == "PAP-01");
        Assert.Equal(10m, pap.TotalQuantity);
        Assert.Equal("UN", pap.Unit);
    }

    [Fact]
    public void Simulation_Traceability_Without_Custody_Returns_Not_Simulated()
    {
        var metric = MetricCalculator.BuildTraceabilityMetric(isCustodySimulated: false);

        Assert.Null(metric.Value);
        Assert.Equal("not_simulated", metric.NotApplicableReason);
    }
}
