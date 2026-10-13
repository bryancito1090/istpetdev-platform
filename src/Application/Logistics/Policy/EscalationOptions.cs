using IstpetDev.Application.Logistics.Planning;

namespace IstpetDev.Application.Logistics.Policy;

public sealed record EscalationOption(
    string Key, // "late_service", "prioritize_critical"
    bool Feasible,
    string? FailureReason,
    IReadOnlyList<VehicleRoute> Routes,
    IReadOnlyDictionary<string, int> LateStops, // pointCode -> lateMinutes
    IReadOnlyList<string> DroppedConsolidation,
    decimal TotalCost);
