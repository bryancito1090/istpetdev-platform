using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IstpetDev.Tests.Support;
using Xunit;

namespace IstpetDev.Tests.E2E;

// Cualquier dataset del repositorio, subido como lo hace la web, debe llevar a alertas, rutas y agente
// sin intervención: docs/dataset (dataset nacional) y data/fixtures (demo-base, demo-region, ejemplo-externo).
[Trait("Category", "E2E")]
public class DatasetsJourneyTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static readonly string Repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));

    private sealed record Plan(string Code, decimal Km, int Stops);
    private sealed record Journey(JsonElement Risks, JsonElement Routes, JsonElement Agent, List<Plan> Plans);

    private static async Task<Journey> RunAsync(ApiFactory api, string dir)
    {
        using var client = api.ClientFor(TestTokens.Planner);
        using var form = new MultipartFormDataContent();
        foreach (var file in Directory.GetFiles(dir).Where(f => f.EndsWith(".csv") || f.EndsWith(".xlsx")))
            form.Add(new ByteArrayContent(await File.ReadAllBytesAsync(file)), Path.GetFileName(file), Path.GetFileName(file));
        var mapping = Path.Combine(dir, "mapping.json");
        if (File.Exists(mapping)) form.Add(new StringContent(await File.ReadAllTextAsync(mapping)), "profile");

        var import = await client.PostAsync("/api/v1/imports?dryRun=false", form);
        Assert.True(import.StatusCode == HttpStatusCode.OK, await import.Content.ReadAsStringAsync());

        async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected)
        {
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"{response.RequestMessage?.RequestUri}: {(int)response.StatusCode} {text}");
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        var risks = await Json(await client.GetAsync("/api/v1/risks?refresh=true"), HttpStatusCode.OK);
        var routes = await Json(await client.PostAsync("/api/v1/route-plans", new StringContent("{}", Encoding.UTF8, "application/json")), HttpStatusCode.Created);
        var agent = await Json(await client.PostAsync("/api/v1/agent/runs", new StringContent("{\"kind\":\"Watch\"}", Encoding.UTF8, "application/json")), HttpStatusCode.OK);

        var plans = new List<Plan>();
        var list = await Json(await client.GetAsync("/api/v1/route-plans"), HttpStatusCode.OK);
        foreach (var item in list.GetProperty("items").EnumerateArray())
        {
            var detail = await Json(await client.GetAsync($"/api/v1/route-plans/{item.GetProperty("id").GetString()}"), HttpStatusCode.OK);
            plans.Add(new Plan(item.GetProperty("code").GetString()!, item.GetProperty("km").GetDecimal(), detail.GetProperty("stops").GetArrayLength()));
        }
        return new Journey(risks, routes, agent, plans);
    }

    private static void AssertAgentAnswered(JsonElement agent)
    {
        // Sin n8n en la prueba, el agente responde por el camino determinista (Fallback) con decisiones.
        Assert.Contains(agent.GetProperty("status").GetString(), new[] { "Completed", "Fallback" });
        Assert.True(agent.GetProperty("decisions").GetArrayLength() > 0);
    }

    [Theory]
    [InlineData("data/fixtures/demo-base", 1)]
    [InlineData("data/fixtures/demo-region", 2)]
    [InlineData("data/fixtures/demo-region-injection", 2)]
    public async Task Single_warehouse_fixtures_reach_alerts_routes_and_agent(string relative, int expectedPlans)
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var journey = await RunAsync(api, Path.Combine(Repo, relative));

        Assert.True(journey.Risks.GetProperty("items").GetArrayLength() > 0);
        Assert.Equal(expectedPlans, journey.Plans.Count);
        Assert.All(journey.Plans, p => Assert.True(p.Stops > 0, $"{p.Code} sin paradas"));
        AssertAgentAnswered(journey.Agent);
    }

    [Fact]
    public async Task National_dataset_routes_each_vehicle_from_its_own_warehouse()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var journey = await RunAsync(api, Path.Combine(Repo, "docs/dataset/test/national-v1-seed-11"));

        // Seis bodegas: antes todos los vehículos salían de la primera (Ambato), cuatro planes quedaban
        // vacíos (0 km) y el resto recorría ~330 km con 3 a 5 paradas.
        Assert.All(journey.Plans, p => Assert.True(p.Stops > 0, $"{p.Code} sin paradas"));
        Assert.All(journey.Plans, p => Assert.True(p.Km < 250m, $"{p.Code} recorre {p.Km} km"));
        var depots = journey.Plans.Select(p => Regex.Match(p.Code, @"VEH-([A-Z]{2})-").Groups[1].Value).Distinct().ToList();
        Assert.Equal(6, depots.Count);
        Assert.Equal(80, journey.Plans.Sum(p => p.Stops));
        AssertAgentAnswered(journey.Agent);
    }

    [Fact]
    public async Task Dataset_without_warehouses_explains_why_there_are_no_routes()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var journey = await RunAsync(api, Path.Combine(Repo, "data/fixtures/ejemplo-externo"));

        Assert.True(journey.Risks.GetProperty("items").GetArrayLength() > 0);
        Assert.Empty(journey.Plans);
        var reasons = journey.Routes.GetProperty("unserved").EnumerateArray().Select(u => u.GetProperty("reason").GetString()).Distinct();
        Assert.Contains("warehouse_stock", reasons);
        AssertAgentAnswered(journey.Agent);
    }

    [Fact]
    public async Task Coordinates_with_more_than_five_decimals_still_produce_routes()
    {
        // Las claves de la matriz de distancias van redondeadas a 5 decimales; antes, coordenadas con más
        // decimales (exportaciones GPS) dejaban todos los puntos como «unreachable».
        var dir = Directory.CreateTempSubdirectory("istpet-gps-").FullName;
        try
        {
            foreach (var file in Directory.GetFiles(Path.Combine(Repo, "data/fixtures/demo-base")))
                File.Copy(file, Path.Combine(dir, Path.GetFileName(file)));
            foreach (var name in new[] { "puntos.csv", "bodegas.csv" })
            {
                var path = Path.Combine(dir, name);
                var text = await File.ReadAllTextAsync(path);
                await File.WriteAllTextAsync(path, Regex.Replace(text, @"(-\d+\.\d{4})(?=[,\r\n])", "${1}123"));
            }

            await using var api = new ApiFactory(postgres.NewDatabase());
            var journey = await RunAsync(api, dir);
            Assert.NotEmpty(journey.Plans);
            Assert.All(journey.Plans, p => Assert.True(p.Stops > 0, $"{p.Code} sin paradas"));
        }
        finally { Directory.Delete(dir, true); }
    }
}
