using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

using DomainLocation = IstpetDev.Domain.Entities.Location;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RoutePlanningTests(PostgisContainer postgres)
{
    private static readonly GeometryFactory Gf = new(new PrecisionModel(), 4326);

    private static IOptions<LogisticsOptions> DefaultOptions() =>
        Options.Create(new LogisticsOptions
        {
            DemandWindowDays = 14,
            DefaultLeadTimeDays = 2m,
            SafetyMarginDays = 3m,
            ConsolidationExtraDays = 3m,
            TargetCoverageDays = 10m,
            SafetyStockUnits = 0m,
            StaleStockDays = 7,
            WarehouseSafetyStockPct = 10m,
            DefaultCostPerKm = 0.50m,
            CostPerHour = 10.00m,
            WorkdayHours = 8,
            MaxDetourMinutes = 30,
            DefaultServiceMinutes = 15,
            DefaultWindowStart = "08:00",
            DefaultWindowEnd = "17:00",
            Clock = new ClockOptions
            {
                Mode = ClockMode.Fixed,
                FixedDate = new DateOnly(2026, 10, 12)
            }
        });

    [Fact]
    public async Task Route_planner_produces_identical_deterministic_json_for_same_db_state_and_clock()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());

        // 1. Configurar datos: Bodega, 2 Puntos, 1 Producto, Stock, Consumos, Vehículos
        var warehouseLoc = new DomainLocation
        {
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-CENTRAL",
            Name = "Bodega Central",
            Kind = LocationKinds.Warehouse,
            Position = Gf.CreatePoint(new Coordinate(-78.5243, -0.2295)),
            IsActive = true
        };
        db.Locations.Add(warehouseLoc);

        var product = new Product
        {
            OrganizationId = DemoOrganization.Id,
            Sku = "MED-01",
            Name = "Amoxicilina 500mg",
            BaseUnit = "caja",
            WeightPerBaseUnit = 0.5m,
            VolumePerBaseUnit = 0.002m,
            IsActive = true
        };
        db.Products.Add(product);

        var sp1 = new ServicePoint
        {
            OrganizationId = DemoOrganization.Id,
            Code = "HOS-01",
            Name = "Hospital Norte",
            Category = "Hospital",
            Criticality = "alta",
            Position = Gf.CreatePoint(new Coordinate(-78.5000, -0.2000)),
            WindowStartLocal = new TimeOnly(8, 0),
            WindowEndLocal = new TimeOnly(17, 0),
            ServiceMinutes = 20,
            IsActive = true
        };
        var sp2 = new ServicePoint
        {
            OrganizationId = DemoOrganization.Id,
            Code = "HOS-02",
            Name = "Hospital Sur",
            Category = "Hospital",
            Criticality = "media",
            Position = Gf.CreatePoint(new Coordinate(-78.5100, -0.2100)),
            WindowStartLocal = new TimeOnly(8, 0),
            WindowEndLocal = new TimeOnly(17, 0),
            ServiceMinutes = 20,
            IsActive = true
        };
        db.ServicePoints.AddRange(sp1, sp2);

        var v1 = new Vehicle
        {
            OrganizationId = DemoOrganization.Id,
            Code = "VEH-01",
            DepotLocationId = warehouseLoc.Id,
            MaxWeight = 2000m,
            MaxVolume = 10m,
            CostPerKm = 0.5m,
            IsActive = true
        };
        var v2 = new Vehicle
        {
            OrganizationId = DemoOrganization.Id,
            Code = "VEH-02",
            DepotLocationId = warehouseLoc.Id,
            MaxWeight = 1500m,
            MaxVolume = 8m,
            CostPerKm = 0.5m,
            IsActive = true
        };
        db.Vehicles.AddRange(v1, v2);

        // Lotes y Stock
        var lot = new Lot
        {
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOTE-001",
            ExpiresOn = new DateOnly(2027, 1, 1),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        var balance = new StockBalance
        {
            OrganizationId = DemoOrganization.Id,
            LocationId = warehouseLoc.Id,
            LotId = lot.Id,
            QuantityOnHand = 1000m,
            QuantityReserved = 0m
        };
        db.StockBalances.Add(balance);

        // Ubicaciones de los puntos de servicio
        var sp1Loc = new DomainLocation
        {
            OrganizationId = DemoOrganization.Id,
            Code = sp1.Code,
            Name = sp1.Name,
            Kind = LocationKinds.ServicePoint,
            ServicePointId = sp1.Id,
            IsActive = true
        };
        var sp2Loc = new DomainLocation
        {
            OrganizationId = DemoOrganization.Id,
            Code = sp2.Code,
            Name = sp2.Name,
            Kind = LocationKinds.ServicePoint,
            ServicePointId = sp2.Id,
            IsActive = true
        };
        db.Locations.AddRange(sp1Loc, sp2Loc);

        db.StockBalances.AddRange(
            new StockBalance
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = sp1Loc.Id,
                LotId = lot.Id,
                QuantityOnHand = 10m,
                QuantityReserved = 0m
            },
            new StockBalance
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = sp2Loc.Id,
                LotId = lot.Id,
                QuantityOnHand = 20m,
                QuantityReserved = 0m
            });

        db.Consumptions.AddRange(
            new Consumption
            {
                OrganizationId = DemoOrganization.Id,
                ServicePointId = sp1.Id,
                ProductId = product.Id,
                OccurredOn = new DateOnly(2026, 10, 1),
                PeriodDays = 10,
                Quantity = 100m, // 10/día
                SourceKind = "test"
            },
            new Consumption
            {
                OrganizationId = DemoOrganization.Id,
                ServicePointId = sp2.Id,
                ProductId = product.Id,
                OccurredOn = new DateOnly(2026, 10, 1),
                PeriodDays = 10,
                Quantity = 100m, // 10/día
                SourceKind = "test"
            });

        await db.SaveChangesAsync();

        // 2. Ejecutar dos planificaciones independientes con el mismo reloj
        var options = DefaultOptions();
        var clock = new OperationalClock(db, options);
        var riskCalc = new RiskCalculator(db, clock, options);
        var allocator = new ReplenishmentAllocator();
        var distProvider = new DistanceProvider(db, new HttpClient(), options, NullLogger<DistanceProvider>.Instance);

        var planner1 = new RoutePlanner(db, clock, riskCalc, allocator, distProvider, options);
        var planner2 = new RoutePlanner(db, clock, riskCalc, allocator, distProvider, options);

        var result1 = await planner1.PlanAsync(new PlanRequest(new DateOnly(2026, 10, 12), null));
        var result2 = await planner2.PlanAsync(new PlanRequest(new DateOnly(2026, 10, 12), null));

        // 3. Serializar y comparar JSON canónico
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var json1 = JsonSerializer.Serialize(result1, jsonOptions);
        var json2 = JsonSerializer.Serialize(result2, jsonOptions);

        Assert.Equal(json1, json2);
        Assert.NotEmpty(result1.Routes);
        Assert.Empty(result1.Unserved);
    }
}
