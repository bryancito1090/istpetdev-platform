using IstpetDev.Application.Agent;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace IstpetDev.Tests.Integration.Agent;

[Trait("Category", "Integration")]
public sealed class AgentRunServiceTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Callback_locks_transition_and_commits_decisions_with_completed_status()
    {
        var connection = postgres.NewDatabase();
        await using var db = await TestDb.CreateMigratedAsync(connection);
        var run = new AgentRun { OrganizationId = DemoOrganization.Id, Kind = "Watch", Status = "Running", StartedAtUtc = DateTime.UtcNow };
        var decision = new AgentDecision { OrganizationId = DemoOrganization.Id, CorrelationId = run.Id, Rationale = "Plantilla", ProposalJson = "{\"facts\":[]}" };
        db.AgentRuns.Add(run);
        db.AgentDecisions.Add(decision);
        await db.SaveChangesAsync();
        using var reached = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        var validator = new PausedValidator(reached, resume);
        var service = new AgentRunService(db, null!, null!, null!, null!, null!, null!, validator,
            Options.Create(new AgentOptions()), NullLogger<AgentRunService>.Instance);
        var result = new AgentResult([new DecisionText(decision.Id, "Explicación validada", "Mensaje validado")], null, "deepseek-chat", "v1", "test", null);
        var callback = Task.Run(() => service.SubmitResultAsync(run.Id, result, default));
        try
        {
            Assert.True(reached.Wait(TimeSpan.FromSeconds(10)));
            await using var observer = TestDb.Context(connection);
            await observer.Database.OpenConnectionAsync();
            Assert.Equal("Running", (await observer.AgentRuns.AsNoTracking().SingleAsync()).Status);
            Assert.Equal("Plantilla", (await observer.AgentDecisions.AsNoTracking().SingleAsync()).Rationale);

            // A competing timeout must wait for the callback transaction, not win
            // while the callback is preparing its decision text.
            await observer.Database.ExecuteSqlRawAsync("SET statement_timeout = '200ms'");
            var error = await Assert.ThrowsAsync<PostgresException>(() => observer.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"AgentRun\" SET \"Status\" = 'Fallback' WHERE \"Id\" = {run.Id} AND \"Status\" = 'Running'"));
            Assert.Equal(PostgresErrorCodes.QueryCanceled, error.SqlState);
        }
        finally
        {
            resume.Set();
        }
        Assert.Equal(ResultOutcome.Accepted, await callback);
        await using var final = TestDb.Context(connection);
        Assert.Equal("Completed", (await final.AgentRuns.SingleAsync()).Status);
        Assert.Equal("Explicación validada", (await final.AgentDecisions.SingleAsync()).Rationale);
        Assert.Equal(ResultOutcome.Late, await service.SubmitResultAsync(run.Id,
            result with { Decisions = [new DecisionText(decision.Id, "Texto tardío", "Mensaje tardío")] }, default));
        Assert.Equal("Explicación validada", (await final.AgentDecisions.AsNoTracking().SingleAsync()).Rationale);
    }

    private sealed class PausedValidator(ManualResetEventSlim reached, ManualResetEventSlim resume) : IExplanationValidator
    {
        public ValidationResult Validate(string text, IReadOnlyList<Fact> facts)
        {
            reached.Set();
            if (!resume.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Callback test did not resume");
            return new ValidationResult(true, null, []);
        }
    }
}
