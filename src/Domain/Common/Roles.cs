namespace IstpetDev.Domain.Common;

// Siete roles fijos: seis humanos (ADR-14, ADR-26) y uno técnico. El rol de cada cuenta vive en Account.Role.
public static class Roles
{
    public const string Gerencia = "Gerencia";
    public const string Planificador = "Planificador";
    public const string Auditor = "Auditor";
    public const string Bodega = "Bodega";
    public const string Conductor = "Conductor";
    public const string Receptor = "Receptor";
    public const string Automatizacion = "Automatizacion";

    // Valores para [Authorize(Roles = ...)], que acepta nombres separados por coma.
    public const string Planning = Gerencia + "," + Planificador;
    public const string Human = Gerencia + "," + Planificador + "," + Auditor + "," + Bodega + "," + Conductor + "," + Receptor;
    public const string Any = Human + "," + Automatizacion;

    public static readonly IReadOnlyList<string> HumanRoles = [Gerencia, Planificador, Auditor, Bodega, Conductor, Receptor];
    public static readonly IReadOnlyList<string> All = [.. HumanRoles, Automatizacion];

    public static bool IsValid(string? role) => role is not null && All.Contains(role);

    public static bool IsHuman(string? role) => role is not null && HumanRoles.Contains(role);

    // ADR-24: Gerencia, Planificador y Auditor operan en la web; Bodega, Conductor y Receptor en la PWA.
    public static string? PlatformOf(string? role) => role switch
    {
        Gerencia or Planificador or Auditor => "web",
        Bodega or Conductor or Receptor => "field",
        _ => null,
    };
}
