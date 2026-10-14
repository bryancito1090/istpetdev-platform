using System.Net;
using System.Net.Http.Headers;
using IstpetDev.Domain.Common;
using IstpetDev.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class AuthenticationClosureTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Retired_auth_login_route_returns_not_found()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var client = api.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/login", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Retired_auth_me_route_returns_not_found()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Production_without_authority_fails_during_startup()
    {
        using var api = ProductionApi(authority: "", allowedClientId: "production-client");

        var exception = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.Contains("Authentication:Authority es obligatorio fuera de Development", exception.ToString());
    }

    [Fact]
    public void Production_without_allowed_client_ids_fails_during_startup()
    {
        using var api = ProductionApi(authority: TestTokens.Issuer, allowedClientId: null);

        var exception = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.Contains("Authentication:AllowedClientIds es obligatorio fuera de Development", exception.ToString());
    }

    [Fact]
    public async Task Production_with_cognito_authority_and_client_id_starts()
    {
        using var api = ProductionApi(
            authority: "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_example",
            allowedClientId: "production-client");
        using var client = api.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Testing_keeps_local_authentication_configuration_optional()
    {
        using var api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Main"] = "Host=localhost;Database=not_used;Username=not_used;Password=not_used",
                ["Authentication:Authority"] = "",
                ["Authentication:AllowedClientIds:0"] = null,
                ["Demo:Enabled"] = "false",
                ["ApplyMigrations"] = "false",
                ["EnableSwagger"] = "false"
            }));
        });
        using var client = api.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> ProductionApi(string authority, string? allowedClientId) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Main"] = "Host=localhost;Database=not_used;Username=not_used;Password=not_used",
                ["Authentication:Authority"] = authority,
                ["Authentication:AllowedClientIds:0"] = allowedClientId,
                ["Demo:Enabled"] = "false",
                ["ApplyMigrations"] = "false",
                ["EnableSwagger"] = "false"
            }));
        });
}
