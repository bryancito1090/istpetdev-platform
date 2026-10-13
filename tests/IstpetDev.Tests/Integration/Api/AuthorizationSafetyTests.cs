using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class AuthorizationSafetyTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Forged_role_does_not_elevate_and_revocation_applies_to_the_same_token()
    {
        var connection = postgres.NewDatabase(); await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Driver);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(TestTokens.Driver, role: Roles.Gerencia));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/service-points")).StatusCode);
        await using (var db = TestDb.Context(connection))
        {
            var account = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
            account.IsActive = false; await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/service-points")).StatusCode);
        await using (var db = TestDb.Context(connection))
        {
            await AccountSeeder.SeedAsync(db, TestTokens.Issuer, [new IstpetDev.Application.Demo.DemoAccount { Subject = TestTokens.Driver, Role = Roles.Gerencia }]);
            var revoked = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
            Assert.False(revoked.IsActive); Assert.Equal(Roles.Conductor, revoked.Role);
        }
    }

    [Theory]
    [InlineData(Roles.Gerencia, HttpStatusCode.OK)]
    [InlineData(Roles.Planificador, HttpStatusCode.OK)]
    [InlineData(Roles.Bodega, HttpStatusCode.Forbidden)]
    [InlineData(Roles.Conductor, HttpStatusCode.Forbidden)]
    [InlineData(Roles.Receptor, HttpStatusCode.Forbidden)]
    public async Task Fixed_role_matrix_is_enforced(string role, HttpStatusCode expected)
    {
        var connection = postgres.NewDatabase(); await using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Driver);
        await using (var db = TestDb.Context(connection))
        { var account = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver); account.Role = role; await db.SaveChangesAsync(); }
        using var content = new MultipartFormDataContent { { new ByteArrayContent("sku,name,base_unit\nS,Producto,L\n"u8.ToArray()), "products", "p.csv" } };
        Assert.Equal(expected, (await client.PostAsync("/api/v1/imports", content)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/service-points")).StatusCode);
    }

    [Fact]
    public async Task Invalid_issuer_client_scope_signature_expiration_and_id_token_are_rejected()
    {
        await using var api = new ApiFactory(postgres.NewDatabase()); using var client = api.ClientFor(TestTokens.Planner);
        using var rsa = RSA.Create(2048);
        string[] invalid = [
            TestTokens.Create(TestTokens.Planner, issuer: "https://invalid.example"),
            TestTokens.Create(TestTokens.Planner, clientId: "wrong-client"),
            TestTokens.Create(TestTokens.Planner, scope: ""),
            TestTokens.Create(TestTokens.Planner, signingKey: new RsaSecurityKey(rsa)),
            TestTokens.Create(TestTokens.Planner, expires: DateTime.UtcNow.AddMinutes(-5)),
            TestTokens.Create(TestTokens.Planner, tokenUse: "id")];
        foreach (var token in invalid)
        { client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/service-points")).StatusCode); }
    }

    [Fact]
    public async Task Technical_account_uses_registered_client_and_technical_scope_and_cannot_import()
    {
        const string technicalClient = "technical-test-client";
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, overrides: new Dictionary<string, string?>
        { ["Authentication:AllowedClientIds:1"] = technicalClient, ["Authentication:TechnicalClientIds:0"] = technicalClient });
        using var client = api.ClientFor(TestTokens.Planner);
        await using (var db = TestDb.Context(connection))
        { db.Add(new Account { Kind = "technical", Issuer = TestTokens.Issuer, Subject = technicalClient, Role = Roles.Automatizacion, IsActive = true }); await db.SaveChangesAsync(); }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create("provider-sub", clientId: technicalClient, scope: "automation/risk.read"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/service-points")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/imports", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/demo/reset", null)).StatusCode);
    }
}
