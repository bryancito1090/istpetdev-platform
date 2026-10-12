using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class ScenarioDataset : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSynthetic { get; set; }
    public string? Description { get; set; }
}

public sealed class ScenarioDatasetVersion : OrganizationEntity
{
    public Guid ScenarioDatasetId { get; set; }
    public int Revision { get; set; }
    public int Seed { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string ManifestJson { get; set; } = "{}";
    public Guid? StoredFileId { get; set; }
}

public sealed class ScenarioEvent : OrganizationEntity
{
    public Guid ScenarioDatasetVersionId { get; set; }
    public int SequenceNumber { get; set; }
    public long SimulationTime { get; set; }
    public string EventKind { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}

public sealed class SimulationPolicyVersion : OrganizationEntity
{
    public string Code { get; set; } = string.Empty;
    public int Revision { get; set; }
    public string HandlerCode { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "{}";
    public string ContentHash { get; set; } = string.Empty;
    public Guid ConfigurationVersionId { get; set; }
}

public sealed class SimulationRun : OrganizationEntity
{
    public Guid JobExecutionId { get; set; }
    public Guid ScenarioDatasetVersionId { get; set; }
    public Guid SimulationPolicyVersionId { get; set; }
    public Guid? RoadMatrixId { get; set; }
    public int Seed { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InputHash { get; set; } = string.Empty;
}

public sealed class SimulationEvent : OrganizationEntity
{
    public Guid SimulationRunId { get; set; }
    public int SequenceNumber { get; set; }
    public long SimulationTime { get; set; }
    public string EventKind { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}

public sealed class SimulationMetric : OrganizationEntity
{
    public Guid SimulationRunId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DefinitionVersion { get; set; } = string.Empty;
    public Guid? ProductId { get; set; }
    public decimal Numerator { get; set; }
    public decimal? Denominator { get; set; }
    public decimal? Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? NotApplicableReason { get; set; }
}

public sealed class SimulationArtifact : OrganizationEntity
{
    public Guid SimulationRunId { get; set; }
    public Guid StoredFileId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
}
