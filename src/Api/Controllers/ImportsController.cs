using IstpetDev.Api.Authentication;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IstpetDev.Api.Controllers;

[ApiController, Route("api/v1/imports"), Authorize(Roles = Roles.Planning), EnableRateLimiting("imports")]
public sealed class ImportsController(IImportService importer) : ControllerBase
{
    [HttpPost, RequestSizeLimit(ImportLimits.RequestBytes), RequestFormLimits(MultipartBodyLengthLimit = ImportLimits.RequestBytes, ValueLengthLimit = ImportLimits.ProfileBytes)]
    public async Task<IActionResult> Import([FromQuery] bool dryRun = true, [FromQuery] bool allowPartial = false, CancellationToken cancellationToken = default)
    {
        if (!Request.HasFormContentType) return Problem(statusCode: 400, title: "Envíe multipart/form-data con archivos CSV/XLSX.");
        var form = await Request.ReadFormAsync(cancellationToken);
        try
        {
            if (form.Files.Count is < 1 or > 7 || form.Files.Any(f => f.Name == "profile"))
                return Problem(statusCode: 400, title: "Envíe de uno a siete archivos; profile debe ser un campo de texto JSON.");
            if (form.Keys.Any(k => k != "profile") || form["profile"].Count > 1)
                return Problem(statusCode: 400, title: "El único campo de texto admitido es profile, una sola vez.");
            var profile = form.TryGetValue("profile", out var json) && !string.IsNullOrWhiteSpace(json)
                ? MappingProfile.Parse(json.ToString()) : MappingProfile.Auto();
            var selected = new List<(IFormFile File, ImportEntity Entity)>();
            foreach (var file in form.Files)
            {
                if (file.Length > ImportLimits.FileBytes) return Problem(statusCode: 413, title: "El archivo supera 10 MiB; divídalo.");
                var byProfile = profile.EntityForFile(Path.GetFileName(file.FileName));
                ImportEntity? byField = ImportEntityKeys.TryParse(file.Name, out var parsed) ? parsed : null;
                if (byProfile is not null && byField is not null && byProfile != byField)
                    return Problem(statusCode: 400, title: "El nombre del campo y el perfil asignan entidades diferentes; corrija uno de ellos.");
                var entity = byProfile ?? byField;
                if (entity is null) return Problem(statusCode: 400, title: "Archivo sin entidad; use un campo canónico/alias o declare file en profile.", detail: Path.GetFileName(file.FileName));
                if (selected.Any(x => x.Entity == entity)) return Problem(statusCode: 400, title: "Se admite un archivo por entidad; combine las filas en un archivo.");
                selected.Add((file, entity.Value));
            }
            var files = selected.Select(x => new ImportFile(x.Entity, Path.GetFileName(x.File.FileName), x.File.OpenReadStream())).ToList();
            try
            {
                var report = await importer.RunAsync(new ImportRequest(files, profile, dryRun, allowPartial, User.AccountId()), cancellationToken);
                return report.HasErrors && (dryRun || !report.Applied) || !dryRun && !report.Applied ? UnprocessableEntity(report) : Ok(report);
            }
            finally { foreach (var file in files) await file.Content.DisposeAsync(); }
        }
        catch (MappingProfileException ex) { return Problem(statusCode: 400, title: "Perfil inválido", detail: string.Join(" ", ex.Errors)); }
        catch (ImportConflictException ex) { return Problem(statusCode: 409, title: "Conflicto de importación", detail: ex.Message); }
    }
}
