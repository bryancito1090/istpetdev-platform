using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IstpetDev.Infrastructure.Persistence;

internal static class CompleteSchema
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<Organization>(entity =>
        {
            Common(entity, "Organization");
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.TimeZone).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.AuthorizationVersion).HasColumnType("bigint").IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Organization_Version", "\"Version\" > 0"));
        });

        model.Entity<Account>(entity =>
        {
            Common(entity, "Account");
            entity.Property(e => e.Kind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Issuer).HasColumnType("character varying(512)").HasMaxLength(512).IsRequired();
            entity.Property(e => e.Subject).HasColumnType("character varying(512)").HasMaxLength(512);
            entity.Property(e => e.ClientId).HasColumnType("character varying(512)").HasMaxLength(512);
            entity.Property(e => e.DisplayName).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.Issuer, e.Subject }).IsUnique().HasFilter("\"Kind\" = 'human'");
            entity.HasIndex(e => new { e.Issuer, e.ClientId }).IsUnique().HasFilter("\"Kind\" = 'technical'");
            entity.ToTable(t => t.HasCheckConstraint("CK_Account_Kind", "(\"Kind\" = 'human' AND \"Subject\" IS NOT NULL AND \"ClientId\" IS NULL) OR (\"Kind\" = 'technical' AND \"ClientId\" IS NOT NULL AND \"Subject\" IS NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Account_Version", "\"Version\" > 0"));
        });

        model.Entity<Permission>(entity =>
        {
            Common(entity, "Permission");
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ModuleCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ActionCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ResourceKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.IsDelegable).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Permission_Version", "\"Version\" > 0"));
        });

        model.Entity<FieldDefinition>(entity =>
        {
            Common(entity, "FieldDefinition");
            entity.Property(e => e.EntityCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.FieldCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.DataType).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.IsServerControlled).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.Sensitivity).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.EntityCode, e.FieldCode }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_FieldDefinition_Sensitivity", "\"Sensitivity\" IN ('public', 'internal', 'sensitive')"));
            entity.ToTable(t => t.HasCheckConstraint("CK_FieldDefinition_Version", "\"Version\" > 0"));
        });

        model.Entity<Role>(entity =>
        {
            Common(entity, "Role");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnType("character varying(2000)").HasMaxLength(2000);
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.IsSeed).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.IsTechnical).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Role_Version", "\"Version\" > 0"));
        });

        model.Entity<RolePermission>(entity =>
        {
            Common(entity, "RolePermission");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoleId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Role>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PermissionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Permission>().WithMany().HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.RoleId, e.PermissionId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_RolePermission_Version", "\"Version\" > 0"));
        });

        model.Entity<RoleFieldPermission>(entity =>
        {
            Common(entity, "RoleFieldPermission");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoleId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Role>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.FieldDefinitionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<FieldDefinition>().WithMany().HasForeignKey(e => e.FieldDefinitionId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CanRead).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.CanWrite).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.RoleId, e.FieldDefinitionId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_RoleFieldPermission_ReadBeforeWrite", "NOT \"CanWrite\" OR \"CanRead\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_RoleFieldPermission_Version", "\"Version\" > 0"));
        });

        model.Entity<OrganizationAccess>(entity =>
        {
            Common(entity, "OrganizationAccess");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.AccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.ValidFromUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.ValidToUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.GrantedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.GrantedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RevokedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.AccountId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_OrganizationAccess_Interval", "\"ValidToUtc\" IS NULL OR \"ValidFromUtc\" < \"ValidToUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_OrganizationAccess_Version", "\"Version\" > 0"));
        });

        model.Entity<AccessRole>(entity =>
        {
            Common(entity, "AccessRole");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OrganizationAccessId).HasColumnType("uuid").IsRequired();
            entity.HasOne<OrganizationAccess>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.OrganizationAccessId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoleId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Role>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.GrantedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.GrantedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RevokedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.OrganizationAccessId, e.RoleId }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_AccessRole_Version", "\"Version\" > 0"));
        });

        model.Entity<AccessLocation>(entity =>
        {
            Common(entity, "AccessLocation");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OrganizationAccessId).HasColumnType("uuid").IsRequired();
            entity.HasOne<OrganizationAccess>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.OrganizationAccessId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.GrantedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.GrantedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RevokedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.OrganizationAccessId, e.LocationId }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_AccessLocation_Version", "\"Version\" > 0"));
        });

        model.Entity<AccessServicePoint>(entity =>
        {
            Common(entity, "AccessServicePoint");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OrganizationAccessId).HasColumnType("uuid").IsRequired();
            entity.HasOne<OrganizationAccess>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.OrganizationAccessId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.GrantedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.GrantedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RevokedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.OrganizationAccessId, e.ServicePointId }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_AccessServicePoint_Version", "\"Version\" > 0"));
        });

        model.Entity<AccessDelegation>(entity =>
        {
            Common(entity, "AccessDelegation");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OrganizationAccessId).HasColumnType("uuid").IsRequired();
            entity.HasOne<OrganizationAccess>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.OrganizationAccessId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PermissionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Permission>().WithMany().HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ScopeKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.LocationId).HasColumnType("uuid");
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid");
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CanDelegateFurther).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.ValidFromUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.ValidToUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.GrantedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.GrantedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RevokedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.OrganizationAccessId, e.PermissionId, e.ScopeKind }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL AND \"ScopeKind\" = 'organization'");
            entity.HasIndex(e => new { e.OrganizationId, e.OrganizationAccessId, e.PermissionId, e.ScopeKind, e.LocationId }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL AND \"LocationId\" IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.OrganizationAccessId, e.PermissionId, e.ScopeKind, e.ServicePointId }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL AND \"ServicePointId\" IS NOT NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_AccessDelegation_Scope", "(\"ScopeKind\" = 'organization' AND \"LocationId\" IS NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'location' AND \"LocationId\" IS NOT NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'service-point' AND \"ServicePointId\" IS NOT NULL AND \"LocationId\" IS NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_AccessDelegation_Interval", "\"ValidToUtc\" IS NULL OR \"ValidFromUtc\" < \"ValidToUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_AccessDelegation_Version", "\"Version\" > 0"));
        });

        model.Entity<RbacChange>(entity =>
        {
            Common(entity, "RbacChange");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ActorAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TargetAccessId).HasColumnType("uuid");
            entity.HasOne<OrganizationAccess>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TargetAccessId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TargetRoleId).HasColumnType("uuid");
            entity.HasOne<Role>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TargetRoleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ChangeKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.BeforeJson).HasColumnType("jsonb");
            entity.Property(e => e.AfterJson).HasColumnType("jsonb");
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.CorrelationId).HasColumnType("character varying(128)").HasMaxLength(128).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_RbacChange_Version", "\"Version\" > 0"));
        });

        model.Entity<ConfigurationDefinition>(entity =>
        {
            Common(entity, "ConfigurationDefinition");
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ModuleCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ValidationSchemaJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Sensitivity).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.AllowedScopes).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ConfigurationDefinition_Version", "\"Version\" > 0"));
        });

        model.Entity<ConfigurationVersion>(entity =>
        {
            Common(entity, "ConfigurationVersion");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConfigurationDefinitionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ConfigurationDefinition>().WithMany().HasForeignKey(e => e.ConfigurationDefinitionId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Revision).HasColumnType("integer").IsRequired();
            entity.Property(e => e.ValuesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PublishedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CreatedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.ConfigurationDefinitionId, e.Revision }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ConfigurationVersion_Revision", "\"Revision\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ConfigurationVersion_Version", "\"Version\" > 0"));
        });

        model.Entity<ConfigurationActivation>(entity =>
        {
            Common(entity, "ConfigurationActivation");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConfigurationVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ConfigurationVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConfigurationVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ScopeKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.LocationId).HasColumnType("uuid");
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid");
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.EffectiveFromUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.EffectiveToUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.ChangedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ChangedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_ConfigurationActivation_Scope", "(\"ScopeKind\" = 'organization' AND \"LocationId\" IS NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'location' AND \"LocationId\" IS NOT NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'service-point' AND \"ServicePointId\" IS NOT NULL AND \"LocationId\" IS NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ConfigurationActivation_Interval", "\"EffectiveToUtc\" IS NULL OR \"EffectiveFromUtc\" < \"EffectiveToUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_ConfigurationActivation_Version", "\"Version\" > 0"));
        });

        model.Entity<Catalog>(entity =>
        {
            Common(entity, "Catalog");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ValueType).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Catalog_Version", "\"Version\" > 0"));
        });

        model.Entity<CatalogOption>(entity =>
        {
            Common(entity, "CatalogOption");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CatalogId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Catalog>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.CatalogId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Label).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.OrderNumber).HasColumnType("integer").IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.MetadataJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.OrganizationId, e.CatalogId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_CatalogOption_Version", "\"Version\" > 0"));
        });

        model.Entity<WorkflowDefinition>(entity =>
        {
            Common(entity, "WorkflowDefinition");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ModuleCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_WorkflowDefinition_Version", "\"Version\" > 0"));
        });

        model.Entity<WorkflowVersion>(entity =>
        {
            Common(entity, "WorkflowVersion");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.WorkflowDefinitionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowDefinition>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowDefinitionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Revision).HasColumnType("integer").IsRequired();
            entity.Property(e => e.PublishedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CreatedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.WorkflowDefinitionId, e.Revision }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_WorkflowVersion_Revision", "\"Revision\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_WorkflowVersion_Version", "\"Version\" > 0"));
        });

        model.Entity<WorkflowState>(entity =>
        {
            Common(entity, "WorkflowState");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.WorkflowVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Label).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsInitial).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.IsTerminal).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.WorkflowVersionId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_WorkflowState_Version", "\"Version\" > 0"));
        });

        model.Entity<WorkflowTransition>(entity =>
        {
            Common(entity, "WorkflowTransition");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.WorkflowVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.FromStateId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.FromStateId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ToStateId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ToStateId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PermissionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Permission>().WithMany().HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.HandlerCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ConditionJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.OrganizationId, e.WorkflowVersionId, e.FromStateId, e.ToStateId, e.HandlerCode }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_WorkflowTransition_Version", "\"Version\" > 0"));
        });

        model.Entity<Template>(entity =>
        {
            Common(entity, "Template");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Kind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ModuleCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Template_Kind", "\"Kind\" IN ('form', 'document', 'qr-label', 'explanation-prompt')"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Template_Version", "\"Version\" > 0"));
        });

        model.Entity<TemplateVersion>(entity =>
        {
            Common(entity, "TemplateVersion");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TemplateId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Template>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TemplateId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Revision).HasColumnType("integer").IsRequired();
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ValidationSchemaJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.LayoutJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ContentJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.WorkflowVersionId).HasColumnType("uuid");
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PublishedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CreatedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.TemplateId, e.Revision }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateVersion_Status", "\"Status\" IN ('draft', 'published', 'retired') AND (\"Status\" = 'draft' OR \"PublishedAtUtc\" IS NOT NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateVersion_Revision", "\"Revision\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateVersion_Version", "\"Version\" > 0"));
        });

        model.Entity<TemplateField>(entity =>
        {
            Common(entity, "TemplateField");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TemplateVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<TemplateVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TemplateVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Key).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Label).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.DataType).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.IsRequired).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.OrderNumber).HasColumnType("integer").IsRequired();
            entity.Property(e => e.ValidationJson).HasColumnType("jsonb");
            entity.Property(e => e.VisibilityConditionJson).HasColumnType("jsonb");
            entity.Property(e => e.Sensitivity).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.CatalogId).HasColumnType("uuid");
            entity.HasOne<Catalog>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.CatalogId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.TemplateVersionId, e.Key }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateField_Version", "\"Version\" > 0"));
        });

        model.Entity<TemplateFieldOption>(entity =>
        {
            Common(entity, "TemplateFieldOption");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TemplateFieldId).HasColumnType("uuid").IsRequired();
            entity.HasOne<TemplateField>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TemplateFieldId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Label).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.OrderNumber).HasColumnType("integer").IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.TemplateFieldId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateFieldOption_Version", "\"Version\" > 0"));
        });

        model.Entity<TemplateFieldAccess>(entity =>
        {
            Common(entity, "TemplateFieldAccess");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoleId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Role>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TemplateFieldId).HasColumnType("uuid").IsRequired();
            entity.HasOne<TemplateField>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TemplateFieldId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CanRead).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.CanWrite).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.RoleId, e.TemplateFieldId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateFieldAccess_ReadBeforeWrite", "NOT \"CanWrite\" OR \"CanRead\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateFieldAccess_Version", "\"Version\" > 0"));
        });

        model.Entity<TemplateBinding>(entity =>
        {
            Common(entity, "TemplateBinding");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TemplateVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<TemplateVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TemplateVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ModuleCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ActionCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ScopeKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.LocationId).HasColumnType("uuid");
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid");
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.EffectiveFromUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.EffectiveToUtc).HasColumnType("timestamp with time zone");
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateBinding_Scope", "(\"ScopeKind\" = 'organization' AND \"LocationId\" IS NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'location' AND \"LocationId\" IS NOT NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'service-point' AND \"ServicePointId\" IS NOT NULL AND \"LocationId\" IS NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateBinding_Interval", "\"EffectiveToUtc\" IS NULL OR \"EffectiveFromUtc\" < \"EffectiveToUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_TemplateBinding_Version", "\"Version\" > 0"));
        });

        model.Entity<FormSubmission>(entity =>
        {
            Common(entity, "FormSubmission");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TemplateVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<TemplateVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TemplateVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SubmittedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.SubmittedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DataJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.SensitiveDataCiphertext).HasColumnType("bytea");
            entity.Property(e => e.EncryptionKeyVersion).HasColumnType("character varying(64)").HasMaxLength(64);
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.SupersedesSubmissionId).HasColumnType("uuid");
            entity.HasOne<FormSubmission>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SupersedesSubmissionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryId).HasColumnType("uuid");
            entity.HasOne<Delivery>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReceiptId).HasColumnType("uuid");
            entity.HasOne<Receipt>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReceiptId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid");
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.InventoryMovementId).HasColumnType("uuid");
            entity.HasOne<InventoryMovement>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.InventoryMovementId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.SubmittedByAccountId, e.OperationId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_FormSubmission_Context", "num_nonnulls(\"DeliveryId\", \"ReceiptId\", \"ServicePointId\", \"InventoryMovementId\") <= 1"));
            entity.ToTable(t => t.HasCheckConstraint("CK_FormSubmission_Encryption", "(\"SensitiveDataCiphertext\" IS NULL) = (\"EncryptionKeyVersion\" IS NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_FormSubmission_Version", "\"Version\" > 0"));
        });

        model.Entity<FormAttachment>(entity =>
        {
            Common(entity, "FormAttachment");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.FormSubmissionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<FormSubmission>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.FormSubmissionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StoredFileId).HasColumnType("uuid").IsRequired();
            entity.HasOne<StoredFile>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StoredFileId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TemplateFieldId).HasColumnType("uuid").IsRequired();
            entity.HasOne<TemplateField>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TemplateFieldId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.FormSubmissionId, e.StoredFileId, e.TemplateFieldId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_FormAttachment_Version", "\"Version\" > 0"));
        });

        model.Entity<ServicePoint>(entity =>
        {
            Common(entity, "ServicePoint");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.TypeOptionId).HasColumnType("uuid");
            entity.HasOne<CatalogOption>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TypeOptionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Position).HasColumnType("geography(Point,4326)");
            entity.Property(e => e.Criticality).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.ConfigurationVersionId).HasColumnType("uuid");
            entity.HasOne<ConfigurationVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConfigurationVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.HasIndex(e => e.Position).HasMethod("gist");
            entity.ToTable(t => t.HasCheckConstraint("CK_ServicePoint_Version", "\"Version\" > 0"));
        });

        model.Entity<ServicePointWindow>(entity =>
        {
            Common(entity, "ServicePointWindow");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DayOfWeek).HasColumnType("integer").IsRequired();
            entity.Property(e => e.StartLocalTime).HasColumnType("time without time zone").IsRequired();
            entity.Property(e => e.EndLocalTime).HasColumnType("time without time zone").IsRequired();
            entity.Property(e => e.ValidFromDate).HasColumnType("date").IsRequired();
            entity.Property(e => e.ValidToDate).HasColumnType("date");
            entity.ToTable(t => t.HasCheckConstraint("CK_ServicePointWindow_Day", "\"DayOfWeek\" BETWEEN 0 AND 6"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ServicePointWindow_Time", "\"StartLocalTime\" < \"EndLocalTime\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_ServicePointWindow_Interval", "\"ValidToDate\" IS NULL OR \"ValidFromDate\" < \"ValidToDate\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_ServicePointWindow_Version", "\"Version\" > 0"));
        });

        model.Entity<ServicePointContact>(entity =>
        {
            Common(entity, "ServicePointContact");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DisplayNameCiphertext).HasColumnType("bytea");
            entity.Property(e => e.PhoneCiphertext).HasColumnType("bytea");
            entity.Property(e => e.EmailCiphertext).HasColumnType("bytea");
            entity.Property(e => e.EncryptionKeyVersion).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_ServicePointContact_Version", "\"Version\" > 0"));
        });

        model.Entity<Location>(entity =>
        {
            Common(entity, "Location");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Kind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ServicePointId).HasColumnType("uuid");
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.VehicleId).HasColumnType("uuid");
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.VehicleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Position).HasColumnType("geography(Point,4326)");
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.VehicleId }).IsUnique().HasFilter("\"VehicleId\" IS NOT NULL");
            entity.HasIndex(e => e.Position).HasMethod("gist");
            entity.ToTable(t => t.HasCheckConstraint("CK_Location_Kind", "(\"Kind\" IN ('warehouse', 'service-point', 'quarantine') AND \"ServicePointId\" IS NOT NULL AND \"VehicleId\" IS NULL) OR (\"Kind\" = 'vehicle' AND \"VehicleId\" IS NOT NULL AND \"ServicePointId\" IS NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Location_Version", "\"Version\" > 0"));
        });

        model.Entity<Vehicle>(entity =>
        {
            Common(entity, "Vehicle");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RegistrationCiphertext).HasColumnType("bytea").IsRequired();
            entity.Property(e => e.MaxWeight).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.MaxVolume).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Vehicle_MaxWeight", "\"MaxWeight\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Vehicle_MaxVolume", "\"MaxVolume\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Vehicle_Version", "\"Version\" > 0"));
        });

        model.Entity<VehicleAvailability>(entity =>
        {
            Common(entity, "VehicleAvailability");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.VehicleId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.VehicleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.AvailableFromUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.AvailableToUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.DepotLocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DepotLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_VehicleAvailability_Interval", "\"AvailableToUtc\" IS NULL OR \"AvailableFromUtc\" < \"AvailableToUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_VehicleAvailability_Version", "\"Version\" > 0"));
        });

        model.Entity<VehiclePosition>(entity =>
        {
            Common(entity, "VehiclePosition");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.VehicleId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.VehicleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoutePlanId).HasColumnType("uuid");
            entity.HasOne<RoutePlan>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoutePlanId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RecordedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.RecordedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OccurredAtDevice).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.ReceivedAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.Position).HasColumnType("geography(Point,4326)").IsRequired();
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.RecordedByAccountId, e.OperationId }).IsUnique();
            entity.HasIndex(e => e.Position).HasMethod("gist");
            entity.HasIndex(e => new { e.OrganizationId, e.VehicleId, e.ReceivedAtServer });
            entity.ToTable(t => t.HasCheckConstraint("CK_VehiclePosition_Version", "\"Version\" > 0"));
        });

        model.Entity<Product>(entity =>
        {
            Common(entity, "Product");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Sku).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.BaseUnit).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.WeightPerBaseUnit).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.VolumePerBaseUnit).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.Sku }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Product_WeightPerBaseUnit", "\"WeightPerBaseUnit\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Product_VolumePerBaseUnit", "\"VolumePerBaseUnit\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Product_Version", "\"Version\" > 0"));
        });

        model.Entity<ProductUnitConversion>(entity =>
        {
            Common(entity, "ProductUnitConversion");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ProductId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Product>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ProductId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PackagingCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.BaseQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.PackagingCode }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ProductUnitConversion_BaseQuantity", "\"BaseQuantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ProductUnitConversion_Version", "\"Version\" > 0"));
        });

        model.Entity<Lot>(entity =>
        {
            Common(entity, "Lot");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ProductId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Product>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ProductId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Origin).HasColumnType("character varying(64)").HasMaxLength(64);
            entity.Property(e => e.ExpiresOn).HasColumnType("date");
            entity.Property(e => e.ConditionOptionId).HasColumnType("uuid");
            entity.HasOne<CatalogOption>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConditionOptionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.IsBlocked).HasColumnType("boolean").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Lot_Version", "\"Version\" > 0"));
        });

        model.Entity<HandlingUnit>(entity =>
        {
            Common(entity, "HandlingUnit");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.LocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.SealedAtUtc).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_HandlingUnit_Version", "\"Version\" > 0"));
        });

        model.Entity<HandlingUnitLine>(entity =>
        {
            Common(entity, "HandlingUnitLine");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.HandlingUnitId).HasColumnType("uuid").IsRequired();
            entity.HasOne<HandlingUnit>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.HandlingUnitId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Lot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.HandlingUnitId, e.LotId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_HandlingUnitLine_Quantity", "\"Quantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_HandlingUnitLine_Version", "\"Version\" > 0"));
        });

        model.Entity<InventoryMovement>(entity =>
        {
            Common(entity, "InventoryMovement");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RecordedSequence).HasColumnType("bigint").IsRequired().UseIdentityAlwaysColumn();
            entity.Property(e => e.Kind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.LotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Lot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.FromLocationId).HasColumnType("uuid");
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.FromLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ToLocationId).HasColumnType("uuid");
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ToLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.ActorAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.OccurredAtDevice).HasColumnType("timestamp with time zone");
            entity.Property(e => e.ReceivedAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.DeliveryLineId).HasColumnType("uuid");
            entity.HasOne<DeliveryLine>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryLineId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReceiptLineId).HasColumnType("uuid");
            entity.HasOne<ReceiptLine>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReceiptLineId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConsumptionId).HasColumnType("uuid");
            entity.HasOne<Consumption>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConsumptionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.RecordedSequence }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.LotId, e.ReceivedAtServer });
            entity.ToTable(t => t.HasCheckConstraint("CK_InventoryMovement_Direction", "(\"Kind\" = 'inbound' AND \"FromLocationId\" IS NULL AND \"ToLocationId\" IS NOT NULL) OR (\"Kind\" IN ('outbound', 'consumption', 'loss') AND \"FromLocationId\" IS NOT NULL AND \"ToLocationId\" IS NULL) OR (\"Kind\" = 'transfer' AND \"FromLocationId\" IS NOT NULL AND \"ToLocationId\" IS NOT NULL AND \"FromLocationId\" <> \"ToLocationId\")"));
            entity.ToTable(t => t.HasCheckConstraint("CK_InventoryMovement_Quantity", "\"Quantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_InventoryMovement_Version", "\"Version\" > 0"));
        });

        model.Entity<StockBalance>(entity =>
        {
            Common(entity, "StockBalance");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Lot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.QuantityOnHand).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.QuantityReserved).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.LastMovementSequence).HasColumnType("bigint").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.LocationId, e.LotId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_StockBalance_Reserved", "\"QuantityReserved\" <= \"QuantityOnHand\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_StockBalance_QuantityOnHand", "\"QuantityOnHand\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_StockBalance_QuantityReserved", "\"QuantityReserved\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_StockBalance_Version", "\"Version\" > 0"));
        });

        model.Entity<InventorySnapshot>(entity =>
        {
            Common(entity, "InventorySnapshot");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CutoffUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.LastMovementSequence).HasColumnType("bigint").IsRequired();
            entity.Property(e => e.CreatedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_InventorySnapshot_Version", "\"Version\" > 0"));
        });

        model.Entity<InventorySnapshotLine>(entity =>
        {
            Common(entity, "InventorySnapshotLine");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.InventorySnapshotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<InventorySnapshot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.InventorySnapshotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Lot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.QuantityOnHand).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.QuantityReserved).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.BalanceVersion).HasColumnType("bigint").IsRequired();
            entity.Property(e => e.LotConditionCode).HasColumnType("character varying(64)").HasMaxLength(64);
            entity.Property(e => e.WasLotBlocked).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.LotExpiresOn).HasColumnType("date");
            entity.HasIndex(e => new { e.OrganizationId, e.InventorySnapshotId, e.LocationId, e.LotId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_InventorySnapshotLine_Reserved", "\"QuantityReserved\" <= \"QuantityOnHand\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_InventorySnapshotLine_QuantityOnHand", "\"QuantityOnHand\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_InventorySnapshotLine_QuantityReserved", "\"QuantityReserved\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_InventorySnapshotLine_Version", "\"Version\" > 0"));
        });

        model.Entity<Reservation>(entity =>
        {
            Common(entity, "Reservation");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryLineId).HasColumnType("uuid").IsRequired();
            entity.HasOne<DeliveryLine>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryLineId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Lot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.ConsumedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.ReleasedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_Reservation_Remaining", "\"ConsumedQuantity\" + \"ReleasedQuantity\" <= \"Quantity\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_Reservation_Quantity", "\"Quantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Reservation_ConsumedQuantity", "\"ConsumedQuantity\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Reservation_ReleasedQuantity", "\"ReleasedQuantity\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Reservation_Version", "\"Version\" > 0"));
        });

        model.Entity<Consumption>(entity =>
        {
            Common(entity, "Consumption");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Lot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.SourceKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ActorAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OccurredAtDevice).HasColumnType("timestamp with time zone");
            entity.Property(e => e.ReceivedAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.ActorAccountId, e.OperationId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Consumption_Quantity", "\"Quantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Consumption_Version", "\"Version\" > 0"));
        });

        model.Entity<DemandObservation>(entity =>
        {
            Common(entity, "DemandObservation");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ProductId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Product>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ProductId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PeriodFromUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.PeriodToUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.RequestedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.ObservedConsumption).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.SourceKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RecordedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.ServicePointId, e.ProductId, e.PeriodFromUtc, e.PeriodToUtc, e.SourceKind }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_DemandObservation_Interval", "\"PeriodToUtc\" IS NULL OR \"PeriodFromUtc\" < \"PeriodToUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_DemandObservation_RequestedQuantity", "\"RequestedQuantity\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_DemandObservation_ObservedConsumption", "\"ObservedConsumption\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_DemandObservation_Version", "\"Version\" > 0"));
        });

        model.Entity<ReplenishmentRequest>(entity =>
        {
            Common(entity, "ReplenishmentRequest");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RequestedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.NeededByUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.WorkflowVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StateId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StateId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConfigurationVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ConfigurationVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConfigurationVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_ReplenishmentRequest_Version", "\"Version\" > 0"));
        });

        model.Entity<ReplenishmentRequestLine>(entity =>
        {
            Common(entity, "ReplenishmentRequestLine");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReplenishmentRequestId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ReplenishmentRequest>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReplenishmentRequestId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ProductId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Product>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ProductId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RequestedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.AllocatedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.ReplenishmentRequestId, e.ProductId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ReplenishmentRequestLine_Allocation", "\"AllocatedQuantity\" <= \"RequestedQuantity\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_ReplenishmentRequestLine_RequestedQuantity", "\"RequestedQuantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ReplenishmentRequestLine_AllocatedQuantity", "\"AllocatedQuantity\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ReplenishmentRequestLine_Version", "\"Version\" > 0"));
        });

        model.Entity<RoadMatrix>(entity =>
        {
            Common(entity, "RoadMatrix");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ProviderCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.DatasetVersionId).HasColumnType("uuid");
            entity.HasOne<ScenarioDatasetVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DatasetVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RequestedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.RetrievedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ConfigurationVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ConfigurationVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConfigurationVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_RoadMatrix_Version", "\"Version\" > 0"));
        });

        model.Entity<RoadMatrixEdge>(entity =>
        {
            Common(entity, "RoadMatrixEdge");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoadMatrixId).HasColumnType("uuid").IsRequired();
            entity.HasOne<RoadMatrix>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoadMatrixId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.FromLocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.FromLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ToLocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ToLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DistanceKm).HasColumnType("numeric(18,6)");
            entity.Property(e => e.DurationSeconds).HasColumnType("integer");
            entity.Property(e => e.IsReachable).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.UnreachableReason).HasColumnType("character varying(2000)").HasMaxLength(2000);
            entity.HasIndex(e => new { e.OrganizationId, e.RoadMatrixId, e.FromLocationId, e.ToLocationId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_RoadMatrixEdge_Reachable", "(\"IsReachable\" AND \"DistanceKm\" IS NOT NULL AND \"DurationSeconds\" IS NOT NULL AND \"UnreachableReason\" IS NULL) OR (NOT \"IsReachable\" AND \"DistanceKm\" IS NULL AND \"DurationSeconds\" IS NULL AND \"UnreachableReason\" IS NOT NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RoadMatrixEdge_DistanceKm", "\"DistanceKm\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RoadMatrixEdge_DurationSeconds", "\"DurationSeconds\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RoadMatrixEdge_Version", "\"Version\" > 0"));
        });

        model.Entity<RoutePlan>(entity =>
        {
            Common(entity, "RoutePlan");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Revision).HasColumnType("integer").IsRequired();
            entity.Property(e => e.SupersedesRoutePlanId).HasColumnType("uuid");
            entity.HasOne<RoutePlan>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SupersedesRoutePlanId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.VehicleId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Vehicle>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.VehicleId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoadMatrixId).HasColumnType("uuid").IsRequired();
            entity.HasOne<RoadMatrix>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoadMatrixId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConfigurationVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ConfigurationVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConfigurationVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.WorkflowVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StateId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StateId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PlannedStartUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.PlannedEndUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.PublishedByAccountId).HasColumnType("uuid");
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.PublishedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.Code, e.Revision }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_RoutePlan_Interval", "\"PlannedEndUtc\" IS NULL OR \"PlannedStartUtc\" < \"PlannedEndUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_RoutePlan_Revision", "\"Revision\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RoutePlan_Version", "\"Version\" > 0"));
        });

        model.Entity<RoutePlanningSnapshot>(entity =>
        {
            Common(entity, "RoutePlanningSnapshot");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoutePlanId).HasColumnType("uuid").IsRequired();
            entity.HasOne<RoutePlan>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoutePlanId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.InputJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.InputHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.AlgorithmCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.AlgorithmVersion).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.CutoffUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.RoutePlanId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_RoutePlanningSnapshot_Version", "\"Version\" > 0"));
        });

        model.Entity<RouteStop>(entity =>
        {
            Common(entity, "RouteStop");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoutePlanId).HasColumnType("uuid").IsRequired();
            entity.HasOne<RoutePlan>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoutePlanId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SequenceNumber).HasColumnType("integer").IsRequired();
            entity.Property(e => e.PlannedArrivalUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.PlannedDepartureUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.ActualArrivalUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.ActualDepartureUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PendingReason).HasColumnType("character varying(2000)").HasMaxLength(2000);
            entity.HasIndex(e => new { e.OrganizationId, e.RoutePlanId, e.SequenceNumber }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_RouteStop_Interval", "\"PlannedDepartureUtc\" IS NULL OR \"PlannedArrivalUtc\" < \"PlannedDepartureUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_RouteStop_SequenceNumber", "\"SequenceNumber\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RouteStop_Version", "\"Version\" > 0"));
        });

        model.Entity<Delivery>(entity =>
        {
            Common(entity, "Delivery");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RouteStopId).HasColumnType("uuid");
            entity.HasOne<RouteStop>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RouteStopId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OriginLocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.OriginLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DestinationLocationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DestinationLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.WorkflowVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StateId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StateId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DispatchedAtUtc).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Delivery_Locations", "\"OriginLocationId\" <> \"DestinationLocationId\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_Delivery_Version", "\"Version\" > 0"));
        });

        model.Entity<DeliveryLine>(entity =>
        {
            Common(entity, "DeliveryLine");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Delivery>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReplenishmentRequestLineId).HasColumnType("uuid");
            entity.HasOne<ReplenishmentRequestLine>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReplenishmentRequestLineId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.LotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Lot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.PlannedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.DispatchedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.AcceptedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.RejectedQuantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.PackagingSnapshotJson).HasColumnType("jsonb").IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryLine_Totals", "\"AcceptedQuantity\" + \"RejectedQuantity\" <= \"DispatchedQuantity\" AND \"DispatchedQuantity\" <= \"PlannedQuantity\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryLine_PlannedQuantity", "\"PlannedQuantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryLine_DispatchedQuantity", "\"DispatchedQuantity\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryLine_AcceptedQuantity", "\"AcceptedQuantity\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryLine_RejectedQuantity", "\"RejectedQuantity\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryLine_Version", "\"Version\" > 0"));
        });

        model.Entity<DeliveryHandlingUnit>(entity =>
        {
            Common(entity, "DeliveryHandlingUnit");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Delivery>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.HandlingUnitId).HasColumnType("uuid").IsRequired();
            entity.HasOne<HandlingUnit>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.HandlingUnitId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReleasedAtUtc).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.OrganizationId, e.HandlingUnitId }).IsUnique().HasFilter("\"ReleasedAtUtc\" IS NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryHandlingUnit_Version", "\"Version\" > 0"));
        });

        model.Entity<DeliveryHandlingUnitLine>(entity =>
        {
            Common(entity, "DeliveryHandlingUnitLine");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryHandlingUnitId).HasColumnType("uuid").IsRequired();
            entity.HasOne<DeliveryHandlingUnit>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryHandlingUnitId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryLineId).HasColumnType("uuid").IsRequired();
            entity.HasOne<DeliveryLine>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryLineId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.HandlingUnitLineId).HasColumnType("uuid").IsRequired();
            entity.HasOne<HandlingUnitLine>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.HandlingUnitLineId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)").IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryHandlingUnitLine_Quantity", "\"Quantity\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryHandlingUnitLine_Version", "\"Version\" > 0"));
        });

        model.Entity<DeliveryAssignment>(entity =>
        {
            Common(entity, "DeliveryAssignment");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Delivery>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OrganizationAccessId).HasColumnType("uuid").IsRequired();
            entity.HasOne<OrganizationAccess>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.OrganizationAccessId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.AssignmentKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.AssignedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.AssignedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.AssignedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.RevokedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.DeliveryId, e.OrganizationAccessId, e.AssignmentKind }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryAssignment_Kind", "\"AssignmentKind\" IN ('driver', 'receiver')"));
            entity.ToTable(t => t.HasCheckConstraint("CK_DeliveryAssignment_Version", "\"Version\" > 0"));
        });

        model.Entity<Receipt>(entity =>
        {
            Common(entity, "Receipt");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Delivery>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ActorAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.WorkflowVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StateId).HasColumnType("uuid").IsRequired();
            entity.HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StateId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.OccurredAtDevice).HasColumnType("timestamp with time zone");
            entity.Property(e => e.ReceivedAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.AcceptedAtServer).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.OrganizationId, e.ActorAccountId, e.OperationId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Receipt_Version", "\"Version\" > 0"));
        });

        model.Entity<ReceiptLine>(entity =>
        {
            Common(entity, "ReceiptLine");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReceiptId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Receipt>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReceiptId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryLineId).HasColumnType("uuid").IsRequired();
            entity.HasOne<DeliveryLine>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryLineId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.QuantityAccepted).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.QuantityRejected).HasColumnType("numeric(18,4)").IsRequired();
            entity.Property(e => e.RejectionReasonOptionId).HasColumnType("uuid");
            entity.HasOne<CatalogOption>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RejectionReasonOptionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReturnLocationId).HasColumnType("uuid");
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReturnLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.ReceiptId, e.DeliveryLineId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ReceiptLine_Quantity", "\"QuantityAccepted\" + \"QuantityRejected\" > 0 AND (\"QuantityRejected\" = 0 OR (\"RejectionReasonOptionId\" IS NOT NULL AND \"ReturnLocationId\" IS NOT NULL))"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ReceiptLine_QuantityAccepted", "\"QuantityAccepted\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ReceiptLine_QuantityRejected", "\"QuantityRejected\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ReceiptLine_Version", "\"Version\" > 0"));
        });

        model.Entity<StoredFile>(entity =>
        {
            Common(entity, "StoredFile");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StorageKey).HasColumnType("character varying(512)").HasMaxLength(512).IsRequired();
            entity.Property(e => e.Kind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.DeclaredMimeType).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.VerifiedMimeType).HasColumnType("character varying(64)").HasMaxLength(64);
            entity.Property(e => e.ByteSize).HasColumnType("bigint").IsRequired();
            entity.Property(e => e.Sha256).HasColumnType("character varying(64)").HasMaxLength(64);
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.EncryptionKeyReference).HasColumnType("character varying(512)").HasMaxLength(512);
            entity.Property(e => e.CreatedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ExpiresAtUtc).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.OrganizationId, e.StorageKey }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_StoredFile_ByteSize", "\"ByteSize\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_StoredFile_Version", "\"Version\" > 0"));
        });

        model.Entity<Evidence>(entity =>
        {
            Common(entity, "Evidence");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReceiptId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Receipt>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReceiptId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReceiptLineId).HasColumnType("uuid");
            entity.HasOne<ReceiptLine>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReceiptLineId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StoredFileId).HasColumnType("uuid").IsRequired();
            entity.HasOne<StoredFile>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StoredFileId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Kind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.UploadedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.UploadedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConfirmedAtUtc).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.OrganizationId, e.ReceiptId, e.StoredFileId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Evidence_Version", "\"Version\" > 0"));
        });

        model.Entity<CustodyEvent>(entity =>
        {
            Common(entity, "CustodyEvent");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.HandlingUnitId).HasColumnType("uuid").IsRequired();
            entity.HasOne<HandlingUnit>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.HandlingUnitId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryId).HasColumnType("uuid");
            entity.HasOne<Delivery>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReceiptId).HasColumnType("uuid");
            entity.HasOne<Receipt>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ReceiptId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.FromLocationId).HasColumnType("uuid");
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.FromLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ToLocationId).HasColumnType("uuid");
            entity.HasOne<Location>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ToLocationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ActorAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.EventKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.OccurredAtDevice).HasColumnType("timestamp with time zone");
            entity.Property(e => e.ReceivedAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_CustodyEvent_Version", "\"Version\" > 0"));
        });

        model.Entity<QrAccessToken>(entity =>
        {
            Common(entity, "QrAccessToken");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.HandlingUnitId).HasColumnType("uuid").IsRequired();
            entity.HasOne<HandlingUnit>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.HandlingUnitId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TokenHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ScopeCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ExpiresAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.RevokedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CreatedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.TokenHash }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_QrAccessToken_Version", "\"Version\" > 0"));
        });

        model.Entity<RiskCalculationRun>(entity =>
        {
            Common(entity, "RiskCalculationRun");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.JobExecutionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<JobExecution>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.JobExecutionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.InventorySnapshotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<InventorySnapshot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.InventorySnapshotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConfigurationVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ConfigurationVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConfigurationVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.FormulaCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.FormulaVersion).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.CutoffUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.InputHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_RiskCalculationRun_Version", "\"Version\" > 0"));
        });

        model.Entity<RiskSnapshot>(entity =>
        {
            Common(entity, "RiskSnapshot");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RiskCalculationRunId).HasColumnType("uuid").IsRequired();
            entity.HasOne<RiskCalculationRun>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RiskCalculationRunId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ProductId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Product>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ProductId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CoverageDays).HasColumnType("numeric(18,6)");
            entity.Property(e => e.LeadTimeDays).HasColumnType("numeric(18,6)");
            entity.Property(e => e.SafetyMarginDays).HasColumnType("numeric(18,6)");
            entity.Property(e => e.RiskCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.InputSnapshotJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.UncertaintyJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.OrganizationId, e.RiskCalculationRunId, e.ServicePointId, e.ProductId }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.ServicePointId, e.ProductId, e.RiskCalculationRunId });
            entity.ToTable(t => t.HasCheckConstraint("CK_RiskSnapshot_CoverageDays", "\"CoverageDays\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RiskSnapshot_LeadTimeDays", "\"LeadTimeDays\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RiskSnapshot_SafetyMarginDays", "\"SafetyMarginDays\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_RiskSnapshot_Version", "\"Version\" > 0"));
        });

        model.Entity<RiskExplanation>(entity =>
        {
            Common(entity, "RiskExplanation");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RiskSnapshotId).HasColumnType("uuid").IsRequired();
            entity.HasOne<RiskSnapshot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RiskSnapshotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.IntegrationRunId).HasColumnType("uuid");
            entity.HasOne<IntegrationRun>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.IntegrationRunId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TemplateVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<TemplateVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.TemplateVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ExplanationText).HasColumnType("character varying(2000)").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.RecommendedAction).HasColumnType("character varying(2000)").HasMaxLength(2000);
            entity.Property(e => e.ProviderCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ModelCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_RiskExplanation_Version", "\"Version\" > 0"));
        });

        model.Entity<AnomalyCase>(entity =>
        {
            Common(entity, "AnomalyCase");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ProductId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Product>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ProductId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RiskSnapshotId).HasColumnType("uuid");
            entity.HasOne<RiskSnapshot>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RiskSnapshotId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.DetectedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.ClosedByAccountId).HasColumnType("uuid");
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ClosedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Resolution).HasColumnType("character varying(2000)").HasMaxLength(2000);
            entity.Property(e => e.LossMovementId).HasColumnType("uuid");
            entity.HasOne<InventoryMovement>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.LossMovementId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_AnomalyCase_Version", "\"Version\" > 0"));
        });

        model.Entity<Incident>(entity =>
        {
            Common(entity, "Incident");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ServicePointId).HasColumnType("uuid");
            entity.HasOne<ServicePoint>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ServicePointId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoutePlanId).HasColumnType("uuid");
            entity.HasOne<RoutePlan>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoutePlanId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ReportedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ReportedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SeverityOptionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<CatalogOption>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SeverityOptionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Position).HasColumnType("geography(Point,4326)");
            entity.Property(e => e.IsSynthetic).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.ScenarioEventId).HasColumnType("uuid");
            entity.HasOne<ScenarioEvent>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ScenarioEventId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.OccurredAtDevice).HasColumnType("timestamp with time zone");
            entity.Property(e => e.ReceivedAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.HasIndex(e => e.Position).HasMethod("gist");
            entity.ToTable(t => t.HasCheckConstraint("CK_Incident_Version", "\"Version\" > 0"));
        });

        model.Entity<IncidentRoadRestriction>(entity =>
        {
            Common(entity, "IncidentRoadRestriction");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.IncidentId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Incident>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.IncidentId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoadMatrixEdgeId).HasColumnType("uuid").IsRequired();
            entity.HasOne<RoadMatrixEdge>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoadMatrixEdgeId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.EffectiveFromUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.EffectiveToUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.IsClosed).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.AddedDurationSeconds).HasColumnType("integer").IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_IncidentRoadRestriction_Interval", "\"EffectiveToUtc\" IS NULL OR \"EffectiveFromUtc\" < \"EffectiveToUtc\""));
            entity.ToTable(t => t.HasCheckConstraint("CK_IncidentRoadRestriction_AddedDurationSeconds", "\"AddedDurationSeconds\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_IncidentRoadRestriction_Version", "\"Version\" > 0"));
        });

        model.Entity<RoutePlanIncident>(entity =>
        {
            Common(entity, "RoutePlanIncident");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoutePlanId).HasColumnType("uuid").IsRequired();
            entity.HasOne<RoutePlan>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoutePlanId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.IncidentId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Incident>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.IncidentId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.RoutePlanId, e.IncidentId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_RoutePlanIncident_Version", "\"Version\" > 0"));
        });

        model.Entity<IntegrationConnection>(entity =>
        {
            Common(entity, "IntegrationConnection");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ProviderCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.EndpointReference).HasColumnType("character varying(512)").HasMaxLength(512).IsRequired();
            entity.Property(e => e.SecretReference).HasColumnType("character varying(512)").HasMaxLength(512).IsRequired();
            entity.Property(e => e.IsActive).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.ConfigurationVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ConfigurationVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConfigurationVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_IntegrationConnection_Version", "\"Version\" > 0"));
        });

        model.Entity<JobExecution>(entity =>
        {
            Common(entity, "JobExecution");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Kind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RequestedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RequestedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.StartedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CompletedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Progress).HasColumnType("integer").IsRequired();
            entity.Property(e => e.ErrorCode).HasColumnType("character varying(64)").HasMaxLength(64);
            entity.HasIndex(e => new { e.OrganizationId, e.RequestedByAccountId, e.OperationId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_JobExecution_Progress", "\"Progress\" BETWEEN 0 AND 100"));
            entity.ToTable(t => t.HasCheckConstraint("CK_JobExecution_Version", "\"Version\" > 0"));
        });

        model.Entity<IntegrationRun>(entity =>
        {
            Common(entity, "IntegrationRun");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.IntegrationConnectionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<IntegrationConnection>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.IntegrationConnectionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.JobExecutionId).HasColumnType("uuid");
            entity.HasOne<JobExecution>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.JobExecutionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RunKey).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ProviderExecutionId).HasColumnType("character varying(512)").HasMaxLength(512);
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Attempts).HasColumnType("integer").IsRequired();
            entity.Property(e => e.TokenCount).HasColumnType("integer");
            entity.Property(e => e.EstimatedCost).HasColumnType("numeric(18,2)");
            entity.Property(e => e.Currency).HasColumnType("character varying(3)").HasMaxLength(3);
            entity.Property(e => e.ErrorCode).HasColumnType("character varying(64)").HasMaxLength(64);
            entity.HasIndex(e => new { e.OrganizationId, e.IntegrationConnectionId, e.RunKey }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_IntegrationRun_Attempts", "\"Attempts\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_IntegrationRun_TokenCount", "\"TokenCount\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_IntegrationRun_EstimatedCost", "\"EstimatedCost\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_IntegrationRun_Version", "\"Version\" > 0"));
        });

        model.Entity<Conversation>(entity =>
        {
            Common(entity, "Conversation");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeliveryId).HasColumnType("uuid");
            entity.HasOne<Delivery>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.DeliveryId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoutePlanId).HasColumnType("uuid");
            entity.HasOne<RoutePlan>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoutePlanId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.CreatedByAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_Conversation_Context", "num_nonnulls(\"DeliveryId\", \"RoutePlanId\") <= 1"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Conversation_Version", "\"Version\" > 0"));
        });

        model.Entity<ConversationParticipant>(entity =>
        {
            Common(entity, "ConversationParticipant");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConversationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Conversation>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConversationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OrganizationAccessId).HasColumnType("uuid").IsRequired();
            entity.HasOne<OrganizationAccess>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.OrganizationAccessId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.JoinedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.RevokedAtUtc).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.OrganizationId, e.ConversationId, e.OrganizationAccessId }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_ConversationParticipant_Version", "\"Version\" > 0"));
        });

        model.Entity<Message>(entity =>
        {
            Common(entity, "Message");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConversationId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Conversation>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConversationId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.AuthorAccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.AuthorAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.BodyText).HasColumnType("character varying(4000)").HasMaxLength(4000).IsRequired();
            entity.Property(e => e.ReceivedAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.RedactedAtUtc).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => new { e.OrganizationId, e.AuthorAccountId, e.OperationId }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.ConversationId, e.ReceivedAtServer, e.Id });
            entity.ToTable(t => t.HasCheckConstraint("CK_Message_Version", "\"Version\" > 0"));
        });

        model.Entity<MessageAttachment>(entity =>
        {
            Common(entity, "MessageAttachment");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.MessageId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Message>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.MessageId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StoredFileId).HasColumnType("uuid").IsRequired();
            entity.HasOne<StoredFile>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StoredFileId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.MessageId, e.StoredFileId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_MessageAttachment_Version", "\"Version\" > 0"));
        });

        model.Entity<Notification>(entity =>
        {
            Common(entity, "Notification");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RecipientAccessId).HasColumnType("uuid").IsRequired();
            entity.HasOne<OrganizationAccess>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RecipientAccessId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.EventType).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ReadAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.SourceEventId).HasColumnType("uuid");
            entity.HasOne<OutboxEvent>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SourceEventId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.RecipientAccessId, e.SourceEventId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Notification_Version", "\"Version\" > 0"));
        });

        model.Entity<IdempotencyRecord>(entity =>
        {
            Common(entity, "IdempotencyRecord");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.AccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.ActionCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RequestHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ResponseStatus).HasColumnType("integer");
            entity.Property(e => e.ResponseJson).HasColumnType("jsonb");
            entity.Property(e => e.CompletedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.RetainUntilUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.AccountId, e.ActionCode, e.OperationId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_IdempotencyRecord_Version", "\"Version\" > 0"));
        });

        model.Entity<OutboxEvent>(entity =>
        {
            Common(entity, "OutboxEvent");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.EventType).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.SchemaVersion).HasColumnType("integer").IsRequired();
            entity.Property(e => e.AggregateId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.AggregateType).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.AggregateVersion).HasColumnType("bigint").IsRequired();
            entity.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.OccurredAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.PublishedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Attempts).HasColumnType("integer").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAtUtc }).HasFilter("\"PublishedAtUtc\" IS NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_OutboxEvent_Attempts", "\"Attempts\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_OutboxEvent_Version", "\"Version\" > 0"));
        });

        model.Entity<AuditEvent>(entity =>
        {
            Common(entity, "AuditEvent");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ActorAccountId).HasColumnType("uuid");
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ActionCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ResourceType).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ResourceId).HasColumnType("uuid");
            entity.Property(e => e.ResultCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Reason).HasColumnType("character varying(2000)").HasMaxLength(2000);
            entity.Property(e => e.CorrelationId).HasColumnType("character varying(128)").HasMaxLength(128).IsRequired();
            entity.Property(e => e.BeforeJson).HasColumnType("jsonb");
            entity.Property(e => e.AfterJson).HasColumnType("jsonb");
            entity.Property(e => e.RecordedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_AuditEvent_Version", "\"Version\" > 0"));
        });

        model.Entity<SyncBatch>(entity =>
        {
            Common(entity, "SyncBatch");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.AccountId).HasColumnType("uuid").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.DeviceReference).HasColumnType("character varying(512)").HasMaxLength(512).IsRequired();
            entity.Property(e => e.OperationId).HasColumnType("uuid").IsRequired();
            entity.Property(e => e.ReceivedAtServer).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.AccountId, e.OperationId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_SyncBatch_Version", "\"Version\" > 0"));
        });

        model.Entity<SyncOperation>(entity =>
        {
            Common(entity, "SyncOperation");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SyncBatchId).HasColumnType("uuid").IsRequired();
            entity.HasOne<SyncBatch>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SyncBatchId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.IdempotencyRecordId).HasColumnType("uuid").IsRequired();
            entity.HasOne<IdempotencyRecord>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.IdempotencyRecordId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ActionCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ClientOccurredAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.ResultCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.SyncBatchId, e.IdempotencyRecordId }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_SyncOperation_Version", "\"Version\" > 0"));
        });

        model.Entity<ScenarioDataset>(entity =>
        {
            Common(entity, "ScenarioDataset");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnType("character varying(200)").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsSynthetic).HasColumnType("boolean").IsRequired();
            entity.Property(e => e.Description).HasColumnType("character varying(2000)").HasMaxLength(2000);
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ScenarioDataset_Version", "\"Version\" > 0"));
        });

        model.Entity<ScenarioDatasetVersion>(entity =>
        {
            Common(entity, "ScenarioDatasetVersion");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ScenarioDatasetId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ScenarioDataset>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ScenarioDatasetId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Revision).HasColumnType("integer").IsRequired();
            entity.Property(e => e.Seed).HasColumnType("integer").IsRequired();
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ManifestJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.StoredFileId).HasColumnType("uuid");
            entity.HasOne<StoredFile>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StoredFileId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.ScenarioDatasetId, e.Revision }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ScenarioDatasetVersion_Revision", "\"Revision\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ScenarioDatasetVersion_Version", "\"Version\" > 0"));
        });

        model.Entity<ScenarioEvent>(entity =>
        {
            Common(entity, "ScenarioEvent");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ScenarioDatasetVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ScenarioDatasetVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ScenarioDatasetVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SequenceNumber).HasColumnType("integer").IsRequired();
            entity.Property(e => e.SimulationTime).HasColumnType("bigint").IsRequired();
            entity.Property(e => e.EventKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.ScenarioDatasetVersionId, e.SequenceNumber }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_ScenarioEvent_SequenceNumber", "\"SequenceNumber\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_ScenarioEvent_Version", "\"Version\" > 0"));
        });

        model.Entity<SimulationPolicyVersion>(entity =>
        {
            Common(entity, "SimulationPolicyVersion");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Revision).HasColumnType("integer").IsRequired();
            entity.Property(e => e.HandlerCode).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ParametersJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ConfigurationVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ConfigurationVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ConfigurationVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.OrganizationId, e.Code, e.Revision }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationPolicyVersion_Revision", "\"Revision\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationPolicyVersion_Version", "\"Version\" > 0"));
        });

        model.Entity<SimulationRun>(entity =>
        {
            Common(entity, "SimulationRun");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.JobExecutionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<JobExecution>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.JobExecutionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ScenarioDatasetVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<ScenarioDatasetVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ScenarioDatasetVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SimulationPolicyVersionId).HasColumnType("uuid").IsRequired();
            entity.HasOne<SimulationPolicyVersion>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SimulationPolicyVersionId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RoadMatrixId).HasColumnType("uuid");
            entity.HasOne<RoadMatrix>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.RoadMatrixId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Seed).HasColumnType("integer").IsRequired();
            entity.Property(e => e.StartedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(e => e.CompletedAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(e => e.Status).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.InputHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationRun_Version", "\"Version\" > 0"));
        });

        model.Entity<SimulationEvent>(entity =>
        {
            Common(entity, "SimulationEvent");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SimulationRunId).HasColumnType("uuid").IsRequired();
            entity.HasOne<SimulationRun>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SimulationRunId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SequenceNumber).HasColumnType("integer").IsRequired();
            entity.Property(e => e.SimulationTime).HasColumnType("bigint").IsRequired();
            entity.Property(e => e.EventKind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.SimulationRunId, e.SequenceNumber }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationEvent_SequenceNumber", "\"SequenceNumber\" > 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationEvent_Version", "\"Version\" > 0"));
        });

        model.Entity<SimulationMetric>(entity =>
        {
            Common(entity, "SimulationMetric");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SimulationRunId).HasColumnType("uuid").IsRequired();
            entity.HasOne<SimulationRun>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SimulationRunId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Code).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.DefinitionVersion).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ProductId).HasColumnType("uuid");
            entity.HasOne<Product>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.ProductId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Numerator).HasColumnType("numeric(18,6)").IsRequired();
            entity.Property(e => e.Denominator).HasColumnType("numeric(18,6)");
            entity.Property(e => e.Value).HasColumnType("numeric(18,6)");
            entity.Property(e => e.Unit).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.NotApplicableReason).HasColumnType("character varying(64)").HasMaxLength(64);
            entity.HasIndex(e => new { e.OrganizationId, e.SimulationRunId, e.Code, e.DefinitionVersion, e.ProductId }).IsUnique().HasFilter("\"ProductId\" IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.SimulationRunId, e.Code, e.DefinitionVersion }).IsUnique().HasFilter("\"ProductId\" IS NULL");
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationMetric_Division", "(\"Denominator\" IS DISTINCT FROM 0 AND \"Value\" IS NOT NULL AND \"NotApplicableReason\" IS NULL) OR (\"Value\" IS NULL AND \"NotApplicableReason\" IS NOT NULL)"));
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationMetric_Denominator", "\"Denominator\" >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationMetric_Version", "\"Version\" > 0"));
        });

        model.Entity<SimulationArtifact>(entity =>
        {
            Common(entity, "SimulationArtifact");
            entity.HasAlternateKey(e => new { e.OrganizationId, e.Id });
            entity.HasOne<Organization>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.SimulationRunId).HasColumnType("uuid").IsRequired();
            entity.HasOne<SimulationRun>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.SimulationRunId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.StoredFileId).HasColumnType("uuid").IsRequired();
            entity.HasOne<StoredFile>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.StoredFileId }).HasPrincipalKey(e => new { e.OrganizationId, e.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Kind).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ContentHash).HasColumnType("character varying(64)").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.OrganizationId, e.SimulationRunId, e.Kind }).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_SimulationArtifact_Version", "\"Version\" > 0"));
        });

        model.Entity<WorkflowTransition>().HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.FromStateId }).HasPrincipalKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<WorkflowTransition>().HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.ToStateId }).HasPrincipalKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RoutePlan>().HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.StateId }).HasPrincipalKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ReplenishmentRequest>().HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.StateId }).HasPrincipalKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Delivery>().HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.StateId }).HasPrincipalKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.Id }).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Receipt>().HasOne<WorkflowState>().WithMany().HasForeignKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.StateId }).HasPrincipalKey(e => new { e.OrganizationId, e.WorkflowVersionId, e.Id }).OnDelete(DeleteBehavior.Restrict);
    }

    private static void Common<T>(EntityTypeBuilder<T> entity, string table) where T : BaseEntity
    {
        entity.ToTable(table);
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedNever();
        entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd().Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        entity.Property(e => e.UpdatedAtUtc).ValueGeneratedOnAddOrUpdate().Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        entity.Property(e => e.Version).HasDefaultValue(1L).IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
    }
}
