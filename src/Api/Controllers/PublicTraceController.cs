using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.PublicTrace.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/public/trace")]
[AllowAnonymous]
public class PublicTraceController : ControllerBase
{
    private readonly IPublicTraceService _traceService;

    public PublicTraceController(IPublicTraceService traceService)
    {
        _traceService = traceService;
    }

    [HttpGet("{unitId}")]
    public async Task<IActionResult> GetTrace(string unitId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return BadRequest(new { error = "Unit ID is required" });
        }

        var trace = await _traceService.GetTraceAsync(unitId, cancellationToken);
        if (trace is null)
        {
            return NotFound(new { error = "Unit not found" });
        }

        return Ok(trace);
    }
}
