using System;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/deliveries")]
[Authorize]
public sealed class DeliveriesController : ControllerBase
{
    private readonly IFieldService _fieldService;

    public DeliveriesController(IFieldService fieldService)
    {
        _fieldService = fieldService;
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
            var actorAccountId = User.AccountId();
            if (actorAccountId is null) return Forbid();
            var result = await _fieldService.CreateHandlingUnitAsync(id, request, actorAccountId.Value, ct);
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
            var actorAccountId = User.AccountId();
            if (actorAccountId is null) return Forbid();
            var result = await _fieldService.DispatchAsync(id, request, actorAccountId.Value, key, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// #162: el despacho ya no autoriza al Conductor a confirmar stock en destino.
    /// La recepción final la registra el Receptor con el QR del bulto.
    /// </summary>
    [HttpPost("{id:guid}/receipts")]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.Gerencia}")]
    public IActionResult CreateReceipt(Guid id, [FromBody] CreateReceiptRequest request)
    {
        return Problem(
            detail: "La recepción final la registra el Receptor escaneando el QR del bulto entregado.",
            statusCode: StatusCodes.Status409Conflict,
            title: FieldErrorCodes.ReceiptRequiresReceiver);
    }

    /// <summary>
    /// #162: el conductor asignado recoge los bultos despachados (Dispatched → InTransit).
    /// </summary>
    [HttpPost("{id:guid}/pickup")]
    [Authorize(Roles = Roles.Conductor)]
    public async Task<IActionResult> Pickup(
        Guid id,
        [FromBody] CustodyTransitionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        try
        {
            var key = !string.IsNullOrWhiteSpace(idempotencyKey) ? idempotencyKey : request.OperationId.ToString();
            var actorAccountId = User.AccountId();
            if (actorAccountId is null) return Forbid();
            var result = await _fieldService.PickupAsync(id, request, actorAccountId.Value, key, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// #162: el conductor asignado declara la entrega física en el punto (InTransit → Delivered).
    /// </summary>
    [HttpPost("{id:guid}/deliver")]
    [Authorize(Roles = Roles.Conductor)]
    public async Task<IActionResult> Deliver(
        Guid id,
        [FromBody] CustodyTransitionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        try
        {
            var key = !string.IsNullOrWhiteSpace(idempotencyKey) ? idempotencyKey : request.OperationId.ToString();
            var actorAccountId = User.AccountId();
            if (actorAccountId is null) return Forbid();
            var result = await _fieldService.DeliverAsync(id, request, actorAccountId.Value, key, ct);
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
    [Authorize(Roles = $"{Roles.Gerencia},{Roles.Planificador},{Roles.Bodega},{Roles.Auditor}")]
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

    /// <summary>
    /// Spec 004 / Issue #121: Detalle de la entrega con líneas y bultos creados. Rol Bodega y Gerencia.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Bodega},{Roles.Gerencia}")]
    public async Task<IActionResult> GetDeliveryDetail(
        Guid id,
        CancellationToken ct)
    {
        try
        {
            var result = await _fieldService.GetDeliveryDetailAsync(id, ct);
            return Ok(result);
        }
        catch (FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }

    /// <summary>
    /// Issue #117: Catálogo normativo versionado de motivos de rechazo de recepción en campo.
    /// </summary>
    [HttpGet("rejection-reasons")]
    [Authorize]
    public IActionResult GetRejectionReasons()
    {
        var catalog = _fieldService.GetRejectionReasonsCatalog();
        return Ok(catalog);
    }
}
