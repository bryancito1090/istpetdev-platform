using System.Net;
using System.Net.Http.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class AuditorRoleTests(PostgisContainer postgres)
{
    private static readonly string[] Reads =
        ["/api/v1/service-points", "/api/v1/risks", "/api/v1/route-plans", "/api/v1/agent-decisions", "/api/v1/autonomy/mode"];

    private static readonly (HttpMethod Method, string Path)[] Writes =
    [
        (HttpMethod.Post, "/api/v1/imports"), (HttpMethod.Post, "/api/v1/demo/reset"), (HttpMethod.Post, "/api/v1/demo/incidents"),
        (HttpMethod.Post, "/api/v1/incidents"), (HttpMethod.Post, "/api/v1/route-plans"),
        (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/publish"), (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/replan"),
        (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/revert"), (HttpMethod.Post, $"/api/v1/route-plans/{Guid.Empty}/stops/1/complete"),
        (HttpMethod.Post, $"/api/v1/agent-decisions/{Guid.Empty}/approve"), (HttpMethod.Post, $"/api/v1/agent-decisions/{Guid.Empty}/reject"),
        (HttpMethod.Put, "/api/v1/autonomy/mode"),
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
        foreach (var (method, path) in Writes)
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(new { }) };
            Assert.True((await auditor.SendAsync(request)).StatusCode == HttpStatusCode.Forbidden, $"{method} {path}");
        }
    }
}
