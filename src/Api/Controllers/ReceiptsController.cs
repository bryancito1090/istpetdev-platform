using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

/// <summary>
/// Issue #116: recepciones con novedades para la web del Planificador, Gerencia y Auditor (solo lectura).
/// Novedad = alguna línea con cantidad rechazada (ESC-06) o con ajuste por stock en percha (FieldAdjustment, ESC-09).
/// </summary>
[ApiController]
[Route("api/v1/receipts")]
[Authorize]
public sealed class ReceiptsController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet("exceptions")]
    [Authorize(Roles = $"{Roles.Gerencia},{Roles.Planificador},{Roles.Auditor}")]
    public async Task<ActionResult<ReceiptExceptionsResponse>> ListExceptions(
        [FromQuery] Guid? servicePointId = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var org = DemoOrganization.Id;

        var adjustedLineIds = db.InventoryMovements
            .Where(m => m.OrganizationId == org && m.Kind == MovementKinds.FieldAdjustment && m.ReceiptLineId != null)
            .Select(m => m.ReceiptLineId!.Value);
        var exceptionReceiptIds = db.ReceiptLines
            .Where(l => l.OrganizationId == org && (l.QuantityRejected > 0 || adjustedLineIds.Contains(l.Id)))
            .Select(l => l.ReceiptId);

        var query = db.Receipts.Where(r => r.OrganizationId == org && exceptionReceiptIds.Contains(r.Id));
        if (servicePointId.HasValue)
            query = query.Where(r => db.Deliveries.Any(d => d.Id == r.DeliveryId && d.ServicePointId == servicePointId.Value));

        var total = await query.CountAsync(ct);
        var receipts = await query
            .OrderByDescending(r => r.ReceivedAtServer)
            .ThenByDescending(r => r.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(ct);
        if (receipts.Count == 0) return Ok(new ReceiptExceptionsResponse([], total));

        var receiptIds = receipts.Select(r => r.Id).ToList();
        var lines = await db.ReceiptLines.Where(l => receiptIds.Contains(l.ReceiptId)).ToListAsync(ct);
        var lineIds = lines.Select(l => l.Id).ToList();
        var adjustments = (await db.InventoryMovements
                .Where(m => m.Kind == MovementKinds.FieldAdjustment && m.ReceiptLineId != null && lineIds.Contains(m.ReceiptLineId.Value))
                .ToListAsync(ct))
            .ToLookup(m => m.ReceiptLineId!.Value);

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
        var evidence = (await db.Evidences.Where(e => receiptIds.Contains(e.ReceiptId)).OrderBy(e => e.CreatedAtUtc).ToListAsync(ct))
            .ToLookup(e => e.ReceiptId);

        var items = receipts.Select(r =>
        {
            deliveries.TryGetValue(r.DeliveryId, out var delivery);
            var point = delivery != null && points.TryGetValue(delivery.ServicePointId, out var p) ? p : null;

            var lineDtos = lines.Where(l => l.ReceiptId == r.Id).Select(l =>
            {
                var lot = deliveryLines.TryGetValue(l.DeliveryLineId, out var dl) && lots.TryGetValue(dl.LotId, out var lt) ? lt : null;
                var product = lot != null && products.TryGetValue(lot.ProductId, out var pr) ? pr : null;
                var lineAdjustments = adjustments[l.Id].ToList();
                // Entrada al punto = conteo mayor que el saldo (+); salida = consumo derivado (−).
                decimal? signed = lineAdjustments.Count == 0 ? null
                    : lineAdjustments.Sum(m => m.ToLocationId.HasValue ? m.Quantity : -m.Quantity);
                return new ReceiptExceptionLineDto(
                    l.Id,
                    product?.Sku ?? "",
                    product?.Name ?? "",
                    lot?.Code ?? "",
                    l.QuantityAccepted,
                    l.QuantityRejected,
                    l.RejectionReason,
                    l.RemainingQuantity,
                    signed,
                    lineAdjustments.Count == 0 ? null : string.Join(" · ", lineAdjustments.Select(m => m.Reason)));
            }).ToList();

            var evidenceDtos = evidence[r.Id].Select(e => new ReceiptEvidenceDto(
                e.Id, e.Kind, e.Status, e.MimeType, e.ByteSize, e.Sha256, e.ConfirmedAtUtc)).ToList();

            return new ReceiptExceptionDto(
                r.Id,
                r.DeliveryId,
                delivery?.Code ?? "",
                delivery?.ServicePointId ?? Guid.Empty,
                point?.Code ?? "",
                point?.Name ?? "",
                actors.TryGetValue(r.ActorAccountId, out var name) ? name : "",
                r.ReceivedAtServer,
                r.OccurredAtDevice,
                lineDtos.Any(l => l.QuantityRejected > 0),
                lineDtos.Any(l => l.AdjustmentQuantity.HasValue),
                lineDtos,
                evidenceDtos);
        }).ToList();

        return Ok(new ReceiptExceptionsResponse(items, total));
    }
}
