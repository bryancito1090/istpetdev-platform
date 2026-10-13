using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.ServicePoints.Services;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/service-points")]
[Authorize(Roles = Roles.Any)]
public class ServicePointsController : ControllerBase
{
    private readonly IServicePointsService _servicePointsService;

    public ServicePointsController(IServicePointsService servicePointsService)
    {
        _servicePointsService = servicePointsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var points = await _servicePointsService.GetAllAsync(cancellationToken);
        return Ok(points);
    }
}
