using System;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Enums;

namespace IstpetDev.Domain.Entities;

public class Delivery : BaseEntity
{
    public string TrackingNumber { get; set; } = string.Empty;
    public Guid OriginServicePointId { get; set; }
    public ServicePoint? OriginServicePoint { get; set; }
    public Guid DestinationServicePointId { get; set; }
    public ServicePoint? DestinationServicePoint { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Draft;
    public int Version { get; set; } = 1;
}
