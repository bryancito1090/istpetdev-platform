using System;
using System.Collections.Generic;

namespace IstpetDev.Application.Field.Dtos;

public static class FieldRoles
{
    public const string Bodega = "Bodega";
    public const string Conductor = "Conductor";
    public const string Planificador = "Planificador";
    public const string Gerencia = "Gerencia";
}

public static class FieldErrorCodes
{
    public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
    public const string AssignmentChanged = "ASSIGNMENT_CHANGED";
    public const string QuantityExceeded = "QUANTITY_EXCEEDED";
    public const string UnauthorizedRole = "UNAUTHORIZED_ROLE";
    public const string UnauthorizedAssignment = "UNAUTHORIZED_ASSIGNMENT";
    public const string UnitNotFound = "UNIT_NOT_FOUND";
    public const string TokenRevokedOrExpired = "TOKEN_REVOKED_OR_EXPIRED";

    // #162: transiciones de custodia fuera de orden.
    public const string NotDispatched = "NOT_DISPATCHED";
    public const string NotDelivered = "NOT_DELIVERED";
    public const string AlreadyReceived = "ALREADY_RECEIVED";
    public const string InvalidStateTransition = "INVALID_STATE_TRANSITION";
    public const string HandlingUnitStateConflict = "HANDLING_UNIT_STATE_CONFLICT";
    public const string NoHandlingUnits = "NO_HANDLING_UNITS";
    public const string ReceiptRequiresReceiver = "RECEIPT_REQUIRES_RECEIVER";
    public const string IncompleteHandlingUnits = "INCOMPLETE_HANDLING_UNITS";
    public const string InsufficientVehicleStock = "INSUFFICIENT_VEHICLE_STOCK";
    public const string ReceiptCoverageMismatch = "RECEIPT_COVERAGE_MISMATCH";
    public const string InvalidOperationId = "INVALID_OPERATION_ID";
}

// ==========================================
// T2: Handling Unit & QR
// ==========================================

public sealed record CreateHandlingUnitLineRequest(
    Guid DeliveryLineId,
    Guid LotId,
    decimal Quantity);

public sealed record CreateHandlingUnitRequest(
    Guid OperationId,
    IReadOnlyList<CreateHandlingUnitLineRequest> Lines);

public sealed record HandlingUnitLineDto(
    Guid DeliveryLineId,
    Guid LotId,
    string Sku,
    decimal Quantity,
    string Unit);

public sealed record HandlingUnitResponse(
    Guid HandlingUnitId,
    string Code,
    Guid DeliveryId,
    string Status,
    string LabelUrl,
    string? Token,
    IReadOnlyList<HandlingUnitLineDto> Lines,
    DateTime CreatedAtUtc);

// ==========================================
// T3: Dispatch
// ==========================================

public sealed record DispatchRequest(
    Guid OperationId,
    IReadOnlyList<Guid> HandlingUnitIds,
    DateTime? OccurredAtDevice = null);

public sealed record DispatchResponse(
    Guid DeliveryId,
    string Status,
    IReadOnlyList<Guid> DispatchedHandlingUnits,
    DateTime DispatchedAtUtc,
    Guid CustodyEventId);

// ==========================================
// #162: Custodia del conductor (recogida y entrega física)
// ==========================================

public sealed record CustodyTransitionRequest(
    Guid OperationId,
    DateTime? OccurredAtDevice = null);

public sealed record CustodyTransitionResponse(
    Guid DeliveryId,
    string Status,
    IReadOnlyList<Guid> HandlingUnitIds,
    DateTime ChangedAtUtc,
    Guid CustodyEventId);

// ==========================================
// T4 & T5: Receipts
// ==========================================

public sealed record ReceiptLineRequest(
    Guid DeliveryLineId,
    Guid LotId,
    decimal QuantityAccepted,
    decimal QuantityRejected,
    string Unit,
    string? RejectionReason = null,
    decimal? RemainingQuantity = null,
    decimal? NextRequestedQuantity = null);

public sealed record CreateReceiptRequest(
    Guid OperationId,
    long ExpectedDeliveryVersion,
    DateTime OccurredAtDevice,
    IReadOnlyList<ReceiptLineRequest> Lines);

public sealed record ReceiptLineDto(
    Guid DeliveryLineId,
    Guid LotId,
    decimal QuantityAccepted,
    decimal QuantityRejected,
    string Unit,
    string? RejectionReason = null,
    decimal? RemainingQuantity = null,
    decimal? NextRequestedQuantity = null);

