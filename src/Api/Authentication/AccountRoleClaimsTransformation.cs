using System.Security.Claims;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Authentication;

public sealed class AccountRoleClaimsTransformation(ApplicationDbContext database, IConfiguration configuration) : IClaimsTransformation
{
    public const string AccountIdClaim = "istpetdev:account_id";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity) return principal;
        foreach (var item in principal.Identities)
            foreach (var claim in item.Claims.Where(c => c.Type == item.RoleClaimType || c.Type is "role" or "roles" or AccountIdClaim).ToArray())
                item.RemoveClaim(claim);
        var issuer = principal.FindFirst("iss")?.Value;
        var technical = (configuration.GetSection("Authentication:TechnicalClientIds").Get<string[]>() ?? [])
            .Contains(principal.FindFirst("client_id")?.Value);
        var subject = technical ? principal.FindFirst("client_id")?.Value
            : principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (issuer is null || subject is null) return principal;
        Domain.Entities.Account? account;
        try
        {
            account = await database.Accounts.AsNoTracking()
                .Where(a => a.OrganizationId == DemoOrganization.Id && a.Issuer == issuer && a.Subject == subject
                    && a.Kind == (technical ? "technical" : "human") && database.Organizations.Any(o => o.Id == a.OrganizationId && o.IsActive))
                .SingleOrDefaultAsync();
        }
        catch
        {
            // Sin una cuenta comprobada en la base, la identidad no recibe permisos.
            return principal;
        }

        if (account is null || !account.IsActive || !Roles.IsValid(account.Role) || technical != (account.Role == Roles.Automatizacion))
            return principal;

        identity.AddClaim(new Claim(AccountIdClaim, account.Id.ToString()));
        identity.AddClaim(new Claim(identity.RoleClaimType, account.Role));
        return principal;
    }
}
