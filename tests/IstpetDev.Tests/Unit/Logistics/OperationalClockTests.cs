using System.Text.Json;
using IstpetDev.Application.Logistics;
using Microsoft.Extensions.Options;

namespace IstpetDev.Tests.Unit.Logistics;

[Trait("Category", "Unit")]
public class OperationalClockTests
{
    [Fact]
    public async Task Dataset_mode_returns_day_after_period_end()
    {
        var options = Options.Create(new LogisticsOptions
        {
            Clock = new ClockOptions { Mode = ClockMode.Dataset },
            WorkdayStart = "07:00"
        });

        // Consumo [2025-09-01, 2025-09-08) -> OccurredOn = 2025-09-01, PeriodDays = 7
        var clock = new OperationalClock(
            db: null,
            options: options,
            timeProvider: TimeProvider.System,
            customDatasetDateResolver: _ => Task.FromResult<DateOnly?>(new DateOnly(2025, 9, 8)));

        var today = await clock.TodayAsync();
        Assert.Equal(new DateOnly(2025, 9, 8), today);
    }

    [Fact]
    public async Task Fixed_mode_returns_configured_date()
    {
        var fixedDate = new DateOnly(2026, 10, 15);
        var options = Options.Create(new LogisticsOptions
        {
            Clock = new ClockOptions
            {
                Mode = ClockMode.Fixed,
                FixedDate = fixedDate
            }
        });

        var clock = new OperationalClock(null, options, TimeProvider.System);
        var today = await clock.TodayAsync();
        Assert.Equal(fixedDate, today);
    }

    [Fact]
    public async Task Real_mode_uses_local_date_from_time_provider()
    {
        var fakeTime = new DateTimeOffset(2026, 10, 12, 14, 30, 0, TimeSpan.FromHours(-5));
        var fakeProvider = new FakeTimeProvider(fakeTime);

        var options = Options.Create(new LogisticsOptions
        {
            Clock = new ClockOptions { Mode = ClockMode.Real }
        });

        var clock = new OperationalClock(null, options, fakeProvider);
        var today = await clock.TodayAsync();
        Assert.Equal(new DateOnly(2026, 10, 12), today);
    }

    [Fact]
    public async Task Dataset_mode_falls_back_to_fixed_or_real_when_no_consumptions()
    {
        var fixedDate = new DateOnly(2026, 10, 14);
        var options = Options.Create(new LogisticsOptions
        {
            Clock = new ClockOptions
            {
                Mode = ClockMode.Dataset,
                FixedDate = fixedDate
            }
        });

        var clock = new OperationalClock(
            db: null,
            options: options,
            timeProvider: TimeProvider.System,
            customDatasetDateResolver: _ => Task.FromResult<DateOnly?>(null));

        var today = await clock.TodayAsync();
        Assert.Equal(fixedDate, today);
    }

    [Fact]
    public async Task WorkdayStartUtcAsync_converts_local_start_to_utc()
    {
        // Para 2026-10-12 07:00 en America/Guayaquil (UTC-5), WorkdayStartUtcAsync devuelve 2026-10-12T12:00:00Z
        var fixedDate = new DateOnly(2026, 10, 12);
        var options = Options.Create(new LogisticsOptions
        {
            Clock = new ClockOptions { Mode = ClockMode.Fixed, FixedDate = fixedDate },
            WorkdayStart = "07:00",
            FallbackUtcOffsetHours = -5
        });

        var clock = new OperationalClock(null, options, TimeProvider.System);
        var workdayStartUtc = await clock.WorkdayStartUtcAsync();

        Assert.Equal(DateTimeKind.Utc, workdayStartUtc.Kind);
        Assert.Equal(new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc), workdayStartUtc);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("Fake", now.Offset, "Fake", "Fake");
    }
}
