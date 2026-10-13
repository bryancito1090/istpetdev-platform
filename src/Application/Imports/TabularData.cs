namespace IstpetDev.Application.Imports;

// InvariantNumbers = true cuando los números vienen tipados (XLSX): no se reinterpretan con el separador del perfil.
public sealed record TabularData(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows,
    bool InvariantNumbers = false,
    IReadOnlyList<IReadOnlySet<string>>? NumericCells = null,
    IReadOnlyList<int>? RowNumbers = null,
    string? Encoding = null,
    string? Sheet = null)
{
    public static string[] UniqueHeaders(IEnumerable<string?> raw)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var headers = raw.Select((header, index) =>
        {
            var name = string.IsNullOrWhiteSpace(header) ? $"columna_{index + 1}" : header.Trim();
            if (!seen.TryAdd(name, 1)) throw new UnsupportedFileException("Encabezado repetido; renombre la columna.");
            return name;
        }).ToArray();
        if (headers.Length > ImportLimits.Columns)
            throw new UnsupportedFileException("El archivo supera 128 columnas; divida o reduzca sus columnas.");
        if (headers.GroupBy(HeaderNormalizer.Normalize).Any(g => g.Count() > 1))
            throw new UnsupportedFileException("Hay encabezados equivalentes; renómbrelos para identificar cada columna.");
        return headers;
    }
}

public interface ITabularReader
{
    TabularData Read(Stream content, string fileName, char? delimiter, string? sheet);
}

public sealed class UnsupportedFileException(string message) : Exception(message);
