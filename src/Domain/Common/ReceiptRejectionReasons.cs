using System.Collections.Frozen;

namespace IstpetDev.Domain.Common;

/// <summary>
/// Catálogo versionado de motivos de rechazo en recepción de campo (Issue #117 / ISS-08).
/// Reemplaza el texto libre no estructurado con constantes verificadas.
/// V2 (#236): agrega ErrorRotulado; los códigos de V1 no cambian.
/// </summary>
public static class ReceiptRejectionReasons
{
    public const string Version = "V2";

    public const string Faltante = "Faltante";
    public const string Sobrante = "Sobrante";
    public const string Daño = "Daño";
    public const string PrecintoViolado = "PrecintoViolado";
    public const string LoteNoCoincide = "LoteNoCoincide";
    public const string CadenaDeFrio = "CadenaDeFrio";
    public const string ErrorRotulado = "ErrorRotulado";

    public static readonly IReadOnlyList<string> All =
    [
        Faltante,
        Sobrante,
        Daño,
        PrecintoViolado,
        LoteNoCoincide,
        CadenaDeFrio,
        ErrorRotulado
    ];

    private static readonly FrozenDictionary<string, string> NormalizationMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["faltante"] = Faltante,
        ["shortage"] = Faltante,
        ["missing"] = Faltante,

        ["sobrante"] = Sobrante,
        ["overage"] = Sobrante,
        ["surplus"] = Sobrante,

        ["daño"] = Daño,
        ["dano"] = Daño,
        ["damaged"] = Daño,
        ["deteriorado"] = Daño,

        ["precintoviolado"] = PrecintoViolado,
        ["precinto_violado"] = PrecintoViolado,
        ["precinto-violado"] = PrecintoViolado,
        ["broken_seal"] = PrecintoViolado,
        ["brokenseal"] = PrecintoViolado,

        ["lotenocoincide"] = LoteNoCoincide,
        ["lote_no_coincide"] = LoteNoCoincide,
        ["lote-no-coincide"] = LoteNoCoincide,
        ["lot_mismatch"] = LoteNoCoincide,
        ["wrong_lot"] = LoteNoCoincide,

        ["cadenadefrio"] = CadenaDeFrio,
        ["cadena_de_frio"] = CadenaDeFrio,
        ["cadena-de-frio"] = CadenaDeFrio,
        ["cadenafrio"] = CadenaDeFrio,
        ["cold_chain_breach"] = CadenaDeFrio,
        ["coldchainbreach"] = CadenaDeFrio,
        ["temperatura"] = CadenaDeFrio,

        ["errorrotulado"] = ErrorRotulado,
        ["error_rotulado"] = ErrorRotulado,
        ["error-rotulado"] = ErrorRotulado,
        ["mislabeled"] = ErrorRotulado,
        ["wrong_label"] = ErrorRotulado
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        [Faltante] = "Cantidad física menor a la manifestada en la orden de despacho",
        [Sobrante] = "Cantidad física entregada superior a la manifestada en la orden de despacho",
        [Daño] = "Bulto o producto con daño físico visible, rotura o derrame",
        [PrecintoViolado] = "Precinto de seguridad roto, adulterado o ausente",
        [LoteNoCoincide] = "El código de lote físico no coincide con el lote asignado en el despacho",
        [CadenaDeFrio] = "Ruptura o excursión de temperatura de cadena de frío comprobada",
        [ErrorRotulado] = "El rótulo o la identificación del empaque no coincide con el producto o el despacho"
    }.ToFrozenDictionary();

    public static bool IsValid(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && (All.Contains(reason) || NormalizationMap.ContainsKey(reason.Trim()));

    public static bool TryNormalize(string? input, out string canonical)
    {
        canonical = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var trimmed = input.Trim();
        if (All.Contains(trimmed))
        {
            canonical = trimmed;
            return true;
        }

        return NormalizationMap.TryGetValue(trimmed, out canonical!);
    }

    public static string GetDescription(string reason) =>
        Descriptions.GetValueOrDefault(reason, "Motivo no catalogado");
}
