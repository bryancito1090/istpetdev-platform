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
        ReplanMode replanMode = ReplanMode.Strict)
    {
        if (stops.Count < 3) return stops;

        var currentStops = new List<StopCandidate>(stops);
        var currentEvaluation = RouteFeasibility.EvaluateRoute(
            vehicle, depotPos, currentStops, workdayStartUtc, workdayHours,
            costPerHour, defaultCostPerKm, distanceMatrix, incidentEffect, replanMode);

        if (!currentEvaluation.IsFeasible) return stops;

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
                        costPerHour, defaultCostPerKm, distanceMatrix, incidentEffect, replanMode);

                    if (eval.IsFeasible && eval.CumulativeCost < currentEvaluation.CumulativeCost)
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
