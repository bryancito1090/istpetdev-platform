namespace IstpetDev.Domain.Common;

// Seis roles funcionales de ADR-14, fijos (spec 001). El rol de cada cuenta vive en Account.Role.
public static class Roles
{
    public const string Gerencia = "Gerencia";
    public const string Planificador = "Planificador";
    public const string Bodega = "Bodega";
    public const string Conductor = "Conductor";
    public const string Receptor = "Receptor";
    public const string Automatizacion = "Automatizacion";

    // Valores para [Authorize(Roles = ...)], que acepta nombres separados por coma.
    public const string Planning = Gerencia + "," + Planificador;
    public const string Any = Gerencia + "," + Planificador + "," + Bodega + "," + Conductor + "," + Receptor + "," + Automatizacion;

    public static readonly IReadOnlyList<string> All = [Gerencia, Planificador, Bodega, Conductor, Receptor, Automatizacion];

    public static bool IsValid(string? role) => role is not null && All.Contains(role);
}
