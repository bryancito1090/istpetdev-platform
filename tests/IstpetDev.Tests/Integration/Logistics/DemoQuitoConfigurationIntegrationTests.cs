using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public class DemoQuitoConfigurationIntegrationTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static string DemoQuitoConfigFile =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json"));

    [Fact]
    public async Task Api_with_demo_quito_config_boots_healthy_and_loads_service_cost_and_traffic_bands()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["LOGISTICS_CONFIG_FILE"] = DemoQuitoConfigFile
        };

        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true, overrides: overrides);
        var client = api.CreateClient();

        // 1. Health check /health/ready debe responder Healthy (200 OK)
        var health = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        // 2. Comprobar que en DI se hayan cargado todas las secciones desde appsettings.demo-quito.json
        DateOnly today;
        using (var scope = api.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<IOptions<LogisticsOptions>>().Value;
            Assert.NotNull(options.ServiceCost);
            Assert.True(options.ServiceCost.IsValid);
            Assert.Equal(3.11m, options.ServiceCost.FuelPricePerGallonUsd);
            Assert.Equal(4.13m, options.ServiceCost.HelperCostPerHour);
            Assert.Equal(3, options.TrafficBands.Count);
            Assert.NotNull(options.ContractedCarrier);
            Assert.True(options.ContractedCarrier.IsConfigured);
            Assert.NotNull(options.Recommendation);
            Assert.Equal(0.35m, options.Recommendation.WeightCost);

            today = await scope.ServiceProvider.GetRequiredService<IOperationalClock>().TodayAsync();
        }

        // 3. Reset del fixture demo-quito-planificacion y generación de rutas
        var planner = api.ClientFor(TestTokens.Planner);
        var reset = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-quito-planificacion", null);
        Assert.True(reset.IsSuccessStatusCode, await reset.Content.ReadAsStringAsync());

        var planResponse = await planner.PostAsJsonAsync("/api/v1/route-plans", new PlanRequest(today, null, true));
        Assert.Equal(HttpStatusCode.Created, planResponse.StatusCode);

        var planDoc = JsonDocument.Parse(await planResponse.Content.ReadAsStringAsync()).RootElement;
        var routes = planDoc.GetProperty("routes").EnumerateArray().ToList();
        Assert.NotEmpty(routes);

        // Con jornada a las 07:00 (hora pico matutina 06:00-09:30), la primera parada reporta factor 0.64
        var firstStop = routes[0].GetProperty("stops").EnumerateArray().First();
        Assert.Equal(0.64m, firstStop.GetProperty("trafficFactorApplied").GetDecimal());

        // 4. Evaluar ruta con desglose de costos y horas pico (#240 y #245 activos)
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vehicle = await db.Vehicles.FirstAsync(v => v.OrganizationId == DemoOrganization.Id && v.Code == "VEH-0001");
            var warehouse = await db.Locations.FirstAsync(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse);
            var sp = await db.ServicePoints.FirstAsync(p => p.OrganizationId == DemoOrganization.Id);
            var product = await db.Products.FirstAsync(p => p.OrganizationId == DemoOrganization.Id && p.Sku == "PRD-0003");

            var stopsToEval = new List<RouteStopCandidate>
            {
                new(1, RouteStopKind.Pickup, null, warehouse.Id, warehouse.Code, warehouse.Name ?? "Bodega",
                    new GeoPoint((decimal)warehouse.Position!.Y, (decimal)warehouse.Position!.X),
                    303m, 0.45m, new TimeOnly(7, 0), new TimeOnly(18, 0), 20, [new(product.Id, product.Sku, 10m, "saco")]),
                new(2, RouteStopKind.Delivery, sp.Id, null, sp.Code, sp.Name,
                    new GeoPoint((decimal)sp.Position!.Y, (decimal)sp.Position!.X),
                    -303m, -0.45m, new TimeOnly(7, 0), new TimeOnly(11, 0), 30, [new(product.Id, product.Sku, 10m, "saco")])
            };

            var evalPeakRes = await planner.PostAsJsonAsync("/api/v1/route-plans/evaluate", new EvaluateRouteRequest(
                stopsToEval, vehicle.Id, new DateTime(2026, 10, 16, 12, 30, 0, DateTimeKind.Utc)));
            Assert.Equal(HttpStatusCode.OK, evalPeakRes.StatusCode);
            var evalPeakJson = await evalPeakRes.Content.ReadFromJsonAsync<JsonElement>();

            var evalOffPeakRes = await planner.PostAsJsonAsync("/api/v1/route-plans/evaluate", new EvaluateRouteRequest(
                stopsToEval, vehicle.Id, new DateTime(2026, 10, 16, 16, 0, 0, DateTimeKind.Utc)));
            Assert.Equal(HttpStatusCode.OK, evalOffPeakRes.StatusCode);
            var evalOffPeakJson = await evalOffPeakRes.Content.ReadFromJsonAsync<JsonElement>();

            // Criterio #275: La misma ruta a las 07:30 tarda más que a las 11:00
            var durPeak = evalPeakJson.GetProperty("totalDurationMinutes").GetDecimal();
            var durOffPeak = evalOffPeakJson.GetProperty("totalDurationMinutes").GetDecimal();
            Assert.True(durPeak > durOffPeak, $"Hora pico ({durPeak} min) debe ser mayor que fuera de pico ({durOffPeak} min)");

            // Criterio #275: El desglose de una ruta muestra combustible, ayudantes y merma distintos de cero
            var breakdown = evalPeakJson.GetProperty("serviceCostBreakdown");
            Assert.True(breakdown.GetProperty("fuel").GetDecimal() > 0m);
            Assert.True(breakdown.GetProperty("wear").GetDecimal() > 0m);
            Assert.True(breakdown.GetProperty("driver").GetDecimal() > 0m);
            Assert.True(breakdown.GetProperty("helpers").GetDecimal() > 0m);
            Assert.True(breakdown.GetProperty("shrink").GetDecimal() > 0m);
            Assert.Equal(3.11m, breakdown.GetProperty("fuelPrice").GetProperty("value").GetDecimal());
        }
    }
}
