using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IstpetDev.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Account> Accounts { get; }
    DbSet<ServicePoint> ServicePoints { get; }
    DbSet<Location> Locations { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Product> Products { get; }
    DbSet<Lot> Lots { get; }
    DbSet<HandlingUnit> HandlingUnits { get; }
    DbSet<HandlingUnitLine> HandlingUnitLines { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<StockBalance> StockBalances { get; }
    DbSet<Consumption> Consumptions { get; }
    DbSet<RoutePlan> RoutePlans { get; }
    DbSet<RouteStop> RouteStops { get; }
    DbSet<Delivery> Deliveries { get; }
    DbSet<DeliveryLine> DeliveryLines { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<ReceiptLine> ReceiptLines { get; }
    DbSet<Evidence> Evidences { get; }
    DbSet<CustodyEvent> CustodyEvents { get; }
    DbSet<RiskSnapshot> RiskSnapshots { get; }
    DbSet<Incident> Incidents { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }
    DbSet<ImportBatch> ImportBatches { get; }
    DbSet<AgentRun> AgentRuns { get; }
    DbSet<AgentDecision> AgentDecisions { get; }
    DbSet<SimulationRun> SimulationRuns { get; }
    DbSet<SimulationMetric> SimulationMetrics { get; }
    DbSet<RoadMatrixEdge> RoadMatrixEdges { get; }
    DbSet<RouteStockReservation> RouteStockReservations { get; }
    DbSet<QrAccessToken> QrAccessTokens { get; }
    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }
    Task LockOrganizationAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
