namespace IstpetDev.Application.Simulation;

public sealed record SimulationComparisonRequest(
    string? DatasetVersion = "national-v1",
    int[]? Seeds = null,
    bool IncludeAdverse = true,
    Dictionary<string, string>? PolicyVersions = null,
    IReadOnlyList<DisruptionEvent>? CustomDisruptions = null);

public sealed record SimulationComparisonResponse(
    Guid ComparisonId,
    string Status,
    DateTime CreatedAtUtc);

public sealed record SimulationComparisonSummary(
    Guid ComparisonId,
    string Status,
    string DatasetVersion,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    int ProgressPercentage,
    string Origin);

public sealed record SimulationRunSummaryDto(
    Guid Id,
    int Seed,
    string ScenarioCode,
    string PolicyCode,
    string Status,
    int ProgressPercentage,
    string? FailureReason);

public sealed record SimulationMetricDto(
    string Code,
    string PolicyCode,
    string? Sku,
    decimal Numerator,
    decimal? Denominator,
    decimal? Value,
    string Unit,
    string? NotApplicableReason);

public sealed record SimulationComparisonDetail(
    Guid ComparisonId,
    string Status,
    string DatasetVersion,
    int ProgressPercentage,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    string Origin,
    string? HostName,
    string? CommitHash,
    string? FailureReason,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<SimulationRunSummaryDto> Runs,
    IReadOnlyList<AggregatedMetricSummary> Aggregated,
    IReadOnlyList<SimulationMetricDto> Metrics);

public sealed record SimulationImportResult(
    bool Success,
    Guid? ComparisonId,
    int ImportedRunsCount,
    string? ErrorMessage);

public interface ISimulationService
{
    Task<SimulationComparisonResponse> QueueComparisonAsync(SimulationComparisonRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SimulationComparisonSummary>> ListComparisonsAsync(int limit = 50, CancellationToken ct = default);
    Task<SimulationComparisonDetail?> GetComparisonDetailAsync(Guid comparisonId, CancellationToken ct = default);
    Task<ExportPackage?> ExportComparisonAsync(Guid comparisonId, CancellationToken ct = default);
    Task<SimulationImportResult> ImportComparisonAsync(byte[] zipBytes, string? hostName = null, string? commitHash = null, CancellationToken ct = default);
}

public interface ISimulationWorker
{
    Task EnqueueComparisonAsync(Guid comparisonId, SimulationComparisonRequest request, CancellationToken ct = default);
    Task ExecuteComparisonAsync(Guid comparisonId, SimulationComparisonRequest request, CancellationToken ct = default);
}
