using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class ReactivePolicyTests
{
    [Fact]
    public void Threshold_Triggers_Alert_At_19L_But_Not_At_20L_With_Mean_10_And_LeadTime_2()
    {
        var policy = new ReactivePolicy();
        decimal meanDaily = 10m;
        decimal leadTimeDays = 2m;

        // Saldo = 19L -> por debajo del umbral (20L) -> dispara pedido
        var order19 = policy.EvaluatePointDemand(
            pointCode: "P01",
            sku: "DES-01",
            availableStock: 19m,
            inTransitStock: 0m,
            historicalMeanDaily: meanDaily,
            leadTimeDays: leadTimeDays,
            targetCoverageDays: 14);

        Assert.NotNull(order19);
        Assert.True(order19.Triggered);

        // Saldo = 20L exactos -> NO dispara pedido (límite explícito)
        var order20 = policy.EvaluatePointDemand(
            pointCode: "P01",
            sku: "DES-01",
            availableStock: 20m,
            inTransitStock: 0m,
            historicalMeanDaily: meanDaily,
            leadTimeDays: leadTimeDays,
            targetCoverageDays: 14);

        Assert.NotNull(order20);
        Assert.False(order20.Triggered);
        Assert.Equal(0m, order20.RequestedQuantity);
    }

    [Fact]
    public void Order_Quantity_Reaches_14_Days_Deducting_Available_And_InTransit()
    {
        var policy = new ReactivePolicy();
        decimal meanDaily = 10m;
        decimal leadTimeDays = 2m;
        decimal available = 19m;
        decimal inTransit = 1m;

        // meta 14 días = 140L. Cantidad = max(0, 140 - 19 - 1) = 120L
        var decision = policy.EvaluatePointDemand(
            pointCode: "P01",
            sku: "DES-01",
            availableStock: available,
            inTransitStock: inTransit,
            historicalMeanDaily: meanDaily,
            leadTimeDays: leadTimeDays,
            targetCoverageDays: 14);

        Assert.True(decision.Triggered);
        Assert.Equal(120m, decision.RequestedQuantity);
    }

    [Fact]
    public void Without_History_Uses_Dataset_Base_Consumption_And_Records_Assumption()
    {
        var policy = new ReactivePolicy();

        var decision = policy.EvaluatePointDemand(
            pointCode: "P02",
            sku: "DES-01",
            availableStock: 5m,
            inTransitStock: 0m,
            historicalMeanDaily: null, // Sin historial
            leadTimeDays: 2m,
            targetCoverageDays: 14,
            fallbackBaseDaily: 8m);

        Assert.True(decision.Triggered);
        Assert.Equal("assumed_dataset_base_consumption", decision.Assumption);
        // Meta 14 * 8 = 112L. Pedido = 112 - 5 = 107L
        Assert.Equal(107m, decision.RequestedQuantity);
    }

    [Fact]
    public void Priority_Order_Follows_Time_Then_Criticality_Then_Code()
    {
        var items = new List<ReactiveNotice>
        {
            new("P03", "normal", new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc)),
            new("P01", "alta", new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc)),
            new("P02", "alta", new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc)),
            new("P04", "media", new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc))
        };

        var sorted = items.OrderBy(x => x.NoticeTimeUtc)
                          .ThenByDescending(x => x.CriticalityRank)
                          .ThenBy(x => x.PointCode)
                          .Select(x => x.PointCode)
                          .ToList();

        // A las 08:00 hay 3 items: P02 (alta), P04 (media), P03 (normal)
        // A las 09:00 está P01 (alta)
        Assert.Equal(new[] { "P02", "P04", "P03", "P01" }, sorted);
    }
}
