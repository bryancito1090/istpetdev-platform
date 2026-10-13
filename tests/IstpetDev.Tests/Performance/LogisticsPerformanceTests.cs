using System.Diagnostics;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace IstpetDev.Tests.Performance;

using DomainLocation = IstpetDev.Domain.Entities.Location;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Performance")]
public sealed class LogisticsPerformanceTests(PostgisContainer postgres)
{
    private static readonly GeometryFactory Gf = new(new PrecisionModel(), 4326);

    private static IOptions<LogisticsOptions> PerformanceOptions() =>
        Options.Create(new LogisticsOptions
        {
            DemandWindowDays = 14,
            DefaultLeadTimeDays = 2m,
            SafetyMarginDays = 3m,
            ConsolidationExtraDays = 3m,
            TargetCoverageDays = 10m,
            SafetyStockUnits = 0m,
            WarehouseSafetyStockPct = 10m,
            DefaultCostPerKm = 0.50m,
            CostPerHour = 10.00m,
            WorkdayHours = 10,
            MaxDetourMinutes = 60,
            DefaultServiceMinutes = 10,
            DefaultWindowStart = "07:00",
            DefaultWindowEnd = "19:00",
            Clock = new ClockOptions
            {
                Mode = ClockMode.Fixed,
                FixedDate = new DateOnly(2026, 10, 12)
            }
        });

    private static async Task SeedSyntheticScenarioAsync(
        ApplicationDbContext db,
        int pointCount,
        int vehicleCount,
        int seed = 42)
    {
        var random = new Random(seed);

        var warehouseLoc = new DomainLocation
        {
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-PERF",
            Name = "Bodega Performance",
            Kind = LocationKinds.Warehouse,
            Position = Gf.CreatePoint(new Coordinate(-78.5000, -0.2000)),
            IsActive = true
        };
        db.Locations.Add(warehouseLoc);

        var product = new Product
        {
            OrganizationId = DemoOrganization.Id,
            Sku = "SKU-PERF",
            Name = "Producto Performance",
            BaseUnit = "UN",
            WeightPerBaseUnit = 1.0m,
            VolumePerBaseUnit = 0.01m,
            IsActive = true
        };
        db.Products.Add(product);

        var lot = new Lot
        {
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = "LOT-PERF",
            ExpiresOn = new DateOnly(2030, 1, 1),
            IsBlocked = false
        };
        db.Lots.Add(lot);

        db.StockBalances.Add(new StockBalance
        {
            OrganizationId = DemoOrganization.Id,
            LocationId = warehouseLoc.Id,
            LotId = lot.Id,
            QuantityOnHand = 50000m,
            QuantityReserved = 0m
        });

        for (int v = 1; v <= vehicleCount; v++)
        {
            var veh = new Vehicle
            {
                OrganizationId = DemoOrganization.Id,
                Code = $"VEH-P{v:D2}",
                DepotLocationId = warehouseLoc.Id,
                MaxWeight = 2500m,
                MaxVolume = 20m,
                CostPerKm = 0.45m,
                IsActive = true
            };
            db.Vehicles.Add(veh);
        }

        for (int i = 1; i <= pointCount; i++)
        {
            // Puntos dispersos en rango Quito: Lat [-0.35, -0.10], Lon [-78.55, -78.45]
            double lat = -0.35 + (random.NextDouble() * 0.25);
            double lon = -78.55 + (random.NextDouble() * 0.10);

            var sp = new ServicePoint
            {
                OrganizationId = DemoOrganization.Id,
                Code = $"SP-P{i:D3}",
                Name = $"Punto {i}",
                Criticality = (i % 3 == 0) ? "alta" : "normal",
                ServiceMinutes = 10,
                WindowStartLocal = new TimeOnly(7, 0),
                WindowEndLocal = new TimeOnly(19, 0),
                LeadTimeDays = 2m,
                Position = Gf.CreatePoint(new Coordinate(lon, lat)),
                IsActive = true
            };
            db.ServicePoints.Add(sp);

            var spLoc = new DomainLocation
            {
                OrganizationId = DemoOrganization.Id,
                Code = sp.Code,
                Name = sp.Name,
                Kind = LocationKinds.ServicePoint,
                ServicePointId = sp.Id,
                Position = sp.Position,
                IsActive = true
            };
            db.Locations.Add(spLoc);

            db.StockBalances.Add(new StockBalance
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = spLoc.Id,
                LotId = lot.Id,
                QuantityOnHand = 10m,
                QuantityReserved = 0m
            });

            db.Consumptions.Add(new Consumption
            {
                OrganizationId = DemoOrganization.Id,
                ServicePointId = sp.Id,
                ProductId = product.Id,
                PeriodDays = 7,
                Quantity = 70m, // 10/dia -> cobertura 1 dia -> Critico
                OccurredOn = new DateOnly(2026, 10, 5),
                SourceKind = "test"
            });
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Benchmark_15_points_completes_under_1_second()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        await SeedSyntheticScenarioAsync(db, pointCount: 15, vehicleCount: 2, seed: 1515);

        var opts = PerformanceOptions();
        var clock = new OperationalClock(db, opts);
        var distProvider = new DistanceProvider(db, new HttpClient(), opts, NullLogger<DistanceProvider>.Instance);
        var riskCalc = new RiskCalculator(db, clock, opts);
        var allocator = new ReplenishmentAllocator();
        var planner = new RoutePlanner(db, clock, riskCalc, allocator, distProvider, opts);

        var request = new PlanRequest(new DateOnly(2026, 10, 12), null, true);

        // Warmup
        await planner.PlanAsync(request);

        // Cronometraje exclusivo de PlanAsync
        var sw = Stopwatch.StartNew();
        var result = await planner.PlanAsync(request);
        sw.Stop();

        Assert.NotNull(result);
        Assert.True(result.Routes.Count > 0);
        Assert.True(sw.ElapsedMilliseconds <= 1000, $"Planificación de 15 puntos tardó {sw.ElapsedMilliseconds} ms (presupuesto: <= 1000 ms)");
    }

    [Fact]
    public async Task Benchmark_80_points_completes_under_5_seconds()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        await SeedSyntheticScenarioAsync(db, pointCount: 80, vehicleCount: 2, seed: 8080);

        var opts = PerformanceOptions();
        var clock = new OperationalClock(db, opts);
        var distProvider = new DistanceProvider(db, new HttpClient(), opts, NullLogger<DistanceProvider>.Instance);
        var riskCalc = new RiskCalculator(db, clock, opts);
        var allocator = new ReplenishmentAllocator();
        var planner = new RoutePlanner(db, clock, riskCalc, allocator, distProvider, opts);

        var request = new PlanRequest(new DateOnly(2026, 10, 12), null, true);

        // Warmup
        await planner.PlanAsync(request);

        // Cronometraje exclusivo de PlanAsync
        var sw = Stopwatch.StartNew();
        var result = await planner.PlanAsync(request);
        sw.Stop();

        Assert.NotNull(result);
        Assert.True(result.Routes.Count > 0);
        Assert.True(sw.ElapsedMilliseconds <= 5000, $"Planificación de 80 puntos tardó {sw.ElapsedMilliseconds} ms (presupuesto: <= 5000 ms)");
    }
}
