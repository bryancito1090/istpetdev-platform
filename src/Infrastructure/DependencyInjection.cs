using System;
using Amazon;
using Amazon.CognitoIdentityProvider;
using IstpetDev.Application.Accounts;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Infrastructure.HealthChecks;
using IstpetDev.Infrastructure.Identity;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Application.Imports;
using IstpetDev.Application.Demo;
using IstpetDev.Application.Agent;
using IstpetDev.Infrastructure.Agent;
using IstpetDev.Infrastructure.Imports;
using IstpetDev.Infrastructure.Demo;
using IstpetDev.Application.Simulation;
using IstpetDev.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IstpetDev.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Main");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Placeholder connection string to allow process startup when DB is not yet provisioned/configured
            connectionString = "Host=localhost;Port=15432;Database=istpetdev_dev;Username=placeholder;Password=placeholder;";
        }

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
                options.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("Main") ?? connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseNetTopologySuite();
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.CommandTimeout(30);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<ITabularReader, TabularReader>();
        services.AddScoped<IDemoResetService, DemoResetService>();

        services.Configure<N8nOptions>(configuration.GetSection("Agent:N8n"));
        services.AddHttpClient<IN8nTrigger, N8nTrigger>();

        // Perezoso: WebApplicationFactory aplica la configuración de pruebas después de registrar servicios.
        services.AddSingleton<IIdentityDirectory>(provider =>
        {
            var pool = CognitoPool.FromAuthority(provider.GetRequiredService<IConfiguration>()["Authentication:Authority"]);
            if (pool is null) return new UnconfiguredIdentityDirectory();
            var client = new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(pool.Region));
            return new CognitoIdentityDirectory(client, pool, provider.GetRequiredService<ILogger<CognitoIdentityDirectory>>());
        });

        services.AddSingleton<IstpetDev.Application.Field.IEvidenceStorage, IstpetDev.Infrastructure.Storage.S3EvidenceStorage>();

        // Simulation services (005)
        services.AddSingleton<SimulationWorker>();
        services.AddSingleton<ISimulationWorker>(sp => sp.GetRequiredService<SimulationWorker>());
        services.AddHostedService(sp => sp.GetRequiredService<SimulationWorker>());
        services.AddScoped<ISimulationService, SimulationService>();

        // Register readiness health check
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database_ready", tags: new[] { "ready" });

        return services;
    }
}
