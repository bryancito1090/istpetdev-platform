using System;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
            // Test connection with timeout (10s to accommodate cloud tunnel latency)
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            bool canConnect = await _dbContext.Database.CanConnectAsync(cts.Token);
            if (canConnect)
            {
                var pending = await _dbContext.Database.GetPendingMigrationsAsync(cts.Token);
                if (pending.Any())
                    return HealthCheckResult.Unhealthy("Database migrations are pending.");
                var actual = await _dbContext.Database.SqlQueryRaw<string>(
                    "SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE' AND table_name NOT IN ('__EFMigrationsHistory','spatial_ref_sys')").ToListAsync(cts.Token);
                var expected = _dbContext.Model.GetEntityTypes().Select(e => e.GetTableName()).Distinct().Order().ToArray();
                if (!actual.Order().SequenceEqual(expected)) return HealthCheckResult.Unhealthy("Database schema is incompatible with DemoCore.");
                return HealthCheckResult.Healthy("PostgreSQL database is available.");
            }

            return HealthCheckResult.Unhealthy("PostgreSQL database is unreachable or not configured.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Database health check failed: {ErrorType}", ex.GetType().Name);
            return HealthCheckResult.Unhealthy("PostgreSQL connection check failed.", ex);
        }
    }
}
