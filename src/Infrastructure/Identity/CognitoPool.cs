namespace IstpetDev.Infrastructure.Identity;

// Región y pool salen del issuer ya configurado en Authentication:Authority; no hay otra configuración que mantener.
public sealed record CognitoPool(string Region, string UserPoolId, string Issuer)
{
    public static CognitoPool? FromAuthority(string? authority)
    {
        var issuer = authority?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(issuer)) return null;
        if (issuer.StartsWith("https://identity.istpetdev.local", StringComparison.OrdinalIgnoreCase) ||
            issuer.Contains("localhost", StringComparison.OrdinalIgnoreCase)) return null;
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return null;
        var host = uri.Host.Split('.');
        var poolId = uri.AbsolutePath.Trim('/');
        if (host is not ["cognito-idp", var region, "amazonaws", "com"]) return null;
        if (poolId.Split('_') is not [var poolRegion, { Length: > 0 }] || poolRegion != region) return null;
        return new CognitoPool(region, poolId, issuer!);
    }
}
