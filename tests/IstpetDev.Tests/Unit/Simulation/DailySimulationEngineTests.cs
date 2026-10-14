using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class DailySimulationEngineTests
{
    [Fact]
    public void Demand_At_10_With_Delivery_At_12_And_Initial_Stock_0_Leaves_Stockout_At_10()
    {
        var date = new DateOnly(2026, 10, 12);
        var clock = new SimulationClock(date);
        var engine = new DailySimulationEngine("REG-CENTRO", clock);

        engine.SetInitialPointStock("P01", "DES-01", 0m);

        // Programar demanda de 5L a las 10:00
        var demandAt10 = new DemandEvent(
            OccurredAtUtc: date.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
            RegionCode: "REG-CENTRO",
            PointCode: "P01",
            Sku: "DES-01",
            Requested: 5m,
            Unit: "L",
            DueAtUtc: date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc),
            IsSpike: false);

        // Programar entrega de 5L a las 12:00
        var deliveryAt12 = new SimulatedDelivery(
            PointCode: "P01",
            Sku: "DES-01",
            Quantity: 5m,
            EtaUtc: date.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc));

        var result = engine.ExecuteDay(
            date: date,
            demands: [demandAt10],
            deliveries: [deliveryAt12],
            disruptions: Array.Empty<DisruptionEvent>());

        // Debe registrar 1 quiebre ocurrido a las 10:00
        Assert.Single(result.Stockouts);
        var stockout = result.Stockouts[0];
        Assert.Equal("P01", stockout.PointCode);
        Assert.Equal(5m, stockout.UnservedQuantity);

        // El stock final al cierre del día es 5L (porque llegó a las 12:00)
        Assert.Equal(5m, engine.GetPointStock("P01", "DES-01"));
    }

    [Fact]
    public void Delivery_At_09_Avoids_Stockout_For_Demand_At_10()
    {
        var date = new DateOnly(2026, 10, 12);
        var clock = new SimulationClock(date);
        var engine = new DailySimulationEngine("REG-CENTRO", clock);

        engine.SetInitialPointStock("P01", "DES-01", 0m);

        // Programar entrega a las 09:00
        var deliveryAt09 = new SimulatedDelivery(
            PointCode: "P01",
            Sku: "DES-01",
            Quantity: 5m,
            EtaUtc: date.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc));

        // Programar demanda a las 10:00
        var demandAt10 = new DemandEvent(
            OccurredAtUtc: date.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
            RegionCode: "REG-CENTRO",
            PointCode: "P01",
            Sku: "DES-01",
            Requested: 5m,
            Unit: "L",
            DueAtUtc: date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc),
            IsSpike: false);

        var result = engine.ExecuteDay(
            date: date,
            demands: [demandAt10],
            deliveries: [deliveryAt09],
            disruptions: Array.Empty<DisruptionEvent>());

        // La entrega temprana evitó el quiebre
        Assert.Empty(result.Stockouts);
        Assert.Equal(0m, engine.GetPointStock("P01", "DES-01"));
    }

    [Fact]
    public void Disruption_Occurred_At_08_Known_At_11_Is_Not_Visible_At_09()
    {
        var date = new DateOnly(2026, 10, 12);
        var clock = new SimulationClock(date);
        var engine = new DailySimulationEngine("REG-CENTRO", clock);

        var disruption = new DisruptionEvent(
            OccurredAtUtc: date.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc),
            KnownAtUtc: date.ToDateTime(new TimeOnly(11, 0), DateTimeKind.Utc),
            RegionCode: "REG-CENTRO",
            Kind: "road_closure",
            SubjectCode: "SEG-01",
            Factor: 2.0m);

        var time09 = date.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
        var time1130 = date.ToDateTime(new TimeOnly(11, 30), DateTimeKind.Utc);

        var knownAt09 = engine.GetKnownDisruptions([disruption], time09);
        var knownAt1130 = engine.GetKnownDisruptions([disruption], time1130);

        Assert.Empty(knownAt09);
        Assert.Single(knownAt1130);
    }
}
