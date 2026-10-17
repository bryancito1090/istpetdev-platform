using System.Globalization;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Logistics;

// Franjas sintéticas, en hora local. Zone compara ServicePoint.Region; no interpreta calles ni ordenanzas.
public sealed class VehicleRestrictionOptions
{
    public string? VehicleType { get; set; }
    public string? UsageScope { get; set; }
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public string? Zone { get; set; }
}

public static class VehicleRestrictions
{
    public static bool Configured(LogisticsOptions options) => options.VehicleRestrictions.Count > 0 || options.UrbanRegions.Count > 0;

    public static async Task<string[]> RegionsAsync(IApplicationDbContext? db, IReadOnlyList<RouteStopCandidate> stops, CancellationToken ct)
    {
        if (db is null) return [];
        var ids = stops.Where(s => s.ServicePointId.HasValue).Select(s => s.ServicePointId!.Value).Distinct().ToArray();
        return await db.ServicePoints.AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id && ids.Contains(p.Id) && p.Region != null)
            .Select(p => p.Region!).Distinct().ToArrayAsync(ct);
    }

    public static DateTime LocalTime(DateTime utc, string? timeZone, int fallbackOffset)
    {
        utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        try { return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(timeZone ?? "America/Guayaquil")); }
        catch (TimeZoneNotFoundException) { return utc.AddHours(fallbackOffset); }
        catch (InvalidTimeZoneException) { return utc.AddHours(fallbackOffset); }
    }

    public static string? DiscardReason(LogisticsOptions options, Vehicle vehicle, DateTime localStart, DateTime localEnd, IReadOnlyList<string> regions)
    {
        foreach (var rule in options.VehicleRestrictions)
        {
            if (string.IsNullOrWhiteSpace(rule.VehicleType) && string.IsNullOrWhiteSpace(rule.UsageScope)) continue;
            if (!string.IsNullOrWhiteSpace(rule.VehicleType) && !Same(rule.VehicleType, vehicle.VehicleType)) continue;
            if (!string.IsNullOrWhiteSpace(rule.UsageScope) && !Same(rule.UsageScope, vehicle.UsageScope)) continue;
            if (!string.IsNullOrWhiteSpace(rule.Zone) && !regions.Any(r => Same(r, rule.Zone))) continue;
            if (!TimeOnly.TryParseExact(rule.Start, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !TimeOnly.TryParseExact(rule.End, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end) || start == end) continue;
            // Intervalos [Start, End); una franja nocturna termina al día siguiente.
            for (var day = localStart.Date.AddDays(-1); day <= localEnd.Date; day = day.AddDays(1))
            {
                var bandStart = day.Add(start.ToTimeSpan());
                var bandEnd = day.Add(end.ToTimeSpan()).AddDays(end < start ? 1 : 0);
                if (localStart < bandEnd && (localEnd > bandStart || localEnd == localStart && localStart >= bandStart))
                    return $"restriccion_horaria ({vehicle.VehicleType ?? vehicle.UsageScope} no circula {start:HH:mm}–{end:HH:mm}{(string.IsNullOrWhiteSpace(rule.Zone) ? "" : $" en {rule.Zone}")}; regla sintética)";
            }
        }
        return null;
    }

    public static IReadOnlyList<string> Warnings(LogisticsOptions options, Vehicle vehicle, IReadOnlyList<string> regions)
    {
        if (!Same(vehicle.UsageScope, "urbano") || options.UrbanRegions.Count == 0) return [];
        var outside = regions.Where(r => !string.IsNullOrWhiteSpace(r) && !options.UrbanRegions.Any(urban => Same(r, urban))).Order(StringComparer.Ordinal).ToArray();
        return outside.Length == 0 ? [] : [$"uso_extraurbano (Vehículo urbano visita {string.Join(", ", outside)} fuera del área urbana configurada; aviso, no descarte)"];
    }

    private static bool Same(string? left, string? right) => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
}
