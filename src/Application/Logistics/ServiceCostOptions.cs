namespace IstpetDev.Application.Logistics;

public sealed class ServiceCostOptions
{
    public decimal FuelPricePerGallonUsd { get; set; }
    public DateOnly? FuelPriceEffectiveDate { get; set; }
    public string? FuelPriceSource { get; set; }
    public decimal LoadFuelFactor { get; set; } = 0.3m;
    public decimal HelperCostPerHour { get; set; }
    public decimal ExpectedShrinkPct { get; set; } = 0.5m;
    public decimal OverloadShrinkPct { get; set; } = 2m;
    public decimal OverloadThresholdPct { get; set; } = 90m;
    public Dictionary<string, VehicleCostProfile> VehicleProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ServiceLaborRule> LaborRules { get; set; } = [];

    internal IReadOnlyList<ServiceLaborRule> Rules => LaborRules.Count > 0 ? LaborRules : DefaultLaborRules;
    private static readonly ServiceLaborRule[] DefaultLaborRules =
    [
        new() { MaxKg = 150m, MaxPackageKg = 25m, Helpers = 0 },
        new() { MaxKg = 400m, MaxPackageKg = 25m, Helpers = 1 },
        new() { Helpers = 2 }
    ];

    // Una configuración incompleta nunca convierte la tarifa anterior en un costo cero.
    public bool IsValid => FuelPricePerGallonUsd >= 0 && FuelPriceEffectiveDate.HasValue &&
        !string.IsNullOrWhiteSpace(FuelPriceSource) && LoadFuelFactor >= 0 && HelperCostPerHour >= 0 &&
        ExpectedShrinkPct is >= 0 and <= 100 && OverloadShrinkPct is >= 0 and <= 100 &&
        OverloadThresholdPct is >= 0 and <= 100 && Rules.Count > 0 &&
        Rules.All(r => r.Helpers >= 0 && r.MaxKg is null or >= 0 && r.MaxPackageKg is null or >= 0) &&
        Rules.Any(r => r.MaxKg is null && r.MaxPackageKg is null);
}

public sealed class VehicleCostProfile
{
    public decimal LitersPer100Km { get; set; }
    public decimal WearPerKm { get; set; }
}

public sealed class ServiceLaborRule
{
    public decimal? MaxKg { get; set; }
    public decimal? MaxPackageKg { get; set; }
    public int Helpers { get; set; }
}
