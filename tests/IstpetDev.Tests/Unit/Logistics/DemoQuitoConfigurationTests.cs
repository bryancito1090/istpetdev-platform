using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Unit.Logistics;

public class DemoQuitoConfigurationTests
{
    private static string DemoQuitoConfigFile =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json"));

    [Fact]
    public void Appsettings_demo_quito_exists_and_binds_valid_configuration()
    {
        Assert.True(File.Exists(DemoQuitoConfigFile), $"El archivo {DemoQuitoConfigFile} debe existir.");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(DemoQuitoConfigFile, optional: false)
            .Build();

        var options = new LogisticsOptions();
        configuration.GetSection(LogisticsOptions.SectionName).Bind(options);

        // ServiceCost
        Assert.NotNull(options.ServiceCost);
        Assert.True(options.ServiceCost.IsValid, "ServiceCost debe ser válido según sus reglas de negocio.");
        Assert.Equal(3.11m, options.ServiceCost.FuelPricePerGallonUsd);
        Assert.Equal(new DateOnly(2026, 10, 12), options.ServiceCost.FuelPriceEffectiveDate);
        Assert.Contains("Primicias", options.ServiceCost.FuelPriceSource);
        Assert.Equal(0.3m, options.ServiceCost.LoadFuelFactor);
        Assert.Equal(4.13m, options.ServiceCost.HelperCostPerHour);
        Assert.Equal(0.5m, options.ServiceCost.ExpectedShrinkPct);
        Assert.Equal(2.0m, options.ServiceCost.OverloadShrinkPct);
        Assert.Equal(90.0m, options.ServiceCost.OverloadThresholdPct);

        // Perfiles de vehículo
        Assert.True(options.ServiceCost.VehicleProfiles.ContainsKey("camioneta"));
        Assert.True(options.ServiceCost.VehicleProfiles.ContainsKey("camion_liviano"));
        Assert.True(options.ServiceCost.VehicleProfiles.ContainsKey("camion_pesado"));
        Assert.Equal(12.21m, options.ServiceCost.VehicleProfiles["camioneta"].LitersPer100Km);
        Assert.Equal(14.84m, options.ServiceCost.VehicleProfiles["camion_liviano"].LitersPer100Km);
        Assert.Equal(18.93m, options.ServiceCost.VehicleProfiles["camion_pesado"].LitersPer100Km);

        // Reglas de ayudantes
        Assert.Equal(3, options.ServiceCost.LaborRules.Count);
        Assert.Equal(0, options.ServiceCost.LaborRules[0].Helpers);
        Assert.Equal(1, options.ServiceCost.LaborRules[1].Helpers);
        Assert.Equal(2, options.ServiceCost.LaborRules[2].Helpers);

        // TrafficBands
        Assert.Equal(3, options.TrafficBands.Count);
        Assert.Equal("06:00", options.TrafficBands[0].Start);
        Assert.Equal("09:30", options.TrafficBands[0].End);
        Assert.Equal(0.64m, options.TrafficBands[0].SpeedFactor);

        Assert.Equal("12:30", options.TrafficBands[1].Start);
        Assert.Equal("14:00", options.TrafficBands[1].End);
        Assert.Equal(0.80m, options.TrafficBands[1].SpeedFactor);

        Assert.Equal("16:00", options.TrafficBands[2].Start);
        Assert.Equal("20:00", options.TrafficBands[2].End);
        Assert.Equal(0.64m, options.TrafficBands[2].SpeedFactor);

        // ContractedCarrier
        Assert.NotNull(options.ContractedCarrier);
        Assert.True(options.ContractedCarrier.IsConfigured);
        Assert.Equal(25.0m, options.ContractedCarrier.BaseFeeUsd);
        Assert.Equal(0.55m, options.ContractedCarrier.PerKmUsd);
        Assert.Equal(0.01m, options.ContractedCarrier.PerKgUsd);
        Assert.Equal(4, options.ContractedCarrier.ResponseHours);

        // Recommendation
        Assert.NotNull(options.Recommendation);
        Assert.Equal(0.35m, options.Recommendation.WeightCost);
        Assert.Equal(0.35m, options.Recommendation.WeightCriticalOnTime);
        Assert.Equal(0.20m, options.Recommendation.WeightPendingDemand);
        Assert.Equal(0.10m, options.Recommendation.WeightRisk);
    }

    [Fact]
    public void ServiceCostCalculator_with_demo_quito_config_calculates_non_zero_breakdown()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(DemoQuitoConfigFile, optional: false)
            .Build();

        var options = new LogisticsOptions();
        configuration.GetSection(LogisticsOptions.SectionName).Bind(options);

        var vehicle = new Vehicle
        {
            Code = "VEH-0001",
            VehicleType = "camion_liviano",
            UsageScope = "urbano",
            MaxWeight = 3500m,
            MaxVolume = 14m,
            CostPerKm = 0.21m
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = "PRD-0003",
            Name = "Detergente industrial en polvo (saco 30 kg)",
            UnitValue = 42.0m,
            WeightPerBaseUnit = 30.3m,
            VolumePerBaseUnit = 0.045m,
            UnitsPerPackage = 1m
        };

        var stopPickup = new RouteStopCandidate(
            Sequence: 1,
            Kind: RouteStopKind.Pickup,
            ServicePointId: null,
            LocationId: Guid.NewGuid(),
            Code: "BOD-0001",
            Name: "Bodega Central Sur",
            Position: new GeoPoint(-0.31888m, -78.54932m),
            DeltaWeight: 303m,
            DeltaVolume: 0.45m,
            WindowStart: new TimeOnly(7, 0),
            WindowEnd: new TimeOnly(18, 0),
            ServiceMinutes: 20,
            Loads: [new(product.Id, product.Sku, 10m, "saco")]
        );

