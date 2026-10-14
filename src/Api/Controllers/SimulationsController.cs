using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Simulation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/simulations/comparisons")]
public sealed class SimulationsController : ControllerBase
{
    private readonly ISimulationService _simulationService;

    public SimulationsController(ISimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    [HttpPost]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> CreateComparison(
        [FromBody] SimulationComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _simulationService.QueueComparisonAsync(request, cancellationToken);
            return Accepted(response);
        }
        catch (SimulationIsolationException ex)
        {
            return UnprocessableEntity(new
            {
                error = "SIMULATION_DATABASE_NOT_ISOLATED",
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new
            {
                error = "UNPROCESSABLE_DATASET",
                message = ex.Message
            });
        }
    }

    [HttpGet]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> ListComparisons(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var list = await _simulationService.ListComparisonsAsync(limit, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> GetComparison(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var detail = await _simulationService.GetComparisonDetailAsync(id, cancellationToken);
        if (detail == null)
        {
            return NotFound(new { message = $"Comparison '{id}' not found." });
        }

        return Ok(detail);
    }

    [HttpGet("{id:guid}/export")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> ExportComparison(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var detail = await _simulationService.GetComparisonDetailAsync(id, cancellationToken);
        if (detail == null)
        {
            return NotFound(new { message = $"Comparison '{id}' not found." });
        }

        if (detail.Status != "Completed")
        {
            return Conflict(new
            {
                error = "SIMULATION_IN_PROGRESS",
                message = "La comparación aún no ha finalizado o no fue completada exitosamente."
            });
        }

        var package = await _simulationService.ExportComparisonAsync(id, cancellationToken);
        if (package == null)
        {
            return Conflict(new
            {
                error = "EXPORT_UNAVAILABLE",
                message = "No se pudo generar el paquete de exportación."
            });
        }

        Response.Headers.ETag = package.ETag;
        return File(package.ZipBytes, "application/zip", $"simulation-{id}.zip");
    }

    [HttpPost("import")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> ImportComparison(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        byte[] bytes;

        if (file != null && file.Length > 0)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }
        else if (Request.Body != null && Request.ContentLength > 0)
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }
        else
        {
            return BadRequest(new { message = "Se requiere un archivo ZIP de simulación para importar." });
        }

        var result = await _simulationService.ImportComparisonAsync(
            bytes,
            hostName: Environment.MachineName,
            commitHash: "imported",
            ct: cancellationToken);

        if (!result.Success)
        {
            return UnprocessableEntity(new
            {
                error = "INVALID_HASH",
                message = result.ErrorMessage
            });
        }

        return Ok(new
        {
            comparisonId = result.ComparisonId,
            importedRunsCount = result.ImportedRunsCount,
            origin = "imported",
            message = "Comparación importada exitosamente."
        });
    }
}
