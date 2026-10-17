using System;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/return-guides")]
[Authorize]
public sealed class ReturnGuidesController : ControllerBase
{
    private readonly IFieldService _fieldService;

    public ReturnGuidesController(IFieldService fieldService)
    {
        _fieldService = fieldService;
    }

    /// <summary>
    /// Issue #247: Genera la guía de retorno para bultos no entregados al cerrar la ruta.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Bodega},{Roles.Planificador},{Roles.Gerencia}")]
    public async Task<IActionResult> GenerateGuide(
        [FromBody] GenerateReturnGuideRequest request,
        CancellationToken ct)
    {
        try
        {
            var actorAccountId = User.AccountId() ?? Guid.NewGuid();
            var guide = await _fieldService.GenerateReturnGuideAsync(request.RoutePlanId, actorAccountId, ct);
            return Ok(guide);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// Issue #247: Consulta la guía de retorno por su número (p. ej. GRT-B1C2D3E4).
    /// </summary>
    [HttpGet("{guideNumber}")]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Bodega},{Roles.Planificador},{Roles.Gerencia},{Roles.Auditor}")]
    public async Task<IActionResult> GetGuide(
        string guideNumber,
        CancellationToken ct)
    {
        try
        {
            var guide = await _fieldService.GetReturnGuideAsync(guideNumber, ct);
            return Ok(guide);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// Issue #247: Recepción en bodega de la guía de retorno, conciliando inventario y reinyectando demanda prioritaria.
    /// </summary>
    [HttpPost("receive")]
    [Authorize(Roles = $"{Roles.Bodega},{Roles.Gerencia}")]
    public async Task<IActionResult> ReceiveGuide(
        [FromBody] ReceiveReturnGuideRequest request,
        CancellationToken ct)
    {
        try
        {
            var actorAccountId = User.AccountId() ?? Guid.NewGuid();
            var response = await _fieldService.ReceiveReturnGuideAsync(request, actorAccountId, ct);
            return Ok(response);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }
}
