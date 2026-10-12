-- This transition is intentionally only for empty InitialSchema databases.
DO $guard$
DECLARE table_name text; nonempty boolean; expected_role text;
BEGIN
    IF current_database() ~ '^istpetdev_dev_0[1-5]$' THEN
        expected_role := 'migrator_' || substring(current_database() from 11);
    ELSIF current_database() = 'istpetdev_integration' THEN
        expected_role := 'migrator_integration';
    ELSE
        RAISE EXCEPTION 'Schema95 must run in an ISTPET development database';
    END IF;
    IF current_user <> expected_role THEN
        RAISE EXCEPTION 'Schema95 requires %, current role is %', expected_role, current_user;
    END IF;
    IF EXISTS (SELECT FROM pg_roles WHERE rolname = current_user AND (rolsuper OR rolbypassrls)) THEN
        RAISE EXCEPTION 'Use the limited migrator role, without SUPERUSER/BYPASSRLS';
    END IF;
    IF EXISTS (
        SELECT FROM pg_roles
        WHERE (rolname = 'user_' || substring(current_database() from 11)
               OR (current_database() = 'istpetdev_integration' AND rolname ~ '^user_integ_dev_0[1-5]$'))
          AND (rolsuper OR rolbypassrls OR pg_has_role(rolname, current_user, 'MEMBER'))
    ) THEN
        RAISE EXCEPTION 'Application role can bypass isolation or assume the migrator';
    END IF;
    FOREACH table_name IN ARRAY ARRAY['Products','ServicePoints','InventoryItems','Deliveries','RiskAssessments'] LOOP
        EXECUTE format('LOCK TABLE public.%I IN ACCESS EXCLUSIVE MODE', table_name);
        EXECUTE format('SELECT EXISTS (SELECT FROM public.%I)', table_name) INTO nonempty;
        IF nonempty THEN
            RAISE EXCEPTION 'Schema95 stopped: % contains records. Reconcile legacy data before migrating; nothing was deleted.', table_name;
        END IF;
    END LOOP;
END $guard$;
