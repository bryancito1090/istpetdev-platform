using System.Text.Json;

namespace IstpetDev.Application.Imports;

public enum IssueSeverity { Warning, Error }

public sealed record ImportIssue(string Entity, int Row, string? Field, IssueSeverity Severity, string Code, string Message, string? File = null);

public sealed record Assumption(string Note, int Rows);

public sealed class EntityImportReport
{
    public required string Entity { get; init; }
    public string? File { get; set; }
    public int RowsRead { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Duplicates { get; set; }
    public string? Encoding { get; set; }
    public string? Sheet { get; set; }
    public Dictionary<string, string> ResolvedColumns { get; set; } = new();
    public List<string> IgnoredColumns { get; set; } = new();
    public Dictionary<string, Assumption> Assumptions { get; } = new();

    public void CountDefault(CanonicalField field)
    {
        var note = field.DefaultValue is null
            ? field.MissingNote!
            : $"vacío → '{field.DefaultValue}'" + (field.MissingNote is null ? "" : $" ({field.MissingNote})");
        Assumptions[field.Name] = new Assumption(note, Assumptions.TryGetValue(field.Name, out var previous) ? previous.Rows + 1 : 1);
    }
}

public sealed class ImportReport(Guid batchId, string profile, bool dryRun)
{
    public const int MaxIssues = 500;

    public Guid BatchId { get; } = batchId;
    public string Profile { get; } = profile;
    public bool DryRun { get; } = dryRun;
    public bool Applied { get; set; }
    public string CountsMode { get; set; } = "Planned";
    public int ErrorCount { get; private set; }
    public int WarningCount { get; private set; }
    public int OmittedIssues { get; private set; }
    public bool HasErrors => ErrorCount > 0;
    public List<EntityImportReport> Entities { get; } = new();
    public List<ImportIssue> Issues { get; } = new();

    public EntityImportReport Entity(ImportEntity entity)
    {
        var key = entity.Key();
        var report = Entities.FirstOrDefault(e => e.Entity == key);
        if (report is null) Entities.Add(report = new EntityImportReport { Entity = key });
        return report;
    }

    public void Error(ImportEntity entity, int row, string? field, string code, string message)
    {
        ErrorCount++;
        Add(new ImportIssue(entity.Key(), row, field, IssueSeverity.Error, code, message));
    }

    public void Warn(ImportEntity entity, int row, string? field, string code, string message)
    {
        WarningCount++;
        Add(new ImportIssue(entity.Key(), row, field, IssueSeverity.Warning, code, message));
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonDefaults.Options);

    private void Add(ImportIssue issue)
    {
        if (Issues.Count < MaxIssues) Issues.Add(issue with { File = Entities.FirstOrDefault(e => e.Entity == issue.Entity)?.File });
        else OmittedIssues++;
    }
}
