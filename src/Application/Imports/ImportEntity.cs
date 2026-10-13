namespace IstpetDev.Application.Imports;

// El orden del enum es el orden de escritura: cada entidad solo referencia entidades anteriores.
public enum ImportEntity { Warehouses, ServicePoints, Products, Vehicles, Lots, Stock, Consumptions, RoadSegments }

public static class ImportEntityKeys
{
    private static readonly Dictionary<ImportEntity, string> Keys = new()
    {
        [ImportEntity.Warehouses] = "warehouses",
        [ImportEntity.ServicePoints] = "service_points",
        [ImportEntity.Products] = "products",
        [ImportEntity.Vehicles] = "vehicles",
        [ImportEntity.Lots] = "lots",
        [ImportEntity.Stock] = "stock",
        [ImportEntity.Consumptions] = "consumptions",
        [ImportEntity.RoadSegments] = "road_segments",
    };

    private static readonly Dictionary<string, ImportEntity> Aliases = new()
    {
        ["bodegas"] = ImportEntity.Warehouses, ["almacenes"] = ImportEntity.Warehouses,
        ["puntos"] = ImportEntity.ServicePoints, ["clientes"] = ImportEntity.ServicePoints, ["puntos_de_servicio"] = ImportEntity.ServicePoints,
        ["productos"] = ImportEntity.Products, ["catalogo"] = ImportEntity.Products,
        ["vehiculos"] = ImportEntity.Vehicles, ["flota"] = ImportEntity.Vehicles,
        ["lotes"] = ImportEntity.Lots,
        ["inventario"] = ImportEntity.Stock, ["saldos"] = ImportEntity.Stock,
        ["consumos"] = ImportEntity.Consumptions, ["demanda"] = ImportEntity.Consumptions,
        ["tramos"] = ImportEntity.RoadSegments, ["matriz"] = ImportEntity.RoadSegments, ["distancias"] = ImportEntity.RoadSegments,
    };

    public static IReadOnlyCollection<string> All => Keys.Values;

    public static string Key(this ImportEntity entity) => Keys[entity];

    public static bool TryParse(string? text, out ImportEntity entity)
    {
        var normalized = HeaderNormalizer.Normalize(text);
        foreach (var (candidate, key) in Keys)
        {
            if (key != normalized) continue;
            entity = candidate;
            return true;
        }
        return Aliases.TryGetValue(normalized, out entity);
    }
}
