using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class ExogenousCalendarTests
{
    [Fact]
    public void Weekday_And_Weekend_Factors_Are_Exact_When_Noise_Is_Zero()
    {
        // Laborable: factor 1.0 (10 L -> 10 L)
        // Fin de semana: factor 0.8 (10 L -> 8 L)
        var monday = new DateOnly(2026, 10, 12); // Lunes
        var saturday = new DateOnly(2026, 10, 17); // Sábado

        var qtyMonday = ExogenousCalendar.CalculateExpectedDemand(10m, monday, noisePercent: 0m, isSpike: false);
        var qtySaturday = ExogenousCalendar.CalculateExpectedDemand(10m, saturday, noisePercent: 0m, isSpike: false);

        Assert.Equal(10m, qtyMonday);
        Assert.Equal(8m, qtySaturday);
    }

    [Fact]
    public void Noise_Remains_Within_Plus_Minus_15_Percent()
    {
        var key = new ScenarioKey("national-v1", 11, "base");
        var calendar = ExogenousCalendar.Build(
            key,
            points: [("REG-CENTRO", "P01"), ("REG-CENTRO", "P02")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 14);

        foreach (var demand in calendar.Demand.Where(d => !d.IsSpike))
        {
            var isWeekend = demand.OccurredAtUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var expectedBase = isWeekend ? 8m : 10m;
            var minAllowed = expectedBase * 0.85m;
            var maxAllowed = expectedBase * 1.15m;

            Assert.InRange(demand.Requested, minAllowed, maxAllowed);
        }
    }

    [Fact]
    public void Spikes_Are_Tagged_And_Occur_On_Same_Keys_For_Same_Seed()
    {
        var key = new ScenarioKey("national-v1", 11, "base");
        var cal1 = ExogenousCalendar.Build(
            key,
            points: [("REG-CENTRO", "P01"), ("REG-CENTRO", "P02"), ("REG-NORTE", "P03")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 30);

        var cal2 = ExogenousCalendar.Build(
            key,
            points: [("REG-CENTRO", "P01"), ("REG-CENTRO", "P02"), ("REG-NORTE", "P03")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 30);

        var spikes1 = cal1.Demand.Where(d => d.IsSpike).Select(d => (d.OccurredAtUtc, d.PointCode, d.Sku)).ToList();
        var spikes2 = cal2.Demand.Where(d => d.IsSpike).Select(d => (d.OccurredAtUtc, d.PointCode, d.Sku)).ToList();

        Assert.NotEmpty(spikes1);
        Assert.Equal(spikes1, spikes2);
        Assert.Equal(cal1.Sha256, cal2.Sha256);
    }

    [Fact]
    public void VisibleAt_Does_Not_Leak_Future_Demand_Or_Disruptions()
    {
        var key = new ScenarioKey("national-v1", 11, "base");
        var startDate = new DateOnly(2026, 10, 12);
        var disruptions = new List<DisruptionEvent>
        {
            new(
                OccurredAtUtc: new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc),
                KnownAtUtc: new DateTime(2026, 10, 13, 11, 0, 0, DateTimeKind.Utc),
                RegionCode: "REG-CENTRO",
                Kind: "road_block",
                SubjectCode: "SEG-01",
                Factor: 2.5m)
        };

        var calendar = ExogenousCalendar.Build(
            key,
            points: [("REG-CENTRO", "P01")],
            products: [("SKU-01", 10m, "L")],
            startDate: startDate,
            days: 3,
            customDisruptions: disruptions);

        // A las 09:00 del día 13, la demanda del día 13 (a las 06:00) es visible
        var t1 = new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc);
        var visibleDemand = calendar.DemandVisibleAt(t1);
        var visibleDisruptions = calendar.DisruptionsVisibleAt(t1);

        // La demanda del día 14 (futuro) NO debe estar visible
        Assert.DoesNotContain(visibleDemand, d => d.OccurredAtUtc > t1);
        // El incidente ocurrió a las 08:00 pero se conoce a las 11:00 -> a las 09:00 NO es visible
        Assert.Empty(visibleDisruptions);

        // A las 11:30 el incidente ya es visible
        var t2 = new DateTime(2026, 10, 13, 11, 30, 0, DateTimeKind.Utc);
        var visibleDisruptionsAfter = calendar.DisruptionsVisibleAt(t2);
        Assert.Single(visibleDisruptionsAfter);
    }

    [Fact]
    public void Adverse_Scenario_Derives_Demand_Times_1_20()
    {
        var baseKey = new ScenarioKey("national-v1", 42, "base");
        var baseCalendar = ExogenousCalendar.Build(
            baseKey,
            points: [("REG-SUR", "P10")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 5);

        var adverseCalendar = ExogenousCalendar.DeriveAdverseScenario(baseCalendar, "VEH-ADVERSE-01");

        Assert.Equal("adverse", adverseCalendar.Key.ScenarioCode);
        Assert.Equal(baseCalendar.Demand.Count, adverseCalendar.Demand.Count);

        for (int i = 0; i < baseCalendar.Demand.Count; i++)
        {
            var b = baseCalendar.Demand[i];
            var a = adverseCalendar.Demand[i];

            Assert.Equal(b.OccurredAtUtc, a.OccurredAtUtc);
            Assert.Equal(b.PointCode, a.PointCode);
            Assert.Equal(b.Sku, a.Sku);
            Assert.Equal(b.IsSpike, a.IsSpike);
            Assert.Equal(Math.Round(b.Requested * 1.20m, 3), a.Requested);
        }
    }
}
