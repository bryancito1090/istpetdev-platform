using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Location = IstpetDev.Domain.Entities.Location;

namespace IstpetDev.Application.Imports;

// Revisión de viabilidad logística del dataset (issue #209): lee el estado planificado en memoria
// y produce hechos deterministas para el dictamen. No persiste datos ni completa valores faltantes.
internal static class FeasibilityAnalyzer
{
    public static FeasibilityReport Analyze(
        ImportContext context,
        IReadOnlyDictionary<ImportEntity, List<ParsedRow>> parsed,
        DateOnly today)
    {
        var report = new FeasibilityReport();
        var demand = DemandBySku(parsed);
        var involvedPoints = InvolvedPoints(context, parsed, report);
        AddProductsWithoutCapacity(context, parsed, report);
        AddWarehousesWithoutFleet(context, parsed, demand, report);
        AddStockShortages(context, demand, today, report);

        var noFleet = demand.Count > 0 && !context.Vehicles.Values.Any(v => v.IsActive && v.MaxWeight > 0);
        var noRoutablePoint = demand.Count > 0 && involvedPoints.Count > 0 && involvedPoints.All(p => p.Position is null);
        report.Verdict = report.StockShortages.Count > 0 || noFleet || noRoutablePoint
            ? FeasibilityVerdicts.Inviable
            : report.HasFindings ? FeasibilityVerdicts.ViableWithLimitations : FeasibilityVerdicts.Viable;
        return report;
    }

    private static Dictionary<string, decimal> DemandBySku(IReadOnlyDictionary<ImportEntity, List<ParsedRow>> parsed)
    {
        var demand = new Dictionary<string, decimal>();
        if (!parsed.TryGetValue(ImportEntity.Consumptions, out var rows)) return demand;
        foreach (var row in rows)
        {
            if (row.Text("sku") is not { } sku) continue;
            demand[sku] = demand.GetValueOrDefault(sku) + (row.Decimal("quantity") ?? 0m);
        }
        return demand;
    }

    private static List<ServicePoint> InvolvedPoints(
        ImportContext context, IReadOnlyDictionary<ImportEntity, List<ParsedRow>> parsed, FeasibilityReport report)
    {
        var rowsByCode = new Dictionary<string, int>();
        if (parsed.TryGetValue(ImportEntity.ServicePoints, out var pointRows))
            foreach (var row in pointRows)
                if (row.Text("code") is { } code) rowsByCode.TryAdd(code, row.Row);
        if (parsed.TryGetValue(ImportEntity.Consumptions, out var consumptionRows))
            foreach (var row in consumptionRows)
                if (row.Text("point_code") is { } code) rowsByCode.TryAdd(code, row.Row);

        var points = new List<ServicePoint>();
        foreach (var (code, row) in rowsByCode.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!context.ServicePoints.TryGetValue(code, out var point)) continue;
            points.Add(point);
            if (point.Position is null) report.UnroutablePoints.Add(new UnroutablePoint(point.Code, point.Name, row));
        }
        return points;
    }

    private static void AddProductsWithoutCapacity(
        ImportContext context, IReadOnlyDictionary<ImportEntity, List<ParsedRow>> parsed, FeasibilityReport report)
    {
        var skus = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entity in new[] { ImportEntity.Products, ImportEntity.Lots, ImportEntity.Stock, ImportEntity.Consumptions })
        {
            if (!parsed.TryGetValue(entity, out var rows)) continue;
            foreach (var row in rows)
                if (row.Text("sku") is { } sku) skus.Add(sku);
        }
        foreach (var sku in skus)
        {
            if (!context.Products.TryGetValue(sku, out var product)) continue;
            var weight = product.WeightPerBaseUnit <= 0m;
            var volume = product.VolumePerBaseUnit <= 0m;
            if (!weight && !volume) continue;
            report.ProductsWithoutCapacity.Add(new ProductWithoutCapacity(
                product.Sku, product.Name, weight && volume ? "both" : weight ? "weight" : "volume"));
        }
    }

    private static void AddWarehousesWithoutFleet(
        ImportContext context, IReadOnlyDictionary<ImportEntity, List<ParsedRow>> parsed,
        Dictionary<string, decimal> demand, FeasibilityReport report)
    {
        var involved = new Dictionary<Guid, Location>();
        void AddByCode(string? code, string kind)
        {
            if (code is null || !context.Locations.TryGetValue((kind, code), out var location)) return;
            if (location.Kind == LocationKinds.Warehouse && location.IsActive) involved.TryAdd(location.Id, location);
        }
        if (parsed.TryGetValue(ImportEntity.Warehouses, out var warehouseRows))
            foreach (var row in warehouseRows) AddByCode(row.Text("code"), LocationKinds.Warehouse);
        if (parsed.TryGetValue(ImportEntity.Vehicles, out var vehicleRows))
            foreach (var row in vehicleRows) AddByCode(row.Text("depot_code"), LocationKinds.Warehouse);
        // Con demanda importada, cualquier bodega activa es un origen posible.
        if (demand.Count > 0)
            foreach (var location in context.Locations.Values)
                if (location.Kind == LocationKinds.Warehouse && location.IsActive) involved.TryAdd(location.Id, location);

        var withFleet = context.Vehicles.Values
            .Where(v => v.IsActive && v.MaxWeight > 0m)
            .Select(v => v.DepotLocationId)
            .ToHashSet();
        foreach (var warehouse in involved.Values.OrderBy(l => l.Code, StringComparer.Ordinal))
            if (!withFleet.Contains(warehouse.Id))
                report.WarehousesWithoutFleet.Add(new WarehouseWithoutFleet(warehouse.Code, warehouse.Name));
    }

    private static void AddStockShortages(
        ImportContext context, Dictionary<string, decimal> demand, DateOnly today, FeasibilityReport report)
    {
        if (demand.Count == 0) return;
        var warehouseIds = context.Locations.Values
            .Where(l => l.Kind == LocationKinds.Warehouse && l.IsActive)
            .Select(l => l.Id)
            .ToHashSet();
        var lotsById = context.Lots.Values.ToDictionary(l => l.Id);
        var available = new Dictionary<Guid, decimal>();
        foreach (var balance in context.Balances.Values)
        {
            if (!warehouseIds.Contains(balance.LocationId)) continue;
            if (!lotsById.TryGetValue(balance.LotId, out var lot)) continue;
            if (lot.IsBlocked || (lot.ExpiresOn is { } expires && expires <= today)) continue;
            var net = Math.Max(0m, balance.QuantityOnHand - balance.QuantityReserved);
            available[lot.ProductId] = available.GetValueOrDefault(lot.ProductId) + net;
        }
        foreach (var (sku, demanded) in demand.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!context.Products.TryGetValue(sku, out var product)) continue;
            var usable = available.GetValueOrDefault(product.Id);
            if (demanded <= usable) continue;
            report.StockShortages.Add(new StockShortage(product.Sku, product.Name, product.BaseUnit, demanded, usable));
        }
    }
}