public sealed record RejectionReasonDto(string Code, string Description);

public sealed record RejectionReasonsCatalogResponse(string Version, IReadOnlyList<RejectionReasonDto> Reasons);

public sealed record ReceiptResponse(
    Guid ReceiptId,
    Guid DeliveryId,
    DateTime AcceptedAtServer,
    long DeliveryVersion,
    string DeliveryStatus,
    string EvidenceStatus,
    IReadOnlyList<ReceiptLineDto> Lines);

// ==========================================
// T6: Evidences
// ==========================================

public sealed record EvidenceUploadRequest(
    Guid OperationId,
    string Kind,
    string MimeType,
    long ByteSize,
    string Sha256);

public sealed record EvidenceUploadResponse(
    Guid EvidenceId,
    string UploadUrl,
    IReadOnlyDictionary<string, string>? RequiredHeaders,
    DateTime ExpiresAtUtc);

public sealed record EvidenceConfirmRequest(
    Guid OperationId,
    string Sha256,
    long ByteSize);

public sealed record EvidenceConfirmResponse(
    Guid EvidenceId,
    string Status,
    DateTime ConfirmedAtUtc);

// ==========================================
// T8: Mobile Assignments
// ==========================================

public sealed record MobileAssignmentDelivery(
    Guid DeliveryId,
    int Sequence,
    string PointCode,
    string PointName,
    string Address,
    DateTime DueAtUtc,
    string Status,
    IReadOnlyList<HandlingUnitResponse> HandlingUnits);

public sealed record MobileAssignmentPickupStop(
    Guid StopId,
    int Sequence,
    Guid? LocationId,
    string PointCode,
    string PointName,
    string Address,
    DateTime DueAtUtc);

public sealed record MobileAssignmentRoute(
    Guid RoutePlanId,
    int Revision,
    DateTime DownloadedAtUtc,
    string? DriverMessage,
    string VehicleCode,
    int StopsCount,
    IReadOnlyList<MobileAssignmentDelivery> Deliveries,
    string? OriginWarehouseName = null,
    IReadOnlyList<MobileAssignmentPickupStop>? PickupStops = null);

public sealed record MobileAssignmentResponse(
    DateTime ServerTimeUtc,
    Guid DriverAccountId,
    IReadOnlyList<MobileAssignmentRoute> Routes);

// ==========================================
// FR-19: Deliveries Paginated List
// ==========================================

public sealed record DeliveryListItem(
    Guid DeliveryId,
    Guid RoutePlanId,
    string PointCode,
    string PointName,
    DateTime DueAtUtc,
    string Status,
    string EvidenceStatus,
    int PackagesCount);

public sealed record DeliveryListResponse(
    IReadOnlyList<DeliveryListItem> Items,
    string? NextCursor,
    int? Total);

// ==========================================
// Spec 004 / Issue #121: Delivery Detail
// ==========================================

public sealed record DeliveryDetailLineDto(
    Guid DeliveryLineId,
    Guid LotId,
    string LotCode,
    string Sku,
    string ProductName,
    decimal PlannedQuantity,
    string Unit);

public sealed record DeliveryDetailHandlingUnitDto(
    Guid HandlingUnitId,
    string Code,
    string Status);

public sealed record DeliveryDetailResponse(
    Guid DeliveryId,
    string Status,
    long Version,
    string PointName,
    Guid RoutePlanId,
    IReadOnlyList<DeliveryDetailLineDto> Lines,
    IReadOnlyList<DeliveryDetailHandlingUnitDto> HandlingUnits,
    // #160: la Bodega ve a qué vehículo y conductor va la entrega y desde qué bodega sale.
    string? VehicleCode = null,
    string? DriverName = null,
    string? OriginWarehouseName = null,
    // #163: cantidad de evidencias fotográficas/firma confirmadas en S3.
    int ConfirmedEvidenceCount = 0);

// ==========================================
// FR-16: Demo Advance
// ==========================================

public sealed record DemoAdvanceRequest(
    Guid OperationId,
    int Stops);

public sealed record CompletedStopDto(
    int Sequence,
    Guid DeliveryId,
    Guid ReceiptId,
    DateTime AcceptedAtUtc);

public sealed record DemoAdvanceResponse(
    IReadOnlyList<CompletedStopDto> CompletedStops);

// ==========================================
// T7: Public & Operative Trace
// ==========================================

