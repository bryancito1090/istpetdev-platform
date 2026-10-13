using IstpetDev.Api.Authentication;
using IstpetDev.Application.Agent;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/agent/runs")]
public sealed class AgentRunsController(IAgentRunService agentRunService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> StartRun([FromBody] StartRunRequest request, CancellationToken ct = default)
    {
        var view = await agentRunService.StartAndWaitAsync(request, User.AccountId(), ct);
        return Ok(view);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct = default)
    {
        var view = await agentRunService.GetAsync(id, ct);
        if (view == null) return NotFound();

        return Ok(view);
    }

    [HttpGet]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> ListRuns(
        [FromQuery] int limit = 50,
        [FromQuery] string? kind = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        AgentRunKind? parsedKind = null;
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<AgentRunKind>(kind, true, out var k))
        {
            parsedKind = k;
        }

        AgentRunStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AgentRunStatus>(status, true, out var s))
        {
            parsedStatus = s;
        }

        var views = await agentRunService.ListAsync(limit, parsedKind, parsedStatus, ct);
        return Ok(new { items = views });
    }

    [HttpPost("{id:guid}/result")]
    [Authorize(Roles = Roles.Automatizacion)]
    public async Task<IActionResult> SubmitResult([FromRoute] Guid id, [FromBody] AgentResult result, CancellationToken ct = default)
    {
        var outcome = await agentRunService.SubmitResultAsync(id, result, ct);

        return outcome switch
        {
            ResultOutcome.Accepted => StatusCode(StatusCodes.Status202Accepted, new { status = "Accepted" }),
            ResultOutcome.Late => StatusCode(StatusCodes.Status409Conflict, new { status = "Late", message = "Result received after deadline or run already finished" }),
            ResultOutcome.Invalid => StatusCode(StatusCodes.Status422UnprocessableEntity, new { status = "Invalid", message = "Invalid decisions or payload" }),
            _ => BadRequest()
        };
    }
}
