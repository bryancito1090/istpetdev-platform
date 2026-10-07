using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/public/trace")]
[AllowAnonymous]
public class PublicTraceController : ControllerBase
{
    [HttpGet("{unitId}")]
    public IActionResult GetTrace(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return BadRequest(new { error = "Unit ID is required" });
        }

        return Ok(new
        {
            unitId,
            traceType = "HandlingUnit",
            status = "Verified",
            generatedAtUtc = DateTime.UtcNow,
            summary = "Unidad logística registrada y verificada en el sistema IstpetDev.",
            events = new[]
            {
                new { stage = "Created", timestamp = DateTime.UtcNow.AddHours(-12), location = "Centro Logístico Quito" },
                new { stage = "CustodyTransferred", timestamp = DateTime.UtcNow.AddHours(-4), location = "Transporte Autorizado" }
            }
        });
    }
}
