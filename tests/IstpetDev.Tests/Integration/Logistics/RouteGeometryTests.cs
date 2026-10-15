using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

// Recorrido por calles para el mapa: OSRM route si está configurado; rectas aproximadas si no.
[Trait("Category", "Integration")]
public class RouteGeometryTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static readonly Dictionary<string, string?> NoOsrm = new() { ["Logistics:Osrm:RouteGeometryUrl"] = "" };

    private static async Task<Guid> ProposeFirstPlanAsync(HttpClient planner)
    {
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        var created = await planner.PostAsync("/api/v1/route-plans", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("routes")[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Without_osrm_returns_the_depot_and_stops_in_order_as_approximate()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: NoOsrm);
        using var planner = api.ClientFor(TestTokens.Planner);
        var planId = await ProposeFirstPlanAsync(planner);

        var response = await planner.GetAsync($"/api/v1/route-plans/{planId}/geometry");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal("approximate", root.GetProperty("source").GetString());
        Assert.Equal("VEH-01", root.GetProperty("vehicleCode").GetString());
        var coordinates = root.GetProperty("coordinates").EnumerateArray().ToList();
        var depot = root.GetProperty("depot");
        // Sale de la bodega, recorre las 3 paradas de demo-base y vuelve.
        Assert.Equal(5, coordinates.Count);
        Assert.Equal(depot.GetProperty("latitude").GetDouble(), coordinates[0][0].GetDouble(), 5);
        Assert.Equal(depot.GetProperty("latitude").GetDouble(), coordinates[^1][0].GetDouble(), 5);
        var stops = root.GetProperty("stops").EnumerateArray().Select(s => s.GetProperty("sequence").GetInt32()).ToList();
        Assert.Equal(new[] { 1, 2, 3 }, stops);

        Assert.Equal(HttpStatusCode.NotFound, (await planner.GetAsync($"/api/v1/route-plans/{Guid.NewGuid()}/geometry")).StatusCode);
    }

    [Fact]
    public async Task With_osrm_returns_road_geometry_and_caches_it()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: NoOsrm);
        using var planner = api.ClientFor(TestTokens.Planner);
        var planId = await ProposeFirstPlanAsync(planner);

        var handler = new FakeOsrm();
        var options = Options.Create(new LogisticsOptions { Osrm = new OsrmOptions { RouteGeometryUrl = "https://osrm.test" } });
        using var scope = api.Services.CreateScope();
        var service = new RouteGeometryService(scope.ServiceProvider.GetRequiredService<IApplicationDbContext>(),
            new HttpClient(handler), options, new RouteGeometryCache(), NullLogger<RouteGeometryService>.Instance);

        var first = await service.GetAsync(planId);
        var second = await service.GetAsync(planId);

        Assert.Equal("osrm", first!.Source);
        Assert.Equal(12.345, first.RoadKm);
        Assert.Equal(4, first.Coordinates.Count);
        Assert.Equal(new[] { -0.20, -78.50 }, first.Coordinates[0]); // [lat, lon] a partir de [lon, lat] de OSRM
        Assert.Equal(1, handler.Calls);
        Assert.Same(first, second);
        Assert.StartsWith("https://osrm.test/route/v1/driving/", handler.LastUrl);
        Assert.Contains("overview=full&geometries=geojson", handler.LastUrl);
        Assert.StartsWith("IstpetDev/", handler.UserAgent); // sin User-Agent, el OSRM público responde 403
    }

    [Fact]
    public async Task When_osrm_fails_falls_back_to_approximate_lines()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), overrides: NoOsrm);
        using var planner = api.ClientFor(TestTokens.Planner);
        var planId = await ProposeFirstPlanAsync(planner);

        var options = Options.Create(new LogisticsOptions { Osrm = new OsrmOptions { RouteGeometryUrl = "https://osrm.test" } });
        using var scope = api.Services.CreateScope();
        var service = new RouteGeometryService(scope.ServiceProvider.GetRequiredService<IApplicationDbContext>(),
            new HttpClient(new FakeOsrm { Status = HttpStatusCode.ServiceUnavailable }), options, new RouteGeometryCache(),
            NullLogger<RouteGeometryService>.Instance);

        var result = await service.GetAsync(planId);
        Assert.Equal("approximate", result!.Source);
        Assert.Equal(5, result.Coordinates.Count);
    }

    private sealed class FakeOsrm : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public int Calls { get; private set; }
        public string LastUrl { get; private set; } = "";
        public string UserAgent { get; private set; } = "";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastUrl = request.RequestUri!.ToString();
            UserAgent = request.Headers.UserAgent.ToString();
            const string body = """
                {"code":"Ok","routes":[{"distance":12345,"duration":1800,
                 "geometry":{"type":"LineString","coordinates":[[-78.50,-0.20],[-78.49,-0.18],[-78.48,-0.17],[-78.50,-0.20]]}}]}
                """;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
