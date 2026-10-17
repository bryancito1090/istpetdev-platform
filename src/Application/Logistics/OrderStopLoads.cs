using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Logistics;

/// <summary>
/// Rutas del asistente (pedidos y emergencia): sus paradas no guardan cargas, pero el plan guarda los pedidos.
/// A cada parada de entrega sin cargas se le asignan las líneas pendientes de los pedidos de su punto.
/// Lo usan la publicación (para crear las entregas) y el detalle del plan (para mostrar qué se lleva).
/// </summary>
public static class OrderStopLoads
{
    public static IReadOnlyList<Guid> OrderIds(JsonElement root)
    {
        if (!root.TryGetProperty("orderIds", out var idsElem) || idsElem.ValueKind != JsonValueKind.Array) return [];
        return idsElem.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => Guid.TryParse(e.GetString(), out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToList();
    }

    public static async Task FillAsync(IApplicationDbContext db, IReadOnlyList<Guid> orderIds, IEnumerable<RouteStop> stops,
        Dictionary<int, List<StopLoad>> loadsBySeq, CancellationToken ct)
    {
        if (orderIds.Count == 0) return;

        var orders = await db.DeliveryOrders
            .Where(o => o.OrganizationId == DemoOrganization.Id && orderIds.Contains(o.Id) && o.Status != DeliveryOrderStatus.Cancelled)
            .ToListAsync(ct);
        var ids = orders.Select(o => o.Id).ToList();
        var lines = await db.DeliveryOrderLines.Where(l => l.OrganizationId == DemoOrganization.Id && ids.Contains(l.DeliveryOrderId)).ToListAsync(ct);
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => p.OrganizationId == DemoOrganization.Id && productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        foreach (var stop in stops.Where(s => s.Kind != RouteStopKind.Pickup))
        {
            if (loadsBySeq.TryGetValue(stop.SequenceNumber, out var existing) && existing.Count > 0) continue;
            var pointOrders = orders.Where(o =>
                (stop.ServicePointId != Guid.Empty && o.DestinationServicePointId == stop.ServicePointId) ||
                (stop.LocationId.HasValue && o.DestinationLocationId == stop.LocationId)
            ).Select(o => o.Id).ToHashSet();

            var loads = lines.Where(l => pointOrders.Contains(l.DeliveryOrderId))
                .GroupBy(l => l.ProductId)
                .Select(g => (ProductId: g.Key, Quantity: g.Sum(l => l.PlannedQuantity > 0 ? l.PlannedQuantity : Math.Max(0m, l.RequestedQuantity - l.DeliveredQuantity))))
                .Where(x => x.Quantity > 0 && products.ContainsKey(x.ProductId))
                .Select(x => new StopLoad(x.ProductId, products[x.ProductId].Sku, x.Quantity, products[x.ProductId].BaseUnit))
                .ToList();
            if (loads.Count > 0) loadsBySeq[stop.SequenceNumber] = loads;
        }
    }
}
