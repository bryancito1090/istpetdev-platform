using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

[Trait("Category", "Integration")]
public class ReceiptRejectionHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private const string Receiver = "receiver-1";
    private const string Token = "token-receptor-0123456789abcdef0123456789abcdef";

    private ApiFactory NewApi() => new(postgres.NewDatabase(), overrides: new Dictionary<string, string?>
    {
        ["Demo:Accounts:5:Subject"] = Receiver,
        ["Demo:Accounts:5:DisplayName"] = "Receptor de prueba",
        ["Demo:Accounts:5:Role"] = Roles.Receptor,
    });

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static async Task<(Guid UnitId, FieldSeed.Dispatched Delivery)> SeedUnitAsync(ApiFactory api, decimal quantity = 12m)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var d = await FieldSeed.DispatchedDeliveryAsync(api, quantity, suffix);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actor = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var unit = new HandlingUnit { Code = $"CAJA-{suffix}", LocationId = d.VehicleLocationId, Status = "Dispatched", DeliveryId = d.DeliveryId };
        db.AddRange(unit,
            new HandlingUnitLine { HandlingUnitId = unit.Id, LotId = d.LotId, Quantity = quantity },
            new QrAccessToken { HandlingUnitId = unit.Id, TokenHash = Hash(Token), ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedByAccountId = actor.Id });
        await db.SaveChangesAsync();
        return (unit.Id, d);
    }

    private static HttpRequestMessage Request(HttpMethod method, string url, string? token, object? body = null, Guid? operationId = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null) request.Headers.Add("X-Trace-Token", token);
        if (operationId is not null) request.Headers.Add("Idempotency-Key", operationId.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static object ReceiptBody(Guid operationId, FieldSeed.Dispatched d, decimal accepted, decimal rejected, string? rejectionReason) => new
    {
        operationId,
        expectedDeliveryVersion = 0,
        occurredAtDevice = DateTime.UtcNow,
        lines = new[]
        {
            new
            {
                deliveryLineId = d.DeliveryLineId,
                lotId = d.LotId,
                quantityAccepted = accepted,
                quantityRejected = rejected,
                rejectionReason,
                unit = "L"
            }
        },
    };

    [Fact]
    public async Task Rejection_reasons_catalog_endpoint_returns_catalog()
    {
        await using var api = NewApi();
        var response = await api.ClientFor(Receiver).GetAsync("/api/v1/receipts/rejection-reasons");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        Assert.Equal(ReceiptRejectionReasons.Version, root.GetProperty("version").GetString());
        var items = root.GetProperty("items").EnumerateArray();
        Assert.Contains(items, item => item.GetProperty("code").GetString() == "faltante" && item.GetProperty("label").GetString() == "Faltante");
    }

    [Fact]
    public async Task Rejection_validation_enforces_catalog_codes()
    {
        await using var api = NewApi();
        var (unitId, d) = await SeedUnitAsync(api);
        var client = api.ClientFor(Receiver);

        // 1. Rechazo con "roto por golpe" → 422 INVALID_REJECTION_REASON
        var opInvalid = Guid.NewGuid();
        var invalidBody = ReceiptBody(opInvalid, d, accepted: 10m, rejected: 2m, rejectionReason: "roto por golpe");
        var resInvalid = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/v1/receiver/handling-units/{unitId}/receipt", Token, invalidBody, opInvalid));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resInvalid.StatusCode);
        var errText = await resInvalid.Content.ReadAsStringAsync();
        Assert.Contains(FieldErrorCodes.InvalidRejectionReason, errText);

        // 2. Rechazo con "dano: caja aplastada" → 200 OK
        var opValid = Guid.NewGuid();
        var validBody = ReceiptBody(opValid, d, accepted: 10m, rejected: 2m, rejectionReason: "dano: caja aplastada");
        var resValid = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/v1/receiver/handling-units/{unitId}/receipt", Token, validBody, opValid));

        Assert.Equal(HttpStatusCode.OK, resValid.StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var receiptLine = await db.ReceiptLines.SingleAsync(r => r.DeliveryLineId == d.DeliveryLineId);
        Assert.Equal(2m, receiptLine.QuantityRejected);
        Assert.Equal("dano: caja aplastada", receiptLine.RejectionReason);
    }
}
