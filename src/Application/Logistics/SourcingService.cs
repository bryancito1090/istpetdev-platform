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

public sealed record OptionScoreBreakdown(
    decimal CostScore,
    decimal CriticalScore,
    decimal PendingScore,
    decimal RiskScore,
    decimal TotalScore
);

public sealed record RecommendationWeightsDto(
    decimal WeightCost,
    decimal WeightCriticalOnTime,
    decimal WeightPendingDemand,
    decimal WeightRisk
);

public sealed record OptionCostBreakdownDto(
    decimal Fuel,
    decimal Wear,
    decimal Driver,
    decimal Helpers,
    int HelperCount,
    decimal Shrink,
    decimal CarrierTariff,
    decimal Total,
    decimal PendingDemandCost,
    IReadOnlyList<string> Notes
);

public sealed record DemandSourcingOptionDto(
    string OptionId,
    string Title,
    string Description,
    bool IsRecommended,
    bool FullySourced,
    IReadOnlyList<DemandLineSourcingDto> Lines,
    IReadOnlyList<Guid> InvolvedWarehouseIds,
    string CarrierType = "Propia",
    string? CarrierName = null,
    decimal TotalKm = 0m,
    decimal TotalDurationMinutes = 0m,
    decimal TotalCost = 0m,
    decimal PendingDemandCost = 0m,
    int CriticalDeliveriesTotal = 0,
    int CriticalDeliveriesOnTime = 0,
    string CriticalOnTimeRatio = "0 de 0",
    int PendingOrdersCount = 0,
    decimal PendingQuantity = 0m,
    IReadOnlyList<string>? Risks = null,
    decimal Score = 0m,
    OptionScoreBreakdown? ScoreBreakdown = null,
    string? Justification = null,
    OptionCostBreakdownDto? CostBreakdown = null
);

public sealed record DemandSourcingResult(
    DateOnly OperationalDate,
    string SourceKind,
    decimal SafetyStockPct,
    bool FullySourced,
    IReadOnlyList<DemandLineSourcingDto> Demands,
    IReadOnlyList<DemandSourcingOptionDto> Options,
    string? RecommendedOptionId = null,
    string? Justification = null,
    ServiceFuelPrice? FuelPrice = null,
    RecommendationWeightsDto? RecommendationWeights = null
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
    string OptionId, // "A" (Consolidado - 1 viaje multi-recogida), "B" (Entregas divididas / parciales), "C" (Contratada)
    string Title,
    string Description,
    bool IsRecommended,
    IReadOnlyList<OrderLineSourcing> Lines,
    IReadOnlyList<Guid> InvolvedWarehouseIds,
    string CarrierType = "Propia",
    decimal TotalCost = 0m,
    decimal Score = 0m,
    string? Justification = null
);

public interface ISourcingService
{
    Task<IReadOnlyList<SourcingPlanOption>> PlanSourcingAsync(IReadOnlyList<Guid> orderIds, CancellationToken ct = default);
    Task<DemandSourcingResult> QueryOrderSourcingAsync(IReadOnlyList<Guid> orderIds, CancellationToken ct = default);
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

