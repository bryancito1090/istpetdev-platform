using System.Linq.Expressions;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IstpetDev.Infrastructure.Persistence;

// Esquema de la demo (spec 001): sin RLS ni configuración dinámica; OrganizationId apunta a la organización demo.
internal static class LeanSchema
{
    private const string PointType = "geometry(Point,4326)";

    private static readonly HashSet<string> LongText =
        ["Description", "Rationale", "FailureReason", "PendingReason", "Reason", "RejectionReason", "EncodedPolyline", "ExplanationMismatch"];

    public static void Configure(ModelBuilder model)
    {
        model.HasPostgresExtension("postgis");

        model.Entity<Organization>(e =>
        {
            e.HasIndex(o => o.Code).IsUnique();
            e.HasData(new Organization
            {
                Id = DemoOrganization.Id, Code = "demo", Name = "ISTPET Demo", TimeZone = "America/Guayaquil",
                IsActive = true, CreatedAtUtc = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc), Version = 1,
            });
        });
        model.Entity<Account>(e =>
        {
            e.Property(a => a.Issuer).HasMaxLength(512);
            e.Property(a => a.Subject).HasMaxLength(512);
            e.HasIndex(a => new { a.Issuer, a.Subject }).IsUnique();
        });

        model.Entity<ServicePoint>(e =>
        {
            e.HasIndex(p => new { p.OrganizationId, p.Code }).IsUnique();
            e.Property(p => p.Position).HasColumnType(PointType);
            Fk<ServicePoint, Location>(e, p => p.ServingWarehouseLocationId);
        });
        model.Entity<Location>(e =>
        {
            e.HasIndex(l => new { l.OrganizationId, l.Kind, l.Code }).IsUnique();
            e.Property(l => l.Position).HasColumnType(PointType);
            Fk<Location, ServicePoint>(e, l => l.ServicePointId);
            Fk<Location, Vehicle>(e, l => l.VehicleId);
        });
        model.Entity<Vehicle>(e =>
        {
            e.HasIndex(v => new { v.OrganizationId, v.Code }).IsUnique();
            Fk<Vehicle, Location>(e, v => v.DepotLocationId);
        });
        model.Entity<Product>(e => e.HasIndex(p => new { p.OrganizationId, p.Sku }).IsUnique());
        model.Entity<Lot>(e =>
        {
            e.HasIndex(l => new { l.ProductId, l.Code }).IsUnique();
            Fk<Lot, Product>(e, l => l.ProductId);
        });
        model.Entity<HandlingUnit>(e =>
        {
            e.HasIndex(h => new { h.OrganizationId, h.Code }).IsUnique();
            Fk<HandlingUnit, Location>(e, h => h.LocationId);
            Fk<HandlingUnit, Delivery>(e, h => h.DeliveryId);
        });
        model.Entity<HandlingUnitLine>(e =>
        {
            Fk<HandlingUnitLine, HandlingUnit>(e, l => l.HandlingUnitId);
            Fk<HandlingUnitLine, Lot>(e, l => l.LotId);
            Fk<HandlingUnitLine, DeliveryLine>(e, l => l.DeliveryLineId);
        });

        model.Entity<InventoryMovement>(e =>
        {
            e.HasIndex(m => new { m.OrganizationId, m.OperationId });
            Fk<InventoryMovement, Lot>(e, m => m.LotId);
            Fk<InventoryMovement, Location>(e, m => m.FromLocationId);
            Fk<InventoryMovement, Location>(e, m => m.ToLocationId);
            Fk<InventoryMovement, ImportBatch>(e, m => m.ImportBatchId);
            Fk<InventoryMovement, Account>(e, m => m.ActorAccountId);
            e.ToTable(t => t.HasCheckConstraint("CK_InventoryMovement_Positive", "\"Quantity\" > 0 AND (\"FromLocationId\" IS NOT NULL OR \"ToLocationId\" IS NOT NULL) AND (\"FromLocationId\" IS NULL OR \"ToLocationId\" IS NULL OR \"FromLocationId\" <> \"ToLocationId\")"));
        });
        model.Entity<StockBalance>(e =>
        {
            e.HasIndex(b => new { b.LocationId, b.LotId }).IsUnique();
            Fk<StockBalance, Location>(e, b => b.LocationId);
            Fk<StockBalance, Lot>(e, b => b.LotId);
            e.ToTable(t => t.HasCheckConstraint("CK_StockBalance_NonNegative", "\"QuantityOnHand\" >= 0 AND \"QuantityReserved\" >= 0"));
        });
        model.Entity<Consumption>(e =>
        {
            e.HasIndex(c => new { c.ServicePointId, c.ProductId, c.OccurredOn }).IsUnique();
            Fk<Consumption, ServicePoint>(e, c => c.ServicePointId);
            Fk<Consumption, Product>(e, c => c.ProductId);
            Fk<Consumption, ImportBatch>(e, c => c.ImportBatchId);
            e.ToTable(t => t.HasCheckConstraint("CK_Consumption_Valid", "\"PeriodDays\" >= 1 AND \"Quantity\" >= 0"));
        });

