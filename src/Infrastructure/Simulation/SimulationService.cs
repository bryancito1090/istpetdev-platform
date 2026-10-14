using System.IO.Compression;
using System.Text;
using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Simulation;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IstpetDev.Infrastructure.Simulation;

public sealed class SimulationService : ISimulationService
{
    private readonly IApplicationDbContext _db;
    private readonly ISimulationWorker _worker;
    private readonly IConfiguration _configuration;

    public SimulationService(
        IApplicationDbContext db,
        ISimulationWorker worker,
        IConfiguration configuration)
    {
        _db = db;
        _worker = worker;
        _configuration = configuration;
    }

    public async Task<SimulationComparisonResponse> QueueComparisonAsync(
        SimulationComparisonRequest request,
        CancellationToken ct = default)
    {
        var operationalConn = _configuration.GetConnectionString("DefaultConnection") ?? "";
        var simulationConn = _configuration.GetConnectionString("Simulation") ?? operationalConn;

        // Validar aislamiento de bases de datos
        SimulationDatabase.ValidateIsolation(operationalConn, simulationConn);

        var datasetVersion = request.DatasetVersion ?? "national-v1";
        if (string.IsNullOrWhiteSpace(datasetVersion))
        {
            throw new ArgumentException("DatasetVersion is required");
        }

        var baseSeeds = request.Seeds is { Length: > 0 } ? request.Seeds : [42, 101, 2024];
        var scenarios = new List<(int Seed, string ScenarioCode)>();

        foreach (var s in baseSeeds)
        {
            scenarios.Add((s, "base"));
        }

        if (request.IncludeAdverse)
        {
            scenarios.Add((9999, "adverse"));
        }

        var policyVersions = request.PolicyVersions ?? new Dictionary<string, string>
        {
            { "reactive", ReactivePolicy.Version },
            { "fixed", FixedCalendarPolicy.Version },
            { "istpetdev", IstpetDevPolicy.Version }
        };

        var comparisonId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var policies = new[]
        {
            policyVersions.GetValueOrDefault("reactive", ReactivePolicy.Version),
            policyVersions.GetValueOrDefault("fixed", FixedCalendarPolicy.Version),
            policyVersions.GetValueOrDefault("istpetdev", IstpetDevPolicy.Version)
        };

        foreach (var (seed, scenarioCode) in scenarios)
        {
            foreach (var policy in policies)
            {
                var run = new SimulationRun
                {
                    OrganizationId = DemoOrganization.Id,
                    ComparisonId = comparisonId,
                    Seed = seed,
                    ScenarioCode = scenarioCode,
                    PolicyCode = policy,
                    PolicyVersion = policy,
                    DatasetVersion = datasetVersion,
                    Status = "Queued",
                    ProgressPercentage = 0,
                    Origin = "computed",
                    StartedAtUtc = now
                };

                _db.SimulationRuns.Add(run);
            }
        }

        await _db.SaveChangesAsync(ct);

        // Encolar al worker asíncrono
        await _worker.EnqueueComparisonAsync(comparisonId, request, ct);

        return new SimulationComparisonResponse(comparisonId, "Queued", now);
    }

    public async Task<IReadOnlyList<SimulationComparisonSummary>> ListComparisonsAsync(
        int limit = 50,
        CancellationToken ct = default)
    {
        var runs = await _db.SimulationRuns
            .Where(r => r.OrganizationId == DemoOrganization.Id && r.ComparisonId != Guid.Empty)
            .OrderByDescending(r => r.StartedAtUtc)
            .ToListAsync(ct);

        var grouped = runs
            .GroupBy(r => r.ComparisonId)
            .Take(limit)
            .Select(g =>
            {
                var runsList = g.ToList();
                var status = runsList.Any(r => r.Status == "Running") ? "Running"
                    : runsList.Any(r => r.Status == "Failed") ? "Failed"
                    : runsList.Any(r => r.Status == "Interrupted") ? "Interrupted"
                    : runsList.All(r => r.Status == "Completed") ? "Completed"
                    : "Queued";

                var progress = (int)runsList.Average(r => r.ProgressPercentage);
                var first = runsList.First();

                return new SimulationComparisonSummary(
                    ComparisonId: g.Key,
                    Status: status,
                    DatasetVersion: first.DatasetVersion,
                    CreatedAtUtc: runsList.Min(r => r.StartedAtUtc),
                    CompletedAtUtc: runsList.Max(r => r.CompletedAtUtc),
                    ProgressPercentage: progress,
                    Origin: first.Origin ?? "computed");
            })
            .ToList();

        return grouped;
    }

