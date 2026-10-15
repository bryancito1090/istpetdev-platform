using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class RouteEvaluatorTests
{
    private sealed class FakeDistanceProvider : IDistanceProvider
    {
        public Task<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>> MatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default)
        {
            var dict = new Dictionary<(GeoPoint From, GeoPoint To), Leg>();
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                {
                    var p1 = points[i];
                    var p2 = points[j];
                    decimal km = DistanceProvider.HaversineDistanceKm(p1, p2) * 1.2m;
                    decimal min = Math.Round((km / 40m) * 60m, 1);
                    dict[(p1, p2)] = new Leg(p1, p2, km, min, DistanceSource.Road, null);
                }
            }
            return Task.FromResult<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>>(dict);
        }
    }

    private sealed class FakeGeometry(RoadPath? path) : IRouteGeometryService
    {
        public Task<RouteGeometryResponse?> GetAsync(Guid routePlanId, CancellationToken ct = default) => Task.FromResult<RouteGeometryResponse?>(null);
        public Task<RoadPath?> RoadPathAsync(IReadOnlyList<double[]> waypoints, CancellationToken ct = default) => Task.FromResult(path);
    }

    private static List<RouteStopCandidate> PickupThenDelivery() =>
    [
        new(1, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-01", "Bodega Central", new GeoPoint(-0.25m, -78.52m), 50m, 0.5m, new TimeOnly(7, 0), new TimeOnly(19, 0), 20),
        new(2, RouteStopKind.Delivery, Guid.NewGuid(), null, "HOS-01", "Hospital 1", new GeoPoint(-0.2m, -78.5m), -50m, -0.5m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15)
    ];

    [Fact]
    public async Task EvaluateSequenceAsync_UsesRoadPathForTheMapWithoutChangingKm()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new LogisticsOptions());
        var straight = await new RouteEvaluator(new FakeDistanceProvider(), options).EvaluateSequenceAsync(PickupThenDelivery(), null, DateTime.UtcNow);
        var road = new RoadPath(9.9, [[-0.25, -78.52], [-0.23, -78.51], [-0.2, -78.5]]);
        var byRoad = await new RouteEvaluator(new FakeDistanceProvider(), options, new FakeGeometry(road)).EvaluateSequenceAsync(PickupThenDelivery(), null, DateTime.UtcNow);
        var unavailable = await new RouteEvaluator(new FakeDistanceProvider(), options, new FakeGeometry(null)).EvaluateSequenceAsync(PickupThenDelivery(), null, DateTime.UtcNow);

        Assert.Equal("approximate", straight.CoordinatesSource);
        Assert.Equal("osrm", byRoad.CoordinatesSource);
        Assert.Equal(3, byRoad.Coordinates.Count);
        Assert.Equal(straight.TotalDistanceKm, byRoad.TotalDistanceKm); // los km de la evaluación no cambian
        Assert.Equal("approximate", unavailable.CoordinatesSource);
        Assert.Equal(straight.Coordinates.Count, unavailable.Coordinates.Count);
    }

    [Fact]
    public async Task EvaluateSequenceAsync_WhenStartsWithoutPickup_FailsPrecedence()
    {
        var evaluator = new RouteEvaluator(new FakeDistanceProvider(), Microsoft.Extensions.Options.Options.Create(new LogisticsOptions()));

        var stops = new List<RouteStopCandidate>
        {
            new(1, RouteStopKind.Delivery, Guid.NewGuid(), null, "HOS-01", "Hospital 1", new GeoPoint(-0.2m, -78.5m), -50m, -0.5m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15),
            new(2, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-01", "Bodega Central", new GeoPoint(-0.25m, -78.52m), 50m, 0.5m, new TimeOnly(7, 0), new TimeOnly(19, 0), 20)
        };

        var result = await evaluator.EvaluateSequenceAsync(stops, null, DateTime.UtcNow);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("Infracción de precedencia"));
    }

    [Fact]
    public async Task EvaluateSequenceAsync_WhenPickupBeforeDelivery_Succeeds()
    {
        var evaluator = new RouteEvaluator(new FakeDistanceProvider(), Microsoft.Extensions.Options.Options.Create(new LogisticsOptions()));

        var stops = new List<RouteStopCandidate>
        {
            new(1, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-01", "Bodega Central", new GeoPoint(-0.25m, -78.52m), 50m, 0.5m, new TimeOnly(7, 0), new TimeOnly(19, 0), 20),
            new(2, RouteStopKind.Delivery, Guid.NewGuid(), null, "HOS-01", "Hospital 1", new GeoPoint(-0.2m, -78.5m), -50m, -0.5m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15)
        };

        var result = await evaluator.EvaluateSequenceAsync(stops, null, DateTime.UtcNow);

        Assert.True(result.IsValid);
        Assert.Empty(result.ValidationErrors);
        Assert.Equal(50m, result.MaxWeightOnRoute);
    }

    [Fact]
    public async Task EvaluateSequenceAsync_WhenVehicleExceeded_ReportsCapacityError()
    {
        var evaluator = new RouteEvaluator(new FakeDistanceProvider(), Microsoft.Extensions.Options.Options.Create(new LogisticsOptions()));

        var vehicle = new Vehicle
        {
            Code = "CAMION-PEQUEÑO",
            MaxWeight = 100m,
            MaxVolume = 1.0m,
            IsActive = true
        };

        var stops = new List<RouteStopCandidate>
        {
            new(1, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-01", "Bodega", new GeoPoint(-0.25m, -78.52m), 250m, 0.8m, new TimeOnly(7, 0), new TimeOnly(19, 0), 20),
            new(2, RouteStopKind.Delivery, Guid.NewGuid(), null, "HOS-01", "Hospital", new GeoPoint(-0.2m, -78.5m), -250m, -0.8m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15)
        };

        var result = await evaluator.EvaluateSequenceAsync(stops, vehicle, DateTime.UtcNow);

        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("excede la capacidad del vehículo"));
    }
}
