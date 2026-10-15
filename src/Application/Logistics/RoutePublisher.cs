using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record PublishRequest(
    Guid RoutePlanId,
    IReadOnlyDictionary<string, string>? DriverAssignments = null, // vehicleCode -> driverAccountId / sub
    Guid? PublishedByAccountId = null);

public sealed record PublishResult(
    Guid RoutePlanId,
    string Status,
    IReadOnlyList<Delivery> Deliveries,
    IReadOnlyList<RouteStockReservation> Reservations);

public sealed record CompleteStopRequest(
    Guid RoutePlanId,
    int SequenceNumber,
    DateTime? ActualArrivalUtc = null,
    DateTime? ActualDepartureUtc = null);

public interface IRoutePublisher
{
    Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken ct = default);
    Task<RouteStop> CompleteStopAsync(CompleteStopRequest request, CancellationToken ct = default);
}

public sealed class RoutePublisher(
    IApplicationDbContext db,
    IOperationalClock clock,
    IOptions<LogisticsOptions> options) : IRoutePublisher
{
    private readonly LogisticsOptions _options = options.Value;

    public async Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken ct = default)
    {
        var plan = await db.RoutePlans
            .FirstOrDefaultAsync(p => p.Id == request.RoutePlanId && p.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"RoutePlan {request.RoutePlanId} no encontrado.");

        // Idempotencia: Si ya está publicado, devolver entregas y reservas existentes
        if (plan.Status == "Published")
        {
            var existingDeliveries = await db.Deliveries
                .Where(d => d.OrganizationId == DemoOrganization.Id &&
                            db.RouteStops.Any(s => s.RoutePlanId == plan.Id && s.Id == d.RouteStopId))
                .ToListAsync(ct);

            var existingReservations = await db.RouteStockReservations
                .Where(r => r.OrganizationId == DemoOrganization.Id && r.RoutePlanId == plan.Id)
                .ToListAsync(ct);

            // Un plan publicado sin entregas quedó vacío (rutas del asistente publicadas antes de esta corrección): se repara
            // publicándolo de nuevo en lugar de devolver una ruta que ningún conductor puede ver.
            if (existingDeliveries.Count > 0)
                return new PublishResult(plan.Id, plan.Status, existingDeliveries, existingReservations);
        }

        var today = await clock.TodayAsync(ct);

        // Deserializar la propuesta almacenada en InputJson o reconstruirla
        // La propuesta original contiene las paradas y cargas
        var stops = await db.RouteStops
            .Where(s => s.RoutePlanId == plan.Id && s.OrganizationId == DemoOrganization.Id)
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync(ct);

        // Bodega de origen por parada (ADR-28, varias bodegas): la última recogida en bodega previa en la ruta o,
        // si no hay, la bodega del vehículo. Antes todas las entregas salían de la primera bodega encontrada.
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == plan.VehicleId, ct);
        var warehouses = await db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse)
            .ToDictionaryAsync(l => l.Id, ct);
        if (warehouses.Count == 0)
            throw new InvalidOperationException("No se encontró la bodega central de origen.");
        var vehicleDepotId = vehicle != null && warehouses.ContainsKey(vehicle.DepotLocationId)
            ? vehicle.DepotLocationId
            : warehouses.Keys.First();

        // Cargar lotes de las bodegas disponibles
        var balances = await db.StockBalances
            .Where(b => b.OrganizationId == DemoOrganization.Id && warehouses.Keys.Contains(b.LocationId))
            .ToListAsync(ct);

        var lotIds = balances.Select(b => b.LotId).Distinct().ToHashSet();
        var lots = await db.Lots
            .Where(l => l.OrganizationId == DemoOrganization.Id && lotIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l, ct);

        // Parsear cargas planificadas desde el plan almacenado
        var routeDoc = JsonDocument.Parse(plan.InputJson);
        var stopsRoot = routeDoc.RootElement.TryGetProperty("stops", out var sProp) ? sProp : default;

        var plannedLoadsBySeq = new Dictionary<int, List<StopLoad>>();
        if (stopsRoot.ValueKind == JsonValueKind.Array)
        {
            foreach (var sElem in stopsRoot.EnumerateArray())
            {
                int seq = sElem.GetProperty("sequence").GetInt32();
                var loads = new List<StopLoad>();
                if (sElem.TryGetProperty("loads", out var loadsElem) && loadsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var lElem in loadsElem.EnumerateArray())
                    {
                        var prodId = lElem.TryGetProperty("productId", out var p1) ? p1.GetGuid() : lElem.GetProperty("ProductId").GetGuid();
                        var sku = lElem.TryGetProperty("sku", out var s1) ? s1.GetString() : lElem.GetProperty("Sku").GetString();
                        var qty = lElem.TryGetProperty("quantity", out var q1) ? q1.GetDecimal() : lElem.GetProperty("Quantity").GetDecimal();
                        var unit = lElem.TryGetProperty("baseUnit", out var b1) ? b1.GetString() : (lElem.TryGetProperty("BaseUnit", out var b2) ? b2.GetString() : "U");

                        loads.Add(new StopLoad(prodId, sku ?? "", qty, unit ?? "U"));
                    }
                }
                plannedLoadsBySeq[seq] = loads;
            }
        }

        await FillLoadsFromOrdersAsync(routeDoc.RootElement, stops, plannedLoadsBySeq, ct);

        if (plan.SupersedesRoutePlanId.HasValue && plan.ProposedBy == "System:IncidentReplanner")
            return await PublishRevisionAsync(plan, stops, plannedLoadsBySeq, request, ct);

        // Determinar asignación de conductor si existe
        // El conductor solo ve las entregas con su cuenta en AssignedAccountId (GET /mobile/assignments).
        Guid? assignedAccountId = null;
        if (request.DriverAssignments != null && vehicle != null &&
            request.DriverAssignments.TryGetValue(vehicle.Code, out var driverText))
        {
            if (!Guid.TryParse(driverText, out var driverId) ||
                !await db.Accounts.AnyAsync(a => a.Id == driverId && a.IsActive && a.Role == Roles.Conductor &&
                                                  a.OrganizationId == DemoOrganization.Id, ct))
                throw new ArgumentException($"La cuenta indicada para {vehicle.Code} no es un Conductor activo.");
            assignedAccountId = driverId;
        }

        var createdDeliveries = new List<Delivery>();
        var createdLines = new List<DeliveryLine>();
        var createdReservations = new List<RouteStockReservation>();

        // Organizar stock disponible por producto ordenado por FEFO
        // Invariante de seguridad de bodega: mantener WarehouseSafetyStockPct
        var availableByProduct = balances
            .Where(b =>
            {
                if (!lots.TryGetValue(b.LotId, out var lot)) return false;
                if (lot.IsBlocked) return false;
                if (lot.ExpiresOn.HasValue && lot.ExpiresOn.Value <= today) return false;
                return (b.QuantityOnHand - b.QuantityReserved) > 0;
            })
            .GroupBy(b => (b.LocationId, lots[b.LotId].ProductId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(b => lots[b.LotId].ExpiresOn ?? DateOnly.MaxValue)
                      .ThenBy(b => lots[b.LotId].Code, StringComparer.Ordinal)
                      .ToList());

        // Validar y asignar lotes FEFO
        var originId = vehicleDepotId;
        foreach (var stop in stops.Where(s => s.Status != "Completed"))
        {
            // Una recogida en bodega cambia el origen de las entregas siguientes.
            if (stop.Kind == RouteStopKind.Pickup && stop.LocationId is { } pickupId && warehouses.ContainsKey(pickupId))
            {
                originId = pickupId;
                continue;
            }
            if (!plannedLoadsBySeq.TryGetValue(stop.SequenceNumber, out var loads) || loads.Count == 0)
            {
                continue;
            }

            var deliveryCode = $"DEL-{plan.Code}-R{plan.Revision}-{stop.SequenceNumber:D2}";
            var delivery = new Delivery
            {
                OrganizationId = DemoOrganization.Id,
                Code = deliveryCode,
                RouteStopId = stop.Id,
                OriginLocationId = originId,
                ServicePointId = stop.ServicePointId,
                Status = nameof(DeliveryStatus.Draft),
                AssignedAccountId = assignedAccountId
            };
            createdDeliveries.Add(delivery);

            foreach (var load in loads)
            {
                decimal qtyNeeded = load.Quantity;
                if (!availableByProduct.TryGetValue((originId, load.ProductId), out var candidateBalances))
                {
                    throw new InvalidOperationException(
                        $"Conflicto de stock: No hay stock disponible para el producto {load.Sku} en la bodega {warehouses[originId].Code}.");
                }

                foreach (var bal in candidateBalances)
                {
                    if (qtyNeeded <= 0) break;

                    decimal available = bal.QuantityOnHand - bal.QuantityReserved;
                    if (available <= 0) continue;

                    decimal allocate = Math.Min(available, qtyNeeded);

                    var line = new DeliveryLine
                    {
                        OrganizationId = DemoOrganization.Id,
                        DeliveryId = delivery.Id,
                        LotId = bal.LotId,
                        PlannedQuantity = allocate,
                        DispatchedQuantity = 0m,
                        AcceptedQuantity = 0m,
                        RejectedQuantity = 0m
                    };
                    createdLines.Add(line);

                    var reservation = new RouteStockReservation
                    {
                        OrganizationId = DemoOrganization.Id,
                        RoutePlanId = plan.Id,
                        DeliveryId = delivery.Id,
                        DeliveryLineId = line.Id,
                        LotId = bal.LotId,
                        StockBalanceId = bal.Id,
                        Quantity = allocate,
                        Status = RouteStockReservationStatus.Active,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    createdReservations.Add(reservation);

                    // Actualizar QuantityReserved en saldo
                    bal.QuantityReserved += allocate;
                    qtyNeeded -= allocate;
                }

                if (qtyNeeded > 0)
                {
                    throw new InvalidOperationException(
                        $"Conflicto de stock: Stock insuficiente para cubrir la cantidad planificada de {load.Sku} ({load.Quantity} requerida, faltan {qtyNeeded}).");
                }
            }
        }

        if (createdDeliveries.Count == 0)
            throw new InvalidOperationException(
                "La ruta no tiene productos para entregar: ninguna parada tiene cargas planificadas ni pedidos asociados. No se publicó.");

        // Persistir entregas, líneas, reservas y actualizar estado del plan
        plan.Status = "Published";
        plan.PublishedAtUtc = DateTime.UtcNow;
        plan.PublishedByAccountId = request.PublishedByAccountId;

        db.Deliveries.AddRange(createdDeliveries);
        db.DeliveryLines.AddRange(createdLines);
        db.RouteStockReservations.AddRange(createdReservations);

        await db.SaveChangesAsync(ct);

        return new PublishResult(plan.Id, plan.Status, createdDeliveries, createdReservations);
    }

    // Rutas del asistente (pedidos y emergencia): sus paradas no guardan cargas, pero el plan guarda los pedidos.
    // A cada parada de entrega sin cargas se le asignan las líneas pendientes de los pedidos de su punto.
    private async Task FillLoadsFromOrdersAsync(JsonElement root, List<RouteStop> stops,
        Dictionary<int, List<StopLoad>> loadsBySeq, CancellationToken ct)
    {
        if (!root.TryGetProperty("orderIds", out var idsElem) || idsElem.ValueKind != JsonValueKind.Array) return;
        var orderIds = idsElem.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => Guid.TryParse(e.GetString(), out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToList();
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

    private async Task<PublishResult> PublishRevisionAsync(RoutePlan plan, List<RouteStop> stops,
        Dictionary<int, List<StopLoad>> loads, PublishRequest request, CancellationToken ct)
    {
        var previous = await db.RoutePlans.FirstAsync(p => p.Id == plan.SupersedesRoutePlanId &&
            p.OrganizationId == DemoOrganization.Id, ct);
        if (previous.Status != "Published" || previous.VehicleId != plan.VehicleId)
            throw new ReplanConflictException("La revisión requiere una ruta vigente del mismo vehículo.");
        var oldStops = await db.RouteStops.Where(s => s.RoutePlanId == previous.Id &&
            s.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(s => s.Id, ct);
        var deliveries = await db.Deliveries.Where(d => d.OrganizationId == DemoOrganization.Id &&
            d.RouteStopId.HasValue && oldStops.Keys.Contains(d.RouteStopId.Value)).ToListAsync(ct);
        var reservations = await db.RouteStockReservations.Where(r => r.OrganizationId == DemoOrganization.Id &&
            r.RoutePlanId == previous.Id && r.Status == RouteStockReservationStatus.Active).ToListAsync(ct);
        var lines = await db.DeliveryLines.Where(l => l.OrganizationId == DemoOrganization.Id &&
            deliveries.Select(d => d.Id).Contains(l.DeliveryId))
            .ToListAsync(ct);
        var lots = await db.Lots.Where(l => l.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(l => l.Id, ct);
        foreach (var delivery in deliveries.Where(d => oldStops[d.RouteStopId!.Value].Status != "Completed"))
        {
            var original = oldStops[delivery.RouteStopId!.Value];
            var target = stops.SingleOrDefault(s => s.ServicePointId == original.ServicePointId && s.Status != "Completed");
            if (target is null)
            {
                if (delivery.Status == "Dispatched" || delivery.Status == "Delivered")
                    throw new ReplanConflictException("No se puede retirar una entrega ya despachada.");
                foreach (var reservation in reservations.Where(r => r.DeliveryId == delivery.Id))
                {
                    var balance = await db.StockBalances.FirstAsync(b => b.Id == reservation.StockBalanceId, ct);
                    balance.QuantityReserved -= reservation.Quantity;
                    reservation.Status = RouteStockReservationStatus.Released;
                    reservation.ClosedAtUtc = DateTime.UtcNow;
                }
                delivery.Status = "Cancelled";
                continue;
            }
            var existing = lines.Where(l => l.DeliveryId == delivery.Id)
                .GroupBy(l => lots[l.LotId].ProductId).ToDictionary(g => g.Key, g => g.Sum(l => l.PlannedQuantity));
            var proposed = loads.GetValueOrDefault(target.SequenceNumber, [])
                .GroupBy(l => l.ProductId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
            if (existing.Count != proposed.Count || existing.Any(p => proposed.GetValueOrDefault(p.Key) != p.Value))
                throw new ReplanConflictException("La revisión no puede cambiar la carga reservada o despachada.");
            delivery.RouteStopId = target.Id;
            foreach (var reservation in reservations.Where(r => r.DeliveryId == delivery.Id))
                reservation.RoutePlanId = plan.Id;
        }
        previous.Status = "Superseded";
        plan.Status = "Published";
        plan.PublishedAtUtc = DateTime.UtcNow;
        plan.PublishedByAccountId = request.PublishedByAccountId;
        await db.SaveChangesAsync(ct);
        return new PublishResult(plan.Id, plan.Status,
            deliveries.Where(d => stops.Any(s => s.Id == d.RouteStopId)).ToList(),
            reservations.Where(r => r.RoutePlanId == plan.Id).ToList());
    }

    public async Task<RouteStop> CompleteStopAsync(CompleteStopRequest request, CancellationToken ct = default)
    {
        var stop = await db.RouteStops
            .FirstOrDefaultAsync(s => s.RoutePlanId == request.RoutePlanId &&
                                      s.SequenceNumber == request.SequenceNumber &&
                                      s.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"RouteStop seq {request.SequenceNumber} en plan {request.RoutePlanId} no encontrada.");

        // Idempotencia: Si ya está completada, no retrocede ni altera
        if (stop.Status == "Completed")
        {
            return stop;
        }

        stop.Status = "Completed";
        stop.ActualArrivalUtc = request.ActualArrivalUtc ?? stop.PlannedArrivalUtc ?? DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return stop;
    }
}
