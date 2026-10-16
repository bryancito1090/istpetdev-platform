using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record ProductWarehouseStock(
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    decimal DistanceKm,
    decimal AvailableQuantity,
    decimal SafetyStock,
    IReadOnlyList<WarehouseLotStock> UsableLots
);

public sealed record WarehouseLotStockDetailDto(
    Guid LotId,
    string LotCode,
    DateOnly? ExpiresOn,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal NetUsableQuantity
);

public sealed record WarehouseProductStockDto(
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal NetUsableQuantity,
    decimal SafetyStock,
    decimal AvailableQuantity,
    IReadOnlyList<WarehouseLotStockDetailDto> Lots
);

public sealed record WarehouseStockQueryResult(
    DateOnly OperationalDate,
    decimal SafetyStockPct,
    Guid ProductId,
    string Sku,
    string ProductName,
    string BaseUnit,
    IReadOnlyList<WarehouseProductStockDto> Warehouses
);

public sealed record SourcingDemandInput(
    Guid? OrderId,
    string? OrderNumber,
    string SourceKind, // "Order" o "Risk"
    Guid DestinationId,
    bool IsDestinationWarehouse,
    string DestinationCode,
    string DestinationName,
    GeoPoint DestinationPosition,
    Guid ProductId,
    decimal RequestedQuantity,
    string? RiskLevel = null,
    decimal? CoverageDays = null
);

public sealed record LotAllocationDto(
    Guid LotId,
    string LotCode,
    DateOnly? ExpiresOn,
    decimal Quantity
);

public sealed record WarehouseAllocationDto(
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    decimal DistanceKm,
    GeoPoint WarehousePosition,
    decimal Quantity,
    IReadOnlyList<LotAllocationDto> Lots
);

public sealed record DemandLineSourcingDto(
    Guid? OrderId,
    string? OrderNumber,
    string SourceKind,
    Guid DestinationId,
    bool IsDestinationWarehouse,
    string DestinationCode,
    string DestinationName,
    GeoPoint DestinationPosition,
    Guid ProductId,
    string Sku,
    string ProductName,
    string BaseUnit,
    decimal RequestedQuantity,
    decimal AllocatedQuantity,
    decimal TotalAllocated,
    decimal ShortageQuantity,
    bool FullySourced,
    string? RiskLevel,
    decimal? CoverageDays,
    IReadOnlyList<WarehouseAllocationDto> Allocations
);

public sealed record DemandSourcingOptionDto(
    string OptionId,
    string Title,
    string Description,
    bool IsRecommended,
    bool FullySourced,
    IReadOnlyList<DemandLineSourcingDto> Lines,
    IReadOnlyList<Guid> InvolvedWarehouseIds
);

public sealed record DemandSourcingResult(
    DateOnly OperationalDate,
    string SourceKind,
    decimal SafetyStockPct,
    bool FullySourced,
    IReadOnlyList<DemandLineSourcingDto> Demands,
    IReadOnlyList<DemandSourcingOptionDto> Options
);

public sealed record OrderLineSourcing(
    Guid OrderId,
    string OrderNumber,
    Guid DestinationId,
    bool IsDestinationWarehouse,
    string DestinationCode,
    string DestinationName,
    GeoPoint DestinationPosition,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal RequestedQuantity,
    IReadOnlyList<WarehouseAllocation> Allocations,
    decimal TotalAllocated,
    bool FullySourced
);

public sealed record WarehouseAllocation(
    Guid WarehouseId,
    string WarehouseCode,
    GeoPoint WarehousePosition,
    decimal Quantity,
    IReadOnlyList<LotAllocation> Lots
);

public sealed record SourcingPlanOption(
    string OptionId, // "A" (Consolidado - 1 viaje multi-recogida), "B" (Entregas divididas / parciales)
    string Title,
    string Description,
    bool IsRecommended,
    IReadOnlyList<OrderLineSourcing> Lines,
    IReadOnlyList<Guid> InvolvedWarehouseIds
);

public interface ISourcingService
{
    Task<IReadOnlyList<SourcingPlanOption>> PlanSourcingAsync(IReadOnlyList<Guid> orderIds, CancellationToken ct = default);
    Task<WarehouseStockQueryResult?> GetWarehouseStockAsync(Guid productId, Guid? warehouseId = null, CancellationToken ct = default);
    Task<DemandSourcingResult> PlanDemandSourcingAsync(IReadOnlyList<SourcingDemandInput> demands, string sourceKind, CancellationToken ct = default);
}

