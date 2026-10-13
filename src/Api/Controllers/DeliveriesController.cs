using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/deliveries")]
[Authorize]
public sealed class DeliveriesController : ControllerBase
{
    private readonly IFieldService _fieldService;
    private readonly IApplicationDbContext _db;

    public DeliveriesController(IFieldService fieldService, IApplicationDbContext db)
    {
        _fieldService = fieldService;
        _db = db;
    }

    /// <summary>
    /// FR-01 & FR-02: Preparar caja/HandlingUnit y emitir token QR. Rol Bodega.
    /// </summary>
    [HttpPost("{id:guid}/handling-units")]
    [Authorize(Roles = $"{Roles.Bodega},{Roles.Gerencia}")]
    public async Task<IActionResult> CreateHandlingUnit(
        Guid id,
        [FromBody] CreateHandlingUnitRequest request,
        CancellationToken ct)
    {
        try
        {
            var actorAccountId = await ResolveAccountIdAsync(ct);
            var result = await _fieldService.CreateHandlingUnitAsync(id, request, actorAccountId, ct);
            return StatusCode(201, result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// FR-03: Despacho atómico e idempotente de caja a vehículo con custodia. Rol Bodega.
    /// </summary>
    [HttpPost("{id:guid}/dispatch")]
    [Authorize(Roles = $"{Roles.Bodega},{Roles.Gerencia}")]
    public async Task<IActionResult> Dispatch(
        Guid id,
        [FromBody] DispatchRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        try
        {
            var key = !string.IsNullOrWhiteSpace(idempotencyKey) ? idempotencyKey : request.OperationId.ToString();
            var actorAccountId = await ResolveAccountIdAsync(ct);
            var result = await _fieldService.DispatchAsync(id, request, actorAccountId, key, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// FR-06 & FR-07: Recepción en punto, conciliación de cantidades y completar parada. Conductor asignado.
    /// </summary>
    [HttpPost("{id:guid}/receipts")]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Gerencia}")]
    public async Task<IActionResult> CreateReceipt(
        Guid id,
        [FromBody] CreateReceiptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        try
        {
            var key = !string.IsNullOrWhiteSpace(idempotencyKey) ? idempotencyKey : request.OperationId.ToString();
            var actorAccountId = await ResolveAccountIdAsync(ct);
            var result = await _fieldService.CreateReceiptAsync(id, request, actorAccountId, key, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// FR-19: Lectura paginada de entregas para el panel de control (005).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Gerencia},{Roles.Planificador},{Roles.Bodega}")]
    public async Task<IActionResult> ListDeliveries(
        [FromQuery] Guid? routePlanId = null,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        var result = await _fieldService.GetDeliveriesAsync(routePlanId, status, limit, cursor, ct);
        return Ok(result);
    }

    private async Task<Guid> ResolveAccountIdAsync(CancellationToken ct)
    {
        var claimVal = User.FindFirst(AccountRoleClaimsTransformation.AccountIdClaim)?.Value;
        if (Guid.TryParse(claimVal, out var accountId)) return accountId;

        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out var subGuid))
        {
            return subGuid;
        }

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
