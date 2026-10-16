using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record VehicleMatchResult(
    Vehicle Vehicle,
    bool IsMatch,
    string? DiscardReason, // "excede_peso", "excede_volumen", "jornada_excedida", "ocupado_en_otra_ruta", "ventana_incompatible"
    decimal WeightUtilizationPct,
    decimal VolumeUtilizationPct,
    decimal EstimatedTotalCost,
    decimal DistanceToFirstPickupKm,
    ServiceCostBreakdown? ServiceCostBreakdown = null
);

public sealed record AvailableVehicleDto(
    Guid VehicleId,
    string Code,
    decimal MaxWeight,
    decimal? MaxVolume,
    Guid DepotLocationId,
    string DepotCode,
    string DepotName,
    decimal CostPerKm,
    string CostUnit,
    decimal? EstimatedTotalCost
);

public sealed record AvailableVehiclesResult(
    DateOnly OperationalDate,
    DateOnly Date,
    string TimeZone,
    DateTime DayStartUtc,
    DateTime DayEndUtc,
    IReadOnlyList<AvailableVehicleDto> Vehicles
);

public interface IVehicleMatcher
{
    Task<IReadOnlyList<VehicleMatchResult>> MatchVehiclesAsync(
        IReadOnlyList<RouteStopCandidate> stops,
        DateTime plannedStartUtc,
        CancellationToken ct = default);

    Task<AvailableVehiclesResult> GetAvailableVehiclesAsync(
        DateOnly? date = null,
        CancellationToken ct = default);
}

public sealed record RouteStopCandidate(
    int Sequence,
    string Kind, // "Pickup" o "Delivery"
    Guid? ServicePointId,
    Guid? LocationId,
    string Code,
    string Name,
    GeoPoint Position,
    decimal DeltaWeight, // Positivo en Pickup, Negativo en Delivery
    decimal DeltaVolume,
    TimeOnly WindowStart,
    TimeOnly WindowEnd,
    int ServiceMinutes,
    IReadOnlyList<StopLoadDto>? Loads = null
);

public sealed record StopLoadDto(
    Guid ProductId,
    string Sku,
    decimal Quantity,
    string BaseUnit
);

public sealed class VehicleMatcher : IVehicleMatcher
{
    private readonly IApplicationDbContext _db;
    private readonly IDistanceProvider _distanceProvider;
    private readonly LogisticsOptions _options;
    private readonly IOperationalClock _clock;

    public VehicleMatcher(
        IApplicationDbContext db,
        IDistanceProvider distanceProvider,
        IOptions<LogisticsOptions> options,
        IOperationalClock? clock = null)
    {
        _db = db;
        _distanceProvider = distanceProvider;
        _options = options.Value;
        _clock = clock ?? new OperationalClock(db, options);
    }

    public async Task<AvailableVehiclesResult> GetAvailableVehiclesAsync(
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var operationalDate = await _clock.TodayAsync(ct);
        var targetDate = date ?? operationalDate;
        var snapshot = await LoadAvailabilitySnapshotAsync(targetDate, ct);

        var available = snapshot.ActiveVehicles
            .Where(v => !snapshot.BusyVehicleIds.Contains(v.Id))
            .OrderBy(v => v.Code, StringComparer.Ordinal)
            .ThenBy(v => v.Id)
            .Select(v =>
            {
                snapshot.DepotLocations.TryGetValue(v.DepotLocationId, out var depot);
                decimal costPerKm = v.CostPerKm ?? _options.DefaultCostPerKm;
                return new AvailableVehicleDto(
                    v.Id,
                    v.Code,
                    v.MaxWeight,
                    v.MaxVolume,
                    v.DepotLocationId,
                    depot?.Code ?? string.Empty,
                    depot?.Name ?? string.Empty,
                    costPerKm,
                    "USD/km",
                    EstimatedTotalCost: null
                );
            })
            .ToList();

        return new AvailableVehiclesResult(
            operationalDate,
            targetDate,
            snapshot.TimeZoneId,
            snapshot.DayStartUtc,
            snapshot.DayEndUtc,
            available
        );
    }

