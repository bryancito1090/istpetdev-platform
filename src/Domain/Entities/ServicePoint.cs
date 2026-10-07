using System;
using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public class ServicePoint : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Hospital";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Point? Location { get; set; }
    public bool IsActive { get; set; } = true;
}
