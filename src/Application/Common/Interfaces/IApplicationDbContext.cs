using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ServicePoint> ServicePoints { get; }
    DbSet<Product> Products { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<Delivery> Deliveries { get; }
    DbSet<RiskAssessment> RiskAssessments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
