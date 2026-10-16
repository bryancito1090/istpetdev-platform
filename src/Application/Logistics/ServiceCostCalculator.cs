using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Logistics;

public sealed record ServiceFuelPrice(decimal? Value, DateOnly? Date, string? Source, string Unit = "USD/gal");

public sealed record ServiceCostBreakdown(
    decimal Fuel, decimal Wear, decimal Driver, decimal Helpers, decimal Shrink, decimal Total,
    decimal LoadPct, int HelperCount, ServiceFuelPrice FuelPrice, IReadOnlyList<string> Notes,
    decimal LegacyTransport = 0m, string Label = "estimado", string Currency = "USD");

/// <summary>Estimación sintética. Nunca modifica el calculador ni las tarifas históricas.</summary>
public sealed class ServiceCostCalculator(
    LogisticsOptions options,
    IReadOnlyDictionary<Guid, Product>? products = null,
    IReadOnlyDictionary<Guid, int?>? pointServiceMinutes = null)
{
    public static async Task<ServiceCostCalculator> LoadAsync(LogisticsOptions options,
        IReadOnlyList<RouteStopCandidate> stops, IApplicationDbContext? db, CancellationToken ct)
    {
        if (options.ServiceCost is null || db is null) return new(options);
        var ids = stops.SelectMany(s => s.Loads ?? []).Select(l => l.ProductId).Distinct().ToArray();
        var pointIds = stops.Where(s => s.ServicePointId.HasValue).Select(s => s.ServicePointId!.Value).Distinct().ToArray();
        var products = await db.Products.AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id && ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var points = await db.ServicePoints.AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id && pointIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ServiceMinutes, ct);
        return new(options, products, points);
    }

    private VehicleCostProfile? Profile(Vehicle? vehicle) => options.ServiceCost is { IsValid: true } config &&
        vehicle?.VehicleType is { } type && config.VehicleProfiles.TryGetValue(type, out var profile) &&
        profile.LitersPer100Km >= 0 && profile.WearPerKm >= 0 ? profile : null;

    public int HelpersForStop(RouteStopCandidate stop)
    {
        if (options.ServiceCost is not { IsValid: true } config) return 0;
        var (_, maxPackageKg) = Packages(stop);
        return config.Rules.First(r =>
            (!r.MaxKg.HasValue || Math.Abs(stop.DeltaWeight) <= r.MaxKg.Value) &&
            (!r.MaxPackageKg.HasValue || maxPackageKg.HasValue && maxPackageKg.Value <= r.MaxPackageKg.Value)).Helpers;
    }

    public int HelperCount(IReadOnlyList<RouteStopCandidate> stops) =>
        stops.Select(HelpersForStop).DefaultIfEmpty(0).Max();

    public int ServiceMinutes(RouteStopCandidate stop, Vehicle? vehicle, int? routeHelperCount = null)
    {
        if (Profile(vehicle) is null || stop.Kind != RouteStopKind.Delivery) return stop.ServiceMinutes;
        if (stop.ServicePointId is { } id)
        {
            // Sin acceso al punto no se puede distinguir el tiempo explícito del fallback.
            if (pointServiceMinutes is null || !pointServiceMinutes.TryGetValue(id, out var minutes)) return stop.ServiceMinutes;
            if (minutes.HasValue) return minutes.Value;
        }
        var (packages, _) = Packages(stop);
        return packages.HasValue
            ? (int)Math.Ceiling(10m + packages.Value / (1m + (routeHelperCount ?? HelpersForStop(stop))))
            : stop.ServiceMinutes;
    }

    private (decimal? Count, decimal? MaxKg) Packages(RouteStopCandidate stop)
    {
        if (stop.Loads is not { Count: > 0 } || products is null) return (null, null);
        decimal count = 0, maxKg = 0;
        foreach (var group in stop.Loads.GroupBy(l => l.ProductId))
        {
            if (!products.TryGetValue(group.Key, out var product) || product.UnitsPerPackage is not > 0 || product.WeightPerBaseUnit <= 0)
                return (null, null);
            var quantity = group.Sum(l => Math.Max(0, l.Quantity));
            count += Math.Ceiling(quantity / product.UnitsPerPackage.Value);
            maxKg = Math.Max(maxKg, Math.Min(quantity, product.UnitsPerPackage.Value) * product.WeightPerBaseUnit);
        }
        return (count, maxKg);
    }

    public ServiceCostBreakdown Calculate(decimal distanceKm, decimal durationMinutes, Vehicle? vehicle,
        decimal maxWeightOnRoute, IReadOnlyList<RouteStopCandidate> stops)
    {
        var config = options.ServiceCost;
        var price = config is { FuelPriceEffectiveDate: not null, FuelPricePerGallonUsd: >= 0 } &&
            !string.IsNullOrWhiteSpace(config.FuelPriceSource)
            ? new ServiceFuelPrice(config.FuelPricePerGallonUsd, config.FuelPriceEffectiveDate, config.FuelPriceSource)
            : new ServiceFuelPrice(null, null, null);
        var loadPct = vehicle?.MaxWeight > 0 ? Math.Max(0m, maxWeightOnRoute) / vehicle.MaxWeight * 100m : 0m;
        var notes = new List<string> { "Costo estimado; parámetros sintéticos, no mediciones." };
        var km = Math.Max(0m, distanceKm);
        var hours = Math.Max(0m, durationMinutes) / 60m;
        var driver = hours * Math.Max(0m, options.CostPerHour);
        var profile = Profile(vehicle);
        if (profile is null)
        {
            notes.Add("perfil no configurado; se conserva la fórmula anterior de km y horas.");
            if (config is null) notes.Add("ServiceCost no configurado.");
            else if (!config.IsValid) notes.Add("ServiceCost incompleto o inválido; requiere precio, fecha, fuente y reglas válidas.");
            var rate = vehicle?.CostPerKm ?? options.DefaultCostPerKm;
            return new(0, 0, Round(driver), 0, 0,
                LogisticsCostCalculator.CalculateCost(km, durationMinutes, rate, options.CostPerHour),
                loadPct, 0, price, notes, Round(km * Math.Max(0, rate)));
        }

        var helperCount = HelperCount(stops);
        var fuel = km * profile.LitersPer100Km / 100m * (1m + config!.LoadFuelFactor * loadPct / 100m) * config.FuelPricePerGallonUsd / 3.785m;
        var wear = km * profile.WearPerKm;
        var helpers = helperCount * hours * config.HelperCostPerHour;
        decimal cargoValue = 0m;
        // Solo recogidas: las mismas unidades no se vuelven a valorar en cada entrega.
        foreach (var stop in stops.Where(s => s.Kind == RouteStopKind.Pickup))
        {
            if (stop.Loads is not { Count: > 0 }) notes.Add($"{stop.Code}: valor de carga sin dato; merma parcial.");
            foreach (var load in stop.Loads ?? [])
                if (products is not null && products.TryGetValue(load.ProductId, out var product) && product.UnitValue is >= 0)
                    cargoValue += Math.Max(0m, load.Quantity) * product.UnitValue.Value;
                else notes.Add($"{load.Sku}: valor unitario sin dato; merma parcial.");
        }
        foreach (var stop in stops.Where(s => Packages(s).Count is null))
            notes.Add($"{stop.Code}: bultos sin dato; cuadrilla conservadora y tiempo definido o anterior.");
        var shrinkPct = loadPct > config.OverloadThresholdPct ? config.OverloadShrinkPct : config.ExpectedShrinkPct;
        var shrink = cargoValue * shrinkPct / 100m;
        // Los componentes visibles en centavos suman exactamente el total publicado.
        return new(Round(fuel), Round(wear), Round(driver), Round(helpers), Round(shrink),
            Round(fuel) + Round(wear) + Round(driver) + Round(helpers) + Round(shrink),
            loadPct, helperCount, price, notes.Distinct().ToArray());
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
