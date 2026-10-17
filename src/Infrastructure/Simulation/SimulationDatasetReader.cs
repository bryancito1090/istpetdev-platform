using System.Security.Cryptography;
using System.Text;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Infrastructure.Simulation;

/// <summary>
/// Construye el escenario de simulación desde los datos importados en la base operativa.
/// No lee fixtures del disco ni depende de Demo:Enabled.
/// </summary>
public static class SimulationDatasetReader
{
    public const int HorizonDays = 90;

    public static async Task<ImportBatch?> LatestAppliedBatchAsync(IApplicationDbContext db, CancellationToken ct = default)
    {
        return await db.ImportBatches
            .AsNoTracking()
            .Where(b => b.OrganizationId == DemoOrganization.Id && !b.DryRun && b.Status == "Applied")
            .OrderByDescending(b => b.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public static async Task<SimulationDataset> LoadAsync(
        IApplicationDbContext db,
        string? expectedVersion = null,
        string? expectedHash = null,
        CancellationToken ct = default)
    {
        var batch = await LatestAppliedBatchAsync(db, ct);
        if (expectedHash is { Length: > 0 } && batch is not null &&
            !string.Equals(batch.InputHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new SimulationDatasetChangedException(
                "El dataset importado cambió después de encolar la corrida. Vuelva a lanzarla para usar los datos vigentes.");
        }

        var points = await db.ServicePoints
            .AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive && p.Position != null)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);
        var products = await db.Products
            .AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive)
            .OrderBy(p => p.Sku)
            .ToListAsync(ct);
        var locations = await db.Locations
            .AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.IsActive)
            .ToListAsync(ct);
        var vehicles = await db.Vehicles
            .AsNoTracking()
            .Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive)
            .OrderBy(v => v.Code)
            .ToListAsync(ct);
        var lots = await db.Lots
            .AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var balances = await db.StockBalances
            .AsNoTracking()
            .Where(b => b.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var consumptions = await db.Consumptions
            .AsNoTracking()
            .Where(c => c.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var edges = await db.RoadMatrixEdges
            .AsNoTracking()
            .Where(e => e.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        if (points.Count == 0) throw new SimulationDatasetException("El dataset no tiene puntos de servicio con coordenadas.");
        if (products.Count == 0) throw new SimulationDatasetException("El dataset no tiene productos activos.");
        if (vehicles.Count == 0) throw new SimulationDatasetException("El dataset no tiene vehículos activos.");
        if (consumptions.Count == 0) throw new SimulationDatasetException("El dataset no tiene consumos históricos para generar demanda.");

        var warehouses = locations
            .Where(l => l.Kind == LocationKinds.Warehouse)
            .OrderBy(l => l.Code)
            .ToList();
        if (warehouses.Count == 0) throw new SimulationDatasetException("El dataset no tiene bodegas activas.");

        var pointLocationByServicePoint = locations
            .Where(l => l.Kind == LocationKinds.ServicePoint && l.ServicePointId.HasValue)
            .GroupBy(l => l.ServicePointId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var productById = products.ToDictionary(p => p.Id);
        var lotProduct = lots.ToDictionary(l => l.Id, l => l.ProductId);
        var locationById = locations.ToDictionary(l => l.Id, l => l);

        var pointRecords = new List<SimulationPoint>();
        foreach (var point in points)
        {
            if (!pointLocationByServicePoint.TryGetValue(point.Id, out var location)) continue;
            pointRecords.Add(new SimulationPoint(
                Id: point.Id,
                LocationId: location.Id,
                Code: point.Code,
                Region: point.Region ?? "SIN-REGION",
                Criticality: point.Criticality,
                Position: new GeoPoint((decimal)point.Position!.Y, (decimal)point.Position.X).Round(),
                WindowStart: point.WindowStartLocal ?? new TimeOnly(8, 0),
                WindowEnd: point.WindowEndLocal ?? new TimeOnly(17, 0),
                ServiceMinutes: point.ServiceMinutes ?? 15,
                LeadTimeDays: point.LeadTimeDays is > 0 ? point.LeadTimeDays.Value : 2m));
        }

        if (pointRecords.Count == 0) throw new SimulationDatasetException("El dataset no tiene ubicaciones de punto de servicio.");
        var pointByCode = pointRecords.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var pointCodeByLocation = pointRecords.ToDictionary(p => p.LocationId, p => p.Code);
        var warehouseCodeByLocation = warehouses.ToDictionary(w => w.Id, w => w.Code);

        var initialPointStock = new Dictionary<(string PointCode, string Sku), decimal>();
        var initialWarehouseStock = new Dictionary<(string WarehouseCode, string Sku), decimal>();
        foreach (var balance in balances)
        {
            if (!lotProduct.TryGetValue(balance.LotId, out var productId) ||
                !productById.TryGetValue(productId, out var product)) continue;
            var available = Math.Max(0m, balance.QuantityOnHand - balance.QuantityReserved);
            if (pointCodeByLocation.TryGetValue(balance.LocationId, out var pointCode))
                Add(initialPointStock, (pointCode, product.Sku), available);
            else if (warehouseCodeByLocation.TryGetValue(balance.LocationId, out var warehouseCode))
                Add(initialWarehouseStock, (warehouseCode, product.Sku), available);
        }

        var pointWarehouse = AssignWarehouses(pointRecords, warehouses);
        var profiles = BuildDemandProfiles(consumptions, pointByCode, productById);
        if (profiles.Count == 0)
            throw new SimulationDatasetException("Los consumos no permiten construir demanda por punto y SKU.");

        var startDate = consumptions.Max(c => c.OccurredOn).AddDays(1);
        var roadMatrix = edges
            .GroupBy(e => (e.FromLat, e.FromLon, e.ToLat, e.ToLon))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var first = g.First();
                    return new RoadEdge(first.DistanceKm, first.DurationMinutes);
                });

        var version = batch?.ProfileName
            ?? (string.IsNullOrWhiteSpace(expectedVersion) ? "dataset-local" : expectedVersion);
        var hash = batch?.InputHash;
        if (string.IsNullOrWhiteSpace(hash))
        {
            hash = ComputeFingerprint(pointRecords, products, vehicles, initialPointStock, initialWarehouseStock, startDate);
        }

        // El fixture declara VEH-0001; para otros datasets se retira el último código
        // de forma determinista. Si solo hay un vehículo, no se retira.
        var adverseVehicle = vehicles.Count > 1
            ? (vehicles.FirstOrDefault(v => string.Equals(v.Code, "VEH-0001", StringComparison.OrdinalIgnoreCase))?.Code
               ?? vehicles.OrderByDescending(v => v.Code, StringComparer.OrdinalIgnoreCase).First().Code)
            : null;

        return new SimulationDataset(
            Version: version,
            Hash: hash,
            StartDate: startDate,
            HorizonDays: HorizonDays,
            Points: pointRecords,
            Products: products.Select(p => new SimulationProduct(
                p.Id, p.Sku, p.BaseUnit.ToUpperInvariant(), p.WeightPerBaseUnit,
                p.VolumePerBaseUnit, p.UnitValue, p.UnitsPerPackage)).ToList(),
            Vehicles: vehicles.Select(v => new SimulationVehicle(
                v.Id, v.Code, v.DepotLocationId,
                locationById.TryGetValue(v.DepotLocationId, out var depot) ? depot.Code : "",
                v.MaxWeight, v.MaxVolume, v.CostPerKm, v.VehicleType)).ToList(),
            DemandProfiles: profiles,
            InitialPointStock: initialPointStock,
            InitialWarehouseStock: initialWarehouseStock,
            PointWarehouse: pointWarehouse,
            WarehousePositions: warehouses
                .Where(w => w.Position is not null)
                .ToDictionary(w => w.Code, w => new GeoPoint((decimal)w.Position!.Y, (decimal)w.Position.X).Round()),
            RoadMatrix: roadMatrix,
            AdverseRetiredVehicle: adverseVehicle);
    }

    private static void Add<TKey>(Dictionary<TKey, decimal> target, TKey key, decimal value) where TKey : notnull
    {
        target[key] = target.TryGetValue(key, out var current) ? current + value : value;
    }

    private static IReadOnlyDictionary<string, string> AssignWarehouses(
        IReadOnlyList<SimulationPoint> points,
        IReadOnlyList<Location> warehouses)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var point in points)
        {
            var nearest = warehouses
                .Where(w => w.Position is not null)
                .Select(w => new
                {
                    w.Code,
                    Km = SimulationGeo.Haversine(point.Position, new GeoPoint((decimal)w.Position!.Y, (decimal)w.Position.X).Round())
                })
                .OrderBy(x => x.Km)
                .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (nearest is not null) result[point.Code] = nearest.Code;
        }
        return result;
    }

    private static List<DemandProfile> BuildDemandProfiles(
        IReadOnlyList<Consumption> consumptions,
        IReadOnlyDictionary<string, SimulationPoint> points,
        IReadOnlyDictionary<Guid, Product> products)
    {
        var result = new List<DemandProfile>();
        var pointById = points.Values.ToDictionary(p => p.Id, p => p);
        var minDate = consumptions.Min(c => c.OccurredOn);
        var maxDate = consumptions.Max(c => c.OccurredOn);
        var totalDays = Math.Max(1, maxDate.DayNumber - minDate.DayNumber + 1);

        foreach (var group in consumptions.GroupBy(c => (c.ServicePointId, c.ProductId)))
        {
            if (!pointById.TryGetValue(group.Key.ServicePointId, out var point) ||
                !products.TryGetValue(group.Key.ProductId, out var product)) continue;

            var quantitiesByDate = group
                .GroupBy(c => c.OccurredOn)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Quantity));
            var meanByWeekday = new Dictionary<DayOfWeek, decimal>();
            for (var day = DayOfWeek.Sunday; day <= DayOfWeek.Saturday; day++)
            {
                var dates = Enumerable.Range(0, totalDays)
                    .Select(offset => minDate.AddDays(offset))
                    .Where(d => d.DayOfWeek == day)
                    .ToList();
                if (dates.Count == 0) { meanByWeekday[day] = 0m; continue; }
                var total = dates.Sum(d => quantitiesByDate.GetValueOrDefault(d, 0m));
                meanByWeekday[day] = Math.Round(total / dates.Count, 3, MidpointRounding.AwayFromZero);
            }

            var dailyValues = Enumerable.Range(0, totalDays)
                .Select(offset => quantitiesByDate.GetValueOrDefault(minDate.AddDays(offset), 0m))
                .ToList();
            var mean = dailyValues.Average();
            if (mean <= 0m) continue;
            var variance = dailyValues.Sum(v => (v - mean) * (v - mean)) / dailyValues.Count;
            var cv = (decimal)Math.Sqrt((double)variance) / mean;
            var volatility = Math.Clamp(cv, 0.05m, 0.15m);

            result.Add(new DemandProfile(
                PointCode: point.Code,
                Region: point.Region,
                ProductId: product.Id,
                Sku: product.Sku,
                Unit: product.BaseUnit.ToUpperInvariant(),
                MeanByWeekday: meanByWeekday,
                Volatility: Math.Round(volatility, 4, MidpointRounding.AwayFromZero)));
        }

        return result
            .OrderBy(p => p.PointCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Sku, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ComputeFingerprint(
        IReadOnlyList<SimulationPoint> points,
        IReadOnlyList<Product> products,
        IReadOnlyList<Vehicle> vehicles,
        IReadOnlyDictionary<(string PointCode, string Sku), decimal> pointStock,
        IReadOnlyDictionary<(string WarehouseCode, string Sku), decimal> warehouseStock,
        DateOnly startDate)
    {
        var builder = new StringBuilder();
        builder.Append("simulation-dataset|").Append(startDate).Append('|');
        foreach (var p in points.OrderBy(p => p.Code))
            builder.Append(p.Code).Append(':').Append(p.Region).Append(':').Append(p.Position.Lat).Append(',').Append(p.Position.Lon).Append('|');
        foreach (var p in products.OrderBy(p => p.Sku))
            builder.Append(p.Sku).Append(':').Append(p.BaseUnit).Append('|');
        foreach (var v in vehicles.OrderBy(v => v.Code))
            builder.Append(v.Code).Append(':').Append(v.MaxWeight).Append('|');
        foreach (var kv in pointStock.OrderBy(k => k.Key.PointCode).ThenBy(k => k.Key.Sku))
            builder.Append(kv.Key.PointCode).Append(':').Append(kv.Key.Sku).Append('=').Append(kv.Value).Append('|');
        foreach (var kv in warehouseStock.OrderBy(k => k.Key.WarehouseCode).ThenBy(k => k.Key.Sku))
            builder.Append(kv.Key.WarehouseCode).Append(':').Append(kv.Key.Sku).Append('=').Append(kv.Value).Append('|');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }
}