        var stopDelivery = new RouteStopCandidate(
            Sequence: 2,
            Kind: RouteStopKind.Delivery,
            ServicePointId: Guid.NewGuid(),
            LocationId: null,
            Code: "PTO-0001",
            Name: "Hospital Eugenio Espejo",
            Position: new GeoPoint(-0.21531m, -78.49848m),
            DeltaWeight: -303m,
            DeltaVolume: -0.45m,
            WindowStart: new TimeOnly(7, 0),
            WindowEnd: new TimeOnly(11, 0),
            ServiceMinutes: 30,
            Loads: [new(product.Id, product.Sku, 10m, "saco")]
        );

        var calculator = new ServiceCostCalculator(options, new Dictionary<Guid, Product> { [product.Id] = product });
        var breakdown = calculator.Calculate(
            distanceKm: 30.0m,
            durationMinutes: 70.0m,
            vehicle: vehicle,
            maxWeightOnRoute: 303m,
            stops: [stopPickup, stopDelivery]
        );

        Assert.True(breakdown.Fuel > 0m, $"Combustible debe ser > 0, fue {breakdown.Fuel}");
        Assert.True(breakdown.Wear > 0m, $"Desgaste debe ser > 0, fue {breakdown.Wear}");
        Assert.True(breakdown.Driver > 0m, $"Conductor debe ser > 0, fue {breakdown.Driver}");
        Assert.True(breakdown.Helpers > 0m, $"Ayudantes debe ser > 0 (bulto 30.3 kg > 25 kg), fue {breakdown.Helpers}");
        Assert.True(breakdown.Shrink > 0m, $"Merma debe ser > 0, fue {breakdown.Shrink}");
        Assert.Equal(breakdown.Total, breakdown.Fuel + breakdown.Wear + breakdown.Driver + breakdown.Helpers + breakdown.Shrink);
        Assert.Equal(3.11m, breakdown.FuelPrice?.Value);
    }

    [Fact]
    public async Task RouteEvaluator_with_demo_quito_traffic_bands_takes_longer_at_0730_than_at_1100()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(DemoQuitoConfigFile, optional: false)
            .Build();

        var options = new LogisticsOptions();
        configuration.GetSection(LogisticsOptions.SectionName).Bind(options);

        var vehicle = new Vehicle
        {
            Code = "VEH-0001",
            VehicleType = "camion_liviano",
            UsageScope = "urbano",
            MaxWeight = 3500m,
            MaxVolume = 14m,
            CostPerKm = 0.21m
        };

        var originPoint = new GeoPoint(-0.31888m, -78.54932m);
        var destPoint = new GeoPoint(-0.21531m, -78.49848m);

        var stopPickup = new RouteStopCandidate(
            Sequence: 1,
            Kind: RouteStopKind.Pickup,
            ServicePointId: null,
            LocationId: Guid.NewGuid(),
            Code: "BOD-0001",
            Name: "Bodega Central Sur",
            Position: originPoint,
            DeltaWeight: 100m,
            DeltaVolume: 0.2m,
            WindowStart: new TimeOnly(6, 0),
            WindowEnd: new TimeOnly(18, 0),
            ServiceMinutes: 10
        );

        var stopDelivery = new RouteStopCandidate(
            Sequence: 2,
            Kind: RouteStopKind.Delivery,
            ServicePointId: Guid.NewGuid(),
            LocationId: null,
            Code: "PTO-0001",
            Name: "Hospital Eugenio Espejo",
            Position: destPoint,
            DeltaWeight: -100m,
            DeltaVolume: -0.2m,
            WindowStart: new TimeOnly(7, 0),
            WindowEnd: new TimeOnly(12, 0),
            ServiceMinutes: 20
        );

        var distance = new FixedDistanceProvider(km: 15.0m, minutes: 30.0m);
        var evaluator = new RouteEvaluator(distance, Options.Create(options));

        // En UTC-5:
        // 07:30 local = 12:30 UTC (dentro de franja pico 06:00-09:30, SpeedFactor = 0.64)
        var utcPeak = new DateTime(2026, 10, 16, 12, 30, 0, DateTimeKind.Utc);
        var evalPeak = await evaluator.EvaluateSequenceAsync([stopPickup, stopDelivery], vehicle, utcPeak);

        // 11:00 local = 16:00 UTC (fuera de horas pico, SpeedFactor = 1.0)
        var utcOffPeak = new DateTime(2026, 10, 16, 16, 00, 0, DateTimeKind.Utc);
        var evalOffPeak = await evaluator.EvaluateSequenceAsync([stopPickup, stopDelivery], vehicle, utcOffPeak);

        Assert.True(evalPeak.TotalDurationMinutes > evalOffPeak.TotalDurationMinutes,
            $"Duración en hora pico (07:30)={evalPeak.TotalDurationMinutes} debe ser mayor que fuera de pico (11:00)={evalOffPeak.TotalDurationMinutes}");

        Assert.Equal(0.64m, evalPeak.Stops[1].TrafficFactorApplied);
        Assert.Null(evalOffPeak.Stops[1].TrafficFactorApplied);
    }

    private sealed class FixedDistanceProvider(decimal km, decimal minutes) : IDistanceProvider
    {
        public Task<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>> MatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default)
        {
            var dict = new Dictionary<(GeoPoint, GeoPoint), Leg>();
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                {
                    dict[(points[i], points[j])] = new Leg(
                        points[i],
                        points[j],
                        i == j ? 0m : km,
                        i == j ? 0m : minutes,
                        DistanceSource.Road,
                        null
                    );
                }
            }
            return Task.FromResult<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>>(dict);
        }
    }
}
