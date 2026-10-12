-- Run after migrating, as the migrator, in a disposable PERSONAL database.
-- All synthetic accounts/organizations/permissions are rolled back.
-- Not a production bootstrap and not a Cognito login.
\set ON_ERROR_STOP on
BEGIN;
DO $$
BEGIN
    IF current_database() = 'istpetdev_integration' THEN
        RAISE EXCEPTION 'Run the synthetic test in a personal/disposable database';
    END IF;
    IF (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND c.relkind='r' AND c.relrowsecurity AND c.relforcerowsecurity) <> 95 THEN
        RAISE EXCEPTION 'Expected 95 tables with ENABLE/FORCE RLS';
    END IF;
    IF EXISTS (SELECT FROM pg_roles WHERE rolname = 'user_' || substring(current_database() from 11) AND (rolsuper OR rolbypassrls)) THEN
        RAISE EXCEPTION 'Application role must not bypass RLS';
    END IF;
END $$;

INSERT INTO public."Account" ("Id","Kind","Issuer","Subject","DisplayName","IsActive") VALUES
 ('01990000-0000-7000-8000-000000000101','human','https://synthetic.invalid','test-a','Synthetic A',true),
 ('01990000-0000-7000-8000-000000000102','human','https://synthetic.invalid','test-b','Synthetic B',true);
INSERT INTO public."Organization" ("Id","Code","Name","TimeZone","IsActive","AuthorizationVersion") VALUES
 ('01990000-0000-7000-8000-000000000103','test-b','Synthetic B','America/Guayaquil',true,1);
INSERT INTO public."OrganizationAccess" ("Id","OrganizationId","AccountId","IsActive","ValidFromUtc","GrantedByAccountId","Reason") VALUES
 ('01990000-0000-7000-8000-000000000104','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000101',true,CURRENT_TIMESTAMP - interval '1 day','01990000-0000-7000-8000-000000000101','synthetic fixture'),
 ('01990000-0000-7000-8000-000000000105','01990000-0000-7000-8000-000000000103','01990000-0000-7000-8000-000000000102',true,CURRENT_TIMESTAMP - interval '1 day','01990000-0000-7000-8000-000000000102','synthetic fixture');
INSERT INTO public."Role" ("Id","OrganizationId","Code","Name","IsActive","IsSeed","IsTechnical") VALUES
 ('01990000-0000-7000-8000-000000000106','01990000-0000-7000-8000-000000000001','test-reader','Synthetic reader A',true,false,false),
 ('01990000-0000-7000-8000-000000000107','01990000-0000-7000-8000-000000000103','test-reader','Synthetic reader B',true,false,false);
INSERT INTO public."RolePermission" ("Id","OrganizationId","RoleId","PermissionId") VALUES
 ('01990000-0000-7000-8000-000000000108','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000106','01990000-0000-7000-8000-000000000002'),
 ('01990000-0000-7000-8000-000000000109','01990000-0000-7000-8000-000000000103','01990000-0000-7000-8000-000000000107','01990000-0000-7000-8000-000000000002');
INSERT INTO public."AccessRole" ("Id","OrganizationId","OrganizationAccessId","RoleId","GrantedByAccountId","Reason") VALUES
 ('01990000-0000-7000-8000-000000000110','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000104','01990000-0000-7000-8000-000000000106','01990000-0000-7000-8000-000000000101','synthetic fixture'),
 ('01990000-0000-7000-8000-000000000111','01990000-0000-7000-8000-000000000103','01990000-0000-7000-8000-000000000105','01990000-0000-7000-8000-000000000107','01990000-0000-7000-8000-000000000102','synthetic fixture');
INSERT INTO public."ServicePoint" ("Id","OrganizationId","Code","Name","Criticality","IsActive") VALUES
 ('01990000-0000-7000-8000-000000000112','01990000-0000-7000-8000-000000000001','test-a','A visible','normal',true),
 ('01990000-0000-7000-8000-000000000113','01990000-0000-7000-8000-000000000103','test-b','B visible','normal',true),
 ('01990000-0000-7000-8000-000000000114','01990000-0000-7000-8000-000000000001','test-hidden','A unassigned','normal',true);
INSERT INTO public."AccessServicePoint" ("Id","OrganizationId","OrganizationAccessId","ServicePointId","GrantedByAccountId","Reason") VALUES
 ('01990000-0000-7000-8000-000000000115','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000104','01990000-0000-7000-8000-000000000112','01990000-0000-7000-8000-000000000101','synthetic fixture'),
 ('01990000-0000-7000-8000-000000000116','01990000-0000-7000-8000-000000000103','01990000-0000-7000-8000-000000000105','01990000-0000-7000-8000-000000000113','01990000-0000-7000-8000-000000000102','synthetic fixture');

