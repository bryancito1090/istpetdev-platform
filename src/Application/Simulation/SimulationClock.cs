namespace IstpetDev.Application.Simulation;

public sealed class SimulationClock
{
    private static readonly TimeZoneInfo EcuadorTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Guayaquil");

    public DateOnly CurrentDate { get; private set; }
    public DateTime CurrentTimeUtc { get; private set; }

    public SimulationClock(DateOnly initialDate)
    {
        CurrentDate = initialDate;
        CurrentTimeUtc = initialDate.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc);
    }

    public void AdvanceTo(DateTime utcTime)
    {
        CurrentTimeUtc = utcTime;
        CurrentDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcTime, EcuadorTimeZone));
    }

    public DateTime WorkdayStartUtc(DateOnly date) =>
        date.ToDateTime(new TimeOnly(6, 0), DateTimeKind.Utc);

    public DateTime PlanningTimeUtc(DateOnly date) =>
        date.ToDateTime(new TimeOnly(6, 5), DateTimeKind.Utc);

    public DateTime WorkdayEndUtc(DateOnly date) =>
        date.ToDateTime(new TimeOnly(18, 0), DateTimeKind.Utc);

    public static int WorkdaysBetween(DateOnly from, DateOnly to)
    {
        var count = 0;
        var current = from;
        while (current < to)
        {
            current = current.AddDays(1);
            if (current.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) count++;
        }
        return count;
    }

    public static DateOnly AddWorkdays(DateOnly date, int workdays)
    {
        var result = date;
        while (workdays > 0)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) workdays--;
        }
        return result;
    }
}
