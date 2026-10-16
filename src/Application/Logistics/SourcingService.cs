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

    public async Task<IReadOnlyList<SourcingPlanOption>> PlanSourcingAsync(IReadOnlyList<Guid> orderIds, CancellationToken ct = default)
    {
        var today = await _clock.TodayAsync(ct);

        // 1. Cargar pedidos y líneas
        var orders = await _db.DeliveryOrders
            .Where(o => o.OrganizationId == DemoOrganization.Id && orderIds.Contains(o.Id))
            .ToListAsync(ct);

        var lines = await _db.DeliveryOrderLines
            .Where(l => l.OrganizationId == DemoOrganization.Id && orderIds.Contains(l.DeliveryOrderId))
            .ToListAsync(ct);

        // 2. Cargar bodegas y puntos de servicio
        var warehouses = await _db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive)
            .ToListAsync(ct);

        var warehouseIds = warehouses.Select(w => w.Id).ToHashSet();

        var servicePoints = await _db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(sp => sp.Id, ct);

        // 3. Cargar existencias por bodega y producto
        var balances = await _db.StockBalances
            .Where(b => b.OrganizationId == DemoOrganization.Id && warehouseIds.Contains(b.LocationId))
            .ToListAsync(ct);

        var lotIds = balances.Select(b => b.LotId).Distinct().ToHashSet();
        var lots = await _db.Lots
            .Where(l => l.OrganizationId == DemoOrganization.Id && lotIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, ct);

        var products = await _db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, ct);

        // 4. Preparar destinos de pedidos con coordenadas
        var orderDestinations = new Dictionary<Guid, (Guid Id, string Code, string Name, bool IsWh, GeoPoint Pos)>();
        foreach (var ord in orders)
        {
            if (ord.DestinationServicePointId.HasValue && servicePoints.TryGetValue(ord.DestinationServicePointId.Value, out var sp))
            {
                var pos = sp.Position != null
                    ? new GeoPoint((decimal)sp.Position.Y, (decimal)sp.Position.X)
                    : new GeoPoint(-0.2295m, -78.5243m);
                orderDestinations[ord.Id] = (sp.Id, sp.Code, sp.Name, false, pos);
            }
            else if (ord.DestinationLocationId.HasValue)
            {
                var wh = warehouses.FirstOrDefault(w => w.Id == ord.DestinationLocationId.Value);
                var pos = wh?.Position != null
                    ? new GeoPoint((decimal)wh.Position.Y, (decimal)wh.Position.X)
                    : new GeoPoint(-0.2295m, -78.5243m);
                orderDestinations[ord.Id] = (wh?.Id ?? Guid.Empty, wh?.Code ?? "WH", wh?.Name ?? "Bodega", true, pos);
            }
            else
            {
                orderDestinations[ord.Id] = (Guid.Empty, "UNK", "Destino Desconocido", false, new GeoPoint(-0.2295m, -78.5243m));
            }
        }

        // Matriz de distancias entre bodegas y destinos
        var allPoints = new List<GeoPoint>();
        foreach (var w in warehouses)
        {
            if (w.Position != null) allPoints.Add(new GeoPoint((decimal)w.Position.Y, (decimal)w.Position.X));
        }
        foreach (var dest in orderDestinations.Values)
        {
            allPoints.Add(dest.Pos);
        }
        var distanceMatrix = await _distanceProvider.MatrixAsync(allPoints, ct);

        // 5. Simular asignación FEFO desde bodegas ordenadas por distancia para cada destino
        var sourcedLinesOptionA = new List<OrderLineSourcing>();
        var sourcedLinesOptionB = new List<OrderLineSourcing>();
        bool splitOccurred = false;

        // Mantener control de stock disponible en memoria por bodega/lote durante la asignación
        var availableStockByWarehouseLot = balances
            .Where(b => lots.TryGetValue(b.LotId, out var lot) && !lot.IsBlocked && (lot.ExpiresOn == null || lot.ExpiresOn > today))
            .ToDictionary(b => (b.LocationId, b.LotId), b => Math.Max(0m, b.QuantityOnHand - b.QuantityReserved));

        foreach (var ord in orders)
        {
            var dest = orderDestinations[ord.Id];
            var ordLines = lines.Where(l => l.DeliveryOrderId == ord.Id).ToList();

            foreach (var line in ordLines)
            {
                products.TryGetValue(line.ProductId, out var prod);
                // #199: solo el remanente sin planificar; lo ya confirmado en otra ruta no se vuelve a ofrecer.
                decimal requested = Math.Max(0m, line.RequestedQuantity - line.PlannedQuantity);
                if (requested <= 0) continue;

                // Ordenar bodegas por distancia por carretera al destino
                var rankedWarehouses = warehouses
                    .Where(w => w.Position != null)
                    .Select(w =>
                    {
                        var wPos = new GeoPoint((decimal)w.Position!.Y, (decimal)w.Position.X);
                        var legKey = (wPos, dest.Pos);
                        decimal km = distanceMatrix.TryGetValue(legKey, out var leg) ? leg.Km : DistanceProvider.HaversineDistanceKm(wPos, dest.Pos);
                        return new { Warehouse = w, Position = wPos, DistanceKm = km };
                    })
                    .OrderBy(x => x.DistanceKm)
                    .ToList();

                var allocationsA = new List<WarehouseAllocation>();
                decimal remainingNeeded = requested;

                foreach (var rw in rankedWarehouses)
                {
                    if (remainingNeeded <= 0) break;

                    // Buscar lotes FEFO para este producto en esta bodega
                    var whLots = lots.Values
                        .Where(l => l.ProductId == line.ProductId && !l.IsBlocked && (l.ExpiresOn == null || l.ExpiresOn > today))
                        .OrderBy(l => l.ExpiresOn ?? DateOnly.MaxValue)
                        .ThenBy(l => l.Code, StringComparer.Ordinal)
                        .ToList();

                    var lotAllocations = new List<LotAllocation>();
                    decimal allocatedFromThisWh = 0m;

                    foreach (var lot in whLots)
                    {
                        if (remainingNeeded <= 0) break;
                        var key = (rw.Warehouse.Id, lot.Id);
                        if (!availableStockByWarehouseLot.TryGetValue(key, out var avail) || avail <= 0) continue;

                        decimal take = Math.Min(remainingNeeded, avail);
                        lotAllocations.Add(new LotAllocation(lot.Id, lot.Code, take));
                        availableStockByWarehouseLot[key] = avail - take;
                        remainingNeeded -= take;
                        allocatedFromThisWh += take;
                    }

                    if (allocatedFromThisWh > 0)
                    {
                        allocationsA.Add(new WarehouseAllocation(
                            rw.Warehouse.Id,
                            rw.Warehouse.Code,
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
                bool fullySourced = totalAlloc >= requested;

                sourcedLinesOptionA.Add(new OrderLineSourcing(
                    ord.Id,
                    ord.OrderNumber,
                    dest.Id,
                    dest.IsWh,
                    dest.Code,
                    dest.Name,
                    dest.Pos,
                    line.ProductId,
                    prod?.Sku ?? "SKU",
                    prod?.Name ?? "Producto",
                    requested,
                    allocationsA,
                    totalAlloc,
                    fullySourced
                ));
            }
        }

        // Opción A: Unificada (1 viaje que recoge en las bodegas necesarias)
        var involvedWarehousesA = sourcedLinesOptionA
            .SelectMany(l => l.Allocations.Select(a => a.WarehouseId))
            .Distinct()
            .ToList();

        var optionA = new SourcingPlanOption(
            "A",
            "Viaje Multi-Recogida Consolidado",
            splitOccurred
                ? "El vehículo pasará recogiendo el producto por las bodegas con stock antes de proceder a la entrega."
                : "Recogida directa en la bodega más cercana con stock completo.",
            IsRecommended: true,
            sourcedLinesOptionA,
            involvedWarehousesA
        );

        // Opción B: Entregas Escalonadas / Parciales (si hubo división de bodegas)
        var optionsList = new List<SourcingPlanOption> { optionA };

        if (splitOccurred)
        {
            // En Opción B, se planifica para entregar primero lo disponible en la bodega más cercana
            var optionB = new SourcingPlanOption(
                "B",
                "Dos Entregas Escalonadas (Mañana y Tarde)",
                "Entrega inmediata del stock de la bodega más cercana en el primer turno, y despacho posterior del remanente desde la segunda bodega.",
                IsRecommended: false,
                sourcedLinesOptionA, // Mismas asignaciones pero divididas en fases operativas
                involvedWarehousesA
            );
            optionsList.Add(optionB);
        }

        return optionsList;
    }
}
