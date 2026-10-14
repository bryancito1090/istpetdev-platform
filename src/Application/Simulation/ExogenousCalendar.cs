using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IstpetDev.Application.Simulation;

public static class ExogenousCalendar
{
    public static decimal CalculateExpectedDemand(decimal baseDaily, DateOnly date, decimal noisePercent, bool isSpike)
    {
        var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var dayFactor = isWeekend ? 0.8m : 1.0m;
        var noiseFactor = 1.0m + noisePercent;
        var spikeMultiplier = isSpike ? 2.5m : 1.0m;

        var result = baseDaily * dayFactor * noiseFactor * spikeMultiplier;
        return Math.Round(result, 3, MidpointRounding.AwayFromZero);
    }

    public static ScenarioCalendar Build(
        ScenarioKey key,
        IReadOnlyList<(string RegionCode, string PointCode)> points,
        IReadOnlyList<(string Sku, decimal BaseDaily, string Unit)> products,
        DateOnly startDate,
        int days,
        IReadOnlyList<DisruptionEvent>? customDisruptions = null)
    {
        var demandList = new List<DemandEvent>();

        // PRNG determinista por clave de escenario y punto/sku/fecha
        // Garantiza que la misma (datasetVersion, seed) genere exactamente los mismos eventos
        for (int dayOffset = 0; dayOffset < days; dayOffset++)
        {
            var date = startDate.AddDays(dayOffset);
            var occurredUtc = date.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc);
            var dueUtc = date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc);

            foreach (var (regionCode, pointCode) in points.OrderBy(p => p.RegionCode).ThenBy(p => p.PointCode))
            {
                foreach (var (sku, baseDaily, unit) in products.OrderBy(pr => pr.Sku))
                {
                    // Obtener hash pseudoaleatorio estable
                    var keyString = $"{key.DatasetVersion}:{key.Seed}:{pointCode}:{sku}:{date:yyyy-MM-dd}";
                    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
                    var u1 = BitConverter.ToUInt32(hash, 0);
                    var u2 = BitConverter.ToUInt32(hash, 4);

                    // Ruido determinista en [-0.15, +0.15]
                    var noise = ((u1 % 3001) / 10000m) - 0.15m; // paso de 0.0001, rango -0.15 a +0.15

                    // Spike en 5% de las claves (u2 % 100 < 5)
                    var isSpike = (u2 % 100) < 5;

                    var requested = CalculateExpectedDemand(baseDaily, date, noise, isSpike);

                    demandList.Add(new DemandEvent(
                        OccurredAtUtc: occurredUtc,
                        RegionCode: regionCode,
                        PointCode: pointCode,
                        Sku: sku,
                        Requested: requested,
                        Unit: unit,
                        DueAtUtc: dueUtc,
                        IsSpike: isSpike));
                }
            }
        }

        var disruptions = (customDisruptions ?? Array.Empty<DisruptionEvent>())
            .OrderBy(d => d.OccurredAtUtc)
            .ThenBy(d => d.RegionCode)
            .ThenBy(d => d.SubjectCode)
            .ToList();

        var sha256 = ComputeCalendarSha256(key, demandList, disruptions);

        return new ScenarioCalendar(key, demandList, disruptions, sha256);
    }

    public static ScenarioCalendar DeriveAdverseScenario(ScenarioCalendar baseCalendar, string retiredVehicleCode)
    {
        var adverseKey = new ScenarioKey(
            baseCalendar.Key.DatasetVersion,
            baseCalendar.Key.Seed,
            "adverse");

        var adverseDemand = baseCalendar.Demand.Select(d => new DemandEvent(
            OccurredAtUtc: d.OccurredAtUtc,
            RegionCode: d.RegionCode,
            PointCode: d.PointCode,
            Sku: d.Sku,
            Requested: Math.Round(d.Requested * 1.20m, 3, MidpointRounding.AwayFromZero),
            Unit: d.Unit,
            DueAtUtc: d.DueAtUtc,
            IsSpike: d.IsSpike)).ToList();

        var disruptions = baseCalendar.Disruptions.ToList();
        var sha256 = ComputeCalendarSha256(adverseKey, adverseDemand, disruptions);

        return new ScenarioCalendar(adverseKey, adverseDemand, disruptions, sha256);
    }

    public static IReadOnlyList<DemandEvent> DemandVisibleAt(this ScenarioCalendar calendar, DateTime asOfUtc)
    {
        return calendar.Demand.Where(d => d.OccurredAtUtc <= asOfUtc).ToList();
    }

    public static IReadOnlyList<DisruptionEvent> DisruptionsVisibleAt(this ScenarioCalendar calendar, DateTime asOfUtc)
    {
        return calendar.Disruptions.Where(d => d.KnownAtUtc <= asOfUtc).ToList();
    }

    private static string ComputeCalendarSha256(
        ScenarioKey key,
        IReadOnlyList<DemandEvent> demand,
        IReadOnlyList<DisruptionEvent> disruptions)
    {
        using var sha = SHA256.Create();
        var sb = new StringBuilder();
        sb.Append($"{key.DatasetVersion}|{key.Seed}|{key.ScenarioCode}\n");

        foreach (var d in demand.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.RegionCode).ThenBy(x => x.PointCode).ThenBy(x => x.Sku))
        {
            sb.Append($"{d.OccurredAtUtc:O}|{d.RegionCode}|{d.PointCode}|{d.Sku}|{d.Requested:F3}|{d.IsSpike}\n");
        }

        foreach (var r in disruptions.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.RegionCode).ThenBy(x => x.SubjectCode))
        {
            sb.Append($"{r.OccurredAtUtc:O}|{r.KnownAtUtc:O}|{r.RegionCode}|{r.Kind}|{r.SubjectCode}|{r.Factor:F3}\n");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
