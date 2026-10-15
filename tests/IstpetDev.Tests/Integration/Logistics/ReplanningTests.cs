using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Application.Logistics.Policy;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Domain.Enums;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

using DomainLocation = IstpetDev.Domain.Entities.Location;

[Trait("Category", "Integration")]
public sealed class ReplanningTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static readonly GeometryFactory Gf = new(new PrecisionModel(), 4326);

    private static IOptions<LogisticsOptions> DefaultOptions() =>
        Options.Create(new LogisticsOptions
        {
            DemandWindowDays = 14,
            DefaultLeadTimeDays = 2m,
            SafetyMarginDays = 3m,
            ConsolidationExtraDays = 3m,
            TargetCoverageDays = 10m,
            SafetyStockUnits = 0m,
            StaleStockDays = 7,
            WarehouseSafetyStockPct = 10m,
            DefaultCostPerKm = 0.50m,
            CostPerHour = 10.00m,
            WorkdayHours = 8,
            MaxDetourMinutes = 30,
            DefaultServiceMinutes = 15,
            DefaultWindowStart = "08:00",
            DefaultWindowEnd = "17:00",
            Clock = new ClockOptions
            {
                Mode = ClockMode.Fixed,
                FixedDate = new DateOnly(2026, 10, 12)
            },
            Policy = new PolicyOptions
            {
                Mode = "AutoWithLimits",
                MaxCostIncreasePct = 15m,
                MaxDelayMinutes = 60m,
                AutoPublishInitialPlans = false
            }
        });

    [Fact]
    public async Task Replan_preserves_completed_stops_creates_incremented_revision_and_evaluates_policy()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        // 1. Configurar datos
        var warehouseLoc = new DomainLocation
        {
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-CENTRAL",
            Name = "Bodega Central",
            Kind = LocationKinds.Warehouse,
            Position = Gf.CreatePoint(new Coordinate(-78.5243, -0.2295)),
            IsActive = true
        };
        db.Locations.Add(warehouseLoc);

        var sp1 = new ServicePoint
        {
            OrganizationId = DemoOrganization.Id,
            Code = "HOS-01",
            Name = "Hospital Norte",
            Category = "Hospital",
            Criticality = "alta",
            Position = Gf.CreatePoint(new Coordinate(-78.5000, -0.2000)),
            IsActive = true
        };
        var sp2 = new ServicePoint
        {
            OrganizationId = DemoOrganization.Id,
            Code = "HOS-02",
            Name = "Hospital Sur",
            Category = "Hospital",
            Criticality = "alta",
            Position = Gf.CreatePoint(new Coordinate(-78.5100, -0.2100)),
            IsActive = true
        };
        db.ServicePoints.AddRange(sp1, sp2);

        var vehicle = new Vehicle
        {
            OrganizationId = DemoOrganization.Id,
            Code = "VEH-01",
            DepotLocationId = warehouseLoc.Id,
            MaxWeight = 2000m,
            MaxVolume = 10m,
            CostPerKm = 0.5m,
            IsActive = true
        };
        db.Vehicles.Add(vehicle);

        var product = new Product
        {
            OrganizationId = DemoOrganization.Id,
            Sku = "MED-01",
            Name = "Paracetamol",
            BaseUnit = "caja",
            WeightPerBaseUnit = 0.5m,
            VolumePerBaseUnit = 0.002m,
            IsActive = true
        };
        db.Products.Add(product);

        var lot = new Lot
        {
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOTE-001",
            ExpiresOn = new DateOnly(2027, 1, 1),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        var balance = new StockBalance
        {
            OrganizationId = DemoOrganization.Id,
            LocationId = warehouseLoc.Id,
            LotId = lot.Id,
            QuantityOnHand = 1000m,
            QuantityReserved = 0m
        };
        db.StockBalances.Add(balance);

        // Plan inicial publicado con 2 paradas
        var plan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = "PLAN-20261012-01",
            Revision = 1,
            VehicleId = vehicle.Id,
            Status = "Published",
            TotalKm = 20m,
            TotalMinutes = 45m,
            TotalCost = 50m,
            PlannedStartUtc = new DateTime(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc),
            InputJson = JsonSerializer.Serialize(new
            {
                stops = new[]
                {
                    new { sequence = 1, pointCode = sp1.Code, loads = new[] { new { productId = product.Id, sku = product.Sku, quantity = 10m, baseUnit = "U" } } },
                    new { sequence = 2, pointCode = sp2.Code, loads = new[] { new { productId = product.Id, sku = product.Sku, quantity = 15m, baseUnit = "U" } } }
                },
                legs = new[]
                {
                    new { from = new { lat = -0.2295m, lon = -78.5243m }, to = new { lat = -0.2000m, lon = -78.5000m }, km = 10m, minutes = 20m },
                    new { from = new { lat = -0.2000m, lon = -78.5000m }, to = new { lat = -0.2100m, lon = -78.5100m }, km = 5m, minutes = 15m },
                    new { from = new { lat = -0.2100m, lon = -78.5100m }, to = new { lat = -0.2295m, lon = -78.5243m }, km = 5m, minutes = 10m }
                }
            })
        };
        db.RoutePlans.Add(plan);

        // Parada 1 Completada, Parada 2 Pendiente
        var stop1 = new RouteStop
        {
            OrganizationId = DemoOrganization.Id,
            RoutePlanId = plan.Id,
            ServicePointId = sp1.Id,
            SequenceNumber = 1,
            PlannedArrivalUtc = plan.PlannedStartUtc.AddMinutes(20),
            ActualArrivalUtc = plan.PlannedStartUtc.AddMinutes(22),
            Status = "Completed"
        };
        var stop2 = new RouteStop
        {
            OrganizationId = DemoOrganization.Id,
            RoutePlanId = plan.Id,
            ServicePointId = sp2.Id,
            SequenceNumber = 2,
            PlannedArrivalUtc = plan.PlannedStartUtc.AddMinutes(40),
            Status = "Pending"
        };
        db.RouteStops.AddRange(stop1, stop2);

        // Crear entrega asociada a la parada para verificar publicación previa
        var delivery = new Delivery
        {
            OrganizationId = DemoOrganization.Id,
            Code = "DEL-01",
            RouteStopId = stop2.Id,
            OriginLocationId = warehouseLoc.Id,
            ServicePointId = sp2.Id,
            Status = nameof(DeliveryStatus.Draft)
        };
        db.Deliveries.Add(delivery);

        var delLine = new DeliveryLine
        {
            OrganizationId = DemoOrganization.Id,
            DeliveryId = delivery.Id,
            LotId = lot.Id,
            PlannedQuantity = 15m
        };
        db.DeliveryLines.Add(delLine);

        var reservation = new RouteStockReservation
        {
            OrganizationId = DemoOrganization.Id,
            RoutePlanId = plan.Id,
            DeliveryId = delivery.Id,
            DeliveryLineId = delLine.Id,
            LotId = lot.Id,
            StockBalanceId = balance.Id,
            Quantity = 15m,
            Status = RouteStockReservationStatus.Active
        };
        db.RouteStockReservations.Add(reservation);
        balance.QuantityReserved = 15m;

        // Incidente: ROAD_CLOSURE en la ruta que afecta el tramo hacia sp2
        var incident = new Incident
        {
            OrganizationId = DemoOrganization.Id,
            Kind = "ROAD_CLOSURE",
            Severity = "ALTA",
            Description = "Derrumbe en tramo norte",
            Position = Gf.CreatePoint(new Coordinate(-78.5050, -0.2050)),
            RadiusKm = 1.0m,
            IsSynthetic = true,
            Status = "Active",
            OccurredAtUtc = DateTime.UtcNow
        };
        db.Incidents.Add(incident);

        await db.SaveChangesAsync();

        // 2. Ejecutar Replanificación
        var options = DefaultOptions();
        var clock = new OperationalClock(db, options);
        var distProvider = new DistanceProvider(db, new HttpClient(), options, NullLogger<DistanceProvider>.Instance);
        var riskCalc = new RiskCalculator(db, clock, options);
        var allocator = new ReplenishmentAllocator();
        var routePlanner = new RoutePlanner(db, clock, riskCalc, allocator, distProvider, options);
        var routePublisher = new RoutePublisher(db, clock, options);
        var autonomyPolicy = new AutonomyPolicy(options);
        var autonomyState = new AutonomyState(options);
        var explanationTemplates = new ExplanationTemplates();
        var evaluator = new IncidentEvaluator(db);

        var replanner = new Replanner(
            db, clock, evaluator, routePlanner, autonomyPolicy, autonomyState,
            routePublisher, explanationTemplates, options);

        var result = await replanner.ReplanAsync(plan.Id, incident.Id);

        // Verificaciones
        Assert.Equal(plan.Id, result.PreviousRoutePlanId);
        Assert.Equal(1, result.PreviousRevision);
        Assert.Equal(2, result.ProposedRevision);
        Assert.NotNull(result.Diff);
        Assert.NotNull(result.AgentDecisionId);

        var recordedDecision = await db.AgentDecisions.FirstAsync(d => d.Id == result.AgentDecisionId);
        Assert.Equal(result.ProposedRoutePlanId, recordedDecision.CorrelationId);

        // Verificar que la nueva revisión existe en BD
        var newPlan = await db.RoutePlans.FirstAsync(p => p.Id == result.ProposedRoutePlanId);
        Assert.Equal(2, newPlan.Revision);
        Assert.Equal(plan.Id, newPlan.SupersedesRoutePlanId);

        // Parada completada se conserva intacta
        var newStops = await db.RouteStops
            .Where(s => s.RoutePlanId == newPlan.Id)
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync();
        Assert.NotEmpty(newStops);
        Assert.Equal("Completed", newStops[0].Status);

        // 3. Probar reversión idempotente
        var revertRequest = new RevertPlanRequest(newPlan.Id);
        var revertedPlan = await replanner.RevertPlanAsync(revertRequest);

        Assert.Equal(3, revertedPlan.Revision); // Tercera revisión
        Assert.Equal(newPlan.Id, revertedPlan.SupersedesRoutePlanId);
        Assert.Equal("Published", revertedPlan.Status);

        // Verificar invariante: reservas de la revisión saliente (newPlan) quedaron Released
        var releasedReservations = await db.RouteStockReservations
            .Where(r => r.RoutePlanId == newPlan.Id)
            .ToListAsync();
        Assert.All(releasedReservations, r => Assert.Equal(RouteStockReservationStatus.Released, r.Status));

        // Y la tercera revisión restaurada tiene sus reservas activas
        var restoredReservations = await db.RouteStockReservations
            .Where(r => r.RoutePlanId == revertedPlan.Id)
            .ToListAsync();
        Assert.NotEmpty(restoredReservations);
        Assert.All(restoredReservations, r => Assert.Equal(RouteStockReservationStatus.Active, r.Status));

        // Repetir reversión devuelve la misma tercera revisión (idempotente)
        var repeatedRevert = await replanner.RevertPlanAsync(revertRequest);
        Assert.Equal(revertedPlan.Id, repeatedRevert.Id);
    }
}
