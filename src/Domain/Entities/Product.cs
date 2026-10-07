using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "Unit";
    public string? Description { get; set; }
}
