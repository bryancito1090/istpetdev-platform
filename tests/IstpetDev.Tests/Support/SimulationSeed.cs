using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using NetTopologySuite.Geometries;
using Location = IstpetDev.Domain.Entities.Location;
using Point = NetTopologySuite.Geometries.Point;

namespace IstpetDev.Tests.Support;

public static class SimulationSeed
{
    public static readonly DateOnly StartDate = new(2026, 10, 12);

    public static async Task SeedAsync(
        ApplicationDbContext db,
        int pointCount = 2,
        decimal pointStock = 30m,
        decimal warehouseStock = 1_000_000m,
        bool includeRoadMatrix = true,
        int historyDays = 28)
    {
        var now = DateTime.UtcNow;
        db.ImportBatches.Add(new ImportBatch
        {
            OrganizationId = DemoOrganization.Id,
            ProfileName = "test-dataset",
            Status = "Applied",
            DryRun = false,
            InputHash = "simulation-test-hash",
            ProfileHash = "simulation-test-profile",
            CreatedAtUtc = now
        });

        var warehouse = new Location
        {
            OrganizationId = DemoOrganization.Id,
            Code = "BOD-01",
            Name = "Bodega de prueba",
            Kind = LocationKinds.Warehouse,
            Position = new Point(-78.50, -0.20) { SRID = 4326 },
            IsActive = true
        };
        db.Locations.Add(warehouse);

        var products = new[]
        {
            new Product
            {
                OrganizationId = DemoOrganization.Id,
                Sku = "SKU-01",
                Name = "Producto líquido",
                BaseUnit = "L",
                WeightPerBaseUnit = 1m,
                VolumePerBaseUnit = 0.001m,
                UnitValue = 2m,
                UnitsPerPackage = 10m,
                IsActive = true
            },
            new Product
            {
                OrganizationId = DemoOrganization.Id,
                Sku = "SKU-02",
                Name = "Producto sólido",
                BaseUnit = "kg",
                WeightPerBaseUnit = 1m,
                VolumePerBaseUnit = 0.001m,
                UnitValue = 3m,
                UnitsPerPackage = 10m,
                IsActive = true
            }
        };
        db.Products.AddRange(products);

        var lots = products.Select(product => new Lot
        {
            OrganizationId = DemoOrganization.Id,
            ProductId = product.Id,
            Code = $"LOTE-{product.Sku}",
            ExpiresOn = StartDate.AddYears(1)
        }).ToList();
        db.Lots.AddRange(lots);

        var points = new List<ServicePoint>();
        var pointLocations = new List<Location>();
        for (var i = 0; i < pointCount; i++)
        {
            var point = new ServicePoint
            {
                OrganizationId = DemoOrganization.Id,
                Code = $"P{i + 1:00}",
                Name = $"Punto {i + 1}",
                Region = "REG-01",
                Criticality = "alta",
                Position = new Point(-78.50 + (i * 0.001), -0.20 + (i * 0.001)) { SRID = 4326 },
                WindowStartLocal = new TimeOnly(7, 0),
                WindowEndLocal = new TimeOnly(11, 0),
                ServiceMinutes = 15,
                LeadTimeDays = 2m,
                IsActive = true
            };
            points.Add(point);
            pointLocations.Add(new Location
            {
                OrganizationId = DemoOrganization.Id,
                Code = point.Code,
                Name = point.Name,
                Kind = LocationKinds.ServicePoint,
                Position = point.Position,
                ServicePointId = point.Id,
                IsActive = true
            });
        }
        db.ServicePoints.AddRange(points);
        db.Locations.AddRange(pointLocations);

        var vehicle = new Vehicle
        {
            OrganizationId = DemoOrganization.Id,
            Code = "VEH-01",
            DepotLocationId = warehouse.Id,
            MaxWeight = 1_000_000m,
            MaxVolume = 1_000_000m,
            CostPerKm = 1.5m,
            VehicleType = "camion_liviano",
            IsActive = true
        };
        db.Vehicles.Add(vehicle);
        db.Locations.Add(new Location
        {
            OrganizationId = DemoOrganization.Id,
            Code = vehicle.Code,
            Name = "Vehículo de prueba",
            Kind = LocationKinds.Vehicle,
            VehicleId = vehicle.Id,
            IsActive = true
        });

        foreach (var product in products)
        {
            db.StockBalances.Add(new StockBalance
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = warehouse.Id,
                LotId = lots.Single(l => l.ProductId == product.Id).Id,
                QuantityOnHand = warehouseStock
            });
        }

        foreach (var point in points)
        {
            var pointLocation = pointLocations.Single(l => l.ServicePointId == point.Id);
            foreach (var product in products)
            {
                db.StockBalances.Add(new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = pointLocation.Id,
                    LotId = lots.Single(l => l.ProductId == product.Id).Id,
                    QuantityOnHand = pointStock
                });
            }
        }

        for (var day = historyDays; day >= 1; day--)
        {
            var date = StartDate.AddDays(-day);
            foreach (var point in points)
            {
                foreach (var product in products)
                {
                    db.Consumptions.Add(new Consumption
                    {
                        OrganizationId = DemoOrganization.Id,
                        ServicePointId = point.Id,
                        ProductId = product.Id,
                        OccurredOn = date,
                        Quantity = product.Sku == "SKU-01" ? 10m : 5m,
                        PeriodDays = 1,
                        SourceKind = "test"
                    });
                }
            }
        }

        if (includeRoadMatrix)
        {
            var locations = new List<(decimal Lat, decimal Lon)>
            {
                ((decimal)warehouse.Position!.Y, (decimal)warehouse.Position!.X)
            };
            locations.AddRange(points.Select(p => ((decimal)p.Position!.Y, (decimal)p.Position!.X)));
            foreach (var from in locations)
            {
                foreach (var to in locations)
                {
                    if (from == to) continue;
                    var km = SimulationGeo.Haversine(
                        new IstpetDev.Application.Logistics.GeoPoint(from.Lat, from.Lon).Round(),
                        new IstpetDev.Application.Logistics.GeoPoint(to.Lat, to.Lon).Round());
                    db.RoadMatrixEdges.Add(new RoadMatrixEdge
                    {
                        OrganizationId = DemoOrganization.Id,
                        FromLat = Math.Round(from.Lat, 5),
                        FromLon = Math.Round(from.Lon, 5),
                        ToLat = Math.Round(to.Lat, 5),
                        ToLon = Math.Round(to.Lon, 5),
                        DistanceKm = Math.Max(0.1m, Math.Round(km, 3)),
                        DurationMinutes = Math.Max(1m, Math.Round(km / 40m * 60m, 3)),
                        Source = "test",
                        RetrievedAtUtc = now
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }
}