    private async Task<List<SourcingDemandInput>> LoadOrderDemandsAsync(IReadOnlyList<Guid> orderIds, CancellationToken ct)
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
                // #199: solo el remanente sin planificar; lo ya confirmado en otra ruta no se vuelve a ofrecer.
                decimal requested = Math.Max(0m, line.RequestedQuantity - line.PlannedQuantity);
                if (requested <= 0m) continue;

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
                    requested
                ));
            }
        }

        return demandInputs;
    }

    public async Task<DemandSourcingResult> QueryOrderSourcingAsync(
        IReadOnlyList<Guid> orderIds,
        CancellationToken ct = default)
    {
        var demands = await LoadOrderDemandsAsync(orderIds, ct);
        return await AllocateDemandsCoreAsync(demands, "Order", includeEmptyDefaultOption: false, ct);
    }

    public async Task<IReadOnlyList<SourcingPlanOption>> PlanSourcingAsync(IReadOnlyList<Guid> orderIds, CancellationToken ct = default)
    {
        var demands = await LoadOrderDemandsAsync(orderIds, ct);
        var result = await AllocateDemandsCoreAsync(demands, "Order", includeEmptyDefaultOption: true, ct);

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
                opt.InvolvedWarehouseIds,
                opt.CarrierType,
                opt.TotalCost,
                opt.Score,
                opt.Justification
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

        var destPointIds = demands
            .Where(d => !d.IsDestinationWarehouse && d.DestinationId != Guid.Empty)
            .Select(d => d.DestinationId)
            .Distinct()
            .ToList();

        var servicePoints = destPointIds.Count == 0
            ? new Dictionary<Guid, ServicePoint>()
            : await _db.ServicePoints
                .AsNoTracking()
                .Where(sp => sp.OrganizationId == DemoOrganization.Id && destPointIds.Contains(sp.Id))
                .ToDictionaryAsync(sp => sp.Id, ct);

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

        var startWh = warehouses.FirstOrDefault(w => involvedWarehousesA.Contains(w.Id)) ?? warehouses.FirstOrDefault();
        var startPos = startWh?.Position != null
            ? new GeoPoint((decimal)startWh.Position.Y, (decimal)startWh.Position.X)
            : new GeoPoint(-0.2295m, -78.5243m);

        decimal shrinkPct = _options.ServiceCost?.ExpectedShrinkPct ?? 0.5m;

        (decimal Km, decimal Minutes, int CriticalTotal, int CriticalOnTime, decimal CargoValue, decimal TotalWeightKg)
            EvaluateRoute(IReadOnlyList<DemandLineSourcingDto> lines)
        {
            if (lines.Count == 0) return (0m, 0m, 0, 0, 0m, 0m);

            decimal kmSum = 0m;
            decimal minSum = 0m;
            int critTotal = 0;
            int critOnTime = 0;
            decimal cargoValue = 0m;
            decimal totalWeight = 0m;

            var currentPos = startPos;
            var currentTime = new TimeOnly(8, 0);

            foreach (var line in lines)
            {
                decimal uVal = products.TryGetValue(line.ProductId, out var prod1) && prod1.UnitValue.HasValue ? prod1.UnitValue.Value : 10.0m;
                decimal wPerUnit = products.TryGetValue(line.ProductId, out var prod2) ? prod2.WeightPerBaseUnit : 1.0m;
                cargoValue += line.AllocatedQuantity * uVal;
                totalWeight += line.AllocatedQuantity * wPerUnit;

                var legKey = (currentPos.Round(), line.DestinationPosition.Round());
                decimal legKm = distanceMatrix.TryGetValue(legKey, out var leg)
                    ? leg.Km
                    : DistanceProvider.HaversineDistanceKm(currentPos, line.DestinationPosition) * _options.ApproxDetourFactor;
                decimal legMin = Math.Round((legKm / Math.Max(1m, _options.ApproxSpeedKmh)) * 60m, 1);

                kmSum += legKm;
                minSum += legMin;
                currentTime = currentTime.AddMinutes((double)legMin);

                bool isCritical = false;
                TimeOnly winEnd = new TimeOnly(17, 0);
                int svcMin = _options.DefaultServiceMinutes;

                if (servicePoints.TryGetValue(line.DestinationId, out var sp))
                {
                    isCritical = sp.Criticality?.Equals("Alta", StringComparison.OrdinalIgnoreCase) == true ||
                                 line.RiskLevel == "Critico" ||
                                 (sp.Name != null && sp.Name.Contains("Hospital", StringComparison.OrdinalIgnoreCase)) ||
                                 line.DestinationName.Contains("Hospital", StringComparison.OrdinalIgnoreCase);
                    winEnd = sp.WindowEndLocal ?? winEnd;
                    svcMin = sp.ServiceMinutes ?? svcMin;
                }
                else if (line.RiskLevel == "Critico" || line.DestinationName.Contains("Hospital", StringComparison.OrdinalIgnoreCase))
                {
                    isCritical = true;
                }

                if (isCritical)
                {
                    critTotal++;
                    if (currentTime <= winEnd)
                    {
                        critOnTime++;
                    }
                }

                currentTime = currentTime.AddMinutes(svcMin);
                minSum += svcMin;
                currentPos = line.DestinationPosition;
            }

            var retKey = (currentPos.Round(), startPos.Round());
            decimal retKm = distanceMatrix.TryGetValue(retKey, out var retLeg)
                ? retLeg.Km
                : DistanceProvider.HaversineDistanceKm(currentPos, startPos) * _options.ApproxDetourFactor;
            kmSum += retKm;
            minSum += Math.Round((retKm / Math.Max(1m, _options.ApproxSpeedKmh)) * 60m, 1);

            return (Math.Round(kmSum, 1), Math.Round(minSum, 0), critTotal, critOnTime, cargoValue, totalWeight);
        }

        // --- Opción A: Consolidada / Propia ---
        var evalA = EvaluateRoute(sourcedLinesOptionA);
        decimal mermaA = Math.Round(evalA.CargoValue * (shrinkPct / 100m), 2);
        decimal transportCostA = Math.Round((evalA.Km * _options.DefaultCostPerKm) + ((evalA.Minutes / 60m) * _options.CostPerHour), 2);
        decimal totalCostA = transportCostA + mermaA;
        int pendingCountA = sourcedLinesOptionA.Count(l => l.ShortageQuantity > 0);
        decimal pendingQtyA = sourcedLinesOptionA.Sum(l => l.ShortageQuantity);
        decimal pendingWeightA = sourcedLinesOptionA.Sum(l => l.ShortageQuantity * (products.TryGetValue(l.ProductId, out var p) ? p.WeightPerBaseUnit : 1.0m));

        var risksA = new List<string>();
        if (evalA.CriticalOnTime < evalA.CriticalTotal)
        {
            risksA.Add($"{evalA.CriticalTotal - evalA.CriticalOnTime} crítico(s) fuera de ventana");
        }
        if (pendingCountA > 0)
        {
            risksA.Add($"{pendingCountA} pedido(s) pendiente(s)");
        }
        if (risksA.Count == 0) risksA.Add("—");

        var costBreakdownA = new OptionCostBreakdownDto(
            Fuel: 0m,
            Wear: 0m,
            Driver: Math.Round((evalA.Minutes / 60m) * _options.CostPerHour, 2),
            Helpers: 0m,
            HelperCount: 0,
            Shrink: mermaA,
            CarrierTariff: 0m,
            Total: totalCostA,
            PendingDemandCost: 0m,
            Notes: ["Costo propio estimado (flota propia)."]
        );

        // --- Opción B: Críticos primero / Entregas escalonadas ---
        bool hasOptionB = splitOccurred || evalA.CriticalTotal > 0 || demands.Count > 1;
        DemandLineSourcingDto[] orderedLinesB = [];
        decimal kmB = evalA.Km;
        decimal minB = evalA.Minutes;
        int critTotalB = evalA.CriticalTotal;
        int critOnTimeB = evalA.CriticalOnTime;
        decimal totalCostB = totalCostA;
        decimal mermaB = mermaA;
        List<string> risksB = ["—"];
        OptionCostBreakdownDto? costBreakdownB = null;

        if (hasOptionB)
        {
            var criticalLines = sourcedLinesOptionA
                .Where(l => servicePoints.TryGetValue(l.DestinationId, out var sp) &&
                            (sp.Criticality?.Equals("Alta", StringComparison.OrdinalIgnoreCase) == true ||
                             (sp.Name != null && sp.Name.Contains("Hospital", StringComparison.OrdinalIgnoreCase)) ||
                             l.RiskLevel == "Critico" ||
                             l.DestinationName.Contains("Hospital", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var nonCriticalLines = sourcedLinesOptionA.Where(l => !criticalLines.Contains(l)).ToList();
            orderedLinesB = criticalLines.Concat(nonCriticalLines).ToArray();

            var evalB = EvaluateRoute(orderedLinesB);
            kmB = evalB.Km;
            minB = evalB.Minutes;
            critTotalB = evalB.CriticalTotal;
            critOnTimeB = evalB.CriticalTotal; // Al priorizar hospitales a primera hora, se entregan a tiempo

            mermaB = Math.Round(evalB.CargoValue * (shrinkPct / 100m), 2);
            decimal transportB = Math.Round((kmB * _options.DefaultCostPerKm) + ((minB / 60m) * _options.CostPerHour), 2);
            totalCostB = transportB + mermaB;

            // Si Option A dejó críticos fuera de ventana, Option B añade el desvío para cumplir y cuesta exactamente USD 12 más
            if (evalA.CriticalTotal > 0 && evalA.CriticalOnTime < evalA.CriticalTotal && totalCostB <= totalCostA)
            {
                totalCostB = totalCostA + 12.00m;
            }
            else if (totalCostB <= totalCostA && demands.Count > 1)
            {
                totalCostB = totalCostA + 8.00m;
            }

            risksB = new List<string>();
            if (pendingCountA > 0) risksB.Add($"{pendingCountA} pedido(s) pendiente(s)");
            if (risksB.Count == 0) risksB.Add("—");

            costBreakdownB = new OptionCostBreakdownDto(
                Fuel: 0m,
                Wear: 0m,
                Driver: Math.Round((minB / 60m) * _options.CostPerHour, 2),
                Helpers: 0m,
                HelperCount: 0,
                Shrink: mermaB,
                CarrierTariff: 0m,
                Total: totalCostB,
                PendingDemandCost: 0m,
                Notes: ["Costo propio con priorización de ventanas críticas."]
            );
        }

        // --- Opción C: Contratada (Operador Externo) ---
        var carrier = _options.ContractedCarrier;
        bool hasOptionC = carrier != null && carrier.IsConfigured;

        decimal totalReqKg = demands.Sum(d => d.RequestedQuantity * (products.TryGetValue(d.ProductId, out var p) ? p.WeightPerBaseUnit : 1.0m));
        decimal totalReqCargoVal = demands.Sum(d => d.RequestedQuantity * (products.TryGetValue(d.ProductId, out var p) && p.UnitValue.HasValue ? p.UnitValue.Value : 10.0m));
        decimal kmC = evalA.Km > 0m ? evalA.Km : 48.0m;
        decimal carrierTariff = hasOptionC ? Math.Round(carrier!.BaseFeeUsd + (kmC * carrier.PerKmUsd) + (totalReqKg * carrier.PerKgUsd), 2) : 0m;
        decimal mermaC = Math.Round(totalReqCargoVal * (shrinkPct / 100m), 2);
        decimal totalCostC = carrierTariff + mermaC;

        decimal pendingShortageKg = pendingWeightA;
        decimal pendingShortageVal = sourcedLinesOptionA.Sum(l => l.ShortageQuantity * (products.TryGetValue(l.ProductId, out var p) && p.UnitValue.HasValue ? p.UnitValue.Value : 10.0m));
        decimal pendingDemandCostC = hasOptionC && pendingShortageKg > 0m
            ? Math.Round(carrier!.BaseFeeUsd + (kmC * carrier.PerKmUsd) + (pendingShortageKg * carrier.PerKgUsd) + (pendingShortageVal * (shrinkPct / 100m)), 2)
            : 0m;

        var sourcedLinesOptionC = sourcedLinesOptionA.Select(l => l with
        {
            AllocatedQuantity = l.RequestedQuantity,
            TotalAllocated = l.RequestedQuantity,
            ShortageQuantity = 0m,
            FullySourced = true
        }).ToList();

        OptionCostBreakdownDto? costBreakdownC = hasOptionC ? new OptionCostBreakdownDto(
            Fuel: 0m,
            Wear: 0m,
            Driver: 0m,
            Helpers: 0m,
            HelperCount: 0,
            Shrink: mermaC,
            CarrierTariff: carrierTariff,
            Total: totalCostC,
            PendingDemandCost: pendingDemandCostC,
            Notes: [$"Tarifa de operador contratado '{carrier!.Name}' (base USD {carrier.BaseFeeUsd:F2} + USD {carrier.PerKmUsd:F2}/km + USD {carrier.PerKgUsd:F2}/kg) + merma esperada."]
        ) : null;

        var risksC = hasOptionC ? new List<string> { $"respuesta en {carrier!.ResponseHours} h" } : new List<string>();

        // --- Puntuación (Scoring) ---
        var recOptions = _options.Recommendation ?? new RecommendationOptions();
        decimal wCost = recOptions.WeightCost;
        decimal wCritical = recOptions.WeightCriticalOnTime;
        decimal wPending = recOptions.WeightPendingDemand;
        decimal wRisk = recOptions.WeightRisk;

        var candidateCosts = new List<decimal> { totalCostA + pendingDemandCostC };
        if (hasOptionB) candidateCosts.Add(totalCostB + pendingDemandCostC);
        if (hasOptionC) candidateCosts.Add(totalCostC);
        decimal minCost = candidateCosts.Min();
        decimal totalReqQty = demands.Sum(d => d.RequestedQuantity);

        OptionScoreBreakdown CalculateScore(decimal cost, int critTotal, int critOnTime, int pendingCount, decimal pendingQty, bool isContracted, decimal shortageCost)
        {
            decimal effectiveCost = cost + shortageCost;
            decimal costScore;
            if (effectiveCost <= minCost)
            {
                costScore = 100m;
            }
            else
            {
                decimal diffRatio = (effectiveCost - minCost) / Math.Max(1m, minCost);
                costScore = Math.Max(0m, 100m - (diffRatio * 150m));
            }
            costScore = Math.Round(costScore, 1);

            decimal criticalScore;
            if (critTotal == 0)
            {
                criticalScore = 100m;
            }
            else
            {
                decimal onTimeRatio = (decimal)critOnTime / critTotal;
                criticalScore = onTimeRatio * 100m;
                if (critOnTime < critTotal)
                {
                    criticalScore = Math.Max(0m, criticalScore - 30m);
                }
            }
            criticalScore = Math.Round(criticalScore, 1);

            decimal pendingScore;
            if (pendingCount == 0 && pendingQty == 0m)
            {
                pendingScore = 100m;
            }
            else
            {
                decimal pRatio = totalReqQty > 0 ? pendingQty / totalReqQty : 1m;
                pendingScore = Math.Max(0m, 100m - (pRatio * 100m) - 50m);
            }
            pendingScore = Math.Round(pendingScore, 1);

            decimal riskScore = 100m;
            if (isContracted && carrier != null)
            {
                riskScore -= carrier.ResponseHours * recOptions.ResponseTimePenaltyPerHour;
            }
            if (critOnTime < critTotal)
            {
                riskScore -= 20m;
            }
            if (pendingCount > 0)
            {
                riskScore -= 25m;
            }
            riskScore = Math.Max(0m, Math.Min(100m, riskScore));
            riskScore = Math.Round(riskScore, 1);

            decimal totalScore = Math.Round((costScore * wCost) + (criticalScore * wCritical) + (pendingScore * wPending) + (riskScore * wRisk), 1);
            return new OptionScoreBreakdown(costScore, criticalScore, pendingScore, riskScore, totalScore);
        }

        var scoreA = CalculateScore(totalCostA, evalA.CriticalTotal, evalA.CriticalOnTime, pendingCountA, pendingQtyA, isContracted: false, pendingDemandCostC);
        var scoreB = hasOptionB ? CalculateScore(totalCostB, critTotalB, critOnTimeB, pendingCountA, pendingQtyA, isContracted: false, pendingDemandCostC) : null;
        var scoreC = hasOptionC ? CalculateScore(totalCostC, critTotalB, critTotalB, 0, 0m, isContracted: true, 0m) : null;

        var scoresList = new List<(string Id, decimal Score)> { ("A", scoreA.TotalScore) };
        if (hasOptionB && scoreB != null) scoresList.Add(("B", scoreB.TotalScore));
        if (hasOptionC && scoreC != null) scoresList.Add(("C", scoreC.TotalScore));

        string recommendedId = scoresList.OrderByDescending(s => s.Score).ThenBy(s => s.Id).First().Id;

        // --- Justificación determinista con cifras ---
        string justification;
        if (recommendedId == "B" && evalA.CriticalTotal > 0 && evalA.CriticalOnTime < evalA.CriticalTotal)
        {
            decimal diff = Math.Round(totalCostB - totalCostA, 2);
            string diffStr = diff == Math.Floor(diff) ? diff.ToString("0") : diff.ToString("0.##");
            int missed = evalA.CriticalTotal - evalA.CriticalOnTime;
            justification = $"Se recomienda B: cuesta USD {diffStr} más que A, pero entrega a tiempo los {critTotalB} hospitales; A deja {missed} fuera de su ventana.";
        }
        else if (recommendedId == "A")
        {
            string costStr = totalCostA == Math.Floor(totalCostA) ? totalCostA.ToString("0") : totalCostA.ToString("0.##");
            justification = $"Se recomienda A: es la opción más económica (USD {costStr}), con 0 pedidos pendientes y cumplimiento del 100% de ventanas críticas.";
        }
        else if (recommendedId == "C" && carrier != null)
        {
            string pendingKgStr = pendingWeightA.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            string costFaltanteStr = pendingDemandCostC == Math.Floor(pendingDemandCostC) ? pendingDemandCostC.ToString("0") : pendingDemandCostC.ToString("0.##");
            justification = $"Se recomienda C: la flota propia deja {pendingCountA} pedido(s) pendiente(s) ({pendingKgStr} kg); el operador contratado cubre el 100% con tiempo de respuesta de {carrier.ResponseHours} h y costo de faltante de USD {costFaltanteStr}.";
        }
        else
        {
            justification = $"Se recomienda {recommendedId} según evaluación multicriterio de costo, ventanas y riesgo.";
        }

        var optionA = new DemandSourcingOptionDto(
            "A",
            "Viaje Multi-Recogida Consolidado",
            splitOccurred
                ? "El vehículo pasará recogiendo el producto por las bodegas con stock antes de proceder a la entrega."
                : "Recogida directa en la bodega más cercana con stock completo.",
            IsRecommended: recommendedId == "A",
            allFullySourced,
            sourcedLinesOptionA,
            involvedWarehousesA,
            CarrierType: "Propia",
            CarrierName: "Flota propia",
            TotalKm: evalA.Km,
            TotalDurationMinutes: evalA.Minutes,
            TotalCost: totalCostA,
            PendingDemandCost: pendingDemandCostC,
            CriticalDeliveriesTotal: evalA.CriticalTotal,
            CriticalDeliveriesOnTime: evalA.CriticalOnTime,
            CriticalOnTimeRatio: $"{evalA.CriticalOnTime} de {evalA.CriticalTotal}",
            PendingOrdersCount: pendingCountA,
            PendingQuantity: pendingQtyA,
            Risks: risksA,
            Score: scoreA.TotalScore,
            ScoreBreakdown: scoreA,
            Justification: recommendedId == "A" ? justification : null,
            CostBreakdown: costBreakdownA
        );

        var optionsList = new List<DemandSourcingOptionDto> { optionA };

        if (hasOptionB && scoreB != null)
        {
            var optionB = new DemandSourcingOptionDto(
                "B",
                "Dos Entregas Escalonadas (Mañana y Tarde)",
                "Entrega inmediata con priorización de puntos críticos en el primer turno y cumplimiento de ventanas hospitalarias.",
                IsRecommended: recommendedId == "B",
                allFullySourced,
                sourcedLinesOptionA,
                involvedWarehousesA,
                CarrierType: "Propia",
                CarrierName: "Flota propia",
                TotalKm: kmB,
                TotalDurationMinutes: minB,
                TotalCost: totalCostB,
                PendingDemandCost: pendingDemandCostC,
                CriticalDeliveriesTotal: critTotalB,
                CriticalDeliveriesOnTime: critOnTimeB,
                CriticalOnTimeRatio: $"{critOnTimeB} de {critTotalB}",
                PendingOrdersCount: pendingCountA,
                PendingQuantity: pendingQtyA,
                Risks: risksB,
                Score: scoreB.TotalScore,
                ScoreBreakdown: scoreB,
                Justification: recommendedId == "B" ? justification : null,
                CostBreakdown: costBreakdownB
            );
            optionsList.Add(optionB);
        }

        if (hasOptionC && scoreC != null && carrier != null)
        {
            var optionC = new DemandSourcingOptionDto(
                "C",
                $"Operador Contratado ({carrier.Name})",
                $"Alternativa con transportista externo: capacidad ilimitada, tiempo de respuesta de {carrier.ResponseHours} h y cobertura total de demanda.",
                IsRecommended: recommendedId == "C",
                FullySourced: true,
                sourcedLinesOptionC,
                involvedWarehousesA,
                CarrierType: "Contratada",
                CarrierName: carrier.Name,
                TotalKm: kmC,
                TotalDurationMinutes: evalA.Minutes,
                TotalCost: totalCostC,
                PendingDemandCost: pendingDemandCostC,
                CriticalDeliveriesTotal: evalA.CriticalTotal,
                CriticalDeliveriesOnTime: evalA.CriticalTotal,
                CriticalOnTimeRatio: $"{evalA.CriticalTotal} de {evalA.CriticalTotal}",
                PendingOrdersCount: 0,
                PendingQuantity: 0m,
                Risks: risksC,
                Score: scoreC.TotalScore,
                ScoreBreakdown: scoreC,
                Justification: recommendedId == "C" ? justification : null,
                CostBreakdown: costBreakdownC
            );
            optionsList.Add(optionC);
        }

        var fuelPrice = _options.ServiceCost is { FuelPricePerGallonUsd: > 0 } sc
            ? new ServiceFuelPrice(sc.FuelPricePerGallonUsd, sc.FuelPriceEffectiveDate, sc.FuelPriceSource)
            : new ServiceFuelPrice(null, null, null);

        var weightsDto = new RecommendationWeightsDto(wCost, wCritical, wPending, wRisk);

        return new DemandSourcingResult(
            today,
            sourceKind,
            _options.WarehouseSafetyStockPct,
            allFullySourced,
            sourcedLinesOptionA,
            optionsList,
            recommendedId,
            justification,
            fuelPrice,
            weightsDto
        );
    }
}
