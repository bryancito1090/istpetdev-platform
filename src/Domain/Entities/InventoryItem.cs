using System;
using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public Guid ServicePointId { get; set; }
    public ServicePoint? ServicePoint { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime? ExpirationDateUtc { get; set; }
}
