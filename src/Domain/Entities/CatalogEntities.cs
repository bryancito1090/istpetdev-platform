using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class ServicePoint : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Region { get; set; }
    public string Criticality { get; set; } = string.Empty;
    public Point? Position { get; set; }
    public TimeOnly? WindowStartLocal { get; set; }
    public TimeOnly? WindowEndLocal { get; set; }
    public int? ServiceMinutes { get; set; }
    public decimal? LeadTimeDays { get; set; }
    public Guid? ServingWarehouseLocationId { get; set; }
    public bool IsActive { get; set; }
}

// Bodega, punto de servicio o vehículo: todo lugar que puede tener saldo.
public sealed class Location : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public Guid? ServicePointId { get; set; }
    public Guid? VehicleId { get; set; }
    public Point? Position { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Vehicle : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid DepotLocationId { get; set; }
    public decimal MaxWeight { get; set; }
    public decimal? MaxVolume { get; set; }
    public decimal? CostPerKm { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Product : OrganizationEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public decimal WeightPerBaseUnit { get; set; }
    public decimal VolumePerBaseUnit { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Lot : OrganizationEntity
{
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsBlocked { get; set; }
}

public sealed class HandlingUnit : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? DeliveryId { get; set; }
    public Guid LocationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SealedAtUtc { get; set; }
}

public sealed class HandlingUnitLine : OrganizationEntity
{
    public Guid HandlingUnitId { get; set; }
    public Guid? DeliveryLineId { get; set; }
    public Guid LotId { get; set; }
    public decimal Quantity { get; set; }
}
