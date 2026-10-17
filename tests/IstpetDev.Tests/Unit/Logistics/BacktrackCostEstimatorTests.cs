using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Entities;

namespace IstpetDev.Tests.Unit.Logistics;

// #237: costo de retroceso (reentrega ida y vuelta bodega → punto) con un caso fijo.
[Trait("Category", "Unit")]
public sealed class BacktrackCostEstimatorTests
{
    private static readonly GeoPoint Warehouse = new(-0.18m, -78.48m);
    private static readonly GeoPoint Point = new(-0.10m, -78.45m);

    private static Leg Road(GeoPoint from, GeoPoint to, decimal km, decimal minutes) => new(from, to, km, minutes, DistanceSource.Road, null);

    [Fact]
    public void Fixed_case_without_cost_profile_uses_km_and_hours_formula()
    {
        // 9,2 km y 18 min por sentido; 0,45 USD/km del vehículo y 6 USD/h por defecto.
        var vehicle = new Vehicle { CostPerKm = 0.45m, MaxWeight = 1000m };
        var result = BacktrackCostEstimator.Estimate(
            Road(Warehouse, Point, 9.2m, 18m), Road(Point, Warehouse, 9.2m, 18m), vehicle, 40m, new LogisticsOptions());

        Assert.Equal(18.4m, result.Km);
        Assert.Equal(0.6m, result.Hours);
        Assert.Equal(11.88m, result.CostUsd); // 18,4 × 0,45 + 0,6 × 6
        Assert.False(result.IsApproximate);
    }

    [Fact]
    public void Without_vehicle_uses_DefaultCostPerKm_and_asymmetric_legs_add_up()
    {
        var options = new LogisticsOptions { DefaultCostPerKm = 0.45m, CostPerHour = 6m };
        var result = BacktrackCostEstimator.Estimate(
            Road(Warehouse, Point, 10m, 20m), Road(Point, Warehouse, 12m, 25m), null, 0m, options);

        Assert.Equal(22m, result.Km);
        Assert.Equal(0.75m, result.Hours);
        Assert.Equal(14.4m, result.CostUsd); // 22 × 0,45 + 0,75 × 6
    }

    [Fact]
    public void Any_straight_line_leg_marks_the_estimate_as_approximate()
    {
        var approx = new Leg(Point, Warehouse, 9.2m, 18m, DistanceSource.Approximation, null);
        var result = BacktrackCostEstimator.Estimate(Road(Warehouse, Point, 9.2m, 18m), approx, null, 0m, new LogisticsOptions());
        Assert.True(result.IsApproximate);
    }

    [Fact]
    public void With_cost_profile_uses_the_service_cost_breakdown_of_240_without_helpers_or_shrink()
    {
        var options = ServiceCostCalculatorTests.Configured();
        var vehicle = new Vehicle { VehicleType = "camioneta", MaxWeight = 1000m, CostPerKm = 0.45m };
        var result = BacktrackCostEstimator.Estimate(
            Road(Warehouse, Point, 50m, 60m), Road(Point, Warehouse, 50m, 60m), vehicle, 0m, options);

        var expected = new ServiceCostCalculator(options).Calculate(100m, 120m, vehicle, 0m, []);
        Assert.Equal(expected.Total, result.CostUsd);
        Assert.Equal(0m, expected.Helpers);
        Assert.Equal(0m, expected.Shrink);
        Assert.Equal(2m, result.Hours);
    }

    [Theory]
    [InlineData("Faltante", true)]
    [InlineData("Daño", true)]
    [InlineData("ErrorRotulado", true)]
    [InlineData("mislabeled", true)]
    [InlineData("Sobrante", false)]
    [InlineData("CadenaDeFrio", false)]
    [InlineData("PrecintoViolado", false)]
    [InlineData("LoteNoCoincide", false)]
    [InlineData("texto libre", false)]
    [InlineData(null, false)]
    public void Only_shortage_damage_or_mislabel_require_redelivery(string? reason, bool expected) =>
        Assert.Equal(expected, BacktrackCostEstimator.RequiresRedelivery(reason));
}