    public async Task<SimulationComparisonDetail?> GetComparisonDetailAsync(
        Guid comparisonId,
        CancellationToken ct = default)
    {
        var runs = await _db.SimulationRuns
            .Where(r => r.OrganizationId == DemoOrganization.Id && r.ComparisonId == comparisonId)
            .OrderBy(r => r.Seed)
            .ThenBy(r => r.PolicyCode)
            .ToListAsync(ct);

        if (runs.Count == 0)
        {
            return null;
        }

        var runIds = runs.Select(r => r.Id).ToList();

        var metrics = await _db.SimulationMetrics
            .Where(m => runIds.Contains(m.SimulationRunId))
            .ToListAsync(ct);

        var runDtos = runs.Select(r => new SimulationRunSummaryDto(
            Id: r.Id,
            Seed: r.Seed,
            ScenarioCode: r.ScenarioCode,
            PolicyCode: r.PolicyCode,
            Status: r.Status,
            ProgressPercentage: r.ProgressPercentage,
            FailureReason: r.FailureReason)).ToList();

        var metricDtos = metrics.Select(m => new SimulationMetricDto(
            Code: m.Code,
            PolicyCode: m.PolicyCode,
            Sku: null,
            Numerator: m.Numerator,
            Denominator: m.Denominator,
            Value: m.Value,
            Unit: m.Unit,
            NotApplicableReason: m.NotApplicableReason)).ToList();

        // Agregación de métricas
        var seedPolicyMetrics = new List<SeedPolicyMetric>();
        foreach (var run in runs)
        {
            var rMetrics = metrics.Where(m => m.SimulationRunId == run.Id);
            foreach (var rm in rMetrics)
            {
                seedPolicyMetrics.Add(new SeedPolicyMetric(
                    Seed: run.Seed,
                    Scenario: run.ScenarioCode,
                    Policy: run.PolicyCode,
                    MetricCode: rm.Code,
                    Value: rm.Value));
            }
        }

        var aggregated = new List<AggregatedMetricSummary>();
        var distinctPolicies = runs.Select(r => r.PolicyCode).Distinct();
        var distinctMetricCodes = metrics.Select(m => m.Code).Distinct();

        foreach (var p in distinctPolicies)
        {
            foreach (var mc in distinctMetricCodes)
            {
                aggregated.Add(ComparisonAggregator.AggregateMetric(seedPolicyMetrics, p, mc));
            }
        }

        var status = runs.Any(r => r.Status == "Running") ? "Running"
            : runs.Any(r => r.Status == "Failed") ? "Failed"
            : runs.Any(r => r.Status == "Interrupted") ? "Interrupted"
            : runs.All(r => r.Status == "Completed") ? "Completed"
            : "Queued";

        var progress = (int)runs.Average(r => r.ProgressPercentage);
        var first = runs.First();

        var assumptions = new List<string>
        {
            "Matriz vial precargada / aproximación euclidiana etiquetada",
            "Lead time nacional: 2 días laborables",
            "Meta de cobertura: 14 días de consumo promedio",
            "Calendario exógeno común e inmutable por seed",
            "Demanda insatisfecha agrupada por SKU y unidad base"
        };

        return new SimulationComparisonDetail(
            ComparisonId: comparisonId,
            Status: status,
            DatasetVersion: first.DatasetVersion,
            ProgressPercentage: progress,
            CreatedAtUtc: runs.Min(r => r.StartedAtUtc),
            CompletedAtUtc: runs.Max(r => r.CompletedAtUtc),
            Origin: first.Origin ?? "computed",
            HostName: first.HostName,
            CommitHash: first.CommitHash,
            FailureReason: runs.FirstOrDefault(r => r.FailureReason != null)?.FailureReason,
            Assumptions: assumptions,
            Runs: runDtos,
            Aggregated: aggregated,
            Metrics: metricDtos);
    }

