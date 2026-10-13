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

namespace IstpetDev.Tests.Integration.Logistics;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class DemoRegionTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Demo_region_reset_imports_canonical_entities_and_calculates_calibrated_risks()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        // 1. Reset specifying demo-region fixture
        var resetResp = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        var contentStr = await resetResp.Content.ReadAsStringAsync();
        Assert.True(resetResp.IsSuccessStatusCode, contentStr);
        Assert.Equal(HttpStatusCode.OK, resetResp.StatusCode);

        var resetDoc = await resetResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(resetDoc.GetProperty("applied").GetBoolean());

        // 2. Verify imported entities in database
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<IOperationalClock>();
            var riskCalc = scope.ServiceProvider.GetRequiredService<IRiskCalculator>();

            // 1 bodega, 15 puntos, 2 vehiculos, 3 SKUs, 240 tramos
            Assert.Equal(1, await db.Locations.CountAsync(w => w.OrganizationId == DemoOrganization.Id && w.Kind == LocationKinds.Warehouse));
            Assert.Equal(15, await db.ServicePoints.CountAsync(sp => sp.OrganizationId == DemoOrganization.Id));
            Assert.Equal(2, await db.Vehicles.CountAsync(v => v.OrganizationId == DemoOrganization.Id));
            Assert.Equal(3, await db.Products.CountAsync(p => p.OrganizationId == DemoOrganization.Id));
            Assert.Equal(240, await db.RoadMatrixEdges.CountAsync(e => e.OrganizationId == DemoOrganization.Id));

            // Reloj Dataset -> 2026-10-12
            var today = await clock.TodayAsync();
            Assert.Equal(new DateOnly(2026, 10, 12), today);

            // 3. Evaluar los 15 items diseñados en el fixture (cada punto evalúa su SKU asignado)
            var risks = await riskCalc.CalculateAsync(persistSnapshots: false);
            var pointSkuMap = new Dictionary<string, string>
            {
                ["HOS-N01"] = "DES-01",
                ["CLI-N02"] = "PAP-01",
                ["OFI-N03"] = "PAP-01",
                ["EDU-N04"] = "PAP-01",
                ["COM-N05"] = "DES-01",
                ["HOT-N06"] = "PAP-01",
                ["OFI-N07"] = "GUA-01",
                ["HOS-S08"] = "DES-01",
                ["CLI-S09"] = "DES-01",
                ["EDU-S10"] = "PAP-01",
                ["COM-S11"] = "GUA-01",
                ["OFI-S12"] = "PAP-01",
                ["IND-S13"] = "GUA-01",
                ["GER-S14"] = "DES-01",
                ["HOS-C15"] = "DES-01"
            };

            var evaluatedRisks = risks.Where(r => pointSkuMap.TryGetValue(r.PointCode, out var sku) && r.Sku == sku).ToList();
            Assert.Equal(15, evaluatedRisks.Count);

            var criticos = evaluatedRisks.Count(r => r.Level == RiskLevel.Critico);
            var altos = evaluatedRisks.Count(r => r.Level == RiskLevel.Alto);
            var vigilar = evaluatedRisks.Count(r => r.Level == RiskLevel.Vigilar);
            var sinDatos = evaluatedRisks.Count(r => r.Level == RiskLevel.SinDatos);
            var normales = evaluatedRisks.Count(r => r.Level == RiskLevel.Normal);

            Assert.Equal(1, criticos);
            Assert.Equal(4, altos);
            Assert.Equal(3, vigilar);
            Assert.Equal(1, sinDatos);
            Assert.Equal(6, normales);

            // Caso del pitch: HOS-N01 / DES-01 tiene 5.0 días de cobertura y nivel Alto
            var hosN01 = evaluatedRisks.Single(r => r.PointCode == "HOS-N01");
            Assert.Equal("DES-01", hosN01.Sku);
            Assert.Equal(5.0m, hosN01.CoverageDays);
            Assert.Equal(RiskLevel.Alto, hosN01.Level);
        }

        // 4. Segundo reset preserva caché de tramos
        var secondReset = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        Assert.Equal(HttpStatusCode.OK, secondReset.StatusCode);

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(240, await db.RoadMatrixEdges.CountAsync(e => e.OrganizationId == DemoOrganization.Id));
        }
    }

    [Fact]
    public async Task Regional_scenario_plan_publish_incident_and_replanning_flow()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        // 1. Reset con demo-region
        var resetResp = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        Assert.Equal(HttpStatusCode.OK, resetResp.StatusCode);

        // 2. Proponer plan
        var planReq = new PlanRequest(new DateOnly(2026, 10, 12), null, true);
        var planResp = await planner.PostAsJsonAsync("/api/v1/route-plans", planReq);
        Assert.Equal(HttpStatusCode.Created, planResp.StatusCode);

        var planDoc = await planResp.Content.ReadFromJsonAsync<JsonElement>();
        var routes = planDoc.GetProperty("routes");
        Assert.True(routes.GetArrayLength() > 0);

        var planIds = new List<Guid>();
        foreach (var r in routes.EnumerateArray())
        {
            planIds.Add(r.GetProperty("routePlanId").GetGuid());
        }

        // 3. Publicar rutas
        foreach (var id in planIds)
        {
            var pubResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{id}/publish", new { });
            Assert.Equal(HttpStatusCode.OK, pubResp.StatusCode);
        }

        // 4. Completar 2 paradas en la primera ruta
        var firstPlanId = planIds[0];
        var c1 = await planner.PostAsJsonAsync($"/api/v1/route-plans/{firstPlanId}/stops/1/complete", new { });
        Assert.Equal(HttpStatusCode.OK, c1.StatusCode);
        var c2 = await planner.PostAsJsonAsync($"/api/v1/route-plans/{firstPlanId}/stops/2/complete", new { });
        Assert.Equal(HttpStatusCode.OK, c2.StatusCode);

        // 5. Crear incidente cierre-norte
        var incReq = new CreateIncidentRequest(
            Kind: "BloqueoVial",
            Lat: -0.1300m,
            Lon: -78.4800m,
            RadiusKm: 2.5m,
            Severity: "ALTA",
            Description: "Cierre vial en corredor norte hacia Hospital Norte",
            IsSynthetic: true
        );
        var incResp = await planner.PostAsJsonAsync("/api/v1/incidents", incReq);
        Assert.Equal(HttpStatusCode.Created, incResp.StatusCode);
        var incDoc = await incResp.Content.ReadFromJsonAsync<JsonElement>();
        var incidentId = incDoc.GetProperty("incidentId").GetGuid();

        // 6. Replanificar cada ruta afectada
        var affected = incDoc.GetProperty("affectedRoutes");
        Assert.True(affected.GetArrayLength() > 0);

        foreach (var aff in affected.EnumerateArray())
        {
            var affPlanId = aff.GetProperty("routePlanId").GetGuid();
            var replanResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{affPlanId}/replan", new { incidentId });
            Assert.Equal(HttpStatusCode.Created, replanResp.StatusCode);
            var replanDoc = await replanResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(replanDoc.TryGetProperty("decision", out var dec));
            Assert.True(replanDoc.TryGetProperty("diff", out _));
        }
    }
}
