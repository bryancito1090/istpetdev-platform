using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Agent;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class AgentRunTraceTests(PostgisContainer postgres)
{
    private const string TechnicalClientId = "n8n-automation-client";

    [Fact]
    public async Task Automation_request_with_header_appends_trace_to_agent_run()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, overrides: new Dictionary<string, string?>
        {
            ["Authentication:AllowedClientIds:1"] = TechnicalClientId,
            ["Authentication:TechnicalClientIds:0"] = TechnicalClientId,
            ["Authentication:TechnicalScope"] = "istpetdev-api/automation"
        });

        using var initClient = api.ClientFor(TestTokens.Planner);

        // Seed technical account
        await using (var db = TestDb.Context(connection))
        {
            db.Add(new Account
            {
                Kind = "technical",
                Issuer = TestTokens.Issuer,
                Subject = TechnicalClientId,
                Role = Roles.Automatizacion,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        Guid runId;
        await using (var db = TestDb.Context(connection))
        {
            var run = new AgentRun
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Watch",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                ToolCallsJson = "[]"
            };
            db.AgentRuns.Add(run);
            await db.SaveChangesAsync();
            runId = run.Id;
        }

        using var client = api.CreateClient();
        var autoToken = TestTokens.Create(TechnicalClientId, clientId: TechnicalClientId, scope: "istpetdev-api/automation", role: Roles.Automatizacion);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", autoToken);
        client.DefaultRequestHeaders.Add("X-Agent-Run-Id", runId.ToString());

        var resp = await client.GetAsync("/api/v1/risks");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // Verify trace was appended in DB
        await using (var db = TestDb.Context(connection))
        {
            var run = await db.AgentRuns.SingleAsync(r => r.Id == runId);
            using var doc = JsonDocument.Parse(run.ToolCallsJson);
            var array = doc.RootElement.EnumerateArray().ToList();
            Assert.Single(array);
            var trace = array[0];
            Assert.Equal("GET", trace.GetProperty("method").GetString());
            Assert.Equal("/api/v1/risks", trace.GetProperty("path").GetString());
            Assert.Equal(200, trace.GetProperty("status").GetInt32());
        }
    }

    [Fact]
    public async Task Human_request_with_header_does_not_append_trace()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);

        Guid runId;
        await using (var db = TestDb.Context(connection))
        {
            var run = new AgentRun
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Watch",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                ToolCallsJson = "[]"
            };
            db.AgentRuns.Add(run);
            await db.SaveChangesAsync();
            runId = run.Id;
        }

        client.DefaultRequestHeaders.Add("X-Agent-Run-Id", runId.ToString());
        var resp = await client.GetAsync("/api/v1/risks");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // Verify trace was NOT added
        await using (var db = TestDb.Context(connection))
        {
            var run = await db.AgentRuns.SingleAsync(r => r.Id == runId);
            Assert.Equal("[]", run.ToolCallsJson);
        }
    }
}
