using System;
using System.Collections.Generic;

namespace IstpetDev.Application.PublicTrace.Dtos;

public sealed class PublicTraceProductUnitDto
{
    public string Sku { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public sealed class PublicTraceEventDto
{
    public string Stage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public sealed class PublicTraceResponseDto
{
    public string UnitId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<PublicTraceProductUnitDto> ProductUnits { get; set; } = [];
    public IReadOnlyList<PublicTraceEventDto> Events { get; set; } = [];
}
