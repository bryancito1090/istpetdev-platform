using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/risks")]
[Authorize(Roles = Roles.Any)]
public sealed class RisksController(IRiskCalculator riskCalculator) : ControllerBase
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime CachedAtUtc, object Response)> Cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(45);

    [HttpGet]
    public async Task<IActionResult> GetRisks([FromQuery] string? level = null, [FromQuery] bool refresh = false, CancellationToken ct = default)
    {
        var cacheKey = level ?? "__ALL__";
        if (!refresh && Cache.TryGetValue(cacheKey, out var entry) && (DateTime.UtcNow - entry.CachedAtUtc) < CacheTtl)
        {
            return Ok(entry.Response);
        }

        var risks = await riskCalculator.CalculateAsync(persistSnapshots: false, ct);

        if (!string.IsNullOrWhiteSpace(level))
        {
            risks = risks.Where(r => string.Equals(r.Level.ToString(), level, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var facts = risks.SelectMany(r => r.Facts).ToList();

        var response = new
        {
            items = risks.Select(r => new
            {
                servicePointId = r.ServicePointId,
                servicePointCode = r.PointCode,
                criticality = r.Criticality,
                productId = r.ProductId,
                sku = r.Sku,
                baseUnit = r.BaseUnit,
                availableQuantity = r.Available,
                inTransitQuantity = r.InTransit,
                dailyDemand = r.DailyDemand,
                coverageDays = r.CoverageDays,
                leadTimeDays = r.LeadTimeDays,
                safetyMarginDays = r.SafetyMarginDays,
                suggestedQuantity = r.SuggestedQuantity,
                level = r.Level.ToString()
            }),
            facts = Facts.Sort(facts)
        };

        Cache[cacheKey] = (DateTime.UtcNow, response);
        return Ok(response);
    }
}
