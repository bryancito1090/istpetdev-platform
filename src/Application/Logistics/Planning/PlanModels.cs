namespace IstpetDev.Application.Logistics.Planning;

public sealed record PlanRequest(
    DateOnly? Date,
    IReadOnlyList<string>? VehicleCodes,
    bool IncludeConsolidation = true);

public sealed record PlannedStop(
    int Sequence,
    string PointCode,
    Guid ServicePointId,
    DateTime ArrivalUtc,
    DateTime DepartureUtc,
    TimeSpan Wait,
    IReadOnlyList<StopLoad> Loads,
    bool IsConsolidation,
    int? LateMinutes);

public sealed record StopLoad(
    Guid ProductId,
    string Sku,
    decimal Quantity,
    string BaseUnit);

public sealed record VehicleRoute(
    string VehicleCode,
    Guid VehicleId,
    IReadOnlyList<PlannedStop> Stops,
    decimal Km,
    decimal Minutes,
    decimal Cost,
    decimal LoadWeight,
    decimal LoadVolume,
    bool HasApproximations,
    IReadOnlyList<Leg> Legs);

public sealed record Unserved(
    string PointCode,
    string Reason,
    IReadOnlyList<Fact> Facts);

public sealed record PlanResult(
    IReadOnlyList<VehicleRoute> Routes,
    IReadOnlyList<Unserved> Unserved,
    IReadOnlyList<Fact> Facts);

public enum ReplanMode { Strict, LateService, PrioritizeCritical }

public sealed record FixedPrefix(
    string VehicleCode,
    IReadOnlyList<string> CompletedPointCodes,
    GeoPoint Position,
    DateTime ReadyAtUtc,
    IReadOnlyList<string> PendingPointCodes,
    bool VehicleAvailable);

public sealed record IncidentEffect(
    IReadOnlySet<(GeoPoint, GeoPoint)> BlockedLegs,
    IReadOnlyDictionary<(GeoPoint, GeoPoint), decimal> SlowFactors,
    IReadOnlySet<string> UnavailableVehicles);

public sealed record ReplanInput(
    IReadOnlyList<FixedPrefix> Prefixes,
    IncidentEffect Effect,
    ReplanMode Mode);

public interface IRoutePlanner
{
    Task<PlanResult> PlanAsync(PlanRequest request, CancellationToken ct = default);
    Task<PlanResult> ReplanAsync(ReplanInput input, CancellationToken ct = default);
}
