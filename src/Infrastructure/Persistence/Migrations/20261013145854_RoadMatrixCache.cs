using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoadMatrixCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadMatrixEdge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromLat = table.Column<decimal>(type: "numeric(9,5)", precision: 9, scale: 5, nullable: false),
                    FromLon = table.Column<decimal>(type: "numeric(9,5)", precision: 9, scale: 5, nullable: false),
                    ToLat = table.Column<decimal>(type: "numeric(9,5)", precision: 9, scale: 5, nullable: false),
                    ToLon = table.Column<decimal>(type: "numeric(9,5)", precision: 9, scale: 5, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DurationMinutes = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    EncodedPolyline = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RetrievedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadMatrixEdge", x => x.Id);
                    table.UniqueConstraint("AK_RoadMatrixEdge_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_RoadMatrixEdge_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteStockReservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockBalanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStockReservation", x => x.Id);
                    table.UniqueConstraint("AK_RouteStockReservation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_RouteStockReservation_DeliveryLine_OrganizationId_DeliveryL~",
                        columns: x => new { x.OrganizationId, x.DeliveryLineId },
                        principalTable: "DeliveryLine",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_RouteStockReservation_Delivery_OrganizationId_DeliveryId",
                        columns: x => new { x.OrganizationId, x.DeliveryId },
                        principalTable: "Delivery",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_RouteStockReservation_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_RouteStockReservation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteStockReservation_RoutePlan_OrganizationId_RoutePlanId",
                        columns: x => new { x.OrganizationId, x.RoutePlanId },
                        principalTable: "RoutePlan",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_RouteStockReservation_StockBalance_OrganizationId_StockBala~",
                        columns: x => new { x.OrganizationId, x.StockBalanceId },
                        principalTable: "StockBalance",
                        principalColumns: new[] { "OrganizationId", "Id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadMatrixEdge_OrganizationId_FromLat_FromLon_ToLat_ToLon",
                table: "RoadMatrixEdge",
                columns: new[] { "OrganizationId", "FromLat", "FromLon", "ToLat", "ToLon" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteStockReservation_OrganizationId_DeliveryId",
                table: "RouteStockReservation",
                columns: new[] { "OrganizationId", "DeliveryId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStockReservation_OrganizationId_DeliveryLineId",
                table: "RouteStockReservation",
                columns: new[] { "OrganizationId", "DeliveryLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStockReservation_OrganizationId_LotId",
                table: "RouteStockReservation",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStockReservation_OrganizationId_RoutePlanId_DeliveryLi~",
                table: "RouteStockReservation",
                columns: new[] { "OrganizationId", "RoutePlanId", "DeliveryLineId", "LotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteStockReservation_OrganizationId_StockBalanceId_Status",
                table: "RouteStockReservation",
                columns: new[] { "OrganizationId", "StockBalanceId", "Status" });

            migrationBuilder.Sql(DemoGrants.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadMatrixEdge");

            migrationBuilder.DropTable(
                name: "RouteStockReservation");
        }
    }
}
