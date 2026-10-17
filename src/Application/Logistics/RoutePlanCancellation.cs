using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Logistics;

public sealed record CancelledOrderLine(
    Guid OrderId,
    string OrderNumber,
    string Sku,
    decimal ReleasedQuantity,
    decimal PlannedQuantity,
    decimal PendingQuantity,
    string OrderStatus);

public sealed record RoutePlanCancellationResult(
    Guid RoutePlanId,
    string Status,
    bool AlreadyCancelled,
    int CancelledDeliveries,
    decimal ReleasedReservedQuantity,
    IReadOnlyList<CancelledOrderLine> OrderLines);

/// <summary>
/// #199: cancelar una ruta confirmada antes del despacho. Libera sus reservas, cancela sus entregas en Draft y
/// descuenta de cada línea de pedido lo que esta ruta le había sumado a <c>PlannedQuantity</c>; el pedido vuelve a
/// <c>Pending</c> si ya no está planificado por completo.
/// </summary>
public static class RoutePlanCancellation
{
    public const string CancelledStatus = "Cancelled";

    public static async Task<RoutePlanCancellationResult> CancelAsync(IApplicationDbContext db, Guid routePlanId, CancellationToken ct = default)
    {
        var plan = await db.RoutePlans
            .FirstOrDefaultAsync(p => p.Id == routePlanId && p.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"RoutePlan {routePlanId} no encontrado.");

        // Idempotencia: cancelar dos veces no descuenta dos veces.
        if (plan.Status == CancelledStatus)
            return new RoutePlanCancellationResult(plan.Id, plan.Status, true, 0, 0m, []);

        if (plan.Status is not ("Proposed" or "Published"))
            throw new InvalidOperationException($"Solo se cancela una ruta propuesta o publicada; esta está en {plan.Status}. Cancela la revisión vigente.");
        // Revertir deja la revisión anterior en Published: solo la vigente descuenta, para no restar dos veces.
        if (await db.RoutePlans.AnyAsync(p => p.SupersedesRoutePlanId == plan.Id && p.OrganizationId == DemoOrganization.Id && p.Status == "Published", ct))
            throw new InvalidOperationException("La ruta tiene una revisión posterior; cancela la revisión vigente.");

        var deliveries = await db.Deliveries
            .Where(d => d.OrganizationId == DemoOrganization.Id &&
                        db.RouteStops.Any(s => s.RoutePlanId == plan.Id && s.Id == d.RouteStopId))
            .ToListAsync(ct);
        if (deliveries.Any(d => d.Status is not (DeliveryStates.Draft or DeliveryStates.Cancelled)))
            throw new InvalidOperationException("No se puede cancelar: la ruta tiene entregas despachadas, entregadas o recibidas.");

        // 1. Liberar las reservas activas de la ruta.
        var reservations = await db.RouteStockReservations
            .Where(r => r.OrganizationId == DemoOrganization.Id && r.RoutePlanId == plan.Id && r.Status == RouteStockReservationStatus.Active)
            .ToListAsync(ct);
        var balanceIds = reservations.Select(r => r.StockBalanceId).Distinct().ToList();
        var balances = await db.StockBalances.Where(b => balanceIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, ct);
        foreach (var reservation in reservations)
        {
            if (balances.TryGetValue(reservation.StockBalanceId, out var balance))
                balance.QuantityReserved = Math.Max(0m, balance.QuantityReserved - reservation.Quantity);
            reservation.Status = RouteStockReservationStatus.Released;
            reservation.ClosedAtUtc = DateTime.UtcNow;
        }

        // 2. Cancelar las entregas pendientes de despacho.
        var drafts = deliveries.Where(d => d.Status == DeliveryStates.Draft).ToList();
        foreach (var delivery in drafts) delivery.Status = DeliveryStates.Cancelled;

        // 3. Descontar lo que la ruta sumó a cada línea de pedido (guardado en confirm; las revisiones lo heredan).
        var planned = await PlannedOrderLinesAsync(db, plan, ct);
        var orderLineIds = planned.Keys.ToList();
        var lines = await db.DeliveryOrderLines
            .Where(l => l.OrganizationId == DemoOrganization.Id && orderLineIds.Contains(l.Id))
            .ToListAsync(ct);
        var orderIds = lines.Select(l => l.DeliveryOrderId).Distinct().ToList();
        var orders = await db.DeliveryOrders
            .Where(o => o.OrganizationId == DemoOrganization.Id && orderIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, ct);
        var allOrderLines = await db.DeliveryOrderLines
            .Where(l => l.OrganizationId == DemoOrganization.Id && orderIds.Contains(l.DeliveryOrderId))
            .ToListAsync(ct);
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var skus = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Sku, ct);

