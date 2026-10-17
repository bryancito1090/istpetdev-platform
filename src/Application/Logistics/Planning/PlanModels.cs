namespace IstpetDev.Application.Logistics.Planning;

public sealed record PlanRequest(
    DateOnly? Date,
    IReadOnlyList<string>? VehicleCodes,
    bool IncludeConsolidation = true,
    // Hora local "HH:mm" antes de la cual deben atenderse los puntos críticos (#193). Null = sin límite.
    string? CriticalDeadlineLocal = null);

public sealed record PlannedStop(
    int Sequence,
    string PointCode,
    Guid ServicePointId,
    DateTime ArrivalUtc,
    DateTime DepartureUtc,
    TimeSpan Wait,
    IReadOnlyList<StopLoad> Loads,
    bool IsConsolidation,
    int? LateMinutes)
{
    // #245: factor de hora pico aplicado al tramo que llega a esta parada; null fuera de hora pico.
    public decimal? TrafficFactorApplied { get; init; }
}

public sealed record StopLoad(
    Guid ProductId,
    string Sku,
    decimal Quantity,
    string BaseUnit)
{
    // #199: Quantity es lo planificado (lo que se publica y reserva); RequiredQuantity es lo sugerido o pedido
    // (null en cargas anteriores: igual a Quantity) y PendingQuantity lo que de esa línea no quedó en ninguna ruta.
    public decimal? RequiredQuantity { get; init; }
    public decimal PendingQuantity { get; init; }
}

// #199: línea con remanente pendiente (atención parcial o sin atender).
public sealed record PendingLine(
    Guid ProductId,
    string Sku,
    string BaseUnit,
    decimal RequiredQuantity,
    decimal PlannedQuantity,
    decimal PendingQuantity);

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
    IReadOnlyList<Leg> Legs)
{
    // #300: llegada y salida de cada recogida pendiente del prefijo (FixedPrefix.LeadingStops), en orden.
    // Solo interno: el replanificador lo usa para las paradas de la revisión; no se expone en la API.
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<(DateTime ArrivalUtc, DateTime DepartureUtc)> LeadingTimes { get; init; } = [];
}

public sealed record FeasibilityCheck(
    bool IsFeasible,
    string? FailureReason, // "capacity", "time_window", "workday", "unreachable", "blocked_leg"
    DateTime ArrivalUtc,
    DateTime DepartureUtc,
    TimeSpan WaitTime,
    int? LateMinutes,
    decimal CumulativeKm,
    decimal CumulativeMinutes,
    decimal CumulativeCost,
    decimal TotalWeight,
    decimal TotalVolume,
    IReadOnlyList<Leg> Legs,
    bool HasApproximations,
    decimal WaitMinutes = 0m,
    decimal DrivingMinutes = 0m,
    decimal ServiceMinutes = 0m);

public sealed record Unserved(
    string PointCode,
    string Reason,
    IReadOnlyList<Fact> Facts)
{
    // #199: cantidades por línea; vacío cuando el motivo no se refiere a una carga.
    public IReadOnlyList<PendingLine> Lines { get; init; } = [];
}

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
    bool VehicleAvailable)
{
    // #300: plan que se replanifica (de él salen las cargas); null = la última ruta publicada del vehículo.
    public Guid? RoutePlanId { get; init; }
    // #300: recogidas en bodega pendientes que el vehículo hace, en orden, antes de las entregas.
    public IReadOnlyList<FixedStop> LeadingStops { get; init; } = [];
    // #300: paradas ya hechas, incluidas las recogidas; null = CompletedPointCodes.Count. Alinea los tramos guardados.
    public int? CompletedStopCount { get; init; }
}

// #300: parada fija que no es un punto de servicio (recogida en bodega).
public sealed record FixedStop(string Code, GeoPoint Position, int ServiceMinutes);

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
