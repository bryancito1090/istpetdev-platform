using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Tests.Support;
using Xunit;

namespace IstpetDev.Tests.Integration.Imports;

// El sistema se adapta a los archivos como vienen: la web envía cada archivo con su nombre
// (por ejemplo "consumptions.csv") y el servidor reconoce la entidad sin renombrar nada.
[Trait("Category", "Integration")]
public class AdaptiveImportTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private static readonly string Dataset = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../docs/dataset"));

    private static async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(ApiFactory api, string dir, string[] files, bool dryRun = true,
        Func<string, string>? fieldName = null, string? profile = null)
    {
        using var client = api.ClientFor(TestTokens.Planner);
        using var form = new MultipartFormDataContent();
        foreach (var f in files)
            form.Add(new ByteArrayContent(await File.ReadAllBytesAsync(Path.Combine(dir, f))), (fieldName ?? (x => x))(f), f);
        if (profile is not null) form.Add(new StringContent(profile), "profile");
        var response = await client.PostAsync($"/api/v1/imports?dryRun={dryRun.ToString().ToLowerInvariant()}", form);
        return (response.StatusCode, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone());
    }

    [Fact]
    public async Task The_national_test_dataset_imports_as_the_web_sends_it()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var dir = Path.Combine(Dataset, "test/national-v1-seed-11");
        var csvs = Directory.GetFiles(dir, "*.csv").Select(Path.GetFileName).ToArray()!;

        var (status, body) = await PostAsync(api, dir, csvs!, dryRun: false);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.True(body.GetProperty("applied").GetBoolean());
        Assert.Equal(0, body.GetProperty("errorCount").GetInt32());
        int Rows(string entity) => body.GetProperty("entities").EnumerateArray()
            .Single(e => e.GetProperty("entity").GetString() == entity).GetProperty("rowsRead").GetInt32();
        Assert.Equal(80, Rows("service_points"));
        Assert.Equal(6, Rows("warehouses"));
        Assert.Equal(12, Rows("vehicles"));
        Assert.Equal(2240, Rows("consumptions"));
    }

    [Fact]
    public async Task A_file_with_an_unknown_name_is_recognized_by_its_columns()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var dir = Directory.CreateTempSubdirectory("istpet-adaptive-").FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "exportacion_erp.csv"),
                "Código;Nombre;Latitud;Longitud;Región;Criticidad\nHOS-9;Hospital de prueba;-0,18;-78,48;Pichincha;alta\n", Encoding.UTF8);

            var (status, body) = await PostAsync(api, dir, ["exportacion_erp.csv"]);

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Contains(body.GetProperty("entities").EnumerateArray(), e => e.GetProperty("entity").GetString() == "service_points");
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task A_dataset_that_is_not_operational_data_is_rejected_with_an_explanation()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var dir = Path.Combine(Dataset, "reales/acess-2026-09");

        var (status, body) = await PostAsync(api, dir, ["acess-2026-09-agregado.csv"]);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Contains("No se reconoce", body.GetProperty("detail").GetString());
    }
}
