using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/agent-decisions")]
public sealed class AgentDecisionsController(
    IApplicationDbContext db,
    IReplanner replanner) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> ListDecisions([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var query = db.AgentDecisions.Where(d => d.OrganizationId == DemoOrganization.Id);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.Status == status);
        }

        var list = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> ApproveOption([FromRoute] Guid id, [FromBody] ApproveOptionInput input, CancellationToken ct = default)
    {
        var plan = await replanner.ApproveDecisionOptionAsync(
            new ApproveOptionRequest(id, input.Option, User.AccountId()), ct);

        return Ok(new { routePlanId = plan.Id, status = plan.Status });
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> RejectOption([FromRoute] Guid id, CancellationToken ct = default)
    {
        var decision = await replanner.RejectDecisionAsync(id, User.AccountId(), ct);
        return Ok(new { agentDecisionId = decision.Id, status = decision.Status });
    }
}

public sealed record ApproveOptionInput(string Option);
