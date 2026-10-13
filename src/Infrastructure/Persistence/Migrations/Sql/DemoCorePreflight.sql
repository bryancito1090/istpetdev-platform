-- Empty-schema transition only. Never delete data to pass this guard.
DO $guard$
DECLARE
    tables text[] := ARRAY['AccessDelegation', 'AccessLocation', 'AccessRole', 'AccessServicePoint', 'Account', 'AnomalyCase', 'AuditEvent', 'Catalog', 'CatalogOption', 'ConfigurationActivation', 'ConfigurationDefinition', 'ConfigurationVersion', 'Consumption', 'Conversation', 'ConversationParticipant', 'CustodyEvent', 'Delivery', 'DeliveryAssignment', 'DeliveryHandlingUnit', 'DeliveryHandlingUnitLine', 'DeliveryLine', 'DemandObservation', 'Evidence', 'FieldDefinition', 'FormAttachment', 'FormSubmission', 'HandlingUnit', 'HandlingUnitLine', 'IdempotencyRecord', 'Incident', 'IncidentRoadRestriction', 'IntegrationConnection', 'IntegrationRun', 'InventoryMovement', 'InventorySnapshot', 'InventorySnapshotLine', 'JobExecution', 'Location', 'Lot', 'Message', 'MessageAttachment', 'Notification', 'Organization', 'OrganizationAccess', 'OutboxEvent', 'Permission', 'Product', 'ProductUnitConversion', 'QrAccessToken', 'RbacChange', 'Receipt', 'ReceiptLine', 'ReplenishmentRequest', 'ReplenishmentRequestLine', 'Reservation', 'RiskCalculationRun', 'RiskExplanation', 'RiskSnapshot', 'RoadMatrix', 'RoadMatrixEdge', 'Role', 'RoleFieldPermission', 'RolePermission', 'RoutePlan', 'RoutePlanIncident', 'RoutePlanningSnapshot', 'RouteStop', 'ScenarioDataset', 'ScenarioDatasetVersion', 'ScenarioEvent', 'ServicePoint', 'ServicePointContact', 'ServicePointWindow', 'SimulationArtifact', 'SimulationEvent', 'SimulationMetric', 'SimulationPolicyVersion', 'SimulationRun', 'StockBalance', 'StoredFile', 'SyncBatch', 'SyncOperation', 'Template', 'TemplateBinding', 'TemplateField', 'TemplateFieldAccess', 'TemplateFieldOption', 'TemplateVersion', 'Vehicle', 'VehicleAvailability', 'VehiclePosition', 'WorkflowDefinition', 'WorkflowState', 'WorkflowTransition', 'WorkflowVersion'];
    item text; occupied boolean; policy_record record; trigger_record record;
BEGIN
    FOREACH item IN ARRAY tables LOOP
        EXECUTE format('LOCK TABLE public.%I IN ACCESS EXCLUSIVE MODE', item);
        IF item = 'Organization' THEN
            SELECT count(*) <> 1 OR count(*) FILTER (WHERE "Id" = '01990000-0000-7000-8000-000000000001'
                AND "Code" = 'demo' AND "Name" = 'ISTPET Demo' AND "TimeZone" = 'America/Guayaquil'
                AND "IsActive" AND "AuthorizationVersion" = 1 AND "Version" = 1 AND "UpdatedAtUtc" IS NULL) <> 1
                INTO occupied FROM public."Organization";
        ELSIF item = 'Permission' THEN
            SELECT count(*) <> 1 OR count(*) FILTER (WHERE "Id" = '01990000-0000-7000-8000-000000000002'
                AND "Code" = 'service-point.read' AND "ModuleCode" = 'catalog' AND "ActionCode" = 'read'
                AND "ResourceKind" = 'service-point' AND "IsDelegable" AND "Version" = 1 AND "UpdatedAtUtc" IS NULL) <> 1
                INTO occupied FROM public."Permission";
        ELSIF item = 'Role' THEN
            SELECT count(*) <> 7 OR count(*) FILTER (WHERE "OrganizationId" = '01990000-0000-7000-8000-000000000001'
                AND "IsActive" AND "IsSeed" AND "Version" = 1 AND "UpdatedAtUtc" IS NULL
                AND ("Id", "Code", "Name", "IsTechnical") IN (
                    ('01990000-0000-7000-8000-000000000010'::uuid, 'gerencia', 'Gerencia', false),
                    ('01990000-0000-7000-8000-000000000011'::uuid, 'planificador', 'Planificador', false),
                    ('01990000-0000-7000-8000-000000000012'::uuid, 'bodega', 'Bodega', false),
                    ('01990000-0000-7000-8000-000000000013'::uuid, 'conductor', 'Conductor', false),
                    ('01990000-0000-7000-8000-000000000014'::uuid, 'receptor', 'Receptor', false),
                    ('01990000-0000-7000-8000-000000000015'::uuid, 'automatizacion', 'Automatización', true),
                    ('01990000-0000-7000-8000-000000000016'::uuid, 'configuration-admin', 'ConfigurationAdmin', false))) <> 7
                INTO occupied FROM public."Role";
        ELSE
            EXECUTE format('SELECT EXISTS (SELECT FROM public.%I)', item) INTO occupied;
        END IF;
        IF occupied THEN
            RAISE EXCEPTION 'DemoCore stopped: % contains records. Back up and reconcile into a separate demo database; no data was deleted.', item;
        END IF;
    END LOOP;
    FOREACH item IN ARRAY tables LOOP
        FOR policy_record IN SELECT policyname FROM pg_policies WHERE schemaname = 'public' AND tablename = item LOOP
            EXECUTE format('DROP POLICY %I ON public.%I', policy_record.policyname, item);
        END LOOP;
        FOR trigger_record IN SELECT t.tgname FROM pg_trigger t JOIN pg_class c ON c.oid = t.tgrelid
            JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relname = item AND NOT t.tgisinternal LOOP
            EXECUTE format('DROP TRIGGER %I ON public.%I', trigger_record.tgname, item);
        END LOOP;
        EXECUTE format('ALTER TABLE public.%I DISABLE ROW LEVEL SECURITY', item);
        EXECUTE format('ALTER TABLE public.%I NO FORCE ROW LEVEL SECURITY', item);
    END LOOP;
END $guard$;
