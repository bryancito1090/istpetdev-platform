using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Imports;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.E2E;

[Collection(PostgisCollection.Name)]
[Trait("Category", "E2E")]
public class DatasetJourneyTests(PostgisContainer postgres)
{
    private static MultipartFormDataContent Upload(string fixture)
    {
        var folder = Fixtures.Folder(fixture);
        var profileJson = File.ReadAllText(Path.Combine(folder, "mapping.json"));
        var content = new MultipartFormDataContent { { new StringContent(profileJson), "profile" } };
        foreach (var file in MappingProfile.Parse(profileJson).Files.Values)
            content.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(folder, file.File!))), "files", file.File!);
        return content;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<JsonElement>())!;
    }

    // E2E-01 + E2E-02: demo lista → dataset ajeno validado en seco → aplicado → reimportado sin duplicar.
    [Fact]
    public async Task Planner_resets_demo_then_adds_an_external_dataset_without_code_changes()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);

        var reset = await Json(await planner.PostAsync("/api/v1/demo/reset", null));
        Assert.True(reset.GetProperty("applied").GetBoolean());
        Assert.Equal(6, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        var dryRun = await Json(await planner.PostAsync("/api/v1/imports", Upload("ejemplo-externo")));
        Assert.Equal(0, dryRun.GetProperty("errorCount").GetInt32());
        Assert.Equal(6, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        var applied = await Json(await planner.PostAsync("/api/v1/imports?dryRun=false", Upload("ejemplo-externo")));
        Assert.True(applied.GetProperty("applied").GetBoolean());
        Assert.Equal(8, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        var again = await Json(await planner.PostAsync("/api/v1/imports?dryRun=false", Upload("ejemplo-externo")));
        Assert.All(again.GetProperty("entities").EnumerateArray(), e => Assert.Equal(0, e.GetProperty("created").GetInt32()));
        Assert.Equal(8, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        Assert.Equal(6, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());
    }
}
