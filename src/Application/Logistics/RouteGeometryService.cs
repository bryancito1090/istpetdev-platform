using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record RouteGeometryWaypoint(string Code, string Name, double Latitude, double Longitude, int Sequence = 0);

// Coordinates: [lat, lon] para Leaflet. Source: "osrm" (por calles) o "approximate" (rectas entre paradas).
public sealed record RouteGeometryResponse(
    Guid RoutePlanId,
    string VehicleCode,
    string Source,
    double? RoadKm,
    IReadOnlyList<double[]> Coordinates,
    RouteGeometryWaypoint? Depot,
    IReadOnlyList<RouteGeometryWaypoint> Stops);

public sealed record RoadPath(double Km, IReadOnlyList<double[]> Coordinates);

public interface IRouteGeometryService
{
    Task<RouteGeometryResponse?> GetAsync(Guid routePlanId, CancellationToken ct = default);

    /// <summary>Recorrido por calles entre puntos [lat, lon] en orden; null si OSRM no está configurado o falla.</summary>
    Task<RoadPath?> RoadPathAsync(IReadOnlyList<double[]> waypoints, CancellationToken ct = default);
}

// Un plan no cambia una vez creado (cada cambio es una revisión nueva), así que su trazado se guarda en memoria.
public sealed class RouteGeometryCache
{
    private readonly ConcurrentDictionary<string, (RouteGeometryResponse Value, DateTime ExpiresUtc)> _items = new();
    private readonly ConcurrentDictionary<string, (RoadPath Value, DateTime ExpiresUtc)> _paths = new();

    // El asistente evalúa varias veces la misma secuencia mientras el usuario edita: no se repite la consulta.
    public bool TryGetPath(string key, out RoadPath value)
    {
        if (_paths.TryGetValue(key, out var item) && item.ExpiresUtc > DateTime.UtcNow) { value = item.Value; return true; }
        value = null!;
        return false;
    }

    public void SetPath(string key, RoadPath value, TimeSpan ttl)
    {
        if (_paths.Count > 500) _paths.Clear();
        _paths[key] = (value, DateTime.UtcNow.Add(ttl));
    }

    public bool TryGet(string key, out RouteGeometryResponse value)
    {
        if (_items.TryGetValue(key, out var item) && item.ExpiresUtc > DateTime.UtcNow) { value = item.Value; return true; }
        value = null!;
        return false;
    }

    public void Set(string key, RouteGeometryResponse value, TimeSpan ttl)
    {
        if (_items.Count > 500) _items.Clear();
        _items[key] = (value, DateTime.UtcNow.Add(ttl));
    }
}

