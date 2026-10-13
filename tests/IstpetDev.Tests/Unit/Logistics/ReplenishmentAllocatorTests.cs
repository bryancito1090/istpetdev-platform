using IstpetDev.Application.Logistics;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public class ReplenishmentAllocatorTests
{
    [Fact]
    public void Allocator_prioritizes_mandatory_by_risk_level_coverage_and_criticality()
    {
        var skuId = Guid.NewGuid();
        var sku = "DES-01";

        // Hospital 1: Critico, Cobertura 1.5, sugerido 50
        var itemCritico = new RiskItem(
            ServicePointId: Guid.NewGuid(),
            PointCode: "HOS-01",
            Criticality: "alta",
            ProductId: skuId,
            Sku: sku,
            BaseUnit: "L",
            Available: 10m,
            DailyDemand: 10m,
            CoverageDays: 1.0m,
            LeadTimeDays: 2m,
            SafetyMarginDays: 3m,
            Level: RiskLevel.Critico,
            SuggestedQuantity: 50m,
            InTransit: 0m,
            Facts: []);

        // Hospital 2: Alto, Cobertura 4.0, sugerido 50
        var itemAlto = new RiskItem(
            ServicePointId: Guid.NewGuid(),
            PointCode: "HOS-02",
            Criticality: "alta",
            ProductId: skuId,
            Sku: sku,
            BaseUnit: "L",
            Available: 40m,
            DailyDemand: 10m,
            CoverageDays: 4.0m,
            LeadTimeDays: 2m,
            SafetyMarginDays: 3m,
            Level: RiskLevel.Alto,
            SuggestedQuantity: 50m,
            InTransit: 0m,
            Facts: []);

        // Clínica 3: Vigilar (Consolidación), sugerido 30
        var itemVigilar = new RiskItem(
            ServicePointId: Guid.NewGuid(),
            PointCode: "CLI-03",
            Criticality: "normal",
            ProductId: skuId,
            Sku: sku,
            BaseUnit: "L",
            Available: 70m,
            DailyDemand: 10m,
            CoverageDays: 7.0m,
            LeadTimeDays: 2m,
            SafetyMarginDays: 3m,
            Level: RiskLevel.Vigilar,
            SuggestedQuantity: 30m,
            InTransit: 0m,
            Facts: []);

        // Bodega: Total 80 L utilizables. Seguridad bodega 10% = 8 L. Asignable = 72 L.
        var today = new DateOnly(2026, 10, 15);
        var lots = new List<WarehouseLotStock>
        {
            new(Guid.NewGuid(), "LOTE-01", skuId, 50m, today.AddDays(30), false),
            new(Guid.NewGuid(), "LOTE-02", skuId, 30m, today.AddDays(60), false)
        };

        var allocator = new ReplenishmentAllocator();
        var result = allocator.Allocate(
            items: [itemAlto, itemVigilar, itemCritico], // orden desordenado a propósito
            warehouseLots: lots,
            warehouseSafetyStockPct: 10m,
            today: today);

        // HOS-01 (Critico) debe recibir sus 50 L completos
        Assert.Equal(50m, result.Allocations[itemCritico.ServicePointId].AllocatedQuantity);

        // Quedan 72 - 50 = 22 L disponibles. HOS-02 (Alto) pide 50, solo recibe 22 L
        Assert.Equal(22m, result.Allocations[itemAlto.ServicePointId].AllocatedQuantity);

        // CLI-03 (Vigilar) no recibe nada porque no queda remanente (marcado en unserved warehouse_stock)
        Assert.Equal(0m, result.Allocations[itemVigilar.ServicePointId].AllocatedQuantity);
        Assert.Contains(result.Unserved, u => u.PointCode == "CLI-03" && u.Reason == "warehouse_stock");
    }

    [Fact]
    public void Allocator_skips_blocked_and_expired_lots()
    {
        var skuId = Guid.NewGuid();
        var today = new DateOnly(2026, 10, 15);

        var lots = new List<WarehouseLotStock>
        {
            new(Guid.NewGuid(), "LOTE-VENCIDO", skuId, 100m, today.AddDays(-1), false), // Vencido ayer
            new(Guid.NewGuid(), "LOTE-BLOQUEADO", skuId, 100m, today.AddDays(30), true), // Bloqueado
            new(Guid.NewGuid(), "LOTE-VALIDO", skuId, 40m, today.AddDays(10), false)    // Válido
        };

        var item = new RiskItem(
            ServicePointId: Guid.NewGuid(),
            PointCode: "HOS-01",
            Criticality: "alta",
            ProductId: skuId,
            Sku: "DES-01",
            BaseUnit: "L",
            Available: 10m,
            DailyDemand: 10m,
            CoverageDays: 1.0m,
            LeadTimeDays: 2m,
            SafetyMarginDays: 3m,
            Level: RiskLevel.Critico,
            SuggestedQuantity: 50m,
            InTransit: 0m,
            Facts: []);

        var allocator = new ReplenishmentAllocator();
        var result = allocator.Allocate([item], lots, warehouseSafetyStockPct: 0m, today);

        // Solo puede usar el lote válido (40 L)
        var alloc = result.Allocations[item.ServicePointId];
        Assert.Equal(40m, alloc.AllocatedQuantity);
        Assert.Single(alloc.AllocatedLots);
        Assert.Equal("LOTE-VALIDO", alloc.AllocatedLots[0].LotCode);
    }
}
