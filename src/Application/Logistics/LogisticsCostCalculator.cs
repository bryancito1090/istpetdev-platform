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
    /// Garantiza paridad exacta con ComputeBreakdown: Total = Peajes + KmCost + HourCost.
    /// </summary>
    public static decimal CalculateCost(
        decimal distanceKm,
        decimal durationMinutes,
        decimal costPerKm,
        decimal costPerHour,
        decimal tollsCost = 0m)
    {
        var cleanDist = Math.Max(0m, distanceKm);
        var cleanMin = Math.Max(0m, durationMinutes);
        var cleanCostKm = Math.Max(0m, costPerKm);
        var cleanCostH = Math.Max(0m, costPerHour);
        var cleanTolls = Math.Round(Math.Max(0m, tollsCost), 2, MidpointRounding.AwayFromZero);

        var kmCost = Math.Round(cleanDist * cleanCostKm, 2, MidpointRounding.AwayFromZero);
        var hourCost = Math.Round((cleanMin / 60m) * cleanCostH, 2, MidpointRounding.AwayFromZero);

        return cleanTolls + kmCost + hourCost;
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
        string currency = DefaultCurrency,
        decimal? drivingMinutes = null,
        decimal? serviceMinutes = null,
        decimal? waitMinutes = null,
        string scope = "operational_transport_estimate",
        string costStatus = "estimated")
    {
        var cleanDist = Math.Max(0m, distanceKm);
        var cleanMin = Math.Max(0m, durationMinutes);
        var cleanCostKm = Math.Max(0m, costPerKm);
        var cleanCostH = Math.Max(0m, costPerHour);
        var cleanTolls = Math.Round(Math.Max(0m, tollsCost), 2, MidpointRounding.AwayFromZero);

        var kmCost = Math.Round(cleanDist * cleanCostKm, 2, MidpointRounding.AwayFromZero);
        var hours = Math.Round(cleanMin / 60m, 4);
        var hourCost = Math.Round((cleanMin / 60m) * cleanCostH, 2, MidpointRounding.AwayFromZero);
        var total = cleanTolls + kmCost + hourCost;

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
            CalculatorVersion: Version,
            DrivingMinutes: drivingMinutes,
            ServiceMinutes: serviceMinutes,
            WaitMinutes: waitMinutes,
            Scope: scope,
            CostStatus: costStatus
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
    string CalculatorVersion,
    decimal? DrivingMinutes = null,
    decimal? ServiceMinutes = null,
    decimal? WaitMinutes = null,
    string Scope = "operational_transport_estimate",
    string CostStatus = "estimated"
);
