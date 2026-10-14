using System.Diagnostics;
using System.Text.Json;
using IstpetDev.Application.Agent;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Agent;

public sealed class AgentRunTraceMiddleware(RequestDelegate next, ILogger<AgentRunTraceMiddleware> logger)
{
    private const string HeaderName = "X-Agent-Run-Id";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var runIdStr) ||
            !Guid.TryParse(runIdStr, out var runId))
        {
            await next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            // Only trace if the caller has the Automatizacion role
            if (context.User.IsInRole(Roles.Automatizacion))
            {
                var method = context.Request.Method;
                var path = context.Request.Path.Value ?? "";
                var statusCode = context.Response.StatusCode;
                var elapsedMs = (int)sw.ElapsedMilliseconds;

                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                    var trace = JsonSerializer.Serialize(new[] { new ToolCallTrace(method, path, statusCode, elapsedMs, DateTime.UtcNow) }, JsonOpts);
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE \"AgentRun\" SET \"ToolCallsJson\" = (\"ToolCallsJson\"::jsonb || {trace}::jsonb) WHERE \"Id\" = {runId} AND \"OrganizationId\" = {DemoOrganization.Id}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to record tool call trace for run {RunId}", runId);
                }
            }
        }
    }
}
