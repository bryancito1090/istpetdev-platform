using IstpetDev.Application.Common;
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
    // vehicleCode -> Account.Id interno del conductor (no el sub de Cognito): se compara con User.AccountId().
    IReadOnlyDictionary<string, string>? DriverAssignments = null,
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
    Task<int> AssignDriverAsync(Guid routePlanId, Guid driverAccountId, CancellationToken ct = default);
}

public sealed class RoutePublisher(
    IApplicationDbContext db,
    IOperationalClock clock,
    IOptions<LogisticsOptions> options) : IRoutePublisher
{
    private readonly LogisticsOptions _options = options.Value;

    public async Task<PublishResult> PublishAsync(PublishRequest request, CancellationToken ct = default)
    {
        await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
        var result = await PublishAsyncCore(request, ct);
        var published = await db.RoutePlans.SingleAsync(p => p.Id == result.RoutePlanId && p.OrganizationId == DemoOrganization.Id, ct);
        // Replanificar o revertir una operación no vuelve a elegir su propuesta original.
        if (published.AlgorithmVersion == "2.0-deterministic" && published.SupersedesRoutePlanId is null)
            await RouteProposalAcceptance.AcceptAsync(db, await RouteProposalAcceptance.ResolveBatchAsync(db, published, ct), ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return result;
    }

    private async Task<PublishResult> PublishAsyncCore(PublishRequest request, CancellationToken ct)
    {
        var plan = await db.RoutePlans
            .FirstOrDefaultAsync(p => p.Id == request.RoutePlanId && p.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"RoutePlan {request.RoutePlanId} no encontrado.");

        // Idempotencia: Si ya está publicado, devolver entregas y reservas existentes
        if (plan.Status == "Published")
        {
            var existingDeliveries = await db.Deliveries
                .Where(d => d.OrganizationId == DemoOrganization.Id &&
                            db.RouteStops.Any(s => s.OrganizationId == DemoOrganization.Id && s.RoutePlanId == plan.Id && s.Id == d.RouteStopId))
                .ToListAsync(ct);

            var existingReservations = await db.RouteStockReservations
                .Where(r => r.OrganizationId == DemoOrganization.Id && r.RoutePlanId == plan.Id)
                .ToListAsync(ct);

            // Un plan publicado sin entregas quedó vacío (rutas del asistente publicadas antes de esta corrección): se repara
            // publicándolo de nuevo en lugar de devolver una ruta que ningún conductor puede ver.
            if (existingDeliveries.Count > 0)
                return new PublishResult(plan.Id, plan.Status, existingDeliveries, existingReservations);
        }

        if (plan.Status is not ("Proposed" or "Published"))
            throw new InvalidOperationException($"La ruta está en {plan.Status}; solo se publica una propuesta vigente.");

        var today = await clock.TodayAsync(ct);

        // Deserializar la propuesta almacenada en InputJson o reconstruirla
        // La propuesta original contiene las paradas y cargas
        var stops = await db.RouteStops
            .Where(s => s.RoutePlanId == plan.Id && s.OrganizationId == DemoOrganization.Id)
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync(ct);

        // Bodega de origen por parada (ADR-28, varias bodegas): la última recogida en bodega previa en la ruta o,
        // si no hay, la bodega del vehículo. Antes todas las entregas salían de la primera bodega encontrada.
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.OrganizationId == DemoOrganization.Id && v.Id == plan.VehicleId, ct);
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

        await OrderStopLoads.FillAsync(db, OrderStopLoads.OrderIds(routeDoc.RootElement), stops, plannedLoadsBySeq, ct);

        if (plan.SupersedesRoutePlanId.HasValue && plan.ProposedBy == "System:IncidentReplanner")
            return await PublishRevisionAsync(plan, stops, plannedLoadsBySeq, request, ct);

        // Determinar asignación de conductor si existe
        // El conductor solo ve las entregas con su cuenta en AssignedAccountId (GET /mobile/assignments).
        Guid? assignedAccountId = null;
        if (request.DriverAssignments != null && vehicle != null &&
            request.DriverAssignments.TryGetValue(vehicle.Code, out var driverText))
        {
            if (!Guid.TryParse(driverText, out var driverId) || !await IsActiveDriverAsync(driverId, ct))
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
        var visitedPickupWarehouseIds = new HashSet<Guid>();
        foreach (var stop in stops.Where(s => s.Status != "Completed"))
        {
            // Una recogida en bodega cambia el origen de las entregas siguientes.
            if (stop.Kind == RouteStopKind.Pickup && stop.LocationId is { } pickupId && warehouses.ContainsKey(pickupId))
            {
                visitedPickupWarehouseIds.Add(pickupId);
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
                var effectiveOriginId = originId;
                if (!availableByProduct.TryGetValue((effectiveOriginId, load.ProductId), out var candidateBalances))
                {
                    // Buscar entre las bodegas de recogida visitadas previamente en la ruta
                    var altOrigin = visitedPickupWarehouseIds.FirstOrDefault(whId => availableByProduct.ContainsKey((whId, load.ProductId)));
                    if (altOrigin != Guid.Empty && availableByProduct.TryGetValue((altOrigin, load.ProductId), out var altBalances))
                    {
                        effectiveOriginId = altOrigin;
                        candidateBalances = altBalances;
                        delivery.OriginLocationId = altOrigin;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Conflicto de stock: No hay stock disponible para el producto {load.Sku} en la bodega {warehouses[originId].Code}.");
                    }
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

    private Task<bool> IsActiveDriverAsync(Guid accountId, CancellationToken ct) =>
        db.Accounts.AnyAsync(a => a.Id == accountId && a.IsActive && a.Role == Roles.Conductor &&
                                  a.OrganizationId == DemoOrganization.Id, ct);

    /// <summary>
    /// #160: cambia el conductor de una ruta ya publicada. Publicar es idempotente y no sirve para reasignar.
    /// Solo toca entregas pendientes (no entregadas, parciales ni canceladas); su Version sube al guardar y la PWA
    /// detecta el cambio en la siguiente descarga.
    /// </summary>
    public async Task<int> AssignDriverAsync(Guid routePlanId, Guid driverAccountId, CancellationToken ct = default)
    {
        await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
        var result = await AssignDriverAsyncCore(routePlanId, driverAccountId, ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return result;
    }

    private async Task<int> AssignDriverAsyncCore(Guid routePlanId, Guid driverAccountId, CancellationToken ct)
    {
        var plan = await db.RoutePlans
            .FirstOrDefaultAsync(p => p.Id == routePlanId && p.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"RoutePlan {routePlanId} no encontrado.");
        if (plan.Status != "Published")
            throw new InvalidOperationException("Solo se reasigna el conductor de una ruta publicada; al publicar se elige el conductor.");
        if (!await IsActiveDriverAsync(driverAccountId, ct))
            throw new ArgumentException("La cuenta indicada no es un Conductor activo.");

        var closed = new[] { DeliveryStates.Delivered, DeliveryStates.Partial, DeliveryStates.Received, DeliveryStates.Cancelled };
        var pending = await db.Deliveries
            .Where(d => d.OrganizationId == DemoOrganization.Id && !closed.Contains(d.Status) &&
                        db.RouteStops.Any(s => s.OrganizationId == DemoOrganization.Id && s.RoutePlanId == plan.Id && s.Id == d.RouteStopId))
            .ToListAsync(ct);
        if (pending.Count == 0)
            throw new InvalidOperationException("La ruta no tiene entregas pendientes a las que asignar conductor.");

        foreach (var delivery in pending) delivery.AssignedAccountId = driverAccountId;
        await db.SaveChangesAsync(ct);
        return pending.Count;
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
        var removedStops = new List<RouteStop>();
        foreach (var delivery in deliveries.Where(d => oldStops[d.RouteStopId!.Value].Status != "Completed"))
        {
            var original = oldStops[delivery.RouteStopId!.Value];
            // #300: se compara también tipo y bodega; en rutas del asistente la recogida y las entregas a bodega
            // llevan un ServicePointId de relleno que puede coincidir con el de una entrega.
            var target = stops.SingleOrDefault(s => s.ServicePointId == original.ServicePointId && s.Kind == original.Kind &&
                s.LocationId == original.LocationId && s.Status != "Completed");
            if (target is null)
            {
                if (delivery.Status is DeliveryStates.Dispatched or DeliveryStates.InTransit or DeliveryStates.Delivered)
                    throw new ReplanConflictException("No se puede retirar una entrega ya despachada.");
                foreach (var reservation in reservations.Where(r => r.DeliveryId == delivery.Id))
                {
                    var balance = await db.StockBalances.FirstAsync(b => b.OrganizationId == DemoOrganization.Id && b.Id == reservation.StockBalanceId, ct);
                    balance.QuantityReserved -= reservation.Quantity;
                    reservation.Status = RouteStockReservationStatus.Released;
                    reservation.ClosedAtUtc = DateTime.UtcNow;
                }
                delivery.Status = "Cancelled";
                removedStops.Add(original);
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
        await ReleaseRemovedOrderLinesAsync(plan, previous, removedStops, ct);
        previous.Status = "Superseded";
        plan.Status = "Published";
        plan.PublishedAtUtc = DateTime.UtcNow;
        plan.PublishedByAccountId = request.PublishedByAccountId;
        await db.SaveChangesAsync(ct);
        return new PublishResult(plan.Id, plan.Status,
            deliveries.Where(d => stops.Any(s => s.Id == d.RouteStopId)).ToList(),
            reservations.Where(r => r.RoutePlanId == plan.Id).ToList());
    }

    /// <summary>
    /// #199 (FR-28): la revisión retira paradas con pedidos. Se resta de <c>PlannedQuantity</c> lo que el plan sumó a los
    /// pedidos de esos destinos y la revisión guarda el <c>orderLines</c> restante, para que cancelarla no descuente dos
    /// veces. Sin <c>orderLines</c> (planes confirmados antes de #264) no hay datos para descontar.
    /// </summary>
    private async Task ReleaseRemovedOrderLinesAsync(RoutePlan plan, RoutePlan previous, IReadOnlyList<RouteStop> removed, CancellationToken ct)
    {
        var removedStops = removed.Where(s => s.Kind != RouteStopKind.Pickup).ToList();
        if (removedStops.Count == 0) return;
        var planned = await RoutePlanCancellation.EffectiveOrderLinesAsync(db, previous, ct);
        if (planned is null || planned.Count == 0) return;

        var lineIds = planned.Keys.ToList();
        var lineOrders = await db.DeliveryOrderLines
            .Where(l => l.OrganizationId == DemoOrganization.Id && lineIds.Contains(l.Id))
            .Select(l => new { l.Id, l.DeliveryOrderId })
            .ToListAsync(ct);
        var orderIds = lineOrders.Select(l => l.DeliveryOrderId).Distinct().ToList();
        var destinations = await db.DeliveryOrders
            .Where(o => o.OrganizationId == DemoOrganization.Id && orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.DestinationServicePointId, o.DestinationLocationId })
            .ToDictionaryAsync(o => o.Id, ct);
        // Mismo criterio que OrderStopLoads: el pedido va a la parada de su punto o de su bodega de destino.
        bool GoesToRemovedStop(Guid orderId) => destinations.TryGetValue(orderId, out var d) && removedStops.Any(s =>
            (d.DestinationServicePointId.HasValue && d.DestinationServicePointId == s.ServicePointId) ||
            (d.DestinationLocationId.HasValue && d.DestinationLocationId == s.LocationId));

        var release = lineOrders.Where(l => GoesToRemovedStop(l.DeliveryOrderId))
            .ToDictionary(l => l.Id, l => -planned[l.Id]);
        if (release.Count == 0) return;
        await RoutePlanCancellation.AdjustPlannedAsync(db, release, ct);
        plan.InputJson = RoutePlanCancellation.WithOrderLines(plan.InputJson,
            planned.Where(kv => !release.ContainsKey(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
    }

    public async Task<RouteStop> CompleteStopAsync(CompleteStopRequest request, CancellationToken ct = default)
    {
        await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
        var result = await CompleteStopAsyncCore(request, ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return result;
    }

    private async Task<RouteStop> CompleteStopAsyncCore(CompleteStopRequest request, CancellationToken ct)
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
