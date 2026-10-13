using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;
using IstpetDev.Api.Agent;
using IstpetDev.Api.Authentication;
using IstpetDev.Application;
using IstpetDev.Application.Agent;
using IstpetDev.Application.Demo;
using IstpetDev.Application.Logistics;
using IstpetDev.Infrastructure;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Safe logging: ensure no headers/tokens are leaked in logs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Cognito valida la identidad (ADR-10); el rol se lee de Account (spec 001).
builder.Services.AddCognitoAuthentication(builder.Configuration);
builder.Services.AddScoped<IClaimsTransformation, AccountRoleClaimsTransformation>();

builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.Section));
builder.Services.Configure<LogisticsOptions>(builder.Configuration.GetSection(LogisticsOptions.SectionName));
builder.Services.Configure<PolicyOptions>(builder.Configuration.GetSection(PolicyOptions.SectionName));
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));
// Rutas relativas de fixtures se resuelven contra la raíz de contenido (src/Api en desarrollo, /app en Docker).
builder.Services.PostConfigure<DemoOptions>(options =>
    options.FixturePath = Path.GetFullPath(options.FixturePath, builder.Environment.ContentRootPath));

// CORS configuration for local Angular web (4200) and Ionic mobile (8100)
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ??
    new[] { "http://localhost:4200", "http://localhost:8100" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("IstpetDevCors", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("imports", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.AccountId()?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();

await PrepareDatabaseAsync(app, args);
if (args.Contains("--migrate")) return;

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("IstpetDevCors");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<AgentRunTraceMiddleware>();

// 1. Process Liveness: returns 200 OK as long as the process is alive, regardless of cloud/DB
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = "Live",
            timestamp = DateTime.UtcNow,
            service = "IstpetDev API"
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// 2. Database Readiness: verifies PostgreSQL connection, reports Unhealthy/503 if unreachable
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var isHealthy = report.Status == HealthStatus.Healthy;
        context.Response.StatusCode = isHealthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

app.MapControllers();

app.Run();

// Migraciones opcionales y cuentas de demo. Un fallo se registra y la API arranca; /health/ready lo reporta.
static async Task PrepareDatabaseAsync(WebApplication app, string[] args)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrateRequested = args.Contains("--migrate");
    try
    {
        if (app.Configuration.GetValue<bool>("ApplyMigrations") || migrateRequested)
        {
            app.Logger.LogInformation("Verificando y aplicando migraciones pendientes de EF Core...");
            await DatabaseInitializer.MigrateAsync(db);
            app.Logger.LogInformation("Migraciones de base de datos aplicadas exitosamente.");
        }
        var demo = scope.ServiceProvider.GetRequiredService<IOptions<DemoOptions>>().Value;
        if (demo.Enabled && demo.Accounts.Count > 0)
            await AccountSeeder.SeedAsync(db, app.Configuration["Authentication:Authority"], demo.Accounts);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al preparar la base de datos en el arranque.");
        throw;
    }
}

public partial class Program { }
