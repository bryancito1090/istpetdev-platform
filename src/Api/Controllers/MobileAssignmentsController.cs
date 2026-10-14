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
[Route("api/v1/mobile/assignments")]
[Authorize]
public sealed class MobileAssignmentsController : ControllerBase
{
    private readonly IFieldService _fieldService;

    public MobileAssignmentsController(IFieldService fieldService)
    {
        _fieldService = fieldService;
    }

    /// <summary>
    /// FR-04 & ALK-21: Descarga de rutas y entregas asignadas al conductor logueado.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Gerencia}")]
    public async Task<IActionResult> GetAssignments(CancellationToken ct)
    {
        try
        {
            var driverAccountId = User.AccountId();
            if (driverAccountId is null) return Forbid();
            var result = await _fieldService.GetMobileAssignmentsAsync(driverAccountId.Value, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }
}
