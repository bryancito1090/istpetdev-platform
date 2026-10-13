using System.Globalization;
using IstpetDev.Application.Imports;

namespace IstpetDev.Tests.Unit.Imports;

[Trait("Category", "Unit")]
public class PrimitivesTests
{
    private static readonly ParseSettings Auto = new(null, ParseSettings.DefaultDateFormats);

    [Theory]
    [InlineData("Cód. Cliente", "cod_cliente")]
    [InlineData("  CÓDIGO  ", "codigo")]
    [InlineData("Razón Social", "razon_social")]
    [InlineData("lead-time (días)", "lead_time_dias")]
    [InlineData("Año__Mes", "ano_mes")]
    [InlineData("", "")]
    public void Normalize_strips_accents_case_and_symbols(string header, string expected)
        => Assert.Equal(expected, HeaderNormalizer.Normalize(header));

    [Theory]
    [InlineData("service_points", ImportEntity.ServicePoints)]
    [InlineData("Puntos", ImportEntity.ServicePoints)]
    [InlineData("inventario", ImportEntity.Stock)]
    [InlineData("Vehículos", ImportEntity.Vehicles)]
    public void Entity_keys_accept_canonical_names_and_spanish_aliases(string text, ImportEntity expected)
    {
        Assert.True(ImportEntityKeys.TryParse(text, out var entity));
        Assert.Equal(expected, entity);
    }

    [Fact]
    public void Entity_keys_reject_unknown_names() => Assert.False(ImportEntityKeys.TryParse("pedidos", out _));

    [Fact]
    public void Every_entity_has_required_fields()
    {
        foreach (var entity in CanonicalSchema.Order)
            Assert.Contains(CanonicalSchema.Fields[entity], field => field.Required);
    }

    [Fact]
    public void Header_candidates_do_not_collide_within_an_entity()
    {
        foreach (var entity in CanonicalSchema.Order)
        {
            var candidates = CanonicalSchema.Fields[entity].SelectMany(f => f.Candidates().Distinct()).ToList();
            Assert.Equal(candidates.Count, candidates.Distinct().Count());
        }
    }

    [Theory]
    [InlineData("12.5", "12.5")]
    [InlineData("12,5", "12.5")]
    [InlineData("1.234,56", "1234.56")]
    [InlineData("1,234.56", "1234.56")]
    [InlineData("1.234.567", "1234567")]
    [InlineData("1,234,567", "1234567")]
    [InlineData("-3", "-3")]
    [InlineData("1.2E+3", "1200")]
    public void Decimal_infers_the_separator(string text, string expected)
    {
        Assert.True(ValueParser.TryParse(text, FieldType.Decimal, Auto, out var value));
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), value);
    }

    [Fact]
    public void Decimal_respects_a_declared_separator()
    {
        var comma = new ParseSettings(",", ParseSettings.DefaultDateFormats);
        Assert.True(ValueParser.TryParse("1.250", FieldType.Decimal, comma, out var value));
        Assert.Equal(1250m, value);
    }

    [Theory]
    [InlineData("2026-10-05", 2026, 10, 5)]
    [InlineData("05/10/2026", 2026, 10, 5)]
    [InlineData("2026-09", 2026, 9, 1)]
    [InlineData("46300", 2026, 10, 5)]
    public void Date_accepts_common_formats_and_excel_serials(string text, int year, int month, int day)
    {
        Assert.True(ValueParser.TryParse(text, FieldType.Date, Auto, out var value));
        Assert.Equal(new DateOnly(year, month, day), value);
    }

    [Theory]
    [InlineData("07:00", 7, 0)]
    [InlineData("7:30", 7, 30)]
    [InlineData("0.5", 12, 0)]
    public void Time_accepts_clock_text_and_excel_fractions(string text, int hour, int minute)
    {
        Assert.True(ValueParser.TryParse(text, FieldType.Time, Auto, out var value));
        Assert.Equal(new TimeOnly(hour, minute), value);
    }

    [Theory]
    [InlineData("abc", FieldType.Decimal)]
    [InlineData("15.5", FieldType.Integer)]
    [InlineData("31/02/2026", FieldType.Date)]
    [InlineData("25:00", FieldType.Time)]
    public void Invalid_values_are_rejected(string text, FieldType type)
        => Assert.False(ValueParser.TryParse(text, type, Auto, out _));
}
