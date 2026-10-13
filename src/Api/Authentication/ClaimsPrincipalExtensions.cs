using System.Security.Claims;

namespace IstpetDev.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static Guid? AccountId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(AccountRoleClaimsTransformation.AccountIdClaim)?.Value, out var id) ? id : null;
}
