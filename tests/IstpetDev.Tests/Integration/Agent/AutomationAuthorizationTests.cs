using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Agent;

[Trait("Category", "Integration")]
public sealed class AutomationAuthorizationTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private const string TechnicalClientId = "n8n-automation-client";

    [Fact]
    public async Task Technical_token_with_automation_scope_cannot_execute_human_decision_endpoints()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, overrides: new Dictionary<string, string?>
        {
            ["Authentication:AllowedClientIds:1"] = TechnicalClientId,
            ["Authentication:TechnicalClientIds:0"] = TechnicalClientId,
            ["Authentication:TechnicalScope"] = "istpetdev-api/automation"
        });

        using var client = api.ClientFor(TestTokens.Planner);

        // Seed technical account in DB
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
        var automationToken = TestTokens.Create(
            subject: TechnicalClientId,
            clientId: TechnicalClientId,
            scope: "istpetdev-api/automation",
            role: Roles.Automatizacion);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", automationToken);

        var randomId = Guid.NewGuid();

        // 1. POST /api/v1/route-plans/{id}/publish -> 403 Forbidden
        var publishResp = await client.PostAsync($"/api/v1/route-plans/{randomId}/publish", null);
        Assert.Equal(HttpStatusCode.Forbidden, publishResp.StatusCode);

        // 2. POST /api/v1/agent-decisions/{id}/approve -> 403 Forbidden
        var approveContent = new StringContent(JsonSerializer.Serialize(new { option = "Strict" }), Encoding.UTF8, "application/json");
        var approveResp = await client.PostAsync($"/api/v1/agent-decisions/{randomId}/approve", approveContent);
        Assert.Equal(HttpStatusCode.Forbidden, approveResp.StatusCode);

        // 3. POST /api/v1/agent-decisions/{id}/reject -> 403 Forbidden
        var rejectResp = await client.PostAsync($"/api/v1/agent-decisions/{randomId}/reject", null);
        Assert.Equal(HttpStatusCode.Forbidden, rejectResp.StatusCode);

        // 4. POST /api/v1/route-plans/{id}/revert -> 403 Forbidden
        var revertResp = await client.PostAsync($"/api/v1/route-plans/{randomId}/revert", null);
        Assert.Equal(HttpStatusCode.Forbidden, revertResp.StatusCode);

        // 5. PUT /api/v1/autonomy/mode -> 403 Forbidden
        var modeContent = new StringContent(JsonSerializer.Serialize(new { mode = "ProposeOnly" }), Encoding.UTF8, "application/json");
        var modeResp = await client.PutAsync("/api/v1/autonomy/mode", modeContent);
        Assert.Equal(HttpStatusCode.Forbidden, modeResp.StatusCode);

        // Permitted 1: GET /api/v1/risks -> 200 OK
        var risksResp = await client.GetAsync("/api/v1/risks");
        Assert.Equal(HttpStatusCode.OK, risksResp.StatusCode);

        // Permitted 2: POST /api/v1/incidents -> 201 Created
        var incidentReq = new
        {
            kind = "BloqueoVial",
            lat = -0.180653m,
            lon = -78.467834m,
            radiusKm = 2.0m,
            severity = "ALTA",
            description = "Derrumbe detectado por monitoreo vial"
        };
        var incidentResp = await client.PostAsJsonAsync("/api/v1/incidents", incidentReq);
        Assert.Equal(HttpStatusCode.Created, incidentResp.StatusCode);
    }

    [Fact]
    public async Task Technical_client_with_missing_scope_is_rejected_with_401()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, overrides: new Dictionary<string, string?>
        {
            ["Authentication:AllowedClientIds:1"] = TechnicalClientId,
            ["Authentication:TechnicalClientIds:0"] = TechnicalClientId,
            ["Authentication:TechnicalScope"] = "istpetdev-api/automation"
        });

        using var client = api.ClientFor(TestTokens.Planner);

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
        // Missing the required automation scope
        var badToken = TestTokens.Create(
            subject: TechnicalClientId,
            clientId: TechnicalClientId,
            scope: "unrelated/scope",
            role: Roles.Automatizacion);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", badToken);

        var resp = await client.GetAsync("/api/v1/risks");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
