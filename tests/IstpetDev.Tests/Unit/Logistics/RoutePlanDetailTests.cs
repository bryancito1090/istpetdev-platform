using System.Text.Json;
using IstpetDev.Api.Controllers;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class RoutePlanDetailTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static RoutePlansController CreateController(ApplicationDbContext db)
    {
        var controller = new RoutePlansController(
            db,
            planner: null!,
            publisher: null!,
            replanner: null!,
            templates: null!,
            clock: null!,
            sourcingService: null!,
            vehicleMatcher: null!,
            routeEvaluator: null!,
            emergencyPlanner: null!);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    [Fact]
    public async Task GetById_Returns_Detailed_Stops_Lines_Products_And_Driver_Info()
    {
        // Issue #164: Detalle de la planificación por ruta, producto y conductor
        using var db = CreateDb();

        var vehicle = new Vehicle
        {
            OrganizationId = DemoOrganization.Id,
            Code = "CAM-01",
            MaxWeight = 3500m,
            DepotLocationId = Guid.NewGuid(),
            IsActive = true
        };
        db.Vehicles.Add(vehicle);

        var driver = new Account
        {
            OrganizationId = DemoOrganization.Id,
            Subject = "sub-driver-1",
            DisplayName = "Carlos Conductor",
            Role = Roles.Conductor,
            IsActive = true
        };
        db.Accounts.Add(driver);

        var depot = new Location
        {
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-CENTRAL",
            Name = "Bodega Central Guayaquil",
            Kind = LocationKinds.Warehouse
        };
        db.Locations.Add(depot);

        var sp1 = new ServicePoint
        {
            OrganizationId = DemoOrganization.Id,
            Code = "HOSP-01",
            Name = "Hospital General Norte",
            ServiceMinutes = 20
        };
        db.ServicePoints.Add(sp1);

        var product = new Product
        {
            OrganizationId = DemoOrganization.Id,
            Sku = "MED-PARACETAMOL-500",
            Name = "Paracetamol 500mg",
            BaseUnit = "Caja",
            IsActive = true
        };
        db.Products.Add(product);

        var lot = new Lot
        {
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOT-2026-X1",
            ExpiresOn = new DateOnly(2027, 12, 31)
        };
        db.Lots.Add(lot);

        var plan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = "PLAN-20261015-CAM-01",
            Revision = 1,
            Status = "Published",
            VehicleId = vehicle.Id,
            PlannedStartUtc = DateTime.UtcNow,
            TotalKm = 25.5m,
            TotalMinutes = 60m
        };
        db.RoutePlans.Add(plan);

        var stop1 = new RouteStop
        {
            OrganizationId = DemoOrganization.Id,
            RoutePlanId = plan.Id,
            ServicePointId = sp1.Id,
            SequenceNumber = 1,
            Status = "Pending",
            PlannedArrivalUtc = DateTime.UtcNow.AddMinutes(30)
        };
        db.RouteStops.Add(stop1);

        var delivery = new Delivery
        {
            OrganizationId = DemoOrganization.Id,
            Code = "DEL-001",
            RouteStopId = stop1.Id,
            OriginLocationId = depot.Id,
            ServicePointId = sp1.Id,
            Status = "Assigned",
            AssignedAccountId = driver.Id
        };
        db.Deliveries.Add(delivery);

        var hu = new HandlingUnit
        {
            OrganizationId = DemoOrganization.Id,
            Code = "HU-BOX-001",
            DeliveryId = delivery.Id,
            LocationId = depot.Id,
            Status = "Sealed"
        };
        db.HandlingUnits.Add(hu);

        var line = new DeliveryLine
        {
            OrganizationId = DemoOrganization.Id,
            DeliveryId = delivery.Id,
            LotId = lot.Id,
            HandlingUnitId = hu.Id,
            PlannedQuantity = 150m
        };
        db.DeliveryLines.Add(line);

        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetById(plan.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("PLAN-20261015-CAM-01", root.GetProperty("code").GetString());
        Assert.Equal("CAM-01", root.GetProperty("vehicleCode").GetString());
        Assert.Equal("Bodega Central Guayaquil", root.GetProperty("originWarehouseName").GetString());
        Assert.Equal("Carlos Conductor", root.GetProperty("driverName").GetString());
        Assert.Equal(driver.Id.ToString(), root.GetProperty("driverAccountId").GetString());

        // Verificar paradas y líneas detalladas
        var stopsElem = root.GetProperty("stops");
        Assert.Equal(1, stopsElem.GetArrayLength());
        var stopElem = stopsElem[0];
        Assert.Equal("HOSP-01", stopElem.GetProperty("servicePointCode").GetString());
        Assert.Equal("Hospital General Norte", stopElem.GetProperty("servicePointName").GetString());
        Assert.Equal("DEL-001", stopElem.GetProperty("deliveryCode").GetString());
        Assert.Equal("Assigned", stopElem.GetProperty("deliveryStatus").GetString());

        var linesElem = stopElem.GetProperty("lines");
        Assert.Equal(1, linesElem.GetArrayLength());
        var lineElem = linesElem[0];
        Assert.Equal("MED-PARACETAMOL-500", lineElem.GetProperty("sku").GetString());
        Assert.Equal("Paracetamol 500mg", lineElem.GetProperty("productName").GetString());
        Assert.Equal("LOT-2026-X1", lineElem.GetProperty("lotCode").GetString());
        Assert.Equal(150m, lineElem.GetProperty("plannedQuantity").GetDecimal());
        Assert.Equal("HU-BOX-001", lineElem.GetProperty("handlingUnitCode").GetString());
        Assert.Equal("Sealed", lineElem.GetProperty("handlingUnitStatus").GetString());

        // Verificar vista agregada por producto
        var productsSummary = root.GetProperty("productsSummary");
        Assert.Equal(1, productsSummary.GetArrayLength());
        var prodSum = productsSummary[0];
        Assert.Equal("MED-PARACETAMOL-500", prodSum.GetProperty("sku").GetString());
        Assert.Equal(150m, prodSum.GetProperty("totalPlannedQuantity").GetDecimal());
        Assert.Equal(1, prodSum.GetProperty("stopsCount").GetInt32());

        // Verificar rutas asignadas al conductor
        var driverRoutes = root.GetProperty("driverRoutes");
        Assert.Equal(1, driverRoutes.GetArrayLength());
        var dRoute = driverRoutes[0];
        Assert.Equal("PLAN-20261015-CAM-01", dRoute.GetProperty("code").GetString());
        Assert.True(dRoute.GetProperty("isCurrent").GetBoolean());
    }

    [Fact]
    public async Task ListRoutePlans_Includes_DriverName_And_DriverAccountId()
    {
        // Issue #164: Conductor visible en la lista de rutas
        using var db = CreateDb();

        var vehicle = new Vehicle
        {
            OrganizationId = DemoOrganization.Id,
            Code = "CAM-02",
            MaxWeight = 3000m,
            DepotLocationId = Guid.NewGuid(),
            IsActive = true
        };
        db.Vehicles.Add(vehicle);

        var driver = new Account
        {
            OrganizationId = DemoOrganization.Id,
            Subject = "sub-driver-2",
            DisplayName = "María Conductora",
            Role = Roles.Conductor,
            IsActive = true
        };
        db.Accounts.Add(driver);

        var plan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = "PLAN-20261015-CAM-02",
            Revision = 1,
            Status = "Published",
            VehicleId = vehicle.Id,
            PlannedStartUtc = DateTime.UtcNow,
            TotalKm = 40m,
            TotalMinutes = 80m
        };
        db.RoutePlans.Add(plan);

        var stop = new RouteStop
        {
            OrganizationId = DemoOrganization.Id,
            RoutePlanId = plan.Id,
            ServicePointId = Guid.NewGuid(),
            SequenceNumber = 1,
            Status = "Pending"
        };
        db.RouteStops.Add(stop);

        var delivery = new Delivery
        {
            OrganizationId = DemoOrganization.Id,
            Code = "DEL-002",
            RouteStopId = stop.Id,
            OriginLocationId = Guid.NewGuid(),
            ServicePointId = stop.ServicePointId,
            Status = "Assigned",
            AssignedAccountId = driver.Id
        };
        db.Deliveries.Add(delivery);

        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.ListRoutePlans();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");

        Assert.Equal(1, items.GetArrayLength());
        var item = items[0];
        Assert.Equal("PLAN-20261015-CAM-02", item.GetProperty("code").GetString());
        Assert.Equal("María Conductora", item.GetProperty("driverName").GetString());
        Assert.Equal(driver.Id.ToString(), item.GetProperty("driverAccountId").GetString());
    }
}