        model.Entity<RiskSnapshot>(e =>
        {
            e.HasIndex(r => new { r.ServicePointId, r.ProductId, r.CalculatedAtUtc });
            Fk<RiskSnapshot, ServicePoint>(e, r => r.ServicePointId);
            Fk<RiskSnapshot, Product>(e, r => r.ProductId);
        });
        model.Entity<Incident>(e =>
        {
            e.Property(i => i.Position).HasColumnType(PointType);
            Fk<Incident, ServicePoint>(e, i => i.ServicePointId);
            Fk<Incident, RoutePlan>(e, i => i.RoutePlanId);
        });

        model.Entity<RoutePlan>(e =>
        {
            e.HasIndex(r => new { r.OrganizationId, r.Code, r.Revision }).IsUnique();
            Fk<RoutePlan, Vehicle>(e, r => r.VehicleId);
            Fk<RoutePlan, RoutePlan>(e, r => r.SupersedesRoutePlanId);
        });
        model.Entity<RouteStop>(e =>
        {
            e.HasIndex(s => new { s.RoutePlanId, s.SequenceNumber }).IsUnique();
            Fk<RouteStop, RoutePlan>(e, s => s.RoutePlanId);
            Fk<RouteStop, ServicePoint>(e, s => s.ServicePointId);
            Fk<RouteStop, Location>(e, s => s.LocationId);
        });
        model.Entity<RouteStopLine>(e =>
        {
            Fk<RouteStopLine, RouteStop>(e, l => l.RouteStopId);
            Fk<RouteStopLine, Product>(e, l => l.ProductId);
            Fk<RouteStopLine, Lot>(e, l => l.LotId);
            Fk<RouteStopLine, DeliveryOrder>(e, l => l.DeliveryOrderId);
        });
        model.Entity<DeliveryOrder>(e =>
        {
            e.HasIndex(o => new { o.OrganizationId, o.OrderNumber }).IsUnique();
            Fk<DeliveryOrder, ServicePoint>(e, o => o.DestinationServicePointId);
            Fk<DeliveryOrder, Location>(e, o => o.DestinationLocationId);
            Fk<DeliveryOrder, TransferSuggestion>(e, o => o.TransferSuggestionId);
        });
        model.Entity<DeliveryOrderLine>(e =>
        {
            Fk<DeliveryOrderLine, DeliveryOrder>(e, l => l.DeliveryOrderId);
            Fk<DeliveryOrderLine, Product>(e, l => l.ProductId);
        });
        model.Entity<WarehouseStockPolicy>(e =>
        {
            e.HasIndex(p => new { p.OrganizationId, p.LocationId, p.ProductId }).IsUnique();
            Fk<WarehouseStockPolicy, Location>(e, p => p.LocationId);
            Fk<WarehouseStockPolicy, Product>(e, p => p.ProductId);
        });
        model.Entity<TransferSuggestion>(e =>
        {
            e.HasIndex(t => new { t.OrganizationId, t.Status });
            Fk<TransferSuggestion, Location>(e, t => t.SourceLocationId);
            Fk<TransferSuggestion, Location>(e, t => t.TargetLocationId);
            Fk<TransferSuggestion, Product>(e, t => t.ProductId);
            Fk<TransferSuggestion, Account>(e, t => t.ReviewedByAccountId);
        });
        model.Entity<Delivery>(e =>
        {
            e.HasIndex(d => new { d.OrganizationId, d.Code }).IsUnique();
            Fk<Delivery, RouteStop>(e, d => d.RouteStopId);
            Fk<Delivery, DeliveryOrder>(e, d => d.DeliveryOrderId);
            Fk<Delivery, Location>(e, d => d.OriginLocationId);
            Fk<Delivery, ServicePoint>(e, d => d.ServicePointId);
            Fk<Delivery, Location>(e, d => d.DestinationLocationId);
            Fk<Delivery, Account>(e, d => d.AssignedAccountId);
        });
        model.Entity<DeliveryLine>(e =>
        {
            Fk<DeliveryLine, Delivery>(e, l => l.DeliveryId);
            Fk<DeliveryLine, Lot>(e, l => l.LotId);
            Fk<DeliveryLine, HandlingUnit>(e, l => l.HandlingUnitId);
        });
        model.Entity<Receipt>(e =>
        {
            e.HasIndex(r => new { r.OrganizationId, r.OperationId }).IsUnique();
            Fk<Receipt, Delivery>(e, r => r.DeliveryId);
            Fk<Receipt, Account>(e, r => r.ActorAccountId);
        });
        model.Entity<ReceiptLine>(e =>
        {
            Fk<ReceiptLine, Receipt>(e, l => l.ReceiptId);
            Fk<ReceiptLine, DeliveryLine>(e, l => l.DeliveryLineId);
        });
        model.Entity<Evidence>(e => Fk<Evidence, Receipt>(e, x => x.ReceiptId));
        model.Entity<CustodyEvent>(e =>
        {
            // #162: una operación de custodia genera un evento por bulto; el índice no puede ser único.
            e.HasIndex(c => new { c.OrganizationId, c.OperationId });
            Fk<CustodyEvent, HandlingUnit>(e, c => c.HandlingUnitId);
            Fk<CustodyEvent, Account>(e, c => c.ActorAccountId);
        });

