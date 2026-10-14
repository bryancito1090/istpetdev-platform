using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Field;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/mobile/assignments")]
[Authorize]
public sealed class MobileAssignmentsController : ControllerBase
{
    private readonly IFieldService _fieldService;
    private readonly IApplicationDbContext _db;

    public MobileAssignmentsController(IFieldService fieldService, IApplicationDbContext db)
    {
        _fieldService = fieldService;
        _db = db;
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
            var driverAccountId = await ResolveAccountIdAsync(ct);
            var result = await _fieldService.GetMobileAssignmentsAsync(driverAccountId, ct);
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
