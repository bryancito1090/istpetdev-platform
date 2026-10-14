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
    decimal DistanceToFirstPickupKm
);

public interface IVehicleMatcher
{
    Task<IReadOnlyList<VehicleMatchResult>> MatchVehiclesAsync(
        IReadOnlyList<RouteStopCandidate> stops,
        DateTime plannedStartUtc,
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
    int ServiceMinutes
);

public sealed class VehicleMatcher : IVehicleMatcher
{
    private readonly IApplicationDbContext _db;
    private readonly IDistanceProvider _distanceProvider;
    private readonly LogisticsOptions _options;

    public VehicleMatcher(
        IApplicationDbContext db,
        IDistanceProvider distanceProvider,
        IOptions<LogisticsOptions> options)
    {
        _db = db;
        _distanceProvider = distanceProvider;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<VehicleMatchResult>> MatchVehiclesAsync(
        IReadOnlyList<RouteStopCandidate> stops,
        DateTime plannedStartUtc,
        CancellationToken ct = default)
    {
        // 1. Obtener vehículos activos con sus depósitos
        var vehicles = await _db.Vehicles
            .Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive)
            .ToListAsync(ct);

        var depotLocations = await _db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, ct);

        // 2. Planes publicados en el mismo día para detectar conflictos de horario
        var startDay = plannedStartUtc.Date;
        var endDay = startDay.AddDays(1);
        var busyVehicleIds = await _db.RoutePlans
            .Where(p => p.OrganizationId == DemoOrganization.Id &&
                        p.Status == "Published" &&
                        p.PlannedStartUtc >= startDay &&
                        p.PlannedStartUtc < endDay)
            .Select(p => p.VehicleId)
            .Distinct()
            .ToListAsync(ct);

        // 3. Evaluar perfil de carga tramo a tramo (Leg-by-Leg)
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

        var results = new List<VehicleMatchResult>();
        var firstStop = stops.FirstOrDefault();
        var firstPos = firstStop?.Position ?? new GeoPoint(-0.2295m, -78.5243m);

        foreach (var v in vehicles)
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

            // Conflicto de disponibilidad
            if (isMatch && busyVehicleIds.Contains(v.Id))
            {
                isMatch = false;
                discardReason = "ocupado_en_otra_ruta (Tiene ruta publicada en la misma fecha)";
            }

            // Distancia del depósito base al primer punto
            decimal distToPickup = 0m;
            if (depotLocations.TryGetValue(v.DepotLocationId, out var depotLoc) && depotLoc.Position != null)
            {
                var depotPos = new GeoPoint((decimal)depotLoc.Position.Y, (decimal)depotLoc.Position.X);
                distToPickup = DistanceProvider.HaversineDistanceKm(depotPos, firstPos);
            }

            decimal weightUtil = v.MaxWeight > 0 ? Math.Round((maxWeightOnTrip / v.MaxWeight) * 100m, 1) : 0m;
            decimal volUtil = v.MaxVolume.HasValue && v.MaxVolume.Value > 0 ? Math.Round((maxVolumeOnTrip / v.MaxVolume.Value) * 100m, 1) : 0m;
            decimal costPerKm = v.CostPerKm ?? _options.DefaultCostPerKm;
            decimal estCost = Math.Round(distToPickup * costPerKm, 2);

            results.Add(new VehicleMatchResult(
                v,
                isMatch,
                discardReason,
                weightUtil,
                volUtil,
                estCost,
                Math.Round(distToPickup, 1)
            ));
        }

        // Ordenar: Primero los factibles (ordenados por costo estimado y proximidad), luego los descartados
        return results
            .OrderByDescending(r => r.IsMatch ? 1 : 0)
            .ThenBy(r => r.EstimatedTotalCost)
            .ThenBy(r => r.DistanceToFirstPickupKm)
            .ToList();
    }
}
