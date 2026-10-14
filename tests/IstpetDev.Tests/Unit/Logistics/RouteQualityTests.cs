using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Entities;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class RouteQualityTests
{
    private readonly GeoPoint _depot = new(-0.2295m, -78.5243m);
    private readonly DateTime _workdayStartUtc = new(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc);
    private readonly int _workdayHours = 12;
    private readonly decimal _costPerHour = 10m;
    private readonly decimal _costPerKm = 0.5m;

    [Fact]
    public void TwoOpt_and_insertion_heuristic_cost_is_within_110_percent_of_brute_force_optimal_on_small_instances()
    {
        // 5 puntos de entrega (5! = 120 permutaciones posibles para brute force)
        var points = new[]
        {
            new GeoPoint(-0.2100m, -78.5100m),
            new GeoPoint(-0.1900m, -78.4900m),
            new GeoPoint(-0.1700m, -78.4800m),
            new GeoPoint(-0.2300m, -78.5300m),
            new GeoPoint(-0.2500m, -78.5500m),
        };

        var allNodes = new List<GeoPoint> { _depot };
        allNodes.AddRange(points);

        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>();
        foreach (var from in allNodes)
        {
            foreach (var to in allNodes)
            {
                if (from.Equals(to))
                {
                    matrix[(from, to)] = new Leg(from, to, 0m, 0m, DistanceSource.Road, null);
                }
                else
                {
                    var dist = DistanceProvider.HaversineDistanceKm(from, to);
                    var minutes = dist / 40m * 60m; // 40 km/h
                    matrix[(from, to)] = new Leg(from, to, dist, minutes, DistanceSource.Road, null);
                }
            }
        }

        var vehicle = new Vehicle { Id = Guid.NewGuid(), Code = "VEH-01", MaxWeight = 10000m };
        var candidates = points.Select((p, idx) => new StopCandidate(
            Guid.NewGuid(),
            $"P{idx + 1}",
            p,
            new TimeOnly(8, 0),
            new TimeOnly(20, 0),
            10,
            10m,
            0.1m,
            Array.Empty<StopLoad>(),
            false,
            RiskLevel.Critico,
            "alta",
            1m,
            2m)).ToList();

        // 1. Calcular óptimo global por fuerza bruta
        decimal bruteForceMinCost = decimal.MaxValue;
        foreach (var perm in GetPermutations(candidates, candidates.Count))
        {
            var eval = RouteFeasibility.EvaluateRoute(
                vehicle, _depot, perm.ToList(), _workdayStartUtc, _workdayHours,
                _costPerHour, _costPerKm, matrix);
            if (eval.IsFeasible && eval.CumulativeCost < bruteForceMinCost)
            {
                bruteForceMinCost = eval.CumulativeCost;
            }
        }

        // 2. Ejecutar inserción + 2-opt
        var initialRoute = new List<StopCandidate>();
        foreach (var cand in candidates)
        {
            int bestPos = 0;
            decimal minCost = decimal.MaxValue;
            for (int pos = 0; pos <= initialRoute.Count; pos++)
            {
                var testRoute = new List<StopCandidate>(initialRoute);
                testRoute.Insert(pos, cand);
                var eval = RouteFeasibility.EvaluateRoute(
                    vehicle, _depot, testRoute, _workdayStartUtc, _workdayHours,
                    _costPerHour, _costPerKm, matrix);
                if (eval.IsFeasible && eval.CumulativeCost < minCost)
                {
                    minCost = eval.CumulativeCost;
                    bestPos = pos;
                }
            }
            initialRoute.Insert(bestPos, cand);
        }

        var improvedRoute = RouteImprover.TwoOpt(
            vehicle, _depot, initialRoute, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);

        var finalEval = RouteFeasibility.EvaluateRoute(
            vehicle, _depot, improvedRoute, _workdayStartUtc, _workdayHours,
            _costPerHour, _costPerKm, matrix);

        Assert.True(finalEval.IsFeasible);
        // Costo debe ser <= 110% del óptimo absoluto
        Assert.True(finalEval.CumulativeCost <= bruteForceMinCost * 1.10m,
            $"Costo heurístico {finalEval.CumulativeCost} excede 110% del óptimo {bruteForceMinCost} (umbral: {bruteForceMinCost * 1.10m})");
    }

    private static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
    {
        if (length == 1) return list.Select(t => new[] { t });
        return GetPermutations(list, length - 1)
            .SelectMany(t => list.Where(e => !t.Contains(e)),
                (t1, t2) => t1.Concat(new[] { t2 }));
    }
}
