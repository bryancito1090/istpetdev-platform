using System;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace IstpetDev.Infrastructure.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(ApplicationDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connection with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            bool canConnect = await _dbContext.Database.CanConnectAsync(cts.Token);
            if (canConnect)
            {
                return HealthCheckResult.Healthy("PostgreSQL database is available.");
            }

            return HealthCheckResult.Unhealthy("PostgreSQL database is unreachable or not configured.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Database health check failed: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy("PostgreSQL connection check failed.", ex);
        }
    }
}
