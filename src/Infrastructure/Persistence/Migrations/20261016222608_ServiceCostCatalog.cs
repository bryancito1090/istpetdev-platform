using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ServiceCostCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsageScope",
                table: "Vehicle",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Vehicle",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitValue",
                table: "Product",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitsPerPackage",
                table: "Product",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsageScope",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "UnitValue",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "UnitsPerPackage",
                table: "Product");
        }
    }
}
