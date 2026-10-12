using System;
using System.Text.Json;
using IstpetDev.Api.Authentication;
using IstpetDev.Application;
using IstpetDev.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Safe logging: ensure no headers/tokens are leaked in logs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add Application layer services (Use Cases, Queries, Services)
builder.Services.AddApplicationServices();

// Add Infrastructure services (DbContext, Npgsql, PostGIS, HealthChecks)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Cognito OIDC JWT Authentication per ADR-10
builder.Services.AddCognitoAuthentication(builder.Configuration);

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

builder.Services.AddControllers();
builder.Services.AddScoped<IstpetDev.Api.Authentication.OrganizationContextFilter>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar migraciones pendientes de EF Core si está configurado (ej. ApplyMigrations=true en Compose)
if (app.Configuration.GetValue<bool>("ApplyMigrations", false) || args.Contains("--migrate"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IstpetDev.Infrastructure.Persistence.ApplicationDbContext>();
        app.Logger.LogInformation("Verificando y aplicando migraciones pendientes de EF Core...");
        db.Database.Migrate();
        app.Logger.LogInformation("Migraciones de base de datos aplicadas exitosamente.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al aplicar migraciones de base de datos en arranque.");
        if (args.Contains("--migrate")) throw;
    }
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("IstpetDevCors");

app.UseAuthentication();
app.UseAuthorization();

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
