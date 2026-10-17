using System.Text.Json;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Unit.Security;

[Trait("Category", "Unit")]
public sealed class MergedEndpointsIsolationTests
{
    private static readonly Guid ForeignOrg = Guid.NewGuid();
    private static ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static RoutePlansController Routes(ApplicationDbContext db) => new(db, null!, null!, null!, null!, null!, null!, null!, null!, null!)
    { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
    private static JsonElement Json(object? value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public async Task Route_detail_list_geometry_and_incident_ignore_foreign_related_rows()
    {
        await using var db = Db();
        var vehicle = new Vehicle { OrganizationId = ForeignOrg, Code = "FOREIGN-VEHICLE" };
        var point = new ServicePoint { OrganizationId = ForeignOrg, Code = "FOREIGN-POINT" };
        var plan = new RoutePlan { Code = "LOCAL", VehicleId = vehicle.Id, Revision = 1, Status = "Published" };
        var stop = new RouteStop { OrganizationId = ForeignOrg, RoutePlanId = plan.Id, ServicePointId = point.Id, SequenceNumber = 1 };
        var incident = new Incident { Kind = "VEHICLE_BREAKDOWN", RoutePlanId = plan.Id };
        db.AddRange(vehicle, point, plan, stop, incident);
        await db.SaveChangesAsync();
        var controller = Routes(db);
        var detail = Json(Assert.IsType<OkObjectResult>(await controller.GetById(plan.Id)).Value);
        Assert.Equal("", detail.GetProperty("vehicleCode").GetString());
        Assert.Equal(0, detail.GetProperty("stops").GetArrayLength());
        var list = Json(Assert.IsType<OkObjectResult>(await controller.ListRoutePlans()).Value);
        Assert.Equal(0, list.GetProperty("items")[0].GetProperty("stopsCount").GetInt32());
        var geometry = new RouteGeometryService(db, new HttpClient(), Options.Create(new LogisticsOptions()),
            new RouteGeometryCache(), NullLogger<RouteGeometryService>.Instance);
        var path = await geometry.GetAsync(plan.Id);
        Assert.Equal("", path!.VehicleCode);
        Assert.Empty(path.Stops);
        Assert.Empty((await new IncidentEvaluator(db).EvaluateAsync(incident.Id)).AffectedRoutes);
    }

    [Fact]
    public async Task Incident_does_not_include_foreign_stops_for_local_vehicle()
    {
        await using var db = Db();
        var vehicle = new Vehicle { Code = "LOCAL" };
        var plan = new RoutePlan { Code = "LOCAL", VehicleId = vehicle.Id, Status = "Published" };
        var incident = new Incident { Kind = "VEHICLE_BREAKDOWN", RoutePlanId = plan.Id };
        db.AddRange(vehicle, plan, incident, new RouteStop { OrganizationId = ForeignOrg, RoutePlanId = plan.Id, SequenceNumber = 1 });
        await db.SaveChangesAsync();
        var impact = await new IncidentEvaluator(db).EvaluateAsync(incident.Id);
        Assert.Empty(Assert.Single(impact.AffectedRoutes).AffectedStops);
    }

    [Fact]
    public async Task Receipt_exceptions_do_not_expose_foreign_lines_actors_or_evidence()
    {
        await using var db = Db();
        var actor = new Account { OrganizationId = ForeignOrg, DisplayName = "FOREIGN-ACTOR" };
        var delivery = new Delivery { Code = "LOCAL-DELIVERY" };
        var receipt = new Receipt { DeliveryId = delivery.Id, ActorAccountId = actor.Id };
        db.AddRange(actor, delivery, receipt,
            new ReceiptLine { ReceiptId = receipt.Id, QuantityRejected = 1, RejectionReason = "Sobrante" },
            new ReceiptLine { OrganizationId = ForeignOrg, ReceiptId = receipt.Id, QuantityRejected = 999 },
            new Evidence { OrganizationId = ForeignOrg, ReceiptId = receipt.Id, StorageKey = "secret" });
        await db.SaveChangesAsync();
        var controller = new ReceiptsController(db, null!, Options.Create(new LogisticsOptions()));
        var response = await controller.ListExceptions();
        var json = Json(Assert.IsType<OkObjectResult>(response.Result).Value);
        var item = json.GetProperty("Items")[0];
        Assert.Equal(1, item.GetProperty("Lines").GetArrayLength());
        Assert.Equal(0, item.GetProperty("Evidence").GetArrayLength());
        Assert.DoesNotContain("FOREIGN-ACTOR", json.ToString());
        Assert.DoesNotContain("999", json.ToString());
    }

    [Fact]
    public async Task Planned_loads_ignore_foreign_order_lines_and_products()
    {
        await using var db = Db();
        var pointId = Guid.NewGuid();
        var order = new DeliveryOrder { OrderNumber = "LOCAL", DestinationServicePointId = pointId };
        var product = new Product { Sku = "LOCAL" };
        var foreignProduct = new Product { OrganizationId = ForeignOrg, Sku = "FOREIGN" };
        db.AddRange(order, product, foreignProduct,
            new DeliveryOrderLine { OrganizationId = ForeignOrg, DeliveryOrderId = order.Id, ProductId = product.Id, RequestedQuantity = 99 },
            new DeliveryOrderLine { DeliveryOrderId = order.Id, ProductId = foreignProduct.Id, RequestedQuantity = 88 });
        await db.SaveChangesAsync();
        var loads = new Dictionary<int, List<StopLoad>>();
        await OrderStopLoads.FillAsync(db, [order.Id], [new RouteStop { ServicePointId = pointId, SequenceNumber = 1 }], loads, default);
        Assert.Empty(loads);
    }

    [Fact]
    public async Task Cancel_cannot_release_a_foreign_balance_referenced_by_local_reservation()
    {
        await using var db = Db();
        var plan = new RoutePlan { Code = "LOCAL", Status = "Proposed" };
        var balance = new StockBalance { OrganizationId = ForeignOrg, QuantityOnHand = 100, QuantityReserved = 50 };
        db.AddRange(plan, balance, new RouteStockReservation { RoutePlanId = plan.Id, StockBalanceId = balance.Id, Quantity = 10 });
        await db.SaveChangesAsync();
        await RoutePlanCancellation.CancelAsync(db, plan.Id);
        Assert.Equal(50, balance.QuantityReserved);
    }

    [Fact]
    public async Task Risks_ignore_foreign_lots_and_pending_delivery_lines()
    {
        await using var db = Db();
        var point = new ServicePoint { Code = "LOCAL", IsActive = true };
        var product = new Product { Sku = "LOCAL", IsActive = true };
        var foreignLot = new Lot { OrganizationId = ForeignOrg, ProductId = product.Id, Code = "FOREIGN" };
        var localLot = new Lot { ProductId = product.Id, Code = "LOCAL" };
        var location = new Location { Kind = LocationKinds.ServicePoint, ServicePointId = point.Id };
        var delivery = new Delivery { ServicePointId = point.Id, Status = DeliveryStates.Draft };
        db.AddRange(point, product, foreignLot, localLot, location, delivery,
            new StockBalance { LocationId = location.Id, LotId = foreignLot.Id, QuantityOnHand = 99 },
            new DeliveryLine { DeliveryId = delivery.Id, LotId = foreignLot.Id, PlannedQuantity = 88 },
            new DeliveryLine { OrganizationId = ForeignOrg, DeliveryId = delivery.Id, LotId = localLot.Id, PlannedQuantity = 77 });
        await db.SaveChangesAsync();
        var options = Options.Create(new LogisticsOptions());
        var calculator = new RiskCalculator(db, new OperationalClock(db, options), options);
        var risk = Assert.Single(await calculator.CalculateAsync());
        Assert.Equal(0, risk.Available);
        Assert.Equal(0, risk.InTransit);
    }

}
