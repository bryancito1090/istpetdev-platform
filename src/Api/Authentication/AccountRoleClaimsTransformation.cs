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
        var tokenRole = principal.FindFirst("role")?.Value;
        foreach (var item in principal.Identities)
            foreach (var claim in item.Claims.Where(c => c.Type == item.RoleClaimType || c.Type is "role" or "roles" or AccountIdClaim).ToArray())
                item.RemoveClaim(claim);
        var issuer = principal.FindFirst("iss")?.Value;
        var technical = (configuration.GetSection("Authentication:TechnicalClientIds").Get<string[]>() ?? [])
            .Contains(principal.FindFirst("client_id")?.Value);
        var subject = technical ? principal.FindFirst("client_id")?.Value
            : principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (issuer is null || subject is null) return principal;
        string? resolvedRole = null;
        Guid? resolvedAccountId = null;

        try
        {
            var account = await database.Accounts.AsNoTracking()
                .Where(a => a.OrganizationId == DemoOrganization.Id && a.Issuer == issuer && a.Subject == subject
                    && a.Kind == (technical ? "technical" : "human") && database.Organizations.Any(o => o.Id == a.OrganizationId && o.IsActive))
                .Select(a => new { a.Id, a.Role, a.IsActive }).SingleOrDefaultAsync();

            if (account is not null)
            {
                if (!account.IsActive)
                {
                    // Cuenta revocada/inactiva: denegación estricta sin permitir fallbacks
                    return principal;
                }

                if (Roles.IsValid(account.Role) && technical == (account.Role == Roles.Automatizacion))
                {
                    resolvedRole = account.Role;
                    resolvedAccountId = account.Id;
                }
            }
        }
        catch
        {
            // Fallback resiliente si la conexión a base de datos de desarrollo no está disponible
        }

        // Fallback en desarrollo/demo: si la cuenta aún no está persistida en BD, verificar rol institucional del token firmado
        if (resolvedRole is null && Roles.IsValid(tokenRole))
        {
            resolvedRole = tokenRole;
        }

        if (resolvedRole is null) return principal;

        // Si tenemos rol pero no accountId registrado en BD, intentar sincronizar la cuenta demo en BD para que no fallen las FKs
        if (!resolvedAccountId.HasValue)
        {
            try
            {
                var existing = await database.Accounts.FirstOrDefaultAsync(a => a.OrganizationId == DemoOrganization.Id && a.Issuer == issuer && a.Subject == subject);
                if (existing is null)
                {
                    var newAccount = new Domain.Entities.Account
                    {
                        OrganizationId = DemoOrganization.Id,
                        Issuer = issuer,
                        Subject = subject,
                        Kind = technical ? "technical" : "human",
                        DisplayName = subject,
                        Role = resolvedRole,
                        IsActive = true
                    };
                    database.Accounts.Add(newAccount);
                    await database.SaveChangesAsync();
                    resolvedAccountId = newAccount.Id;
                }
                else
                {
                    resolvedAccountId = existing.Id;
                }
            }
            catch
            {
                // Si la BD no permite inserción temporal, dejar resolvedAccountId en null para no generar una FK rota
            }
        }

        if (resolvedAccountId.HasValue)
        {
            identity.AddClaim(new Claim(AccountIdClaim, resolvedAccountId.Value.ToString()));
        }
        identity.AddClaim(new Claim(identity.RoleClaimType, resolvedRole));
        return principal;
    }
}
