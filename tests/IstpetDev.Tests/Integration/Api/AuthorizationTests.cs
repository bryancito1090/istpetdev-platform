using System.Net;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class AuthorizationTests(PostgisContainer postgres)
{
    [Theory]
    [InlineData(null, "access", HttpStatusCode.Unauthorized)]
    [InlineData("identidad-sin-cuenta", "access", HttpStatusCode.Forbidden)]
    [InlineData(TestTokens.Planner, "id", HttpStatusCode.Unauthorized)]
    [InlineData(TestTokens.Planner, "access", HttpStatusCode.OK)]
    [InlineData(TestTokens.Driver, "access", HttpStatusCode.OK)]
    public async Task Service_points_require_a_registered_role(string? subject, string tokenUse, HttpStatusCode expected)
    {
        await using var api = new ApiFactory(postgres.NewDatabase());

        var response = await api.ClientFor(subject, tokenUse).GetAsync("/api/v1/service-points");

        Assert.Equal(expected, response.StatusCode);
    }
}
