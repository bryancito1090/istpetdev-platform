using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IstpetDev.Application.Simulation;

public sealed record ScenarioGenerationResult(
    string OutputDirectory,
    ScenarioCalendar Calendar,
    string CalendarSha256,
    string ManifestSha256,
    IReadOnlyDictionary<string, string> FileHashes);

public static class ScenarioGenerator
{
    public static async Task<ScenarioGenerationResult> GenerateFromScenarioFileAsync(
        string scenarioPath,
        string outputDirectory,
        int seed,
        string scenarioCode = "base",
        CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(scenarioPath, cancellationToken);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var datasetVersion = root.GetProperty("datasetVersion").GetString() ?? "national-v1";
        var horizonDays = root.GetProperty("horizonDays").GetInt32();
        var startDateStr = root.GetProperty("startDate").GetString() ?? "2026-10-12";
        var startDate = DateOnly.Parse(startDateStr, CultureInfo.InvariantCulture);

        var products = new List<(string Sku, string Name, string Unit, decimal Weight, decimal Volume, decimal BaseDaily, decimal WarehouseStock, decimal PointStock)>();
        foreach (var p in root.GetProperty("products").EnumerateArray())
        {
            products.Add((
                p.GetProperty("sku").GetString()!,
                p.GetProperty("name").GetString()!,
                p.GetProperty("base_unit").GetString()!,
                p.GetProperty("weight_per_unit").GetDecimal(),
                p.GetProperty("volume_per_unit").GetDecimal(),
                p.GetProperty("base_daily_demand").GetDecimal(),
                p.GetProperty("initial_stock_warehouse").GetDecimal(),
                p.GetProperty("initial_stock_point").GetDecimal()
            ));
        }

        var warehouses = new List<(string Code, string Name, decimal Lat, decimal Lon)>();
        var vehicles = new List<(string Code, string DepotCode, decimal MaxWeight, decimal MaxVolume, decimal CostPerKm)>();
        var points = new List<(string Code, string Name, decimal Lat, decimal Lon, string Region, string Category, string Criticality, string WindowStart, string WindowEnd, int ServiceMinutes, decimal LeadTimeDays)>();
        var roadSegments = new List<(string FromCode, string ToCode, decimal DistanceKm, decimal DurationMin, string FromKind, string ToKind)>();

        foreach (var reg in root.GetProperty("regions").EnumerateArray())
        {
            var regCode = reg.GetProperty("code").GetString()!;
            var w = reg.GetProperty("warehouse");
            var wCode = w.GetProperty("code").GetString()!;
            warehouses.Add((wCode, w.GetProperty("name").GetString()!, w.GetProperty("lat").GetDecimal(), w.GetProperty("lon").GetDecimal()));

            foreach (var v in reg.GetProperty("vehicles").EnumerateArray())
            {
                vehicles.Add((v.GetProperty("code").GetString()!, wCode, v.GetProperty("max_weight").GetDecimal(), v.GetProperty("max_volume").GetDecimal(), v.GetProperty("cost_per_km").GetDecimal()));
            }

            foreach (var pt in reg.GetProperty("service_points").EnumerateArray())
            {
                points.Add((
                    pt.GetProperty("code").GetString()!,
                    pt.GetProperty("name").GetString()!,
                    pt.GetProperty("lat").GetDecimal(),
                    pt.GetProperty("lon").GetDecimal(),
                    regCode,
                    pt.GetProperty("category").GetString()!,
                    pt.GetProperty("criticality").GetString()!,
                    pt.GetProperty("window_start").GetString()!,
                    pt.GetProperty("window_end").GetString()!,
                    pt.GetProperty("service_minutes").GetInt32(),
                    pt.GetProperty("lead_time_days").GetDecimal()
                ));
            }

            if (reg.TryGetProperty("road_segments", out var segmentsEl))
            {
                foreach (var seg in segmentsEl.EnumerateArray())
                {
                    roadSegments.Add((
                        seg.GetProperty("from_code").GetString()!,
                        seg.GetProperty("to_code").GetString()!,
                        seg.GetProperty("distance_km").GetDecimal(),
                        seg.GetProperty("duration_min").GetDecimal(),
                        seg.GetProperty("from_kind").GetString()!,
                        seg.GetProperty("to_kind").GetString()!
                    ));
                }
            }
        }

        return await WriteCanonicalDatasetAsync(
            outputDirectory: outputDirectory,
            datasetVersion: datasetVersion,
            seed: seed,
            scenarioCode: scenarioCode,
            startDate: startDate,
            horizonDays: horizonDays,
            warehouses: warehouses,
            vehicles: vehicles,
            points: points,
            products: products,
            roadSegments: roadSegments,
            cancellationToken: cancellationToken);
    }

