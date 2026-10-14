namespace IstpetDev.Application.Simulation;

public sealed class SimulationMatrixException : Exception
{
    public SimulationMatrixException(string message) : base(message) { }
}

public sealed class IstpetDevPolicy
{
    public const string Version = "istpetdev-v1";
    private readonly IRegionalPlanner _planner;

    public IstpetDevPolicy(IRegionalPlanner planner)
    {
        _planner = planner;
    }

    public async Task<RegionalPlan> PlanRegionDayAsync(
        DateOnly date,
        string regionCode,
        string distanceMode,
        IReadOnlyList<string> availableVehicles,
        CancellationToken cancellationToken = default)
    {
        var request = new RegionalPlanRequest(
            Date: date,
            RegionCode: regionCode,
            DistanceMode: distanceMode,
            AvailableVehicleCodes: availableVehicles);

        return await _planner.PlanAsync(request, cancellationToken);
    }
}
