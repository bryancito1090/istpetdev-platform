using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Domain.Enums;
using Microsoft.EntityFrameworkCore;
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
        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Entrega {deliveryId} no encontrada.");

        if (delivery.Status == "Dispatched" || delivery.Status == "Delivered")
        {
            throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict, "La entrega ya fue despachada o recibida.");
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
            Status = "Prepared",
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
        // 1. Idempotencia
        var existingRecord = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.OrganizationId == DemoOrganization.Id &&
                                      r.AccountId == actorAccountId &&
                                      r.OperationId == request.OperationId &&
                                      r.ActionCode == "Dispatch", ct);

        if (existingRecord != null)
        {
            if (existingRecord.ResponseStatus == 200 && !string.IsNullOrWhiteSpace(existingRecord.ResponseJson))
            {
                return JsonSerializer.Deserialize<DispatchResponse>(existingRecord.ResponseJson, JsonOptions)
                    ?? throw new InvalidOperationException("No se pudo deserializar respuesta previa.");
            }
            throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict, "Operación de despacho ya en curso o duplicada con conflicto.");
        }

        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Entrega {deliveryId} no encontrada.");

        if (delivery.Status == "Dispatched" || delivery.Status == "Delivered")
        {
            throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict, "La entrega ya se encuentra despachada.");
        }

        var handlingUnits = await _db.HandlingUnits
            .Where(hu => request.HandlingUnitIds.Contains(hu.Id) && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        if (handlingUnits.Count != request.HandlingUnitIds.Count)
        {
            throw new FieldUnprocessableException(FieldErrorCodes.UnitNotFound, "Una o más cajas solicitadas no existen.");
        }

        foreach (var hu in handlingUnits)
        {
            if (hu.DeliveryId != delivery.Id)
            {
                throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict, $"La caja {hu.Code} no pertenece a esta entrega.");
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

        // Consumir reservas y mover stock de bodega a vehículo
        var huLines = await _db.HandlingUnitLines
            .Where(hul => request.HandlingUnitIds.Contains(hul.HandlingUnitId) && hul.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var reservations = await _db.RouteStockReservations
            .Where(r => r.DeliveryId == delivery.Id && r.OrganizationId == DemoOrganization.Id && r.Status == RouteStockReservationStatus.Active)
            .ToListAsync(ct);

        var deliveryLines = await _db.DeliveryLines
            .Where(dl => dl.DeliveryId == delivery.Id && dl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var dLinesById = deliveryLines.ToDictionary(dl => dl.Id, dl => dl);

        foreach (var huLine in huLines)
        {
            if (huLine.DeliveryLineId.HasValue && dLinesById.TryGetValue(huLine.DeliveryLineId.Value, out var dLine))
            {
                dLine.DispatchedQuantity += huLine.Quantity;

                var res = reservations.FirstOrDefault(r => r.DeliveryLineId == dLine.Id && r.LotId == huLine.LotId && r.Status == RouteStockReservationStatus.Active);
                if (res != null)
                {
                    res.Status = RouteStockReservationStatus.Consumed;
                    res.ClosedAtUtc = DateTime.UtcNow;

                    // Descontar en StockBalance de bodega
                    var depotBalance = await _db.StockBalances
                        .FirstOrDefaultAsync(sb => sb.LocationId == delivery.OriginLocationId && sb.LotId == huLine.LotId && sb.OrganizationId == DemoOrganization.Id, ct);

                    if (depotBalance != null)
                    {
                        depotBalance.QuantityReserved = Math.Max(0m, depotBalance.QuantityReserved - huLine.Quantity);
                        depotBalance.QuantityOnHand = Math.Max(0m, depotBalance.QuantityOnHand - huLine.Quantity);
                    }

                    // Acreditar en StockBalance del vehículo
                    var vehicleBalance = await _db.StockBalances
                        .FirstOrDefaultAsync(sb => sb.LocationId == vehicleLocationId && sb.LotId == huLine.LotId && sb.OrganizationId == DemoOrganization.Id, ct);

                    if (vehicleBalance == null)
                    {
                        vehicleBalance = new StockBalance
                        {
                            OrganizationId = DemoOrganization.Id,
                            LocationId = vehicleLocationId,
                            LotId = huLine.LotId,
                            QuantityOnHand = huLine.Quantity,
                            QuantityReserved = 0m
                        };
                        _db.StockBalances.Add(vehicleBalance);
                    }
                    else
                    {
                        vehicleBalance.QuantityOnHand += huLine.Quantity;
                    }

                    // Registrar movimiento de inventario bodega -> vehículo
                    var movement = new InventoryMovement
                    {
                        OrganizationId = DemoOrganization.Id,
                        Kind = "Dispatch",
                        LotId = huLine.LotId,
                        FromLocationId = delivery.OriginLocationId,
                        ToLocationId = vehicleLocationId,
                        Quantity = huLine.Quantity,
                        ActorAccountId = actorAccountId,
                        OperationId = request.OperationId,
                        OccurredAtUtc = request.OccurredAtDevice ?? DateTime.UtcNow,
                        DeliveryLineId = dLine.Id,
                        Reason = "Despacho de entrega y transferencia de custodia"
                    };
                    _db.InventoryMovements.Add(movement);
                }
            }
        }

        Guid firstCustodyEventId = Guid.Empty;
        var now = DateTime.UtcNow;

        foreach (var hu in handlingUnits)
        {
            hu.Status = "Dispatched";

            var custodyEvent = new CustodyEvent
            {
                OrganizationId = DemoOrganization.Id,
                HandlingUnitId = hu.Id,
                DeliveryId = delivery.Id,
                FromLocationId = delivery.OriginLocationId,
                ToLocationId = vehicleLocationId,
                ActorAccountId = actorAccountId,
                EventKind = "Dispatched",
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

        delivery.Status = "Dispatched";
        delivery.DispatchedAtUtc = now;

        var response = new DispatchResponse(
            delivery.Id,
            "Dispatched",
            handlingUnits.Select(h => h.Id).ToList(),
            now,
            firstCustodyEventId != Guid.Empty ? firstCustodyEventId : Guid.NewGuid());

        var record = new IdempotencyRecord
        {
            OrganizationId = DemoOrganization.Id,
            AccountId = actorAccountId,
            OperationId = request.OperationId,
            ActionCode = "Dispatch",
            RequestHash = ComputeSha256(idempotencyKey + request.OperationId),
            ResponseStatus = 200,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
            CompletedAtUtc = now,
            RetainUntilUtc = now.AddDays(7)
        };
        _db.IdempotencyRecords.Add(record);

        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<ReceiptResponse> CreateReceiptAsync(
        Guid deliveryId,
        CreateReceiptRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        return await CreateReceiptCoreAsync(deliveryId, request, actorAccountId, idempotencyKey, requireAssignedDriver: true, ct);
    }

    private async Task<ReceiptResponse> CreateReceiptCoreAsync(
        Guid deliveryId,
        CreateReceiptRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        bool requireAssignedDriver,
        CancellationToken ct)
    {
        // 1. Idempotencia
        var existingRecord = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.OrganizationId == DemoOrganization.Id &&
                                      r.AccountId == actorAccountId &&
                                      r.OperationId == request.OperationId &&
                                      r.ActionCode == "Receipt", ct);

        if (existingRecord != null)
        {
            if (existingRecord.ResponseStatus == 200 && !string.IsNullOrWhiteSpace(existingRecord.ResponseJson))
            {
                return JsonSerializer.Deserialize<ReceiptResponse>(existingRecord.ResponseJson, JsonOptions)
                    ?? throw new InvalidOperationException("No se pudo deserializar recibo previo.");
            }
            throw new FieldConflictException(FieldErrorCodes.IdempotencyConflict, "Recepción ya procesada con conflicto.");
        }

        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == deliveryId && d.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Entrega {deliveryId} no encontrada.");

        // 2. Autorización por asignación de conductor
        if (requireAssignedDriver && delivery.AssignedAccountId.HasValue && delivery.AssignedAccountId.Value != actorAccountId)
        {
            throw new FieldForbiddenException(FieldErrorCodes.UnauthorizedAssignment, "El conductor no está asignado a esta entrega.");
        }

        // 3. Validación de versión esperada
        if (request.ExpectedDeliveryVersion > 0 && delivery.Version != request.ExpectedDeliveryVersion)
        {
            throw new FieldConflictException(FieldErrorCodes.AssignmentChanged,
                $"Conflicto de versión de entrega (servidor: {delivery.Version}, esperado: {request.ExpectedDeliveryVersion}).");
        }

        var deliveryLines = await _db.DeliveryLines
            .Where(dl => dl.DeliveryId == delivery.Id && dl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var dLinesById = deliveryLines.ToDictionary(dl => dl.Id, dl => dl);

        // Ubicaciones para movimiento
        Guid? vehicleLocationId = delivery.OriginLocationId;
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

            var huLocId = await _db.HandlingUnits
                .Where(hu => hu.DeliveryId == delivery.Id && hu.OrganizationId == DemoOrganization.Id)
                .Select(hu => (Guid?)hu.LocationId)
                .FirstOrDefaultAsync(ct);
            if (huLocId.HasValue) vehicleLocationId = huLocId.Value;
        }

        if (vehicleLocationId == destinationLocationId)
        {
            vehicleLocationId = null;
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

        // 4. Validar líneas y registrar lo recibido
        var receiptLines = new List<(ReceiptLineRequest Request, DeliveryLine DeliveryLine, ReceiptLine ReceiptLine)>();
        foreach (var reqLine in request.Lines)
        {
            if (!dLinesById.TryGetValue(reqLine.DeliveryLineId, out var dLine))
            {
                throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded, $"Línea de entrega {reqLine.DeliveryLineId} no existe.");
            }

            var totalReported = reqLine.QuantityAccepted + reqLine.QuantityRejected;
            var maxAllowed = dLine.DispatchedQuantity > 0 ? dLine.DispatchedQuantity : dLine.PlannedQuantity;

            if (dLine.AcceptedQuantity + dLine.RejectedQuantity + totalReported > maxAllowed)
            {
                throw new FieldConflictException(FieldErrorCodes.QuantityExceeded,
                    $"Cantidad total ({totalReported}) excede lo disponible por recibir ({maxAllowed - dLine.AcceptedQuantity - dLine.RejectedQuantity}).");
            }

            dLine.AcceptedQuantity += reqLine.QuantityAccepted;
            dLine.RejectedQuantity += reqLine.QuantityRejected;

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

        // 6. Mover inventario de lo aceptado vehículo -> punto de destino
        foreach (var (reqLine, dLine, rLine) in receiptLines)
        {
            if (reqLine.QuantityAccepted <= 0m) continue;

            if (vehicleLocationId.HasValue)
            {
                var vehicleBalance = await FindBalanceAsync(vehicleLocationId.Value, reqLine.LotId);
                if (vehicleBalance != null)
                {
                    vehicleBalance.QuantityOnHand = Math.Max(0m, vehicleBalance.QuantityOnHand - reqLine.QuantityAccepted);
                }
            }

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

        var handlingUnits = await _db.HandlingUnits
            .Where(hu => hu.DeliveryId == delivery.Id && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var finalStatus = hasRejections ? "Partial" : "Delivered";
        delivery.Status = finalStatus;

        foreach (var hu in handlingUnits)
        {
            hu.Status = hasRejections ? "Partial" : "Received";

            var custodyEvent = new CustodyEvent
            {
                OrganizationId = DemoOrganization.Id,
                HandlingUnitId = hu.Id,
                DeliveryId = delivery.Id,
                ReceiptId = receipt.Id,
                FromLocationId = vehicleLocationId,
                ToLocationId = destinationLocationId,
                ActorAccountId = actorAccountId,
                EventKind = hasRejections ? "Partial" : "Received",
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
            RequestHash = ComputeSha256(idempotencyKey + request.OperationId),
            ResponseStatus = 200,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
            CompletedAtUtc = DateTime.UtcNow,
            RetainUntilUtc = DateTime.UtcNow.AddDays(7)
        };
        _db.IdempotencyRecords.Add(record);

        await _db.SaveChangesAsync(ct);
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

        var stopsById = stops.ToDictionary(s => s.Id, s => s);

        var planIds = stops.Select(s => s.RoutePlanId).Distinct().ToList();
        var plans = await _db.RoutePlans
            .Where(p => planIds.Contains(p.Id) && p.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var points = await _db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(sp => sp.Id, sp => sp, ct);

        var vehicles = await _db.Vehicles
            .Where(v => v.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(v => v.Id, v => v, ct);

        var handlingUnits = await _db.HandlingUnits
            .Where(hu => deliveries.Select(d => d.Id).Contains(hu.DeliveryId ?? Guid.Empty) && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var huByDelivery = handlingUnits.GroupBy(hu => hu.DeliveryId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var routes = new List<MobileAssignmentRoute>();

        foreach (var plan in plans)
        {
            var planStops = stops.Where(s => s.RoutePlanId == plan.Id).OrderBy(s => s.SequenceNumber).ToList();
            var planDeliveries = new List<MobileAssignmentDelivery>();

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
                        Array.Empty<HandlingUnitLineDto>(),
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
                vehicle?.Code ?? "VEH-01",
                planStops.Count,
                planDeliveries));
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

        return new DeliveryDetailResponse(
            delivery.Id,
            delivery.Status,
            delivery.Version,
            servicePoint?.Name ?? "Punto de Entrega",
            routePlanId,
            lineDtos,
            handlingUnits);
    }

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
                "Dispatched" => "Despachada",
                "Received" => "Recibida",
                "Partial" => "Recibida Parcial",
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
        var deliveryLines = await _db.DeliveryLines.Where(l => l.DeliveryId == deliveryId).ToListAsync(ct);
        var unitLines = await _db.HandlingUnitLines.Where(l => l.HandlingUnitId == hu.Id).ToListAsync(ct);
        var lotIds = unitLines.Select(l => l.LotId).ToList();
        var lots = await _db.Lots.Where(l => lotIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
        var productIds = lots.Values.Select(l => l.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var lines = unitLines.Select(l =>
        {
            // La línea de la caja puede no traer DeliveryLineId: se resuelve por lote dentro de la entrega.
            var deliveryLineId = l.DeliveryLineId ?? deliveryLines.First(dl => dl.LotId == l.LotId).Id;
            var lot = lots[l.LotId];
            var product = products[lot.ProductId];
            return new ReceiverUnitLineDto(deliveryLineId, lot.Id, lot.Code, product.Sku, product.Name, l.Quantity, product.BaseUnit);
        }).ToList();

        return new ReceiverUnitResponse(hu.Id, hu.Code, hu.Status, deliveryId, delivery.Version, point?.Name ?? "", lines);
    }

    public async Task<ReceiptResponse> CreateReceiverReceiptAsync(Guid unitId, string? traceToken, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default)
    {
        var hu = await RequireUnitWithTokenAsync(unitId, traceToken, ct);
        if (hu.DeliveryId is not Guid deliveryId) throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Unit not found.");
        foreach (var line in request.Lines.Where(l => l.QuantityRejected > 0))
            if (!ReceiptRejectionReasons.IsValid(line.RejectionReason))
                throw new FieldUnprocessableException("INVALID_REJECTION_REASON", "Selecciona un motivo del catálogo V1.");
        // El QR prueba que el Receptor tiene el bulto en la mano; no hay conductor asignado que validar.
        return await CreateReceiptCoreAsync(deliveryId, request, actorAccountId, idempotencyKey, requireAssignedDriver: false, ct);
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

                // 1. Preparar HandlingUnit si no existe
                var existingHu = await _db.HandlingUnits
                    .FirstOrDefaultAsync(hu => hu.DeliveryId == del.Id && hu.OrganizationId == DemoOrganization.Id, ct);

                Guid huId;
                if (existingHu == null)
                {
                    var prepReq = new CreateHandlingUnitRequest(
                        Guid.NewGuid(),
                        delLines.Select(dl => new CreateHandlingUnitLineRequest(dl.Id, dl.LotId, dl.PlannedQuantity)).ToList());

                    var huResp = await CreateHandlingUnitAsync(del.Id, prepReq, actorAccountId, ct);
                    huId = huResp.HandlingUnitId;
                }
                else
                {
                    huId = existingHu.Id;
                }

                // 2. Despachar si no está despachada
                if (del.Status != "Dispatched" && del.Status != "Delivered")
                {
                    var dispReq = new DispatchRequest(Guid.NewGuid(), new[] { huId }, occurredAt);
                    await DispatchAsync(del.Id, dispReq, actorAccountId, $"adv-{Guid.NewGuid()}", ct);
                }

                // 3. Recibir 100% aceptado
                var receiptReq = new CreateReceiptRequest(
                    Guid.NewGuid(),
                    del.Version,
                    occurredAt,
                    delLines.Select(dl => new ReceiptLineRequest(dl.Id, dl.LotId, dl.PlannedQuantity, 0m, "U")).ToList());

                var receiptResp = await CreateReceiptAsync(del.Id, receiptReq, actorAccountId, $"rec-{Guid.NewGuid()}", ct);

                completedStops.Add(new CompletedStopDto(
                    stop.SequenceNumber,
                    del.Id,
                    receiptResp.ReceiptId,
                    occurredAt));
            }
        }

        return new DemoAdvanceResponse(completedStops);
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
}
