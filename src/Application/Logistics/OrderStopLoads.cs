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
        var lines = await db.DeliveryOrderLines.Where(l => ids.Contains(l.DeliveryOrderId)).ToListAsync(ct);
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        foreach (var stop in stops.Where(s => s.Kind != RouteStopKind.Pickup))
        {
            if (loadsBySeq.TryGetValue(stop.SequenceNumber, out var existing) && existing.Count > 0) continue;
            var pointOrders = orders.Where(o => o.DestinationServicePointId == stop.ServicePointId).Select(o => o.Id).ToHashSet();
            var loads = lines.Where(l => pointOrders.Contains(l.DeliveryOrderId))
                .GroupBy(l => l.ProductId)
                .Select(g => (ProductId: g.Key, Quantity: g.Sum(l => Math.Max(0m, l.RequestedQuantity - l.DeliveredQuantity))))
                .Where(x => x.Quantity > 0 && products.ContainsKey(x.ProductId))
                .Select(x => new StopLoad(x.ProductId, products[x.ProductId].Sku, x.Quantity, products[x.ProductId].BaseUnit))
                .ToList();
            if (loads.Count > 0) loadsBySeq[stop.SequenceNumber] = loads;
        }
    }

    /// <summary>
    /// #199: recalcula en el servidor qué se lleva de cada pedido en el vehículo elegido (no se confía en el panel).
    /// Pendiente por línea = solicitado − ya planificado. Reparte la capacidad en peso y volumen con prioridad
    /// determinista: criticidad del punto, fecha requerida, número de pedido y SKU. Lo que no cabe queda pendiente.
    /// </summary>
    public static async Task<OrderLoadPlan> PlanForVehicleAsync(IApplicationDbContext db, Vehicle vehicle,
        IReadOnlyList<Guid> orderIds, IReadOnlyList<OrderRouteStop> stops, CancellationToken ct)
    {
        var distinctIds = orderIds.Distinct().ToList();
        var orders = await db.DeliveryOrders
            .Where(o => o.OrganizationId == DemoOrganization.Id && distinctIds.Contains(o.Id))
            .ToListAsync(ct);
        var invalid = distinctIds.FirstOrDefault(id => orders.All(o => o.Id != id || o.Status == DeliveryOrderStatus.Cancelled));
        if (invalid != Guid.Empty)
            return OrderLoadPlan.Rejected($"El pedido {invalid} no existe o está cancelado.");

        var lines = await db.DeliveryOrderLines.Where(l => distinctIds.Contains(l.DeliveryOrderId)).ToListAsync(ct);
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var pointIds = orders.Where(o => o.DestinationServicePointId.HasValue).Select(o => o.DestinationServicePointId!.Value).ToList();
        var points = await db.ServicePoints.Where(p => pointIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        // Cada pedido se entrega en la primera parada de entrega de su destino.
        var stopByOrder = new Dictionary<Guid, OrderRouteStop>();
        foreach (var order in orders.OrderBy(o => o.OrderNumber, StringComparer.Ordinal))
        {
            var stop = stops.Where(s => s.Kind != RouteStopKind.Pickup &&
                    ((order.DestinationServicePointId.HasValue && s.ServicePointId == order.DestinationServicePointId) ||
                     (order.DestinationLocationId.HasValue && s.LocationId == order.DestinationLocationId)))
                .OrderBy(s => s.Sequence).FirstOrDefault();
            if (stop is null)
                return OrderLoadPlan.Rejected($"El pedido {order.OrderNumber} no tiene parada de entrega en la ruta.");
            if (lines.Where(l => l.DeliveryOrderId == order.Id).All(l => l.RequestedQuantity - l.PlannedQuantity <= 0))
                return OrderLoadPlan.Rejected($"El pedido {order.OrderNumber} no tiene cantidades pendientes de planificar.");
            stopByOrder[order.Id] = stop;
        }

        var orderById = orders.ToDictionary(o => o.Id);
        string PointCode(DeliveryOrder o) => o.DestinationServicePointId is { } id && points.TryGetValue(id, out var p) ? p.Code : stopByOrder[o.Id].Code;
        int CriticalityRank(DeliveryOrder o) =>
            (o.DestinationServicePointId is { } id && points.TryGetValue(id, out var p) ? p.Criticality : "").ToLowerInvariant() switch
            {
                "alta" => 0,
                "media" => 1,
                "normal" => 2,
                _ => 3
            };

        decimal? freeWeight = vehicle.MaxWeight > 0 ? vehicle.MaxWeight : null;
        decimal? freeVolume = vehicle.MaxVolume is > 0 ? vehicle.MaxVolume.Value : null;
        var planned = new List<OrderLinePlan>();
        var ordered = lines
            .Where(l => l.RequestedQuantity - l.PlannedQuantity > 0 && products.ContainsKey(l.ProductId))
            .Select(l => (Line: l, Order: orderById[l.DeliveryOrderId], Product: products[l.ProductId]))
            .OrderBy(x => CriticalityRank(x.Order))
            .ThenBy(x => x.Order.RequiredDateUtc)
            .ThenBy(x => x.Order.OrderNumber, StringComparer.Ordinal)
            .ThenBy(x => x.Product.Sku, StringComparer.Ordinal)
            .ThenBy(x => x.Line.Id);
        foreach (var (line, order, product) in ordered)
        {
            var required = line.RequestedQuantity - line.PlannedQuantity;
            var fit = Planning.RoutePlanner.FitLoads(
                [new StopLoad(product.Id, product.Sku, required, product.BaseUnit)], freeWeight, freeVolume, products);
            var take = fit.Sum(l => l.Quantity);
            if (freeWeight.HasValue) freeWeight -= take * product.WeightPerBaseUnit;
            if (freeVolume.HasValue) freeVolume -= take * product.VolumePerBaseUnit;
            planned.Add(new OrderLinePlan(order.Id, order.OrderNumber, line.Id, stopByOrder[order.Id].Sequence, PointCode(order),
                product.Id, product.Sku, product.BaseUnit, required, take, required - take));
        }

        if (planned.Sum(l => l.PlannedQuantity) <= 0)
            return OrderLoadPlan.Rejected($"El vehículo {vehicle.Code} no tiene capacidad para ninguna unidad de los pedidos seleccionados.");

        var loadsBySeq = planned.GroupBy(l => l.Sequence).ToDictionary(
            g => g.Key,
            g => g.GroupBy(l => (l.ProductId, l.Sku, l.BaseUnit)).OrderBy(p => p.Key.Sku, StringComparer.Ordinal)
                .Where(p => p.Sum(l => l.PlannedQuantity) > 0)
                .Select(p => new StopLoad(p.Key.ProductId, p.Key.Sku, p.Sum(l => l.PlannedQuantity), p.Key.BaseUnit)
                {
                    RequiredQuantity = p.Sum(l => l.RequiredQuantity),
                    PendingQuantity = p.Sum(l => l.PendingQuantity)
                }).ToList());
        var weight = planned.Sum(l => l.PlannedQuantity * products[l.ProductId].WeightPerBaseUnit);
        var volume = planned.Sum(l => l.PlannedQuantity * products[l.ProductId].VolumePerBaseUnit);
        return new OrderLoadPlan(loadsBySeq, planned, weight, volume, null);
    }
}

// #199: parada enviada por el asistente, solo con lo necesario para ubicar los pedidos.
public sealed record OrderRouteStop(int Sequence, string Kind, Guid? ServicePointId, Guid? LocationId, string Code);

// #199: reparto de una línea de pedido en la ruta confirmada.
public sealed record OrderLinePlan(
    Guid OrderId,
    string OrderNumber,
    Guid OrderLineId,
    int Sequence,
    string PointCode,
    Guid ProductId,
    string Sku,
    string BaseUnit,
    decimal RequiredQuantity,
    decimal PlannedQuantity,
    decimal PendingQuantity);

public sealed record OrderLoadPlan(
    IReadOnlyDictionary<int, List<StopLoad>> LoadsBySeq,
    IReadOnlyList<OrderLinePlan> Lines,
    decimal LoadWeight,
    decimal LoadVolume,
    string? Error)
{
    public static OrderLoadPlan Rejected(string error) =>
        new(new Dictionary<int, List<StopLoad>>(), [], 0m, 0m, error);
}
