using System;
using System.Collections.Generic;

namespace IstpetDev.Application.PublicTrace.Dtos;

public class PublicTraceEventDto
{
    public string Stage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class PublicTraceDto
{
    public string UnitId { get; set; } = string.Empty;
    public string TraceType { get; set; } = "HandlingUnit";
    public string Status { get; set; } = "Verified";
    public DateTime GeneratedAtUtc { get; set; }
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<PublicTraceEventDto> Events { get; set; } = Array.Empty<PublicTraceEventDto>();
}
