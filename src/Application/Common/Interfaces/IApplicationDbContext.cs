using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ServicePoint> ServicePoints { get; }
    DbSet<CatalogOption> CatalogOptions { get; }
    DbSet<Product> Products { get; }
    DbSet<Delivery> Deliveries { get; }
    DbSet<StockBalance> StockBalances { get; }
    DbSet<RiskSnapshot> RiskSnapshots { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
