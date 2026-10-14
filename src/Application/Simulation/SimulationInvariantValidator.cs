namespace IstpetDev.Application.Simulation;

public sealed record StockMovementRecord(
    string LocationCode,
    string LotCode,
    string Sku,
    decimal Initial,
    decimal Inflow,
    decimal Outflow,
    decimal Final);

public sealed record DemandBalanceRecord(
    string PointCode,
    string Sku,
    decimal Requested,
    decimal Satisfied,
    decimal Unserved);

public sealed record InvariantValidationResult(bool IsValid, IReadOnlyList<string> Violations);

public static class SimulationInvariantValidator
{
    public static InvariantValidationResult ValidateStockBalance(IReadOnlyList<StockMovementRecord> movements)
    {
        var violations = new List<string>();

        foreach (var m in movements)
        {
            var calculatedFinal = m.Initial + m.Inflow - m.Outflow;
            if (calculatedFinal != m.Final)
            {
                violations.Add($"Stock balance mismatch at {m.LocationCode} for SKU {m.Sku} (Lot: {m.LotCode}): Expected {calculatedFinal}, observed {m.Final}");
            }
        }

        return new InvariantValidationResult(violations.Count == 0, violations);
    }

    public static InvariantValidationResult ValidateDemandBalance(IReadOnlyList<DemandBalanceRecord> demands)
    {
        var violations = new List<string>();

        foreach (var d in demands)
        {
            var sum = d.Satisfied + d.Unserved;
            if (sum != d.Requested)
            {
                violations.Add($"Demand balance mismatch at {d.PointCode} for SKU {d.Sku}: Requested {d.Requested} != Satisfied ({d.Satisfied}) + Unserved ({d.Unserved})");
            }
        }

        return new InvariantValidationResult(violations.Count == 0, violations);
    }
}
