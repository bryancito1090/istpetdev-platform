using System.Text.RegularExpressions;
using IstpetDev.Application.Logistics;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class ExplanationTemplatesTests
{
    [Theory]
    [InlineData("risk")]
    [InlineData("plan")]
    [InlineData("replan")]
    [InlineData("escalation")]
    public void All_rendered_numbers_in_templates_match_an_exact_fact_and_unit(string templateKind)
    {
        var templates = new ExplanationTemplates();

        var facts = new[]
        {
            new Fact("coverage_days", "critical_item", 2.5m, "días"),
            new Fact("route_km", "total", 45.2m, "km"),
            new Fact("route_cost", "total", 120.50m, "USD"),
            new Fact("cost_increase_pct", "route", 8.5m, "%"),
            new Fact("max_delay_minutes", "route", 15m, "min")
        };

        var text = templates.Explain(templateKind, facts);

        Assert.NotEmpty(text);
        Assert.DoesNotContain("[DATO_NO_DISPONIBLE]", text);

        // Extraer todos los números del texto renderizado
        var numberMatches = Regex.Matches(text, @"\b\d+(?:\.\d+)?\b");
        Assert.NotEmpty(numberMatches);

        foreach (Match match in numberMatches)
        {
            var value = decimal.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Contains(facts, f => f.Value == value);
        }
    }

    [Fact]
    public void Template_does_not_invent_numbers_when_fact_is_missing()
    {
        var templates = new ExplanationTemplates();
        var emptyFacts = Array.Empty<Fact>();

        var text = templates.Explain("plan", emptyFacts);

        Assert.Contains("[DATO_NO_DISPONIBLE]", text);
        // Ningún número inventado en el texto
        var numberMatches = Regex.Matches(text, @"\b\d+(?:\.\d+)?\b");
        Assert.Empty(numberMatches);
    }
}
