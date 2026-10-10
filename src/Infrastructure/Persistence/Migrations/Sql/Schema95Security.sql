-- Helpers are invoker functions, except the narrow authenticated-identity lookup.
-- Roles with service SQL credentials can set context: keep credentials out of clients.
CREATE FUNCTION public.istpetdev_organization_id() RETURNS uuid
LANGUAGE sql STABLE SET search_path = pg_catalog
AS $$ SELECT NULLIF(current_setting('app.organization_id', true), '')::uuid $$;

CREATE FUNCTION public.istpetdev_actor_id() RETURNS uuid
LANGUAGE sql STABLE SET search_path = pg_catalog
AS $$ SELECT NULLIF(current_setting('app.account_id', true), '')::uuid $$;

CREATE FUNCTION public.istpetdev_is_member(p_organization uuid) RETURNS boolean
LANGUAGE sql STABLE SET search_path = pg_catalog
AS $$
    SELECT p_organization = public.istpetdev_organization_id()
       AND EXISTS (
           SELECT FROM public."OrganizationAccess" access
           JOIN public."Account" account ON account."Id" = access."AccountId"
           JOIN public."Organization" organization ON organization."Id" = access."OrganizationId"
           WHERE access."OrganizationId" = p_organization
             AND access."AccountId" = public.istpetdev_actor_id()
             AND access."IsActive" AND access."RevokedAtUtc" IS NULL
             AND access."ValidFromUtc" <= CURRENT_TIMESTAMP
             AND (access."ValidToUtc" IS NULL OR access."ValidToUtc" > CURRENT_TIMESTAMP)
             AND account."IsActive" AND organization."IsActive"
       )
$$;

CREATE FUNCTION public.istpetdev_has_permission(p_organization uuid, p_permission text) RETURNS boolean
LANGUAGE sql STABLE SET search_path = pg_catalog
AS $$
    SELECT public.istpetdev_is_member(p_organization) AND EXISTS (
        SELECT FROM public."OrganizationAccess" access
        JOIN public."AccessRole" assignment
          ON assignment."OrganizationId" = access."OrganizationId" AND assignment."OrganizationAccessId" = access."Id"
        JOIN public."Role" role
          ON role."OrganizationId" = assignment."OrganizationId" AND role."Id" = assignment."RoleId"
        JOIN public."RolePermission" grant_row
          ON grant_row."OrganizationId" = role."OrganizationId" AND grant_row."RoleId" = role."Id"
        JOIN public."Permission" permission ON permission."Id" = grant_row."PermissionId"
        WHERE access."OrganizationId" = p_organization AND access."AccountId" = public.istpetdev_actor_id()
          AND assignment."RevokedAtUtc" IS NULL AND role."IsActive" AND permission."Code" = p_permission
          AND role."IsTechnical" = (EXISTS (SELECT FROM public."Account" a WHERE a."Id" = access."AccountId" AND a."Kind" = 'technical'))
    )
$$;

CREATE FUNCTION public.istpetdev_location_allowed(p_organization uuid, p_location uuid) RETURNS boolean
LANGUAGE sql STABLE SET search_path = pg_catalog
AS $$
    SELECT public.istpetdev_is_member(p_organization) AND EXISTS (
        SELECT FROM public."AccessLocation" scope
        JOIN public."OrganizationAccess" access ON access."OrganizationId" = scope."OrganizationId" AND access."Id" = scope."OrganizationAccessId"
        WHERE scope."OrganizationId" = p_organization AND scope."LocationId" = p_location
          AND scope."RevokedAtUtc" IS NULL AND access."AccountId" = public.istpetdev_actor_id()
    )
$$;

CREATE FUNCTION public.istpetdev_point_allowed(p_organization uuid, p_point uuid) RETURNS boolean
LANGUAGE sql STABLE SET search_path = pg_catalog
AS $$
    SELECT public.istpetdev_is_member(p_organization) AND EXISTS (
        SELECT FROM public."AccessServicePoint" scope
        JOIN public."OrganizationAccess" access ON access."OrganizationId" = scope."OrganizationId" AND access."Id" = scope."OrganizationAccessId"
        WHERE scope."OrganizationId" = p_organization AND scope."ServicePointId" = p_point
          AND scope."RevokedAtUtc" IS NULL AND access."AccountId" = public.istpetdev_actor_id()
    )
$$;

CREATE FUNCTION public.istpetdev_account_id(p_issuer text, p_subject text) RETURNS uuid
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = pg_catalog
AS $$
    SELECT account."Id" FROM public."Account" account
    WHERE account."Kind" = 'human' AND account."Issuer" = p_issuer
      AND account."Subject" = p_subject AND account."IsActive"
$$;

CREATE FUNCTION public.istpetdev_stamp_row() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog
AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        NEW."CreatedAtUtc" := CURRENT_TIMESTAMP;
        NEW."UpdatedAtUtc" := NULL;
        NEW."Version" := 1;
    ELSE
        IF NEW."Id" IS DISTINCT FROM OLD."Id" OR NEW."CreatedAtUtc" IS DISTINCT FROM OLD."CreatedAtUtc" THEN
            RAISE EXCEPTION 'Identity and creation time are immutable' USING ERRCODE = '23514';
        END IF;
        IF to_jsonb(NEW)->>'OrganizationId' IS DISTINCT FROM to_jsonb(OLD)->>'OrganizationId' THEN
            RAISE EXCEPTION 'OrganizationId is immutable' USING ERRCODE = '23514';
        END IF;
        IF TG_TABLE_NAME = 'Account' AND (
            to_jsonb(NEW)->>'Issuer' IS DISTINCT FROM to_jsonb(OLD)->>'Issuer' OR to_jsonb(NEW)->>'Subject' IS DISTINCT FROM to_jsonb(OLD)->>'Subject'
            OR to_jsonb(NEW)->>'ClientId' IS DISTINCT FROM to_jsonb(OLD)->>'ClientId' OR to_jsonb(NEW)->>'Kind' IS DISTINCT FROM to_jsonb(OLD)->>'Kind'
        ) THEN
            RAISE EXCEPTION 'External account identity is immutable' USING ERRCODE = '23514';
        END IF;
        NEW."UpdatedAtUtc" := CURRENT_TIMESTAMP;
        NEW."Version" := OLD."Version" + 1;
    END IF;
    RETURN NEW;
END $$;

CREATE FUNCTION public.istpetdev_insert_only() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog
AS $$ BEGIN RAISE EXCEPTION '% is append-only; record a compensating operation', TG_TABLE_NAME USING ERRCODE = '23514'; END $$;

CREATE FUNCTION public.istpetdev_frozen_version() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog
AS $$
BEGIN
    IF (to_jsonb(OLD)->>'PublishedAtUtc') IS NOT NULL THEN
        -- A published template may be retired, but its schema/content cannot change.
        IF TG_TABLE_NAME = 'TemplateVersion' AND TG_OP = 'UPDATE'
           AND to_jsonb(OLD)->>'Status' = 'published' AND to_jsonb(NEW)->>'Status' = 'retired'
           AND (to_jsonb(OLD) - ARRAY['Status','UpdatedAtUtc','Version']) = (to_jsonb(NEW) - ARRAY['Status','UpdatedAtUtc','Version']) THEN
            RETURN NEW;
        END IF;
        RAISE EXCEPTION '% is published; create a new version', TG_TABLE_NAME USING ERRCODE = '23514';
    END IF;
    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
    RETURN NEW;
END $$;

CREATE FUNCTION public.istpetdev_frozen_child() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog
AS $$
DECLARE parent_table text := TG_ARGV[0]; parent_column text := TG_ARGV[1];
        row_data jsonb; old_data jsonb; published boolean;
BEGIN
    row_data := CASE WHEN TG_OP = 'DELETE' THEN to_jsonb(OLD) ELSE to_jsonb(NEW) END;
    old_data := CASE WHEN TG_OP = 'INSERT' THEN row_data ELSE to_jsonb(OLD) END;
    -- Lock both old and new parents to serialize publication and child edits.
    FOR published IN EXECUTE format('SELECT "PublishedAtUtc" IS NOT NULL FROM public.%I WHERE "OrganizationId" = $1 AND "Id" IN ($2,$3) FOR UPDATE', parent_table)
        USING (row_data->>'OrganizationId')::uuid, (row_data->>parent_column)::uuid, (old_data->>parent_column)::uuid LOOP
        IF published THEN RAISE EXCEPTION 'Published version fields/options/states are immutable' USING ERRCODE = '23514'; END IF;
    END LOOP;
    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
    RETURN NEW;
END $$;