    public static async Task<ScenarioGenerationResult> GenerateSyntheticAsync(
        string outputDirectory,
        string datasetVersion,
        int seed,
        int pointCount,
        int days,
        int skuCount,
        DateOnly startDate,
        string scenarioCode = "base",
        CancellationToken cancellationToken = default)
    {
        var warehouses = new List<(string Code, string Name, decimal Lat, decimal Lon)>
        {
            ("BOD-01", "Bodega Central (sintética)", -0.2000m, -78.5000m)
        };

        var vehicles = new List<(string Code, string DepotCode, decimal MaxWeight, decimal MaxVolume, decimal CostPerKm)>
        {
            ("VEH-01", "BOD-01", 1200m, 8m, 0.45m),
            ("VEH-02", "BOD-01", 1200m, 8m, 0.45m)
        };

        var products = new List<(string Sku, string Name, string Unit, decimal Weight, decimal Volume, decimal BaseDaily, decimal WarehouseStock, decimal PointStock)>();
        for (int i = 1; i <= skuCount; i++)
        {
            var sku = $"SKU-{i:02d}";
            var unit = i % 2 == 1 ? "L" : "UN";
            products.Add((sku, $"Producto Sintético {i}", unit, 1.0m, 0.002m, 10m * i, 1000m * i, 30m * i));
        }

        var points = new List<(string Code, string Name, decimal Lat, decimal Lon, string Region, string Category, string Criticality, string WindowStart, string WindowEnd, int ServiceMinutes, decimal LeadTimeDays)>();
        for (int i = 1; i <= pointCount; i++)
        {
            var code = $"P{i:02d}";
            var lat = -0.2000m + (i * 0.0100m);
            var lon = -78.5000m + (i * 0.0080m);
            points.Add((code, $"Punto {i}", lat, lon, "REG-01", "hospital", "alta", "07:00", "12:00", 15, 2m));
        }

        // Road segments between all pairs (warehouse + points)
        var allLocs = warehouses.Select(w => (w.Code, w.Lat, w.Lon, Kind: "warehouse"))
            .Concat(points.Select(p => (p.Code, p.Lat, p.Lon, Kind: "service_point")))
            .ToList();

        var roadSegments = new List<(string FromCode, string ToCode, decimal DistanceKm, decimal DurationMin, string FromKind, string ToKind)>();
        for (int i = 0; i < allLocs.Count; i++)
        {
            for (int j = 0; j < allLocs.Count; j++)
            {
                if (i == j) continue;
                var o = allLocs[i];
                var d = allLocs[j];
                var dist = Math.Round(Math.Max(1.0m, Math.Abs(o.Lat - d.Lat) * 111m + Math.Abs(o.Lon - d.Lon) * 111m), 3);
                var dur = Math.Round(dist * 2.0m, 3);
                roadSegments.Add((o.Code, d.Code, dist, dur, o.Kind, d.Kind));
            }
        }

        return await WriteCanonicalDatasetAsync(
            outputDirectory: outputDirectory,
            datasetVersion: datasetVersion,
            seed: seed,
            scenarioCode: scenarioCode,
            startDate: startDate,
            horizonDays: days,
            warehouses: warehouses,
            vehicles: vehicles,
            points: points,
            products: products,
            roadSegments: roadSegments,
            cancellationToken: cancellationToken);
    }

