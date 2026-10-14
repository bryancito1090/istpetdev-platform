namespace IstpetDev.Application.Simulation;

public sealed record ScenarioKey(string DatasetVersion, int Seed, string ScenarioCode);

public sealed record DemandEvent(
    DateTime OccurredAtUtc,
    string RegionCode,
    string PointCode,
    string Sku,
    decimal Requested,
    string Unit,
    DateTime DueAtUtc,
    bool IsSpike);

public sealed record DisruptionEvent(
    DateTime OccurredAtUtc,
    DateTime KnownAtUtc,
    string RegionCode,
    string Kind,
    string SubjectCode,
    decimal Factor);

public sealed record ScenarioCalendar(
    ScenarioKey Key,
    IReadOnlyList<DemandEvent> Demand,
    IReadOnlyList<DisruptionEvent> Disruptions,
    string Sha256);

public sealed record PolicyResult(
    string PolicyVersion,
    ScenarioKey Key,
    IReadOnlyList<SimulationDayResult> Days,
    IReadOnlyList<SimulationEventDto> Events);

public sealed record SimulationDayResult(
    DateOnly Date,
    string RegionCode,
    decimal Km,
    decimal TransportCost,
    int CompletedDeliveries,
    int OnTimeCommonObligations);

public sealed record SimulationEventDto(
    long Sequence,
    DateTime OccurredAtUtc,
    string Kind,
    string RegionCode,
    string PointCode,
    string Sku,
    decimal? Requested,
    decimal? Satisfied,
    string? Reason);

public sealed record MetricValue(
    string Code,
    string DefinitionVersion,
    string? Sku,
    decimal Numerator,
    decimal? Denominator,
    decimal? Value,
    string Unit,
    string? NotApplicableReason);

public interface IScenarioImporter
{
    Task ImportInitialStateAsync(
        string canonicalDirectory,
        string simulationConnection,
        CancellationToken cancellationToken);
}

public interface IRegionalPlanner
{
    Task<RegionalPlan> PlanAsync(RegionalPlanRequest request, CancellationToken cancellationToken);
}

public sealed record RegionalPlanRequest(
    DateOnly Date,
    string RegionCode,
    string DistanceMode,
    IReadOnlyList<string> AvailableVehicleCodes);

public sealed record RegionalPlan(
    decimal Km,
    decimal TransportCost,
    IReadOnlyList<string> CompletedPointCodes,
    IReadOnlyList<string> UnservedPointCodes);
