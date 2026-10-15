using System.Net;
using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public sealed class DistanceProviderTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(handler(request));
        }
    }

    [Fact]
    public async Task Stored_edge_avoids_http_and_preserves_road_source()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var pA = new GeoPoint(-0.20000m, -78.50000m);
        var pB = new GeoPoint(-0.21000m, -78.51000m);

        db.RoadMatrixEdges.AddRange(
            new RoadMatrixEdge
            {
                OrganizationId = DemoOrganization.Id,
                FromLat = pA.Lat,
                FromLon = pA.Lon,
                ToLat = pB.Lat,
                ToLon = pB.Lon,
                DistanceKm = 5.250m,
                DurationMinutes = 11.500m,
                EncodedPolyline = "_polyline_stored",
                Source = "imported",
                RetrievedAtUtc = DateTime.UtcNow
            },
            new RoadMatrixEdge
            {
                OrganizationId = DemoOrganization.Id,
                FromLat = pB.Lat,
                FromLon = pB.Lon,
                ToLat = pA.Lat,
                ToLon = pA.Lon,
                DistanceKm = 5.400m,
                DurationMinutes = 12.000m,
                EncodedPolyline = "_polyline_stored_ba",
                Source = "imported",
                RetrievedAtUtc = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var options = Options.Create(new LogisticsOptions
        {
            Osrm = new OsrmOptions { BaseUrl = "http://osrm-mock" }
        });

        var provider = new DistanceProvider(db, client, options, NullLogger<DistanceProvider>.Instance);
        var matrix = await provider.MatrixAsync(new[] { pA, pB });

        Assert.Equal(0, handler.CallCount); // Stored edge avoided HTTP!
        var leg = matrix[(pA, pB)];
        Assert.Equal(DistanceSource.Road, leg.Source);
        Assert.Equal(5.250m, leg.Km);
        Assert.Equal(11.500m, leg.Minutes);
        Assert.Equal("_polyline_stored", leg.EncodedPolyline);
    }

    [Fact]
    public async Task Missing_pair_queries_osrm_and_persists_distance_and_duration()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var pA = new GeoPoint(-0.20000m, -78.50000m);
        var pB = new GeoPoint(-0.21000m, -78.51000m);

        var osrmResponse = new
        {
            code = "Ok",
            distances = new double[][]
            {
                new double[] { 0, 4500 },
                new double[] { 4800, 0 }
            },
            durations = new double[][]
            {
                new double[] { 0, 600 },
                new double[] { 660, 0 }
            }
        };

        var handler = new MockHttpMessageHandler(req =>
        {
            var json = JsonSerializer.Serialize(osrmResponse);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler);
        var options = Options.Create(new LogisticsOptions
        {
            Osrm = new OsrmOptions { BaseUrl = "http://osrm-mock" }
        });

        var provider = new DistanceProvider(db, client, options, NullLogger<DistanceProvider>.Instance);
        var matrix = await provider.MatrixAsync(new[] { pA, pB });

        Assert.Equal(1, handler.CallCount);
        var legAB = matrix[(pA, pB)];
        Assert.Equal(DistanceSource.Road, legAB.Source);
        Assert.Equal(4.500m, legAB.Km); // 4500 m = 4.5 km
        Assert.Equal(10.000m, legAB.Minutes); // 600 s = 10 min

        // Verify persisted in database
        var persisted = await db.RoadMatrixEdges.SingleOrDefaultAsync(e => e.FromLat == pA.Lat && e.ToLat == pB.Lat);
        Assert.NotNull(persisted);
        Assert.Equal(4.500m, persisted.DistanceKm);
        Assert.Equal("osrm", persisted.Source);
    }

    [Fact]
    public async Task Down_or_missing_osrm_falls_back_to_haversine_approximation_without_persisting()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        var pA = new GeoPoint(-0.20000m, -78.50000m);
        var pB = new GeoPoint(-0.21000m, -78.51000m);

        var handler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new HttpClient(handler);
        var options = Options.Create(new LogisticsOptions
        {
            Osrm = new OsrmOptions { BaseUrl = "http://osrm-down" },
            ApproxDetourFactor = 1.3m,
            ApproxSpeedKmh = 40m
        });

        var provider = new DistanceProvider(db, client, options, NullLogger<DistanceProvider>.Instance);
        var matrix = await provider.MatrixAsync(new[] { pA, pB });

        var legAB = matrix[(pA, pB)];
        Assert.Equal(DistanceSource.Approximation, legAB.Source);
        Assert.Null(legAB.EncodedPolyline);

        var haversine = DistanceProvider.HaversineDistanceKm(pA, pB);
        var expectedKm = Math.Round(haversine * 1.3m, 3, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedKm, legAB.Km);

        // Crucial requirement: Approximation is NOT saved to the database
        Assert.Equal(0, await db.RoadMatrixEdges.CountAsync());
    }

    [Fact]
    public async Task More_than_100_points_are_requested_in_blocks_with_user_agent_and_all_pairs_come_from_the_road()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var points = Enumerable.Range(0, 120).Select(i => new GeoPoint(-0.2m - i * 0.001m, -78.5m - i * 0.001m)).ToList();
        var maxCoordinates = 0;
        var userAgents = new HashSet<string>();
        var handler = new MockHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();
            var coordinates = url.Split("/driving/")[1].Split('?')[0].Split(';').Length;
            maxCoordinates = Math.Max(maxCoordinates, coordinates);
            userAgents.Add(request.Headers.UserAgent.ToString());
            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            var sources = query["sources"]!.Split(';').Length;
            var destinations = query["destinations"]!.Split(';').Length;
            var row = string.Join(",", Enumerable.Repeat("1500", destinations));
            var table = "[" + string.Join(",", Enumerable.Repeat("[" + row + "]", sources)) + "]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"code\":\"Ok\",\"distances\":{table},\"durations\":{table}}}")
            };
        });
        var options = Options.Create(new LogisticsOptions { Osrm = new OsrmOptions { BaseUrl = "http://osrm-mock", TimeoutSeconds = 10 } });
        var provider = new DistanceProvider(db, new HttpClient(handler), options, NullLogger<DistanceProvider>.Instance);

        var matrix = await provider.MatrixAsync(points);

        Assert.True(handler.CallCount > 1, "Con 120 puntos la tabla debe pedirse por bloques");
        Assert.True(maxCoordinates <= 100, $"Una petición llevó {maxCoordinates} coordenadas (límite del servidor: 100)");
        Assert.All(userAgents, ua => Assert.StartsWith("IstpetDev/", ua)); // sin User-Agent, el OSRM público responde 403
        Assert.All(matrix.Where(m => m.Key.From != m.Key.To), m => Assert.Equal(DistanceSource.Road, m.Value.Source));
        Assert.Equal(120 * 119, await db.RoadMatrixEdges.CountAsync(e => e.Source == "osrm"));
    }
}
