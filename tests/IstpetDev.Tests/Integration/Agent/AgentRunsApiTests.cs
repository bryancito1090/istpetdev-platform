using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Application.Agent;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Agent;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class AgentRunsApiTests(PostgisContainer postgres)
{
    private const string TechnicalClientId = "n8n-automation-client";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Start_run_watch_with_agent_disabled_falls_back_immediately_with_200()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, overrides: new Dictionary<string, string?>
        {
            ["Agent:Enabled"] = "false"
        });

        using var client = api.ClientFor(TestTokens.Planner);

        var request = new StartRunRequest(AgentRunKind.Watch, null);
        var response = await client.PostAsJsonAsync("/api/v1/agent/runs", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var view = await response.Content.ReadFromJsonAsync<AgentRunView>(JsonOpts);
        Assert.NotNull(view);
        Assert.Equal(AgentRunKind.Watch, view.Kind);
        Assert.Equal(AgentRunStatus.Fallback, view.Status);
        Assert.Equal("agent_disabled", view.FallbackReason);
        Assert.NotEmpty(view.Decisions);

        // Verify GET /api/v1/agent/runs/{id}
        var getResp = await client.GetAsync($"/api/v1/agent/runs/{view.RunId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var getView = await getResp.Content.ReadFromJsonAsync<AgentRunView>(JsonOpts);
        Assert.NotNull(getView);
        Assert.Equal(view.RunId, getView.RunId);

        // Verify GET /api/v1/agent/runs
        var listResp = await client.GetAsync("/api/v1/agent/runs?limit=10");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
    }

    [Fact]
    public async Task Submit_result_requires_automation_role_and_updates_decisions()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, overrides: new Dictionary<string, string?>
        {
            ["Authentication:AllowedClientIds:1"] = TechnicalClientId,
            ["Authentication:TechnicalClientIds:0"] = TechnicalClientId,
            ["Authentication:TechnicalScope"] = "istpetdev-api/automation"
        });

        using var plannerClient = api.ClientFor(TestTokens.Planner);

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
        Guid decisionId;

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

            var decision = new AgentDecision
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "RISK_WATCH",
                Status = "Evaluated",
                CorrelationId = run.Id,
                ProposalJson = JsonSerializer.Serialize(new
                {
                    facts = new[]
                    {
                        new { code = "stock_coverage_days", subject = "HOS-N01:DES-01", value = 5.0m, unit = "dias" }
                    }
                }),
                Rationale = "Plantilla inicial",
                ExplanationSource = "Template",
                CreatedAtUtc = DateTime.UtcNow
            };
            db.AgentDecisions.Add(decision);
            await db.SaveChangesAsync();

            runId = run.Id;
            decisionId = decision.Id;
        }

        var resultPayload = new AgentResult(
            Decisions: [new DecisionText(decisionId, "Cobertura en HOS-N01 es 5,0 dias para DES-01", "Alerta de stock: 5,0 dias")],
            Summary: "Monitoreo completado",
            Model: "gpt-4o",
            PromptVersion: "v1.2",
            N8nExecutionId: "exec-999",
            AgentError: null);

        // 1. Planner calling submit result -> 403 Forbidden
        var plannerResp = await plannerClient.PostAsJsonAsync($"/api/v1/agent/runs/{runId}/result", resultPayload);
        Assert.Equal(HttpStatusCode.Forbidden, plannerResp.StatusCode);

        // 2. Automation calling submit result -> 202 Accepted
        using var autoClient = api.CreateClient();
        var autoToken = TestTokens.Create(TechnicalClientId, clientId: TechnicalClientId, scope: "istpetdev-api/automation", role: Roles.Automatizacion);
        autoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", autoToken);

        var autoResp = await autoClient.PostAsJsonAsync($"/api/v1/agent/runs/{runId}/result", resultPayload);
        Assert.Equal(HttpStatusCode.Accepted, autoResp.StatusCode);

        // Verify DB was updated with validated LLM explanation
        await using (var db = TestDb.Context(connection))
        {
            var updatedRun = await db.AgentRuns.SingleAsync(r => r.Id == runId);
            Assert.Equal("Completed", updatedRun.Status);
            Assert.Equal("gpt-4o", updatedRun.Model);

            var updatedDec = await db.AgentDecisions.SingleAsync(d => d.Id == decisionId);
            Assert.Equal("Llm", updatedDec.ExplanationSource);
            Assert.Equal("Cobertura en HOS-N01 es 5,0 dias para DES-01", updatedDec.Rationale);
            Assert.Null(updatedDec.ExplanationMismatch);
        }

        // 3. Resubmitting result after completion -> 409 Conflict (Late)
        var lateResp = await autoClient.PostAsJsonAsync($"/api/v1/agent/runs/{runId}/result", resultPayload);
        Assert.Equal(HttpStatusCode.Conflict, lateResp.StatusCode);
    }

    [Fact]
    public async Task Incident_details_can_be_retrieved_via_GET()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);

        var incidentReq = new
        {
            kind = "BloqueoVial",
            lat = -0.180653m,
            lon = -78.467834m,
            radiusKm = 2.0m,
            severity = "ALTA",
            description = "Derrumbe en tramo norte"
        };
        var createResp = await client.PostAsJsonAsync("/api/v1/incidents", incidentReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        using var doc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var incidentId = doc.RootElement.GetProperty("incidentId").GetGuid();

        // GET /api/v1/incidents/{id}
        var getResp = await client.GetAsync($"/api/v1/incidents/{incidentId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        Assert.Equal(incidentId, getDoc.RootElement.GetProperty("incidentId").GetGuid());
        Assert.Equal("BloqueoVial", getDoc.RootElement.GetProperty("kind").GetString());
        Assert.True(getDoc.RootElement.TryGetProperty("affectedRoutes", out _));
    }
}
