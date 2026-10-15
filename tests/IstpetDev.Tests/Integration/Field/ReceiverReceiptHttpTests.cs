using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

// El Receptor del hospital escanea el QR del bulto (unidad + token) y registra la recepción con su propia cuenta.
[Trait("Category", "Integration")]
public class ReceiverReceiptHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
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

    private static object ReceiptBody(Guid operationId, FieldSeed.Dispatched d, decimal accepted, decimal rejected, string? rejectionReason = null) => new
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
                rejectionReason = rejectionReason ?? (rejected > 0 ? "dano: rotura en transporte" : null),
                unit = "L"
            }
        },
    };

    [Fact]
    public async Task Receiver_with_the_qr_token_sees_what_to_receive()
    {
        await using var api = NewApi();
        var (unitId, d) = await SeedUnitAsync(api);

        var response = await api.ClientFor(Receiver).SendAsync(Request(HttpMethod.Get, $"/api/v1/receiver/handling-units/{unitId}", Token));
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.Equal(d.DeliveryId, root.GetProperty("deliveryId").GetGuid());
        Assert.Equal("Dispatched", root.GetProperty("status").GetString());
        Assert.Equal("Punto de prueba", root.GetProperty("destinationName").GetString());
        var line = Assert.Single(root.GetProperty("lines").EnumerateArray());
        Assert.Equal(d.DeliveryLineId, line.GetProperty("deliveryLineId").GetGuid());
        Assert.Equal("Producto de prueba", line.GetProperty("productName").GetString());
        Assert.Equal(12m, line.GetProperty("quantity").GetDecimal());
        Assert.DoesNotContain(Token, text);
    }

    [Fact]
    public async Task Wrong_or_missing_token_is_404_and_other_roles_are_403()
    {
        await using var api = NewApi();
        var (unitId, _) = await SeedUnitAsync(api);
        var url = $"/api/v1/receiver/handling-units/{unitId}";

        var wrong = await api.ClientFor(Receiver).SendAsync(Request(HttpMethod.Get, url, Token + "x"));
        var missing = await api.ClientFor(Receiver).SendAsync(Request(HttpMethod.Get, url, null));
        var planner = await api.ClientFor(TestTokens.Planner).SendAsync(Request(HttpMethod.Get, url, Token));

        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, planner.StatusCode);
    }

    [Fact]
    public async Task Receiver_registers_the_receipt_and_the_unit_is_received()
    {
        await using var api = NewApi();
        var (unitId, d) = await SeedUnitAsync(api);
        var operationId = Guid.NewGuid();

        var response = await api.ClientFor(Receiver).SendAsync(Request(HttpMethod.Post,
            $"/api/v1/receiver/handling-units/{unitId}/receipt", Token, ReceiptBody(operationId, d, 12m, 0m), operationId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var receiver = await db.Accounts.SingleAsync(a => a.Subject == Receiver);
        Assert.Equal("Delivered", (await db.Deliveries.SingleAsync(x => x.Id == d.DeliveryId)).Status);
        Assert.Equal("Received", (await db.HandlingUnits.SingleAsync(x => x.Id == unitId)).Status);
        Assert.True(await db.Receipts.AnyAsync(r => r.DeliveryId == d.DeliveryId && r.ActorAccountId == receiver.Id));
    }

    [Fact]
    public async Task Repeating_the_same_operation_is_idempotent_and_a_second_receipt_is_rejected()
    {
        await using var api = NewApi();
        var (unitId, d) = await SeedUnitAsync(api);
        var url = $"/api/v1/receiver/handling-units/{unitId}/receipt";
        var operationId = Guid.NewGuid();
        var client = api.ClientFor(Receiver);

        var first = await client.SendAsync(Request(HttpMethod.Post, url, Token, ReceiptBody(operationId, d, 10m, 2m), operationId));
        var retry = await client.SendAsync(Request(HttpMethod.Post, url, Token, ReceiptBody(operationId, d, 10m, 2m), operationId));
        var other = Guid.NewGuid();
        var second = await client.SendAsync(Request(HttpMethod.Post, url, Token, ReceiptBody(other, d, 1m, 0m), other));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await retry.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Receipt_without_a_valid_token_changes_nothing()
    {
        await using var api = NewApi();
        var (unitId, d) = await SeedUnitAsync(api);
        var operationId = Guid.NewGuid();

        var response = await api.ClientFor(Receiver).SendAsync(Request(HttpMethod.Post,
            $"/api/v1/receiver/handling-units/{unitId}/receipt", "token-falso", ReceiptBody(operationId, d, 12m, 0m), operationId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal("Dispatched", (await db.Deliveries.SingleAsync(x => x.Id == d.DeliveryId)).Status);
        Assert.False(await db.Receipts.AnyAsync(r => r.DeliveryId == d.DeliveryId));
    }
}
