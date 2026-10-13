using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Application.Imports;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;

namespace IstpetDev.Tests.Integration.Api;

[Collection(PostgisCollection.Name), Trait("Category", "Integration")]
public sealed class ImportBoundaryTests(PostgisContainer postgres)
{
    private static MultipartFormDataContent Upload(string csv, string entity = "products", string? profile = null)
    {
        var content = new MultipartFormDataContent { { new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), entity, "data.csv" } };
        if (profile is not null) content.Add(new StringContent(profile), "profile");
        return content;
    }

    [Fact]
    public async Task Dry_run_reports_row_errors_with_422_and_partial_requires_valid_rows()
    {
        await using var api = new ApiFactory(postgres.NewDatabase()); using var client = api.ClientFor(TestTokens.Planner);
        const string invalid = "sku,name,base_unit\nS,,L\n";
        foreach (var url in new[] { "/api/v1/imports", "/api/v1/imports?dryRun=false&allowPartial=true" })
        {
            using var form = Upload(invalid); var response = await client.PostAsync(url, form);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("applied").GetBoolean()); Assert.Equal(1, body.GetProperty("errorCount").GetInt32());
        }
    }

    [Theory]
    [InlineData("{\"files\":{\"products\":{\"constants\":{\"organization_id\":\"other\"}}}}")]
    [InlineData("{\"actorAccountId\":\"fake\"}")]
    [InlineData("{\"files\":{\"stock\":{\"file\":\"data.csv\"}}}")]
    public async Task Server_fields_and_conflicting_entity_mapping_are_rejected(string profile)
    {
        await using var api = new ApiFactory(postgres.NewDatabase()); using var client = api.ClientFor(TestTokens.Planner);
        using var form = Upload("sku,name,base_unit\nS,Producto,L\n", profile: profile);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/imports", form)).StatusCode);
    }

    [Fact]
    public async Task Duplicate_entity_and_profile_file_are_rejected_before_import()
    {
        await using var api = new ApiFactory(postgres.NewDatabase()); using var client = api.ClientFor(TestTokens.Planner);
        using var duplicate = Upload("sku,name,base_unit\nS,Producto,L\n");
        duplicate.Add(new ByteArrayContent("sku,name,base_unit\nB,Otro,L\n"u8.ToArray()), "productos", "other.csv");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/imports", duplicate)).StatusCode);
        using var profileFile = Upload("sku,name,base_unit\nS,Producto,L\n");
        profileFile.Add(new ByteArrayContent("{}"u8.ToArray()), "profile", "mapping.json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/imports", profileFile)).StatusCode);
    }

    [Fact]
    public async Task File_limit_and_content_validation_are_enforced()
    {
        await using var api = new ApiFactory(postgres.NewDatabase()); using var client = api.ClientFor(TestTokens.Planner);
        using var large = new MultipartFormDataContent { { new ByteArrayContent(new byte[ImportLimits.FileBytes + 1]), "products", "large.csv" } };
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.PostAsync("/api/v1/imports", large)).StatusCode);
        using var fake = new MultipartFormDataContent { { new ByteArrayContent("plain text"u8.ToArray()), "products", "fake.xlsx" } };
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await client.PostAsync("/api/v1/imports", fake)).StatusCode);
    }

    [Fact]
    public async Task Import_budget_is_partitioned_by_registered_account_and_sets_retry_after()
    {
        var connection = postgres.NewDatabase(); await using var api = new ApiFactory(connection); using var client = api.ClientFor(TestTokens.Planner);
        for (var i = 0; i < 20; i++) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/imports", null)).StatusCode);
        var limited = await client.PostAsync("/api/v1/imports", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.NotNull(limited.Headers.RetryAfter);
        await using (var db = TestDb.Context(connection))
        { db.Add(new Account { Issuer = TestTokens.Issuer, Subject = "second-planner", Role = Roles.Planificador, IsActive = true }); await db.SaveChangesAsync(); }
        using var other = api.ClientFor("second-planner");
        Assert.Equal(HttpStatusCode.BadRequest, (await other.PostAsync("/api/v1/imports", null)).StatusCode);
    }
}
