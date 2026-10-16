using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Tests.Integration.Imports;

[Trait("Category", "Integration")]
public sealed class FeasibilityReportTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private const string TechnicalClientId = "n8n-dataset-review-client";

    private static MultipartFormDataContent Upload(params (string Field, string File, string Csv)[] files)
    {
        var content = new MultipartFormDataContent();
        foreach (var (field, file, csv) in files)
            content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), field, file);
        return content;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    [Fact]
    public async Task Dry_run_reports_status_and_unroutable_points_without_writing()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        using var form = Upload(("service_points", "puntos.csv",
            "code,name,lat,lon\nP-1,Punto sin gps,,\nP-2,Punto con gps,-0.2,-78.5\n"));

        using var response = await planner.PostAsync("/api/v1/imports?dryRun=true&allowPartial=false", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.True(body.GetProperty("dryRun").GetBoolean());
        Assert.Equal("DryRun", body.GetProperty("status").GetString());
        Assert.False(body.GetProperty("applied").GetBoolean());
        var feasibility = body.GetProperty("feasibility");
        Assert.Equal("ViableWithLimitations", feasibility.GetProperty("verdict").GetString());
        var point = Assert.Single(feasibility.GetProperty("unroutablePoints").EnumerateArray());
        Assert.Equal("P-1", point.GetProperty("code").GetString());
        Assert.Equal("Punto sin gps", point.GetProperty("name").GetString());
        Assert.Equal(2, point.GetProperty("row").GetInt32());
        Assert.Equal(0, (await Json(await planner.GetAsync("/api/v1/service-points"))).GetArrayLength());
    }

    [Fact]
    public async Task Complete_dataset_with_fleet_and_covered_demand_is_viable()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        using var form = Upload(
            ("warehouses", "bodegas.csv", "code,name,lat,lon\nW-1,Bodega,-0.22,-78.51\n"),
            ("products", "productos.csv", "sku,name,base_unit,weight_per_unit,volume_per_unit\nSKU-1,Producto,UNIDAD,2.5,0.8\n"),
            ("vehicles", "vehiculos.csv", "code,depot_code,max_weight,max_volume\nV-1,W-1,1000,10\n"),
            ("service_points", "puntos.csv", "code,name,lat,lon\nP-1,Punto,-0.2,-78.5\n"),
            ("stock", "stock.csv", "location_code,sku,quantity\nW-1,SKU-1,100\n"),
            ("consumptions", "consumos.csv", "point_code,sku,date,quantity\nP-1,SKU-1,2026-10-01,10\n"));

        using var response = await planner.PostAsync("/api/v1/imports", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.Equal("DryRun", body.GetProperty("status").GetString());
        Assert.Equal("Viable", body.GetProperty("feasibility").GetProperty("verdict").GetString());
        Assert.Empty(body.GetProperty("feasibility").GetProperty("unroutablePoints").EnumerateArray());
        Assert.Empty(body.GetProperty("feasibility").GetProperty("warehousesWithoutFleet").EnumerateArray());
        Assert.Empty(body.GetProperty("feasibility").GetProperty("productsWithoutCapacity").EnumerateArray());
        Assert.Empty(body.GetProperty("feasibility").GetProperty("stockShortages").EnumerateArray());
    }

    [Fact]
    public async Task Demand_without_stock_is_inviable_with_deficit_detail()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        using var form = Upload(
            ("products", "productos.csv", "sku,name,base_unit,weight_per_unit,volume_per_unit\nSKU-1,Producto,UNIDAD,2.5,0.8\n"),
            ("service_points", "puntos.csv", "code,name,lat,lon\nP-1,Punto,-0.2,-78.5\n"),
            ("consumptions", "consumos.csv", "point_code,sku,date,quantity\nP-1,SKU-1,2026-10-01,100\n"));

        using var response = await planner.PostAsync("/api/v1/imports", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var feasibility = (await Json(response)).GetProperty("feasibility");
        Assert.Equal("Inviable", feasibility.GetProperty("verdict").GetString());
        var shortage = Assert.Single(feasibility.GetProperty("stockShortages").EnumerateArray());
        Assert.Equal("SKU-1", shortage.GetProperty("sku").GetString());
        Assert.Equal(100m, shortage.GetProperty("demanded").GetDecimal());
        Assert.Equal(0m, shortage.GetProperty("available").GetDecimal());
    }

    [Fact]
    public async Task Warehouses_without_fleet_and_products_without_capacity_are_listed()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var planner = api.ClientFor(TestTokens.Planner);
        using var form = Upload(
            ("warehouses", "bodegas.csv", "code,name,lat,lon\nW-1,Bodega,-0.22,-78.51\n"),
            ("products", "productos.csv", "sku,name,base_unit\nSKU-1,Producto,UNIDAD\n"));

        using var response = await planner.PostAsync("/api/v1/imports", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var feasibility = (await Json(response)).GetProperty("feasibility");
        Assert.Equal("ViableWithLimitations", feasibility.GetProperty("verdict").GetString());
        var warehouse = Assert.Single(feasibility.GetProperty("warehousesWithoutFleet").EnumerateArray());
        Assert.Equal("W-1", warehouse.GetProperty("code").GetString());
        var product = Assert.Single(feasibility.GetProperty("productsWithoutCapacity").EnumerateArray());
        Assert.Equal("SKU-1", product.GetProperty("sku").GetString());
        Assert.Equal("both", product.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Row_errors_reject_with_status_and_full_issue_and_keep_database_untouched()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection);
        var planner = api.ClientFor(TestTokens.Planner);
        using var form = Upload(("warehouses", "bodegas.csv",
            "code,name,lat,lon\nW-1,Bodega,-0.2,-78.5\nW-2,Bodega mala,-200,-78.5\n"));

        using var response = await planner.PostAsync("/api/v1/imports?dryRun=false", form);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await Json(response);
        Assert.Equal("Rejected", body.GetProperty("status").GetString());
        Assert.False(body.GetProperty("applied").GetBoolean());
        var issue = Assert.Single(body.GetProperty("issues").EnumerateArray());
        Assert.Equal("warehouses", issue.GetProperty("entity").GetString());
        Assert.Equal("bodegas.csv", issue.GetProperty("file").GetString());
        Assert.Equal(3, issue.GetProperty("row").GetInt32());
        Assert.Equal("lat", issue.GetProperty("field").GetString());
        Assert.Equal("out_of_range", issue.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(issue.GetProperty("message").GetString()));
        await using var db = TestDb.Context(connection);
        Assert.Equal(0, await db.Locations.CountAsync());
        Assert.Equal("Rejected", (await db.ImportBatches.SingleAsync()).Status);
    }

    [Fact]
    public async Task Automation_token_can_only_run_import_dry_runs()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, overrides: new Dictionary<string, string?>
        {
            ["Authentication:AllowedClientIds:1"] = TechnicalClientId,
            ["Authentication:TechnicalClientIds:0"] = TechnicalClientId,
            ["Authentication:TechnicalScope"] = "istpetdev-api/automation"
        });
        using var client = api.ClientFor(null);
        await using (var db = TestDb.Context(connection))
        {
            db.Add(new Account { Kind = "technical", Issuer = TestTokens.Issuer, Subject = TechnicalClientId, Role = Roles.Automatizacion, IsActive = true });
            await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            TestTokens.Create(TechnicalClientId, clientId: TechnicalClientId, scope: "istpetdev-api/automation", role: Roles.Automatizacion));
        const string csv = "sku,name,base_unit\nS,Producto,L\n";

        using var dryRun = await client.PostAsync("/api/v1/imports", Upload(("products", "p.csv", csv)));
        Assert.Equal(HttpStatusCode.OK, dryRun.StatusCode);
        Assert.Equal("DryRun", (await Json(dryRun)).GetProperty("status").GetString());

        using var apply = await client.PostAsync("/api/v1/imports?dryRun=false", Upload(("products", "p.csv", csv)));
        Assert.Equal(HttpStatusCode.Forbidden, apply.StatusCode);
        using var partial = await client.PostAsync("/api/v1/imports?allowPartial=true", Upload(("products", "p.csv", csv)));
        Assert.Equal(HttpStatusCode.Forbidden, partial.StatusCode);
        await using var check = TestDb.Context(connection);
        Assert.Equal(0, await check.Products.CountAsync());
    }
}
