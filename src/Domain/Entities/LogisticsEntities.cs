using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class RoadMatrix : OrganizationEntity
{
    public string ProviderCode { get; set; } = string.Empty;
    public Guid? DatasetVersionId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime RetrievedAtUtc { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public Guid ConfigurationVersionId { get; set; }
}

public sealed class RoadMatrixEdge : OrganizationEntity
{
    public Guid RoadMatrixId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? DurationSeconds { get; set; }
    public bool IsReachable { get; set; }
    public string? UnreachableReason { get; set; }
}

public sealed class RoutePlan : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public int Revision { get; set; }
    public Guid? SupersedesRoutePlanId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid RoadMatrixId { get; set; }
    public Guid ConfigurationVersionId { get; set; }
    public Guid WorkflowVersionId { get; set; }
    public Guid StateId { get; set; }
    public DateTime PlannedStartUtc { get; set; }
    public DateTime? PlannedEndUtc { get; set; }
    public Guid? PublishedByAccountId { get; set; }
}

public sealed class RoutePlanningSnapshot : OrganizationEntity
{
    public Guid RoutePlanId { get; set; }
    public string InputJson { get; set; } = "{}";
    public string InputHash { get; set; } = string.Empty;
    public string AlgorithmCode { get; set; } = string.Empty;
    public string AlgorithmVersion { get; set; } = string.Empty;
    public DateTime CutoffUtc { get; set; }
}

public sealed class RouteStop : OrganizationEntity
{
    public Guid RoutePlanId { get; set; }
    public Guid ServicePointId { get; set; }
    public Guid LocationId { get; set; }
    public int SequenceNumber { get; set; }
    public DateTime PlannedArrivalUtc { get; set; }
    public DateTime PlannedDepartureUtc { get; set; }
    public DateTime? ActualArrivalUtc { get; set; }
    public DateTime? ActualDepartureUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PendingReason { get; set; }
}

public sealed class Delivery : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? RouteStopId { get; set; }
    public Guid OriginLocationId { get; set; }
    public Guid DestinationLocationId { get; set; }
    public Guid ServicePointId { get; set; }
    public Guid WorkflowVersionId { get; set; }
    public Guid StateId { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
}

public sealed class DeliveryLine : OrganizationEntity
{
    public Guid DeliveryId { get; set; }
    public Guid? ReplenishmentRequestLineId { get; set; }
    public Guid LotId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public string PackagingSnapshotJson { get; set; } = "{}";
}

public sealed class DeliveryHandlingUnit : OrganizationEntity
{
    public Guid DeliveryId { get; set; }
    public Guid HandlingUnitId { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
}

public sealed class DeliveryHandlingUnitLine : OrganizationEntity
{
    public Guid DeliveryHandlingUnitId { get; set; }
    public Guid DeliveryLineId { get; set; }
    public Guid HandlingUnitLineId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class DeliveryAssignment : OrganizationEntity
{
    public Guid DeliveryId { get; set; }
    public Guid OrganizationAccessId { get; set; }
    public string AssignmentKind { get; set; } = string.Empty;
    public Guid AssignedByAccountId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class Receipt : OrganizationEntity
{
    public Guid DeliveryId { get; set; }
    public Guid ActorAccountId { get; set; }
    public Guid WorkflowVersionId { get; set; }
    public Guid StateId { get; set; }
    public Guid OperationId { get; set; }
    public DateTime? OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
    public DateTime? AcceptedAtServer { get; set; }
}

public sealed class ReceiptLine : OrganizationEntity
{
    public Guid ReceiptId { get; set; }
    public Guid DeliveryLineId { get; set; }
    public decimal QuantityAccepted { get; set; }
    public decimal QuantityRejected { get; set; }
    public Guid? RejectionReasonOptionId { get; set; }
    public Guid? ReturnLocationId { get; set; }
}

public sealed class StoredFile : OrganizationEntity
{
    public string StorageKey { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string DeclaredMimeType { get; set; } = string.Empty;
    public string? VerifiedMimeType { get; set; }
    public long ByteSize { get; set; }
    public string? Sha256 { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? EncryptionKeyReference { get; set; }
    public Guid CreatedByAccountId { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public sealed class Evidence : OrganizationEntity
{
    public Guid ReceiptId { get; set; }
    public Guid? ReceiptLineId { get; set; }
    public Guid StoredFileId { get; set; }
    public string Kind { get; set; } = string.Empty;
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

public sealed class QrAccessToken : OrganizationEntity
{
    public Guid HandlingUnitId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string ScopeCode { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid CreatedByAccountId { get; set; }
}
