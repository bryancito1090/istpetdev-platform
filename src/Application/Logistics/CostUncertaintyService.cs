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
    string Message,
    decimal? Wape = null,
    decimal? QuantileLowerOffset = null,
    decimal? QuantileUpperOffset = null,
    decimal? IntervalWidth = null,
    int? HoldoutSampleSize = null,
    decimal? HoldoutEmpiricalCoverage = null
);

public sealed record HistoricalTripCost(
    decimal EstimatedCost,
    decimal ActualReconciledCost,
    DateOnly TripDate,
    string? TripCode = null,
    string? VehicleCode = null,
    bool HadIncidents = false
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
            Message: message,
            Wape: null,
            QuantileLowerOffset: null,
            QuantileUpperOffset: null,
            IntervalWidth: null,
            HoldoutSampleSize: null,
            HoldoutEmpiricalCoverage: null
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
                Message: UncalibratedMessage,
                Wape: null,
                QuantileLowerOffset: null,
                QuantileUpperOffset: null,
                IntervalWidth: null,
                HoldoutSampleSize: null,
                HoldoutEmpiricalCoverage: null
            );
        }

        var n = historicalTrips.Count;
        var residuals = new List<decimal>(n);
        var absResiduals = new List<decimal>(n);
        decimal sumResiduals = 0m;
        decimal sumAbsResiduals = 0m;
        decimal sumActual = 0m;

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
            sumActual += trip.ActualReconciledCost;

            if (trip.TripDate < minDate) minDate = trip.TripDate;
            if (trip.TripDate > maxDate) maxDate = trip.TripDate;
        }

        absResiduals.Sort();
        residuals.Sort();

        // Cuantil conforme ceil((n + 1) * targetCoverage) / n
        int rank = (int)Math.Ceiling((n + 1) * (double)targetCoverage);
        int index = Math.Min(n - 1, Math.Max(0, rank - 1));
        var q = absResiduals[index];

        var meanBias = Math.Round(sumResiduals / n, 2, MidpointRounding.AwayFromZero);
        var mae = Math.Round(sumAbsResiduals / n, 2, MidpointRounding.AwayFromZero);
        decimal? wape = sumActual > 0m ? Math.Round(sumAbsResiduals / sumActual, 4, MidpointRounding.AwayFromZero) : null;

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
        var intervalWidth = upperBound - lowerBound;

        // Cuantiles empíricos de los residuos con signo
        decimal alpha = 1m - targetCoverage;
        decimal pLower = alpha / 2m;
        decimal pUpper = 1m - (alpha / 2m);
        int lowerIndex = Math.Clamp((int)Math.Ceiling((double)(pLower * n)) - 1, 0, n - 1);
        int upperIndex = Math.Clamp((int)Math.Ceiling((double)(pUpper * n)) - 1, 0, n - 1);
        decimal qLower = residuals[lowerIndex];
        decimal qUpper = residuals[upperIndex];
        decimal quantileLowerOffset = Math.Round(qLower, 2, MidpointRounding.AwayFromZero);
        decimal quantileUpperOffset = Math.Round(qUpper, 2, MidpointRounding.AwayFromZero);

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
            Message: $"Intervalo de predicción calibrado al {targetCoverage:P0} con {n} viajes históricos reales.",
            Wape: wape,
            QuantileLowerOffset: quantileLowerOffset,
            QuantileUpperOffset: quantileUpperOffset,
            IntervalWidth: intervalWidth,
            HoldoutSampleSize: null,
            HoldoutEmpiricalCoverage: null
        );
    }

    /// <summary>
    /// Evalúa la incertidumbre aplicando validación temporal:
    /// Deduplica por TripCode, ordena por TripDate, y cuando n >= 14
    /// separa cronológicamente calibración (~70%, mín 10) y holdout posterior.
    /// </summary>
    public static CostUncertaintyResult EvaluateTemporalValidation(
        decimal estimatedCost,
        IReadOnlyList<HistoricalTripCost>? historicalTrips,
        decimal targetCoverage = 0.90m)
    {
        if (historicalTrips == null || historicalTrips.Count == 0)
        {
            return GetUncalibratedResult();
        }

        // Agrupar/deduplicar por TripCode si se provee
        var seenTripCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduplicated = new List<HistoricalTripCost>();
        foreach (var trip in historicalTrips)
        {
            if (!string.IsNullOrWhiteSpace(trip.TripCode))
            {
                if (seenTripCodes.Add(trip.TripCode))
                {
                    deduplicated.Add(trip);
                }
            }
            else
            {
                deduplicated.Add(trip);
            }
        }

        // Ordenar cronológicamente por TripDate
        var sortedTrips = deduplicated.OrderBy(t => t.TripDate).ToList();
        int n = sortedTrips.Count;

        if (n < MinimumSampleSizeForCalibration)
        {
            return new CostUncertaintyResult(
                Status: "uncalibrated",
                LowerBound: null,
                UpperBound: null,
                MarginOfError: null,
                SampleSize: n,
                Period: null,
                Method: MethodName,
                MeanBias: null,
                Mae: null,
                EmpiricalCoverage: null,
                Message: UncalibratedMessage,
                Wape: null,
                QuantileLowerOffset: null,
                QuantileUpperOffset: null,
                IntervalWidth: null,
                HoldoutSampleSize: null,
                HoldoutEmpiricalCoverage: null
            );
        }

        IReadOnlyList<HistoricalTripCost> calibTrips;
        IReadOnlyList<HistoricalTripCost> holdoutTrips;

        if (n >= 14)
        {
            int calibCount = Math.Max(10, (int)Math.Round(n * 0.70m, MidpointRounding.AwayFromZero));
            if (calibCount >= n) calibCount = n - 1;
            calibTrips = sortedTrips.Take(calibCount).ToList();
            holdoutTrips = sortedTrips.Skip(calibCount).ToList();
        }
        else
        {
            calibTrips = sortedTrips;
            holdoutTrips = Array.Empty<HistoricalTripCost>();
        }

        int nCalib = calibTrips.Count;
        var residuals = new List<decimal>(nCalib);
        var absResiduals = new List<decimal>(nCalib);
        decimal sumResiduals = 0m;
        decimal sumAbsResiduals = 0m;
        decimal sumActual = 0m;

        foreach (var trip in calibTrips)
        {
            var e = trip.ActualReconciledCost - trip.EstimatedCost;
            residuals.Add(e);
            var absE = Math.Abs(e);
            absResiduals.Add(absE);
            sumResiduals += e;
            sumAbsResiduals += absE;
            sumActual += trip.ActualReconciledCost;
        }

        absResiduals.Sort();
        residuals.Sort();

        int rank = (int)Math.Ceiling((nCalib + 1) * (double)targetCoverage);
        int index = Math.Min(nCalib - 1, Math.Max(0, rank - 1));
        var q = absResiduals[index];

        var meanBias = Math.Round(sumResiduals / nCalib, 2, MidpointRounding.AwayFromZero);
        var mae = Math.Round(sumAbsResiduals / nCalib, 2, MidpointRounding.AwayFromZero);
        decimal? wape = sumActual > 0m ? Math.Round(sumAbsResiduals / sumActual, 4, MidpointRounding.AwayFromZero) : null;

        int calibCovered = absResiduals.Count(absE => absE <= q);
        var empiricalCoverage = Math.Round((decimal)calibCovered / nCalib, 4);

        var lowerBound = Math.Max(0m, Math.Round(estimatedCost - q, 2, MidpointRounding.AwayFromZero));
        var upperBound = Math.Round(estimatedCost + q, 2, MidpointRounding.AwayFromZero);
        var margin = Math.Round(q, 2, MidpointRounding.AwayFromZero);
        var intervalWidth = upperBound - lowerBound;

        decimal alpha = 1m - targetCoverage;
        decimal pLower = alpha / 2m;
        decimal pUpper = 1m - (alpha / 2m);
        int lowerIndex = Math.Clamp((int)Math.Ceiling((double)(pLower * nCalib)) - 1, 0, nCalib - 1);
        int upperIndex = Math.Clamp((int)Math.Ceiling((double)(pUpper * nCalib)) - 1, 0, nCalib - 1);
        decimal qLower = residuals[lowerIndex];
        decimal qUpper = residuals[upperIndex];
        decimal quantileLowerOffset = Math.Round(qLower, 2, MidpointRounding.AwayFromZero);
        decimal quantileUpperOffset = Math.Round(qUpper, 2, MidpointRounding.AwayFromZero);

        int? holdoutSampleSize = null;
        decimal? holdoutEmpiricalCoverage = null;

        if (n >= 14)
        {
            holdoutSampleSize = holdoutTrips.Count;
            int holdoutCovered = holdoutTrips.Count(t => Math.Abs(t.ActualReconciledCost - t.EstimatedCost) <= q);
            holdoutEmpiricalCoverage = holdoutTrips.Count > 0 ? Math.Round((decimal)holdoutCovered / holdoutTrips.Count, 4) : null;
        }
        else
        {
            holdoutSampleSize = 0;
            holdoutEmpiricalCoverage = null;
        }

        var minDate = sortedTrips[0].TripDate;
        var maxDate = sortedTrips[^1].TripDate;
        var period = $"{minDate:yyyy-MM-dd} a {maxDate:yyyy-MM-dd}";

        string message = n >= 14
            ? $"Intervalo de predicción calibrado al {targetCoverage:P0} con {nCalib} viajes en calibración y {holdoutTrips.Count} en holdout."
            : $"Intervalo de predicción calibrado al {targetCoverage:P0} con {n} viajes históricos reales.";

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
            Message: message,
            Wape: wape,
            QuantileLowerOffset: quantileLowerOffset,
            QuantileUpperOffset: quantileUpperOffset,
            IntervalWidth: intervalWidth,
            HoldoutSampleSize: holdoutSampleSize,
            HoldoutEmpiricalCoverage: holdoutEmpiricalCoverage
        );
    }
}
