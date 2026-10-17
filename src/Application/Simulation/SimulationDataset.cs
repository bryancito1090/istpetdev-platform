using IstpetDev.Application.Logistics;

namespace IstpetDev.Application.Simulation;

public sealed record SimulationPoint(
    Guid Id,
    Guid LocationId,
    string Code,
    string Region,
    string Criticality,
    GeoPoint Position,
    TimeOnly WindowStart,
    TimeOnly WindowEnd,
    int ServiceMinutes,
    decimal LeadTimeDays);

public sealed record SimulationProduct(
    Guid Id,
    string Sku,
    string Unit,
    decimal WeightPerUnit,
    decimal VolumePerUnit,
    decimal? UnitValue,
    decimal? UnitsPerPackage);

public sealed record SimulationVehicle(
    Guid Id,
    string Code,
    Guid DepotLocationId,
    string DepotCode,
    decimal MaxWeight,
    decimal? MaxVolume,
    decimal? CostPerKm,
    string? VehicleType);

public sealed record DemandProfile(
    string PointCode,
    string Region,
    Guid ProductId,
    string Sku,
    string Unit,
    IReadOnlyDictionary<DayOfWeek, decimal> MeanByWeekday,
    decimal Volatility)
{
    public decimal AverageDailyDemand =>
        MeanByWeekday.Count == 0 ? 0m : MeanByWeekday.Values.Sum() / 7m;

    public decimal MeanFor(DayOfWeek day) =>
        MeanByWeekday.TryGetValue(day, out var value) ? value : AverageDailyDemand;
}

public sealed record RoadEdge(decimal DistanceKm, decimal DurationMinutes);

public sealed record SimulationDataset(
    string Version,
    string Hash,
    DateOnly StartDate,
    int HorizonDays,
    IReadOnlyList<SimulationPoint> Points,
    IReadOnlyList<SimulationProduct> Products,
    IReadOnlyList<SimulationVehicle> Vehicles,
    IReadOnlyList<DemandProfile> DemandProfiles,
    IReadOnlyDictionary<(string PointCode, string Sku), decimal> InitialPointStock,
    IReadOnlyDictionary<(string WarehouseCode, string Sku), decimal> InitialWarehouseStock,
    IReadOnlyDictionary<string, string> PointWarehouse,
    IReadOnlyDictionary<string, GeoPoint> WarehousePositions,
    IReadOnlyDictionary<(decimal FromLat, decimal FromLon, decimal ToLat, decimal ToLon), RoadEdge> RoadMatrix,
    string? AdverseRetiredVehicle)
{
    public bool HasRoadMatrix => RoadMatrix.Count > 0;
}

public sealed class SimulationDatasetException(string message) : InvalidOperationException(message);

public sealed class SimulationDatasetChangedException(string message) : InvalidOperationException(message);

public static class SimulationGeo
{
    public static decimal Haversine(GeoPoint a, GeoPoint b)
    {
        const double radians = Math.PI / 180.0;
        const double earthKm = 6371.0;
        var dLat = (double)(b.Lat - a.Lat) * radians;
        var dLon = (double)(b.Lon - a.Lon) * radians;
        var lat1 = (double)a.Lat * radians;
        var lat2 = (double)b.Lat * radians;
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        return (decimal)(2 * earthKm * Math.Asin(Math.Sqrt(h)));
    }
}
