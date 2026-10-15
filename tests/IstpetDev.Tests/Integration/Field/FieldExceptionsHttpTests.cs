using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

// #116: novedades de recepción y pedidos de campo para la web del Planificador.
[Trait("Category", "Integration")]
public class FieldExceptionsHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static async Task ReceiveAsync(ApiFactory api, FieldSeed.Dispatched d, decimal accepted, decimal rejected,
        string? reason = null, decimal? remaining = null, decimal? next = null)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
        var op = BaseEntity.NewId();
        await field.CreateReceiptAsync(d.DeliveryId, new CreateReceiptRequest(op, 0, DateTime.UtcNow,
            [new ReceiptLineRequest(d.DeliveryLineId, d.LotId, accepted, rejected, "L", reason, remaining, next)]), driver.Id, op.ToString());
    }

    private static async Task<JsonElement> GetJsonAsync(System.Net.Http.HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    [Fact]
    public async Task Lists_only_receipts_with_rejections_or_shelf_adjustments_and_filters_by_point()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var rejected = await FieldSeed.DispatchedDeliveryAsync(api, 12m, "EXC-R");
        var adjusted = await FieldSeed.DispatchedDeliveryAsync(api, 12m, "EXC-A");
        var clean = await FieldSeed.DispatchedDeliveryAsync(api, 12m, "EXC-C");
        await ReceiveAsync(api, rejected, 8m, 4m, reason: "Daño en el empaque");
        await ReceiveAsync(api, adjusted, 12m, 0m, remaining: 3m); // saldo previo 0 → ajuste +3
        await ReceiveAsync(api, clean, 12m, 0m);

        using var planner = api.ClientFor(TestTokens.Planner);
        var body = await GetJsonAsync(planner, "/api/v1/receipts/exceptions");

        Assert.Equal(2, body.GetProperty("total").GetInt32());
        var items = body.GetProperty("items").EnumerateArray().ToList();
        var withRejection = items.Single(i => i.GetProperty("deliveryId").GetGuid() == rejected.DeliveryId);
        Assert.True(withRejection.GetProperty("hasRejections").GetBoolean());
        var rejectedLine = withRejection.GetProperty("lines")[0];
        Assert.Equal(4m, rejectedLine.GetProperty("quantityRejected").GetDecimal());
        Assert.Equal("Daño en el empaque", rejectedLine.GetProperty("rejectionReason").GetString());

        var withAdjustment = items.Single(i => i.GetProperty("deliveryId").GetGuid() == adjusted.DeliveryId);
        Assert.True(withAdjustment.GetProperty("hasAdjustments").GetBoolean());
        Assert.Equal(3m, withAdjustment.GetProperty("lines")[0].GetProperty("adjustmentQuantity").GetDecimal());
        Assert.DoesNotContain(items, i => i.GetProperty("deliveryId").GetGuid() == clean.DeliveryId);

        Guid pointId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pointId = await db.Deliveries.Where(d => d.Id == rejected.DeliveryId).Select(d => d.ServicePointId).SingleAsync();
        }
        var filtered = await GetJsonAsync(planner, $"/api/v1/receipts/exceptions?servicePointId={pointId}");
        Assert.Equal(1, filtered.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Field_roles_cannot_read_receipt_exceptions()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        using var driver = api.ClientFor(TestTokens.Driver);
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.GetAsync("/api/v1/receipts/exceptions")).StatusCode);
    }

    [Fact]
    public async Task Field_request_orders_are_filtered_and_cancellation_respects_status_and_version()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var d = await FieldSeed.DispatchedDeliveryAsync(api, 12m, "EXC-O");
        await ReceiveAsync(api, d, 12m, 0m, next: 20m); // crea un pedido FieldRequest

        Guid planned;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pointId = await db.Deliveries.Where(x => x.Id == d.DeliveryId).Select(x => x.ServicePointId).SingleAsync();
            var manual = new DeliveryOrder { OrderNumber = "ORD-MANUAL-116", DestinationServicePointId = pointId, SourceType = "Manual",
                                             RequiredDateUtc = DateTime.UtcNow.AddDays(2) };
            var plannedOrder = new DeliveryOrder { OrderNumber = "ORD-PLANNED-116", DestinationServicePointId = pointId, SourceType = "FieldRequest",
                                                   Status = DeliveryOrderStatus.Planned, RequiredDateUtc = DateTime.UtcNow.AddDays(2) };
            db.AddRange(manual, plannedOrder);
            await db.SaveChangesAsync();
            planned = plannedOrder.Id;
        }

        using var planner = api.ClientFor(TestTokens.Planner);
        var orders = (await GetJsonAsync(planner, "/api/v1/delivery-orders?sourceType=FieldRequest")).EnumerateArray().ToList();
        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal("FieldRequest", o.GetProperty("sourceType").GetString()));

        var pending = orders.Single(o => o.GetProperty("status").GetString() == DeliveryOrderStatus.Pending);
        var pendingId = pending.GetProperty("id").GetGuid();
        var version = pending.GetProperty("version").GetInt64();

        // Versión vieja: otro usuario lo cambió → 409 sin modificar.
        var stale = await planner.PatchAsync($"/api/v1/delivery-orders/{pendingId}/cancel?expectedVersion={version - 1}", null);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains("version_conflict", await stale.Content.ReadAsStringAsync());

        var cancel = await planner.PatchAsync($"/api/v1/delivery-orders/{pendingId}/cancel?expectedVersion={version}", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        // Reintento o doble clic: sigue cancelado, sin error.
        Assert.Equal(HttpStatusCode.OK, (await planner.PatchAsync($"/api/v1/delivery-orders/{pendingId}/cancel", null)).StatusCode);

        // Ya planificado: no se cancela desde aquí.
        var plannedCancel = await planner.PatchAsync($"/api/v1/delivery-orders/{planned}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, plannedCancel.StatusCode);
        Assert.Contains("order_not_pending", await plannedCancel.Content.ReadAsStringAsync());

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(DeliveryOrderStatus.Cancelled, await db.DeliveryOrders.Where(o => o.Id == pendingId).Select(o => o.Status).SingleAsync());
            Assert.Equal(DeliveryOrderStatus.Planned, await db.DeliveryOrders.Where(o => o.Id == planned).Select(o => o.Status).SingleAsync());
        }
    }
}
