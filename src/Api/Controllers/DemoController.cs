using IstpetDev.Application.Demo;
using IstpetDev.Api.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/demo")]
[Authorize(Roles = Roles.Planning)]
[EnableRateLimiting("imports")]
public sealed class DemoController(IDemoResetService reset, IOptions<DemoOptions> options) : ControllerBase
{
    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromQuery] string? fixture = null, [FromBody] ResetRequest? req = null, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled) return NotFound();
        var selected = fixture ?? req?.Fixture;
        var report = await reset.ResetAsync(selected, cancellationToken, User.AccountId());
        return report.Applied ? Ok(report) : UnprocessableEntity(report);
    }

    [HttpPost("incidents")]
    public IActionResult TriggerPresetIncident([FromBody] PresetIncidentRequest request)
    {
        if (!options.Value.Enabled) return NotFound();

        // Preset conocido "cierre-norte"
        return Ok(new
        {
            preset = request.Preset,
            status = "Triggered"
        });
    }
}

public sealed record PresetIncidentRequest(string Preset);
public sealed record ResetRequest(string? Fixture = null);
