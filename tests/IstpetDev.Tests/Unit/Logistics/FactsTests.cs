using System.Text.Json;
using IstpetDev.Application.Logistics;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public class FactsTests
{
    [Fact]
    public void Facts_order_stably_by_code_subject_and_unit()
    {
        var facts = new List<Fact>
        {
            new("route_cost", "VEH-01", 45.50m, "USD"),
            new("coverage_days", "HOS-N01/DES-01", 5.0m, "días"),
            new("coverage_days", "CLI-N02/PAP-01", 20.0m, "días"),
            new("route_km", "VEH-01", 32.4m, "km")
        };

        var sorted = Facts.Sort(facts);

        Assert.Equal("coverage_days", sorted[0].Code);
        Assert.Equal("CLI-N02/PAP-01", sorted[0].Subject);

        Assert.Equal("coverage_days", sorted[1].Code);
        Assert.Equal("HOS-N01/DES-01", sorted[1].Subject);

        Assert.Equal("route_cost", sorted[2].Code);
        Assert.Equal("route_km", sorted[3].Code);
    }

    [Fact]
    public void Fact_serializes_with_invariant_camel_case_json()
    {
        var fact = new Fact("coverage_days", "HOS-N01/DES-01", 5.000m, "días");
        var json = JsonSerializer.Serialize(fact, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("coverage_days", root.GetProperty("code").GetString());
        Assert.Equal("HOS-N01/DES-01", root.GetProperty("subject").GetString());
        Assert.Equal(5.000m, root.GetProperty("value").GetDecimal());
        Assert.Equal("días", root.GetProperty("unit").GetString());
    }

    [Fact]
    public void FactBuilder_creates_standard_facts()
    {
        var coverageFact = Facts.CoverageDays("HOS-N01", "DES-01", 5.0m);
        Assert.Equal("coverage_days", coverageFact.Code);
        Assert.Equal("HOS-N01/DES-01", coverageFact.Subject);
        Assert.Equal(5.0m, coverageFact.Value);
        Assert.Equal("días", coverageFact.Unit);

        var kmFact = Facts.RouteKm("VEH-01", 12.5m);
        Assert.Equal("route_km", kmFact.Code);
        Assert.Equal("VEH-01", kmFact.Subject);
        Assert.Equal(12.5m, kmFact.Value);
        Assert.Equal("km", kmFact.Unit);
    }
}
