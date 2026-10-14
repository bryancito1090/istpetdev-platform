namespace IstpetDev.Application.Simulation;

public sealed class FixedCalendarPolicy
{
    public const string Version = "fixed7-v1";

    public static Dictionary<string, DayOfWeek> AssignWeeklyDays(IReadOnlyList<string> orderedPointCodes)
    {
        var result = new Dictionary<string, DayOfWeek>();
        var sorted = orderedPointCodes.OrderBy(c => c).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var weekdayIndex = i % 5; // 0..4 -> Lunes a Viernes
            var dayOfWeek = (DayOfWeek)(weekdayIndex + 1); // 1 = Monday, 5 = Friday
            result[sorted[i]] = dayOfWeek;
        }

        return result;
    }

    public decimal CalculateFixedDemand(
        string pointCode,
        string sku,
        decimal currentStock,
        decimal historicalMeanDaily,
        bool isSpike = false)
    {
        // Fijo: 7 * media diaria histórica previa al horizonte
        // No cambia por pico ni por saldo actual
        return Math.Round(7m * historicalMeanDaily, 3, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<string> SequenceStops(string depotCode, IReadOnlyList<string> stops)
    {
        return CommonRouteSequencer.Sequence(depotCode, stops);
    }
}

public static class IstpetDevPolicyHelper
{
    public static IReadOnlyList<string> SequenceStops(string depotCode, IReadOnlyList<string> stops)
    {
        return CommonRouteSequencer.Sequence(depotCode, stops);
    }
}

public static class CommonRouteSequencer
{
    public static IReadOnlyList<string> Sequence(string depotCode, IReadOnlyList<string> stops)
    {
        return stops.OrderBy(s => s).ToList();
    }
}
