using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record EmergencyDemandItem(
    Guid TargetId,
    bool IsWarehouse,
    string Code,
    string Name,
    GeoPoint Position,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal QuantityNeeded,
    decimal CoverageDays,
    string UrgencyLevel, // "Critical", "High", "Warning"
    string Reason
);

public interface IEmergencyPlanner
{
    Task<IReadOnlyList<EmergencyDemandItem>> DetectEmergencyDemandsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryOrder>> CreateEmergencyOrdersAsync(IReadOnlyList<EmergencyDemandItem> items, CancellationToken ct = default);
}

public sealed class EmergencyPlanner : IEmergencyPlanner
{
    private readonly IApplicationDbContext _db;
    private readonly IRiskCalculator _riskCalculator;
    private readonly IWarehouseConsumptionAnalyzer _warehouseAnalyzer;
    private readonly IOperationalClock _clock;
    private readonly LogisticsOptions _options;

    public EmergencyPlanner(
        IApplicationDbContext db,
        IRiskCalculator riskCalculator,
        IWarehouseConsumptionAnalyzer warehouseAnalyzer,
        IOperationalClock clock,
        IOptions<LogisticsOptions> options)
    {
        _db = db;
        _riskCalculator = riskCalculator;
        _warehouseAnalyzer = warehouseAnalyzer;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<EmergencyDemandItem>> DetectEmergencyDemandsAsync(CancellationToken ct = default)
    {
        var items = new List<EmergencyDemandItem>();

        // 1. Riesgos en Puntos de Servicio (ServicePoints) usando RiskCalculator
        var risks = await _riskCalculator.CalculateAsync(persistSnapshots: false, ct);
        var criticalAndHighSp = risks
            .Where(r => r.Level is RiskLevel.Critico or RiskLevel.Alto && r.SuggestedQuantity > 0)
            .ToList();

        var spIds = criticalAndHighSp.Select(r => r.ServicePointId).Distinct().ToList();
        var servicePoints = await _db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id && spIds.Contains(sp.Id))
            .ToDictionaryAsync(sp => sp.Id, ct);

        foreach (var r in criticalAndHighSp)
        {
            if (!servicePoints.TryGetValue(r.ServicePointId, out var sp)) continue;
            var pos = sp.Position != null
                ? new GeoPoint((decimal)sp.Position.Y, (decimal)sp.Position.X)
                : new GeoPoint(-0.2295m, -78.5243m);

            items.Add(new EmergencyDemandItem(
                sp.Id,
                IsWarehouse: false,
                sp.Code,
                sp.Name,
                pos,
                r.ProductId,
                r.Sku,
                r.Sku,
                r.SuggestedQuantity,
                r.CoverageDays ?? 0m,
                r.Level == RiskLevel.Critico ? "Critical" : "High",
                $"Punto en riesgo {r.Level}: Cobertura {r.CoverageDays:F1}d frente a Lead Time {r.LeadTimeDays:F1}d."
            ));
        }

        // 2. Riesgos en Bodegas (Warehouses)
        var warehouseConsumptions = await _warehouseAnalyzer.AnalyzeAsync(ct);
        var warehousePolicies = await _db.WarehouseStockPolicies
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => (p.LocationId, p.ProductId), ct);

        var warehouses = await _db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse)
            .ToDictionaryAsync(l => l.Id, ct);

        foreach (var wc in warehouseConsumptions)
        {
            warehousePolicies.TryGetValue((wc.LocationId, wc.ProductId), out var policy);
            int leadTime = policy?.LeadTimeDays ?? (int)Math.Ceiling(_options.DefaultLeadTimeDays);
            decimal minStock = policy?.MinStock ?? Math.Round(wc.DailyConsumptionRate * (leadTime + 3), 0);

            // Alerta si el stock actual está por debajo del mínimo o si los días de cobertura son menores que el lead time
            bool isBelowMin = wc.AvailableQuantity < minStock;
            bool isCriticallyLowCoverage = wc.CoverageDays < (leadTime + 1m);

            if (isBelowMin || isCriticallyLowCoverage)
            {
                if (!warehouses.TryGetValue(wc.LocationId, out var wh)) continue;
                var pos = wh.Position != null
                    ? new GeoPoint((decimal)wh.Position.Y, (decimal)wh.Position.X)
                    : new GeoPoint(-0.2295m, -78.5243m);

                decimal deficit = Math.Max(minStock - wc.AvailableQuantity, Math.Round(wc.DailyConsumptionRate * (leadTime + 5), 0));
                if (deficit <= 0) deficit = 10m; // Fallback razonable

                items.Add(new EmergencyDemandItem(
                    wh.Id,
                    IsWarehouse: true,
                    wh.Code,
                    wh.Name,
                    pos,
                    wc.ProductId,
                    wc.Sku,
                    wc.ProductName,
                    deficit,
                    wc.CoverageDays,
                    isCriticallyLowCoverage ? "Critical" : "High",
                    $"Bodega por debajo de stock mínimo ({wc.AvailableQuantity:F0} < {minStock:F0}). Cobertura estimada: {wc.CoverageDays:F1}d."
                ));
            }
        }

        // Ordenar por prioridad de urgencia
        return items
            .OrderBy(i => i.UrgencyLevel == "Critical" ? 0 : 1)
            .ThenBy(i => i.CoverageDays)
            .ToList();
    }

    public async Task<IReadOnlyList<DeliveryOrder>> CreateEmergencyOrdersAsync(IReadOnlyList<EmergencyDemandItem> items, CancellationToken ct = default)
    {
        var today = await _clock.TodayAsync(ct);
        var createdOrders = new List<DeliveryOrder>();
        var orderCount = await _db.DeliveryOrders.CountAsync(o => o.OrganizationId == DemoOrganization.Id, ct);

        // Agrupar por destino para no duplicar pedidos al mismo lugar
        var byTarget = items.GroupBy(i => (i.TargetId, i.IsWarehouse));

        foreach (var group in byTarget)
        {
            orderCount++;
            var (targetId, isWarehouse) = group.Key;
            var firstItem = group.First();

            var orderNumber = $"ORD-EMG-{today:yyyyMMdd}-{orderCount:D3}";
            var order = new DeliveryOrder
            {
                OrganizationId = DemoOrganization.Id,
                OrderNumber = orderNumber,
                DestinationServicePointId = !isWarehouse ? targetId : null,
                DestinationLocationId = isWarehouse ? targetId : null,
                RequiredDateUtc = DateTime.UtcNow.AddHours(24),
                Priority = "Critical",
                Status = DeliveryOrderStatus.Pending,
                SourceType = isWarehouse ? "EmergencyWarehouse" : "EmergencyPoint",
                Notes = $"Despacho de emergencia generado automáticamente. Destino: {firstItem.Name}"
            };

            _db.DeliveryOrders.Add(order);

            foreach (var item in group)
            {
                var line = new DeliveryOrderLine
                {
                    OrganizationId = DemoOrganization.Id,
                    DeliveryOrderId = order.Id,
                    ProductId = item.ProductId,
                    RequestedQuantity = item.QuantityNeeded,
                    PlannedQuantity = 0m,
                    DeliveredQuantity = 0m
                };
                _db.DeliveryOrderLines.Add(line);
            }

            createdOrders.Add(order);
        }

        await _db.SaveChangesAsync(ct);
        return createdOrders;
    }
}
