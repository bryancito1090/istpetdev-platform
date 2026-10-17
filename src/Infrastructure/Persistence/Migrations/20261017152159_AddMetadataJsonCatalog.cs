using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataJsonCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Vehicle",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Product",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Product");
        }
    }
}
