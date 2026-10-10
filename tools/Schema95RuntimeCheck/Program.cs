using System.Security.Claims;
using IstpetDev.Api.Authentication;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.ServicePoints.Dtos;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Npgsql;
using NpgsqlTypes;

// No WebApplication, HTTP listener, user-secrets or AWS. The launcher owns this disposable database.
var appConnection = Connection("SCHEMA95_APP_CONNECTION", "user_dev_01");
var migratorConnection = Connection("SCHEMA95_MIGRATOR_CONNECTION", "migrator_dev_01");
await using var appServices = Services(appConnection);
await using var migratorServices = Services(migratorConnection);
await using var migrationScope = migratorServices.CreateAsyncScope();
var setup = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await setup.Database.MigrateAsync();

var organizationA = Guid.Parse("01990000-0000-7000-8000-000000000001");
var readPermission = Guid.Parse("01990000-0000-7000-8000-000000000002");
const string issuer = "https://synthetic.invalid/runtime";
var organizationB = new Organization { Code = "runtime-b", Name = "Runtime B", TimeZone = "America/Guayaquil", IsActive = true, AuthorizationVersion = 1 };
Account Account(string subject, bool active = true) => new() { Kind = "human", Issuer = issuer, Subject = subject, DisplayName = subject, IsActive = active };
var accountA = Account("runtime-a");
var accountB = Account("runtime-b");
var noPermission = Account("runtime-no-permission");
var noMembership = Account("runtime-no-membership");
var inactive = Account("runtime-inactive", false);
var managePermission = new Permission { Code = "service-point.manage", ModuleCode = "catalog", ActionCode = "manage", ResourceKind = "service-point" };
var catalog = new Catalog { OrganizationId = organizationA, Code = "runtime-types", Name = "Runtime types", ValueType = "text", IsActive = true };
setup.AddRange(organizationB, accountA, accountB, noPermission, noMembership, inactive, managePermission, catalog);
await setup.SaveChangesAsync();
var option = new CatalogOption { OrganizationId = organizationA, CatalogId = catalog.Id, Code = "clinic", Label = "Clinic", IsActive = true };
setup.Add(option);
await setup.SaveChangesAsync();
ServicePoint Point(Guid organization, string code, Point? position = null) => new() { OrganizationId = organization, Code = code, Name = code, Criticality = "normal", IsActive = true, Position = position };
var origin = new Point(-79.9, -2.2) { SRID = 4326 };
var visibleA = Point(organizationA, "runtime-a-geo", origin);
visibleA.TypeOptionId = option.Id;
var nullA = Point(organizationA, "runtime-a-null");
var hiddenA = Point(organizationA, "runtime-a-hidden", new Point(-79.91, -2.21) { SRID = 4326 });
var visibleB = Point(organizationB.Id, "runtime-b-geo", new Point(-78.5, -0.2) { SRID = 4326 });
var roleA = new Role { OrganizationId = organizationA, Code = "runtime-a", Name = "Runtime A", IsActive = true };
var roleB = new Role { OrganizationId = organizationB.Id, Code = "runtime-b", Name = "Runtime B", IsActive = true };
OrganizationAccess Access(Account account, Guid organization) => new() { OrganizationId = organization, AccountId = account.Id, IsActive = true, ValidFromUtc = DateTime.UtcNow.AddDays(-1), GrantedByAccountId = account.Id, Reason = "runtime fixture" };
var accessA = Access(accountA, organizationA);
var accessB = Access(accountB, organizationB.Id);
setup.AddRange(visibleA, nullA, hiddenA, visibleB, roleA, roleB, accessA, accessB, Access(noPermission, organizationA));
await setup.SaveChangesAsync();
setup.AddRange(
    new RolePermission { OrganizationId = organizationA, RoleId = roleA.Id, PermissionId = readPermission },
    new RolePermission { OrganizationId = organizationA, RoleId = roleA.Id, PermissionId = managePermission.Id },
    new RolePermission { OrganizationId = organizationB.Id, RoleId = roleB.Id, PermissionId = readPermission },
    new AccessRole { OrganizationId = organizationA, OrganizationAccessId = accessA.Id, RoleId = roleA.Id, GrantedByAccountId = accountA.Id, Reason = "runtime fixture" },
    new AccessRole { OrganizationId = organizationB.Id, OrganizationAccessId = accessB.Id, RoleId = roleB.Id, GrantedByAccountId = accountB.Id, Reason = "runtime fixture" });
foreach (var (access, point) in new[] { (accessA, visibleA), (accessA, nullA), (accessB, visibleB) })
    setup.Add(new AccessServicePoint { OrganizationId = access.OrganizationId, OrganizationAccessId = access.Id, ServicePointId = point.Id, GrantedByAccountId = access.AccountId, Reason = "runtime fixture" });
await setup.SaveChangesAsync();
setup.ChangeTracker.Clear();

