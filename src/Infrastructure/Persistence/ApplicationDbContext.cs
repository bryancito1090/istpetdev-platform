using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleFieldPermission> RoleFieldPermissions => Set<RoleFieldPermission>();
    public DbSet<OrganizationAccess> OrganizationAccesses => Set<OrganizationAccess>();
    public DbSet<AccessRole> AccessRoles => Set<AccessRole>();
    public DbSet<AccessLocation> AccessLocations => Set<AccessLocation>();
    public DbSet<AccessServicePoint> AccessServicePoints => Set<AccessServicePoint>();
    public DbSet<AccessDelegation> AccessDelegations => Set<AccessDelegation>();
    public DbSet<RbacChange> RbacChanges => Set<RbacChange>();
    public DbSet<ConfigurationDefinition> ConfigurationDefinitions => Set<ConfigurationDefinition>();
    public DbSet<ConfigurationVersion> ConfigurationVersions => Set<ConfigurationVersion>();
    public DbSet<ConfigurationActivation> ConfigurationActivations => Set<ConfigurationActivation>();
    public DbSet<Catalog> Catalogs => Set<Catalog>();
    public DbSet<CatalogOption> CatalogOptions => Set<CatalogOption>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<TemplateField> TemplateFields => Set<TemplateField>();
    public DbSet<TemplateFieldOption> TemplateFieldOptions => Set<TemplateFieldOption>();
    public DbSet<TemplateFieldAccess> TemplateFieldAccesss => Set<TemplateFieldAccess>();
    public DbSet<TemplateBinding> TemplateBindings => Set<TemplateBinding>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<FormAttachment> FormAttachments => Set<FormAttachment>();
    public DbSet<ServicePoint> ServicePoints => Set<ServicePoint>();
    public DbSet<ServicePointWindow> ServicePointWindows => Set<ServicePointWindow>();
    public DbSet<ServicePointContact> ServicePointContacts => Set<ServicePointContact>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleAvailability> VehicleAvailabilities => Set<VehicleAvailability>();
    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductUnitConversion> ProductUnitConversions => Set<ProductUnitConversion>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<HandlingUnit> HandlingUnits => Set<HandlingUnit>();
    public DbSet<HandlingUnitLine> HandlingUnitLines => Set<HandlingUnitLine>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<InventorySnapshot> InventorySnapshots => Set<InventorySnapshot>();
    public DbSet<InventorySnapshotLine> InventorySnapshotLines => Set<InventorySnapshotLine>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Consumption> Consumptions => Set<Consumption>();
    public DbSet<DemandObservation> DemandObservations => Set<DemandObservation>();
    public DbSet<ReplenishmentRequest> ReplenishmentRequests => Set<ReplenishmentRequest>();
    public DbSet<ReplenishmentRequestLine> ReplenishmentRequestLines => Set<ReplenishmentRequestLine>();
    public DbSet<RoadMatrix> RoadMatrixs => Set<RoadMatrix>();
    public DbSet<RoadMatrixEdge> RoadMatrixEdges => Set<RoadMatrixEdge>();
    public DbSet<RoutePlan> RoutePlans => Set<RoutePlan>();
    public DbSet<RoutePlanningSnapshot> RoutePlanningSnapshots => Set<RoutePlanningSnapshot>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryLine> DeliveryLines => Set<DeliveryLine>();
    public DbSet<DeliveryHandlingUnit> DeliveryHandlingUnits => Set<DeliveryHandlingUnit>();
    public DbSet<DeliveryHandlingUnitLine> DeliveryHandlingUnitLines => Set<DeliveryHandlingUnitLine>();
    public DbSet<DeliveryAssignment> DeliveryAssignments => Set<DeliveryAssignment>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<Evidence> Evidences => Set<Evidence>();
    public DbSet<CustodyEvent> CustodyEvents => Set<CustodyEvent>();
    public DbSet<QrAccessToken> QrAccessTokens => Set<QrAccessToken>();
    public DbSet<RiskCalculationRun> RiskCalculationRuns => Set<RiskCalculationRun>();
    public DbSet<RiskSnapshot> RiskSnapshots => Set<RiskSnapshot>();
    public DbSet<RiskExplanation> RiskExplanations => Set<RiskExplanation>();
    public DbSet<AnomalyCase> AnomalyCases => Set<AnomalyCase>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentRoadRestriction> IncidentRoadRestrictions => Set<IncidentRoadRestriction>();
    public DbSet<RoutePlanIncident> RoutePlanIncidents => Set<RoutePlanIncident>();
    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    public DbSet<IntegrationRun> IntegrationRuns => Set<IntegrationRun>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<SyncBatch> SyncBatchs => Set<SyncBatch>();
    public DbSet<SyncOperation> SyncOperations => Set<SyncOperation>();
    public DbSet<ScenarioDataset> ScenarioDatasets => Set<ScenarioDataset>();
    public DbSet<ScenarioDatasetVersion> ScenarioDatasetVersions => Set<ScenarioDatasetVersion>();
    public DbSet<ScenarioEvent> ScenarioEvents => Set<ScenarioEvent>();
    public DbSet<SimulationPolicyVersion> SimulationPolicyVersions => Set<SimulationPolicyVersion>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<SimulationEvent> SimulationEvents => Set<SimulationEvent>();
    public DbSet<SimulationMetric> SimulationMetrics => Set<SimulationMetric>();
    public DbSet<SimulationArtifact> SimulationArtifacts => Set<SimulationArtifact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("postgis");
        CompleteSchema.Configure(modelBuilder);
    }
}
