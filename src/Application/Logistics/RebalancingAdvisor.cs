using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public interface IRebalancingAdvisor
{
    Task<IReadOnlyList<TransferSuggestion>> GenerateSuggestionsAsync(CancellationToken ct = default);
    Task<TransferSuggestion> AcceptSuggestionAsync(Guid suggestionId, decimal? acceptedQuantity, Guid reviewerAccountId, CancellationToken ct = default);
    Task<TransferSuggestion> DismissSuggestionAsync(Guid suggestionId, string reason, Guid reviewerAccountId, CancellationToken ct = default);
}

public sealed class RebalancingAdvisor : IRebalancingAdvisor
{
    private readonly IApplicationDbContext _db;
    private readonly IWarehouseConsumptionAnalyzer _analyzer;
    private readonly IOperationalClock _clock;
    private readonly LogisticsOptions _options;

    public RebalancingAdvisor(
        IApplicationDbContext db,
        IWarehouseConsumptionAnalyzer analyzer,
        IOperationalClock clock,
        IOptions<LogisticsOptions> options)
    {
        _db = db;
        _analyzer = analyzer;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<TransferSuggestion>> GenerateSuggestionsAsync(CancellationToken ct = default)
    {
        var consumptions = await _analyzer.AnalyzeAsync(ct);
        var today = await _clock.TodayAsync(ct);

        var policies = await _db.WarehouseStockPolicies
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => (p.LocationId, p.ProductId), ct);

        // Agrupar consumos por producto
        var byProduct = consumptions.GroupBy(c => c.ProductId);

        var activeSuggestions = await _db.TransferSuggestions
            .Where(t => t.OrganizationId == DemoOrganization.Id &&
                        (t.Status == TransferSuggestionStatus.Suggested || t.Status == TransferSuggestionStatus.Accepted))
            .ToListAsync(ct);

        var dismissedRecently = await _db.TransferSuggestions
            .Where(t => t.OrganizationId == DemoOrganization.Id &&
                        t.Status == TransferSuggestionStatus.Dismissed &&
                        t.ReviewedAtUtc.HasValue &&
                        t.ReviewedAtUtc.Value >= DateTime.UtcNow.AddDays(-_options.Rebalancing.CooldownDaysAfterDismiss))
            .ToListAsync(ct);

        var newSuggestions = new List<TransferSuggestion>();

        foreach (var prodGroup in byProduct)
        {
            var productId = prodGroup.Key;
            var list = prodGroup.ToList();
            if (list.Count < 2) continue; // Necesita al menos 2 bodegas

            // Calcular consumo total y stock disponible total en la red para este producto
            var totalAvailableStock = list.Sum(x => x.AvailableQuantity);
            var totalDailyRate = list.Sum(x => x.DailyConsumptionRate);

            if (totalAvailableStock <= 0 || totalDailyRate <= 0) continue;

            // Días de cobertura de la red
            decimal networkTargetCoverageDays = totalAvailableStock / totalDailyRate;

            // Determinar si hay disparidad significativa entre bodegas
            var maxCoverage = list.Max(x => x.CoverageDays);
            var minCoverage = list.Min(x => x.CoverageDays);

            if ((maxCoverage - minCoverage) < _options.Rebalancing.MinCoverageDifferenceDays)
            {
                continue; // La red está razonablemente balanceada
            }

            // Identificar bodegas con exceso y bodegas con déficit
            var surplus = new List<(WarehouseProductConsumption Item, decimal Excess, decimal MinStock)>();
            var deficits = new List<(WarehouseProductConsumption Item, decimal Deficit, decimal TargetStock)>();

            foreach (var item in list)
            {
                policies.TryGetValue((item.LocationId, item.ProductId), out var policy);

                int leadTime = policy?.LeadTimeDays ?? 2;
                decimal minStock = policy?.MinStock ?? Math.Round(item.DailyConsumptionRate * (leadTime + 3), 0);

                // Stock objetivo basado en la cobertura equilibrada de la red
                decimal targetStock = Math.Round(item.DailyConsumptionRate * networkTargetCoverageDays, 0);
                if (targetStock < minStock) targetStock = minStock;

                if (item.AvailableQuantity > targetStock)
                {
                    decimal excess = item.AvailableQuantity - targetStock;
                    // Asegurar que después de ceder el exceso no quede por debajo de su MinStock
                    decimal transferable = Math.Max(0m, item.AvailableQuantity - minStock);
                    decimal actualExcess = Math.Min(excess, transferable);

                    if (actualExcess > 0)
                    {
                        surplus.Add((item, actualExcess, minStock));
                    }
                }
                else if (item.AvailableQuantity < targetStock)
                {
                    decimal needed = targetStock - item.AvailableQuantity;
                    if (needed > 0)
                    {
                        deficits.Add((item, needed, targetStock));
                    }
                }
            }

            // Ordenar déficits por mayor urgencia (menor cobertura de días)
            deficits = deficits.OrderBy(d => d.Item.CoverageDays).ToList();
            // Ordenar surplus por mayor exceso
            surplus = surplus.OrderByDescending(s => s.Excess).ToList();

            foreach (var def in deficits)
            {
                decimal remainingNeeded = def.Deficit;

                for (int i = 0; i < surplus.Count; i++)
                {
                    if (remainingNeeded <= 0) break;
                    var sur = surplus[i];
                    if (sur.Excess <= 0) continue;

                    // Verificar cooldown por rechazo previo
                    bool wasDismissed = dismissedRecently.Any(d =>
                        d.SourceLocationId == sur.Item.LocationId &&
                        d.TargetLocationId == def.Item.LocationId &&
                        d.ProductId == productId);
                    if (wasDismissed) continue;

                    // Verificar si ya existe sugerencia activa pendiente
                    bool alreadyActive = activeSuggestions.Any(a =>
                        a.SourceLocationId == sur.Item.LocationId &&
                        a.TargetLocationId == def.Item.LocationId &&
                        a.ProductId == productId);
                    if (alreadyActive) continue;

                    decimal transferQty = Math.Min(remainingNeeded, sur.Excess);

                    // Verificar umbral mínimo de transferencia
                    decimal minThreshold = def.TargetStock * (_options.Rebalancing.MinTransferThresholdPct / 100m);
                    if (transferQty < minThreshold && transferQty < 5m)
                    {
                        continue;
                    }

                    // Coberturas antes y después
                    decimal srcCovBefore = sur.Item.CoverageDays;
                    decimal srcCovAfter = sur.Item.DailyConsumptionRate > 0
                        ? Math.Round((sur.Item.AvailableQuantity - transferQty) / sur.Item.DailyConsumptionRate, 1)
                        : 999m;

                    decimal tgtCovBefore = def.Item.CoverageDays;
                    decimal tgtCovAfter = def.Item.DailyConsumptionRate > 0
                        ? Math.Round((def.Item.AvailableQuantity + transferQty) / def.Item.DailyConsumptionRate, 1)
                        : 999m;

                    string rationale = $"Equilibrar días de cobertura para {def.Item.ProductName} ({def.Item.Sku}): " +
                                       $"{sur.Item.LocationCode} cubre {srcCovBefore:F0} días ({sur.Item.DailyConsumptionRate:F1} u/día) y " +
                                       $"{def.Item.LocationCode} cubre {tgtCovBefore:F0} días ({def.Item.DailyConsumptionRate:F1} u/día). " +
                                       $"Trasladando {transferQty:F0} u, ambas convergen a ~{srcCovAfter:F0} y ~{tgtCovAfter:F0} días de autonomía.";

                    var suggestion = new TransferSuggestion
                    {
                        OrganizationId = DemoOrganization.Id,
                        SourceLocationId = sur.Item.LocationId,
                        TargetLocationId = def.Item.LocationId,
                        ProductId = productId,
                        SuggestedQuantity = transferQty,
                        SourceCoverageDaysBefore = srcCovBefore,
                        SourceCoverageDaysAfter = srcCovAfter,
                        TargetCoverageDaysBefore = tgtCovBefore,
                        TargetCoverageDaysAfter = tgtCovAfter,
                        Status = TransferSuggestionStatus.Suggested,
                        Rationale = rationale
                    };

                    _db.TransferSuggestions.Add(suggestion);
                    newSuggestions.Add(suggestion);

                    // Actualizar excedente restante
                    remainingNeeded -= transferQty;
                    surplus[i] = (sur.Item, sur.Excess - transferQty, sur.MinStock);
                }
            }
        }

        if (newSuggestions.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        // Devolver todas las sugerencias activas
        return await _db.TransferSuggestions
            .Where(t => t.OrganizationId == DemoOrganization.Id && t.Status == TransferSuggestionStatus.Suggested)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<TransferSuggestion> AcceptSuggestionAsync(
        Guid suggestionId,
        decimal? acceptedQuantity,
        Guid reviewerAccountId,
        CancellationToken ct = default)
    {
        var suggestion = await _db.TransferSuggestions
            .FirstOrDefaultAsync(t => t.OrganizationId == DemoOrganization.Id && t.Id == suggestionId, ct)
            ?? throw new KeyNotFoundException($"Sugerencia {suggestionId} no encontrada.");

        if (suggestion.Status != TransferSuggestionStatus.Suggested)
        {
            throw new InvalidOperationException($"La sugerencia ya se encuentra en estado '{suggestion.Status}'.");
        }

        decimal qty = acceptedQuantity ?? suggestion.SuggestedQuantity;
        if (qty <= 0) throw new ArgumentException("La cantidad aceptada debe ser mayor a 0.");

        var today = await _clock.TodayAsync(ct);

        suggestion.Status = TransferSuggestionStatus.Accepted;
        suggestion.AcceptedQuantity = qty;
        suggestion.ReviewedByAccountId = reviewerAccountId;
        suggestion.ReviewedAtUtc = DateTime.UtcNow;

        // Crear DeliveryOrder correspondiente con destino a la bodega target
        var orderCount = await _db.DeliveryOrders.CountAsync(o => o.OrganizationId == DemoOrganization.Id, ct);
        var orderNumber = $"ORD-TRF-{today:yyyyMMdd}-{(orderCount + 1):D3}";

        var order = new DeliveryOrder
        {
            OrganizationId = DemoOrganization.Id,
            OrderNumber = orderNumber,
            DestinationLocationId = suggestion.TargetLocationId,
            DestinationServicePointId = null,
            RequiredDateUtc = DateTime.UtcNow.AddDays(2),
            Priority = "High",
            Status = DeliveryOrderStatus.Pending,
            SourceType = "Rebalancing",
            TransferSuggestionId = suggestion.Id,
            Notes = $"Traslado de rebalanceo sugerido: {suggestion.Rationale}"
        };

        _db.DeliveryOrders.Add(order);

        var line = new DeliveryOrderLine
        {
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = order.Id,
            ProductId = suggestion.ProductId,
            RequestedQuantity = qty,
            PlannedQuantity = 0m,
            DeliveredQuantity = 0m
        };

        _db.DeliveryOrderLines.Add(line);

        await _db.SaveChangesAsync(ct);
        return suggestion;
    }

    public async Task<TransferSuggestion> DismissSuggestionAsync(
        Guid suggestionId,
        string reason,
        Guid reviewerAccountId,
        CancellationToken ct = default)
    {
        var suggestion = await _db.TransferSuggestions
            .FirstOrDefaultAsync(t => t.OrganizationId == DemoOrganization.Id && t.Id == suggestionId, ct)
            ?? throw new KeyNotFoundException($"Sugerencia {suggestionId} no encontrada.");

        suggestion.Status = TransferSuggestionStatus.Dismissed;
        suggestion.DismissReason = reason;
        suggestion.ReviewedByAccountId = reviewerAccountId;
        suggestion.ReviewedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return suggestion;
    }
}