var roundTrip = await setup.ServicePoints.AsNoTracking().SingleAsync(p => p.Id == visibleA.Id);
Check(roundTrip.Position is { SRID: 4326 } && roundTrip.Position.X == -79.9 && roundTrip.Position.Y == -2.2, "EF/NTS geography round-trip");
Check((await setup.ServicePoints.AsNoTracking().SingleAsync(p => p.Id == nullA.Id)).Position is null, "EF geography null round-trip");
await RejectSpatial(new Point(0, 0) { SRID = 3857 }, "invalid geography SRID");
await RejectSpatial(new LineString(new[] { new Coordinate(0, 0), new Coordinate(1, 1) }) { SRID = 4326 }, "invalid geography subtype");

var backendPids = new List<int>();
var checks = new List<string>();
await Request(accountA, organizationA, "commit", "commit-kept");
Check((await setup.ServicePoints.AsNoTracking().SingleAsync(p => p.Id == visibleA.Id)).Name == "commit-kept", "successful action committed its write");
await Request(accountB, organizationB.Id, "read");
foreach (var outcome in new[] { "rollback", "exception", "cancel" })
{
    await Request(accountA, organizationA, outcome, "must-rollback");
    Check((await setup.ServicePoints.AsNoTracking().SingleAsync(p => p.Id == visibleA.Id)).Name == "commit-kept", outcome + " reverted the write");
    await Request(accountB, organizationB.Id, "read");
}
await Request(accountA, organizationA, "spatial");
await Request(accountB, organizationB.Id, "write-denied");
await Request(accountA, organizationB.Id, "denied", expectedStatus: 403);
await Request(noMembership, organizationA, "denied", expectedStatus: 403);
await Request(noPermission, organizationA, "denied", expectedStatus: 403);
await Request(inactive, organizationA, "denied", expectedStatus: 403);
await Request(null, organizationA, "denied", expectedStatus: 401);
await Request(accountA, null, "denied", expectedStatus: 400);
Check(backendPids.Distinct().Count() == 1, "all leases reused the same physical Npgsql connection, including cancellation");
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { result = "schema95_runtime_checks_passed", app_backend_pid = backendPids[0], app_leases = backendPids.Count, checks, geography_roundtrip = true, null_roundtrip = true, invalid_srid_rejected = true, invalid_subtype_rejected = true, spatial_translation = "ST_Distance", actual_filter_controller = true, ssl_mode = "VerifyFull", api_started = false, synthetic_claims = true, rds_tested = false }));

async Task Request(Account? account, Guid? organization, string outcome, string? write = null, int expectedStatus = 200)
{
    await using var scope = appServices.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await database.Database.OpenConnectionAsync();
    var connection = (NpgsqlConnection)database.Database.GetDbConnection();
    backendPids.Add(connection.ProcessID);
    Check(await Scalar(database, "SELECT current_user") is "user_dev_01", "application login, not SET ROLE/migrator");
    Check(await Scalar(database, "SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname=current_user") is false, "no RLS bypass");
    await Clean(database, "before " + outcome);
    var http = new DefaultHttpContext();
    if (account is not null)
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("iss", issuer), new Claim("sub", account.Subject!) }, "synthetic-test"));
    if (organization is not null) http.Request.Headers["X-Organization-Id"] = organization.ToString();
    using var cancellation = new CancellationTokenSource();
    http.RequestAborted = cancellation.Token;
    var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
    var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    var filter = new OrganizationContextFilter(database);
    IActionResult? result = null;
    var called = false;
    try
    {
        await filter.OnActionExecutionAsync(executing, async () =>
        {
            called = true;
            Check(await Scalar(database, "SELECT public.istpetdev_organization_id()") is Guid tenant && tenant == organization, "organization context selected");
            Check(await Scalar(database, "SELECT public.istpetdev_actor_id()") is Guid actor && actor == account!.Id, "account resolved by real identity helper");
            if (write is not null)
                Check(await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE public.\"ServicePoint\" SET \"Name\"={write} WHERE \"Id\"={visibleA.Id}") == 1, "authorized write inside transaction");
            if (outcome == "exception") await Scalar(database, "SELECT 1/0");
            if (outcome == "cancel")
            {
                cancellation.CancelAfter(150);
                await Scalar(database, "SELECT pg_sleep(10)", cancellation.Token);
                throw new InvalidOperationException("Cancellation unexpectedly completed");
            }
            if (outcome == "spatial")
            {
                var query = database.ServicePoints.Where(p => p.Position != null && p.Position.Distance(origin) < 100);
                Check(query.ToQueryString().Contains("ST_Distance"), "EF spatial predicate translated to PostGIS");
                Check((await query.Select(p => p.Id).ToListAsync()).SequenceEqual(new[] { visibleA.Id }), "spatial query respects scope and excludes nulls");
            }
            if (outcome == "write-denied")
                Check(await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE public.\"ServicePoint\" SET \"Name\"='forbidden' WHERE \"Id\"={visibleB.Id}") == 0, "read-only account denied UPDATE by RLS");
            result = outcome == "rollback" ? new BadRequestResult() : await new ServicePointsController(database).GetAll(CancellationToken.None);
            return new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()) { Result = result };
        });
        Check(outcome is not ("exception" or "cancel"), "expected transaction failure was observed");
    }
    catch (PostgresException error) when (outcome == "exception" && error.SqlState == "22012") { }
    catch (OperationCanceledException) when (outcome == "cancel" && cancellation.IsCancellationRequested) { }
    Check(called == (expectedStatus == 200), "denied filter never reached action");
    if (expectedStatus != 200)
    {
        // ForbidResult asks authentication to forbid; it does not expose IStatusCodeActionResult.
        var status = executing.Result is ForbidResult ? 403 : (executing.Result as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode;
        Check(status == expectedStatus, "filter result " + expectedStatus);
    }
    // MVC enumerates this projection after the action/filter returns; verify the actual DTO then.
    if (result is OkObjectResult ok)
    {
        var points = ((IEnumerable<ServicePointDto>)ok.Value!).OrderBy(p => p.Code).ToArray();
        var expected = account == accountA ? new[] { "runtime-a-geo", "runtime-a-null" } : new[] { "runtime-b-geo" };
        Check(points.Select(p => p.Code).SequenceEqual(expected), "real ServicePoints controller returned only assigned tenant rows");
        if (account == accountA)
        {
            Check(points[0].Latitude == -2.2 && points[0].Longitude == -79.9 && points[0].Type == "clinic", "NTS coordinates and catalog option in DTO");
            Check(points[1].Latitude is null && points[1].Longitude is null && points[1].Type == "", "null coordinates/type in DTO");
        }
    }
    Check(database.Database.CurrentTransaction is null, "transaction disposed");
    await Clean(database, "after " + outcome); // Before closing: proves SET LOCAL cleanup, not only pool reset.
    Check(await database.ServicePoints.CountAsync() == 0, "cleared context denied rows on still-open connection");
    await database.Database.CloseConnectionAsync();
    checks.Add(outcome + ":" + (account?.Subject ?? "anonymous") + ":filter_status=" + expectedStatus);
}