    private static async Task<ScenarioGenerationResult> WriteCanonicalDatasetAsync(
        string outputDirectory,
        string datasetVersion,
        int seed,
        string scenarioCode,
        DateOnly startDate,
        int horizonDays,
        IReadOnlyList<(string Code, string Name, decimal Lat, decimal Lon)> warehouses,
        IReadOnlyList<(string Code, string DepotCode, decimal MaxWeight, decimal MaxVolume, decimal CostPerKm)> vehicles,
        IReadOnlyList<(string Code, string Name, decimal Lat, decimal Lon, string Region, string Category, string Criticality, string WindowStart, string WindowEnd, int ServiceMinutes, decimal LeadTimeDays)> points,
        IReadOnlyList<(string Sku, string Name, string Unit, decimal Weight, decimal Volume, decimal BaseDaily, decimal WarehouseStock, decimal PointStock)> products,
        IReadOnlyList<(string FromCode, string ToCode, decimal DistanceKm, decimal DurationMin, string FromKind, string ToKind)> roadSegments,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileHashes = new Dictionary<string, string>();

        // 1. warehouses.csv
        var whSb = new StringBuilder();
        whSb.AppendLine("code,name,lat,lon");
        foreach (var w in warehouses.OrderBy(x => x.Code))
        {
            whSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F4},{3:F4}", w.Code, w.Name, w.Lat, w.Lon));
        }
        await WriteAndHashAsync(outputDirectory, "warehouses.csv", whSb.ToString(), fileHashes, cancellationToken);

