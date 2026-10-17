using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public enum RiskLevel { Critico, Alto, Vigilar, Normal, SinDatos }

public sealed record RiskItem(
    Guid ServicePointId,
    string PointCode,
    string Criticality,
    Guid ProductId,
    string Sku,
    string BaseUnit,
    decimal Available,
    decimal? DailyDemand,
    decimal? CoverageDays,
    decimal LeadTimeDays,
    decimal SafetyMarginDays,
    RiskLevel Level,
    decimal SuggestedQuantity,
    decimal InTransit,
    IReadOnlyList<Fact> Facts);

public interface IRiskCalculator
{
    Task<IReadOnlyList<RiskItem>> CalculateAsync(bool persistSnapshots = false, CancellationToken ct = default);
}

// FR-02, RF-03: Calculador de cobertura, demanda y riesgo de abastecimiento.
public sealed class RiskCalculator(
    IApplicationDbContext db,
    IOperationalClock clock,
    IOptions<LogisticsOptions> options) : IRiskCalculator
{
    private readonly LogisticsOptions _options = options.Value;

    public async Task<IReadOnlyList<RiskItem>> CalculateAsync(bool persistSnapshots = false, CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        var windowStart = today.AddDays(-_options.DemandWindowDays);

        var servicePoints = await db.ServicePoints
            .AsNoTracking()
            .Where(sp => sp.OrganizationId == DemoOrganization.Id && sp.IsActive)
            .ToListAsync(ct);

        var products = await db.Products
            .AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive)
            .ToListAsync(ct);

        var spIds = servicePoints.Select(sp => sp.Id).ToHashSet();

        // 1. Stock balances en ubicaciones de los puntos de servicio
        var spLocations = await db.Locations
            .AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.ServicePointId != null && spIds.Contains(l.ServicePointId.Value))
            .Select(l => new { l.Id, ServicePointId = l.ServicePointId!.Value })
            .ToListAsync(ct);

        var locToSp = spLocations.ToDictionary(l => l.Id, l => l.ServicePointId);
        var locIds = spLocations.Select(l => l.Id).ToHashSet();

        var balances = await db.StockBalances
            .AsNoTracking()
            .Where(sb => sb.OrganizationId == DemoOrganization.Id && locIds.Contains(sb.LocationId))
            .ToListAsync(ct);

        var lotIds = balances.Select(b => b.LotId).Distinct().ToList();
        var lots = await db.Lots
            .AsNoTracking()
            .Where(l => lotIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, ct);

        // 2. Consumos en la ventana de demanda
        var consumptions = await db.Consumptions
            .AsNoTracking()
            .Where(c => c.OrganizationId == DemoOrganization.Id && spIds.Contains(c.ServicePointId))
            .ToListAsync(ct);

        // 3. Entregas en tránsito hacia el punto (despachadas, recogidas o entregadas y aún no recibidas).
        // #162: Delivered sigue pendiente del recibo del receptor; Received/Partial ya conciliaron.
        // Compatibilidad legada: una entrega Delivered con recibo previo ya es recepción final.
        // #199: una entrega publicada en Draft (sin despachar) ya tiene stock reservado para el punto; cuenta como en camino
        // por su cantidad planificada. Al despacharse deja de ser Draft y cuenta por lo despachado: no se suma dos veces.
        var pendingDeliveries = await db.Deliveries
            .AsNoTracking()
            .Where(d => d.OrganizationId == DemoOrganization.Id && spIds.Contains(d.ServicePointId)
                && ((d.Status == DeliveryStates.Draft && d.DispatchedAtUtc == null)
                    || (d.DispatchedAtUtc != null
                        && (d.Status == DeliveryStates.Dispatched || d.Status == DeliveryStates.InTransit || d.Status == DeliveryStates.Delivered)
                        && !(d.Status == DeliveryStates.Delivered && db.Receipts.Any(r => r.OrganizationId == DemoOrganization.Id && r.DeliveryId == d.Id)))))
            .Select(d => new { d.Id, d.ServicePointId, IsDraft = d.Status == DeliveryStates.Draft })
            .ToListAsync(ct);
        var pendingDeliveryById = pendingDeliveries.ToDictionary(d => d.Id);
        var pendingDeliveryIds = pendingDeliveryById.Keys.ToList();

        var pendingLines = await db.DeliveryLines
            .AsNoTracking()
            .Where(dl => pendingDeliveryIds.Contains(dl.DeliveryId))
            .ToListAsync(ct);

        // Los lotes en camino salen de bodega: pueden no tener saldo todavía en el punto.
        var pendingLotIds = pendingLines.Select(l => l.LotId).Distinct().ToList();
        var pendingLotProduct = await db.Lots
            .AsNoTracking()
            .Where(l => pendingLotIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.ProductId, ct);

        var results = new List<RiskItem>();

        foreach (var sp in servicePoints.OrderBy(s => s.Code, StringComparer.Ordinal))
        {
            var spLocIds = spLocations.Where(l => l.ServicePointId == sp.Id).Select(l => l.Id).ToHashSet();
            var leadTime = sp.LeadTimeDays ?? _options.DefaultLeadTimeDays;
            var safetyMargin = _options.SafetyMarginDays;
            var consolidationExtra = _options.ConsolidationExtraDays;

            foreach (var product in products.OrderBy(p => p.Sku, StringComparer.Ordinal))
            {
                // Stock disponible en este punto para este producto (lotes válidos no vencidos ni bloqueados)
                var availableStock = balances
                    .Where(b => spLocIds.Contains(b.LocationId) && lots.TryGetValue(b.LotId, out var lot) && lot.ProductId == product.Id && !lot.IsBlocked && (lot.ExpiresOn == null || lot.ExpiresOn > today))
                    .Sum(b => b.QuantityOnHand - b.QuantityReserved);

                // Consumos asociados
                var prodConsumptions = consumptions
                    .Where(c => c.ServicePointId == sp.Id && c.ProductId == product.Id)
                    .ToList();

                decimal totalProrated = 0m;
                int totalOverlapDays = 0;

                foreach (var c in prodConsumptions)
                {
                    var (prorated, overlap) = ProrateConsumption(c.OccurredOn, c.PeriodDays, c.Quantity, windowStart, today);
                    totalProrated += prorated;
                    totalOverlapDays += overlap;
                }

                decimal? dailyDemand = (totalOverlapDays > 0 && totalProrated > 0)
                    ? Math.Round(totalProrated / totalOverlapDays, 3)
                    : null;

                // En camino hacia este punto: lo reservado en Draft por lo planificado; lo despachado por lo aún no aceptado.
                var inTransit = pendingLines
                    .Where(l => pendingDeliveryById[l.DeliveryId].ServicePointId == sp.Id
                        && pendingLotProduct.TryGetValue(l.LotId, out var productId) && productId == product.Id)
                    .Sum(l => pendingDeliveryById[l.DeliveryId].IsDraft ? l.PlannedQuantity : l.DispatchedQuantity - l.AcceptedQuantity);

                decimal? coverage = ComputeCoverage(availableStock, dailyDemand);
                var level = ClassifyLevel(coverage, leadTime, safetyMargin, consolidationExtra);
                var suggested = ComputeSuggestedQuantity(dailyDemand, _options.TargetCoverageDays, _options.SafetyStockUnits, availableStock, inTransit);

                var facts = new List<Fact>();
                if (coverage.HasValue)
                    facts.Add(Facts.CoverageDays(sp.Code, product.Sku, coverage.Value));
                facts.Add(Facts.Generic("available_qty", $"{sp.Code}/{product.Sku}", availableStock, product.BaseUnit));
                if (dailyDemand.HasValue)
                    facts.Add(Facts.Generic("daily_demand", $"{sp.Code}/{product.Sku}", dailyDemand.Value, $"{product.BaseUnit}/día"));
                if (suggested > 0)
                    facts.Add(Facts.Generic("suggested_qty", $"{sp.Code}/{product.Sku}", suggested, product.BaseUnit));
                if (inTransit > 0)
                    facts.Add(Facts.Generic("in_transit_qty", $"{sp.Code}/{product.Sku}", inTransit, product.BaseUnit));

                var item = new RiskItem(
                    ServicePointId: sp.Id,
                    PointCode: sp.Code,
                    Criticality: sp.Criticality,
                    ProductId: product.Id,
                    Sku: product.Sku,
                    BaseUnit: product.BaseUnit,
                    Available: availableStock,
                    DailyDemand: dailyDemand,
                    CoverageDays: coverage,
                    LeadTimeDays: leadTime,
                    SafetyMarginDays: safetyMargin,
                    Level: level,
                    SuggestedQuantity: suggested,
                    InTransit: inTransit,
                    Facts: Facts.Sort(facts));

                results.Add(item);

                if (persistSnapshots)
                {
                    db.RiskSnapshots.Add(new RiskSnapshot
                    {
                        OrganizationId = DemoOrganization.Id,
                        ServicePointId = sp.Id,
                        ProductId = product.Id,
                        CalculatedAtUtc = DateTime.UtcNow,
                        FormulaVersion = "v1-moving-average",
                        AvailableQuantity = availableStock,
                        DailyDemand = dailyDemand,
                        CoverageDays = coverage,
                        LeadTimeDays = leadTime,
                        SafetyMarginDays = safetyMargin,
                        SuggestedQuantity = suggested,
                        RiskCode = level.ToString(),
                        InputSnapshotJson = JsonSerializer.Serialize(new
                        {
                            clock = today.ToString("yyyy-MM-dd"),
                            windowDays = _options.DemandWindowDays,
                            totalProrated,
                            totalOverlapDays
                        })
                    });
                }
            }
        }

        if (persistSnapshots)
        {
            await db.SaveChangesAsync(ct);
        }

        return results;
    }

    public static (decimal ProratedQty, int OverlapDays) ProrateConsumption(
        DateOnly occurredOn,
        int periodDays,
        decimal quantity,
        DateOnly windowStart,
        DateOnly windowEnd)
    {
        var periodEnd = occurredOn.AddDays(periodDays);
        var overlapStart = occurredOn > windowStart ? occurredOn : windowStart;
        var overlapEnd = periodEnd < windowEnd ? periodEnd : windowEnd;

        if (overlapEnd <= overlapStart)
            return (0m, 0);

        var overlapDays = overlapEnd.DayNumber - overlapStart.DayNumber;
        if (overlapDays <= 0 || periodDays <= 0)
            return (0m, 0);

        var fraction = (decimal)overlapDays / periodDays;
        return (Math.Round(quantity * fraction, 3), overlapDays);
    }

    public static decimal? ComputeCoverage(decimal available, decimal? dailyDemand)
    {
        if (dailyDemand is null || dailyDemand.Value <= 0)
            return null;

        return Math.Round(available / dailyDemand.Value, 1);
    }

    public static RiskLevel ClassifyLevel(
        decimal? coverageDays,
        decimal leadTimeDays,
        decimal safetyMarginDays,
        decimal consolidationExtraDays)
    {
        if (coverageDays is null)
            return RiskLevel.SinDatos;

        var cov = coverageDays.Value;
        if (cov <= leadTimeDays)
            return RiskLevel.Critico;

        if (cov <= leadTimeDays + safetyMarginDays)
            return RiskLevel.Alto;

        if (cov <= leadTimeDays + safetyMarginDays + consolidationExtraDays)
            return RiskLevel.Vigilar;

        return RiskLevel.Normal;
    }

    public static decimal ComputeSuggestedQuantity(
        decimal? dailyDemand,
        decimal targetCoverageDays,
        decimal safetyStockUnits,
        decimal available,
        decimal inTransit)
    {
        if (dailyDemand is null || dailyDemand.Value <= 0)
            return 0m;

        var target = (dailyDemand.Value * targetCoverageDays) + safetyStockUnits;
        var netNeeded = target - (available + inTransit);

        if (netNeeded <= 0)
            return 0m;

        return Math.Ceiling(netNeeded * 1000m) / 1000m;
    }
}
