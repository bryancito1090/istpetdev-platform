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
        var profiles = new List<DemandProfile>();
        foreach (var (regionCode, pointCode) in points.OrderBy(p => p.RegionCode).ThenBy(p => p.PointCode))
        {
            foreach (var (sku, baseDaily, unit) in products.OrderBy(pr => pr.Sku))
            {
                profiles.Add(new DemandProfile(
                    pointCode, regionCode, Guid.Empty, sku, unit,
                    new Dictionary<DayOfWeek, decimal>
                    {
                        [DayOfWeek.Monday] = baseDaily,
                        [DayOfWeek.Tuesday] = baseDaily,
                        [DayOfWeek.Wednesday] = baseDaily,
                        [DayOfWeek.Thursday] = baseDaily,
                        [DayOfWeek.Friday] = baseDaily,
                        [DayOfWeek.Saturday] = baseDaily * 0.8m,
                        [DayOfWeek.Sunday] = baseDaily * 0.8m
                    },
                    0.15m));
            }
        }
        return Build(key, profiles, startDate, days, customDisruptions);
    }

    public static ScenarioCalendar Build(
        ScenarioKey key,
        IReadOnlyList<DemandProfile> profiles,
        DateOnly startDate,
        int days,
        IReadOnlyList<DisruptionEvent>? customDisruptions = null)
    {
        var demandList = new List<DemandEvent>();

        // PRNG determinista por clave de escenario y punto/sku/fecha.
        // La media por día de la semana y la volatilidad provienen del histórico importado.
        for (int dayOffset = 0; dayOffset < days; dayOffset++)
        {
            var date = startDate.AddDays(dayOffset);
            var occurredUtc = date.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc);
            var dueUtc = date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc);

            foreach (var profile in profiles.OrderBy(p => p.PointCode).ThenBy(p => p.Sku))
            {
                var meanDaily = profile.MeanFor(date.DayOfWeek);
                if (meanDaily <= 0m) continue;

                var keyString = $"{key.DatasetVersion}:{key.Seed}:{profile.PointCode}:{profile.Sku}:{date:yyyy-MM-dd}";
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
                var u1 = BitConverter.ToUInt32(hash, 0);
                var u2 = BitConverter.ToUInt32(hash, 4);

                var noiseRange = Math.Clamp(profile.Volatility, 0.05m, 0.15m);
                var noise = (((u1 % 3001) / 10000m) - 0.15m) * (noiseRange / 0.15m);
                var isSpike = (u2 % 100) < 5;
                var requested = meanDaily * (1m + noise) * (isSpike ? 2.5m : 1m);
                requested = Math.Round(requested, 3, MidpointRounding.AwayFromZero);
                if (requested <= 0m) continue;

                demandList.Add(new DemandEvent(
                    OccurredAtUtc: occurredUtc,
                    RegionCode: profile.Region,
                    PointCode: profile.PointCode,
                    Sku: profile.Sku,
                    Requested: requested,
                    Unit: profile.Unit,
                    DueAtUtc: dueUtc,
                    IsSpike: isSpike));
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
