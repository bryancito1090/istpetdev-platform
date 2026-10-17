using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Entities;

namespace IstpetDev.Application.Logistics.Planning;

public sealed record StopCandidate(
    Guid ServicePointId,
    string PointCode,
    GeoPoint Position,
    TimeOnly WindowStartLocal,
    TimeOnly WindowEndLocal,
    int ServiceMinutes,
    decimal Weight,
    decimal Volume,
    IReadOnlyList<StopLoad> Loads,
    bool IsConsolidation,
    RiskLevel Level,
    string Criticality,
    decimal CoverageDays,
    decimal LeadTimeDays);

public static class RouteFeasibility
{
    public static FeasibilityCheck EvaluateRoute(
        Vehicle vehicle,
        GeoPoint depotPos,
        IReadOnlyList<StopCandidate> stops,
        DateTime workdayStartUtc,
        int workdayHours,
        decimal costPerHour,
        decimal defaultCostPerKm,
        IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg> distanceMatrix,
        IncidentEffect? incidentEffect = null,
        ReplanMode replanMode = ReplanMode.Strict,
        GeoPoint? returnDepot = null,
        DateTime? workdayEndUtc = null,
        IReadOnlyList<TrafficBandOptions>? trafficBands = null)
    {
        // 1. Capacidad: Peso y Volumen
        decimal totalWeight = stops.Sum(s => s.Weight);
        decimal totalVolume = stops.Sum(s => s.Volume);

        if (vehicle.MaxWeight > 0 && totalWeight > vehicle.MaxWeight)
        {
            return Infeasible("capacity");
        }

        if (vehicle.MaxVolume.HasValue && vehicle.MaxVolume.Value > 0 && totalVolume > vehicle.MaxVolume.Value)
        {
            return Infeasible("capacity");
        }

        // Si la ruta no tiene paradas, es un viaje vacío factible
        if (stops.Count == 0)
        {
            return new FeasibilityCheck(
                true, null, workdayStartUtc, workdayStartUtc, TimeSpan.Zero, null,
                0m, 0m, 0m, 0m, 0m, Array.Empty<Leg>(), false);
        }

        // 2. Tiempos, Ventanas y Regreso a Depósito
        var legs = new List<Leg>();
        bool hasApproximations = false;
        decimal totalKm = 0m;
        decimal drivingMinutes = 0m;
        decimal serviceMinutes = 0m;
        decimal waitMinutes = 0m;

        DateTime currentTimeUtc = workdayStartUtc;
        GeoPoint currentPos = depotPos;

        for (int i = 0; i < stops.Count; i++)
        {
            var stop = stops[i];
            var nextPos = stop.Position;

            // Verificar si el tramo está bloqueado
            if (incidentEffect?.BlockedLegs.Contains((currentPos, nextPos)) == true)
            {
                return Infeasible("blocked_leg");
            }

            if (!distanceMatrix.TryGetValue((currentPos, nextPos), out var leg))
            {
                return Infeasible("unreachable");
            }

            if (leg.Source == DistanceSource.Approximation) hasApproximations = true;

            // #245: hora pico según la hora local de inicio del tramo (UTC-5 como el resto del cálculo)
            decimal legMinutes = TrafficBands.Apply(leg.Minutes,
                TrafficBands.SpeedFactorAt(trafficBands, TimeOnly.FromDateTime(currentTimeUtc.AddHours(-5))));
            if (incidentEffect != null && incidentEffect.SlowFactors.TryGetValue((currentPos, nextPos), out var factor))
            {
                legMinutes *= factor;
            }
            legs.Add(leg with { Minutes = legMinutes });

            totalKm += leg.Km;
            drivingMinutes += legMinutes;

            currentTimeUtc = currentTimeUtc.AddMinutes((double)legMinutes);
            var arrivalUtc = currentTimeUtc;

            // Ventana horaria (en hora local asumida a partir de la fecha base)
            // Para simplicidad en UTC / local de demo (UTC-5):
            var localArrival = TimeOnly.FromDateTime(currentTimeUtc.AddHours(-5)); // Fallback UTC-5 estándar
            TimeSpan wait = TimeSpan.Zero;
            int? lateMinutes = null;

            if (localArrival < stop.WindowStartLocal)
            {
                wait = stop.WindowStartLocal.ToTimeSpan() - localArrival.ToTimeSpan();
                waitMinutes += (decimal)wait.TotalMinutes;
                currentTimeUtc = currentTimeUtc.Add(wait);
            }
            else if (localArrival > stop.WindowEndLocal)
            {
                var lateSpan = localArrival.ToTimeSpan() - stop.WindowEndLocal.ToTimeSpan();
                lateMinutes = (int)Math.Ceiling(lateSpan.TotalMinutes);

                // En modo Strict, cualquier retraso de ventana hace inviable la ruta
                if (replanMode != ReplanMode.LateService || stop.IsConsolidation)
                {
                    return Infeasible("time_window");
                }
                // En LateService o PrioritizeCritical, permitimos retraso pero se registra lateMinutes
            }

            // Tiempo de servicio en la parada
            currentTimeUtc = currentTimeUtc.AddMinutes(stop.ServiceMinutes);
            serviceMinutes += stop.ServiceMinutes;

            currentPos = nextPos;
        }

        // Regreso al depósito
        var destination = returnDepot ?? depotPos;
        if (incidentEffect?.BlockedLegs.Contains((currentPos, destination)) == true)
        {
            return Infeasible("blocked_leg");
        }

        if (!distanceMatrix.TryGetValue((currentPos, destination), out var returnLeg))
        {
            return Infeasible("unreachable");
        }

        if (returnLeg.Source == DistanceSource.Approximation) hasApproximations = true;

        decimal returnMinutes = TrafficBands.Apply(returnLeg.Minutes,
            TrafficBands.SpeedFactorAt(trafficBands, TimeOnly.FromDateTime(currentTimeUtc.AddHours(-5))));
        if (incidentEffect != null && incidentEffect.SlowFactors.TryGetValue((currentPos, destination), out var returnFactor))
        {
            returnMinutes *= returnFactor;
        }
        legs.Add(returnLeg with { Minutes = returnMinutes });

        totalKm += returnLeg.Km;
        drivingMinutes += returnMinutes;
        currentTimeUtc = currentTimeUtc.AddMinutes((double)returnMinutes);

        // 3. Jornada laboral máxima
        var maxWorkdayEndUtc = workdayEndUtc ?? workdayStartUtc.AddHours(workdayHours);
        if (currentTimeUtc > maxWorkdayEndUtc)
        {
            return Infeasible("workday");
        }

        var totalElapsedMinutes = drivingMinutes + serviceMinutes + waitMinutes;
        var costPerKm = vehicle.CostPerKm ?? defaultCostPerKm;
        var totalCost = LogisticsCostCalculator.CalculateCost(totalKm, totalElapsedMinutes, costPerKm, costPerHour);
        var totalMinutes = drivingMinutes + serviceMinutes;

        return new FeasibilityCheck(
            true, null, workdayStartUtc, currentTimeUtc, TimeSpan.FromMinutes((double)waitMinutes), null,
            totalKm, totalMinutes, totalCost, totalWeight, totalVolume, legs, hasApproximations,
            waitMinutes, drivingMinutes, serviceMinutes);
    }

    private static FeasibilityCheck Infeasible(string reason) =>
        new(false, reason, DateTime.MinValue, DateTime.MinValue, TimeSpan.Zero, null, 0m, 0m, 0m, 0m, 0m, Array.Empty<Leg>(), false);
}