public sealed class SourcingService : ISourcingService
{
    private readonly IApplicationDbContext _db;
    private readonly IOperationalClock _clock;
    private readonly IDistanceProvider _distanceProvider;
    private readonly LogisticsOptions _options;

    public SourcingService(
        IApplicationDbContext db,
        IOperationalClock clock,
        IDistanceProvider distanceProvider,
        IOptions<LogisticsOptions> options)
    {
        _db = db;
        _clock = clock;
        _distanceProvider = distanceProvider;
        _options = options.Value;
    }

    public static decimal ComputeSafetyStock(decimal netUsableQuantity, decimal safetyStockPct)
        => netUsableQuantity <= 0m || safetyStockPct <= 0m
            ? 0m
            : netUsableQuantity * (safetyStockPct / 100m);

    public static decimal ComputeAvailableQuantity(decimal netUsableQuantity, decimal safetyStockPct)
        => Math.Max(0m, netUsableQuantity - ComputeSafetyStock(netUsableQuantity, safetyStockPct));

    public async Task<WarehouseStockQueryResult?> GetWarehouseStockAsync(
        Guid productId,
        Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var today = await _clock.TodayAsync(ct);

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrganizationId == DemoOrganization.Id && p.Id == productId, ct);

        if (product is null)
        {
            return null;
        }

