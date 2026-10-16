using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Integration.Logistics;

using DomainLocation = IstpetDev.Domain.Entities.Location;

// #162: el stock en tránsito incluye las entregas Dispatched, InTransit y Delivered sin recibo;
// Received/Partial (y el legado Delivered con recibo) ya conciliaron y no cuentan.
[Trait("Category", "Integration")]
public sealed class RiskPendingDeliveriesTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static IOptions<LogisticsOptions> Options() =>
        Microsoft.Extensions.Options.Options.Create(new LogisticsOptions
        {
            DemandWindowDays = 14,
            DefaultLeadTimeDays = 2m,
            SafetyMarginDays = 3m,
            ConsolidationExtraDays = 3m,
            TargetCoverageDays = 10m,
            SafetyStockUnits = 0m,
            StaleStockDays = 7,
            WarehouseSafetyStockPct = 10m,
            Clock = new ClockOptions { Mode = ClockMode.Fixed, FixedDate = new DateOnly(2026, 10, 12) }
        });

    [Fact]
    public async Task In_transit_counts_delivered_pending_and_excludes_received()
    {
        var connection = postgres.NewDatabase();
        await using var db = TestDb.Context(connection);

        var point = new ServicePoint { Code = "SP-RISK", Name = "Punto", Criticality = "normal", IsActive = true };
        var product = new Product { Sku = "SKU-RISK", Name = "Producto", BaseUnit = "L", IsActive = true };
        var lot = new Lot { ProductId = product.Id, Code = "L-RISK" };
        var warehouse = new DomainLocation { Code = "B-RISK", Name = "Bodega", Kind = LocationKinds.Warehouse, IsActive = true };
        var pointLocation = new DomainLocation { Code = "SP-RISK", Name = "Punto", Kind = LocationKinds.ServicePoint, ServicePointId = point.Id, IsActive = true };
        var actor = new Account { Issuer = TestTokens.Issuer, Subject = "risk-actor", Kind = "human", DisplayName = "Receptor", Role = Roles.Receptor, IsActive = true };
        // El cálculo arma el catálogo de lotes desde los saldos del punto; sin saldo no evalúa líneas en tránsito.
        var pointBalance = new StockBalance { LocationId = pointLocation.Id, LotId = lot.Id, QuantityOnHand = 0m, QuantityReserved = 0m };
        db.AddRange(point, product, lot, warehouse, pointLocation, actor, pointBalance);

        Delivery AddDelivery(string code, string status, bool withReceipt, decimal accepted, decimal rejected)
        {
            var delivery = new Delivery
            {
                Code = code,
                OriginLocationId = warehouse.Id,
                ServicePointId = point.Id,
                Status = status,
                DispatchedAtUtc = DateTime.UtcNow,
            };
            db.Add(delivery);
            var line = new DeliveryLine { DeliveryId = delivery.Id, LotId = lot.Id, PlannedQuantity = 10m, DispatchedQuantity = 10m, AcceptedQuantity = accepted, RejectedQuantity = rejected };
            db.Add(line);
            if (withReceipt)
            {
                var receipt = new Receipt { DeliveryId = delivery.Id, ActorAccountId = actor.Id, OperationId = BaseEntity.NewId(), ReceivedAtServer = DateTime.UtcNow };
                db.Add(receipt);
                db.Add(new ReceiptLine { ReceiptId = receipt.Id, DeliveryLineId = line.Id, QuantityAccepted = accepted, QuantityRejected = rejected });
            }
            return delivery;
        }

        AddDelivery("D-RISK-DISP", DeliveryStates.Dispatched, withReceipt: false, accepted: 0m, rejected: 0m);
        AddDelivery("D-RISK-TRANSIT", DeliveryStates.InTransit, withReceipt: false, accepted: 0m, rejected: 0m);
        AddDelivery("D-RISK-DELIVERED", DeliveryStates.Delivered, withReceipt: false, accepted: 0m, rejected: 0m);
        AddDelivery("D-RISK-RECEIVED", DeliveryStates.Received, withReceipt: true, accepted: 10m, rejected: 0m);
        AddDelivery("D-RISK-PARTIAL", DeliveryStates.Partial, withReceipt: true, accepted: 0m, rejected: 10m);
        // Legado: Delivered con recibo previo ya es recepción final y no debe contarse.
        AddDelivery("D-RISK-LEGACY", DeliveryStates.Delivered, withReceipt: true, accepted: 0m, rejected: 10m);

        await db.SaveChangesAsync();

        var options = Options();
        var calculator = new RiskCalculator(db, new OperationalClock(db, options), options);
        var items = await calculator.CalculateAsync();

        var item = Assert.Single(items, i => i.ServicePointId == point.Id && i.ProductId == product.Id);
        Assert.Equal(30m, item.InTransit); // Dispatched 10 + InTransit 10 + Delivered pendiente 10
    }
}
