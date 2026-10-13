using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IstpetDev.Application.Imports;

namespace IstpetDev.Infrastructure.Imports;

public sealed class CsvTabularReader
{
    private static readonly char[] CandidateDelimiters = [';', ',', '\t', '|'];

    public TabularData Read(Stream content, char? delimiter)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        if (buffer.Length > ImportLimits.FileBytes) throw new UnsupportedFileException("El archivo supera 10 MiB; divídalo.");
        string text, encoding;
        try { text = new UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF'); encoding = "UTF-8"; }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            text = Encoding.GetEncoding(1252).GetString(buffer.ToArray()); encoding = "Windows-1252";
        }
        if (text.Contains('\0')) throw new UnsupportedFileException("El CSV contiene bytes binarios; exporte texto UTF-8.");
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = (delimiter ?? DetectDelimiter(text)).ToString(), HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim, MaxFieldSize = ImportLimits.CellCharacters,
        };
        using var parser = new CsvParser(new StringReader(text), configuration);
        string[]? headers = null;
        var rows = new List<IReadOnlyDictionary<string, string>>();
        var rowNumbers = new List<int>();
        while (parser.Read())
        {
            var record = parser.Record ?? [];
            if (headers is null) { headers = TabularData.UniqueHeaders(record); continue; }
            if (record.All(string.IsNullOrWhiteSpace)) continue;
            if (record.Length != headers.Length) throw new UnsupportedFileException($"Fila {parser.RawRow}: número de columnas distinto del encabezado; revise separador y comillas.");
            if (rows.Count == ImportLimits.Rows) throw new UnsupportedFileException("El CSV supera 100 000 filas; divídalo.");
            rows.Add(headers.Select((header, i) => (header, value: record[i])).ToDictionary(x => x.header, x => x.value));
            rowNumbers.Add(parser.RawRow);
        }
        return new TabularData(headers ?? [], rows, RowNumbers: rowNumbers, Encoding: encoding);
    }

    public static string Decode(byte[] bytes)
    {
        try { return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException) { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding(1252).GetString(bytes); }
    }

    public static char DetectDelimiter(string text)
    {
        var counts = CandidateDelimiters.ToDictionary(c => c, _ => 0);
        var quoted = false;
        foreach (var c in text)
        {
            if (c == '"') quoted = !quoted;
            if (!quoted && c is '\r' or '\n') break;
            if (!quoted && counts.ContainsKey(c)) counts[c]++;
        }
        return CandidateDelimiters.OrderByDescending(c => counts[c]).First();
    }
}
