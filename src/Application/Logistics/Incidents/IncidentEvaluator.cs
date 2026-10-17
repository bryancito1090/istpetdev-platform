using System.Text.Json;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Logistics.Incidents;

public sealed record AffectedStop(string PointCode, int SequenceNumber);

public sealed record AffectedRoute(
    Guid RoutePlanId,
    string VehicleCode,
    IReadOnlyList<(GeoPoint From, GeoPoint To)> AffectedLegs,
    IReadOnlyList<AffectedStop> AffectedStops);

public sealed record IncidentImpact(
    Guid IncidentId,
    IReadOnlyList<AffectedRoute> AffectedRoutes,
    IncidentEffect Effect);

public interface IIncidentEvaluator
{
    Task<IncidentImpact> EvaluateAsync(Guid incidentId, CancellationToken ct = default);
}

public sealed class IncidentEvaluator(IApplicationDbContext db) : IIncidentEvaluator
{
    public async Task<IncidentImpact> EvaluateAsync(Guid incidentId, CancellationToken ct = default)
    {
        var incident = await db.Incidents
            .FirstOrDefaultAsync(i => i.Id == incidentId && i.OrganizationId == DemoOrganization.Id, ct)
            ?? throw new KeyNotFoundException($"Incidente {incidentId} no encontrado.");

        var publishedPlans = await db.RoutePlans
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.Status == "Published")
            .ToListAsync(ct);

        var blockedLegs = new HashSet<(GeoPoint, GeoPoint)>();
        var slowFactors = new Dictionary<(GeoPoint, GeoPoint), decimal>();
        var unavailableVehicles = new HashSet<string>();
        var affectedRoutes = new List<AffectedRoute>();

