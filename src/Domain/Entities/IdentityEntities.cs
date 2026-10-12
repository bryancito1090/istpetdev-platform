using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class Organization : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long AuthorizationVersion { get; set; }
}

public sealed class Account : BaseEntity
{
    public string Kind { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? ClientId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string ResourceKind { get; set; } = string.Empty;
    public bool IsDelegable { get; set; }
}

public sealed class FieldDefinition : BaseEntity
{
    public string EntityCode { get; set; } = string.Empty;
    public string FieldCode { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsServerControlled { get; set; }
    public string Sensitivity { get; set; } = string.Empty;
}

public sealed class Role : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsSeed { get; set; }
    public bool IsTechnical { get; set; }
}

public sealed class RolePermission : OrganizationEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public sealed class RoleFieldPermission : OrganizationEntity
{
    public Guid RoleId { get; set; }
    public Guid FieldDefinitionId { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
}

public sealed class OrganizationAccess : OrganizationEntity
{
    public Guid AccountId { get; set; }
    public bool IsActive { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public Guid GrantedByAccountId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class AccessRole : OrganizationEntity
{
    public Guid OrganizationAccessId { get; set; }
    public Guid RoleId { get; set; }
    public Guid GrantedByAccountId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class AccessLocation : OrganizationEntity
{
    public Guid OrganizationAccessId { get; set; }
    public Guid LocationId { get; set; }
    public Guid GrantedByAccountId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class AccessServicePoint : OrganizationEntity
{
    public Guid OrganizationAccessId { get; set; }
    public Guid ServicePointId { get; set; }
    public Guid GrantedByAccountId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class AccessDelegation : OrganizationEntity
{
    public Guid OrganizationAccessId { get; set; }
    public Guid PermissionId { get; set; }
    public string ScopeKind { get; set; } = string.Empty;
    public Guid? LocationId { get; set; }
    public Guid? ServicePointId { get; set; }
    public bool CanDelegateFurther { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public Guid GrantedByAccountId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class RbacChange : OrganizationEntity
{
    public Guid ActorAccountId { get; set; }
    public Guid? TargetAccessId { get; set; }
    public Guid? TargetRoleId { get; set; }
    public string ChangeKind { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
