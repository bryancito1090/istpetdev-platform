using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Entities;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class VehicleRestrictionTests
{
    private static LogisticsOptions Options(string start = "06:30", string end = "09:30", string? zone = null) => new()
    {
        VehicleRestrictions = [new() { VehicleType = "camion_pesado", Start = start, End = end, Zone = zone }],
        UrbanRegions = ["Centro"]
    };
    private static readonly Vehicle Heavy = new() { VehicleType = "camion_pesado", UsageScope = "extraurbano" };
    private static DateTime At(int hour, int minute = 0) => new(2026, 10, 16, hour, minute, 0);

    [Theory]
    [InlineData(7, 0, 8, 0, true)]
    [InlineData(10, 0, 11, 0, false)]
    [InlineData(6, 0, 6, 30, false)]
    [InlineData(6, 0, 6, 31, true)]
    [InlineData(6, 30, 6, 31, true)]
    [InlineData(9, 30, 10, 0, false)]
    [InlineData(7, 0, 7, 0, true)]
    public void Overlap_uses_the_whole_route_and_half_open_boundaries(int sh, int sm, int eh, int em, bool blocked)
    {
        var reason = VehicleRestrictions.DiscardReason(Options(), Heavy, At(sh, sm), At(eh, em), []);
        Assert.Equal(blocked, reason is not null);
        if (blocked) { Assert.StartsWith("restriccion_horaria", reason); Assert.Contains("06:30–09:30", reason); Assert.Contains("sintética", reason); }
    }

    [Theory]
    [InlineData("22:00", "06:00", 23, 1, true)]
    [InlineData("22:00", "06:00", 5, 6, true)]
    [InlineData("22:00", "06:00", 6, 7, false)]
    [InlineData("bad", "09:30", 7, 8, false)]
    [InlineData("06:30", "25:00", 7, 8, false)]
    [InlineData("07:00", "07:00", 7, 8, false)]
    public void Overnight_and_invalid_bands(string start, string end, int from, int to, bool blocked)
    {
        var until = At(to); if (until < At(from)) until = until.AddDays(1);
        Assert.Equal(blocked, VehicleRestrictions.DiscardReason(Options(start, end), Heavy, At(from), until, []) is not null);
    }

    [Fact]
    public void Selectors_and_zone_match_exact_values_ignoring_case_and_spaces()
    {
        var options = Options(zone: " Centro ");
        Assert.NotNull(VehicleRestrictions.DiscardReason(options, Heavy, At(7), At(8), ["CENTRO"]));
        Assert.Null(VehicleRestrictions.DiscardReason(options, Heavy, At(7), At(8), ["Valle"]));
        Assert.Null(VehicleRestrictions.DiscardReason(options, Heavy, At(7), At(8), []));
        Assert.Null(VehicleRestrictions.DiscardReason(options, new() { VehicleType = "camioneta" }, At(7), At(8), ["Centro"]));
        options.VehicleRestrictions[0].UsageScope = "urbano";
        Assert.Null(VehicleRestrictions.DiscardReason(options, Heavy, At(7), At(8), ["Centro"]));
        options.VehicleRestrictions[0].VehicleType = null;
        Assert.NotNull(VehicleRestrictions.DiscardReason(options, new() { UsageScope = "urbano" }, At(7), At(8), ["Centro"]));
        options.VehicleRestrictions[0].UsageScope = null;
        Assert.Null(VehicleRestrictions.DiscardReason(options, Heavy, At(7), At(8), ["Centro"]));
    }

    [Fact]
    public void Urban_scope_only_warns_for_known_regions_outside_explicit_urban_list()
    {
        var options = Options(); var urban = new Vehicle { UsageScope = "URBANO" };
        Assert.Empty(VehicleRestrictions.Warnings(options, urban, ["centro"]));
        Assert.Empty(VehicleRestrictions.Warnings(options, urban, []));
        Assert.StartsWith("uso_extraurbano", Assert.Single(VehicleRestrictions.Warnings(options, urban, ["Valle"])));
        Assert.Empty(VehicleRestrictions.Warnings(options, Heavy, ["Valle"]));
        Assert.Empty(VehicleRestrictions.Warnings(new(), urban, ["Valle"]));
        Assert.Null(VehicleRestrictions.DiscardReason(new(), Heavy, At(7), At(8), ["Centro"]));
    }

    [Fact]
    public void Time_is_converted_from_UTC_to_the_organization_zone_with_fallback()
    {
        var utc = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(At(7), VehicleRestrictions.LocalTime(utc, "America/Guayaquil", -5));
        Assert.Equal(At(8), VehicleRestrictions.LocalTime(utc, "missing-zone-244", -4));
    }
}
