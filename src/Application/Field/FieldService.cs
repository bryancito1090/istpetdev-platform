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
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IstpetDev.Application.Field;

public sealed class FieldService : IFieldService
{
    private readonly IApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<FieldService> _logger;

    public FieldService(
        IApplicationDbContext db,
        IConfiguration config,
        ILogger<FieldService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
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
        Guid vehicleLocationId = delivery.OriginLocationId;
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

            var rLine = new ReceiptLine
            {
                OrganizationId = DemoOrganization.Id,
                ReceiptId = receipt.Id,
                DeliveryLineId = dLine.Id,
                QuantityAccepted = reqLine.QuantityAccepted,
                QuantityRejected = reqLine.QuantityRejected,
                RejectionReason = reqLine.RejectionReason,
                RemainingQuantity = reqLine.RemainingQuantity,
                NextRequestedQuantity = reqLine.NextRequestedQuantity
            };
            _db.ReceiptLines.Add(rLine);

            // Mover inventario de lo aceptado vehículo -> punto de destino
            if (reqLine.QuantityAccepted > 0)
            {
                var vehicleBalance = await _db.StockBalances
                    .FirstOrDefaultAsync(sb => sb.LocationId == vehicleLocationId && sb.LotId == reqLine.LotId && sb.OrganizationId == DemoOrganization.Id, ct);

                if (vehicleBalance != null)
                {
                    vehicleBalance.QuantityOnHand = Math.Max(0m, vehicleBalance.QuantityOnHand - reqLine.QuantityAccepted);
                }

                var destBalance = await _db.StockBalances
                    .FirstOrDefaultAsync(sb => sb.LocationId == destinationLocationId && sb.LotId == reqLine.LotId && sb.OrganizationId == DemoOrganization.Id, ct);

                // Reconciliación con el stock en percha declarado ("me quedan X")
                if (reqLine.RemainingQuantity.HasValue)
                {
                    var reportedRemaining = Math.Max(0m, reqLine.RemainingQuantity.Value);
                    var previousOnHand = destBalance?.QuantityOnHand ?? 0m;

                    // Si tenía más de lo que queda, el hospital consumió la diferencia
                    if (previousOnHand > reportedRemaining && lots.TryGetValue(reqLine.LotId, out var lot))
                    {
                        var consumed = previousOnHand - reportedRemaining;
                        _db.Consumptions.Add(new Consumption
                        {
                            OrganizationId = DemoOrganization.Id,
                            ServicePointId = delivery.ServicePointId,
                            ProductId = lot.ProductId,
                            OccurredOn = DateOnly.FromDateTime(DateTime.UtcNow),
                            Quantity = consumed,
                            PeriodDays = 7,
                            SourceKind = "FieldReconciliation"
                        });
                    }

                    if (destBalance == null)
                    {
                        destBalance = new StockBalance
                        {
                            OrganizationId = DemoOrganization.Id,
                            LocationId = destinationLocationId,
                            LotId = reqLine.LotId,
                            QuantityOnHand = reportedRemaining + reqLine.QuantityAccepted,
                            QuantityReserved = 0m
                        };
                        _db.StockBalances.Add(destBalance);
                    }
                    else
                    {
                        destBalance.QuantityOnHand = reportedRemaining + reqLine.QuantityAccepted;
                    }
                }
                else
                {
                    if (destBalance == null)
                    {
                        destBalance = new StockBalance
                        {
                            OrganizationId = DemoOrganization.Id,
                            LocationId = destinationLocationId,
                            LotId = reqLine.LotId,
                            QuantityOnHand = reqLine.QuantityAccepted,
                            QuantityReserved = 0m
                        };
                        _db.StockBalances.Add(destBalance);
                    }
                    else
                    {
                        destBalance.QuantityOnHand += reqLine.QuantityAccepted;
                    }
                }

                var movement = new InventoryMovement
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
                };
                _db.InventoryMovements.Add(movement);
            }

            // Pedido futuro solicitado en campo ("necesito X más")
            if (reqLine.NextRequestedQuantity.HasValue && reqLine.NextRequestedQuantity.Value > 0 && lots.TryGetValue(reqLine.LotId, out var reqLot))
            {
                var deliveryOrder = new DeliveryOrder
                {
                    OrganizationId = DemoOrganization.Id,
                    OrderNumber = $"ORD-{delivery.Code}-{DateTime.UtcNow:MMddHHmm}",
                    DestinationServicePointId = delivery.ServicePointId,
                    RequiredDateUtc = DateTime.UtcNow.AddDays(2),
                    Priority = "Normal",
                    Status = DeliveryOrderStatus.Pending,
                    SourceType = "FieldRequest",
                    Notes = $"Solicitud en campo por receptor al conductor. Reposición requerida: {reqLine.NextRequestedQuantity.Value}"
                };
                _db.DeliveryOrders.Add(deliveryOrder);

                _db.DeliveryOrderLines.Add(new DeliveryOrderLine
                {
                    OrganizationId = DemoOrganization.Id,
                    DeliveryOrderId = deliveryOrder.Id,
                    ProductId = reqLot.ProductId,
                    RequestedQuantity = reqLine.NextRequestedQuantity.Value,
                    PlannedQuantity = 0m,
                    DeliveredQuantity = 0m
                });
            }

            responseLines.Add(new ReceiptLineDto(
                reqLine.DeliveryLineId,
                reqLine.LotId,
                reqLine.QuantityAccepted,
                reqLine.QuantityRejected,
                reqLine.Unit,
                reqLine.RemainingQuantity,
                reqLine.NextRequestedQuantity));
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

        var evidence = new Evidence
        {
            OrganizationId = DemoOrganization.Id,
            ReceiptId = receipt.Id,
            Kind = request.Kind,
            StorageKey = $"evidences/{receipt.Id}/{request.Kind}-{request.OperationId}.bin",
            MimeType = request.MimeType,
            ByteSize = request.ByteSize,
            Sha256 = request.Sha256,
            Status = "Pending",
            UploadedByAccountId = actorAccountId
        };

        _db.Evidences.Add(evidence);
        await _db.SaveChangesAsync(ct);

        var uploadUrl = $"https://s3.istpetdev.local/evidences/{evidence.Id}";
        return new EvidenceUploadResponse(
            evidence.Id,
            uploadUrl,
            new Dictionary<string, string> { { "Content-Type", request.MimeType } },
            DateTime.UtcNow.AddMinutes(15));
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

        evidence.Status = "Confirmed";
        evidence.ConfirmedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new EvidenceConfirmResponse(
            evidence.Id,
            "Confirmed",
            evidence.ConfirmedAtUtc.Value);
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
