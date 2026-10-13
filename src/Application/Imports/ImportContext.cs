using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Entities;
using IstpetDev.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Imports;

// Entidades existentes indexadas por clave natural; el escritor añade aquí lo que crea en el mismo lote.
internal sealed class ImportContext
{
    public Dictionary<(string Kind, string Code), Location> Locations { get; } = new();
    public Dictionary<string, ServicePoint> ServicePoints { get; } = new();
    public Dictionary<string, Product> Products { get; } = new();
    public Dictionary<string, Vehicle> Vehicles { get; } = new();
    public Dictionary<(Guid ProductId, string Code), Lot> Lots { get; } = new();
    public Dictionary<(Guid LocationId, Guid LotId), StockBalance> Balances { get; } = new();
    public Dictionary<(Guid ServicePointId, Guid ProductId, DateOnly Date), Consumption> Consumptions { get; } = new();

    public static async Task<ImportContext> LoadAsync(IApplicationDbContext db, CancellationToken cancellationToken)
    {
        var context = new ImportContext();
        foreach (var x in await db.Locations.Where(x => x.OrganizationId == DemoOrganization.Id).ToListAsync(cancellationToken)) context.Locations[(x.Kind, x.Code)] = x;
        foreach (var x in await db.ServicePoints.Where(x => x.OrganizationId == DemoOrganization.Id).ToListAsync(cancellationToken)) context.ServicePoints[x.Code] = x;
        foreach (var x in await db.Products.Where(x => x.OrganizationId == DemoOrganization.Id).ToListAsync(cancellationToken)) context.Products[x.Sku] = x;
        foreach (var x in await db.Vehicles.Where(x => x.OrganizationId == DemoOrganization.Id).ToListAsync(cancellationToken)) context.Vehicles[x.Code] = x;
        foreach (var x in await db.Lots.Where(x => x.OrganizationId == DemoOrganization.Id).ToListAsync(cancellationToken)) context.Lots[(x.ProductId, x.Code)] = x;
        foreach (var x in await db.StockBalances.Where(x => x.OrganizationId == DemoOrganization.Id).ToListAsync(cancellationToken)) context.Balances[(x.LocationId, x.LotId)] = x;
        foreach (var x in await db.Consumptions.Where(x => x.OrganizationId == DemoOrganization.Id).ToListAsync(cancellationToken)) context.Consumptions[(x.ServicePointId, x.ProductId, x.OccurredOn)] = x;
        return context;
    }
}