        var released = new List<(DeliveryOrderLine Line, decimal Quantity)>();
        foreach (var line in lines)
        {
            var quantity = Math.Min(planned[line.Id], line.PlannedQuantity);
            line.PlannedQuantity -= quantity;
            released.Add((line, quantity));
        }

        foreach (var order in orders.Values.Where(o => o.Status is DeliveryOrderStatus.Planned or DeliveryOrderStatus.Pending))
        {
            var ordLines = allOrderLines.Where(l => l.DeliveryOrderId == order.Id).ToList();
            var fullyPlanned = ordLines.Count > 0 && ordLines.All(l => l.PlannedQuantity + l.DeliveredQuantity >= l.RequestedQuantity);
            order.Status = fullyPlanned ? DeliveryOrderStatus.Planned : DeliveryOrderStatus.Pending;
        }

        plan.Status = CancelledStatus;
        await db.SaveChangesAsync(ct);

        var result = released
            .Select(r =>
            {
                var order = orders[r.Line.DeliveryOrderId];
                return new CancelledOrderLine(order.Id, order.OrderNumber, skus.GetValueOrDefault(r.Line.ProductId, ""), r.Quantity,
                    r.Line.PlannedQuantity, Math.Max(0m, r.Line.RequestedQuantity - r.Line.PlannedQuantity - r.Line.DeliveredQuantity), order.Status);
            })
            .OrderBy(l => l.OrderNumber, StringComparer.Ordinal).ThenBy(l => l.Sku, StringComparer.Ordinal)
            .ToList();
        return new RoutePlanCancellationResult(plan.Id, plan.Status, false, drafts.Count, reservations.Sum(r => r.Quantity), result);
    }

    // Las revisiones por incidente no copian los pedidos: se buscan en la revisión más cercana que los tenga.
    private static async Task<Dictionary<Guid, decimal>> PlannedOrderLinesAsync(IApplicationDbContext db, RoutePlan plan, CancellationToken ct)
    {
        var current = plan;
        for (var depth = 0; current is not null && depth < 20; depth++)
        {
            var lines = ParseOrderLines(current.InputJson);
            if (lines is not null) return lines;
            if (!current.SupersedesRoutePlanId.HasValue) break;
            var previousId = current.SupersedesRoutePlanId.Value;
            current = await db.RoutePlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == previousId && p.OrganizationId == DemoOrganization.Id, ct);
        }
        return [];
    }

    public static Dictionary<Guid, decimal>? ParseOrderLines(string? inputJson)
    {
        if (string.IsNullOrWhiteSpace(inputJson)) return null;
        using var doc = JsonDocument.Parse(inputJson);
        if (!doc.RootElement.TryGetProperty("orderLines", out var linesElem) || linesElem.ValueKind != JsonValueKind.Array) return null;
        var result = new Dictionary<Guid, decimal>();
        foreach (var e in linesElem.EnumerateArray())
        {
            if (!e.TryGetProperty("deliveryOrderLineId", out var idElem) || !idElem.TryGetGuid(out var id)) continue;
            var qty = e.TryGetProperty("plannedQuantity", out var q) && q.TryGetDecimal(out var v) ? v : 0m;
            if (qty > 0) result[id] = result.GetValueOrDefault(id) + qty;
        }
        return result;
    }
}