        // 2. service_points.csv
        var spSb = new StringBuilder();
        spSb.AppendLine("code,name,lat,lon,region,category,criticality,window_start,window_end,service_minutes,lead_time_days");
        foreach (var p in points.OrderBy(x => x.Code))
        {
            spSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F4},{3:F4},{4},{5},{6},{7},{8},{9},{10:F1}",
                p.Code, p.Name, p.Lat, p.Lon, p.Region, p.Category, p.Criticality, p.WindowStart, p.WindowEnd, p.ServiceMinutes, p.LeadTimeDays));
        }
        await WriteAndHashAsync(outputDirectory, "service_points.csv", spSb.ToString(), fileHashes, cancellationToken);

        // 3. products.csv
        var prSb = new StringBuilder();
        prSb.AppendLine("sku,name,base_unit,weight_per_unit,volume_per_unit");
        foreach (var pr in products.OrderBy(x => x.Sku))
        {
            prSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3:F3},{4:F4}",
                pr.Sku, pr.Name, pr.Unit, pr.Weight, pr.Volume));
        }
        await WriteAndHashAsync(outputDirectory, "products.csv", prSb.ToString(), fileHashes, cancellationToken);

        // 4. vehicles.csv
        var vhSb = new StringBuilder();
        vhSb.AppendLine("code,depot_code,max_weight,max_volume,cost_per_km");
        foreach (var v in vehicles.OrderBy(x => x.Code))
        {
            vhSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F1},{3:F1},{4:F2}",
                v.Code, v.DepotCode, v.MaxWeight, v.MaxVolume, v.CostPerKm));
        }
        await WriteAndHashAsync(outputDirectory, "vehicles.csv", vhSb.ToString(), fileHashes, cancellationToken);

        // 5. lots.csv
        var ltSb = new StringBuilder();
        ltSb.AppendLine("sku,lot_code,expires_on,origin,product_name,base_unit");
        foreach (var pr in products.OrderBy(x => x.Sku))
        {
            var lotCode = $"LOT-{pr.Sku}-01";
            var exp = startDate.AddDays(180).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ltSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5}",
                pr.Sku, lotCode, exp, "Nacional", pr.Name, pr.Unit));
        }
        await WriteAndHashAsync(outputDirectory, "lots.csv", ltSb.ToString(), fileHashes, cancellationToken);

        // 6. stock.csv
        var stSb = new StringBuilder();
        stSb.AppendLine("location_kind,location_code,sku,lot_code,quantity");
        foreach (var w in warehouses.OrderBy(x => x.Code))
        {
            foreach (var pr in products.OrderBy(x => x.Sku))
            {
                var lotCode = $"LOT-{pr.Sku}-01";
                stSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "warehouse,{0},{1},{2},{3:F1}",
                    w.Code, pr.Sku, lotCode, pr.WarehouseStock));
            }
        }
        foreach (var p in points.OrderBy(x => x.Code))
        {
            foreach (var pr in products.OrderBy(x => x.Sku))
            {
                var lotCode = $"LOT-{pr.Sku}-01";
                stSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "service_point,{0},{1},{2},{3:F1}",
                    p.Code, pr.Sku, lotCode, pr.PointStock));
            }
        }
        await WriteAndHashAsync(outputDirectory, "stock.csv", stSb.ToString(), fileHashes, cancellationToken);

        // 7. consumptions.csv (Histórico previo: 14 días anteriores a startDate)
        var csSb = new StringBuilder();
        csSb.AppendLine("point_code,sku,date,quantity,period_days");
        for (int h = 14; h >= 1; h--)
        {
            var histDate = startDate.AddDays(-h);
            foreach (var p in points.OrderBy(x => x.Code))
            {
                foreach (var pr in products.OrderBy(x => x.Sku))
                {
                    // Consumo histórico determinista
                    var histQty = pr.BaseDaily * (histDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.8m : 1.0m);
                    csSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:yyyy-MM-dd},{3:F1},1",
                        p.Code, pr.Sku, histDate, histQty));
                }
            }
        }
        await WriteAndHashAsync(outputDirectory, "consumptions.csv", csSb.ToString(), fileHashes, cancellationToken);

        // 8. road_segments.csv
        var rsSb = new StringBuilder();
        rsSb.AppendLine("from_code,to_code,distance_km,duration_min,geometry,from_kind,to_kind");
        foreach (var rs in roadSegments.OrderBy(x => x.FromCode).ThenBy(x => x.ToCode))
        {
            rsSb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F3},{3:F3},,{4},{5}",
                rs.FromCode, rs.ToCode, rs.DistanceKm, rs.DurationMin, rs.FromKind, rs.ToKind));
        }
        await WriteAndHashAsync(outputDirectory, "road_segments.csv", rsSb.ToString(), fileHashes, cancellationToken);

        // 9. Construir calendario exógeno
        var scenarioKey = new ScenarioKey(datasetVersion, seed, scenarioCode);
        var calendarPoints = points.Select(p => (p.Region, p.Code)).ToList();
        var calendarProducts = products.Select(pr => (pr.Sku, pr.BaseDaily, pr.Unit)).ToList();

        var calendar = ExogenousCalendar.Build(
            scenarioKey,
            calendarPoints,
            calendarProducts,
            startDate,
            horizonDays);

        if (scenarioCode == "adverse")
        {
            calendar = ExogenousCalendar.DeriveAdverseScenario(calendar, "VEH-RETIRED");
        }

        var calJson = JsonSerializer.Serialize(calendar, new JsonSerializerOptions { WriteIndented = true });
        await WriteAndHashAsync(outputDirectory, "calendar.json", calJson, fileHashes, cancellationToken);

        // 10. manifest.json
        var manifestObj = new
        {
            datasetVersion,
            seed,
            scenarioCode,
            startDate = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            horizonDays,
            pointsCount = points.Count,
            warehousesCount = warehouses.Count,
            vehiclesCount = vehicles.Count,
            productsCount = products.Count,
            calendarSha256 = calendar.Sha256,
            files = fileHashes
        };

        var manifestJson = JsonSerializer.Serialize(manifestObj, new JsonSerializerOptions { WriteIndented = true });
        var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "manifest.json"), manifestBytes, cancellationToken);

        var manifestHash = Convert.ToHexString(SHA256.HashData(manifestBytes)).ToLowerInvariant();

        return new ScenarioGenerationResult(
            OutputDirectory: outputDirectory,
            Calendar: calendar,
            CalendarSha256: calendar.Sha256,
            ManifestSha256: manifestHash,
            FileHashes: fileHashes);
    }

    private static async Task WriteAndHashAsync(
        string dir,
        string fileName,
        string content,
        Dictionary<string, string> hashes,
        CancellationToken ct)
    {
        var filePath = Path.Combine(dir, fileName);
        var bytes = Encoding.UTF8.GetBytes(content);
        await File.WriteAllBytesAsync(filePath, bytes, ct);
        hashes[fileName] = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
