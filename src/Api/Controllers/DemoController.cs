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
    public async Task<IActionResult> Reset(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return NotFound();
        var report = await reset.ResetAsync(cancellationToken, User.AccountId());
        return report.Applied ? Ok(report) : UnprocessableEntity(report);
    }
}
