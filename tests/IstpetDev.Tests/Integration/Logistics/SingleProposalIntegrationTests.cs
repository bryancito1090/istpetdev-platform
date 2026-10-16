using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public class SingleProposalIntegrationTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Suggest_returns_single_proposal_with_cost_breakdown_uncertainty_and_requestId_idempotency()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        Guid pointId, productId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pointId = (await db.ServicePoints.FirstAsync(p => p.OrganizationId == DemoOrganization.Id)).Id;
            productId = (await db.Products.FirstAsync(p => p.OrganizationId == DemoOrganization.Id && p.Sku == "DES-01")).Id;
        }

        var orderResponse = await planner.PostAsJsonAsync("/api/v1/delivery-orders", new
        {
            destinationServicePointId = pointId,
            requiredDateUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(14),
            priority = "High",
            lines = new[] { new { productId, quantity = 10m } }
        });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var orderDoc = await orderResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = orderDoc.GetProperty("id").GetGuid();

        var requestId = Guid.NewGuid().ToString("N");

        // Primera llamada con requestId
        var suggestResp1 = await planner.PostAsJsonAsync("/api/v1/route-plans/suggest", new
        {
            orderIds = new[] { orderId },
            requestId = requestId
        });
        Assert.Equal(HttpStatusCode.OK, suggestResp1.StatusCode);
        var doc1 = await suggestResp1.Content.ReadFromJsonAsync<JsonElement>();

        // Criterio 1: Propuesta única (desaparece menú A/B hacia el usuario)
        var options = doc1.GetProperty("options").EnumerateArray().ToList();
        Assert.Single(options);

        var singleOption = options[0];
        Assert.True(singleOption.GetProperty("isRecommended").GetBoolean());
        Assert.True(doc1.TryGetProperty("chosenVehicle", out var chosenVehicle));
        Assert.False(string.IsNullOrEmpty(chosenVehicle.GetProperty("code").GetString()));

        // Criterio 9: Desglose canónico de costo unificado
        Assert.True(singleOption.TryGetProperty("costBreakdown", out var costBreakdown));
        Assert.True(costBreakdown.GetProperty("totalCost").GetDecimal() > 0m);
        Assert.Equal("USD", costBreakdown.GetProperty("currency").GetString());
        Assert.Equal("1.0", costBreakdown.GetProperty("calculatorVersion").GetString());
        Assert.True(costBreakdown.TryGetProperty("kmCost", out _));
        Assert.True(costBreakdown.TryGetProperty("hourCost", out _));

        // Criterio 10: Incertidumbre no calibrada honesta (sin fabricar intervalos)
        Assert.True(singleOption.TryGetProperty("costUncertainty", out var costUncertainty));
        Assert.Equal("uncalibrated", costUncertainty.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, costUncertainty.GetProperty("lowerBound").ValueKind);
        Assert.Equal(JsonValueKind.Null, costUncertainty.GetProperty("upperBound").ValueKind);
        Assert.Equal("conformal_residuals_quantile", costUncertainty.GetProperty("method").GetString());

        // Criterio 1: Idempotencia ante requestId repetido
        var suggestResp2 = await planner.PostAsJsonAsync("/api/v1/route-plans/suggest", new
        {
            orderIds = new[] { orderId },
            requestId = requestId
        });
        Assert.Equal(HttpStatusCode.OK, suggestResp2.StatusCode);
        var doc2 = await suggestResp2.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            singleOption.GetProperty("estimatedTotalCost").GetDecimal(),
            doc2.GetProperty("options")[0].GetProperty("estimatedTotalCost").GetDecimal()
        );
    }

    [Fact]
    public async Task Confirm_rejects_priority_inversion_and_supports_proposalId_idempotency()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        Guid p1, p2, productId, vehicleId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var points = await db.ServicePoints.Where(p => p.OrganizationId == DemoOrganization.Id).Take(2).ToListAsync();
            p1 = points[0].Id;
            p2 = points[1].Id;
            productId = (await db.Products.FirstAsync(p => p.OrganizationId == DemoOrganization.Id && p.Sku == "DES-01")).Id;
            vehicleId = (await db.Vehicles.FirstAsync(v => v.OrganizationId == DemoOrganization.Id && v.IsActive)).Id;
        }

        // Crear orden Crítica en p1 y orden Low en p2
        var critOrder = await planner.PostAsJsonAsync("/api/v1/delivery-orders", new
        {
            destinationServicePointId = p1,
            requiredDateUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
            priority = "Critical",
            lines = new[] { new { productId, quantity = 5m } }
        });
        var critOrderId = (await critOrder.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var lowOrder = await planner.PostAsJsonAsync("/api/v1/delivery-orders", new
        {
            destinationServicePointId = p2,
            requiredDateUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(14),
            priority = "Low",
            lines = new[] { new { productId, quantity = 5m } }
        });
        var lowOrderId = (await lowOrder.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var warehouseId = Guid.NewGuid();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            warehouseId = (await db.Locations.FirstAsync(l => l.Kind == LocationKinds.Warehouse)).Id;
        }

        // Intento de confirmación con inversión de prioridad: Low (p2) antes que Critical (p1)
        var invertedStops = new object[]
        {
            new { sequence = 1, kind = "Pickup", locationId = warehouseId, code = "BOD-UIO", name = "Bodega", position = new { lat = -0.22m, lon = -78.52m }, deltaWeight = 10m, deltaVolume = 0.1m, serviceMinutes = 15 },
            new { sequence = 2, kind = "Delivery", servicePointId = p2, code = "P2-LOW", name = "P2", position = new { lat = -0.23m, lon = -78.51m }, deltaWeight = -5m, deltaVolume = -0.05m, serviceMinutes = 15 },
            new { sequence = 3, kind = "Delivery", servicePointId = p1, code = "P1-CRIT", name = "P1", position = new { lat = -0.24m, lon = -78.50m }, deltaWeight = -5m, deltaVolume = -0.05m, serviceMinutes = 15 }
        };

        var invertConfirmResp = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = "RP-INV-01",
            vehicleId,
            orderIds = new[] { critOrderId, lowOrderId },
            stops = invertedStops
        });
        // Criterio 8: Rechazar orden que invierta prioridad
        Assert.Equal(HttpStatusCode.Conflict, invertConfirmResp.StatusCode);

        // Confirmación válida: Critical (p1) antes que Low (p2)
        var validStops = new object[]
        {
            new { sequence = 1, kind = "Pickup", locationId = warehouseId, code = "BOD-UIO", name = "Bodega", position = new { lat = -0.22m, lon = -78.52m }, deltaWeight = 10m, deltaVolume = 0.1m, serviceMinutes = 15 },
            new { sequence = 2, kind = "Delivery", servicePointId = p1, code = "P1-CRIT", name = "P1", position = new { lat = -0.24m, lon = -78.50m }, deltaWeight = -5m, deltaVolume = -0.05m, serviceMinutes = 15 },
            new { sequence = 3, kind = "Delivery", servicePointId = p2, code = "P2-LOW", name = "P2", position = new { lat = -0.23m, lon = -78.51m }, deltaWeight = -5m, deltaVolume = -0.05m, serviceMinutes = 15 }
        };

        var proposalId = Guid.NewGuid();
        var validConfirmResp = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = "RP-VAL-01",
            vehicleId,
            proposalId,
            orderIds = new[] { critOrderId, lowOrderId },
            stops = validStops
        });
        Assert.Equal(HttpStatusCode.Created, validConfirmResp.StatusCode);
        var createdDoc = await validConfirmResp.Content.ReadFromJsonAsync<JsonElement>();
        var createdPlanId = createdDoc.GetProperty("id").GetGuid();

        // Idempotencia con proposalId
        var duplicateConfirmResp = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = "RP-VAL-01",
            vehicleId,
            proposalId,
            orderIds = new[] { critOrderId, lowOrderId },
            stops = validStops
        });
        Assert.Equal(HttpStatusCode.OK, duplicateConfirmResp.StatusCode);
        var dupDoc = await duplicateConfirmResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(createdPlanId, dupDoc.GetProperty("id").GetGuid());
    }
}
