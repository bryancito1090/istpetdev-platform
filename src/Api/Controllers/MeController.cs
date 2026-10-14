using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

// FR-05 de 013: las apps deciden acceso y navegación con esta respuesta, no con claims del token.
[ApiController]
[Route("api/v1/me")]
public sealed class MeController(IApplicationDbContext database) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Human)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
    {
        var accountId = User.AccountId();
        var account = accountId is null ? null : await database.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId && a.IsActive)
            .Select(a => new { a.Id, a.DisplayName, a.Role })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null || Roles.PlatformOf(account.Role) is not { } platform) return Forbid();
        return new MeResponse(account.Id, account.DisplayName, account.Role, platform);
    }
}

public sealed record MeResponse(Guid AccountId, string DisplayName, string Role, string Platform);
