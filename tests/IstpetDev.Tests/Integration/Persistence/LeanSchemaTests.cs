using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Location = IstpetDev.Domain.Entities.Location;

namespace IstpetDev.Tests.Integration.Persistence;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class LeanSchemaTests(PostgisContainer postgres)
{
    private static readonly string[] CoreTables =
    [
        "Organization", "Account", "ServicePoint", "Location", "Vehicle", "Product", "Lot", "HandlingUnit", "HandlingUnitLine",
        "InventoryMovement", "StockBalance", "Consumption", "RoutePlan", "RouteStop", "Delivery", "DeliveryLine", "Receipt",
        "ReceiptLine", "Evidence", "CustodyEvent", "RiskSnapshot", "Incident", "IdempotencyRecord", "ImportBatch",
        "AgentDecision", "AgentRun", "SimulationRun", "SimulationMetric", "RoadMatrixEdge", "RouteStockReservation", "QrAccessToken",
    ];

    [Fact]
    public async Task Migration_creates_exactly_the_core_tables_and_the_demo_organization()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var tables = await db.Database.SqlQueryRaw<string>(
            "SELECT table_name AS \"Value\" FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' " +
            "AND table_name NOT IN ('__EFMigrationsHistory', 'spatial_ref_sys')").ToListAsync();

        Assert.Equal(CoreTables.Order(), tables.Order());
        Assert.True(await db.Organizations.AnyAsync(o => o.Id == DemoOrganization.Id));
    }

    [Fact]
    public async Task Service_point_position_round_trips_and_organization_defaults_to_demo()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        db.ServicePoints.Add(new ServicePoint { Code = "P-1", Name = "Punto", Criticality = "normal", Position = new Point(-78.5, -0.2) { SRID = 4326 }, IsActive = true });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var point = await db.ServicePoints.SingleAsync();
        Assert.Equal(DemoOrganization.Id, point.OrganizationId);
        Assert.Equal(-0.2, point.Position!.Y, 6);
        Assert.Equal(-78.5, point.Position.X, 6);
        Assert.Equal(1, point.Version);
    }

    [Fact]
    public async Task Stock_balance_cannot_be_negative()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var product = new Product { Sku = "S", Name = "S", BaseUnit = "L", IsActive = true };
        var lot = new Lot { ProductId = product.Id, Code = "L1" };
        var location = new Location { Code = "B", Name = "B", Kind = LocationKinds.Warehouse, IsActive = true };
        db.AddRange(product, lot, location, new StockBalance { LocationId = location.Id, LotId = lot.Id, QuantityOnHand = -1 });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
