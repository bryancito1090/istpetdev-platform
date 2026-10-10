using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using IstpetDev.Api.Controllers;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Schema95HttpCheck;
using static Schema95HttpCheck.Fixture;

var originalOutput = Console.Out;
var apiOutput = new StringWriter();
Console.SetOut(TextWriter.Synchronized(apiOutput));
var tokens = new List<string>();
Task<object?>? apiTask = null;
object? report = null;
try
{
    var appConnection = Connection("SCHEMA95_APP_CONNECTION", "user_dev_01");
    var migrationConnection = Connection("SCHEMA95_MIGRATOR_CONNECTION", "migrator_dev_01");
    var adminConnection = Connection("SCHEMA95_ADMIN_CONNECTION", "postgres");
    using var npgsqlLogging = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new RollbackLoggerProvider()));
    NpgsqlLoggingConfiguration.InitializeLogging(npgsqlLogging);
    await using var services = Services(migrationConnection);
    await using var scope = services.CreateAsyncScope();
    var setup = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await Seed(setup);
    await using var admin = new NpgsqlConnection(adminConnection);
    await admin.OpenAsync();
    await using (var catalog = new NpgsqlCommand("SELECT count(*) FROM pg_class WHERE relnamespace='public'::regnamespace AND relkind='r' AND relname NOT IN ('spatial_ref_sys','__EFMigrationsHistory') AND relrowsecurity AND relforcerowsecurity", admin))
        Check(Convert.ToInt32(await catalog.ExecuteScalarAsync()) == 95, "Disposable catalog has 95 tables with ENABLE/FORCE RLS");

    using var rsa = RSA.Create(2048);
    using var wrongRsa = RSA.Create(2048);
    var key = new RsaSecurityKey(rsa) { KeyId = "schema95-synthetic" };
    SyntheticHostingStartup.PublicKey = new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = key.KeyId };
    var apiAssembly = typeof(ServicePointsController).Assembly;
    // Invoke the actual API entrypoint; no duplicate middleware or test authentication scheme.
    var apiArgs = new[] {
        "--environment", "Schema95SyntheticTest", "--applicationName", apiAssembly.GetName().Name!,
        "--contentRoot", Path.GetFullPath("src/Api"),
        "--urls", "http://127.0.0.1:0", "--hostingStartupAssemblies", typeof(SyntheticHostingStartup).Assembly.GetName().Name!,
        "--preventHostingStartup", "false", "--ConnectionStrings:Main", appConnection,
        "--Authentication:Authority", Issuer, "--Authentication:AllowedClientIds:0", Client
    };
    apiTask = Task.Run(() => apiAssembly.EntryPoint!.Invoke(null, new object[] { apiArgs }));
    var started = await Task.WhenAny(Probe.Started.Task, apiTask).WaitAsync(TimeSpan.FromSeconds(30));
    if (started == apiTask) { await apiTask; throw new InvalidOperationException("Actual API exited before startup"); }
    var address = await Probe.Started.Task;
    Check(new Uri(address).Host == "127.0.0.1", "API listener restricted to loopback");
    using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(40) };
    var results = new List<object>();
    var a = Token("a");
    var b = Token("b");
    var other = Token("a-other");
    await Request("no-bearer", null, OrganizationA.ToString(), 401);
    await Request("malformed", "not-a-jwt", OrganizationA.ToString(), 401);
    await Request("wrong-signature", Token("a", signingKey: new RsaSecurityKey(wrongRsa) { KeyId = key.KeyId }), OrganizationA.ToString(), 401);
    await Request("wrong-issuer", Token("a", issuer: "https://foreign.invalid"), OrganizationA.ToString(), 401);
    await Request("expired", Token("a", expired: true), OrganizationA.ToString(), 401);
    await Request("future-token", Token("a", future: true), OrganizationA.ToString(), 401);
    await Request("unsigned", Token("a", unsigned: true), OrganizationA.ToString(), 401);
    await Request("id-token", Token("a", use: "id"), OrganizationA.ToString(), 401);
    await Request("foreign-client", Token("a", clientId: "foreign"), OrganizationA.ToString(), 401);
    await Request("missing-client", Token("a", clientId: null), OrganizationA.ToString(), 401);
    await Request("missing-scope", Token("a", scope: null), OrganizationA.ToString(), 401);
    await Request("wrong-scope", Token("a", scope: "istpetdev-api/access-extra"), OrganizationA.ToString(), 401);
    await Request("missing-subject", Token(null), OrganizationA.ToString(), 401);
    await Request("missing-header", a, null, 400);
    await Request("invalid-header", a, "not-a-uuid", 400);
    await Request("ambiguous-header", a, OrganizationA + "," + OrganizationB, 400);
    await Request("sql-header", a, "' OR true --", 400);
    foreach (var subject in new[] { "unknown", "inactive", "no-membership", "no-permission", "revoked", "access-inactive", "future", "expired", "role-revoked", "role-inactive", "' OR true --" })
        await Request("denied-" + subject, Token(subject), OrganizationA.ToString(), 403);
    await Request("foreign-org", a, OrganizationB.ToString(), 403);
    await Request("scope-revoked", Token("scope-revoked"), OrganizationA.ToString(), 200, expected: Array.Empty<string>());
    await Request("authorized-a", a, OrganizationA.ToString(), 200, expected: new[] { "http-a-geo", "http-a-null" });
    await Request("authorized-b", b, OrganizationB.ToString(), 200, expected: new[] { "http-b-geo" });
    await Request("same-org-other-account", other, OrganizationA.ToString(), 200, expected: new[] { "http-a-other" });
    await Request("query-cannot-select-actor", a, OrganizationA.ToString(), 200, expected: new[] { "http-a-geo", "http-a-null" }, path: "/api/v1/service-points?organizationId=" + OrganizationB + "&actorId=" + AccountB);
    var work = Enumerable.Range(0, 60).Select(i =>
        i % 3 == 0 ? Request("parallel-" + i, a, OrganizationA.ToString(), 200, expected: new[] { "http-a-geo", "http-a-null" }) :
        i % 3 == 1 ? Request("parallel-" + i, b, OrganizationB.ToString(), 200, expected: new[] { "http-b-geo" }) :
        Request("parallel-" + i, other, OrganizationA.ToString(), 200, expected: new[] { "http-a-other" })).ToArray();
    await Task.WhenAll(work);
    Check(Probe.Peak == 10, "Concurrent HTTP saturated the configured ten-connection pool");
    var pids = Probe.Leases.Where(pair => pair.Key.StartsWith("parallel-")).Select(pair => pair.Value.Pid).Distinct().ToArray();
    Check(pids.Length == 10, "Ten physical PostgreSQL connections observed under concurrency");
    Check(Probe.Leases.Where(pair => pair.Key.StartsWith("parallel-")).GroupBy(pair => pair.Value.Pid).Count(group => group.Select(pair => pair.Value.Subject).Distinct().Count() > 1) > 0,
        "Different accounts reused physical connections during concurrent traffic");

    await Request("error", a, OrganizationA.ToString(), 500, path: "/__schema95/fault/error");
    await AssertWriteOutcome("error", mustRollback: true);
    await HealthyAfter("error");
    await CancelRequest("query-cancel", "query");
    await AssertWriteOutcome("query-cancel", mustRollback: true);
    await HealthyAfter("query-cancel");

    var brokenRequest = Request("broken", a, OrganizationA.ToString(), 500, path: "/__schema95/fault/broken");
    await Probe.Signal(Probe.Phases, "broken:action").Task.WaitAsync(TimeSpan.FromSeconds(10));
    var brokenPid = await WaitForDatabasePhase("query");
    await using (var kill = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)", admin))
    {
        kill.Parameters.AddWithValue("pid", brokenPid);
        Check(await kill.ExecuteScalarAsync() is true, "Terminated only the fixture's active API backend");
    }
    await brokenRequest;
    Check(Probe.Leases["broken"].Broken, "Dead physical connection was not retained open");
    await AssertWriteOutcome("broken", mustRollback: true);
    await HealthyAfter("broken");
    Check(Probe.Leases["after-broken"].Pid != brokenPid, "Healthy request did not reuse terminated backend");
    await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Request("parallel-recovery-" + i, b, OrganizationB.ToString(), 200, expected: new[] { "http-b-geo" })));
    var recoveredPids = Probe.Leases.Where(pair => pair.Key.StartsWith("parallel-recovery-")).Select(pair => pair.Value.Pid).Distinct().ToArray();
    Check(recoveredPids.Length == 10 && !recoveredPids.Contains(brokenPid) && recoveredPids.Except(pids).Any(), "Pool restored ten live physical connections with a replacement backend");

    await CancelRequest("commit-cancel", "commit");
    var committedAfterAbort = await AssertWriteOutcome("commit-cancel", mustRollback: false);
    await HealthyAfter("commit-cancel");
    await CancelRequest("disposal-cancel", "disposal");
    Check(Probe.DisposalCancellationObserved && Probe.DisposalStackObserved, "Client abort observed inside Npgsql DisposeAsync rollback");
    await AssertWriteOutcome("disposal-cancel", mustRollback: true);
    await HealthyAfter("disposal-cancel");
    Check(Probe.Errors.IsEmpty, "Pool cleanup diagnostics: " + string.Join(";", Probe.Errors));
    Check(Probe.Leases.Values.All(lease => lease.Clean || lease.Broken), "Every live connection had empty context before pool return");
    report = new {
        result = "schema95_http_checks_passed", api_started = true, api_loopback = true, actual_api_entrypoint = true,
        synthetic_signed_jwt = true, real_cognito_tested = false, rds_tested = false, ssl_mode = "VerifyFull",
        http_checks = results, concurrent_requests = 60, pool_size = 10, peak_leases = Probe.Peak,
        concurrent_backend_pids = pids, recovered_backend_pids = recoveredPids, total_http_leases = Probe.Leases.Count,
        query_cancel_rolled_back = true, broken_connection_recovered = true,
        commit_cancel_observed_in_postgres = true, commit_completed_after_client_abort = committedAfterAbort,
        commit_atomic = true, disposal_abort_observed_inside_npgsql = true,
        disposal_cancel_rolled_back = true, empty_context_before_pool_return = true
    };

    string Token(string? subject, string issuer = Issuer, string? clientId = Client, string? scope = "istpetdev-api/access", string use = "access", SecurityKey? signingKey = null, bool expired = false, bool future = false, bool unsigned = false)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim> { new("token_use", use) };
        if (subject is not null) claims.Add(new("sub", subject));
        if (clientId is not null) claims.Add(new("client_id", clientId));
        if (scope is not null) claims.Add(new("scope", scope));
        var jwt = new JwtSecurityToken(issuer, claims: claims, notBefore: expired ? now.AddMinutes(-20) : future ? now.AddMinutes(5) : now.AddMinutes(-1), expires: expired ? now.AddMinutes(-5) : now.AddMinutes(20), signingCredentials: unsigned ? null : new SigningCredentials(signingKey ?? key, SecurityAlgorithms.RsaSha256));
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        tokens.Add(token);
        return token;
    }

    async Task Request(string name, string? token, string? organization, int status, string[]? expected = null, string path = "/api/v1/service-points", CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Schema95-Test-Case", name);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (organization is not null) request.Headers.TryAddWithoutValidation("X-Organization-Id", organization);
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        Check((int)response.StatusCode == status, name + " HTTP status " + (int)response.StatusCode + " expected " + status);
        if (status == 403 || status == 401 && name != "missing-subject") Check(body == "", name + " production challenge/forbid body is empty");
        if (status == 401 && name != "missing-subject") Check(response.Headers.WwwAuthenticate.Any(header => header.Scheme == "Bearer"), "Bearer challenge header");
        if (name == "missing-subject")
        {
            Check(response.Content.Headers.ContentType?.MediaType == "application/problem+json", "Filter UnauthorizedResult problem media type");
            Check(JsonDocument.Parse(body).RootElement.GetProperty("status").GetInt32() == 401, "Filter UnauthorizedResult problem status");
        }
        if (status == 400) Check(JsonDocument.Parse(body).RootElement.GetProperty("code").GetString() == "organization_context_required", "400 JSON code");
        if (expected is not null)
        {
            Check(response.Content.Headers.ContentType?.MediaType == "application/json", name + " JSON media type");
            using var json = JsonDocument.Parse(body);
            var points = json.RootElement.EnumerateArray().OrderBy(p => p.GetProperty("code").GetString()).ToArray();
            Check(points.Select(p => p.GetProperty("code").GetString()).SequenceEqual(expected), name + " exact account/tenant scope");
            foreach (var point in points)
            {
                Check(!point.TryGetProperty("organizationId", out _) && !point.TryGetProperty("createdByAccountId", out _), "DTO omits server identity fields");
                var code = point.GetProperty("code").GetString();
                if (code == "http-a-geo") Check(point.GetProperty("latitude").GetDouble() == -2.2 && point.GetProperty("longitude").GetDouble() == -79.9 && point.GetProperty("type").GetString() == "clinic", "Coordinates and catalog type serialized");
                if (code is "http-a-null" or "http-a-other") Check(point.GetProperty("latitude").ValueKind == JsonValueKind.Null && point.GetProperty("longitude").ValueKind == JsonValueKind.Null && point.GetProperty("type").GetString() == "", "Null coordinates and empty type serialized");
                if (code == "http-b-geo") Check(point.GetProperty("latitude").GetDouble() == -0.2 && point.GetProperty("longitude").GetDouble() == -78.5, "Tenant B coordinates");
            }
        }
        await Probe.Signal(Probe.Completed, name).Task.WaitAsync(TimeSpan.FromSeconds(20));
        if (status == 500) Check(body == "", "Production unhandled error body is empty");
        lock (results) results.Add(new { name, status, body = body == "" ? "empty" : "json", rows = expected?.Length });
    }

    async Task<int> WaitForDatabasePhase(string phase)
    {
        for (var i = 0; i < 200; i++)
        {
            await using var command = new NpgsqlCommand("SELECT pid FROM pg_stat_activity WHERE usename='user_dev_01' AND datname=current_database() AND state='active' AND wait_event='PgSleep' AND (@commit AND query LIKE 'COMMIT%' OR NOT @commit AND query LIKE 'SELECT pg_sleep%')", admin);
            command.Parameters.AddWithValue("commit", phase == "commit");
            if (await command.ExecuteScalarAsync() is int pid) return pid;
            await Task.Delay(25);
        }
        throw new InvalidOperationException("Did not observe PostgreSQL phase " + phase);
    }

    async Task CancelRequest(string mode, string phase)
    {
        using var cancel = new CancellationTokenSource();
        var pending = Request(mode, a, OrganizationA.ToString(), 200, path: "/__schema95/fault/" + mode, ct: cancel.Token);
        await Probe.Signal(Probe.Phases, mode + (phase == "disposal" ? ":disposing" : ":action")).Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (phase != "disposal") await WaitForDatabasePhase(phase);
        var neighbors = Enumerable.Range(0, 12).Select(i => Request("parallel-" + mode + "-" + i, b, OrganizationB.ToString(), 200, expected: new[] { "http-b-geo" })).ToArray();
        cancel.Cancel();
        try { await pending; throw new InvalidOperationException("Expected client request cancellation"); }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
        await Task.WhenAll(neighbors);
        await Probe.Signal(Probe.Completed, mode).Task.WaitAsync(TimeSpan.FromSeconds(20));
        results.Add(new { name = mode, status = "client_cancelled", body = "no_response" });
    }

    async Task<bool> AssertWriteOutcome(string mode, bool mustRollback)
    {
        var names = await setup.ServicePoints.AsNoTracking().Where(p => p.Id == PointA || p.Id == NullPointA).Select(p => p.Name).ToArrayAsync();
        var writes = names.Count(name => name == "must-rollback:" + mode);
        Check(writes is 0 or 2, mode + " is atomic: both writes committed or neither");
        if (mustRollback) Check(writes == 0, mode + " reverted both writes");
        return writes == 2;
    }

    Task HealthyAfter(string mode) => Request("after-" + mode, b, OrganizationB.ToString(), 200, expected: new[] { "http-b-geo" });
}
finally
{
    Probe.Lifetime?.StopApplication();
    if (apiTask is not null) await apiTask.WaitAsync(TimeSpan.FromSeconds(20));
    Console.SetOut(originalOutput);
    Check(!tokens.Any(token => apiOutput.ToString().Contains(token, StringComparison.Ordinal)), "API logs must not contain JWTs");
    originalOutput.Write(apiOutput.ToString());
}
Console.WriteLine(JsonSerializer.Serialize(report));
