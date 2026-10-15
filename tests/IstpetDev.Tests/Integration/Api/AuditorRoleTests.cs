using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Api;

[Trait("Category", "Integration")]
public sealed class AuditorRoleTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static readonly string[] Reads =
        ["/api/v1/service-points", "/api/v1/risks", "/api/v1/route-plans", "/api/v1/agent-decisions", "/api/v1/autonomy/mode", "/api/v1/deliveries", "/api/v1/simulations/comparisons",
         "/api/v1/receipts/exceptions", "/api/v1/delivery-orders?sourceType=FieldRequest"];

    private static readonly (HttpMethod Method, string Path)[] Writes =
    [
        (HttpMethod.Post, "/api/v1/imports"), (HttpMethod.Post, "/api/v1/demo/reset"), (HttpMethod.Post, "/api/v1/demo/incidents"),
        (HttpMethod.Post, "/api/v1/incidents"), (HttpMethod.Post, "/api/v1/route-plans"),
        (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/publish"), (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/replan"),
        (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/revert"), (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/stops/1/complete"),
        (HttpMethod.Post, $"/api/v1/agent-decisions/{Guid.Empty}/approve"), (HttpMethod.Post, $"/api/v1/agent-decisions/{Guid.Empty}/reject"),
        (HttpMethod.Put, "/api/v1/autonomy/mode"),
        (HttpMethod.Post, $"/api/v1/deliveries/{Guid.Empty}/handling-units"),
        (HttpMethod.Post, $"/api/v1/deliveries/{Guid.Empty}/dispatch"),
        (HttpMethod.Post, $"/api/v1/deliveries/{Guid.Empty}/receipts"),
        (HttpMethod.Post, $"/api/v1/handling-units/{Guid.Empty}/public-token/revoke"),
        (HttpMethod.Post, $"/api/v1/receipts/{Guid.Empty}/evidence-uploads"),
        (HttpMethod.Post, $"/api/v1/evidence/{Guid.Empty}/confirm"),
        (HttpMethod.Post, "/api/v1/agent/runs"),
        (HttpMethod.Post, $"/api/v1/demo/route-plans/{Guid.Empty}/advance"),
        (HttpMethod.Post, "/api/v1/simulations/comparisons"),
        (HttpMethod.Post, "/api/v1/simulations/comparisons/import"),
        (HttpMethod.Patch, $"/api/v1/delivery-orders/{Guid.Empty}/cancel"),
    ];

    [Fact]
    public async Task Auditor_reads_everything_and_writes_nothing()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, Roles.Auditor);
        using var auditor = api.ClientFor(TestTokens.Driver);

        foreach (var path in Reads)
            Assert.True((await auditor.GetAsync(path)).StatusCode == HttpStatusCode.OK, $"GET {path}");

        var unknownComparison = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await auditor.GetAsync($"/api/v1/simulations/comparisons/{unknownComparison}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await auditor.GetAsync($"/api/v1/simulations/comparisons/{unknownComparison}/export")).StatusCode);

        var traceResponse = await auditor.GetAsync($"/api/v1/handling-units/{Guid.NewGuid()}/trace");
        Assert.NotEqual(HttpStatusCode.Forbidden, traceResponse.StatusCode);
        Assert.True(traceResponse.StatusCode == HttpStatusCode.OK || traceResponse.StatusCode == HttpStatusCode.NotFound, $"GET trace status {traceResponse.StatusCode}");

        foreach (var (method, path) in Writes)
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(new { }) };
            Assert.True((await auditor.SendAsync(request)).StatusCode == HttpStatusCode.Forbidden, $"{method} {path}");
        }
    }

    [Fact]
    public async Task Simulation_http_permissions_follow_the_role_matrix()
    {
        const string technicalClient = "technical-simulation-client";
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, overrides: new Dictionary<string, string?>
        {
            ["Authentication:AllowedClientIds:1"] = technicalClient,
            ["Authentication:TechnicalClientIds:0"] = technicalClient
        });
        using var bootstrap = api.ClientFor(TestTokens.Driver);
        var unknownComparison = Guid.NewGuid();

        foreach (var (role, canRead, canWrite) in new (string Role, bool CanRead, bool CanWrite)[]
        {
            (Roles.Gerencia, true, true), (Roles.Planificador, true, true),
            (Roles.Auditor, true, false), (Roles.Bodega, false, false),
            (Roles.Conductor, false, false), (Roles.Receptor, false, false)
        })
        {
            await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, role);
            using var client = api.ClientFor(TestTokens.Driver);
            var expectedRead = canRead ? HttpStatusCode.OK : HttpStatusCode.Forbidden;
            var expectedMissingRead = canRead ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden;
            Assert.Equal(expectedRead, (await client.GetAsync("/api/v1/simulations/comparisons")).StatusCode);
            Assert.Equal(expectedMissingRead, (await client.GetAsync($"/api/v1/simulations/comparisons/{unknownComparison}")).StatusCode);
            Assert.Equal(expectedMissingRead, (await client.GetAsync($"/api/v1/simulations/comparisons/{unknownComparison}/export")).StatusCode);

            // Empty import reaches validation only for roles allowed to write; it never changes data.
            var expectedWrite = canWrite ? HttpStatusCode.BadRequest : HttpStatusCode.Forbidden;
            Assert.Equal(expectedWrite, (await client.PostAsync("/api/v1/simulations/comparisons/import", null)).StatusCode);
        }

        await using (var db = TestDb.Context(connection))
        {
            db.Add(new Account { Kind = "technical", Issuer = TestTokens.Issuer, Subject = technicalClient, Role = Roles.Automatizacion, IsActive = true });
            await db.SaveChangesAsync();
        }

        using var automation = api.ClientFor(TestTokens.Driver);
        automation.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestTokens.Create("provider-sub", clientId: technicalClient, scope: "automation/risk.read"));
        Assert.Equal(HttpStatusCode.Forbidden, (await automation.GetAsync("/api/v1/simulations/comparisons")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await automation.GetAsync($"/api/v1/simulations/comparisons/{unknownComparison}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await automation.GetAsync($"/api/v1/simulations/comparisons/{unknownComparison}/export")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await automation.PostAsync("/api/v1/simulations/comparisons/import", null)).StatusCode);
    }
}
