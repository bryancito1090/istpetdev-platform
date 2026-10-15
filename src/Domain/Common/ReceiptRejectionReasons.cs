namespace IstpetDev.Domain.Common;

public sealed record RejectionReasonItem(string Code, string Label);

public static class ReceiptRejectionReasons
{
    public const string Version = "2026-10-15";

    public const string Faltante = "faltante";
    public const string Sobrante = "sobrante";
    public const string Dano = "dano";
    public const string PrecintoViolado = "precinto_violado";
    public const string LoteNoCoincide = "lote_no_coincide";
    public const string CadenaFrio = "cadena_frio";

    public static readonly IReadOnlyList<RejectionReasonItem> Items =
    [
        new(Faltante, "Faltante"),
        new(Sobrante, "Sobrante no solicitado"),
        new(Dano, "Producto dañado"),
        new(PrecintoViolado, "Precinto violado o alterado"),
        new(LoteNoCoincide, "Lote o vencimiento no coincide"),
        new(CadenaFrio, "Cadena de frío rota"),
    ];

    private static readonly HashSet<string> ValidCodes = new(Items.Select(i => i.Code), StringComparer.Ordinal);

    public static bool IsValid(string? code) => code is not null && ValidCodes.Contains(code);

    public static bool TryValidate(string? rawReason, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(rawReason))
        {
            errorMessage = "El motivo de rechazo es obligatorio si hay cantidad rechazada.";
            return false;
        }

        var trimmed = rawReason.Trim();
        var colonIdx = trimmed.IndexOf(':');
        string code;
        if (colonIdx >= 0)
        {
            code = trimmed[..colonIdx].Trim();
            var detail = trimmed[(colonIdx + 1)..].Trim();
            if (detail.Length > 200)
            {
                errorMessage = "El detalle del motivo de rechazo no puede superar los 200 caracteres.";
                return false;
            }
        }
        else
        {
            code = trimmed;
        }

        if (!IsValid(code))
        {
            errorMessage = $"El motivo de rechazo '{code}' no pertenece al catálogo válido.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
