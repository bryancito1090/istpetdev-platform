using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record PolicySuggestionDto(
    Guid LocationId,
    string LocationCode,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal CurrentMinStock,
    decimal CurrentMaxStock,
    decimal SuggestedMinStock,
    decimal SuggestedMaxStock,
    int LeadTimeDays,
    decimal TargetCoverageDays,
    bool IsManual,
    decimal DailyConsumptionRate,
    string Rationale
);

public interface IStockPolicyAdvisor
{
    Task<IReadOnlyList<PolicySuggestionDto>> CalculateSuggestionsAsync(CancellationToken ct = default);
    Task<WarehouseStockPolicy> ApplyPolicyAsync(Guid locationId, Guid productId, decimal minStock, decimal maxStock, int leadTimeDays, decimal targetCoverageDays, bool isManual, CancellationToken ct = default);
}

public sealed class StockPolicyAdvisor : IStockPolicyAdvisor
{
    private readonly IApplicationDbContext _db;
    private readonly IWarehouseConsumptionAnalyzer _analyzer;
    private readonly LogisticsOptions _options;

    public StockPolicyAdvisor(
        IApplicationDbContext db,
        IWarehouseConsumptionAnalyzer analyzer,
        IOptions<LogisticsOptions> options)
    {
        _db = db;
        _analyzer = analyzer;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<PolicySuggestionDto>> CalculateSuggestionsAsync(CancellationToken ct = default)
    {
        var consumptions = await _analyzer.AnalyzeAsync(ct);
        var existingPolicies = await _db.WarehouseStockPolicies
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => (p.LocationId, p.ProductId), ct);

        var list = new List<PolicySuggestionDto>();

        foreach (var c in consumptions)
        {
            existingPolicies.TryGetValue((c.LocationId, c.ProductId), out var policy);

            int leadTime = policy?.LeadTimeDays ?? (int)Math.Ceiling(_options.DefaultLeadTimeDays);
            decimal targetCoverage = policy?.TargetCoverageDays ?? _options.Rebalancing.DefaultTargetCoverageDays;

            // MinStock = consumo diario * tiempo de reposición + margen de seguridad (3 días de consumo)
            decimal safetyMarginDays = _options.SafetyMarginDays > 0 ? _options.SafetyMarginDays : 3m;
            decimal suggestedMin = Math.Round(c.DailyConsumptionRate * (leadTime + safetyMarginDays), 0);

            // MaxStock = consumo diario * días de cobertura objetivo
            decimal suggestedMax = Math.Round(c.DailyConsumptionRate * targetCoverage, 0);
            if (suggestedMax < suggestedMin) suggestedMax = suggestedMin + Math.Round(c.DailyConsumptionRate * 7m, 0);

            // Rationale explicativo
            string rationale = c.DailyConsumptionRate > 0
                ? $"Basado en consumo de {c.DailyConsumptionRate:F1} u/día: Mínimo cubre {leadTime}d lead time + {safetyMarginDays}d seguridad; Máximo cubre {targetCoverage}d cobertura."
                : "Sin historial de consumo suficiente en la ventana. Se recomienda ingresar valores manualmente.";

            list.Add(new PolicySuggestionDto(
                c.LocationId,
                c.LocationCode,
                c.ProductId,
                c.Sku,
                c.ProductName,
                policy?.MinStock ?? 0m,
                policy?.MaxStock ?? 0m,
                suggestedMin,
                suggestedMax,
                leadTime,
                targetCoverage,
                policy?.IsManual ?? true,
                c.DailyConsumptionRate,
                rationale
            ));
        }

        return list;
    }

    public async Task<WarehouseStockPolicy> ApplyPolicyAsync(
        Guid locationId,
        Guid productId,
        decimal minStock,
        decimal maxStock,
        int leadTimeDays,
        decimal targetCoverageDays,
        bool isManual,
        CancellationToken ct = default)
    {
        var policy = await _db.WarehouseStockPolicies
            .FirstOrDefaultAsync(p => p.OrganizationId == DemoOrganization.Id && p.LocationId == locationId && p.ProductId == productId, ct);

        var now = DateTime.UtcNow;

        if (policy == null)
        {
            policy = new WarehouseStockPolicy
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = locationId,
                ProductId = productId,
                MinStock = minStock,
                MaxStock = maxStock,
                LeadTimeDays = leadTimeDays,
                TargetCoverageDays = targetCoverageDays,
                IsManual = isManual,
                LastCalculatedAtUtc = now
            };
            _db.WarehouseStockPolicies.Add(policy);
        }
        else
        {
            policy.MinStock = minStock;
            policy.MaxStock = maxStock;
            policy.LeadTimeDays = leadTimeDays;
            policy.TargetCoverageDays = targetCoverageDays;
            policy.IsManual = isManual;
            policy.LastCalculatedAtUtc = now;
        }

        await _db.SaveChangesAsync(ct);
        return policy;
    }
}
