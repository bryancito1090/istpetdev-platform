using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FieldOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RetainUntilUtc",
                table: "IdempotencyRecord",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryLineId",
                table: "HandlingUnitLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryId",
                table: "HandlingUnit",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ByteSize",
                table: "Evidence",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Evidence",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "QrAccessToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HandlingUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrAccessToken", x => x.Id);
                    table.UniqueConstraint("AK_QrAccessToken_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_QrAccessToken_Account_OrganizationId_CreatedByAccountId",
                        columns: x => new { x.OrganizationId, x.CreatedByAccountId },
                        principalTable: "Account",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_QrAccessToken_HandlingUnit_OrganizationId_HandlingUnitId",
                        columns: x => new { x.OrganizationId, x.HandlingUnitId },
                        principalTable: "HandlingUnit",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_QrAccessToken_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitLine_OrganizationId_DeliveryLineId",
                table: "HandlingUnitLine",
                columns: new[] { "OrganizationId", "DeliveryLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnit_OrganizationId_DeliveryId",
                table: "HandlingUnit",
                columns: new[] { "OrganizationId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_QrAccessToken_OrganizationId_CreatedByAccountId",
                table: "QrAccessToken",
                columns: new[] { "OrganizationId", "CreatedByAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_QrAccessToken_OrganizationId_HandlingUnitId",
                table: "QrAccessToken",
                columns: new[] { "OrganizationId", "HandlingUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_QrAccessToken_OrganizationId_TokenHash",
                table: "QrAccessToken",
                columns: new[] { "OrganizationId", "TokenHash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HandlingUnit_Delivery_OrganizationId_DeliveryId",
                table: "HandlingUnit",
                columns: new[] { "OrganizationId", "DeliveryId" },
                principalTable: "Delivery",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_HandlingUnitLine_DeliveryLine_OrganizationId_DeliveryLineId",
                table: "HandlingUnitLine",
                columns: new[] { "OrganizationId", "DeliveryLineId" },
                principalTable: "DeliveryLine",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.Sql(DemoGrants.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HandlingUnit_Delivery_OrganizationId_DeliveryId",
                table: "HandlingUnit");

            migrationBuilder.DropForeignKey(
                name: "FK_HandlingUnitLine_DeliveryLine_OrganizationId_DeliveryLineId",
                table: "HandlingUnitLine");

            migrationBuilder.DropTable(
                name: "QrAccessToken");

            migrationBuilder.DropIndex(
                name: "IX_HandlingUnitLine_OrganizationId_DeliveryLineId",
                table: "HandlingUnitLine");

            migrationBuilder.DropIndex(
                name: "IX_HandlingUnit_OrganizationId_DeliveryId",
                table: "HandlingUnit");

            migrationBuilder.DropColumn(
                name: "RetainUntilUtc",
                table: "IdempotencyRecord");

            migrationBuilder.DropColumn(
                name: "DeliveryLineId",
                table: "HandlingUnitLine");

            migrationBuilder.DropColumn(
                name: "DeliveryId",
                table: "HandlingUnit");

            migrationBuilder.DropColumn(
                name: "ByteSize",
                table: "Evidence");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Evidence");
        }
    }
}
