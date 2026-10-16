using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class SingleProposalAndCostTests
{
    [Fact]
    public void CanonicalCostCalculator_ComputesExactFixture_87Dollars()
    {
        // Fixture canónico oficial de issue #207:
        // 100 km, 3 h (180 min), $0.50/km, $10.00/h, $7.00 peajes = $87.00
        decimal km = 100m;
        decimal minutes = 180m;
        decimal costPerKm = 0.50m;
        decimal costPerHour = 10.00m;
        decimal tolls = 7.00m;

        decimal totalCost = LogisticsCostCalculator.CalculateCost(km, minutes, costPerKm, costPerHour, tolls);
        Assert.Equal(87.00m, totalCost);

        var breakdown = LogisticsCostCalculator.ComputeBreakdown(km, minutes, costPerKm, costPerHour, tolls);
        Assert.Equal(50.00m, breakdown.KmCost);
        Assert.Equal(30.00m, breakdown.HourCost);
        Assert.Equal(7.00m, breakdown.TollsCost);
        Assert.Equal(87.00m, breakdown.TotalCost);
        Assert.Equal(0.50m, breakdown.CostPerKm);
        Assert.Equal(10.00m, breakdown.CostPerHour);
        Assert.Equal(100m, breakdown.DistanceKm);
        Assert.Equal(3.0m, breakdown.DurationHours);
        Assert.Equal("USD", breakdown.Currency);
        Assert.Equal(LogisticsCostCalculator.Version, breakdown.CalculatorVersion);
    }

    [Fact]
    public void CostUncertaintyService_ReturnsUncalibrated_WhenNoHistoricalData()
    {
        var result = CostUncertaintyService.GetUncalibratedResult();

        Assert.Equal("uncalibrated", result.Status);
        Assert.Null(result.LowerBound);
        Assert.Null(result.UpperBound);
        Assert.Null(result.MarginOfError);
        Assert.Equal(CostUncertaintyService.MethodName, result.Method);
        Assert.Equal(0, result.SampleSize);
        Assert.Equal(CostUncertaintyService.UncalibratedMessage, result.Message);

        var evaluatedWithoutData = CostUncertaintyService.Evaluate(87.00m, null);
        Assert.Equal("uncalibrated", evaluatedWithoutData.Status);
        Assert.Null(evaluatedWithoutData.LowerBound);
        Assert.Null(evaluatedWithoutData.UpperBound);
    }

    [Fact]
    public void CostUncertaintyService_ReturnsCalibrated_WhenSufficientResidualsProvided()
    {
        // 12 viajes históricos reales con costo estimado vs conciliado
        var baseDate = new DateOnly(2026, 10, 1);
        var historical = new List<HistoricalTripCost>();
        var deltas = new[] { -5.0m, -3.2m, -2.0m, -1.0m, -0.5m, 0.2m, 0.8m, 1.5m, 2.2m, 3.0m, 4.5m, 5.0m };
        for (int i = 0; i < deltas.Length; i++)
        {
            decimal est = 80m + i * 2;
            decimal actual = est + deltas[i];
            historical.Add(new HistoricalTripCost(est, actual, baseDate.AddDays(i)));
        }

        var result = CostUncertaintyService.Evaluate(87.00m, historical, targetCoverage: 0.90m);

        Assert.Equal("calibrated", result.Status);
        Assert.NotNull(result.LowerBound);
        Assert.NotNull(result.UpperBound);
        Assert.NotNull(result.MarginOfError);
        Assert.True(result.LowerBound < 87.00m);
        Assert.True(result.UpperBound > 87.00m);
        Assert.Equal(CostUncertaintyService.MethodName, result.Method);
        Assert.Equal(12, result.SampleSize);
        Assert.NotNull(result.MeanBias);
        Assert.NotNull(result.Mae);
        Assert.NotNull(result.EmpiricalCoverage);
    }

    [Fact]
    public void DeliveryStopSequencer_MaintainsStrictPriorityOrdering()
    {
        var depot = new GeoPoint(-0.2295m, -78.5243m);
        var day = new DateTime(2026, 10, 17, 15, 0, 0, DateTimeKind.Utc);

        var drafts = new[]
        {
            new DeliveryStopDraft(new RouteStopCandidate(0, RouteStopKind.Delivery, Guid.NewGuid(), null, "DEL-LOW", "Punto Bajo", new GeoPoint(-0.23m, -78.52m), -10m, -0.1m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15), "Low", day),
            new DeliveryStopDraft(new RouteStopCandidate(0, RouteStopKind.Delivery, Guid.NewGuid(), null, "DEL-CRIT", "Punto Critico", new GeoPoint(-0.25m, -78.53m), -10m, -0.1m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15), "Critical", day),
            new DeliveryStopDraft(new RouteStopCandidate(0, RouteStopKind.Delivery, Guid.NewGuid(), null, "DEL-NORM", "Punto Normal", new GeoPoint(-0.24m, -78.51m), -10m, -0.1m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15), "Normal", day),
            new DeliveryStopDraft(new RouteStopCandidate(0, RouteStopKind.Delivery, Guid.NewGuid(), null, "DEL-HIGH", "Punto Alto", new GeoPoint(-0.26m, -78.50m), -10m, -0.1m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15), "High", day),
        };

        var ordered = DeliveryStopSequencer.Order(drafts, depot);
        var codes = ordered.Select(d => d.Stop.Code).ToArray();

        Assert.Equal(4, ordered.Count);
        Assert.Equal("DEL-CRIT", codes[0]);
        Assert.Equal("DEL-HIGH", codes[1]);
        Assert.Equal("DEL-NORM", codes[2]);
        Assert.Equal("DEL-LOW", codes[3]);
    }
}
