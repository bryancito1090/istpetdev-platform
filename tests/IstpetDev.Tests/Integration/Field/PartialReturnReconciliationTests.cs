using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ServicePoint = IstpetDev.Domain.Entities.ServicePoint;

namespace IstpetDev.Tests.Integration.Field;

// #287: un retorno parcial mueve solo lo rechazado y conserva una demanda por destino.
[Trait("Category", "Integration")]
public sealed class PartialReturnReconciliationTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private sealed record Seeded(
        Guid PlanId, string GuideNumber, Guid WarehouseId, Guid VehicleLocationId,
        Guid PointAId, Guid PointBId, Guid LotAId, Guid LotBId,
        Guid DeliveryAId, Guid DeliveryBId, Guid UnitAId, Guid UnitBId);

    private static async Task<Seeded> SeedAsync(ApiFactory api)
    {
        var a = await FieldSeed.DispatchedDeliveryAsync(api, 10m, "RET287A", withHandlingUnit: true);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var vehicleLocation = await db.Locations.SingleAsync(l => l.Id == a.VehicleLocationId);
        var deliveryA = await db.Deliveries.SingleAsync(d => d.Id == a.DeliveryId);
        var pointAId = deliveryA.ServicePointId;
        var now = DateTime.UtcNow;

        var productB = new Product
        {
            OrganizationId = DemoOrganization.Id, Sku = "SKU-RET287B", Name = "Producto de retorno B",
            BaseUnit = "L", WeightPerBaseUnit = 1m, VolumePerBaseUnit = 0.001m, IsActive = true
        };
        var lotB = new Lot { OrganizationId = DemoOrganization.Id, ProductId = productB.Id, Code = "LOTE-RET287B" };
        var pointB = new ServicePoint
        {
            OrganizationId = DemoOrganization.Id, Code = "P-RET287B", Name = "Punto crítico de retorno",
            Criticality = "Critical", IsActive = true
        };
        var pointLocationB = new Location
        {
            OrganizationId = DemoOrganization.Id, Code = pointB.Code, Name = pointB.Name,
            Kind = LocationKinds.ServicePoint, ServicePointId = pointB.Id, IsActive = true
        };
        var deliveryB = new Delivery
        {
            OrganizationId = DemoOrganization.Id, Code = "D-RET287B", OriginLocationId = deliveryA.OriginLocationId,
            ServicePointId = pointB.Id, Status = DeliveryStates.Dispatched, AssignedAccountId = a.DriverAccountId,
            DispatchedAtUtc = now
        };
        var lineB = new DeliveryLine
        {
            OrganizationId = DemoOrganization.Id, DeliveryId = deliveryB.Id, LotId = lotB.Id,
            PlannedQuantity = 8m, DispatchedQuantity = 8m
        };
        var unitB = new HandlingUnit
        {
            OrganizationId = DemoOrganization.Id, Code = "HU-RET287B", DeliveryId = deliveryB.Id,
            LocationId = vehicleLocation.Id, Status = HandlingUnitStates.Dispatched
        };
        var plan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id, Code = "R-RET287", Revision = 1,
            VehicleId = vehicleLocation.VehicleId!.Value, Status = "Published", PlannedStartUtc = now,
            AlgorithmVersion = "test", ProposedBy = "test"
        };
        var stopA = new RouteStop
        {
            OrganizationId = DemoOrganization.Id, RoutePlanId = plan.Id, ServicePointId = pointAId, SequenceNumber = 1
        };
        var stopB = new RouteStop
        {
            OrganizationId = DemoOrganization.Id, RoutePlanId = plan.Id, ServicePointId = pointB.Id, SequenceNumber = 2
        };
        deliveryA.RouteStopId = stopA.Id;
        deliveryB.RouteStopId = stopB.Id;

        var receiptA = new Receipt
        {
            OrganizationId = DemoOrganization.Id, DeliveryId = deliveryA.Id, ActorAccountId = a.DriverAccountId,
            OperationId = Guid.NewGuid(), ReceivedAtServer = now
        };
        var receiptB = new Receipt
        {
            OrganizationId = DemoOrganization.Id, DeliveryId = deliveryB.Id, ActorAccountId = a.DriverAccountId,
            OperationId = Guid.NewGuid(), ReceivedAtServer = now
        };

        db.AddRange(productB, lotB, pointB, pointLocationB, deliveryB, lineB, unitB,
            new HandlingUnitLine
            {
                OrganizationId = DemoOrganization.Id, HandlingUnitId = unitB.Id,
                DeliveryLineId = lineB.Id, LotId = lotB.Id, Quantity = 8m
            },
            new StockBalance
            {
                OrganizationId = DemoOrganization.Id, LocationId = vehicleLocation.Id,
                LotId = lotB.Id, QuantityOnHand = 8m
            },
            plan, stopA, stopB, receiptA, receiptB,
            new ReceiptLine
            {
                OrganizationId = DemoOrganization.Id, ReceiptId = receiptA.Id, DeliveryLineId = a.DeliveryLineId,
                QuantityAccepted = 6m, QuantityRejected = 4m, RejectionReason = "Daño"
            },
            new ReceiptLine
            {
                OrganizationId = DemoOrganization.Id, ReceiptId = receiptB.Id, DeliveryLineId = lineB.Id,
                QuantityAccepted = 5m, QuantityRejected = 3m, RejectionReason = "Faltante"
            });
        await db.SaveChangesAsync();

        var guideNumber = $"GRT-{plan.Id.ToString()[..8].ToUpperInvariant()}";
        return new Seeded(plan.Id, guideNumber, deliveryA.OriginLocationId, vehicleLocation.Id,
            pointAId, pointB.Id, a.LotId, lotB.Id, deliveryA.Id, deliveryB.Id, a.UnitId!.Value, unitB.Id);
    }

    private static async Task<string> SnapshotAsync(ApiFactory api)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return JsonSerializer.Serialize(new
        {
            Units = await db.HandlingUnits.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Balances = await db.StockBalances.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Movements = await db.InventoryMovements.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Orders = await db.DeliveryOrders.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            OrderLines = await db.DeliveryOrderLines.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Locations = await db.Locations.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Replays = await db.IdempotencyRecords.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
        });
    }

    private static async Task<decimal> OnHandAsync(ApplicationDbContext db, Guid locationId, Guid lotId) =>
        await db.StockBalances.Where(b => b.LocationId == locationId && b.LotId == lotId)
            .Select(b => b.QuantityOnHand).SingleOrDefaultAsync();

    [Fact]
    public async Task Partial_return_moves_rejected_quantity_to_quarantine_and_rebalances_per_destination()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var planner = api.ClientFor(TestTokens.Planner);
        using var warehouse = api.ClientFor(TestTokens.Warehouse);
        var seed = await SeedAsync(api);

        var guide = await planner.GetFromJsonAsync<ReturnGuideDto>($"/api/v1/return-guides/{seed.GuideNumber}");
        Assert.NotNull(guide);
        Assert.Equal("PendingWarehouse", guide!.Status);
        Assert.Equal(2, guide.Lines.Count);

        var lineA = guide.Lines.Single(line => line.DeliveryId == seed.DeliveryAId);
        Assert.Equal(4m, lineA.Quantity);
        Assert.Equal("Daño", lineA.Reason);
        Assert.Equal("Quarantine", lineA.StockDisposition);
        Assert.Equal(seed.PointAId, lineA.DestinationServicePointId);
        var lineB = guide.Lines.Single(line => line.DeliveryId == seed.DeliveryBId);
        Assert.Equal(3m, lineB.Quantity);
        Assert.Equal("Faltante", lineB.Reason);
        Assert.Equal("Quarantine", lineB.StockDisposition);
        Assert.Equal(seed.PointBId, lineB.DestinationServicePointId);

        var receive = await warehouse.PostAsJsonAsync("/api/v1/return-guides/receive",
            new ReceiveReturnGuideRequest(seed.GuideNumber, Guid.NewGuid(), DateTime.UtcNow));
        Assert.Equal(HttpStatusCode.OK, receive.StatusCode);
        var response = await receive.Content.ReadFromJsonAsync<ReceiveReturnGuideResponse>();
        Assert.NotNull(response);
        Assert.Equal(2, response!.ItemsReconciled);
        Assert.Equal(2, response.RebalanceOrderIds!.Count);

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var quarantine = await db.Locations.SingleAsync(l =>
                l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Quarantine);
            Assert.Equal(4m, await OnHandAsync(db, quarantine.Id, seed.LotAId));
            Assert.Equal(3m, await OnHandAsync(db, quarantine.Id, seed.LotBId));
            Assert.Equal(6m, await OnHandAsync(db, seed.VehicleLocationId, seed.LotAId));
            Assert.Equal(5m, await OnHandAsync(db, seed.VehicleLocationId, seed.LotBId));
            Assert.Equal(0m, await OnHandAsync(db, seed.WarehouseId, seed.LotAId));
            Assert.Equal(0m, await OnHandAsync(db, seed.WarehouseId, seed.LotBId));

            var unitA = await db.HandlingUnits.SingleAsync(unit => unit.Id == seed.UnitAId);
            var unitB = await db.HandlingUnits.SingleAsync(unit => unit.Id == seed.UnitBId);
            Assert.Equal(HandlingUnitStates.Returned, unitA.Status);
            Assert.Equal(HandlingUnitStates.Returned, unitB.Status);
            Assert.Equal(quarantine.Id, unitA.LocationId);
            Assert.Equal(quarantine.Id, unitB.LocationId);

            var orders = await db.DeliveryOrders.Where(order =>
                order.OrganizationId == DemoOrganization.Id && order.SourceType == "ReturnGuide").ToListAsync();
            Assert.Equal(2, orders.Count);
            var orderA = orders.Single(order => order.DestinationServicePointId == seed.PointAId);
            var orderB = orders.Single(order => order.DestinationServicePointId == seed.PointBId);
            Assert.Equal("High", orderA.Priority);
            Assert.Equal("Critical", orderB.Priority);
            Assert.Equal(4m, await db.DeliveryOrderLines.Where(line => line.DeliveryOrderId == orderA.Id)
                .SumAsync(line => line.RequestedQuantity));
            Assert.Equal(3m, await db.DeliveryOrderLines.Where(line => line.DeliveryOrderId == orderB.Id)
                .SumAsync(line => line.RequestedQuantity));
            Assert.Equal(2, await db.InventoryMovements.CountAsync(movement =>
                movement.OrganizationId == DemoOrganization.Id && movement.Kind == MovementKinds.Return));
            Assert.Equal(1, await db.IdempotencyRecords.CountAsync(record =>
                record.ActionCode == "ReceiveReturnGuide"));
        }

        var before = await SnapshotAsync(api);
        var duplicate = await warehouse.PostAsJsonAsync("/api/v1/return-guides/receive",
            new ReceiveReturnGuideRequest(seed.GuideNumber, Guid.NewGuid(), DateTime.UtcNow));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains(FieldErrorCodes.AlreadyReceived, await duplicate.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Fact]
    public async Task Return_receive_rejects_insufficient_vehicle_stock_without_moving_anything()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var warehouse = api.ClientFor(TestTokens.Warehouse);
        var seed = await SeedAsync(api);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var balance = await db.StockBalances.SingleAsync(balance =>
                balance.LocationId == seed.VehicleLocationId && balance.LotId == seed.LotAId);
            balance.QuantityOnHand = 2m;
            await db.SaveChangesAsync();
        }

        var before = await SnapshotAsync(api);
        var receive = await warehouse.PostAsJsonAsync("/api/v1/return-guides/receive",
            new ReceiveReturnGuideRequest(seed.GuideNumber, Guid.NewGuid(), DateTime.UtcNow));
        Assert.Equal(HttpStatusCode.Conflict, receive.StatusCode);
        Assert.Contains(FieldErrorCodes.InsufficientVehicleStock, await receive.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotAsync(api));
    }
}
