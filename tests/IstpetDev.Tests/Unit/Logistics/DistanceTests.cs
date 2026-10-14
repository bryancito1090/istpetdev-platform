using IstpetDev.Application.Logistics;
using Xunit;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public sealed class DistanceTests
{
    [Fact]
    public void Polyline_encodes_and_decodes_known_google_example_and_checks_endpoints()
    {
        // Google example: (38.5, -120.2), (40.7, -120.95), (43.252, -126.453)
        // Encoded string: "_p~iF~ps|U_ulLnnqC_mqNvxq`@"
        var p1 = new GeoPoint(38.5m, -120.2m);
        var p2 = new GeoPoint(40.7m, -120.95m);
        var p3 = new GeoPoint(43.252m, -126.453m);

        var points = new[] { p1, p2, p3 };
        var encoded = Polyline.Encode(points);
        Assert.Equal("_p~iF~ps|U_ulLnnqC_mqNvxq`@", encoded);

        var decoded = Polyline.Decode("_p~iF~ps|U_ulLnnqC_mqNvxq`@");
        Assert.Equal(3, decoded.Count);
        Assert.Equal(p1.Lat, decoded[0].Lat);
        Assert.Equal(p1.Lon, decoded[0].Lon);
        Assert.Equal(p3.Lat, decoded[2].Lat);
        Assert.Equal(p3.Lon, decoded[2].Lon);
    }

    [Fact]
    public void Haversine_computes_approximate_spherical_distance()
    {
        // Quito (-0.18065, -78.46783) a Guayaquil (-2.18941, -79.88907) ~ 268-275 km en línea recta
        var quito = new GeoPoint(-0.18065m, -78.46783m);
        var guayaquil = new GeoPoint(-2.18941m, -79.88907m);

        var dist = DistanceProvider.HaversineDistanceKm(quito, guayaquil);
        Assert.InRange(dist, 260m, 280m);
    }

    [Fact]
    public void Directed_pairs_A_to_B_does_not_imply_B_to_A()
    {
        var pA = new GeoPoint(-0.20000m, -78.50000m);
        var pB = new GeoPoint(-0.21000m, -78.51000m);

        var legAB = new Leg(pA, pB, 2.5m, 5.0m, DistanceSource.Road, null);
        var legBA = new Leg(pB, pA, 3.2m, 7.5m, DistanceSource.Road, null);

        Assert.NotEqual(legAB.From, legBA.From);
        Assert.NotEqual(legAB.Km, legBA.Km);
        Assert.NotEqual(legAB.Minutes, legBA.Minutes);
    }
}
