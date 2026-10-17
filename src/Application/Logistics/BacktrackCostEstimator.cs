using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;

namespace IstpetDev.Application.Logistics;

/// <summary>Reentrega estimada: siempre es una estimación; aproximada si algún tramo salió de línea recta.</summary>
public sealed record BacktrackEstimate(decimal Km, decimal CostUsd, decimal Hours, bool IsApproximate);

/// <summary>
/// #237: costo de retroceso de una recepción con rechazo, como un viaje de ida y vuelta bodega de origen → punto.
/// Usa el calculador de costo de servicio de #240 sin modificarlo (con su respaldo de km y horas de
/// <see cref="LogisticsCostCalculator"/>). No se suma al costo de la ruta original.
/// </summary>
public static class BacktrackCostEstimator
{
    // Motivos que obligan a volver con producto; Sobrante, precinto, lote o frío no implican reentrega por sí solos.
    private static readonly HashSet<string> RedeliveryReasons =
    [
        ReceiptRejectionReasons.Faltante,
        ReceiptRejectionReasons.Daño,
        ReceiptRejectionReasons.ErrorRotulado
    ];

    public static bool RequiresRedelivery(string? reason) =>
        ReceiptRejectionReasons.TryNormalize(reason, out var canonical) && RedeliveryReasons.Contains(canonical);

    /// <param name="outbound">Tramo bodega → punto.</param>
    /// <param name="inbound">Tramo punto → bodega.</param>
    /// <param name="vehicle">Vehículo de la ruta original; sin él se usa <c>DefaultCostPerKm</c>.</param>
    /// <param name="redeliveryWeightKg">Peso de lo que se vuelve a llevar (carga para el consumo de #240).</param>
    public static BacktrackEstimate Estimate(Leg outbound, Leg inbound, Vehicle? vehicle, decimal redeliveryWeightKg, LogisticsOptions options)
    {
        var km = Math.Max(0m, outbound.Km) + Math.Max(0m, inbound.Km);
        var minutes = Math.Max(0m, outbound.Minutes) + Math.Max(0m, inbound.Minutes);
        // Sin paradas: no hay ayudantes ni merma de carga, solo el viaje.
        var cost = new ServiceCostCalculator(options).Calculate(km, minutes, vehicle, Math.Max(0m, redeliveryWeightKg), []);
        return new BacktrackEstimate(
            Math.Round(km, 2, MidpointRounding.AwayFromZero),
            cost.Total,
            Math.Round(minutes / 60m, 2, MidpointRounding.AwayFromZero),
            outbound.Source == DistanceSource.Approximation || inbound.Source == DistanceSource.Approximation);
    }
}