public sealed record PublicTraceProductUnit(
    string Sku,
    string Unit);

public sealed record PublicTraceEvent(
    string Stage,
    DateTime Timestamp);

public sealed record PublicTraceUnitDto(
    Guid UnitId,
    string Code,
    string Status,
    IReadOnlyList<PublicTraceProductUnit> ProductUnits,
    IReadOnlyList<PublicTraceEvent> Events);

public sealed record HandlingUnitTraceEvent(
    Guid EventId,
    string EventKind,
    Guid ActorAccountId,
    string? FromLocation,
    string? ToLocation,
    DateTime Timestamp);

public sealed record HandlingUnitTraceResponse(
    Guid HandlingUnitId,
    string Code,
    string Status,
    Guid? DeliveryId,
    IReadOnlyList<HandlingUnitLineDto> Lines,
    IReadOnlyList<HandlingUnitTraceEvent> Events);

// 014: lo que ve el Receptor al escanear el QR del bulto.
// #162: el recibo es por ENTREGA completa; la respuesta muestra todos los bultos y todas las
// líneas despachadas para que el receptor conozca el alcance antes de confirmar.
public sealed record ReceiverUnitBoxDto(Guid HandlingUnitId, string Code, string Status);

public sealed record ReceiverUnitLineDto(Guid DeliveryLineId, Guid LotId, string LotCode, string Sku, string ProductName, decimal Quantity, string Unit);

public sealed record ReceiverUnitResponse(
    Guid HandlingUnitId, string Code, string Status, Guid DeliveryId, long DeliveryVersion,
    string DestinationName, IReadOnlyList<ReceiverUnitBoxDto> Boxes, IReadOnlyList<ReceiverUnitLineDto> Lines);


// ==========================================
// Issue #116: Recepciones con novedades para la web (rechazos y ajustes de stock en percha)
// ==========================================

public sealed record ReceiptExceptionLineDto(
    Guid ReceiptLineId,
    string Sku,
    string ProductName,
    string LotCode,
    decimal QuantityAccepted,
    decimal QuantityRejected,
    string? RejectionReason,
    decimal? RemainingDeclared,
    decimal? AdjustmentQuantity, // Con signo: negativo = consumo derivado; positivo = conteo mayor que el saldo
    string? AdjustmentReason);

// La evidencia se registra como metadato; el archivo todavía no se almacena (la URL de subida es un stub).
public sealed record ReceiptEvidenceDto(
    Guid EvidenceId,
    string Kind,
    string Status,
    string MimeType,
    long? ByteSize,
    string? Sha256,
    DateTime? ConfirmedAtUtc);

public sealed record ReceiptExceptionDto(
    Guid ReceiptId,
    Guid DeliveryId,
    string DeliveryCode,
    Guid ServicePointId,
    string ServicePointCode,
    string ServicePointName,
    string ActorName,
    DateTime ReceivedAtServer,
    DateTime? OccurredAtDevice,
    bool HasRejections,
    bool HasAdjustments,
    IReadOnlyList<ReceiptExceptionLineDto> Lines,
    IReadOnlyList<ReceiptEvidenceDto> Evidence);

public sealed record ReceiptExceptionsResponse(IReadOnlyList<ReceiptExceptionDto> Items, int Total);

// ==========================================
// Issue #247: Guía de Retorno de lo No Entregado
// ==========================================

public sealed record ReturnGuideLineDto(
    Guid HandlingUnitId,
    string HandlingUnitCode,
    Guid ProductId,
    string Sku,
    string ProductName,
    Guid LotId,
    string LotCode,
    decimal Quantity,
    string Unit,
    string Reason);

public sealed record ReturnGuideDto(
    string GuideNumber,
    Guid RoutePlanId,
    string VehiclePlate,
    string DriverName,
    Guid DestinationWarehouseId,
    string DestinationWarehouseName,
    DateTime GeneratedAtUtc,
    string Status,
    IReadOnlyList<ReturnGuideLineDto> Lines);

public sealed record GenerateReturnGuideRequest(
    Guid RoutePlanId,
    string? Notes = null);

public sealed record ReceiveReturnGuideRequest(
    string GuideNumber,
    Guid OperationId,
    DateTime? OccurredAtDevice = null);

public sealed record ReceiveReturnGuideResponse(
    string GuideNumber,
    string Status,
    DateTime ReceivedAtUtc,
    int ItemsReconciled,
    Guid RebalanceOrderId);

