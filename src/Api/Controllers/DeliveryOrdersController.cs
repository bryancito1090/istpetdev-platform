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

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(limit)
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
    public async Task<IActionResult> CancelOrder([FromRoute] Guid id, CancellationToken ct = default)
    {
        var order = await db.DeliveryOrders.FirstOrDefaultAsync(o => o.OrganizationId == DemoOrganization.Id && o.Id == id, ct);
        if (order == null) return NotFound();

        order.Status = DeliveryOrderStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true, id = order.Id, status = order.Status });
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
