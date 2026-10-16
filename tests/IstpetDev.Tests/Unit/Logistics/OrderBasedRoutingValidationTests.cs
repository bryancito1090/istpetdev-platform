using System.Security.Claims;
using System.Text.Json;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class OrderBasedRoutingValidationTests
{
    private sealed class FakeDistanceProvider : IDistanceProvider
    {
        public Task<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>> MatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default)
        {
            var dict = new Dictionary<(GeoPoint From, GeoPoint To), Leg>();
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                {
                    var p1 = points[i];
                    var p2 = points[j];
                    decimal km = DistanceProvider.HaversineDistanceKm(p1, p2) * 1.2m;
                    decimal min = Math.Round((km / 40m) * 60m, 1);
                    dict[(p1, p2)] = new Leg(p1, p2, km, min, DistanceSource.Road, null);
                }
            }
            return Task.FromResult<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>>(dict);
        }
    }

    private sealed class FakeClock(DateOnly today) : IOperationalClock
    {
        public Task<DateOnly> TodayAsync(CancellationToken ct = default) => Task.FromResult(today);
        public Task<DateTime> WorkdayStartUtcAsync(CancellationToken ct = default) => Task.FromResult(today.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Utc));
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static RoutePlansController CreateController(ApplicationDbContext db, IOperationalClock clock)
    {
        var logOptions = Options.Create(new LogisticsOptions
        {
            WorkdayHours = 10,
            DefaultCostPerKm = 0.45m,
            CostPerHour = 6.00m
        });
        var distanceProvider = new FakeDistanceProvider();
        var evaluator = new RouteEvaluator(distanceProvider, logOptions);
        var sourcing = new SourcingService(db, clock, distanceProvider, logOptions);
        var matcher = new VehicleMatcher(db, distanceProvider, logOptions);

        var controller = new RoutePlansController(
            db,
            planner: null!,
            publisher: null!,
            replanner: null!,
            templates: null!,
            clock: clock,
            sourcingService: sourcing,
            vehicleMatcher: matcher,
            routeEvaluator: evaluator,
            emergencyPlanner: null!);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, Roles.Planning)
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    [Fact]
    public async Task Confirm_WithoutVehicleId_ReturnsConflict409()
    {
        using var db = CreateDb();
        var clock = new FakeClock(new DateOnly(2026, 10, 16));
        var controller = CreateController(db, clock);

        var req = new ConfirmRoutePlanRequest(
            Code: "TEST-PLAN",
            VehicleId: null, // Falta vehicleId
            TotalDistanceKm: 10m,
            TotalDurationMinutes: 30m,
            TotalCost: 15m,
            PlannedStartUtc: DateTime.UtcNow,
            OrderIds: null,
            Stops: new List<RouteStopCandidate>
            {
                new(1, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-1", "Bodega", new GeoPoint(-0.2m, -78.5m), 10m, 0.1m, new TimeOnly(7, 0), new TimeOnly(19, 0), 20),
                new(2, RouteStopKind.Delivery, Guid.NewGuid(), null, "CLI-1", "Cliente", new GeoPoint(-0.21m, -78.51m), -10m, -0.1m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15)
            },
            Polyline: null,
            Coordinates: null
        );

        var result = await controller.ConfirmRoutePlan(req);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Confirm_WithManipulatedLoadExceedingCapacity_ReturnsConflict409()
    {
        // Criterio de cierre: Prueba: un confirm manipulado con carga mayor que la capacidad devuelve 409.
        using var db = CreateDb();
        var clock = new FakeClock(new DateOnly(2026, 10, 16));
        var controller = CreateController(db, clock);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "CAM-LIVIANO",
            MaxWeight = 100m, // Capacidad máxima: 100 kg
            MaxVolume = 1.0m,
            IsActive = true
        };
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        // 1. Confirm manipulado directamente en paradas con carga de 500 kg (> 100 kg)
        var reqManipulated = new ConfirmRoutePlanRequest(
            Code: "PLAN-MANIPULADO",
            VehicleId: vehicle.Id,
            TotalDistanceKm: 15m,
            TotalDurationMinutes: 40m,
            TotalCost: 20m,
            PlannedStartUtc: DateTime.UtcNow,
            OrderIds: null,
            Stops: new List<RouteStopCandidate>
            {
                new(1, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-1", "Bodega", new GeoPoint(-0.2m, -78.5m), 500m, 2.0m, new TimeOnly(7, 0), new TimeOnly(19, 0), 20),
                new(2, RouteStopKind.Delivery, Guid.NewGuid(), null, "CLI-1", "Cliente", new GeoPoint(-0.21m, -78.51m), -500m, -2.0m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15)
            },
            Polyline: null,
            Coordinates: null
        );

        var result = await controller.ConfirmRoutePlan(reqManipulated);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenOrdersExceedVehicleCapacityEvenWithZeroStopDeltas_ReturnsConflict409()
    {
        // Verifica que el servidor recalcula la carga real a partir de los pedidos y no se deja engañar por deltas en 0
        using var db = CreateDb();
        var clock = new FakeClock(new DateOnly(2026, 10, 16));
        var controller = CreateController(db, clock);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "FURGON-PEQUENO",
            MaxWeight = 50m, // Solo aguanta 50 kg
            MaxVolume = 0.5m,
            IsActive = true
        };
        db.Vehicles.Add(vehicle);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "QUIM-PESADO",
            Name = "Químico de alta densidad",
            BaseUnit = "Caneca",
            WeightPerBaseUnit = 25m, // 25 kg cada una
            VolumePerBaseUnit = 0.1m,
            IsActive = true
        };
        db.Products.Add(product);

        var point = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "HOSP-SUR",
            Name = "Hospital del Sur",
            IsActive = true
        };
        db.ServicePoints.Add(point);

        var order = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-EXCESO",
            DestinationServicePointId = point.Id,
            Priority = "Normal",
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(order);

        // Se piden 10 canecas x 25 kg = 250 kg (el camión solo soporta 50 kg)
        var line = new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = order.Id,
            ProductId = product.Id,
            RequestedQuantity = 10m,
            PlannedQuantity = 0m,
            DeliveredQuantity = 0m
        };
        db.DeliveryOrderLines.Add(line);
        await db.SaveChangesAsync();

        // El cliente envía stops con deltaWeight = 0 para intentar burlar la validación
        var req = new ConfirmRoutePlanRequest(
            Code: "PLAN-DISFRAZADO",
            VehicleId: vehicle.Id,
            TotalDistanceKm: 10m,
            TotalDurationMinutes: 30m,
            TotalCost: 10m,
            PlannedStartUtc: DateTime.UtcNow,
            OrderIds: new[] { order.Id },
            Stops: new List<RouteStopCandidate>
            {
                new(1, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-1", "Bodega", new GeoPoint(-0.2m, -78.5m), 0m, 0m, new TimeOnly(7, 0), new TimeOnly(19, 0), 20),
                new(2, RouteStopKind.Delivery, point.Id, null, point.Code, point.Name, new GeoPoint(-0.21m, -78.51m), 0m, 0m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15)
            },
            Polyline: null,
            Coordinates: null
        );

        var result = await controller.ConfirmRoutePlan(req);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Suggest_PlacesCriticalOrderBeforeNormal_WhenBothAreFeasible_AndUsesPointWindows()
    {
        // Criterio de cierre: Prueba: un pedido Critical queda antes que uno Normal cuando ambos son factibles.
        // Y además usa las ventanas y minutos de servicio reales del punto.
        using var db = CreateDb();
        var clock = new FakeClock(new DateOnly(2026, 10, 16));
        var controller = CreateController(db, clock);

        var wh = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-NORTE",
            Name = "Bodega Norte",
            Kind = LocationKinds.Warehouse,
            Position = new NetTopologySuite.Geometries.Point(-78.5243, -0.2295),
            IsActive = true
        };
        db.Locations.Add(wh);

        var spNormal = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "SP-NORMAL",
            Name = "Centro de Salud Normal",
            Position = new NetTopologySuite.Geometries.Point(-78.5200, -0.2200),
            WindowStartLocal = new TimeOnly(9, 30),
            WindowEndLocal = new TimeOnly(16, 0),
            ServiceMinutes = 25,
            IsActive = true
        };
        db.ServicePoints.Add(spNormal);

        var spCritical = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "SP-CRITICAL",
            Name = "Hospital Neonatal Crítico",
            Position = new NetTopologySuite.Geometries.Point(-78.5100, -0.2100),
            WindowStartLocal = new TimeOnly(7, 30),
            WindowEndLocal = new TimeOnly(12, 0),
            ServiceMinutes = 35,
            IsActive = true
        };
        db.ServicePoints.Add(spCritical);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "MED-GLUCOSA",
            Name = "Solución Glucosa 5%",
            BaseUnit = "Frasco",
            WeightPerBaseUnit = 0.5m,
            VolumePerBaseUnit = 0.001m,
            IsActive = true
        };
        db.Products.Add(product);

        var lot = new Lot
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOTE-001",
            ExpiresOn = new DateOnly(2027, 1, 1),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        var balance = new StockBalance
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            LocationId = wh.Id,
            LotId = lot.Id,
            QuantityOnHand = 1000m,
            QuantityReserved = 0m
        };
        db.StockBalances.Add(balance);

        // Pedido 1: Normal
        var orderNormal = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-NORMAL",
            DestinationServicePointId = spNormal.Id,
            Priority = "Normal",
            RequiredDateUtc = new DateTime(2026, 10, 16, 14, 0, 0, DateTimeKind.Utc),
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(orderNormal);
        db.DeliveryOrderLines.Add(new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = orderNormal.Id,
            ProductId = product.Id,
            RequestedQuantity = 20m
        });

        // Pedido 2: Critical (agregado DESPUÉS en la BD para probar que no es por orden de inserción)
        var orderCritical = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-CRITICAL",
            DestinationServicePointId = spCritical.Id,
            Priority = "Critical",
            RequiredDateUtc = new DateTime(2026, 10, 16, 11, 0, 0, DateTimeKind.Utc),
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(orderCritical);
        db.DeliveryOrderLines.Add(new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = orderCritical.Id,
            ProductId = product.Id,
            RequestedQuantity = 15m
        });

        await db.SaveChangesAsync();

        // Llamar a /suggest con ambos pedidos (pasando el normal primero para retar el ordenamiento)
        var suggestReq = new SuggestRoutesRequest([orderNormal.Id, orderCritical.Id], new DateOnly(2026, 10, 16));
        var actionResult = await controller.SuggestRoutes(suggestReq);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var json = JsonSerializer.Serialize(okResult.Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var doc = JsonDocument.Parse(json);

        var optionsArray = doc.RootElement.GetProperty("options");
        Assert.True(optionsArray.GetArrayLength() > 0);

        var firstOpt = optionsArray[0];
        var stops = firstOpt.GetProperty("stops").EnumerateArray().ToList();

        // Parada 1: Pickup en bodega
        Assert.Equal("Pickup", stops[0].GetProperty("kind").GetString());

        // Paradas de entrega: Delivery
        var deliveryStops = stops.Where(s => s.GetProperty("kind").GetString() == "Delivery").ToList();
        Assert.Equal(2, deliveryStops.Count);

        // La PRIMERA entrega debe ser la Crítica (spCritical)
        Assert.Equal(spCritical.Id, deliveryStops[0].GetProperty("servicePointId").GetGuid());
        Assert.Equal("SP-CRITICAL", deliveryStops[0].GetProperty("code").GetString());

        // Debe usar las ventanas reales del punto crítico (07:30 - 12:00, 35 min)
        Assert.Equal("07:30:00", deliveryStops[0].GetProperty("windowStart").GetString());
        Assert.Equal("12:00:00", deliveryStops[0].GetProperty("windowEnd").GetString());
        Assert.Equal(35, deliveryStops[0].GetProperty("serviceMinutes").GetInt32());

        // La SEGUNDA entrega debe ser la Normal (spNormal)
        Assert.Equal(spNormal.Id, deliveryStops[1].GetProperty("servicePointId").GetGuid());
        Assert.Equal("SP-NORMAL", deliveryStops[1].GetProperty("code").GetString());
        Assert.Equal("09:30:00", deliveryStops[1].GetProperty("windowStart").GetString());
        Assert.Equal("16:00:00", deliveryStops[1].GetProperty("windowEnd").GetString());
        Assert.Equal(25, deliveryStops[1].GetProperty("serviceMinutes").GetInt32());
    }

    [Fact]
    public async Task Confirm_PartialOrder_RetainsRemnantAsPending_AndSetsLineQuantities()
    {
        // Criterio de cierre: Prueba: un pedido parcial conserva su remanente como pendiente.
        // Y refleja por línea la cantidad planificada y la pendiente.
        using var db = CreateDb();
        var clock = new FakeClock(new DateOnly(2026, 10, 16));
        var controller = CreateController(db, clock);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "CAM-CAPAZ",
            MaxWeight = 2000m,
            MaxVolume = 10m,
            IsActive = true
        };
        db.Vehicles.Add(vehicle);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "MED-SUERO-1000",
            Name = "Suero Fisiológico 1000ml",
            BaseUnit = "Funda",
            WeightPerBaseUnit = 1.0m,
            VolumePerBaseUnit = 0.002m,
            IsActive = true
        };
        db.Products.Add(product);

        var spPartial = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "HOSP-PARTIAL",
            Name = "Hospital con Entrega Parcial",
            IsActive = true
        };
        db.ServicePoints.Add(spPartial);

        var spFull = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "HOSP-FULL",
            Name = "Hospital con Entrega Completa",
            IsActive = true
        };
        db.ServicePoints.Add(spFull);

        // Pedido 1: Solicitadas 100 unidades
        var orderPartial = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-PARTIAL-1",
            DestinationServicePointId = spPartial.Id,
            Priority = "Normal",
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(orderPartial);

        var linePartial = new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = orderPartial.Id,
            ProductId = product.Id,
            RequestedQuantity = 100m,
            PlannedQuantity = 0m,
            DeliveredQuantity = 0m
        };
        db.DeliveryOrderLines.Add(linePartial);

        // Pedido 2: Solicitadas 50 unidades (se planificará completo)
        var orderFull = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-FULL-2",
            DestinationServicePointId = spFull.Id,
            Priority = "Normal",
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(orderFull);

        var lineFull = new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = orderFull.Id,
            ProductId = product.Id,
            RequestedQuantity = 50m,
            PlannedQuantity = 0m,
            DeliveredQuantity = 0m
        };
        db.DeliveryOrderLines.Add(lineFull);

        await db.SaveChangesAsync();

        // Confirmar ruta donde para orderPartial solo se planifican 60 de las 100 unidades solicitadas,
        // y para orderFull se planifican las 50 unidades completas.
        var confirmReq = new ConfirmRoutePlanRequest(
            Code: "PLAN-PARTIAL-TEST",
            VehicleId: vehicle.Id,
            TotalDistanceKm: 20m,
            TotalDurationMinutes: 60m,
            TotalCost: 30m,
            PlannedStartUtc: DateTime.UtcNow,
            OrderIds: [orderPartial.Id, orderFull.Id],
            Stops: new List<RouteStopCandidate>
            {
                new(1, RouteStopKind.Pickup, null, Guid.NewGuid(), "BOD-1", "Bodega", new GeoPoint(-0.2m, -78.5m), 110m, 0.22m, new TimeOnly(7, 0), new TimeOnly(19, 0), 20),
                new(2, RouteStopKind.Delivery, spPartial.Id, null, spPartial.Code, spPartial.Name, new GeoPoint(-0.21m, -78.51m), -60m, -0.12m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15),
                new(3, RouteStopKind.Delivery, spFull.Id, null, spFull.Code, spFull.Name, new GeoPoint(-0.22m, -78.52m), -50m, -0.10m, new TimeOnly(8, 0), new TimeOnly(17, 0), 15)
            },
            Polyline: null,
            Coordinates: null,
            Lines: new List<ConfirmLineDto>
            {
                new(orderPartial.Id, product.Id, 60m), // Solo 60 de 100
                new(orderFull.Id, product.Id, 50m)      // 50 de 50
            }
        );

        var result = await controller.ConfirmRoutePlan(confirmReq);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        // Recargar pedidos y líneas de la BD
        var dbOrderPartial = await db.DeliveryOrders.FindAsync(orderPartial.Id);
        var dbLinePartial = await db.DeliveryOrderLines.FindAsync(linePartial.Id);
        var dbOrderFull = await db.DeliveryOrders.FindAsync(orderFull.Id);
        var dbLineFull = await db.DeliveryOrderLines.FindAsync(lineFull.Id);

        // VERIFICACIONES CLAVE:
        // 1. El pedido parcial CONSERVA SU ESTADO PENDING (no se marca Planned)
        Assert.Equal(DeliveryOrderStatus.Pending, dbOrderPartial!.Status);

        // 2. La línea refleja la cantidad planificada (60) y el remanente (40) queda pendiente
        Assert.Equal(60m, dbLinePartial!.PlannedQuantity);
        decimal remnant = dbLinePartial.RequestedQuantity - dbLinePartial.PlannedQuantity - dbLinePartial.DeliveredQuantity;
        Assert.Equal(40m, remnant);

        // 3. El pedido completo SÍ se marca Planned
        Assert.Equal(DeliveryOrderStatus.Planned, dbOrderFull!.Status);
        Assert.Equal(50m, dbLineFull!.PlannedQuantity);
        decimal fullRemnant = dbLineFull.RequestedQuantity - dbLineFull.PlannedQuantity - dbLineFull.DeliveredQuantity;
        Assert.Equal(0m, fullRemnant);

        // 4. OrderStopLoads debe devolver exactamente las 60 unidades planificadas para el punto parcial
        var routePlanId = (Guid)created.Value!.GetType().GetProperty("routePlanId")!.GetValue(created.Value)!;
        var routeStops = await db.RouteStops.Where(s => s.RoutePlanId == routePlanId).ToListAsync();
        var loadsBySeq = new Dictionary<int, List<StopLoad>>();
        await OrderStopLoads.FillAsync(db, [orderPartial.Id, orderFull.Id], routeStops, loadsBySeq, CancellationToken.None);

        var partialStop = routeStops.First(s => s.ServicePointId == spPartial.Id);
        Assert.True(loadsBySeq.TryGetValue(partialStop.SequenceNumber, out var partialLoads));
        Assert.Equal(60m, partialLoads.First(l => l.ProductId == product.Id).Quantity);
    }
}
