using System.Globalization;

namespace IstpetDev.Application.Logistics;

// #245: franja de hora pico en hora local ("HH:mm"), configurada en Logistics:TrafficBands.
// SpeedFactor es la fracción de la velocidad normal: 0,5 = el tramo dura el doble.
public sealed class TrafficBandOptions
{
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public decimal SpeedFactor { get; set; } = 1m;
}

public static class TrafficBands
{
    private static readonly string[] Formats = ["HH:mm", "HH:mm:ss", "H:mm"];

    /// <summary>
    /// Factor de velocidad para un tramo que empieza a la hora local indicada: [Start, End).
    /// Sin franjas, o si ninguna aplica, devuelve 1 (la duración no cambia). Una franja mal formada
    /// o con factor fuera de (0, 1] se ignora; si dos franjas se solapan, gana la más lenta.
    /// </summary>
    public static decimal SpeedFactorAt(IReadOnlyList<TrafficBandOptions>? bands, TimeOnly localStart)
    {
        if (bands is null || bands.Count == 0) return 1m;
        var factor = 1m;
        foreach (var band in bands)
        {
            if (band.SpeedFactor <= 0m || band.SpeedFactor > 1m) continue;
            if (!TryParse(band.Start, out var start) || !TryParse(band.End, out var end) || end <= start) continue;
            if (localStart >= start && localStart < end && band.SpeedFactor < factor) factor = band.SpeedFactor;
        }
        return factor;
    }

    // Minutos del tramo con el factor aplicado (factor 1 = sin cambio).
    public static decimal Apply(decimal minutes, decimal speedFactor) =>
        speedFactor == 1m ? minutes : minutes / speedFactor;

    // Lo que se informa por parada: null fuera de hora pico.
    public static decimal? Reported(decimal speedFactor) => speedFactor == 1m ? null : speedFactor;

    private static bool TryParse(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value?.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
}
