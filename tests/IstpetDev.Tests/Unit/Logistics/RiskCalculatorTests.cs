using IstpetDev.Application.Logistics;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public class RiskCalculatorTests
{
    private static IOptions<LogisticsOptions> DefaultOptions() =>
        Options.Create(new LogisticsOptions
        {
            DemandWindowDays = 14,
            DefaultLeadTimeDays = 2m,
            SafetyMarginDays = 3m,
            ConsolidationExtraDays = 3m,
            TargetCoverageDays = 10m,
            SafetyStockUnits = 0m,
            StaleStockDays = 7
        });

    [Fact]
    public void Demand_prorates_linearly_when_consumption_crosses_window()
    {
        var windowStart = new DateOnly(2026, 10, 1);
        var today = new DateOnly(2026, 10, 15); // ventana [2026-10-01, 2026-10-15), 14 días

        // Consumo de 10 días: [2026-09-26, 2026-10-06) con cantidad 100
        // Intersección con ventana: [2026-10-01, 2026-10-06) -> 5 días de solapamiento
        // Fracción: 5 / 10 = 0.5 -> cantidad prorrateada = 50
        var occurredOn = new DateOnly(2026, 9, 26);
        var periodDays = 10;
        var quantity = 100m;

        var (proratedQty, overlapDays) = RiskCalculator.ProrateConsumption(occurredOn, periodDays, quantity, windowStart, today);

        Assert.Equal(50m, proratedQty);
        Assert.Equal(5, overlapDays);
    }

    [Theory]
    [InlineData(1.5, RiskLevel.Critico)]  // <= 2
    [InlineData(2.0, RiskLevel.Critico)]  // límite exacto crítico
    [InlineData(2.1, RiskLevel.Alto)]     // > 2 y <= 5
    [InlineData(5.0, RiskLevel.Alto)]     // límite exacto caso del pitch: 5.0 es Alto
    [InlineData(5.1, RiskLevel.Vigilar)]  // > 5 y <= 8
    [InlineData(8.0, RiskLevel.Vigilar)]  // límite exacto vigilar
    [InlineData(8.1, RiskLevel.Normal)]   // > 8
    [InlineData(20.0, RiskLevel.Normal)]
    public void Coverage_classifies_risk_levels_with_exact_boundaries(decimal coverage, RiskLevel expectedLevel)
    {
        var level = RiskCalculator.ClassifyLevel(
            coverageDays: coverage,
            leadTimeDays: 2m,
            safetyMarginDays: 3m,
            consolidationExtraDays: 3m);

        Assert.Equal(expectedLevel, level);
    }

    [Fact]
    public void Zero_demand_or_missing_consumptions_produces_SinDatos_with_null_coverage()
    {
        var level = RiskCalculator.ClassifyLevel(
            coverageDays: null,
            leadTimeDays: 2m,
            safetyMarginDays: 3m,
            consolidationExtraDays: 3m);

        Assert.Equal(RiskLevel.SinDatos, level);
    }

    [Fact]
    public void Pitch_case_calculates_coverage_5_and_Alto()
    {
        // Caso del pitch (HOS-N01 / DES-01):
        // 60 L disponibles, 84 L consumidos en 7 días (12 L/día), lead time 2, margen 3
        // Cobertura = 60 / 12 = 5,0 días -> ALTO
        // Sugerido = max(0, 12 * 10 + 0 - 60) = 60 L
        var available = 60m;
        var dailyDemand = 12m;
        var leadTime = 2m;
        var safetyMargin = 3m;
        var targetCoverage = 10m;

        var coverage = RiskCalculator.ComputeCoverage(available, dailyDemand);
        var level = RiskCalculator.ClassifyLevel(coverage, leadTime, safetyMargin, 3m);
        var suggested = RiskCalculator.ComputeSuggestedQuantity(dailyDemand, targetCoverage, 0m, available, 0m);

        Assert.Equal(5.0m, coverage);
        Assert.Equal(RiskLevel.Alto, level);
        Assert.Equal(60.000m, suggested);
    }
}
