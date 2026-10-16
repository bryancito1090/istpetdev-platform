using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Logistics;

// #243: el fixture demo-quito-planificacion se importa completo y sostiene el caso de la avería a mitad de ruta.
[Trait("Category", "Integration")]
public class DemoQuitoPlanificacionTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Fixture_imports_plans_and_a_breakdown_on_matilde_alvarez_affects_the_published_route()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        var reset = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-quito-planificacion", null);
        var resetBody = await reset.Content.ReadAsStringAsync();
        Assert.True(reset.IsSuccessStatusCode, resetBody);

        DateOnly today;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(3, await db.Locations.CountAsync(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse));
            Assert.Equal(40, await db.ServicePoints.CountAsync(p => p.OrganizationId == DemoOrganization.Id));
            Assert.Equal(10, await db.Vehicles.CountAsync(v => v.OrganizationId == DemoOrganization.Id));
            Assert.Equal(12, await db.Products.CountAsync(p => p.OrganizationId == DemoOrganization.Id));
            Assert.Equal(1806, await db.RoadMatrixEdges.CountAsync(e => e.OrganizationId == DemoOrganization.Id));

            // El brote del Enrique Garcés deja el hipoclorito por debajo del tiempo de reposición.
            var risks = await scope.ServiceProvider.GetRequiredService<IRiskCalculator>().CalculateAsync(persistSnapshots: false);
            var garces = risks.Single(r => r.PointCode == "PTO-0005" && r.Sku == "PRD-0001");
            Assert.Equal(RiskLevel.Critico, garces.Level);
            today = await scope.ServiceProvider.GetRequiredService<IOperationalClock>().TodayAsync();
        }

        var plan = await planner.PostAsJsonAsync("/api/v1/route-plans", new PlanRequest(today, null, true));
        Assert.Equal(HttpStatusCode.Created, plan.StatusCode);
        var routes = (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("routes");
        Assert.True(routes.GetArrayLength() > 0);
        var route = routes.EnumerateArray().First();
        var routePlanId = route.GetProperty("routePlanId").GetGuid();
        var vehicleCode = route.GetProperty("vehicleCode").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsJsonAsync($"/api/v1/route-plans/{routePlanId}/publish", new { })).StatusCode);

        // La avería se informa con el código del vehículo, como lo haría el conductor o el agente.
        var breakdown = Fixtures.IncidentPreset("demo-quito-planificacion", "averia-matilde-alvarez");
        var incident = await planner.PostAsJsonAsync("/api/v1/incidents", new
        {
            kind = breakdown.GetProperty("kind").GetString(),
            severity = breakdown.GetProperty("severity").GetString(),
            description = breakdown.GetProperty("description").GetString(),
            lat = breakdown.GetProperty("lat").GetDecimal(),
            lon = breakdown.GetProperty("lon").GetDecimal(),
            radiusKm = breakdown.GetProperty("radiusKm").GetDecimal(),
            vehicleCode,
            isSynthetic = true
        });
        var incidentBody = await incident.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, incident.StatusCode);
        var impact = JsonDocument.Parse(incidentBody).RootElement;
        var affected = Assert.Single(impact.GetProperty("affectedRoutes").EnumerateArray());
        Assert.Equal(routePlanId, affected.GetProperty("routePlanId").GetGuid());
        Assert.Equal(vehicleCode, affected.GetProperty("vehicleCode").GetString());

        // Un vehículo que no existe no crea un incidente huérfano.
        var unknown = await planner.PostAsJsonAsync("/api/v1/incidents", new { kind = "VEHICLE_BREAKDOWN", vehicleCode = "VEH-9999" });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
