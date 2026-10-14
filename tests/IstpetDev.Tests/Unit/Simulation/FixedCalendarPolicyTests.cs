using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class FixedCalendarPolicyTests
{
    [Fact]
    public void Region_Of_10_Points_Assigns_Day_Modulo_5_Two_Per_Weekday()
    {
        var points = Enumerable.Range(1, 10)
            .Select(i => $"P{i:D2}")
            .OrderBy(c => c)
            .ToList();

        var schedule = FixedCalendarPolicy.AssignWeeklyDays(points);

        Assert.Equal(10, schedule.Count);

        // 2 puntos por día laborable (Lunes a Viernes)
        for (int dayIndex = 0; dayIndex < 5; dayIndex++)
        {
            var dayOfWeek = (DayOfWeek)(dayIndex + 1); // 1 = Monday ... 5 = Friday
            var pointsForDay = schedule.Where(kv => kv.Value == dayOfWeek).Select(kv => kv.Key).ToList();
            Assert.Equal(2, pointsForDay.Count);
        }

        // P01 (índice 0) -> Lunes (DayOfWeek.Monday)
        Assert.Equal(DayOfWeek.Monday, schedule["P01"]);
        // P02 (índice 1) -> Martes (DayOfWeek.Tuesday)
        Assert.Equal(DayOfWeek.Tuesday, schedule["P02"]);
        // P05 (índice 4) -> Viernes (DayOfWeek.Friday)
        Assert.Equal(DayOfWeek.Friday, schedule["P05"]);
        // P06 (índice 5) -> Lunes (DayOfWeek.Monday)
        Assert.Equal(DayOfWeek.Monday, schedule["P06"]);
    }

    [Fact]
    public void Fixed_Quantity_Is_7_Times_Mean_Daily_And_Ignores_Spikes_Or_Current_Balance()
    {
        var policy = new FixedCalendarPolicy();
        decimal meanDaily = 10m;

        // Caso 1: saldo bajo
        var req1 = policy.CalculateFixedDemand("P01", "SKU-01", currentStock: 5m, historicalMeanDaily: meanDaily, isSpike: true);
        // Caso 2: saldo alto
        var req2 = policy.CalculateFixedDemand("P01", "SKU-01", currentStock: 60m, historicalMeanDaily: meanDaily, isSpike: false);

        // Ambos reciben 7 * 10 = 70 unidades
        Assert.Equal(70m, req1);
        Assert.Equal(70m, req2);
    }

    [Fact]
    public void Route_Parity_With_Same_Stops_Produces_Identical_Sequence()
    {
        var stops = new List<string> { "P01", "P06", "P02" };
        var depot = "BOD-01";

        // Misma matriz de distancias y ordenación determinista (inserción + 2-opt)
        var seqFixed = FixedCalendarPolicy.SequenceStops(depot, stops);
        var seqIstpet = IstpetDevPolicyHelper.SequenceStops(depot, stops);

        Assert.Equal(seqFixed, seqIstpet);
    }
}