        var vehicles = await db.Vehicles
            .Where(v => v.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(v => v.Id, v => v, ct);

        // Caso VEHICLE_BREAKDOWN: Vehículo queda no disponible
        if (string.Equals(incident.Kind, "VEHICLE_BREAKDOWN", StringComparison.OrdinalIgnoreCase))
        {
            Vehicle? breakdownVehicle = null;
            if (incident.RoutePlanId.HasValue)
            {
                var plan = publishedPlans.FirstOrDefault(p => p.Id == incident.RoutePlanId.Value);
                if (plan != null && vehicles.TryGetValue(plan.VehicleId, out var v))
                {
                    breakdownVehicle = v;
                }
            }

            if (breakdownVehicle != null)
            {
                unavailableVehicles.Add(breakdownVehicle.Code);

                var affectedPlan = publishedPlans.First(p => p.VehicleId == breakdownVehicle.Id);
                var pendingStops = await db.RouteStops
                    .Where(s => s.OrganizationId == DemoOrganization.Id && s.RoutePlanId == affectedPlan.Id && s.Status != "Completed")
                    .OrderBy(s => s.SequenceNumber)
                    .ToListAsync(ct);

                var spIds = pendingStops.Select(s => s.ServicePointId).ToHashSet();
                var spMap = await db.ServicePoints
                    .Where(s => s.OrganizationId == DemoOrganization.Id && spIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s.Code, ct);

                affectedRoutes.Add(new AffectedRoute(
                    affectedPlan.Id,
                    breakdownVehicle.Code,
                    Array.Empty<(GeoPoint, GeoPoint)>(),
                    pendingStops.Select(s => new AffectedStop(spMap.GetValueOrDefault(s.ServicePointId, ""), s.SequenceNumber)).ToList()));
            }

            return new IncidentImpact(
                incident.Id,
                affectedRoutes,
                new IncidentEffect(blockedLegs, slowFactors, unavailableVehicles));
        }

        // Casos ROAD_CLOSURE y DELAY: Intersección espacial
        if (incident.Position == null || !incident.RadiusKm.HasValue)
        {
            return new IncidentImpact(incident.Id, Array.Empty<AffectedRoute>(), new IncidentEffect(blockedLegs, slowFactors, unavailableVehicles));
        }

        var incidentCenter = new GeoPoint((decimal)incident.Position.Y, (decimal)incident.Position.X);
        decimal radiusKm = incident.RadiusKm.Value;
        bool isClosure = string.Equals(incident.Kind, "ROAD_CLOSURE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(incident.Kind, "BloqueoVial", StringComparison.OrdinalIgnoreCase);
        decimal delayFactor = 2.0m; // Factor por defecto para demoras
        // #246: Congestion, Manifestacion y Accidente ralentizan según su SlowdownFactor guardado.
        // Sin factor guardado se conserva el comportamiento anterior (demora × 2).
        var slowdown = !isClosure && IncidentSlowdown.IsSlowdownKind(incident.Kind)
            ? IncidentSlowdown.ReadFactor(incident.Description) : null;
        if (slowdown is { } speedFactor) delayFactor = IncidentSlowdown.MinutesMultiplier(speedFactor);

        // Also exclude affected cached legs from alternative orders, rather than
        // permitting a new leg through the same closure simply because it was
        // absent from the previous route.
        // #246: lo mismo para un retraso parcial, para que otro orden por la misma zona también sea lento.
        if (isClosure || slowdown.HasValue)
        {
            var edges = await db.RoadMatrixEdges.Where(e => e.OrganizationId == DemoOrganization.Id).ToListAsync(ct);
            foreach (var edge in edges)
            {
                var leg = new Leg(new GeoPoint(edge.FromLat, edge.FromLon), new GeoPoint(edge.ToLat, edge.ToLon),
                    edge.DistanceKm, edge.DurationMinutes, DistanceSource.Road, edge.EncodedPolyline);
                if (!IsLegAffected(leg, incidentCenter, radiusKm)) continue;
                if (isClosure) blockedLegs.Add((leg.From, leg.To));
                else slowFactors[(leg.From, leg.To)] = delayFactor;
            }
        }

        foreach (var plan in publishedPlans)
        {
            if (!vehicles.TryGetValue(plan.VehicleId, out var vehicle)) continue;

            // Deserializar legs almacenados en plan.InputJson o reconstruir de paradas
            var legsInRoute = ExtractLegsFromPlan(plan);
            var routeAffectedLegs = new List<(GeoPoint, GeoPoint)>();
            var routeAffectedStops = new List<AffectedStop>();

            var stops = await db.RouteStops
                .Where(s => s.OrganizationId == DemoOrganization.Id && s.RoutePlanId == plan.Id)
                .OrderBy(s => s.SequenceNumber)
                .ToListAsync(ct);

            var spMap = await db.ServicePoints
                .Where(s => s.OrganizationId == DemoOrganization.Id)
                .ToDictionaryAsync(s => s.Id, s => s, ct);

            var completedPrefixLength = stops.TakeWhile(s => s.Status == "Completed").Count();
            foreach (var leg in legsInRoute.Skip(completedPrefixLength))
            {
                if (IsLegAffected(leg, incidentCenter, radiusKm))
                {
                    var pair = (leg.From, leg.To);
                    routeAffectedLegs.Add(pair);

                    if (isClosure)
                    {
                        blockedLegs.Add(pair);
                    }
                    else
                    {
                        slowFactors[pair] = delayFactor;
                    }

                    // Identificar parada de llegada afectada si corresponde
                    var targetStop = stops.FirstOrDefault(s =>
                        spMap.TryGetValue(s.ServicePointId, out var sp) &&
                        sp.Position != null &&
                        Math.Round((decimal)sp.Position.Y, 5) == leg.To.Lat &&
                        Math.Round((decimal)sp.Position.X, 5) == leg.To.Lon);

                    if (targetStop != null && targetStop.Status != "Completed")
                    {
                        routeAffectedStops.Add(new AffectedStop(spMap[targetStop.ServicePointId].Code, targetStop.SequenceNumber));
                    }
                }
            }

            if (routeAffectedLegs.Count > 0)
            {
                affectedRoutes.Add(new AffectedRoute(
                    plan.Id,
                    vehicle.Code,
                    routeAffectedLegs,
                    routeAffectedStops.DistinctBy(s => s.SequenceNumber).ToList()));
            }
        }

        return new IncidentImpact(
            incident.Id,
            affectedRoutes,
            new IncidentEffect(blockedLegs, slowFactors, unavailableVehicles));
    }

    public static bool IsLegAffected(Leg leg, GeoPoint center, decimal radiusKm)
    {
        // 1. Si tiene polyline decodificada, probar cada segmento contiguo
        if (!string.IsNullOrEmpty(leg.EncodedPolyline))
        {
            var polyPoints = Polyline.Decode(leg.EncodedPolyline);
            for (int i = 0; i < polyPoints.Count - 1; i++)
            {
                if (PointToSegmentDistanceKm(center, polyPoints[i], polyPoints[i + 1]) <= radiusKm)
                {
                    return true;
                }
            }
            return false;
        }

        // 2. Si no tiene polyline (aproximación), usar el segmento recto From -> To
        return PointToSegmentDistanceKm(center, leg.From, leg.To) <= radiusKm;
    }

    // Distancia punto a segmento en proyección local plana equirectangular
    public static decimal PointToSegmentDistanceKm(GeoPoint p, GeoPoint a, GeoPoint b)
    {
        decimal midLatRad = (decimal)((double)(a.Lat + b.Lat) / 2.0 * Math.PI / 180.0);
        decimal cosLat = (decimal)Math.Cos((double)midLatRad);

        // Coordenadas locales en km (1 grado latitud ~ 110.574 km, 1 grado longitud ~ 111.320 * cos(lat) km)
        decimal kLat = 110.574m;
        decimal kLon = 111.320m * cosLat;

        decimal px = p.Lon * kLon;
        decimal py = p.Lat * kLat;
        decimal ax = a.Lon * kLon;
        decimal ay = a.Lat * kLat;
        decimal bx = b.Lon * kLon;
        decimal by = b.Lat * kLat;

        decimal dx = bx - ax;
        decimal dy = by - ay;
        decimal segLenSq = dx * dx + dy * dy;

        if (segLenSq == 0m)
        {
            decimal dpx = px - ax;
            decimal dpy = py - ay;
            return (decimal)Math.Sqrt((double)(dpx * dpx + dpy * dpy));
        }

        decimal t = ((px - ax) * dx + (py - ay) * dy) / segLenSq;
        t = Math.Clamp(t, 0m, 1m);

        decimal projX = ax + t * dx;
        decimal projY = ay + t * dy;

        decimal diffX = px - projX;
        decimal diffY = py - projY;

        return (decimal)Math.Sqrt((double)(diffX * diffX + diffY * diffY));
    }

    private static List<Leg> ExtractLegsFromPlan(RoutePlan plan)
    {
        var list = new List<Leg>();
        try
        {
            var doc = JsonDocument.Parse(plan.InputJson);
            if (doc.RootElement.TryGetProperty("legs", out var legsElem) && legsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var l in legsElem.EnumerateArray())
                {
                    var fLat = l.GetProperty("from").GetProperty("lat").GetDecimal();
                    var fLon = l.GetProperty("from").GetProperty("lon").GetDecimal();
                    var tLat = l.GetProperty("to").GetProperty("lat").GetDecimal();
                    var tLon = l.GetProperty("to").GetProperty("lon").GetDecimal();
                    var km = l.GetProperty("km").GetDecimal();
                    var min = l.GetProperty("minutes").GetDecimal();
                    var poly = l.TryGetProperty("encodedPolyline", out var p) ? p.GetString() : null;

                    list.Add(new Leg(
                        new GeoPoint(fLat, fLon),
                        new GeoPoint(tLat, tLon),
                        km, min, DistanceSource.Road, poly));
                }
            }
        }
        catch
        {
            // Json vacío o simplificado
        }
        return list;
    }
}
