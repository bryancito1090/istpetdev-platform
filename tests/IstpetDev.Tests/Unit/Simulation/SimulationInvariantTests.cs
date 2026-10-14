using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class SimulationInvariantTests
{
    [Fact]
    public void Stock_Balance_Invariant_Holds_When_Initial_Plus_In_Minus_Out_Equals_Final()
    {
        var records = new List<StockMovementRecord>
        {
            new("BOD-01", "LOT-01", "DES-01", Initial: 100m, Inflow: 50m, Outflow: 30m, Final: 120m)
        };

        var result = SimulationInvariantValidator.ValidateStockBalance(records);

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Injected_Extra_Outflow_Of_1L_Violates_Invariant_And_Fails()
    {
        // 100 + 50 - 31 != 120 (faltó 1L)
        var records = new List<StockMovementRecord>
        {
            new("BOD-01", "LOT-01", "DES-01", Initial: 100m, Inflow: 50m, Outflow: 31m, Final: 120m)
        };

        var result = SimulationInvariantValidator.ValidateStockBalance(records);

        Assert.False(result.IsValid);
        Assert.Single(result.Violations);
        Assert.Contains("Stock balance mismatch", result.Violations[0]);
    }

    [Fact]
    public void Demand_Invariant_Demands_Requested_Equals_Satisfied_Plus_Unserved()
    {
        var demands = new List<DemandBalanceRecord>
        {
            new("P01", "DES-01", Requested: 10m, Satisfied: 8m, Unserved: 2m)
        };

        var result = SimulationInvariantValidator.ValidateDemandBalance(demands);
        Assert.True(result.IsValid);

        // Falla si Requested != Satisfied + Unserved
        var invalidDemands = new List<DemandBalanceRecord>
        {
            new("P01", "DES-01", Requested: 10m, Satisfied: 8m, Unserved: 1m)
        };

        var invalidResult = SimulationInvariantValidator.ValidateDemandBalance(invalidDemands);
        Assert.False(invalidResult.IsValid);
    }
}
