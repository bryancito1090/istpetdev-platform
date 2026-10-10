using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteSystemSchema95 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("Schema95Preflight.sql"));

            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_ServicePoints_DestinationServicePointId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_ServicePoints_OriginServicePointId",
                table: "Deliveries");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "RiskAssessments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicePoints",
                table: "ServicePoints");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Deliveries",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_DestinationServicePointId",
                table: "Deliveries");

            migrationBuilder.DropIndex(
                name: "IX_Deliveries_OriginServicePointId",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ServicePoints");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "ServicePoints");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ServicePoints");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ServicePoints");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "Deliveries");

            migrationBuilder.RenameTable(
                name: "ServicePoints",
                newName: "ServicePoint");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "Product");

            migrationBuilder.RenameTable(
                name: "Deliveries",
                newName: "Delivery");

            migrationBuilder.RenameColumn(
                name: "OriginServicePointId",
                table: "Delivery",
                newName: "WorkflowVersionId");

            migrationBuilder.RenameColumn(
                name: "DestinationServicePointId",
                table: "Delivery",
                newName: "StateId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ServicePoint",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ServicePoint",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfigurationVersionId",
                table: "ServicePoint",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Criticality",
                table: "ServicePoint",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ServicePoint",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Point>(
                name: "Position",
                table: "ServicePoint",
                type: "geography(Point,4326)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TypeOptionId",
                table: "ServicePoint",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ServicePoint",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Product",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Product",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "BaseUnit",
                table: "Product",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Product",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Product",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Product",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumePerBaseUnit",
                table: "Product",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightPerBaseUnit",
                table: "Product",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Delivery",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 1L,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Delivery",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Delivery",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationLocationId",
                table: "Delivery",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAtUtc",
                table: "Delivery",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Delivery",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OriginLocationId",
                table: "Delivery",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RouteStopId",
                table: "Delivery",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServicePointId",
                table: "Delivery",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ServicePoint_OrganizationId_Id",
                table: "ServicePoint",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicePoint",
                table: "ServicePoint",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Product_OrganizationId_Id",
                table: "Product",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Product",
                table: "Product",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Delivery_OrganizationId_Id",
                table: "Delivery",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Delivery",
                table: "Delivery",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                    table.CheckConstraint("CK_Account_Kind", "(\"Kind\" = 'human' AND \"Subject\" IS NOT NULL AND \"ClientId\" IS NULL) OR (\"Kind\" = 'technical' AND \"ClientId\" IS NOT NULL AND \"Subject\" IS NULL)");
                    table.CheckConstraint("CK_Account_Version", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValidationSchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    Sensitivity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AllowedScopes = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationDefinition", x => x.Id);
                    table.CheckConstraint("CK_ConfigurationDefinition_Version", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "FieldDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FieldCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DataType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsServerControlled = table.Column<bool>(type: "boolean", nullable: false),
                    Sensitivity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldDefinition", x => x.Id);
                    table.CheckConstraint("CK_FieldDefinition_Sensitivity", "\"Sensitivity\" IN ('public', 'internal', 'sensitive')");
                    table.CheckConstraint("CK_FieldDefinition_Version", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorizationVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                    table.CheckConstraint("CK_Organization_Version", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsDelegable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                    table.CheckConstraint("CK_Permission_Version", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "AuditEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvent", x => x.Id);
                    table.UniqueConstraint("AK_AuditEvent_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_AuditEvent_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_AuditEvent_Account_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvent_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog", x => x.Id);
                    table.UniqueConstraint("AK_Catalog_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Catalog_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Catalog_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    ValuesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationVersion", x => x.Id);
                    table.UniqueConstraint("AK_ConfigurationVersion_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ConfigurationVersion_Revision", "\"Revision\" > 0");
                    table.CheckConstraint("CK_ConfigurationVersion_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ConfigurationVersion_Account_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationVersion_ConfigurationDefinition_ConfigurationD~",
                        column: x => x.ConfigurationDefinitionId,
                        principalTable: "ConfigurationDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationVersion_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandObservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ObservedConsumption = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SourceKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandObservation", x => x.Id);
                    table.UniqueConstraint("AK_DemandObservation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_DemandObservation_Interval", "\"PeriodToUtc\" IS NULL OR \"PeriodFromUtc\" < \"PeriodToUtc\"");
                    table.CheckConstraint("CK_DemandObservation_ObservedConsumption", "\"ObservedConsumption\" >= 0");
                    table.CheckConstraint("CK_DemandObservation_RequestedQuantity", "\"RequestedQuantity\" >= 0");
                    table.CheckConstraint("CK_DemandObservation_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_DemandObservation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandObservation_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandObservation_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResponseStatus = table.Column<int>(type: "integer", nullable: true),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetainUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecord", x => x.Id);
                    table.UniqueConstraint("AK_IdempotencyRecord_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_IdempotencyRecord_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_IdempotencyRecord_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdempotencyRecord_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventorySnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CutoffUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMovementSequence = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySnapshot", x => x.Id);
                    table.UniqueConstraint("AK_InventorySnapshot_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_InventorySnapshot_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_InventorySnapshot_Account_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventorySnapshot_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobExecution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecution", x => x.Id);
                    table.UniqueConstraint("AK_JobExecution_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_JobExecution_Progress", "\"Progress\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_JobExecution_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_JobExecution_Account_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobExecution_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAccess", x => x.Id);
                    table.UniqueConstraint("AK_OrganizationAccess_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_OrganizationAccess_Interval", "\"ValidToUtc\" IS NULL OR \"ValidFromUtc\" < \"ValidToUtc\"");
                    table.CheckConstraint("CK_OrganizationAccess_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_OrganizationAccess_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationAccess_Account_GrantedByAccountId",
                        column: x => x.GrantedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationAccess_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AggregateVersion = table.Column<long>(type: "bigint", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvent", x => x.Id);
                    table.UniqueConstraint("AK_OutboxEvent_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_OutboxEvent_Attempts", "\"Attempts\" >= 0");
                    table.CheckConstraint("CK_OutboxEvent_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_OutboxEvent_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductUnitConversion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackagingCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnitConversion", x => x.Id);
                    table.UniqueConstraint("AK_ProductUnitConversion_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ProductUnitConversion_BaseQuantity", "\"BaseQuantity\" > 0");
                    table.CheckConstraint("CK_ProductUnitConversion_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ProductUnitConversion_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductUnitConversion_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSeed = table.Column<bool>(type: "boolean", nullable: false),
                    IsTechnical = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                    table.UniqueConstraint("AK_Role_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Role_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Role_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioDataset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsSynthetic = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioDataset", x => x.Id);
                    table.UniqueConstraint("AK_ScenarioDataset_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ScenarioDataset_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ScenarioDataset_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicePointContact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayNameCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    PhoneCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    EmailCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    EncryptionKeyVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePointContact", x => x.Id);
                    table.UniqueConstraint("AK_ServicePointContact_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ServicePointContact_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ServicePointContact_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicePointContact_ServicePoint_OrganizationId_ServicePoin~",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicePointWindow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ValidFromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePointWindow", x => x.Id);
                    table.UniqueConstraint("AK_ServicePointWindow_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ServicePointWindow_Day", "\"DayOfWeek\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_ServicePointWindow_Interval", "\"ValidToDate\" IS NULL OR \"ValidFromDate\" < \"ValidToDate\"");
                    table.CheckConstraint("CK_ServicePointWindow_Time", "\"StartLocalTime\" < \"EndLocalTime\"");
                    table.CheckConstraint("CK_ServicePointWindow_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ServicePointWindow_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicePointWindow_ServicePoint_OrganizationId_ServicePoint~",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoredFile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DeclaredMimeType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerifiedMimeType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EncryptionKeyReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFile", x => x.Id);
                    table.UniqueConstraint("AK_StoredFile_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_StoredFile_ByteSize", "\"ByteSize\" >= 0");
                    table.CheckConstraint("CK_StoredFile_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_StoredFile_Account_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoredFile_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncBatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncBatch", x => x.Id);
                    table.UniqueConstraint("AK_SyncBatch_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_SyncBatch_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_SyncBatch_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncBatch_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Template",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Template", x => x.Id);
                    table.UniqueConstraint("AK_Template_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Template_Kind", "\"Kind\" IN ('form', 'document', 'qr-label', 'explanation-prompt')");
                    table.CheckConstraint("CK_Template_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Template_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RegistrationCiphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    MaxWeight = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MaxVolume = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle", x => x.Id);
                    table.UniqueConstraint("AK_Vehicle_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Vehicle_MaxVolume", "\"MaxVolume\" > 0");
                    table.CheckConstraint("CK_Vehicle_MaxWeight", "\"MaxWeight\" > 0");
                    table.CheckConstraint("CK_Vehicle_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Vehicle_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinition", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowDefinition_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_WorkflowDefinition_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_WorkflowDefinition_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatalogOption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrderNumber = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogOption", x => x.Id);
                    table.UniqueConstraint("AK_CatalogOption_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_CatalogOption_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_CatalogOption_Catalog_OrganizationId_CatalogId",
                        columns: x => new { x.OrganizationId, x.CatalogId },
                        principalTable: "Catalog",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CatalogOption_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationConnection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EndpointReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SecretReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigurationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnection", x => x.Id);
                    table.UniqueConstraint("AK_IntegrationConnection_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_IntegrationConnection_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_IntegrationConnection_ConfigurationVersion_OrganizationId_C~",
                        columns: x => new { x.OrganizationId, x.ConfigurationVersionId },
                        principalTable: "ConfigurationVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationConnection_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SimulationPolicyVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    HandlerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfigurationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationPolicyVersion", x => x.Id);
                    table.UniqueConstraint("AK_SimulationPolicyVersion_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_SimulationPolicyVersion_Revision", "\"Revision\" > 0");
                    table.CheckConstraint("CK_SimulationPolicyVersion_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_SimulationPolicyVersion_ConfigurationVersion_OrganizationId~",
                        columns: x => new { x.OrganizationId, x.ConfigurationVersionId },
                        principalTable: "ConfigurationVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationPolicyVersion_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskCalculationRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormulaCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FormulaVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CutoffUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InputHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskCalculationRun", x => x.Id);
                    table.UniqueConstraint("AK_RiskCalculationRun_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RiskCalculationRun_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RiskCalculationRun_ConfigurationVersion_OrganizationId_Conf~",
                        columns: x => new { x.OrganizationId, x.ConfigurationVersionId },
                        principalTable: "ConfigurationVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskCalculationRun_InventorySnapshot_OrganizationId_Invento~",
                        columns: x => new { x.OrganizationId, x.InventorySnapshotId },
                        principalTable: "InventorySnapshot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskCalculationRun_JobExecution_OrganizationId_JobExecution~",
                        columns: x => new { x.OrganizationId, x.JobExecutionId },
                        principalTable: "JobExecution",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskCalculationRun_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessServicePoint",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessServicePoint", x => x.Id);
                    table.UniqueConstraint("AK_AccessServicePoint_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_AccessServicePoint_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_AccessServicePoint_Account_GrantedByAccountId",
                        column: x => x.GrantedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessServicePoint_OrganizationAccess_OrganizationId_Organi~",
                        columns: x => new { x.OrganizationId, x.OrganizationAccessId },
                        principalTable: "OrganizationAccess",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessServicePoint_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessServicePoint_ServicePoint_OrganizationId_ServicePoint~",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AssignedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAssignment", x => x.Id);
                    table.UniqueConstraint("AK_DeliveryAssignment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_DeliveryAssignment_Kind", "\"AssignmentKind\" IN ('driver', 'receiver')");
                    table.CheckConstraint("CK_DeliveryAssignment_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_DeliveryAssignment_Account_AssignedByAccountId",
                        column: x => x.AssignedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryAssignment_Delivery_OrganizationId_DeliveryId",
                        columns: x => new { x.OrganizationId, x.DeliveryId },
                        principalTable: "Delivery",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryAssignment_OrganizationAccess_OrganizationId_Organi~",
                        columns: x => new { x.OrganizationId, x.OrganizationAccessId },
                        principalTable: "OrganizationAccess",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryAssignment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.UniqueConstraint("AK_Notification_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Notification_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Notification_OrganizationAccess_OrganizationId_RecipientAcc~",
                        columns: x => new { x.OrganizationId, x.RecipientAccessId },
                        principalTable: "OrganizationAccess",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notification_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notification_OutboxEvent_OrganizationId_SourceEventId",
                        columns: x => new { x.OrganizationId, x.SourceEventId },
                        principalTable: "OutboxEvent",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRole", x => x.Id);
                    table.UniqueConstraint("AK_AccessRole_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_AccessRole_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_AccessRole_Account_GrantedByAccountId",
                        column: x => x.GrantedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessRole_OrganizationAccess_OrganizationId_OrganizationAc~",
                        columns: x => new { x.OrganizationId, x.OrganizationAccessId },
                        principalTable: "OrganizationAccess",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessRole_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessRole_Role_OrganizationId_RoleId",
                        columns: x => new { x.OrganizationId, x.RoleId },
                        principalTable: "Role",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RbacChange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetAccessId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RbacChange", x => x.Id);
                    table.UniqueConstraint("AK_RbacChange_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RbacChange_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RbacChange_Account_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RbacChange_OrganizationAccess_OrganizationId_TargetAccessId",
                        columns: x => new { x.OrganizationId, x.TargetAccessId },
                        principalTable: "OrganizationAccess",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RbacChange_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RbacChange_Role_OrganizationId_TargetRoleId",
                        columns: x => new { x.OrganizationId, x.TargetRoleId },
                        principalTable: "Role",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleFieldPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanRead = table.Column<bool>(type: "boolean", nullable: false),
                    CanWrite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleFieldPermission", x => x.Id);
                    table.UniqueConstraint("AK_RoleFieldPermission_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RoleFieldPermission_ReadBeforeWrite", "NOT \"CanWrite\" OR \"CanRead\"");
                    table.CheckConstraint("CK_RoleFieldPermission_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RoleFieldPermission_FieldDefinition_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "FieldDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleFieldPermission_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleFieldPermission_Role_OrganizationId_RoleId",
                        columns: x => new { x.OrganizationId, x.RoleId },
                        principalTable: "Role",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => x.Id);
                    table.UniqueConstraint("AK_RolePermission_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RolePermission_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RolePermission_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_OrganizationId_RoleId",
                        columns: x => new { x.OrganizationId, x.RoleId },
                        principalTable: "Role",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioDatasetVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioDatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ManifestJson = table.Column<string>(type: "jsonb", nullable: false),
                    StoredFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioDatasetVersion", x => x.Id);
                    table.UniqueConstraint("AK_ScenarioDatasetVersion_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ScenarioDatasetVersion_Revision", "\"Revision\" > 0");
                    table.CheckConstraint("CK_ScenarioDatasetVersion_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ScenarioDatasetVersion_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScenarioDatasetVersion_ScenarioDataset_OrganizationId_Scena~",
                        columns: x => new { x.OrganizationId, x.ScenarioDatasetId },
                        principalTable: "ScenarioDataset",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScenarioDatasetVersion_StoredFile_OrganizationId_StoredFile~",
                        columns: x => new { x.OrganizationId, x.StoredFileId },
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncOperation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClientOccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResultCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOperation", x => x.Id);
                    table.UniqueConstraint("AK_SyncOperation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_SyncOperation_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_SyncOperation_IdempotencyRecord_OrganizationId_IdempotencyR~",
                        columns: x => new { x.OrganizationId, x.IdempotencyRecordId },
                        principalTable: "IdempotencyRecord",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncOperation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncOperation_SyncBatch_OrganizationId_SyncBatchId",
                        columns: x => new { x.OrganizationId, x.SyncBatchId },
                        principalTable: "SyncBatch",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Position = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.Id);
                    table.UniqueConstraint("AK_Location_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Location_Kind", "(\"Kind\" IN ('warehouse', 'service-point', 'quarantine') AND \"ServicePointId\" IS NOT NULL AND \"VehicleId\" IS NULL) OR (\"Kind\" = 'vehicle' AND \"VehicleId\" IS NOT NULL AND \"ServicePointId\" IS NULL)");
                    table.CheckConstraint("CK_Location_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Location_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Location_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Location_Vehicle_OrganizationId_VehicleId",
                        columns: x => new { x.OrganizationId, x.VehicleId },
                        principalTable: "Vehicle",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowVersion", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowVersion_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_WorkflowVersion_Revision", "\"Revision\" > 0");
                    table.CheckConstraint("CK_WorkflowVersion_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_WorkflowVersion_Account_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersion_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowVersion_WorkflowDefinition_OrganizationId_WorkflowD~",
                        columns: x => new { x.OrganizationId, x.WorkflowDefinitionId },
                        principalTable: "WorkflowDefinition",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Origin = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ConditionOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lot", x => x.Id);
                    table.UniqueConstraint("AK_Lot_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Lot_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Lot_CatalogOption_OrganizationId_ConditionOptionId",
                        columns: x => new { x.OrganizationId, x.ConditionOptionId },
                        principalTable: "CatalogOption",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lot_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lot_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobExecutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RunKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderExecutionId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationRun", x => x.Id);
                    table.UniqueConstraint("AK_IntegrationRun_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_IntegrationRun_Attempts", "\"Attempts\" >= 0");
                    table.CheckConstraint("CK_IntegrationRun_EstimatedCost", "\"EstimatedCost\" >= 0");
                    table.CheckConstraint("CK_IntegrationRun_TokenCount", "\"TokenCount\" >= 0");
                    table.CheckConstraint("CK_IntegrationRun_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_IntegrationRun_IntegrationConnection_OrganizationId_Integra~",
                        columns: x => new { x.OrganizationId, x.IntegrationConnectionId },
                        principalTable: "IntegrationConnection",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationRun_JobExecution_OrganizationId_JobExecutionId",
                        columns: x => new { x.OrganizationId, x.JobExecutionId },
                        principalTable: "JobExecution",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationRun_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskCalculationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoverageDays = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    LeadTimeDays = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    SafetyMarginDays = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    RiskCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    UncertaintyJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskSnapshot", x => x.Id);
                    table.UniqueConstraint("AK_RiskSnapshot_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RiskSnapshot_CoverageDays", "\"CoverageDays\" >= 0");
                    table.CheckConstraint("CK_RiskSnapshot_LeadTimeDays", "\"LeadTimeDays\" >= 0");
                    table.CheckConstraint("CK_RiskSnapshot_SafetyMarginDays", "\"SafetyMarginDays\" >= 0");
                    table.CheckConstraint("CK_RiskSnapshot_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RiskSnapshot_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskSnapshot_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskSnapshot_RiskCalculationRun_OrganizationId_RiskCalculat~",
                        columns: x => new { x.OrganizationId, x.RiskCalculationRunId },
                        principalTable: "RiskCalculationRun",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskSnapshot_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadMatrix",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DatasetVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetrievedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfigurationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadMatrix", x => x.Id);
                    table.UniqueConstraint("AK_RoadMatrix_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RoadMatrix_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RoadMatrix_ConfigurationVersion_OrganizationId_Configuratio~",
                        columns: x => new { x.OrganizationId, x.ConfigurationVersionId },
                        principalTable: "ConfigurationVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadMatrix_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadMatrix_ScenarioDatasetVersion_OrganizationId_DatasetVer~",
                        columns: x => new { x.OrganizationId, x.DatasetVersionId },
                        principalTable: "ScenarioDatasetVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioDatasetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    SimulationTime = table.Column<long>(type: "bigint", nullable: false),
                    EventKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioEvent", x => x.Id);
                    table.UniqueConstraint("AK_ScenarioEvent_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ScenarioEvent_SequenceNumber", "\"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_ScenarioEvent_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ScenarioEvent_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScenarioEvent_ScenarioDatasetVersion_OrganizationId_Scenari~",
                        columns: x => new { x.OrganizationId, x.ScenarioDatasetVersionId },
                        principalTable: "ScenarioDatasetVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessDelegation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: true),
                    CanDelegateFurther = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessDelegation", x => x.Id);
                    table.UniqueConstraint("AK_AccessDelegation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_AccessDelegation_Interval", "\"ValidToUtc\" IS NULL OR \"ValidFromUtc\" < \"ValidToUtc\"");
                    table.CheckConstraint("CK_AccessDelegation_Scope", "(\"ScopeKind\" = 'organization' AND \"LocationId\" IS NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'location' AND \"LocationId\" IS NOT NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'service-point' AND \"ServicePointId\" IS NOT NULL AND \"LocationId\" IS NULL)");
                    table.CheckConstraint("CK_AccessDelegation_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_AccessDelegation_Account_GrantedByAccountId",
                        column: x => x.GrantedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessDelegation_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessDelegation_OrganizationAccess_OrganizationId_Organiza~",
                        columns: x => new { x.OrganizationId, x.OrganizationAccessId },
                        principalTable: "OrganizationAccess",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessDelegation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessDelegation_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessDelegation_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessLocation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLocation", x => x.Id);
                    table.UniqueConstraint("AK_AccessLocation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_AccessLocation_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_AccessLocation_Account_GrantedByAccountId",
                        column: x => x.GrantedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessLocation_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessLocation_OrganizationAccess_OrganizationId_Organizati~",
                        columns: x => new { x.OrganizationId, x.OrganizationAccessId },
                        principalTable: "OrganizationAccess",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessLocation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationActivation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChangedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationActivation", x => x.Id);
                    table.UniqueConstraint("AK_ConfigurationActivation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ConfigurationActivation_Interval", "\"EffectiveToUtc\" IS NULL OR \"EffectiveFromUtc\" < \"EffectiveToUtc\"");
                    table.CheckConstraint("CK_ConfigurationActivation_Scope", "(\"ScopeKind\" = 'organization' AND \"LocationId\" IS NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'location' AND \"LocationId\" IS NOT NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'service-point' AND \"ServicePointId\" IS NOT NULL AND \"LocationId\" IS NULL)");
                    table.CheckConstraint("CK_ConfigurationActivation_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ConfigurationActivation_Account_ChangedByAccountId",
                        column: x => x.ChangedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationActivation_ConfigurationVersion_OrganizationId~",
                        columns: x => new { x.OrganizationId, x.ConfigurationVersionId },
                        principalTable: "ConfigurationVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationActivation_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationActivation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationActivation_ServicePoint_OrganizationId_Service~",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandlingUnit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SealedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandlingUnit", x => x.Id);
                    table.UniqueConstraint("AK_HandlingUnit_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_HandlingUnit_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_HandlingUnit_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandlingUnit_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAvailability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailableToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepotLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAvailability", x => x.Id);
                    table.UniqueConstraint("AK_VehicleAvailability_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_VehicleAvailability_Interval", "\"AvailableToUtc\" IS NULL OR \"AvailableFromUtc\" < \"AvailableToUtc\"");
                    table.CheckConstraint("CK_VehicleAvailability_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_VehicleAvailability_Location_OrganizationId_DepotLocationId",
                        columns: x => new { x.OrganizationId, x.DepotLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAvailability_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAvailability_Vehicle_OrganizationId_VehicleId",
                        columns: x => new { x.OrganizationId, x.VehicleId },
                        principalTable: "Vehicle",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValidationSchemaJson = table.Column<string>(type: "jsonb", nullable: false),
                    LayoutJson = table.Column<string>(type: "jsonb", nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVersion", x => x.Id);
                    table.UniqueConstraint("AK_TemplateVersion_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_TemplateVersion_Revision", "\"Revision\" > 0");
                    table.CheckConstraint("CK_TemplateVersion_Status", "\"Status\" IN ('draft', 'published', 'retired') AND (\"Status\" = 'draft' OR \"PublishedAtUtc\" IS NOT NULL)");
                    table.CheckConstraint("CK_TemplateVersion_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_TemplateVersion_Account_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateVersion_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateVersion_Template_OrganizationId_TemplateId",
                        columns: x => new { x.OrganizationId, x.TemplateId },
                        principalTable: "Template",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateVersion_WorkflowVersion_OrganizationId_WorkflowVers~",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId },
                        principalTable: "WorkflowVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowState",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsInitial = table.Column<bool>(type: "boolean", nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowState", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowState_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_WorkflowState_OrganizationId_WorkflowVersionId_Id", x => new { x.OrganizationId, x.WorkflowVersionId, x.Id });
                    table.CheckConstraint("CK_WorkflowState_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_WorkflowState_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowState_WorkflowVersion_OrganizationId_WorkflowVersio~",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId },
                        principalTable: "WorkflowVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Consumption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    SourceKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtDevice = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumption", x => x.Id);
                    table.UniqueConstraint("AK_Consumption_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Consumption_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_Consumption_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Consumption_Account_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consumption_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consumption_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consumption_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventorySnapshotLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BalanceVersion = table.Column<long>(type: "bigint", nullable: false),
                    LotConditionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WasLotBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    LotExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySnapshotLine", x => x.Id);
                    table.UniqueConstraint("AK_InventorySnapshotLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_InventorySnapshotLine_QuantityOnHand", "\"QuantityOnHand\" >= 0");
                    table.CheckConstraint("CK_InventorySnapshotLine_QuantityReserved", "\"QuantityReserved\" >= 0");
                    table.CheckConstraint("CK_InventorySnapshotLine_Reserved", "\"QuantityReserved\" <= \"QuantityOnHand\"");
                    table.CheckConstraint("CK_InventorySnapshotLine_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_InventorySnapshotLine_InventorySnapshot_OrganizationId_Inve~",
                        columns: x => new { x.OrganizationId, x.InventorySnapshotId },
                        principalTable: "InventorySnapshot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventorySnapshotLine_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventorySnapshotLine_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventorySnapshotLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockBalance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityReserved = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LastMovementSequence = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBalance", x => x.Id);
                    table.UniqueConstraint("AK_StockBalance_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_StockBalance_QuantityOnHand", "\"QuantityOnHand\" >= 0");
                    table.CheckConstraint("CK_StockBalance_QuantityReserved", "\"QuantityReserved\" >= 0");
                    table.CheckConstraint("CK_StockBalance_Reserved", "\"QuantityReserved\" <= \"QuantityOnHand\"");
                    table.CheckConstraint("CK_StockBalance_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_StockBalance_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBalance_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBalance_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadMatrixEdge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadMatrixId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    IsReachable = table.Column<bool>(type: "boolean", nullable: false),
                    UnreachableReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadMatrixEdge", x => x.Id);
                    table.UniqueConstraint("AK_RoadMatrixEdge_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RoadMatrixEdge_DistanceKm", "\"DistanceKm\" >= 0");
                    table.CheckConstraint("CK_RoadMatrixEdge_DurationSeconds", "\"DurationSeconds\" >= 0");
                    table.CheckConstraint("CK_RoadMatrixEdge_Reachable", "(\"IsReachable\" AND \"DistanceKm\" IS NOT NULL AND \"DurationSeconds\" IS NOT NULL AND \"UnreachableReason\" IS NULL) OR (NOT \"IsReachable\" AND \"DistanceKm\" IS NULL AND \"DurationSeconds\" IS NULL AND \"UnreachableReason\" IS NOT NULL)");
                    table.CheckConstraint("CK_RoadMatrixEdge_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RoadMatrixEdge_Location_OrganizationId_FromLocationId",
                        columns: x => new { x.OrganizationId, x.FromLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadMatrixEdge_Location_OrganizationId_ToLocationId",
                        columns: x => new { x.OrganizationId, x.ToLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadMatrixEdge_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadMatrixEdge_RoadMatrix_OrganizationId_RoadMatrixId",
                        columns: x => new { x.OrganizationId, x.RoadMatrixId },
                        principalTable: "RoadMatrix",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SimulationRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioDatasetVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationPolicyVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadMatrixId = table.Column<Guid>(type: "uuid", nullable: true),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationRun", x => x.Id);
                    table.UniqueConstraint("AK_SimulationRun_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_SimulationRun_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_SimulationRun_JobExecution_OrganizationId_JobExecutionId",
                        columns: x => new { x.OrganizationId, x.JobExecutionId },
                        principalTable: "JobExecution",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationRun_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationRun_RoadMatrix_OrganizationId_RoadMatrixId",
                        columns: x => new { x.OrganizationId, x.RoadMatrixId },
                        principalTable: "RoadMatrix",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationRun_ScenarioDatasetVersion_OrganizationId_Scenari~",
                        columns: x => new { x.OrganizationId, x.ScenarioDatasetVersionId },
                        principalTable: "ScenarioDatasetVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationRun_SimulationPolicyVersion_OrganizationId_Simula~",
                        columns: x => new { x.OrganizationId, x.SimulationPolicyVersionId },
                        principalTable: "SimulationPolicyVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryHandlingUnit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryHandlingUnit", x => x.Id);
                    table.UniqueConstraint("AK_DeliveryHandlingUnit_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_DeliveryHandlingUnit_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_DeliveryHandlingUnit_Delivery_OrganizationId_DeliveryId",
                        columns: x => new { x.OrganizationId, x.DeliveryId },
                        principalTable: "Delivery",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryHandlingUnit_HandlingUnit_OrganizationId_HandlingUn~",
                        columns: x => new { x.OrganizationId, x.HandlingUnitId },
                        principalTable: "HandlingUnit",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryHandlingUnit_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandlingUnitLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandlingUnitLine", x => x.Id);
                    table.UniqueConstraint("AK_HandlingUnitLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_HandlingUnitLine_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_HandlingUnitLine_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_HandlingUnitLine_HandlingUnit_OrganizationId_HandlingUnitId",
                        columns: x => new { x.OrganizationId, x.HandlingUnitId },
                        principalTable: "HandlingUnit",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandlingUnitLine_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandlingUnitLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QrAccessToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScopeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrAccessToken", x => x.Id);
                    table.UniqueConstraint("AK_QrAccessToken_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_QrAccessToken_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_QrAccessToken_Account_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QrAccessToken_HandlingUnit_OrganizationId_HandlingUnitId",
                        columns: x => new { x.OrganizationId, x.HandlingUnitId },
                        principalTable: "HandlingUnit",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QrAccessToken_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskExplanation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExplanationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RecommendedAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskExplanation", x => x.Id);
                    table.UniqueConstraint("AK_RiskExplanation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RiskExplanation_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RiskExplanation_IntegrationRun_OrganizationId_IntegrationRu~",
                        columns: x => new { x.OrganizationId, x.IntegrationRunId },
                        principalTable: "IntegrationRun",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskExplanation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskExplanation_RiskSnapshot_OrganizationId_RiskSnapshotId",
                        columns: x => new { x.OrganizationId, x.RiskSnapshotId },
                        principalTable: "RiskSnapshot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskExplanation_TemplateVersion_OrganizationId_TemplateVers~",
                        columns: x => new { x.OrganizationId, x.TemplateVersionId },
                        principalTable: "TemplateVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateBinding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActionCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScopeKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateBinding", x => x.Id);
                    table.UniqueConstraint("AK_TemplateBinding_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_TemplateBinding_Interval", "\"EffectiveToUtc\" IS NULL OR \"EffectiveFromUtc\" < \"EffectiveToUtc\"");
                    table.CheckConstraint("CK_TemplateBinding_Scope", "(\"ScopeKind\" = 'organization' AND \"LocationId\" IS NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'location' AND \"LocationId\" IS NOT NULL AND \"ServicePointId\" IS NULL) OR (\"ScopeKind\" = 'service-point' AND \"ServicePointId\" IS NOT NULL AND \"LocationId\" IS NULL)");
                    table.CheckConstraint("CK_TemplateBinding_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_TemplateBinding_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateBinding_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateBinding_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateBinding_TemplateVersion_OrganizationId_TemplateVers~",
                        columns: x => new { x.OrganizationId, x.TemplateVersionId },
                        principalTable: "TemplateVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateField",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    OrderNumber = table.Column<int>(type: "integer", nullable: false),
                    ValidationJson = table.Column<string>(type: "jsonb", nullable: true),
                    VisibilityConditionJson = table.Column<string>(type: "jsonb", nullable: true),
                    Sensitivity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CatalogId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateField", x => x.Id);
                    table.UniqueConstraint("AK_TemplateField_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_TemplateField_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_TemplateField_Catalog_OrganizationId_CatalogId",
                        columns: x => new { x.OrganizationId, x.CatalogId },
                        principalTable: "Catalog",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateField_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateField_TemplateVersion_OrganizationId_TemplateVersio~",
                        columns: x => new { x.OrganizationId, x.TemplateVersionId },
                        principalTable: "TemplateVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Receipt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtDevice = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipt", x => x.Id);
                    table.UniqueConstraint("AK_Receipt_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Receipt_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Receipt_Account_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipt_Delivery_OrganizationId_DeliveryId",
                        columns: x => new { x.OrganizationId, x.DeliveryId },
                        principalTable: "Delivery",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipt_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipt_WorkflowState_OrganizationId_StateId",
                        columns: x => new { x.OrganizationId, x.StateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipt_WorkflowState_OrganizationId_WorkflowVersionId_Stat~",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId, x.StateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "WorkflowVersionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipt_WorkflowVersion_OrganizationId_WorkflowVersionId",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId },
                        principalTable: "WorkflowVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReplenishmentRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    NeededByUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplenishmentRequest", x => x.Id);
                    table.UniqueConstraint("AK_ReplenishmentRequest_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ReplenishmentRequest_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_Account_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_ConfigurationVersion_OrganizationId_Co~",
                        columns: x => new { x.OrganizationId, x.ConfigurationVersionId },
                        principalTable: "ConfigurationVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_ServicePoint_OrganizationId_ServicePoi~",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_WorkflowState_OrganizationId_StateId",
                        columns: x => new { x.OrganizationId, x.StateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_WorkflowState_OrganizationId_WorkflowV~",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId, x.StateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "WorkflowVersionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_WorkflowVersion_OrganizationId_Workflo~",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId },
                        principalTable: "WorkflowVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoutePlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    SupersedesRoutePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadMatrixId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePlan", x => x.Id);
                    table.UniqueConstraint("AK_RoutePlan_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RoutePlan_Interval", "\"PlannedEndUtc\" IS NULL OR \"PlannedStartUtc\" < \"PlannedEndUtc\"");
                    table.CheckConstraint("CK_RoutePlan_Revision", "\"Revision\" > 0");
                    table.CheckConstraint("CK_RoutePlan_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RoutePlan_Account_PublishedByAccountId",
                        column: x => x.PublishedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlan_ConfigurationVersion_OrganizationId_Configuration~",
                        columns: x => new { x.OrganizationId, x.ConfigurationVersionId },
                        principalTable: "ConfigurationVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlan_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlan_RoadMatrix_OrganizationId_RoadMatrixId",
                        columns: x => new { x.OrganizationId, x.RoadMatrixId },
                        principalTable: "RoadMatrix",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlan_RoutePlan_OrganizationId_SupersedesRoutePlanId",
                        columns: x => new { x.OrganizationId, x.SupersedesRoutePlanId },
                        principalTable: "RoutePlan",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlan_Vehicle_OrganizationId_VehicleId",
                        columns: x => new { x.OrganizationId, x.VehicleId },
                        principalTable: "Vehicle",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlan_WorkflowState_OrganizationId_StateId",
                        columns: x => new { x.OrganizationId, x.StateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlan_WorkflowState_OrganizationId_WorkflowVersionId_St~",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId, x.StateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "WorkflowVersionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlan_WorkflowVersion_OrganizationId_WorkflowVersionId",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId },
                        principalTable: "WorkflowVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlerCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConditionJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransition", x => x.Id);
                    table.UniqueConstraint("AK_WorkflowTransition_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_WorkflowTransition_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_WorkflowTransition_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransition_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransition_WorkflowState_OrganizationId_FromStateId",
                        columns: x => new { x.OrganizationId, x.FromStateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransition_WorkflowState_OrganizationId_ToStateId",
                        columns: x => new { x.OrganizationId, x.ToStateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransition_WorkflowState_OrganizationId_WorkflowVer~",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId, x.FromStateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "WorkflowVersionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransition_WorkflowState_OrganizationId_WorkflowVe~1",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId, x.ToStateId },
                        principalTable: "WorkflowState",
                        principalColumns: new[] { "OrganizationId", "WorkflowVersionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransition_WorkflowVersion_OrganizationId_WorkflowV~",
                        columns: x => new { x.OrganizationId, x.WorkflowVersionId },
                        principalTable: "WorkflowVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SimulationArtifact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationArtifact", x => x.Id);
                    table.UniqueConstraint("AK_SimulationArtifact_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_SimulationArtifact_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_SimulationArtifact_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationArtifact_SimulationRun_OrganizationId_SimulationR~",
                        columns: x => new { x.OrganizationId, x.SimulationRunId },
                        principalTable: "SimulationRun",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationArtifact_StoredFile_OrganizationId_StoredFileId",
                        columns: x => new { x.OrganizationId, x.StoredFileId },
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SimulationEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    SimulationTime = table.Column<long>(type: "bigint", nullable: false),
                    EventKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationEvent", x => x.Id);
                    table.UniqueConstraint("AK_SimulationEvent_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_SimulationEvent_SequenceNumber", "\"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_SimulationEvent_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_SimulationEvent_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationEvent_SimulationRun_OrganizationId_SimulationRunId",
                        columns: x => new { x.OrganizationId, x.SimulationRunId },
                        principalTable: "SimulationRun",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SimulationMetric",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefinitionVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Numerator = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    Denominator = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    Value = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    Unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NotApplicableReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationMetric", x => x.Id);
                    table.UniqueConstraint("AK_SimulationMetric_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_SimulationMetric_Denominator", "\"Denominator\" >= 0");
                    table.CheckConstraint("CK_SimulationMetric_Division", "(\"Denominator\" IS DISTINCT FROM 0 AND \"Value\" IS NOT NULL AND \"NotApplicableReason\" IS NULL) OR (\"Value\" IS NULL AND \"NotApplicableReason\" IS NOT NULL)");
                    table.CheckConstraint("CK_SimulationMetric_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_SimulationMetric_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationMetric_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulationMetric_SimulationRun_OrganizationId_SimulationRun~",
                        columns: x => new { x.OrganizationId, x.SimulationRunId },
                        principalTable: "SimulationRun",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateFieldAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanRead = table.Column<bool>(type: "boolean", nullable: false),
                    CanWrite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateFieldAccess", x => x.Id);
                    table.UniqueConstraint("AK_TemplateFieldAccess_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_TemplateFieldAccess_ReadBeforeWrite", "NOT \"CanWrite\" OR \"CanRead\"");
                    table.CheckConstraint("CK_TemplateFieldAccess_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_TemplateFieldAccess_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFieldAccess_Role_OrganizationId_RoleId",
                        columns: x => new { x.OrganizationId, x.RoleId },
                        principalTable: "Role",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFieldAccess_TemplateField_OrganizationId_TemplateFi~",
                        columns: x => new { x.OrganizationId, x.TemplateFieldId },
                        principalTable: "TemplateField",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateFieldOption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrderNumber = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateFieldOption", x => x.Id);
                    table.UniqueConstraint("AK_TemplateFieldOption_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_TemplateFieldOption_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_TemplateFieldOption_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFieldOption_TemplateField_OrganizationId_TemplateFi~",
                        columns: x => new { x.OrganizationId, x.TemplateFieldId },
                        principalTable: "TemplateField",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustodyEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtDevice = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyEvent", x => x.Id);
                    table.UniqueConstraint("AK_CustodyEvent_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_CustodyEvent_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_CustodyEvent_Account_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyEvent_Delivery_OrganizationId_DeliveryId",
                        columns: x => new { x.OrganizationId, x.DeliveryId },
                        principalTable: "Delivery",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyEvent_HandlingUnit_OrganizationId_HandlingUnitId",
                        columns: x => new { x.OrganizationId, x.HandlingUnitId },
                        principalTable: "HandlingUnit",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyEvent_Location_OrganizationId_FromLocationId",
                        columns: x => new { x.OrganizationId, x.FromLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyEvent_Location_OrganizationId_ToLocationId",
                        columns: x => new { x.OrganizationId, x.ToLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyEvent_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustodyEvent_Receipt_OrganizationId_ReceiptId",
                        columns: x => new { x.OrganizationId, x.ReceiptId },
                        principalTable: "Receipt",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReplenishmentRequestLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplenishmentRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AllocatedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplenishmentRequestLine", x => x.Id);
                    table.UniqueConstraint("AK_ReplenishmentRequestLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ReplenishmentRequestLine_AllocatedQuantity", "\"AllocatedQuantity\" >= 0");
                    table.CheckConstraint("CK_ReplenishmentRequestLine_Allocation", "\"AllocatedQuantity\" <= \"RequestedQuantity\"");
                    table.CheckConstraint("CK_ReplenishmentRequestLine_RequestedQuantity", "\"RequestedQuantity\" > 0");
                    table.CheckConstraint("CK_ReplenishmentRequestLine_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequestLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequestLine_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequestLine_ReplenishmentRequest_OrganizationI~",
                        columns: x => new { x.OrganizationId, x.ReplenishmentRequestId },
                        principalTable: "ReplenishmentRequest",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoutePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversation", x => x.Id);
                    table.UniqueConstraint("AK_Conversation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Conversation_Context", "num_nonnulls(\"DeliveryId\", \"RoutePlanId\") <= 1");
                    table.CheckConstraint("CK_Conversation_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Conversation_Account_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversation_Delivery_OrganizationId_DeliveryId",
                        columns: x => new { x.OrganizationId, x.DeliveryId },
                        principalTable: "Delivery",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversation_RoutePlan_OrganizationId_RoutePlanId",
                        columns: x => new { x.OrganizationId, x.RoutePlanId },
                        principalTable: "RoutePlan",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Incident",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoutePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeverityOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    IsSynthetic = table.Column<bool>(type: "boolean", nullable: false),
                    ScenarioEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAtDevice = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incident", x => x.Id);
                    table.UniqueConstraint("AK_Incident_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Incident_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Incident_Account_ReportedByAccountId",
                        column: x => x.ReportedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incident_CatalogOption_OrganizationId_SeverityOptionId",
                        columns: x => new { x.OrganizationId, x.SeverityOptionId },
                        principalTable: "CatalogOption",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incident_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incident_RoutePlan_OrganizationId_RoutePlanId",
                        columns: x => new { x.OrganizationId, x.RoutePlanId },
                        principalTable: "RoutePlan",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incident_ScenarioEvent_OrganizationId_ScenarioEventId",
                        columns: x => new { x.OrganizationId, x.ScenarioEventId },
                        principalTable: "ScenarioEvent",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Incident_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoutePlanningSnapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    InputHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AlgorithmCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CutoffUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePlanningSnapshot", x => x.Id);
                    table.UniqueConstraint("AK_RoutePlanningSnapshot_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RoutePlanningSnapshot_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RoutePlanningSnapshot_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlanningSnapshot_RoutePlan_OrganizationId_RoutePlanId",
                        columns: x => new { x.OrganizationId, x.RoutePlanId },
                        principalTable: "RoutePlan",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteStop",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    PlannedArrivalUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedDepartureUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualArrivalUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualDepartureUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PendingReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStop", x => x.Id);
                    table.UniqueConstraint("AK_RouteStop_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RouteStop_Interval", "\"PlannedDepartureUtc\" IS NULL OR \"PlannedArrivalUtc\" < \"PlannedDepartureUtc\"");
                    table.CheckConstraint("CK_RouteStop_SequenceNumber", "\"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_RouteStop_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RouteStop_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteStop_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteStop_RoutePlan_OrganizationId_RoutePlanId",
                        columns: x => new { x.OrganizationId, x.RoutePlanId },
                        principalTable: "RoutePlan",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteStop_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehiclePosition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutePlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtDevice = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Position = table.Column<Point>(type: "geography(Point,4326)", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiclePosition", x => x.Id);
                    table.UniqueConstraint("AK_VehiclePosition_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_VehiclePosition_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_VehiclePosition_Account_RecordedByAccountId",
                        column: x => x.RecordedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePosition_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePosition_RoutePlan_OrganizationId_RoutePlanId",
                        columns: x => new { x.OrganizationId, x.RoutePlanId },
                        principalTable: "RoutePlan",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePosition_Vehicle_OrganizationId_VehicleId",
                        columns: x => new { x.OrganizationId, x.VehicleId },
                        principalTable: "Vehicle",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplenishmentRequestLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DispatchedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PackagingSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryLine", x => x.Id);
                    table.UniqueConstraint("AK_DeliveryLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_DeliveryLine_AcceptedQuantity", "\"AcceptedQuantity\" >= 0");
                    table.CheckConstraint("CK_DeliveryLine_DispatchedQuantity", "\"DispatchedQuantity\" >= 0");
                    table.CheckConstraint("CK_DeliveryLine_PlannedQuantity", "\"PlannedQuantity\" > 0");
                    table.CheckConstraint("CK_DeliveryLine_RejectedQuantity", "\"RejectedQuantity\" >= 0");
                    table.CheckConstraint("CK_DeliveryLine_Totals", "\"AcceptedQuantity\" + \"RejectedQuantity\" <= \"DispatchedQuantity\" AND \"DispatchedQuantity\" <= \"PlannedQuantity\"");
                    table.CheckConstraint("CK_DeliveryLine_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_DeliveryLine_Delivery_OrganizationId_DeliveryId",
                        columns: x => new { x.OrganizationId, x.DeliveryId },
                        principalTable: "Delivery",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryLine_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryLine_ReplenishmentRequestLine_OrganizationId_Replen~",
                        columns: x => new { x.OrganizationId, x.ReplenishmentRequestLineId },
                        principalTable: "ReplenishmentRequestLine",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipant", x => x.Id);
                    table.UniqueConstraint("AK_ConversationParticipant_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ConversationParticipant_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Conversation_OrganizationId_Convers~",
                        columns: x => new { x.OrganizationId, x.ConversationId },
                        principalTable: "Conversation",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_OrganizationAccess_OrganizationId_O~",
                        columns: x => new { x.OrganizationId, x.OrganizationAccessId },
                        principalTable: "OrganizationAccess",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationParticipant_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodyText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ReceivedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RedactedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.Id);
                    table.UniqueConstraint("AK_Message_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Message_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Message_Account_AuthorAccountId",
                        column: x => x.AuthorAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Message_Conversation_OrganizationId_ConversationId",
                        columns: x => new { x.OrganizationId, x.ConversationId },
                        principalTable: "Conversation",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Message_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncidentRoadRestriction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadMatrixEdgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    AddedDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentRoadRestriction", x => x.Id);
                    table.UniqueConstraint("AK_IncidentRoadRestriction_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_IncidentRoadRestriction_AddedDurationSeconds", "\"AddedDurationSeconds\" >= 0");
                    table.CheckConstraint("CK_IncidentRoadRestriction_Interval", "\"EffectiveToUtc\" IS NULL OR \"EffectiveFromUtc\" < \"EffectiveToUtc\"");
                    table.CheckConstraint("CK_IncidentRoadRestriction_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_IncidentRoadRestriction_Incident_OrganizationId_IncidentId",
                        columns: x => new { x.OrganizationId, x.IncidentId },
                        principalTable: "Incident",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentRoadRestriction_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentRoadRestriction_RoadMatrixEdge_OrganizationId_RoadM~",
                        columns: x => new { x.OrganizationId, x.RoadMatrixEdgeId },
                        principalTable: "RoadMatrixEdge",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoutePlanIncident",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePlanIncident", x => x.Id);
                    table.UniqueConstraint("AK_RoutePlanIncident_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_RoutePlanIncident_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_RoutePlanIncident_Incident_OrganizationId_IncidentId",
                        columns: x => new { x.OrganizationId, x.IncidentId },
                        principalTable: "Incident",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlanIncident_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoutePlanIncident_RoutePlan_OrganizationId_RoutePlanId",
                        columns: x => new { x.OrganizationId, x.RoutePlanId },
                        principalTable: "RoutePlan",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryHandlingUnitLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryHandlingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlingUnitLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryHandlingUnitLine", x => x.Id);
                    table.UniqueConstraint("AK_DeliveryHandlingUnitLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_DeliveryHandlingUnitLine_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_DeliveryHandlingUnitLine_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_DeliveryHandlingUnitLine_DeliveryHandlingUnit_OrganizationI~",
                        columns: x => new { x.OrganizationId, x.DeliveryHandlingUnitId },
                        principalTable: "DeliveryHandlingUnit",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryHandlingUnitLine_DeliveryLine_OrganizationId_Delive~",
                        columns: x => new { x.OrganizationId, x.DeliveryLineId },
                        principalTable: "DeliveryLine",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryHandlingUnitLine_HandlingUnitLine_OrganizationId_Ha~",
                        columns: x => new { x.OrganizationId, x.HandlingUnitLineId },
                        principalTable: "HandlingUnitLine",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryHandlingUnitLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityAccepted = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    RejectionReasonOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReturnLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptLine", x => x.Id);
                    table.UniqueConstraint("AK_ReceiptLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ReceiptLine_Quantity", "\"QuantityAccepted\" + \"QuantityRejected\" > 0 AND (\"QuantityRejected\" = 0 OR (\"RejectionReasonOptionId\" IS NOT NULL AND \"ReturnLocationId\" IS NOT NULL))");
                    table.CheckConstraint("CK_ReceiptLine_QuantityAccepted", "\"QuantityAccepted\" >= 0");
                    table.CheckConstraint("CK_ReceiptLine_QuantityRejected", "\"QuantityRejected\" >= 0");
                    table.CheckConstraint("CK_ReceiptLine_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_ReceiptLine_CatalogOption_OrganizationId_RejectionReasonOpt~",
                        columns: x => new { x.OrganizationId, x.RejectionReasonOptionId },
                        principalTable: "CatalogOption",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptLine_DeliveryLine_OrganizationId_DeliveryLineId",
                        columns: x => new { x.OrganizationId, x.DeliveryLineId },
                        principalTable: "DeliveryLine",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptLine_Location_OrganizationId_ReturnLocationId",
                        columns: x => new { x.OrganizationId, x.ReturnLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptLine_Receipt_OrganizationId_ReceiptId",
                        columns: x => new { x.OrganizationId, x.ReceiptId },
                        principalTable: "Receipt",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ConsumedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReleasedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.Id);
                    table.UniqueConstraint("AK_Reservation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Reservation_ConsumedQuantity", "\"ConsumedQuantity\" >= 0");
                    table.CheckConstraint("CK_Reservation_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_Reservation_ReleasedQuantity", "\"ReleasedQuantity\" >= 0");
                    table.CheckConstraint("CK_Reservation_Remaining", "\"ConsumedQuantity\" + \"ReleasedQuantity\" <= \"Quantity\"");
                    table.CheckConstraint("CK_Reservation_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Reservation_DeliveryLine_OrganizationId_DeliveryLineId",
                        columns: x => new { x.OrganizationId, x.DeliveryLineId },
                        principalTable: "DeliveryLine",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachment", x => x.Id);
                    table.UniqueConstraint("AK_MessageAttachment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_MessageAttachment_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_MessageAttachment_Message_OrganizationId_MessageId",
                        columns: x => new { x.OrganizationId, x.MessageId },
                        principalTable: "Message",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageAttachment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageAttachment_StoredFile_OrganizationId_StoredFileId",
                        columns: x => new { x.OrganizationId, x.StoredFileId },
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoredFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UploadedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidence", x => x.Id);
                    table.UniqueConstraint("AK_Evidence_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Evidence_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_Evidence_Account_UploadedByAccountId",
                        column: x => x.UploadedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidence_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidence_ReceiptLine_OrganizationId_ReceiptLineId",
                        columns: x => new { x.OrganizationId, x.ReceiptLineId },
                        principalTable: "ReceiptLine",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidence_Receipt_OrganizationId_ReceiptId",
                        columns: x => new { x.OrganizationId, x.ReceiptId },
                        principalTable: "Receipt",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidence_StoredFile_OrganizationId_StoredFileId",
                        columns: x => new { x.OrganizationId, x.StoredFileId },
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedSequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtDevice = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtServer = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveryLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovement", x => x.Id);
                    table.UniqueConstraint("AK_InventoryMovement_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_InventoryMovement_Direction", "(\"Kind\" = 'inbound' AND \"FromLocationId\" IS NULL AND \"ToLocationId\" IS NOT NULL) OR (\"Kind\" IN ('outbound', 'consumption', 'loss') AND \"FromLocationId\" IS NOT NULL AND \"ToLocationId\" IS NULL) OR (\"Kind\" = 'transfer' AND \"FromLocationId\" IS NOT NULL AND \"ToLocationId\" IS NOT NULL AND \"FromLocationId\" <> \"ToLocationId\")");
                    table.CheckConstraint("CK_InventoryMovement_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_InventoryMovement_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_InventoryMovement_Account_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovement_Consumption_OrganizationId_ConsumptionId",
                        columns: x => new { x.OrganizationId, x.ConsumptionId },
                        principalTable: "Consumption",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovement_DeliveryLine_OrganizationId_DeliveryLineId",
                        columns: x => new { x.OrganizationId, x.DeliveryLineId },
                        principalTable: "DeliveryLine",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovement_Location_OrganizationId_FromLocationId",
                        columns: x => new { x.OrganizationId, x.FromLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovement_Location_OrganizationId_ToLocationId",
                        columns: x => new { x.OrganizationId, x.ToLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovement_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovement_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryMovement_ReceiptLine_OrganizationId_ReceiptLineId",
                        columns: x => new { x.OrganizationId, x.ReceiptLineId },
                        principalTable: "ReceiptLine",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnomalyCase",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LossMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyCase", x => x.Id);
                    table.UniqueConstraint("AK_AnomalyCase_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_AnomalyCase_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_AnomalyCase_Account_ClosedByAccountId",
                        column: x => x.ClosedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnomalyCase_InventoryMovement_OrganizationId_LossMovementId",
                        columns: x => new { x.OrganizationId, x.LossMovementId },
                        principalTable: "InventoryMovement",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnomalyCase_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnomalyCase_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnomalyCase_RiskSnapshot_OrganizationId_RiskSnapshotId",
                        columns: x => new { x.OrganizationId, x.RiskSnapshotId },
                        principalTable: "RiskSnapshot",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnomalyCase_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataJson = table.Column<string>(type: "jsonb", nullable: false),
                    SensitiveDataCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    EncryptionKeyVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupersedesSubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryMovementId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmission", x => x.Id);
                    table.UniqueConstraint("AK_FormSubmission_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_FormSubmission_Context", "num_nonnulls(\"DeliveryId\", \"ReceiptId\", \"ServicePointId\", \"InventoryMovementId\") <= 1");
                    table.CheckConstraint("CK_FormSubmission_Encryption", "(\"SensitiveDataCiphertext\" IS NULL) = (\"EncryptionKeyVersion\" IS NULL)");
                    table.CheckConstraint("CK_FormSubmission_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_FormSubmission_Account_SubmittedByAccountId",
                        column: x => x.SubmittedByAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmission_Delivery_OrganizationId_DeliveryId",
                        columns: x => new { x.OrganizationId, x.DeliveryId },
                        principalTable: "Delivery",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmission_FormSubmission_OrganizationId_SupersedesSubm~",
                        columns: x => new { x.OrganizationId, x.SupersedesSubmissionId },
                        principalTable: "FormSubmission",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmission_InventoryMovement_OrganizationId_InventoryMo~",
                        columns: x => new { x.OrganizationId, x.InventoryMovementId },
                        principalTable: "InventoryMovement",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmission_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmission_Receipt_OrganizationId_ReceiptId",
                        columns: x => new { x.OrganizationId, x.ReceiptId },
                        principalTable: "Receipt",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmission_ServicePoint_OrganizationId_ServicePointId",
                        columns: x => new { x.OrganizationId, x.ServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSubmission_TemplateVersion_OrganizationId_TemplateVersi~",
                        columns: x => new { x.OrganizationId, x.TemplateVersionId },
                        principalTable: "TemplateVersion",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", rowVersion: true, nullable: false, defaultValue: 1L),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAttachment", x => x.Id);
                    table.UniqueConstraint("AK_FormAttachment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_FormAttachment_Version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_FormAttachment_FormSubmission_OrganizationId_FormSubmission~",
                        columns: x => new { x.OrganizationId, x.FormSubmissionId },
                        principalTable: "FormSubmission",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormAttachment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormAttachment_StoredFile_OrganizationId_StoredFileId",
                        columns: x => new { x.OrganizationId, x.StoredFileId },
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormAttachment_TemplateField_OrganizationId_TemplateFieldId",
                        columns: x => new { x.OrganizationId, x.TemplateFieldId },
                        principalTable: "TemplateField",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePoint_OrganizationId_Code",
                table: "ServicePoint",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePoint_OrganizationId_ConfigurationVersionId",
                table: "ServicePoint",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePoint_OrganizationId_TypeOptionId",
                table: "ServicePoint",
                columns: new[] { "OrganizationId", "TypeOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePoint_Position",
                table: "ServicePoint",
                column: "Position")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServicePoint_Version",
                table: "ServicePoint",
                sql: "\"Version\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Product_OrganizationId_Sku",
                table: "Product",
                columns: new[] { "OrganizationId", "Sku" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_Version",
                table: "Product",
                sql: "\"Version\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_VolumePerBaseUnit",
                table: "Product",
                sql: "\"VolumePerBaseUnit\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_WeightPerBaseUnit",
                table: "Product",
                sql: "\"WeightPerBaseUnit\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_Code",
                table: "Delivery",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_DestinationLocationId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "DestinationLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_OriginLocationId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "OriginLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_RouteStopId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "RouteStopId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_ServicePointId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_StateId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "StateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_WorkflowVersionId_StateId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "WorkflowVersionId", "StateId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Delivery_Locations",
                table: "Delivery",
                sql: "\"OriginLocationId\" <> \"DestinationLocationId\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Delivery_Version",
                table: "Delivery",
                sql: "\"Version\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDelegation_GrantedByAccountId",
                table: "AccessDelegation",
                column: "GrantedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDelegation_OrganizationId_LocationId",
                table: "AccessDelegation",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessDelegation_OrganizationId_OrganizationAccessId_Permi~1",
                table: "AccessDelegation",
                columns: new[] { "OrganizationId", "OrganizationAccessId", "PermissionId", "ScopeKind", "LocationId" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL AND \"LocationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDelegation_OrganizationId_OrganizationAccessId_Permi~2",
                table: "AccessDelegation",
                columns: new[] { "OrganizationId", "OrganizationAccessId", "PermissionId", "ScopeKind", "ServicePointId" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL AND \"ServicePointId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDelegation_OrganizationId_OrganizationAccessId_Permis~",
                table: "AccessDelegation",
                columns: new[] { "OrganizationId", "OrganizationAccessId", "PermissionId", "ScopeKind" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL AND \"ScopeKind\" = 'organization'");

            migrationBuilder.CreateIndex(
                name: "IX_AccessDelegation_OrganizationId_ServicePointId",
                table: "AccessDelegation",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessDelegation_PermissionId",
                table: "AccessDelegation",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessLocation_GrantedByAccountId",
                table: "AccessLocation",
                column: "GrantedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessLocation_OrganizationId_LocationId",
                table: "AccessLocation",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessLocation_OrganizationId_OrganizationAccessId_Location~",
                table: "AccessLocation",
                columns: new[] { "OrganizationId", "OrganizationAccessId", "LocationId" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRole_GrantedByAccountId",
                table: "AccessRole",
                column: "GrantedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRole_OrganizationId_OrganizationAccessId_RoleId",
                table: "AccessRole",
                columns: new[] { "OrganizationId", "OrganizationAccessId", "RoleId" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRole_OrganizationId_RoleId",
                table: "AccessRole",
                columns: new[] { "OrganizationId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessServicePoint_GrantedByAccountId",
                table: "AccessServicePoint",
                column: "GrantedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessServicePoint_OrganizationId_OrganizationAccessId_Serv~",
                table: "AccessServicePoint",
                columns: new[] { "OrganizationId", "OrganizationAccessId", "ServicePointId" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessServicePoint_OrganizationId_ServicePointId",
                table: "AccessServicePoint",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_Account_Issuer_ClientId",
                table: "Account",
                columns: new[] { "Issuer", "ClientId" },
                unique: true,
                filter: "\"Kind\" = 'technical'");

            migrationBuilder.CreateIndex(
                name: "IX_Account_Issuer_Subject",
                table: "Account",
                columns: new[] { "Issuer", "Subject" },
                unique: true,
                filter: "\"Kind\" = 'human'");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyCase_ClosedByAccountId",
                table: "AnomalyCase",
                column: "ClosedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyCase_OrganizationId_LossMovementId",
                table: "AnomalyCase",
                columns: new[] { "OrganizationId", "LossMovementId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyCase_OrganizationId_ProductId",
                table: "AnomalyCase",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyCase_OrganizationId_RiskSnapshotId",
                table: "AnomalyCase",
                columns: new[] { "OrganizationId", "RiskSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyCase_OrganizationId_ServicePointId",
                table: "AnomalyCase",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_ActorAccountId",
                table: "AuditEvent",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_OrganizationId_Code",
                table: "Catalog",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogOption_OrganizationId_CatalogId_Code",
                table: "CatalogOption",
                columns: new[] { "OrganizationId", "CatalogId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationActivation_ChangedByAccountId",
                table: "ConfigurationActivation",
                column: "ChangedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationActivation_OrganizationId_ConfigurationVersion~",
                table: "ConfigurationActivation",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationActivation_OrganizationId_LocationId",
                table: "ConfigurationActivation",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationActivation_OrganizationId_ServicePointId",
                table: "ConfigurationActivation",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationDefinition_Code",
                table: "ConfigurationDefinition",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_ConfigurationDefinitionId",
                table: "ConfigurationVersion",
                column: "ConfigurationDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_CreatedByAccountId",
                table: "ConfigurationVersion",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationVersion_OrganizationId_ConfigurationDefinition~",
                table: "ConfigurationVersion",
                columns: new[] { "OrganizationId", "ConfigurationDefinitionId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_ActorAccountId",
                table: "Consumption",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_OrganizationId_ActorAccountId_OperationId",
                table: "Consumption",
                columns: new[] { "OrganizationId", "ActorAccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_OrganizationId_LotId",
                table: "Consumption",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_OrganizationId_ServicePointId",
                table: "Consumption",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_CreatedByAccountId",
                table: "Conversation",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_OrganizationId_DeliveryId",
                table: "Conversation",
                columns: new[] { "OrganizationId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_OrganizationId_RoutePlanId",
                table: "Conversation",
                columns: new[] { "OrganizationId", "RoutePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_OrganizationId_ConversationId_Organ~",
                table: "ConversationParticipant",
                columns: new[] { "OrganizationId", "ConversationId", "OrganizationAccessId" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipant_OrganizationId_OrganizationAccessId",
                table: "ConversationParticipant",
                columns: new[] { "OrganizationId", "OrganizationAccessId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_ActorAccountId",
                table: "CustodyEvent",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_DeliveryId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_FromLocationId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "FromLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_HandlingUnitId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "HandlingUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_ReceiptId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "ReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_ToLocationId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "ToLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAssignment_AssignedByAccountId",
                table: "DeliveryAssignment",
                column: "AssignedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAssignment_OrganizationId_DeliveryId_OrganizationAc~",
                table: "DeliveryAssignment",
                columns: new[] { "OrganizationId", "DeliveryId", "OrganizationAccessId", "AssignmentKind" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAssignment_OrganizationId_OrganizationAccessId",
                table: "DeliveryAssignment",
                columns: new[] { "OrganizationId", "OrganizationAccessId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryHandlingUnit_OrganizationId_DeliveryId",
                table: "DeliveryHandlingUnit",
                columns: new[] { "OrganizationId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryHandlingUnit_OrganizationId_HandlingUnitId",
                table: "DeliveryHandlingUnit",
                columns: new[] { "OrganizationId", "HandlingUnitId" },
                unique: true,
                filter: "\"ReleasedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryHandlingUnitLine_OrganizationId_DeliveryHandlingUni~",
                table: "DeliveryHandlingUnitLine",
                columns: new[] { "OrganizationId", "DeliveryHandlingUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryHandlingUnitLine_OrganizationId_DeliveryLineId",
                table: "DeliveryHandlingUnitLine",
                columns: new[] { "OrganizationId", "DeliveryLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryHandlingUnitLine_OrganizationId_HandlingUnitLineId",
                table: "DeliveryHandlingUnitLine",
                columns: new[] { "OrganizationId", "HandlingUnitLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryLine_OrganizationId_DeliveryId",
                table: "DeliveryLine",
                columns: new[] { "OrganizationId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryLine_OrganizationId_LotId",
                table: "DeliveryLine",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryLine_OrganizationId_ReplenishmentRequestLineId",
                table: "DeliveryLine",
                columns: new[] { "OrganizationId", "ReplenishmentRequestLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandObservation_OrganizationId_ProductId",
                table: "DemandObservation",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandObservation_OrganizationId_ServicePointId_ProductId_P~",
                table: "DemandObservation",
                columns: new[] { "OrganizationId", "ServicePointId", "ProductId", "PeriodFromUtc", "PeriodToUtc", "SourceKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evidence_OrganizationId_ReceiptId_StoredFileId",
                table: "Evidence",
                columns: new[] { "OrganizationId", "ReceiptId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evidence_OrganizationId_ReceiptLineId",
                table: "Evidence",
                columns: new[] { "OrganizationId", "ReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Evidence_OrganizationId_StoredFileId",
                table: "Evidence",
                columns: new[] { "OrganizationId", "StoredFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Evidence_UploadedByAccountId",
                table: "Evidence",
                column: "UploadedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldDefinition_EntityCode_FieldCode",
                table: "FieldDefinition",
                columns: new[] { "EntityCode", "FieldCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormAttachment_OrganizationId_FormSubmissionId_StoredFileId~",
                table: "FormAttachment",
                columns: new[] { "OrganizationId", "FormSubmissionId", "StoredFileId", "TemplateFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormAttachment_OrganizationId_StoredFileId",
                table: "FormAttachment",
                columns: new[] { "OrganizationId", "StoredFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAttachment_OrganizationId_TemplateFieldId",
                table: "FormAttachment",
                columns: new[] { "OrganizationId", "TemplateFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmission_OrganizationId_DeliveryId",
                table: "FormSubmission",
                columns: new[] { "OrganizationId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmission_OrganizationId_InventoryMovementId",
                table: "FormSubmission",
                columns: new[] { "OrganizationId", "InventoryMovementId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmission_OrganizationId_ReceiptId",
                table: "FormSubmission",
                columns: new[] { "OrganizationId", "ReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmission_OrganizationId_ServicePointId",
                table: "FormSubmission",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmission_OrganizationId_SubmittedByAccountId_Operatio~",
                table: "FormSubmission",
                columns: new[] { "OrganizationId", "SubmittedByAccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmission_OrganizationId_SupersedesSubmissionId",
                table: "FormSubmission",
                columns: new[] { "OrganizationId", "SupersedesSubmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmission_OrganizationId_TemplateVersionId",
                table: "FormSubmission",
                columns: new[] { "OrganizationId", "TemplateVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmission_SubmittedByAccountId",
                table: "FormSubmission",
                column: "SubmittedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnit_OrganizationId_Code",
                table: "HandlingUnit",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnit_OrganizationId_LocationId",
                table: "HandlingUnit",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitLine_OrganizationId_HandlingUnitId_LotId",
                table: "HandlingUnitLine",
                columns: new[] { "OrganizationId", "HandlingUnitId", "LotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitLine_OrganizationId_LotId",
                table: "HandlingUnitLine",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_AccountId",
                table: "IdempotencyRecord",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_OrganizationId_AccountId_ActionCode_Opera~",
                table: "IdempotencyRecord",
                columns: new[] { "OrganizationId", "AccountId", "ActionCode", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incident_OrganizationId_RoutePlanId",
                table: "Incident",
                columns: new[] { "OrganizationId", "RoutePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_OrganizationId_ScenarioEventId",
                table: "Incident",
                columns: new[] { "OrganizationId", "ScenarioEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_OrganizationId_ServicePointId",
                table: "Incident",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_OrganizationId_SeverityOptionId",
                table: "Incident",
                columns: new[] { "OrganizationId", "SeverityOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Incident_Position",
                table: "Incident",
                column: "Position")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_ReportedByAccountId",
                table: "Incident",
                column: "ReportedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRoadRestriction_OrganizationId_IncidentId",
                table: "IncidentRoadRestriction",
                columns: new[] { "OrganizationId", "IncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRoadRestriction_OrganizationId_RoadMatrixEdgeId",
                table: "IncidentRoadRestriction",
                columns: new[] { "OrganizationId", "RoadMatrixEdgeId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnection_OrganizationId_Code",
                table: "IntegrationConnection",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnection_OrganizationId_ConfigurationVersionId",
                table: "IntegrationConnection",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRun_OrganizationId_IntegrationConnectionId_RunKey",
                table: "IntegrationRun",
                columns: new[] { "OrganizationId", "IntegrationConnectionId", "RunKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRun_OrganizationId_JobExecutionId",
                table: "IntegrationRun",
                columns: new[] { "OrganizationId", "JobExecutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_ActorAccountId",
                table: "InventoryMovement",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_ConsumptionId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "ConsumptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_DeliveryLineId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "DeliveryLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_FromLocationId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "FromLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_LotId_ReceivedAtServer",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "LotId", "ReceivedAtServer" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_ReceiptLineId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "ReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_RecordedSequence",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "RecordedSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_ToLocationId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "ToLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshot_CreatedByAccountId",
                table: "InventorySnapshot",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotLine_OrganizationId_InventorySnapshotId_Lo~",
                table: "InventorySnapshotLine",
                columns: new[] { "OrganizationId", "InventorySnapshotId", "LocationId", "LotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotLine_OrganizationId_LocationId",
                table: "InventorySnapshotLine",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshotLine_OrganizationId_LotId",
                table: "InventorySnapshotLine",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecution_OrganizationId_RequestedByAccountId_OperationId",
                table: "JobExecution",
                columns: new[] { "OrganizationId", "RequestedByAccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobExecution_RequestedByAccountId",
                table: "JobExecution",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Location_OrganizationId_Code",
                table: "Location",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Location_OrganizationId_ServicePointId",
                table: "Location",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_Location_OrganizationId_VehicleId",
                table: "Location",
                columns: new[] { "OrganizationId", "VehicleId" },
                unique: true,
                filter: "\"VehicleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Location_Position",
                table: "Location",
                column: "Position")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_Lot_OrganizationId_ConditionOptionId",
                table: "Lot",
                columns: new[] { "OrganizationId", "ConditionOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Lot_OrganizationId_ProductId_Code",
                table: "Lot",
                columns: new[] { "OrganizationId", "ProductId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Message_AuthorAccountId",
                table: "Message",
                column: "AuthorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_OrganizationId_AuthorAccountId_OperationId",
                table: "Message",
                columns: new[] { "OrganizationId", "AuthorAccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Message_OrganizationId_ConversationId_ReceivedAtServer_Id",
                table: "Message",
                columns: new[] { "OrganizationId", "ConversationId", "ReceivedAtServer", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachment_OrganizationId_MessageId_StoredFileId",
                table: "MessageAttachment",
                columns: new[] { "OrganizationId", "MessageId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachment_OrganizationId_StoredFileId",
                table: "MessageAttachment",
                columns: new[] { "OrganizationId", "StoredFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_OrganizationId_RecipientAccessId_SourceEventId",
                table: "Notification",
                columns: new[] { "OrganizationId", "RecipientAccessId", "SourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_OrganizationId_SourceEventId",
                table: "Notification",
                columns: new[] { "OrganizationId", "SourceEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Organization_Code",
                table: "Organization",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAccess_AccountId",
                table: "OrganizationAccess",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAccess_GrantedByAccountId",
                table: "OrganizationAccess",
                column: "GrantedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAccess_OrganizationId_AccountId",
                table: "OrganizationAccess",
                columns: new[] { "OrganizationId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_OrganizationId_CreatedAtUtc",
                table: "OutboxEvent",
                columns: new[] { "OrganizationId", "CreatedAtUtc" },
                filter: "\"PublishedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Code",
                table: "Permission",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitConversion_OrganizationId_ProductId_PackagingCode",
                table: "ProductUnitConversion",
                columns: new[] { "OrganizationId", "ProductId", "PackagingCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrAccessToken_CreatedByAccountId",
                table: "QrAccessToken",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_QrAccessToken_OrganizationId_HandlingUnitId",
                table: "QrAccessToken",
                columns: new[] { "OrganizationId", "HandlingUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_QrAccessToken_OrganizationId_TokenHash",
                table: "QrAccessToken",
                columns: new[] { "OrganizationId", "TokenHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RbacChange_ActorAccountId",
                table: "RbacChange",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RbacChange_OrganizationId_TargetAccessId",
                table: "RbacChange",
                columns: new[] { "OrganizationId", "TargetAccessId" });

            migrationBuilder.CreateIndex(
                name: "IX_RbacChange_OrganizationId_TargetRoleId",
                table: "RbacChange",
                columns: new[] { "OrganizationId", "TargetRoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_ActorAccountId",
                table: "Receipt",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_OrganizationId_ActorAccountId_OperationId",
                table: "Receipt",
                columns: new[] { "OrganizationId", "ActorAccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_OrganizationId_DeliveryId",
                table: "Receipt",
                columns: new[] { "OrganizationId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_OrganizationId_StateId",
                table: "Receipt",
                columns: new[] { "OrganizationId", "StateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_OrganizationId_WorkflowVersionId_StateId",
                table: "Receipt",
                columns: new[] { "OrganizationId", "WorkflowVersionId", "StateId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptLine_OrganizationId_DeliveryLineId",
                table: "ReceiptLine",
                columns: new[] { "OrganizationId", "DeliveryLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptLine_OrganizationId_ReceiptId_DeliveryLineId",
                table: "ReceiptLine",
                columns: new[] { "OrganizationId", "ReceiptId", "DeliveryLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptLine_OrganizationId_RejectionReasonOptionId",
                table: "ReceiptLine",
                columns: new[] { "OrganizationId", "RejectionReasonOptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptLine_OrganizationId_ReturnLocationId",
                table: "ReceiptLine",
                columns: new[] { "OrganizationId", "ReturnLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_OrganizationId_ConfigurationVersionId",
                table: "ReplenishmentRequest",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_OrganizationId_ServicePointId",
                table: "ReplenishmentRequest",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_OrganizationId_StateId",
                table: "ReplenishmentRequest",
                columns: new[] { "OrganizationId", "StateId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_OrganizationId_WorkflowVersionId_State~",
                table: "ReplenishmentRequest",
                columns: new[] { "OrganizationId", "WorkflowVersionId", "StateId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_RequestedByAccountId",
                table: "ReplenishmentRequest",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequestLine_OrganizationId_ProductId",
                table: "ReplenishmentRequestLine",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequestLine_OrganizationId_ReplenishmentReques~",
                table: "ReplenishmentRequestLine",
                columns: new[] { "OrganizationId", "ReplenishmentRequestId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_DeliveryLineId",
                table: "Reservation",
                columns: new[] { "OrganizationId", "DeliveryLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_LocationId",
                table: "Reservation",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_LotId",
                table: "Reservation",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskCalculationRun_OrganizationId_ConfigurationVersionId",
                table: "RiskCalculationRun",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskCalculationRun_OrganizationId_InventorySnapshotId",
                table: "RiskCalculationRun",
                columns: new[] { "OrganizationId", "InventorySnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskCalculationRun_OrganizationId_JobExecutionId",
                table: "RiskCalculationRun",
                columns: new[] { "OrganizationId", "JobExecutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskExplanation_OrganizationId_IntegrationRunId",
                table: "RiskExplanation",
                columns: new[] { "OrganizationId", "IntegrationRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskExplanation_OrganizationId_RiskSnapshotId",
                table: "RiskExplanation",
                columns: new[] { "OrganizationId", "RiskSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskExplanation_OrganizationId_TemplateVersionId",
                table: "RiskExplanation",
                columns: new[] { "OrganizationId", "TemplateVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskSnapshot_OrganizationId_ProductId",
                table: "RiskSnapshot",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskSnapshot_OrganizationId_RiskCalculationRunId_ServicePoi~",
                table: "RiskSnapshot",
                columns: new[] { "OrganizationId", "RiskCalculationRunId", "ServicePointId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskSnapshot_OrganizationId_ServicePointId_ProductId_RiskCa~",
                table: "RiskSnapshot",
                columns: new[] { "OrganizationId", "ServicePointId", "ProductId", "RiskCalculationRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadMatrix_OrganizationId_ConfigurationVersionId",
                table: "RoadMatrix",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadMatrix_OrganizationId_DatasetVersionId",
                table: "RoadMatrix",
                columns: new[] { "OrganizationId", "DatasetVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadMatrixEdge_OrganizationId_FromLocationId",
                table: "RoadMatrixEdge",
                columns: new[] { "OrganizationId", "FromLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadMatrixEdge_OrganizationId_RoadMatrixId_FromLocationId_T~",
                table: "RoadMatrixEdge",
                columns: new[] { "OrganizationId", "RoadMatrixId", "FromLocationId", "ToLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadMatrixEdge_OrganizationId_ToLocationId",
                table: "RoadMatrixEdge",
                columns: new[] { "OrganizationId", "ToLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Role_OrganizationId_Code",
                table: "Role",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleFieldPermission_FieldDefinitionId",
                table: "RoleFieldPermission",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleFieldPermission_OrganizationId_RoleId_FieldDefinitionId",
                table: "RoleFieldPermission",
                columns: new[] { "OrganizationId", "RoleId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_OrganizationId_RoleId_PermissionId",
                table: "RolePermission",
                columns: new[] { "OrganizationId", "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlan_OrganizationId_Code_Revision",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "Code", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlan_OrganizationId_ConfigurationVersionId",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlan_OrganizationId_RoadMatrixId",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "RoadMatrixId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlan_OrganizationId_StateId",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "StateId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlan_OrganizationId_SupersedesRoutePlanId",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "SupersedesRoutePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlan_OrganizationId_VehicleId",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlan_OrganizationId_WorkflowVersionId_StateId",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "WorkflowVersionId", "StateId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlan_PublishedByAccountId",
                table: "RoutePlan",
                column: "PublishedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlanIncident_OrganizationId_IncidentId",
                table: "RoutePlanIncident",
                columns: new[] { "OrganizationId", "IncidentId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlanIncident_OrganizationId_RoutePlanId_IncidentId",
                table: "RoutePlanIncident",
                columns: new[] { "OrganizationId", "RoutePlanId", "IncidentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlanningSnapshot_OrganizationId_RoutePlanId",
                table: "RoutePlanningSnapshot",
                columns: new[] { "OrganizationId", "RoutePlanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteStop_OrganizationId_LocationId",
                table: "RouteStop",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStop_OrganizationId_RoutePlanId_SequenceNumber",
                table: "RouteStop",
                columns: new[] { "OrganizationId", "RoutePlanId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteStop_OrganizationId_ServicePointId",
                table: "RouteStop",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioDataset_OrganizationId_Code",
                table: "ScenarioDataset",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioDatasetVersion_OrganizationId_ScenarioDatasetId_Rev~",
                table: "ScenarioDatasetVersion",
                columns: new[] { "OrganizationId", "ScenarioDatasetId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioDatasetVersion_OrganizationId_StoredFileId",
                table: "ScenarioDatasetVersion",
                columns: new[] { "OrganizationId", "StoredFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioEvent_OrganizationId_ScenarioDatasetVersionId_Seque~",
                table: "ScenarioEvent",
                columns: new[] { "OrganizationId", "ScenarioDatasetVersionId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePointContact_OrganizationId_ServicePointId",
                table: "ServicePointContact",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePointWindow_OrganizationId_ServicePointId",
                table: "ServicePointWindow",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationArtifact_OrganizationId_SimulationRunId_Kind",
                table: "SimulationArtifact",
                columns: new[] { "OrganizationId", "SimulationRunId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationArtifact_OrganizationId_StoredFileId",
                table: "SimulationArtifact",
                columns: new[] { "OrganizationId", "StoredFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationEvent_OrganizationId_SimulationRunId_SequenceNumb~",
                table: "SimulationEvent",
                columns: new[] { "OrganizationId", "SimulationRunId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationMetric_OrganizationId_ProductId",
                table: "SimulationMetric",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationMetric_OrganizationId_SimulationRunId_Code_Defin~1",
                table: "SimulationMetric",
                columns: new[] { "OrganizationId", "SimulationRunId", "Code", "DefinitionVersion", "ProductId" },
                unique: true,
                filter: "\"ProductId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationMetric_OrganizationId_SimulationRunId_Code_Defini~",
                table: "SimulationMetric",
                columns: new[] { "OrganizationId", "SimulationRunId", "Code", "DefinitionVersion" },
                unique: true,
                filter: "\"ProductId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationPolicyVersion_OrganizationId_Code_Revision",
                table: "SimulationPolicyVersion",
                columns: new[] { "OrganizationId", "Code", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulationPolicyVersion_OrganizationId_ConfigurationVersion~",
                table: "SimulationPolicyVersion",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRun_OrganizationId_JobExecutionId",
                table: "SimulationRun",
                columns: new[] { "OrganizationId", "JobExecutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRun_OrganizationId_RoadMatrixId",
                table: "SimulationRun",
                columns: new[] { "OrganizationId", "RoadMatrixId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRun_OrganizationId_ScenarioDatasetVersionId",
                table: "SimulationRun",
                columns: new[] { "OrganizationId", "ScenarioDatasetVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRun_OrganizationId_SimulationPolicyVersionId",
                table: "SimulationRun",
                columns: new[] { "OrganizationId", "SimulationPolicyVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_OrganizationId_LocationId_LotId",
                table: "StockBalance",
                columns: new[] { "OrganizationId", "LocationId", "LotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_OrganizationId_LotId",
                table: "StockBalance",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFile_CreatedByAccountId",
                table: "StoredFile",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFile_OrganizationId_StorageKey",
                table: "StoredFile",
                columns: new[] { "OrganizationId", "StorageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncBatch_AccountId",
                table: "SyncBatch",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncBatch_OrganizationId_AccountId_OperationId",
                table: "SyncBatch",
                columns: new[] { "OrganizationId", "AccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncOperation_OrganizationId_IdempotencyRecordId",
                table: "SyncOperation",
                columns: new[] { "OrganizationId", "IdempotencyRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncOperation_OrganizationId_SyncBatchId_IdempotencyRecordId",
                table: "SyncOperation",
                columns: new[] { "OrganizationId", "SyncBatchId", "IdempotencyRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Template_OrganizationId_Code",
                table: "Template",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBinding_OrganizationId_LocationId",
                table: "TemplateBinding",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBinding_OrganizationId_ServicePointId",
                table: "TemplateBinding",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBinding_OrganizationId_TemplateVersionId",
                table: "TemplateBinding",
                columns: new[] { "OrganizationId", "TemplateVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateField_OrganizationId_CatalogId",
                table: "TemplateField",
                columns: new[] { "OrganizationId", "CatalogId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateField_OrganizationId_TemplateVersionId_Key",
                table: "TemplateField",
                columns: new[] { "OrganizationId", "TemplateVersionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFieldAccess_OrganizationId_RoleId_TemplateFieldId",
                table: "TemplateFieldAccess",
                columns: new[] { "OrganizationId", "RoleId", "TemplateFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFieldAccess_OrganizationId_TemplateFieldId",
                table: "TemplateFieldAccess",
                columns: new[] { "OrganizationId", "TemplateFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFieldOption_OrganizationId_TemplateFieldId_Code",
                table: "TemplateFieldOption",
                columns: new[] { "OrganizationId", "TemplateFieldId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersion_CreatedByAccountId",
                table: "TemplateVersion",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersion_OrganizationId_TemplateId_Revision",
                table: "TemplateVersion",
                columns: new[] { "OrganizationId", "TemplateId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersion_OrganizationId_WorkflowVersionId",
                table: "TemplateVersion",
                columns: new[] { "OrganizationId", "WorkflowVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_OrganizationId_Code",
                table: "Vehicle",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAvailability_OrganizationId_DepotLocationId",
                table: "VehicleAvailability",
                columns: new[] { "OrganizationId", "DepotLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAvailability_OrganizationId_VehicleId",
                table: "VehicleAvailability",
                columns: new[] { "OrganizationId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePosition_OrganizationId_RecordedByAccountId_Operatio~",
                table: "VehiclePosition",
                columns: new[] { "OrganizationId", "RecordedByAccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePosition_OrganizationId_RoutePlanId",
                table: "VehiclePosition",
                columns: new[] { "OrganizationId", "RoutePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePosition_OrganizationId_VehicleId_ReceivedAtServer",
                table: "VehiclePosition",
                columns: new[] { "OrganizationId", "VehicleId", "ReceivedAtServer" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePosition_Position",
                table: "VehiclePosition",
                column: "Position")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePosition_RecordedByAccountId",
                table: "VehiclePosition",
                column: "RecordedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_OrganizationId_Code",
                table: "WorkflowDefinition",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_OrganizationId_WorkflowVersionId_Code",
                table: "WorkflowState",
                columns: new[] { "OrganizationId", "WorkflowVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransition_OrganizationId_FromStateId",
                table: "WorkflowTransition",
                columns: new[] { "OrganizationId", "FromStateId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransition_OrganizationId_ToStateId",
                table: "WorkflowTransition",
                columns: new[] { "OrganizationId", "ToStateId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransition_OrganizationId_WorkflowVersionId_FromSta~",
                table: "WorkflowTransition",
                columns: new[] { "OrganizationId", "WorkflowVersionId", "FromStateId", "ToStateId", "HandlerCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransition_OrganizationId_WorkflowVersionId_ToState~",
                table: "WorkflowTransition",
                columns: new[] { "OrganizationId", "WorkflowVersionId", "ToStateId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransition_PermissionId",
                table: "WorkflowTransition",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersion_CreatedByAccountId",
                table: "WorkflowVersion",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersion_OrganizationId_WorkflowDefinitionId_Revision",
                table: "WorkflowVersion",
                columns: new[] { "OrganizationId", "WorkflowDefinitionId", "Revision" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Location_OrganizationId_DestinationLocationId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "DestinationLocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Location_OrganizationId_OriginLocationId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "OriginLocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Organization_OrganizationId",
                table: "Delivery",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_RouteStop_OrganizationId_RouteStopId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "RouteStopId" },
                principalTable: "RouteStop",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_ServicePoint_OrganizationId_ServicePointId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "ServicePointId" },
                principalTable: "ServicePoint",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_WorkflowState_OrganizationId_StateId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "StateId" },
                principalTable: "WorkflowState",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_WorkflowState_OrganizationId_WorkflowVersionId_Sta~",
                table: "Delivery",
                columns: new[] { "OrganizationId", "WorkflowVersionId", "StateId" },
                principalTable: "WorkflowState",
                principalColumns: new[] { "OrganizationId", "WorkflowVersionId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_WorkflowVersion_OrganizationId_WorkflowVersionId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "WorkflowVersionId" },
                principalTable: "WorkflowVersion",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Organization_OrganizationId",
                table: "Product",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServicePoint_CatalogOption_OrganizationId_TypeOptionId",
                table: "ServicePoint",
                columns: new[] { "OrganizationId", "TypeOptionId" },
                principalTable: "CatalogOption",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServicePoint_ConfigurationVersion_OrganizationId_Configurat~",
                table: "ServicePoint",
                columns: new[] { "OrganizationId", "ConfigurationVersionId" },
                principalTable: "ConfigurationVersion",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServicePoint_Organization_OrganizationId",
                table: "ServicePoint",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(ReadSql("Schema95Security.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ReadSql("Schema95DownGuard.sql"));

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Location_OrganizationId_DestinationLocationId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Location_OrganizationId_OriginLocationId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Organization_OrganizationId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_RouteStop_OrganizationId_RouteStopId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_ServicePoint_OrganizationId_ServicePointId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_WorkflowState_OrganizationId_StateId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_WorkflowState_OrganizationId_WorkflowVersionId_Sta~",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_WorkflowVersion_OrganizationId_WorkflowVersionId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_Organization_OrganizationId",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePoint_CatalogOption_OrganizationId_TypeOptionId",
                table: "ServicePoint");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePoint_ConfigurationVersion_OrganizationId_Configurat~",
                table: "ServicePoint");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePoint_Organization_OrganizationId",
                table: "ServicePoint");

            migrationBuilder.DropTable(
                name: "AccessDelegation");

            migrationBuilder.DropTable(
                name: "AccessLocation");

            migrationBuilder.DropTable(
                name: "AccessRole");

            migrationBuilder.DropTable(
                name: "AccessServicePoint");

            migrationBuilder.DropTable(
                name: "AnomalyCase");

            migrationBuilder.DropTable(
                name: "AuditEvent");

            migrationBuilder.DropTable(
                name: "ConfigurationActivation");

            migrationBuilder.DropTable(
                name: "ConversationParticipant");

            migrationBuilder.DropTable(
                name: "CustodyEvent");

            migrationBuilder.DropTable(
                name: "DeliveryAssignment");

            migrationBuilder.DropTable(
                name: "DeliveryHandlingUnitLine");

            migrationBuilder.DropTable(
                name: "DemandObservation");

            migrationBuilder.DropTable(
                name: "Evidence");

            migrationBuilder.DropTable(
                name: "FormAttachment");

            migrationBuilder.DropTable(
                name: "IncidentRoadRestriction");

            migrationBuilder.DropTable(
                name: "InventorySnapshotLine");

            migrationBuilder.DropTable(
                name: "MessageAttachment");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "ProductUnitConversion");

            migrationBuilder.DropTable(
                name: "QrAccessToken");

            migrationBuilder.DropTable(
                name: "RbacChange");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "RiskExplanation");

            migrationBuilder.DropTable(
                name: "RoleFieldPermission");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "RoutePlanIncident");

            migrationBuilder.DropTable(
                name: "RoutePlanningSnapshot");

            migrationBuilder.DropTable(
                name: "RouteStop");

            migrationBuilder.DropTable(
                name: "ServicePointContact");

            migrationBuilder.DropTable(
                name: "ServicePointWindow");

            migrationBuilder.DropTable(
                name: "SimulationArtifact");

            migrationBuilder.DropTable(
                name: "SimulationEvent");

            migrationBuilder.DropTable(
                name: "SimulationMetric");

            migrationBuilder.DropTable(
                name: "StockBalance");

            migrationBuilder.DropTable(
                name: "SyncOperation");

            migrationBuilder.DropTable(
                name: "TemplateBinding");

            migrationBuilder.DropTable(
                name: "TemplateFieldAccess");

            migrationBuilder.DropTable(
                name: "TemplateFieldOption");

            migrationBuilder.DropTable(
                name: "VehicleAvailability");

            migrationBuilder.DropTable(
                name: "VehiclePosition");

            migrationBuilder.DropTable(
                name: "WorkflowTransition");

            migrationBuilder.DropTable(
                name: "DeliveryHandlingUnit");

            migrationBuilder.DropTable(
                name: "HandlingUnitLine");

            migrationBuilder.DropTable(
                name: "FormSubmission");

            migrationBuilder.DropTable(
                name: "RoadMatrixEdge");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "OutboxEvent");

            migrationBuilder.DropTable(
                name: "OrganizationAccess");

            migrationBuilder.DropTable(
                name: "IntegrationRun");

            migrationBuilder.DropTable(
                name: "RiskSnapshot");

            migrationBuilder.DropTable(
                name: "FieldDefinition");

            migrationBuilder.DropTable(
                name: "Incident");

            migrationBuilder.DropTable(
                name: "SimulationRun");

            migrationBuilder.DropTable(
                name: "IdempotencyRecord");

            migrationBuilder.DropTable(
                name: "SyncBatch");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "TemplateField");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "HandlingUnit");

            migrationBuilder.DropTable(
                name: "InventoryMovement");

            migrationBuilder.DropTable(
                name: "Conversation");

            migrationBuilder.DropTable(
                name: "IntegrationConnection");

            migrationBuilder.DropTable(
                name: "RiskCalculationRun");

            migrationBuilder.DropTable(
                name: "ScenarioEvent");

            migrationBuilder.DropTable(
                name: "SimulationPolicyVersion");

            migrationBuilder.DropTable(
                name: "TemplateVersion");

            migrationBuilder.DropTable(
                name: "Consumption");

            migrationBuilder.DropTable(
                name: "ReceiptLine");

            migrationBuilder.DropTable(
                name: "RoutePlan");

            migrationBuilder.DropTable(
                name: "InventorySnapshot");

            migrationBuilder.DropTable(
                name: "JobExecution");

            migrationBuilder.DropTable(
                name: "Template");

            migrationBuilder.DropTable(
                name: "DeliveryLine");

            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropTable(
                name: "Receipt");

            migrationBuilder.DropTable(
                name: "RoadMatrix");

            migrationBuilder.DropTable(
                name: "Lot");

            migrationBuilder.DropTable(
                name: "ReplenishmentRequestLine");

            migrationBuilder.DropTable(
                name: "Vehicle");

            migrationBuilder.DropTable(
                name: "ScenarioDatasetVersion");

            migrationBuilder.DropTable(
                name: "CatalogOption");

            migrationBuilder.DropTable(
                name: "ReplenishmentRequest");

            migrationBuilder.DropTable(
                name: "ScenarioDataset");

            migrationBuilder.DropTable(
                name: "StoredFile");

            migrationBuilder.DropTable(
                name: "Catalog");

            migrationBuilder.DropTable(
                name: "ConfigurationVersion");

            migrationBuilder.DropTable(
                name: "WorkflowState");

            migrationBuilder.DropTable(
                name: "ConfigurationDefinition");

            migrationBuilder.DropTable(
                name: "WorkflowVersion");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "WorkflowDefinition");

            migrationBuilder.DropTable(
                name: "Organization");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ServicePoint_OrganizationId_Id",
                table: "ServicePoint");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicePoint",
                table: "ServicePoint");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoint_OrganizationId_Code",
                table: "ServicePoint");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoint_OrganizationId_ConfigurationVersionId",
                table: "ServicePoint");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoint_OrganizationId_TypeOptionId",
                table: "ServicePoint");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoint_Position",
                table: "ServicePoint");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServicePoint_Version",
                table: "ServicePoint");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Product_OrganizationId_Id",
                table: "Product");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Product",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_OrganizationId_Sku",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Version",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_VolumePerBaseUnit",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_WeightPerBaseUnit",
                table: "Product");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Delivery_OrganizationId_Id",
                table: "Delivery");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Delivery",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_Code",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_DestinationLocationId",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_OriginLocationId",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_RouteStopId",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_ServicePointId",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_StateId",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_WorkflowVersionId_StateId",
                table: "Delivery");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Delivery_Locations",
                table: "Delivery");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Delivery_Version",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "ConfigurationVersionId",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "Criticality",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "TypeOptionId",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "BaseUnit",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "VolumePerBaseUnit",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "WeightPerBaseUnit",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "DestinationLocationId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "DispatchedAtUtc",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "OriginLocationId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "RouteStopId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "ServicePointId",
                table: "Delivery");

            migrationBuilder.RenameTable(
                name: "ServicePoint",
                newName: "ServicePoints");

            migrationBuilder.RenameTable(
                name: "Product",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "Delivery",
                newName: "Deliveries");

            migrationBuilder.RenameColumn(
                name: "WorkflowVersionId",
                table: "Deliveries",
                newName: "OriginServicePointId");

            migrationBuilder.RenameColumn(
                name: "StateId",
                table: "Deliveries",
                newName: "DestinationServicePointId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ServicePoints",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ServicePoints",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "ServicePoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "ServicePoints",
                type: "geometry",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "ServicePoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ServicePoints",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "Deliveries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "Deliveries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicePoints",
                table: "ServicePoints",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Deliveries",
                table: "Deliveries",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpirationDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LotNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_ServicePoints_ServicePointId",
                        column: x => x.ServicePointId,
                        principalTable: "ServicePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePointId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskAssessments_ServicePoints_ServicePointId",
                        column: x => x.ServicePointId,
                        principalTable: "ServicePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_DestinationServicePointId",
                table: "Deliveries",
                column: "DestinationServicePointId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_OriginServicePointId",
                table: "Deliveries",
                column: "OriginServicePointId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProductId",
                table: "InventoryItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ServicePointId",
                table: "InventoryItems",
                column: "ServicePointId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAssessments_ServicePointId",
                table: "RiskAssessments",
                column: "ServicePointId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_ServicePoints_DestinationServicePointId",
                table: "Deliveries",
                column: "DestinationServicePointId",
                principalTable: "ServicePoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_ServicePoints_OriginServicePointId",
                table: "Deliveries",
                column: "OriginServicePointId",
                principalTable: "ServicePoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
        private static string ReadSql(string name)
        {
            using var stream = typeof(CompleteSystemSchema95).Assembly.GetManifestResourceStream(
                "IstpetDev.Infrastructure.Persistence.Migrations.Sql." + name)
                ?? throw new InvalidOperationException("Missing embedded migration SQL: " + name);
            using var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
