using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/delivery-orders")]
public sealed class DeliveryOrdersController(
    IApplicationDbContext db,
    IOperationalClock clock) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> ListOrders(
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int limit = 50,
        [FromQuery] string? sourceType = null,
        CancellationToken ct = default)
    {
        var query = db.DeliveryOrders.Where(o => o.OrganizationId == DemoOrganization.Id);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(o => o.Priority == priority);
        }

        // #116: la web del Planificador lista solo los pedidos pedidos en campo (FieldRequest).
        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            query = query.Where(o => o.SourceType == sourceType);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);

        var orderIds = orders.Select(o => o.Id).ToList();
        var lines = await db.DeliveryOrderLines
            .Where(l => orderIds.Contains(l.DeliveryOrderId))
            .ToListAsync(ct);

        var products = await db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, ct);

        var servicePoints = await db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(sp => sp.Id, ct);

        var locations = await db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, ct);

        var response = orders.Select(o =>
        {
            servicePoints.TryGetValue(o.DestinationServicePointId ?? Guid.Empty, out var sp);
            locations.TryGetValue(o.DestinationLocationId ?? Guid.Empty, out var loc);

            var ordLines = lines.Where(l => l.DeliveryOrderId == o.Id).Select(l =>
            {
                products.TryGetValue(l.ProductId, out var prod);
                return new
                {
                    id = l.Id,
                    productId = l.ProductId,
                    sku = prod?.Sku ?? "",
                    productName = prod?.Name ?? "",
                    requestedQuantity = l.RequestedQuantity,
                    plannedQuantity = l.PlannedQuantity,
                    deliveredQuantity = l.DeliveredQuantity
                };
            }).ToList();

            return new
            {
                id = o.Id,
                orderNumber = o.OrderNumber,
                destinationServicePointId = o.DestinationServicePointId,
                destinationLocationId = o.DestinationLocationId,
                destinationCode = sp?.Code ?? loc?.Code ?? "DEST",
                destinationName = sp?.Name ?? loc?.Name ?? "Destino",
                isWarehouse = o.DestinationLocationId.HasValue,
                requiredDateUtc = o.RequiredDateUtc,
                priority = o.Priority,
                status = o.Status,
                sourceType = o.SourceType,
                notes = o.Notes,
                createdAtUtc = o.CreatedAtUtc,
                version = o.Version,
                lines = ordLines
            };
        });

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateDeliveryOrderRequest req, CancellationToken ct = default)
    {
        if (req == null || req.Lines == null || req.Lines.Count == 0)
        {
            return BadRequest(new { message = "El pedido debe contener al menos un producto." });
        }

        if (!req.DestinationServicePointId.HasValue && !req.DestinationLocationId.HasValue)
        {
            return BadRequest(new { message = "Debe especificar un destino (punto de servicio o bodega)." });
        }

        var today = await clock.TodayAsync(ct);
        var orderCount = await db.DeliveryOrders.CountAsync(o => o.OrganizationId == DemoOrganization.Id, ct);
        var orderNumber = $"ORD-{today:yyyyMMdd}-{(orderCount + 1):D3}";

        var order = new DeliveryOrder
        {
            OrganizationId = DemoOrganization.Id,
            OrderNumber = orderNumber,
            DestinationServicePointId = req.DestinationServicePointId,
            DestinationLocationId = req.DestinationLocationId,
            RequiredDateUtc = req.RequiredDateUtc ?? DateTime.UtcNow.AddDays(1),
            Priority = req.Priority ?? "Normal",
            Status = DeliveryOrderStatus.Pending,
            SourceType = req.SourceType ?? "Manual",
            Notes = req.Notes ?? ""
        };

        db.DeliveryOrders.Add(order);

        foreach (var l in req.Lines)
        {
            db.DeliveryOrderLines.Add(new DeliveryOrderLine
            {
                OrganizationId = DemoOrganization.Id,
                DeliveryOrderId = order.Id,
                ProductId = l.ProductId,
                RequestedQuantity = l.Quantity,
                PlannedQuantity = 0m,
                DeliveredQuantity = 0m
            });
        }

        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = order.Id,
            orderNumber = order.OrderNumber,
            status = order.Status
        });
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> CancelOrder(
        [FromRoute] Guid id,
        [FromQuery] long? expectedVersion = null,
        CancellationToken ct = default)
    {
        var order = await db.DeliveryOrders.FirstOrDefaultAsync(o => o.OrganizationId == DemoOrganization.Id && o.Id == id, ct);
        if (order == null) return NotFound();

        // Repetir la cancelación de un pedido ya cancelado no es un error (reintento o doble clic).
        if (order.Status == DeliveryOrderStatus.Cancelled)
            return Ok(new { success = true, id = order.Id, status = order.Status, version = order.Version });

        // #116: un pedido ya planificado, despachado o entregado tiene rutas y movimientos asociados;
        // cancelarlo aquí dejaría esos registros sin pedido vigente.
        if (order.Status != DeliveryOrderStatus.Pending)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "order_not_pending",
                detail: $"El pedido {order.OrderNumber} está en estado {order.Status} y ya no se puede cancelar.");

        // El cliente envía la versión que vio; si otro usuario o proceso lo cambió, no se pisa.
        if (expectedVersion.HasValue && expectedVersion.Value != order.Version)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "version_conflict",
                detail: $"El pedido {order.OrderNumber} cambió mientras lo revisabas. Actualiza la lista y vuelve a intentarlo.");

        order.Status = DeliveryOrderStatus.Cancelled;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra transacción (por ejemplo, la planificación de rutas) modificó el pedido entre la lectura y el guardado.
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "version_conflict",
                detail: $"El pedido {order.OrderNumber} cambió mientras se cancelaba. Actualiza la lista y vuelve a intentarlo.");
        }

        return Ok(new { success = true, id = order.Id, status = order.Status, version = order.Version });
    }
}

public sealed record CreateDeliveryOrderRequest(
    Guid? DestinationServicePointId,
    Guid? DestinationLocationId,
    DateTime? RequiredDateUtc,
    string? Priority,
    string? SourceType,
    string? Notes,
    IReadOnlyList<OrderLineInputDto> Lines
);

public sealed record OrderLineInputDto(
    Guid ProductId,
    decimal Quantity
);
