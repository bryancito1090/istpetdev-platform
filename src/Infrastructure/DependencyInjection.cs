using System;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Infrastructure.HealthChecks;
using IstpetDev.Infrastructure.Persistence;
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
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseNetTopologySuite();
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.CommandTimeout(30);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Register readiness health check
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database_ready", tags: new[] { "ready" });

        return services;
    }
}
