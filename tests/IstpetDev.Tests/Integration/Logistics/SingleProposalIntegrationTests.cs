using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Logistics;
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

        // Idempotencia con proposalId: el reintento reproduce la respuesta original (201) sin crear otro plan.
        var duplicateConfirmResp = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = "RP-VAL-01",
            vehicleId,
            proposalId,
            orderIds = new[] { critOrderId, lowOrderId },
            stops = validStops
        });
        Assert.Equal(HttpStatusCode.Created, duplicateConfirmResp.StatusCode);
        var dupDoc = await duplicateConfirmResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(createdPlanId, dupDoc.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Confirm_and_manual_plans_enforce_server_costs_and_unknown_cost_semantics()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        Guid pointId, productId, vehicleId, warehouseId;
        Vehicle vehicle;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pointId = (await db.ServicePoints.FirstAsync(p => p.OrganizationId == DemoOrganization.Id)).Id;
            productId = (await db.Products.FirstAsync(p => p.OrganizationId == DemoOrganization.Id && p.Sku == "DES-01")).Id;
            vehicle = await db.Vehicles.FirstAsync(v => v.OrganizationId == DemoOrganization.Id && v.IsActive);
            vehicleId = vehicle.Id;
            warehouseId = (await db.Locations.FirstAsync(l => l.Kind == LocationKinds.Warehouse)).Id;
        }

        // 1. Confirm ignora totalCost = 999999.99m manipulado por el cliente y conserva el costo calculado por servidor,
        // incluso cuando el depósito coincide con la primera recogida (distanceToFirstPickupKm == 0).
        var orderResponse = await planner.PostAsJsonAsync("/api/v1/delivery-orders", new
        {
            destinationServicePointId = pointId,
            requiredDateUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(14),
            priority = "High",
            lines = new[] { new { productId, quantity = 5m } }
        });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var orderId = (await orderResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var stops = new object[]
        {
            new { sequence = 1, kind = "Pickup", locationId = warehouseId, code = "BOD-UIO", name = "Bodega Central", position = new { lat = -0.22m, lon = -78.52m }, deltaWeight = 5m, deltaVolume = 0.05m, serviceMinutes = 15 },
            new { sequence = 2, kind = "Delivery", servicePointId = pointId, code = "SP-01", name = "Punto 1", position = new { lat = -0.24m, lon = -78.50m }, deltaWeight = -5m, deltaVolume = -0.05m, serviceMinutes = 15 }
        };

        var proposalId = Guid.NewGuid();
        var confirmResp = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = "RP-COST-TAMPER-01",
            vehicleId,
            proposalId,
            totalCost = 999999.99m,
            orderIds = new[] { orderId },
            stops
        });
        Assert.Equal(HttpStatusCode.Created, confirmResp.StatusCode);
        var confirmDoc = await confirmResp.Content.ReadFromJsonAsync<JsonElement>();
        var confirmedCost = confirmDoc.GetProperty("totalCost").GetDecimal();
        Assert.NotEqual(999999.99m, confirmedCost);
        Assert.True(confirmedCost > 0m);

        Assert.True(confirmDoc.TryGetProperty("costBreakdown", out var confirmBreakdown));
        Assert.Equal(confirmedCost, confirmBreakdown.GetProperty("totalCost").GetDecimal());
        Assert.True(confirmDoc.TryGetProperty("costUncertainty", out var confirmUncertainty));
        Assert.Equal("uncalibrated", confirmUncertainty.GetProperty("status").GetString());

        var planId = confirmDoc.GetProperty("id").GetGuid();

        // Verificar GetById y snapshot congelado
        var getByIdResp = await planner.GetAsync($"/api/v1/route-plans/{planId}");
        Assert.Equal(HttpStatusCode.OK, getByIdResp.StatusCode);
        var getByIdDoc = await getByIdResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(confirmedCost, getByIdDoc.GetProperty("totalCost").GetDecimal());
        Assert.Equal("estimated", getByIdDoc.GetProperty("costStatus").GetString());
        Assert.True(getByIdDoc.TryGetProperty("estimateSnapshot", out var snapshot));
        Assert.Equal(JsonValueKind.Object, snapshot.ValueKind);
        Assert.Equal(confirmedCost, snapshot.GetProperty("totalCost").GetDecimal());
        Assert.Equal("1.0", snapshot.GetProperty("calculatorVersion").GetString());

        // 2. POST /api/v1/route-plans/manual calcula TotalCost con horas y CostPerKm del vehículo, no km * 0.45m sin horas.
        var manualResp = await planner.PostAsJsonAsync("/api/v1/route-plans/manual", new
        {
            code = "RP-MANUAL-COST-01",
            vehicleId,
            totalDistanceKm = 20m,
            totalDurationMinutes = 120m,
            stops = new[]
            {
                new { sequence = 1, servicePointId = pointId, code = "SP-01", name = "Punto 1" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, manualResp.StatusCode);
        var manualDoc = await manualResp.Content.ReadFromJsonAsync<JsonElement>();
        var manualCost = manualDoc.GetProperty("totalCost").GetDecimal();
        var rateKm = vehicle.CostPerKm ?? 0.45m;
        var expectedManualCost = LogisticsCostCalculator.CalculateCost(20m, 120m, rateKm, 6.00m);
        Assert.Equal(expectedManualCost, manualCost);
        Assert.True(manualCost > 20m * 0.45m);

        // 3. GET /api/v1/route-plans devuelve cost = null y costStatus = "unknown" si TotalCost = null
        Guid nullCostPlanId = Guid.NewGuid();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.RoutePlans.Add(new RoutePlan
            {
                Id = nullCostPlanId,
                OrganizationId = DemoOrganization.Id,
                Code = "RP-NULL-COST-01",
                Revision = 1,
                VehicleId = vehicleId,
                Status = "Draft",
                ProposedBy = "Test",
                PlannedStartUtc = DateTime.UtcNow.AddDays(2),
                TotalKm = 15m,
                TotalMinutes = 45m,
                TotalCost = null,
                AlgorithmVersion = "test"
            });
            await db.SaveChangesAsync();
        }

        var listResp = await planner.GetAsync("/api/v1/route-plans?status=Draft");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var listDoc = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var items = listDoc.GetProperty("items").EnumerateArray().ToList();
        var nullPlanItem = items.First(i => i.GetProperty("id").GetGuid() == nullCostPlanId);
        Assert.Equal(JsonValueKind.Null, nullPlanItem.GetProperty("cost").ValueKind);
        Assert.Equal(JsonValueKind.Null, nullPlanItem.GetProperty("totalCost").ValueKind);
        Assert.Equal("unknown", nullPlanItem.GetProperty("costStatus").GetString());
    }

    [Fact]
    public async Task ReconcileCost_and_CostCalibration_flow_respects_rules_and_preserves_inputJson()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        Guid vehicleId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            vehicleId = (await db.Vehicles.FirstAsync(v => v.OrganizationId == DemoOrganization.Id && v.IsActive)).Id;
        }

        // Crear una ruta base
        var planResp = await planner.PostAsJsonAsync("/api/v1/route-plans/manual", new
        {
            code = "RP-REC-01",
            vehicleId,
            totalDistanceKm = 50m,
            totalDurationMinutes = 120m,
            stops = new[]
            {
                new { sequence = 1, servicePointId = Guid.NewGuid(), code = "SP-01", name = "Punto 1" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, planResp.StatusCode);
        var planDoc = await planResp.Content.ReadFromJsonAsync<JsonElement>();
        var planId = planDoc.GetProperty("id").GetGuid();
        var originalEstimated = planDoc.GetProperty("totalCost").GetDecimal();

        // 1. Incidente sin cuantificar: costStatus == "incomplete_unknown" y reconciledTotalCost == null
        var incResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/reconcile-cost", new
        {
            incidentKind = "bloqueo_vial",
            incidentQuantified = false
        });
        Assert.Equal(HttpStatusCode.OK, incResp.StatusCode);
        var incDoc = await incResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("incomplete_unknown", incDoc.GetProperty("costStatus").GetString());
        Assert.Equal(JsonValueKind.Null, incDoc.GetProperty("reconciledTotalCost").ValueKind);
        var missing1 = incDoc.GetProperty("missingComponents").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("unquantified_incident", missing1);

        // 2. Conciliación completa aplicando reglas de combustible y mantenimiento
        var recResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/reconcile-cost", new
        {
            actualDistanceKm = 48.5m,
            actualDrivingMinutes = 110m,
            actualServiceMinutes = 15m,
            actualWaitMinutes = 5m,
            fuelLitersConsumed = 10m,
            fuelPricePerLiter = 2.50m,
            fuelPurchasedTotalUsd = 30.00m,
            driverHours = 2.5m,
            driverHourlyCost = 8.00m,
            helperHours = 2.5m,
            helperHourlyCost = 4.00m,
            wearAndMaintenanceCost = 15.00m,
            tollsAndDirectCost = 3.00m,
            incidentExtraCost = 12.00m,
            incidentKind = "reparacion_menor",
            incidentQuantified = true,
            maintenanceCoversIncidentRepair = true
        });
        Assert.Equal(HttpStatusCode.OK, recResp.StatusCode);
        var recDoc = await recResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("reconciled", recDoc.GetProperty("costStatus").GetString());

        // Fuel: 10 * 2.5 = 25.00
        // Driver: 2.5 * 8 = 20.00
        // Helper: 2.5 * 4 = 10.00
        // Personnel: 30.00
        // Wear & Incident con maintenanceCoversIncidentRepair: max(15, 12) = 15.00
        // Tolls: 3.00
        // Total = 25 + 30 + 15 + 3 = 73.00
        Assert.Equal(73.00m, recDoc.GetProperty("reconciledTotalCost").GetDecimal());
        var notes = recDoc.GetProperty("reconciliationNotes").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("incident_repair_reconciled_with_maintenance", notes);
        Assert.Equal(Math.Round(73.00m - originalEstimated, 2), recDoc.GetProperty("residualError").GetDecimal());

        // 3. GetById expone reconciledCost y reconciledExecution
        var detailResp = await planner.GetAsync($"/api/v1/route-plans/{planId}");
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);
        var detailDoc = await detailResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(73.00m, detailDoc.GetProperty("reconciledCost").GetDecimal());
        Assert.True(detailDoc.TryGetProperty("reconciledExecution", out var recExecEl));
        Assert.Equal(JsonValueKind.Object, recExecEl.ValueKind);
        Assert.Equal("reconciled", detailDoc.GetProperty("costStatus").GetString());

        // 4. GET /api/v1/route-plans/cost-calibration con 1 viaje retorna "uncalibrated" (requiere >= 10)
        var calibResp = await planner.GetAsync("/api/v1/route-plans/cost-calibration?estimatedCost=80");
        Assert.Equal(HttpStatusCode.OK, calibResp.StatusCode);
        var calibDoc = await calibResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("uncalibrated", calibDoc.GetProperty("status").GetString());
        Assert.Equal(1, calibDoc.GetProperty("sampleSize").GetInt32());
    }
}
