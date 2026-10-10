using System.Collections.Concurrent;
using IstpetDev.Api.Authentication;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using static Schema95HttpCheck.Fixture;

[assembly: HostingStartup(typeof(Schema95HttpCheck.SyntheticHostingStartup))]

namespace Schema95HttpCheck;

// Loaded only by the standalone test process. Production references no test project.
public sealed class SyntheticHostingStartup : IHostingStartup
{
    internal static SecurityKey? PublicKey;
    public void Configure(IWebHostBuilder builder) => builder.ConfigureServices(services =>
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            Check(options.Authority == Issuer && PublicKey is not null, "Synthetic host must select its own issuer/key");
            Check(options.RequireHttpsMetadata && options.TokenValidationParameters.ValidateIssuer && options.TokenValidationParameters.ValidateLifetime && options.TokenValidationParameters.ValidateIssuerSigningKey,
                "Production issuer/lifetime/signature validation must remain enabled");
            var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
            metadata.SigningKeys.Add(PublicKey!);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            // Preserve production TokenValidationParameters, OnTokenValidated and challenge/forbid behavior.
        });
        services.AddControllers().AddApplicationPart(typeof(FaultController).Assembly);
        services.AddSingleton<IStartupFilter, ProbeStartupFilter>();
        services.AddHostedService<HostCapture>();
    });
}

internal sealed class HostCapture(IServer server, IHostApplicationLifetime lifetime) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        Probe.Lifetime = lifetime;
        lifetime.ApplicationStarted.Register(() => Probe.Started.TrySetResult(server.Features.Get<IServerAddressesFeature>()!.Addresses.Single()));
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

internal static class Probe
{
    internal sealed record Lease(int Pid, string? Subject, string? Failure, bool Clean, bool Broken);
    public static readonly TaskCompletionSource<string> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public static readonly ConcurrentDictionary<string, Lease> Leases = new();
    public static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> Completed = new();
    public static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> Phases = new();
    public static readonly ConcurrentDictionary<string, int> ActivePids = new();
    public static readonly ConcurrentQueue<string> Errors = new();
    public static readonly AsyncLocal<HttpContext?> Current = new();
    public static IHostApplicationLifetime? Lifetime;
    public static int Active, Peak;
    public static bool DisposalCancellationObserved, DisposalStackObserved;
    public static TaskCompletionSource<bool> Signal(ConcurrentDictionary<string, TaskCompletionSource<bool>> dictionary, string name) => dictionary.GetOrAdd(name, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
}

internal sealed class ProbeStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (http, run) =>
        {
            if (!http.Request.Headers.TryGetValue("X-Schema95-Test-Case", out var label)) { await run(); return; }
            var name = label.ToString();
            var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
            int pid = 0;
            bool clean = false, broken = false, registered = false;
            string? failure = null;
            Probe.Current.Value = http;
            try
            {
                await db.Database.OpenConnectionAsync(http.RequestAborted);
                pid = ((NpgsqlConnection)db.Database.GetDbConnection()).ProcessID;
                Check(Probe.ActivePids.TryAdd(pid.ToString(), pid), "Same physical connection leased simultaneously");
                registered = true;
                var active = Interlocked.Increment(ref Probe.Active);
                int peak;
                do { peak = Probe.Peak; if (peak >= active) break; } while (Interlocked.CompareExchange(ref Probe.Peak, active, peak) != peak);
                await AssertClean();
                Check(await Scalar(db, "SELECT current_user = 'user_dev_01' AND NOT (SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname=current_user)") is true, "Limited API login");
                if (name.StartsWith("parallel-")) await Task.Delay(100, http.RequestAborted);
                await run(); // The unchanged production authentication, authorization, MVC and filter pipeline.
            }
            catch (Exception e) { failure = e.GetType().Name; throw; }
            finally
            {
                try
                {
                    Check(db.Database.CurrentTransaction is null, "Filter transaction disposed after " + name);
                    broken = db.Database.GetDbConnection().State != System.Data.ConnectionState.Open;
                    if (!broken) { await AssertClean(); clean = true; }
                }
                catch (Exception e) { Probe.Errors.Enqueue(name + ":" + e.GetType().Name + ":" + e.Message); }
                // Release diagnostic ownership before CloseAsync makes the connector available again.
                if (registered) { Probe.ActivePids.TryRemove(pid.ToString(), out _); Interlocked.Decrement(ref Probe.Active); }
                await db.Database.CloseConnectionAsync();
                Probe.Leases[name] = new(pid, http.User.FindFirst("sub")?.Value ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, failure, clean, broken);
                Probe.Current.Value = null;
                Probe.Signal(Probe.Completed, name).TrySetResult(true);
            }

            async Task AssertClean()
            {
                Check(await Scalar(db, "SELECT NULLIF(current_setting('app.organization_id',true),'') IS NULL AND NULLIF(current_setting('app.account_id',true),'') IS NULL") is true, "Empty account/organization before pool return");
                Check(await db.ServicePoints.CountAsync() == 0, "No-context connection denies tenant rows");
            }
        });
        next(app);
    };
}

[ApiController, Route("__schema95/fault"), Authorize, ServiceFilter(typeof(OrganizationContextFilter))]
public sealed class FaultController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("{mode}")]
    public async Task<IActionResult> Get(string mode)
    {
        Check(await Scalar(db, "SELECT public.istpetdev_actor_id()") is Guid actor && actor == AccountA, "Fault action requires fixture account A");
        var name = "must-rollback:" + mode;
        Check(await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE public.\"ServicePoint\" SET \"Name\"={name} WHERE \"Id\"={PointA} OR \"Id\"={NullPointA}") == 2, "Scoped fault writes both rows");
        Probe.Signal(Probe.Phases, mode + ":action").TrySetResult(true);
        if (mode == "error") await Scalar(db, "SELECT 1/0");
        if (mode is "query-cancel" or "broken") await Scalar(db, "SELECT pg_sleep(20)", HttpContext.RequestAborted);
        if (mode == "disposal-cancel") return BadRequest(new { code = "synthetic_rollback" });
        return Ok(new { synthetic = true });
    }
}

// Synchronize a client disconnect at the real Npgsql DisposeAsync rollback boundary.
// The callback never prints SQL, parameters, connection strings or tokens.
internal sealed class RollbackLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new RollbackLogger(categoryName);
    public void Dispose() { }
    private sealed class RollbackLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
        {
            var http = Probe.Current.Value;
            // Npgsql 8.0.6 logs the pre-ROLLBACK boundary through its Connection logger.
            if (http?.Request.Path != "/__schema95/fault/disposal-cancel" || !category.Contains("Connection") || !formatter(state, error).StartsWith("Rolling back transaction", StringComparison.Ordinal)) return;
            Probe.DisposalStackObserved = new System.Diagnostics.StackTrace().GetFrames().Any(frame =>
            {
                var method = frame.GetMethod();
                var description = method?.DeclaringType?.FullName + "." + method?.Name;
                return description.Contains("NpgsqlTransaction", StringComparison.Ordinal) && description.Contains("DisposeAsync", StringComparison.Ordinal);
            });
            Probe.Signal(Probe.Phases, "disposal-cancel:disposing").TrySetResult(true);
            Probe.DisposalCancellationObserved = http.RequestAborted.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
        }
    }
}