ALTER TABLE public."Organization" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Organization" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Organization', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Organization" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Organization" FOR SELECT USING ("Id" = public.istpetdev_organization_id() AND "IsActive" AND EXISTS (SELECT FROM public."OrganizationAccess" a JOIN public."Account" c ON c."Id" = a."AccountId" WHERE a."OrganizationId" = "Organization"."Id" AND a."AccountId" = public.istpetdev_actor_id() AND a."IsActive" AND a."RevokedAtUtc" IS NULL AND a."ValidFromUtc" <= CURRENT_TIMESTAMP AND (a."ValidToUtc" IS NULL OR a."ValidToUtc" > CURRENT_TIMESTAMP) AND c."IsActive"));
ALTER TABLE public."Account" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Account" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Account', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Account" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Account" FOR SELECT USING ("Id" = public.istpetdev_actor_id());
ALTER TABLE public."Permission" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Permission" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Permission', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Permission" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Permission" FOR SELECT USING (public.istpetdev_is_member(public.istpetdev_organization_id()));
ALTER TABLE public."FieldDefinition" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."FieldDefinition" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'FieldDefinition', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."FieldDefinition" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."FieldDefinition" FOR SELECT USING (public.istpetdev_is_member(public.istpetdev_organization_id()));
ALTER TABLE public."Role" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Role" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Role', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Role" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Role" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId"));
ALTER TABLE public."RolePermission" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RolePermission" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RolePermission', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RolePermission" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."RolePermission" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId"));
ALTER TABLE public."RoleFieldPermission" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RoleFieldPermission" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RoleFieldPermission', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RoleFieldPermission" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."RoleFieldPermission" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId"));
ALTER TABLE public."OrganizationAccess" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."OrganizationAccess" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'OrganizationAccess', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."OrganizationAccess" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."OrganizationAccess" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND "AccountId" = public.istpetdev_actor_id());
ALTER TABLE public."AccessRole" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AccessRole" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'AccessRole', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."AccessRole" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."AccessRole" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "AccessRole"."OrganizationId" AND a."Id" = "AccessRole"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()));
ALTER TABLE public."AccessLocation" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AccessLocation" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'AccessLocation', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."AccessLocation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."AccessLocation" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "AccessLocation"."OrganizationId" AND a."Id" = "AccessLocation"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()));
ALTER TABLE public."AccessServicePoint" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AccessServicePoint" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'AccessServicePoint', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."AccessServicePoint" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."AccessServicePoint" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "AccessServicePoint"."OrganizationId" AND a."Id" = "AccessServicePoint"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()));
ALTER TABLE public."AccessDelegation" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AccessDelegation" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'AccessDelegation', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."AccessDelegation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."AccessDelegation" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "AccessDelegation"."OrganizationId" AND a."Id" = "AccessDelegation"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()));
ALTER TABLE public."RbacChange" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RbacChange" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RbacChange', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RbacChange" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."RbacChange" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."RbacChange" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'audit.read'));
CREATE POLICY application_insert ON public."RbacChange" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'access.manage') OR public.istpetdev_has_permission("OrganizationId", 'role.manage') OR public.istpetdev_has_permission("OrganizationId", 'role.assign')));
ALTER TABLE public."ConfigurationDefinition" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ConfigurationDefinition" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ConfigurationDefinition', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ConfigurationDefinition" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ConfigurationDefinition" FOR SELECT USING (public.istpetdev_is_member(public.istpetdev_organization_id()));
ALTER TABLE public."ConfigurationVersion" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ConfigurationVersion" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ConfigurationVersion', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ConfigurationVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER freeze_version BEFORE UPDATE OR DELETE ON public."ConfigurationVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_frozen_version();
CREATE POLICY application_read ON public."ConfigurationVersion" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'configuration.read'));
CREATE POLICY application_insert ON public."ConfigurationVersion" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_update ON public."ConfigurationVersion" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_delete ON public."ConfigurationVersion" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
ALTER TABLE public."ConfigurationActivation" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ConfigurationActivation" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ConfigurationActivation', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ConfigurationActivation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ConfigurationActivation" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'configuration.read'));
CREATE POLICY application_insert ON public."ConfigurationActivation" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_update ON public."ConfigurationActivation" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_delete ON public."ConfigurationActivation" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
ALTER TABLE public."Catalog" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Catalog" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Catalog', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Catalog" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Catalog" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'configuration.read'));
CREATE POLICY application_insert ON public."Catalog" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_update ON public."Catalog" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_delete ON public."Catalog" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
ALTER TABLE public."CatalogOption" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."CatalogOption" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'CatalogOption', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."CatalogOption" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."CatalogOption" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'catalog.read') OR public.istpetdev_has_permission("OrganizationId", 'service-point.read')));
CREATE POLICY application_insert ON public."CatalogOption" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_update ON public."CatalogOption" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_delete ON public."CatalogOption" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
ALTER TABLE public."WorkflowDefinition" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."WorkflowDefinition" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'WorkflowDefinition', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."WorkflowDefinition" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."WorkflowDefinition" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'configuration.read'));
CREATE POLICY application_insert ON public."WorkflowDefinition" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_update ON public."WorkflowDefinition" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_delete ON public."WorkflowDefinition" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
ALTER TABLE public."WorkflowVersion" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."WorkflowVersion" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'WorkflowVersion', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."WorkflowVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER freeze_version BEFORE UPDATE OR DELETE ON public."WorkflowVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_frozen_version();
CREATE POLICY application_read ON public."WorkflowVersion" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'configuration.read'));
CREATE POLICY application_insert ON public."WorkflowVersion" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_update ON public."WorkflowVersion" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_delete ON public."WorkflowVersion" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
ALTER TABLE public."WorkflowState" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."WorkflowState" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'WorkflowState', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."WorkflowState" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER freeze_child BEFORE INSERT OR UPDATE OR DELETE ON public."WorkflowState" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_frozen_child('WorkflowVersion', 'WorkflowVersionId');
CREATE POLICY application_read ON public."WorkflowState" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'configuration.read'));
CREATE POLICY application_insert ON public."WorkflowState" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_update ON public."WorkflowState" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_delete ON public."WorkflowState" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
ALTER TABLE public."WorkflowTransition" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."WorkflowTransition" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'WorkflowTransition', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."WorkflowTransition" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER freeze_child BEFORE INSERT OR UPDATE OR DELETE ON public."WorkflowTransition" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_frozen_child('WorkflowVersion', 'WorkflowVersionId');
CREATE POLICY application_read ON public."WorkflowTransition" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'configuration.read'));
CREATE POLICY application_insert ON public."WorkflowTransition" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_update ON public."WorkflowTransition" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
CREATE POLICY application_delete ON public."WorkflowTransition" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'configuration.manage') OR public.istpetdev_has_permission("OrganizationId", 'configuration.publish')));
ALTER TABLE public."Template" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Template" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Template', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Template" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Template" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'template.read'));
CREATE POLICY application_insert ON public."Template" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
CREATE POLICY application_update ON public."Template" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
CREATE POLICY application_delete ON public."Template" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
ALTER TABLE public."TemplateVersion" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."TemplateVersion" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'TemplateVersion', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."TemplateVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER freeze_version BEFORE UPDATE OR DELETE ON public."TemplateVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_frozen_version();
CREATE POLICY application_read ON public."TemplateVersion" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'template.read'));
CREATE POLICY application_insert ON public."TemplateVersion" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage') OR public.istpetdev_has_permission("OrganizationId", 'template.publish')));
CREATE POLICY application_update ON public."TemplateVersion" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage') OR public.istpetdev_has_permission("OrganizationId", 'template.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage') OR public.istpetdev_has_permission("OrganizationId", 'template.publish')));
CREATE POLICY application_delete ON public."TemplateVersion" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage') OR public.istpetdev_has_permission("OrganizationId", 'template.publish')));
ALTER TABLE public."TemplateField" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."TemplateField" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'TemplateField', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."TemplateField" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER freeze_child BEFORE INSERT OR UPDATE OR DELETE ON public."TemplateField" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_frozen_child('TemplateVersion', 'TemplateVersionId');
CREATE POLICY application_read ON public."TemplateField" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'template.read'));
CREATE POLICY application_insert ON public."TemplateField" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
CREATE POLICY application_update ON public."TemplateField" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
CREATE POLICY application_delete ON public."TemplateField" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
ALTER TABLE public."TemplateFieldOption" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."TemplateFieldOption" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'TemplateFieldOption', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."TemplateFieldOption" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."TemplateFieldOption" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'template.read'));
CREATE POLICY application_insert ON public."TemplateFieldOption" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
CREATE POLICY application_update ON public."TemplateFieldOption" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
CREATE POLICY application_delete ON public."TemplateFieldOption" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.manage')));
ALTER TABLE public."TemplateFieldAccess" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."TemplateFieldAccess" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'TemplateFieldAccess', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."TemplateFieldAccess" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."TemplateFieldAccess" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'template.read'));
CREATE POLICY application_insert ON public."TemplateFieldAccess" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.field-access.manage')));
CREATE POLICY application_update ON public."TemplateFieldAccess" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.field-access.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.field-access.manage')));
CREATE POLICY application_delete ON public."TemplateFieldAccess" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'template.field-access.manage')));
ALTER TABLE public."TemplateBinding" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."TemplateBinding" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'TemplateBinding', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."TemplateBinding" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."TemplateBinding" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'template.read'));
CREATE POLICY application_insert ON public."TemplateBinding" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'template.publish')));
CREATE POLICY application_update ON public."TemplateBinding" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'template.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'template.publish')));
CREATE POLICY application_delete ON public."TemplateBinding" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'template.publish')));
ALTER TABLE public."FormSubmission" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."FormSubmission" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'FormSubmission', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."FormSubmission" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."FormSubmission" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."FormSubmission" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "FormSubmission"."OrganizationId" AND p."Id" = "FormSubmission"."DeliveryId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "FormSubmission"."OrganizationId" AND p."Id" = "FormSubmission"."ReceiptId")) AND public.istpetdev_has_permission("OrganizationId", 'form.read'));
CREATE POLICY application_insert ON public."FormSubmission" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "FormSubmission"."OrganizationId" AND p."Id" = "FormSubmission"."DeliveryId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "FormSubmission"."OrganizationId" AND p."Id" = "FormSubmission"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'form.submit')));
ALTER TABLE public."FormAttachment" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."FormAttachment" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'FormAttachment', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."FormAttachment" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."FormAttachment" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FormSubmissionId" IS NULL OR EXISTS (SELECT FROM public."FormSubmission" p WHERE p."OrganizationId" = "FormAttachment"."OrganizationId" AND p."Id" = "FormAttachment"."FormSubmissionId")) AND public.istpetdev_has_permission("OrganizationId", 'form.read'));
CREATE POLICY application_insert ON public."FormAttachment" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FormSubmissionId" IS NULL OR EXISTS (SELECT FROM public."FormSubmission" p WHERE p."OrganizationId" = "FormAttachment"."OrganizationId" AND p."Id" = "FormAttachment"."FormSubmissionId")) AND (public.istpetdev_has_permission("OrganizationId", 'form.submit')));
CREATE POLICY application_update ON public."FormAttachment" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FormSubmissionId" IS NULL OR EXISTS (SELECT FROM public."FormSubmission" p WHERE p."OrganizationId" = "FormAttachment"."OrganizationId" AND p."Id" = "FormAttachment"."FormSubmissionId")) AND (public.istpetdev_has_permission("OrganizationId", 'form.submit'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FormSubmissionId" IS NULL OR EXISTS (SELECT FROM public."FormSubmission" p WHERE p."OrganizationId" = "FormAttachment"."OrganizationId" AND p."Id" = "FormAttachment"."FormSubmissionId")) AND (public.istpetdev_has_permission("OrganizationId", 'form.submit')));
CREATE POLICY application_delete ON public."FormAttachment" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FormSubmissionId" IS NULL OR EXISTS (SELECT FROM public."FormSubmission" p WHERE p."OrganizationId" = "FormAttachment"."OrganizationId" AND p."Id" = "FormAttachment"."FormSubmissionId")) AND (public.istpetdev_has_permission("OrganizationId", 'form.submit')));
ALTER TABLE public."ServicePoint" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ServicePoint" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ServicePoint', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ServicePoint" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ServicePoint" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_point_allowed("OrganizationId", "Id") AND public.istpetdev_has_permission("OrganizationId", 'service-point.read'));
CREATE POLICY application_insert ON public."ServicePoint" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_point_allowed("OrganizationId", "Id") AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
CREATE POLICY application_update ON public."ServicePoint" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_point_allowed("OrganizationId", "Id") AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_point_allowed("OrganizationId", "Id") AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
CREATE POLICY application_delete ON public."ServicePoint" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_point_allowed("OrganizationId", "Id") AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
ALTER TABLE public."ServicePointWindow" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ServicePointWindow" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ServicePointWindow', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ServicePointWindow" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ServicePointWindow" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'service-point.read'));
CREATE POLICY application_insert ON public."ServicePointWindow" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
CREATE POLICY application_update ON public."ServicePointWindow" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
CREATE POLICY application_delete ON public."ServicePointWindow" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
ALTER TABLE public."ServicePointContact" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ServicePointContact" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ServicePointContact', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ServicePointContact" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ServicePointContact" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'service-point.read'));
CREATE POLICY application_insert ON public."ServicePointContact" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
CREATE POLICY application_update ON public."ServicePointContact" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
CREATE POLICY application_delete ON public."ServicePointContact" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'service-point.manage')));
ALTER TABLE public."Location" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Location" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Location', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Location" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Location" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_location_allowed("OrganizationId", "Id") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."Location" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_location_allowed("OrganizationId", "Id") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust')));
CREATE POLICY application_update ON public."Location" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_location_allowed("OrganizationId", "Id") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_location_allowed("OrganizationId", "Id") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust')));
CREATE POLICY application_delete ON public."Location" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_location_allowed("OrganizationId", "Id") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust')));
ALTER TABLE public."Vehicle" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Vehicle" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Vehicle', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Vehicle" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Vehicle" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'vehicle.read'));
CREATE POLICY application_insert ON public."Vehicle" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.manage')));
CREATE POLICY application_update ON public."Vehicle" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.manage')));
CREATE POLICY application_delete ON public."Vehicle" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.manage')));
ALTER TABLE public."VehicleAvailability" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."VehicleAvailability" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'VehicleAvailability', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."VehicleAvailability" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."VehicleAvailability" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DepotLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DepotLocationId")) AND public.istpetdev_has_permission("OrganizationId", 'vehicle.read'));
CREATE POLICY application_insert ON public."VehicleAvailability" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DepotLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DepotLocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.manage')));
CREATE POLICY application_update ON public."VehicleAvailability" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DepotLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DepotLocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DepotLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DepotLocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.manage')));
CREATE POLICY application_delete ON public."VehicleAvailability" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DepotLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DepotLocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.manage')));
ALTER TABLE public."VehiclePosition" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."VehiclePosition" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'VehiclePosition', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."VehiclePosition" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."VehiclePosition" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."VehiclePosition" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'vehicle.read'));
CREATE POLICY application_insert ON public."VehiclePosition" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'vehicle.position.create')));
ALTER TABLE public."Product" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Product" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Product', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Product" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Product" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'product.read'));
CREATE POLICY application_insert ON public."Product" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'product.manage')));
CREATE POLICY application_update ON public."Product" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'product.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'product.manage')));
CREATE POLICY application_delete ON public."Product" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'product.manage')));
ALTER TABLE public."ProductUnitConversion" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ProductUnitConversion" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ProductUnitConversion', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ProductUnitConversion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ProductUnitConversion" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'product.read'));
CREATE POLICY application_insert ON public."ProductUnitConversion" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'product.manage')));
CREATE POLICY application_update ON public."ProductUnitConversion" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'product.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'product.manage')));
CREATE POLICY application_delete ON public."ProductUnitConversion" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'product.manage')));
ALTER TABLE public."Lot" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Lot" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Lot', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Lot" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Lot" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."Lot" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'lot.create') OR public.istpetdev_has_permission("OrganizationId", 'inventory.adjust')));
CREATE POLICY application_update ON public."Lot" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'lot.create') OR public.istpetdev_has_permission("OrganizationId", 'inventory.adjust'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'lot.create') OR public.istpetdev_has_permission("OrganizationId", 'inventory.adjust')));
CREATE POLICY application_delete ON public."Lot" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'lot.create') OR public.istpetdev_has_permission("OrganizationId", 'inventory.adjust')));
ALTER TABLE public."HandlingUnit" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."HandlingUnit" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'HandlingUnit', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."HandlingUnit" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."HandlingUnit" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."HandlingUnit" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'handling-unit.create') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_update ON public."HandlingUnit" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'handling-unit.create') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'handling-unit.create') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_delete ON public."HandlingUnit" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'handling-unit.create') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
ALTER TABLE public."HandlingUnitLine" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."HandlingUnitLine" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'HandlingUnitLine', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."HandlingUnitLine" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."HandlingUnitLine" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."HandlingUnitLine" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'handling-unit.create')));
CREATE POLICY application_update ON public."HandlingUnitLine" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'handling-unit.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'handling-unit.create')));
CREATE POLICY application_delete ON public."HandlingUnitLine" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'handling-unit.create')));
ALTER TABLE public."InventoryMovement" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."InventoryMovement" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'InventoryMovement', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."InventoryMovement" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."InventoryMovement" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."InventoryMovement" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FromLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "FromLocationId")) AND ("ToLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ToLocationId")) AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."InventoryMovement" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FromLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "FromLocationId")) AND ("ToLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ToLocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
ALTER TABLE public."StockBalance" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."StockBalance" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'StockBalance', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."StockBalance" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."StockBalance" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."StockBalance" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
CREATE POLICY application_update ON public."StockBalance" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
CREATE POLICY application_delete ON public."StockBalance" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
ALTER TABLE public."InventorySnapshot" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."InventorySnapshot" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'InventorySnapshot', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."InventorySnapshot" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."InventorySnapshot" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."InventorySnapshot" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."InventorySnapshot" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
ALTER TABLE public."InventorySnapshotLine" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."InventorySnapshotLine" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'InventorySnapshotLine', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."InventorySnapshotLine" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."InventorySnapshotLine" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."InventorySnapshotLine" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."InventorySnapshotLine" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
ALTER TABLE public."Reservation" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Reservation" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Reservation', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Reservation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Reservation" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."Reservation" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
CREATE POLICY application_update ON public."Reservation" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
CREATE POLICY application_delete ON public."Reservation" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'inventory.adjust') OR public.istpetdev_has_permission("OrganizationId", 'stock.reserve') OR public.istpetdev_has_permission("OrganizationId", 'consumption.create') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept') OR public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
ALTER TABLE public."Consumption" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Consumption" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Consumption', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Consumption" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Consumption" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'inventory.read'));
CREATE POLICY application_insert ON public."Consumption" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'consumption.create')));
CREATE POLICY application_update ON public."Consumption" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'consumption.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'consumption.create')));
CREATE POLICY application_delete ON public."Consumption" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'consumption.create')));
ALTER TABLE public."DemandObservation" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."DemandObservation" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'DemandObservation', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."DemandObservation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."DemandObservation" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'replenishment.read'));
CREATE POLICY application_insert ON public."DemandObservation" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create')));
CREATE POLICY application_update ON public."DemandObservation" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create')));
CREATE POLICY application_delete ON public."DemandObservation" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create')));
ALTER TABLE public."ReplenishmentRequest" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ReplenishmentRequest" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ReplenishmentRequest', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ReplenishmentRequest" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ReplenishmentRequest" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'replenishment.read'));
CREATE POLICY application_insert ON public."ReplenishmentRequest" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create') OR public.istpetdev_has_permission("OrganizationId", 'replenishment.approve')));
CREATE POLICY application_update ON public."ReplenishmentRequest" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create') OR public.istpetdev_has_permission("OrganizationId", 'replenishment.approve'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create') OR public.istpetdev_has_permission("OrganizationId", 'replenishment.approve')));
CREATE POLICY application_delete ON public."ReplenishmentRequest" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create') OR public.istpetdev_has_permission("OrganizationId", 'replenishment.approve')));
ALTER TABLE public."ReplenishmentRequestLine" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ReplenishmentRequestLine" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ReplenishmentRequestLine', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ReplenishmentRequestLine" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ReplenishmentRequestLine" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'replenishment.read'));
CREATE POLICY application_insert ON public."ReplenishmentRequestLine" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create') OR public.istpetdev_has_permission("OrganizationId", 'replenishment.approve')));
CREATE POLICY application_update ON public."ReplenishmentRequestLine" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create') OR public.istpetdev_has_permission("OrganizationId", 'replenishment.approve'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create') OR public.istpetdev_has_permission("OrganizationId", 'replenishment.approve')));
CREATE POLICY application_delete ON public."ReplenishmentRequestLine" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'replenishment.create') OR public.istpetdev_has_permission("OrganizationId", 'replenishment.approve')));
ALTER TABLE public."RoadMatrix" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RoadMatrix" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RoadMatrix', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RoadMatrix" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."RoadMatrix" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."RoadMatrix" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'route.read'));
CREATE POLICY application_insert ON public."RoadMatrix" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate')));
ALTER TABLE public."RoadMatrixEdge" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RoadMatrixEdge" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RoadMatrixEdge', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RoadMatrixEdge" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."RoadMatrixEdge" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."RoadMatrixEdge" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FromLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "FromLocationId")) AND ("ToLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ToLocationId")) AND public.istpetdev_has_permission("OrganizationId", 'route.read'));
CREATE POLICY application_insert ON public."RoadMatrixEdge" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FromLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "FromLocationId")) AND ("ToLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ToLocationId")) AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate')));
ALTER TABLE public."RoutePlan" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RoutePlan" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RoutePlan', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RoutePlan" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."RoutePlan" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'route.read'));
CREATE POLICY application_insert ON public."RoutePlan" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.publish')));
CREATE POLICY application_update ON public."RoutePlan" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.publish')));
CREATE POLICY application_delete ON public."RoutePlan" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.publish')));
ALTER TABLE public."RoutePlanningSnapshot" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RoutePlanningSnapshot" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RoutePlanningSnapshot', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RoutePlanningSnapshot" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."RoutePlanningSnapshot" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."RoutePlanningSnapshot" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'route.read'));
CREATE POLICY application_insert ON public."RoutePlanningSnapshot" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate')));
ALTER TABLE public."RouteStop" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RouteStop" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RouteStop', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RouteStop" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."RouteStop" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'route.read'));
CREATE POLICY application_insert ON public."RouteStop" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.publish')));
CREATE POLICY application_update ON public."RouteStop" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.publish'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.publish')));
CREATE POLICY application_delete ON public."RouteStop" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("LocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "LocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.publish')));
ALTER TABLE public."Delivery" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Delivery" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Delivery', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Delivery" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Delivery" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DestinationLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DestinationLocationId")) AND ("OriginLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "OriginLocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND EXISTS (SELECT FROM public."DeliveryAssignment" a WHERE a."OrganizationId" = "Delivery"."OrganizationId" AND a."DeliveryId" = "Delivery"."Id" AND a."RevokedAtUtc" IS NULL) AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."Delivery" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DestinationLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DestinationLocationId")) AND ("OriginLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "OriginLocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND EXISTS (SELECT FROM public."DeliveryAssignment" a WHERE a."OrganizationId" = "Delivery"."OrganizationId" AND a."DeliveryId" = "Delivery"."Id" AND a."RevokedAtUtc" IS NULL) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_update ON public."Delivery" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DestinationLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DestinationLocationId")) AND ("OriginLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "OriginLocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND EXISTS (SELECT FROM public."DeliveryAssignment" a WHERE a."OrganizationId" = "Delivery"."OrganizationId" AND a."DeliveryId" = "Delivery"."Id" AND a."RevokedAtUtc" IS NULL) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DestinationLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DestinationLocationId")) AND ("OriginLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "OriginLocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND EXISTS (SELECT FROM public."DeliveryAssignment" a WHERE a."OrganizationId" = "Delivery"."OrganizationId" AND a."DeliveryId" = "Delivery"."Id" AND a."RevokedAtUtc" IS NULL) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_delete ON public."Delivery" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DestinationLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "DestinationLocationId")) AND ("OriginLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "OriginLocationId")) AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND EXISTS (SELECT FROM public."DeliveryAssignment" a WHERE a."OrganizationId" = "Delivery"."OrganizationId" AND a."DeliveryId" = "Delivery"."Id" AND a."RevokedAtUtc" IS NULL) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
ALTER TABLE public."DeliveryLine" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."DeliveryLine" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'DeliveryLine', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."DeliveryLine" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."DeliveryLine" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryLine"."OrganizationId" AND p."Id" = "DeliveryLine"."DeliveryId")) AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."DeliveryLine" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryLine"."OrganizationId" AND p."Id" = "DeliveryLine"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_update ON public."DeliveryLine" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryLine"."OrganizationId" AND p."Id" = "DeliveryLine"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryLine"."OrganizationId" AND p."Id" = "DeliveryLine"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_delete ON public."DeliveryLine" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryLine"."OrganizationId" AND p."Id" = "DeliveryLine"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
ALTER TABLE public."DeliveryHandlingUnit" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."DeliveryHandlingUnit" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'DeliveryHandlingUnit', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."DeliveryHandlingUnit" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."DeliveryHandlingUnit" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryHandlingUnit"."OrganizationId" AND p."Id" = "DeliveryHandlingUnit"."DeliveryId")) AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."DeliveryHandlingUnit" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryHandlingUnit"."OrganizationId" AND p."Id" = "DeliveryHandlingUnit"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_update ON public."DeliveryHandlingUnit" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryHandlingUnit"."OrganizationId" AND p."Id" = "DeliveryHandlingUnit"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryHandlingUnit"."OrganizationId" AND p."Id" = "DeliveryHandlingUnit"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_delete ON public."DeliveryHandlingUnit" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "DeliveryHandlingUnit"."OrganizationId" AND p."Id" = "DeliveryHandlingUnit"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
ALTER TABLE public."DeliveryHandlingUnitLine" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."DeliveryHandlingUnitLine" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'DeliveryHandlingUnitLine', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."DeliveryHandlingUnitLine" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."DeliveryHandlingUnitLine" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."DeliveryHandlingUnitLine" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_update ON public."DeliveryHandlingUnitLine" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_delete ON public."DeliveryHandlingUnitLine" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
ALTER TABLE public."DeliveryAssignment" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."DeliveryAssignment" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'DeliveryAssignment', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."DeliveryAssignment" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."DeliveryAssignment" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "DeliveryAssignment"."OrganizationId" AND a."Id" = "DeliveryAssignment"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."DeliveryAssignment" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "DeliveryAssignment"."OrganizationId" AND a."Id" = "DeliveryAssignment"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.assign')));
CREATE POLICY application_update ON public."DeliveryAssignment" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "DeliveryAssignment"."OrganizationId" AND a."Id" = "DeliveryAssignment"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.assign'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "DeliveryAssignment"."OrganizationId" AND a."Id" = "DeliveryAssignment"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.assign')));
CREATE POLICY application_delete ON public."DeliveryAssignment" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "DeliveryAssignment"."OrganizationId" AND a."Id" = "DeliveryAssignment"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.assign')));
ALTER TABLE public."Receipt" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Receipt" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Receipt', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Receipt" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Receipt" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Receipt"."OrganizationId" AND p."Id" = "Receipt"."DeliveryId")) AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."Receipt" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Receipt"."OrganizationId" AND p."Id" = "Receipt"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_update ON public."Receipt" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Receipt"."OrganizationId" AND p."Id" = "Receipt"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Receipt"."OrganizationId" AND p."Id" = "Receipt"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_delete ON public."Receipt" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Receipt"."OrganizationId" AND p."Id" = "Receipt"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
ALTER TABLE public."ReceiptLine" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ReceiptLine" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ReceiptLine', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ReceiptLine" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ReceiptLine" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReturnLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ReturnLocationId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "ReceiptLine"."OrganizationId" AND p."Id" = "ReceiptLine"."ReceiptId")) AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."ReceiptLine" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReturnLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ReturnLocationId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "ReceiptLine"."OrganizationId" AND p."Id" = "ReceiptLine"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_update ON public."ReceiptLine" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReturnLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ReturnLocationId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "ReceiptLine"."OrganizationId" AND p."Id" = "ReceiptLine"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReturnLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ReturnLocationId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "ReceiptLine"."OrganizationId" AND p."Id" = "ReceiptLine"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
CREATE POLICY application_delete ON public."ReceiptLine" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReturnLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ReturnLocationId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "ReceiptLine"."OrganizationId" AND p."Id" = "ReceiptLine"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
ALTER TABLE public."StoredFile" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."StoredFile" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'StoredFile', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."StoredFile" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."StoredFile" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("CreatedByAccountId" = public.istpetdev_actor_id() OR EXISTS (SELECT FROM public."Evidence" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."MessageAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."FormAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."SimulationArtifact" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.read') OR public.istpetdev_has_permission("OrganizationId", 'message.read') OR public.istpetdev_has_permission("OrganizationId", 'form.read') OR public.istpetdev_has_permission("OrganizationId", 'simulation.read')));
CREATE POLICY application_insert ON public."StoredFile" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("CreatedByAccountId" = public.istpetdev_actor_id() OR EXISTS (SELECT FROM public."Evidence" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."MessageAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."FormAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."SimulationArtifact" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.upload') OR public.istpetdev_has_permission("OrganizationId", 'message.create') OR public.istpetdev_has_permission("OrganizationId", 'form.submit') OR public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
CREATE POLICY application_update ON public."StoredFile" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("CreatedByAccountId" = public.istpetdev_actor_id() OR EXISTS (SELECT FROM public."Evidence" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."MessageAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."FormAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."SimulationArtifact" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.upload') OR public.istpetdev_has_permission("OrganizationId", 'message.create') OR public.istpetdev_has_permission("OrganizationId", 'form.submit') OR public.istpetdev_has_permission("OrganizationId", 'simulation.run'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("CreatedByAccountId" = public.istpetdev_actor_id() OR EXISTS (SELECT FROM public."Evidence" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."MessageAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."FormAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."SimulationArtifact" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.upload') OR public.istpetdev_has_permission("OrganizationId", 'message.create') OR public.istpetdev_has_permission("OrganizationId", 'form.submit') OR public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
CREATE POLICY application_delete ON public."StoredFile" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("CreatedByAccountId" = public.istpetdev_actor_id() OR EXISTS (SELECT FROM public."Evidence" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."MessageAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."FormAttachment" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id") OR EXISTS (SELECT FROM public."SimulationArtifact" a WHERE a."OrganizationId" = "StoredFile"."OrganizationId" AND a."StoredFileId" = "StoredFile"."Id")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.upload') OR public.istpetdev_has_permission("OrganizationId", 'message.create') OR public.istpetdev_has_permission("OrganizationId", 'form.submit') OR public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
ALTER TABLE public."Evidence" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Evidence" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Evidence', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Evidence" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Evidence" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "Evidence"."OrganizationId" AND p."Id" = "Evidence"."ReceiptId")) AND public.istpetdev_has_permission("OrganizationId", 'evidence.read'));
CREATE POLICY application_insert ON public."Evidence" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "Evidence"."OrganizationId" AND p."Id" = "Evidence"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.upload')));
CREATE POLICY application_update ON public."Evidence" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "Evidence"."OrganizationId" AND p."Id" = "Evidence"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.upload'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "Evidence"."OrganizationId" AND p."Id" = "Evidence"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.upload')));
CREATE POLICY application_delete ON public."Evidence" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "Evidence"."OrganizationId" AND p."Id" = "Evidence"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'evidence.upload')));
ALTER TABLE public."CustodyEvent" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."CustodyEvent" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'CustodyEvent', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."CustodyEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."CustodyEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."CustodyEvent" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FromLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "FromLocationId")) AND ("ToLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ToLocationId")) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "CustodyEvent"."OrganizationId" AND p."Id" = "CustodyEvent"."DeliveryId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "CustodyEvent"."OrganizationId" AND p."Id" = "CustodyEvent"."ReceiptId")) AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."CustodyEvent" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("FromLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "FromLocationId")) AND ("ToLocationId" IS NULL OR public.istpetdev_location_allowed("OrganizationId", "ToLocationId")) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "CustodyEvent"."OrganizationId" AND p."Id" = "CustodyEvent"."DeliveryId")) AND ("ReceiptId" IS NULL OR EXISTS (SELECT FROM public."Receipt" p WHERE p."OrganizationId" = "CustodyEvent"."OrganizationId" AND p."Id" = "CustodyEvent"."ReceiptId")) AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch') OR public.istpetdev_has_permission("OrganizationId", 'delivery.capture-receipt') OR public.istpetdev_has_permission("OrganizationId", 'receipt.accept')));
ALTER TABLE public."QrAccessToken" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."QrAccessToken" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'QrAccessToken', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."QrAccessToken" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."QrAccessToken" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'delivery.read'));
CREATE POLICY application_insert ON public."QrAccessToken" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
CREATE POLICY application_update ON public."QrAccessToken" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
CREATE POLICY application_delete ON public."QrAccessToken" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'delivery.dispatch')));
ALTER TABLE public."RiskCalculationRun" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RiskCalculationRun" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RiskCalculationRun', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RiskCalculationRun" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."RiskCalculationRun" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'risk.read'));
CREATE POLICY application_insert ON public."RiskCalculationRun" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
CREATE POLICY application_update ON public."RiskCalculationRun" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
CREATE POLICY application_delete ON public."RiskCalculationRun" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
ALTER TABLE public."RiskSnapshot" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RiskSnapshot" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RiskSnapshot', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RiskSnapshot" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."RiskSnapshot" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'risk.read'));
CREATE POLICY application_insert ON public."RiskSnapshot" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
CREATE POLICY application_update ON public."RiskSnapshot" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
CREATE POLICY application_delete ON public."RiskSnapshot" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
ALTER TABLE public."RiskExplanation" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RiskExplanation" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RiskExplanation', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RiskExplanation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."RiskExplanation" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'risk.read'));
CREATE POLICY application_insert ON public."RiskExplanation" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
CREATE POLICY application_update ON public."RiskExplanation" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
CREATE POLICY application_delete ON public."RiskExplanation" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'risk.explain')));
ALTER TABLE public."AnomalyCase" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AnomalyCase" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'AnomalyCase', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."AnomalyCase" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."AnomalyCase" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'anomaly.read'));
CREATE POLICY application_insert ON public."AnomalyCase" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'anomaly.resolve')));
CREATE POLICY application_update ON public."AnomalyCase" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'anomaly.resolve'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'anomaly.resolve')));
CREATE POLICY application_delete ON public."AnomalyCase" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'anomaly.resolve')));
ALTER TABLE public."Incident" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Incident" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Incident', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Incident" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Incident" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND public.istpetdev_has_permission("OrganizationId", 'incident.read'));
CREATE POLICY application_insert ON public."Incident" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'incident.create') OR public.istpetdev_has_permission("OrganizationId", 'demo.incident.create')));
CREATE POLICY application_update ON public."Incident" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'incident.create') OR public.istpetdev_has_permission("OrganizationId", 'demo.incident.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'incident.create') OR public.istpetdev_has_permission("OrganizationId", 'demo.incident.create')));
CREATE POLICY application_delete ON public."Incident" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ServicePointId" IS NULL OR public.istpetdev_point_allowed("OrganizationId", "ServicePointId")) AND (public.istpetdev_has_permission("OrganizationId", 'incident.create') OR public.istpetdev_has_permission("OrganizationId", 'demo.incident.create')));
ALTER TABLE public."IncidentRoadRestriction" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."IncidentRoadRestriction" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'IncidentRoadRestriction', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."IncidentRoadRestriction" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."IncidentRoadRestriction" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'incident.read'));
CREATE POLICY application_insert ON public."IncidentRoadRestriction" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'incident.create') OR public.istpetdev_has_permission("OrganizationId", 'demo.incident.create')));
CREATE POLICY application_update ON public."IncidentRoadRestriction" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'incident.create') OR public.istpetdev_has_permission("OrganizationId", 'demo.incident.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'incident.create') OR public.istpetdev_has_permission("OrganizationId", 'demo.incident.create')));
CREATE POLICY application_delete ON public."IncidentRoadRestriction" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'incident.create') OR public.istpetdev_has_permission("OrganizationId", 'demo.incident.create')));
ALTER TABLE public."RoutePlanIncident" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."RoutePlanIncident" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'RoutePlanIncident', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."RoutePlanIncident" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."RoutePlanIncident" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'route.read'));
CREATE POLICY application_insert ON public."RoutePlanIncident" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate')));
CREATE POLICY application_update ON public."RoutePlanIncident" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate')));
CREATE POLICY application_delete ON public."RoutePlanIncident" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'route.calculate')));
ALTER TABLE public."IntegrationConnection" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."IntegrationConnection" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'IntegrationConnection', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."IntegrationConnection" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."IntegrationConnection" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'integration.read'));
CREATE POLICY application_insert ON public."IntegrationConnection" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'integration.manage')));
CREATE POLICY application_update ON public."IntegrationConnection" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'integration.manage'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'integration.manage')));
CREATE POLICY application_delete ON public."IntegrationConnection" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'integration.manage')));
ALTER TABLE public."JobExecution" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."JobExecution" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'JobExecution', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."JobExecution" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."JobExecution" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'integration.read'));
CREATE POLICY application_insert ON public."JobExecution" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'simulation.run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-explain')));
CREATE POLICY application_update ON public."JobExecution" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'simulation.run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-explain'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'simulation.run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-explain')));
CREATE POLICY application_delete ON public."JobExecution" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'risk.calculate') OR public.istpetdev_has_permission("OrganizationId", 'route.calculate') OR public.istpetdev_has_permission("OrganizationId", 'simulation.run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-explain')));
ALTER TABLE public."IntegrationRun" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."IntegrationRun" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'IntegrationRun', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."IntegrationRun" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."IntegrationRun" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'integration.read'));
CREATE POLICY application_insert ON public."IntegrationRun" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'automation.risk-run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-explain')));
CREATE POLICY application_update ON public."IntegrationRun" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'automation.risk-run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-explain'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'automation.risk-run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-explain')));
CREATE POLICY application_delete ON public."IntegrationRun" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'automation.risk-run') OR public.istpetdev_has_permission("OrganizationId", 'automation.risk-explain')));
ALTER TABLE public."Conversation" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Conversation" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Conversation', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Conversation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Conversation" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."ConversationParticipant" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."ConversationId" = "Conversation"."Id" AND p."RevokedAtUtc" IS NULL) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."Id" = "Conversation"."DeliveryId")) AND public.istpetdev_has_permission("OrganizationId", 'conversation.read'));
CREATE POLICY application_insert ON public."Conversation" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."ConversationParticipant" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."ConversationId" = "Conversation"."Id" AND p."RevokedAtUtc" IS NULL) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."Id" = "Conversation"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'conversation.manage') OR public.istpetdev_has_permission("OrganizationId", 'message.create')));
CREATE POLICY application_update ON public."Conversation" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."ConversationParticipant" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."ConversationId" = "Conversation"."Id" AND p."RevokedAtUtc" IS NULL) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."Id" = "Conversation"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'conversation.manage') OR public.istpetdev_has_permission("OrganizationId", 'message.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."ConversationParticipant" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."ConversationId" = "Conversation"."Id" AND p."RevokedAtUtc" IS NULL) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."Id" = "Conversation"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'conversation.manage') OR public.istpetdev_has_permission("OrganizationId", 'message.create')));
CREATE POLICY application_delete ON public."Conversation" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."ConversationParticipant" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."ConversationId" = "Conversation"."Id" AND p."RevokedAtUtc" IS NULL) AND ("DeliveryId" IS NULL OR EXISTS (SELECT FROM public."Delivery" p WHERE p."OrganizationId" = "Conversation"."OrganizationId" AND p."Id" = "Conversation"."DeliveryId")) AND (public.istpetdev_has_permission("OrganizationId", 'conversation.manage') OR public.istpetdev_has_permission("OrganizationId", 'message.create')));
ALTER TABLE public."ConversationParticipant" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ConversationParticipant" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ConversationParticipant', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ConversationParticipant" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ConversationParticipant" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "ConversationParticipant"."OrganizationId" AND a."Id" = "ConversationParticipant"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND public.istpetdev_has_permission("OrganizationId", 'conversation.read'));
CREATE POLICY application_insert ON public."ConversationParticipant" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "ConversationParticipant"."OrganizationId" AND a."Id" = "ConversationParticipant"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND (public.istpetdev_has_permission("OrganizationId", 'conversation.manage') OR public.istpetdev_has_permission("OrganizationId", 'message.create')));
CREATE POLICY application_update ON public."ConversationParticipant" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "ConversationParticipant"."OrganizationId" AND a."Id" = "ConversationParticipant"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND (public.istpetdev_has_permission("OrganizationId", 'conversation.manage') OR public.istpetdev_has_permission("OrganizationId", 'message.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "ConversationParticipant"."OrganizationId" AND a."Id" = "ConversationParticipant"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND (public.istpetdev_has_permission("OrganizationId", 'conversation.manage') OR public.istpetdev_has_permission("OrganizationId", 'message.create')));
CREATE POLICY application_delete ON public."ConversationParticipant" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "ConversationParticipant"."OrganizationId" AND a."Id" = "ConversationParticipant"."OrganizationAccessId" AND a."AccountId" = public.istpetdev_actor_id()) AND (public.istpetdev_has_permission("OrganizationId", 'conversation.manage') OR public.istpetdev_has_permission("OrganizationId", 'message.create')));
ALTER TABLE public."Message" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Message" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Message', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Message" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Message" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ConversationId" IS NULL OR EXISTS (SELECT FROM public."Conversation" p WHERE p."OrganizationId" = "Message"."OrganizationId" AND p."Id" = "Message"."ConversationId")) AND public.istpetdev_has_permission("OrganizationId", 'message.read'));
CREATE POLICY application_insert ON public."Message" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ConversationId" IS NULL OR EXISTS (SELECT FROM public."Conversation" p WHERE p."OrganizationId" = "Message"."OrganizationId" AND p."Id" = "Message"."ConversationId")) AND (public.istpetdev_has_permission("OrganizationId", 'message.create')));
CREATE POLICY application_update ON public."Message" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ConversationId" IS NULL OR EXISTS (SELECT FROM public."Conversation" p WHERE p."OrganizationId" = "Message"."OrganizationId" AND p."Id" = "Message"."ConversationId")) AND (public.istpetdev_has_permission("OrganizationId", 'message.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ConversationId" IS NULL OR EXISTS (SELECT FROM public."Conversation" p WHERE p."OrganizationId" = "Message"."OrganizationId" AND p."Id" = "Message"."ConversationId")) AND (public.istpetdev_has_permission("OrganizationId", 'message.create')));
CREATE POLICY application_delete ON public."Message" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("ConversationId" IS NULL OR EXISTS (SELECT FROM public."Conversation" p WHERE p."OrganizationId" = "Message"."OrganizationId" AND p."Id" = "Message"."ConversationId")) AND (public.istpetdev_has_permission("OrganizationId", 'message.create')));
ALTER TABLE public."MessageAttachment" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."MessageAttachment" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'MessageAttachment', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."MessageAttachment" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."MessageAttachment" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("MessageId" IS NULL OR EXISTS (SELECT FROM public."Message" p WHERE p."OrganizationId" = "MessageAttachment"."OrganizationId" AND p."Id" = "MessageAttachment"."MessageId")) AND public.istpetdev_has_permission("OrganizationId", 'message.read'));
CREATE POLICY application_insert ON public."MessageAttachment" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("MessageId" IS NULL OR EXISTS (SELECT FROM public."Message" p WHERE p."OrganizationId" = "MessageAttachment"."OrganizationId" AND p."Id" = "MessageAttachment"."MessageId")) AND (public.istpetdev_has_permission("OrganizationId", 'message.create')));
CREATE POLICY application_update ON public."MessageAttachment" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("MessageId" IS NULL OR EXISTS (SELECT FROM public."Message" p WHERE p."OrganizationId" = "MessageAttachment"."OrganizationId" AND p."Id" = "MessageAttachment"."MessageId")) AND (public.istpetdev_has_permission("OrganizationId", 'message.create'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("MessageId" IS NULL OR EXISTS (SELECT FROM public."Message" p WHERE p."OrganizationId" = "MessageAttachment"."OrganizationId" AND p."Id" = "MessageAttachment"."MessageId")) AND (public.istpetdev_has_permission("OrganizationId", 'message.create')));
CREATE POLICY application_delete ON public."MessageAttachment" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("MessageId" IS NULL OR EXISTS (SELECT FROM public."Message" p WHERE p."OrganizationId" = "MessageAttachment"."OrganizationId" AND p."Id" = "MessageAttachment"."MessageId")) AND (public.istpetdev_has_permission("OrganizationId", 'message.create')));
ALTER TABLE public."Notification" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Notification" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'Notification', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."Notification" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."Notification" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND EXISTS (SELECT FROM public."OrganizationAccess" a WHERE a."OrganizationId" = "Notification"."OrganizationId" AND a."Id" = "Notification"."RecipientAccessId" AND a."AccountId" = public.istpetdev_actor_id()));
CREATE POLICY application_insert ON public."Notification" FOR INSERT WITH CHECK (false);
CREATE POLICY application_update ON public."Notification" FOR UPDATE USING (false) WITH CHECK (false);
CREATE POLICY application_delete ON public."Notification" FOR DELETE USING (false);
ALTER TABLE public."IdempotencyRecord" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."IdempotencyRecord" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'IdempotencyRecord', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."IdempotencyRecord" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."IdempotencyRecord" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND "AccountId" = public.istpetdev_actor_id());
CREATE POLICY application_insert ON public."IdempotencyRecord" FOR INSERT WITH CHECK (false);
CREATE POLICY application_update ON public."IdempotencyRecord" FOR UPDATE USING (false) WITH CHECK (false);
CREATE POLICY application_delete ON public."IdempotencyRecord" FOR DELETE USING (false);
ALTER TABLE public."OutboxEvent" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."OutboxEvent" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'OutboxEvent', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."OutboxEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."OutboxEvent" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId"));
CREATE POLICY application_insert ON public."OutboxEvent" FOR INSERT WITH CHECK (false);
CREATE POLICY application_update ON public."OutboxEvent" FOR UPDATE USING (false) WITH CHECK (false);
CREATE POLICY application_delete ON public."OutboxEvent" FOR DELETE USING (false);
ALTER TABLE public."AuditEvent" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."AuditEvent" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'AuditEvent', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."AuditEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."AuditEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."AuditEvent" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'audit.read'));
CREATE POLICY application_insert ON public."AuditEvent" FOR INSERT WITH CHECK (false);
ALTER TABLE public."SyncBatch" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."SyncBatch" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'SyncBatch', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."SyncBatch" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."SyncBatch" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND "AccountId" = public.istpetdev_actor_id());
CREATE POLICY application_insert ON public."SyncBatch" FOR INSERT WITH CHECK (false);
CREATE POLICY application_update ON public."SyncBatch" FOR UPDATE USING (false) WITH CHECK (false);
CREATE POLICY application_delete ON public."SyncBatch" FOR DELETE USING (false);
ALTER TABLE public."SyncOperation" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."SyncOperation" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'SyncOperation', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."SyncOperation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."SyncOperation" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId"));
CREATE POLICY application_insert ON public."SyncOperation" FOR INSERT WITH CHECK (false);
CREATE POLICY application_update ON public."SyncOperation" FOR UPDATE USING (false) WITH CHECK (false);
CREATE POLICY application_delete ON public."SyncOperation" FOR DELETE USING (false);
ALTER TABLE public."ScenarioDataset" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ScenarioDataset" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ScenarioDataset', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ScenarioDataset" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."ScenarioDataset" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'simulation.read'));
CREATE POLICY application_insert ON public."ScenarioDataset" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
CREATE POLICY application_update ON public."ScenarioDataset" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
CREATE POLICY application_delete ON public."ScenarioDataset" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
ALTER TABLE public."ScenarioDatasetVersion" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ScenarioDatasetVersion" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ScenarioDatasetVersion', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ScenarioDatasetVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."ScenarioDatasetVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."ScenarioDatasetVersion" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'simulation.read'));
CREATE POLICY application_insert ON public."ScenarioDatasetVersion" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
ALTER TABLE public."ScenarioEvent" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."ScenarioEvent" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'ScenarioEvent', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."ScenarioEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."ScenarioEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."ScenarioEvent" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'simulation.read'));
CREATE POLICY application_insert ON public."ScenarioEvent" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
ALTER TABLE public."SimulationPolicyVersion" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."SimulationPolicyVersion" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'SimulationPolicyVersion', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."SimulationPolicyVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."SimulationPolicyVersion" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."SimulationPolicyVersion" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'simulation.read'));
CREATE POLICY application_insert ON public."SimulationPolicyVersion" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
ALTER TABLE public."SimulationRun" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."SimulationRun" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'SimulationRun', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."SimulationRun" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE POLICY application_read ON public."SimulationRun" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND public.istpetdev_has_permission("OrganizationId", 'simulation.read'));
CREATE POLICY application_insert ON public."SimulationRun" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
CREATE POLICY application_update ON public."SimulationRun" FOR UPDATE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run'))) WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
CREATE POLICY application_delete ON public."SimulationRun" FOR DELETE USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
ALTER TABLE public."SimulationEvent" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."SimulationEvent" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'SimulationEvent', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."SimulationEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."SimulationEvent" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."SimulationEvent" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("SimulationRunId" IS NULL OR EXISTS (SELECT FROM public."SimulationRun" p WHERE p."OrganizationId" = "SimulationEvent"."OrganizationId" AND p."Id" = "SimulationEvent"."SimulationRunId")) AND public.istpetdev_has_permission("OrganizationId", 'simulation.read'));
CREATE POLICY application_insert ON public."SimulationEvent" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("SimulationRunId" IS NULL OR EXISTS (SELECT FROM public."SimulationRun" p WHERE p."OrganizationId" = "SimulationEvent"."OrganizationId" AND p."Id" = "SimulationEvent"."SimulationRunId")) AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
ALTER TABLE public."SimulationMetric" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."SimulationMetric" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'SimulationMetric', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."SimulationMetric" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."SimulationMetric" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."SimulationMetric" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("SimulationRunId" IS NULL OR EXISTS (SELECT FROM public."SimulationRun" p WHERE p."OrganizationId" = "SimulationMetric"."OrganizationId" AND p."Id" = "SimulationMetric"."SimulationRunId")) AND public.istpetdev_has_permission("OrganizationId", 'simulation.read'));
CREATE POLICY application_insert ON public."SimulationMetric" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("SimulationRunId" IS NULL OR EXISTS (SELECT FROM public."SimulationRun" p WHERE p."OrganizationId" = "SimulationMetric"."OrganizationId" AND p."Id" = "SimulationMetric"."SimulationRunId")) AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
ALTER TABLE public."SimulationArtifact" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."SimulationArtifact" FORCE ROW LEVEL SECURITY;
DO $$ BEGIN EXECUTE format('CREATE POLICY migration_owner ON public.%I TO %I USING (true) WITH CHECK (true)', 'SimulationArtifact', current_user); END $$;
CREATE TRIGGER stamp_row BEFORE INSERT OR UPDATE ON public."SimulationArtifact" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_stamp_row();
CREATE TRIGGER insert_only BEFORE UPDATE OR DELETE ON public."SimulationArtifact" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_insert_only();
CREATE POLICY application_read ON public."SimulationArtifact" FOR SELECT USING ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("SimulationRunId" IS NULL OR EXISTS (SELECT FROM public."SimulationRun" p WHERE p."OrganizationId" = "SimulationArtifact"."OrganizationId" AND p."Id" = "SimulationArtifact"."SimulationRunId")) AND public.istpetdev_has_permission("OrganizationId", 'simulation.read'));
CREATE POLICY application_insert ON public."SimulationArtifact" FOR INSERT WITH CHECK ("OrganizationId" = public.istpetdev_organization_id() AND public.istpetdev_is_member("OrganizationId") AND ("SimulationRunId" IS NULL OR EXISTS (SELECT FROM public."SimulationRun" p WHERE p."OrganizationId" = "SimulationArtifact"."OrganizationId" AND p."Id" = "SimulationArtifact"."SimulationRunId")) AND (public.istpetdev_has_permission("OrganizationId", 'simulation.run')));
-- Cross-row relationships supplement the composite tenant/version foreign keys.
CREATE FUNCTION public.istpetdev_relationship_guard() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog
AS $$
DECLARE valid boolean;
BEGIN
    CASE TG_TABLE_NAME
    WHEN 'RoleFieldPermission' THEN
        SELECT NOT NEW."CanWrite" OR NOT d."IsServerControlled" INTO valid
        FROM public."FieldDefinition" d WHERE d."Id" = NEW."FieldDefinitionId";
    WHEN 'FormAttachment' THEN
        SELECT s."TemplateVersionId" = f."TemplateVersionId" INTO valid
        FROM public."FormSubmission" s, public."TemplateField" f
        WHERE s."OrganizationId" = NEW."OrganizationId" AND s."Id" = NEW."FormSubmissionId"
          AND f."OrganizationId" = NEW."OrganizationId" AND f."Id" = NEW."TemplateFieldId";
    WHEN 'ReceiptLine' THEN
        SELECT r."DeliveryId" = l."DeliveryId" INTO valid
        FROM public."Receipt" r, public."DeliveryLine" l
        WHERE r."OrganizationId" = NEW."OrganizationId" AND r."Id" = NEW."ReceiptId"
          AND l."OrganizationId" = NEW."OrganizationId" AND l."Id" = NEW."DeliveryLineId";
    WHEN 'Evidence' THEN
        IF NEW."ReceiptLineId" IS NULL THEN RETURN NEW; END IF;
        SELECT l."ReceiptId" = NEW."ReceiptId" INTO valid FROM public."ReceiptLine" l
        WHERE l."OrganizationId" = NEW."OrganizationId" AND l."Id" = NEW."ReceiptLineId";
    WHEN 'DeliveryHandlingUnitLine' THEN
        SELECT unit."DeliveryId" = line."DeliveryId" AND composition."HandlingUnitId" = unit."HandlingUnitId"
          AND composition."LotId" = line."LotId" INTO valid
        FROM public."DeliveryHandlingUnit" unit, public."DeliveryLine" line, public."HandlingUnitLine" composition
        WHERE unit."OrganizationId" = NEW."OrganizationId" AND unit."Id" = NEW."DeliveryHandlingUnitId"
          AND line."OrganizationId" = NEW."OrganizationId" AND line."Id" = NEW."DeliveryLineId"
          AND composition."OrganizationId" = NEW."OrganizationId" AND composition."Id" = NEW."HandlingUnitLineId";
    WHEN 'DeliveryLine' THEN
        IF NEW."ReplenishmentRequestLineId" IS NULL THEN RETURN NEW; END IF;
        SELECT request."ProductId" = lot."ProductId" INTO valid
        FROM public."ReplenishmentRequestLine" request, public."Lot" lot
        WHERE request."OrganizationId" = NEW."OrganizationId" AND request."Id" = NEW."ReplenishmentRequestLineId"
          AND lot."OrganizationId" = NEW."OrganizationId" AND lot."Id" = NEW."LotId";
    WHEN 'Reservation' THEN
        SELECT l."LotId" = NEW."LotId" INTO valid FROM public."DeliveryLine" l
        WHERE l."OrganizationId" = NEW."OrganizationId" AND l."Id" = NEW."DeliveryLineId";
    WHEN 'SyncOperation' THEN
        SELECT batch."AccountId" = record."AccountId" AND record."ActionCode" = NEW."ActionCode" INTO valid
        FROM public."SyncBatch" batch, public."IdempotencyRecord" record
        WHERE batch."OrganizationId" = NEW."OrganizationId" AND batch."Id" = NEW."SyncBatchId"
          AND record."OrganizationId" = NEW."OrganizationId" AND record."Id" = NEW."IdempotencyRecordId";
    END CASE;
    IF valid IS DISTINCT FROM true THEN
        RAISE EXCEPTION 'Incompatible parent/version/lot/actor relationship in %', TG_TABLE_NAME USING ERRCODE = '23514';
    END IF;
    RETURN NEW;
