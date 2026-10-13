using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Agent;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class AgentRunPersistenceTests(PostgisContainer postgres)
{
    [Fact]
    public async Task AgentRun_persists_all_fields_and_validates_constraints()
    {
        var dbName = postgres.NewDatabase();
        await using var api = new ApiFactory(dbName, demoEnabled: true);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var startedAt = DateTime.UtcNow.AddSeconds(-5);
        var completedAt = DateTime.UtcNow;

        var run = new AgentRun
        {
            OrganizationId = DemoOrganization.Id,
            Kind = "Watch",
            Status = "Completed",
            TriggerJson = "{\"source\":\"manual\"}",
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt,
            LatencyMs = 4500,
            Model = "deepseek-chat",
            PromptVersion = "v1",
            N8nExecutionId = "exec-12345",
            FallbackReason = null,
            ToolCallsJson = "[]"
        };

        db.AgentRuns.Add(run);
        await db.SaveChangesAsync();

        var decision = new AgentDecision
        {
            OrganizationId = DemoOrganization.Id,
            Kind = "Replan",
            Status = "Proposed",
            CorrelationId = run.Id,
            DriverMessage = "Aviso al conductor en menos de 160 chars",
            ExplanationSource = "Llm",
            ExplanationMismatch = null
        };

        db.AgentDecisions.Add(decision);
        await db.SaveChangesAsync();

        // Leer de la base de datos
        var persistedRun = await db.AgentRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == run.Id);
        Assert.NotNull(persistedRun);
        Assert.Equal("Watch", persistedRun.Kind);
        Assert.Equal("Completed", persistedRun.Status);
        Assert.Equal("deepseek-chat", persistedRun.Model);
        Assert.Equal("v1", persistedRun.PromptVersion);
        Assert.Equal("exec-12345", persistedRun.N8nExecutionId);
        Assert.Equal("[]", persistedRun.ToolCallsJson);
        Assert.Equal(4500, persistedRun.LatencyMs);

        var persistedDecision = await db.AgentDecisions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == decision.Id);
        Assert.NotNull(persistedDecision);
        Assert.Equal(run.Id, persistedDecision.CorrelationId);
        Assert.Equal("Aviso al conductor en menos de 160 chars", persistedDecision.DriverMessage);
        Assert.Equal("Llm", persistedDecision.ExplanationSource);
        Assert.Null(persistedDecision.ExplanationMismatch);
    }

    [Fact]
    public async Task DriverMessage_exceeding_160_chars_is_rejected()
    {
        var dbName = postgres.NewDatabase();
        await using var api = new ApiFactory(dbName, demoEnabled: true);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var decision = new AgentDecision
        {
            OrganizationId = DemoOrganization.Id,
            Kind = "Replan",
            Status = "Proposed",
            CorrelationId = Guid.NewGuid(),
            DriverMessage = new string('A', 161),
            ExplanationSource = "Template"
        };

        db.AgentDecisions.Add(decision);
        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DemoReset_cleans_AgentRun_and_leaves_no_dangling_references()
    {
        var dbName = postgres.NewDatabase();
        await using var api = new ApiFactory(dbName, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        // Inicializar datos demo
        var resetResp1 = await planner.PostAsync("/api/v1/demo/reset", null);
        resetResp1.EnsureSuccessStatusCode();

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var run = new AgentRun
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Incident",
                Status = "Fallback",
                StartedAtUtc = DateTime.UtcNow,
                FallbackReason = "timeout",
                ToolCallsJson = "[]"
            };
            db.AgentRuns.Add(run);

            var decision = new AgentDecision
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Replan",
                Status = "Proposed",
                CorrelationId = run.Id,
                DriverMessage = "Mensaje corto"
            };
            db.AgentDecisions.Add(decision);
            await db.SaveChangesAsync();

            Assert.Equal(1, await db.AgentRuns.CountAsync());
            Assert.Equal(1, await db.AgentDecisions.CountAsync());
        }

        // Ejecutar reset de demo
        var resetResp2 = await planner.PostAsync("/api/v1/demo/reset", null);
        resetResp2.EnsureSuccessStatusCode();

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await db.AgentRuns.CountAsync());
            Assert.Equal(0, await db.AgentDecisions.CountAsync());
        }
    }
}
