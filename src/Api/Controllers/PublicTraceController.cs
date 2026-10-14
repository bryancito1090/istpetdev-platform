using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.PublicTrace.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        Response.Headers.CacheControl = "no-store";

        // FR-13: El token nunca debe viajar en la query string. Si viene en query string, 404 inmediato.
        if (Request.Query.ContainsKey("token"))
        {
            return NotFound();
        }

        // FR-13: Requiere obligatoriamente cabecera X-Trace-Token
        if (!Request.Headers.TryGetValue("X-Trace-Token", out var tokenValues) || string.IsNullOrWhiteSpace(tokenValues))
        {
            return NotFound();
        }

        string token = tokenValues.ToString();
        var result = await _traceService.GetTraceAsync(unitId, token, cancellationToken);

        return result switch
        {
            PublicTraceResult.Success success => Ok(success.Trace),
            PublicTraceResult.RateLimited => StatusCode(StatusCodes.Status429TooManyRequests, new { error = "RATE_LIMIT_EXCEEDED" }),
            _ => NotFound()
        };
    }
}
