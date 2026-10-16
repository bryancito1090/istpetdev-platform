using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IstpetDev.Tests.Support;

// Tokens con la forma de un access token de Cognito, firmados con una clave RSA solo de pruebas.
public static class TestTokens
{
    public const string Issuer = "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_TESTPOOL";
    public const string ClientId = "istpetdev-test-client";
    public const string Planner = "planner-1";
    public const string Driver = "driver-1";
    public const string Receiver = "receiver-1";

    public static readonly RsaSecurityKey Key = new(RSA.Create(2048)) { KeyId = "istpetdev-test" };

    public static string Create(string subject, string tokenUse = "access", string scope = "istpetdev-api/access", string? role = null, string? clientId = null, string? issuer = null, DateTime? expires = null, SecurityKey? signingKey = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Issuer,
            IssuedAt = expires?.AddMinutes(-10) ?? DateTime.UtcNow,
            NotBefore = expires?.AddMinutes(-10) ?? DateTime.UtcNow.AddSeconds(-5),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject, ["client_id"] = clientId ?? ClientId, ["token_use"] = tokenUse, ["scope"] = scope, ["role"] = role ?? string.Empty, ["istpetdev:account_id"] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(signingKey ?? Key, SecurityAlgorithms.RsaSha256),
        });
}
