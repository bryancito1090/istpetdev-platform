using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IstpetDev.Application.Agent;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Agent;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
[Trait("Category", "Integration")]
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
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.GetAsync("/api/v1/risks")));
        await using (var db = TestDb.Context(connection))
        {
            using var traces = JsonDocument.Parse((await db.AgentRuns.SingleAsync(r => r.Id == runId)).ToolCallsJson);
            Assert.Equal(9, traces.RootElement.GetArrayLength());
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

    [Fact]
    public async Task Watch_tool_creates_run_correlated_decisions_that_can_be_completed_by_n8n()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: new Dictionary<string, string?>
        {
            ["Authentication:AllowedClientIds:1"] = TechnicalClientId,
            ["Authentication:TechnicalClientIds:0"] = TechnicalClientId,
            ["Authentication:TechnicalScope"] = "istpetdev-api/automation"
        });
        using var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null)).StatusCode);

        Guid runId;
        await using (var db = TestDb.Context(connection))
        {
            db.AgentRuns.Add(new AgentRun
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Watch",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                ToolCallsJson = "[]"
            });
            await db.SaveChangesAsync();
            runId = await db.AgentRuns.Select(r => r.Id).SingleAsync();
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

        using var automation = api.CreateClient();
        var token = TestTokens.Create(
            TechnicalClientId,
            clientId: TechnicalClientId,
            scope: "istpetdev-api/automation",
            role: Roles.Automatizacion);
        automation.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        automation.DefaultRequestHeaders.Add("X-Agent-Run-Id", runId.ToString());

        var planResponse = await automation.PostAsJsonAsync("/api/v1/route-plans", new
        {
            date = "2026-10-12",
            vehicleCodes = (string[]?)null,
            includeConsolidation = true
        });
        Assert.Equal(HttpStatusCode.Created, planResponse.StatusCode);
        using var planDocument = JsonDocument.Parse(await planResponse.Content.ReadAsStringAsync());
        var decisionIds = planDocument.RootElement.GetProperty("routes").EnumerateArray()
            .Select(route => route.GetProperty("agentDecisionId").GetGuid())
            .ToArray();
        Assert.Equal(2, decisionIds.Length);

        var result = new AgentResult(
            decisionIds.Select(id => new DecisionText(id, "Propuesta calculada con los datos de la ruta.", "Continúa con la siguiente parada del plan.")).ToArray(),
            "Las rutas propuestas están listas para revisión.",
            "deepseek-chat",
            "v1",
            "execution-test",
            null);
        var resultResponse = await automation.PostAsJsonAsync($"/api/v1/agent/runs/{runId}/result", result);
        Assert.Equal(HttpStatusCode.Accepted, resultResponse.StatusCode);

        using var viewResponse = await automation.GetAsync($"/api/v1/agent/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);
        var view = await viewResponse.Content.ReadFromJsonAsync<AgentRunView>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });
        Assert.NotNull(view);
        Assert.Equal(AgentRunStatus.Completed, view.Status);
        Assert.Equal(2, view.Decisions.Count);
        Assert.All(view.Decisions, decision => Assert.Equal(ExplanationSource.Llm, decision.Source));
    }

    [Fact]
    public async Task Incident_replan_decision_is_correlated_to_its_run_for_the_n8n_result()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: new Dictionary<string, string?>
        {
            ["Authentication:AllowedClientIds:1"] = TechnicalClientId,
            ["Authentication:TechnicalClientIds:0"] = TechnicalClientId,
            ["Authentication:TechnicalScope"] = "istpetdev-api/automation",
            ["Agent:AutoOnIncident"] = "false"
        });
        using var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null)).StatusCode);

        var planResponse = await planner.PostAsJsonAsync("/api/v1/route-plans", new
        {
            date = "2026-10-12",
            vehicleCodes = (string[]?)null,
            includeConsolidation = true
        });
        Assert.Equal(HttpStatusCode.Created, planResponse.StatusCode);
        using var plans = JsonDocument.Parse(await planResponse.Content.ReadAsStringAsync());
        var planIds = plans.RootElement.GetProperty("routes").EnumerateArray()
            .Select(route => route.GetProperty("routePlanId").GetGuid())
            .ToArray();
        foreach (var planId in planIds)
            Assert.Equal(HttpStatusCode.OK, (await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/publish", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsJsonAsync($"/api/v1/route-plans/{planIds[0]}/stops/1/complete", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsJsonAsync($"/api/v1/route-plans/{planIds[0]}/stops/2/complete", new { })).StatusCode);

        var incidentResponse = await planner.PostAsJsonAsync("/api/v1/incidents", new
        {
            kind = "BloqueoVial",
            lat = -0.1300m,
            lon = -78.4800m,
            radiusKm = 2.5m,
            severity = "ALTA",
            description = "Cierre vial de prueba",
            isSynthetic = true
        });
        Assert.Equal(HttpStatusCode.Created, incidentResponse.StatusCode);
        using var incident = JsonDocument.Parse(await incidentResponse.Content.ReadAsStringAsync());
        var incidentId = incident.RootElement.GetProperty("incidentId").GetGuid();
        var affectedPlanId = incident.RootElement.GetProperty("affectedRoutes")[0].GetProperty("routePlanId").GetGuid();

        Guid runId;
        await using (var db = TestDb.Context(connection))
        {
            var run = new AgentRun
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Incident",
                Status = "Running",
                TriggerJson = JsonSerializer.Serialize(new { incidentId }),
                StartedAtUtc = DateTime.UtcNow,
                ToolCallsJson = "[]"
            };
            db.AgentRuns.Add(run);
            db.Add(new Account
            {
                Kind = "technical",
                Issuer = TestTokens.Issuer,
                Subject = TechnicalClientId,
                Role = Roles.Automatizacion,
                IsActive = true
            });
            await db.SaveChangesAsync();
            runId = run.Id;
        }

        using var automation = api.CreateClient();
        var token = TestTokens.Create(
            TechnicalClientId,
            clientId: TechnicalClientId,
            scope: "istpetdev-api/automation",
            role: Roles.Automatizacion);
        automation.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        automation.DefaultRequestHeaders.Add("X-Agent-Run-Id", runId.ToString());

        var competing = await Task.WhenAll(
            automation.PostAsJsonAsync($"/api/v1/route-plans/{affectedPlanId}/replan", new { incidentId }),
            automation.PostAsJsonAsync($"/api/v1/route-plans/{affectedPlanId}/replan", new { incidentId }));
        Assert.Single(competing, response => response.StatusCode == HttpStatusCode.Conflict);
        var replanResponse = Assert.Single(competing, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.Created, replanResponse.StatusCode);
        using var replan = JsonDocument.Parse(await replanResponse.Content.ReadAsStringAsync());
        var decisionId = replan.RootElement.GetProperty("agentDecisionId").GetGuid();

        var resultResponse = await automation.PostAsJsonAsync($"/api/v1/agent/runs/{runId}/result", new AgentResult(
            [new DecisionText(decisionId, "La ruta fue recalculada con los hechos del incidente.", "Sigue la siguiente parada del plan.")],
            "La propuesta está lista para revisión.",
            "deepseek-chat",
            "v1",
            "incident-execution-test",
            null));
        Assert.Equal(HttpStatusCode.Accepted, resultResponse.StatusCode);

        var view = await automation.GetFromJsonAsync<AgentRunView>($"/api/v1/agent/runs/{runId}", new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });
        Assert.NotNull(view);
        Assert.Equal(AgentRunStatus.Completed, view.Status);
        Assert.Contains(view.Decisions, decision => decision.AgentDecisionId == decisionId && decision.Source == ExplanationSource.Llm);

        // Human approval resolves the proposed plan from ProposalJson even when
        // CorrelationId now points to the agent run. Repeating it is idempotent.
        await using var approvalDb = TestDb.Context(connection);
        var decision = await approvalDb.AgentDecisions.SingleAsync(d => d.Id == decisionId);
        using var proposal = JsonDocument.Parse(decision.ProposalJson);
        var proposedPlanId = proposal.RootElement.GetProperty("proposedRoutePlanId").GetGuid();
        var approval = await planner.PostAsJsonAsync($"/api/v1/agent-decisions/{decisionId}/approve", new { option = "A" });
        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        var repeated = await planner.PostAsJsonAsync($"/api/v1/agent-decisions/{decisionId}/approve", new { option = "A" });
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal("Published", (await approvalDb.RoutePlans.AsNoTracking().SingleAsync(p => p.Id == proposedPlanId)).Status);
    }
}
