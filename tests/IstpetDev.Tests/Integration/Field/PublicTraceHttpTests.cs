using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class PublicTraceHttpTests(PostgisContainer postgres)
{
    private const string Token = "token-de-prueba-0123456789abcdef0123456789abcdef";
    private static readonly string[] AllowedFields = ["unitId", "status", "productUnits", "events"];

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static async Task<Guid> SeedUnitAsync(ApiFactory api, DateTime expiresAtUtc, DateTime? revokedAtUtc = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m, suffix);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actor = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var unit = new HandlingUnit { Code = $"CAJA-{suffix}", LocationId = d.VehicleLocationId, Status = "Dispatched", DeliveryId = d.DeliveryId };
        db.AddRange(unit,
            new HandlingUnitLine { HandlingUnitId = unit.Id, LotId = d.LotId, Quantity = 12m },
            new QrAccessToken { HandlingUnitId = unit.Id, TokenHash = Hash(Token), ExpiresAtUtc = expiresAtUtc, RevokedAtUtc = revokedAtUtc, CreatedByAccountId = actor.Id },
            new CustodyEvent { HandlingUnitId = unit.Id, DeliveryId = d.DeliveryId, ActorAccountId = actor.Id, EventKind = "Dispatched",
                               OperationId = Domain.Common.BaseEntity.NewId(), ReceivedAtServer = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return unit.Id;
    }

    private static Task<HttpResponseMessage> GetTrace(ApiFactory api, Guid unitId, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/public/trace/{unitId}");
        if (token is not null) request.Headers.Add("X-Trace-Token", token);
        return api.CreateClient().SendAsync(request);
    }

    [Fact]
    public async Task Valid_token_in_header_returns_only_public_fields()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var unitId = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7));

        var response = await GetTrace(api, unitId, Token);
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(text);
        Assert.All(json.RootElement.EnumerateObject(), p => Assert.Contains(p.Name, AllowedFields));
        Assert.DoesNotContain(Token, text);
        Assert.DoesNotContain("quantity", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("actor", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_expired_revoked_missing_or_query_tokens_are_uniform_404()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var valid = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7));
        var expired = await SeedUnitAsync(api, DateTime.UtcNow.AddMinutes(-1));
        var revoked = await SeedUnitAsync(api, DateTime.UtcNow.AddDays(7), DateTime.UtcNow);

        var responses = new[]
        {
            await GetTrace(api, valid, null),
            await GetTrace(api, valid, Token + "x"),
            await GetTrace(api, expired, Token),
            await GetTrace(api, revoked, Token),
            await GetTrace(api, Guid.NewGuid(), Token),
            await api.CreateClient().GetAsync($"/api/v1/public/trace/{valid}?token={Token}"),
        };

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
    }
}
