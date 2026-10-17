using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.Extensions.Options;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

// #245: hora pico. Franjas sintéticas del issue (06:30–09:30 a 0,5 y 16:00–19:30 a 0,6).
[Trait("Category", "Unit")]
public sealed class TrafficBandTests
{
    private static readonly GeoPoint Depot = new(-0.2295m, -78.5243m);
    private static readonly GeoPoint Point = new(-0.2000m, -78.5000m);
    private static readonly DateTime Day = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Vehicle Truck = new() { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m };

    private static readonly List<TrafficBandOptions> Bands =
    [
        new() { Start = "06:30", End = "09:30", SpeedFactor = 0.5m },
        new() { Start = "16:00", End = "19:30", SpeedFactor = 0.6m }
    ];

    private static readonly Dictionary<(GeoPoint, GeoPoint), Leg> Matrix = new()
    {
        [(Depot, Point)] = new(Depot, Point, 10m, 20m, DistanceSource.Road, null),
        [(Point, Depot)] = new(Point, Depot, 10m, 20m, DistanceSource.Road, null)
    };

    // Hora local (UTC-5) → UTC del mismo día.
    private static DateTime Local(int hour, int minute) => Day.AddHours(hour + 5).AddMinutes(minute);

    private static StopCandidate Stop(TimeOnly windowStart, TimeOnly windowEnd) => new(
        Guid.NewGuid(), "P1", Point, windowStart, windowEnd, 0, 10m, 0.1m, Array.Empty<StopLoad>(),
        false, RiskLevel.Alto, "media", 5m, 2m);

    private static FeasibilityCheck Evaluate(DateTime startUtc, StopCandidate stop, IReadOnlyList<TrafficBandOptions>? bands,
        ReplanMode mode = ReplanMode.Strict) =>
        RouteFeasibility.EvaluateRoute(Truck, Depot, new[] { stop }, startUtc, 12, 10m, 0.5m, Matrix,
            replanMode: mode, trafficBands: bands);

    [Fact]
    public void RouteFeasibility_same_leg_takes_twice_as_long_at_0730_than_at_1100_with_factor_half()
    {
        var stop = Stop(new TimeOnly(6, 0), new TimeOnly(18, 0));

        var peak = Evaluate(Local(7, 30), stop, Bands);
        var offPeak = Evaluate(Local(11, 0), stop, Bands);

        Assert.True(peak.IsFeasible);
        Assert.True(offPeak.IsFeasible);
        Assert.Equal(40m, peak.Legs[0].Minutes);
        Assert.Equal(20m, offPeak.Legs[0].Minutes);
        Assert.Equal(2m * offPeak.Legs[0].Minutes, peak.Legs[0].Minutes);
        // El regreso sale a las 08:10, todavía en hora pico; a las 11:20 ya no.
        Assert.Equal(40m, peak.Legs[1].Minutes);
        Assert.Equal(20m, offPeak.Legs[1].Minutes);
    }

    [Fact]
    public void RouteFeasibility_respects_windows_with_the_corrected_eta()
    {
        // Sale 07:00: sin hora pico llega 07:20; con factor 0,5 llega 07:40, después del cierre de las 07:30.
        var stop = Stop(new TimeOnly(6, 0), new TimeOnly(7, 30));

        Assert.True(Evaluate(Local(7, 0), stop, null).IsFeasible);
        var strict = Evaluate(Local(7, 0), stop, Bands);
        Assert.False(strict.IsFeasible);
        Assert.Equal("time_window", strict.FailureReason);
        // En servicio tardío la ruta se acepta con la ETA corregida.
        Assert.True(Evaluate(Local(7, 0), stop, Bands, ReplanMode.LateService).IsFeasible);

        // La espera también se calcula con la ETA corregida: llega 07:40 y espera a las 08:00.
        var waiting = Evaluate(Local(7, 0), Stop(new TimeOnly(8, 0), new TimeOnly(12, 0)), Bands);
        Assert.True(waiting.IsFeasible);
        Assert.Equal(40m + 40m, waiting.CumulativeMinutes); // ida 40 (07:00) y regreso 40 (08:00, hora pico)
    }

