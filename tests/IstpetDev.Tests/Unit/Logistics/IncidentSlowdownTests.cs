using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Entities;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

// #246: incidentes de retraso parcial (Congestion, Manifestacion, Accidente).
[Trait("Category", "Unit")]
public sealed class IncidentSlowdownTests
{
    private static readonly GeoPoint Depot = new(-0.2295m, -78.5243m);
    private static readonly GeoPoint Point = new(-0.2000m, -78.5000m);
    private static readonly DateTime StartUtc = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc); // 07:00 local
    private static readonly Vehicle Truck = new() { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m };

    private static readonly Dictionary<(GeoPoint, GeoPoint), Leg> Matrix = new()
    {
        [(Depot, Point)] = new(Depot, Point, 6m, 15m, DistanceSource.Road, null),
        [(Point, Depot)] = new(Point, Depot, 6m, 15m, DistanceSource.Road, null)
    };

    private static StopCandidate Critical(TimeOnly windowEnd) => new(
        Guid.NewGuid(), "HOS-01", Point, new TimeOnly(7, 0), windowEnd, 0, 10m, 0.1m, Array.Empty<StopLoad>(),
        false, RiskLevel.Critico, "alta", 1m, 2m);

    private static IncidentEffect Manifestacion(decimal slowdownFactor) => new(
        new HashSet<(GeoPoint, GeoPoint)>(),
        new Dictionary<(GeoPoint, GeoPoint), decimal> { [(Depot, Point)] = IncidentSlowdown.MinutesMultiplier(slowdownFactor) },
        new HashSet<string>());

    private static FeasibilityCheck Evaluate(StopCandidate stop, IncidentEffect? effect, ReplanMode mode = ReplanMode.Strict) =>
        RouteFeasibility.EvaluateRoute(Truck, Depot, new[] { stop }, StartUtc, 10, 10m, 0.5m, Matrix, effect, mode);

    [Theory]
    [InlineData("Congestion", true)]
    [InlineData("manifestacion", true)]
    [InlineData("Accidente", true)]
    [InlineData("BloqueoVial", false)]
    [InlineData("ROAD_CLOSURE", false)]
    [InlineData("DELAY", false)]
    [InlineData("VEHICLE_BREAKDOWN", false)]
    public void Only_partial_delay_kinds_use_a_slowdown_factor(string kind, bool expected) =>
        Assert.Equal(expected, IncidentSlowdown.IsSlowdownKind(kind));

    [Theory]
    [InlineData(0.3, "Marcha hacia la Plaza Grande (velocidad al 30 %)")]
    [InlineData(0.64, "Marcha hacia la Plaza Grande (velocidad al 64 %)")]
    [InlineData(0.425, "Marcha hacia la Plaza Grande (velocidad al 42.5 %)")]
    public void Factor_is_stored_at_the_end_of_the_description_and_read_back(double factor, string expected)
    {
        var description = IncidentSlowdown.WithFactor("Marcha hacia la Plaza Grande", (decimal)factor);

        Assert.Equal(expected, description);
        Assert.Equal((decimal)factor, IncidentSlowdown.ReadFactor(description));
        // Guardar otra vez reemplaza la marca en lugar de duplicarla.
        Assert.Equal("Marcha hacia la Plaza Grande (velocidad al 50 %)", IncidentSlowdown.WithFactor(description, 0.5m));
    }

    [Fact]
    public void Description_without_a_valid_marker_has_no_factor()
    {
        Assert.Null(IncidentSlowdown.ReadFactor(null));
        Assert.Null(IncidentSlowdown.ReadFactor("Cierre por inundación"));
        Assert.Null(IncidentSlowdown.ReadFactor("Marcha (velocidad al 0 %)"));
        Assert.Null(IncidentSlowdown.ReadFactor("Marcha (velocidad al 30 %) y algo más"));
        Assert.Equal("(velocidad al 30 %)", IncidentSlowdown.WithFactor(null, 0.3m));
        Assert.False(IncidentSlowdown.IsValidFactor(0m));
        Assert.False(IncidentSlowdown.IsValidFactor(1.2m));
        Assert.True(IncidentSlowdown.IsValidFactor(1m));
    }

    [Fact]
    public void RouteFeasibility_manifestacion_at_0_3_raises_the_eta_without_discarding_the_route()
    {
        var stop = Critical(new TimeOnly(11, 0));

        var normal = Evaluate(stop, null);
        var slowed = Evaluate(stop, Manifestacion(0.3m));

        Assert.True(normal.IsFeasible);
        Assert.True(slowed.IsFeasible); // la vía no se cierra
        Assert.Equal(15m, normal.Legs[0].Minutes);
        Assert.Equal(50m, Math.Round(slowed.Legs[0].Minutes, 6)); // 15 / 0,3
        Assert.Equal(15m, slowed.Legs[1].Minutes); // el regreso queda fuera del radio
        Assert.True(slowed.CumulativeMinutes > normal.CumulativeMinutes);
    }

    [Fact]
    public void RouteFeasibility_critical_pushed_out_of_its_window_needs_a_replan_option()
    {
        // Sale 07:00: sin incidente llega 07:15; con la manifestación llega 07:50, después del cierre de las 07:30.
        var stop = Critical(new TimeOnly(7, 30));

        Assert.True(Evaluate(stop, null).IsFeasible);
        var strict = Evaluate(stop, Manifestacion(0.3m));
        Assert.False(strict.IsFeasible);
        Assert.Equal("time_window", strict.FailureReason);
        // Es lo que hace que Replanner escale y ofrezca late_service y prioritize_critical.
        Assert.True(Evaluate(stop, Manifestacion(0.3m), ReplanMode.LateService).IsFeasible);
    }
}
