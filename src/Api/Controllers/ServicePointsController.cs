using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.ServicePoints.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/service-points")]
[Authorize]
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
            .Select(p => new ServicePointDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Type = p.Type,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(points);
    }
}
