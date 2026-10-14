using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IstpetDev.Application.Simulation;

public sealed record ExportPackage(
    byte[] ZipBytes,
    string ETag,
    string ManifestSha256,
    IReadOnlyDictionary<string, string> FileHashes);

public sealed record ZipValidationResult(
    bool IsValid,
    Guid? ComparisonId,
    string? ErrorMessage,
    IReadOnlyDictionary<string, string>? FileHashes = null);

public static class ExportBuilder
{
    public static async Task<ExportPackage> CreateZipPackageAsync(
        Guid comparisonId,
        ScenarioCalendar calendar,
        IReadOnlyList<MetricValue> metrics,
        IReadOnlyList<SimulationEventDto> events,
        string datasetVersion,
        string commitHash,
        string hostName,
        CancellationToken cancellationToken = default)
    {
        var fileContents = new Dictionary<string, byte[]>();

        // 1. metrics.json
        var metricsJson = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
        fileContents["metrics.json"] = Encoding.UTF8.GetBytes(metricsJson);

        // 2. resumen.csv
        var resumenSb = new StringBuilder();
        resumenSb.AppendLine("code,sku,numerator,denominator,value,unit,not_applicable_reason");
        foreach (var m in metrics.OrderBy(x => x.Code).ThenBy(x => x.Sku))
        {
            resumenSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F3},{3},{4},{5},{6}",
                m.Code,
                m.Sku ?? "",
                m.Numerator,
                m.Denominator.HasValue ? m.Denominator.Value.ToString("F3", CultureInfo.InvariantCulture) : "",
                m.Value.HasValue ? m.Value.Value.ToString("F3", CultureInfo.InvariantCulture) : "",
                m.Unit,
                m.NotApplicableReason ?? ""));
        }
        fileContents["resumen.csv"] = Encoding.UTF8.GetBytes(resumenSb.ToString());

        // 3. eventos.csv
        var eventosSb = new StringBuilder();
        eventosSb.AppendLine("sequence,occurred_at_utc,kind,region_code,point_code,sku,requested,satisfied,reason");
        foreach (var ev in events.OrderBy(x => x.Sequence))
        {
            eventosSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:yyyy-MM-ddTHH:mm:ssZ},{2},{3},{4},{5},{6},{7},{8}",
                ev.Sequence,
                ev.OccurredAtUtc,
                ev.Kind,
                ev.RegionCode,
                ev.PointCode,
                ev.Sku,
                ev.Requested.HasValue ? ev.Requested.Value.ToString("F3", CultureInfo.InvariantCulture) : "",
                ev.Satisfied.HasValue ? ev.Satisfied.Value.ToString("F3", CultureInfo.InvariantCulture) : "",
                ev.Reason ?? ""));
        }
        fileContents["eventos.csv"] = Encoding.UTF8.GetBytes(eventosSb.ToString());

        // 4. calendar.json
        var calendarJson = JsonSerializer.Serialize(calendar, new JsonSerializerOptions { WriteIndented = true });
        fileContents["calendar.json"] = Encoding.UTF8.GetBytes(calendarJson);

        // Calcular SHA-256 de los 4 archivos de datos
        var fileHashes = new Dictionary<string, string>();
        foreach (var (fileName, bytes) in fileContents)
        {
            fileHashes[fileName] = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }

        // 5. manifest.json (contiene hashes de los archivos de datos, no de sí mismo)
        var manifestObj = new
        {
            comparisonId = comparisonId.ToString(),
            datasetVersion,
            seed = calendar.Key.Seed,
            scenarioCode = calendar.Key.ScenarioCode,
            commitHash,
            hostName,
            files = fileHashes
        };

        var manifestJson = JsonSerializer.Serialize(manifestObj, new JsonSerializerOptions { WriteIndented = true });
        var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
        var manifestSha256 = Convert.ToHexString(SHA256.HashData(manifestBytes)).ToLowerInvariant();

        // Empaquetar todo en ZIP en memoria
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Escribir manifest.json
            var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Fastest);
            using (var s = manifestEntry.Open())
            {
                await s.WriteAsync(manifestBytes, cancellationToken);
            }

            // Escribir resto de archivos
            foreach (var (fileName, bytes) in fileContents)
            {
                var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                using var s = entry.Open();
                await s.WriteAsync(bytes, cancellationToken);
            }
        }

        var zipBytes = memoryStream.ToArray();
        var etag = $"\"{manifestSha256}\"";

        return new ExportPackage(zipBytes, etag, manifestSha256, fileHashes);
    }

    public static async Task<ZipValidationResult> ValidateZipPackageAsync(
        byte[] zipBytes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var stream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry is null)
            {
                return new ZipValidationResult(false, null, "manifest.json missing in archive");
            }

            using var manifestReader = new StreamReader(manifestEntry.Open(), Encoding.UTF8);
            var manifestText = await manifestReader.ReadToEndAsync(cancellationToken);
            using var doc = JsonDocument.Parse(manifestText);
            var root = doc.RootElement;

            Guid? comparisonId = null;
            if (root.TryGetProperty("comparisonId", out var compProp) && Guid.TryParse(compProp.GetString(), out var cid))
            {
                comparisonId = cid;
            }

            if (!root.TryGetProperty("files", out var filesProp))
            {
                return new ZipValidationResult(false, comparisonId, "files property missing in manifest.json");
            }

            var declaredHashes = new Dictionary<string, string>();
            foreach (var prop in filesProp.EnumerateObject())
            {
                declaredHashes[prop.Name] = prop.Value.GetString() ?? "";
            }

            // Verificar hashes de cada archivo declarado
            foreach (var (fileName, expectedHash) in declaredHashes)
            {
                var entry = archive.GetEntry(fileName);
                if (entry is null)
                {
                    return new ZipValidationResult(false, comparisonId, $"Declared file {fileName} is missing in archive");
                }

                using var ms = new MemoryStream();
                using (var entryStream = entry.Open())
                {
                    await entryStream.CopyToAsync(ms, cancellationToken);
                }

                var actualHash = Convert.ToHexString(SHA256.HashData(ms.ToArray())).ToLowerInvariant();
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    return new ZipValidationResult(false, comparisonId, $"Hash mismatch for {fileName}: Expected {expectedHash}, observed {actualHash}");
                }
            }

            return new ZipValidationResult(true, comparisonId, null, declaredHashes);
        }
        catch (Exception ex)
        {
            return new ZipValidationResult(false, null, $"Invalid zip archive format: {ex.Message}");
        }
    }
}
