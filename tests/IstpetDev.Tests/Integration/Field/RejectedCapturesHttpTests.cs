using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

// #114: la bandeja web de capturas rechazadas consultaba GET /api/v1/deliveries/rejected, que no existía (404).
[Trait("Category", "Integration")]
public class RejectedCapturesHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static async Task ReceiveAsync(ApiFactory api, FieldSeed.Dispatched d, decimal accepted, decimal rejected, string? reason)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var receiver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Receiver);
        var op = BaseEntity.NewId();
        await scope.ServiceProvider.GetRequiredService<IFieldService>().CreateReceiverReceiptAsync(d.UnitId!.Value, d.UnitToken!,
            new CreateReceiptRequest(op, 0, DateTime.UtcNow, [new ReceiptLineRequest(d.DeliveryLineId, d.LotId, accepted, rejected, "L", reason)]),
            receiver.Id, op.ToString());
    }

    private static async Task<FieldSeed.Dispatched> SeedDeliveredAsync(ApiFactory api, string suffix)
    {
        var token = $"token-{suffix}-{Guid.NewGuid():N}";
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m, suffix, withHandlingUnit: true, unitToken: token);
        await FieldSeed.DeliverUnitAsync(api, d);
        return d;
    }

    [Fact]
    public async Task Lists_receipts_with_rejected_lines_using_the_web_contract()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var rejected = await SeedDeliveredAsync(api, "RC-R");
        var clean = await SeedDeliveredAsync(api, "RC-C");
        await ReceiveAsync(api, rejected, 9m, 3m, "Daño");
        await ReceiveAsync(api, clean, 12m, 0m, null);

        using var planner = api.ClientFor(TestTokens.Planner);
        var response = await planner.GetAsync("/api/v1/deliveries/rejected");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(1, body.GetProperty("count").GetInt32());
        var item = body.GetProperty("items")[0];
        Assert.Equal(rejected.DeliveryId, item.GetProperty("deliveryId").GetGuid());
        Assert.Equal("Daño", item.GetProperty("reason").GetString());
        // #162: el rechazo lo registra la cuenta del Receptor (antes el conductor firmaba en su dispositivo).
        Assert.Equal("Receptor de prueba", item.GetProperty("driverName").GetString());
        var line = item.GetProperty("lines")[0];
        Assert.Equal(3m, line.GetProperty("quantityRejected").GetDecimal());
        Assert.Equal("SKU-RC-R", line.GetProperty("sku").GetString());

        var pointId = item.GetProperty("servicePointId").GetGuid();
        var filtered = JsonDocument.Parse(await (await planner.GetAsync($"/api/v1/deliveries/rejected?servicePointId={Guid.NewGuid()}")).Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(0, filtered.GetProperty("count").GetInt32());
        var same = JsonDocument.Parse(await (await planner.GetAsync($"/api/v1/deliveries/rejected?servicePointId={pointId}")).Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, same.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Auditor_reads_and_field_roles_are_forbidden()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        using (var driver = api.ClientFor(TestTokens.Driver))
            Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/deliveries/rejected")).StatusCode);

        await TestAccounts.SetRoleAsync(connection, TestTokens.Driver, Roles.Auditor);
        using var auditor = api.ClientFor(TestTokens.Driver);
        Assert.Equal(HttpStatusCode.OK, (await auditor.GetAsync("/api/v1/deliveries/rejected")).StatusCode);
    }
}