END $$;

CREATE FUNCTION public.istpetdev_activation_guard() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog
AS $$
DECLARE conflict_found boolean; published boolean;
BEGIN
    -- ponytail: serialize bindings per organization; split by definition if contention appears.
    PERFORM pg_advisory_xact_lock(hashtextextended(NEW."OrganizationId"::text, 0));
    IF TG_TABLE_NAME = 'ConfigurationActivation' THEN
        SELECT v."PublishedAtUtc" IS NOT NULL INTO published FROM public."ConfigurationVersion" v
        WHERE v."OrganizationId" = NEW."OrganizationId" AND v."Id" = NEW."ConfigurationVersionId" FOR UPDATE;
        SELECT EXISTS (
            SELECT FROM public."ConfigurationActivation" a
            JOIN public."ConfigurationVersion" existing ON existing."OrganizationId" = a."OrganizationId" AND existing."Id" = a."ConfigurationVersionId"
            JOIN public."ConfigurationVersion" selected ON selected."OrganizationId" = NEW."OrganizationId" AND selected."Id" = NEW."ConfigurationVersionId"
            WHERE a."OrganizationId" = NEW."OrganizationId" AND a."Id" <> NEW."Id"
              AND existing."ConfigurationDefinitionId" = selected."ConfigurationDefinitionId"
              AND a."ScopeKind" = NEW."ScopeKind" AND a."LocationId" IS NOT DISTINCT FROM NEW."LocationId"
              AND a."ServicePointId" IS NOT DISTINCT FROM NEW."ServicePointId"
              AND tstzrange(a."EffectiveFromUtc", a."EffectiveToUtc", '[)') && tstzrange(NEW."EffectiveFromUtc", NEW."EffectiveToUtc", '[)')
        ) INTO conflict_found;
    ELSE
        SELECT v."PublishedAtUtc" IS NOT NULL AND v."Status" = 'published' INTO published FROM public."TemplateVersion" v
        WHERE v."OrganizationId" = NEW."OrganizationId" AND v."Id" = NEW."TemplateVersionId" FOR UPDATE;
        SELECT EXISTS (
            SELECT FROM public."TemplateBinding" b
            WHERE b."OrganizationId" = NEW."OrganizationId" AND b."Id" <> NEW."Id"
              AND b."ModuleCode" = NEW."ModuleCode" AND b."ActionCode" = NEW."ActionCode"
              AND b."ScopeKind" = NEW."ScopeKind" AND b."LocationId" IS NOT DISTINCT FROM NEW."LocationId"
              AND b."ServicePointId" IS NOT DISTINCT FROM NEW."ServicePointId"
              AND tstzrange(b."EffectiveFromUtc", b."EffectiveToUtc", '[)') && tstzrange(NEW."EffectiveFromUtc", NEW."EffectiveToUtc", '[)')
        ) INTO conflict_found;
    END IF;
    IF published IS DISTINCT FROM true THEN RAISE EXCEPTION 'Activation requires a published version' USING ERRCODE='23514'; END IF;
    IF conflict_found THEN RAISE EXCEPTION 'Overlapping activation/binding' USING ERRCODE='23514'; END IF;
    RETURN NEW;
