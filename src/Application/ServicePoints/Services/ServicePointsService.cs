using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.ServicePoints.Dtos;
using IstpetDev.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.ServicePoints.Services;

public sealed class ServicePointsService(IApplicationDbContext _context) : IServicePointsService
{
    public async Task<IReadOnlyList<ServicePointDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var points = await _context.ServicePoints
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new { p.Id, p.Code, p.Name, p.Category, p.Position, p.IsActive })
            .ToListAsync(cancellationToken);

        // Npgsql materializa el Point de NTS; X = longitud, Y = latitud.
        return points.Select(p => new ServicePointDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Type = p.Category ?? string.Empty,
            Latitude = p.Position?.Y,
            Longitude = p.Position?.X,
            IsActive = p.IsActive
        }).ToList();
    }

    public async Task<IReadOnlyList<ServicePointDto>> GetWarehousesAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = await _context.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse)
            .AsNoTracking()
            .OrderBy(l => l.Code)
            .Select(l => new { l.Id, l.Code, l.Name, l.Position, l.IsActive })
            .ToListAsync(cancellationToken);

        return warehouses.Select(w => new ServicePointDto
        {
            Id = w.Id,
            Code = w.Code,
            Name = w.Name,
            Type = "Bodega",
            Latitude = w.Position?.Y,
            Longitude = w.Position?.X,
            IsActive = w.IsActive
        }).ToList();
    }
}
