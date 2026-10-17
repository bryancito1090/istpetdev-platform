using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Logistics;

// #246: la manifestación del centro histórico (preset de demo-quito-planificacion, factor 0,3) vuelve lentos
// los tramos dentro de su radio: la ETA sube sin cerrar la vía y, si un crítico sale de su ventana, se escala
// con las opciones de replanificación.
[Trait("Category", "Integration")]
public class SlowdownIncidentQuitoTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Manifestacion_slows_legs_raises_eta_without_closing_the_road_and_offers_options_for_late_critical()
    {
        await using var api = new ApiFactory(postgres.NewDatabase(), demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);
        var reset = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-quito-planificacion", null);
        Assert.True(reset.IsSuccessStatusCode, await reset.Content.ReadAsStringAsync());
        DateOnly today;
        using (var scope = api.Services.CreateScope())
            today = await scope.ServiceProvider.GetRequiredService<IOperationalClock>().TodayAsync();

        // Sin consolidación: replanificar una ruta con el mismo punto obligatorio y de consolidación falla
        // por un error previo de ReplanAsync (clave repetida), ajeno a este issue.
        var plan = await planner.PostAsJsonAsync("/api/v1/route-plans", new PlanRequest(today, null, false));
        Assert.Equal(HttpStatusCode.Created, plan.StatusCode);
        foreach (var route in (await plan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("routes").EnumerateArray())
        {
            // Una ruta sin stock suficiente en su bodega no se publica (409); las demás sí.
            await planner.PostAsJsonAsync($"/api/v1/route-plans/{route.GetProperty("routePlanId").GetGuid()}/publish", new { });
        }

        // El preset se envía tal cual: kind, severity, radio y slowdownFactor de incidentes.json.
        var preset = Fixtures.IncidentPreset("demo-quito-planificacion", "manifestacion-centro-historico");
        Assert.Equal(0.3m, preset.GetProperty("slowdownFactor").GetDecimal());
        var created = await planner.PostAsJsonAsync("/api/v1/incidents", preset);
        var createdBody = await created.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var impact = JsonDocument.Parse(createdBody).RootElement;
        Assert.Equal(0.3m, impact.GetProperty("slowdownFactor").GetDecimal());
        var incidentId = impact.GetProperty("incidentId").GetGuid();
        var affected = impact.GetProperty("affectedRoutes").EnumerateArray().ToList();
        Assert.NotEmpty(affected);

        // Un tipo de retraso parcial sin factor válido se rechaza sin crear el incidente.
        foreach (var invalid in new object[] { new { kind = "Manifestacion", lat = -0.21951m, lon = -78.51196m, radiusKm = 1.0m },
                     new { kind = "Accidente", lat = -0.21951m, lon = -78.51196m, radiusKm = 1.0m, factor = 1.5m } })
        {
            var rejected = await planner.PostAsJsonAsync("/api/v1/incidents", invalid);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Contains("invalid_slowdown_factor", await rejected.Content.ReadAsStringAsync());
        }

        // El factor queda guardado (al final de la descripción) y se vuelve a leer.
        var stored = await planner.GetFromJsonAsync<JsonElement>($"/api/v1/incidents/{incidentId}");
        Assert.Equal(0.3m, stored.GetProperty("slowdownFactor").GetDecimal());
        Assert.EndsWith("(velocidad al 30 %)", stored.GetProperty("description").GetString());

        // Cada tramo afectado queda ralentizado (minutos × 1 / 0,3), no bloqueado.
        using (var scope = api.Services.CreateScope())
        {
            var effect = (await scope.ServiceProvider.GetRequiredService<IIncidentEvaluator>().EvaluateAsync(incidentId)).Effect;
            Assert.Empty(effect.BlockedLegs);
            foreach (var leg in affected.SelectMany(r => r.GetProperty("affectedLegs").EnumerateArray()))
            {
                var key = (Point(leg.GetProperty("from")), Point(leg.GetProperty("to")));
                Assert.True(effect.SlowFactors.TryGetValue(key, out var multiplier), $"Tramo sin factor: {leg}");
                Assert.Equal(1m / 0.3m, multiplier);
            }
        }

        var etaRoseWithoutDropping = false;
        var lateCriticalWithOptions = false;
        var trace = new List<string>();
        foreach (var route in affected)
        {
            var routePlanId = route.GetProperty("routePlanId").GetGuid();
            var replan = await planner.PostAsJsonAsync($"/api/v1/route-plans/{routePlanId}/replan", new { incidentId });
            var replanBody = await replan.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, replan.StatusCode);
            var result = JsonDocument.Parse(replanBody).RootElement;
            var diff = result.GetProperty("diff");
            var outcome = result.GetProperty("decision").GetProperty("outcome").GetString();
            trace.Add($"{route.GetProperty("vehicleCode")}: {outcome} {diff.GetRawText()}");

            if (diff.GetProperty("droppedStops").GetArrayLength() == 0 &&
                diff.GetProperty("etaChangesMinutes").EnumerateObject().Any(e => e.Value.GetInt32() > 0))
                etaRoseWithoutDropping = true;

            if (outcome == "Escalated")
            {
                var options = result.GetProperty("options").EnumerateArray().ToList();
                Assert.Equal(new[] { "late_service", "prioritize_critical" }, options.Select(o => o.GetProperty("key").GetString()));
                var late = options[0];
                if (late.GetProperty("feasible").GetBoolean())
                {
                    var lateCodes = late.GetProperty("lateStops").EnumerateObject().Select(p => p.Name).ToList();
                    using var scope = api.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    if (await db.ServicePoints.AnyAsync(p => p.OrganizationId == DemoOrganization.Id &&
                            lateCodes.Contains(p.Code) && p.Criticality == "alta"))
                        lateCriticalWithOptions = true;
                }
            }
        }

        Assert.True(etaRoseWithoutDropping, "Ninguna ruta subió su ETA sin descartar paradas: " + string.Join(" | ", trace));
        Assert.True(lateCriticalWithOptions, "Ningún crítico fuera de ventana llegó con servicio tardío: " + string.Join(" | ", trace));
    }

    private static GeoPoint Point(JsonElement p) => new(p.GetProperty("lat").GetDecimal(), p.GetProperty("lon").GetDecimal());
}
