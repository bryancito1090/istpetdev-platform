using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name)]
[Trait("Category", "Integration")]
public class ImportsApiTests(PostgisContainer postgres)
{
    private static MultipartFormDataContent Upload(string fixture, bool includeProfile = true)
    {
        var folder = Fixtures.Folder(fixture);
        var profileJson = File.ReadAllText(Path.Combine(folder, "mapping.json"));
        var content = new MultipartFormDataContent();
        if (includeProfile) content.Add(new StringContent(profileJson), "profile");
        foreach (var file in MappingProfile.Parse(profileJson).Files.Values)
            content.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(folder, file.File!))), "files", file.File!);
        return content;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    [Fact]
    public async Task Demo_reset_loads_the_fixture_on_a_brand_new_database()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);

        var reset = await planner.PostAsync("/api/v1/demo/reset", null);

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.True((await Json(reset)).GetProperty("applied").GetBoolean());
        var points = await Json(await planner.GetAsync("/api/v1/service-points"));
        Assert.Equal(6, points.GetArrayLength());
        var hospital = points.EnumerateArray().Single(p => p.GetProperty("code").GetString() == "HOS-001");
        Assert.Equal("hospital", hospital.GetProperty("type").GetString());
        Assert.Equal(-0.17, hospital.GetProperty("latitude").GetDouble(), 4);
    }

    [Fact]
    public async Task Demo_reset_is_hidden_when_demo_is_disabled_and_forbidden_for_drivers()
    {
        var connection = postgres.NewDatabase();
        await using (var db = await TestDb.CreateMigratedAsync(connection))
            await IstpetDev.Infrastructure.Persistence.AccountSeeder.SeedAsync(db, TestTokens.Issuer,
                [new IstpetDev.Application.Demo.DemoAccount { Subject = TestTokens.Planner, DisplayName = "Planificador", Role = IstpetDev.Domain.Common.Roles.Planificador }]);
        await using var disabled = new ApiFactory(connection, demoEnabled: false);
        Assert.Equal(HttpStatusCode.NotFound, (await disabled.ClientFor(TestTokens.Planner).PostAsync("/api/v1/demo/reset", null)).StatusCode);

        await using var enabled = new ApiFactory(postgres.NewDatabase());
        Assert.Equal(HttpStatusCode.Forbidden, (await enabled.ClientFor(TestTokens.Driver).PostAsync("/api/v1/demo/reset", null)).StatusCode);
    }

    [Fact]
    public async Task Import_defaults_to_dry_run_and_applies_when_asked()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);

        var dryRun = await planner.PostAsync("/api/v1/imports", Upload("ejemplo-externo"));
        Assert.Equal(HttpStatusCode.OK, dryRun.StatusCode);
        Assert.False((await Json(dryRun)).GetProperty("applied").GetBoolean());
        Assert.Equal(0, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());

        var applied = await planner.PostAsync("/api/v1/imports?dryRun=false", Upload("ejemplo-externo"));
        Assert.Equal(HttpStatusCode.OK, applied.StatusCode);
        Assert.Equal(2, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());
    }

    [Fact]
    public async Task Import_rejects_bad_requests_with_explanations()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);

        var badProfile = new MultipartFormDataContent { { new StringContent("""{ "files": { "pedidos": {} } }"""), "profile" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await planner.PostAsync("/api/v1/imports", badProfile)).StatusCode);

        var unknownFile = new MultipartFormDataContent { { new ByteArrayContent("a,b\n1,2\n"u8.ToArray()), "files", "misterio.csv" } };
        var unknown = await planner.PostAsync("/api/v1/imports", unknownFile);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Contains("misterio.csv", await unknown.Content.ReadAsStringAsync());

        var invalidRows = new MultipartFormDataContent { { new ByteArrayContent("code,name,lat,lon\nB1,Bodega,-200,-78\n"u8.ToArray()), "bodegas", "b.csv" } };
        var rejected = await planner.PostAsync("/api/v1/imports?dryRun=false", invalidRows);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        Assert.Equal(1, (await Json(rejected)).GetProperty("errorCount").GetInt32());
    }

    [Fact]
    public async Task Drivers_cannot_import()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var response = await api.ClientFor(TestTokens.Driver).PostAsync("/api/v1/imports", Upload("ejemplo-externo"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Demo_reset_keeps_completed_simulation_evidence_for_spec_005()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var organizationId = await db.Organizations.Select(x => x.Id).SingleAsync();
        var run = new SimulationRun
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, Seed = 11,
            DatasetHash = "fixture-hash", PolicyCode = "fixed7-v1",
            Status = "Completed", StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };
        db.SimulationRuns.Add(run);
        db.SimulationMetrics.Add(new SimulationMetric
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            SimulationRunId = run.Id, Code = "km_total",
            PolicyCode = "fixed7-v1", Numerator = 10m,
            Value = 10m, Unit = "km"
        });
        await db.SaveChangesAsync();

        Assert.Equal(HttpStatusCode.OK,
            (await planner.PostAsync("/api/v1/demo/reset", null)).StatusCode);
        Assert.Equal(1, await db.SimulationRuns.CountAsync(x => x.Id == run.Id));
        Assert.Equal(1, await db.SimulationMetrics.CountAsync(x => x.SimulationRunId == run.Id));
    }
}
