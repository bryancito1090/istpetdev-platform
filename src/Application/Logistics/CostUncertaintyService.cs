namespace IstpetDev.Application.Logistics;

public sealed record CostUncertaintyResult(
    string Status, // "uncalibrated" o "calibrated"
    decimal? LowerBound,
    decimal? UpperBound,
    decimal? MarginOfError,
    int SampleSize,
    string? Period,
    string Method,
    decimal? MeanBias,
    decimal? Mae,
    decimal? EmpiricalCoverage,
    string Message
);

public sealed record HistoricalTripCost(
    decimal EstimatedCost,
    decimal ActualReconciledCost,
    DateOnly TripDate
);

/// <summary>
/// Modelo e inferencia de incertidumbre de costo verificado.
/// Cumple estrictamente con no inventar porcentajes ni intervalos cuando no existen
/// suficientes viajes reales conciliados (estado 'uncalibrated').
/// </summary>
public static class CostUncertaintyService
{
    public const string MethodName = "conformal_residuals_quantile";
    public const int MinimumSampleSizeForCalibration = 10;
    public const string UncalibratedMessage = "Margen de error aún no cuantificable con los datos disponibles";

    /// <summary>
    /// Retorna el estado canónico no calibrado (sin viajes reales o muestra insuficiente).
    /// </summary>
    public static CostUncertaintyResult GetUncalibratedResult(string message = UncalibratedMessage)
    {
        return new CostUncertaintyResult(
            Status: "uncalibrated",
            LowerBound: null,
            UpperBound: null,
            MarginOfError: null,
            SampleSize: 0,
            Period: null,
            Method: MethodName,
            MeanBias: null,
            Mae: null,
            EmpiricalCoverage: null,
            Message: message
        );
    }

    /// <summary>
    /// Evalúa la incertidumbre para un costo estimado en base a un histórico real comparable.
    /// Si la muestra es insuficiente, devuelve 'uncalibrated' con límites estadísticos nulos.
    /// Si la muestra es suficiente, computa residuos exactos, MAE y cuantiles de calibración conforme.
    /// </summary>
    public static CostUncertaintyResult Evaluate(
        decimal estimatedCost,
        IReadOnlyList<HistoricalTripCost>? historicalTrips,
        decimal targetCoverage = 0.90m)
    {
        if (historicalTrips == null || historicalTrips.Count < MinimumSampleSizeForCalibration)
        {
            var count = historicalTrips?.Count ?? 0;
            return new CostUncertaintyResult(
                Status: "uncalibrated",
                LowerBound: null,
                UpperBound: null,
                MarginOfError: null,
                SampleSize: count,
                Period: null,
                Method: MethodName,
                MeanBias: null,
                Mae: null,
                EmpiricalCoverage: null,
                Message: UncalibratedMessage
            );
        }

        var n = historicalTrips.Count;
        var residuals = new List<decimal>(n);
        var absResiduals = new List<decimal>(n);
        decimal sumResiduals = 0m;
        decimal sumAbsResiduals = 0m;

        DateOnly minDate = DateOnly.MaxValue;
        DateOnly maxDate = DateOnly.MinValue;

        foreach (var trip in historicalTrips)
        {
            var e = trip.ActualReconciledCost - trip.EstimatedCost;
            residuals.Add(e);
            var absE = Math.Abs(e);
            absResiduals.Add(absE);
            sumResiduals += e;
            sumAbsResiduals += absE;

            if (trip.TripDate < minDate) minDate = trip.TripDate;
            if (trip.TripDate > maxDate) maxDate = trip.TripDate;
        }

        absResiduals.Sort();

        // Cuantil conforme ceil((n + 1) * targetCoverage) / n
        int rank = (int)Math.Ceiling((n + 1) * (double)targetCoverage);
        int index = Math.Min(n - 1, Math.Max(0, rank - 1));
        var q = absResiduals[index];

        var meanBias = Math.Round(sumResiduals / n, 2, MidpointRounding.AwayFromZero);
        var mae = Math.Round(sumAbsResiduals / n, 2, MidpointRounding.AwayFromZero);

        // Cobertura empírica observada en la muestra
        int coveredCount = 0;
        for (int i = 0; i < n; i++)
        {
            if (absResiduals[i] <= q) coveredCount++;
        }
        var empiricalCoverage = Math.Round((decimal)coveredCount / n, 4);

        var lowerBound = Math.Max(0m, Math.Round(estimatedCost - q, 2, MidpointRounding.AwayFromZero));
        var upperBound = Math.Round(estimatedCost + q, 2, MidpointRounding.AwayFromZero);
        var margin = Math.Round(q, 2, MidpointRounding.AwayFromZero);

        var period = $"{minDate:yyyy-MM-dd} a {maxDate:yyyy-MM-dd}";

        return new CostUncertaintyResult(
            Status: "calibrated",
            LowerBound: lowerBound,
            UpperBound: upperBound,
            MarginOfError: margin,
            SampleSize: n,
            Period: period,
            Method: MethodName,
            MeanBias: meanBias,
            Mae: mae,
            EmpiricalCoverage: empiricalCoverage,
            Message: $"Intervalo de predicción calibrado al {targetCoverage:P0} con {n} viajes históricos reales."
        );
    }
}
