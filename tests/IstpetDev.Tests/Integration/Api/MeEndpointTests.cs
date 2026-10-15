using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Api;

[Trait("Category", "Integration")]
public sealed class MeEndpointTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Theory]
    [InlineData(Roles.Gerencia, "web")]
    [InlineData(Roles.Planificador, "web")]
    [InlineData(Roles.Auditor, "web")]
    [InlineData(Roles.Bodega, "field")]
    [InlineData(Roles.Conductor, "field")]
    [InlineData(Roles.Receptor, "field")]
    public async Task Me_returns_account_role_and_platform(string role, string platform)
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Driver);
        var id = await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, role);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/me");

        Assert.Equal(id, me.GetProperty("accountId").GetGuid());
        Assert.Equal("Conductor de prueba", me.GetProperty("displayName").GetString());
        Assert.Equal(role, me.GetProperty("role").GetString());
        Assert.Equal(platform, me.GetProperty("platform").GetString());
    }

    [Fact]
    public async Task Me_is_forbidden_without_an_active_account()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var stranger = api.ClientFor("identidad-sin-cuenta");
        using var driver = api.ClientFor(TestTokens.Driver);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/api/v1/me")).StatusCode);

        await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, Roles.Conductor, active: false);

        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.ClientFor(null).GetAsync("/api/v1/me")).StatusCode);
    }
}
