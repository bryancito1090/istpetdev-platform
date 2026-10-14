using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Incidents;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class IncidentEvaluatorTests
{
    [Fact]
    public void Point_to_segment_distance_calculates_correct_distance()
    {
        // Segmento horizontal a lo largo del ecuador: A(0, -78.50) a B(0, -78.40)
        var a = new GeoPoint(0m, -78.50m);
        var b = new GeoPoint(0m, -78.40m);

        // Punto P sobre el segmento pero a 0.01 grados al norte (aprox 1.1 km)
        var p = new GeoPoint(0.01m, -78.45m);

        var dist = IncidentEvaluator.PointToSegmentDistanceKm(p, a, b);

        Assert.InRange(dist, 1.0m, 1.2m);
    }

    [Fact]
    public void IsLegAffected_detects_polyline_crossing_circle_even_when_endpoints_are_outside()
    {
        // Extremos A y B lejos del círculo
        var a = new GeoPoint(-0.10m, -78.50m);
        var b = new GeoPoint(-0.30m, -78.50m);

        // Círculo del incidente en el centro (-0.20, -78.50) con radio 0.5 km
        var incidentCenter = new GeoPoint(-0.20m, -78.50m);
        decimal radiusKm = 0.5m;

        // Caso 1: Polilínea que pasa justo por el centro
        var midPoint = new GeoPoint(-0.20m, -78.50m);
        var polyCrossing = Polyline.Encode(new[] { a, midPoint, b });
        var legCrossing = new Leg(a, b, 25m, 35m, DistanceSource.Road, polyCrossing);

        Assert.True(IncidentEvaluator.IsLegAffected(legCrossing, incidentCenter, radiusKm));

        // Caso 2: Tramo recto que no pasa cerca del centro (desviado al este)
        var c = new GeoPoint(-0.10m, -78.40m);
        var d = new GeoPoint(-0.30m, -78.40m);
        var legFar = new Leg(c, d, 25m, 35m, DistanceSource.Road, null);

        // Distancia ~ 11 km > radio 0.5 km
        Assert.False(IncidentEvaluator.IsLegAffected(legFar, incidentCenter, radiusKm));
    }

    [Fact]
    public void IsLegAffected_correctly_excludes_leg_just_outside_radius()
    {
        // Punto incidente en (-0.2000, -78.5000), radio 1.0 km
        var center = new GeoPoint(-0.2000m, -78.5000m);
        decimal radiusKm = 1.0m;

        // Tramo recto paralelo a 1.5 km de distancia
        // 1.5 km en latitud es aprox 1.5 / 110.574 ~ 0.01356 grados
        var a = new GeoPoint(-0.2000m + 0.015m, -78.5200m);
        var b = new GeoPoint(-0.2000m + 0.015m, -78.4800m);
        var leg = new Leg(a, b, 5m, 8m, DistanceSource.Road, null);

        Assert.False(IncidentEvaluator.IsLegAffected(leg, center, radiusKm));
    }
}