    public async Task<IReadOnlyList<VehicleMatchResult>> MatchVehiclesAsync(
        IReadOnlyList<RouteStopCandidate> stops,
        DateTime plannedStartUtc,
        CancellationToken ct = default)
    {
        var targetDate = await ResolveOperationalDateAsync(plannedStartUtc, ct);
        var snapshot = await LoadAvailabilitySnapshotAsync(targetDate, ct);

        var serviceCost = await ServiceCostCalculator.LoadAsync(_options, stops, _db, ct);
        var routeHelperCount = serviceCost.HelperCount(stops);

        // Evaluar perfil de carga tramo a tramo (Leg-by-Leg)
        decimal maxWeightOnTrip = 0m;
        decimal maxVolumeOnTrip = 0m;
        decimal currentWeight = 0m;
        decimal currentVolume = 0m;

        foreach (var stop in stops)
        {
            currentWeight += stop.DeltaWeight;
            currentVolume += stop.DeltaVolume;

            if (currentWeight > maxWeightOnTrip) maxWeightOnTrip = currentWeight;
            if (currentVolume > maxVolumeOnTrip) maxVolumeOnTrip = currentVolume;
        }

        // Precalcular distancia y duración entre paradas del recorrido
        decimal interStopsKm = 0m;
        decimal interStopsDrivingMinutes = 0m;

        for (int i = 0; i < stops.Count; i++)
        {
            if (i > 0)
            {
                var prev = stops[i - 1].Position;
                var curr = stops[i].Position;
                var legKm = DistanceProvider.HaversineDistanceKm(prev, curr) * _options.ApproxDetourFactor;
                var legMin = Math.Round((legKm / _options.ApproxSpeedKmh) * 60m, 1);
                interStopsKm += legKm;
                interStopsDrivingMinutes += legMin;
            }
        }

        var results = new List<VehicleMatchResult>();
        var firstStop = stops.FirstOrDefault();
        var lastStop = stops.Count > 0 ? stops[^1] : null;

        foreach (var v in snapshot.ActiveVehicles)
        {
            bool isMatch = true;
            string? discardReason = null;

            // Capacidad de peso
            if (v.MaxWeight > 0 && maxWeightOnTrip > v.MaxWeight)
            {
                isMatch = false;
                discardReason = $"excede_peso (Requiere {maxWeightOnTrip:F0} kg, capacidad {v.MaxWeight:F0} kg)";
            }

            // Capacidad de volumen
            if (isMatch && v.MaxVolume.HasValue && v.MaxVolume.Value > 0 && maxVolumeOnTrip > v.MaxVolume.Value)
            {
                isMatch = false;
                discardReason = $"excede_volumen (Requiere {maxVolumeOnTrip:F1} m³, capacidad {v.MaxVolume.Value:F1} m³)";
            }

            // Conflicto de disponibilidad (otra ruta publicada en el mismo día)
            if (isMatch && snapshot.BusyVehicleIds.Contains(v.Id))
            {
                isMatch = false;
                discardReason = "ocupado_en_otra_ruta (Tiene ruta publicada en la misma fecha)";
            }

            // Distancia del depósito base a la primera recogida y regreso al depósito desde la última parada
            decimal distToPickup = 0m;
            decimal timeToPickup = 0m;
            decimal distReturn = 0m;
            decimal timeReturn = 0m;

            if (firstStop is not null &&
                snapshot.DepotLocations.TryGetValue(v.DepotLocationId, out var depotLoc) &&
                depotLoc.Position != null)
            {
                var depotPos = new GeoPoint((decimal)depotLoc.Position.Y, (decimal)depotLoc.Position.X);
                distToPickup = DistanceProvider.HaversineDistanceKm(depotPos, firstStop.Position) * _options.ApproxDetourFactor;
                timeToPickup = Math.Round((distToPickup / _options.ApproxSpeedKmh) * 60m, 1);

                if (lastStop is not null)
                {
                    distReturn = DistanceProvider.HaversineDistanceKm(lastStop.Position, depotPos) * _options.ApproxDetourFactor;
                    timeReturn = Math.Round((distReturn / _options.ApproxSpeedKmh) * 60m, 1);
                }
            }

            decimal totalTripKm = Math.Round(distToPickup + interStopsKm + distReturn, 2);
            decimal totalTripMinutes = Math.Round(timeToPickup + interStopsDrivingMinutes + stops.Sum(s => serviceCost.ServiceMinutes(s, v, routeHelperCount)) + timeReturn, 1);

            // Validar jornada laboral máxima si el vehículo fuese asignado
            if (isMatch && _options.WorkdayHours > 0 && totalTripMinutes > _options.WorkdayHours * 60m)
            {
                isMatch = false;
                discardReason = $"jornada_excedida (Duración {totalTripMinutes:F0} min excede jornada {_options.WorkdayHours * 60:F0} min)";
            }

            decimal weightUtil = v.MaxWeight > 0 ? Math.Round((maxWeightOnTrip / v.MaxWeight) * 100m, 1) : 0m;
            decimal volUtil = v.MaxVolume.HasValue && v.MaxVolume.Value > 0 ? Math.Round((maxVolumeOnTrip / v.MaxVolume.Value) * 100m, 1) : 0m;
            var breakdown = serviceCost.Calculate(totalTripKm, totalTripMinutes, v, maxWeightOnTrip, stops);
            decimal fullEstimatedCost = firstStop is not null ? breakdown.Total : 0m;

            results.Add(new VehicleMatchResult(
                v,
                isMatch,
                discardReason,
                weightUtil,
                volUtil,
                fullEstimatedCost,
                Math.Round(distToPickup, 1),
                breakdown
            ));
        }

        // Ordenar: Primero los factibles (ordenados por costo total completo, luego menor tamaño adecuado), luego los descartados
        return results
            .OrderByDescending(r => r.IsMatch ? 1 : 0)
            .ThenBy(r => r.EstimatedTotalCost)
            .ThenBy(r => r.Vehicle.MaxWeight) // Si el costo es igual, priorizar vehículo de menor capacidad adecuada (normal antes que pesado)
            .ThenBy(r => r.DistanceToFirstPickupKm)
            .ThenBy(r => r.Vehicle.Code, StringComparer.Ordinal)
            .ThenBy(r => r.Vehicle.Id)
            .ToList();
    }

