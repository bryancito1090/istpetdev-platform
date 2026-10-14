using System.Net;
using System.Net.Http.Headers;
using System.Text;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class UnprovisionedFieldAccountTests(PostgisContainer postgres)
{
    public static TheoryData<string, string, string> FieldEndpoints => new()
    {
        { "POST", $"/api/v1/deliveries/{Guid.Empty}/handling-units", Roles.Bodega },
        { "POST", $"/api/v1/deliveries/{Guid.Empty}/dispatch", Roles.Bodega },
        { "POST", $"/api/v1/deliveries/{Guid.Empty}/receipts", Roles.Conductor },
        { "POST", $"/api/v1/receipts/{Guid.Empty}/evidence-uploads", Roles.Conductor },
        { "POST", $"/api/v1/evidence/{Guid.Empty}/confirm", Roles.Conductor },
        { "POST", $"/api/v1/handling-units/{Guid.Empty}/public-token/revoke", Roles.Bodega },
        { "GET", "/api/v1/mobile/assignments", Roles.Conductor }
    };

    [Theory]
    [MemberData(nameof(FieldEndpoints))]
    public async Task Valid_token_for_unprovisioned_subject_is_forbidden_without_returning_field_data(
        string method,
        string path,
        string role)
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestTokens.Create("unprovisioned-field-subject", role: role));
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
        {
            request.Content = new StringContent(RequestBody(path), Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        await using var database = TestDb.Context(connection);
        Assert.False(await database.Accounts.AnyAsync(account => account.Subject == "unprovisioned-field-subject"));
    }

    private static string RequestBody(string path) => path switch
    {
        var value when value.EndsWith("/handling-units", StringComparison.Ordinal) =>
            "{\"operationId\":\"00000000-0000-0000-0000-000000000001\",\"lines\":[]}",
        var value when value.EndsWith("/dispatch", StringComparison.Ordinal) =>
            "{\"operationId\":\"00000000-0000-0000-0000-000000000001\",\"handlingUnitIds\":[]}",
        var value when value.EndsWith("/receipts", StringComparison.Ordinal) =>
            "{\"operationId\":\"00000000-0000-0000-0000-000000000001\",\"expectedDeliveryVersion\":0,\"occurredAtDevice\":\"2026-10-14T00:00:00Z\",\"lines\":[]}",
        var value when value.EndsWith("/evidence-uploads", StringComparison.Ordinal) =>
            "{\"operationId\":\"00000000-0000-0000-0000-000000000001\",\"kind\":\"Photo\",\"mimeType\":\"image/jpeg\",\"byteSize\":1,\"sha256\":\"abc\"}",
        _ => "{\"operationId\":\"00000000-0000-0000-0000-000000000001\",\"sha256\":\"abc\",\"byteSize\":1}"
    };
}
