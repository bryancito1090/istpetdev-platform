using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Api.Controllers;

/// <summary>
/// Issue #116: recepciones con novedades para la web del Planificador, Gerencia y Auditor (solo lectura).
/// Novedad = alguna línea con cantidad rechazada (ESC-06) o con ajuste por stock en percha (FieldAdjustment, ESC-09).
/// #237: cada recepción con rechazo Faltante, Daño o ErrorRotulado lleva la reentrega estimada (ida y vuelta).
/// </summary>
[ApiController]
[Route("api/v1/receipts")]
[Authorize]
public sealed class ReceiptsController(
    IApplicationDbContext db,
    IDistanceProvider distanceProvider,
    IOptions<LogisticsOptions> logisticsOptions) : ControllerBase
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
        var backtracks = await EstimateBacktracksAsync(receipts, lines, deliveries, points, deliveryLines, lots, products, ct);
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
                evidenceDtos,
                backtracks.GetValueOrDefault(r.Id));
        }).ToList();

        return Ok(new ReceiptExceptionsResponse(items, total));
    }

    // #237: una reentrega por recepción, bodega de origen de la entrega → punto → bodega. Sin coordenadas
    // de alguno de los dos extremos no se estima (no se inventa una distancia).
    private async Task<Dictionary<Guid, EstimatedBacktrackDto>> EstimateBacktracksAsync(
        IReadOnlyList<Receipt> receipts,
        IReadOnlyList<ReceiptLine> lines,
        IReadOnlyDictionary<Guid, Delivery> deliveries,
        IReadOnlyDictionary<Guid, ServicePoint> points,
        IReadOnlyDictionary<Guid, DeliveryLine> deliveryLines,
        IReadOnlyDictionary<Guid, Lot> lots,
        IReadOnlyDictionary<Guid, Product> products,
        CancellationToken ct)
    {
        var result = new Dictionary<Guid, EstimatedBacktrackDto>();

        // Peso a reentregar por recepción: solo líneas cuyo motivo obliga a volver con producto.
        var weights = new Dictionary<Guid, decimal>();
        foreach (var l in lines.Where(l => l.QuantityRejected > 0 && BacktrackCostEstimator.RequiresRedelivery(l.RejectionReason)))
        {
            var product = deliveryLines.TryGetValue(l.DeliveryLineId, out var dl) && lots.TryGetValue(dl.LotId, out var lot)
                && products.TryGetValue(lot.ProductId, out var pr) ? pr : null;
            weights[l.ReceiptId] = weights.GetValueOrDefault(l.ReceiptId) + l.QuantityRejected * (product?.WeightPerBaseUnit ?? 0m);
        }
        if (weights.Count == 0) return result;

        var candidates = receipts.Where(r => weights.ContainsKey(r.Id) && deliveries.ContainsKey(r.DeliveryId)).ToList();
        var originIds = candidates.Select(r => deliveries[r.DeliveryId].OriginLocationId).Distinct().ToList();
        var origins = (await db.Locations.AsNoTracking().Where(l => originIds.Contains(l.Id) && l.Position != null).ToListAsync(ct))
            .ToDictionary(l => l.Id, l => new GeoPoint((decimal)l.Position!.Y, (decimal)l.Position.X).Round());

        // Vehículo de la ruta que llevó la entrega, para su tarifa o perfil de costo; sin ruta, DefaultCostPerKm.
        var stopIds = candidates.Select(r => deliveries[r.DeliveryId].RouteStopId).OfType<Guid>().Distinct().ToList();
        var planByStop = await db.RouteStops.AsNoTracking().Where(s => stopIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.RoutePlanId, ct);
        var planIds = planByStop.Values.Distinct().ToList();
        var vehicleByPlan = await db.RoutePlans.AsNoTracking().Where(p => planIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.VehicleId, ct);
        var vehicleIds = vehicleByPlan.Values.Distinct().ToList();
        var vehicles = await db.Vehicles.AsNoTracking().Where(v => vehicleIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);

        var trips = new List<(Guid ReceiptId, GeoPoint From, GeoPoint To, Vehicle? Vehicle)>();
        foreach (var r in candidates)
        {
            var delivery = deliveries[r.DeliveryId];
            if (!origins.TryGetValue(delivery.OriginLocationId, out var from)) continue;
            if (!points.TryGetValue(delivery.ServicePointId, out var point) || point.Position is null) continue;
            var to = new GeoPoint((decimal)point.Position.Y, (decimal)point.Position.X).Round();
            Vehicle? vehicle = delivery.RouteStopId is Guid stopId && planByStop.TryGetValue(stopId, out var planId)
                && vehicleByPlan.TryGetValue(planId, out var vehicleId) && vehicles.TryGetValue(vehicleId, out var v) ? v : null;
            trips.Add((r.Id, from, to, vehicle));
        }
        if (trips.Count == 0) return result;

        // Una sola consulta de matriz: tramos guardados, OSRM o aproximación en línea recta.
        var matrix = await distanceProvider.MatrixAsync(trips.SelectMany(t => new[] { t.From, t.To }).Distinct().ToList(), ct);
        var options = logisticsOptions.Value;
        foreach (var t in trips)
        {
            if (!matrix.TryGetValue((t.From, t.To), out var outbound) || !matrix.TryGetValue((t.To, t.From), out var inbound)) continue;
            var estimate = BacktrackCostEstimator.Estimate(outbound, inbound, t.Vehicle, weights[t.ReceiptId], options);
            result[t.ReceiptId] = new EstimatedBacktrackDto(estimate.Km, estimate.CostUsd, estimate.Hours, estimate.IsApproximate);
        }
        return result;
    }
}
