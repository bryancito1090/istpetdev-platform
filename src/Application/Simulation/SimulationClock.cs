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
}
