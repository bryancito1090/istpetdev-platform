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

    [Theory]
    // Crítico: el fin de ventana se recorta a la hora límite
    [InlineData("08:00", "17:00", true, "11:00", "11:00")]
    // Crítico con ventana que ya termina antes: se conserva la del punto
    [InlineData("08:00", "10:00", true, "11:00", "10:00")]
    // Crítico cuya ventana empieza después de la hora límite: se respeta la del punto
    [InlineData("12:00", "16:00", true, "11:00", "16:00")]
    // No crítico: conserva su ventana
    [InlineData("08:00", "17:00", false, "11:00", "17:00")]
    // Sin hora límite: comportamiento actual
    [InlineData("08:00", "17:00", true, null, "17:00")]
    public void Critical_deadline_caps_only_critical_window_end(
        string start, string end, bool critical, string? deadline, string expected)
    {
        var result = RoutePlanner.ApplyCriticalDeadline(
            TimeOnly.Parse(start), TimeOnly.Parse(end), critical, RoutePlanner.ParseCriticalDeadline(deadline));
        Assert.Equal(TimeOnly.Parse(expected), result);
    }

    [Fact]
    public void Critical_deadline_parses_hh_mm_and_rejects_invalid_values()
    {
        Assert.Null(RoutePlanner.ParseCriticalDeadline(null));
        Assert.Null(RoutePlanner.ParseCriticalDeadline(" "));
        Assert.Equal(new TimeOnly(11, 0), RoutePlanner.ParseCriticalDeadline("11:00"));
        var error = Assert.Throws<ArgumentException>(() => RoutePlanner.ParseCriticalDeadline("once"));
        Assert.Equal(nameof(PlanRequest.CriticalDeadlineLocal), error.ParamName);
    }

    [Fact]
    public void Critical_stop_after_deadline_is_rejected_with_time_window()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m };
        var p1 = new GeoPoint(-0.2000m, -78.5000m);
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(_depot, p1)] = new(_depot, p1, 10m, 15m, DistanceSource.Road, null), // Llega 08:15 local
            [(p1, _depot)] = new(p1, _depot, 10m, 15m, DistanceSource.Road, null)
        };
        var deadline = RoutePlanner.ParseCriticalDeadline("08:10");
        var start = new TimeOnly(8, 0);
        var end = new TimeOnly(17, 0);

        // Crítico: ventana 08:00-08:10 por la hora límite -> llega 08:15 -> time_window
        var critical = CreateStop("P1", p1, windowStart: start,
            windowEnd: RoutePlanner.ApplyCriticalDeadline(start, end, true, deadline));
        var evalCritical = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { critical }, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);
        Assert.False(evalCritical.IsFeasible);
        Assert.Equal("time_window", evalCritical.FailureReason);

        // No crítico: conserva 08:00-17:00 -> factible
        var normal = CreateStop("P1", p1, windowStart: start,
            windowEnd: RoutePlanner.ApplyCriticalDeadline(start, end, false, deadline));
        var evalNormal = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { normal }, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);
        Assert.True(evalNormal.IsFeasible);
    }

    [Fact]
    public void Feasibility_waiting_time_reconciles_charged_hours_and_cumulative_minutes()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m, CostPerKm = 0.5m };
        var p1 = new GeoPoint(-0.2000m, -78.5000m);
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(_depot, p1)] = new(_depot, p1, 10m, 15m, DistanceSource.Road, null), // Llega 08:15 local
            [(p1, _depot)] = new(p1, _depot, 10m, 15m, DistanceSource.Road, null)  // Regreso 15m
        };

        // Ventana inicia 09:00 -> Llega 08:15 -> Espera 45 min
        // Servicio en parada = 15 min. Regreso = 15 min.
        var stop = CreateStop("P1", p1, windowStart: new TimeOnly(9, 0), windowEnd: new TimeOnly(12, 0), serviceMinutes: 15);
        var eval = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { stop }, _workdayStartUtc, _workdayHours,
            costPerHour: 20m, defaultCostPerKm: 0.5m, matrix);

        Assert.True(eval.IsFeasible);
        Assert.Equal(30m, eval.DrivingMinutes);
        Assert.Equal(15m, eval.ServiceMinutes);
        Assert.Equal(45m, eval.WaitMinutes);

        // CumulativeMinutes solo acumula conducción + servicio
        Assert.Equal(45m, eval.CumulativeMinutes);
        Assert.Equal(eval.DrivingMinutes + eval.ServiceMinutes, eval.CumulativeMinutes);

        // La diferencia entre el tiempo total transcurrido (horas cobradas) y CumulativeMinutes
        // queda explicada exactamente por WaitMinutes
        var elapsedMinutes = (decimal)(eval.DepartureUtc - eval.ArrivalUtc).TotalMinutes;
        Assert.Equal(90m, elapsedMinutes);
        Assert.Equal(elapsedMinutes, eval.CumulativeMinutes + eval.WaitMinutes);
        Assert.Equal(elapsedMinutes, eval.DrivingMinutes + eval.ServiceMinutes + eval.WaitMinutes);

        // 20 km * $0.50 = $10.00; 90 min (1.5 h) * $20.00/h = $30.00; Total = $40.00
        Assert.Equal(40.00m, eval.CumulativeCost);
    }

    [Fact]
    public void Feasibility_and_ComputeBreakdown_produce_exact_parity_and_components_sum_to_total()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 1000m, CostPerKm = 0.45m };
        var p1 = new GeoPoint(-0.2000m, -78.5000m);
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(_depot, p1)] = new(_depot, p1, 13.75m, 22.5m, DistanceSource.Road, null),
            [(p1, _depot)] = new(p1, _depot, 14.25m, 23.5m, DistanceSource.Road, null)
        };

        var stop = CreateStop("P1", p1, windowStart: new TimeOnly(8, 0), windowEnd: new TimeOnly(12, 0), serviceMinutes: 20);
        var eval = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, new[] { stop }, _workdayStartUtc, _workdayHours,
            costPerHour: 15.50m, defaultCostPerKm: 0.45m, matrix);

        Assert.True(eval.IsFeasible);

        var totalElapsedMinutes = eval.DrivingMinutes + eval.ServiceMinutes + eval.WaitMinutes;
        var breakdown = LogisticsCostCalculator.ComputeBreakdown(
            eval.CumulativeKm, totalElapsedMinutes, 0.45m, 15.50m, tollsCost: 0m,
            drivingMinutes: eval.DrivingMinutes, serviceMinutes: eval.ServiceMinutes, waitMinutes: eval.WaitMinutes);

        // Paridad exacta con el total de EvaluateRoute
        Assert.Equal(eval.CumulativeCost, breakdown.TotalCost);

        // La suma de componentes reproduce exactamente el total redondeado
        Assert.Equal(breakdown.KmCost + breakdown.HourCost + breakdown.TollsCost, breakdown.TotalCost);
        Assert.Equal(LogisticsCostCalculator.CalculateCost(eval.CumulativeKm, totalElapsedMinutes, 0.45m, 15.50m), breakdown.TotalCost);
    }

    [Fact]
    public void ComputeBreakdown_and_CalculateCost_components_sum_exactly_reproduces_total()
    {
        // 100.04 km a $0.10/km = $10.004 -> redondeo 10.00
        // 100.04 min / 60 * $6.00/h = $10.004 -> redondeo 10.00
        // Peajes $5.004 -> redondeo 5.00
        // Suma de componentes redondeados = 25.00
        // Sin unificación, la suma sin redondear previa era 25.012 -> 25.01 (discrepancia de 1 centavo)
        decimal km = 100.04m;
        decimal minutes = 100.04m;
        decimal costPerKm = 0.10m;
        decimal costPerHour = 6.00m;
        decimal tolls = 5.004m;

        var breakdown = LogisticsCostCalculator.ComputeBreakdown(km, minutes, costPerKm, costPerHour, tolls);
        var cost = LogisticsCostCalculator.CalculateCost(km, minutes, costPerKm, costPerHour, tolls);

        Assert.Equal(cost, breakdown.TotalCost);
        Assert.Equal(breakdown.KmCost + breakdown.HourCost + breakdown.TollsCost, breakdown.TotalCost);
        Assert.Equal(10.00m, breakdown.KmCost);
        Assert.Equal(10.00m, breakdown.HourCost);
        Assert.Equal(5.00m, breakdown.TollsCost);
        Assert.Equal(25.00m, breakdown.TotalCost);
        Assert.Equal("operational_transport_estimate", breakdown.Scope);
        Assert.Equal("estimated", breakdown.CostStatus);
    }

    [Fact]
    public void EvaluateTemporalValidation_ReturnsUncalibrated_WhenLessThan10Trips()
    {
        var baseDate = new DateOnly(2026, 10, 1);
        var trips = new List<HistoricalTripCost>();
        for (int i = 0; i < 9; i++)
        {
            trips.Add(new HistoricalTripCost(80m + i, 82m + i, baseDate.AddDays(i), TripCode: $"TRIP-{i}"));
        }

        var result = CostUncertaintyService.EvaluateTemporalValidation(100m, trips, 0.90m);

        Assert.Equal("uncalibrated", result.Status);
        Assert.Null(result.LowerBound);
        Assert.Null(result.UpperBound);
        Assert.Null(result.MarginOfError);
        Assert.Equal(9, result.SampleSize);
        Assert.Equal("conformal_residuals_quantile", result.Method);
        Assert.Equal(CostUncertaintyService.UncalibratedMessage, result.Message);
    }

    [Fact]
    public void EvaluateTemporalValidation_ReturnsCalibrated_When14OrMoreTrips_WithHoldoutAndWape()
    {
        var baseDate = new DateOnly(2026, 10, 1);
        var trips = new List<HistoricalTripCost>();
        var residuals = new[] { -4.0m, -3.0m, -2.5m, -1.0m, -0.5m, 0.0m, 0.5m, 1.0m, 1.5m, 2.0m, 2.5m, 3.0m, 3.5m, 4.0m, 5.0m };
        for (int i = 0; i < residuals.Length; i++)
        {
            decimal est = 80m + i * 2;
            decimal actual = est + residuals[i];
            trips.Add(new HistoricalTripCost(est, actual, baseDate.AddDays(i), TripCode: $"TRIP-{i}", VehicleCode: "VEH-01", HadIncidents: i % 3 == 0));
        }

        var result = CostUncertaintyService.EvaluateTemporalValidation(100m, trips, 0.90m);

        Assert.Equal("calibrated", result.Status);
        Assert.NotNull(result.LowerBound);
        Assert.NotNull(result.UpperBound);
        Assert.NotNull(result.MarginOfError);
        Assert.Equal(15, result.SampleSize);
        Assert.NotNull(result.Wape);
        Assert.True(result.Wape > 0m);
        Assert.NotNull(result.QuantileLowerOffset);
        Assert.NotNull(result.QuantileUpperOffset);
        Assert.NotNull(result.IntervalWidth);
        Assert.Equal(result.UpperBound - result.LowerBound, result.IntervalWidth);
        Assert.NotNull(result.HoldoutSampleSize);
        Assert.True(result.HoldoutSampleSize > 0);
        Assert.NotNull(result.HoldoutEmpiricalCoverage);
        Assert.True(result.HoldoutEmpiricalCoverage >= 0m && result.HoldoutEmpiricalCoverage <= 1m);
    }

    [Fact]
    public void EvaluateTemporalValidation_DeduplicatesByTripCode()
    {
        var baseDate = new DateOnly(2026, 10, 1);
        var trips = new List<HistoricalTripCost>();
        // 14 entradas en total, pero solo 8 TripCodes distintos
        for (int i = 0; i < 14; i++)
        {
            int codeId = i % 8; // códigos TRIP-0 a TRIP-7
            trips.Add(new HistoricalTripCost(80m + i, 82m + i, baseDate.AddDays(i), TripCode: $"TRIP-{codeId}"));
        }

        // Al deduplicar quedan 8 viajes (< 10), por lo que debe devolver uncalibrated
        var result = CostUncertaintyService.EvaluateTemporalValidation(100m, trips, 0.90m);
        Assert.Equal("uncalibrated", result.Status);
        Assert.Equal(8, result.SampleSize);

        // Si agregamos más viajes con códigos únicos para llegar a 14 deduplicados
        for (int i = 8; i < 14; i++)
        {
            trips.Add(new HistoricalTripCost(80m + i, 82m + i, baseDate.AddDays(20 + i), TripCode: $"TRIP-{i}"));
        }

        var resultCalibrated = CostUncertaintyService.EvaluateTemporalValidation(100m, trips, 0.90m);
        Assert.Equal("calibrated", resultCalibrated.Status);
        Assert.Equal(14, resultCalibrated.SampleSize);
        Assert.NotNull(resultCalibrated.HoldoutSampleSize);
        Assert.True(resultCalibrated.HoldoutSampleSize > 0);
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
