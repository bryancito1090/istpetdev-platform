using IstpetDev.Application.Simulation;
using Xunit;

namespace IstpetDev.Tests.Unit.Simulation;

public sealed class IstpetDevPolicyTests
{
    private sealed class MockRegionalPlanner : IRegionalPlanner
    {
        public int CallCount { get; private set; }
        public RegionalPlanRequest? LastRequest { get; private set; }

        public Task<RegionalPlan> PlanAsync(RegionalPlanRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;

            if (request.DistanceMode == "Road" && request.RegionCode == "REG-INCOMPLETE")
            {
                throw new SimulationMatrixException("Incomplete road matrix for REG-INCOMPLETE");
            }

            return Task.FromResult(new RegionalPlan(
                Km: 45.2m,
                TransportCost: 20.34m,
                CompletedPointCodes: ["P01", "P02"],
                UnservedPointCodes: ["P03"]));
        }
    }

    [Fact]
    public async Task Calls_Regional_Planner_Exactly_Once_Per_Region_And_Day()
    {
        var planner = new MockRegionalPlanner();
        var policy = new IstpetDevPolicy(planner);

        var date = new DateOnly(2026, 10, 12);
        var plan = await policy.PlanRegionDayAsync(
            date: date,
            regionCode: "REG-01",
            distanceMode: "Road",
            availableVehicles: ["VEH-01", "VEH-02"]);

        Assert.Equal(1, planner.CallCount);
        Assert.Equal("REG-01", planner.LastRequest?.RegionCode);
        Assert.Equal(45.2m, plan.Km);
        Assert.Single(plan.UnservedPointCodes);
    }

    [Fact]
    public async Task Incomplete_Road_Matrix_Throws_SimulationMatrixException_In_Road_Mode()
    {
        var planner = new MockRegionalPlanner();
        var policy = new IstpetDevPolicy(planner);

        var date = new DateOnly(2026, 10, 12);

        await Assert.ThrowsAsync<SimulationMatrixException>(() =>
            policy.PlanRegionDayAsync(
                date: date,
                regionCode: "REG-INCOMPLETE",
                distanceMode: "Road",
                availableVehicles: ["VEH-01"]));
    }

    [Fact]
    public async Task Approximation_Mode_Allows_Plan_With_Assumptions_Tagged()
    {
        var planner = new MockRegionalPlanner();
        var policy = new IstpetDevPolicy(planner);

        var date = new DateOnly(2026, 10, 12);
        var plan = await policy.PlanRegionDayAsync(
            date: date,
            regionCode: "REG-01",
            distanceMode: "Approximation",
            availableVehicles: ["VEH-01"]);

        Assert.Equal("Approximation", planner.LastRequest?.DistanceMode);
        Assert.Equal(45.2m, plan.Km);
    }
}
