namespace IstpetDev.Infrastructure.Persistence;

// Permisos para los roles de aplicación de RDS (user_dev_0N en bases personales, user_integ_dev_0N en integración).
// No hace nada en bases sin esos roles (pruebas, Docker local). Toda migración que cree tablas termina con este SQL.
public static class DemoGrants
{
    public const string Sql = """
        DO $grants$
        DECLARE role_name text; table_name text;
        BEGIN
            FOR role_name IN
                SELECT rolname FROM pg_roles
                WHERE (current_database() = 'istpetdev_integration' AND rolname ~ '^user_integ_dev_0[1-5]$')
                   OR (current_database() ~ '^istpetdev_dev_0[1-5]$' AND rolname = 'user_' || substring(current_database() from 11))
            LOOP
                EXECUTE format('GRANT USAGE ON SCHEMA public TO %I', role_name);
                FOREACH table_name IN ARRAY ARRAY['Organization', 'Account', 'ServicePoint', 'Location', 'Vehicle', 'Product', 'Lot', 'HandlingUnit', 'HandlingUnitLine', 'InventoryMovement', 'StockBalance', 'Consumption', 'RoutePlan', 'RouteStop', 'Delivery', 'DeliveryLine', 'Receipt', 'ReceiptLine', 'Evidence', 'CustodyEvent', 'RiskSnapshot', 'Incident', 'IdempotencyRecord', 'ImportBatch', 'AgentDecision', 'SimulationRun', 'SimulationMetric'] LOOP
                    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.%I TO %I', table_name, role_name);
                END LOOP;
                EXECUTE format('REVOKE INSERT, UPDATE, DELETE ON TABLE public."Organization" FROM %I', role_name);
                EXECUTE format('GRANT UPDATE ("Version") ON TABLE public."Organization" TO %I', role_name);
                EXECUTE format('REVOKE UPDATE, DELETE ON TABLE public."Account", public."ImportBatch" FROM %I', role_name);
                EXECUTE format('REVOKE UPDATE ON TABLE public."InventoryMovement" FROM %I', role_name);
                EXECUTE format('GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO %I', role_name);
                EXECUTE format('REVOKE INSERT, UPDATE, DELETE ON TABLE public."__EFMigrationsHistory" FROM %I', role_name);
            END LOOP;
        END $grants$;
        """;
}
