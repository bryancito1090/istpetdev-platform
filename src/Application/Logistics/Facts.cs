namespace IstpetDev.Application.Logistics;

public sealed record Fact(string Code, string Subject, decimal Value, string Unit);

public static class Facts
{
    public static Fact CoverageDays(string pointCode, string sku, decimal coverageDays) =>
        new("coverage_days", $"{pointCode}/{sku}", coverageDays, "días");

    public static Fact RouteKm(string vehicleCode, decimal km) =>
        new("route_km", vehicleCode, km, "km");

    public static Fact RouteCost(string vehicleCode, decimal cost) =>
        new("route_cost", vehicleCode, cost, "USD");

    public static Fact RouteMinutes(string vehicleCode, decimal minutes) =>
        new("route_minutes", vehicleCode, minutes, "min");

    public static Fact StopArrival(string pointCode, decimal minutesFromStart) =>
        new("stop_arrival", pointCode, minutesFromStart, "min");

    public static Fact StopWindow(string pointCode, decimal endMinutesFromStart) =>
        new("stop_window", pointCode, endMinutesFromStart, "min");

    public static Fact ConsolidationDetour(string pointCode, decimal detourMinutes) =>
        new("consolidation_detour", pointCode, detourMinutes, "min");

    public static Fact CostIncreasePct(string vehicleCode, decimal pct) =>
        new("cost_increase_pct", vehicleCode, pct, "%");

    public static Fact MaxDelayMinutes(string pointCode, decimal minutes) =>
        new("max_delay_minutes", pointCode, minutes, "min");

    public static Fact Generic(string code, string subject, decimal value, string unit) =>
        new(code, subject, value, unit);

    public static IReadOnlyList<Fact> Sort(IEnumerable<Fact> facts) =>
        facts.OrderBy(f => f.Code, StringComparer.Ordinal)
             .ThenBy(f => f.Subject, StringComparer.Ordinal)
             .ThenBy(f => f.Unit, StringComparer.Ordinal)
             .ToList();
}
