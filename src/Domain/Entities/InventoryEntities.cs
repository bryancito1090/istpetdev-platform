using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class InventoryMovement : OrganizationEntity
{
    public long RecordedSequence { get; set; }
    public string Kind { get; set; } = string.Empty;
    public Guid LotId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public decimal Quantity { get; set; }
    public Guid ActorAccountId { get; set; }
    public Guid OperationId { get; set; }
    public DateTime? OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
    public Guid? DeliveryLineId { get; set; }
    public Guid? ReceiptLineId { get; set; }
    public Guid? ConsumptionId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class StockBalance : OrganizationEntity
{
    public Guid LocationId { get; set; }
    public Guid LotId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public long LastMovementSequence { get; set; }
}

public sealed class InventorySnapshot : OrganizationEntity
{
    public DateTime CutoffUtc { get; set; }
    public long LastMovementSequence { get; set; }
    public Guid CreatedByAccountId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}

public sealed class InventorySnapshotLine : OrganizationEntity
{
    public Guid InventorySnapshotId { get; set; }
    public Guid LocationId { get; set; }
    public Guid LotId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public long BalanceVersion { get; set; }
    public string? LotConditionCode { get; set; }
    public bool WasLotBlocked { get; set; }
    public DateOnly? LotExpiresOn { get; set; }
}

public sealed class Reservation : OrganizationEntity
{
    public Guid DeliveryLineId { get; set; }
    public Guid LocationId { get; set; }
    public Guid LotId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal ReleasedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class Consumption : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public Guid LotId { get; set; }
    public decimal Quantity { get; set; }
    public string SourceKind { get; set; } = string.Empty;
    public Guid ActorAccountId { get; set; }
    public DateTime? OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
    public Guid OperationId { get; set; }
}

public sealed class DemandObservation : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ObservedConsumption { get; set; }
    public string SourceKind { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; }
}

public sealed class ReplenishmentRequest : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public Guid RequestedByAccountId { get; set; }
    public DateTime NeededByUtc { get; set; }
    public Guid WorkflowVersionId { get; set; }
    public Guid StateId { get; set; }
    public Guid ConfigurationVersionId { get; set; }
}

public sealed class ReplenishmentRequestLine : OrganizationEntity
{
    public Guid ReplenishmentRequestId { get; set; }
    public Guid ProductId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal AllocatedQuantity { get; set; }
}
