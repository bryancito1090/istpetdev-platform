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

// #199: si la carga no cabe completa se planifica lo factible y el remanente queda pendiente con motivo.
[Trait("Category", "Integration")]
public sealed class PartialCapacityPlanningTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static readonly GeometryFactory Gf = new(new PrecisionModel(), 4326);

    private static IOptions<LogisticsOptions> Options() =>
        Microsoft.Extensions.Options.Options.Create(new LogisticsOptions
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
            Clock = new ClockOptions { Mode = ClockMode.Fixed, FixedDate = new DateOnly(2026, 10, 12) }
        });

    private sealed class Scenario(ApplicationDbContext db)
    {
        private readonly DomainLocation _warehouse = new()
        {
            OrganizationId = DemoOrganization.Id, Code = "BOD-T", Name = "Bodega", Kind = LocationKinds.Warehouse,
            Position = Gf.CreatePoint(new Coordinate(-78.5243, -0.2295)), IsActive = true
        };
        private readonly Dictionary<string, (Product Product, Lot Lot)> _products = new();
        private bool _warehouseAdded;

        public ApplicationDbContext Db => db;

        private void EnsureWarehouse()
        {
            if (_warehouseAdded) return;
            db.Locations.Add(_warehouse);
            _warehouseAdded = true;
        }

        public Scenario Product(string sku, decimal weight, decimal volume)
        {
            EnsureWarehouse();
            var product = new Product { OrganizationId = DemoOrganization.Id, Sku = sku, Name = sku, BaseUnit = "u",
                                        WeightPerBaseUnit = weight, VolumePerBaseUnit = volume, IsActive = true };
            var lot = new Lot { OrganizationId = DemoOrganization.Id, ProductId = product.Id, Code = $"L-{sku}", ExpiresOn = new DateOnly(2027, 6, 1) };
            db.AddRange(product, lot, new StockBalance { OrganizationId = DemoOrganization.Id, LocationId = _warehouse.Id, LotId = lot.Id, QuantityOnHand = 1000m });
            _products[sku] = (product, lot);
            return this;
        }

        public Scenario Vehicle(string code, decimal maxWeight, decimal maxVolume)
        {
            EnsureWarehouse();
            db.Vehicles.Add(new Vehicle { OrganizationId = DemoOrganization.Id, Code = code, DepotLocationId = _warehouse.Id,
                                          MaxWeight = maxWeight, MaxVolume = maxVolume, CostPerKm = 0.5m, IsActive = true });
            return this;
        }

        // Disponible 10 y demanda 16/día con cobertura objetivo de 10 días: CRITICO con sugerida de 150 por SKU.
        public Scenario Point(string code, string criticality, double lon, double lat, params string[] skus)
        {
            var sp = new ServicePoint { OrganizationId = DemoOrganization.Id, Code = code, Name = code, Category = "Hospital",
                                        Criticality = criticality, Position = Gf.CreatePoint(new Coordinate(lon, lat)),
                                        WindowStartLocal = new TimeOnly(8, 0), WindowEndLocal = new TimeOnly(17, 0), ServiceMinutes = 20, IsActive = true };
            var loc = new DomainLocation { OrganizationId = DemoOrganization.Id, Code = code, Name = code, Kind = LocationKinds.ServicePoint, ServicePointId = sp.Id, IsActive = true };
            db.AddRange(sp, loc);
            foreach (var sku in skus)
            {
                var (product, lot) = _products[sku];
                db.AddRange(
                    new StockBalance { OrganizationId = DemoOrganization.Id, LocationId = loc.Id, LotId = lot.Id, QuantityOnHand = 10m },
                    new Consumption { OrganizationId = DemoOrganization.Id, ServicePointId = sp.Id, ProductId = product.Id,
                                      OccurredOn = new DateOnly(2026, 10, 1), PeriodDays = 10, Quantity = 160m, SourceKind = "test" });
            }
            return this;
        }

        public async Task<PlanResult> PlanAsync()
        {
            await db.SaveChangesAsync();
            var options = Options();
            var clock = new OperationalClock(db, options);
            var planner = new RoutePlanner(db, clock, new RiskCalculator(db, clock, options), new ReplenishmentAllocator(),
                new DistanceProvider(db, new HttpClient(), options, NullLogger<DistanceProvider>.Instance), options);
            return await planner.PlanAsync(new PlanRequest(new DateOnly(2026, 10, 12), null));
        }
    }

    [Fact]
    public async Task Demand_of_150_units_of_1kg_with_a_100kg_vehicle_plans_100_and_keeps_50_pending()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var result = await new Scenario(db).Product("SKU-A", 1m, 0.001m).Vehicle("VEH-01", 100m, 10m)
            .Point("HOS-01", "alta", -78.50, -0.20, "SKU-A").PlanAsync();

        var route = Assert.Single(result.Routes);
        Assert.True(route.LoadWeight <= 100m);
        var load = Assert.Single(Assert.Single(route.Stops).Loads);
        Assert.Equal(100m, load.Quantity);
        Assert.Equal(150m, load.RequiredQuantity);
        Assert.Equal(50m, load.PendingQuantity);

        var unserved = Assert.Single(result.Unserved);
        Assert.Equal("capacity", unserved.Reason);
        var line = Assert.Single(unserved.Lines);
        Assert.Equal((150m, 100m, 50m), (line.RequiredQuantity, line.PlannedQuantity, line.PendingQuantity));
        Assert.Contains(result.Facts, f => f.Code == "pending_quantity" && f.Subject == "HOS-01/SKU-A" && f.Value == 50m);
    }

    [Fact]
    public async Task The_remainder_goes_to_another_vehicle_with_free_capacity()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var result = await new Scenario(db).Product("SKU-A", 1m, 0.001m)
            .Vehicle("VEH-01", 100m, 10m).Vehicle("VEH-02", 100m, 10m)
            .Point("HOS-01", "alta", -78.50, -0.20, "SKU-A").PlanAsync();

        Assert.Equal(2, result.Routes.Count);
        Assert.All(result.Routes, r => Assert.True(r.LoadWeight <= 100m));
        var loads = result.Routes.SelectMany(r => r.Stops).SelectMany(s => s.Loads).ToList();
        Assert.Equal(150m, loads.Sum(l => l.Quantity));
        Assert.All(loads, l => Assert.Equal(0m, l.PendingQuantity));
        Assert.Empty(result.Unserved);
    }

    [Fact]
    public async Task The_more_critical_point_is_served_first_even_with_a_higher_code()
    {
        await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
        var result = await new Scenario(db).Product("SKU-A", 1m, 0.001m).Vehicle("VEH-01", 100m, 10m)
            .Point("HOS-A", "media", -78.51, -0.21, "SKU-A")
            .Point("HOS-B", "alta", -78.50, -0.20, "SKU-A").PlanAsync();

        var stop = Assert.Single(Assert.Single(result.Routes).Stops);
        Assert.Equal("HOS-B", stop.PointCode);
        Assert.Equal(100m, stop.Loads.Single().Quantity);
        var pending = result.Unserved.ToDictionary(u => u.PointCode);
        Assert.Equal(50m, pending["HOS-B"].Lines.Single().PendingQuantity);
        Assert.Equal("capacity", pending["HOS-A"].Reason);
        Assert.Equal(150m, pending["HOS-A"].Lines.Single().PendingQuantity);
    }

    [Fact]
    public async Task Several_products_respect_weight_and_volume_and_repeat_the_same_result()
    {
        // SKU-A pesa y SKU-B ocupa: 150 de A (150 kg, 0,15 m³) y luego B hasta llenar el volumen de 5 m³.
        async Task<PlanResult> RunAsync()
        {
            await using var db = await TestDb.CreateMigratedAsync(postgres.NewDatabase());
            return await new Scenario(db).Product("SKU-A", 1m, 0.001m).Product("SKU-B", 0.1m, 0.1m)
                .Vehicle("VEH-01", 200m, 5m).Point("HOS-01", "alta", -78.50, -0.20, "SKU-A", "SKU-B").PlanAsync();
        }

        var first = await RunAsync();
        var second = await RunAsync();

        var route = Assert.Single(first.Routes);
        Assert.True(route.LoadWeight <= 200m);
        Assert.True(route.LoadVolume <= 5m);
        var loads = Assert.Single(route.Stops).Loads.ToDictionary(l => l.Sku);
        Assert.Equal(150m, loads["SKU-A"].Quantity);
        Assert.Equal(48m, loads["SKU-B"].Quantity);
        Assert.Equal(102m, loads["SKU-B"].PendingQuantity);
        Assert.Equal("capacity", Assert.Single(first.Unserved).Reason);

        // Las Guid de las entidades cambian entre bases; se comparan las cifras.
        static string Canon(PlanResult r) => JsonSerializer.Serialize(new
        {
            routes = r.Routes.Select(x => new { x.VehicleCode, x.LoadWeight, x.LoadVolume, stops = x.Stops.Select(s => new { s.PointCode, loads = s.Loads.Select(l => new { l.Sku, l.Quantity, l.RequiredQuantity, l.PendingQuantity }) }) }),
            unserved = r.Unserved.Select(u => new { u.PointCode, u.Reason, lines = u.Lines.Select(l => new { l.Sku, l.RequiredQuantity, l.PlannedQuantity, l.PendingQuantity }) })
        });
        Assert.Equal(Canon(first), Canon(second));
    }
}
