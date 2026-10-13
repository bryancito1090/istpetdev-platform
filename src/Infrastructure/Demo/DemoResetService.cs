using IstpetDev.Application.Demo;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Infrastructure.Demo;

public sealed class DemoResetService(ApplicationDbContext db, IImportService importer, IOptions<DemoOptions> options) : IDemoResetService
{
    public async Task<ImportReport> ResetAsync(CancellationToken cancellationToken = default, Guid? actorAccountId = null)
    {
        if (!options.Value.Enabled) throw new InvalidOperationException("La demo está desactivada.");
        var folder = Path.GetFullPath(options.Value.FixturePath);
        var files = new List<ImportFile>();
        ImportBatch? batch = null;
        ImportReport? report = null;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var profile = MappingProfile.Parse(await File.ReadAllTextAsync(Path.Combine(folder, "mapping.json"), cancellationToken));
            foreach (var (key, mapping) in profile.Files)
                if (mapping.File is not null && ImportEntityKeys.TryParse(key, out var entity))
                    files.Add(new ImportFile(entity, mapping.File, File.OpenRead(Path.Combine(folder, mapping.File))));
            if (files.Count != 7) throw new InvalidOperationException("El fixture debe incluir las siete entidades.");
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT \"Id\" FROM \"Organization\" WHERE \"Id\" = {DemoOrganization.Id} FOR UPDATE", cancellationToken);
            await DeleteOperationalDataAsync(cancellationToken);
            report = await importer.RunAsync(new ImportRequest(files, profile, false, false, actorAccountId), cancellationToken);
            batch = db.ImportBatches.Local.Single(b => b.Id == report.BatchId);
            if (report.Applied) { await transaction.CommitAsync(cancellationToken); return report; }
        }
        catch (Exception)
        {
            batch ??= new ImportBatch { ProfileName = "demo-reset", ProfileJson = "{}", CreatedByAccountId = actorAccountId };
            report ??= new ImportReport(batch.Id, batch.ProfileName, false);
            report.Error(ImportEntity.Warehouses, 0, null, "reset_failed", "No se restauró el fixture; revise sus siete archivos y la configuración.");
        }
        finally { foreach (var file in files) await file.Content.DisposeAsync(); }
        await transaction.RollbackAsync(CancellationToken.None);
        await transaction.DisposeAsync();
        db.ChangeTracker.Clear();
        report!.Applied = false; report.CountsMode = "None";
        foreach (var entity in report.Entities) { entity.Created = 0; entity.Updated = 0; }
        batch!.Status = "Rejected"; batch.ReportJson = report.ToJson();
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(CancellationToken.None);
        return report;
    }

    private async Task DeleteOperationalDataAsync(CancellationToken ct)
    {
        await db.CustodyEvents.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Evidences.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.ReceiptLines.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Receipts.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.InventoryMovements.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.DeliveryLines.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.RouteStockReservations.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Deliveries.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Incidents.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.RouteStops.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.RoutePlans.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.AgentDecisions.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.RiskSnapshots.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        // Se conservan SimulationRuns y SimulationMetrics completados para spec 005
        await db.IdempotencyRecords.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.StockBalances.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.HandlingUnitLines.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.HandlingUnits.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Consumptions.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Lots.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Locations.Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Vehicle).ExecuteDeleteAsync(ct);
        await db.Vehicles.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Locations.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.ServicePoints.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
        await db.Products.Where(x => x.OrganizationId == DemoOrganization.Id).ExecuteDeleteAsync(ct);
    }
}
