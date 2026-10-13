using IstpetDev.Application.Logistics.Policy;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/autonomy/mode")]
public sealed class AutonomyController(AutonomyState state) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Any)]
    public IActionResult GetMode()
    {
        return Ok(new { mode = state.CurrentMode.ToString() });
    }

    [HttpPut]
    [Authorize(Roles = Roles.Gerencia)]
    public IActionResult SetMode([FromBody] UpdateModeInput input)
    {
        if (string.Equals(input.Mode, "ProposeOnly", StringComparison.OrdinalIgnoreCase))
        {
            state.CurrentMode = AutonomyMode.ProposeOnly;
        }
        else if (string.Equals(input.Mode, "AutoWithLimits", StringComparison.OrdinalIgnoreCase))
        {
            state.CurrentMode = AutonomyMode.AutoWithLimits;
        }
        else
        {
            return BadRequest(new { message = "Modo inválido. Valores aceptados: AutoWithLimits, ProposeOnly" });
        }

        return Ok(new { mode = state.CurrentMode.ToString() });
    }
}

public sealed record UpdateModeInput(string Mode);
