using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class PolicyParityTests
{
    [Fact]
    public void All_Three_Policy_Runners_Receive_Same_Calendar_Hash_And_Do_Not_Share_Mutable_State()
    {
        var key = new ScenarioKey("national-v1", 11, "base");
        var calendar = ExogenousCalendar.Build(
            key,
            points: [("REG-01", "P01"), ("REG-01", "P02")],
            products: [("SKU-01", 10m, "L")],
            startDate: new DateOnly(2026, 10, 12),
            days: 7);

        var initialState = new SimulationInitialState(
            WarehouseStock: new Dictionary<(string Location, string Sku), decimal>
            {
                { ("BOD-01", "SKU-01"), 1000m }
            },
            PointStock: new Dictionary<(string Location, string Sku), decimal>
            {
                { ("P01", "SKU-01"), 30m },
                { ("P02", "SKU-01"), 30m }
            });

        // Crear los tres runners con copia profunda
        var runnerReactive = new PolicyRunner("reactive-v1", calendar, initialState.DeepCopy());
        var runnerFixed = new PolicyRunner("fixed7-v1", calendar, initialState.DeepCopy());
        var runnerIstpet = new PolicyRunner("istpetdev-v1", calendar, initialState.DeepCopy());

        // Comprobar que todos ven el mismo calendar hash
        Assert.Equal(calendar.Sha256, runnerReactive.CalendarSha256);
        Assert.Equal(calendar.Sha256, runnerFixed.CalendarSha256);
        Assert.Equal(calendar.Sha256, runnerIstpet.CalendarSha256);

        // Mutar el stock en uno no altera los otros
        runnerReactive.MutateStockForTest("BOD-01", "SKU-01", -100m);

        Assert.Equal(900m, runnerReactive.GetStock("BOD-01", "SKU-01"));
        Assert.Equal(1000m, runnerFixed.GetStock("BOD-01", "SKU-01"));
        Assert.Equal(1000m, runnerIstpet.GetStock("BOD-01", "SKU-01"));
    }
}