        model.Entity<QrAccessToken>(e =>
        {
            e.HasIndex(t => new { t.OrganizationId, t.TokenHash }).IsUnique();
            Fk<QrAccessToken, HandlingUnit>(e, t => t.HandlingUnitId);
            Fk<QrAccessToken, Account>(e, t => t.CreatedByAccountId);
        });

        model.Entity<IdempotencyRecord>(e => { e.HasIndex(r => new { r.OrganizationId, r.AccountId, r.OperationId }).IsUnique(); Fk<IdempotencyRecord, Account>(e, r => r.AccountId); });
        model.Entity<ImportBatch>(e => Fk<ImportBatch, Account>(e, b => b.CreatedByAccountId));
        model.Entity<AgentRun>(e => e.HasIndex(r => new { r.OrganizationId, r.StartedAtUtc }));
        model.Entity<AgentDecision>(e =>
        {
            e.HasIndex(d => new { d.OrganizationId, d.Status });
            e.Property(d => d.DriverMessage).HasMaxLength(160);
        });
        model.Entity<SimulationRun>(e =>
        {
            e.HasIndex(r => new { r.OrganizationId, r.ComparisonId });
            e.HasIndex(r => new { r.OrganizationId, r.Seed, r.ScenarioCode, r.PolicyCode });
        });
        model.Entity<SimulationMetric>(e => Fk<SimulationMetric, SimulationRun>(e, m => m.SimulationRunId));
        model.Entity<SimulationEvent>(e =>
        {
            e.HasIndex(ev => new { ev.SimulationRunId, ev.Sequence }).IsUnique();
            Fk<SimulationEvent, SimulationRun>(e, ev => ev.SimulationRunId);
        });

        model.Entity<RoadMatrixEdge>(e =>
        {
            e.HasIndex(r => new { r.OrganizationId, r.FromLat, r.FromLon, r.ToLat, r.ToLon }).IsUnique();
            e.Property(r => r.FromLat).HasPrecision(9, 5);
            e.Property(r => r.FromLon).HasPrecision(9, 5);
            e.Property(r => r.ToLat).HasPrecision(9, 5);
            e.Property(r => r.ToLon).HasPrecision(9, 5);
            e.Property(r => r.DistanceKm).HasPrecision(18, 3);
            e.Property(r => r.DurationMinutes).HasPrecision(18, 3);
        });

        model.Entity<RouteStockReservation>(e =>
        {
            e.HasIndex(r => new { r.OrganizationId, r.RoutePlanId, r.DeliveryLineId, r.LotId }).IsUnique();
            e.HasIndex(r => new { r.OrganizationId, r.StockBalanceId, r.Status });
            e.Property(r => r.Quantity).HasPrecision(18, 3);
            Fk<RouteStockReservation, RoutePlan>(e, r => r.RoutePlanId);
            Fk<RouteStockReservation, Delivery>(e, r => r.DeliveryId);
            Fk<RouteStockReservation, DeliveryLine>(e, r => r.DeliveryLineId);
            Fk<RouteStockReservation, Lot>(e, r => r.LotId);
            Fk<RouteStockReservation, StockBalance>(e, r => r.StockBalanceId);
        });

        foreach (var entity in model.Model.GetEntityTypes().ToList())
        {
            entity.SetTableName(entity.ClrType.Name);
            model.Entity(entity.ClrType).Property("Version").IsConcurrencyToken();
            if (typeof(OrganizationEntity).IsAssignableFrom(entity.ClrType))
                {
                model.Entity(entity.ClrType).HasAlternateKey("OrganizationId", "Id");
                model.Entity(entity.ClrType).HasOne(typeof(Organization)).WithMany()
                    .HasForeignKey(nameof(OrganizationEntity.OrganizationId)).OnDelete(DeleteBehavior.NoAction);
            }
            foreach (var property in entity.GetProperties())
            {
                if (property.Name.EndsWith("Json", StringComparison.Ordinal)) { property.SetColumnType("jsonb"); property.SetMaxLength(null); }
                else if (LongText.Contains(property.Name)) { property.SetColumnType("text"); property.SetMaxLength(null); }
            }
        }
    }

    // NO ACTION (no RESTRICT): PostgreSQL valida al final de la sentencia, lo que permite borrar cadenas de revisiones en bloque.
    private static void Fk<TEntity, TPrincipal>(EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, object?>> key)
        where TEntity : class where TPrincipal : class
        {
        var member = key.Body is UnaryExpression unary ? (MemberExpression)unary.Operand : (MemberExpression)key.Body;
        if (typeof(OrganizationEntity).IsAssignableFrom(typeof(TPrincipal)))
            entity.HasOne<TPrincipal>().WithMany().HasForeignKey("OrganizationId", member.Member.Name)
                .HasPrincipalKey("OrganizationId", "Id").OnDelete(DeleteBehavior.NoAction);
        else entity.HasOne<TPrincipal>().WithMany().HasForeignKey(key).OnDelete(DeleteBehavior.NoAction);
    }
}
