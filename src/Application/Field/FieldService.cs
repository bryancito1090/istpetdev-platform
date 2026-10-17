using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Common;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Field;

public sealed class FieldService : IFieldService
{
    // Origen del consumo derivado del stock en percha declarado al recibir (#111).
    private const string FieldReconciliationSource = "FieldReconciliation";

    private readonly IApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<FieldService> _logger;
    private readonly LogisticsOptions _logistics;
    private readonly IEvidenceStorage? _evidenceStorage;

    public FieldService(
        IApplicationDbContext db,
        IConfiguration config,
        ILogger<FieldService> logger,
        IOptions<LogisticsOptions> logistics,
        IEvidenceStorage? evidenceStorage = null)
    {
        _db = db;
        _evidenceStorage = evidenceStorage;
        _config = config;
        _logger = logger;
        _logistics = logistics.Value;
    }

    public async Task<HandlingUnitResponse> CreateHandlingUnitAsync(
        Guid deliveryId,
        CreateHandlingUnitRequest request,
        Guid actorAccountId,
        CancellationToken ct = default)
    {
        // #162: el alta de bultos usa el mismo bloqueo de organización que el despacho; sin él,
        // una preparación concurrente podría crear un bulto Sealed después de que la entrega salió.
        await using var transaction = await BeginSerializedTransactionAsync(ct);

        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Entrega {deliveryId} no encontrada.");

        // #162: solo una entrega Draft admite empaque; no hay preparación retroactiva tras despachar.
        if (delivery.Status != DeliveryStates.Draft)
        {
            throw new FieldConflictException(FieldErrorCodes.InvalidStateTransition,
                $"No se puede preparar bultos en una entrega en estado {delivery.Status}; solo una entrega Draft admite empaque.");
        }

        if (request.Lines.Count == 0 || request.Lines.Any(l => l.Quantity <= 0m))
        {
            throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded,
                "La preparación debe incluir al menos una línea con cantidad positiva.");
        }

        var deliveryLines = await _db.DeliveryLines
            .Where(dl => dl.DeliveryId == delivery.Id && dl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var deliveryLinesById = deliveryLines.ToDictionary(dl => dl.Id, dl => dl);

        // Validar cantidades contra reservas planeadas de la entrega
        var existingHuLines = await _db.HandlingUnitLines
            .Where(hul => hul.OrganizationId == DemoOrganization.Id &&
                          _db.HandlingUnits.Any(hu => hu.Id == hul.HandlingUnitId && hu.DeliveryId == delivery.Id))
            .ToListAsync(ct);

        var preparedByDeliveryLine = existingHuLines
            .Where(hul => hul.DeliveryLineId.HasValue)
            .GroupBy(hul => hul.DeliveryLineId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var lots = await _db.Lots
            .Where(l => l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, l => l, ct);

        var products = await _db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        var createdHuLines = new List<HandlingUnitLine>();
        var responseLines = new List<HandlingUnitLineDto>();

        foreach (var reqLine in request.Lines)
        {
            if (!deliveryLinesById.TryGetValue(reqLine.DeliveryLineId, out var dLine))
            {
                throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded, $"Línea de entrega {reqLine.DeliveryLineId} no pertenece a la entrega.");
            }

            if (dLine.LotId != reqLine.LotId)
            {
                throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded, "El lote no coincide con el lote reservado de la línea.");
            }

            var alreadyPrepared = preparedByDeliveryLine.GetValueOrDefault(dLine.Id, 0m);
            if (alreadyPrepared + reqLine.Quantity > dLine.PlannedQuantity)
            {
                throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded,
                    $"Cantidad solicitada ({reqLine.Quantity}) excede el disponible reservado ({dLine.PlannedQuantity - alreadyPrepared}).");
            }

            lots.TryGetValue(reqLine.LotId, out var lot);
            var sku = "MED-001";
            var unit = "U";
            if (lot != null && products.TryGetValue(lot.ProductId, out var product))
            {
                sku = product.Sku;
                unit = product.BaseUnit;
            }

