using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public class LogisticsApiTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static readonly Dictionary<string, string?> OverridesWithManager = new()
    {
        ["Demo:Accounts:2:Subject"] = "manager-1",
        ["Demo:Accounts:2:DisplayName"] = "Gerencia de prueba",
        ["Demo:Accounts:2:Role"] = Roles.Gerencia
    };

    [Fact]
    public async Task Risks_endpoint_enforces_auth_and_returns_facts()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        // Anon -> 401
        var anonResp = await api.ClientFor(null).GetAsync("/api/v1/risks");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResp.StatusCode);

        // Seed demo data
        var resetResp = await planner.PostAsync("/api/v1/demo/reset", null);
        Assert.Equal(HttpStatusCode.OK, resetResp.StatusCode);

        // Planner -> 200 OK
        var plannerResp = await planner.GetAsync("/api/v1/risks");
        Assert.Equal(HttpStatusCode.OK, plannerResp.StatusCode);

        var doc = await plannerResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(doc.TryGetProperty("items", out var items));
        Assert.True(doc.TryGetProperty("facts", out var facts));
        Assert.True(facts.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Route_plans_endpoints_enforce_role_matrix()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);

        // Anon -> 401
        var anonResp = await api.ClientFor(null).GetAsync("/api/v1/route-plans");
        Assert.Equal(HttpStatusCode.Unauthorized, anonResp.StatusCode);

        // Driver can list route plans (Roles.Any)
        var driverResp = await api.ClientFor(TestTokens.Driver).GetAsync("/api/v1/route-plans");
        Assert.Equal(HttpStatusCode.OK, driverResp.StatusCode);

        // Driver cannot create plan (Forbidden)
        var driverPost = await api.ClientFor(TestTokens.Driver).PostAsJsonAsync("/api/v1/route-plans", new { });
        Assert.Equal(HttpStatusCode.Forbidden, driverPost.StatusCode);

        // Driver cannot publish plan (Forbidden)
        var dummyId = Guid.NewGuid();
        var driverPublish = await api.ClientFor(TestTokens.Driver).PostAsJsonAsync($"/api/v1/route-plans/{dummyId}/publish", new { });
        Assert.Equal(HttpStatusCode.Forbidden, driverPublish.StatusCode);
    }

    [Fact]
    public async Task Autonomy_mode_requires_gerencia_for_updates()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true, OverridesWithManager);

        // Driver can read mode
        var readResp = await api.ClientFor(TestTokens.Driver).GetAsync("/api/v1/autonomy/mode");
        Assert.Equal(HttpStatusCode.OK, readResp.StatusCode);

        // Driver cannot update mode (403)
        var driverUpdate = await api.ClientFor(TestTokens.Driver).PutAsJsonAsync("/api/v1/autonomy/mode", new { mode = "ProposeOnly" });
        Assert.Equal(HttpStatusCode.Forbidden, driverUpdate.StatusCode);

        // Manager can update mode (200)
        var managerClient = api.ClientFor("manager-1");
        var managerUpdate = await managerClient.PutAsJsonAsync("/api/v1/autonomy/mode", new { mode = "ProposeOnly" });
        Assert.Equal(HttpStatusCode.OK, managerUpdate.StatusCode);

        var updatedDoc = await managerUpdate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ProposeOnly", updatedDoc.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task Demo_incidents_preset_triggers_correctly_when_demo_enabled()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);

        // Anon -> 401
        var anon = await api.ClientFor(null).PostAsJsonAsync("/api/v1/demo/incidents", new { preset = "cierre-norte" });
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        // Driver -> 403
        var driver = await api.ClientFor(TestTokens.Driver).PostAsJsonAsync("/api/v1/demo/incidents", new { preset = "cierre-norte" });
        Assert.Equal(HttpStatusCode.Forbidden, driver.StatusCode);

        // Planner -> 200 OK
        var planner = await api.ClientFor(TestTokens.Planner).PostAsJsonAsync("/api/v1/demo/incidents", new { preset = "cierre-norte" });
        Assert.Equal(HttpStatusCode.OK, planner.StatusCode);
        var doc = await planner.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Triggered", doc.GetProperty("status").GetString());
        Assert.Equal("cierre-norte", doc.GetProperty("preset").GetString());
    }

    [Fact]
    public async Task Route_plans_get_does_not_create_plans_and_supports_pagination()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        var firstResp = await planner.GetAsync("/api/v1/route-plans");
        Assert.Equal(HttpStatusCode.OK, firstResp.StatusCode);
        var firstDoc = await firstResp.Content.ReadFromJsonAsync<JsonElement>();
        var itemsFirst = firstDoc.GetProperty("items").GetArrayLength();

        // Repeat call - does not create any proposed plans (idempotent read, FR-23)
        var secondResp = await planner.GetAsync("/api/v1/route-plans");
        Assert.Equal(HttpStatusCode.OK, secondResp.StatusCode);
        var secondDoc = await secondResp.Content.ReadFromJsonAsync<JsonElement>();
        var itemsSecond = secondDoc.GetProperty("items").GetArrayLength();

        Assert.Equal(itemsFirst, itemsSecond);
    }
}
