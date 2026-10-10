using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class ConfigurationDefinition : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string ValidationSchemaJson { get; set; } = "{}";
    public string Sensitivity { get; set; } = string.Empty;
    public string AllowedScopes { get; set; } = string.Empty;
}

public sealed class ConfigurationVersion : OrganizationEntity
{
    public Guid ConfigurationDefinitionId { get; set; }
    public int Revision { get; set; }
    public string ValuesJson { get; set; } = "{}";
    public string ContentHash { get; set; } = string.Empty;
    public DateTime? PublishedAtUtc { get; set; }
    public Guid CreatedByAccountId { get; set; }
}

public sealed class ConfigurationActivation : OrganizationEntity
{
    public Guid ConfigurationVersionId { get; set; }
    public string ScopeKind { get; set; } = string.Empty;
    public Guid? LocationId { get; set; }
    public Guid? ServicePointId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public Guid ChangedByAccountId { get; set; }
}

public sealed class Catalog : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CatalogOption : OrganizationEntity
{
    public Guid CatalogId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int OrderNumber { get; set; }
    public bool IsActive { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class WorkflowDefinition : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class WorkflowVersion : OrganizationEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public int Revision { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public Guid CreatedByAccountId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}

public sealed class WorkflowState : OrganizationEntity
{
    public Guid WorkflowVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsInitial { get; set; }
    public bool IsTerminal { get; set; }
}

public sealed class WorkflowTransition : OrganizationEntity
{
    public Guid WorkflowVersionId { get; set; }
    public Guid FromStateId { get; set; }
    public Guid ToStateId { get; set; }
    public Guid PermissionId { get; set; }
    public string HandlerCode { get; set; } = string.Empty;
    public string? ConditionJson { get; set; }
}

public sealed class Template : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class TemplateVersion : OrganizationEntity
{
    public Guid TemplateId { get; set; }
    public int Revision { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ValidationSchemaJson { get; set; } = "{}";
    public string LayoutJson { get; set; } = "{}";
    public string ContentJson { get; set; } = "{}";
    public string ContentHash { get; set; } = string.Empty;
    public Guid? WorkflowVersionId { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public Guid CreatedByAccountId { get; set; }
}

public sealed class TemplateField : OrganizationEntity
{
    public Guid TemplateVersionId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int OrderNumber { get; set; }
    public string? ValidationJson { get; set; }
    public string? VisibilityConditionJson { get; set; }
    public string Sensitivity { get; set; } = string.Empty;
    public Guid? CatalogId { get; set; }
}

public sealed class TemplateFieldOption : OrganizationEntity
{
    public Guid TemplateFieldId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int OrderNumber { get; set; }
    public bool IsActive { get; set; }
}

public sealed class TemplateFieldAccess : OrganizationEntity
{
    public Guid RoleId { get; set; }
    public Guid TemplateFieldId { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
}

public sealed class TemplateBinding : OrganizationEntity
{
    public Guid TemplateVersionId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string ActionCode { get; set; } = string.Empty;
    public string ScopeKind { get; set; } = string.Empty;
    public Guid? LocationId { get; set; }
    public Guid? ServicePointId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
}

public sealed class FormSubmission : OrganizationEntity
{
    public Guid TemplateVersionId { get; set; }
    public Guid SubmittedByAccountId { get; set; }
    public string DataJson { get; set; } = "{}";
    public byte[]? SensitiveDataCiphertext { get; set; }
    public string? EncryptionKeyVersion { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public Guid OperationId { get; set; }
    public Guid? SupersedesSubmissionId { get; set; }
    public Guid? DeliveryId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? ServicePointId { get; set; }
    public Guid? InventoryMovementId { get; set; }
}

public sealed class FormAttachment : OrganizationEntity
{
    public Guid FormSubmissionId { get; set; }
    public Guid StoredFileId { get; set; }
    public Guid TemplateFieldId { get; set; }
}
