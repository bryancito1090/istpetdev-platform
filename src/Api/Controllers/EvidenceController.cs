using System;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Authorize]
public sealed class EvidenceController : ControllerBase
{
    private readonly IFieldService _fieldService;

    public EvidenceController(IFieldService fieldService)
    {
        _fieldService = fieldService;
    }

    /// <summary>
    /// T6: Solicitar URL presignada para subida de evidencia privada (foto/firma).
    /// </summary>
    [HttpPost("api/v1/receipts/{id:guid}/evidence-uploads")]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Receptor},{Roles.Gerencia}")]
    public async Task<IActionResult> RequestUpload(
        Guid id,
        [FromBody] EvidenceUploadRequest request,
        CancellationToken ct)
    {
        try
        {
            var actorAccountId = User.AccountId();
            if (actorAccountId is null) return Forbid();
            var result = await _fieldService.CreateEvidenceUploadAsync(id, request, actorAccountId.Value, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// T6: Confirmar almacenamiento de evidencia privada tras subida a S3.
    /// </summary>
    [HttpPost("api/v1/evidence/{id:guid}/confirm")]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Receptor},{Roles.Gerencia}")]
    public async Task<IActionResult> ConfirmEvidence(
        Guid id,
        [FromBody] EvidenceConfirmRequest request,
        CancellationToken ct)
    {
        try
        {
            var actorAccountId = User.AccountId();
            if (actorAccountId is null) return Forbid();
            var result = await _fieldService.ConfirmEvidenceAsync(id, request, actorAccountId.Value, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }
}
