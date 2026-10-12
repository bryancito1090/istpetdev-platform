using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.ServicePoints.Dtos;
using IstpetDev.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/service-points")]
[Authorize]
[ServiceFilter(typeof(OrganizationContextFilter))]
public class ServicePointsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public ServicePointsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var points = await _context.ServicePoints
            .AsNoTracking()
            .Select(p => new
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Type = _context.CatalogOptions.Where(o => o.Id == p.TypeOptionId)
                    .Select(o => o.Code).FirstOrDefault() ?? string.Empty,
                p.Position,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        // Npgsql maps Point.X/Y to ST_X/Y, which take geometry rather than geography.
        // Materialize only authorized columns and read coordinates from the returned NTS point.
        return Ok(points.Select(p => new ServicePointDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Type = p.Type,
            Latitude = p.Position?.Y,
            Longitude = p.Position?.X,
            IsActive = p.IsActive
        }));
    }
}
