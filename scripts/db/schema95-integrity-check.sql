-- Migrator-only, personal/disposable database. Synthetic data is rolled back.
\set ON_ERROR_STOP on
BEGIN;
DO $$ BEGIN
    IF current_database() = 'istpetdev_integration' OR current_user NOT LIKE 'migrator_dev_0_' THEN
        RAISE EXCEPTION 'Use a personal/disposable database with its migrator';
    END IF;
END $$;
INSERT INTO public."Account" ("Id","Kind","Issuer","Subject","DisplayName","IsActive")
VALUES ('01990000-0000-7000-8000-000000000301','human','https://synthetic.invalid','integrity-fixture','Synthetic integrity',true);
INSERT INTO public."Template" ("Id","OrganizationId","Code","Kind","ModuleCode","Name","IsActive")
VALUES ('01990000-0000-7000-8000-000000000302','01990000-0000-7000-8000-000000000001','integrity-fixture','form','forms','Synthetic form',true);
INSERT INTO public."TemplateVersion" ("Id","OrganizationId","TemplateId","Revision","Status","ValidationSchemaJson","LayoutJson","ContentJson","ContentHash","CreatedByAccountId")
VALUES ('01990000-0000-7000-8000-000000000303','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000302',1,'draft','{}','{}','{}',repeat('a',64),'01990000-0000-7000-8000-000000000301');
INSERT INTO public."TemplateField" ("Id","OrganizationId","TemplateVersionId","Key","Label","DataType","IsRequired","OrderNumber","Sensitivity")
VALUES ('01990000-0000-7000-8000-000000000304','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000303','fixture','Fixture','text',false,1,'internal');
INSERT INTO public."TemplateFieldOption" ("Id","OrganizationId","TemplateFieldId","Code","Label","OrderNumber","IsActive")
VALUES ('01990000-0000-7000-8000-000000000305','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000304','one','One',1,true);
UPDATE public."TemplateVersion" SET "Status"='published',"PublishedAtUtc"=CURRENT_TIMESTAMP
WHERE "Id"='01990000-0000-7000-8000-000000000303';
DO $$ BEGIN
    BEGIN
        UPDATE public."TemplateVersion" SET "ContentJson"='{"changed":true}' WHERE "Id"='01990000-0000-7000-8000-000000000303';
        RAISE EXCEPTION 'Published template content was mutable';
    EXCEPTION WHEN check_violation THEN NULL; END;
    BEGIN
        UPDATE public."TemplateField" SET "Label"='changed' WHERE "Id"='01990000-0000-7000-8000-000000000304';
        RAISE EXCEPTION 'Published field was mutable';
    EXCEPTION WHEN check_violation THEN NULL; END;
    BEGIN
        UPDATE public."TemplateFieldOption" SET "Label"='changed' WHERE "Id"='01990000-0000-7000-8000-000000000305';
        RAISE EXCEPTION 'Published option was mutable';
    EXCEPTION WHEN check_violation THEN NULL; END;
END $$;
INSERT INTO public."TemplateBinding" ("Id","OrganizationId","TemplateVersionId","ModuleCode","ActionCode","ScopeKind","EffectiveFromUtc")
VALUES ('01990000-0000-7000-8000-000000000306','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000303','forms','submit','organization',CURRENT_TIMESTAMP);
DO $$ BEGIN
    BEGIN
        INSERT INTO public."TemplateBinding" ("Id","OrganizationId","TemplateVersionId","ModuleCode","ActionCode","ScopeKind","EffectiveFromUtc")
        VALUES ('01990000-0000-7000-8000-000000000307','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000303','forms','submit','organization',CURRENT_TIMESTAMP);
        RAISE EXCEPTION 'Overlapping binding was allowed';
    EXCEPTION WHEN check_violation THEN NULL; END;
END $$;
UPDATE public."TemplateVersion" SET "Status"='retired' WHERE "Id"='01990000-0000-7000-8000-000000000303';
INSERT INTO public."ConfigurationDefinition" ("Id","Code","ModuleCode","ValidationSchemaJson","Sensitivity","AllowedScopes")
VALUES ('01990000-0000-7000-8000-000000000308','integrity-fixture','inventory','{}','internal','organization');
INSERT INTO public."ConfigurationVersion" ("Id","OrganizationId","ConfigurationDefinitionId","Revision","ValuesJson","ContentHash","PublishedAtUtc","CreatedByAccountId")
VALUES ('01990000-0000-7000-8000-000000000309','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000308',1,'{}',repeat('a',64),CURRENT_TIMESTAMP,'01990000-0000-7000-8000-000000000301');
INSERT INTO public."ConfigurationActivation" ("Id","OrganizationId","ConfigurationVersionId","ScopeKind","EffectiveFromUtc","ChangedByAccountId")
VALUES ('01990000-0000-7000-8000-000000000310','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000309','organization',CURRENT_TIMESTAMP,'01990000-0000-7000-8000-000000000301');
DO $$ BEGIN
    BEGIN
        UPDATE public."ConfigurationVersion" SET "ValuesJson"='{"changed":true}' WHERE "Id"='01990000-0000-7000-8000-000000000309';
        RAISE EXCEPTION 'Published configuration was mutable';
    EXCEPTION WHEN check_violation THEN NULL; END;
    BEGIN
        INSERT INTO public."ConfigurationActivation" ("Id","OrganizationId","ConfigurationVersionId","ScopeKind","EffectiveFromUtc","ChangedByAccountId")
        VALUES ('01990000-0000-7000-8000-000000000311','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000309','organization',CURRENT_TIMESTAMP,'01990000-0000-7000-8000-000000000301');
        RAISE EXCEPTION 'Overlapping configuration was allowed';
    EXCEPTION WHEN check_violation THEN NULL; END;
    BEGIN
        UPDATE public."Account" SET "Subject"='changed' WHERE "Id"='01990000-0000-7000-8000-000000000301';
        RAISE EXCEPTION 'External account identity was mutable';
    EXCEPTION WHEN check_violation THEN NULL; END;
    BEGIN
        UPDATE public."Account" SET "CreatedAtUtc"=CURRENT_TIMESTAMP + interval '1 day' WHERE "Id"='01990000-0000-7000-8000-000000000301';
        RAISE EXCEPTION 'Creation time was mutable';
    EXCEPTION WHEN check_violation THEN NULL; END;
END $$;
UPDATE public."Account" SET "DisplayName"='Updated' WHERE "Id"='01990000-0000-7000-8000-000000000301';
DO $$ BEGIN
    IF (SELECT "Version" FROM public."Account" WHERE "Id"='01990000-0000-7000-8000-000000000301') <> 2 THEN RAISE EXCEPTION 'Version did not advance'; END IF;
END $$;
INSERT INTO public."AuditEvent" ("Id","OrganizationId","ActorAccountId","ActionCode","ResourceType","ResultCode","CorrelationId","RecordedAtUtc")
VALUES ('01990000-0000-7000-8000-000000000312','01990000-0000-7000-8000-000000000001','01990000-0000-7000-8000-000000000301','test','test','ok','fixture',CURRENT_TIMESTAMP);
DO $$ BEGIN
    BEGIN
        DELETE FROM public."AuditEvent" WHERE "Id"='01990000-0000-7000-8000-000000000312';
        RAISE EXCEPTION 'Audit history was deletable';
    EXCEPTION WHEN check_violation THEN NULL; END;
END $$;
ROLLBACK;
SELECT 'schema95_integrity_checks_passed' AS result;
