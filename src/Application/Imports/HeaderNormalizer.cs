using System.Text;

namespace IstpetDev.Application.Imports;

// Normaliza encabezados y alias sin depender de ICU: el contenedor corre con globalización invariante.
public static class HeaderNormalizer
{
    private const string Accented = "áàäâãéèëêíìïîóòöôõúùüûñçÁÀÄÂÃÉÈËÊÍÌÏÎÓÒÖÔÕÚÙÜÛÑÇ";
    private const string Plain = "aaaaaeeeeiiiiooooouuuuncaaaaaeeeeiiiiooooouuuunc";

    public static string Normalize(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return string.Empty;
        var builder = new StringBuilder(header.Length);
        foreach (var raw in header.Trim())
        {
            var index = Accented.IndexOf(raw);
            var c = char.ToLowerInvariant(index >= 0 ? Plain[index] : raw);
            builder.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '_');
        }
        var collapsed = builder.ToString();
        while (collapsed.Contains("__")) collapsed = collapsed.Replace("__", "_");
        return collapsed.Trim('_');
    }
}