/// <summary>
/// Recorrido por calles de un plan (bodega → paradas → bodega) con el servicio route de OSRM.
/// Sin OSRM configurado o si no responde, devuelve las rectas entre paradas marcadas como aproximadas.
/// </summary>
public sealed class RouteGeometryService(
    IApplicationDbContext db,
    HttpClient http,
    IOptions<LogisticsOptions> options,
    RouteGeometryCache cache,
    ILogger<RouteGeometryService> logger) : IRouteGeometryService
{
    public async Task<RouteGeometryResponse?> GetAsync(Guid routePlanId, CancellationToken ct = default)
    {
        var plan = await db.RoutePlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == routePlanId && p.OrganizationId == DemoOrganization.Id, ct);
        if (plan is null) return null;

        var key = $"{plan.Id}:{plan.Version}";
        if (cache.TryGet(key, out var cached)) return cached;

        var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == plan.VehicleId, ct);
        var warehouses = await db.Locations.AsNoTracking()
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.Position != null)
            .ToListAsync(ct);
        var depotLocation = warehouses.FirstOrDefault(w => vehicle != null && w.Id == vehicle.DepotLocationId) ?? warehouses.FirstOrDefault();
        var depot = depotLocation is null ? null
            : new RouteGeometryWaypoint(depotLocation.Code, depotLocation.Name, depotLocation.Position!.Y, depotLocation.Position.X);

        var stops = await db.RouteStops.AsNoTracking()
            .Where(s => s.RoutePlanId == plan.Id).OrderBy(s => s.SequenceNumber).ToListAsync(ct);
        var pointIds = stops.Select(s => s.ServicePointId).Distinct().ToList();
        var points = await db.ServicePoints.AsNoTracking().Where(p => pointIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var stopLocationIds = stops.Where(s => s.LocationId.HasValue).Select(s => s.LocationId!.Value).Distinct().ToList();
        var stopLocations = await db.Locations.AsNoTracking().Where(l => stopLocationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);

        var waypoints = new List<double[]>();
        var stopWaypoints = new List<RouteGeometryWaypoint>();
        if (depot is not null) waypoints.Add([depot.Latitude, depot.Longitude]);
        foreach (var stop in stops)
        {
            // Parada de recogida en bodega: su ubicación; entrega: el punto de servicio.
            var location = stop.LocationId is { } locationId && stopLocations.TryGetValue(locationId, out var l) ? l : null;
            var point = points.GetValueOrDefault(stop.ServicePointId);
            var position = location?.Position ?? point?.Position;
            if (position is null) continue;
            waypoints.Add([position.Y, position.X]);
            stopWaypoints.Add(new RouteGeometryWaypoint(location?.Code ?? point?.Code ?? "", location?.Name ?? point?.Name ?? "",
                position.Y, position.X, stop.SequenceNumber));
        }
        if (depot is not null && waypoints.Count > 1) waypoints.Add([depot.Latitude, depot.Longitude]);

        var vehicleCode = vehicle?.Code ?? string.Empty;
        var result = await RoadPathAsync(waypoints, ct) is { } road
            ? new RouteGeometryResponse(plan.Id, vehicleCode, "osrm", road.Km, road.Coordinates, depot, stopWaypoints)
            : new RouteGeometryResponse(plan.Id, vehicleCode, "approximate", null, waypoints, depot, stopWaypoints);

        // Un fallo de OSRM se reintenta pronto; un trazado bueno se conserva.
        cache.Set(key, result, result.Source == "osrm" ? TimeSpan.FromHours(12) : TimeSpan.FromMinutes(1));
        return result;
    }

    public async Task<RoadPath?> RoadPathAsync(IReadOnlyList<double[]> waypoints, CancellationToken ct = default)
    {
        var baseUrl = options.Value.Osrm.RouteGeometryUrl;
        if (string.IsNullOrWhiteSpace(baseUrl) || waypoints.Count < 2) return null;

        var path = string.Join(';', waypoints.Select(w =>
            string.Create(CultureInfo.InvariantCulture, $"{w[1]:0.######},{w[0]:0.######}")));
        var url = $"{baseUrl.TrimEnd('/')}/route/v1/driving/{path}?overview=full&geometries=geojson";
        if (cache.TryGetPath(url, out var cachedPath)) return cachedPath;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.Value.Osrm.RouteGeometryTimeoutSeconds)));
            // El servidor público de OSRM responde 403 a peticiones sin User-Agent (HttpClient no envía uno).
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("IstpetDev/1.0 (+https://istpetdev.doicela.dev)");
            using var response = await http.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OSRM route respondió {Status}; se dibujan rectas aproximadas.", (int)response.StatusCode);
                return null;
            }
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cts.Token));
            if (doc.RootElement.GetProperty("code").GetString() != "Ok") return null;
            var route = doc.RootElement.GetProperty("routes")[0];
            var coordinates = route.GetProperty("geometry").GetProperty("coordinates").EnumerateArray()
                .Select(c => new[] { c[1].GetDouble(), c[0].GetDouble() }).ToList();
            if (coordinates.Count < 2) return null;
            var roadPath = new RoadPath(Math.Round(route.GetProperty("distance").GetDouble() / 1000d, 3), coordinates);
            cache.SetPath(url, roadPath, TimeSpan.FromHours(12));
            return roadPath;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            logger.LogWarning("OSRM route no disponible ({Error}); se dibujan rectas aproximadas.", ex.GetType().Name);
            return null;
        }
    }
}
