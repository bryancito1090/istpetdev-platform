namespace IstpetDev.Application.Logistics;

public sealed record WarehouseLotStock(
    Guid LotId,
    string LotCode,
    Guid ProductId,
    decimal AvailableQuantity,
    DateOnly? ExpiresOn,
    bool IsBlocked);

public sealed record LotAllocation(
    Guid LotId,
    string LotCode,
    decimal Quantity);

public sealed record PointAllocation(
    Guid ServicePointId,
    string PointCode,
    Guid ProductId,
    string Sku,
    decimal RequestedQuantity,
    decimal AllocatedQuantity,
    IReadOnlyList<LotAllocation> AllocatedLots);

public sealed record UnservedAllocation(
    string PointCode,
    string Sku,
    string Reason,
    IReadOnlyList<Fact> Facts);

public sealed record AllocationResult(
    IReadOnlyDictionary<Guid, PointAllocation> Allocations,
    IReadOnlyList<UnservedAllocation> Unserved);

// FR-02: Asignador de reposición desde bodega central con política FEFO y stock de seguridad.
public sealed class ReplenishmentAllocator
{
    public AllocationResult Allocate(
        IReadOnlyList<RiskItem> items,
        IReadOnlyList<WarehouseLotStock> warehouseLots,
        decimal warehouseSafetyStockPct,
        DateOnly today)
    {
        var allocations = new Dictionary<Guid, PointAllocation>();
        var unserved = new List<UnservedAllocation>();

        // Agrupar por producto
        var itemsByProduct = items
            .Where(i => i.SuggestedQuantity > 0 && i.Level is RiskLevel.Critico or RiskLevel.Alto or RiskLevel.Vigilar)
            .GroupBy(i => i.ProductId);

        foreach (var group in itemsByProduct)
        {
            var productId = group.Key;

            // Lotes utilizables ordenados por FEFO (First Expired, First Out) y código de lote
            var usableLots = warehouseLots
                .Where(l => l.ProductId == productId && !l.IsBlocked && (l.ExpiresOn == null || l.ExpiresOn > today) && l.AvailableQuantity > 0)
                .OrderBy(l => l.ExpiresOn ?? DateOnly.MaxValue)
                .ThenBy(l => l.LotCode, StringComparer.Ordinal)
                .Select(l => new MutableLotStock(l.LotId, l.LotCode, l.AvailableQuantity))
                .ToList();

            var totalUsableStock = usableLots.Sum(l => l.Remaining);
            var safetyStock = totalUsableStock * (warehouseSafetyStockPct / 100m);
            var assignableStockRemaining = Math.Max(0m, totalUsableStock - safetyStock);

            // Separar obligatorios de consolidación
            var mandatory = group
                .Where(i => i.Level is RiskLevel.Critico or RiskLevel.Alto)
                .OrderBy(i => i.Level == RiskLevel.Critico ? 0 : 1)
                .ThenBy(i => (i.CoverageDays ?? 0m) - i.LeadTimeDays)
                .ThenBy(i => CriticalityRank(i.Criticality))
                .ThenBy(i => i.PointCode, StringComparer.Ordinal)
                .ToList();

            var consolidation = group
                .Where(i => i.Level is RiskLevel.Vigilar)
                .OrderBy(i => (i.CoverageDays ?? 0m) - i.LeadTimeDays)
                .ThenBy(i => CriticalityRank(i.Criticality))
                .ThenBy(i => i.PointCode, StringComparer.Ordinal)
                .ToList();

            // 1. Asignar a obligatorios
            foreach (var item in mandatory)
            {
                var needed = item.SuggestedQuantity;
                var toAssign = Math.Min(needed, assignableStockRemaining);

                var lotAllocations = AllocateFromLots(usableLots, toAssign);
                assignableStockRemaining -= toAssign;

                allocations[item.ServicePointId] = new PointAllocation(
                    item.ServicePointId,
                    item.PointCode,
                    item.ProductId,
                    item.Sku,
                    needed,
                    toAssign,
                    lotAllocations);

                if (toAssign == 0 && needed > 0)
                {
                    unserved.Add(new UnservedAllocation(
                        item.PointCode,
                        item.Sku,
                        "warehouse_stock",
                        item.Facts));
                }
            }

            // 2. Asignar a consolidación (solo del remanente)
            foreach (var item in consolidation)
            {
                var needed = item.SuggestedQuantity;
                var toAssign = Math.Min(needed, assignableStockRemaining);

                var lotAllocations = AllocateFromLots(usableLots, toAssign);
                assignableStockRemaining -= toAssign;

                allocations[item.ServicePointId] = new PointAllocation(
                    item.ServicePointId,
                    item.PointCode,
                    item.ProductId,
                    item.Sku,
                    needed,
                    toAssign,
                    lotAllocations);

                if (toAssign == 0 && needed > 0)
                {
                    unserved.Add(new UnservedAllocation(
                        item.PointCode,
                        item.Sku,
                        "warehouse_stock",
                        item.Facts));
                }
            }
        }

        return new AllocationResult(allocations, unserved);
    }

    private static List<LotAllocation> AllocateFromLots(List<MutableLotStock> lots, decimal quantity)
    {
        var result = new List<LotAllocation>();
        var remainingNeeded = quantity;

        foreach (var lot in lots)
        {
            if (remainingNeeded <= 0) break;
            if (lot.Remaining <= 0) continue;

            var take = Math.Min(lot.Remaining, remainingNeeded);
            lot.Remaining -= take;
            remainingNeeded -= take;

            result.Add(new LotAllocation(lot.LotId, lot.LotCode, take));
        }

        return result;
    }

    private static int CriticalityRank(string criticality) => criticality.ToLowerInvariant() switch
    {
        "alta" => 0,
        "media" => 1,
        "normal" => 2,
        _ => 3
    };

    private sealed class MutableLotStock(Guid lotId, string lotCode, decimal initialQuantity)
    {
        public Guid LotId { get; } = lotId;
        public string LotCode { get; } = lotCode;
        public decimal Remaining { get; set; } = initialQuantity;
    }
}
