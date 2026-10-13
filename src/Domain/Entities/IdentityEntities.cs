using IstpetDev.Domain.Common;

namespace IstpetDev.Domain.Entities;

public sealed class Organization : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// Identidad externa (issuer, sub). Las cuentas técnicas de Cognito (client_credentials) usan sub = client_id.
public sealed class Account : OrganizationEntity
{
    public string Kind { get; set; } = "human";
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
