namespace IstpetDev.Application.Imports;

public static class FeasibilityVerdicts
{
    public const string Viable = "Viable";
    public const string ViableWithLimitations = "ViableWithLimitations";
    public const string Inviable = "Inviable";
}

public sealed record UnroutablePoint(string Code, string Name, int? Row);

public sealed record WarehouseWithoutFleet(string Code, string Name);

public sealed record ProductWithoutCapacity(string Sku, string Name, string Reason);

public sealed record StockShortage(string Sku, string Name, string BaseUnit, decimal Demanded, decimal Available);

// Issue #209: hechos deterministas de viabilidad logística para el dictamen de n8n.
public sealed class FeasibilityReport
{
    public string Verdict { get; set; } = FeasibilityVerdicts.Viable;
    public List<UnroutablePoint> UnroutablePoints { get; } = new();
    public List<WarehouseWithoutFleet> WarehousesWithoutFleet { get; } = new();
    public List<ProductWithoutCapacity> ProductsWithoutCapacity { get; } = new();
    public List<StockShortage> StockShortages { get; } = new();

    public bool HasFindings =>
        UnroutablePoints.Count > 0 || WarehousesWithoutFleet.Count > 0 ||
        ProductsWithoutCapacity.Count > 0 || StockShortages.Count > 0;
}