async Task RejectSpatial(Geometry geometry, string name)
{
    await using var transaction = await setup.Database.BeginTransactionAsync();
    await using var command = setup.Database.GetDbConnection().CreateCommand();
    command.Transaction = transaction.GetDbTransaction();
    command.CommandText = "INSERT INTO public.\"ServicePoint\" (\"Id\",\"OrganizationId\",\"Code\",\"Name\",\"Criticality\",\"IsActive\",\"Position\") VALUES (@id,@organization,'invalid-spatial','Invalid','normal',true,@position)";
    command.Parameters.Add(new NpgsqlParameter("id", Guid.NewGuid()));
    command.Parameters.Add(new NpgsqlParameter("organization", organizationA));
    command.Parameters.Add(new NpgsqlParameter("position", NpgsqlDbType.Geography) { Value = geometry });
    try
    {
        await command.ExecuteNonQueryAsync();
        throw new InvalidOperationException(name + " accepted");
    }
    catch (PostgresException error) when (error.SqlState == "22023") { }
    await transaction.RollbackAsync();
}

static async Task<object?> Scalar(ApplicationDbContext database, string sql, CancellationToken cancellation = default)
{
    await using var command = database.Database.GetDbConnection().CreateCommand();
    command.Transaction = database.Database.CurrentTransaction?.GetDbTransaction();
    command.CommandText = sql;
    return await command.ExecuteScalarAsync(cancellation);
}

static async Task Clean(ApplicationDbContext database, string label)
{
    Check(await Scalar(database, "SELECT NULLIF(current_setting('app.organization_id',true),'') IS NULL AND NULLIF(current_setting('app.account_id',true),'') IS NULL") is true, "no context " + label);
}

static string Connection(string key, string username)
{
    var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable(key) ?? throw new InvalidOperationException("Use scripts/db/check-schema95-runtime.py"));
    Check(builder.Host == "localhost" && builder.Port != 15432 && builder.Database == "istpetdev_dev_01" && builder.Username == username && builder.SslMode == SslMode.VerifyFull, "disposable loopback-only connection required");
    builder.MaxPoolSize = 1;
    builder.MinPoolSize = 1;
    builder.Pooling = true;
    builder.NoResetOnClose = false;
    return builder.ConnectionString;
}

static ServiceProvider Services(string connection)
{
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Main"] = connection }).Build();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddInfrastructureServices(configuration);
    return services.BuildServiceProvider();
}

static void Check(bool value, string message)
{
    if (!value) throw new InvalidOperationException("FAIL: " + message);
}
