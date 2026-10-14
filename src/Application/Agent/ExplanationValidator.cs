using System.Globalization;
using System.Text.RegularExpressions;
using IstpetDev.Application.Imports;
using IstpetDev.Application.Logistics;

namespace IstpetDev.Application.Agent;

public sealed partial class ExplanationValidator : IExplanationValidator
{
    // Excluye cifras pegadas a letras o guiones como parte de códigos (VEH-02, HOS-N01, RP-20261012-01)
    [GeneratedRegex(@"(?<![\p{L}\d]|[\p{L}\d]-)(-?\d{1,2}:\d{2}|-?\d+(?:[.,]\d+)?)(\s?%)?(?![\p{L}\d])")]
    private static partial Regex NumberExtractionRegex();

    public ValidationResult Validate(string text, IReadOnlyList<Fact> facts)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ValidationResult(true, null, []);

        var matches = NumberExtractionRegex().Matches(text);
        var unmatched = new List<string>();

        foreach (Match match in matches)
        {
            var rawValue = match.Groups[1].Value;
            var isPercentage = match.Groups[2].Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value);
            var fullMatchText = match.Value.Trim();

            if (rawValue.Contains(':'))
            {
                // Formato de hora HH:mm
                if (!ValidateTime(rawValue, facts))
                {
                    unmatched.Add(fullMatchText);
                }
            }
            else if (isPercentage)
            {
                // Porcentaje: requiere fact con unidad "%"
                if (!ValidatePercentage(rawValue, facts))
                {
                    unmatched.Add(fullMatchText);
                }
            }
            else
            {
                // Decimal o entero / conteo general
                if (!ValidateGeneralNumber(rawValue, facts))
                {
                    unmatched.Add(fullMatchText);
                }
            }
        }

        if (unmatched.Count > 0)
        {
            var mismatchReason = $"{string.Join(", ", unmatched)} (sin respaldo en facts)";
            return new ValidationResult(false, mismatchReason, unmatched);
        }

        return new ValidationResult(true, null, []);
    }

    private static bool ValidateTime(string timeStr, IReadOnlyList<Fact> facts)
    {
        if (!TimeSpan.TryParse(timeStr, CultureInfo.InvariantCulture, out var time))
            return false;

        var totalHours = (decimal)time.TotalHours;
        var totalMinutes = (decimal)time.TotalMinutes;

        // Coincide con fact de unidad "hora" o "min"
        return facts.Any(f =>
            (string.Equals(f.Unit, "hora", StringComparison.OrdinalIgnoreCase) && Math.Abs(f.Value - totalHours) < 0.05m) ||
            (string.Equals(f.Unit, "horas", StringComparison.OrdinalIgnoreCase) && Math.Abs(f.Value - totalHours) < 0.05m) ||
            (string.Equals(f.Unit, "min", StringComparison.OrdinalIgnoreCase) && Math.Abs(f.Value - totalMinutes) < 1.0m));
    }

    private static bool ValidatePercentage(string numberStr, IReadOnlyList<Fact> facts)
    {
        if (!ValueParser.TryDecimal(numberStr, null, out var number))
            return false;

        var decimalsWritten = GetDecimalsCount(numberStr);
        var percentFacts = facts.Where(f => f.Unit == "%" || f.Unit == "pct" || f.Code.Contains("pct")).ToList();

        return percentFacts.Any(f =>
            Math.Round(f.Value, decimalsWritten) == number ||
            f.Value == number);
    }

    private static bool ValidateGeneralNumber(string numberStr, IReadOnlyList<Fact> facts)
    {
        if (!ValueParser.TryDecimal(numberStr, null, out var number))
            return false;

        var decimalsWritten = GetDecimalsCount(numberStr);

        return facts.Any(f =>
            Math.Round(f.Value, decimalsWritten) == number ||
            f.Value == number);
    }

    private static int GetDecimalsCount(string numberStr)
    {
        var cleaned = numberStr.Trim();
        var separatorIdx = cleaned.LastIndexOfAny([',', '.']);
        if (separatorIdx < 0) return 0;
        return cleaned.Length - separatorIdx - 1;
    }
}
