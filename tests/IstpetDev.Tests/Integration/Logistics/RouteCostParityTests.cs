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
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public class RouteCostParityTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Parity_across_calculator_feasibility_evaluate_confirm_and_manual()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        Guid vehicleId;
        string vehicleCode;
        const decimal costPerKm = 0.45m;
        const decimal costPerHour = 6.00m;
        Guid pointId;
        Guid warehouseId;

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var v = await db.Vehicles.FirstAsync(x => x.OrganizationId == DemoOrganization.Id && x.IsActive);
            v.VehicleType = null;
            v.CostPerKm = costPerKm;
            await db.SaveChangesAsync();
            vehicleId = v.Id;
            vehicleCode = v.Code;

            pointId = (await db.ServicePoints.FirstAsync(x => x.OrganizationId == DemoOrganization.Id)).Id;
            warehouseId = (await db.Locations.FirstAsync(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse)).Id;

            var pos0 = new GeoPoint(-0.1806m, -78.4678m);
            var pos1 = new GeoPoint(-0.2000m, -78.4900m);
            db.RoadMatrixEdges.AddRange(
                new RoadMatrixEdge
                {
                    OrganizationId = DemoOrganization.Id,
                    FromLat = pos0.Lat,
                    FromLon = pos0.Lon,
                    ToLat = pos1.Lat,
                    ToLon = pos1.Lon,
                    DistanceKm = 10.0m,
                    DurationMinutes = 20.0m
                },
                new RoadMatrixEdge
                {
                    OrganizationId = DemoOrganization.Id,
                    FromLat = pos1.Lat,
                    FromLon = pos1.Lon,
                    ToLat = pos0.Lat,
                    ToLon = pos0.Lon,
                    DistanceKm = 10.0m,
                    DurationMinutes = 20.0m
                }
            );
            await db.SaveChangesAsync();
        }

        var stops = new List<RouteStopCandidate>
        {
            new(
                Sequence: 1,
                Kind: RouteStopKind.Pickup,
                ServicePointId: null,
                LocationId: warehouseId,
                Code: "BOD-UIO",
                Name: "Bodega Principal",
                Position: new GeoPoint(-0.1806m, -78.4678m),
                WindowStart: new TimeOnly(8, 0),
                WindowEnd: new TimeOnly(18, 0),
                ServiceMinutes: 10,
                DeltaWeight: 10m,
                DeltaVolume: 0.1m,
                Loads: null
            ),
            new(
                Sequence: 2,
                Kind: RouteStopKind.Delivery,
                ServicePointId: pointId,
                LocationId: null,
                Code: "PT-01",
                Name: "Punto Entrega",
                Position: new GeoPoint(-0.2000m, -78.4900m),
                WindowStart: new TimeOnly(8, 0),
                WindowEnd: new TimeOnly(18, 0),
                ServiceMinutes: 15,
                DeltaWeight: -10m,
                DeltaVolume: -0.1m,
                Loads: null
            )
        };

        var startUtc = new DateTime(2026, 10, 17, 8, 0, 0, DateTimeKind.Utc);

        // 1. POST /api/v1/route-plans/evaluate
        var evalResp = await planner.PostAsJsonAsync("/api/v1/route-plans/evaluate", new
        {
            vehicleId,
            stops,
            plannedStartUtc = startUtc
        });
        Assert.Equal(HttpStatusCode.OK, evalResp.StatusCode);
        var evalDoc = await evalResp.Content.ReadFromJsonAsync<JsonElement>();
        var evalDistance = evalDoc.GetProperty("totalDistanceKm").GetDecimal();
        var evalMinutes = evalDoc.GetProperty("totalDurationMinutes").GetDecimal();
        var evalCost = evalDoc.GetProperty("totalCost").GetDecimal();

        // 2. POST /api/v1/route-plans/confirm
        var confirmResp = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            vehicleId,
            code = "PLAN-CONFIRM-PARITY",
            stops,
            plannedStartUtc = startUtc
        });
        Assert.Equal(HttpStatusCode.Created, confirmResp.StatusCode);
        var confirmDoc = await confirmResp.Content.ReadFromJsonAsync<JsonElement>();
        var confirmCost = confirmDoc.GetProperty("totalCost").GetDecimal();

        // 3. POST /api/v1/route-plans/manual
        var manualResp = await planner.PostAsJsonAsync("/api/v1/route-plans/manual", new
        {
            code = "PLAN-MANUAL-PARITY",
            vehicleId,
            totalDistanceKm = evalDistance,
            totalDurationMinutes = evalMinutes,
            stops = new[]
            {
                new { sequence = 1, servicePointId = pointId, code = "BOD-UIO", name = "Bodega Principal" },
                new { sequence = 2, servicePointId = pointId, code = "PT-01", name = "Punto Entrega" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, manualResp.StatusCode);
        var manualDoc = await manualResp.Content.ReadFromJsonAsync<JsonElement>();
        var manualCost = manualDoc.GetProperty("totalCost").GetDecimal();

        // 4. LogisticsCostCalculator.CalculateCost & ComputeBreakdown
        var calcCost = LogisticsCostCalculator.CalculateCost(evalDistance, evalMinutes, costPerKm, costPerHour);
        var breakdown = LogisticsCostCalculator.ComputeBreakdown(evalDistance, evalMinutes, costPerKm, costPerHour);

        // 5. RouteFeasibility.EvaluateRoute
        var depot = stops[0].Position;
        var p1 = stops[1].Position;
        var matrix = new Dictionary<(GeoPoint, GeoPoint), Leg>
        {
            [(depot, p1)] = new(depot, p1, evalDistance / 2m, evalMinutes / 2m, DistanceSource.Road, null),
            [(p1, depot)] = new(p1, depot, evalDistance / 2m, evalMinutes / 2m, DistanceSource.Road, null)
        };
        var stopCand = new StopCandidate(
            pointId, "PT-01", p1, TimeOnly.MinValue, TimeOnly.MaxValue,
            ServiceMinutes: 0, Weight: 10m, Volume: 0.1m, Loads: Array.Empty<StopLoad>(),
            IsConsolidation: false, Level: RiskLevel.Normal, Criticality: "normal", CoverageDays: 1, LeadTimeDays: 1);
        var vehicleObj = new Vehicle { Id = vehicleId, Code = vehicleCode, CostPerKm = costPerKm, MaxWeight = 1000m };
        var feas = RouteFeasibility.EvaluateRoute(
            vehicleObj, depot, new[] { stopCand }, startUtc, 8,
            costPerHour: costPerHour, defaultCostPerKm: costPerKm, matrix);

        // Exact parity across all methods at 2 decimal places
        Assert.Equal(calcCost, breakdown.TotalCost);
        Assert.Equal(evalCost, calcCost);
        Assert.Equal(confirmCost, calcCost);
        Assert.Equal(manualCost, calcCost);
        Assert.Equal(feas.CumulativeCost, calcCost);

        // KmCost + HourCost + TollsCost == TotalCost
        Assert.Equal(breakdown.KmCost + breakdown.HourCost + breakdown.TollsCost, breakdown.TotalCost);
    }

    [Fact]
    public async Task ReconcileCost_prevents_zero_substitution_double_counting_and_revision_leakage()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        Guid vehicleId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            vehicleId = (await db.Vehicles.FirstAsync(x => x.OrganizationId == DemoOrganization.Id)).Id;
        }

        var rev1Id = Guid.NewGuid();
        var rev2Id = Guid.NewGuid();

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var rev1 = new RoutePlan
            {
                Id = rev1Id,
                OrganizationId = DemoOrganization.Id,
                Code = "RP-AUDIT-01",
                Revision = 1,
                VehicleId = vehicleId,
                Status = "Confirmed",
                TotalKm = 20m,
                TotalMinutes = 60m,
                TotalCost = 50.00m,
                PlannedStartUtc = DateTime.UtcNow.AddHours(-4),
                AlgorithmVersion = "order-based-routing-1.0",
                InputJson = "{}"
            };
            var rev2 = new RoutePlan
            {
                Id = rev2Id,
                OrganizationId = DemoOrganization.Id,
                Code = "RP-AUDIT-01",
                Revision = 2,
                VehicleId = vehicleId,
                Status = "Confirmed",
                TotalKm = 30m,
                TotalMinutes = 90m,
                TotalCost = 75.00m,
                PlannedStartUtc = DateTime.UtcNow.AddHours(-2),
                AlgorithmVersion = "order-based-routing-1.0",
                InputJson = "{}"
            };
            db.RoutePlans.AddRange(rev1, rev2);
            await db.SaveChangesAsync();
        }

        // Caso A (combustible comprado sin litros consumidos medidos):
        // llama a POST /api/v1/route-plans/{rev2Id}/reconcile-cost con FuelPurchasedTotalUsd = 60m y FuelLitersConsumed = null.
        // Verifica que devuelve costStatus == "incomplete_unknown", reconciledTotalCost == null,
        // reconciledExecution.fuelCost == null, knownPartialCost >= 0 y contiene "fuel_consumed_unmeasured_only_purchase_provided" en missingComponents.
        var caseAResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{rev2Id}/reconcile-cost", new
        {
            fuelPurchasedTotalUsd = 60m,
            fuelLitersConsumed = (decimal?)null,
            driverHours = 2m,
            driverHourlyCost = 6m
        });
        Assert.Equal(HttpStatusCode.OK, caseAResp.StatusCode);
        var caseADoc = await caseAResp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("incomplete_unknown", caseADoc.GetProperty("costStatus").GetString());
        Assert.Equal(JsonValueKind.Null, caseADoc.GetProperty("reconciledTotalCost").ValueKind);
        Assert.True(caseADoc.GetProperty("knownPartialCost").GetDecimal() >= 0m);

        var caseAExec = caseADoc.GetProperty("reconciledExecution");
        Assert.Equal(JsonValueKind.Null, caseAExec.GetProperty("fuelCost").ValueKind);

        var missingA = caseADoc.GetProperty("missingComponents").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("fuel_consumed_unmeasured_only_purchase_provided", missingA);

        // Caso B (incidente sin cuantificar):
        // llama a reconcile-cost con IncidentKind = "breakdown", IncidentQuantified = false, IncidentExtraCost = null.
        // Verifica que devuelve costStatus == "incomplete_unknown" y reconciledTotalCost == null.
        var caseBResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{rev2Id}/reconcile-cost", new
        {
            fuelLitersConsumed = 10m,
            fuelPricePerLiter = 0.80m,
            incidentKind = "breakdown",
            incidentQuantified = false,
            incidentExtraCost = (decimal?)null
        });
        Assert.Equal(HttpStatusCode.OK, caseBResp.StatusCode);
        var caseBDoc = await caseBResp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("incomplete_unknown", caseBDoc.GetProperty("costStatus").GetString());
        Assert.Equal(JsonValueKind.Null, caseBDoc.GetProperty("reconciledTotalCost").ValueKind);

        // Caso C (conciliación sin doble conteo de mantenimiento + reparación y contra Revisión 1 congelada):
        // llama a reconcile-cost en la Revisión 2 con FuelLitersConsumed = 10m, FuelPricePerLiter = 0.80m,
        // DriverHours = 2m, DriverHourlyCost = 6m, WearAndMaintenanceCost = 15m, IncidentExtraCost = 10m,
        // MaintenanceCoversIncidentRepair = true.
        // Verifica que wearAndMaintenanceCost == 15m (no suma 15 + 10 = 25), costStatus == "reconciled",
        // reconciledTotalCost == 35.00m (8 + 12 + 15), y que originalEstimatedCost y residualError usan el costo congelado
        // de la Revisión 1, no el de la Revisión 2.
        var caseCResp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{rev2Id}/reconcile-cost", new
        {
            fuelLitersConsumed = 10m,
            fuelPricePerLiter = 0.80m,
            driverHours = 2m,
            driverHourlyCost = 6m,
            wearAndMaintenanceCost = 15m,
            incidentExtraCost = 10m,
            maintenanceCoversIncidentRepair = true
        });
        Assert.Equal(HttpStatusCode.OK, caseCResp.StatusCode);
        var caseCDoc = await caseCResp.Content.ReadFromJsonAsync<JsonElement>();

        var caseCExec = caseCDoc.GetProperty("reconciledExecution");
        Assert.Equal(15.00m, caseCExec.GetProperty("wearAndMaintenanceCost").GetDecimal());
        Assert.Equal("reconciled", caseCDoc.GetProperty("costStatus").GetString());
        Assert.Equal(35.00m, caseCDoc.GetProperty("reconciledTotalCost").GetDecimal());
        Assert.Equal(50.00m, caseCDoc.GetProperty("originalEstimatedCost").GetDecimal());
        Assert.Equal(-15.00m, caseCDoc.GetProperty("residualError").GetDecimal());
    }

    [Fact]
    public async Task Calibration_transitions_from_uncalibrated_to_calibrated_with_holdout_and_segments()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        Guid vehicleId;
        string vehicleCode;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var v = await db.Vehicles.FirstAsync(x => x.OrganizationId == DemoOrganization.Id && x.IsActive);
            vehicleId = v.Id;
            vehicleCode = v.Code;
        }

        // 1. Verifica inicialmente que GET /api/v1/route-plans/cost-calibration devuelve status == "uncalibrated" y lowerBound == null.
        var initResp = await planner.GetAsync("/api/v1/route-plans/cost-calibration");
        Assert.Equal(HttpStatusCode.OK, initResp.StatusCode);
        var initDoc = await initResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("uncalibrated", initDoc.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, initDoc.GetProperty("lowerBound").ValueKind);

        // 2. Registra y concilia 15 viajes reales (RP-CAL-01 .. RP-CAL-15) en fechas sucesivas (algunos con incidente cuantificado y otros sin incidente).
        var planIds = new List<Guid>();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var baseDate = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

            for (int i = 1; i <= 15; i++)
            {
                var plan = new RoutePlan
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = DemoOrganization.Id,
                    Code = $"RP-CAL-{i:D2}",
                    Revision = 1,
                    VehicleId = vehicleId,
                    Status = "Confirmed",
                    TotalKm = 20m + i,
                    TotalMinutes = 60m + i * 2,
                    TotalCost = 50.00m,
                    PlannedStartUtc = baseDate.AddDays(i - 1),
                    AlgorithmVersion = "order-based-routing-1.0",
                    InputJson = "{}"
                };
                db.RoutePlans.Add(plan);
                planIds.Add(plan.Id);
            }
            await db.SaveChangesAsync();
        }

        for (int i = 1; i <= 15; i++)
        {
            var planId = planIds[i - 1];
            bool hasIncident = (i % 3 == 0);

            var reconcileReq = new ReconcileRouteCostRequest(
                ActualDistanceKm: 20m + i,
                ActualDrivingMinutes: 40m + i,
                ActualServiceMinutes: 20m,
                ActualWaitMinutes: 5m,
                FuelLitersConsumed: 10m + (i * 0.5m),
                FuelPricePerLiter: 0.80m,
                DriverHours: 2m,
                DriverHourlyCost: 6m,
                WearAndMaintenanceCost: 10m,
                TollsAndDirectCost: 2m,
                IncidentExtraCost: hasIncident ? 15m : (decimal?)null,
                IncidentKind: hasIncident ? "road_closure" : null,
                IncidentQuantified: true
            );

            var resp = await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/reconcile-cost", reconcileReq);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var rDoc = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("reconciled", rDoc.GetProperty("costStatus").GetString());
        }

        // 3. Llama a GET /api/v1/route-plans/cost-calibration?estimatedCost=50&targetCoverage=0.90 y verifica que devuelve
        // status == "calibrated", sampleSize == 15, holdoutSampleSize > 0, holdoutEmpiricalCoverage != null,
        // wape != null, meanBias != null, mae != null, lowerBound != null, upperBound != null,
        // y que byVehicle y byIncidentPresence contienen los segmentos esperados.
        var calibResp = await planner.GetAsync("/api/v1/route-plans/cost-calibration?estimatedCost=50&targetCoverage=0.90");
        Assert.Equal(HttpStatusCode.OK, calibResp.StatusCode);
        var calibDoc = await calibResp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("calibrated", calibDoc.GetProperty("status").GetString());
        Assert.Equal(15, calibDoc.GetProperty("sampleSize").GetInt32());
        Assert.True(calibDoc.GetProperty("holdoutSampleSize").GetInt32() > 0);
        Assert.NotEqual(JsonValueKind.Null, calibDoc.GetProperty("holdoutEmpiricalCoverage").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, calibDoc.GetProperty("wape").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, calibDoc.GetProperty("meanBias").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, calibDoc.GetProperty("mae").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, calibDoc.GetProperty("lowerBound").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, calibDoc.GetProperty("upperBound").ValueKind);

        var byVehicle = calibDoc.GetProperty("byVehicle");
        var vehicleKeys = byVehicle.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains(vehicleCode, vehicleKeys);

        var byIncident = calibDoc.GetProperty("byIncidentPresence");
        var incidentKeys = byIncident.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("with_incidents", incidentKeys);
        Assert.Contains("without_incidents", incidentKeys);
    }
}
