namespace IstpetDev.Application.Logistics;

/// <summary>
/// Calculador canónico y unificado de costo logístico para IstpetDev (Versión 1.0).
/// Aplica el perímetro completo:
/// C_total = Peajes/CargosFijos + (DistanciaKm * CostoPorKm) + ((Minutos / 60) * CostoPorHora)
/// </summary>
public static class LogisticsCostCalculator
{
    public const string Version = "1.0";
    public const string DefaultCurrency = "USD";

    /// <summary>
    /// Calcula el costo total de transporte y retorna el valor redondeado a 2 decimales.
    /// </summary>
    public static decimal CalculateCost(
        decimal distanceKm,
        decimal durationMinutes,
        decimal costPerKm,
        decimal costPerHour,
        decimal tollsCost = 0m)
    {
        var kmCost = Math.Max(0m, distanceKm) * Math.Max(0m, costPerKm);
        var hours = Math.Max(0m, durationMinutes) / 60m;
        var hourCost = hours * Math.Max(0m, costPerHour);
        var total = Math.Max(0m, tollsCost) + kmCost + hourCost;

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Desglosa los componentes del costo logístico de manera transparente y auditable.
    /// </summary>
    public static CostBreakdown ComputeBreakdown(
        decimal distanceKm,
        decimal durationMinutes,
        decimal costPerKm,
        decimal costPerHour,
        decimal tollsCost = 0m,
        string currency = DefaultCurrency)
    {
        var cleanDist = Math.Max(0m, distanceKm);
        var cleanMin = Math.Max(0m, durationMinutes);
        var cleanCostKm = Math.Max(0m, costPerKm);
        var cleanCostH = Math.Max(0m, costPerHour);
        var cleanTolls = Math.Max(0m, tollsCost);

        var kmCost = Math.Round(cleanDist * cleanCostKm, 2, MidpointRounding.AwayFromZero);
        var hours = Math.Round(cleanMin / 60m, 4);
        var hourCost = Math.Round((cleanMin / 60m) * cleanCostH, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(cleanTolls + (cleanDist * cleanCostKm) + ((cleanMin / 60m) * cleanCostH), 2, MidpointRounding.AwayFromZero);

        return new CostBreakdown(
            DistanceKm: cleanDist,
            DurationMinutes: cleanMin,
            DurationHours: hours,
            CostPerKm: cleanCostKm,
            CostPerHour: cleanCostH,
            KmCost: kmCost,
            HourCost: hourCost,
            TollsCost: cleanTolls,
            TotalCost: total,
            Currency: currency,
            CalculatorVersion: Version
        );
    }
}

public sealed record CostBreakdown(
    decimal DistanceKm,
    decimal DurationMinutes,
    decimal DurationHours,
    decimal CostPerKm,
    decimal CostPerHour,
    decimal KmCost,
    decimal HourCost,
    decimal TollsCost,
    decimal TotalCost,
    string Currency,
    string CalculatorVersion
);