    private async Task<AvailabilitySnapshot> LoadAvailabilitySnapshotAsync(DateOnly date, CancellationToken ct)
    {
        var (timeZoneId, tz) = await ResolveTimeZoneAsync(ct);
        var (dayStartUtc, dayEndUtc) = ComputeUtcWindow(date, tz, _options.FallbackUtcOffsetHours);

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive)
            .OrderBy(v => v.Code)
            .ThenBy(v => v.Id)
            .ToListAsync(ct);

        var depotLocations = await _db.Locations
            .AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, ct);

        var busyVehicleIds = (await _db.RoutePlans
            .AsNoTracking()
            .Where(p => p.OrganizationId == DemoOrganization.Id &&
                        p.Status == "Published" &&
                        p.PlannedStartUtc >= dayStartUtc &&
            p.PlannedStartUtc < dayEndUtc)
            .Select(p => p.VehicleId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();

        return new AvailabilitySnapshot(timeZoneId, dayStartUtc, dayEndUtc, vehicles, depotLocations, busyVehicleIds);
    }

    private async Task<DateOnly> ResolveOperationalDateAsync(DateTime plannedStartUtc, CancellationToken ct)
    {
        var (_, tz) = await ResolveTimeZoneAsync(ct);
        var utc = plannedStartUtc.Kind == DateTimeKind.Utc
            ? plannedStartUtc
            : DateTime.SpecifyKind(plannedStartUtc, DateTimeKind.Utc);

        var localDateTime = tz is not null
            ? TimeZoneInfo.ConvertTimeFromUtc(utc, tz)
            : utc + TimeSpan.FromHours(_options.FallbackUtcOffsetHours);

        return DateOnly.FromDateTime(localDateTime);
    }

    private async Task<(string TimeZoneId, TimeZoneInfo? TimeZone)> ResolveTimeZoneAsync(CancellationToken ct)
    {
        var timeZoneId = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.Id == DemoOrganization.Id)
            .Select(o => o.TimeZone)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            timeZoneId = "America/Guayaquil";
        }

        try
        {
            return (timeZoneId, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }
        catch (TimeZoneNotFoundException)
        {
            return (timeZoneId, null);
        }
    }

    private static (DateTime DayStartUtc, DateTime DayEndUtc) ComputeUtcWindow(
        DateOnly date,
        TimeZoneInfo? tz,
        int fallbackUtcOffsetHours)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        if (tz is not null)
        {
            return (
                TimeZoneInfo.ConvertTimeToUtc(localStart, tz),
                TimeZoneInfo.ConvertTimeToUtc(localEnd, tz)
            );
        }

        var offset = TimeSpan.FromHours(fallbackUtcOffsetHours);
        return (
            DateTime.SpecifyKind(localStart - offset, DateTimeKind.Utc),
            DateTime.SpecifyKind(localEnd - offset, DateTimeKind.Utc)
        );
    }

    private sealed record AvailabilitySnapshot(
        string TimeZoneId,
        DateTime DayStartUtc,
        DateTime DayEndUtc,
        IReadOnlyList<Vehicle> ActiveVehicles,
        IReadOnlyDictionary<Guid, Location> DepotLocations,
        IReadOnlySet<Guid> BusyVehicleIds
    );
}
