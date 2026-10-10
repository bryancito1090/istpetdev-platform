DO $guard$
DECLARE table_name text; nonempty boolean;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['AccessDelegation','AccessLocation','AccessRole','AccessServicePoint','Account','AnomalyCase','AuditEvent','Catalog','CatalogOption','ConfigurationActivation','ConfigurationDefinition','ConfigurationVersion','Consumption','Conversation','ConversationParticipant','CustodyEvent','Delivery','DeliveryAssignment','DeliveryHandlingUnit','DeliveryHandlingUnitLine','DeliveryLine','DemandObservation','Evidence','FieldDefinition','FormAttachment','FormSubmission','HandlingUnit','HandlingUnitLine','IdempotencyRecord','Incident','IncidentRoadRestriction','IntegrationConnection','IntegrationRun','InventoryMovement','InventorySnapshot','InventorySnapshotLine','JobExecution','Location','Lot','Message','MessageAttachment','Notification','Organization','OrganizationAccess','OutboxEvent','Permission','Product','ProductUnitConversion','QrAccessToken','RbacChange','Receipt','ReceiptLine','ReplenishmentRequest','ReplenishmentRequestLine','Reservation','RiskCalculationRun','RiskExplanation','RiskSnapshot','RoadMatrix','RoadMatrixEdge','Role','RoleFieldPermission','RolePermission','RoutePlan','RoutePlanIncident','RoutePlanningSnapshot','RouteStop','ScenarioDataset','ScenarioDatasetVersion','ScenarioEvent','ServicePoint','ServicePointContact','ServicePointWindow','SimulationArtifact','SimulationEvent','SimulationMetric','SimulationPolicyVersion','SimulationRun','StockBalance','StoredFile','SyncBatch','SyncOperation','Template','TemplateBinding','TemplateField','TemplateFieldAccess','TemplateFieldOption','TemplateVersion','Vehicle','VehicleAvailability','VehiclePosition','WorkflowDefinition','WorkflowState','WorkflowTransition','WorkflowVersion'] LOOP
        EXECUTE format('LOCK TABLE public.%I IN ACCESS EXCLUSIVE MODE',table_name);
        EXECUTE format('SELECT EXISTS (SELECT FROM public.%I)',table_name) INTO nonempty;
        IF nonempty THEN RAISE EXCEPTION 'Schema95 downgrade stopped: % contains data; use a reviewed forward migration',table_name; END IF;
    END LOOP;
END $guard$;
-- CASCADE only removes triggers attached to the empty tables guarded above.
DO $drop_functions$
DECLARE signature regprocedure;
BEGIN
    FOR signature IN SELECT p.oid::regprocedure FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public' AND p.proname LIKE 'istpetdev_%' LOOP
        EXECUTE format('DROP FUNCTION IF EXISTS %s CASCADE',signature);
    END LOOP;
END $drop_functions$;
