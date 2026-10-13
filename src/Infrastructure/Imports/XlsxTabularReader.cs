using System.Globalization;
using System.IO.Compression;
using ClosedXML.Excel;
using IstpetDev.Application.Imports;

namespace IstpetDev.Infrastructure.Imports;

public sealed class XlsxTabularReader
{
    public TabularData Read(Stream content, string? sheet)
    {
        using (var zip = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true))
        {
            if (zip.Entries.Sum(e => e.Length) > ImportLimits.ExpandedBytes || zip.Entries.Count > 4096)
                throw new UnsupportedFileException("El XLSX descomprimido supera el límite; reduzca el libro.");
            if (zip.Entries.Any(e => e.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase) || e.FullName.StartsWith("xl/externalLinks/", StringComparison.OrdinalIgnoreCase)))
                throw new UnsupportedFileException("El libro contiene macros o vínculos externos; exporte valores a un XLSX sin macros.");
            if (zip.GetEntry("xl/workbook.xml") is null || zip.GetEntry("[Content_Types].xml") is null)
                throw new UnsupportedFileException("El archivo no es un XLSX válido; exporte un libro .xlsx.");
        }
        content.Position = 0;
        using var workbook = new XLWorkbook(content);
        var worksheet = sheet is null ? workbook.Worksheets.First() : workbook.Worksheet(sheet);
        var range = worksheet.RangeUsed();
        if (range is null) return new TabularData([], [], Sheet: worksheet.Name);
        if (range.RowCount() > ImportLimits.Rows + 1 || range.ColumnCount() > ImportLimits.Columns)
            throw new UnsupportedFileException("El libro supera 100 000 filas o 128 columnas; reduzca la hoja.");
        var width = range.ColumnCount();
        var rows = range.RowsUsed().ToList();
        var headers = TabularData.UniqueHeaders(rows[0].Cells(1, width).Select(c => c.GetString()));
        var data = new List<IReadOnlyDictionary<string, string>>();
        var numeric = new List<IReadOnlySet<string>>();
        var numbers = new List<int>();
        foreach (var row in rows.Skip(1))
        {
            var cells = row.Cells(1, width).ToArray();
            if (cells.Any(c => c.HasFormula)) throw new UnsupportedFileException($"Fila {row.RowNumber()}: exporte los valores de las fórmulas antes de importar.");
            var values = cells.Select(CellText).ToArray();
            if (values.Any(v => v.Length > ImportLimits.CellCharacters)) throw new UnsupportedFileException("Una celda supera 4096 caracteres; reduzca el texto.");
            if (values.All(string.IsNullOrWhiteSpace)) continue;
            data.Add(headers.Select((header, i) => (header, value: values[i])).ToDictionary(p => p.header, p => p.value));
            numeric.Add(headers.Where((_, i) => cells[i].Value.IsNumber).ToHashSet());
            numbers.Add(row.RowNumber());
        }
        return new TabularData(headers, data, NumericCells: numeric, RowNumbers: numbers, Sheet: worksheet.Name);
    }

    private static string CellText(IXLCell cell)
    {
        var value = cell.Value;
        if (value.IsDateTime)
        {
            var moment = value.GetDateTime();
            if (moment.Date <= new DateTime(1900, 1, 1)) return moment.ToString("HH:mm", CultureInfo.InvariantCulture);
            return moment.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        if (value.IsTimeSpan) return value.GetTimeSpan().ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        if (value.IsNumber) return value.GetNumber().ToString(CultureInfo.InvariantCulture);
        return cell.GetString();
    }
}