-- Tenant FKs reject a relation to a parent belonging to another organization,
-- even when using the trusted migrator (whose RLS policy permits maintenance).
DO $$
BEGIN
    BEGIN
        INSERT INTO public."AccessServicePoint" ("Id","OrganizationId","OrganizationAccessId","ServicePointId","GrantedByAccountId","Reason") VALUES
        ('01990000-0000-7000-8000-000000000117','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000104','01990000-0000-7000-8000-000000000113','01990000-0000-7000-8000-000000000101','must fail');
        RAISE EXCEPTION 'Cross-tenant FK unexpectedly accepted';
    EXCEPTION WHEN foreign_key_violation THEN NULL;
    END;
END $$;

SELECT 'user_' || substring(current_database() from 11) AS app_role \gset
SET LOCAL ROLE :"app_role";
SELECT set_config('app.organization_id','',true), set_config('app.account_id','',true);
DO $$
DECLARE table_name text; visible bigint;
BEGIN
    FOR table_name IN SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND c.relkind='r' AND c.relrowsecurity LOOP
        EXECUTE format('SELECT count(*) FROM public.%I',table_name) INTO visible;
        IF visible <> 0 THEN RAISE EXCEPTION 'Empty context leaked records in %',table_name; END IF;
    END LOOP;
END $$;

SELECT set_config('app.organization_id','01990000-0000-7000-8000-000000000001',true),
       set_config('app.account_id','01990000-0000-7000-8000-000000000101',true);
DO $$
DECLARE visible bigint; table_name text;
BEGIN
    IF (SELECT count(*) FROM public."ServicePoint") <> 1 THEN RAISE EXCEPTION 'Point scope A failed'; END IF;
    IF EXISTS (SELECT FROM public."ServicePoint" WHERE "OrganizationId" <> public.istpetdev_organization_id()) THEN RAISE EXCEPTION 'Organization B leaked'; END IF;
    -- Compile and execute every policy, including empty child tables: catches recursion.
    FOR table_name IN SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND c.relkind='r' AND c.relrowsecurity LOOP
        EXECUTE format('SELECT count(*) FROM public.%I',table_name) INTO visible;
    END LOOP;
    BEGIN
        INSERT INTO public."RolePermission" ("Id","OrganizationId","RoleId","PermissionId") VALUES
        ('01990000-0000-7000-8000-000000000118','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000106','01990000-0000-7000-8000-000000000002');
        RAISE EXCEPTION 'Application could manipulate RBAC';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
    BEGIN
        INSERT INTO public."ServicePoint" ("Id","OrganizationId","Code","Name","Criticality","IsActive") VALUES
        ('01990000-0000-7000-8000-000000000119','01990000-0000-7000-8000-000000000103','forbidden','forbidden','normal',true);
        RAISE EXCEPTION 'Cross-tenant insert unexpectedly accepted';
    EXCEPTION WHEN insufficient_privilege THEN NULL;
    END;
    IF has_table_privilege(current_user,'public."AuditEvent"','UPDATE') OR has_table_privilege(current_user,'public."InventoryMovement"','DELETE')
       OR has_table_privilege(current_user,'public."__EFMigrationsHistory"','UPDATE') THEN
        RAISE EXCEPTION 'Application has forbidden history mutation privileges';
    END IF;
END $$;
SELECT set_config('app.organization_id','01990000-0000-7000-8000-000000000103',true),
       set_config('app.account_id','01990000-0000-7000-8000-000000000102',true);
DO $$ BEGIN
    IF (SELECT count(*) FROM public."ServicePoint") <> 1 OR EXISTS (SELECT FROM public."ServicePoint" WHERE "Code" <> 'test-b') THEN RAISE EXCEPTION 'Same-session organization B failed'; END IF;
END $$;
SELECT set_config('app.organization_id','01990000-0000-7000-8000-000000000001',true);
DO $$ BEGIN
    IF EXISTS (SELECT FROM public."ServicePoint") THEN RAISE EXCEPTION 'Non-member could read organization A'; END IF;
END $$;
SELECT set_config('app.organization_id','',true), set_config('app.account_id','',true);
DO $$ BEGIN
    IF EXISTS (SELECT FROM public."ServicePoint") THEN RAISE EXCEPTION 'Cleared context leaked records'; END IF;
END $$;
ROLLBACK;
-- No SET LOCAL identity/tenant/role survives the transaction or is left in a pool.
DO $$ BEGIN
    IF NULLIF(current_setting('app.organization_id',true),'') IS NOT NULL OR NULLIF(current_setting('app.account_id',true),'') IS NOT NULL THEN RAISE EXCEPTION 'Transaction-local context leaked'; END IF;
END $$;
SELECT 'schema95_security_checks_passed' AS result;
