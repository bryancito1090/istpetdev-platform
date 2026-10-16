using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

/// <summary>
/// Conductores activos que el Planificador puede asignar al publicar una ruta (PublishRequest.DriverAssignments).
/// Solo lectura y sin correos: la gestión de cuentas sigue siendo de Gerencia (/accounts).
/// </summary>
[ApiController]
[Route("api/v1/drivers")]
[Authorize(Roles = Roles.Planning)]
public sealed class DriversController(IApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        var drivers = await db.Accounts.AsNoTracking()
            .Where(a => a.OrganizationId == DemoOrganization.Id && a.IsActive && a.Role == Roles.Conductor)
            .OrderBy(a => a.DisplayName)
            .Select(a => new { accountId = a.Id, displayName = a.DisplayName })
            .ToListAsync(ct);
        return Ok(drivers);
    }
}
