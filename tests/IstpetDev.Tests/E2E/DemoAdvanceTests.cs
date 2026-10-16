using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.E2E;

// #162 / FR-16: advance ejecuta preparación, despacho, recogida, entrega y recepción reales
// y es idempotente por operationId; ya no marca paradas con horas planificadas.
[Collection(PostgisCollection.Name)]
[Trait("Category", "E2E")]
public class DemoAdvanceTests(PostgisContainer postgres)
{
    [Fact]
    public async Task Advance_completes_stops_with_real_custody_and_is_idempotent()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);

        var proposal = await (await planner.PostAsJsonAsync("/api/v1/route-plans", new { includeConsolidation = true }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var routePlanId = proposal.GetProperty("routes")[0].GetProperty("routePlanId").GetGuid();

        string vehicleCode;
        using (var planScope = api.Services.CreateScope())
        {
            var planDb = planScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vehicleId = await planDb.RoutePlans.Where(p => p.Id == routePlanId).Select(p => p.VehicleId).SingleAsync();
            vehicleCode = await planDb.Vehicles.Where(v => v.Id == vehicleId).Select(v => v.Code).SingleAsync();
        }

        var publish = await planner.PostAsJsonAsync($"/api/v1/route-plans/{routePlanId}/publish",
            new { driverAssignments = new Dictionary<string, string> { [vehicleCode] = TestTokens.Driver } });
        Assert.True(publish.IsSuccessStatusCode, await publish.Content.ReadAsStringAsync());

        var operationId = BaseEntity.NewId();
        var first = await planner.PostAsJsonAsync($"/api/v1/demo/route-plans/{routePlanId}/advance", new { operationId, stops = 2 });
        var replay = await planner.PostAsJsonAsync($"/api/v1/demo/route-plans/{routePlanId}/advance", new { operationId, stops = 2 });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.Receipts.CountAsync());
        Assert.Equal(2, await db.RouteStops.CountAsync(s => s.RoutePlanId == routePlanId && s.Status == "Completed"));

        var completedStopIds = await db.RouteStops.Where(s => s.RoutePlanId == routePlanId && s.Status == "Completed").Select(s => s.Id).ToListAsync();
        Assert.Equal(2, await db.Deliveries.CountAsync(d => d.RouteStopId.HasValue && completedStopIds.Contains(d.RouteStopId.Value) && d.Status == "Received"));
        // Por parada: despacho, recogida, entrega y recepción (un evento de custodia por bulto).
        Assert.True(await db.CustodyEvents.CountAsync() >= 8, "se esperaban al menos 8 eventos de custodia reales");
    }
}
