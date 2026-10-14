using System;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Field;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/handling-units")]
[Authorize]
public sealed class HandlingUnitsController : ControllerBase
{
    private readonly IFieldService _fieldService;

    public HandlingUnitsController(IFieldService fieldService)
    {
        _fieldService = fieldService;
    }

    /// <summary>
    /// Custodia operativa autorizada para personal interno.
    /// </summary>
    [HttpGet("{id:guid}/trace")]
    [Authorize(Roles = $"{Roles.Bodega},{Roles.Conductor},{Roles.Planificador},{Roles.Gerencia},{Roles.Auditor}")]
    public async Task<IActionResult> GetOperativeTrace(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _fieldService.GetOperativeTraceAsync(id, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// Revocación del enlace público emitido para una unidad.
    /// </summary>
    [HttpPost("{id:guid}/public-token/revoke")]
    [Authorize(Roles = $"{Roles.Bodega},{Roles.Gerencia}")]
    public async Task<IActionResult> RevokePublicToken(Guid id, CancellationToken ct)
    {
        try
        {
            var actorAccountId = User.AccountId();
            if (actorAccountId is null) return Forbid();
            await _fieldService.RevokePublicTokenAsync(id, actorAccountId.Value, ct);
            return NoContent();
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }
}
