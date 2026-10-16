namespace IstpetDev.Application.Logistics;

// Entrega candidata del asistente con la urgencia del pedido más exigente de ese destino.
public sealed record DeliveryStopDraft(
    RouteStopCandidate Stop,
    string Priority,
    DateTime RequiredDateUtc
);

// #197: ordena las entregas de /route-plans/suggest por prioridad y fecha requerida;
// dentro de cada nivel elige el destino más cercano a la parada anterior.
public static class DeliveryStopSequencer
{
    // Ventana y servicio que se usaban fijos antes de #197; quedan como respaldo si el punto no los tiene.
    public static readonly TimeOnly FallbackWindowStart = new(8, 0);
    public static readonly TimeOnly FallbackWindowEnd = new(17, 0);
    public const int FallbackServiceMinutes = 15;

    // Valores de DeliveryOrder.Priority; uno desconocido se trata como Normal.
    public static int PriorityRank(string? priority) => priority switch
    {
        "Critical" => 0,
        "High" => 1,
        "Normal" => 2,
        "Low" => 3,
        _ => 2
    };

    // De varios pedidos al mismo destino manda el más urgente.
    public static string MostUrgent(IEnumerable<string?> priorities) =>
        priorities.Select(p => p ?? "Normal").OrderBy(PriorityRank).FirstOrDefault() ?? "Normal";

    public static IReadOnlyList<DeliveryStopDraft> Order(IReadOnlyList<DeliveryStopDraft> drafts, GeoPoint start)
    {
        var result = new List<DeliveryStopDraft>(drafts.Count);
        var current = start;

        // Nivel = prioridad y día requerido; los niveles se recorren de más a menos urgente.
        var levels = drafts
            .GroupBy(d => (Rank: PriorityRank(d.Priority), Day: d.RequiredDateUtc.Date))
            .OrderBy(g => g.Key.Rank)
            .ThenBy(g => g.Key.Day);

        foreach (var level in levels)
        {
            var pending = level.ToList();
            while (pending.Count > 0)
            {
                // Vecino más cercano; empates por hora requerida y código para que el orden sea estable.
                var next = pending
                    .OrderBy(d => DistanceProvider.HaversineDistanceKm(current, d.Stop.Position))
                    .ThenBy(d => d.RequiredDateUtc)
                    .ThenBy(d => d.Stop.Code, StringComparer.Ordinal)
                    .First();
                result.Add(next);
                pending.Remove(next);
                current = next.Stop.Position;
            }
        }

        return result;
    }
}
