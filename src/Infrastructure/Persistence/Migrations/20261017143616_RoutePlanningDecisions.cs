using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoutePlanningDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoutePlanningDecision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AcceptedOptionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    RouteIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePlanningDecision", x => x.Id);
                    table.UniqueConstraint("AK_RoutePlanningDecision_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_RoutePlanningDecision_Account_OrganizationId_CreatedByAccou~",
                        columns: x => new { x.OrganizationId, x.CreatedByAccountId },
                        principalTable: "Account",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_RoutePlanningDecision_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoutePlanningDecision_OrganizationId_CreatedByAccountId_Cre~",
                table: "RoutePlanningDecision",
                columns: new[] { "OrganizationId", "CreatedByAccountId", "CreatedAtUtc" });
            migrationBuilder.Sql(DemoGrants.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoutePlanningDecision");
        }
    }
}
