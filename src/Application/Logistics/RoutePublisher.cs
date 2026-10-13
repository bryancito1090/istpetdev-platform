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

            return new PublishResult(plan.Id, plan.Status, existingDeliveries, existingReservations);
        }

        var today = await clock.TodayAsync(ct);

        // Deserializar la propuesta almacenada en InputJson o reconstruirla
        // La propuesta original contiene las paradas y cargas
        var stops = await db.RouteStops
            .Where(s => s.RoutePlanId == plan.Id && s.OrganizationId == DemoOrganization.Id)
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync(ct);

        // Ubicación de salida (bodega)
        var depotLoc = await db.Locations
            .FirstOrDefaultAsync(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse, ct)
            ?? throw new InvalidOperationException("No se encontró la bodega central de origen.");

        // Cargar lotes de la bodega disponibles
        var balances = await db.StockBalances
            .Where(b => b.OrganizationId == DemoOrganization.Id && b.LocationId == depotLoc.Id)
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

        // Determinar asignación de conductor si existe
        Guid? assignedAccountId = null;
        if (request.DriverAssignments != null)
        {
            var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == plan.VehicleId, ct);
            if (vehicle != null && request.DriverAssignments.TryGetValue(vehicle.Code, out var driverSubStr) &&
                Guid.TryParse(driverSubStr, out var driverGuid))
            {
                assignedAccountId = driverGuid;
            }
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
            .GroupBy(b => lots[b.LotId].ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(b => lots[b.LotId].ExpiresOn ?? DateOnly.MaxValue)
                      .ThenBy(b => lots[b.LotId].Code, StringComparer.Ordinal)
                      .ToList());

        // Validar y asignar lotes FEFO
        foreach (var stop in stops)
        {
            if (!plannedLoadsBySeq.TryGetValue(stop.SequenceNumber, out var loads) || loads.Count == 0)
            {
                continue;
            }

            var deliveryCode = $"DEL-{plan.Code}-{stop.SequenceNumber:D2}";
            var delivery = new Delivery
            {
                OrganizationId = DemoOrganization.Id,
                Code = deliveryCode,
                RouteStopId = stop.Id,
                OriginLocationId = depotLoc.Id,
                ServicePointId = stop.ServicePointId,
                Status = nameof(DeliveryStatus.Draft),
                AssignedAccountId = assignedAccountId
            };
            createdDeliveries.Add(delivery);

            foreach (var load in loads)
            {
                decimal qtyNeeded = load.Quantity;
                if (!availableByProduct.TryGetValue(load.ProductId, out var candidateBalances))
                {
                    throw new InvalidOperationException($"Conflicto de stock: No hay stock disponible para el producto {load.Sku}.");
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
