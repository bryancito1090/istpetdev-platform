using System.Text;
using ClosedXML.Excel;
using IstpetDev.Application.Imports;
using IstpetDev.Infrastructure.Imports;

namespace IstpetDev.Tests.Unit.Imports;

[Trait("Category", "Unit")]
public class TabularReaderTests
{
    private static TabularData ReadCsv(byte[] bytes, char? delimiter = null) =>
        new TabularReader().Read(new MemoryStream(bytes), "datos.csv", delimiter, null);

    [Fact]
    public void Csv_detects_comma_and_keeps_quoted_commas()
    {
        var table = ReadCsv(Encoding.UTF8.GetBytes("code,name\nP1,\"Clínica, Sur\"\n\nP2,Norte\n"));
        Assert.Equal(new[] { "code", "name" }, table.Headers);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Clínica, Sur", table.Rows[0]["name"]);
        Assert.False(table.InvariantNumbers);
    }

    [Fact]
    public void Csv_rejects_rows_with_wrong_column_count()
    {
        Assert.Throws<UnsupportedFileException>(() => ReadCsv(Encoding.UTF8.GetBytes("a;b;c\n1;2\n")));
    }

    [Fact]
    public void Csv_reads_latin1_files_exported_by_excel()
    {
        var table = ReadCsv(Encoding.Latin1.GetBytes("Código;Descripción\nDES-01;Desinfectante\n"));
        Assert.Equal(new[] { "Código", "Descripción" }, table.Headers);
    }

    [Fact]
    public void Csv_strips_utf8_bom()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("sku,name\nA,B\n")).ToArray();
        Assert.Equal("sku", ReadCsv(bytes).Headers[0]);
    }

    [Fact]
    public void Xlsx_reads_typed_cells_in_invariant_format()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Productos");
        sheet.Cell(1, 1).Value = "sku";
        sheet.Cell(1, 2).Value = "peso";
        sheet.Cell(1, 3).Value = "vence";
        sheet.Cell(1, 4).Value = "apertura";
        sheet.Cell(2, 1).Value = "DES-01";
        sheet.Cell(2, 2).Value = 0.45;
        sheet.Cell(2, 3).Value = new DateTime(2026, 10, 5);
        sheet.Cell(2, 4).Value = TimeSpan.FromHours(7.5);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var table = new TabularReader().Read(stream, "catalogo.xlsx", null, "Productos");

        Assert.Contains("peso", table.NumericCells![0]);
        Assert.Equal("0.45", table.Rows[0]["peso"]);
        Assert.Equal("2026-10-05", table.Rows[0]["vence"]);
        Assert.Equal("07:30", table.Rows[0]["apertura"]);
    }

    [Fact]
    public void Unsupported_extensions_are_rejected_with_guidance()
    {
        var ex = Assert.Throws<UnsupportedFileException>(() => new TabularReader().Read(new MemoryStream(), "datos.pdf", null, null));
        Assert.Contains(".csv o .xlsx", ex.Message);
    }
}
