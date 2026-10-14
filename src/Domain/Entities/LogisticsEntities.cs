using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class RoutePlan : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public int Revision { get; set; }
    public Guid? SupersedesRoutePlanId { get; set; }
    public Guid VehicleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProposedBy { get; set; } = string.Empty;
    public DateTime PlannedStartUtc { get; set; }
    public DateTime? PlannedEndUtc { get; set; }
    public decimal? TotalKm { get; set; }
    public decimal? TotalMinutes { get; set; }
    public decimal? TotalCost { get; set; }
    public string AlgorithmVersion { get; set; } = string.Empty;
    public string InputJson { get; set; } = "{}";
    public Guid? PublishedByAccountId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}

public static class RouteStopKind
{
    public const string Delivery = "Delivery";
    public const string Pickup = "Pickup";
}

public static class RouteStopStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string PickedUp = "PickedUp";
    public const string PartiallyPickedUp = "PartiallyPickedUp";
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";
}

public sealed class RouteStop : OrganizationEntity
{
    public Guid RoutePlanId { get; set; }
    public Guid ServicePointId { get; set; }
    public Guid? LocationId { get; set; } // Bodega de pickup o almacén
    public string Kind { get; set; } = RouteStopKind.Delivery;
    public int SequenceNumber { get; set; }
    public DateTime? PlannedArrivalUtc { get; set; }
    public DateTime? PlannedDepartureUtc { get; set; }
    public DateTime? ActualArrivalUtc { get; set; }
    public string Status { get; set; } = RouteStopStatus.Pending;
    public string? PendingReason { get; set; }
}

public sealed class RouteStopLine : OrganizationEntity
{
    public Guid RouteStopId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? DeliveryOrderId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public string Action { get; set; } = "Dropoff"; // "Pickup" o "Dropoff"
}

public static class DeliveryOrderStatus
{
    public const string Pending = "Pending";
    public const string Planned = "Planned";
    public const string Dispatched = "Dispatched";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
}

public sealed class DeliveryOrder : OrganizationEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid? DestinationServicePointId { get; set; }
    public Guid? DestinationLocationId { get; set; } // Si es transferencia entre bodegas
    public DateTime RequiredDateUtc { get; set; }
    public string Priority { get; set; } = "Normal"; // "Critical", "High", "Normal", "Low"
    public string Status { get; set; } = DeliveryOrderStatus.Pending;
    public string SourceType { get; set; } = "Manual"; // "Manual", "EmergencyPoint", "EmergencyWarehouse", "Rebalancing"
    public Guid? TransferSuggestionId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeliveryOrderLine : OrganizationEntity
{
    public Guid DeliveryOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
}

public sealed class WarehouseStockPolicy : OrganizationEntity
{
    public Guid LocationId { get; set; } // Bodega
    public Guid ProductId { get; set; }
    public decimal MinStock { get; set; }
    public decimal MaxStock { get; set; }
    public int LeadTimeDays { get; set; } = 2;
    public decimal TargetCoverageDays { get; set; } = 30m;
    public bool IsManual { get; set; } = true;
    public decimal? SuggestedMinStock { get; set; }
    public decimal? SuggestedMaxStock { get; set; }
    public DateTime? LastCalculatedAtUtc { get; set; }
}

public static class TransferSuggestionStatus
{
    public const string Suggested = "Suggested";
    public const string Accepted = "Accepted";
    public const string Dismissed = "Dismissed";
    public const string Expired = "Expired";
    public const string Planned = "Planned";
}

public sealed class TransferSuggestion : OrganizationEntity
{
    public Guid SourceLocationId { get; set; } // Bodega origen con exceso
    public Guid TargetLocationId { get; set; } // Bodega destino con déficit
    public Guid ProductId { get; set; }
    public decimal SuggestedQuantity { get; set; }
    public decimal? AcceptedQuantity { get; set; }
    public decimal SourceCoverageDaysBefore { get; set; }
    public decimal SourceCoverageDaysAfter { get; set; }
    public decimal TargetCoverageDaysBefore { get; set; }
    public decimal TargetCoverageDaysAfter { get; set; }
    public string Status { get; set; } = TransferSuggestionStatus.Suggested;
    public string Rationale { get; set; } = string.Empty;
    public Guid? ReviewedByAccountId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? DismissReason { get; set; }
}

public sealed class Delivery : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? RouteStopId { get; set; }
    public Guid? DeliveryOrderId { get; set; }
    public Guid OriginLocationId { get; set; }
    public Guid ServicePointId { get; set; }
    public Guid? DestinationLocationId { get; set; } // Para traslados entre bodegas
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedAccountId { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
}

public sealed class DeliveryLine : OrganizationEntity
{
    public Guid DeliveryId { get; set; }
    public Guid LotId { get; set; }
    public Guid? HandlingUnitId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
}

public sealed class Receipt : OrganizationEntity
{
    public Guid DeliveryId { get; set; }
    public Guid ActorAccountId { get; set; }
    public Guid OperationId { get; set; }
    public DateTime? OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
}

public sealed class ReceiptLine : OrganizationEntity
{
    public Guid ReceiptId { get; set; }
    public Guid DeliveryLineId { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Stock remanente reportado en el punto antes de recibir la nueva entrega ("me quedan X").
    /// </summary>
    public decimal? RemainingQuantity { get; set; }

    /// <summary>
    /// Cantidad solicitada por el receptor para la próxima visita/reposición ("necesito X más").
    /// </summary>
    public decimal? NextRequestedQuantity { get; set; }
}

public sealed class Evidence : OrganizationEntity
{
    public Guid ReceiptId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string? Sha256 { get; set; }
    public long? ByteSize { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid UploadedByAccountId { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
}

public sealed class CustodyEvent : OrganizationEntity
{
    public Guid HandlingUnitId { get; set; }
    public Guid? DeliveryId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public Guid ActorAccountId { get; set; }
    public string EventKind { get; set; } = string.Empty;
    public Guid OperationId { get; set; }
    public DateTime? OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
}

public sealed class RoadMatrixEdge : OrganizationEntity
{
    public decimal FromLat { get; set; }
    public decimal FromLon { get; set; }
    public decimal ToLat { get; set; }
    public decimal ToLon { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal DurationMinutes { get; set; }
    public string? EncodedPolyline { get; set; }
    public string Source { get; set; } = string.Empty; // "osrm", "imported"
    public DateTime RetrievedAtUtc { get; set; }
}

public static class RouteStockReservationStatus
{
    public const string Active = "Active";
    public const string Released = "Released";
    public const string Consumed = "Consumed";
}

public sealed class RouteStockReservation : OrganizationEntity
{
    public Guid RoutePlanId { get; set; }
    public Guid DeliveryId { get; set; }
    public Guid DeliveryLineId { get; set; }
    public Guid LotId { get; set; }
    public Guid StockBalanceId { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = RouteStockReservationStatus.Active;
    public DateTime? ClosedAtUtc { get; set; }
}

public sealed class QrAccessToken : OrganizationEntity
{
    public Guid HandlingUnitId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? CreatedByAccountId { get; set; }
}
