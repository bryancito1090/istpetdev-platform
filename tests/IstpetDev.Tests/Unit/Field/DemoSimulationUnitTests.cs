using System.Security.Claims;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Demo;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Application.Imports;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Unit.Field;

[Trait("Category", "Unit")]
public sealed class DemoSimulationUnitTests
{
    private sealed class FakeDemoResetService : IDemoResetService
    {
        public Task<ImportReport> ResetAsync(CancellationToken cancellationToken = default, Guid? actorAccountId = null) =>
            Task.FromResult(new ImportReport(Guid.NewGuid(), "demo", false) { Applied = true });

        public Task<ImportReport> ResetAsync(string? fixture, CancellationToken cancellationToken = default, Guid? actorAccountId = null) =>
            Task.FromResult(new ImportReport(Guid.NewGuid(), "demo", false) { Applied = true });
    }

    private sealed class FakeSimulationFieldService : IFieldService
    {
        public List<DemoAdvanceRequest> RecordedAdvances { get; } = new();

        public Task<DemoAdvanceResponse> AdvanceDemoAsync(Guid routePlanId, DemoAdvanceRequest request, Guid actorAccountId, CancellationToken ct = default)
        {
            RecordedAdvances.Add(request);
            var completed = new List<CompletedStopDto>();
            if (request.Mode == "complete" || request.Stops >= 2)
            {
                completed.Add(new CompletedStopDto(1, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, request.WithIncidents, request.WithIncidents ? 2m : null, request.WithIncidents ? "Daño" : null));
                completed.Add(new CompletedStopDto(2, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
            }
            else if (request.Mode == "half" || request.Stops == 1)
            {
                completed.Add(new CompletedStopDto(1, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, request.WithIncidents, request.WithIncidents ? 1m : null, request.WithIncidents ? "PrecintoViolado" : null));
            }

            return Task.FromResult(new DemoAdvanceResponse(
                completed,
                request.Mode == "next" ? "Preparación en bodega" : "Completada",
                request.Mode == "next" ? "Despacho a vehículo" : "Ruta finalizada",
                2,
                completed.Count,
                completed.Count == 2 ? "Completed" : completed.Count == 1 ? "InProgress" : "Published"));
        }

        public Task<IReadOnlyList<TodayRoutePlanDto>> GetTodayRoutePlansAsync(CancellationToken ct = default)
        {
            var stops = new List<TodayRouteStopDto>
            {
                new(Guid.NewGuid(), 1, "Delivery", "Hospital Laura", "Pending", DateTime.UtcNow, null),
                new(Guid.NewGuid(), 2, "Delivery", "Centro Salud Centro", "Pending", DateTime.UtcNow, null)
            };

            var plan = new TodayRoutePlanDto(
                Guid.NewGuid(),
                "RUT-TEST-001",
                "VEH-01",
                "Carlos Conductor",
                Guid.NewGuid(),
                "Publicada",
                2,
                0,
                "Preparación en bodega",
                DateTime.UtcNow,
                stops);

            return Task.FromResult<IReadOnlyList<TodayRoutePlanDto>>(new[] { plan });
        }

        public Task<HandlingUnitResponse> CreateHandlingUnitAsync(Guid deliveryId, CreateHandlingUnitRequest request, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<DispatchResponse> DispatchAsync(Guid deliveryId, DispatchRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<CustodyTransitionResponse> PickupAsync(Guid deliveryId, CustodyTransitionRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<CustodyTransitionResponse> DeliverAsync(Guid deliveryId, CustodyTransitionRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<MobileAssignmentResponse> GetMobileAssignmentsAsync(Guid driverAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<DeliveryListResponse> GetDeliveriesAsync(Guid? routePlanId, string? status, int limit, string? cursor, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<DeliveryDetailResponse> GetDeliveryDetailAsync(Guid deliveryId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PublicTraceUnitDto> GetPublicTraceAsync(Guid unitId, string? traceToken, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<HandlingUnitTraceResponse> GetOperativeTraceAsync(Guid unitId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RevokePublicTokenAsync(Guid unitId, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<EvidenceUploadResponse> CreateEvidenceUploadAsync(Guid receiptId, EvidenceUploadRequest request, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<EvidenceConfirmResponse> ConfirmEvidenceAsync(Guid evidenceId, EvidenceConfirmRequest request, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ReceiverUnitResponse> GetReceiverUnitAsync(Guid unitId, string? traceToken, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ReceiptResponse> CreateReceiverReceiptAsync(Guid unitId, string? traceToken, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default) => throw new NotImplementedException();
        public RejectionReasonsCatalogResponse GetRejectionReasonsCatalog() => throw new NotImplementedException();
        public Task<ReturnGuideDto> GenerateReturnGuideAsync(Guid routePlanId, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ReturnGuideDto> GetReturnGuideAsync(string guideNumber, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ReceiveReturnGuideResponse> ReceiveReturnGuideAsync(ReceiveReturnGuideRequest request, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static DemoController CreateController(
        bool demoEnabled,
        bool fieldSimEnabled,
        FakeSimulationFieldService fieldService,
        string role = Roles.Planificador)
    {
        var demoOpts = Options.Create(new DemoOptions { Enabled = demoEnabled });
        var simOpts = Options.Create(new FieldSimulationOptions { Enabled = fieldSimEnabled });
        var controller = new DemoController(
            new FakeDemoResetService(),
            fieldService,
            null!, // db not used by simulation endpoints
            null!, // evaluator not used
            demoOpts,
            simOpts);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("account_id", Guid.NewGuid().ToString())
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task When_FieldSimulation_Is_Disabled_Endpoints_Return_404()
    {
        var field = new FakeSimulationFieldService();
        var controller = CreateController(demoEnabled: true, fieldSimEnabled: false, field);

        var todayResult = await controller.GetTodayRoutePlans();
        Assert.IsType<NotFoundResult>(todayResult);

        var advanceResult = await controller.Advance(Guid.NewGuid(), new DemoAdvanceRequest(Guid.NewGuid(), Mode: "complete"), CancellationToken.None);
        Assert.IsType<NotFoundResult>(advanceResult);
    }

    [Fact]
    public async Task When_FieldSimulation_Is_Enabled_And_Demo_Is_Disabled_Today_And_Advance_Succeed_While_Reset_Is_404()
    {
        var field = new FakeSimulationFieldService();
        var controller = CreateController(demoEnabled: false, fieldSimEnabled: true, field);

        // 1. Reset da 404 porque Demo:Enabled es false
        var resetResult = await controller.Reset();
        Assert.IsType<NotFoundResult>(resetResult);

        // 2. Today da 200 porque FieldSimulation:Enabled es true
        var todayResult = await controller.GetTodayRoutePlans();
        var okToday = Assert.IsType<OkObjectResult>(todayResult);
        var routes = Assert.IsAssignableFrom<IReadOnlyList<TodayRoutePlanDto>>(okToday.Value);
        Assert.Single(routes);
        Assert.Equal("RUT-TEST-001", routes[0].Code);
        Assert.Equal("VEH-01", routes[0].VehicleCode);

        // 3. Advance da 200 porque FieldSimulation:Enabled es true
        var planId = routes[0].RoutePlanId;
        var advResult = await controller.Advance(planId, new DemoAdvanceRequest(Guid.NewGuid(), Mode: "complete", WithIncidents: true), CancellationToken.None);
        var okAdv = Assert.IsType<OkObjectResult>(advResult);
        var advResponse = Assert.IsType<DemoAdvanceResponse>(okAdv.Value);
        Assert.Equal("Completed", advResponse.Status);
        Assert.Equal(2, advResponse.CompletedStops.Count);
        Assert.True(advResponse.CompletedStops[0].HadIncidents);
    }

    [Theory]
    [InlineData(Roles.Planificador)]
    [InlineData(Roles.Gerencia)]
    [InlineData(Roles.Bodega)]
    [InlineData(Roles.Conductor)]
    public async Task Allowed_Roles_Can_Access_Simulation_Endpoints(string role)
    {
        var field = new FakeSimulationFieldService();
        var controller = CreateController(demoEnabled: false, fieldSimEnabled: true, field, role: role);

        var todayResult = await controller.GetTodayRoutePlans();
        Assert.IsType<OkObjectResult>(todayResult);

        var advResult = await controller.Advance(Guid.NewGuid(), new DemoAdvanceRequest(Guid.NewGuid(), Mode: "half"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(advResult);
    }

    [Fact]
    public async Task Half_Mode_Returns_InProgress()
    {
        var field = new FakeSimulationFieldService();
        var controller = CreateController(demoEnabled: false, fieldSimEnabled: true, field);

        var advResult = await controller.Advance(Guid.NewGuid(), new DemoAdvanceRequest(Guid.NewGuid(), Mode: "half"), CancellationToken.None);
        var okAdv = Assert.IsType<OkObjectResult>(advResult);
        var resp = Assert.IsType<DemoAdvanceResponse>(okAdv.Value);
        Assert.Equal("InProgress", resp.Status);
        Assert.Single(resp.CompletedStops);
    }

    [Fact]
    public async Task Next_Mode_Advances_Checkpoints()
    {
        var field = new FakeSimulationFieldService();
        var controller = CreateController(demoEnabled: false, fieldSimEnabled: true, field);

        var advResult = await controller.Advance(Guid.NewGuid(), new DemoAdvanceRequest(Guid.NewGuid(), Mode: "next"), CancellationToken.None);
        var okAdv = Assert.IsType<OkObjectResult>(advResult);
        var resp = Assert.IsType<DemoAdvanceResponse>(okAdv.Value);
        Assert.Equal("Preparación en bodega", resp.CheckpointCompleted);
        Assert.Equal("Despacho a vehículo", resp.NextCheckpoint);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(ApplicationDbContext db, RoutePlan plan, Vehicle vehicle, ServicePoint point1, ServicePoint point2, Account driver)> SeedTestRouteAsync()
    {
        var db = CreateDb();
        var driver = new Account { Subject = "test-driver-" + Guid.NewGuid(), DisplayName = "Conductor Demo", Role = Roles.Conductor };
        var planner = new Account { Subject = "test-planner-" + Guid.NewGuid(), DisplayName = "Planificador Demo", Role = Roles.Planificador };
        db.Accounts.AddRange(driver, planner);

        var warehouse = new Location { Code = "BOD-CENTRAL", Name = "Bodega Central", Kind = LocationKinds.Warehouse, IsActive = true };
        var vehicle = new Vehicle { Code = "VEH-01", DepotLocationId = warehouse.Id, MaxWeight = 1000, IsActive = true };
        var vehicleLoc = new Location { Code = "VEH-01-LOC", Name = "Vehículo 01", Kind = LocationKinds.Vehicle, VehicleId = vehicle.Id, IsActive = true };

        var point1 = new ServicePoint { Code = "P-01", Name = "Hospital Central", Criticality = "alta", IsActive = true };
        var loc1 = new Location { Code = "LOC-P-01", Name = "Loc P-01", Kind = LocationKinds.ServicePoint, ServicePointId = point1.Id, IsActive = true };

        var point2 = new ServicePoint { Code = "P-02", Name = "Centro de Salud Norte", Criticality = "normal", IsActive = true };
        var loc2 = new Location { Code = "LOC-P-02", Name = "Loc P-02", Kind = LocationKinds.ServicePoint, ServicePointId = point2.Id, IsActive = true };

        var product = new Product { Sku = "SKU-AMONIO-01", Name = "Amonio Cuaternario", BaseUnit = "L", IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var lot = new Lot { ProductId = product.Id, Code = "LOTE-2026-01" };
        db.Lots.Add(lot);
        await db.SaveChangesAsync();

        var plan = new RoutePlan
        {
            Code = "RP-TODAY-001",
            Revision = 1,
            Status = "Published",
            VehicleId = vehicle.Id,
            PlannedStartUtc = DateTime.UtcNow,
            AlgorithmVersion = "1.0",
            ProposedBy = "test"
        };
        db.RoutePlans.Add(plan);
        await db.SaveChangesAsync();

        var stop1 = new RouteStop
        {
            RoutePlanId = plan.Id,
            ServicePointId = point1.Id,
            SequenceNumber = 1,
            Status = "Pending",
            PlannedArrivalUtc = DateTime.UtcNow.AddHours(1)
        };

        var stop2 = new RouteStop
        {
            RoutePlanId = plan.Id,
            ServicePointId = point2.Id,
            SequenceNumber = 2,
            Status = "Pending",
            PlannedArrivalUtc = DateTime.UtcNow.AddHours(2)
        };
        db.RouteStops.AddRange(stop1, stop2);
        await db.SaveChangesAsync();

        var del1 = new Delivery
        {
            Code = "DEL-01",
            RouteStopId = stop1.Id,
            OriginLocationId = warehouse.Id,
            ServicePointId = point1.Id,
            DestinationLocationId = loc1.Id,
            Status = "Draft",
            AssignedAccountId = driver.Id
        };

        var del2 = new Delivery
        {
            Code = "DEL-02",
            RouteStopId = stop2.Id,
            OriginLocationId = warehouse.Id,
            ServicePointId = point2.Id,
            DestinationLocationId = loc2.Id,
            Status = "Draft",
            AssignedAccountId = driver.Id
        };
        db.Deliveries.AddRange(del1, del2);
        await db.SaveChangesAsync();

        var line1 = new DeliveryLine
        {
            DeliveryId = del1.Id,
            LotId = lot.Id,
            PlannedQuantity = 10,
            DispatchedQuantity = 0,
            AcceptedQuantity = 0,
            RejectedQuantity = 0
        };

        var line2 = new DeliveryLine
        {
            DeliveryId = del2.Id,
            LotId = lot.Id,
            PlannedQuantity = 5,
            DispatchedQuantity = 0,
            AcceptedQuantity = 0,
            RejectedQuantity = 0
        };

        var stock = new StockBalance
        {
            LocationId = warehouse.Id,
            LotId = lot.Id,
            QuantityOnHand = 1000m,
            QuantityReserved = 0m
        };

        db.Locations.AddRange(warehouse, vehicleLoc, loc1, loc2);
        db.Vehicles.Add(vehicle);
        db.ServicePoints.AddRange(point1, point2);
        db.StockBalances.Add(stock);
        db.DeliveryLines.AddRange(line1, line2);

        await db.SaveChangesAsync();
        return (db, plan, vehicle, point1, point2, driver);
    }

    private static FieldService CreateRealFieldService(ApplicationDbContext db)
    {
        return new FieldService(
            db,
            new ConfigurationBuilder().Build(),
            NullLogger<FieldService>.Instance,
            Options.Create(new LogisticsOptions()));
    }

    [Fact]
    public async Task Real_FieldService_GetTodayRoutePlans_Lists_Published_Routes()
    {
        var (db, plan, vehicle, point1, _, _) = await SeedTestRouteAsync();
        var service = CreateRealFieldService(db);

        var todayPlans = await service.GetTodayRoutePlansAsync();
        Assert.NotEmpty(todayPlans);
        var found = todayPlans.FirstOrDefault(p => p.RoutePlanId == plan.Id);
        Assert.NotNull(found);
        Assert.Equal("RP-TODAY-001", found.Code);
        Assert.Equal("VEH-01", found.VehicleCode);
        Assert.Equal(2, found.TotalStops);
        Assert.Equal(0, found.CompletedStops);
        Assert.Equal("Preparación en bodega", found.NextCheckpoint);
    }

    [Fact]
    public async Task Real_FieldService_AdvanceDemo_Complete_Mode_Completes_All_Stops()
    {
        var (db, plan, _, _, _, driver) = await SeedTestRouteAsync();
        var service = CreateRealFieldService(db);

        var opId = Guid.NewGuid();
        var req = new DemoAdvanceRequest(opId, Mode: "complete", WithIncidents: false);
        var resp = await service.AdvanceDemoAsync(plan.Id, req, driver.Id);

        Assert.Equal(2, resp.CompletedStopsCount);
        Assert.Equal("Completed", resp.Status);
        Assert.Equal("Ruta finalizada", resp.NextCheckpoint);

        // Verificar que las paradas queden como Completed en base de datos
        var stops = await db.RouteStops.Where(s => s.RoutePlanId == plan.Id).ToListAsync();
        Assert.All(stops, s => Assert.Equal("Completed", s.Status));

        // Verificar que las entregas queden como Received
        var stopIds = stops.Select(s => s.Id).ToList();
        var dels = await db.Deliveries.Where(d => d.RouteStopId != null && stopIds.Contains(d.RouteStopId.Value)).ToListAsync();
        Assert.All(dels, d => Assert.Equal("Received", d.Status));
    }

    [Fact]
    public async Task Real_FieldService_AdvanceDemo_Half_Mode_Completes_Ceil_Half_Stops()
    {
        var (db, plan, _, _, _, driver) = await SeedTestRouteAsync();
        var service = CreateRealFieldService(db);

        var opId = Guid.NewGuid();
        var req = new DemoAdvanceRequest(opId, Mode: "half", WithIncidents: false);
        var resp = await service.AdvanceDemoAsync(plan.Id, req, driver.Id);

        // De 2 paradas, la mitad es 1
        Assert.Equal(1, resp.CompletedStopsCount);
        Assert.Equal("InProgress", resp.Status);

        var stops = await db.RouteStops.Where(s => s.RoutePlanId == plan.Id).OrderBy(s => s.SequenceNumber).ToListAsync();
        Assert.Equal("Completed", stops[0].Status);
        Assert.Equal("Pending", stops[1].Status);
    }

    [Fact]
    public async Task Real_FieldService_AdvanceDemo_Next_Mode_Steps_Through_Checkpoints()
    {
        var (db, plan, _, _, _, driver) = await SeedTestRouteAsync();
        var service = CreateRealFieldService(db);

        // Paso 1: Preparación en bodega
        var req1 = new DemoAdvanceRequest(Guid.NewGuid(), Mode: "next");
        var resp1 = await service.AdvanceDemoAsync(plan.Id, req1, driver.Id);
        Assert.Equal("Preparación en bodega", resp1.CheckpointCompleted);
        Assert.Equal("Despacho a vehículo", resp1.NextCheckpoint);

        // Paso 2: Despacho a vehículo
        var req2 = new DemoAdvanceRequest(Guid.NewGuid(), Mode: "next");
        var resp2 = await service.AdvanceDemoAsync(plan.Id, req2, driver.Id);
        Assert.Equal("Despacho a vehículo", resp2.CheckpointCompleted);
        Assert.Contains("Parada 1", resp2.NextCheckpoint);

        // Paso 3: Parada 1
        var req3 = new DemoAdvanceRequest(Guid.NewGuid(), Mode: "next");
        var resp3 = await service.AdvanceDemoAsync(plan.Id, req3, driver.Id);
        Assert.Contains("Parada 1", resp3.CheckpointCompleted);
        Assert.Contains("Parada 2", resp3.NextCheckpoint);

        // Paso 4: Parada 2
        var req4 = new DemoAdvanceRequest(Guid.NewGuid(), Mode: "next");
        var resp4 = await service.AdvanceDemoAsync(plan.Id, req4, driver.Id);
        Assert.Contains("Parada 2", resp4.CheckpointCompleted);
        Assert.Equal("Ruta finalizada", resp4.NextCheckpoint);
        Assert.Equal("Completed", resp4.Status);
    }

    [Fact]
    public async Task Real_FieldService_AdvanceDemo_Is_Idempotent_With_Same_OperationId()
    {
        var (db, plan, _, _, _, driver) = await SeedTestRouteAsync();
        var service = CreateRealFieldService(db);

        var opId = Guid.NewGuid();
        var req = new DemoAdvanceRequest(opId, Mode: "complete", WithIncidents: false);

        var resp1 = await service.AdvanceDemoAsync(plan.Id, req, driver.Id);
        var receiptsCount1 = await db.Receipts.CountAsync();

        // Repetir la misma operación con el mismo OperationId
        var resp2 = await service.AdvanceDemoAsync(plan.Id, req, driver.Id);
        var receiptsCount2 = await db.Receipts.CountAsync();

        Assert.Equal(resp1.CompletedStopsCount, resp2.CompletedStopsCount);
        Assert.Equal(receiptsCount1, receiptsCount2);
    }

    [Fact]
    public async Task Real_FieldService_AdvanceDemo_With_Incidents_Is_Reproducible()
    {
        var (db1, plan1, _, _, _, driver1) = await SeedTestRouteAsync();
        var (db2, plan2, _, _, _, driver2) = await SeedTestRouteAsync();

        var service1 = CreateRealFieldService(db1);
        var service2 = CreateRealFieldService(db2);

        var fixedOpId = Guid.Parse("0180b280-0000-7000-8000-000000000001");
        var req1 = new DemoAdvanceRequest(fixedOpId, Mode: "complete", WithIncidents: true, NoveltyRate: 1.0);
        var req2 = new DemoAdvanceRequest(fixedOpId, Mode: "complete", WithIncidents: true, NoveltyRate: 1.0);

        var resp1 = await service1.AdvanceDemoAsync(plan1.Id, req1, driver1.Id);
        var resp2 = await service2.AdvanceDemoAsync(plan2.Id, req2, driver2.Id);

        Assert.Equal(resp1.CompletedStops.Count, resp2.CompletedStops.Count);
        for (int i = 0; i < resp1.CompletedStops.Count; i++)
        {
            Assert.Equal(resp1.CompletedStops[i].HadIncidents, resp2.CompletedStops[i].HadIncidents);
            Assert.Equal(resp1.CompletedStops[i].QuantityRejected, resp2.CompletedStops[i].QuantityRejected);
            Assert.Equal(resp1.CompletedStops[i].RejectionReason, resp2.CompletedStops[i].RejectionReason);
        }
    }
}