            responseLines.Add(new HandlingUnitLineDto(
                reqLine.DeliveryLineId,
                reqLine.LotId,
                sku,
                reqLine.Quantity,
                unit));
        }

        var existingHuCount = await _db.HandlingUnits
            .CountAsync(h => h.DeliveryId == delivery.Id && h.OrganizationId == DemoOrganization.Id, ct);

        var huCode = $"HU-{delivery.Code}-C{existingHuCount + 1:D2}";
        var handlingUnit = new HandlingUnit
        {
            OrganizationId = DemoOrganization.Id,
            Code = huCode,
            DeliveryId = delivery.Id,
            LocationId = delivery.OriginLocationId,
            Status = HandlingUnitStates.Sealed,
            SealedAtUtc = DateTime.UtcNow
        };

        _db.HandlingUnits.Add(handlingUnit);

        foreach (var reqLine in request.Lines)
        {
            var huLine = new HandlingUnitLine
            {
                OrganizationId = DemoOrganization.Id,
                HandlingUnitId = handlingUnit.Id,
                DeliveryLineId = reqLine.DeliveryLineId,
                LotId = reqLine.LotId,
                Quantity = reqLine.Quantity
            };
            _db.HandlingUnitLines.Add(huLine);
        }

        // Token seguro para el QR (al menos 128 bits de entropía)
        var tokenBytes = RandomNumberGenerator.GetBytes(16);
        var clearToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var tokenHash = ComputeSha256(clearToken);

        // Revocar tokens anteriores de esta caja si existieran
        var previousTokens = await _db.QrAccessTokens
            .Where(t => t.HandlingUnitId == handlingUnit.Id && t.OrganizationId == DemoOrganization.Id && t.RevokedAtUtc == null)
            .ToListAsync(ct);

        foreach (var prev in previousTokens)
        {
            prev.RevokedAtUtc = DateTime.UtcNow;
        }

        var qrToken = new QrAccessToken
        {
            OrganizationId = DemoOrganization.Id,
            HandlingUnitId = handlingUnit.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedByAccountId = actorAccountId
        };
        _db.QrAccessTokens.Add(qrToken);

        await _db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        var labelUrl = $"https://istpetdev.doicela.dev/public-trace/{handlingUnit.Id}#t={clearToken}";

        return new HandlingUnitResponse(
            handlingUnit.Id,
            handlingUnit.Code,
            delivery.Id,
            handlingUnit.Status,
            labelUrl,
            clearToken,
            responseLines,
            handlingUnit.CreatedAtUtc);
    }

    public async Task<DispatchResponse> DispatchAsync(
        Guid deliveryId,
        DispatchRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        const string actionCode = "Dispatch";

        // 1. Replay rápido de la misma operación (respuesta idempotente antes de cualquier efecto).
        var replay = await FindReplayAsync<DispatchResponse>(actorAccountId, request.OperationId, actionCode, deliveryId, request, idempotencyKey, ct, legacyResourceSelector: r => r.DeliveryId);
        if (replay is not null) return replay;

        await using var transaction = await BeginSerializedTransactionAsync(ct);

        // Un reintento concurrente de la misma operación ya entró: devolver su respuesta sin volver a mover stock.
        replay = await FindReplayAsync<DispatchResponse>(actorAccountId, request.OperationId, actionCode, deliveryId, request, idempotencyKey, ct, legacyResourceSelector: r => r.DeliveryId);
        if (replay is not null) return replay;

        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Entrega {deliveryId} no encontrada.");

        // #162: el despacho solo procede desde Draft; no hay despacho retroactivo ni repetido.
        if (delivery.Status != DeliveryStates.Draft)
        {
            throw new FieldConflictException(FieldErrorCodes.InvalidStateTransition,
                $"Solo una entrega Draft puede despacharse; la entrega está en {delivery.Status}.");
        }

        if (request.HandlingUnitIds is null || request.HandlingUnitIds.Count == 0)
        {
            throw new FieldConflictException(FieldErrorCodes.NoHandlingUnits,
                "El despacho debe incluir al menos un bulto preparado por Bodega.");
        }

        // El despacho cubre TODOS los bultos de la entrega: un subconjunto dejaría otra caja Sealed
        // irrecuperable porque después del despacho ya no se puede preparar.
        var allDeliveryUnits = await _db.HandlingUnits
            .Where(hu => hu.DeliveryId == delivery.Id && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        if (allDeliveryUnits.Count == 0)
        {
            throw new FieldConflictException(FieldErrorCodes.NoHandlingUnits,
                "La entrega no tiene bultos preparados por Bodega.");
        }

        var requestedIds = request.HandlingUnitIds.Distinct().ToList();
        var allUnitIds = allDeliveryUnits.Select(hu => hu.Id).ToHashSet();
        if (requestedIds.Count != request.HandlingUnitIds.Count // IDs repetidos
            || requestedIds.Count != allDeliveryUnits.Count
            || !requestedIds.ToHashSet().SetEquals(allUnitIds))
        {
            var missing = allDeliveryUnits.Where(hu => !requestedIds.Contains(hu.Id)).Select(hu => hu.Code).ToList();
            throw new FieldConflictException(FieldErrorCodes.IncompleteHandlingUnits,
                $"El despacho debe incluir exactamente todos los bultos de la entrega; pendientes: {string.Join(", ", missing)}.");
        }

        var handlingUnits = allDeliveryUnits;

        foreach (var hu in handlingUnits)
        {
            if (!HandlingUnitStates.IsPrepared(hu.Status))
            {
                throw new FieldConflictException(FieldErrorCodes.HandlingUnitStateConflict,
                    $"La caja {hu.Code} no está preparada para despacho (estado {hu.Status}).");
            }
        }

        // Obtener la ubicación del vehículo para el traslado
        Guid vehicleLocationId = delivery.OriginLocationId;
        if (delivery.RouteStopId.HasValue)
        {
            var stop = await _db.RouteStops.FirstOrDefaultAsync(s => s.Id == delivery.RouteStopId.Value, ct);
            if (stop != null)
            {
                var plan = await _db.RoutePlans.FirstOrDefaultAsync(p => p.Id == stop.RoutePlanId, ct);
                if (plan != null)
                {
                    var vLoc = await _db.Locations.FirstOrDefaultAsync(l => l.VehicleId == plan.VehicleId && l.OrganizationId == DemoOrganization.Id, ct);
                    if (vLoc != null)
                    {
                        vehicleLocationId = vLoc.Id;
                    }
                }
            }
        }

        // Consumir reservas y mover stock de bodega a vehículo. El ledger se agrupa por línea y lote:
        // dos bultos del mismo lote generan un solo movimiento y evitan que la primera reserva consumida
        // oculte el saldo de la segunda caja.
        var huLines = await _db.HandlingUnitLines
            .Where(hul => request.HandlingUnitIds.Contains(hul.HandlingUnitId) && hul.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        if (huLines.Count == 0 || huLines.Any(l => l.Quantity <= 0m))
        {
            throw new FieldConflictException(FieldErrorCodes.HandlingUnitStateConflict,
                "Los bultos del despacho no tienen líneas válidas con cantidad positiva.");
        }

        var reservations = await _db.RouteStockReservations
            .Where(r => r.DeliveryId == delivery.Id && r.OrganizationId == DemoOrganization.Id && r.Status == RouteStockReservationStatus.Active)
            .ToListAsync(ct);

        var deliveryLines = await _db.DeliveryLines
            .Where(dl => dl.DeliveryId == delivery.Id && dl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var dLinesById = deliveryLines.ToDictionary(dl => dl.Id, dl => dl);

        var dispatchGroups = new List<(DeliveryLine DeliveryLine, Guid LotId, decimal Quantity)>();
        foreach (var group in huLines
            .GroupBy(l => (l.DeliveryLineId, l.LotId))
            .Select(g => new { g.Key.DeliveryLineId, g.Key.LotId, Quantity = g.Sum(x => x.Quantity) })
            .OrderBy(g => g.DeliveryLineId)
            .ThenBy(g => g.LotId))
        {
            if (group.DeliveryLineId is not Guid lineId || !dLinesById.TryGetValue(lineId, out var dLine))
            {
                throw new FieldConflictException(FieldErrorCodes.HandlingUnitStateConflict,
                    "Una línea de bulto no pertenece a las líneas de la entrega.");
            }

            if (dLine.LotId != group.LotId)
            {
                throw new FieldConflictException(FieldErrorCodes.HandlingUnitStateConflict,
                    "El lote del bulto no coincide con el lote reservado de la línea.");
            }

            // El despacho cubre exactamente lo planificado: después no se admiten bultos nuevos y
            // una línea a medias quedaría irrecuperable para el recibo final.
            if (dLine.DispatchedQuantity + group.Quantity != dLine.PlannedQuantity)
            {
                throw new FieldConflictException(FieldErrorCodes.IncompleteHandlingUnits,
                    $"El despacho debe cubrir exactamente lo planificado de la línea {dLine.Id}: hay {dLine.DispatchedQuantity + group.Quantity} de {dLine.PlannedQuantity}.");
            }

            dispatchGroups.Add((dLine, group.LotId, group.Quantity));
        }

        var coveredLineIds = dispatchGroups.Select(g => g.DeliveryLine.Id).ToHashSet();
        var uncoveredLines = deliveryLines.Where(dl => dl.PlannedQuantity > 0m && !coveredLineIds.Contains(dl.Id)).Select(dl => dl.Id).ToList();
        if (uncoveredLines.Count > 0)
        {
            throw new FieldConflictException(FieldErrorCodes.IncompleteHandlingUnits,
                $"El despacho debe cubrir todas las líneas planificadas; sin bultos: {string.Join(", ", uncoveredLines)}.");
        }

        if (vehicleLocationId == delivery.OriginLocationId)
        {
            throw new FieldConflictException(FieldErrorCodes.HandlingUnitStateConflict,
                "La entrega no tiene una ubicación de vehículo distinta a la bodega de origen.");
        }

        // Saldos en memoria: dos líneas del mismo lote comparten la misma fila y no se duplican altas.
        var dispatchBalances = new Dictionary<(Guid LocationId, Guid LotId), StockBalance>();
        async Task<StockBalance?> FindDispatchBalanceAsync(Guid locationId, Guid lotId)
        {
            if (dispatchBalances.TryGetValue((locationId, lotId), out var cached)) return cached;
            var found = await _db.StockBalances
                .FirstOrDefaultAsync(sb => sb.LocationId == locationId && sb.LotId == lotId && sb.OrganizationId == DemoOrganization.Id, ct);
            if (found != null) dispatchBalances[(locationId, lotId)] = found;
            return found;
        }

        StockBalance NewDispatchBalance(Guid locationId, Guid lotId, decimal initial)
        {
            var created = new StockBalance
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = locationId,
                LotId = lotId,
                QuantityOnHand = initial,
                QuantityReserved = 0m
            };
            _db.StockBalances.Add(created);
            dispatchBalances[(locationId, lotId)] = created;
            return created;
        }

        foreach (var (dLine, lotId, quantity) in dispatchGroups)
        {
            dLine.DispatchedQuantity += quantity;

            // Descontar en StockBalance de bodega: sin Math.Max que oculte un faltante.
            var depotBalance = await FindDispatchBalanceAsync(delivery.OriginLocationId, lotId);
            if (depotBalance is null || depotBalance.QuantityOnHand < quantity)
            {
                throw new FieldConflictException(FieldErrorCodes.QuantityExceeded,
                    $"Stock insuficiente en bodega para despachar {quantity} del lote {lotId}.");
            }

            depotBalance.QuantityOnHand -= quantity;
            depotBalance.QuantityReserved = Math.Max(0m, depotBalance.QuantityReserved - quantity);

            // Acreditar en StockBalance del vehículo
            var vehicleBalance = await FindDispatchBalanceAsync(vehicleLocationId, lotId);
            if (vehicleBalance == null) NewDispatchBalance(vehicleLocationId, lotId, quantity);
            else vehicleBalance.QuantityOnHand += quantity;

            // Conciliar todas las reservas activas de la línea y lote.
            foreach (var res in reservations.Where(r => r.DeliveryLineId == dLine.Id && r.LotId == lotId && r.Status == RouteStockReservationStatus.Active))
            {
                res.Status = RouteStockReservationStatus.Consumed;
                res.ClosedAtUtc = DateTime.UtcNow;
            }

            // Registrar un solo movimiento de inventario bodega -> vehículo por línea y lote.
            _db.InventoryMovements.Add(new InventoryMovement
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Dispatch",
                LotId = lotId,
                FromLocationId = delivery.OriginLocationId,
                ToLocationId = vehicleLocationId,
                Quantity = quantity,
                ActorAccountId = actorAccountId,
                OperationId = request.OperationId,
                OccurredAtUtc = request.OccurredAtDevice ?? DateTime.UtcNow,
                DeliveryLineId = dLine.Id,
                Reason = "Despacho de entrega y transferencia de custodia"
            });
        }

        Guid firstCustodyEventId = Guid.Empty;
        var now = DateTime.UtcNow;

        foreach (var hu in handlingUnits)
        {
            hu.Status = HandlingUnitStates.Dispatched;
            hu.LocationId = vehicleLocationId;

            var custodyEvent = new CustodyEvent
            {
                OrganizationId = DemoOrganization.Id,
                HandlingUnitId = hu.Id,
                DeliveryId = delivery.Id,
                FromLocationId = delivery.OriginLocationId,
                ToLocationId = vehicleLocationId,
                ActorAccountId = actorAccountId,
                EventKind = CustodyEventKinds.Dispatched,
                OperationId = request.OperationId,
                OccurredAtDevice = request.OccurredAtDevice,
                ReceivedAtServer = now
            };

            _db.CustodyEvents.Add(custodyEvent);
            if (firstCustodyEventId == Guid.Empty)
            {
                firstCustodyEventId = custodyEvent.Id;
            }
        }

        delivery.Status = DeliveryStates.Dispatched;
        delivery.DispatchedAtUtc = now;

        var response = new DispatchResponse(
            delivery.Id,
            DeliveryStates.Dispatched,
            handlingUnits.Select(h => h.Id).ToList(),
            now,
            firstCustodyEventId != Guid.Empty ? firstCustodyEventId : Guid.NewGuid());

        var record = new IdempotencyRecord
        {
            OrganizationId = DemoOrganization.Id,
            AccountId = actorAccountId,
            OperationId = request.OperationId,
            ActionCode = actionCode,
            RequestHash = ComputeRequestFingerprint(actionCode, deliveryId, request),
            ResponseStatus = 200,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
            CompletedAtUtc = now,
            RetainUntilUtc = now.AddDays(7)
        };
        _db.IdempotencyRecords.Add(record);

        await _db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return response;
    }

    // #162: el conductor asignado toma la custodia (Dispatched → InTransit) y la entrega
    // físicamente en el punto (InTransit → Delivered). Son eventos de custodia sin movimiento de stock.
    public Task<CustodyTransitionResponse> PickupAsync(
        Guid deliveryId,
        CustodyTransitionRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        CancellationToken ct = default) =>
        AdvanceCustodyAsync(deliveryId, request, actorAccountId, idempotencyKey, "Pickup",
            DeliveryStates.Dispatched, DeliveryStates.InTransit, HandlingUnitStates.Dispatched,
            HandlingUnitStates.InTransit, CustodyEventKinds.PickedUp, ct);

    public Task<CustodyTransitionResponse> DeliverAsync(
        Guid deliveryId,
        CustodyTransitionRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        CancellationToken ct = default) =>
        AdvanceCustodyAsync(deliveryId, request, actorAccountId, idempotencyKey, "Deliver",
            DeliveryStates.InTransit, DeliveryStates.Delivered, HandlingUnitStates.InTransit,
            HandlingUnitStates.Delivered, CustodyEventKinds.Delivered, ct);

    private async Task<CustodyTransitionResponse> AdvanceCustodyAsync(
        Guid deliveryId,
        CustodyTransitionRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        string actionCode,
        string fromDeliveryState,
        string toDeliveryState,
        string fromUnitState,
        string toUnitState,
        string eventKind,
        CancellationToken ct,
        bool requireAssignedDriver = true)
    {
        var replay = await FindReplayAsync<CustodyTransitionResponse>(actorAccountId, request.OperationId, actionCode, deliveryId, request, idempotencyKey, ct, legacyResourceSelector: r => r.DeliveryId);
        if (replay is not null) return replay;

        await using var transaction = await BeginSerializedTransactionAsync(ct);

        replay = await FindReplayAsync<CustodyTransitionResponse>(actorAccountId, request.OperationId, actionCode, deliveryId, request, idempotencyKey, ct, legacyResourceSelector: r => r.DeliveryId);
        if (replay is not null) return replay;

        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Entrega {deliveryId} no encontrada.");

        // La ausencia de asignación no concede permiso: sin conductor asignado la transición es 403.
        if (requireAssignedDriver && delivery.AssignedAccountId != actorAccountId)
        {
            throw new FieldForbiddenException(FieldErrorCodes.UnauthorizedAssignment,
                "El conductor no está asignado a esta entrega.");
        }

        if (delivery.Status != fromDeliveryState)
        {
            if (delivery.Status is DeliveryStates.Received or DeliveryStates.Partial)
            {
                throw new FieldConflictException(FieldErrorCodes.AlreadyReceived,
                    "La entrega ya fue recibida en el punto; no admite más transiciones de custodia.");
            }

            if (delivery.Status is DeliveryStates.Draft)
            {
                throw new FieldConflictException(FieldErrorCodes.NotDispatched,
                    "La entrega aún no fue despachada por Bodega.");
            }

            var expectation = actionCode == "Pickup"
                ? "solo una entrega despachada puede recogerse"
                : "solo una entrega en ruta puede marcarse como entregada";
            throw new FieldConflictException(FieldErrorCodes.InvalidStateTransition,
                $"Transición inválida: {expectation} (estado actual {delivery.Status}).");
        }

        var handlingUnits = await _db.HandlingUnits
            .Where(hu => hu.DeliveryId == delivery.Id && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        if (handlingUnits.Count == 0)
        {
            throw new FieldConflictException(FieldErrorCodes.NoHandlingUnits,
                "La entrega no tiene bultos preparados por Bodega; el conductor no puede asumir custodia.");
        }

        foreach (var hu in handlingUnits)
        {
            if (hu.Status != fromUnitState)
            {
                throw new FieldConflictException(FieldErrorCodes.HandlingUnitStateConflict,
                    $"El bulto {hu.Code} no está en el estado esperado ({fromUnitState}); estado actual {hu.Status}.");
            }
        }

        // Evento de custodia: sin movimiento de inventario. Las ubicaciones describen la cadena física.
        var (vehicleLocationId, destinationLocationId) = await ResolveVehicleAndDestinationAsync(delivery, handlingUnits, ct);
        var now = DateTime.UtcNow;
        var fromLocationId = actionCode == "Pickup" ? delivery.OriginLocationId : vehicleLocationId;
        var toLocationId = actionCode == "Pickup" ? vehicleLocationId : destinationLocationId;

        delivery.Status = toDeliveryState;
        Guid firstCustodyEventId = Guid.Empty;
        foreach (var hu in handlingUnits)
        {
            hu.Status = toUnitState;

            var custodyEvent = new CustodyEvent
            {
                OrganizationId = DemoOrganization.Id,
                HandlingUnitId = hu.Id,
                DeliveryId = delivery.Id,
                FromLocationId = fromLocationId,
                ToLocationId = toLocationId,
                ActorAccountId = actorAccountId,
                EventKind = eventKind,
                OperationId = request.OperationId,
                OccurredAtDevice = request.OccurredAtDevice,
                ReceivedAtServer = now
            };
            _db.CustodyEvents.Add(custodyEvent);
            if (firstCustodyEventId == Guid.Empty) firstCustodyEventId = custodyEvent.Id;
        }

        var response = new CustodyTransitionResponse(
            delivery.Id,
            toDeliveryState,
            handlingUnits.Select(h => h.Id).ToList(),
            now,
            firstCustodyEventId != Guid.Empty ? firstCustodyEventId : Guid.NewGuid());

        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            OrganizationId = DemoOrganization.Id,
            AccountId = actorAccountId,
            OperationId = request.OperationId,
            ActionCode = actionCode,
            RequestHash = ComputeRequestFingerprint(actionCode, deliveryId, request),
            ResponseStatus = 200,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
            CompletedAtUtc = now,
            RetainUntilUtc = now.AddDays(7)
        });

        await _db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return response;
    }

    private async Task<ReceiptResponse> CreateReceiptCoreAsync(
        Guid deliveryId,
        CreateReceiptRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        CancellationToken ct)
    {
        const string actionCode = "Receipt";

        // 1. Replay rápido de la misma operación válida: devuelve la respuesta previa incluso
        //    después de que la entrega avanzó a un estado terminal.
        var replay = await FindReplayAsync<ReceiptResponse>(actorAccountId, request.OperationId, actionCode, deliveryId, request, idempotencyKey, ct, legacyResourceSelector: r => r.DeliveryId);
        if (replay is not null) return replay;

        await using var transaction = await BeginSerializedTransactionAsync(ct);

        replay = await FindReplayAsync<ReceiptResponse>(actorAccountId, request.OperationId, actionCode, deliveryId, request, idempotencyKey, ct, legacyResourceSelector: r => r.DeliveryId);
        if (replay is not null) return replay;

        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Entrega {deliveryId} no encontrada.");

        // 2. Máquina de estados #162: Received y Partial son terminales y cualquier recibo previo
        //    (incluso con estado legado Delivered o inconsistente) responde ALREADY_RECEIVED ANTES
        //    de validar la versión: el contrato exige ese código para cualquier recibo final previo.
        var previousReceipt = await _db.Receipts
            .AnyAsync(r => r.DeliveryId == delivery.Id && r.OrganizationId == DemoOrganization.Id, ct);

        if (DeliveryStates.IsReceived(delivery.Status) || previousReceipt)
        {
            throw new FieldConflictException(FieldErrorCodes.AlreadyReceived,
                "La entrega ya tiene un recibo final; no se repiten efectos. Lo rechazado permanece bajo custodia del conductor hasta su devolución explícita.");
        }

        // 3. Validación de versión esperada (solo para entregas aún no recibidas)
        if (request.ExpectedDeliveryVersion > 0 && delivery.Version != request.ExpectedDeliveryVersion)
        {
            throw new FieldConflictException(FieldErrorCodes.AssignmentChanged,
                $"Conflicto de versión de entrega (servidor: {delivery.Version}, esperado: {request.ExpectedDeliveryVersion}).");
        }

        if (request.Lines.Count == 0)
        {
            throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded, "La recepción debe incluir al menos una línea.");
        }

        if (delivery.Status == DeliveryStates.Draft)
        {
            throw new FieldConflictException(FieldErrorCodes.NotDispatched,
                "La entrega aún no fue despachada por Bodega.");
        }

        if (delivery.Status is DeliveryStates.Dispatched or DeliveryStates.InTransit)
        {
            throw new FieldConflictException(FieldErrorCodes.NotDelivered,
                "El Conductor aún no entrega este bulto en el punto.");
        }

        if (delivery.Status != DeliveryStates.Delivered)
        {
            throw new FieldConflictException(FieldErrorCodes.InvalidStateTransition,
                $"La entrega está en un estado que no admite recepción ({delivery.Status}).");
        }

        var handlingUnits = await _db.HandlingUnits
            .Where(hu => hu.DeliveryId == delivery.Id && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        if (handlingUnits.Count == 0)
        {
            throw new FieldConflictException(FieldErrorCodes.NoHandlingUnits,
                "La entrega no tiene bultos entregados que recibir.");
        }

        foreach (var hu in handlingUnits)
        {
            if (HandlingUnitStates.IsReceived(hu.Status))
            {
                throw new FieldConflictException(FieldErrorCodes.AlreadyReceived,
                    $"El bulto {hu.Code} ya fue recibido; no se repiten efectos.");
            }

            if (hu.Status != HandlingUnitStates.Delivered)
            {
                throw new FieldConflictException(FieldErrorCodes.NotDelivered,
                    $"El bulto {hu.Code} no está entregado por el conductor (estado {hu.Status}).");
            }
        }

        var deliveryLines = await _db.DeliveryLines
            .Where(dl => dl.DeliveryId == delivery.Id && dl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var dLinesById = deliveryLines.ToDictionary(dl => dl.Id, dl => dl);

        // 4. Cobertura completa y exacta ANTES de crear Receipt/ReceiptLine: un recibo final por
        //    entrega cubre todas las líneas despachadas, sin omisiones, duplicados ni lotes ajenos.
        if (request.Lines.Select(l => l.DeliveryLineId).Distinct().Count() != request.Lines.Count)
        {
            throw new FieldUnprocessableException(FieldErrorCodes.ReceiptCoverageMismatch,
                "La recepción no admite líneas de entrega duplicadas.");
        }

        if (request.Lines.Any(l => l.QuantityAccepted < 0m || l.QuantityRejected < 0m))
        {
            throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded,
                "Las cantidades aceptadas y rechazadas no pueden ser negativas.");
        }

        var dispatchedLines = deliveryLines.Where(dl => dl.DispatchedQuantity > 0m).ToList();
        var requestedIds = request.Lines.Select(l => l.DeliveryLineId).ToHashSet();
        var expectedIds = dispatchedLines.Select(dl => dl.Id).ToHashSet();

        if (dispatchedLines.Count == 0 || !requestedIds.SetEquals(expectedIds))
        {
            throw new FieldUnprocessableException(FieldErrorCodes.ReceiptCoverageMismatch,
                "El recibo debe cubrir exactamente todas las líneas despachadas de la entrega.");
        }

        foreach (var reqLine in request.Lines)
        {
            var dLine = dLinesById[reqLine.DeliveryLineId];

            if (dLine.LotId != reqLine.LotId)
            {
                throw new FieldUnprocessableException(FieldErrorCodes.ReceiptCoverageMismatch,
                    $"El lote de la línea {dLine.Id} no coincide con lo despachado.");
            }

            if (reqLine.QuantityAccepted + reqLine.QuantityRejected != dLine.DispatchedQuantity)
            {
                throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded,
                    $"La suma aceptada+rechazada debe ser exactamente la cantidad despachada ({dLine.DispatchedQuantity}) por línea.");
            }
        }

        // 5. Ubicaciones: el vehículo debe ser distinto al punto y tener saldo suficiente para lo aceptado.
        Guid? vehicleLocationId = null;
        Guid destinationLocationId = delivery.OriginLocationId;

        if (delivery.RouteStopId.HasValue)
        {
            var stop = await _db.RouteStops.FirstOrDefaultAsync(s => s.Id == delivery.RouteStopId.Value, ct);
            if (stop != null)
            {
                var plan = await _db.RoutePlans.FirstOrDefaultAsync(p => p.Id == stop.RoutePlanId, ct);
                if (plan != null)
                {
                    var vLoc = await _db.Locations.FirstOrDefaultAsync(l => l.VehicleId == plan.VehicleId && l.OrganizationId == DemoOrganization.Id, ct);
                    if (vLoc != null) vehicleLocationId = vLoc.Id;
                }
                var pLoc = await _db.Locations.FirstOrDefaultAsync(l => l.ServicePointId == stop.ServicePointId && l.OrganizationId == DemoOrganization.Id, ct);
                if (pLoc != null) destinationLocationId = pLoc.Id;
            }
        }
        else
        {
            var pLoc = await _db.Locations.FirstOrDefaultAsync(l => l.ServicePointId == delivery.ServicePointId && l.OrganizationId == DemoOrganization.Id, ct);
            if (pLoc != null) destinationLocationId = pLoc.Id;

            vehicleLocationId = handlingUnits.Select(hu => (Guid?)hu.LocationId).FirstOrDefault();
        }

        if (vehicleLocationId is null || vehicleLocationId == destinationLocationId)
        {
            throw new FieldConflictException(FieldErrorCodes.NotDelivered,
                "La entrega no tiene una ubicación de vehículo distinta al punto; no se puede transferir stock.");
        }

        var acceptedByLot = request.Lines
            .GroupBy(l => l.LotId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityAccepted));

        foreach (var (lotId, accepted) in acceptedByLot)
        {
            if (accepted <= 0m) continue;
            var vehicleBalance = await _db.StockBalances
                .FirstOrDefaultAsync(sb => sb.LocationId == vehicleLocationId.Value && sb.LotId == lotId && sb.OrganizationId == DemoOrganization.Id, ct);
            if (vehicleBalance is null || vehicleBalance.QuantityOnHand < accepted)
            {
                throw new FieldConflictException(FieldErrorCodes.InsufficientVehicleStock,
                    $"El vehículo no tiene saldo suficiente del lote {lotId} para transferir {accepted}.");
            }
        }

        var receipt = new Receipt
        {
            OrganizationId = DemoOrganization.Id,
            DeliveryId = delivery.Id,
            ActorAccountId = actorAccountId,
            OperationId = request.OperationId,
            OccurredAtDevice = request.OccurredAtDevice,
            ReceivedAtServer = DateTime.UtcNow
        };
        _db.Receipts.Add(receipt);

        bool hasRejections = false;
        var responseLines = new List<ReceiptLineDto>();

        var lotIds = request.Lines.Select(l => l.LotId).Distinct().ToList();
        var lots = await _db.Lots
            .Where(l => lotIds.Contains(l.Id) && l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, ct);

        // Saldos leídos o creados en esta recepción: una segunda línea del mismo lote debe ver
        // los cambios aún no guardados, en vez de consultar la base y crear un saldo duplicado.
        var balances = new Dictionary<(Guid LocationId, Guid LotId), StockBalance>();

        async Task<StockBalance?> FindBalanceAsync(Guid locationId, Guid lotId)
        {
            if (balances.TryGetValue((locationId, lotId), out var cached)) return cached;
            var found = await _db.StockBalances
                .FirstOrDefaultAsync(sb => sb.LocationId == locationId && sb.LotId == lotId && sb.OrganizationId == DemoOrganization.Id, ct);
            if (found != null) balances[(locationId, lotId)] = found;
            return found;
        }

        StockBalance NewBalance(Guid locationId, Guid lotId)
        {
            var created = new StockBalance
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = locationId,
                LotId = lotId,
                QuantityOnHand = 0m,
                QuantityReserved = 0m
            };
            _db.StockBalances.Add(created);
            balances[(locationId, lotId)] = created;
            return created;
        }

        // 6. Registrar lo recibido: la cobertura exacta ya está validada; es el único recibo de la entrega.
        var receiptLines = new List<(ReceiptLineRequest Request, DeliveryLine DeliveryLine, ReceiptLine ReceiptLine)>();
        foreach (var reqLine in request.Lines)
        {
            var dLine = dLinesById[reqLine.DeliveryLineId];
            dLine.AcceptedQuantity = reqLine.QuantityAccepted;
            dLine.RejectedQuantity = reqLine.QuantityRejected;

            if (reqLine.QuantityRejected > 0)
            {
                hasRejections = true;
            }

            string? normalizedReason = null;
            if (reqLine.QuantityRejected > 0 && !string.IsNullOrWhiteSpace(reqLine.RejectionReason))
            {
                normalizedReason = ReceiptRejectionReasons.TryNormalize(reqLine.RejectionReason, out var canonical)
                    ? canonical
                    : reqLine.RejectionReason.Trim();
            }

            var rLine = new ReceiptLine
            {
                OrganizationId = DemoOrganization.Id,
                ReceiptId = receipt.Id,
                DeliveryLineId = dLine.Id,
                QuantityAccepted = reqLine.QuantityAccepted,
                QuantityRejected = reqLine.QuantityRejected,
                RejectionReason = normalizedReason,
                RemainingQuantity = reqLine.RemainingQuantity,
                NextRequestedQuantity = reqLine.NextRequestedQuantity
            };
            _db.ReceiptLines.Add(rLine);
            receiptLines.Add((reqLine, dLine, rLine));

            responseLines.Add(new ReceiptLineDto(
                reqLine.DeliveryLineId,
                reqLine.LotId,
                reqLine.QuantityAccepted,
                reqLine.QuantityRejected,
                reqLine.Unit,
                normalizedReason,
                reqLine.RemainingQuantity,
                reqLine.NextRequestedQuantity));
        }

        // 5. Stock en percha declarado ("me quedan X"), una vez por lote y antes de sumar lo recibido.
        // La diferencia con el saldo del punto queda como movimiento FieldAdjustment: el saldo se
        // reconstruye sumando movimientos, sin sobrescrituras silenciosas (#111).
        var consumedByProduct = new Dictionary<Guid, decimal>();
        var reconciledLots = new HashSet<Guid>();
        foreach (var (reqLine, _, rLine) in receiptLines)
        {
            if (!reqLine.RemainingQuantity.HasValue || !reconciledLots.Add(reqLine.LotId)) continue;

            var declared = Math.Max(0m, reqLine.RemainingQuantity.Value);
            var destBalance = await FindBalanceAsync(destinationLocationId, reqLine.LotId);
            var previousOnHand = destBalance?.QuantityOnHand ?? 0m;
            var difference = declared - previousOnHand;
            if (difference == 0m) continue;

            destBalance ??= NewBalance(destinationLocationId, reqLine.LotId);
            destBalance.QuantityOnHand = declared;

            _db.InventoryMovements.Add(new InventoryMovement
            {
                OrganizationId = DemoOrganization.Id,
                Kind = MovementKinds.FieldAdjustment,
                LotId = reqLine.LotId,
                FromLocationId = difference < 0m ? destinationLocationId : null,
                ToLocationId = difference > 0m ? destinationLocationId : null,
                Quantity = Math.Abs(difference),
                ActorAccountId = actorAccountId,
                OperationId = request.OperationId,
                OccurredAtUtc = request.OccurredAtDevice,
                ReceiptLineId = rLine.Id,
                Reason = difference < 0m
                    ? $"Stock en percha declarado ({declared}) menor que el saldo del punto ({previousOnHand}): consumo derivado"
                    : $"Stock en percha declarado ({declared}) mayor que el saldo del punto ({previousOnHand}): ajuste por conteo"
            });

            // Si tenía más de lo que queda, el punto consumió la diferencia
            if (difference < 0m && lots.TryGetValue(reqLine.LotId, out var lot))
            {
                consumedByProduct[lot.ProductId] = consumedByProduct.GetValueOrDefault(lot.ProductId) - difference;
            }
        }

        // 7. Mover inventario de lo aceptado vehículo -> punto de destino. El saldo del vehículo
        //    ya se validó por lote antes de crear el recibo; se descuenta lo aceptado, sin ocultar faltantes.
        foreach (var (reqLine, dLine, rLine) in receiptLines)
        {
            if (reqLine.QuantityAccepted <= 0m) continue;

            var vehicleBalance = await FindBalanceAsync(vehicleLocationId!.Value, reqLine.LotId);
            if (vehicleBalance is null || vehicleBalance.QuantityOnHand < reqLine.QuantityAccepted)
            {
                throw new FieldConflictException(FieldErrorCodes.InsufficientVehicleStock,
                    $"El vehículo no tiene saldo suficiente del lote {reqLine.LotId} para transferir {reqLine.QuantityAccepted}.");
            }
            vehicleBalance.QuantityOnHand -= reqLine.QuantityAccepted;

            var destBalance = await FindBalanceAsync(destinationLocationId, reqLine.LotId)
                ?? NewBalance(destinationLocationId, reqLine.LotId);
            destBalance.QuantityOnHand += reqLine.QuantityAccepted;

            _db.InventoryMovements.Add(new InventoryMovement
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Receipt",
                LotId = reqLine.LotId,
                FromLocationId = vehicleLocationId,
                ToLocationId = destinationLocationId,
                Quantity = reqLine.QuantityAccepted,
                ActorAccountId = actorAccountId,
                OperationId = request.OperationId,
                OccurredAtUtc = request.OccurredAtDevice,
                DeliveryLineId = dLine.Id,
                ReceiptLineId = rLine.Id,
                Reason = "Recepción de entrega en punto de servicio"
            });
        }

        // 7. Consumo derivado: una fila por punto, producto y día (índice único de Consumption).
        // Se acumula sobre el consumo de campo del día; un consumo importado no se modifica,
        // y la cantidad queda trazada en el movimiento FieldAdjustment.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var (productId, consumed) in consumedByProduct)
        {
            var existing = await _db.Consumptions
                .FirstOrDefaultAsync(c => c.ServicePointId == delivery.ServicePointId && c.ProductId == productId && c.OccurredOn == today, ct);

            if (existing == null)
            {
                _db.Consumptions.Add(new Consumption
                {
                    OrganizationId = DemoOrganization.Id,
                    ServicePointId = delivery.ServicePointId,
                    ProductId = productId,
                    OccurredOn = today,
                    Quantity = consumed,
                    PeriodDays = Math.Max(1, _logistics.FieldReconciliationPeriodDays),
                    SourceKind = FieldReconciliationSource
                });
            }
            else if (existing.SourceKind == FieldReconciliationSource)
            {
                existing.Quantity += consumed;
            }
            else
            {
                _logger.LogWarning(
                    "Consumo derivado de {Consumed} del producto {ProductId} en la entrega {DeliveryId} no se registra: ya existe un consumo de origen {SourceKind} para {Day}. Queda en el movimiento FieldAdjustment.",
                    consumed, productId, delivery.Id, existing.SourceKind, today);
            }
        }

        // 8. Pedido futuro solicitado en campo ("necesito X más"): un pedido por recepción con una
        // línea por producto. El número se deriva de OperationId, estable ante reintentos.
        var requestedByProduct = receiptLines
            .Where(l => l.Request.NextRequestedQuantity is > 0m && lots.ContainsKey(l.Request.LotId))
            .GroupBy(l => lots[l.Request.LotId].ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(l => l.Request.NextRequestedQuantity!.Value)))
            .ToList();

        if (requestedByProduct.Count > 0)
        {
            var deliveryOrder = new DeliveryOrder
            {
                OrganizationId = DemoOrganization.Id,
                OrderNumber = $"ORD-{delivery.Code}-{request.OperationId:N}",
                DestinationServicePointId = delivery.ServicePointId,
                RequiredDateUtc = DateTime.UtcNow.AddDays(2),
                Priority = "Normal",
                Status = DeliveryOrderStatus.Pending,
                SourceType = "FieldRequest",
                Notes = "Solicitud en campo por receptor al conductor. Reposición requerida: " +
                        string.Join(", ", requestedByProduct.Select(r => r.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            };
            _db.DeliveryOrders.Add(deliveryOrder);

            foreach (var (productId, quantity) in requestedByProduct)
            {
                _db.DeliveryOrderLines.Add(new DeliveryOrderLine
                {
                    OrganizationId = DemoOrganization.Id,
                    DeliveryOrderId = deliveryOrder.Id,
                    ProductId = productId,
                    RequestedQuantity = quantity,
                    PlannedQuantity = 0m,
                    DeliveredQuantity = 0m
                });
            }
        }

        var finalStatus = hasRejections ? DeliveryStates.Partial : DeliveryStates.Received;
        delivery.Status = finalStatus;

        foreach (var hu in handlingUnits)
        {
            hu.Status = hasRejections ? HandlingUnitStates.Partial : HandlingUnitStates.Received;

            var custodyEvent = new CustodyEvent
            {
                OrganizationId = DemoOrganization.Id,
                HandlingUnitId = hu.Id,
                DeliveryId = delivery.Id,
                ReceiptId = receipt.Id,
                FromLocationId = vehicleLocationId,
                ToLocationId = destinationLocationId,
                ActorAccountId = actorAccountId,
                EventKind = hasRejections ? CustodyEventKinds.Partial : CustodyEventKinds.Received,
                OperationId = request.OperationId,
                OccurredAtDevice = request.OccurredAtDevice,
                ReceivedAtServer = DateTime.UtcNow
            };
            _db.CustodyEvents.Add(custodyEvent);
        }

        // FR-15: Completar la RouteStop
        if (delivery.RouteStopId.HasValue)
        {
            var stop = await _db.RouteStops
                .FirstOrDefaultAsync(s => s.Id == delivery.RouteStopId.Value && s.OrganizationId == DemoOrganization.Id, ct);

            if (stop != null)
            {
                stop.Status = "Completed";
                stop.ActualArrivalUtc = request.OccurredAtDevice;
            }
        }

        var response = new ReceiptResponse(
            receipt.Id,
            delivery.Id,
            DateTime.UtcNow,
            delivery.Version + 1,
            finalStatus,
            "Pending",
            responseLines);

        var record = new IdempotencyRecord
        {
            OrganizationId = DemoOrganization.Id,
            AccountId = actorAccountId,
            OperationId = request.OperationId,
            ActionCode = "Receipt",
            RequestHash = ComputeRequestFingerprint(actionCode, deliveryId, request),
            ResponseStatus = 200,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
            CompletedAtUtc = DateTime.UtcNow,
            RetainUntilUtc = DateTime.UtcNow.AddDays(7)
        };
        _db.IdempotencyRecords.Add(record);

        await _db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return response;
    }

    public async Task<MobileAssignmentResponse> GetMobileAssignmentsAsync(
        Guid driverAccountId,
        CancellationToken ct = default)
    {
        var deliveries = await _db.Deliveries
            .Where(d => d.AssignedAccountId == driverAccountId && d.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var stopIds = deliveries.Where(d => d.RouteStopId.HasValue).Select(d => d.RouteStopId!.Value).Distinct().ToList();
        var stops = await _db.RouteStops
            .Where(s => stopIds.Contains(s.Id) && s.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var planIds = stops.Select(s => s.RoutePlanId).Distinct().ToList();
        stops = await _db.RouteStops
            .Where(s => planIds.Contains(s.RoutePlanId) && s.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var plans = await _db.RoutePlans
            .Where(p => planIds.Contains(p.Id) && p.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var points = await _db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(sp => sp.Id, sp => sp, ct);

        var pickupLocationIds = stops
            .Where(s => s.Kind == RouteStopKind.Pickup && s.LocationId.HasValue)
            .Select(s => s.LocationId!.Value)
            .Distinct()
            .ToList();
        var pickupLocations = await _db.Locations
            .Where(location => pickupLocationIds.Contains(location.Id) && location.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(location => location.Id, location => location, ct);

        var vehicles = await _db.Vehicles
            .Where(v => v.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(v => v.Id, v => v, ct);

        var handlingUnits = await _db.HandlingUnits
            .Where(hu => deliveries.Select(d => d.Id).Contains(hu.DeliveryId ?? Guid.Empty) && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var huByDelivery = handlingUnits.GroupBy(hu => hu.DeliveryId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        // #162: el conductor necesita ver los bultos y sus líneas para poder recoger y entregar.
        var assignmentHuIds = handlingUnits.Select(hu => hu.Id).ToList();
        var huLines = await _db.HandlingUnitLines
            .Where(l => assignmentHuIds.Contains(l.HandlingUnitId) && l.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var huLinesByUnit = huLines.GroupBy(l => l.HandlingUnitId).ToDictionary(g => g.Key, g => g.ToList());
        var assignmentLots = await _db.Lots
            .Where(l => l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, l => l, ct);
        var assignmentProducts = await _db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        HandlingUnitLineDto ToAssignmentLineDto(HandlingUnitLine line)
        {
            var sku = "MED-001";
            var unit = "U";
            if (assignmentLots.TryGetValue(line.LotId, out var lot) && assignmentProducts.TryGetValue(lot.ProductId, out var product))
            {
                sku = product.Sku;
                unit = product.BaseUnit;
            }
            return new HandlingUnitLineDto(line.DeliveryLineId ?? Guid.Empty, line.LotId, sku, line.Quantity, unit);
        }

        var routes = new List<MobileAssignmentRoute>();

        foreach (var plan in plans)
        {
            var planStops = stops.Where(s => s.RoutePlanId == plan.Id).OrderBy(s => s.SequenceNumber).ToList();
            var planDeliveries = new List<MobileAssignmentDelivery>();
            var pickupStops = planStops
                .Where(s => s.Kind == RouteStopKind.Pickup)
                .Select(stop =>
                {
                    points.TryGetValue(stop.ServicePointId, out var servicePoint);
                    pickupLocations.TryGetValue(stop.LocationId ?? Guid.Empty, out var location);
                    return new MobileAssignmentPickupStop(
                        stop.Id,
                        stop.SequenceNumber,
                        stop.LocationId,
                        location?.Code ?? servicePoint?.Code ?? "BOD",
                        location?.Name ?? servicePoint?.Name ?? "Bodega de recogida",
                        "",
                        stop.PlannedArrivalUtc ?? DateTime.UtcNow.AddHours(2));
                })
                .ToList();

            vehicles.TryGetValue(plan.VehicleId, out var vehicle);

            foreach (var stop in planStops)
            {
                var stopDels = deliveries.Where(d => d.RouteStopId == stop.Id).ToList();
                points.TryGetValue(stop.ServicePointId, out var point);

                foreach (var del in stopDels)
                {
                    var delHus = huByDelivery.GetValueOrDefault(del.Id, new List<HandlingUnit>());
                    var huDtos = delHus.Select(hu => new HandlingUnitResponse(
                        hu.Id,
                        hu.Code,
                        del.Id,
                        hu.Status,
                        $"https://istpetdev.doicela.dev/public-trace/{hu.Id}",
                        null,
                        huLinesByUnit.GetValueOrDefault(hu.Id, new List<HandlingUnitLine>()).Select(ToAssignmentLineDto).ToList(),
                        hu.CreatedAtUtc)).ToList();

                    planDeliveries.Add(new MobileAssignmentDelivery(
                        del.Id,
                        stop.SequenceNumber,
                        point?.Code ?? "P00",
                        point?.Name ?? "Punto de Entrega",
                        "Dirección registrada",
                        stop.PlannedArrivalUtc ?? DateTime.UtcNow.AddHours(2),
                        del.Status,
                        huDtos));
                }
            }

            routes.Add(new MobileAssignmentRoute(
                plan.Id,
                plan.Revision,
                DateTime.UtcNow,
                "Ruta asignada verificada por plataforma.",
                vehicle?.Code ?? "",
                planStops.Count,
                planDeliveries,
                await OriginWarehouseNameAsync(deliveries.Where(d => planStops.Any(s => s.Id == d.RouteStopId)).Select(d => d.OriginLocationId).FirstOrDefault(), ct),
                pickupStops));
        }

        return new MobileAssignmentResponse(
            DateTime.UtcNow,
            driverAccountId,
            routes);
    }

    public async Task<DeliveryListResponse> GetDeliveriesAsync(
        Guid? routePlanId,
        string? status,
        int limit,
        string? cursor,
        CancellationToken ct = default)
    {
        var query = _db.Deliveries
            .Where(d => d.OrganizationId == DemoOrganization.Id)
            .AsQueryable();

        if (routePlanId.HasValue)
        {
            var stopIds = await _db.RouteStops
                .Where(s => s.RoutePlanId == routePlanId.Value)
                .Select(s => s.Id)
                .ToListAsync(ct);

            query = query.Where(d => d.RouteStopId.HasValue && stopIds.Contains(d.RouteStopId.Value));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(d => d.Status == status);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .ThenByDescending(d => d.Id)
            .Take(Math.Min(limit > 0 ? limit : 50, 100))
            .ToListAsync(ct);

        var points = await _db.ServicePoints
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        var listItems = new List<DeliveryListItem>();
        foreach (var del in items)
        {
            points.TryGetValue(del.ServicePointId, out var sp);
            var huCount = await _db.HandlingUnits.CountAsync(h => h.DeliveryId == del.Id, ct);

            listItems.Add(new DeliveryListItem(
                del.Id,
                routePlanId ?? Guid.Empty,
                sp?.Code ?? "P01",
                sp?.Name ?? "Centro de Salud",
                DateTime.UtcNow.AddHours(4),
                del.Status,
                "Confirmed",
                huCount));
        }

        return new DeliveryListResponse(listItems, null, total);
    }

    private async Task<HandlingUnit> RequireUnitWithTokenAsync(Guid unitId, string? traceToken, CancellationToken ct)
    {
        var notFound = new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Unit not found.");
        if (string.IsNullOrWhiteSpace(traceToken)) throw notFound;
        var hu = await _db.HandlingUnits.FirstOrDefaultAsync(h => h.Id == unitId && h.OrganizationId == DemoOrganization.Id, ct) ?? throw notFound;
        var tokenHash = ComputeSha256(traceToken.Trim());
        var valid = await _db.QrAccessTokens.AnyAsync(t => t.HandlingUnitId == hu.Id && t.TokenHash == tokenHash
            && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow, ct);
        return valid ? hu : throw notFound;
    }

    public async Task<DeliveryDetailResponse> GetDeliveryDetailAsync(
        Guid deliveryId,
        CancellationToken ct = default)
    {
        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Entrega {deliveryId} no encontrada.");

        var servicePoint = await _db.ServicePoints
            .FirstOrDefaultAsync(sp => sp.Id == delivery.ServicePointId && sp.OrganizationId == DemoOrganization.Id, ct);

        var routePlanId = Guid.Empty;
        if (delivery.RouteStopId.HasValue)
        {
            var stop = await _db.RouteStops
                .FirstOrDefaultAsync(s => s.Id == delivery.RouteStopId.Value && s.OrganizationId == DemoOrganization.Id, ct);
            if (stop != null)
            {
                routePlanId = stop.RoutePlanId;
            }
        }

        var lines = await _db.DeliveryLines
            .Where(dl => dl.DeliveryId == delivery.Id && dl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var lotIds = lines.Select(l => l.LotId).Distinct().ToList();
        var lots = await _db.Lots
            .Where(l => lotIds.Contains(l.Id) && l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, l => l, ct);

        var productIds = lots.Values.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        var lineDtos = new List<DeliveryDetailLineDto>();
        foreach (var line in lines)
        {
            lots.TryGetValue(line.LotId, out var lot);
            Product? prod = null;
            if (lot != null)
            {
                products.TryGetValue(lot.ProductId, out prod);
            }

            lineDtos.Add(new DeliveryDetailLineDto(
                line.Id,
                line.LotId,
                lot?.Code ?? "LOTE-DEFAULT",
                prod?.Sku ?? "MED-001",
                prod?.Name ?? "Producto",
                line.PlannedQuantity,
                prod?.BaseUnit ?? "U"));
        }

        var handlingUnits = await _db.HandlingUnits
            .Where(hu => hu.DeliveryId == delivery.Id && hu.OrganizationId == DemoOrganization.Id)
            .Select(hu => new DeliveryDetailHandlingUnitDto(
                hu.Id,
                hu.Code,
                hu.Status))
            .ToListAsync(ct);

        var vehicleCode = routePlanId == Guid.Empty ? null : await _db.RoutePlans.Where(p => p.Id == routePlanId)
            .Join(_db.Vehicles, p => p.VehicleId, v => v.Id, (p, v) => v.Code).FirstOrDefaultAsync(ct);
        var driverName = delivery.AssignedAccountId is { } driverId
            ? await _db.Accounts.Where(a => a.Id == driverId).Select(a => a.DisplayName).FirstOrDefaultAsync(ct)
            : null;

        var receiptIds = await _db.Receipts
            .Where(r => r.DeliveryId == delivery.Id && r.OrganizationId == DemoOrganization.Id)
            .Select(r => r.Id)
            .ToListAsync(ct);
        var confirmedEvidenceCount = receiptIds.Count == 0 ? 0 : await _db.Evidences
            .CountAsync(e => receiptIds.Contains(e.ReceiptId) && e.Status == "Confirmed" && e.OrganizationId == DemoOrganization.Id, ct);

        return new DeliveryDetailResponse(
            delivery.Id,
            delivery.Status,
            delivery.Version,
            servicePoint?.Name ?? "Punto de Entrega",
            routePlanId,
            lineDtos,
            handlingUnits,
            vehicleCode,
            driverName,
            await OriginWarehouseNameAsync(delivery.OriginLocationId, ct),
            confirmedEvidenceCount);
    }

    private async Task<string?> OriginWarehouseNameAsync(Guid locationId, CancellationToken ct) =>
        locationId == Guid.Empty ? null
            : await _db.Locations.Where(l => l.Id == locationId).Select(l => l.Name).FirstOrDefaultAsync(ct);

    public async Task<PublicTraceUnitDto> GetPublicTraceAsync(
        Guid unitId,
        string? traceToken,
        CancellationToken ct = default)
    {
        var hu = await RequireUnitWithTokenAsync(unitId, traceToken, ct);

        var lines = await _db.HandlingUnitLines
            .Where(l => l.HandlingUnitId == hu.Id && l.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var lots = await _db.Lots
            .Where(l => l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, l => l, ct);

        var products = await _db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        var productUnits = new List<PublicTraceProductUnit>();
        foreach (var line in lines)
        {
            if (lots.TryGetValue(line.LotId, out var lot) && products.TryGetValue(lot.ProductId, out var prod))
            {
                if (!productUnits.Any(pu => pu.Sku == prod.Sku))
                {
                    productUnits.Add(new PublicTraceProductUnit(prod.Sku, prod.BaseUnit));
                }
            }
        }

        var custodyEvents = await _db.CustodyEvents
            .Where(ce => ce.HandlingUnitId == hu.Id && ce.OrganizationId == DemoOrganization.Id)
            .OrderBy(ce => ce.ReceivedAtServer)
            .ToListAsync(ct);

        var events = new List<PublicTraceEvent>
        {
            new("Preparada", hu.SealedAtUtc ?? hu.CreatedAtUtc)
        };

        foreach (var ce in custodyEvents)
        {
            var stage = ce.EventKind switch
            {
                CustodyEventKinds.Dispatched => "Despachada",
                CustodyEventKinds.PickedUp => "En ruta",
                CustodyEventKinds.Delivered => "Entregada",
                CustodyEventKinds.Received => "Recibida",
                CustodyEventKinds.Partial => "Recibida Parcial",
                _ => ce.EventKind
            };
            events.Add(new(stage, ce.ReceivedAtServer));
        }

        return new PublicTraceUnitDto(
            hu.Id,
            hu.Code,
            hu.Status,
            productUnits,
            events);
    }

    public async Task<ReceiverUnitResponse> GetReceiverUnitAsync(Guid unitId, string? traceToken, CancellationToken ct = default)
    {
        var hu = await RequireUnitWithTokenAsync(unitId, traceToken, ct);
        if (hu.DeliveryId is not Guid deliveryId) throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Unit not found.");
        var delivery = await _db.Deliveries.SingleAsync(d => d.Id == deliveryId, ct);
        var point = await _db.ServicePoints.FirstOrDefaultAsync(p => p.Id == delivery.ServicePointId, ct);

        // #162: el recibo final es por ENTREGA. El receptor ve todos los bultos y todas las líneas
        // despachadas, no solo las de la caja escaneada, para no perder carga de otras cajas.
        var boxes = await _db.HandlingUnits
            .Where(h => h.DeliveryId == deliveryId && h.OrganizationId == DemoOrganization.Id)
            .OrderBy(h => h.Code)
            .Select(h => new ReceiverUnitBoxDto(h.Id, h.Code, h.Status))
            .ToListAsync(ct);

        var dispatchedLines = await _db.DeliveryLines
            .Where(l => l.DeliveryId == deliveryId && l.DispatchedQuantity > 0m)
            .ToListAsync(ct);

        var lotIds = dispatchedLines.Select(l => l.LotId).Distinct().ToList();
        var lots = await _db.Lots.Where(l => lotIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
        var productIds = lots.Values.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var lines = dispatchedLines.Select(l =>
        {
            lots.TryGetValue(l.LotId, out var lot);
            Product? product = null;
            if (lot != null) products.TryGetValue(lot.ProductId, out product);
            return new ReceiverUnitLineDto(
                l.Id,
                l.LotId,
                lot?.Code ?? "LOTE",
                product?.Sku ?? "MED-001",
                product?.Name ?? "Producto",
                l.DispatchedQuantity,
                product?.BaseUnit ?? "U");
        }).ToList();

        return new ReceiverUnitResponse(hu.Id, hu.Code, hu.Status, deliveryId, delivery.Version, point?.Name ?? "", boxes, lines);
    }

    public async Task<ReceiptResponse> CreateReceiverReceiptAsync(Guid unitId, string? traceToken, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default)
    {
        var hu = await RequireUnitWithTokenAsync(unitId, traceToken, ct);
        if (hu.DeliveryId is not Guid deliveryId) throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Unit not found.");
        foreach (var line in request.Lines.Where(l => l.QuantityRejected > 0))
            if (!ReceiptRejectionReasons.IsValid(line.RejectionReason))
                throw new FieldUnprocessableException("INVALID_REJECTION_REASON", $"Selecciona un motivo del catálogo {ReceiptRejectionReasons.Version}.");
        // El QR prueba que el Receptor tiene el bulto en la mano; el estado de la entrega y de TODOS
        // sus bultos lo valida la máquina de estados del recibo compartido.
        return await CreateReceiptCoreAsync(deliveryId, request, actorAccountId, idempotencyKey, ct);
    }

    public async Task<HandlingUnitTraceResponse> GetOperativeTraceAsync(
        Guid unitId,
        CancellationToken ct = default)
    {
        var hu = await _db.HandlingUnits
            .FirstOrDefaultAsync(h => h.Id == unitId && h.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Unidad {unitId} no encontrada.");

        var lines = await _db.HandlingUnitLines
            .Where(l => l.HandlingUnitId == hu.Id && l.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var lots = await _db.Lots
            .Where(l => l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, l => l, ct);

        var products = await _db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        var lineDtos = lines.Select(l =>
        {
            var sku = "MED-001";
            var unit = "U";
            if (lots.TryGetValue(l.LotId, out var lot) && products.TryGetValue(lot.ProductId, out var prod))
            {
                sku = prod.Sku;
                unit = prod.BaseUnit;
            }
            return new HandlingUnitLineDto(l.DeliveryLineId ?? Guid.Empty, l.LotId, sku, l.Quantity, unit);
        }).ToList();

        var events = await _db.CustodyEvents
            .Where(ce => ce.HandlingUnitId == hu.Id && ce.OrganizationId == DemoOrganization.Id)
            .OrderBy(ce => ce.ReceivedAtServer)
            .ToListAsync(ct);

        var locations = await _db.Locations
            .Where(loc => loc.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(loc => loc.Id, loc => loc.Code, ct);

        var eventDtos = events.Select(e => new HandlingUnitTraceEvent(
            e.Id,
            e.EventKind,
            e.ActorAccountId,
            e.FromLocationId.HasValue ? locations.GetValueOrDefault(e.FromLocationId.Value, "Origen") : null,
            e.ToLocationId.HasValue ? locations.GetValueOrDefault(e.ToLocationId.Value, "Destino") : null,
            e.ReceivedAtServer)).ToList();

        return new HandlingUnitTraceResponse(
            hu.Id,
            hu.Code,
            hu.Status,
            hu.DeliveryId,
            lineDtos,
            eventDtos);
    }

    public async Task RevokePublicTokenAsync(
        Guid unitId,
        Guid actorAccountId,
        CancellationToken ct = default)
    {
        var hu = await _db.HandlingUnits
            .FirstOrDefaultAsync(h => h.Id == unitId && h.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Unidad {unitId} no encontrada.");

        var tokens = await _db.QrAccessTokens
            .Where(t => t.HandlingUnitId == hu.Id && t.OrganizationId == DemoOrganization.Id && t.RevokedAtUtc == null)
            .ToListAsync(ct);

        foreach (var t in tokens)
        {
            t.RevokedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<DemoAdvanceResponse> AdvanceDemoAsync(
        Guid routePlanId,
        DemoAdvanceRequest request,
        Guid actorAccountId,
        CancellationToken ct = default)
    {
        // Idempotencia de la operación de demo completa: repetir el mismo operationId devuelve el resultado previo.
        var legacyAdvanceHash = ComputeSha256($"demo-advance:{routePlanId}:{request.Stops}");
        var replay = await FindReplayAsync<DemoAdvanceResponse>(actorAccountId, request.OperationId, "DemoAdvance", routePlanId, request, request.OperationId.ToString(), ct, legacyAdvanceHash);
        if (replay is not null) return replay;

        await using var transaction = await BeginSerializedTransactionAsync(ct);

        replay = await FindReplayAsync<DemoAdvanceResponse>(actorAccountId, request.OperationId, "DemoAdvance", routePlanId, request, request.OperationId.ToString(), ct, legacyAdvanceHash);
        if (replay is not null) return replay;

        var plan = await _db.RoutePlans
            .FirstOrDefaultAsync(p => p.Id == routePlanId && p.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Ruta {routePlanId} no encontrada.");

        var pendingStops = await _db.RouteStops
            .Where(s => s.RoutePlanId == plan.Id && s.OrganizationId == DemoOrganization.Id && s.Status != "Completed")
            .OrderBy(s => s.SequenceNumber)
            .Take(Math.Max(1, request.Stops))
            .ToListAsync(ct);

        var completedStops = new List<CompletedStopDto>();

        foreach (var stop in pendingStops)
        {
            // Advance simulates progress on the operational date of the route.
            // Server receipt/custody timestamps still record the real execution.
            var occurredAt = stop.PlannedArrivalUtc ?? DateTime.UtcNow;
            var deliveries = await _db.Deliveries
                .Where(d => d.RouteStopId == stop.Id && d.OrganizationId == DemoOrganization.Id)
                .ToListAsync(ct);

            foreach (var del in deliveries)
            {
                var delLines = await _db.DeliveryLines
                    .Where(dl => dl.DeliveryId == del.Id && dl.OrganizationId == DemoOrganization.Id)
                    .ToListAsync(ct);

                // Es una simulación de recorrido, no una acción física: las transiciones reales se
                // atribuyen a la cuenta que invoca el avance (Planificación/Gerencia), no al conductor.
                var custodyActor = actorAccountId;

                // 1. Preparar HandlingUnit si no existe
                var existingHus = await _db.HandlingUnits
                    .Where(hu => hu.DeliveryId == del.Id && hu.OrganizationId == DemoOrganization.Id)
                    .ToListAsync(ct);

                Guid huId;
                if (existingHus.Count == 0)
                {
                    var prepReq = new CreateHandlingUnitRequest(
                        Guid.NewGuid(),
                        delLines.Select(dl => new CreateHandlingUnitLineRequest(dl.Id, dl.LotId, dl.PlannedQuantity)).ToList());

                    var huResp = await CreateHandlingUnitAsync(del.Id, prepReq, actorAccountId, ct);
                    huId = huResp.HandlingUnitId;
                }
                else
                {
                    huId = existingHus[0].Id;
                }

                // 2. Despachar TODOS los bultos (solo desde Draft; una entrega ya avanzada no se re-despacha)
                if (del.Status == DeliveryStates.Draft)
                {
                    var allUnitIds = await _db.HandlingUnits
                        .Where(hu => hu.DeliveryId == del.Id && hu.OrganizationId == DemoOrganization.Id)
                        .Select(hu => hu.Id)
                        .ToListAsync(ct);
                    if (allUnitIds.Count == 0) allUnitIds = new List<Guid> { huId };

                    var dispReq = new DispatchRequest(Guid.NewGuid(), allUnitIds, occurredAt);
                    await DispatchAsync(del.Id, dispReq, actorAccountId, $"adv-{Guid.NewGuid()}", ct);
                }

                // 3. Recoger y entregar: pickup (Dispatched → InTransit) y deliver (InTransit → Delivered)
                if (del.Status == DeliveryStates.Dispatched)
                {
                    await AdvanceCustodyAsync(del.Id, new CustodyTransitionRequest(Guid.NewGuid(), occurredAt),
                        custodyActor, $"adv-{Guid.NewGuid()}", "Pickup",
                        DeliveryStates.Dispatched, DeliveryStates.InTransit, HandlingUnitStates.Dispatched,
                        HandlingUnitStates.InTransit, CustodyEventKinds.PickedUp, ct, requireAssignedDriver: false);
                }
                if (del.Status == DeliveryStates.InTransit)
                {
                    await AdvanceCustodyAsync(del.Id, new CustodyTransitionRequest(Guid.NewGuid(), occurredAt),
                        custodyActor, $"adv-{Guid.NewGuid()}", "Deliver",
                        DeliveryStates.InTransit, DeliveryStates.Delivered, HandlingUnitStates.InTransit,
                        HandlingUnitStates.Delivered, CustodyEventKinds.Delivered, ct, requireAssignedDriver: false);
                }

                // 4. Recibir 100% aceptado cubriendo todas las líneas despachadas (el rol Receptor no participa en el avance de demo)
                var receiptReq = new CreateReceiptRequest(
                    Guid.NewGuid(),
                    del.Version,
                    occurredAt,
                    delLines.Where(dl => dl.DispatchedQuantity > 0m)
                        .Select(dl => new ReceiptLineRequest(dl.Id, dl.LotId, dl.DispatchedQuantity, 0m, "U")).ToList());

                var receiptResp = await CreateReceiptCoreAsync(del.Id, receiptReq, custodyActor, $"rec-{Guid.NewGuid()}", ct);

                completedStops.Add(new CompletedStopDto(
                    stop.SequenceNumber,
                    del.Id,
                    receiptResp.ReceiptId,
                    occurredAt));
            }
        }

        var response = new DemoAdvanceResponse(completedStops);
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            OrganizationId = DemoOrganization.Id,
            AccountId = actorAccountId,
            OperationId = request.OperationId,
            ActionCode = "DemoAdvance",
            RequestHash = ComputeRequestFingerprint("DemoAdvance", routePlanId, request),
            ResponseStatus = 200,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
            CompletedAtUtc = DateTime.UtcNow,
            RetainUntilUtc = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return response;
    }

    public async Task<EvidenceUploadResponse> CreateEvidenceUploadAsync(
        Guid receiptId,
        EvidenceUploadRequest request,
        Guid actorAccountId,
        CancellationToken ct = default)
    {
        var receipt = await _db.Receipts
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Recibo {receiptId} no encontrado.");

        if (receipt.ActorAccountId != actorAccountId)
            throw new FieldForbiddenException(FieldErrorCodes.UnauthorizedAssignment, "Solo quien registró la recepción puede adjuntar sus evidencias.");
        if (request.OperationId == Guid.Empty || request.Kind is not ("photo" or "signature") ||
            request.MimeType is not ("image/jpeg" or "image/png" or "image/webp") ||
            request.ByteSize <= 0 || request.ByteSize > 10 * 1024 * 1024 ||
            request.Sha256.Length != 64 || !request.Sha256.All(Uri.IsHexDigit))
            throw new FieldUnprocessableException("INVALID_EVIDENCE", "Evidencia inválida: imagen de hasta 10 MB y SHA-256 requerido.");

        var storage = _evidenceStorage ?? throw new FieldException(503, "EVIDENCE_STORAGE_UNAVAILABLE", "Almacenamiento no configurado.");
        var key = $"evidences/{receipt.Id}/{request.Kind}-{request.OperationId}";
        // Deterministic PK makes retries reuse the evidence instead of creating orphan rows.
        var evidenceId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
        var evidence = await _db.Evidences.FirstOrDefaultAsync(e => e.Id == evidenceId && e.OrganizationId == DemoOrganization.Id, ct);
        if (evidence != null && (evidence.UploadedByAccountId != actorAccountId || evidence.Sha256 != request.Sha256 || evidence.ByteSize != request.ByteSize || evidence.MimeType != request.MimeType))
            throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict, "La operación de evidencia tiene otro contenido.");
        var upload = await storage.AuthorizeUploadAsync(key, request.MimeType, request.Sha256, ct);
        if (evidence == null)
        {
            evidence = new Evidence
            {
                Id = evidenceId, OrganizationId = DemoOrganization.Id, ReceiptId = receipt.Id,
                Kind = request.Kind, StorageKey = key, MimeType = request.MimeType,
                ByteSize = request.ByteSize, Sha256 = request.Sha256,
                Status = "Pending", UploadedByAccountId = actorAccountId
            };
            _db.Evidences.Add(evidence);
            await _db.SaveChangesAsync(ct);
        }
        return new EvidenceUploadResponse(evidence.Id, upload.Url, upload.Headers, upload.ExpiresAtUtc);
    }

    public async Task<EvidenceConfirmResponse> ConfirmEvidenceAsync(
        Guid evidenceId,
        EvidenceConfirmRequest request,
        Guid actorAccountId,
        CancellationToken ct = default)
    {
        var evidence = await _db.Evidences
            .FirstOrDefaultAsync(e => e.Id == evidenceId && e.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Evidencia {evidenceId} no encontrada.");
        if (evidence.UploadedByAccountId != actorAccountId)
            throw new FieldForbiddenException(FieldErrorCodes.UnauthorizedAssignment, "La evidencia pertenece a otra cuenta.");
        if (evidence.Sha256 != request.Sha256 || evidence.ByteSize != request.ByteSize)
            throw new FieldUnprocessableException("EVIDENCE_MISMATCH", "El hash o tamaño no coincide con la autorización.");
        if (evidence.Status != "Confirmed")
        {
            var storage = _evidenceStorage ?? throw new FieldException(503, "EVIDENCE_STORAGE_UNAVAILABLE", "Almacenamiento no configurado.");
            await storage.VerifyAsync(evidence.StorageKey, evidence.MimeType, request.ByteSize, request.Sha256, ct);
            evidence.Status = "Confirmed";
            evidence.ConfirmedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return new EvidenceConfirmResponse(evidence.Id, "Confirmed", evidence.ConfirmedAtUtc!.Value);
    }

    public RejectionReasonsCatalogResponse GetRejectionReasonsCatalog()
    {
        var items = ReceiptRejectionReasons.All
            .Select(code => new RejectionReasonDto(code, ReceiptRejectionReasons.GetDescription(code)))
            .ToList();
        return new RejectionReasonsCatalogResponse(ReceiptRejectionReasons.Version, items);
    }

    private async Task<T?> FindReplayAsync<T>(
        Guid actorAccountId,
        Guid operationId,
        string actionCode,
        Guid resourceId,
        object payload,
        string idempotencyKey,
        CancellationToken ct,
        string? legacyHash = null,
        Func<T, Guid?>? legacyResourceSelector = null)
        where T : class
    {
        if (operationId == Guid.Empty)
        {
            throw new FieldUnprocessableException(FieldErrorCodes.InvalidOperationId,
                "La operación requiere un operationId distinto de Guid.Empty.");
        }

        // #162: el índice idempotente es (organización, cuenta, operationId) SIN actionCode. Buscar
        // solo por operación evita el 23505 cuando una UUID se reutiliza en otra acción o entrega.
        var record = await _db.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganizationId == DemoOrganization.Id &&
                                      r.AccountId == actorAccountId &&
                                      r.OperationId == operationId, ct);
        if (record is null) return null;

        if (!string.Equals(record.ActionCode, actionCode, StringComparison.Ordinal))
        {
            throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict,
                $"La operación {operationId} ya fue registrada con otra acción ({record.ActionCode}); no se repite con efectos nuevos.");
        }

        var fingerprint = ComputeRequestFingerprint(actionCode, resourceId, payload);
        // Compatibilidad con registros de la primera entrega, cuyo hash incluía la clave del cliente.
        var legacyFingerprint = legacyHash ?? ComputeSha256(idempotencyKey + operationId);
        var matchesCurrentFingerprint = record.RequestHash == fingerprint;
        if (!matchesCurrentFingerprint && record.RequestHash != legacyFingerprint)
        {
            throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict,
                "La misma operación ya fue procesada con otro recurso o contenido.");
        }

        if (record.ResponseStatus != 200 || string.IsNullOrWhiteSpace(record.ResponseJson))
        {
            throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict,
                "La operación previa no tiene una respuesta aceptada.");
        }

        var response = JsonSerializer.Deserialize<T>(record.ResponseJson, JsonOptions)
            ?? throw new InvalidOperationException("No se pudo deserializar la respuesta idempotente previa.");

        // El hash legado solo cubre clave+UUID y no guardó el cuerpo: se valida el recurso de la
        // respuesta antes de aceptar el replay (límite documentado: no se revalida el cuerpo histórico).
        if (!matchesCurrentFingerprint && legacyResourceSelector is not null)
        {
            var storedResource = legacyResourceSelector(response);
            if (storedResource != resourceId)
            {
                throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict,
                    "El registro idempotente legado pertenece a otro recurso; no se repite con efectos nuevos.");
            }
        }

        return response;
    }

    private static string ComputeRequestFingerprint(string actionCode, Guid resourceId, object payload) =>
        ComputeSha256($"{actionCode}|{resourceId:N}|{JsonSerializer.Serialize(payload, JsonOptions)}");

    private Task<IDbContextTransaction?> BeginSerializedTransactionAsync(CancellationToken ct) =>
        OrganizationTransaction.BeginAsync(_db, ct);

    // Ubicación del vehículo y del punto de destino para eventos de custodia y movimientos.
    private async Task<(Guid? VehicleLocationId, Guid DestinationLocationId)> ResolveVehicleAndDestinationAsync(
        Delivery delivery,
        IReadOnlyList<HandlingUnit> handlingUnits,
        CancellationToken ct)
    {
        Guid? vehicleLocationId = null;
        var destinationLocationId = delivery.OriginLocationId;

        if (delivery.RouteStopId.HasValue)
        {
            var stop = await _db.RouteStops.FirstOrDefaultAsync(s => s.Id == delivery.RouteStopId.Value && s.OrganizationId == DemoOrganization.Id, ct);
            if (stop != null)
            {
                var plan = await _db.RoutePlans.FirstOrDefaultAsync(p => p.Id == stop.RoutePlanId && p.OrganizationId == DemoOrganization.Id, ct);
                if (plan != null)
                {
                    var vLoc = await _db.Locations.FirstOrDefaultAsync(l => l.VehicleId == plan.VehicleId && l.OrganizationId == DemoOrganization.Id, ct);
                    if (vLoc != null) vehicleLocationId = vLoc.Id;
                }

                var pLoc = await _db.Locations.FirstOrDefaultAsync(l => l.ServicePointId == stop.ServicePointId && l.OrganizationId == DemoOrganization.Id, ct);
                if (pLoc != null) destinationLocationId = pLoc.Id;
            }
        }

        if (vehicleLocationId is null)
        {
            vehicleLocationId = handlingUnits.Count > 0 ? handlingUnits[0].LocationId : delivery.OriginLocationId;
        }

        if (destinationLocationId == delivery.OriginLocationId)
        {
            var pLoc = await _db.Locations.FirstOrDefaultAsync(l => l.ServicePointId == delivery.ServicePointId && l.OrganizationId == DemoOrganization.Id, ct);
            if (pLoc != null) destinationLocationId = pLoc.Id;
        }

        return (vehicleLocationId, destinationLocationId);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // =========================================================================
    // Issue #247: Guía de Retorno de lo No Entregado y Prioridad al Día Siguiente
    // =========================================================================

    public async Task<ReturnGuideDto> GenerateReturnGuideAsync(
        Guid routePlanId,
        Guid actorAccountId,
        CancellationToken ct = default)
    {
        await using var transaction = await BeginSerializedTransactionAsync(ct);
        var plan = await _db.RoutePlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == routePlanId && p.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Plan de ruta {routePlanId} no encontrado.");
        await EnsureReturnGuideAccessAsync(plan.Id, actorAccountId, generating: true, ct);
        var guide = await BuildReturnGuideAsync(await ResolveReturnGuidePlanAsync(ReturnGuideNumber(plan.Id), ct), ct);
        var huIds = guide.Lines.Select(l => l.HandlingUnitId).Distinct().ToList();
        var units = await _db.HandlingUnits
            .Where(h => huIds.Contains(h.Id) && h.OrganizationId == DemoOrganization.Id && h.Status != HandlingUnitStates.Returned)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var unit in units)
        {
            unit.Status = HandlingUnitStates.NotDelivered;
            if (!await _db.CustodyEvents.AnyAsync(e => e.OrganizationId == DemoOrganization.Id &&
                e.HandlingUnitId == unit.Id && e.EventKind == CustodyEventKinds.NotDelivered, ct))
            {
                _db.CustodyEvents.Add(new CustodyEvent
                {
                    OrganizationId = DemoOrganization.Id,
                    HandlingUnitId = unit.Id,
                    DeliveryId = unit.DeliveryId,
                    ActorAccountId = actorAccountId,
                    EventKind = CustodyEventKinds.NotDelivered,
                    OperationId = Guid.NewGuid(),
                    ReceivedAtServer = now,
                    OccurredAtDevice = now
                });
            }
        }
        await _db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return guide;
    }

    private static string ReturnGuideNumber(Guid planId) => $"GRT-{planId.ToString()[..8].ToUpperInvariant()}";

    private async Task<RoutePlan> ResolveReturnGuidePlanAsync(string guideNumber, CancellationToken ct)
    {
        if (guideNumber is null || !Regex.IsMatch(guideNumber, @"\AGRT-[0-9A-F]{8}\z"))
            throw new FieldUnprocessableException(FieldErrorCodes.InvalidGuideNumber, "El número de guía debe tener el formato GRT-1234ABCD.");

        var prefix = guideNumber[4..].ToLowerInvariant();
        var plans = await _db.RoutePlans.AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.Id.ToString().StartsWith(prefix))
            .Take(2).ToListAsync(ct);
        if (plans.Count > 1)
            throw new FieldConflictException(FieldErrorCodes.AmbiguousGuideNumber, "El número de guía coincide con más de un plan de ruta.");
        return plans.SingleOrDefault()
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Guía de retorno {guideNumber} no encontrada.");
    }

    public async Task<ReturnGuideDto> GetReturnGuideAsync(string guideNumber, Guid actorAccountId, CancellationToken ct = default)
    {
        var plan = await ResolveReturnGuidePlanAsync(guideNumber, ct);
        await EnsureReturnGuideAccessAsync(plan.Id, actorAccountId, generating: false, ct);
        return await BuildReturnGuideAsync(plan, ct);
    }

    private async Task EnsureReturnGuideAccessAsync(Guid routePlanId, Guid actorAccountId, bool generating, CancellationToken ct)
    {
        var account = await _db.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == actorAccountId &&
            a.OrganizationId == DemoOrganization.Id && a.IsActive, ct);
        if (account is null)
            throw new FieldForbiddenException("GUIDE_ACCESS_DENIED", "No tiene acceso a esta guía.");
        if (account.Role == Roles.Conductor)
        {
            var assigned = await _db.Deliveries.AnyAsync(d => d.OrganizationId == DemoOrganization.Id &&
                d.AssignedAccountId == actorAccountId && _db.RouteStops.Any(s => s.OrganizationId == DemoOrganization.Id &&
                    s.RoutePlanId == routePlanId && s.Id == d.RouteStopId), ct);
            if (assigned) return;
        }
        else if (account.Role is Roles.Gerencia or Roles.Planificador or Roles.Bodega || (!generating && account.Role == Roles.Auditor))
            return;
        throw new FieldForbiddenException("GUIDE_ACCESS_DENIED", "No tiene acceso a esta guía.");
    }

    // Proyección compartida de solo lectura: consultar nunca genera eventos ni cambia custodia.
    private async Task<ReturnGuideDto> BuildReturnGuideAsync(RoutePlan plan, CancellationToken ct)
    {
        var routePlanId = plan.Id;
        var vehicle = await _db.Vehicles.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == plan.VehicleId && v.OrganizationId == DemoOrganization.Id, ct);
        var vehiclePlate = vehicle?.Code ?? "Vehículo Asignado";

        // Obtener entregas del plan
        var stops = await _db.RouteStops.AsNoTracking()
            .Where(s => s.RoutePlanId == routePlanId && s.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var stopIds = stops.Select(s => s.Id).ToList();

        var deliveries = await _db.Deliveries.AsNoTracking()
            .Where(d => d.RouteStopId.HasValue && stopIds.Contains(d.RouteStopId.Value) && d.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var deliveryIds = deliveries.Select(d => d.Id).ToList();

        var originLocationId = vehicle?.DepotLocationId ?? deliveries.FirstOrDefault()?.OriginLocationId ?? Guid.Empty;
        var warehouse = await _db.Locations.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == originLocationId && l.OrganizationId == DemoOrganization.Id, ct);
        var warehouseName = warehouse?.Name ?? "Bodega Principal";

        var assignedDriverId = deliveries.FirstOrDefault()?.AssignedAccountId ?? plan.PublishedByAccountId;
        var driver = assignedDriverId.HasValue
            ? await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignedDriverId.Value && a.OrganizationId == DemoOrganization.Id, ct)
            : null;
        var driverName = driver?.DisplayName ?? "Conductor de Ruta";

        var handlingUnits = await _db.HandlingUnits.AsNoTracking()
            .Where(hu => hu.DeliveryId.HasValue && deliveryIds.Contains(hu.DeliveryId.Value) && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var huIds = handlingUnits.Select(h => h.Id).ToList();

        var huLines = await _db.HandlingUnitLines.AsNoTracking()
            .Where(hul => huIds.Contains(hul.HandlingUnitId) && hul.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var deliveryLines = await _db.DeliveryLines.AsNoTracking()
            .Where(dl => deliveryIds.Contains(dl.DeliveryId) && dl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var dLineMap = deliveryLines.ToDictionary(dl => dl.Id, dl => dl);

        var lotIds = huLines.Select(hl => hl.LotId).Distinct().ToList();
        var lots = await _db.Lots.AsNoTracking()
            .Where(l => lotIds.Contains(l.Id) && l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, ct);

        var productIds = lots.Values.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, ct);

        var receipts = await _db.Receipts.AsNoTracking()
            .Where(r => deliveryIds.Contains(r.DeliveryId) && r.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var receiptIds = receipts.Select(r => r.Id).ToList();

        var receiptLines = await _db.ReceiptLines.AsNoTracking()
            .Where(rl => receiptIds.Contains(rl.ReceiptId) && rl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var returnLines = new List<ReturnGuideLineDto>();
        var deliveryById = deliveries.ToDictionary(d => d.Id);
        var stopById = stops.ToDictionary(s => s.Id);
        var receiptsByLine = receiptLines.GroupBy(r => r.DeliveryLineId).ToDictionary(g => g.Key, g => g.ToList());
        var rejectedRemaining = receiptsByLine.ToDictionary(g => g.Key, g => g.Value.Sum(r => r.QuantityRejected));

        // El recibo actual identifica la línea de entrega, no la caja rechazada. Distribuir su total
        // una sola vez entre cajas en orden estable; nunca repetir el rechazo en cada caja del mismo lote.
        foreach (var hu in handlingUnits.OrderBy(h => h.Code, StringComparer.Ordinal).ThenBy(h => h.Id))
        {
            if (hu.DeliveryId is not Guid deliveryId || !deliveryById.TryGetValue(deliveryId, out var delivery)) continue;
            var stop = delivery.RouteStopId is Guid stopId ? stopById.GetValueOrDefault(stopId) : null;
            foreach (var hl in huLines.Where(l => l.HandlingUnitId == hu.Id).OrderBy(l => l.Id))
            {
                var dl = hl.DeliveryLineId is Guid lineId ? dLineMap.GetValueOrDefault(lineId)
                    : deliveryLines.SingleOrDefault(l => l.DeliveryId == deliveryId && l.LotId == hl.LotId && l.HandlingUnitId == hu.Id);
                if (dl is null || dl.DeliveryId != deliveryId || dl.LotId != hl.LotId)
                    throw new FieldConflictException("RETURN_LINE_MISMATCH", "El bulto no tiene una línea de entrega y lote inequívocos.");
                decimal quantity;
                string reason;
                if (receiptsByLine.TryGetValue(dl.Id, out var rejected))
                {
                    quantity = Math.Min(hl.Quantity, rejectedRemaining[dl.Id]);
                    rejectedRemaining[dl.Id] -= quantity;
                    reason = rejected.FirstOrDefault(r => r.QuantityRejected > 0)?.RejectionReason ?? "No entregado en ruta";
                }
                else
                {
                    var inVehicle = hu.Status is HandlingUnitStates.NotDelivered or HandlingUnitStates.Returned
                        or HandlingUnitStates.Dispatched or HandlingUnitStates.InTransit;
                    quantity = inVehicle && dl.DispatchedQuantity > 0 ? hl.Quantity : 0m;
                    reason = "No entregado en ruta";
                }
                if (quantity <= 0) continue;
                if (!lots.TryGetValue(hl.LotId, out var lot) || !products.TryGetValue(lot.ProductId, out var product))
                    throw new FieldConflictException("RETURN_LINE_MISMATCH", "El lote del retorno no tiene un producto válido.");
                returnLines.Add(new ReturnGuideLineDto(hu.Id, hu.Code, product.Id, product.Sku, product.Name,
                    hl.LotId, lot.Code, quantity, product.BaseUnit, reason, delivery.Id, dl.Id,
                    delivery.DestinationLocationId.HasValue || stop?.LocationId.HasValue == true ? null : delivery.ServicePointId,
                    delivery.DestinationLocationId ?? stop?.LocationId,
                    receiptsByLine.ContainsKey(dl.Id) ? "Quarantine" : "Available"));
            }
        }
        if (rejectedRemaining.Any(p => p.Value > 0 &&
                huLines.Any(l => l.DeliveryLineId == p.Key)))
            throw new FieldConflictException("RETURN_LINE_MISMATCH", "La cantidad rechazada excede lo identificado en los bultos despachados.");

        var returnedIds = handlingUnits.Where(h => h.Status == HandlingUnitStates.Returned).Select(h => h.Id).ToHashSet();
        var status = returnLines.Count == 0 ? "Empty"
            : returnLines.All(l => returnedIds.Contains(l.HandlingUnitId)) ? "ReceivedAtWarehouse" : "PendingWarehouse";
        return new ReturnGuideDto(
            ReturnGuideNumber(plan.Id),
            plan.Id,
            vehiclePlate,
            driverName,
            originLocationId,
            warehouseName,
            DateTime.UtcNow,
            status,
            returnLines);
    }

    public async Task<ReceiveReturnGuideResponse> ReceiveReturnGuideAsync(
        ReceiveReturnGuideRequest request,
        Guid actorAccountId,
        CancellationToken ct = default)
    {
        await using var transaction = await BeginSerializedTransactionAsync(ct);
        const string actionCode = "ReceiveReturnGuide";
        var plan = await ResolveReturnGuidePlanAsync(request.GuideNumber, ct);
        var replay = await FindReplayAsync<ReceiveReturnGuideResponse>(actorAccountId, request.OperationId,
            actionCode, plan.Id, request, request.OperationId.ToString(), ct);
        if (replay is not null) return replay;

        var guide = await BuildReturnGuideAsync(plan, ct);
        if (guide.Lines.Count == 0)
            throw new FieldConflictException(FieldErrorCodes.NoHandlingUnits, "La guía de retorno no contiene bultos.");

        var huIds = guide.Lines.Select(l => l.HandlingUnitId).Distinct().ToList();
        var hus = await _db.HandlingUnits
            .Where(h => huIds.Contains(h.Id) && h.OrganizationId == DemoOrganization.Id && h.Status != HandlingUnitStates.Returned)
            .ToListAsync(ct);
        if (hus.Count == 0)
            throw new FieldConflictException(FieldErrorCodes.AlreadyReceived, "La guía de retorno ya fue recibida en bodega.");
        var pendingIds = hus.Select(h => h.Id).ToHashSet();
        var pendingLines = guide.Lines.Where(l => pendingIds.Contains(l.HandlingUnitId)).ToList();

        var warehouseLocationId = guide.DestinationWarehouseId;
        var now = DateTime.UtcNow;

        var warehouse = await _db.Locations.SingleOrDefaultAsync(l => l.OrganizationId == DemoOrganization.Id &&
            l.Id == warehouseLocationId && l.Kind == LocationKinds.Warehouse, ct)
            ?? throw new FieldConflictException("RETURN_WAREHOUSE_MISSING", "La guía no tiene una bodega de recepción válida.");
        var vehicleLocation = await _db.Locations.SingleOrDefaultAsync(l => l.VehicleId == plan.VehicleId &&
            l.Kind == LocationKinds.Vehicle && l.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldConflictException(FieldErrorCodes.InsufficientVehicleStock, "La ruta no tiene ubicación de vehículo.");
        var vehicleLocationId = vehicleLocation.Id;
        var vehicleBalances = await _db.StockBalances.Where(b => b.OrganizationId == DemoOrganization.Id &&
            b.LocationId == vehicleLocationId).ToDictionaryAsync(b => b.LotId, ct);
        foreach (var group in pendingLines.GroupBy(l => l.LotId))
        {
            if (!vehicleBalances.TryGetValue(group.Key, out var balance) ||
                balance.QuantityOnHand - balance.QuantityReserved < group.Sum(l => l.Quantity))
                throw new FieldConflictException(FieldErrorCodes.InsufficientVehicleStock,
                    "La cantidad de retorno excede el saldo disponible del vehículo; no se recibió ninguna línea.");
        }

        // Un bulto con rechazo queda en cuarentena hasta conciliación física. No se bloquea el lote
        // completo (que puede tener existencias sanas) ni se ofrece este saldo como stock de bodega.
        var quarantineUnitIds = pendingLines.Where(l => l.StockDisposition == "Quarantine")
            .Select(l => l.HandlingUnitId).ToHashSet();
        Location? quarantine = null;
        if (quarantineUnitIds.Count > 0)
        {
            var code = $"RET-Q-{warehouseLocationId:N}";
            quarantine = await _db.Locations.SingleOrDefaultAsync(l => l.OrganizationId == DemoOrganization.Id &&
                l.Kind == LocationKinds.Quarantine && l.Code == code, ct);
            if (quarantine is null)
            {
                quarantine = new Location { OrganizationId = DemoOrganization.Id, Code = code,
                    Kind = LocationKinds.Quarantine, Name = $"Retornos por conciliar — {warehouse.Name}", IsActive = true };
                _db.Locations.Add(quarantine);
            }
        }
        Guid Target(ReturnGuideLineDto line) => quarantineUnitIds.Contains(line.HandlingUnitId) ? quarantine!.Id : warehouseLocationId;
        foreach (var group in pendingLines.GroupBy(l => (l.LotId, LocationId: Target(l))))
        {
            var quantity = group.Sum(l => l.Quantity);
            vehicleBalances[group.Key.LotId].QuantityOnHand -= quantity;
            var balance = await _db.StockBalances.SingleOrDefaultAsync(b => b.OrganizationId == DemoOrganization.Id &&
                b.LocationId == group.Key.LocationId && b.LotId == group.Key.LotId, ct);
            if (balance is null)
            {
                balance = new StockBalance { OrganizationId = DemoOrganization.Id,
                    LocationId = group.Key.LocationId, LotId = group.Key.LotId };
                _db.StockBalances.Add(balance);
            }
            balance.QuantityOnHand += quantity;
            foreach (var line in group)
            {
                _db.InventoryMovements.Add(new InventoryMovement
                {
                    OrganizationId = DemoOrganization.Id, Kind = MovementKinds.Return,
                    LotId = line.LotId, FromLocationId = vehicleLocationId, ToLocationId = group.Key.LocationId,
                    Quantity = line.Quantity, ActorAccountId = actorAccountId, OperationId = request.OperationId,
                    DeliveryLineId = line.DeliveryLineId, OccurredAtUtc = request.OccurredAtDevice ?? now,
                    Reason = $"Guía {request.GuideNumber}; destino {line.DestinationServicePointId ?? line.DestinationLocationId}; motivo {line.Reason}"
                });
            }
        }

        // Actualizar solo los bultos pendientes y registrar su retorno.
        foreach (var hu in hus)
        {
            hu.Status = HandlingUnitStates.Returned;
            hu.LocationId = quarantineUnitIds.Contains(hu.Id) ? quarantine!.Id : warehouseLocationId;

            _db.CustodyEvents.Add(new CustodyEvent
            {
                OrganizationId = DemoOrganization.Id,
                HandlingUnitId = hu.Id,
                DeliveryId = hu.DeliveryId,
                FromLocationId = vehicleLocationId,
                ToLocationId = hu.LocationId,
                ActorAccountId = actorAccountId,
                EventKind = CustodyEventKinds.Returned,
                OperationId = request.OperationId,
                OccurredAtDevice = request.OccurredAtDevice,
                ReceivedAtServer = now
            });
        }

        // Una demanda por destino original, conservando motivos; la prioridad se decide para cada punto.
        var rebalanceOrderIds = new List<Guid>();
        foreach (var destination in pendingLines.GroupBy(l => (l.DestinationServicePointId, l.DestinationLocationId))
                     .OrderBy(g => g.Key.DestinationServicePointId).ThenBy(g => g.Key.DestinationLocationId))
        {
            if (destination.Key.DestinationServicePointId is null && destination.Key.DestinationLocationId is null)
                throw new FieldConflictException("RETURN_DESTINATION_MISSING", "Una línea de retorno no tiene destino original.");
            var critical = destination.Key.DestinationServicePointId is Guid pointId && await _db.ServicePoints.AnyAsync(p =>
                p.OrganizationId == DemoOrganization.Id && p.Id == pointId && p.Criticality == "Critical", ct);
            var order = new DeliveryOrder
            {
                OrganizationId = DemoOrganization.Id,
                OrderNumber = $"ORD-RET-{request.OperationId:N}-{rebalanceOrderIds.Count + 1:D3}",
                DestinationServicePointId = destination.Key.DestinationServicePointId,
                DestinationLocationId = destination.Key.DestinationLocationId,
                RequiredDateUtc = now.AddDays(1), Priority = critical ? "Critical" : "High",
                Status = DeliveryOrderStatus.Pending, SourceType = "ReturnGuide",
                Notes = $"Retorno {request.GuideNumber}; motivos: {string.Join(", ", destination.Select(l => l.Reason).Distinct())}"
            };
            _db.DeliveryOrders.Add(order);
            rebalanceOrderIds.Add(order.Id);
            foreach (var product in destination.GroupBy(l => l.ProductId))
                _db.DeliveryOrderLines.Add(new DeliveryOrderLine
                {
                    OrganizationId = DemoOrganization.Id, DeliveryOrderId = order.Id,
                    ProductId = product.Key, RequestedQuantity = product.Sum(l => l.Quantity)
                });
        }

        var response = new ReceiveReturnGuideResponse(
            request.GuideNumber,
            "ReceivedAtWarehouse",
            now,
            pendingLines.Count,
            rebalanceOrderIds[0],
            rebalanceOrderIds);
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            OrganizationId = DemoOrganization.Id,
            AccountId = actorAccountId,
            OperationId = request.OperationId,
            ActionCode = actionCode,
            RequestHash = ComputeRequestFingerprint(actionCode, plan.Id, request),
            ResponseStatus = 200,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
            CompletedAtUtc = now,
            RetainUntilUtc = now.AddDays(7)
        });
        await _db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return response;
    }
}
