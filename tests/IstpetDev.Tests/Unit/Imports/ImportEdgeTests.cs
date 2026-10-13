using System.Text;
using ClosedXML.Excel;
using IstpetDev.Application.Imports;
using IstpetDev.Infrastructure.Imports;

namespace IstpetDev.Tests.Unit.Imports;

[Trait("Category", "Unit")]
public sealed class ImportEdgeTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("{\"files\":null}")]
    [InlineData("{\"extra\":true}")]
    [InlineData("{\"files\":{\"stock\":{\"scale\":{\"quantity\":0}}}}")]
    [InlineData("{\"files\":{\"stock\":{\"scale\":{\"quantity\":-2}}}}")]
    [InlineData("{\"files\":{\"stock\":{\"scale\":{\"sku\":2}}}}")]
    [InlineData("{\"files\":{\"stock\":{\"columns\":{\"sku\":null}}}}")]
    [InlineData("{\"files\":{\"stock\":{\"file\":\"../stock.csv\"}}}")]
    [InlineData("{\"files\":{\"stock\":{},\"inventario\":{}}}")]
    public void Invalid_profiles_are_rejected(string json) => Assert.Throws<MappingProfileException>(() => MappingProfile.Parse(json));

    [Theory]
    [InlineData("1,2,3")]
    [InlineData("12.34.56")]
    [InlineData("0.45", ",")]
    public void Invalid_grouping_does_not_silently_change_a_quantity(string text, string? separator = null)
        => Assert.False(ValueParser.TryDecimal(text, separator, out _));

    [Fact]
    public void Equivalent_headers_and_ambiguous_synonyms_require_an_explicit_choice()
    {
        Assert.Throws<UnsupportedFileException>(() => TabularData.UniqueHeaders(["Código", "codigo"]));
        Assert.Throws<UnsupportedFileException>(() => TabularData.UniqueHeaders(["a", "a"]));
        Assert.Throws<MappingProfileException>(() => ColumnResolver.Resolve(["code", "codigo", "name"], ImportEntity.ServicePoints, null));
        Assert.Throws<MappingProfileException>(() => ColumnResolver.Resolve(["code", "name"], ImportEntity.ServicePoints,
            new FileMapping { Columns = new() { ["code"] = "ausente" } }));
    }

    [Fact]
    public void Native_xlsx_numbers_and_text_decimals_use_their_own_format()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mixta");
        string[] headers = ["sku", "name", "base_unit", "weight_per_unit"];
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        for (var row = 2; row <= 3; row++)
        { sheet.Cell(row, 1).Value = $"S{row}"; sheet.Cell(row, 2).Value = "Producto"; sheet.Cell(row, 3).Value = "L"; }
        sheet.Cell(2, 4).Value = 0.45;
        sheet.Cell(3, 4).Value = "0,45";
        using var stream = new MemoryStream(); workbook.SaveAs(stream); stream.Position = 0;
        var table = new TabularReader().Read(stream, "mixed.xlsx", null, "Mixta");
        var report = new ImportReport(Guid.NewGuid(), "mixta", true);
        var rows = RowParser.Parse(table, ImportEntity.Products, ColumnResolver.Resolve(table.Headers, ImportEntity.Products, null),
            null, new ParseSettings(",", ParseSettings.DefaultDateFormats), report);
        Assert.Equal(2, rows.Count); Assert.All(rows, row => Assert.Equal(0.45m, row.Decimal("weight_per_unit")));
        Assert.Equal(0, report.ErrorCount);
    }

    [Fact]
    public void Xlsx_1904_dates_round_trip_and_formulas_are_rejected()
    {
        using var workbook = new XLWorkbook { Use1904DateSystem = true };
        var sheet = workbook.AddWorksheet("Datos");
        sheet.Cell(1, 1).Value = "date"; sheet.Cell(2, 1).Value = new DateTime(2026, 10, 12);
        using var stream = new MemoryStream(); workbook.SaveAs(stream); stream.Position = 0;
        Assert.Equal("2026-10-12", new TabularReader().Read(stream, "dates.xlsx", null, null).Rows[0]["date"]);
        sheet.Cell(2, 1).FormulaA1 = "1+2";
        using var formula = new MemoryStream(); workbook.SaveAs(formula); formula.Position = 0;
        Assert.Throws<UnsupportedFileException>(() => new TabularReader().Read(formula, "formula.xlsx", null, null));
    }

    [Fact]
    public void Csv_keeps_actual_row_numbers_after_blank_lines_and_quoted_values()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("code|name\n\nP1|\"Dos, lugares\"\n"));
        var table = new TabularReader().Read(stream, "data.csv", null, null);
        Assert.Equal(3, Assert.Single(table.RowNumbers!)); Assert.Equal("Dos, lugares", table.Rows[0]["name"]);
    }
}
