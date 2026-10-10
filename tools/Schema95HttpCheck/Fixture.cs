using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Npgsql;

namespace Schema95HttpCheck;

internal static class Fixture
{
    public const string Issuer = "https://synthetic.invalid/schema95-http";
    public const string Client = "schema95-synthetic-client";
    public static readonly Guid OrganizationA = Guid.Parse("01990000-0000-7000-8000-000000000001");
    public static Guid OrganizationB { get; private set; }
    public static Guid PointA { get; private set; }
    public static Guid NullPointA { get; private set; }
    public static Guid AccountA { get; private set; }
    public static Guid AccountB { get; private set; }

    public static string Connection(string variable, string user)
    {
        var b = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable(variable));
        Check(b.Host == "localhost" && b.Port != 15432 && b.Database == "istpetdev_dev_01" && b.Username == user && b.SslMode == SslMode.VerifyFull,
            "Only the launcher's disposable loopback database is allowed");
        b.MaxPoolSize = 10;
        b.Pooling = true;
        b.NoResetOnClose = false;
        return b.ConnectionString;
    }

    public static ServiceProvider Services(string connection)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Main"] = connection }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);
        return services.BuildServiceProvider();
    }

    public static async Task Seed(ApplicationDbContext db)
    {
        await db.Database.MigrateAsync();
        var orgB = new Organization { Code = "http-b", Name = "HTTP B", TimeZone = "America/Guayaquil", IsActive = true, AuthorizationVersion = 1 };
        OrganizationB = orgB.Id;
        var subjects = new[] { "a", "a-other", "b", "inactive", "no-membership", "no-permission", "revoked", "access-inactive", "future", "expired", "role-revoked", "role-inactive", "scope-revoked" };
        var accounts = subjects.ToDictionary(s => s, s => new Account { Kind = "human", Issuer = Issuer, Subject = s, DisplayName = s, IsActive = s != "inactive" });
        AccountA = accounts["a"].Id;
        AccountB = accounts["b"].Id;
        var catalog = new Catalog { OrganizationId = OrganizationA, Code = "http-types", Name = "HTTP types", ValueType = "text", IsActive = true };
        var manage = new Permission { Code = "service-point.manage", ModuleCode = "catalog", ActionCode = "manage", ResourceKind = "service-point" };
        db.AddRange(orgB, catalog, manage);
        db.AddRange(accounts.Values);
        await db.SaveChangesAsync();
        var option = new CatalogOption { OrganizationId = OrganizationA, CatalogId = catalog.Id, Code = "clinic", Label = "Clinic", IsActive = true };
        db.Add(option);
        await db.SaveChangesAsync();
        ServicePoint Point(Guid org, string code, Point? position = null) => new() { OrganizationId = org, Code = code, Name = code, Criticality = "normal", IsActive = true, Position = position };
        var geo = Point(OrganizationA, "http-a-geo", new Point(-79.9, -2.2) { SRID = 4326 });
        geo.TypeOptionId = option.Id;
        var empty = Point(OrganizationA, "http-a-null");
        var other = Point(OrganizationA, "http-a-other");
        var b = Point(OrganizationB, "http-b-geo", new Point(-78.5, -0.2) { SRID = 4326 });
        PointA = geo.Id;
        NullPointA = empty.Id;
        var roleA = new Role { OrganizationId = OrganizationA, Code = "http-a", Name = "HTTP A", IsActive = true };
        var roleB = new Role { OrganizationId = OrganizationB, Code = "http-b", Name = "HTTP B", IsActive = true };
        var inactiveRole = new Role { OrganizationId = OrganizationA, Code = "http-inactive", Name = "HTTP inactive", IsActive = false };
        db.AddRange(geo, empty, other, b, roleA, roleB, inactiveRole);
        var accesses = new Dictionary<string, OrganizationAccess>();
        foreach (var subject in subjects.Except(new[] { "inactive", "no-membership" }))
        {
            var access = new OrganizationAccess { OrganizationId = subject == "b" ? OrganizationB : OrganizationA, AccountId = accounts[subject].Id, IsActive = subject != "access-inactive", ValidFromUtc = DateTime.UtcNow.AddDays(subject == "future" ? 1 : -2), ValidToUtc = subject == "expired" ? DateTime.UtcNow.AddDays(-1) : null, RevokedAtUtc = subject == "revoked" ? DateTime.UtcNow.AddMinutes(-1) : null, GrantedByAccountId = accounts[subject].Id, Reason = "Synthetic HTTP fixture" };
            accesses.Add(subject, access);
            db.Add(access);
        }
        await db.SaveChangesAsync();
        var read = Guid.Parse("01990000-0000-7000-8000-000000000002");
        foreach (var role in new[] { roleA, roleB, inactiveRole })
            db.Add(new RolePermission { OrganizationId = role.OrganizationId, RoleId = role.Id, PermissionId = read });
        db.Add(new RolePermission { OrganizationId = OrganizationA, RoleId = roleA.Id, PermissionId = manage.Id });
        foreach (var (subject, access) in accesses)
        {
            if (subject != "no-permission")
                db.Add(new AccessRole { OrganizationId = access.OrganizationId, OrganizationAccessId = access.Id, RoleId = subject == "b" ? roleB.Id : subject == "role-inactive" ? inactiveRole.Id : roleA.Id, RevokedAtUtc = subject == "role-revoked" ? DateTime.UtcNow.AddMinutes(-1) : null, GrantedByAccountId = access.AccountId, Reason = "Synthetic HTTP fixture" });
            foreach (var point in subject == "b" ? new[] { b } : subject == "a-other" ? new[] { other } : new[] { geo, empty })
                db.Add(new AccessServicePoint { OrganizationId = access.OrganizationId, OrganizationAccessId = access.Id, ServicePointId = point.Id, RevokedAtUtc = subject == "scope-revoked" ? DateTime.UtcNow.AddMinutes(-1) : null, GrantedByAccountId = access.AccountId, Reason = "Synthetic HTTP fixture" });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        // Fault injection in this disposable DB only; never part of an EF migration.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION public.schema95_commit_delay() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW."Name" = 'must-rollback:commit-cancel' THEN PERFORM pg_sleep(5); END IF; RETURN NEW; END $$;
            CREATE CONSTRAINT TRIGGER schema95_commit_delay AFTER UPDATE ON public."ServicePoint"
              DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.schema95_commit_delay();
            """);
    }

    public static async Task<object?> Scalar(ApplicationDbContext db, string sql, CancellationToken ct = default)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(ct);
    }

    public static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("FAIL: " + message);
    }
}
