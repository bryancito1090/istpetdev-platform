using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class ServicePoint : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? TypeOptionId { get; set; }
    public Point? Position { get; set; }
    public string Criticality { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? ConfigurationVersionId { get; set; }
}

public sealed class ServicePointWindow : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly StartLocalTime { get; set; }
    public TimeOnly EndLocalTime { get; set; }
    public DateOnly ValidFromDate { get; set; }
    public DateOnly? ValidToDate { get; set; }
}

public sealed class ServicePointContact : OrganizationEntity
{
    public Guid ServicePointId { get; set; }
    public byte[]? DisplayNameCiphertext { get; set; }
    public byte[]? PhoneCiphertext { get; set; }
    public byte[]? EmailCiphertext { get; set; }
    public string EncryptionKeyVersion { get; set; } = string.Empty;
}

public sealed class Location : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public Guid? ServicePointId { get; set; }
    public Guid? VehicleId { get; set; }
    public Point? Position { get; set; }
    public bool IsActive { get; set; }
}

public sealed class Vehicle : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public byte[] RegistrationCiphertext { get; set; } = Array.Empty<byte>();
    public decimal MaxWeight { get; set; }
    public decimal MaxVolume { get; set; }
    public bool IsActive { get; set; }
}

public sealed class VehicleAvailability : OrganizationEntity
{
    public Guid VehicleId { get; set; }
    public DateTime AvailableFromUtc { get; set; }
    public DateTime AvailableToUtc { get; set; }
    public Guid DepotLocationId { get; set; }
}

public sealed class VehiclePosition : OrganizationEntity
{
    public Guid VehicleId { get; set; }
    public Guid? RoutePlanId { get; set; }
    public Guid RecordedByAccountId { get; set; }
    public DateTime OccurredAtDevice { get; set; }
    public DateTime ReceivedAtServer { get; set; }
    public Point Position { get; set; } = null!;
    public Guid OperationId { get; set; }
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

public sealed class ProductUnitConversion : OrganizationEntity
{
    public Guid ProductId { get; set; }
    public string PackagingCode { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
}

public sealed class Lot : OrganizationEntity
{
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public Guid? ConditionOptionId { get; set; }
    public bool IsBlocked { get; set; }
}

public sealed class HandlingUnit : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SealedAtUtc { get; set; }
}

public sealed class HandlingUnitLine : OrganizationEntity
{
    public Guid HandlingUnitId { get; set; }
    public Guid LotId { get; set; }
    public decimal Quantity { get; set; }
}
