using IstpetDev.Application.Agent;
using IstpetDev.Application.Logistics;
using Xunit;

namespace IstpetDev.Tests.Unit.Agent;

public class ExplanationValidatorTests
{
    private readonly IExplanationValidator _validator = new ExplanationValidator();

    [Fact]
    public void Matching_decimals_and_rounding_pass_validation()
    {
        var facts = new List<Fact>
        {
            new("stock_coverage_days", "HOS-N01:DES-01", 5.0m, "dias"),
            new("total_km", "VEH-01", 42.5m, "km")
        };

        // "5,0 días" o "5 días" coincide con fact 5.0m
        var res1 = _validator.Validate("La cobertura actual es de 5,0 días en el punto.", facts);
        Assert.True(res1.IsValid);
        Assert.Null(res1.MismatchReason);

        var res2 = _validator.Validate("Se recorren 42.5 km en total con 5 días de cobertura.", facts);
        Assert.True(res2.IsValid);
    }

    [Fact]
    public void Hallucinated_number_fails_validation_with_mismatch_reason()
    {
        var facts = new List<Fact>
        {
            new("stock_coverage_days", "HOS-N01:DES-01", 5.0m, "dias")
        };

        // El modelo dice 6 días cuando el fact es 5.0m
        var res = _validator.Validate("La cobertura es de 6 días.", facts);
        Assert.False(res.IsValid);
        Assert.NotNull(res.MismatchReason);
        Assert.Contains("6", res.MismatchReason);
    }

    [Fact]
    public void Percentage_requires_percentage_fact()
    {
        var facts = new List<Fact>
        {
            new("cost_increase_pct", "VEH-01", 6.1m, "%")
        };

        var valid = _validator.Validate("El incremento de costo es de 6,1 %.", facts);
        Assert.True(valid.IsValid);

        var invalid = _validator.Validate("El incremento de costo es de 8 %.", facts);
        Assert.False(invalid.IsValid);
        Assert.Contains("8 %", invalid.MismatchReason);
    }

    [Fact]
    public void Time_HH_mm_requires_hour_fact()
    {
        // Hora 10:30 requiere fact de unidad "hora" con valor correspondiente o subject/format
        var facts = new List<Fact>
        {
            new("eta_arrival", "HOS-N01", 10.5m, "hora"), // 10.5h = 10:30 o value decimal 10.5
            new("window_start", "HOS-N01", 10.5m, "hora")
        };

        var res = _validator.Validate("La llegada estimada es a las 10:30.", facts);
        Assert.True(res.IsValid);

        var resInvalid = _validator.Validate("La llegada estimada es a las 11:45.", facts);
        Assert.False(resInvalid.IsValid);
        Assert.Contains("11:45", resInvalid.MismatchReason);
    }

    [Fact]
    public void Entity_codes_with_numbers_do_not_produce_false_positives()
    {
        var facts = new List<Fact>
        {
            new("routes_count", "plan", 2m, "rutas"),
            new("stock_coverage_days", "HOS-N01:DES-01", 5.0m, "dias")
        };

        // "VEH-02", "HOS-N01", "RP-20261012-01", "DES-01" no deben tratarse como números independientes
        var res = _validator.Validate("El vehículo VEH-02 atiende al hospital HOS-N01 con producto DES-01 en 2 rutas con 5 días.", facts);
        Assert.True(res.IsValid, res.MismatchReason);
    }

    [Fact]
    public void Counts_match_count_facts()
    {
        var facts = new List<Fact>
        {
            new("routes_count", "plan", 2m, "rutas"),
            new("stops_count", "VEH-01", 8m, "paradas")
        };

        var res = _validator.Validate("Se asignaron 2 rutas con 8 paradas.", facts);
        Assert.True(res.IsValid);
    }
}
