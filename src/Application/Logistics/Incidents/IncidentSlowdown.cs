using System.Globalization;
using System.Text.RegularExpressions;

namespace IstpetDev.Application.Logistics.Incidents;

// #246: incidentes que vuelven lenta la vía sin cerrarla. SlowdownFactor es la fracción de la velocidad
// normal (0,3 = la velocidad cae al 30 % y el tramo dura 1 / 0,3 veces más).
public static partial class IncidentSlowdown
{
    public const string Congestion = "Congestion";
    public const string Manifestacion = "Manifestacion";
    public const string Accidente = "Accidente";

    public static bool IsSlowdownKind(string? kind) =>
        string.Equals(kind, Congestion, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(kind, Manifestacion, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(kind, Accidente, StringComparison.OrdinalIgnoreCase);

    public static bool IsValidFactor(decimal factor) => factor > 0m && factor <= 1m;

    // Multiplicador de minutos que usa IncidentEffect.SlowFactors.
    public static decimal MinutesMultiplier(decimal slowdownFactor) => 1m / slowdownFactor;

    /// <summary>
    /// Sin migración (la única de la noche era #240): el factor se guarda al final de Incident.Description
    /// con un texto legible, p. ej. «Marcha … (velocidad al 30 %)». Reemplaza una marca anterior.
    /// </summary>
    public static string WithFactor(string? description, decimal slowdownFactor)
    {
        var text = Marker().Replace(description ?? string.Empty, string.Empty).TrimEnd();
        var pct = (slowdownFactor * 100m).ToString("0.##", CultureInfo.InvariantCulture);
        var marker = $"(velocidad al {pct} %)";
        return text.Length == 0 ? marker : $"{text} {marker}";
    }

    // Factor guardado en la descripción; null si no tiene marca válida.
    public static decimal? ReadFactor(string? description)
    {
        if (string.IsNullOrEmpty(description)) return null;
        var match = Marker().Match(description);
        if (!match.Success ||
            !decimal.TryParse(match.Groups["pct"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var pct))
            return null;
        var factor = pct / 100m;
        return IsValidFactor(factor) ? factor : null;
    }

    [GeneratedRegex(@"\s*\(velocidad al (?<pct>\d+(?:\.\d+)?) %\)\s*$")]
    private static partial Regex Marker();
}
