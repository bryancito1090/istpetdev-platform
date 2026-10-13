using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics.Policy;

public enum AutonomyMode
{
    AutoWithLimits,
    ProposeOnly
}

public sealed class AutonomyState(IOptions<LogisticsOptions> options)
{
    private readonly object _lock = new();
    private AutonomyMode _currentMode = ParseMode(options.Value.Policy.Mode);
    private readonly AutonomyMode _configuredMode = ParseMode(options.Value.Policy.Mode);

    public AutonomyMode CurrentMode
    {
        get
        {
            lock (_lock) return _currentMode;
        }
        set
        {
            lock (_lock) _currentMode = value;
        }
    }

    public void ResetToConfigured()
    {
        lock (_lock) _currentMode = _configuredMode;
    }

    private static AutonomyMode ParseMode(string? modeStr) =>
        string.Equals(modeStr, "ProposeOnly", StringComparison.OrdinalIgnoreCase)
            ? AutonomyMode.ProposeOnly
            : AutonomyMode.AutoWithLimits;
}