    [Fact]
    public void RouteFeasibility_without_configuration_durations_do_not_change()
    {
        var stop = Stop(new TimeOnly(6, 0), new TimeOnly(18, 0));
        var baseline = RouteFeasibility.EvaluateRoute(Truck, Depot, new[] { stop }, Local(7, 30), 12, 10m, 0.5m, Matrix);

        foreach (var bands in new IReadOnlyList<TrafficBandOptions>?[] { null, [] })
        {
            var result = Evaluate(Local(7, 30), stop, bands);
            Assert.Equal(baseline.CumulativeMinutes, result.CumulativeMinutes);
            Assert.Equal(baseline.CumulativeCost, result.CumulativeCost);
            Assert.Equal(baseline.Legs.Select(l => l.Minutes), result.Legs.Select(l => l.Minutes));
        }
        Assert.Equal(40m, baseline.CumulativeMinutes);
    }

    [Theory]
    [InlineData("06:29", 1.0)]
    [InlineData("06:30", 0.5)] // el inicio de la franja cuenta
    [InlineData("09:29", 0.5)]
    [InlineData("09:30", 1.0)] // el fin no
    [InlineData("11:00", 1.0)]
    [InlineData("17:00", 0.6)]
    public void SpeedFactorAt_uses_the_band_of_the_leg_start(string local, double expected)
    {
        Assert.Equal((decimal)expected, TrafficBands.SpeedFactorAt(Bands, TimeOnly.Parse(local)));
    }

    [Fact]
    public void SpeedFactorAt_ignores_invalid_bands_and_takes_the_slowest_on_overlap()
    {
        List<TrafficBandOptions> bands =
        [
            new() { Start = "7:00", End = "09:00", SpeedFactor = 0.8m },
            new() { Start = "08:00", End = "10:00", SpeedFactor = 0.64m },
            new() { Start = "x", End = "10:00", SpeedFactor = 0.1m },
            new() { Start = "10:00", End = "09:00", SpeedFactor = 0.1m },
            new() { Start = "06:00", End = "12:00", SpeedFactor = 0m },
            new() { Start = "06:00", End = "12:00", SpeedFactor = 1.5m }
        ];

        Assert.Equal(0.8m, TrafficBands.SpeedFactorAt(bands, new TimeOnly(7, 30)));
        Assert.Equal(0.64m, TrafficBands.SpeedFactorAt(bands, new TimeOnly(8, 30)));
        Assert.Equal(1m, TrafficBands.SpeedFactorAt(bands, new TimeOnly(11, 0)));
        Assert.Null(TrafficBands.Reported(1m));
        Assert.Equal(0.64m, TrafficBands.Reported(0.64m));
    }

    [Fact]
    public async Task RouteEvaluator_applies_the_band_to_the_leg_and_reports_the_factor()
    {
        static List<RouteStopCandidate> Sequence() =>
        [
            new(1, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-01", "Bodega", Depot, 50m, 0.5m, new TimeOnly(5, 0), new TimeOnly(20, 0), 0),
            new(2, RouteStopKind.Delivery, Guid.NewGuid(), null, "PTO-01", "Punto", Point, -50m, -0.5m, new TimeOnly(5, 0), new TimeOnly(20, 0), 0)
        ];
        var plain = new RouteEvaluator(new FixedDistance(), Options.Create(new LogisticsOptions()));
        var peakHours = new RouteEvaluator(new FixedDistance(), Options.Create(new LogisticsOptions { TrafficBands = Bands }));

        var baseline = await plain.EvaluateSequenceAsync(Sequence(), null, Local(7, 30));
        var peak = await peakHours.EvaluateSequenceAsync(Sequence(), null, Local(7, 30));
        var offPeak = await peakHours.EvaluateSequenceAsync(Sequence(), null, Local(11, 0));

        Assert.Equal(20m, baseline.Stops[1].LegDurationMinutes);
        Assert.Null(baseline.Stops[1].TrafficFactorApplied);
        Assert.Equal(40m, peak.Stops[1].LegDurationMinutes);
        Assert.Equal(0.5m, peak.Stops[1].TrafficFactorApplied);
        Assert.Equal(Local(8, 10), peak.Stops[1].PlannedArrivalUtc);
        Assert.Equal(20m, offPeak.Stops[1].LegDurationMinutes);
        Assert.Null(offPeak.Stops[1].TrafficFactorApplied);
    }

    private sealed class FixedDistance : IDistanceProvider
    {
        public Task<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>> MatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>>(Matrix);
    }
}