    public async Task<ExportPackage?> ExportComparisonAsync(
        Guid comparisonId,
        CancellationToken ct = default)
    {
        var runs = await _db.SimulationRuns
            .Where(r => r.OrganizationId == DemoOrganization.Id && r.ComparisonId == comparisonId)
            .ToListAsync(ct);

        if (runs.Count == 0)
        {
            return null;
        }

        // Si no han completado todas las corridas, no se puede exportar aún (409)
        if (!runs.All(r => r.Status == "Completed"))
        {
            return null;
        }

        var runIds = runs.Select(r => r.Id).ToList();

        var metrics = await _db.SimulationMetrics
            .Where(m => runIds.Contains(m.SimulationRunId))
            .ToListAsync(ct);

        var events = await _db.SimulationEvents
            .Where(e => runIds.Contains(e.SimulationRunId))
            .OrderBy(e => e.Sequence)
            .ToListAsync(ct);

        var metricValues = metrics.Select(m => new MetricValue(
            Code: m.Code,
            DefinitionVersion: "v1",
            Sku: null,
            Numerator: m.Numerator,
            Denominator: m.Denominator,
            Value: m.Value,
            Unit: m.Unit,
            NotApplicableReason: m.NotApplicableReason)).ToList();

        var eventDtos = events.Select(e => new SimulationEventDto(
            Sequence: e.Sequence,
            OccurredAtUtc: e.OccurredAtUtc,
            Kind: e.Kind,
            RegionCode: e.RegionCode,
            PointCode: e.PointCode,
            Sku: e.Sku,
            Requested: e.Requested,
            Satisfied: e.Satisfied,
            Reason: e.Reason)).ToList();

        var first = runs.First();
        var key = new ScenarioKey(first.DatasetVersion, first.Seed, first.ScenarioCode);
        var calendar = ExogenousCalendar.Build(key, [("REG-01", "P01")], [("SKU-01", 10m, "L")], new DateOnly(2026, 10, 12), 90);

        return await ExportBuilder.CreateZipPackageAsync(
            comparisonId,
            calendar,
            metricValues,
            eventDtos,
            first.DatasetVersion,
            commitHash: first.CommitHash ?? "develop-head",
            hostName: first.HostName ?? Environment.MachineName,
            cancellationToken: ct);
    }

    public async Task<SimulationImportResult> ImportComparisonAsync(
        byte[] zipBytes,
        string? hostName = null,
        string? commitHash = null,
        CancellationToken ct = default)
    {
        var validation = await ExportBuilder.ValidateZipPackageAsync(zipBytes, ct);
        if (!validation.IsValid || !validation.ComparisonId.HasValue)
        {
            return new SimulationImportResult(false, null, 0, validation.ErrorMessage ?? "Archivo ZIP inválido o alterado");
        }

        var comparisonId = validation.ComparisonId.Value;

        // Idempotencia: si ya existe la comparación en la base de datos, no duplicar
        var existingRuns = await _db.SimulationRuns
            .Where(r => r.OrganizationId == DemoOrganization.Id && r.ComparisonId == comparisonId)
            .ToListAsync(ct);

        if (existingRuns.Count > 0)
        {
            return new SimulationImportResult(true, comparisonId, existingRuns.Count, null);
        }

        // Extraer contenido del ZIP
        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var manifestEntry = archive.GetEntry("manifest.json");
        string datasetVersion = "national-v1";
        int seed = 42;
        string scenarioCode = "base";

        if (manifestEntry != null)
        {
            using var r = new StreamReader(manifestEntry.Open(), Encoding.UTF8);
            var mJson = await r.ReadToEndAsync(ct);
            using var doc = JsonDocument.Parse(mJson);
            if (doc.RootElement.TryGetProperty("datasetVersion", out var dProp)) datasetVersion = dProp.GetString() ?? datasetVersion;
            if (doc.RootElement.TryGetProperty("seed", out var sProp)) seed = sProp.GetInt32();
            if (doc.RootElement.TryGetProperty("scenarioCode", out var scProp)) scenarioCode = scProp.GetString() ?? scenarioCode;
        }

        // Crear una corrida importada
        var importedRun = new SimulationRun
        {
            OrganizationId = DemoOrganization.Id,
            ComparisonId = comparisonId,
            Seed = seed,
            ScenarioCode = scenarioCode,
            PolicyCode = "imported-policy",
            PolicyVersion = "imported-v1",
            DatasetVersion = datasetVersion,
            Status = "Completed",
            ProgressPercentage = 100,
            Origin = "imported",
            HostName = hostName ?? Environment.MachineName,
            CommitHash = commitHash ?? "imported-commit",
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        _db.SimulationRuns.Add(importedRun);

        // Leer metrics.json si existe
        var metricsEntry = archive.GetEntry("metrics.json");
        if (metricsEntry != null)
        {
            using var r = new StreamReader(metricsEntry.Open(), Encoding.UTF8);
            var mJson = await r.ReadToEndAsync(ct);
            var metricValues = JsonSerializer.Deserialize<List<MetricValue>>(mJson);
            if (metricValues != null)
            {
                foreach (var mv in metricValues)
                {
                    _db.SimulationMetrics.Add(new SimulationMetric
                    {
                        OrganizationId = DemoOrganization.Id,
                        SimulationRunId = importedRun.Id,
                        Code = mv.Code,
                        PolicyCode = "imported-policy",
                        Numerator = mv.Numerator,
                        Denominator = mv.Denominator,
                        Value = mv.Value,
                        Unit = mv.Unit,
                        NotApplicableReason = mv.NotApplicableReason
                    });
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        return new SimulationImportResult(true, comparisonId, 1, null);
    }
}
