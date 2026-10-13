using IstpetDev.Application.Imports;

namespace IstpetDev.Tests.Unit.Imports;

[Trait("Category", "Unit")]
public class MappingAndParsingTests
{
    private static TabularData Table(params string[] lines)
    {
        var headers = lines[0].Split(',');
        var rows = lines.Skip(1)
            .Select(line => (IReadOnlyDictionary<string, string>)headers.Zip(line.Split(',')).ToDictionary(p => p.First, p => p.Second))
            .ToList();
        return new TabularData(headers, rows);
    }

    private static (List<ParsedRow> Rows, ImportReport Report) Parse(ImportEntity entity, TabularData table, FileMapping? mapping = null)
    {
        var report = new ImportReport(Guid.NewGuid(), "prueba", dryRun: true);
        var resolution = ColumnResolver.Resolve(table.Headers, entity, mapping);
        var rows = RowParser.Parse(table, entity, resolution, mapping, MappingProfile.Auto().ToParseSettings(), report);
        return (rows, report);
    }

    [Fact]
    public void Profile_with_unknown_entity_or_field_is_rejected_with_all_errors()
    {
        var ex = Assert.Throws<MappingProfileException>(() => MappingProfile.Parse("""
            { "files": { "pedidos": { "file": "p.csv" }, "puntos": { "columns": { "latitude": "Lat" } } } }
            """));
        Assert.Equal(2, ex.Errors.Count);
        Assert.Contains(ex.Errors, e => e.Contains("pedidos"));
        Assert.Contains(ex.Errors, e => e.Contains("puntos.latitude"));
    }

    [Fact]
    public void Profile_finds_entities_by_alias_and_file_name()
    {
        var profile = MappingProfile.Parse("""{ "name": "x", "files": { "puntos": { "file": "Clientes.CSV" } } }""");
        Assert.NotNull(profile.For(ImportEntity.ServicePoints));
        Assert.Equal(ImportEntity.ServicePoints, profile.EntityForFile("clientes.csv"));
        Assert.Null(profile.EntityForFile("otro.csv"));
    }

    [Fact]
    public void Profile_date_formats_come_before_defaults()
    {
        var settings = MappingProfile.Parse("""{ "dateFormats": ["MM/dd/yyyy"] }""").ToParseSettings();
        Assert.Equal("MM/dd/yyyy", settings.DateFormats[0]);
        Assert.Contains("yyyy-MM-dd", settings.DateFormats);
    }

    [Fact]
    public void Resolver_matches_synonyms_and_reports_ignored_and_missing_columns()
    {
        var resolution = ColumnResolver.Resolve(["Código", "Razón Social", "Latitud", "Notas"], ImportEntity.ServicePoints, null);
        Assert.Equal("Código", resolution.Columns["code"]);
        Assert.Equal("Razón Social", resolution.Columns["name"]);
        Assert.Equal("Latitud", resolution.Columns["lat"]);
        Assert.Equal(new[] { "Notas" }, resolution.Ignored);
        Assert.Empty(resolution.MissingRequired);

        var missing = ColumnResolver.Resolve(["Nombre"], ImportEntity.ServicePoints, null);
        Assert.Equal(new[] { "code" }, missing.MissingRequired);
    }

    [Fact]
    public void Resolver_uses_declared_columns_and_constants_satisfy_required_fields()
    {
        var mapping = new FileMapping
        {
            Columns = new() { ["sku"] = "Cód. Producto", ["name"] = "Descripción" },
            Constants = new() { ["base_unit"] = "L" },
        };
        var resolution = ColumnResolver.Resolve(["Cod Producto", "Descripción"], ImportEntity.Products, mapping);
        Assert.Equal("Cod Producto", resolution.Columns["sku"]);
        Assert.Empty(resolution.MissingRequired);
    }

    [Fact]
    public void Parser_applies_defaults_constants_scale_and_code_normalization()
    {
        var mapping = new FileMapping { Constants = new() { ["period_days"] = "30" }, Scale = new() { ["quantity"] = 3.78541m } };
        var (rows, report) = Parse(ImportEntity.Consumptions, Table("point_code,sku,date,quantity", " hos-001 ,des-01,2026-09,95"), mapping);
        var row = Assert.Single(rows);
        Assert.Equal("HOS-001", row.Text("point_code"));
        Assert.Equal("DES-01", row.Text("sku"));
        Assert.Equal(95m * 3.78541m, row.Decimal("quantity"));
        Assert.Equal(30, row.Int("period_days"));
        Assert.False(report.HasErrors);
    }

    [Fact]
    public void Parser_counts_assumptions_for_missing_optional_values()
    {
        var (rows, report) = Parse(ImportEntity.ServicePoints, Table("code,name", "P1,Uno", "P2,Dos"));
        Assert.Equal(2, rows.Count);
        var assumptions = report.Entity(ImportEntity.ServicePoints).Assumptions;
        Assert.Equal(2, assumptions["criticality"].Rows);
        Assert.Equal(2, assumptions["lat"].Rows);
        Assert.Equal("normal", rows[0].Text("criticality"));
    }

    [Fact]
    public void Parser_reports_row_numbers_and_skips_invalid_rows()
    {
        var (rows, report) = Parse(ImportEntity.Warehouses, Table("code,name,lat,lon", "B1,Bodega,-0.2,-78.5", "B2,,-0.2,-78.5", "B3,Mala,-95,-78.5"));
        Assert.Single(rows);
        Assert.Equal(2, report.ErrorCount);
        Assert.Contains(report.Issues, i => i.Row == 3 && i.Field == "name" && i.Code == "required");
        Assert.Contains(report.Issues, i => i.Row == 4 && i.Field == "lat" && i.Code == "out_of_range");
        Assert.Equal(2, report.Entity(ImportEntity.Warehouses).Skipped);
    }

    [Fact]
    public void Parser_rejects_incomplete_coordinates()
    {
        var (rows, report) = Parse(ImportEntity.ServicePoints, Table("code,name,lat,lon", "P1,Uno,-0.2,"));
        Assert.Empty(rows);
        Assert.Equal(1, report.ErrorCount);
    }

    [Fact]
    public void Report_keeps_counting_after_the_issue_cap()
    {
        var report = new ImportReport(Guid.NewGuid(), "prueba", dryRun: false);
        for (var i = 0; i < 1200; i++) report.Error(ImportEntity.Stock, i + 2, "quantity", "negative", "negativo");
        Assert.Equal(ImportReport.MaxIssues, report.Issues.Count);
        Assert.Equal(1200, report.ErrorCount);
        Assert.Equal(700, report.OmittedIssues);
        Assert.True(report.HasErrors);
    }

    [Fact]
    public void Unique_headers_name_blanks_and_reject_duplicates()
        {
        Assert.Equal(new[] { "a", "columna_2" }, TabularData.UniqueHeaders(new[] { "a", " " }));
        Assert.Throws<UnsupportedFileException>(() => TabularData.UniqueHeaders(new[] { "a", "a" }));
    }
}
