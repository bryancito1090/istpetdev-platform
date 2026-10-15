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
    decimal? RemainingQuantity = null,
    decimal? NextRequestedQuantity = null);

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

public sealed record MobileAssignmentRoute(
    Guid RoutePlanId,
    int Revision,
    DateTime DownloadedAtUtc,
    string? DriverMessage,
    string VehicleCode,
    int StopsCount,
    IReadOnlyList<MobileAssignmentDelivery> Deliveries);

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
public sealed record ReceiverUnitLineDto(Guid DeliveryLineId, Guid LotId, string LotCode, string Sku, string ProductName, decimal Quantity, string Unit);

public sealed record ReceiverUnitResponse(
    Guid HandlingUnitId, string Code, string Status, Guid DeliveryId, long DeliveryVersion,
    string DestinationName, IReadOnlyList<ReceiverUnitLineDto> Lines);