END $$;

CREATE FUNCTION public.istpetdev_frozen_option() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog
AS $$
DECLARE row_data jsonb; old_data jsonb; published boolean;
BEGIN
    row_data := CASE WHEN TG_OP = 'DELETE' THEN to_jsonb(OLD) ELSE to_jsonb(NEW) END;
    old_data := CASE WHEN TG_OP = 'INSERT' THEN row_data ELSE to_jsonb(OLD) END;
    FOR published IN SELECT v."PublishedAtUtc" IS NOT NULL
        FROM public."TemplateVersion" v JOIN public."TemplateField" f ON f."OrganizationId" = v."OrganizationId" AND f."TemplateVersionId" = v."Id"
        WHERE f."OrganizationId" = (row_data->>'OrganizationId')::uuid
          AND f."Id" IN ((row_data->>'TemplateFieldId')::uuid, (old_data->>'TemplateFieldId')::uuid)
        FOR UPDATE OF v LOOP
        IF published THEN RAISE EXCEPTION 'Published template options are immutable' USING ERRCODE='23514'; END IF;
    END LOOP;
    IF TG_OP='DELETE' THEN RETURN OLD; END IF;
    RETURN NEW;
END $$;

DO $$
DECLARE table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['RoleFieldPermission','FormAttachment','ReceiptLine','Evidence','DeliveryHandlingUnitLine','DeliveryLine','Reservation','SyncOperation'] LOOP
        EXECUTE format('CREATE TRIGGER relationship_guard BEFORE INSERT OR UPDATE ON public.%I FOR EACH ROW EXECUTE FUNCTION public.istpetdev_relationship_guard()',table_name);
    END LOOP;
