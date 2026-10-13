using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgentRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverMessage",
                table: "AgentDecision",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExplanationMismatch",
                table: "AgentDecision",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExplanationSource",
                table: "AgentDecision",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AgentRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TriggerJson = table.Column<string>(type: "jsonb", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: false),
                    Model = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    N8nExecutionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FallbackReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ToolCallsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRun", x => x.Id);
                    table.UniqueConstraint("AK_AgentRun_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_AgentRun_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRun_OrganizationId_StartedAtUtc",
                table: "AgentRun",
                columns: new[] { "OrganizationId", "StartedAtUtc" });

            migrationBuilder.Sql(DemoGrants.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentRun");

            migrationBuilder.DropColumn(
                name: "DriverMessage",
                table: "AgentDecision");

            migrationBuilder.DropColumn(
                name: "ExplanationMismatch",
                table: "AgentDecision");

            migrationBuilder.DropColumn(
                name: "ExplanationSource",
                table: "AgentDecision");
        }
    }
}
