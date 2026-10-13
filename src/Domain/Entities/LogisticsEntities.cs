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

public sealed class RouteStop : OrganizationEntity
{
    public Guid RoutePlanId { get; set; }
    public Guid ServicePointId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime? PlannedArrivalUtc { get; set; }
    public DateTime? PlannedDepartureUtc { get; set; }
    public DateTime? ActualArrivalUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PendingReason { get; set; }
}

public sealed class Delivery : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? RouteStopId { get; set; }
    public Guid OriginLocationId { get; set; }
    public Guid ServicePointId { get; set; }
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
}

public sealed class Evidence : OrganizationEntity
{
    public Guid ReceiptId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string? Sha256 { get; set; }
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
