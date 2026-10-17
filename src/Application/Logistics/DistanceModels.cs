namespace IstpetDev.Application.Logistics;

public enum DistanceSource { Road, Approximation }

public readonly record struct GeoPoint(decimal Lat, decimal Lon)
{
    public GeoPoint Round() => new(Math.Round(Lat, 5), Math.Round(Lon, 5));
}

public sealed record Leg(
    GeoPoint From,
    GeoPoint To,
    decimal Km,
    decimal Minutes,
    DistanceSource Source,
    string? EncodedPolyline);

public interface IDistanceProvider
{
    Task<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>> MatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default, bool persistCache = true);
}
