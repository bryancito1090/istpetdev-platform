using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

// #197: el asistente de pedidos ordena entregas por prioridad y usa la ventana del punto.
[Trait("Category", "Integration")]
public class RouteSuggestPriorityTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Suggest_places_critical_order_before_normal_and_uses_point_windows()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        Guid nearId, farId, productId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // CLI-002 queda más cerca de BOD-UIO que HOS-001 (data/fixtures/demo-base).
            var near = await db.ServicePoints.SingleAsync(p => p.OrganizationId == DemoOrganization.Id && p.Code == "CLI-002");
            var far = await db.ServicePoints.SingleAsync(p => p.OrganizationId == DemoOrganization.Id && p.Code == "HOS-001");
            productId = (await db.Products.SingleAsync(p => p.OrganizationId == DemoOrganization.Id && p.Sku == "DES-01")).Id;

            // Ventana propia en un punto y sin ventana en el otro para probar el respaldo 08:00–17:00.
            near.WindowStartLocal = new TimeOnly(9, 15);
            near.WindowEndLocal = new TimeOnly(11, 45);
            near.ServiceMinutes = 25;
            far.WindowStartLocal = null;
            far.WindowEndLocal = null;
            far.ServiceMinutes = null;
            await db.SaveChangesAsync();
            nearId = near.Id;
            farId = far.Id;
        }

        var requiredDate = DateTime.UtcNow.Date.AddDays(1).AddHours(15);
        var normalOrderId = await CreateOrderAsync(planner, nearId, productId, "Normal", requiredDate);
        var farOrderId = await CreateOrderAsync(planner, farId, productId, "Normal", requiredDate);

        // Misma prioridad y día: manda el vecino más cercano.
        var sameLevel = await DeliveriesAsync(planner, normalOrderId, farOrderId);
        Assert.Equal(new[] { "CLI-002", "HOS-001" }, sameLevel.Select(s => s.GetProperty("code").GetString()).ToArray());

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = await db.DeliveryOrders.SingleAsync(o => o.Id == farOrderId);
            order.Priority = "Critical";
            await db.SaveChangesAsync();
        }

        // Critical va antes que Normal aunque esté más lejos.
        var deliveries = await DeliveriesAsync(planner, normalOrderId, farOrderId);
        Assert.Equal(new[] { "HOS-001", "CLI-002" }, deliveries.Select(s => s.GetProperty("code").GetString()).ToArray());

        var critical = deliveries[0];
        var normal = deliveries[1];
        Assert.True(critical.GetProperty("sequence").GetInt32() < normal.GetProperty("sequence").GetInt32());

        // Ventanas y servicio salen del punto; sin datos en el punto se usa el respaldo.
        Assert.StartsWith("09:15", normal.GetProperty("windowStart").GetString());
        Assert.StartsWith("11:45", normal.GetProperty("windowEnd").GetString());
        Assert.Equal(25, normal.GetProperty("serviceMinutes").GetInt32());
        Assert.StartsWith("08:00", critical.GetProperty("windowStart").GetString());
        Assert.StartsWith("17:00", critical.GetProperty("windowEnd").GetString());
        Assert.Equal(15, critical.GetProperty("serviceMinutes").GetInt32());
    }

    private static async Task<Guid> CreateOrderAsync(HttpClient client, Guid servicePointId, Guid productId, string priority, DateTime requiredDateUtc)
    {
        var response = await client.PostAsJsonAsync("/api/v1/delivery-orders", new
        {
            destinationServicePointId = servicePointId,
            requiredDateUtc,
            priority,
            lines = new[] { new { productId, quantity = 5m } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        return doc.GetProperty("id").GetGuid();
    }

    // Entregas de la opción recomendada, en el orden de la secuencia sugerida.
    private static async Task<JsonElement[]> DeliveriesAsync(HttpClient client, params Guid[] orderIds)
    {
        var response = await client.PostAsJsonAsync("/api/v1/route-plans/suggest", new { orderIds });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        var option = doc.GetProperty("options").EnumerateArray().First(o => o.GetProperty("isRecommended").GetBoolean());
        var stops = option.GetProperty("stops").EnumerateArray().ToArray();

        // Las recogidas siguen antes que las entregas.
        var firstDelivery = Array.FindIndex(stops, s => s.GetProperty("kind").GetString() == RouteStopKind.Delivery);
        Assert.True(firstDelivery > 0);
        Assert.All(stops[..firstDelivery], s => Assert.Equal(RouteStopKind.Pickup, s.GetProperty("kind").GetString()));

        return stops[firstDelivery..].OrderBy(s => s.GetProperty("sequence").GetInt32()).ToArray();
    }
}
