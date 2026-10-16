using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Imports;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IstpetDev.Tests.Integration.Persistence;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class MigrationSafetyTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Full_history_applies_to_a_new_database_and_runtime_role_can_import_without_DDL()
    {
        var connection = await postgres.NewEmptyDatabaseAsync();
        await using (var migrate = await TestDb.CreateMigratedAsync(connection))
        {
            Assert.Equal(9, (await migrate.Database.GetAppliedMigrationsAsync()).Count());
            Assert.False(migrate.Database.HasPendingModelChanges());
            Assert.Equal(0, await migrate.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public' AND c.relkind='r' AND (c.relrowsecurity OR c.relforcerowsecurity)").SingleAsync());
        }
        var runtime = new NpgsqlConnectionStringBuilder(connection) { Username = "user_dev_02" }.ConnectionString;
        await using var db = TestDb.Context(runtime);
        var request = Fixtures.Request("demo-base");
        try { Assert.True((await new ImportService(db, new TabularReader()).RunAsync(request)).Applied); }
        finally { foreach (var file in request.Files) await file.Content.DisposeAsync(); }
        Assert.Equal(6, await db.ServicePoints.CountAsync());
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT has_schema_privilege(current_user, 'public', 'CREATE') AS \"Value\"").SingleAsync());
    }

    [Fact]
    public async Task Transition_rejects_non_seed_data_without_deleting_it_or_rewriting_history()
    {
        await using var db = TestDb.Context(postgres.NewSchema95Database());
        var product = new Product { Sku = "PRESERVE", Name = "Conservar", BaseUnit = "L", IsActive = true };
        db.Add(product); await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.MigrateAsync());
        Assert.Contains("contains records", error.MessageText);
        Assert.Equal(2, (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Equal(1, await db.Products.CountAsync());
        Assert.Equal(95, await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE' AND table_name NOT IN ('__EFMigrationsHistory','spatial_ref_sys')").SingleAsync());
    }
}
