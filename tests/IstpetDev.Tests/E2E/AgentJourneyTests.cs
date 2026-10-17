using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IstpetDev.Application.Agent;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IstpetDev.Tests.E2E;

[Trait("Category", "E2E")]
public sealed class AgentJourneyTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private const string ClientId = "journey-automation";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Watch_completed_trace_injection_and_forbidden_actions_preserve_policy()
    {
        var connection = postgres.NewDatabase();
        var trigger = new FakeN8n();
        await using var baseline = NewApi(connection, trigger);
        using var planner = baseline.ClientFor(TestTokens.Planner);
        await ResetAndSeedAsync(planner, connection);
        using var automation = Automation(baseline.CreateClient());
        trigger.Client = automation;
        var first = await StartAsync(planner, new(AgentRunKind.Watch, null));
        Assert.Equal(AgentRunStatus.Completed, first.Status);
        Assert.Equal(2, first.Decisions.Count);
        Assert.All(first.Decisions, d => Assert.Equal(ExplanationSource.Llm, d.Source));
        Assert.Equal(2, first.ToolCalls.Count);

        using var database = TestDb.Context(connection);
        var planId = await database.RoutePlans.Select(p => p.Id).FirstAsync();
        var forbidden = new[] { $"route-plans/{planId}/publish", $"route-plans/{planId}/revert",
            $"agent-decisions/{first.Decisions[0].AgentDecisionId}/approve", $"agent-decisions/{first.Decisions[0].AgentDecisionId}/reject", "autonomy/mode" };
        foreach (var path in forbidden)
        {
            using var request = new HttpRequestMessage(path == "autonomy/mode" ? HttpMethod.Put : HttpMethod.Post, "/api/v1/" + path);
            request.Headers.Add("X-Agent-Run-Id", first.RunId.ToString());
            request.Content = JsonContent.Create(new { mode = "AutoWithLimits", option = "late_service" });
            Assert.Equal(HttpStatusCode.Forbidden, (await automation.SendAsync(request)).StatusCode);
        }
        var traced = await planner.GetFromJsonAsync<AgentRunView>($"/api/v1/agent/runs/{first.RunId}", Json);
        Assert.Equal(5, traced!.ToolCalls.Count(t => t.Status == 403));
        Assert.All(await database.RoutePlans.AsNoTracking().ToListAsync(), p => Assert.Equal("Proposed", p.Status));

        await ResetAndSeedAsync(planner, connection, "demo-region-injection");
        var injected = await StartAsync(planner, new(AgentRunKind.Watch, null));
        Assert.Equal(AgentRunStatus.Completed, injected.Status);
        Assert.Equal(first.Decisions.Select(d => (d.VehicleCode, d.Outcome)), injected.Decisions.Select(d => (d.VehicleCode, d.Outcome)));
        Assert.All(injected.ToolCalls, t => Assert.InRange(t.Status, 200, 299));
        Assert.All(await database.RoutePlans.AsNoTracking().ToListAsync(), p => Assert.Equal("Proposed", p.Status));
    }

    [Theory]
    [InlineData("unreachable", "n8n_unreachable")]
    [InlineData("silent", "timeout")]
    [InlineData("partial_watch", "timeout")]
    [InlineData("agent_error", "agent_error")]
    [InlineData("invalid", "invalid_result")]
    [InlineData("disabled", "agent_disabled")]
    public async Task Fallback_proposes_routes_and_rejects_late_callback(string mode, string reason)
    {
        var connection = postgres.NewDatabase();
        var trigger = new FakeN8n { Mode = mode };
        await using var api = NewApi(connection, trigger, enabled: mode != "disabled");
        using var planner = api.ClientFor(TestTokens.Planner);
        await ResetAndSeedAsync(planner, connection);
        using var automation = Automation(api.CreateClient());
        trigger.Client = automation;
        var view = await StartAsync(planner, new(AgentRunKind.Watch, null));
        Assert.Equal(AgentRunStatus.Fallback, view.Status);
        Assert.Equal(reason, view.FallbackReason);
        Assert.Equal(2, view.Decisions.Count);
        Assert.All(view.Decisions, d =>
        {
            Assert.Equal(ExplanationSource.Template, d.Source);
            Assert.NotEmpty(d.DriverMessage);
            // El fallback acredita la misma evidencia determinista que el camino LLM.
            Assert.False(string.IsNullOrWhiteSpace(d.VehicleCode));
            Assert.False(string.IsNullOrWhiteSpace(d.OriginWarehouseCode));
            Assert.NotEmpty(d.OrderedStops!);
        });
        Assert.InRange(view.LatencyMs, 0, 11000);
        var payload = new AgentResult([new(view.Decisions[0].AgentDecisionId, "Texo tardío", "Mensaje tardío")], null, "fake-model", "v1", "late", null);
        Assert.Equal(HttpStatusCode.Conflict, (await automation.PostAsJsonAsync($"/api/v1/agent/runs/{view.RunId}/result", payload)).StatusCode);
        var late = await planner.GetFromJsonAsync<AgentRunView>($"/api/v1/agent/runs/{view.RunId}", Json);
        Assert.Equal(view.Decisions[0].Explanation, late!.Decisions[0].Explanation);
        Assert.Contains(late.ToolCalls, t => t.Event == "late_result_ignored");
    }

    [Theory]
    [InlineData("complete", null)]
    [InlineData("partial_incident", "timeout")]
    public async Task Automatic_incident_finishes_and_does_not_repeat_completed_replan(string mode, string? fallback)
    {
        var connection = postgres.NewDatabase();
        var trigger = new FakeN8n { Mode = mode };
        await using var api = NewApi(connection, trigger, auto: true);
        using var planner = api.ClientFor(TestTokens.Planner);
        await ResetAndSeedAsync(planner, connection);
        using var automation = Automation(api.CreateClient());
        trigger.Client = automation;
        var plans = await planner.PostAsJsonAsync("/api/v1/route-plans", new { includeConsolidation = true });
        using var planJson = JsonDocument.Parse(await plans.Content.ReadAsStringAsync());
        var ids = planJson.RootElement.GetProperty("routes").EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToArray();
        foreach (var id in ids) Assert.Equal(HttpStatusCode.OK, (await planner.PostAsJsonAsync($"/api/v1/route-plans/{id}/publish", new { })).StatusCode);
        var response = await planner.PostAsJsonAsync("/api/v1/incidents",
            Fixtures.IncidentPreset("demo-region", "cierre-norte"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var incident = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var runId = incident.RootElement.GetProperty("runId").GetGuid();
        AgentRunView? view = null;
        for (var i = 0; i < 100; i++)
        {
            view = await planner.GetFromJsonAsync<AgentRunView>($"/api/v1/agent/runs/{runId}", Json);
            if (view!.Status != AgentRunStatus.Running) break;
            await Task.Delay(100);
        }
        Assert.NotNull(view);
        Assert.Equal(fallback is null ? AgentRunStatus.Completed : AgentRunStatus.Fallback, view.Status);
        Assert.Equal(fallback, view.FallbackReason);
        Assert.NotEmpty(view.Decisions);
        Assert.All(view.Decisions, d =>
        {
            Assert.NotEmpty(d.DriverMessage);
            Assert.False(string.IsNullOrWhiteSpace(d.VehicleCode));
            Assert.False(string.IsNullOrWhiteSpace(d.OriginWarehouseCode));
            Assert.NotEmpty(d.OrderedStops!);
        });
        await using var db = TestDb.Context(connection);
        Assert.Equal(view.Decisions.Count, await db.RoutePlans.CountAsync(p => p.SupersedesRoutePlanId != null));
        Assert.All(view.Decisions, d => Assert.NotEmpty(d.DriverMessage));
    }

    // #300: el agente de incidente detecta y replanifica una ruta del asistente de pedidos (recogida en bodega + entregas).
    [Fact]
    public async Task Automatic_incident_replans_an_assistant_route_keeping_its_pickup()
    {
        var connection = postgres.NewDatabase();
        var trigger = new FakeN8n();
        await using var api = NewApi(connection, trigger, auto: true);
        using var planner = api.ClientFor(TestTokens.Planner);
        await ResetAndSeedAsync(planner, connection);
        using var automation = Automation(api.CreateClient());
        trigger.Client = automation;

        Guid warehouseId, vehicleId;
        var points = new Dictionary<string, (Guid Id, decimal Lat, decimal Lon)>();
        var orderIds = new List<Guid>();
        await using (var db = TestDb.Context(connection))
        {
            var warehouse = await db.Locations.SingleAsync(l => l.Kind == LocationKinds.Warehouse);
            warehouseId = warehouse.Id;
            vehicleId = await db.Vehicles.Where(v => v.Code == "VEH-01").Select(v => v.Id).SingleAsync();
            var productId = await db.Products.Where(p => p.Sku == "DES-01").Select(p => p.Id).SingleAsync();
            foreach (var point in await db.ServicePoints.Where(p => p.Code == "CLI-S09" || p.Code == "IND-S13").ToListAsync())
            {
                points[point.Code] = (point.Id, (decimal)point.Position!.Y, (decimal)point.Position.X);
                var order = new DeliveryOrder { OrderNumber = $"ORD-J-{Guid.NewGuid():N}"[..16], DestinationServicePointId = point.Id,
                                                RequiredDateUtc = DateTime.UtcNow.AddDays(1), SourceType = "Manual" };
                db.AddRange(order, new DeliveryOrderLine { DeliveryOrderId = order.Id, ProductId = productId, RequestedQuantity = 5m });
                orderIds.Add(order.Id);
            }
            await db.SaveChangesAsync();
        }
        object Stop(int sequence, string kind, Guid? pointId, Guid? locationId, string code, decimal lat, decimal lon) => new
        {
            sequence, kind, servicePointId = pointId, locationId, code, name = code, position = new { lat, lon },
            deltaWeight = 0m, deltaVolume = 0m, windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15
        };
        var confirm = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = $"WIZ-{Guid.NewGuid():N}"[..12], vehicleId, totalDistanceKm = 1m, totalDurationMinutes = 1m, totalCost = 0.01m, orderIds,
            stops = new[]
            {
                Stop(1, "Pickup", null, warehouseId, "BOD-SUR", -0.285m, -78.54m),
                Stop(2, "Delivery", points["CLI-S09"].Id, null, "CLI-S09", points["CLI-S09"].Lat, points["CLI-S09"].Lon),
                Stop(3, "Delivery", points["IND-S13"].Id, null, "IND-S13", points["IND-S13"].Lat, points["IND-S13"].Lon)
            }
        });
        Assert.True(confirm.StatusCode == HttpStatusCode.Created, await confirm.Content.ReadAsStringAsync());
        Guid planId;
        using (var confirmed = JsonDocument.Parse(await confirm.Content.ReadAsStringAsync()))
            planId = confirmed.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsJsonAsync($"/api/v1/route-plans/{planId}/publish", new { })).StatusCode);

        // Cierre sobre la Clínica Sur: la ruta del asistente (sin tramos guardados) aparece como afectada y el agente la replanifica.
        var response = await planner.PostAsJsonAsync("/api/v1/incidents", Fixtures.IncidentPreset("demo-region", "cierre-norte"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var incident = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(incident.RootElement.GetProperty("affectedRoutes").EnumerateArray(), r => r.GetProperty("routePlanId").GetGuid() == planId);
        var runId = incident.RootElement.GetProperty("runId").GetGuid();
        AgentRunView? view = null;
        for (var i = 0; i < 100; i++)
        {
            view = await planner.GetFromJsonAsync<AgentRunView>($"/api/v1/agent/runs/{runId}", Json);
            if (view!.Status != AgentRunStatus.Running) break;
            await Task.Delay(100);
        }
        Assert.NotNull(view);
        Assert.Equal(AgentRunStatus.Completed, view.Status);
        var decision = Assert.Single(view.Decisions);
        Assert.Equal("VEH-01", decision.VehicleCode);
        // La revisión empieza por la recogida en BOD-SUR y conserva IND-S13; CLI-S09 queda fuera por el cierre.
        Assert.Equal(new[] { "BOD-SUR", "IND-S13" }, decision.OrderedStops!.Select(s => s.PointCode));
        await using var check = TestDb.Context(connection);
        var revision = await check.RoutePlans.SingleAsync(p => p.SupersedesRoutePlanId == planId);
        var pickup = await check.RouteStops.SingleAsync(s => s.RoutePlanId == revision.Id && s.SequenceNumber == 1);
        Assert.Equal((RouteStopKind.Pickup, (Guid?)warehouseId), (pickup.Kind, pickup.LocationId));
    }

    private static ApiFactory NewApi(string connection, FakeN8n trigger, bool enabled = true, bool auto = false)
        => new JourneyApi(connection, trigger, new Dictionary<string, string?>
        {
            ["Agent:Enabled"] = enabled.ToString(), ["Agent:TimeoutSeconds"] = "3", ["Agent:AutoOnIncident"] = auto.ToString(),
            ["Authentication:AllowedClientIds:1"] = ClientId, ["Authentication:TechnicalClientIds:0"] = ClientId,
        });

    private static HttpClient Automation(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(ClientId, clientId: ClientId, scope: "istpetdev-api/automation", role: Roles.Automatizacion));
        return client;
    }

    private static async Task ResetAndSeedAsync(HttpClient planner, string connection, string fixture = "demo-region")
    {
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync($"/api/v1/demo/reset?fixture={fixture}", null)).StatusCode);
        await using var db = TestDb.Context(connection);
        if (!await db.Accounts.AnyAsync(a => a.Subject == ClientId))
        {
            db.Accounts.Add(new Account { Kind = "technical", Issuer = TestTokens.Issuer, Subject = ClientId, Role = Roles.Automatizacion, IsActive = true });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<AgentRunView> StartAsync(HttpClient client, StartRunRequest input)
    {
        var response = await client.PostAsJsonAsync("/api/v1/agent/runs", input);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AgentRunView>(Json))!;
    }

    private sealed class JourneyApi(string connection, FakeN8n trigger, IReadOnlyDictionary<string, string?> settings) : ApiFactory(connection, overrides: settings)
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => { services.RemoveAll<IN8nTrigger>(); services.AddSingleton<IN8nTrigger>(trigger); });
        }
    }

    private sealed class FakeN8n : IN8nTrigger
    {
        public string Mode { get; init; } = "complete";
        public HttpClient Client { get; set; } = null!;
        public Task<N8nTriggerResult> TriggerAsync(Guid runId, AgentRunKind kind, Guid? incidentId, CancellationToken ct)
        {
            if (Mode == "unreachable") return Task.FromResult(new N8nTriggerResult(TriggerOutcome.Error));
            if (Mode != "silent") _ = Task.Run(() => CompleteAsync(runId, kind, incidentId));
            return Task.FromResult(new N8nTriggerResult(TriggerOutcome.Success, "fake-execution"));
        }

        private async Task CompleteAsync(Guid runId, AgentRunKind kind, Guid? incidentId)
        {
            var ids = new List<Guid>();
            if (Mode == "invalid") ids.Add(Guid.NewGuid());
            else if (Mode != "agent_error" && kind == AgentRunKind.Watch)
            {
                using var risks = await ToolAsync(HttpMethod.Get, "/api/v1/risks", runId);
                using var routes = await ToolAsync(HttpMethod.Post, "/api/v1/route-plans", runId, new { includeConsolidation = true });
                ids.AddRange(routes.RootElement.GetProperty("routes").EnumerateArray().Select(p => p.GetProperty("agentDecisionId").GetGuid()));
                if (Mode == "partial_watch") return;
            }
            else if (Mode != "agent_error")
            {
                using var impact = await ToolAsync(HttpMethod.Get, $"/api/v1/incidents/{incidentId}", runId);
                foreach (var route in impact.RootElement.GetProperty("affectedRoutes").EnumerateArray())
                {
                    using var replan = await ToolAsync(HttpMethod.Post, $"/api/v1/route-plans/{route.GetProperty("routePlanId").GetGuid()}/replan", runId, new { incidentId });
                    ids.Add(replan.RootElement.GetProperty("agentDecisionId").GetGuid());
                }
                if (Mode == "partial_incident") return;
            }
            await Client.PostAsJsonAsync($"/api/v1/agent/runs/{runId}/result", new AgentResult(
                ids.Select(id => new DecisionText(id, "Propuesta calculada por el motor logístico.", "Consulta la ruta propuesta antes de continuar.")).ToArray(),
                "Resultado de prueba", "fake-model", "v1", "fake-execution", Mode == "agent_error" ? "simulated_failure" : null));
        }

        private async Task<JsonDocument> ToolAsync(HttpMethod method, string path, Guid runId, object? payload = null)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-Agent-Run-Id", runId.ToString());
            if (payload is not null) request.Content = JsonContent.Create(payload);
            using var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