END $$;
CREATE TRIGGER activation_guard BEFORE INSERT OR UPDATE ON public."ConfigurationActivation" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_activation_guard();
CREATE TRIGGER activation_guard BEFORE INSERT OR UPDATE ON public."TemplateBinding" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_activation_guard();
CREATE TRIGGER freeze_option BEFORE INSERT OR UPDATE OR DELETE ON public."TemplateFieldOption" FOR EACH ROW EXECUTE FUNCTION public.istpetdev_frozen_option();

-- Apply to the existing per-developer roles, without creating or rotating identities.
DO $grants$
DECLARE role_name text; table_name text; function_name regprocedure;
BEGIN
    IF current_database() = 'istpetdev_integration' THEN
        FOR role_name IN SELECT rolname FROM pg_roles WHERE rolname ~ '^user_integ_dev_0[1-5]$' LOOP
            EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', role_name);
        EXECUTE format('REVOKE INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER ON TABLE public."__EFMigrationsHistory" FROM %I', role_name);
            EXECUTE format('REVOKE CREATE ON SCHEMA public FROM %I', role_name);
            FOREACH table_name IN ARRAY ARRAY['AccessDelegation','AccessLocation','AccessRole','AccessServicePoint','Account','AnomalyCase','AuditEvent','Catalog','CatalogOption','ConfigurationActivation','ConfigurationDefinition','ConfigurationVersion','Consumption','Conversation','ConversationParticipant','CustodyEvent','Delivery','DeliveryAssignment','DeliveryHandlingUnit','DeliveryHandlingUnitLine','DeliveryLine','DemandObservation','Evidence','FieldDefinition','FormAttachment','FormSubmission','HandlingUnit','HandlingUnitLine','IdempotencyRecord','Incident','IncidentRoadRestriction','IntegrationConnection','IntegrationRun','InventoryMovement','InventorySnapshot','InventorySnapshotLine','JobExecution','Location','Lot','Message','MessageAttachment','Notification','Organization','OrganizationAccess','OutboxEvent','Permission','Product','ProductUnitConversion','QrAccessToken','RbacChange','Receipt','ReceiptLine','ReplenishmentRequest','ReplenishmentRequestLine','Reservation','RiskCalculationRun','RiskExplanation','RiskSnapshot','RoadMatrix','RoadMatrixEdge','Role','RoleFieldPermission','RolePermission','RoutePlan','RoutePlanIncident','RoutePlanningSnapshot','RouteStop','ScenarioDataset','ScenarioDatasetVersion','ScenarioEvent','ServicePoint','ServicePointContact','ServicePointWindow','SimulationArtifact','SimulationEvent','SimulationMetric','SimulationPolicyVersion','SimulationRun','StockBalance','StoredFile','SyncBatch','SyncOperation','Template','TemplateBinding','TemplateField','TemplateFieldAccess','TemplateFieldOption','TemplateVersion','Vehicle','VehicleAvailability','VehiclePosition','WorkflowDefinition','WorkflowState','WorkflowTransition','WorkflowVersion'] LOOP
                EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC, %I', table_name, role_name);
                EXECUTE format('GRANT SELECT ON TABLE public.%I TO %I', table_name, role_name);
                IF NOT table_name = ANY(ARRAY['AccessDelegation','AccessLocation','AccessRole','AccessServicePoint','Account','ConfigurationDefinition','FieldDefinition','Organization','OrganizationAccess','Permission','Role','RoleFieldPermission','RolePermission']) THEN
                    EXECUTE format('GRANT INSERT ON TABLE public.%I TO %I', table_name, role_name);
                    IF NOT table_name = ANY(ARRAY['AuditEvent','CustodyEvent','FormSubmission','InventoryMovement','InventorySnapshot','InventorySnapshotLine','RbacChange','RoadMatrix','RoadMatrixEdge','RoutePlanningSnapshot','ScenarioDatasetVersion','ScenarioEvent','SimulationArtifact','SimulationEvent','SimulationMetric','SimulationPolicyVersion','VehiclePosition']) THEN
                        EXECUTE format('GRANT UPDATE, DELETE ON TABLE public.%I TO %I', table_name, role_name);
                    END IF;
                END IF;
            END LOOP;
            FOR function_name IN SELECT p.oid::regprocedure FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public' AND p.proname LIKE 'istpetdev_%' LOOP
                EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC', function_name);
                EXECUTE format('GRANT EXECUTE ON FUNCTION %s TO %I', function_name, role_name);
            END LOOP;
            EXECUTE format('GRANT USAGE, SELECT ON SEQUENCE public."InventoryMovement_RecordedSequence_seq" TO %I',role_name);
        END LOOP;
    ELSE
        role_name := 'user_' || substring(current_database() from 11);
        IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = role_name) THEN RAISE EXCEPTION 'Missing application role %', role_name; END IF;
        EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', role_name);
        EXECUTE format('REVOKE INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER ON TABLE public."__EFMigrationsHistory" FROM %I', role_name);
        EXECUTE format('REVOKE CREATE ON SCHEMA public FROM %I', role_name);
        FOREACH table_name IN ARRAY ARRAY['AccessDelegation','AccessLocation','AccessRole','AccessServicePoint','Account','AnomalyCase','AuditEvent','Catalog','CatalogOption','ConfigurationActivation','ConfigurationDefinition','ConfigurationVersion','Consumption','Conversation','ConversationParticipant','CustodyEvent','Delivery','DeliveryAssignment','DeliveryHandlingUnit','DeliveryHandlingUnitLine','DeliveryLine','DemandObservation','Evidence','FieldDefinition','FormAttachment','FormSubmission','HandlingUnit','HandlingUnitLine','IdempotencyRecord','Incident','IncidentRoadRestriction','IntegrationConnection','IntegrationRun','InventoryMovement','InventorySnapshot','InventorySnapshotLine','JobExecution','Location','Lot','Message','MessageAttachment','Notification','Organization','OrganizationAccess','OutboxEvent','Permission','Product','ProductUnitConversion','QrAccessToken','RbacChange','Receipt','ReceiptLine','ReplenishmentRequest','ReplenishmentRequestLine','Reservation','RiskCalculationRun','RiskExplanation','RiskSnapshot','RoadMatrix','RoadMatrixEdge','Role','RoleFieldPermission','RolePermission','RoutePlan','RoutePlanIncident','RoutePlanningSnapshot','RouteStop','ScenarioDataset','ScenarioDatasetVersion','ScenarioEvent','ServicePoint','ServicePointContact','ServicePointWindow','SimulationArtifact','SimulationEvent','SimulationMetric','SimulationPolicyVersion','SimulationRun','StockBalance','StoredFile','SyncBatch','SyncOperation','Template','TemplateBinding','TemplateField','TemplateFieldAccess','TemplateFieldOption','TemplateVersion','Vehicle','VehicleAvailability','VehiclePosition','WorkflowDefinition','WorkflowState','WorkflowTransition','WorkflowVersion'] LOOP
            EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC, %I', table_name, role_name);
            EXECUTE format('GRANT SELECT ON TABLE public.%I TO %I', table_name, role_name);
            IF NOT table_name = ANY(ARRAY['AccessDelegation','AccessLocation','AccessRole','AccessServicePoint','Account','ConfigurationDefinition','FieldDefinition','Organization','OrganizationAccess','Permission','Role','RoleFieldPermission','RolePermission']) THEN
                EXECUTE format('GRANT INSERT ON TABLE public.%I TO %I', table_name, role_name);
                IF NOT table_name = ANY(ARRAY['AuditEvent','CustodyEvent','FormSubmission','InventoryMovement','InventorySnapshot','InventorySnapshotLine','RbacChange','RoadMatrix','RoadMatrixEdge','RoutePlanningSnapshot','ScenarioDatasetVersion','ScenarioEvent','SimulationArtifact','SimulationEvent','SimulationMetric','SimulationPolicyVersion','VehiclePosition']) THEN
                    EXECUTE format('GRANT UPDATE, DELETE ON TABLE public.%I TO %I', table_name, role_name);
                END IF;
            END IF;
        END LOOP;
        FOR function_name IN SELECT p.oid::regprocedure FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public' AND p.proname LIKE 'istpetdev_%' LOOP
            EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC', function_name);
            EXECUTE format('GRANT EXECUTE ON FUNCTION %s TO %I', function_name, role_name);
        END LOOP;
        EXECUTE format('GRANT USAGE, SELECT ON SEQUENCE public."InventoryMovement_RecordedSequence_seq" TO %I',role_name);
    END IF;
