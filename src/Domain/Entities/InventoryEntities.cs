using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class InventoryMovement : OrganizationEntity
{
    public string Kind { get; set; } = string.Empty;
    public Guid LotId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? ActorAccountId { get; set; }
    public Guid OperationId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? DeliveryLineId { get; set; }
    public Guid? ReceiptLineId { get; set; }
    public Guid? ImportBatchId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class StockBalance : OrganizationEntity
{
    public Guid LocationId { get; set; }
    public Guid LotId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
}

// Consumo de un punto y SKU; PeriodDays permite cargar totales semanales o mensuales sin desagregarlos.
public sealed class Consumption : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public Guid ProductId { get; set; }
    public DateOnly OccurredOn { get; set; }
    public decimal Quantity { get; set; }
    public int PeriodDays { get; set; } = 1;
    public string SourceKind { get; set; } = string.Empty;
    public Guid? ImportBatchId { get; set; }
}
