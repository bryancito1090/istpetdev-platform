namespace IstpetDev.Application.Logistics;

public static class Polyline
{
    public static string Encode(IEnumerable<GeoPoint> points)
    {
        var result = new System.Text.StringBuilder();
        int lastLat = 0;
        int lastLon = 0;

        foreach (var point in points)
        {
            int lat = (int)Math.Round(point.Lat * 1e5m, MidpointRounding.AwayFromZero);
            int lon = (int)Math.Round(point.Lon * 1e5m, MidpointRounding.AwayFromZero);

            EncodeSigned(lat - lastLat, result);
            EncodeSigned(lon - lastLon, result);

            lastLat = lat;
            lastLon = lon;
        }

        return result.ToString();
    }

    public static IReadOnlyList<GeoPoint> Decode(string? encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return Array.Empty<GeoPoint>();

        var points = new List<GeoPoint>();
        int index = 0;
        int lat = 0;
        int lon = 0;

        while (index < encoded.Length)
        {
            lat += DecodeSigned(encoded, ref index);
            lon += DecodeSigned(encoded, ref index);

            points.Add(new GeoPoint(lat / 1e5m, lon / 1e5m));
        }

        return points;
    }

    private static void EncodeSigned(int value, System.Text.StringBuilder sb)
    {
        int v = value < 0 ? ~(value << 1) : (value << 1);
        while (v >= 0x20)
        {
            sb.Append((char)((0x20 | (v & 0x1f)) + 63));
            v >>= 5;
        }
        sb.Append((char)(v + 63));
    }

    private static int DecodeSigned(string encoded, ref int index)
    {
        int result = 0;
        int shift = 0;
        int b;
        do
        {
            if (index >= encoded.Length) break;
            b = encoded[index++] - 63;
            result |= (b & 0x1f) << shift;
            shift += 5;
        } while (b >= 0x20);

        return (result & 1) != 0 ? ~(result >> 1) : (result >> 1);
    }
}
