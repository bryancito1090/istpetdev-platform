namespace IstpetDev.Infrastructure.Persistence;

internal static class DemoCoreTransition
{
    public static string Preflight()
    {
        var assembly = typeof(DemoCoreTransition).Assembly;
        using var stream = assembly.GetManifestResourceStream("IstpetDev.Infrastructure.Persistence.Migrations.Sql.DemoCorePreflight.sql")
            ?? throw new InvalidOperationException("Falta el preflight de DemoCore.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
