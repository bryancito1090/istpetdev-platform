using IstpetDev.Application.Imports;

namespace IstpetDev.Infrastructure.Imports;

public sealed class TabularReader : ITabularReader
{
    public TabularData Read(Stream content, string fileName, char? delimiter, string? sheet) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".xlsx" => new XlsxTabularReader().Read(content, sheet),
            ".tsv" => new CsvTabularReader().Read(content, delimiter ?? '\t'),
            ".csv" => new CsvTabularReader().Read(content, delimiter),
            var other => throw new UnsupportedFileException($"Formato '{other}' no soportado. Guarde el archivo como .csv o .xlsx."),
        };
}
