using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Authorize]
public sealed class EvidenceController : ControllerBase
{
    private readonly IFieldService _fieldService;
    private readonly IApplicationDbContext _db;

    public EvidenceController(IFieldService fieldService, IApplicationDbContext db)
    {
        _fieldService = fieldService;
        _db = db;
    }

    /// <summary>
    /// T6: Solicitar URL presignada para subida de evidencia privada (foto/firma).
    /// </summary>
    [HttpPost("api/v1/receipts/{id:guid}/evidence-uploads")]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Gerencia}")]
    public async Task<IActionResult> RequestUpload(
        Guid id,
        [FromBody] EvidenceUploadRequest request,
        CancellationToken ct)
    {
        try
        {
            var actorAccountId = await ResolveAccountIdAsync(ct);
            var result = await _fieldService.CreateEvidenceUploadAsync(id, request, actorAccountId, ct);
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
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Gerencia}")]
    public async Task<IActionResult> ConfirmEvidence(
        Guid id,
        [FromBody] EvidenceConfirmRequest request,
        CancellationToken ct)
    {
        try
        {
            var actorAccountId = await ResolveAccountIdAsync(ct);
            var result = await _fieldService.ConfirmEvidenceAsync(id, request, actorAccountId, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    private async Task<Guid> ResolveAccountIdAsync(CancellationToken ct)
    {
        var claimVal = User.FindFirst(AccountRoleClaimsTransformation.AccountIdClaim)?.Value;
        if (Guid.TryParse(claimVal, out var accountId)) return accountId;

        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out var subGuid)) return subGuid;

        if (!string.IsNullOrWhiteSpace(sub))
        {
            var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.Subject == sub && a.OrganizationId == DemoOrganization.Id, ct);
            if (acc != null) return acc.Id;
        }

        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value ?? Roles.Conductor;
        var existingForRole = await _db.Accounts.FirstOrDefaultAsync(a => a.Role == role && a.OrganizationId == DemoOrganization.Id, ct);
        if (existingForRole != null) return existingForRole.Id;

        return DemoOrganization.Id;
    }
}
