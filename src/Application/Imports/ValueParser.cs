using System.Globalization;

namespace IstpetDev.Application.Imports;

public sealed record ParseSettings(string? DecimalSeparator, IReadOnlyList<string> DateFormats)
{
    public static readonly IReadOnlyList<string> DefaultDateFormats =
        ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM"];
}

public static class ValueParser
{
    private static readonly string[] TimeFormats = ["HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss"];

    public static bool TryParse(string text, FieldType type, ParseSettings settings, out object? value)
    {
        value = null;
        switch (type)
        {
            case FieldType.Text:
                value = text.Trim();
                return true;
            case FieldType.Decimal when TryDecimal(text, settings.DecimalSeparator, out var number):
                value = number;
                return true;
            case FieldType.Integer when TryDecimal(text, settings.DecimalSeparator, out var whole)
                                        && whole == decimal.Truncate(whole) && whole >= int.MinValue && whole <= int.MaxValue:
                value = (int)whole;
                return true;
            case FieldType.Date when TryDate(text, settings, out var date):
                value = date;
                return true;
            case FieldType.Time when TryTime(text, out var time):
                value = time;
                return true;
            default:
                return false;
        }
    }

    public static bool TryDecimal(string text, string? decimalSeparator, out decimal value)
    {
        var cleaned = text.Trim().Replace(" ", "").Replace(" ", "");
        var separator = decimalSeparator ?? InferDecimalSeparator(cleaned);
        var thousands = separator == "," ? '.' : ',';
        var mantissa = cleaned.Split('e', 'E')[0];
        if (mantissa.Contains(thousands))
        {
            var groups = mantissa.Split(separator[0])[0].TrimStart('+', '-').Split(thousands);
            if (groups[0].Length is < 1 or > 3 || groups.Skip(1).Any(g => g.Length != 3 || !g.All(char.IsDigit)))
            { value = 0; return false; }
        }
        cleaned = cleaned.Replace(thousands.ToString(), "");
        if (separator == ",") cleaned = cleaned.Replace(',', '.');
        return decimal.TryParse(cleaned, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture, out value);
    }

    // Un único separador se toma como decimal (convención es-EC); varios del mismo tipo son miles.
    private static string InferDecimalSeparator(string text)
    {
        int comma = text.LastIndexOf(','), dot = text.LastIndexOf('.');
        if (comma >= 0 && dot >= 0) return comma > dot ? "," : ".";
        if (comma >= 0) return text.Count(c => c == ',') == 1 ? "," : ".";
        if (dot >= 0 && text.Count(c => c == '.') > 1) return ",";
        return ".";
    }

    private static bool TryDate(string text, ParseSettings settings, out DateOnly date)
    {
        var trimmed = text.Trim();
        if (DateTime.TryParseExact(trimmed, settings.DateFormats.ToArray(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            date = DateOnly.FromDateTime(parsed);
            return true;
        }
        // Serial de fecha de Excel (días desde 1899-12-30), acotado a 1954–2119.
        if (double.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var serial) && serial >= 20000 && serial <= 80000)
        {
            date = DateOnly.FromDateTime(DateTime.FromOADate(Math.Floor(serial)));
            return true;
        }
        date = default;
        return false;
    }

    private static bool TryTime(string text, out TimeOnly time)
    {
        var trimmed = text.Trim();
        if (TimeOnly.TryParseExact(trimmed, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) return true;
        // Fracción de día de Excel (0.5 = 12:00).
        if (double.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fraction) && fraction >= 0 && fraction < 1)
        {
            time = TimeOnly.FromTimeSpan(TimeSpan.FromDays(fraction));
            return true;
        }
        return false;
    }
}
