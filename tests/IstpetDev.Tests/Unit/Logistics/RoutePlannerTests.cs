using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Entities;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class RoutePlannerTests
{
    private readonly GeoPoint _depot = new(-0.2295m, -78.5243m);
    private readonly DateTime _workdayStartUtc = new(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc); // 08:00 Local (UTC-5)
    private readonly int _workdayHours = 8;
    private readonly decimal _costPerHour = 10m;
    private readonly decimal _costPerKm = 0.5m;

    [Fact]
    public void Feasibility_rejects_capacity_weight_and_volume_independently()
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Code = "VEH-01",
            MaxWeight = 100m,
            MaxVolume = 5m
        };

        var p1 = new GeoPoint(-0.2000m, -78.5000m);
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(_depot, p1)] = new(_depot, p1, 10m, 15m, DistanceSource.Road, null),
            [(p1, _depot)] = new(p1, _depot, 10m, 15m, DistanceSource.Road, null)
        };

        // 1. Excede peso
        var heavyStop = CreateStop("P1", p1, weight: 120m, volume: 2m);
        var evalHeavy = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { heavyStop }, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);
        Assert.False(evalHeavy.IsFeasible);
        Assert.Equal("capacity", evalHeavy.FailureReason);

        // 2. Excede volumen
        var bulkyStop = CreateStop("P1", p1, weight: 50m, volume: 8m);
        var evalBulky = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { bulkyStop }, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);
        Assert.False(evalBulky.IsFeasible);
        Assert.Equal("capacity", evalBulky.FailureReason);
    }

    [Fact]
    public void Feasibility_respects_time_window_early_arrival_wait_and_late_arrival_rejection()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m };
        var p1 = new GeoPoint(-0.2000m, -78.5000m);
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(_depot, p1)] = new(_depot, p1, 10m, 15m, DistanceSource.Road, null), // Llega 08:15 local
            [(p1, _depot)] = new(p1, _depot, 10m, 15m, DistanceSource.Road, null)
        };

        // Ventana empieza 09:00 -> Llega 08:15 -> Espera 45 minutos
        var earlyStop = CreateStop("P1", p1, windowStart: new TimeOnly(9, 0), windowEnd: new TimeOnly(12, 0), serviceMinutes: 15);
        var evalEarly = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { earlyStop }, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);
        Assert.True(evalEarly.IsFeasible);

        // Ventana termina 08:10 -> Llega 08:15 -> Tarde en modo Strict -> Rechazado
        var lateStop = CreateStop("P1", p1, windowStart: new TimeOnly(7, 0), windowEnd: new TimeOnly(8, 10), serviceMinutes: 15);
        var evalLate = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { lateStop }, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);
        Assert.False(evalLate.IsFeasible);
        Assert.Equal("time_window", evalLate.FailureReason);
    }

    [Fact]
    public void Feasibility_rejects_exceeding_workday_hours()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m };
        var p1 = new GeoPoint(-0.2000m, -78.5000m);
        // Viaje de 5 horas ida + 4 horas vuelta = 9 horas > 8 horas workday
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(_depot, p1)] = new(_depot, p1, 100m, 300m, DistanceSource.Road, null),
            [(p1, _depot)] = new(p1, _depot, 100m, 240m, DistanceSource.Road, null)
        };

        var stop = CreateStop("P1", p1, windowStart: new TimeOnly(8, 0), windowEnd: new TimeOnly(18, 0), serviceMinutes: 30);
        var eval = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { stop }, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);

        Assert.False(eval.IsFeasible);
        Assert.Equal("workday", eval.FailureReason);
    }

    [Fact]
    public void Feasibility_rejects_unreachable_point_missing_matrix_entry()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m };
        var p1 = new GeoPoint(-0.2000m, -78.5000m);
        var emptyMatrix = new Dictionary<(GeoPoint, GeoPoint), Leg>();

        var stop = CreateStop("P1", p1);
        var eval = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { stop }, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, emptyMatrix);

        Assert.False(eval.IsFeasible);
        Assert.Equal("unreachable", eval.FailureReason);
    }

    [Fact]
    public void Feasibility_calculates_cost_strictly_by_formula()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m, CostPerKm = 0.5m };
        var p1 = new GeoPoint(-0.2000m, -78.5000m);
        // 20 km total, 60 minutos viaje + 60 minutos servicio = 2 horas total
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(_depot, p1)] = new(_depot, p1, 10m, 30m, DistanceSource.Road, null),
            [(p1, _depot)] = new(p1, _depot, 10m, 30m, DistanceSource.Road, null)
        };

        var stop = CreateStop("P1", p1, serviceMinutes: 60);
        var eval = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { stop }, _workdayStartUtc, _workdayHours,
            costPerHour: 20m, defaultCostPerKm: 0.5m, matrix);

        Assert.True(eval.IsFeasible);
        Assert.Equal(20m, eval.CumulativeKm);
        // Costo = 20 km * 0.5 USD/km + 2 horas * 20 USD/h = 10 + 40 = 50 USD
        Assert.Equal(50m, eval.CumulativeCost);
    }

    [Fact]
    public void Replanning_returns_to_real_depot_applies_delay_and_keeps_original_workday_end()
    {
        var position = new GeoPoint(-0.2100m, -78.5100m);
        var point = new GeoPoint(-0.2000m, -78.5000m);
        var vehicle = new Vehicle { Code = "V", MaxWeight = 1000m };
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(position, point)] = new(position, point, 2, 10, DistanceSource.Road, null),
            [(point, _depot)] = new(point, _depot, 5, 20, DistanceSource.Road, null)
        };
        var effect = new IncidentEffect(new HashSet<(GeoPoint, GeoPoint)>(),
            new Dictionary<(GeoPoint, GeoPoint), decimal> { [(position, point)] = 2m }, new HashSet<string>());
        var ready = _workdayStartUtc.AddHours(7);
        var stop = CreateStop("P", point, serviceMinutes: 15);
        var result = RouteFeasibility.EvaluateRoute(vehicle, position, new[] { stop }, ready,
            8, 10, .5m, matrix, effect, ReplanMode.Strict, _depot, _workdayStartUtc.AddHours(8));
        Assert.True(result.IsFeasible);
        Assert.Equal(55m, result.CumulativeMinutes);
        Assert.Equal(20m, result.Legs[0].Minutes);
        Assert.Equal(_depot, result.Legs.Last().To);
        var expired = RouteFeasibility.EvaluateRoute(vehicle, position, new[] { stop }, ready.AddMinutes(10),
            8, 10, .5m, matrix, effect, ReplanMode.Strict, _depot, _workdayStartUtc.AddHours(8));
        Assert.False(expired.IsFeasible);
        Assert.Equal("workday", expired.FailureReason);
        var closed = effect with { BlockedLegs = new HashSet<(GeoPoint, GeoPoint)> { (position, point) } };
        Assert.Equal("blocked_leg", RouteFeasibility.EvaluateRoute(vehicle, position, new[] { stop }, ready,
            8, 10, .5m, matrix, closed, returnDepot: _depot).FailureReason);
    }

    [Theory]
    [InlineData(ReplanMode.Strict, false, false)]
    [InlineData(ReplanMode.PrioritizeCritical, false, false)]
    [InlineData(ReplanMode.LateService, false, true)]
    [InlineData(ReplanMode.LateService, true, false)]
    public void Only_late_service_relaxes_mandatory_windows(ReplanMode mode, bool consolidation, bool feasible)
    {
        var point = new GeoPoint(-0.2m, -78.5m);
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(_depot, point)] = new(_depot, point, 2, 20, DistanceSource.Road, null),
            [(point, _depot)] = new(point, _depot, 2, 20, DistanceSource.Road, null)
        };
        var stop = CreateStop("P", point, windowEnd: new TimeOnly(8, 10)) with { IsConsolidation = consolidation };
        var result = RouteFeasibility.EvaluateRoute(new Vehicle { Code = "V", MaxWeight = 1000 }, _depot,
            new[] { stop }, _workdayStartUtc, 8, 10, .5m, matrix, replanMode: mode);
        Assert.Equal(feasible, result.IsFeasible);
    }

    private static StopCandidate CreateStop(
        string code,
        GeoPoint pos,
        decimal weight = 10m,
        decimal volume = 0.5m,
        TimeOnly? windowStart = null,
        TimeOnly? windowEnd = null,
        int serviceMinutes = 15)
    {
        return new StopCandidate(
            Guid.NewGuid(),
            code,
            pos,
            windowStart ?? new TimeOnly(8, 0),
            windowEnd ?? new TimeOnly(17, 0),
            serviceMinutes,
            weight,
            volume,
            new[] { new StopLoad(Guid.NewGuid(), "SKU-01", 10m, "U") },
            false,
            RiskLevel.Critico,
            "alta",
            1m,
            2m);
    }
}
