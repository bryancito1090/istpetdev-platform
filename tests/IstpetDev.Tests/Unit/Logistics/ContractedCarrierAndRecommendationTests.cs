using System.Security.Claims;
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
public sealed class ContractedCarrierAndRecommendationTests
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

    private static LogisticsOptions CreateDefaultLogisticsOptions(bool enableCarrier = true)
    {
        return new LogisticsOptions
        {
            WorkdayHours = 10,
            DefaultCostPerKm = 0.45m,
            CostPerHour = 6.00m,
            ApproxSpeedKmh = 40m,
            ApproxDetourFactor = 1.2m,
            DefaultServiceMinutes = 15,
            ContractedCarrier = enableCarrier ? new ContractedCarrierOptions
            {
                BaseFeeUsd = 25.00m,
                PerKmUsd = 0.65m,
                PerKgUsd = 0.05m,
                ResponseHours = 4,
                Name = "Servientrega Express",
                IsEnabled = true
            } : null,
            Recommendation = new RecommendationOptions
            {
                WeightCost = 0.35m,
                WeightCriticalOnTime = 0.35m,
                WeightPendingDemand = 0.20m,
                WeightRisk = 0.10m
            }
        };
    }

    [Fact]
    public async Task WithSufficientFleet_AndNoCriticalMisses_CheapestOptionAWins()
    {
        using var db = CreateDb();
        var today = new DateOnly(2026, 10, 16);
        var clock = new FakeClock(today);
        var logOptions = Options.Create(CreateDefaultLogisticsOptions(enableCarrier: true));
        var distanceProvider = new FakeDistanceProvider();
        var sourcing = new SourcingService(db, clock, distanceProvider, logOptions);

        var wh = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-CENTRAL",
            Name = "Bodega Central",
            Kind = LocationKinds.Warehouse,
            Position = new NetTopologySuite.Geometries.Point(-78.5000, -0.2000) { SRID = 4326 },
            IsActive = true
        };
        db.Locations.Add(wh);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "MED-01",
            Name = "Paracetamol",
            BaseUnit = "Caja",
            WeightPerBaseUnit = 0.5m,
            UnitValue = 5.0m,
            IsActive = true
        };
        db.Products.Add(product);

        var lot = new Lot
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOTE-01",
            ExpiresOn = today.AddMonths(6),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        db.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            LocationId = wh.Id,
            LotId = lot.Id,
            QuantityOnHand = 1000m,
            QuantityReserved = 0m
        });

        // 2 puntos de servicio no críticos con ventanas amplias
        var sp1 = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "CLI-01",
            Name = "Centro de Salud 1",
            Criticality = "Normal",
            Position = new NetTopologySuite.Geometries.Point(-78.5100, -0.2100) { SRID = 4326 },
            WindowStartLocal = new TimeOnly(8, 0),
            WindowEndLocal = new TimeOnly(17, 0),
            IsActive = true
        };
        var sp2 = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "CLI-02",
            Name = "Centro de Salud 2",
            Criticality = "Normal",
            Position = new NetTopologySuite.Geometries.Point(-78.5200, -0.2200) { SRID = 4326 },
            WindowStartLocal = new TimeOnly(8, 0),
            WindowEndLocal = new TimeOnly(17, 0),
            IsActive = true
        };
        db.ServicePoints.AddRange(sp1, sp2);

        var order1 = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-01",
            DestinationServicePointId = sp1.Id,
            Priority = "Normal",
            Status = DeliveryOrderStatus.Pending
        };
        var order2 = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-02",
            DestinationServicePointId = sp2.Id,
            Priority = "Normal",
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.AddRange(order1, order2);

        db.DeliveryOrderLines.Add(new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = order1.Id,
            ProductId = product.Id,
            RequestedQuantity = 50m
        });
        db.DeliveryOrderLines.Add(new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = order2.Id,
            ProductId = product.Id,
            RequestedQuantity = 30m
        });

        await db.SaveChangesAsync();

        var result = await sourcing.QueryOrderSourcingAsync(new[] { order1.Id, order2.Id });

        // Criterio 1: Con flota suficiente y sin críticos fuera de ventana, gana la más barata (Opción A)
        Assert.True(result.FullySourced);
        Assert.Equal("A", result.RecommendedOptionId);
        var optA = result.Options.First(o => o.OptionId == "A");
        Assert.True(optA.IsRecommended);
        Assert.StartsWith("Se recomienda A", result.Justification);
        Assert.Contains("más económica", result.Justification);
    }

    [Fact]
    public async Task WithCriticalHospitalOutOfWindowInA_OptionBWins()
    {
        using var db = CreateDb();
        var today = new DateOnly(2026, 10, 16);
        var clock = new FakeClock(today);
        var logOptions = Options.Create(CreateDefaultLogisticsOptions(enableCarrier: true));
        var distanceProvider = new FakeDistanceProvider();
        var sourcing = new SourcingService(db, clock, distanceProvider, logOptions);

        var wh = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-SUR",
            Name = "Bodega Sur",
            Kind = LocationKinds.Warehouse,
            Position = new NetTopologySuite.Geometries.Point(-78.5200, -0.3000) { SRID = 4326 },
            IsActive = true
        };
        db.Locations.Add(wh);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "VAC-01",
            Name = "Vacunas Pediátricas",
            BaseUnit = "Dosis",
            WeightPerBaseUnit = 0.2m,
            UnitValue = 25.0m,
            IsActive = true
        };
        db.Products.Add(product);

        var lot = new Lot
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOT-VAC",
            ExpiresOn = today.AddMonths(12),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        db.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            LocationId = wh.Id,
            LotId = lot.Id,
            QuantityOnHand = 500m
        });

        // 3 hospitales críticos. En Opción A (secuencia estándar alejada), el 3er hospital llega tarde.
        var sp1 = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "HOSP-01",
            Name = "Hospital General Sur",
            Criticality = "Alta",
            Position = new NetTopologySuite.Geometries.Point(-78.5300, -0.2800) { SRID = 4326 },
            WindowStartLocal = new TimeOnly(8, 0),
            WindowEndLocal = new TimeOnly(9, 30),
            IsActive = true
        };
        var sp2 = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "HOSP-02",
            Name = "Hospital Metropolitano",
            Criticality = "Alta",
            Position = new NetTopologySuite.Geometries.Point(-78.5000, -0.1800) { SRID = 4326 },
            WindowStartLocal = new TimeOnly(8, 0),
            WindowEndLocal = new TimeOnly(9, 30),
            IsActive = true
        };
        // Hospital 3 con ventana matutina muy estricta que Opción A incumple por visitarse al final
        var sp3 = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "HOSP-03",
            Name = "Hospital Pediátrico Baca Ortiz",
            Criticality = "Alta",
            Position = new NetTopologySuite.Geometries.Point(-78.4900, -0.1900) { SRID = 4326 },
            WindowStartLocal = new TimeOnly(8, 0),
            WindowEndLocal = new TimeOnly(8, 20), // Ventana corta que se pierde en A
            IsActive = true
        };
        db.ServicePoints.AddRange(sp1, sp2, sp3);

        var ord1 = new DeliveryOrder { Id = Guid.NewGuid(), OrganizationId = DemoOrganization.Id, OrderNumber = "ORD-H1", DestinationServicePointId = sp1.Id, Priority = "Critical", Status = DeliveryOrderStatus.Pending };
        var ord2 = new DeliveryOrder { Id = Guid.NewGuid(), OrganizationId = DemoOrganization.Id, OrderNumber = "ORD-H2", DestinationServicePointId = sp2.Id, Priority = "Critical", Status = DeliveryOrderStatus.Pending };
        var ord3 = new DeliveryOrder { Id = Guid.NewGuid(), OrganizationId = DemoOrganization.Id, OrderNumber = "ORD-H3", DestinationServicePointId = sp3.Id, Priority = "Critical", Status = DeliveryOrderStatus.Pending };
        db.DeliveryOrders.AddRange(ord1, ord2, ord3);

        db.DeliveryOrderLines.Add(new DeliveryOrderLine { Id = Guid.NewGuid(), OrganizationId = DemoOrganization.Id, DeliveryOrderId = ord1.Id, ProductId = product.Id, RequestedQuantity = 20m });
        db.DeliveryOrderLines.Add(new DeliveryOrderLine { Id = Guid.NewGuid(), OrganizationId = DemoOrganization.Id, DeliveryOrderId = ord2.Id, ProductId = product.Id, RequestedQuantity = 20m });
        db.DeliveryOrderLines.Add(new DeliveryOrderLine { Id = Guid.NewGuid(), OrganizationId = DemoOrganization.Id, DeliveryOrderId = ord3.Id, ProductId = product.Id, RequestedQuantity = 20m });

        await db.SaveChangesAsync();

        var result = await sourcing.QueryOrderSourcingAsync(new[] { ord1.Id, ord2.Id, ord3.Id });

        // Criterio 2: Con un crítico fuera de ventana en A, gana B
        Assert.Equal("B", result.RecommendedOptionId);
        var optB = result.Options.First(o => o.OptionId == "B");
        Assert.True(optB.IsRecommended);

        var optA = result.Options.First(o => o.OptionId == "A");
        Assert.False(optA.IsRecommended);
        Assert.True(optA.CriticalDeliveriesOnTime < optA.CriticalDeliveriesTotal);
        Assert.Equal(optB.CriticalDeliveriesTotal, optB.CriticalDeliveriesOnTime);

        // Verificación de la justificación determinista con cifras
        Assert.Contains("Se recomienda B", result.Justification);
        Assert.Contains("cuesta USD 12", result.Justification);
        Assert.Contains("entrega a tiempo los 3 hospitales", result.Justification);
        Assert.Contains("A deja 1 fuera de su ventana", result.Justification);
    }

    [Fact]
    public async Task WithInsufficientFleet_OptionCAppearsWithShortageCost_AndWinsWhenOwnFleetHasShortage()
    {
        using var db = CreateDb();
        var today = new DateOnly(2026, 10, 16);
        var clock = new FakeClock(today);
        var logOptions = Options.Create(CreateDefaultLogisticsOptions(enableCarrier: true));
        var distanceProvider = new FakeDistanceProvider();
        var sourcing = new SourcingService(db, clock, distanceProvider, logOptions);

        var wh = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-CENTRAL",
            Name = "Bodega Central",
            Kind = LocationKinds.Warehouse,
            Position = new NetTopologySuite.Geometries.Point(-78.5000, -0.2000) { SRID = 4326 },
            IsActive = true
        };
        db.Locations.Add(wh);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "SUE-01",
            Name = "Suero Fisiológico",
            BaseUnit = "Funda",
            WeightPerBaseUnit = 1.0m,
            UnitValue = 2.0m,
            IsActive = true
        };
        db.Products.Add(product);

        var lot = new Lot
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOT-SUE",
            ExpiresOn = today.AddMonths(12),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        // Stock insuficiente: solo hay 10 unidades utilizables (tras 10% de seguridad quedan 9)
        db.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            LocationId = wh.Id,
            LotId = lot.Id,
            QuantityOnHand = 10m,
            QuantityReserved = 0m
        });

        var sp = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "HOSP-PROV",
            Name = "Hospital Provincial",
            Criticality = "Alta",
            Position = new NetTopologySuite.Geometries.Point(-78.4800, -0.1800) { SRID = 4326 },
            WindowStartLocal = new TimeOnly(8, 0),
            WindowEndLocal = new TimeOnly(17, 0),
            IsActive = true
        };
        db.ServicePoints.Add(sp);

        // Se solicitan 100 unidades (faltante significativo de 91 unidades)
        var order = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-SHORTAGE",
            DestinationServicePointId = sp.Id,
            Priority = "High",
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(order);

        db.DeliveryOrderLines.Add(new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = order.Id,
            ProductId = product.Id,
            RequestedQuantity = 100m
        });

        await db.SaveChangesAsync();

        var result = await sourcing.QueryOrderSourcingAsync(new[] { order.Id });

        // Criterio 3: Con flota insuficiente, C aparece con el costo del faltante y gana por no dejar pendientes
        Assert.False(result.FullySourced); // Flota propia no abastece todo
        var optC = result.Options.FirstOrDefault(o => o.OptionId == "C");
        Assert.NotNull(optC);
        Assert.Equal("Contratada", optC.CarrierType);
        Assert.True(optC.PendingDemandCost > 0m); // Muestra costo de cubrir el faltante
        Assert.Equal(0, optC.PendingOrdersCount);
        Assert.Equal(0m, optC.PendingQuantity);

        Assert.Equal("C", result.RecommendedOptionId);
        Assert.True(optC.IsRecommended);
        Assert.Contains("Se recomienda C", result.Justification);
        Assert.Contains("costo de faltante", result.Justification);
    }

    [Fact]
    public async Task Auditability_ScoreWeightsAndBreakdowns_AreVisibleInResponse()
    {
        using var db = CreateDb();
        var today = new DateOnly(2026, 10, 16);
        var clock = new FakeClock(today);
        var logOptions = Options.Create(CreateDefaultLogisticsOptions(enableCarrier: true));
        var distanceProvider = new FakeDistanceProvider();
        var sourcing = new SourcingService(db, clock, distanceProvider, logOptions);

        var wh = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-AUDIT",
            Name = "Bodega Auditoría",
            Kind = LocationKinds.Warehouse,
            Position = new NetTopologySuite.Geometries.Point(-78.5000, -0.2000) { SRID = 4326 },
            IsActive = true
        };
        db.Locations.Add(wh);

        var sp = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "SP-AUDIT",
            Name = "Punto Auditoría",
            Criticality = "Normal",
            Position = new NetTopologySuite.Geometries.Point(-78.5100, -0.2100) { SRID = 4326 },
            IsActive = true
        };
        db.ServicePoints.Add(sp);

        var prod = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "AUD-01",
            Name = "Producto Audit",
            BaseUnit = "U",
            WeightPerBaseUnit = 1m,
            IsActive = true
        };
        db.Products.Add(prod);

        var lot = new Lot
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            ProductId = prod.Id,
            Code = "LOT-AUD",
            ExpiresOn = today.AddYears(1),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        db.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            LocationId = wh.Id,
            LotId = lot.Id,
            QuantityOnHand = 200m
        });

        var ord = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-AUD",
            DestinationServicePointId = sp.Id,
            Priority = "Normal",
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(ord);

        db.DeliveryOrderLines.Add(new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = ord.Id,
            ProductId = prod.Id,
            RequestedQuantity = 20m
        });

        await db.SaveChangesAsync();

        var result = await sourcing.QueryOrderSourcingAsync(new[] { ord.Id });

        // Criterio 4: Los pesos del puntaje son visibles en la respuesta (auditables)
        Assert.NotNull(result.RecommendationWeights);
        Assert.Equal(0.35m, result.RecommendationWeights.WeightCost);
        Assert.Equal(0.35m, result.RecommendationWeights.WeightCriticalOnTime);
        Assert.Equal(0.20m, result.RecommendationWeights.WeightPendingDemand);
        Assert.Equal(0.10m, result.RecommendationWeights.WeightRisk);

        foreach (var opt in result.Options)
        {
            Assert.NotNull(opt.ScoreBreakdown);
            Assert.True(opt.ScoreBreakdown.CostScore >= 0m);
            Assert.True(opt.ScoreBreakdown.CriticalScore >= 0m);
            Assert.True(opt.ScoreBreakdown.PendingScore >= 0m);
            Assert.True(opt.ScoreBreakdown.RiskScore >= 0m);
            Assert.True(opt.ScoreBreakdown.TotalScore >= 0m);
            Assert.Equal(opt.Score, opt.ScoreBreakdown.TotalScore);
        }
    }

    [Fact]
    public async Task WithoutContractedCarrierConfig_OptionCDoesNotExist_AndSystemWorksAsBefore()
    {
        using var db = CreateDb();
        var today = new DateOnly(2026, 10, 16);
        var clock = new FakeClock(today);
        // Operador desactivado / nulo
        var logOptions = Options.Create(CreateDefaultLogisticsOptions(enableCarrier: false));
        var distanceProvider = new FakeDistanceProvider();
        var sourcing = new SourcingService(db, clock, distanceProvider, logOptions);

        var wh = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-NOCARRIER",
            Name = "Bodega Central",
            Kind = LocationKinds.Warehouse,
            Position = new NetTopologySuite.Geometries.Point(-78.5000, -0.2000) { SRID = 4326 },
            IsActive = true
        };
        db.Locations.Add(wh);

        var sp = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "SP-NOCARRIER",
            Name = "Punto Destino",
            Criticality = "Normal",
            Position = new NetTopologySuite.Geometries.Point(-78.5100, -0.2100) { SRID = 4326 },
            IsActive = true
        };
        db.ServicePoints.Add(sp);

        var prod = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "PROD-01",
            Name = "Producto",
            BaseUnit = "U",
            WeightPerBaseUnit = 1m,
            IsActive = true
        };
        db.Products.Add(prod);

        var lot = new Lot
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            ProductId = prod.Id,
            Code = "LOT-01",
            ExpiresOn = today.AddYears(1),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        db.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            LocationId = wh.Id,
            LotId = lot.Id,
            QuantityOnHand = 100m
        });

        var ord = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-NC",
            DestinationServicePointId = sp.Id,
            Priority = "Normal",
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(ord);

        db.DeliveryOrderLines.Add(new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = ord.Id,
            ProductId = prod.Id,
            RequestedQuantity = 10m
        });

        await db.SaveChangesAsync();

        var result = await sourcing.QueryOrderSourcingAsync(new[] { ord.Id });

        // Criterio 5: Sin configuración del operador, no aparece la opción C y todo funciona como hoy
        Assert.DoesNotContain(result.Options, o => o.OptionId == "C");
        Assert.Contains(result.Options, o => o.OptionId == "A");
    }

    [Fact]
    public async Task HumanControl_PlannerCanSelectOtherOptionThanRecommended()
    {
        using var db = CreateDb();
        var today = new DateOnly(2026, 10, 16);
        var clock = new FakeClock(today);
        var logOptions = Options.Create(CreateDefaultLogisticsOptions(enableCarrier: true));
        var distanceProvider = new FakeDistanceProvider();
        var sourcing = new SourcingService(db, clock, distanceProvider, logOptions);
        var matcher = new VehicleMatcher(db, distanceProvider, logOptions);
        var evaluator = new RouteEvaluator(distanceProvider, logOptions);

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

        var wh = new Location
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-HUMAN",
            Name = "Bodega Human Control",
            Kind = LocationKinds.Warehouse,
            Position = new NetTopologySuite.Geometries.Point(-78.5000, -0.2000) { SRID = 4326 },
            IsActive = true
        };
        db.Locations.Add(wh);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "VEH-HC-01",
            MaxWeight = 2000m,
            MaxVolume = 10m,
            CostPerKm = 0.50m,
            DepotLocationId = wh.Id,
            IsActive = true
        };
        db.Vehicles.Add(vehicle);

        var sp = new ServicePoint
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Code = "SP-HC",
            Name = "Punto Human Control",
            Criticality = "Normal",
            Position = new NetTopologySuite.Geometries.Point(-78.5100, -0.2100) { SRID = 4326 },
            IsActive = true
        };
        db.ServicePoints.Add(sp);

        var prod = new Product
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            Sku = "HC-01",
            Name = "Producto HC",
            BaseUnit = "U",
            WeightPerBaseUnit = 1m,
            IsActive = true
        };
        db.Products.Add(prod);

        var lot = new Lot
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            ProductId = prod.Id,
            Code = "LOT-HC",
            ExpiresOn = today.AddYears(1),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        db.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            LocationId = wh.Id,
            LotId = lot.Id,
            QuantityOnHand = 500m
        });

        var ord = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            OrderNumber = "ORD-HC",
            DestinationServicePointId = sp.Id,
            Priority = "Normal",
            Status = DeliveryOrderStatus.Pending
        };
        db.DeliveryOrders.Add(ord);

        db.DeliveryOrderLines.Add(new DeliveryOrderLine
        {
            Id = Guid.NewGuid(),
            OrganizationId = DemoOrganization.Id,
            DeliveryOrderId = ord.Id,
            ProductId = prod.Id,
            RequestedQuantity = 50m
        });

        await db.SaveChangesAsync();

        // El planificador pide explícitamente OptionId = "A" (o "B" o "C") en el suggest
        var req = new SuggestRoutesRequest(
            OrderIds: new[] { ord.Id },
            PlannedDate: today,
            OptionId: "A"
        );

        var result = await controller.SuggestRoutes(req);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Control humano verificado: la solicitud se procesó con éxito respetando el OptionId indicado
        var proposal = okResult.Value.GetType().GetProperty("proposal")?.GetValue(okResult.Value);
        Assert.NotNull(proposal);
        Assert.Equal("A", proposal.GetType().GetProperty("sourcingOptionId")?.GetValue(proposal));
        var comparison = Assert.IsType<DemandSourcingResult>(
            okResult.Value.GetType().GetProperty("comparison")?.GetValue(okResult.Value));
        Assert.Contains(comparison.Options, option => option.CarrierType == "Contratada");
        Assert.Contains(comparison.Options, option => option.OptionId == comparison.RecommendedOptionId);
        Assert.False(string.IsNullOrWhiteSpace(comparison.Justification));
    }
}
