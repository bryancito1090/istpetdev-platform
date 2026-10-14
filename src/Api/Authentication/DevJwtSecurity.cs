using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace IstpetDev.Api.Authentication;

/// <summary>
/// Clave de validación para el modo de autenticación local. No emite tokens.
/// </summary>
public static class DevJwtSecurity
{
    public const string DefaultAuthority = "https://identity.istpetdev.local";

    private static readonly RSA Rsa = RSA.Create(2048);
    public static readonly RsaSecurityKey DevKey = new(Rsa) { KeyId = "istpetdev-dev-key" };
}
