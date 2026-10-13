using System.Text.Json;
using System.Text.Json.Serialization;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed class DistanceProvider(
    IApplicationDbContext db,
    HttpClient httpClient,
    IOptions<LogisticsOptions> options,
    ILogger<DistanceProvider> logger) : IDistanceProvider
{
    public async Task<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>> MatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default)
    {
        var result = new Dictionary<(GeoPoint From, GeoPoint To), Leg>();
        if (points == null || points.Count == 0) return result;

        var distinctPoints = points.Select(p => p.Round()).Distinct().ToList();

        // 1. Identificar todos los pares dirigidos requeridos (A->B y B->A son distintos)
        var requiredPairs = new HashSet<(GeoPoint From, GeoPoint To)>();
        foreach (var from in distinctPoints)
        {
            foreach (var to in distinctPoints)
            {
                if (from == to)
                {
                    result[(from, to)] = new Leg(from, to, 0m, 0m, DistanceSource.Road, null);
                }
                else
                {
                    requiredPairs.Add((from, to));
                }
            }
        }

        if (requiredPairs.Count == 0) return result;

        // 2. Stored: Cargar de RoadMatrixEdge
        var storedEdges = await db.RoadMatrixEdges
            .Where(e => e.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var storedLookup = storedEdges.ToDictionary(
            e => (new GeoPoint(e.FromLat, e.FromLon).Round(), new GeoPoint(e.ToLat, e.ToLon).Round()),
            e => e);

        var missingPairs = new List<(GeoPoint From, GeoPoint To)>();
        foreach (var pair in requiredPairs)
        {
            if (storedLookup.TryGetValue(pair, out var edge))
            {
                result[pair] = new Leg(pair.From, pair.To, edge.DistanceKm, edge.DurationMinutes, DistanceSource.Road, edge.EncodedPolyline);
            }
            else
            {
                missingPairs.Add(pair);
            }
        }

        // 3. OSRM: Si hay pares faltantes y URL configurada
        var osrmBaseUrl = options.Value.Osrm.BaseUrl;
        if (missingPairs.Count > 0 && !string.IsNullOrWhiteSpace(osrmBaseUrl))
        {
            try
            {
                await QueryOsrmAndSaveAsync(missingPairs, distinctPoints, result, osrmBaseUrl.TrimEnd('/'), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "OSRM query failed or timed out. Falling back to approximation.");
            }
        }

        // 4. Approximation Fallback para lo que siga faltando (sin guardar en base de datos)
        var detourFactor = options.Value.ApproxDetourFactor;
        var speedKmh = options.Value.ApproxSpeedKmh;
        if (speedKmh <= 0) speedKmh = 40m;

        foreach (var pair in requiredPairs)
        {
            if (!result.ContainsKey(pair))
            {
                var haversineKm = HaversineDistanceKm(pair.From, pair.To);
                var km = Math.Round(haversineKm * detourFactor, 3, MidpointRounding.AwayFromZero);
                var hours = km / speedKmh;
                var minutes = Math.Round(hours * 60m, 3, MidpointRounding.AwayFromZero);
                result[pair] = new Leg(pair.From, pair.To, km, minutes, DistanceSource.Approximation, null);
            }
        }

        return result;
    }

    private async Task QueryOsrmAndSaveAsync(
        List<(GeoPoint From, GeoPoint To)> missingPairs,
        List<GeoPoint> allPoints,
        Dictionary<(GeoPoint From, GeoPoint To), Leg> result,
        string osrmBaseUrl,
        CancellationToken ct)
    {
        // OSRM table service: /table/v1/driving/{coords}?annotations=distance,duration
        // Formato coords: {lon},{lat};{lon},{lat}...
        var coordsStr = string.Join(";", allPoints.Select(p => $"{p.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{p.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        var url = $"{osrmBaseUrl}/table/v1/driving/{coordsStr}?annotations=distance,duration";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.Value.Osrm.TimeoutSeconds)));

        var response = await httpClient.GetAsync(url, cts.Token);
        if (!response.IsSuccessStatusCode) return;

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("code", out var codeElem) || codeElem.GetString() != "Ok") return;

        var distances = root.GetProperty("distances");
        var durations = root.GetProperty("durations");

        var newEdgesToPersist = new List<RoadMatrixEdge>();

        for (int i = 0; i < allPoints.Count; i++)
        {
            for (int j = 0; j < allPoints.Count; j++)
            {
                var from = allPoints[i];
                var to = allPoints[j];
                var pair = (from, to);

                if (from == to || !missingPairs.Contains(pair)) continue;

                var distMetersElem = distances[i][j];
                var durSecondsElem = durations[i][j];

                if (distMetersElem.ValueKind != JsonValueKind.Number || durSecondsElem.ValueKind != JsonValueKind.Number)
                    continue;

                var km = Math.Round((decimal)distMetersElem.GetDouble() / 1000m, 3, MidpointRounding.AwayFromZero);
                var minutes = Math.Round((decimal)durSecondsElem.GetDouble() / 60m, 3, MidpointRounding.AwayFromZero);

                var leg = new Leg(from, to, km, minutes, DistanceSource.Road, null);
                result[pair] = leg;

                var edge = new RoadMatrixEdge
                {
                    FromLat = from.Lat,
                    FromLon = from.Lon,
                    ToLat = to.Lat,
                    ToLon = to.Lon,
                    DistanceKm = km,
                    DurationMinutes = minutes,
                    Source = "osrm",
                    RetrievedAtUtc = DateTime.UtcNow
                };
                newEdgesToPersist.Add(edge);
                db.RoadMatrixEdges.Add(edge);
            }
        }

        if (newEdgesToPersist.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    public static decimal HaversineDistanceKm(GeoPoint p1, GeoPoint p2)
    {
        const double EarthRadiusKm = 6371.0;
        double dLat = ToRadians((double)(p2.Lat - p1.Lat));
        double dLon = ToRadians((double)(p2.Lon - p1.Lon));
        double lat1 = ToRadians((double)p1.Lat);
        double lat2 = ToRadians((double)p2.Lat);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (decimal)(EarthRadiusKm * c);
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180.0);
}
