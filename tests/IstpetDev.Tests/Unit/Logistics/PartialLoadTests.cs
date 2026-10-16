using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Entities;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

// #199: recorte de cargas a la capacidad libre en peso y volumen.
public sealed class PartialLoadTests
{
    private static readonly Product Heavy = new() { Sku = "A", BaseUnit = "u", WeightPerBaseUnit = 1m, VolumePerBaseUnit = 0.001m };
    private static readonly Product Bulky = new() { Sku = "B", BaseUnit = "u", WeightPerBaseUnit = 0.1m, VolumePerBaseUnit = 0.1m };
    private static readonly Dictionary<Guid, Product> Products = new() { [Heavy.Id] = Heavy, [Bulky.Id] = Bulky };

    [Fact]
    public void A_line_that_does_not_fit_is_cut_to_whole_units_of_free_weight()
    {
        var fitted = RoutePlanner.FitLoads([new StopLoad(Heavy.Id, "A", 150m, "u")], 100m, 10m, Products);

        Assert.Equal(100m, Assert.Single(fitted).Quantity);
    }

    [Fact]
    public void Lines_are_filled_in_order_and_volume_also_limits()
    {
        var fitted = RoutePlanner.FitLoads(
            [new StopLoad(Heavy.Id, "A", 150m, "u"), new StopLoad(Bulky.Id, "B", 150m, "u")], 200m, 5m, Products);

        Assert.Equal(150m, fitted[0].Quantity);
        Assert.Equal(48m, fitted[1].Quantity); // 4,85 m³ libres ÷ 0,1 = 48,5 → 48 unidades enteras
    }

    [Fact]
    public void Without_free_capacity_nothing_is_taken_and_without_limits_everything_is()
    {
        Assert.Empty(RoutePlanner.FitLoads([new StopLoad(Heavy.Id, "A", 5m, "u")], 0m, 10m, Products));
        Assert.Equal(5.5m, RoutePlanner.FitLoads([new StopLoad(Heavy.Id, "A", 5.5m, "u")], null, null, Products).Single().Quantity);
    }
}
