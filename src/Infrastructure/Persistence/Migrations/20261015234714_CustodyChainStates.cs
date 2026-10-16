using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustodyChainStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustodyEvent_OrganizationId_OperationId",
                table: "CustodyEvent");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_OperationId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "OperationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustodyEvent_OrganizationId_OperationId",
                table: "CustodyEvent");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_OperationId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "OperationId" },
                unique: true);
        }
    }
}
