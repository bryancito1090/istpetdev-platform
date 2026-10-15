using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

/// <summary>
/// Issue #114: bandeja web de capturas rechazadas por punto (contrato RejectedCapturesResponse de shared-core).
/// La web la consultaba sin que existiera (404). Devuelve las recepciones con alguna línea rechazada por el receptor.
/// Los envíos que el servidor rechaza al sincronizar (409/422) no se guardan en la base: quedan en el outbox de la PWA
/// como «requiere revisión» y no aparecen aquí.
/// </summary>
[ApiController]
[Route("api/v1/deliveries/rejected")]
[Authorize(Roles = $"{Roles.Gerencia},{Roles.Planificador},{Roles.Auditor}")]
public sealed class RejectedCapturesController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? servicePointId = null, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var org = DemoOrganization.Id;
        var rejectedReceiptIds = db.ReceiptLines
            .Where(l => l.OrganizationId == org && l.QuantityRejected > 0)
            .Select(l => l.ReceiptId);

        var query = db.Receipts.Where(r => r.OrganizationId == org && rejectedReceiptIds.Contains(r.Id));
        if (servicePointId.HasValue)
            query = query.Where(r => db.Deliveries.Any(d => d.Id == r.DeliveryId && d.ServicePointId == servicePointId.Value));

        var receipts = await query
            .OrderByDescending(r => r.ReceivedAtServer)
            .ThenByDescending(r => r.Id)
            .Take(Math.Clamp(limit, 1, 200))
            .ToListAsync(ct);
        if (receipts.Count == 0) return Ok(new { items = Array.Empty<object>(), count = 0 });

        var receiptIds = receipts.Select(r => r.Id).ToList();
        var lines = await db.ReceiptLines.Where(l => receiptIds.Contains(l.ReceiptId)).ToListAsync(ct);
        var deliveryLineIds = lines.Select(l => l.DeliveryLineId).Distinct().ToList();
        var deliveryLines = await db.DeliveryLines.Where(dl => deliveryLineIds.Contains(dl.Id)).ToDictionaryAsync(dl => dl.Id, ct);
        var lotIds = deliveryLines.Values.Select(dl => dl.LotId).Distinct().ToList();
        var lots = await db.Lots.Where(l => lotIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
        var productIds = lots.Values.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var deliveryIds = receipts.Select(r => r.DeliveryId).Distinct().ToList();
        var deliveries = await db.Deliveries.Where(d => deliveryIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, ct);
        var pointIds = deliveries.Values.Select(d => d.ServicePointId).Distinct().ToList();
        var points = await db.ServicePoints.Where(p => pointIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var actorIds = receipts.Select(r => r.ActorAccountId).Distinct().ToList();
        var actors = await db.Accounts.Where(a => actorIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.DisplayName, ct);

        var items = receipts.Select(r =>
        {
            deliveries.TryGetValue(r.DeliveryId, out var delivery);
            var point = delivery != null && points.TryGetValue(delivery.ServicePointId, out var p) ? p : null;
            var receiptLines = lines.Where(l => l.ReceiptId == r.Id).Select(l =>
            {
                var lot = deliveryLines.TryGetValue(l.DeliveryLineId, out var dl) && lots.TryGetValue(dl.LotId, out var lt) ? lt : null;
                var product = lot != null && products.TryGetValue(lot.ProductId, out var pr) ? pr : null;
                return new
                {
                    productId = product?.Id ?? Guid.Empty,
                    productName = product?.Name ?? "",
                    sku = product?.Sku ?? "",
                    quantityAccepted = l.QuantityAccepted,
                    quantityRejected = l.QuantityRejected,
                    rejectionReason = l.RejectionReason
                };
            }).ToList();

            var reasons = receiptLines.Where(l => l.quantityRejected > 0)
                .Select(l => string.IsNullOrWhiteSpace(l.rejectionReason) ? "Rechazo sin motivo registrado" : l.rejectionReason!.Trim())
                .Distinct().ToList();

            return new
            {
                receiptId = r.Id,
                deliveryId = r.DeliveryId,
                deliveryCode = delivery?.Code ?? "",
                servicePointId = delivery?.ServicePointId ?? Guid.Empty,
                servicePointCode = point?.Code ?? "",
                servicePointName = point?.Name ?? "",
                driverId = r.ActorAccountId,
                driverName = actors.TryGetValue(r.ActorAccountId, out var name) ? name : "",
                receivedAtUtc = r.ReceivedAtServer,
                reason = string.Join(" · ", reasons),
                status = delivery?.Status ?? "",
                lines = receiptLines
            };
        }).ToList();

        return Ok(new { items, count = items.Count });
    }
}
