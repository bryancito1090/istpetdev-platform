using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServicePoint> ServicePoints => Set<ServicePoint>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostGIS extension support in PostgreSQL
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<ServicePoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LotNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);

            entity.HasOne(e => e.ServicePoint)
                .WithMany()
                .HasForeignKey(e => e.ServicePointId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TrackingNumber).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.OriginServicePoint)
                .WithMany()
                .HasForeignKey(e => e.OriginServicePointId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.DestinationServicePoint)
                .WithMany()
                .HasForeignKey(e => e.DestinationServicePointId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RiskAssessment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Summary).HasMaxLength(500);

            entity.HasOne(e => e.ServicePoint)
                .WithMany()
                .HasForeignKey(e => e.ServicePointId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
