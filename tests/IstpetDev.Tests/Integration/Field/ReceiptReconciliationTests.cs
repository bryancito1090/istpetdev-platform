using System;
using System.Linq;
using System.Threading.Tasks;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

// #111: el stock en percha y el pedido en campo no deben romper la recepción ni el libro de movimientos.
[Trait("Category", "Integration")]
public class ReceiptReconciliationTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private sealed record Seeded(Guid DeliveryId, Guid LineA, Guid LotA, Guid LineB, Guid LotB, Guid ProductA, Guid ProductB,
        Guid ServicePointId, Guid PointLocationId, Guid DriverId, Guid UnitId, string UnitToken);

    // Entrega con parada de ruta (destino = punto) y dos lotes; secondProduct = false pone ambos lotes en el mismo SKU.
    private static async Task<Seeded> SeedAsync(ApiFactory api, decimal shelfA, decimal shelfB, bool secondProduct = false,
        Guid? servicePointId = null)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var s = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var productA = await db.Products.FirstOrDefaultAsync(p => p.Sku == "SKU-111-A")
                       ?? new Product { Sku = "SKU-111-A", Name = "Producto A", BaseUnit = "L", IsActive = true };
        var productB = secondProduct ? new Product { Sku = $"SKU-111-B-{s}", Name = "Producto B", BaseUnit = "L", IsActive = true } : productA;
        if (db.Entry(productA).State == EntityState.Detached) db.Add(productA);
        if (secondProduct) db.Add(productB);

        var lotA = new Lot { ProductId = productA.Id, Code = $"LA-{s}" };
        var lotB = new Lot { ProductId = productB.Id, Code = $"LB-{s}" };

        ServicePoint point;
        Location pointLocation;
        if (servicePointId is { } existingPoint)
        {
            point = await db.ServicePoints.SingleAsync(p => p.Id == existingPoint);
            pointLocation = await db.Locations.SingleAsync(l => l.ServicePointId == existingPoint);
        }
        else
        {
            point = new ServicePoint { Code = $"P-{s}", Name = "Punto", Criticality = "normal", IsActive = true };
            pointLocation = new Location { Code = $"P-{s}", Name = "Punto", Kind = LocationKinds.ServicePoint, ServicePointId = point.Id, IsActive = true };
            db.AddRange(point, pointLocation);
        }

        var warehouse = new Location { Code = $"B-{s}", Name = "Bodega", Kind = LocationKinds.Warehouse, IsActive = true };
        var vehicle = new Vehicle { Code = $"V-{s}", DepotLocationId = warehouse.Id, MaxWeight = 1000, IsActive = true };
        var vehicleLocation = new Location { Code = $"V-{s}", Name = "Vehículo", Kind = LocationKinds.Vehicle, VehicleId = vehicle.Id, IsActive = true };
        var plan = new RoutePlan { Code = $"R-{s}", Revision = 1, VehicleId = vehicle.Id, Status = "Published", ProposedBy = "test",
                                   PlannedStartUtc = DateTime.UtcNow, AlgorithmVersion = "test" };
        var stop = new RouteStop { RoutePlanId = plan.Id, ServicePointId = point.Id, SequenceNumber = 1 };
        var delivery = new Delivery { Code = $"D-{s}", RouteStopId = stop.Id, OriginLocationId = warehouse.Id, ServicePointId = point.Id,
                                      Status = "Dispatched", AssignedAccountId = driver.Id, DispatchedAtUtc = DateTime.UtcNow };
        var lineA = new DeliveryLine { DeliveryId = delivery.Id, LotId = lotA.Id, PlannedQuantity = 10m, DispatchedQuantity = 10m };
        var lineB = new DeliveryLine { DeliveryId = delivery.Id, LotId = lotB.Id, PlannedQuantity = 10m, DispatchedQuantity = 10m };
        var unit = new HandlingUnit { Code = $"HU-{s}", DeliveryId = delivery.Id, LocationId = vehicleLocation.Id, Status = HandlingUnitStates.Dispatched };
        var unitToken = $"token-111-{s}-0123456789abcdef";

        db.AddRange(lotA, lotB, warehouse, vehicle, vehicleLocation, plan, stop, delivery, lineA, lineB, unit,
            new HandlingUnitLine { HandlingUnitId = unit.Id, DeliveryLineId = lineA.Id, LotId = lotA.Id, Quantity = 10m },
            new QrAccessToken { HandlingUnitId = unit.Id, TokenHash = FieldSeed.HashToken(unitToken), ExpiresAtUtc = DateTime.UtcNow.AddDays(7) },
            new StockBalance { LocationId = vehicleLocation.Id, LotId = lotA.Id, QuantityOnHand = 10m },
            new StockBalance { LocationId = vehicleLocation.Id, LotId = lotB.Id, QuantityOnHand = 10m },
            new StockBalance { LocationId = pointLocation.Id, LotId = lotA.Id, QuantityOnHand = shelfA },
            new StockBalance { LocationId = pointLocation.Id, LotId = lotB.Id, QuantityOnHand = shelfB });
        await db.SaveChangesAsync();

        // #162: la recepción exige que el conductor haya entregado el bulto; se ejecutan los comandos reales.
        await FieldSeed.DeliverUnitAsync(api, new FieldSeed.Dispatched(delivery.Id, lineA.Id, lotA.Id, pointLocation.Id, vehicleLocation.Id, delivery.Version, unit.Id, driver.Id, unitToken));

        return new Seeded(delivery.Id, lineA.Id, lotA.Id, lineB.Id, lotB.Id, productA.Id, productB.Id, point.Id, pointLocation.Id, driver.Id, unit.Id, unitToken);
    }

    private static async Task<ReceiptResponse> ReceiveAsync(ApiFactory api, Seeded d, ReceiptLineRequest a, ReceiptLineRequest b,
        Guid? operationId = null, DateTime? occurredAt = null)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
        var receiver = await db.Accounts.SingleAsync(x => x.Subject == TestTokens.Receiver);
        var op = operationId ?? BaseEntity.NewId();
        return await field.CreateReceiverReceiptAsync(d.UnitId, d.UnitToken, new CreateReceiptRequest(op, 0, occurredAt ?? DateTime.UtcNow, [a, b]), receiver.Id, op.ToString());
    }

    private static ReceiptLineRequest Line(Guid lineId, Guid lotId, decimal accepted, decimal? remaining = null, decimal? next = null) =>
        new(lineId, lotId, accepted, 0m, "L", null, remaining, next);

    private static ApplicationDbContext Db(ApiFactory api, out IServiceScope scope)
    {
        scope = api.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task Two_lots_of_the_same_product_with_shelf_stock_are_received_with_one_consumption()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await SeedAsync(api, shelfA: 10m, shelfB: 5m);

        await ReceiveAsync(api, d, Line(d.LineA, d.LotA, 10m, remaining: 4m), Line(d.LineB, d.LotB, 10m, remaining: 2m));

        var db = Db(api, out var scope);
        using (scope)
        {
            var consumption = await db.Consumptions.SingleAsync(c => c.ServicePointId == d.ServicePointId && c.ProductId == d.ProductA);
            Assert.Equal(9m, consumption.Quantity); // (10 - 4) + (5 - 2)
            Assert.Equal("FieldReconciliation", consumption.SourceKind);
            Assert.Equal(7, consumption.PeriodDays);
            Assert.Equal(14m, await OnHand(db, d.PointLocationId, d.LotA)); // 4 declarados + 10 recibidos
            Assert.Equal(12m, await OnHand(db, d.PointLocationId, d.LotB)); // 2 declarados + 10 recibidos
        }
    }

    [Fact]
    public async Task A_second_receipt_on_the_same_day_accumulates_the_field_consumption()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var first = await SeedAsync(api, shelfA: 10m, shelfB: 0m);
        await ReceiveAsync(api, first, Line(first.LineA, first.LotA, 10m, remaining: 4m), Line(first.LineB, first.LotB, 10m));

        var second = await SeedAsync(api, shelfA: 8m, shelfB: 0m, servicePointId: first.ServicePointId);
        await ReceiveAsync(api, second, Line(second.LineA, second.LotA, 10m, remaining: 5m), Line(second.LineB, second.LotB, 10m));

        var db = Db(api, out var scope);
        using (scope)
        {
            var consumption = await db.Consumptions.SingleAsync(c => c.ServicePointId == first.ServicePointId && c.ProductId == first.ProductA);
            Assert.Equal(9m, consumption.Quantity); // 6 de la primera + 3 de la segunda
        }
    }

    [Fact]
    public async Task An_imported_consumption_of_the_day_is_kept_and_the_difference_stays_in_the_ledger()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await SeedAsync(api, shelfA: 10m, shelfB: 0m);
        var db = Db(api, out var scope);
        using (scope)
        {
            db.Consumptions.Add(new Consumption { ServicePointId = d.ServicePointId, ProductId = d.ProductA,
                OccurredOn = DateOnly.FromDateTime(DateTime.UtcNow), Quantity = 12m, PeriodDays = 1, SourceKind = "import" });
            await db.SaveChangesAsync();
        }

        await ReceiveAsync(api, d, Line(d.LineA, d.LotA, 10m, remaining: 4m), Line(d.LineB, d.LotB, 10m));

        db = Db(api, out scope);
        using (scope)
        {
            var consumption = await db.Consumptions.SingleAsync(c => c.ServicePointId == d.ServicePointId && c.ProductId == d.ProductA);
            Assert.Equal(12m, consumption.Quantity);
            Assert.Equal("import", consumption.SourceKind);
            var adjustment = await db.InventoryMovements.SingleAsync(m => m.LotId == d.LotA && m.Kind == MovementKinds.FieldAdjustment);
            Assert.Equal(6m, adjustment.Quantity);
            Assert.Equal(d.PointLocationId, adjustment.FromLocationId);
        }
    }

    [Fact]
    public async Task Shelf_stock_above_the_balance_is_an_explicit_adjustment_and_the_balance_matches_the_ledger()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await SeedAsync(api, shelfA: 3m, shelfB: 0m);

        await ReceiveAsync(api, d, Line(d.LineA, d.LotA, 10m, remaining: 8m), Line(d.LineB, d.LotB, 10m));

        var db = Db(api, out var scope);
        using (scope)
        {
            var adjustment = await db.InventoryMovements.SingleAsync(m => m.LotId == d.LotA && m.Kind == MovementKinds.FieldAdjustment);
            Assert.Equal(5m, adjustment.Quantity);
            Assert.Equal(d.PointLocationId, adjustment.ToLocationId);
            Assert.Null(adjustment.FromLocationId);
            Assert.False(await db.Consumptions.AnyAsync(c => c.ServicePointId == d.ServicePointId));

            // Saldo inicial + entradas - salidas del punto = saldo final.
            var movements = await db.InventoryMovements.Where(m => m.LotId == d.LotA).ToListAsync();
            var net = movements.Where(m => m.ToLocationId == d.PointLocationId).Sum(m => m.Quantity)
                    - movements.Where(m => m.FromLocationId == d.PointLocationId).Sum(m => m.Quantity);
            Assert.Equal(18m, await OnHand(db, d.PointLocationId, d.LotA));
            Assert.Equal(18m, 3m + net);
        }
    }

    [Fact]
    public async Task Two_lines_requesting_restock_create_one_order_with_one_line_per_product_and_retries_reuse_it()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await SeedAsync(api, shelfA: 0m, shelfB: 0m, secondProduct: true);
        var operationId = BaseEntity.NewId();

        var occurredAt = DateTime.UtcNow;
        var first = await ReceiveAsync(api, d, Line(d.LineA, d.LotA, 10m, next: 20m), Line(d.LineB, d.LotB, 10m, next: 15m), operationId, occurredAt: occurredAt);
        var retry = await ReceiveAsync(api, d, Line(d.LineA, d.LotA, 10m, next: 20m), Line(d.LineB, d.LotB, 10m, next: 15m), operationId, occurredAt: occurredAt);

        Assert.Equal(first.ReceiptId, retry.ReceiptId);
        var db = Db(api, out var scope);
        using (scope)
        {
            var order = await db.DeliveryOrders.SingleAsync(o => o.SourceType == "FieldRequest");
            Assert.EndsWith(operationId.ToString("N"), order.OrderNumber);
            var lines = await db.DeliveryOrderLines.Where(l => l.DeliveryOrderId == order.Id).ToListAsync();
            Assert.Equal(2, lines.Count);
            Assert.Equal(20m, lines.Single(l => l.ProductId == d.ProductA).RequestedQuantity);
            Assert.Equal(15m, lines.Single(l => l.ProductId == d.ProductB).RequestedQuantity);
        }
    }

    private static Task<decimal> OnHand(ApplicationDbContext db, Guid locationId, Guid lotId) =>
        db.StockBalances.Where(b => b.LocationId == locationId && b.LotId == lotId).Select(b => b.QuantityOnHand).SingleAsync();
}
