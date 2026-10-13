using IstpetDev.Application.Imports;

namespace IstpetDev.Application.Demo;

public sealed class DemoOptions
{
    public const string Section = "Demo";

    public bool Enabled { get; set; }
    public string FixturePath { get; set; } = "data/fixtures/demo-base";
    public List<DemoAccount> Accounts { get; set; } = new();
}

public sealed class DemoAccount
{
    public string Kind { get; set; } = "human";
    public string Subject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public interface IDemoResetService
{
    Task<ImportReport> ResetAsync(CancellationToken cancellationToken = default, Guid? actorAccountId = null);
}
