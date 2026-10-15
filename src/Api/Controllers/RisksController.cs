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
    [HttpGet]
    public async Task<IActionResult> GetRisks([FromQuery] string? level = null, [FromQuery] bool refresh = false, CancellationToken ct = default)
    {
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

        return Ok(response);
    }
}
