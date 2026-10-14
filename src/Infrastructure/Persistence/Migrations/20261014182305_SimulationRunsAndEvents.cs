using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimulationRunsAndEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalendarHash",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CommitHash",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComparisonId",
                table: "SimulationRun",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DatasetVersion",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "SimulationRun",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostName",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManifestHash",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PolicyVersion",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "SimulationRun",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScenarioCode",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SimulationEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Kind = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RegionCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PointCode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Sku = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Requested = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    Satisfied = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationEvent", x => x.Id);
                    table.UniqueConstraint("AK_SimulationEvent_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_SimulationEvent_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SimulationEvent_SimulationRun_OrganizationId_SimulationRunId",
                        columns: x => new { x.OrganizationId, x.SimulationRunId },
                        principalTable: "SimulationRun",
                        principalColumns: new[] { "OrganizationId", "Id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRun_OrganizationId_ComparisonId",
                table: "SimulationRun",
                columns: new[] { "OrganizationId", "ComparisonId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRun_OrganizationId_Seed_ScenarioCode_PolicyCode",
                table: "SimulationRun",
                columns: new[] { "OrganizationId", "Seed", "ScenarioCode", "PolicyCode" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationEvent_OrganizationId_SimulationRunId",
                table: "SimulationEvent",
                columns: new[] { "OrganizationId", "SimulationRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationEvent_SimulationRunId_Sequence",
                table: "SimulationEvent",
                columns: new[] { "SimulationRunId", "Sequence" },
                unique: true);

            migrationBuilder.Sql(DemoGrants.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimulationEvent");

            migrationBuilder.DropIndex(
                name: "IX_SimulationRun_OrganizationId_ComparisonId",
                table: "SimulationRun");

            migrationBuilder.DropIndex(
                name: "IX_SimulationRun_OrganizationId_Seed_ScenarioCode_PolicyCode",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "CalendarHash",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "CommitHash",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "ComparisonId",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "DatasetVersion",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "HostName",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "ManifestHash",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "PolicyVersion",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "ScenarioCode",
                table: "SimulationRun");
        }
    }
}
