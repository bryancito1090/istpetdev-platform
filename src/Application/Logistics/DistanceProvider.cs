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
    public async Task<IReadOnlyDictionary<(GeoPoint From, GeoPoint To), Leg>> MatrixAsync(IReadOnlyList<GeoPoint> points, CancellationToken ct = default, bool persistCache = true)
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
            .AsNoTracking()
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
                await QueryOsrmAndSaveAsync(missingPairs, distinctPoints, result, osrmBaseUrl.TrimEnd('/'), persistCache, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "OSRM query failed or timed out. Falling back to approximation.");
            }

            var stillMissing = missingPairs.Count(pair => !result.ContainsKey(pair));
            if (stillMissing > 0)
                logger.LogWarning("OSRM no devolvió {Missing} de {Requested} tramos; se usa la aproximación en línea recta para ellos.",
                    stillMissing, missingPairs.Count);
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

    // El servidor público de OSRM admite hasta 100 coordenadas por petición de tabla (max-table-size).
    private const int OsrmMaxTableCoordinates = 100;

    private async Task QueryOsrmAndSaveAsync(
        List<(GeoPoint From, GeoPoint To)> missingPairs,
        List<GeoPoint> allPoints,
        Dictionary<(GeoPoint From, GeoPoint To), Leg> result,
        string osrmBaseUrl,
        bool persistCache,
        CancellationToken ct)
    {
        var missing = missingPairs.ToHashSet();
        var newEdgesToPersist = new List<RoadMatrixEdge>();

        // Con más de 100 puntos se pide la tabla por bloques: cada petición lleva un bloque de orígenes y uno
        // de destinos (como máximo 100 coordenadas) y solo los pares que todavía faltan.
        var blockSize = allPoints.Count <= OsrmMaxTableCoordinates ? allPoints.Count : OsrmMaxTableCoordinates / 2;
        var blocks = allPoints.Chunk(blockSize).ToList();
        foreach (var sources in blocks)
        foreach (var destinations in blocks)
        {
            if (!sources.Any(f => destinations.Any(t => missing.Contains((f, t))))) continue;
            await QueryOsrmBlockAsync(sources, destinations, missing, result, newEdgesToPersist, osrmBaseUrl, persistCache, ct);
        }

        if (persistCache && newEdgesToPersist.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task QueryOsrmBlockAsync(
        GeoPoint[] sources,
        GeoPoint[] destinations,
        HashSet<(GeoPoint From, GeoPoint To)> missing,
        Dictionary<(GeoPoint From, GeoPoint To), Leg> result,
        List<RoadMatrixEdge> newEdgesToPersist,
        string osrmBaseUrl,
        bool persistCache,
        CancellationToken ct)
    {
        var points = sources.Concat(destinations).Distinct().ToList();
        var index = points.Select((point, i) => (point, i)).ToDictionary(x => x.point, x => x.i);
        // OSRM table service: /table/v1/driving/{lon,lat;...}?sources=..&destinations=..&annotations=distance,duration
        var coordsStr = string.Join(";", points.Select(p => $"{p.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{p.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
        var url = $"{osrmBaseUrl}/table/v1/driving/{coordsStr}?annotations=distance,duration" +
                  $"&sources={string.Join(';', sources.Select(p => index[p]))}&destinations={string.Join(';', destinations.Select(p => index[p]))}";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.Value.Osrm.TimeoutSeconds)));

        // El servidor público de OSRM responde 403 a peticiones sin User-Agent (HttpClient no envía uno).
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("IstpetDev/1.0 (+https://istpetdev.doicela.dev)");
        using var response = await httpClient.SendAsync(request, cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("OSRM table respondió {Status}.", (int)response.StatusCode);
            return;
        }

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("code", out var codeElem) || codeElem.GetString() != "Ok") return;

        var distances = root.GetProperty("distances");
        var durations = root.GetProperty("durations");

        for (int i = 0; i < sources.Length; i++)
        {
            for (int j = 0; j < destinations.Length; j++)
            {
                var from = sources[i];
                var to = destinations[j];
                var pair = (from, to);

                if (from == to || !missing.Contains(pair) || result.ContainsKey(pair)) continue;

                var distMetersElem = distances[i][j];
                var durSecondsElem = durations[i][j];

                if (distMetersElem.ValueKind != JsonValueKind.Number || durSecondsElem.ValueKind != JsonValueKind.Number)
                    continue;

                var km = Math.Round((decimal)distMetersElem.GetDouble() / 1000m, 3, MidpointRounding.AwayFromZero);
                var minutes = Math.Round((decimal)durSecondsElem.GetDouble() / 60m, 3, MidpointRounding.AwayFromZero);

                var leg = new Leg(from, to, km, minutes, DistanceSource.Road, null);
                result[pair] = leg;

                if (!persistCache) continue;

                var edge = new RoadMatrixEdge
                {
                    OrganizationId = DemoOrganization.Id,
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
