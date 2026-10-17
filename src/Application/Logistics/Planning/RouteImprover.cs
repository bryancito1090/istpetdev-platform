using IstpetDev.Domain.Entities;

namespace IstpetDev.Application.Logistics.Planning;

public static class RouteImprover
{
    public static List<StopCandidate> TwoOpt(
        Vehicle vehicle,
        GeoPoint depotPos,
        List<StopCandidate> stops,
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
        if (stops.Count < 2) return stops;

        var currentStops = new List<StopCandidate>(stops);
        var currentEvaluation = RouteFeasibility.EvaluateRoute(
            vehicle, depotPos, currentStops, workdayStartUtc, workdayHours,
            costPerHour, defaultCostPerKm, distanceMatrix, incidentEffect, replanMode, returnDepot, workdayEndUtc, trafficBands);

        // A closure may require relocating a stop before any reversal is
        // feasible (for three stops, the two cyclic orders are relocations).
        if (!currentEvaluation.IsFeasible)
        {
            for (var from = 0; from < stops.Count && !currentEvaluation.IsFeasible; from++)
            for (var to = 0; to < stops.Count && !currentEvaluation.IsFeasible; to++)
            {
                var repaired = new List<StopCandidate>(stops);
                var moved = repaired[from];
                repaired.RemoveAt(from);
                repaired.Insert(to, moved);
                var evaluation = RouteFeasibility.EvaluateRoute(vehicle, depotPos, repaired, workdayStartUtc,
                    workdayHours, costPerHour, defaultCostPerKm, distanceMatrix, incidentEffect, replanMode,
                    returnDepot, workdayEndUtc, trafficBands);
                if (evaluation.IsFeasible) { currentStops = repaired; currentEvaluation = evaluation; }
            }
        }

        bool improved = true;
        while (improved)
        {
            improved = false;
            for (int i = 0; i < currentStops.Count - 1; i++)
            {
                for (int k = i + 1; k < currentStops.Count; k++)
                {
                    var newRoute = TwoOptSwap(currentStops, i, k);
                    var eval = RouteFeasibility.EvaluateRoute(
                        vehicle, depotPos, newRoute, workdayStartUtc, workdayHours,
                        costPerHour, defaultCostPerKm, distanceMatrix, incidentEffect, replanMode, returnDepot, workdayEndUtc, trafficBands);

                    if (eval.IsFeasible && (!currentEvaluation.IsFeasible || eval.CumulativeCost < currentEvaluation.CumulativeCost))
                    {
                        currentStops = newRoute;
                        currentEvaluation = eval;
                        improved = true;
                        break;
                    }
                }
                if (improved) break;
            }
        }

        return currentStops;
    }

    private static List<StopCandidate> TwoOptSwap(List<StopCandidate> route, int i, int k)
    {
        var newRoute = new List<StopCandidate>();
        for (int c = 0; c < i; c++) newRoute.Add(route[c]);
        for (int c = k; c >= i; c--) newRoute.Add(route[c]);
        for (int c = k + 1; c < route.Count; c++) newRoute.Add(route[c]);
        return newRoute;
    }
}
