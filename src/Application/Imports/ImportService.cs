using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Imports;

public sealed record ImportFile(ImportEntity Entity, string FileName, Stream Content);
public sealed record ImportRequest(IReadOnlyList<ImportFile> Files, MappingProfile Profile, bool DryRun, bool AllowPartial, Guid? ActorAccountId);
public interface IImportService
{
    Task<ImportReport> RunAsync(ImportRequest request, CancellationToken cancellationToken = default);
}

public sealed class ImportConflictException(string message) : Exception(message);

public sealed class ImportService(IApplicationDbContext db, ITabularReader reader) : IImportService
{
    public async Task<ImportReport> RunAsync(ImportRequest request, CancellationToken cancellationToken = default)
    {
        var profileErrors = request.Profile.Validate();
        if (profileErrors.Count > 0) throw new MappingProfileException(profileErrors);
        if (request.Files.Count is < 1 or > 7 || request.Files.Select(f => f.Entity).Distinct().Count() != request.Files.Count)
            throw new MappingProfileException(["Envíe entre uno y siete archivos, uno por entidad."]);
        var batch = new ImportBatch
        {
            ProfileName = request.Profile.Name, DryRun = request.DryRun,
            ProfileJson = request.Profile.ToJson(), CreatedByAccountId = request.ActorAccountId,
        };
        batch.ProfileHash = Hash(Encoding.UTF8.GetBytes(batch.ProfileJson));
        var report = new ImportReport(batch.Id, request.Profile.Name, request.DryRun);
        var parsed = new Dictionary<ImportEntity, List<ParsedRow>>();
        var manifest = new List<object>();
        long totalBytes = 0;
        foreach (var file in request.Files.OrderBy(f => f.Entity))
        {
            var entityReport = report.Entity(file.Entity);
            entityReport.File = Path.GetFileName(file.FileName);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int size;
            while ((size = await file.Content.ReadAsync(chunk, cancellationToken)) > 0)
            {
                totalBytes += size;
                if (buffer.Length + size > ImportLimits.FileBytes || totalBytes > ImportLimits.RequestBytes)
                    throw new MappingProfileException(["Los archivos superan 10 MiB por archivo o 50 MiB en total; divídalos."]);
                await buffer.WriteAsync(chunk.AsMemory(0, size), cancellationToken);
            }
            manifest.Add(new { entity = file.Entity.Key(), file = entityReport.File, bytes = buffer.Length, sha256 = Hash(buffer.ToArray()) });
            buffer.Position = 0;
            var mapping = request.Profile.For(file.Entity);
            try
            {
                var table = reader.Read(buffer, file.FileName, request.Profile.Delimiter, mapping?.Sheet);
                entityReport.RowsRead = table.Rows.Count;
                entityReport.Encoding = table.Encoding;
                entityReport.Sheet = table.Sheet;
                var resolution = ColumnResolver.Resolve(table.Headers, file.Entity, mapping);
                entityReport.ResolvedColumns = resolution.Columns;
                entityReport.IgnoredColumns = resolution.Ignored;
                foreach (var missing in resolution.MissingRequired)
                    report.Error(file.Entity, 1, missing, "missing_column", $"Incluya la columna obligatoria '{missing}' o declare su mapeo/constante.");
                if (resolution.MissingRequired.Count > 0) { entityReport.Skipped = table.Rows.Count; continue; }
                parsed[file.Entity] = RowParser.Parse(table, file.Entity, resolution, mapping, request.Profile.ToParseSettings(), report);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Parser internals can contain raw input. Keep the public error bounded and actionable.
                var message = ex is UnsupportedFileException or MappingProfileException ? ex.Message : "Archivo dañado o formato inválido; expórtelo como CSV/XLSX y revise separador, hoja y comillas.";
                report.Error(file.Entity, 0, null, "file_unreadable", message);
            }
        }
        batch.FilesJson = JsonSerializer.Serialize(manifest, JsonDefaults.Options);
        batch.InputHash = Hash(Encoding.UTF8.GetBytes(batch.FilesJson));
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            // ponytail: one organization lock; split by dataset only when throughput needs it.
            await db.LockOrganizationAsync(cancellationToken);
            var context = await ImportContext.LoadAsync(db, cancellationToken);
            var writer = new CanonicalWriter(db, context, report, batch.Id, request.ActorAccountId);
            foreach (var entity in CanonicalSchema.Order)
                if (parsed.TryGetValue(entity, out var rows)) writer.Write(entity, rows);
            var validChanges = report.Entities.Sum(e => e.Created + e.Updated) > 0;
            if (!validChanges && !report.HasErrors) report.Error(request.Files[0].Entity, 0, null, "empty_dataset", "El envío no contiene filas válidas; añada datos antes de importar.");
            report.Applied = !request.DryRun && validChanges && (!report.HasErrors || request.AllowPartial);
            if (!report.Applied) db.ChangeTracker.Clear();
            batch.Status = report.Applied ? "Applied" : report.HasErrors || !validChanges ? "Rejected" : "DryRun";
            report.CountsMode = report.Applied ? "Committed" : batch.Status == "DryRun" ? "Planned" : "None";
            if (batch.Status == "Rejected")
                foreach (var entity in report.Entities) { entity.Created = 0; entity.Updated = 0; }
            batch.ReportJson = report.ToJson();
            db.ImportBatches.Add(batch);
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return report;
        }
        catch (Exception ex) when (ownsTransaction)
        {
            await transaction!.RollbackAsync(CancellationToken.None);
            await transaction.DisposeAsync();
            db.ChangeTracker.Clear();
            report.Applied = false;
            report.CountsMode = "None";
            foreach (var entity in report.Entities) { entity.Created = 0; entity.Updated = 0; }
            report.Error(request.Files[0].Entity, 0, null, "transaction_failed", "No se aplicó el lote; revise el conflicto y vuelva a validar en seco.");
            batch.Status = "Rejected"; batch.ReportJson = report.ToJson();
            db.ImportBatches.Add(batch);
            try { await db.SaveChangesAsync(CancellationToken.None); }
            catch { db.ChangeTracker.Clear(); throw; }
            if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                throw new ImportConflictException("La base rechazó el cambio concurrente; valide de nuevo antes de aplicar.");
            throw;
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
