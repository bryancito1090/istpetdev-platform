using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IstpetDev.Api.Authentication;

/// <summary>
/// Provee claves criptográficas y generación de tokens JWT para entornos de desarrollo y evaluación local.
/// Permite que Frontend y Móvil operen fluidamente sin requerir un AWS Cognito User Pool en vivo.
/// </summary>
public static class DevJwtSecurity
{
    public const string DefaultAuthority = "https://identity.istpetdev.local";
    public const string DefaultScope = "istpetdev-api/access";

    private static readonly RSA Rsa = RSA.Create(2048);
    public static readonly RsaSecurityKey DevKey = new(Rsa) { KeyId = "istpetdev-dev-key" };
    public static readonly SigningCredentials DevSigningCredentials = new(DevKey, SecurityAlgorithms.RsaSha256);

    public static string CreateToken(
        string subject,
        string role,
        string clientId = "istpetdev-web-client",
        string? issuer = null,
        TimeSpan? lifetime = null)
    {
        var handler = new JsonWebTokenHandler();
        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer ?? DefaultAuthority,
            IssuedAt = now,
            NotBefore = now.AddSeconds(-5),
            Expires = now.Add(lifetime ?? TimeSpan.FromHours(8)),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject,
                ["client_id"] = clientId,
                ["token_use"] = "access",
                ["scope"] = DefaultScope,
                ["role"] = role,
                ["istpetdev:account_id"] = Guid.NewGuid().ToString()
            },
            SigningCredentials = DevSigningCredentials
        };

        return handler.CreateToken(tokenDescriptor);
    }
}
