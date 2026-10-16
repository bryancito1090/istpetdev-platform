using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Domain.Enums;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

using DomainLocation = IstpetDev.Domain.Entities.Location;

[Trait("Category", "Integration")]
public sealed class RoutePublisherTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
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
            Clock = new ClockOptions
            {
                Mode = ClockMode.Fixed,
                FixedDate = new DateOnly(2026, 10, 12)
            }
        });

    [Fact]
    public async Task Publish_creates_fefo_deliveries_and_active_reservations_without_touching_physical_stock()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        // 1. Configurar datos básicos
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

        var product = new Product
        {
            OrganizationId = DemoOrganization.Id,
            Sku = "MED-01",
            Name = "Amoxicilina 500mg",
            BaseUnit = "caja",
            WeightPerBaseUnit = 0.5m,
            VolumePerBaseUnit = 0.002m,
            IsActive = true
        };
        db.Products.Add(product);

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
        db.ServicePoints.Add(sp1);

        var vehicle = new Vehicle
        {
            OrganizationId = DemoOrganization.Id,
            Code = "VEH-01",
            DepotLocationId = warehouseLoc.Id,
            MaxWeight = 2000m,
            MaxVolume = 10m,
            IsActive = true
        };
        db.Vehicles.Add(vehicle);

        // Lotes FEFO: LOTE-01 expira antes (2026-11-01), LOTE-02 expira después (2027-01-01)
        var lotEarly = new Lot
        {
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOTE-01",
            ExpiresOn = new DateOnly(2026, 11, 1),
            IsBlocked = false
        };
        var lotLate = new Lot
        {
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOTE-02",
            ExpiresOn = new DateOnly(2027, 1, 1),
            IsBlocked = false
        };
        db.Lots.AddRange(lotEarly, lotLate);

        var balEarly = new StockBalance
        {
            OrganizationId = DemoOrganization.Id,
            LocationId = warehouseLoc.Id,
            LotId = lotEarly.Id,
            QuantityOnHand = 50m,
            QuantityReserved = 0m
        };
        var balLate = new StockBalance
        {
            OrganizationId = DemoOrganization.Id,
            LocationId = warehouseLoc.Id,
            LotId = lotLate.Id,
            QuantityOnHand = 100m,
            QuantityReserved = 0m
        };
        db.StockBalances.AddRange(balEarly, balLate);

        // Propuesta de ruta con 70 unidades requeridas (50 deben venir de lotEarly, 20 de lotLate)
        var plan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = "PLAN-20261012-01",
            Revision = 1,
            VehicleId = vehicle.Id,
            Status = "Proposed",
            PlannedStartUtc = new DateTime(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc),
            InputJson = JsonSerializer.Serialize(new
            {
                stops = new[]
                {
                    new
                    {
                        sequence = 1,
                        pointCode = sp1.Code,
                        loads = new[]
                        {
                            new
                            {
                                productId = product.Id,
                                sku = product.Sku,
                                quantity = 70m,
                                baseUnit = product.BaseUnit
                            }
                        }
                    }
                }
            })
        };
        db.RoutePlans.Add(plan);

        var stop = new RouteStop
        {
            OrganizationId = DemoOrganization.Id,
            RoutePlanId = plan.Id,
            ServicePointId = sp1.Id,
            SequenceNumber = 1,
            PlannedArrivalUtc = plan.PlannedStartUtc.AddMinutes(30),
            Status = "Pending"
        };
        db.RouteStops.Add(stop);

        await db.SaveChangesAsync();

        // 2. Ejecutar Publicación
        var options = DefaultOptions();
        var clock = new OperationalClock(db, options);
        var publisher = new RoutePublisher(db, clock, options);

        var pubResult1 = await publisher.PublishAsync(new PublishRequest(plan.Id));

        Assert.Equal("Published", pubResult1.Status);
        Assert.Single(pubResult1.Deliveries);
        Assert.Equal(2, pubResult1.Reservations.Count);

        // Invariante: Saldo físico intacto, QuantityReserved aumentó exactamente por el total
        var updatedBalEarly = await db.StockBalances.FirstAsync(b => b.Id == balEarly.Id);
        var updatedBalLate = await db.StockBalances.FirstAsync(b => b.Id == balLate.Id);

        Assert.Equal(50m, updatedBalEarly.QuantityOnHand);
        Assert.Equal(50m, updatedBalEarly.QuantityReserved); // Todo LOTE-01 reservado por FEFO

        Assert.Equal(100m, updatedBalLate.QuantityOnHand);
        Assert.Equal(20m, updatedBalLate.QuantityReserved); // 20 restantes de LOTE-02

        // Verificar Invariante QuantityReserved = sum(Reservas activas)
        var activeResEarly = await db.RouteStockReservations
            .Where(r => r.StockBalanceId == balEarly.Id && r.Status == RouteStockReservationStatus.Active)
            .SumAsync(r => r.Quantity);
        var activeResLate = await db.RouteStockReservations
            .Where(r => r.StockBalanceId == balLate.Id && r.Status == RouteStockReservationStatus.Active)
            .SumAsync(r => r.Quantity);

        Assert.Equal(updatedBalEarly.QuantityReserved, activeResEarly);
        Assert.Equal(updatedBalLate.QuantityReserved, activeResLate);

        // 3. Probar idempotencia: repetir publicación devuelve exactamente lo mismo sin duplicar reservas
        var pubResult2 = await publisher.PublishAsync(new PublishRequest(plan.Id));
        Assert.Equal(pubResult1.Deliveries.Count, pubResult2.Deliveries.Count);
        Assert.Equal(pubResult1.Reservations.Count, pubResult2.Reservations.Count);

        var finalBalEarly = await db.StockBalances.FirstAsync(b => b.Id == balEarly.Id);
        Assert.Equal(50m, finalBalEarly.QuantityReserved); // No duplicó reserva
    }

    [Fact]
    public async Task CompleteStop_marks_completed_once_and_is_idempotent()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

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

        var sp = new ServicePoint
        {
            OrganizationId = DemoOrganization.Id,
            Code = "HOS-01",
            Name = "Hospital Norte",
            Category = "Hospital",
            Criticality = "alta",
            Position = Gf.CreatePoint(new Coordinate(-78.5000, -0.2000)),
            IsActive = true
        };
        db.ServicePoints.Add(sp);

        var vehicle = new Vehicle
        {
            OrganizationId = DemoOrganization.Id,
            Code = "VEH-01",
            DepotLocationId = warehouseLoc.Id,
            MaxWeight = 2000m,
            MaxVolume = 10m,
            IsActive = true
        };
        db.Vehicles.Add(vehicle);

        var plan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = "PLAN-20261012-02",
            Revision = 1,
            VehicleId = vehicle.Id,
            Status = "Published"
        };
        db.RoutePlans.Add(plan);

        var plannedArr = new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc);
        var stop = new RouteStop
        {
            OrganizationId = DemoOrganization.Id,
            RoutePlanId = plan.Id,
            ServicePointId = sp.Id,
            SequenceNumber = 1,
            PlannedArrivalUtc = plannedArr,
            Status = "Pending"
        };
        db.RouteStops.Add(stop);
        await db.SaveChangesAsync();

        var options = DefaultOptions();
        var clock = new OperationalClock(db, options);
        var publisher = new RoutePublisher(db, clock, options);

        // Completar primera vez
        var completedStop1 = await publisher.CompleteStopAsync(new CompleteStopRequest(plan.Id, 1));
        Assert.Equal("Completed", completedStop1.Status);
        Assert.Equal(plannedArr, completedStop1.ActualArrivalUtc);

        // Completar segunda vez con otra fecha no retrocede ni altera
        var differentTime = plannedArr.AddHours(2);
        var completedStop2 = await publisher.CompleteStopAsync(new CompleteStopRequest(plan.Id, 1, differentTime));
        Assert.Equal("Completed", completedStop2.Status);
        Assert.Equal(plannedArr, completedStop2.ActualArrivalUtc);
    }

    // #162: una revisión no puede retirar una entrega ya en tránsito ni entregada pendiente de recibo.
    [Fact]
    public async Task Revision_cannot_remove_an_in_transit_delivery()
    {
        var connection = postgres.NewDatabase();
        await using var db = TestDb.Context(connection);

        var warehouse = new DomainLocation { Code = "B-REV2", Name = "Bodega", Kind = LocationKinds.Warehouse, IsActive = true };
        var spOld = new ServicePoint { Code = "SP-OLD2", Name = "Viejo", Criticality = "normal", IsActive = true };
        var spNew = new ServicePoint { Code = "SP-NEW2", Name = "Nuevo", Criticality = "normal", IsActive = true };
        var vehicle = new Vehicle { Code = "V-REV2", DepotLocationId = warehouse.Id, MaxWeight = 1000, IsActive = true };
        var previous = new RoutePlan { Code = "R-REV2", Revision = 1, VehicleId = vehicle.Id, Status = "Published", ProposedBy = "test", PlannedStartUtc = DateTime.UtcNow, AlgorithmVersion = "test" };
        var oldStop = new RouteStop { RoutePlanId = previous.Id, ServicePointId = spOld.Id, SequenceNumber = 1, Status = "Pending" };
        var delivery = new Delivery { Code = "D-REV2", RouteStopId = oldStop.Id, OriginLocationId = warehouse.Id, ServicePointId = spOld.Id, Status = DeliveryStates.InTransit, DispatchedAtUtc = DateTime.UtcNow };
        var current = new RoutePlan
        {
            Code = "R-REV2",
            Revision = 2,
            VehicleId = vehicle.Id,
            Status = "Proposed",
            ProposedBy = "System:IncidentReplanner",
            SupersedesRoutePlanId = previous.Id,
            PlannedStartUtc = DateTime.UtcNow,
            AlgorithmVersion = "test",
            InputJson = "{\"stops\":[]}"
        };
        var newStop = new RouteStop { RoutePlanId = current.Id, ServicePointId = spNew.Id, SequenceNumber = 1, Status = "Pending" };
        db.AddRange(warehouse, spOld, spNew, vehicle, previous, oldStop, delivery, current, newStop);
        await db.SaveChangesAsync();

        var options = DefaultOptions();
        var publisher = new RoutePublisher(db, new OperationalClock(db, options), options);

        var error = await Assert.ThrowsAsync<ReplanConflictException>(() => publisher.PublishAsync(new PublishRequest(current.Id)));
        Assert.Contains("despachada", error.Message);
        Assert.Equal("Proposed", (await db.RoutePlans.SingleAsync(p => p.Id == current.Id)).Status);
        Assert.Equal(DeliveryStates.InTransit, (await db.Deliveries.SingleAsync(d => d.Id == delivery.Id)).Status);
    }
}
