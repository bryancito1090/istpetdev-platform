using System.Text.Json;
using Microsoft.Extensions.Configuration;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class ServiceCostCalculatorTests
{
    private readonly Product _product = new() { Sku = "TEST", UnitValue = 10m, UnitsPerPackage = 10m, WeightPerBaseUnit = 2m };
    private readonly Vehicle _vehicle = new() { VehicleType = "camioneta", MaxWeight = 1000m, CostPerKm = 0.45m };

    public static LogisticsOptions Configured() => new()
    {
        ServiceCost = new()
        {
            FuelPricePerGallonUsd = 3.785m,
            FuelPriceEffectiveDate = new(2026, 10, 16),
            FuelPriceSource = "sintético",
            HelperCostPerHour = 4m,
            VehicleProfiles = new() { ["camioneta"] = new() { LitersPer100Km = 10m, WearPerKm = 0.1m } }
        }
    };

    private RouteStopCandidate Stop(decimal weight, string kind = RouteStopKind.Delivery, decimal quantity = 100m) =>
        new(1, kind, null, null, "STOP", "Prueba", new GeoPoint(0, 0), weight, 0,
            new(0, 0), new(23, 59), 15, [new(_product.Id, _product.Sku, quantity, "UN")]);

    private ServiceCostCalculator Calculator(LogisticsOptions? options = null) =>
        new(options ?? Configured(), new Dictionary<Guid, Product> { [_product.Id] = _product });

    [Theory]
    [InlineData(0, 0)]
    [InlineData(150, 0)]
    [InlineData(150.01, 1)]
    [InlineData(400, 1)]
    [InlineData(400.01, 2)]
    public void Labor_rules_respect_weight_boundaries(decimal weight, int helpers) =>
        Assert.Equal(helpers, Calculator().HelpersForStop(Stop(-weight)));

    [Fact]
    public void Heavy_package_requires_two_helpers_even_at_low_total_weight()
    {
        _product.UnitsPerPackage = 15m; // 30 kg por bulto
        Assert.Equal(2, Calculator().HelpersForStop(Stop(-90, quantity: 45)));
        // Un paquete parcial de 2 unidades pesa 4 kg, no 30 kg.
        Assert.Equal(0, Calculator().HelpersForStop(Stop(-4, quantity: 2)));
    }

    [Fact]
    public void Complete_formula_uses_route_crew_and_counts_cargo_value_once()
    {
        var result = Calculator().Calculate(100, 120, _vehicle, 500,
            [Stop(500, RouteStopKind.Pickup), Stop(-200), Stop(-300)]);
        Assert.Equal(11.50m, result.Fuel);
        Assert.Equal(10m, result.Wear);
        Assert.Equal(12m, result.Driver);
        Assert.Equal(2, result.HelperCount); // máximo de todas las paradas, incluida la recogida
        Assert.Equal(16m, result.Helpers);
        Assert.Equal(5m, result.Shrink);
        Assert.Equal(54.50m, result.Total);
        Assert.Equal(50m, result.LoadPct);
        Assert.Equal(result.Fuel + result.Wear + result.Driver + result.Helpers + result.Shrink, result.Total);
    }

    [Fact]
    public void Fuel_increases_with_load_including_over_capacity_without_clamping()
    {
        var calc = Calculator();
        var empty = calc.Calculate(100, 60, _vehicle, 0, []);
        var half = calc.Calculate(100, 60, _vehicle, 500, []);
        var full = calc.Calculate(100, 60, _vehicle, 1000, []);
        var overload = calc.Calculate(100, 60, _vehicle, 1100, []);
        Assert.Equal(10m, empty.Fuel);
        Assert.Equal(11.5m, half.Fuel);
        Assert.Equal(13m, full.Fuel);
        Assert.True(overload.Fuel > full.Fuel);
    }

    [Theory]
    [InlineData(899, 5)]
    [InlineData(900, 5)]
    [InlineData(900.01, 20)]
    public void Shrink_rate_changes_strictly_above_threshold(decimal weight, decimal shrink) =>
        Assert.Equal(shrink, Calculator().Calculate(100, 60, _vehicle, weight,
            [Stop(weight, RouteStopKind.Pickup), Stop(-weight)]).Shrink);

    [Fact]
    public void Largest_stop_crew_travels_for_the_whole_route()
    {
        var result = Calculator().Calculate(100, 120, _vehicle, 800,
            [Stop(800, RouteStopKind.Pickup), Stop(-100), Stop(-200), Stop(-500)]);
        Assert.Equal(2, result.HelperCount);
        Assert.Equal(16m, result.Helpers);
    }

    [Theory]
    [InlineData(100, 20)] // 10 bultos, conductor
    [InlineData(200, 15)] // 10 bultos, 1 ayudante
    [InlineData(500, 14)] // 10 bultos, 2 ayudantes, techo al minuto
    public void Unspecified_unloading_time_uses_packages_and_local_helpers(decimal weight, int minutes) =>
        Assert.Equal(minutes, Calculator().ServiceMinutes(Stop(-weight), _vehicle));

    [Fact]
    public void Route_crew_from_pickup_also_accelerates_smaller_deliveries()
    {
        var calc = Calculator();
        var stops = new[] { Stop(500, RouteStopKind.Pickup), Stop(-100), Stop(-400) };
        var crew = calc.HelperCount(stops);
        Assert.Equal(2, crew);
        Assert.Equal(14, calc.ServiceMinutes(stops[1], _vehicle, crew));
    }

    [Fact]
    public void Explicit_point_time_including_zero_is_preserved()
    {
        var id = Guid.NewGuid();
        var stop = Stop(-100) with { ServicePointId = id };
        foreach (var minutes in new[] { 0, 27 })
        {
            var calc = new ServiceCostCalculator(Configured(), new Dictionary<Guid, Product> { [_product.Id] = _product },
                new Dictionary<Guid, int?> { [id] = minutes });
            Assert.Equal(minutes, calc.ServiceMinutes(stop, _vehicle));
        }
    }

    [Fact]
    public void Fractional_packages_round_up_per_sku_not_per_line()
    {
        var stop = Stop(-100, quantity: 21) with { Loads = [new(_product.Id, "TEST", 21, "UN"), new(_product.Id, "TEST", 9, "UN")] };
        Assert.Equal(13, Calculator().ServiceMinutes(stop, _vehicle));
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("untyped")]
    [InlineData("unknown")]
    [InlineData("incomplete")]
    [InlineData("negative")]
    public void Unconfigured_service_or_profile_preserves_legacy_cost_and_times(string reason)
    {
        var options = Configured();
        if (reason == "absent") options.ServiceCost = null;
        if (reason == "untyped") _vehicle.VehicleType = null;
        if (reason == "unknown") _vehicle.VehicleType = "camion_pesado";
        if (reason == "incomplete") options.ServiceCost!.FuelPriceEffectiveDate = null;
        if (reason == "negative") options.ServiceCost!.LoadFuelFactor = -1;
        var calc = Calculator(options);
        var stop = Stop(-500);
        var result = calc.Calculate(123.456m, 197.5m, _vehicle, 500, [stop]);
        Assert.Equal(LogisticsCostCalculator.CalculateCost(123.456m, 197.5m, 0.45m, 6m), result.Total);
        Assert.Equal(15, calc.ServiceMinutes(stop, _vehicle));
        Assert.Contains(result.Notes, n => n.Contains("perfil no configurado"));
        Assert.Equal(0, result.Fuel);
        Assert.True(result.LegacyTransport > 0);
        if (reason is "absent" or "incomplete")
        {
            Assert.Null(result.FuelPrice.Value);
            Assert.Null(result.FuelPrice.Date);
            Assert.Null(result.FuelPrice.Source);
        }
    }

    [Fact]
    public void Missing_catalog_data_is_explicit_and_never_divides_by_zero()
    {
        _product.UnitValue = null;
        _product.UnitsPerPackage = 0;
        var calc = Calculator();
        var result = calc.Calculate(100, 60, _vehicle, 100, [Stop(100, RouteStopKind.Pickup), Stop(-100)]);
        Assert.Equal(0, result.Shrink);
        Assert.Equal(2, result.HelperCount);
        Assert.Equal(15, calc.ServiceMinutes(Stop(-100), _vehicle));
        Assert.Contains(result.Notes, n => n.Contains("valor unitario sin dato"));
        Assert.Contains(result.Notes, n => n.Contains("bultos sin dato"));
    }

    [Fact]
    public void Configured_labor_rules_replace_defaults_instead_of_appending_after_them()
    {
        var settings = new Dictionary<string, string?> {
            ["ServiceCost:FuelPriceEffectiveDate"] = "2026-10-16",
            ["ServiceCost:FuelPriceSource"] = "sintético",
            ["ServiceCost:LaborRules:0:Helpers"] = "3"
        };
        var options = new ConfigurationBuilder().AddInMemoryCollection(settings).Build().Get<LogisticsOptions>()!;
        Assert.Equal(3, Calculator(options).HelpersForStop(Stop(-100)));
    }

    [Fact]
    public void Json_exposes_estimated_label_and_dated_synthetic_fuel_price()
    {
        var result = Calculator().Calculate(100, 60, _vehicle, 500, []);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Equal("estimado", json.RootElement.GetProperty("label").GetString());
        var price = json.RootElement.GetProperty("fuelPrice");
        Assert.Equal(3.785m, price.GetProperty("value").GetDecimal());
        Assert.Equal("2026-10-16", price.GetProperty("date").GetString());
        Assert.Equal("sintético", price.GetProperty("source").GetString());
    }
}
