using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.PublicTrace.Dtos;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.PublicTrace.Services;

public class PublicTraceService : IPublicTraceService
{
    private readonly IApplicationDbContext _context;
    private readonly IPublicTraceRateLimiter _rateLimiter;

    public PublicTraceService(IApplicationDbContext context, IPublicTraceRateLimiter rateLimiter)
    {
        _context = context;
        _rateLimiter = rateLimiter;
    }

    public async Task<PublicTraceResult> GetTraceAsync(string unitId, string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(unitId) || string.IsNullOrWhiteSpace(token))
        {
            return new PublicTraceResult.NotFound();
        }

        // 1. Hash del token de entrada (SHA-256 en minúsculas hexadecimal)
        byte[] inputHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        string tokenHashHex = Convert.ToHexString(inputHashBytes).ToLowerInvariant();

        // 2. Control de límite de consultas por token antes de revelar datos (PA-05)
        if (!_rateLimiter.IsAllowed(tokenHashHex))
        {
            return new PublicTraceResult.RateLimited();
        }

        // 3. Resolver la unidad de manipulación real (por Guid o por Code)
        var normalizedId = unitId.Trim();
        HandlingUnit? unit = null;

        if (Guid.TryParse(normalizedId, out var parsedGuid))
        {
            unit = await _context.HandlingUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == parsedGuid, cancellationToken);
        }

        if (unit == null)
        {
            unit = await _context.HandlingUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Code == normalizedId, cancellationToken);
        }

        if (unit == null)
        {
            return new PublicTraceResult.NotFound();
        }

        // 4. Resolver tokens autorizados para la unidad y comparar en tiempo constante
        var tokenRecords = await _context.QrAccessTokens
            .AsNoTracking()
            .Where(t => t.HandlingUnitId == unit.Id)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        bool tokenIsValid = false;

        foreach (var record in tokenRecords)
        {
            if (string.IsNullOrWhiteSpace(record.TokenHash))
            {
                continue;
            }

            byte[] storedHashBytes;
            try
            {
                storedHashBytes = Convert.FromHexString(record.TokenHash);
            }
            catch
            {
                continue;
            }

            // Comparación en tiempo constante para evitar ataques de temporización
            if (CryptographicOperations.FixedTimeEquals(inputHashBytes, storedHashBytes))
            {
                // El token no debe estar revocado ni vencido
                if (record.RevokedAtUtc == null && record.ExpiresAtUtc > now)
                {
                    tokenIsValid = true;
                    break;
                }
            }
        }

        if (!tokenIsValid)
        {
            // 404 uniforme: un token alterado, revocado o vencido no revela si la unidad existe
            return new PublicTraceResult.NotFound();
        }

        // 5. Construir respuesta mínima permitida (código, productos/unidades, estado, hitos con horas de servidor)
        var lines = await _context.HandlingUnitLines
            .AsNoTracking()
            .Where(l => l.HandlingUnitId == unit.Id)
            .ToListAsync(cancellationToken);

        var lotIds = lines.Select(l => l.LotId).Distinct().ToList();
        var lots = await _context.Lots
            .AsNoTracking()
            .Where(l => lotIds.Contains(l.Id))
            .ToListAsync(cancellationToken);

        var productIds = lots.Select(l => l.ProductId).Distinct().ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var lotProductMap = lots.ToDictionary(l => l.Id, l => products.GetValueOrDefault(l.ProductId));

        var productUnits = new List<PublicTraceProductUnitDto>();
        foreach (var line in lines)
        {
            if (lotProductMap.TryGetValue(line.LotId, out var prod) && prod != null)
            {
                productUnits.Add(new PublicTraceProductUnitDto
                {
                    Sku = prod.Sku,
                    Unit = prod.BaseUnit
                });
            }
        }

        // Custodia real registrada en el servidor
        var custodyEvents = await _context.CustodyEvents
            .AsNoTracking()
            .Where(e => e.HandlingUnitId == unit.Id)
            .OrderBy(e => e.ReceivedAtServer)
            .ToListAsync(cancellationToken);

        // #162: hitos legibles de la cadena Bodega → Conductor → Receptor.
        var events = new List<PublicTraceEventDto>
        {
            new() { Stage = "Preparada", Timestamp = unit.SealedAtUtc ?? unit.CreatedAtUtc }
        };
        events.AddRange(custodyEvents.Select(e => new PublicTraceEventDto
        {
            Stage = e.EventKind switch
            {
                "Dispatched" => "Despachada",
                "PickedUp" => "En ruta",
                "Delivered" => "Entregada",
                "Received" => "Recibida",
                "Partial" => "Recibida Parcial",
                _ => e.EventKind
            },
            Timestamp = e.ReceivedAtServer
        }));

        var responseDto = new PublicTraceResponseDto
        {
            UnitId = !string.IsNullOrWhiteSpace(unit.Code) ? unit.Code : unit.Id.ToString(),
            Status = unit.Status,
            ProductUnits = productUnits,
            Events = events
        };

        return new PublicTraceResult.Success(responseDto);
    }
}
