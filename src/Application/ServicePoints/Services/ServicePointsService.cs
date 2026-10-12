using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.ServicePoints.Dtos;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.ServicePoints.Services;

public class ServicePointsService : IServicePointsService
{
    private readonly IApplicationDbContext _context;

    public ServicePointsService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ServicePointDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var points = await _context.ServicePoints
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                Type = _context.CatalogOptions
                    .Where(o => o.Id == p.TypeOptionId)
                    .Select(o => o.Code)
                    .FirstOrDefault() ?? string.Empty,
                p.Position,
                p.IsActive
            })
            .ToListAsync(cancellationToken);

        // Npgsql mapea Point.X/Y a ST_X/Y. Materializamos y leemos las coordenadas del punto NTS.
        return points.Select(p => new ServicePointDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Type = p.Type,
            Latitude = p.Position?.Y,
            Longitude = p.Position?.X,
            IsActive = p.IsActive
        }).ToList();
    }
}
