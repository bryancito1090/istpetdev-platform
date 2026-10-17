using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/warehouses")]
public sealed class WarehousesController(
    IApplicationDbContext db,
    IWarehouseConsumptionAnalyzer analyzer,
    IStockPolicyAdvisor policyAdvisor,
    IRebalancingAdvisor rebalancingAdvisor) : ControllerBase
{
    [HttpGet("analysis")]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> GetConsumptionAnalysis(CancellationToken ct = default)
    {
        var consumptions = await analyzer.AnalyzeAsync(ct);
        var policies = await db.WarehouseStockPolicies
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => (p.LocationId, p.ProductId), ct);

        var result = consumptions.Select(c =>
        {
            policies.TryGetValue((c.LocationId, c.ProductId), out var policy);
            var minStock = policy?.MinStock ?? 0m;
            var maxStock = policy?.MaxStock ?? 0m;

            string status = "Normal";
            if (c.AvailableQuantity <= 0) status = "SinStock";
            else if (c.AvailableQuantity < minStock) status = "Critico";
            else if (maxStock > 0 && c.AvailableQuantity > maxStock * 1.5m) status = "Exceso";

            return new
            {
                locationId = c.LocationId,
                locationCode = c.LocationCode,
                productId = c.ProductId,
                sku = c.Sku,
                productName = c.ProductName,
                totalConsumedInWindow = c.TotalConsumedInWindow,
                windowDays = c.WindowDays,
                dailyConsumptionRate = c.DailyConsumptionRate,
                monthlyConsumptionRate = Math.Round(c.DailyConsumptionRate * 30m, 1),
                quantityOnHand = c.QuantityOnHand,
                quantityReserved = c.QuantityReserved,
                availableQuantity = c.AvailableQuantity,
                coverageDays = c.CoverageDays,
                confidenceLevel = c.ConfidenceLevel,
                minStock,
                maxStock,
                status
            };
        });

        return Ok(result);
    }

    [HttpGet("policies")]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> GetPolicies(CancellationToken ct = default)
    {
        var suggestions = await policyAdvisor.CalculateSuggestionsAsync(ct);
        return Ok(suggestions);
    }

    [HttpPut("{locationId:guid}/policies/{productId:guid}")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> UpdatePolicy(
        [FromRoute] Guid locationId,
        [FromRoute] Guid productId,
        [FromBody] UpdatePolicyRequest req,
        CancellationToken ct = default)
    {
        var updated = await policyAdvisor.ApplyPolicyAsync(
            locationId,
            productId,
            req.MinStock,
            req.MaxStock,
            req.LeadTimeDays,
            req.TargetCoverageDays,
            req.IsManual,
            ct);

        return Ok(updated);
    }

    [HttpPost("rebalancing/generate")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> GenerateRebalancing(CancellationToken ct = default)
    {
        await rebalancingAdvisor.GenerateSuggestionsAsync(ct);
        return await GetSuggestions(TransferSuggestionStatus.Suggested, ct);
    }

    [HttpGet("rebalancing/suggestions")]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> GetSuggestions([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var query = db.TransferSuggestions.Where(t => t.OrganizationId == DemoOrganization.Id);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        var list = await query.OrderByDescending(t => t.CreatedAtUtc).ToListAsync(ct);
        var locations = await db.Locations.Where(l => l.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(l => l.Id, ct);
        var products = await db.Products.Where(p => p.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(p => p.Id, ct);

        var result = list.Select(s =>
        {
            locations.TryGetValue(s.SourceLocationId, out var src);
            locations.TryGetValue(s.TargetLocationId, out var tgt);
            products.TryGetValue(s.ProductId, out var prod);

            return new
            {
                id = s.Id,
                sourceLocationId = s.SourceLocationId,
                sourceLocationCode = src?.Code ?? "",
                sourceLocationName = src?.Name ?? "",
                targetLocationId = s.TargetLocationId,
                targetLocationCode = tgt?.Code ?? "",
                targetLocationName = tgt?.Name ?? "",
                productId = s.ProductId,
                sku = prod?.Sku ?? "",
                productName = prod?.Name ?? "",
                suggestedQuantity = s.SuggestedQuantity,
                acceptedQuantity = s.AcceptedQuantity,
                sourceCoverageDaysBefore = s.SourceCoverageDaysBefore,
                sourceCoverageDaysAfter = s.SourceCoverageDaysAfter,
                targetCoverageDaysBefore = s.TargetCoverageDaysBefore,
                targetCoverageDaysAfter = s.TargetCoverageDaysAfter,
                status = s.Status,
                rationale = s.Rationale,
                createdAtUtc = s.CreatedAtUtc
            };
        });

        return Ok(result);
    }

    [HttpPost("rebalancing/suggestions/{id:guid}/accept")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> AcceptSuggestion(
        [FromRoute] Guid id,
        [FromBody] AcceptSuggestionRequest? req = null,
        CancellationToken ct = default)
    {
        var reviewer = User.AccountId() ?? Guid.NewGuid();
        var accepted = await rebalancingAdvisor.AcceptSuggestionAsync(id, req?.AcceptedQuantity, reviewer, ct);
        return Ok(accepted);
    }

    [HttpPost("rebalancing/suggestions/{id:guid}/dismiss")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> DismissSuggestion(
        [FromRoute] Guid id,
        [FromBody] DismissSuggestionRequest req,
        CancellationToken ct = default)
    {
        var reviewer = User.AccountId() ?? Guid.NewGuid();
        var dismissed = await rebalancingAdvisor.DismissSuggestionAsync(id, req?.Reason ?? "Descartado por planificador", reviewer, ct);
        return Ok(dismissed);
    }
}

public sealed record UpdatePolicyRequest(
    decimal MinStock,
    decimal MaxStock,
    int LeadTimeDays,
    decimal TargetCoverageDays,
    bool IsManual
);

public sealed record AcceptSuggestionRequest(decimal? AcceptedQuantity);
public sealed record DismissSuggestionRequest(string Reason);