END $grants$;

-- One explicit demo organization; no account is authorized by default.
INSERT INTO public."Organization" ("Id","Code","Name","TimeZone","IsActive","AuthorizationVersion")
VALUES ('01990000-0000-7000-8000-000000000001','demo','ISTPET Demo','America/Guayaquil',true,1);
-- Only the already-implemented service-point listing is registered as an action.
INSERT INTO public."Permission" ("Id","Code","ModuleCode","ActionCode","ResourceKind","IsDelegable")
VALUES ('01990000-0000-7000-8000-000000000002','service-point.read','catalog','read','service-point',true);
INSERT INTO public."Role" ("Id","OrganizationId","Code","Name","IsActive","IsSeed","IsTechnical") VALUES ('01990000-0000-7000-8000-000000000010','01990000-0000-7000-8000-000000000001','gerencia','Gerencia',true,true,false);
INSERT INTO public."Role" ("Id","OrganizationId","Code","Name","IsActive","IsSeed","IsTechnical") VALUES ('01990000-0000-7000-8000-000000000011','01990000-0000-7000-8000-000000000001','planificador','Planificador',true,true,false);
INSERT INTO public."Role" ("Id","OrganizationId","Code","Name","IsActive","IsSeed","IsTechnical") VALUES ('01990000-0000-7000-8000-000000000012','01990000-0000-7000-8000-000000000001','bodega','Bodega',true,true,false);
INSERT INTO public."Role" ("Id","OrganizationId","Code","Name","IsActive","IsSeed","IsTechnical") VALUES ('01990000-0000-7000-8000-000000000013','01990000-0000-7000-8000-000000000001','conductor','Conductor',true,true,false);
INSERT INTO public."Role" ("Id","OrganizationId","Code","Name","IsActive","IsSeed","IsTechnical") VALUES ('01990000-0000-7000-8000-000000000014','01990000-0000-7000-8000-000000000001','receptor','Receptor',true,true,false);
INSERT INTO public."Role" ("Id","OrganizationId","Code","Name","IsActive","IsSeed","IsTechnical") VALUES ('01990000-0000-7000-8000-000000000015','01990000-0000-7000-8000-000000000001','automatizacion','Automatización',true,true,true);
INSERT INTO public."Role" ("Id","OrganizationId","Code","Name","IsActive","IsSeed","IsTechnical") VALUES ('01990000-0000-7000-8000-000000000016','01990000-0000-7000-8000-000000000001','configuration-admin','ConfigurationAdmin',true,true,false);
