using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderBasedRoutingAndWarehouseStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServingWarehouseLocationId",
                table: "ServicePoint",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "RouteStop",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "RouteStop",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NextRequestedQuantity",
                table: "ReceiptLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingQuantity",
                table: "ReceiptLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryOrderId",
                table: "Delivery",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationLocationId",
                table: "Delivery",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransferSuggestion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    SourceCoverageDaysBefore = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    SourceCoverageDaysAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    TargetCoverageDaysBefore = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    TargetCoverageDaysAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    ReviewedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DismissReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferSuggestion", x => x.Id);
                    table.UniqueConstraint("AK_TransferSuggestion_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_TransferSuggestion_Account_OrganizationId_ReviewedByAccount~",
                        columns: x => new { x.OrganizationId, x.ReviewedByAccountId },
                        principalTable: "Account",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_TransferSuggestion_Location_OrganizationId_SourceLocationId",
                        columns: x => new { x.OrganizationId, x.SourceLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_TransferSuggestion_Location_OrganizationId_TargetLocationId",
                        columns: x => new { x.OrganizationId, x.TargetLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_TransferSuggestion_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferSuggestion_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "WarehouseStockPolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    TargetCoverageDays = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    IsManual = table.Column<bool>(type: "boolean", nullable: false),
                    SuggestedMinStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    SuggestedMaxStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    LastCalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStockPolicy", x => x.Id);
                    table.UniqueConstraint("AK_WarehouseStockPolicy_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_WarehouseStockPolicy_Location_OrganizationId_LocationId",
                        columns: x => new { x.OrganizationId, x.LocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_WarehouseStockPolicy_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseStockPolicy_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DestinationServicePointId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiredDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TransferSuggestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrder", x => x.Id);
                    table.UniqueConstraint("AK_DeliveryOrder_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_Location_OrganizationId_DestinationLocationId",
                        columns: x => new { x.OrganizationId, x.DestinationLocationId },
                        principalTable: "Location",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_ServicePoint_OrganizationId_DestinationServic~",
                        columns: x => new { x.OrganizationId, x.DestinationServicePointId },
                        principalTable: "ServicePoint",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_TransferSuggestion_OrganizationId_TransferSug~",
                        columns: x => new { x.OrganizationId, x.TransferSuggestionId },
                        principalTable: "TransferSuggestion",
                        principalColumns: new[] { "OrganizationId", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrderLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrderLine", x => x.Id);
                    table.UniqueConstraint("AK_DeliveryOrderLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_DeliveryOrderLine_DeliveryOrder_OrganizationId_DeliveryOrde~",
                        columns: x => new { x.OrganizationId, x.DeliveryOrderId },
                        principalTable: "DeliveryOrder",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_DeliveryOrderLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeliveryOrderLine_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" });
                });

            migrationBuilder.CreateTable(
                name: "RouteStopLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Action = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStopLine", x => x.Id);
                    table.UniqueConstraint("AK_RouteStopLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_RouteStopLine_DeliveryOrder_OrganizationId_DeliveryOrderId",
                        columns: x => new { x.OrganizationId, x.DeliveryOrderId },
                        principalTable: "DeliveryOrder",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_RouteStopLine_Lot_OrganizationId_LotId",
                        columns: x => new { x.OrganizationId, x.LotId },
                        principalTable: "Lot",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_RouteStopLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteStopLine_Product_OrganizationId_ProductId",
                        columns: x => new { x.OrganizationId, x.ProductId },
                        principalTable: "Product",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_RouteStopLine_RouteStop_OrganizationId_RouteStopId",
                        columns: x => new { x.OrganizationId, x.RouteStopId },
                        principalTable: "RouteStop",
                        principalColumns: new[] { "OrganizationId", "Id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePoint_OrganizationId_ServingWarehouseLocationId",
                table: "ServicePoint",
                columns: new[] { "OrganizationId", "ServingWarehouseLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStop_OrganizationId_LocationId",
                table: "RouteStop",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_DeliveryOrderId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "DeliveryOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_DestinationLocationId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "DestinationLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_OrganizationId_DestinationLocationId",
                table: "DeliveryOrder",
                columns: new[] { "OrganizationId", "DestinationLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_OrganizationId_DestinationServicePointId",
                table: "DeliveryOrder",
                columns: new[] { "OrganizationId", "DestinationServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_OrganizationId_OrderNumber",
                table: "DeliveryOrder",
                columns: new[] { "OrganizationId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_OrganizationId_TransferSuggestionId",
                table: "DeliveryOrder",
                columns: new[] { "OrganizationId", "TransferSuggestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderLine_OrganizationId_DeliveryOrderId",
                table: "DeliveryOrderLine",
                columns: new[] { "OrganizationId", "DeliveryOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderLine_OrganizationId_ProductId",
                table: "DeliveryOrderLine",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStopLine_OrganizationId_DeliveryOrderId",
                table: "RouteStopLine",
                columns: new[] { "OrganizationId", "DeliveryOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStopLine_OrganizationId_LotId",
                table: "RouteStopLine",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStopLine_OrganizationId_ProductId",
                table: "RouteStopLine",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStopLine_OrganizationId_RouteStopId",
                table: "RouteStopLine",
                columns: new[] { "OrganizationId", "RouteStopId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferSuggestion_OrganizationId_ProductId",
                table: "TransferSuggestion",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferSuggestion_OrganizationId_ReviewedByAccountId",
                table: "TransferSuggestion",
                columns: new[] { "OrganizationId", "ReviewedByAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferSuggestion_OrganizationId_SourceLocationId",
                table: "TransferSuggestion",
                columns: new[] { "OrganizationId", "SourceLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferSuggestion_OrganizationId_Status",
                table: "TransferSuggestion",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferSuggestion_OrganizationId_TargetLocationId",
                table: "TransferSuggestion",
                columns: new[] { "OrganizationId", "TargetLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockPolicy_OrganizationId_LocationId_ProductId",
                table: "WarehouseStockPolicy",
                columns: new[] { "OrganizationId", "LocationId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStockPolicy_OrganizationId_ProductId",
                table: "WarehouseStockPolicy",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_DeliveryOrder_OrganizationId_DeliveryOrderId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "DeliveryOrderId" },
                principalTable: "DeliveryOrder",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Location_OrganizationId_DestinationLocationId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "DestinationLocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_RouteStop_Location_OrganizationId_LocationId",
                table: "RouteStop",
                columns: new[] { "OrganizationId", "LocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_ServicePoint_Location_OrganizationId_ServingWarehouseLocati~",
                table: "ServicePoint",
                columns: new[] { "OrganizationId", "ServingWarehouseLocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.Sql(DemoGrants.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_DeliveryOrder_OrganizationId_DeliveryOrderId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Location_OrganizationId_DestinationLocationId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteStop_Location_OrganizationId_LocationId",
                table: "RouteStop");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePoint_Location_OrganizationId_ServingWarehouseLocati~",
                table: "ServicePoint");

            migrationBuilder.DropTable(
                name: "DeliveryOrderLine");

            migrationBuilder.DropTable(
                name: "RouteStopLine");

            migrationBuilder.DropTable(
                name: "WarehouseStockPolicy");

            migrationBuilder.DropTable(
                name: "DeliveryOrder");

            migrationBuilder.DropTable(
                name: "TransferSuggestion");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoint_OrganizationId_ServingWarehouseLocationId",
                table: "ServicePoint");

            migrationBuilder.DropIndex(
                name: "IX_RouteStop_OrganizationId_LocationId",
                table: "RouteStop");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_DeliveryOrderId",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_DestinationLocationId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "ServingWarehouseLocationId",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "RouteStop");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "RouteStop");

            migrationBuilder.DropColumn(
                name: "NextRequestedQuantity",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "RemainingQuantity",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "DeliveryOrderId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "DestinationLocationId",
                table: "Delivery");
        }
    }
}