        var warehousesQuery = _db.Locations
            .AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive);

        if (warehouseId.HasValue)
        {
            warehousesQuery = warehousesQuery.Where(l => l.Id == warehouseId.Value);
        }

        var warehouses = await warehousesQuery
            .OrderBy(l => l.Code)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);

        var warehouseIds = warehouses.Select(w => w.Id).ToHashSet();

        var usableLots = await _db.Lots
            .AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id &&
                        l.ProductId == productId &&
                        !l.IsBlocked &&
                        (l.ExpiresOn == null || l.ExpiresOn > today))
            .ToDictionaryAsync(l => l.Id, ct);

        var usableLotIds = usableLots.Keys.ToHashSet();

        var balances = warehouseIds.Count == 0 || usableLotIds.Count == 0
            ? []
            : await _db.StockBalances
                .AsNoTracking()
                .Where(b => b.OrganizationId == DemoOrganization.Id &&
                            warehouseIds.Contains(b.LocationId) &&
                            usableLotIds.Contains(b.LotId))
                .ToListAsync(ct);

        var warehouseDtos = new List<WarehouseProductStockDto>(warehouses.Count);

        foreach (var wh in warehouses)
        {
            var whBalancesByLot = balances
                .Where(b => b.LocationId == wh.Id)
                .GroupBy(b => b.LotId);

            var lotDtos = new List<WarehouseLotStockDetailDto>();

            foreach (var grp in whBalancesByLot)
            {
                if (!usableLots.TryGetValue(grp.Key, out var lot)) continue;

                decimal onHand = grp.Sum(b => b.QuantityOnHand);
                decimal reserved = grp.Sum(b => b.QuantityReserved);
                if (onHand <= 0m && reserved <= 0m) continue;

                decimal netUsable = Math.Max(0m, onHand - reserved);
                lotDtos.Add(new WarehouseLotStockDetailDto(
                    lot.Id,
                    lot.Code,
                    lot.ExpiresOn,
                    onHand,
                    reserved,
                    netUsable
                ));
            }

            var orderedLots = lotDtos
                .OrderBy(l => l.ExpiresOn ?? DateOnly.MaxValue)
                .ThenBy(l => l.LotCode, StringComparer.Ordinal)
                .ThenBy(l => l.LotId)
                .ToList();

            decimal totalOnHand = orderedLots.Sum(l => l.QuantityOnHand);
            decimal totalReserved = orderedLots.Sum(l => l.QuantityReserved);
            decimal totalNetUsable = orderedLots.Sum(l => l.NetUsableQuantity);
            decimal safetyStock = ComputeSafetyStock(totalNetUsable, _options.WarehouseSafetyStockPct);
            decimal available = Math.Max(0m, totalNetUsable - safetyStock);

            warehouseDtos.Add(new WarehouseProductStockDto(
                wh.Id,
                wh.Code,
                wh.Name,
                totalOnHand,
                totalReserved,
                totalNetUsable,
                safetyStock,
                available,
                orderedLots
            ));
        }

        return new WarehouseStockQueryResult(
            today,
            _options.WarehouseSafetyStockPct,
            product.Id,
            product.Sku,
            product.Name,
            product.BaseUnit,
            warehouseDtos
        );
    }

    public async Task<IReadOnlyList<SourcingPlanOption>> PlanSourcingAsync(IReadOnlyList<Guid> orderIds, CancellationToken ct = default)
    {
        var distinctOrderIds = orderIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var orders = distinctOrderIds.Count == 0
            ? []
            : await _db.DeliveryOrders
                .AsNoTracking()
                .Where(o => o.OrganizationId == DemoOrganization.Id && distinctOrderIds.Contains(o.Id))
                .ToListAsync(ct);

        var lines = distinctOrderIds.Count == 0
            ? []
            : await _db.DeliveryOrderLines
                .AsNoTracking()
                .Where(l => l.OrganizationId == DemoOrganization.Id && distinctOrderIds.Contains(l.DeliveryOrderId))
                .ToListAsync(ct);

        var warehouses = await _db.Locations
            .AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive)
            .OrderBy(l => l.Code)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);

        var servicePoints = await _db.ServicePoints
            .AsNoTracking()
            .Where(sp => sp.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(sp => sp.Id, ct);

        var ordersById = orders.ToDictionary(o => o.Id);
        var demandInputs = new List<SourcingDemandInput>();

        foreach (var orderId in distinctOrderIds)
        {
            if (!ordersById.TryGetValue(orderId, out var ord)) continue;

            (Guid Id, string Code, string Name, bool IsWh, GeoPoint Pos) dest;
            if (ord.DestinationServicePointId.HasValue && servicePoints.TryGetValue(ord.DestinationServicePointId.Value, out var sp))
            {
                var pos = sp.Position != null
                    ? new GeoPoint((decimal)sp.Position.Y, (decimal)sp.Position.X)
                    : new GeoPoint(-0.2295m, -78.5243m);
                dest = (sp.Id, sp.Code, sp.Name, false, pos);
            }
            else if (ord.DestinationLocationId.HasValue)
            {
                var wh = warehouses.FirstOrDefault(w => w.Id == ord.DestinationLocationId.Value);
                var pos = wh?.Position != null
                    ? new GeoPoint((decimal)wh.Position.Y, (decimal)wh.Position.X)
                    : new GeoPoint(-0.2295m, -78.5243m);
                dest = (wh?.Id ?? Guid.Empty, wh?.Code ?? "WH", wh?.Name ?? "Bodega", true, pos);
            }
            else
            {
                dest = (Guid.Empty, "UNK", "Destino Desconocido", false, new GeoPoint(-0.2295m, -78.5243m));
            }

            var ordLines = lines
                .Where(l => l.DeliveryOrderId == ord.Id)
                .OrderBy(l => l.ProductId)
                .ThenBy(l => l.Id)
                .ToList();

            foreach (var line in ordLines)
            {
                demandInputs.Add(new SourcingDemandInput(
                    ord.Id,
                    ord.OrderNumber,
                    "Order",
                    dest.Id,
                    dest.IsWh,
                    dest.Code,
                    dest.Name,
                    dest.Pos,
                    line.ProductId,
                    line.RequestedQuantity
                ));
            }
        }

        var result = await AllocateDemandsCoreAsync(demandInputs, "Order", includeEmptyDefaultOption: true, ct);

        return result.Options
            .Select(opt => new SourcingPlanOption(
                opt.OptionId,
                opt.Title,
                opt.Description,
                opt.IsRecommended,
                opt.Lines.Select(l => new OrderLineSourcing(
                    l.OrderId ?? Guid.Empty,
                    l.OrderNumber ?? string.Empty,
                    l.DestinationId,
                    l.IsDestinationWarehouse,
                    l.DestinationCode,
                    l.DestinationName,
                    l.DestinationPosition,
                    l.ProductId,
                    l.Sku,
                    l.ProductName,
                    l.RequestedQuantity,
                    l.Allocations.Select(a => new WarehouseAllocation(
                        a.WarehouseId,
                        a.WarehouseCode,
                        a.WarehousePosition,
                        a.Quantity,
                        a.Lots.Select(lot => new LotAllocation(lot.LotId, lot.LotCode, lot.Quantity)).ToList()
                    )).ToList(),
                    l.AllocatedQuantity,
                    l.FullySourced
                )).ToList(),
                opt.InvolvedWarehouseIds
            ))
            .ToList();
    }

    public Task<DemandSourcingResult> PlanDemandSourcingAsync(
        IReadOnlyList<SourcingDemandInput> demands,
        string sourceKind,
        CancellationToken ct = default)
        => AllocateDemandsCoreAsync(demands, sourceKind, includeEmptyDefaultOption: false, ct);

    private async Task<DemandSourcingResult> AllocateDemandsCoreAsync(
        IReadOnlyList<SourcingDemandInput> demands,
        string sourceKind,
        bool includeEmptyDefaultOption,
        CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);

        if (demands.Count == 0 && !includeEmptyDefaultOption)
        {
            return new DemandSourcingResult(
                today,
                sourceKind,
                _options.WarehouseSafetyStockPct,
                FullySourced: true,
                Array.Empty<DemandLineSourcingDto>(),
                Array.Empty<DemandSourcingOptionDto>()
            );
        }

        var warehouses = await _db.Locations
            .AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive)
            .OrderBy(l => l.Code)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);

        var warehouseIds = warehouses.Select(w => w.Id).ToHashSet();

        var balances = warehouseIds.Count == 0
            ? []
            : await _db.StockBalances
                .AsNoTracking()
                .Where(b => b.OrganizationId == DemoOrganization.Id && warehouseIds.Contains(b.LocationId))
                .ToListAsync(ct);

        var lotIds = balances.Select(b => b.LotId).Distinct().ToHashSet();
        var lots = lotIds.Count == 0
            ? new Dictionary<Guid, Lot>()
            : await _db.Lots
                .AsNoTracking()
                .Where(l => l.OrganizationId == DemoOrganization.Id && lotIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, ct);

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, ct);

        var allPoints = new List<GeoPoint>();
        foreach (var w in warehouses)
        {
            if (w.Position != null)
            {
                allPoints.Add(new GeoPoint((decimal)w.Position.Y, (decimal)w.Position.X));
            }
        }
        foreach (var d in demands)
        {
            allPoints.Add(d.DestinationPosition);
        }

        var distanceMatrix = await _distanceProvider.MatrixAsync(allPoints, ct);

        // Saldo neto utilizable por (bodega, lote) de lotes no bloqueados y no vencidos
        var availableStockByWarehouseLot = balances
            .Where(b => lots.TryGetValue(b.LotId, out var lot) && !lot.IsBlocked && (lot.ExpiresOn == null || lot.ExpiresOn > today))
            .GroupBy(b => (b.LocationId, b.LotId))
            .ToDictionary(
                g => g.Key,
                g => Math.Max(0m, g.Sum(b => b.QuantityOnHand) - g.Sum(b => b.QuantityReserved)));

        // Presupuesto abastecible por (bodega, producto): descuenta stock de seguridad UNA SOLA VEZ por bodega y producto
        var assignableRemainingByWarehouseProduct = availableStockByWarehouseLot
            .Where(kv => kv.Value > 0m && lots.ContainsKey(kv.Key.LotId))
            .GroupBy(kv => (WarehouseId: kv.Key.LocationId, ProductId: lots[kv.Key.LotId].ProductId))
            .ToDictionary(
                g => g.Key,
                g => ComputeAvailableQuantity(g.Sum(x => x.Value), _options.WarehouseSafetyStockPct));

        var sourcedLinesOptionA = new List<DemandLineSourcingDto>(demands.Count);
        bool splitOccurred = false;

        foreach (var demand in demands)
        {
            products.TryGetValue(demand.ProductId, out var prod);
            decimal requested = demand.RequestedQuantity;

            var rankedWarehouses = warehouses
                .Where(w => w.Position != null)
                .Select(w =>
                {
                    var wPos = new GeoPoint((decimal)w.Position!.Y, (decimal)w.Position.X);
                    var legKey = (wPos.Round(), demand.DestinationPosition.Round());
                    decimal km = distanceMatrix.TryGetValue(legKey, out var leg)
                        ? leg.Km
                        : DistanceProvider.HaversineDistanceKm(wPos, demand.DestinationPosition);
                    return new { Warehouse = w, Position = wPos, DistanceKm = Math.Round(km, 3) };
                })
                .OrderBy(x => x.DistanceKm)
                .ThenBy(x => x.Warehouse.Code, StringComparer.Ordinal)
                .ThenBy(x => x.Warehouse.Id)
                .ToList();

            var allocationsA = new List<WarehouseAllocationDto>();
            decimal remainingNeeded = requested;

            foreach (var rw in rankedWarehouses)
            {
                if (remainingNeeded <= 0m) break;

                var whProdKey = (rw.Warehouse.Id, demand.ProductId);
                if (!assignableRemainingByWarehouseProduct.TryGetValue(whProdKey, out var whAssignable) || whAssignable <= 0m)
                {
                    continue;
                }

                var whLots = lots.Values
                    .Where(l => l.ProductId == demand.ProductId && !l.IsBlocked && (l.ExpiresOn == null || l.ExpiresOn > today))
                    .OrderBy(l => l.ExpiresOn ?? DateOnly.MaxValue)
                    .ThenBy(l => l.Code, StringComparer.Ordinal)
                    .ThenBy(l => l.Id)
                    .ToList();

                var lotAllocations = new List<LotAllocationDto>();
                decimal allocatedFromThisWh = 0m;

                foreach (var lot in whLots)
                {
                    if (remainingNeeded <= 0m || whAssignable <= 0m) break;

                    var key = (rw.Warehouse.Id, lot.Id);
                    if (!availableStockByWarehouseLot.TryGetValue(key, out var lotAvail) || lotAvail <= 0m)
                    {
                        continue;
                    }

                    decimal take = Math.Min(remainingNeeded, Math.Min(lotAvail, whAssignable));
                    if (take <= 0m) continue;

                    lotAllocations.Add(new LotAllocationDto(lot.Id, lot.Code, lot.ExpiresOn, take));
                    availableStockByWarehouseLot[key] = lotAvail - take;
                    whAssignable -= take;
                    assignableRemainingByWarehouseProduct[whProdKey] = whAssignable;
                    remainingNeeded -= take;
                    allocatedFromThisWh += take;
                }

                if (allocatedFromThisWh > 0m)
                {
                    allocationsA.Add(new WarehouseAllocationDto(
                        rw.Warehouse.Id,
                        rw.Warehouse.Code,
                        rw.Warehouse.Name,
                        rw.DistanceKm,
                        rw.Position,
                        allocatedFromThisWh,
                        lotAllocations
                    ));
                }
            }

            if (allocationsA.Count > 1)
            {
                splitOccurred = true;
            }

            decimal totalAlloc = allocationsA.Sum(a => a.Quantity);
            decimal shortage = Math.Max(0m, requested - totalAlloc);
            bool fullySourced = totalAlloc >= requested;

            sourcedLinesOptionA.Add(new DemandLineSourcingDto(
                demand.OrderId,
                demand.OrderNumber,
                demand.SourceKind,
                demand.DestinationId,
                demand.IsDestinationWarehouse,
                demand.DestinationCode,
                demand.DestinationName,
                demand.DestinationPosition,
                demand.ProductId,
                prod?.Sku ?? "SKU",
                prod?.Name ?? "Producto",
                prod?.BaseUnit ?? "U",
                requested,
                totalAlloc,
                totalAlloc,
                shortage,
                fullySourced,
                demand.RiskLevel,
                demand.CoverageDays,
                allocationsA
            ));
        }

        var involvedWarehousesA = sourcedLinesOptionA
            .SelectMany(l => l.Allocations.Select(a => a.WarehouseId))
            .Distinct()
            .ToList();

        bool allFullySourced = sourcedLinesOptionA.All(l => l.FullySourced);

        var optionA = new DemandSourcingOptionDto(
            "A",
            "Viaje Multi-Recogida Consolidado",
            splitOccurred
                ? "El vehículo pasará recogiendo el producto por las bodegas con stock antes de proceder a la entrega."
                : "Recogida directa en la bodega más cercana con stock completo.",
            IsRecommended: true,
            allFullySourced,
            sourcedLinesOptionA,
            involvedWarehousesA
        );

        var optionsList = new List<DemandSourcingOptionDto> { optionA };

        if (splitOccurred)
        {
            var optionB = new DemandSourcingOptionDto(
                "B",
                "Dos Entregas Escalonadas (Mañana y Tarde)",
                "Entrega inmediata del stock de la bodega más cercana en el primer turno, y despacho posterior del remanente desde la segunda bodega.",
                IsRecommended: false,
                allFullySourced,
                sourcedLinesOptionA,
                involvedWarehousesA
            );
            optionsList.Add(optionB);
        }

        return new DemandSourcingResult(
            today,
            sourceKind,
            _options.WarehouseSafetyStockPct,
            allFullySourced,
            sourcedLinesOptionA,
            optionsList
        );
    }
}
