using System.Globalization;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public enum ClockMode { Dataset, Real, Fixed }

public interface IOperationalClock
{
    Task<DateOnly> TodayAsync(CancellationToken ct = default);
    Task<DateTime> WorkdayStartUtcAsync(CancellationToken ct = default);
}

// FR-01: Reloj operativo determinista con soporte para modos Dataset, Real y Fixed.
public sealed class OperationalClock(
    IApplicationDbContext? db,
    IOptions<LogisticsOptions> options,
    TimeProvider? timeProvider = null,
    Func<CancellationToken, Task<DateOnly?>>? customDatasetDateResolver = null) : IOperationalClock
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly LogisticsOptions _options = options.Value;

    public async Task<DateOnly> TodayAsync(CancellationToken ct = default)
    {
        var mode = _options.Clock.Mode;
        if (mode == ClockMode.Dataset)
        {
            var datasetDate = customDatasetDateResolver is not null
                ? await customDatasetDateResolver(ct)
                : await ResolveDatasetDateFromDbAsync(ct);

            if (datasetDate.HasValue)
                return datasetDate.Value;

            // Sin consumos, Dataset cae a Fixed o Real según la especificación
            return _options.Clock.FixedDate ?? GetRealDate();
        }

        if (mode == ClockMode.Fixed)
            return _options.Clock.FixedDate ?? GetRealDate();

        return GetRealDate();
    }

    public async Task<DateTime> WorkdayStartUtcAsync(CancellationToken ct = default)
    {
        var today = await TodayAsync(ct);
        var workdayTime = TimeOnly.TryParse(_options.WorkdayStart, CultureInfo.InvariantCulture, out var t)
            ? t
            : new TimeOnly(7, 0);

        var localDateTime = today.ToDateTime(workdayTime);
        var tz = await ResolveTimeZoneAsync(ct);

        if (tz is not null)
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), tz);
        }

        var offset = TimeSpan.FromHours(_options.FallbackUtcOffsetHours);
        var utc = localDateTime - offset;
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
    }

    private DateOnly GetRealDate()
    {
        var now = _timeProvider.GetLocalNow();
        return DateOnly.FromDateTime(now.DateTime);
    }

    private async Task<DateOnly?> ResolveDatasetDateFromDbAsync(CancellationToken ct)
    {
        if (db is null) return null;

        var consumptions = await db.Consumptions
            .Where(c => c.OrganizationId == DemoOrganization.Id)
            .Select(c => new { c.OccurredOn, c.PeriodDays })
            .ToListAsync(ct);

        if (consumptions.Count == 0) return null;

        return consumptions.Max(c => c.OccurredOn.AddDays(c.PeriodDays));
    }

    private async Task<TimeZoneInfo?> ResolveTimeZoneAsync(CancellationToken ct)
    {
        string? timeZoneId = null;
        if (db is not null)
        {
            timeZoneId = await db.Organizations
                .Where(o => o.Id == DemoOrganization.Id)
                .Select(o => o.TimeZone)
                .FirstOrDefaultAsync(ct);
        }

        timeZoneId ??= "America/Guayaquil";

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
    }
}
