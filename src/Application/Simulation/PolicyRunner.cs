namespace IstpetDev.Application.Simulation;

public sealed record SimulationInitialState(
    Dictionary<(string Location, string Sku), decimal> WarehouseStock,
    Dictionary<(string Location, string Sku), decimal> PointStock)
{
    public SimulationInitialState DeepCopy()
    {
        return new SimulationInitialState(
            new Dictionary<(string Location, string Sku), decimal>(WarehouseStock),
            new Dictionary<(string Location, string Sku), decimal>(PointStock));
    }
}

public sealed class PolicyRunner
{
    public string PolicyVersion { get; }
    public ScenarioCalendar Calendar { get; }
    public SimulationInitialState State { get; }

    public string CalendarSha256 => Calendar.Sha256;

    public PolicyRunner(string policyVersion, ScenarioCalendar calendar, SimulationInitialState state)
    {
        PolicyVersion = policyVersion;
        Calendar = calendar;
        State = state;
    }

    public decimal GetStock(string location, string sku)
    {
        if (State.WarehouseStock.TryGetValue((location, sku), out var wQty)) return wQty;
        if (State.PointStock.TryGetValue((location, sku), out var pQty)) return pQty;
        return 0m;
    }

    public void MutateStockForTest(string location, string sku, decimal delta)
    {
        if (State.WarehouseStock.ContainsKey((location, sku)))
        {
            State.WarehouseStock[(location, sku)] += delta;
        }
        else if (State.PointStock.ContainsKey((location, sku)))
        {
            State.PointStock[(location, sku)] += delta;
        }
        else
        {
            State.WarehouseStock[(location, sku)] = delta;
        }
    }
}
