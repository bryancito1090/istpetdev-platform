using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record WarehouseProductConsumption(
    Guid LocationId,
    string LocationCode,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal TotalConsumedInWindow,
    int WindowDays,
    decimal DailyConsumptionRate,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal AvailableQuantity,
    decimal CoverageDays,
    string ConfidenceLevel // "High", "Low"
);

public interface IWarehouseConsumptionAnalyzer
{
    Task<IReadOnlyList<WarehouseProductConsumption>> AnalyzeAsync(CancellationToken ct = default);
    Task<WarehouseProductConsumption?> AnalyzeProductWarehouseAsync(Guid locationId, Guid productId, CancellationToken ct = default);
}

public sealed class WarehouseConsumptionAnalyzer : IWarehouseConsumptionAnalyzer
{
    private readonly IApplicationDbContext _db;
    private readonly IOperationalClock _clock;
    private readonly LogisticsOptions _options;

    public WarehouseConsumptionAnalyzer(
        IApplicationDbContext db,
        IOperationalClock clock,
        IOptions<LogisticsOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<WarehouseProductConsumption>> AnalyzeAsync(CancellationToken ct = default)
    {
        var today = await _clock.TodayAsync(ct);
        var windowDays = _options.Rebalancing.ConsumptionHistoryDays > 0 ? _options.Rebalancing.ConsumptionHistoryDays : 90;
        var startWindow = today.AddDays(-windowDays);

        // 1. Obtener todas las bodegas activas
        var warehouses = await _db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive)
            .OrderBy(l => l.Code)
            .ToListAsync(ct);

        if (warehouses.Count == 0) return Array.Empty<WarehouseProductConsumption>();

        var warehouseIds = warehouses.Select(w => w.Id).ToHashSet();

        // 2. Obtener puntos de servicio y sus bodegas asignadas
        var servicePoints = await _db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id && sp.IsActive)
            .ToListAsync(ct);

        // Mapa de ServicePointId -> ServingWarehouseLocationId
        // Si no tiene asignado, se asigna temporalmente a la primera bodega o la más cercana
        var defaultWarehouseId = warehouses[0].Id;
        var spToWarehouse = new Dictionary<Guid, Guid>();
        foreach (var sp in servicePoints)
        {
            if (sp.ServingWarehouseLocationId.HasValue && warehouseIds.Contains(sp.ServingWarehouseLocationId.Value))
            {
                spToWarehouse[sp.Id] = sp.ServingWarehouseLocationId.Value;
            }
            else
            {
                // Fallback por proximidad si existen coordenadas
                Guid assignedId = defaultWarehouseId;
                if (sp.Position != null)
                {
                    double minDist = double.MaxValue;
                    foreach (var w in warehouses)
                    {
                        if (w.Position != null)
                        {
                            var dist = Math.Pow(sp.Position.Y - w.Position.Y, 2) + Math.Pow(sp.Position.X - w.Position.X, 2);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                assignedId = w.Id;
                            }
                        }
                    }
                }
                spToWarehouse[sp.Id] = assignedId;
            }
        }

        // 3. Consumos en la ventana
        var consumptions = await _db.Consumptions
            .Where(c => c.OrganizationId == DemoOrganization.Id && c.OccurredOn >= startWindow)
            .ToListAsync(ct);

        // 4. Salidas de inventario directas de bodega (por si hay movimientos sin consumo registrado)
        var stockMovements = await _db.InventoryMovements
            .Where(m => m.OrganizationId == DemoOrganization.Id &&
                        m.FromLocationId.HasValue && warehouseIds.Contains(m.FromLocationId.Value) &&
                        m.OccurredAtUtc >= startWindow.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            .ToListAsync(ct);

        // 5. Stock balances actuales
        var balances = await _db.StockBalances
            .Where(b => b.OrganizationId == DemoOrganization.Id && warehouseIds.Contains(b.LocationId))
            .ToListAsync(ct);

        // 6. Productos activos
        var products = await _db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive)
            .ToDictionaryAsync(p => p.Id, ct);

        var result = new List<WarehouseProductConsumption>();

        foreach (var warehouse in warehouses)
        {
            var spsForWarehouse = spToWarehouse
                .Where(kv => kv.Value == warehouse.Id)
                .Select(kv => kv.Key)
                .ToHashSet();

            foreach (var prod in products.Values)
            {
                // Consumo de puntos asignados
                var consumedSp = consumptions
                    .Where(c => c.ProductId == prod.Id && spsForWarehouse.Contains(c.ServicePointId))
                    .Sum(c => c.Quantity);

                // Si no hay consumos de puntos, usar salidas directas de la bodega
                decimal totalConsumed = consumedSp;
                string confidence = "High";

                if (totalConsumed <= 0)
                {
                    var directOut = stockMovements
                        .Where(m => m.FromLocationId == warehouse.Id)
                        .Join(_db.Lots.Where(l => l.ProductId == prod.Id), m => m.LotId, l => l.Id, (m, l) => m.Quantity)
                        .Sum();

                    totalConsumed = directOut;
                    confidence = directOut > 0 ? "Low" : "Low";
                }

                var dailyRate = windowDays > 0 ? (totalConsumed / windowDays) : 0m;

                var warehouseBalances = balances.Where(b => b.LocationId == warehouse.Id).ToList();
                // Necesitamos unir con Lots para saber qué balance corresponde a este producto
                var lotIdsForProd = await _db.Lots
                    .Where(l => l.OrganizationId == DemoOrganization.Id && l.ProductId == prod.Id && !l.IsBlocked && (l.ExpiresOn == null || l.ExpiresOn > today))
                    .Select(l => l.Id)
                    .ToListAsync(ct);

                var prodBalances = warehouseBalances.Where(b => lotIdsForProd.Contains(b.LotId)).ToList();
                var onHand = prodBalances.Sum(b => b.QuantityOnHand);
                var reserved = prodBalances.Sum(b => b.QuantityReserved);
                var available = Math.Max(0m, onHand - reserved);

                decimal coverageDays = 999m;
                if (dailyRate > 0)
                {
                    coverageDays = Math.Round(available / dailyRate, 1);
                }
                else if (available == 0)
                {
                    coverageDays = 0m;
                }

                result.Add(new WarehouseProductConsumption(
                    warehouse.Id,
                    warehouse.Code,
                    prod.Id,
                    prod.Sku,
                    prod.Name,
                    totalConsumed,
                    windowDays,
                    dailyRate,
                    onHand,
                    reserved,
                    available,
                    coverageDays,
                    confidence
                ));
            }
        }

        return result;
    }

    public async Task<WarehouseProductConsumption?> AnalyzeProductWarehouseAsync(Guid locationId, Guid productId, CancellationToken ct = default)
    {
        var all = await AnalyzeAsync(ct);
        return all.FirstOrDefault(x => x.LocationId == locationId && x.ProductId == productId);
    }
}
