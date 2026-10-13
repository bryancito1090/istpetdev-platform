using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IstpetDev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DemoCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DemoCoreTransition.Preflight());
            migrationBuilder.DropForeignKey(
                name: "FK_Consumption_Account_ActorAccountId",
                table: "Consumption");

            migrationBuilder.DropForeignKey(
                name: "FK_Consumption_Lot_OrganizationId_LotId",
                table: "Consumption");

            migrationBuilder.DropForeignKey(
                name: "FK_Consumption_Organization_OrganizationId",
                table: "Consumption");

            migrationBuilder.DropForeignKey(
                name: "FK_Consumption_ServicePoint_OrganizationId_ServicePointId",
                table: "Consumption");

            migrationBuilder.DropForeignKey(
                name: "FK_CustodyEvent_Account_ActorAccountId",
                table: "CustodyEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_CustodyEvent_Delivery_OrganizationId_DeliveryId",
                table: "CustodyEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_CustodyEvent_HandlingUnit_OrganizationId_HandlingUnitId",
                table: "CustodyEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_CustodyEvent_Location_OrganizationId_FromLocationId",
                table: "CustodyEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_CustodyEvent_Location_OrganizationId_ToLocationId",
                table: "CustodyEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_CustodyEvent_Organization_OrganizationId",
                table: "CustodyEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_CustodyEvent_Receipt_OrganizationId_ReceiptId",
                table: "CustodyEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Location_OrganizationId_DestinationLocationId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Location_OrganizationId_OriginLocationId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_Organization_OrganizationId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_RouteStop_OrganizationId_RouteStopId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_ServicePoint_OrganizationId_ServicePointId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_WorkflowState_OrganizationId_StateId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_WorkflowState_OrganizationId_WorkflowVersionId_Sta~",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_Delivery_WorkflowVersion_OrganizationId_WorkflowVersionId",
                table: "Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryLine_Delivery_OrganizationId_DeliveryId",
                table: "DeliveryLine");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryLine_Lot_OrganizationId_LotId",
                table: "DeliveryLine");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryLine_Organization_OrganizationId",
                table: "DeliveryLine");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryLine_ReplenishmentRequestLine_OrganizationId_Replen~",
                table: "DeliveryLine");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidence_Account_UploadedByAccountId",
                table: "Evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidence_Organization_OrganizationId",
                table: "Evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidence_ReceiptLine_OrganizationId_ReceiptLineId",
                table: "Evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidence_Receipt_OrganizationId_ReceiptId",
                table: "Evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_Evidence_StoredFile_OrganizationId_StoredFileId",
                table: "Evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_HandlingUnit_Location_OrganizationId_LocationId",
                table: "HandlingUnit");

            migrationBuilder.DropForeignKey(
                name: "FK_HandlingUnit_Organization_OrganizationId",
                table: "HandlingUnit");

            migrationBuilder.DropForeignKey(
                name: "FK_HandlingUnitLine_HandlingUnit_OrganizationId_HandlingUnitId",
                table: "HandlingUnitLine");

            migrationBuilder.DropForeignKey(
                name: "FK_HandlingUnitLine_Lot_OrganizationId_LotId",
                table: "HandlingUnitLine");

            migrationBuilder.DropForeignKey(
                name: "FK_HandlingUnitLine_Organization_OrganizationId",
                table: "HandlingUnitLine");

            migrationBuilder.DropForeignKey(
                name: "FK_IdempotencyRecord_Account_AccountId",
                table: "IdempotencyRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_IdempotencyRecord_Organization_OrganizationId",
                table: "IdempotencyRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_Incident_Account_ReportedByAccountId",
                table: "Incident");

            migrationBuilder.DropForeignKey(
                name: "FK_Incident_CatalogOption_OrganizationId_SeverityOptionId",
                table: "Incident");

            migrationBuilder.DropForeignKey(
                name: "FK_Incident_Organization_OrganizationId",
                table: "Incident");

            migrationBuilder.DropForeignKey(
                name: "FK_Incident_RoutePlan_OrganizationId_RoutePlanId",
                table: "Incident");

            migrationBuilder.DropForeignKey(
                name: "FK_Incident_ScenarioEvent_OrganizationId_ScenarioEventId",
                table: "Incident");

            migrationBuilder.DropForeignKey(
                name: "FK_Incident_ServicePoint_OrganizationId_ServicePointId",
                table: "Incident");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovement_Account_ActorAccountId",
                table: "InventoryMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovement_Consumption_OrganizationId_ConsumptionId",
                table: "InventoryMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovement_DeliveryLine_OrganizationId_DeliveryLineId",
                table: "InventoryMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovement_Location_OrganizationId_FromLocationId",
                table: "InventoryMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovement_Location_OrganizationId_ToLocationId",
                table: "InventoryMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovement_Lot_OrganizationId_LotId",
                table: "InventoryMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovement_Organization_OrganizationId",
                table: "InventoryMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovement_ReceiptLine_OrganizationId_ReceiptLineId",
                table: "InventoryMovement");

            migrationBuilder.DropForeignKey(
                name: "FK_Location_Organization_OrganizationId",
                table: "Location");

            migrationBuilder.DropForeignKey(
                name: "FK_Location_ServicePoint_OrganizationId_ServicePointId",
                table: "Location");

            migrationBuilder.DropForeignKey(
                name: "FK_Location_Vehicle_OrganizationId_VehicleId",
                table: "Location");

            migrationBuilder.DropForeignKey(
                name: "FK_Lot_CatalogOption_OrganizationId_ConditionOptionId",
                table: "Lot");

            migrationBuilder.DropForeignKey(
                name: "FK_Lot_Organization_OrganizationId",
                table: "Lot");

            migrationBuilder.DropForeignKey(
                name: "FK_Lot_Product_OrganizationId_ProductId",
                table: "Lot");

            migrationBuilder.DropForeignKey(
                name: "FK_Product_Organization_OrganizationId",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipt_Account_ActorAccountId",
                table: "Receipt");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipt_Delivery_OrganizationId_DeliveryId",
                table: "Receipt");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipt_Organization_OrganizationId",
                table: "Receipt");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipt_WorkflowState_OrganizationId_StateId",
                table: "Receipt");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipt_WorkflowState_OrganizationId_WorkflowVersionId_Stat~",
                table: "Receipt");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipt_WorkflowVersion_OrganizationId_WorkflowVersionId",
                table: "Receipt");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptLine_CatalogOption_OrganizationId_RejectionReasonOpt~",
                table: "ReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptLine_DeliveryLine_OrganizationId_DeliveryLineId",
                table: "ReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptLine_Location_OrganizationId_ReturnLocationId",
                table: "ReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptLine_Organization_OrganizationId",
                table: "ReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceiptLine_Receipt_OrganizationId_ReceiptId",
                table: "ReceiptLine");

            migrationBuilder.DropForeignKey(
                name: "FK_RiskSnapshot_Organization_OrganizationId",
                table: "RiskSnapshot");

            migrationBuilder.DropForeignKey(
                name: "FK_RiskSnapshot_Product_OrganizationId_ProductId",
                table: "RiskSnapshot");

            migrationBuilder.DropForeignKey(
                name: "FK_RiskSnapshot_RiskCalculationRun_OrganizationId_RiskCalculat~",
                table: "RiskSnapshot");

            migrationBuilder.DropForeignKey(
                name: "FK_RiskSnapshot_ServicePoint_OrganizationId_ServicePointId",
                table: "RiskSnapshot");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_Account_PublishedByAccountId",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_ConfigurationVersion_OrganizationId_Configuration~",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_Organization_OrganizationId",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_RoadMatrix_OrganizationId_RoadMatrixId",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_RoutePlan_OrganizationId_SupersedesRoutePlanId",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_Vehicle_OrganizationId_VehicleId",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_WorkflowState_OrganizationId_StateId",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_WorkflowState_OrganizationId_WorkflowVersionId_St~",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutePlan_WorkflowVersion_OrganizationId_WorkflowVersionId",
                table: "RoutePlan");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteStop_Location_OrganizationId_LocationId",
                table: "RouteStop");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteStop_Organization_OrganizationId",
                table: "RouteStop");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteStop_RoutePlan_OrganizationId_RoutePlanId",
                table: "RouteStop");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteStop_ServicePoint_OrganizationId_ServicePointId",
                table: "RouteStop");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePoint_CatalogOption_OrganizationId_TypeOptionId",
                table: "ServicePoint");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePoint_ConfigurationVersion_OrganizationId_Configurat~",
                table: "ServicePoint");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePoint_Organization_OrganizationId",
                table: "ServicePoint");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationMetric_Organization_OrganizationId",
                table: "SimulationMetric");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationMetric_Product_OrganizationId_ProductId",
                table: "SimulationMetric");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationMetric_SimulationRun_OrganizationId_SimulationRun~",
                table: "SimulationMetric");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationRun_JobExecution_OrganizationId_JobExecutionId",
                table: "SimulationRun");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationRun_Organization_OrganizationId",
                table: "SimulationRun");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationRun_RoadMatrix_OrganizationId_RoadMatrixId",
                table: "SimulationRun");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationRun_ScenarioDatasetVersion_OrganizationId_Scenari~",
                table: "SimulationRun");

            migrationBuilder.DropForeignKey(
                name: "FK_SimulationRun_SimulationPolicyVersion_OrganizationId_Simula~",
                table: "SimulationRun");

            migrationBuilder.DropForeignKey(
                name: "FK_StockBalance_Location_OrganizationId_LocationId",
                table: "StockBalance");

            migrationBuilder.DropForeignKey(
                name: "FK_StockBalance_Lot_OrganizationId_LotId",
                table: "StockBalance");

            migrationBuilder.DropForeignKey(
                name: "FK_StockBalance_Organization_OrganizationId",
                table: "StockBalance");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicle_Organization_OrganizationId",
                table: "Vehicle");

            migrationBuilder.DropTable(
                name: "AccessDelegation");

            migrationBuilder.DropTable(
                name: "AccessLocation");

            migrationBuilder.DropTable(
                name: "AccessRole");

            migrationBuilder.DropTable(
                name: "AccessServicePoint");

            migrationBuilder.DropTable(
                name: "AnomalyCase");

            migrationBuilder.DropTable(
                name: "AuditEvent");

            migrationBuilder.DropTable(
                name: "CatalogOption");

            migrationBuilder.DropTable(
                name: "ConfigurationActivation");

            migrationBuilder.DropTable(
                name: "ConversationParticipant");

            migrationBuilder.DropTable(
                name: "DeliveryAssignment");

            migrationBuilder.DropTable(
                name: "DeliveryHandlingUnitLine");

            migrationBuilder.DropTable(
                name: "DemandObservation");

            migrationBuilder.DropTable(
                name: "FormAttachment");

            migrationBuilder.DropTable(
                name: "IncidentRoadRestriction");

            migrationBuilder.DropTable(
                name: "InventorySnapshotLine");

            migrationBuilder.DropTable(
                name: "MessageAttachment");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "ProductUnitConversion");

            migrationBuilder.DropTable(
                name: "QrAccessToken");

            migrationBuilder.DropTable(
                name: "RbacChange");

            migrationBuilder.DropTable(
                name: "ReplenishmentRequestLine");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "RiskCalculationRun");

            migrationBuilder.DropTable(
                name: "RiskExplanation");

            migrationBuilder.DropTable(
                name: "RoleFieldPermission");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "RoutePlanIncident");

            migrationBuilder.DropTable(
                name: "RoutePlanningSnapshot");

            migrationBuilder.DropTable(
                name: "ScenarioEvent");

            migrationBuilder.DropTable(
                name: "ServicePointContact");

            migrationBuilder.DropTable(
                name: "ServicePointWindow");

            migrationBuilder.DropTable(
                name: "SimulationArtifact");

            migrationBuilder.DropTable(
                name: "SimulationEvent");

            migrationBuilder.DropTable(
                name: "SimulationPolicyVersion");

            migrationBuilder.DropTable(
                name: "SyncOperation");

            migrationBuilder.DropTable(
                name: "TemplateBinding");

            migrationBuilder.DropTable(
                name: "TemplateFieldAccess");

            migrationBuilder.DropTable(
                name: "TemplateFieldOption");

            migrationBuilder.DropTable(
                name: "VehicleAvailability");

            migrationBuilder.DropTable(
                name: "VehiclePosition");

            migrationBuilder.DropTable(
                name: "WorkflowTransition");

            migrationBuilder.DropTable(
                name: "DeliveryHandlingUnit");

            migrationBuilder.DropTable(
                name: "FormSubmission");

            migrationBuilder.DropTable(
                name: "RoadMatrixEdge");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "OutboxEvent");

            migrationBuilder.DropTable(
                name: "OrganizationAccess");

            migrationBuilder.DropTable(
                name: "ReplenishmentRequest");

            migrationBuilder.DropTable(
                name: "InventorySnapshot");

            migrationBuilder.DropTable(
                name: "IntegrationRun");

            migrationBuilder.DropTable(
                name: "FieldDefinition");

            migrationBuilder.DropTable(
                name: "SyncBatch");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "TemplateField");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "RoadMatrix");

            migrationBuilder.DropTable(
                name: "Conversation");

            migrationBuilder.DropTable(
                name: "WorkflowState");

            migrationBuilder.DropTable(
                name: "IntegrationConnection");

            migrationBuilder.DropTable(
                name: "JobExecution");

            migrationBuilder.DropTable(
                name: "Catalog");

            migrationBuilder.DropTable(
                name: "TemplateVersion");

            migrationBuilder.DropTable(
                name: "ScenarioDatasetVersion");

            migrationBuilder.DropTable(
                name: "ConfigurationVersion");

            migrationBuilder.DropTable(
                name: "Template");

            migrationBuilder.DropTable(
                name: "WorkflowVersion");

            migrationBuilder.DropTable(
                name: "ScenarioDataset");

            migrationBuilder.DropTable(
                name: "StoredFile");

            migrationBuilder.DropTable(
                name: "ConfigurationDefinition");

            migrationBuilder.DropTable(
                name: "WorkflowDefinition");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Vehicle_MaxVolume",
                table: "Vehicle");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Vehicle_MaxWeight",
                table: "Vehicle");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Vehicle_Version",
                table: "Vehicle");

            migrationBuilder.DropIndex(
                name: "IX_StockBalance_OrganizationId_LocationId_LotId",
                table: "StockBalance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockBalance_QuantityOnHand",
                table: "StockBalance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockBalance_QuantityReserved",
                table: "StockBalance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockBalance_Reserved",
                table: "StockBalance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockBalance_Version",
                table: "StockBalance");

            migrationBuilder.DropIndex(
                name: "IX_SimulationRun_OrganizationId_JobExecutionId",
                table: "SimulationRun");

            migrationBuilder.DropIndex(
                name: "IX_SimulationRun_OrganizationId_RoadMatrixId",
                table: "SimulationRun");

            migrationBuilder.DropIndex(
                name: "IX_SimulationRun_OrganizationId_ScenarioDatasetVersionId",
                table: "SimulationRun");

            migrationBuilder.DropIndex(
                name: "IX_SimulationRun_OrganizationId_SimulationPolicyVersionId",
                table: "SimulationRun");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SimulationRun_Version",
                table: "SimulationRun");

            migrationBuilder.DropIndex(
                name: "IX_SimulationMetric_OrganizationId_ProductId",
                table: "SimulationMetric");

            migrationBuilder.DropIndex(
                name: "IX_SimulationMetric_OrganizationId_SimulationRunId_Code_Defin~1",
                table: "SimulationMetric");

            migrationBuilder.DropIndex(
                name: "IX_SimulationMetric_OrganizationId_SimulationRunId_Code_Defini~",
                table: "SimulationMetric");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SimulationMetric_Denominator",
                table: "SimulationMetric");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SimulationMetric_Division",
                table: "SimulationMetric");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SimulationMetric_Version",
                table: "SimulationMetric");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoint_OrganizationId_ConfigurationVersionId",
                table: "ServicePoint");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoint_OrganizationId_TypeOptionId",
                table: "ServicePoint");

            migrationBuilder.DropIndex(
                name: "IX_ServicePoint_Position",
                table: "ServicePoint");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServicePoint_Version",
                table: "ServicePoint");

            migrationBuilder.DropIndex(
                name: "IX_RouteStop_OrganizationId_LocationId",
                table: "RouteStop");

            migrationBuilder.DropIndex(
                name: "IX_RouteStop_OrganizationId_RoutePlanId_SequenceNumber",
                table: "RouteStop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RouteStop_Interval",
                table: "RouteStop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RouteStop_SequenceNumber",
                table: "RouteStop");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RouteStop_Version",
                table: "RouteStop");

            migrationBuilder.DropIndex(
                name: "IX_RoutePlan_OrganizationId_ConfigurationVersionId",
                table: "RoutePlan");

            migrationBuilder.DropIndex(
                name: "IX_RoutePlan_OrganizationId_RoadMatrixId",
                table: "RoutePlan");

            migrationBuilder.DropIndex(
                name: "IX_RoutePlan_OrganizationId_StateId",
                table: "RoutePlan");

            migrationBuilder.DropIndex(
                name: "IX_RoutePlan_OrganizationId_WorkflowVersionId_StateId",
                table: "RoutePlan");

            migrationBuilder.DropIndex(
                name: "IX_RoutePlan_PublishedByAccountId",
                table: "RoutePlan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoutePlan_Interval",
                table: "RoutePlan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoutePlan_Revision",
                table: "RoutePlan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoutePlan_Version",
                table: "RoutePlan");

            migrationBuilder.DropIndex(
                name: "IX_RiskSnapshot_OrganizationId_RiskCalculationRunId_ServicePoi~",
                table: "RiskSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_RiskSnapshot_OrganizationId_ServicePointId_ProductId_RiskCa~",
                table: "RiskSnapshot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskSnapshot_CoverageDays",
                table: "RiskSnapshot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskSnapshot_LeadTimeDays",
                table: "RiskSnapshot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskSnapshot_SafetyMarginDays",
                table: "RiskSnapshot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskSnapshot_Version",
                table: "RiskSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptLine_OrganizationId_ReceiptId_DeliveryLineId",
                table: "ReceiptLine");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptLine_OrganizationId_RejectionReasonOptionId",
                table: "ReceiptLine");

            migrationBuilder.DropIndex(
                name: "IX_ReceiptLine_OrganizationId_ReturnLocationId",
                table: "ReceiptLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReceiptLine_Quantity",
                table: "ReceiptLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReceiptLine_QuantityAccepted",
                table: "ReceiptLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReceiptLine_QuantityRejected",
                table: "ReceiptLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReceiptLine_Version",
                table: "ReceiptLine");

            migrationBuilder.DropIndex(
                name: "IX_Receipt_ActorAccountId",
                table: "Receipt");

            migrationBuilder.DropIndex(
                name: "IX_Receipt_OrganizationId_ActorAccountId_OperationId",
                table: "Receipt");

            migrationBuilder.DropIndex(
                name: "IX_Receipt_OrganizationId_StateId",
                table: "Receipt");

            migrationBuilder.DropIndex(
                name: "IX_Receipt_OrganizationId_WorkflowVersionId_StateId",
                table: "Receipt");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Receipt_Version",
                table: "Receipt");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Version",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_VolumePerBaseUnit",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_WeightPerBaseUnit",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Organization_Version",
                table: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_Lot_OrganizationId_ConditionOptionId",
                table: "Lot");

            migrationBuilder.DropIndex(
                name: "IX_Lot_OrganizationId_ProductId_Code",
                table: "Lot");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Lot_Version",
                table: "Lot");

            migrationBuilder.DropIndex(
                name: "IX_Location_OrganizationId_Code",
                table: "Location");

            migrationBuilder.DropIndex(
                name: "IX_Location_OrganizationId_VehicleId",
                table: "Location");

            migrationBuilder.DropIndex(
                name: "IX_Location_Position",
                table: "Location");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Location_Kind",
                table: "Location");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Location_Version",
                table: "Location");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovement_ActorAccountId",
                table: "InventoryMovement");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovement_OrganizationId_DeliveryLineId",
                table: "InventoryMovement");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovement_OrganizationId_LotId_ReceivedAtServer",
                table: "InventoryMovement");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovement_OrganizationId_ReceiptLineId",
                table: "InventoryMovement");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovement_OrganizationId_RecordedSequence",
                table: "InventoryMovement");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryMovement_Direction",
                table: "InventoryMovement");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryMovement_Quantity",
                table: "InventoryMovement");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryMovement_Version",
                table: "InventoryMovement");

            migrationBuilder.DropIndex(
                name: "IX_Incident_OrganizationId_ScenarioEventId",
                table: "Incident");

            migrationBuilder.DropIndex(
                name: "IX_Incident_OrganizationId_SeverityOptionId",
                table: "Incident");

            migrationBuilder.DropIndex(
                name: "IX_Incident_Position",
                table: "Incident");

            migrationBuilder.DropIndex(
                name: "IX_Incident_ReportedByAccountId",
                table: "Incident");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Incident_Version",
                table: "Incident");

            migrationBuilder.DropIndex(
                name: "IX_IdempotencyRecord_AccountId",
                table: "IdempotencyRecord");

            migrationBuilder.DropIndex(
                name: "IX_IdempotencyRecord_OrganizationId_AccountId_ActionCode_Opera~",
                table: "IdempotencyRecord");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdempotencyRecord_Version",
                table: "IdempotencyRecord");

            migrationBuilder.DropIndex(
                name: "IX_HandlingUnitLine_OrganizationId_HandlingUnitId_LotId",
                table: "HandlingUnitLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HandlingUnitLine_Quantity",
                table: "HandlingUnitLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HandlingUnitLine_Version",
                table: "HandlingUnitLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HandlingUnit_Version",
                table: "HandlingUnit");

            migrationBuilder.DropIndex(
                name: "IX_Evidence_OrganizationId_ReceiptId_StoredFileId",
                table: "Evidence");

            migrationBuilder.DropIndex(
                name: "IX_Evidence_OrganizationId_ReceiptLineId",
                table: "Evidence");

            migrationBuilder.DropIndex(
                name: "IX_Evidence_OrganizationId_StoredFileId",
                table: "Evidence");

            migrationBuilder.DropIndex(
                name: "IX_Evidence_UploadedByAccountId",
                table: "Evidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Evidence_Version",
                table: "Evidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeliveryLine_AcceptedQuantity",
                table: "DeliveryLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeliveryLine_DispatchedQuantity",
                table: "DeliveryLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeliveryLine_PlannedQuantity",
                table: "DeliveryLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeliveryLine_RejectedQuantity",
                table: "DeliveryLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeliveryLine_Totals",
                table: "DeliveryLine");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeliveryLine_Version",
                table: "DeliveryLine");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_DestinationLocationId",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_StateId",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_OrganizationId_WorkflowVersionId_StateId",
                table: "Delivery");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Delivery_Locations",
                table: "Delivery");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Delivery_Version",
                table: "Delivery");

            migrationBuilder.DropIndex(
                name: "IX_CustodyEvent_ActorAccountId",
                table: "CustodyEvent");

            migrationBuilder.DropIndex(
                name: "IX_CustodyEvent_OrganizationId_DeliveryId",
                table: "CustodyEvent");

            migrationBuilder.DropIndex(
                name: "IX_CustodyEvent_OrganizationId_FromLocationId",
                table: "CustodyEvent");

            migrationBuilder.DropIndex(
                name: "IX_CustodyEvent_OrganizationId_ReceiptId",
                table: "CustodyEvent");

            migrationBuilder.DropIndex(
                name: "IX_CustodyEvent_OrganizationId_ToLocationId",
                table: "CustodyEvent");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CustodyEvent_Version",
                table: "CustodyEvent");

            migrationBuilder.DropIndex(
                name: "IX_Consumption_ActorAccountId",
                table: "Consumption");

            migrationBuilder.DropIndex(
                name: "IX_Consumption_OrganizationId_ActorAccountId_OperationId",
                table: "Consumption");

            migrationBuilder.DropIndex(
                name: "IX_Consumption_OrganizationId_LotId",
                table: "Consumption");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Consumption_Quantity",
                table: "Consumption");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Consumption_Version",
                table: "Consumption");

            migrationBuilder.DropIndex(
                name: "IX_Account_Issuer_ClientId",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_Issuer_Subject",
                table: "Account");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Account_Kind",
                table: "Account");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Account_Version",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "RegistrationCiphertext",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "LastMovementSequence",
                table: "StockBalance");

            migrationBuilder.DropColumn(
                name: "InputHash",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "JobExecutionId",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "RoadMatrixId",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "ScenarioDatasetVersionId",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "SimulationPolicyVersionId",
                table: "SimulationRun");

            migrationBuilder.DropColumn(
                name: "DefinitionVersion",
                table: "SimulationMetric");

            migrationBuilder.DropColumn(
                name: "ConfigurationVersionId",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "TypeOptionId",
                table: "ServicePoint");

            migrationBuilder.DropColumn(
                name: "ActualDepartureUtc",
                table: "RouteStop");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "RouteStop");

            migrationBuilder.DropColumn(
                name: "ConfigurationVersionId",
                table: "RoutePlan");

            migrationBuilder.DropColumn(
                name: "RoadMatrixId",
                table: "RoutePlan");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "RoutePlan");

            migrationBuilder.DropColumn(
                name: "WorkflowVersionId",
                table: "RoutePlan");

            migrationBuilder.DropColumn(
                name: "RiskCalculationRunId",
                table: "RiskSnapshot");

            migrationBuilder.DropColumn(
                name: "RejectionReasonOptionId",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "ReturnLocationId",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "AcceptedAtServer",
                table: "Receipt");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "Receipt");

            migrationBuilder.DropColumn(
                name: "WorkflowVersionId",
                table: "Receipt");

            migrationBuilder.DropColumn(
                name: "AuthorizationVersion",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "ConditionOptionId",
                table: "Lot");

            migrationBuilder.DropColumn(
                name: "OccurredAtDevice",
                table: "InventoryMovement");

            migrationBuilder.DropColumn(
                name: "RecordedSequence",
                table: "InventoryMovement");

            migrationBuilder.DropColumn(
                name: "OccurredAtDevice",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "ScenarioEventId",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "SeverityOptionId",
                table: "Incident");

            migrationBuilder.DropColumn(
                name: "RetainUntilUtc",
                table: "IdempotencyRecord");

            migrationBuilder.DropColumn(
                name: "ReceiptLineId",
                table: "Evidence");

            migrationBuilder.DropColumn(
                name: "StoredFileId",
                table: "Evidence");

            migrationBuilder.DropColumn(
                name: "PackagingSnapshotJson",
                table: "DeliveryLine");

            migrationBuilder.DropColumn(
                name: "DestinationLocationId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "WorkflowVersionId",
                table: "Delivery");

            migrationBuilder.DropColumn(
                name: "ActorAccountId",
                table: "Consumption");

            migrationBuilder.DropColumn(
                name: "LotId",
                table: "Consumption");

            migrationBuilder.DropColumn(
                name: "OccurredAtDevice",
                table: "Consumption");

            migrationBuilder.DropColumn(
                name: "ReceivedAtServer",
                table: "Consumption");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Account");

            migrationBuilder.RenameColumn(
                name: "ReceivedAtServer",
                table: "InventoryMovement",
                newName: "OccurredAtUtc");

            migrationBuilder.RenameColumn(
                name: "ConsumptionId",
                table: "InventoryMovement",
                newName: "ImportBatchId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryMovement_OrganizationId_ConsumptionId",
                table: "InventoryMovement",
                newName: "IX_InventoryMovement_OrganizationId_ImportBatchId");

            migrationBuilder.RenameColumn(
                name: "ReceivedAtServer",
                table: "Incident",
                newName: "OccurredAtUtc");

            migrationBuilder.RenameColumn(
                name: "ReplenishmentRequestLineId",
                table: "DeliveryLine",
                newName: "HandlingUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryLine_OrganizationId_ReplenishmentRequestLineId",
                table: "DeliveryLine",
                newName: "IX_DeliveryLine_OrganizationId_HandlingUnitId");

            migrationBuilder.RenameColumn(
                name: "OperationId",
                table: "Consumption",
                newName: "ProductId");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Vehicle",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxWeight",
                table: "Vehicle",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxVolume",
                table: "Vehicle",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Vehicle",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Vehicle",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<decimal>(
                name: "CostPerKm",
                table: "Vehicle",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepotLocationId",
                table: "Vehicle",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "StockBalance",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityReserved",
                table: "StockBalance",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityOnHand",
                table: "StockBalance",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "StockBalance",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "SimulationRun",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "SimulationRun",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "DatasetHash",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ParametersJson",
                table: "SimulationRun",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PolicyCode",
                table: "SimulationRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "SimulationMetric",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "SimulationMetric",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "SimulationMetric",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<decimal>(
                name: "Numerator",
                table: "SimulationMetric",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "NotApplicableReason",
                table: "SimulationMetric",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Denominator",
                table: "SimulationMetric",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "SimulationMetric",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "SimulationMetric",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "PolicyCode",
                table: "SimulationMetric",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "ServicePoint",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.Sql("ALTER TABLE \"ServicePoint\" ALTER COLUMN \"Position\" TYPE geometry(Point,4326) USING \"Position\"::geometry(Point,4326)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ServicePoint",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Criticality",
                table: "ServicePoint",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ServicePoint",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ServicePoint",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ServicePoint",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LeadTimeDays",
                table: "ServicePoint",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "ServicePoint",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceMinutes",
                table: "ServicePoint",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WindowEndLocal",
                table: "ServicePoint",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WindowStartLocal",
                table: "ServicePoint",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "RouteStop",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "RouteStop",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PlannedDepartureUtc",
                table: "RouteStop",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PlannedArrivalUtc",
                table: "RouteStop",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "PendingReason",
                table: "RouteStop",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "RouteStop",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "RoutePlan",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "RoutePlan",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "RoutePlan",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "AlgorithmVersion",
                table: "RoutePlan",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InputJson",
                table: "RoutePlan",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProposedBy",
                table: "RoutePlan",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "RoutePlan",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "RoutePlan",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "RoutePlan",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalKm",
                table: "RoutePlan",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalMinutes",
                table: "RoutePlan",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "RiskSnapshot",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<decimal>(
                name: "SafetyMarginDays",
                table: "RiskSnapshot",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RiskCode",
                table: "RiskSnapshot",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<decimal>(
                name: "LeadTimeDays",
                table: "RiskSnapshot",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "RiskSnapshot",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "CoverageDays",
                table: "RiskSnapshot",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableQuantity",
                table: "RiskSnapshot",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalculatedAtUtc",
                table: "RiskSnapshot",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDemand",
                table: "RiskSnapshot",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaVersion",
                table: "RiskSnapshot",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedQuantity",
                table: "RiskSnapshot",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "ReceiptLine",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityRejected",
                table: "ReceiptLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityAccepted",
                table: "ReceiptLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ReceiptLine",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ReceiptLine",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Receipt",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Receipt",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "WeightPerBaseUnit",
                table: "Product",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "VolumePerBaseUnit",
                table: "Product",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Product",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Product",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Product",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Product",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "BaseUnit",
                table: "Product",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Organization",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "TimeZone",
                table: "Organization",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Organization",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Organization",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Organization",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Lot",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Origin",
                table: "Lot",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Lot",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Lot",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Location",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.Sql("ALTER TABLE \"Location\" ALTER COLUMN \"Position\" TYPE geometry(Point,4326) USING \"Position\"::geometry(Point,4326)");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Location",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Location",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Location",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Location",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "InventoryMovement",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "InventoryMovement",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InventoryMovement",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "InventoryMovement",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "InventoryMovement",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorAccountId",
                table: "InventoryMovement",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Incident",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Incident",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<Guid>(
                name: "ReportedByAccountId",
                table: "Incident",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.Sql("ALTER TABLE \"Incident\" ALTER COLUMN \"Position\" TYPE geometry(Point,4326) USING \"Position\"::geometry(Point,4326)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Incident",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Incident",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Incident",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RadiusKm",
                table: "Incident",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "Incident",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "IdempotencyRecord",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "RequestHash",
                table: "IdempotencyRecord",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "IdempotencyRecord",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "ActionCode",
                table: "IdempotencyRecord",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "HandlingUnitLine",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "HandlingUnitLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "HandlingUnitLine",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "HandlingUnit",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "HandlingUnit",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "HandlingUnit",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "HandlingUnit",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Evidence",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Evidence",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Evidence",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "Evidence",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sha256",
                table: "Evidence",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Evidence",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "DeliveryLine",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<decimal>(
                name: "RejectedQuantity",
                table: "DeliveryLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PlannedQuantity",
                table: "DeliveryLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DispatchedQuantity",
                table: "DeliveryLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "DeliveryLine",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "AcceptedQuantity",
                table: "DeliveryLine",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Delivery",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Delivery",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Delivery",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedAccountId",
                table: "Delivery",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Delivery",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "CustodyEvent",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "EventKind",
                table: "CustodyEvent",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "CustodyEvent",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Consumption",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "SourceKind",
                table: "Consumption",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Consumption",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Consumption",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "ImportBatchId",
                table: "Consumption",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "OccurredOn",
                table: "Consumption",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "PeriodDays",
                table: "Consumption",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "Account",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldRowVersion: true,
                oldDefaultValue: 1L);

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "Account",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Account",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "Account",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Account",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Account",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Account",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Account_OrganizationId_Id",
                table: "Account",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.CreateTable(
                name: "AgentDecision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TriggerJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProposalJson = table.Column<string>(type: "jsonb", nullable: false),
                    ToolCallsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecidedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDecision", x => x.Id);
                    table.UniqueConstraint("AK_AgentDecision_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_AgentDecision_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ImportBatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InputHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FilesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProfileHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProfileJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReportJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatch", x => x.Id);
                    table.UniqueConstraint("AK_ImportBatch_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ImportBatch_Account_OrganizationId_CreatedByAccountId",
                        columns: x => new { x.OrganizationId, x.CreatedByAccountId },
                        principalTable: "Account",
                        principalColumns: new[] { "OrganizationId", "Id" });
                    table.ForeignKey(
                        name: "FK_ImportBatch_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id");
                });


            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_OrganizationId_DepotLocationId",
                table: "Vehicle",
                columns: new[] { "OrganizationId", "DepotLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_LocationId_LotId",
                table: "StockBalance",
                columns: new[] { "LocationId", "LotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockBalance_OrganizationId_LocationId",
                table: "StockBalance",
                columns: new[] { "OrganizationId", "LocationId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockBalance_NonNegative",
                table: "StockBalance",
                sql: "\"QuantityOnHand\" >= 0 AND \"QuantityReserved\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationMetric_OrganizationId_SimulationRunId",
                table: "SimulationMetric",
                columns: new[] { "OrganizationId", "SimulationRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStop_OrganizationId_RoutePlanId",
                table: "RouteStop",
                columns: new[] { "OrganizationId", "RoutePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStop_RoutePlanId_SequenceNumber",
                table: "RouteStop",
                columns: new[] { "RoutePlanId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskSnapshot_OrganizationId_ServicePointId",
                table: "RiskSnapshot",
                columns: new[] { "OrganizationId", "ServicePointId" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskSnapshot_ServicePointId_ProductId_CalculatedAtUtc",
                table: "RiskSnapshot",
                columns: new[] { "ServicePointId", "ProductId", "CalculatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptLine_OrganizationId_ReceiptId",
                table: "ReceiptLine",
                columns: new[] { "OrganizationId", "ReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_OrganizationId_ActorAccountId",
                table: "Receipt",
                columns: new[] { "OrganizationId", "ActorAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_OrganizationId_OperationId",
                table: "Receipt",
                columns: new[] { "OrganizationId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lot_OrganizationId_ProductId",
                table: "Lot",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Lot_ProductId_Code",
                table: "Lot",
                columns: new[] { "ProductId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Location_OrganizationId_Kind_Code",
                table: "Location",
                columns: new[] { "OrganizationId", "Kind", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Location_OrganizationId_VehicleId",
                table: "Location",
                columns: new[] { "OrganizationId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_ActorAccountId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "ActorAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_LotId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "LotId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovement_OrganizationId_OperationId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "OperationId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryMovement_Positive",
                table: "InventoryMovement",
                sql: "\"Quantity\" > 0 AND (\"FromLocationId\" IS NOT NULL OR \"ToLocationId\" IS NOT NULL) AND (\"FromLocationId\" IS NULL OR \"ToLocationId\" IS NULL OR \"FromLocationId\" <> \"ToLocationId\")");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecord_OrganizationId_AccountId_OperationId",
                table: "IdempotencyRecord",
                columns: new[] { "OrganizationId", "AccountId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandlingUnitLine_OrganizationId_HandlingUnitId",
                table: "HandlingUnitLine",
                columns: new[] { "OrganizationId", "HandlingUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Evidence_OrganizationId_ReceiptId",
                table: "Evidence",
                columns: new[] { "OrganizationId", "ReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_OrganizationId_AssignedAccountId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "AssignedAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_ActorAccountId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "ActorAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvent_OrganizationId_OperationId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "OperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_OrganizationId_ImportBatchId",
                table: "Consumption",
                columns: new[] { "OrganizationId", "ImportBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_OrganizationId_ProductId",
                table: "Consumption",
                columns: new[] { "OrganizationId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_ServicePointId_ProductId_OccurredOn",
                table: "Consumption",
                columns: new[] { "ServicePointId", "ProductId", "OccurredOn" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Consumption_Valid",
                table: "Consumption",
                sql: "\"PeriodDays\" >= 1 AND \"Quantity\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Account_Issuer_Subject",
                table: "Account",
                columns: new[] { "Issuer", "Subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentDecision_OrganizationId_Status",
                table: "AgentDecision",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatch_OrganizationId_CreatedByAccountId",
                table: "ImportBatch",
                columns: new[] { "OrganizationId", "CreatedByAccountId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Account_Organization_OrganizationId",
                table: "Account",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Consumption_ImportBatch_OrganizationId_ImportBatchId",
                table: "Consumption",
                columns: new[] { "OrganizationId", "ImportBatchId" },
                principalTable: "ImportBatch",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Consumption_Organization_OrganizationId",
                table: "Consumption",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Consumption_Product_OrganizationId_ProductId",
                table: "Consumption",
                columns: new[] { "OrganizationId", "ProductId" },
                principalTable: "Product",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Consumption_ServicePoint_OrganizationId_ServicePointId",
                table: "Consumption",
                columns: new[] { "OrganizationId", "ServicePointId" },
                principalTable: "ServicePoint",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_CustodyEvent_Account_OrganizationId_ActorAccountId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "ActorAccountId" },
                principalTable: "Account",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_CustodyEvent_HandlingUnit_OrganizationId_HandlingUnitId",
                table: "CustodyEvent",
                columns: new[] { "OrganizationId", "HandlingUnitId" },
                principalTable: "HandlingUnit",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_CustodyEvent_Organization_OrganizationId",
                table: "CustodyEvent",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Account_OrganizationId_AssignedAccountId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "AssignedAccountId" },
                principalTable: "Account",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Location_OrganizationId_OriginLocationId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "OriginLocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_Organization_OrganizationId",
                table: "Delivery",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_RouteStop_OrganizationId_RouteStopId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "RouteStopId" },
                principalTable: "RouteStop",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Delivery_ServicePoint_OrganizationId_ServicePointId",
                table: "Delivery",
                columns: new[] { "OrganizationId", "ServicePointId" },
                principalTable: "ServicePoint",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryLine_Delivery_OrganizationId_DeliveryId",
                table: "DeliveryLine",
                columns: new[] { "OrganizationId", "DeliveryId" },
                principalTable: "Delivery",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryLine_HandlingUnit_OrganizationId_HandlingUnitId",
                table: "DeliveryLine",
                columns: new[] { "OrganizationId", "HandlingUnitId" },
                principalTable: "HandlingUnit",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryLine_Lot_OrganizationId_LotId",
                table: "DeliveryLine",
                columns: new[] { "OrganizationId", "LotId" },
                principalTable: "Lot",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryLine_Organization_OrganizationId",
                table: "DeliveryLine",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Evidence_Organization_OrganizationId",
                table: "Evidence",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Evidence_Receipt_OrganizationId_ReceiptId",
                table: "Evidence",
                columns: new[] { "OrganizationId", "ReceiptId" },
                principalTable: "Receipt",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_HandlingUnit_Location_OrganizationId_LocationId",
                table: "HandlingUnit",
                columns: new[] { "OrganizationId", "LocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_HandlingUnit_Organization_OrganizationId",
                table: "HandlingUnit",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HandlingUnitLine_HandlingUnit_OrganizationId_HandlingUnitId",
                table: "HandlingUnitLine",
                columns: new[] { "OrganizationId", "HandlingUnitId" },
                principalTable: "HandlingUnit",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_HandlingUnitLine_Lot_OrganizationId_LotId",
                table: "HandlingUnitLine",
                columns: new[] { "OrganizationId", "LotId" },
                principalTable: "Lot",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_HandlingUnitLine_Organization_OrganizationId",
                table: "HandlingUnitLine",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IdempotencyRecord_Account_OrganizationId_AccountId",
                table: "IdempotencyRecord",
                columns: new[] { "OrganizationId", "AccountId" },
                principalTable: "Account",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_IdempotencyRecord_Organization_OrganizationId",
                table: "IdempotencyRecord",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Incident_Organization_OrganizationId",
                table: "Incident",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Incident_RoutePlan_OrganizationId_RoutePlanId",
                table: "Incident",
                columns: new[] { "OrganizationId", "RoutePlanId" },
                principalTable: "RoutePlan",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Incident_ServicePoint_OrganizationId_ServicePointId",
                table: "Incident",
                columns: new[] { "OrganizationId", "ServicePointId" },
                principalTable: "ServicePoint",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovement_Account_OrganizationId_ActorAccountId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "ActorAccountId" },
                principalTable: "Account",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovement_ImportBatch_OrganizationId_ImportBatchId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "ImportBatchId" },
                principalTable: "ImportBatch",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovement_Location_OrganizationId_FromLocationId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "FromLocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovement_Location_OrganizationId_ToLocationId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "ToLocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovement_Lot_OrganizationId_LotId",
                table: "InventoryMovement",
                columns: new[] { "OrganizationId", "LotId" },
                principalTable: "Lot",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovement_Organization_OrganizationId",
                table: "InventoryMovement",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Location_Organization_OrganizationId",
                table: "Location",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Location_ServicePoint_OrganizationId_ServicePointId",
                table: "Location",
                columns: new[] { "OrganizationId", "ServicePointId" },
                principalTable: "ServicePoint",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Location_Vehicle_OrganizationId_VehicleId",
                table: "Location",
                columns: new[] { "OrganizationId", "VehicleId" },
                principalTable: "Vehicle",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Lot_Organization_OrganizationId",
                table: "Lot",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Lot_Product_OrganizationId_ProductId",
                table: "Lot",
                columns: new[] { "OrganizationId", "ProductId" },
                principalTable: "Product",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Organization_OrganizationId",
                table: "Product",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Receipt_Account_OrganizationId_ActorAccountId",
                table: "Receipt",
                columns: new[] { "OrganizationId", "ActorAccountId" },
                principalTable: "Account",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Receipt_Delivery_OrganizationId_DeliveryId",
                table: "Receipt",
                columns: new[] { "OrganizationId", "DeliveryId" },
                principalTable: "Delivery",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Receipt_Organization_OrganizationId",
                table: "Receipt",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptLine_DeliveryLine_OrganizationId_DeliveryLineId",
                table: "ReceiptLine",
                columns: new[] { "OrganizationId", "DeliveryLineId" },
                principalTable: "DeliveryLine",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptLine_Organization_OrganizationId",
                table: "ReceiptLine",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceiptLine_Receipt_OrganizationId_ReceiptId",
                table: "ReceiptLine",
                columns: new[] { "OrganizationId", "ReceiptId" },
                principalTable: "Receipt",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_RiskSnapshot_Organization_OrganizationId",
                table: "RiskSnapshot",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RiskSnapshot_Product_OrganizationId_ProductId",
                table: "RiskSnapshot",
                columns: new[] { "OrganizationId", "ProductId" },
                principalTable: "Product",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_RiskSnapshot_ServicePoint_OrganizationId_ServicePointId",
                table: "RiskSnapshot",
                columns: new[] { "OrganizationId", "ServicePointId" },
                principalTable: "ServicePoint",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_RoutePlan_Organization_OrganizationId",
                table: "RoutePlan",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoutePlan_RoutePlan_OrganizationId_SupersedesRoutePlanId",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "SupersedesRoutePlanId" },
                principalTable: "RoutePlan",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_RoutePlan_Vehicle_OrganizationId_VehicleId",
                table: "RoutePlan",
                columns: new[] { "OrganizationId", "VehicleId" },
                principalTable: "Vehicle",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_RouteStop_Organization_OrganizationId",
                table: "RouteStop",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteStop_RoutePlan_OrganizationId_RoutePlanId",
                table: "RouteStop",
                columns: new[] { "OrganizationId", "RoutePlanId" },
                principalTable: "RoutePlan",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_RouteStop_ServicePoint_OrganizationId_ServicePointId",
                table: "RouteStop",
                columns: new[] { "OrganizationId", "ServicePointId" },
                principalTable: "ServicePoint",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_ServicePoint_Organization_OrganizationId",
                table: "ServicePoint",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationMetric_Organization_OrganizationId",
                table: "SimulationMetric",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationMetric_SimulationRun_OrganizationId_SimulationRun~",
                table: "SimulationMetric",
                columns: new[] { "OrganizationId", "SimulationRunId" },
                principalTable: "SimulationRun",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationRun_Organization_OrganizationId",
                table: "SimulationRun",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockBalance_Location_OrganizationId_LocationId",
                table: "StockBalance",
                columns: new[] { "OrganizationId", "LocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockBalance_Lot_OrganizationId_LotId",
                table: "StockBalance",
                columns: new[] { "OrganizationId", "LotId" },
                principalTable: "Lot",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockBalance_Organization_OrganizationId",
                table: "StockBalance",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicle_Location_OrganizationId_DepotLocationId",
                table: "Vehicle",
                columns: new[] { "OrganizationId", "DepotLocationId" },
                principalTable: "Location",
                principalColumns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicle_Organization_OrganizationId",
                table: "Vehicle",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id");
            migrationBuilder.Sql(DemoGrants.Sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("DemoCore requires restoring a verified Schema95 backup; Down cannot reconstruct removed data.");
        }
    }
}
