using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController, Route("api/v1/imports"), Authorize(Roles = Roles.Human), EnableRateLimiting("imports")]
public sealed class ImportsController(IImportService importer, IApplicationDbContext db) : ControllerBase
{
    [HttpGet("data/{entity}")]
    [Authorize(Roles = Roles.Human)]
    public async Task<IActionResult> GetEntityData([FromRoute] string entity, CancellationToken cancellationToken = default)
    {
        if (!ImportEntityKeys.TryParse(entity, out var parsed))
            return BadRequest(new { message = $"Entidad desconocida: '{entity}'. Válidas: {string.Join(", ", ImportEntityKeys.All)}" });

        switch (parsed)
        {
            case ImportEntity.Warehouses:
            {
                var list = await db.Locations
                    .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse)
                    .AsNoTracking()
                    .OrderBy(l => l.Code)
                    .Select(l => new
                    {
                        code = l.Code,
                        name = l.Name,
                        lat = l.Position != null ? (double?)l.Position.Y : null,
                        lon = l.Position != null ? (double?)l.Position.X : null,
                        isActive = l.IsActive
                    })
                    .ToListAsync(cancellationToken);
                return Ok(list);
            }
            case ImportEntity.ServicePoints:
            {
                var list = await db.ServicePoints
                    .Where(p => p.OrganizationId == DemoOrganization.Id)
                    .AsNoTracking()
                    .OrderBy(p => p.Code)
                    .Select(p => new
                    {
                        code = p.Code,
                        name = p.Name,
                        lat = p.Position != null ? (double?)p.Position.Y : null,
                        lon = p.Position != null ? (double?)p.Position.X : null,
                        region = p.Region ?? "",
                        category = p.Category ?? "",
                        criticality = p.Criticality ?? "normal",
                        window_start = p.WindowStartLocal.HasValue ? p.WindowStartLocal.Value.ToString("HH:mm") : null,
                        window_end = p.WindowEndLocal.HasValue ? p.WindowEndLocal.Value.ToString("HH:mm") : null,
                        service_minutes = p.ServiceMinutes,
                        lead_time_days = p.LeadTimeDays,
                        isActive = p.IsActive
                    })
                    .ToListAsync(cancellationToken);
                return Ok(list);
            }
            case ImportEntity.Products:
            {
                var list = await db.Products
                    .Where(p => p.OrganizationId == DemoOrganization.Id)
                    .AsNoTracking()
                    .OrderBy(p => p.Sku)
                    .Select(p => new
                    {
                        sku = p.Sku,
                        name = p.Name,
                        base_unit = p.BaseUnit,
                        weight_per_unit = p.WeightPerBaseUnit,
                        volume_per_unit = p.VolumePerBaseUnit,
                        isActive = p.IsActive
                    })
                    .ToListAsync(cancellationToken);
                return Ok(list);
            }
            case ImportEntity.Vehicles:
            {
                var list = await (from v in db.Vehicles.Where(v => v.OrganizationId == DemoOrganization.Id).AsNoTracking()
                                  join l in db.Locations on v.DepotLocationId equals l.Id into locs
                                  from loc in locs.DefaultIfEmpty()
                                  orderby v.Code
                                  select new
                                  {
                                      code = v.Code,
                                      depot_code = loc != null ? loc.Code : "",
                                      max_weight = v.MaxWeight,
                                      max_volume = v.MaxVolume,
                                      cost_per_km = v.CostPerKm,
                                      isActive = v.IsActive
                                  }).ToListAsync(cancellationToken);
                return Ok(list);
            }
            case ImportEntity.Lots:
            {
                var list = await (from l in db.Lots.Where(l => l.OrganizationId == DemoOrganization.Id).AsNoTracking()
                                  join p in db.Products on l.ProductId equals p.Id into prods
                                  from prod in prods.DefaultIfEmpty()
                                  orderby l.Code
                                  select new
                                  {
                                      sku = prod != null ? prod.Sku : "",
                                      product_name = prod != null ? prod.Name : "",
                                      base_unit = prod != null ? prod.BaseUnit : "",
                                      lot_code = l.Code,
                                      origin = l.Origin ?? "",
                                      expires_on = l.ExpiresOn.HasValue ? l.ExpiresOn.Value.ToString("yyyy-MM-dd") : null,
                                      isBlocked = l.IsBlocked
                                  }).ToListAsync(cancellationToken);
                return Ok(list);
            }
            case ImportEntity.Stock:
            {
                var list = await (from s in db.StockBalances.Where(s => s.OrganizationId == DemoOrganization.Id).AsNoTracking()
                                  join loc in db.Locations on s.LocationId equals loc.Id into locs
                                  from loc in locs.DefaultIfEmpty()
                                  join l in db.Lots on s.LotId equals l.Id into lots
                                  from lot in lots.DefaultIfEmpty()
                                  join prod in db.Products on (lot != null ? lot.ProductId : Guid.Empty) equals prod.Id into prods
                                  from prod in prods.DefaultIfEmpty()
                                  orderby loc != null ? loc.Code : "", prod != null ? prod.Sku : ""
                                  select new
                                  {
                                      location_code = loc != null ? loc.Code : "",
                                      location_kind = loc != null ? loc.Kind : "",
                                      sku = prod != null ? prod.Sku : "",
                                      lot_code = lot != null ? lot.Code : "",
                                      quantity = s.QuantityOnHand,
                                      reserved = s.QuantityReserved
                                  }).ToListAsync(cancellationToken);
                return Ok(list);
            }
            case ImportEntity.Consumptions:
            {
                var list = await (from c in db.Consumptions.Where(c => c.OrganizationId == DemoOrganization.Id).AsNoTracking()
                                  join sp in db.ServicePoints on c.ServicePointId equals sp.Id into sps
                                  from sp in sps.DefaultIfEmpty()
                                  join prod in db.Products on c.ProductId equals prod.Id into prods
                                  from prod in prods.DefaultIfEmpty()
                                  orderby c.OccurredOn descending, sp != null ? sp.Code : ""
                                  select new
                                  {
                                      point_code = sp != null ? sp.Code : "",
                                      sku = prod != null ? prod.Sku : "",
                                      date = c.OccurredOn.ToString("yyyy-MM-dd"),
                                      quantity = c.Quantity,
                                      period_days = c.PeriodDays
                                  }).Take(100).ToListAsync(cancellationToken);
                return Ok(list);
            }
            default:
                return Ok(Array.Empty<object>());
        }
    }
    [HttpPost, Authorize(Roles = Roles.Planning), RequestSizeLimit(ImportLimits.RequestBytes), RequestFormLimits(MultipartBodyLengthLimit = ImportLimits.RequestBytes, ValueLengthLimit = ImportLimits.ProfileBytes)]
    public async Task<IActionResult> Import([FromQuery] bool dryRun = true, [FromQuery] bool allowPartial = false, CancellationToken cancellationToken = default)
    {
        if (!Request.HasFormContentType) return Problem(statusCode: 400, title: "Envíe multipart/form-data con archivos CSV/XLSX.");
        var form = await Request.ReadFormAsync(cancellationToken);
        try
        {
            if (form.Files.Count < 1 || form.Files.Count > CanonicalSchema.Order.Length || form.Files.Any(f => f.Name == "profile"))
                return Problem(statusCode: 400, title: $"Envíe de uno a {CanonicalSchema.Order.Length} archivos; profile debe ser un campo de texto JSON.");
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
