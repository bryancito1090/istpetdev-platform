using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Logistics;

// #245: con las franjas de appsettings.demo-quito.json, las paradas del plan informan el factor de hora pico.
[Trait("Category", "Integration")]
public class TrafficBandsQuitoTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    // Mismas claves y valores que data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json (#243).
    private static readonly Dictionary<string, string?> QuitoBands = new()
    {
        ["Logistics:TrafficBands:0:Start"] = "06:00",
        ["Logistics:TrafficBands:0:End"] = "09:30",
        ["Logistics:TrafficBands:0:SpeedFactor"] = "0.64",
        ["Logistics:TrafficBands:1:Start"] = "12:30",
        ["Logistics:TrafficBands:1:End"] = "14:00",
        ["Logistics:TrafficBands:1:SpeedFactor"] = "0.80",
        ["Logistics:TrafficBands:2:Start"] = "16:00",
        ["Logistics:TrafficBands:2:End"] = "20:00",
        ["Logistics:TrafficBands:2:SpeedFactor"] = "0.64"
    };

    [Fact]
    public async Task Plan_with_quito_bands_reports_the_peak_hour_factor_per_stop()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true, overrides: QuitoBands);
        var planner = api.ClientFor(TestTokens.Planner);
        var reset = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-quito-planificacion", null);
        Assert.True(reset.IsSuccessStatusCode, await reset.Content.ReadAsStringAsync());

        DateOnly today;
        using (var scope = api.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<LogisticsOptions>>().Value;
            Assert.Equal(3, options.TrafficBands.Count);
            today = await scope.ServiceProvider.GetRequiredService<IOperationalClock>().TodayAsync();
        }

        var plan = await planner.PostAsJsonAsync("/api/v1/route-plans", new PlanRequest(today, null, true));
        var body = await plan.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, plan.StatusCode);
        var routes = JsonDocument.Parse(body).RootElement.GetProperty("routes").EnumerateArray().ToList();
        Assert.NotEmpty(routes);

        // La jornada empieza 07:00 (hora pico): la primera parada de cada ruta llega con el factor 0,64.
        foreach (var route in routes)
        {
            var first = route.GetProperty("stops").EnumerateArray().First();
            Assert.Equal(0.64m, first.GetProperty("trafficFactorApplied").GetDecimal());
        }

        // El detalle de la ruta conserva el factor guardado al proponerla.
        var routePlanId = routes[0].GetProperty("routePlanId").GetGuid();
        var detail = await planner.GetFromJsonAsync<JsonElement>($"/api/v1/route-plans/{routePlanId}");
        var firstDetail = detail.GetProperty("stops").EnumerateArray().First(s => s.GetProperty("sequence").GetInt32() == 1);
        Assert.Equal(0.64m, firstDetail.GetProperty("trafficFactorApplied").GetDecimal());
    }
}
