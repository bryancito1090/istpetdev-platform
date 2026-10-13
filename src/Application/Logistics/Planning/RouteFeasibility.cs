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

public sealed record FeasibilityCheck(
    bool IsFeasible,
    string? FailureReason, // "capacity", "time_window", "workday", "unreachable", "blocked_leg"
    DateTime ArrivalUtc,
    DateTime DepartureUtc,
    TimeSpan WaitTime,
    int? LateMinutes,
    decimal CumulativeKm,
    decimal CumulativeMinutes,
    decimal CumulativeCost,
    decimal TotalWeight,
    decimal TotalVolume,
    IReadOnlyList<Leg> Legs,
    bool HasApproximations);

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
        ReplanMode replanMode = ReplanMode.Strict)
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
        decimal totalMinutes = 0m;

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

            legs.Add(leg);
            if (leg.Source == DistanceSource.Approximation) hasApproximations = true;

            decimal legMinutes = leg.Minutes;
            if (incidentEffect != null && incidentEffect.SlowFactors.TryGetValue((currentPos, nextPos), out var factor))
            {
                legMinutes *= factor;
            }

            totalKm += leg.Km;
            totalMinutes += legMinutes;

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
                currentTimeUtc = currentTimeUtc.Add(wait);
            }
            else if (localArrival > stop.WindowEndLocal)
            {
                var lateSpan = localArrival.ToTimeSpan() - stop.WindowEndLocal.ToTimeSpan();
                lateMinutes = (int)Math.Ceiling(lateSpan.TotalMinutes);

                // En modo Strict, cualquier retraso de ventana hace inviable la ruta
                if (replanMode == ReplanMode.Strict)
                {
                    return Infeasible("time_window");
                }
                // En LateService o PrioritizeCritical, permitimos retraso pero se registra lateMinutes
            }

            // Tiempo de servicio en la parada
            currentTimeUtc = currentTimeUtc.AddMinutes(stop.ServiceMinutes);
            totalMinutes += stop.ServiceMinutes;

            currentPos = nextPos;
        }

        // Regreso al depósito
        if (incidentEffect?.BlockedLegs.Contains((currentPos, depotPos)) == true)
        {
            return Infeasible("blocked_leg");
        }

        if (!distanceMatrix.TryGetValue((currentPos, depotPos), out var returnLeg))
        {
            return Infeasible("unreachable");
        }

        legs.Add(returnLeg);
        if (returnLeg.Source == DistanceSource.Approximation) hasApproximations = true;

        decimal returnMinutes = returnLeg.Minutes;
        if (incidentEffect != null && incidentEffect.SlowFactors.TryGetValue((currentPos, depotPos), out var returnFactor))
        {
            returnMinutes *= returnFactor;
        }

        totalKm += returnLeg.Km;
        totalMinutes += returnMinutes;
        currentTimeUtc = currentTimeUtc.AddMinutes((double)returnMinutes);

        // 3. Jornada laboral máxima
        var maxWorkdayEndUtc = workdayStartUtc.AddHours(workdayHours);
        if (currentTimeUtc > maxWorkdayEndUtc)
        {
            return Infeasible("workday");
        }

        // Costo = km * CostPerKm + horas * CostPerHour
        var costPerKm = vehicle.CostPerKm ?? defaultCostPerKm;
        var totalHours = (decimal)(currentTimeUtc - workdayStartUtc).TotalHours;
        var totalCost = Math.Round(totalKm * costPerKm + totalHours * costPerHour, 3, MidpointRounding.AwayFromZero);

        return new FeasibilityCheck(
            true, null, workdayStartUtc, currentTimeUtc, TimeSpan.Zero, null,
            totalKm, totalMinutes, totalCost, totalWeight, totalVolume, legs, hasApproximations);
    }

    private static FeasibilityCheck Infeasible(string reason) =>
        new(false, reason, DateTime.MinValue, DateTime.MinValue, TimeSpan.Zero, null, 0m, 0m, 0m, 0m, 0m, Array.Empty<Leg>(), false);
}
