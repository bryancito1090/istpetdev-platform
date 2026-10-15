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

namespace IstpetDev.Tests.E2E;

[Trait("Category", "E2E")]
public sealed class LogisticsJourneyTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static readonly Dictionary<string, string?> OverridesWithManager = new()
    {
        ["Demo:Accounts:2:Subject"] = "manager-1",
        ["Demo:Accounts:2:DisplayName"] = "Gerencia de prueba",
        ["Demo:Accounts:2:Role"] = Roles.Gerencia
    };

    [Fact]
    public async Task E2E_01_Reset_regional_and_risks()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        // 1. Reset fixture demo-region
        var resetResp = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        Assert.Equal(HttpStatusCode.OK, resetResp.StatusCode);

        // 2. GET /risks
        var risksResp = await planner.GetAsync("/api/v1/risks");
        Assert.Equal(HttpStatusCode.OK, risksResp.StatusCode);

        var risksDoc = await risksResp.Content.ReadFromJsonAsync<JsonElement>();
        var items = risksDoc.GetProperty("items");
        var facts = risksDoc.GetProperty("facts");

        Assert.True(items.GetArrayLength() > 0);
        Assert.True(facts.GetArrayLength() > 0);

        // Verificar HOS-N01 cobertura 5.0 y nivel Alto
        bool foundHosN01 = false;
        foreach (var item in items.EnumerateArray())
        {
            if (item.GetProperty("servicePointCode").GetString() == "HOS-N01" &&
                item.GetProperty("sku").GetString() == "DES-01")
            {
                foundHosN01 = true;
                Assert.Equal(5.0m, item.GetProperty("coverageDays").GetDecimal());
                Assert.Equal("Alto", item.GetProperty("level").GetString());
            }
        }
        Assert.True(foundHosN01);
    }

    [Fact]
    public async Task E2E_02_Route_determinism_and_fefo_publication()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        var planReq = new PlanRequest(new DateOnly(2026, 10, 12), null, true);

        // 1. Planificación inicial
        var resp1 = await planner.PostAsJsonAsync("/api/v1/route-plans", planReq);
        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);
        var doc1 = await resp1.Content.ReadFromJsonAsync<JsonElement>();
        var routes1 = doc1.GetProperty("routes");
        Assert.Equal(2, routes1.GetArrayLength());

        var firstRoutePlanId = routes1[0].GetProperty("routePlanId").GetGuid();

        // 2. Publicación FEFO
        var pubResp1 = await planner.PostAsJsonAsync($"/api/v1/route-plans/{firstRoutePlanId}/publish", new { });
        Assert.Equal(HttpStatusCode.OK, pubResp1.StatusCode);

        // Idempotencia: segunda publicación da OK sin duplicar
        var pubResp2 = await planner.PostAsJsonAsync($"/api/v1/route-plans/{firstRoutePlanId}/publish", new { });
        Assert.Equal(HttpStatusCode.OK, pubResp2.StatusCode);

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var stopIds = await db.RouteStops.Where(s => s.RoutePlanId == firstRoutePlanId).Select(s => s.Id).ToListAsync();
        var deliveries = await db.Deliveries.Where(d => d.RouteStopId != null && stopIds.Contains(d.RouteStopId.Value)).ToListAsync();
        Assert.NotEmpty(deliveries);

        var reservations = await db.RouteStockReservations.Where(r => r.RoutePlanId == firstRoutePlanId).ToListAsync();
        Assert.NotEmpty(reservations);
        Assert.All(reservations, r => Assert.Equal("Active", r.Status));
    }

    [Fact]
    public async Task E2E_03_Predefined_incident_autonomy_and_approval()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        var planReq = new PlanRequest(new DateOnly(2026, 10, 12), null, true);
        var planResp = await planner.PostAsJsonAsync("/api/v1/route-plans", planReq);
        var planDoc = await planResp.Content.ReadFromJsonAsync<JsonElement>();
        var routes = planDoc.GetProperty("routes");

        var planIds = new List<Guid>();
        foreach (var r in routes.EnumerateArray())
        {
            var id = r.GetProperty("routePlanId").GetGuid();
            planIds.Add(id);
            await planner.PostAsJsonAsync($"/api/v1/route-plans/{id}/publish", new { });
        }

        // Completar 2 paradas en la ruta 1
        await planner.PostAsJsonAsync($"/api/v1/route-plans/{planIds[0]}/stops/1/complete", new { });
        await planner.PostAsJsonAsync($"/api/v1/route-plans/{planIds[0]}/stops/2/complete", new { });

        // Disparar incidente predefinido cierre-norte
        var incResp = await planner.PostAsJsonAsync("/api/v1/demo/incidents", new { preset = "cierre-norte" });
        Assert.Equal(HttpStatusCode.OK, incResp.StatusCode);

        // Crear incidente real en la BD para probar replanificación
        var createIncResp = await planner.PostAsJsonAsync("/api/v1/incidents",
            Fixtures.IncidentPreset("demo-region", "cierre-norte"));
        Assert.Equal(HttpStatusCode.Created, createIncResp.StatusCode);
        var incDoc = await createIncResp.Content.ReadFromJsonAsync<JsonElement>();
        var incidentId = incDoc.GetProperty("incidentId").GetGuid();

        // Replanificar primera ruta afectada
        var replanResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planIds[0]}/replan", new { incidentId });
        Assert.Equal(HttpStatusCode.Created, replanResp.StatusCode);
        var replanDoc = await replanResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(replanDoc.TryGetProperty("decision", out var decision));
        Assert.True(replanDoc.TryGetProperty("diff", out _));

        // Verificar decisiones
        var decisionsResp = await planner.GetAsync("/api/v1/agent-decisions");
        Assert.Equal(HttpStatusCode.OK, decisionsResp.StatusCode);
    }

    [Fact]
    public async Task E2E_04_Revert_and_autonomy_mode_control()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true, OverridesWithManager);
        var planner = api.ClientFor(TestTokens.Planner);
        var manager = api.ClientFor("manager-1");

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        // 1. Cambiar modo de autonomía a ProposeOnly como Gerencia
        var modeResp = await manager.PutAsJsonAsync("/api/v1/autonomy/mode", new { mode = "ProposeOnly" });
        Assert.Equal(HttpStatusCode.OK, modeResp.StatusCode);

        // 2. Planificar y publicar
        var planResp = await planner.PostAsJsonAsync("/api/v1/route-plans", new PlanRequest(new DateOnly(2026, 10, 12), null, true));
        var planDoc = await planResp.Content.ReadFromJsonAsync<JsonElement>();
        var planId = planDoc.GetProperty("routes")[0].GetProperty("routePlanId").GetGuid();
        await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/publish", new { });

        // 3. Crear incidente y replanificar bajo modo ProposeOnly
        var createIncResp = await planner.PostAsJsonAsync("/api/v1/incidents",
            Fixtures.IncidentPreset("demo-region", "cierre-norte"));
        var incDoc = await createIncResp.Content.ReadFromJsonAsync<JsonElement>();
        var incidentId = incDoc.GetProperty("incidentId").GetGuid();

        var replanResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/replan", new { incidentId });
        Assert.Equal(HttpStatusCode.Created, replanResp.StatusCode);
        var replanDoc = await replanResp.Content.ReadFromJsonAsync<JsonElement>();

        // En ProposeOnly, la decisión queda Escalated y no se publica automáticamente
        Assert.Equal("Escalated", replanDoc.GetProperty("decision").GetProperty("outcome").GetString());
        var rev2Id = replanDoc.GetProperty("proposedRoutePlanId").GetGuid();

        // 4. Publicar la revisión 2
        await planner.PostAsJsonAsync($"/api/v1/route-plans/{rev2Id}/publish", new { });

        // 5. Revertir la revisión 2 genera una tercera revisión que restaura la revisión 1
        var revertResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{rev2Id}/revert", new { });
        Assert.Equal(HttpStatusCode.OK, revertResp.StatusCode);

        var revertDoc = await revertResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, revertDoc.GetProperty("revision").GetInt32());
        Assert.Equal("Published", revertDoc.GetProperty("status").GetString());
    }

    [Fact]
    public async Task E2E_05_Offline_operation_and_road_cache()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Verificar que todos los 240 tramos viales están cacheados
        var edgeCount = await db.RoadMatrixEdges.CountAsync(e => e.OrganizationId == DemoOrganization.Id);
        Assert.Equal(240, edgeCount);

        // Planificar sin acceso externo utiliza caché local
        var planResp = await planner.PostAsJsonAsync("/api/v1/route-plans", new PlanRequest(new DateOnly(2026, 10, 12), null, true));
        Assert.Equal(HttpStatusCode.Created, planResp.StatusCode);
    }
}
