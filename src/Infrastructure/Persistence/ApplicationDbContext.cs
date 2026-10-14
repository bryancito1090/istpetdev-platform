using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ServicePoint> ServicePoints => Set<ServicePoint>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<HandlingUnit> HandlingUnits => Set<HandlingUnit>();
    public DbSet<HandlingUnitLine> HandlingUnitLines => Set<HandlingUnitLine>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<Consumption> Consumptions => Set<Consumption>();
    public DbSet<RoutePlan> RoutePlans => Set<RoutePlan>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryLine> DeliveryLines => Set<DeliveryLine>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Evidence> Evidences => Set<Evidence>();
    public DbSet<CustodyEvent> CustodyEvents => Set<CustodyEvent>();
    public DbSet<RiskSnapshot> RiskSnapshots => Set<RiskSnapshot>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<AgentDecision> AgentDecisions => Set<AgentDecision>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<SimulationMetric> SimulationMetrics => Set<SimulationMetric>();
    public DbSet<SimulationEvent> SimulationEvents => Set<SimulationEvent>();
    public DbSet<RoadMatrixEdge> RoadMatrixEdges => Set<RoadMatrixEdge>();
    public DbSet<RouteStockReservation> RouteStockReservations => Set<RouteStockReservation>();
    public DbSet<QrAccessToken> QrAccessTokens => Set<QrAccessToken>();
    public DbSet<RouteStopLine> RouteStopLines => Set<RouteStopLine>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryOrderLine> DeliveryOrderLines => Set<DeliveryOrderLine>();
    public DbSet<WarehouseStockPolicy> WarehouseStockPolicies => Set<WarehouseStockPolicy>();
    public DbSet<TransferSuggestion> TransferSuggestions => Set<TransferSuggestion>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        configuration.Properties<decimal>().HavePrecision(18, 3);
        configuration.Properties<string>().HaveMaxLength(256);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => LeanSchema.Configure(modelBuilder);

    public Task LockOrganizationAsync(CancellationToken cancellationToken = default) =>
        Database.ExecuteSqlInterpolatedAsync($"SELECT \"Id\" FROM \"Organization\" WHERE \"Id\" = {DemoOrganization.Id} FOR UPDATE", cancellationToken);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.Version = 1;
                if (entry.Entity is OrganizationEntity { OrganizationId: var organization } scoped && organization == Guid.Empty)
                    scoped.OrganizationId = DemoOrganization.Id;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.Version++;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
