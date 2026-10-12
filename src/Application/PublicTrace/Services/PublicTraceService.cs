using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.PublicTrace.Dtos;

namespace IstpetDev.Application.PublicTrace.Services;

public class PublicTraceService : IPublicTraceService
{
    public Task<PublicTraceDto?> GetTraceAsync(string unitId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return Task.FromResult<PublicTraceDto?>(null);
        }

        var now = DateTime.UtcNow;
        var trace = new PublicTraceDto
        {
            UnitId = unitId.Trim(),
            TraceType = "HandlingUnit",
            Status = "Verified",
            GeneratedAtUtc = now,
            Summary = "Unidad logística registrada y verificada en el sistema IstpetDev.",
            Events = new List<PublicTraceEventDto>
            {
                new() { Stage = "Created", Timestamp = now.AddHours(-12), Location = "Centro Logístico Quito" },
                new() { Stage = "CustodyTransferred", Timestamp = now.AddHours(-4), Location = "Transporte Autorizado" }
            }
        };

        return Task.FromResult<PublicTraceDto?>(trace);
    }
}
