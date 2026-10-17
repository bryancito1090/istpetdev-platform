namespace IstpetDev.Application.Simulation;

public sealed record SimulatedDelivery(
    string PointCode,
    string Sku,
    decimal Quantity,
    DateTime EtaUtc);

public sealed record StockoutRecord(
    string PointCode,
    string Sku,
    DateTime OccurredAtUtc,
    decimal UnservedQuantity);

public sealed record DailySimulationResult(
    DateOnly Date,
    string RegionCode,
    IReadOnlyList<StockoutRecord> Stockouts,
    IReadOnlyList<SimulationEventDto> Events);

public sealed class DailySimulationEngine
{
    private readonly Dictionary<(string PointCode, string Sku), decimal> _pointStock = new();
    private long _eventSequence;

    public string RegionCode { get; }
    public SimulationClock Clock { get; }

    public DailySimulationEngine(string regionCode, SimulationClock clock)
    {
        RegionCode = regionCode;
        Clock = clock;
    }

    public void SetInitialPointStock(string pointCode, string sku, decimal quantity)
    {
        _pointStock[(pointCode, sku)] = quantity;
    }

    public decimal GetPointStock(string pointCode, string sku)
    {
        return _pointStock.TryGetValue((pointCode, sku), out var q) ? q : 0m;
    }

    public IReadOnlyList<DisruptionEvent> GetKnownDisruptions(
        IEnumerable<DisruptionEvent> disruptions,
        DateTime asOfUtc)
    {
        return disruptions.Where(d => d.KnownAtUtc <= asOfUtc).ToList();
    }

    public DailySimulationResult ExecuteDay(
        DateOnly date,
        IReadOnlyList<DemandEvent> demands,
        IReadOnlyList<SimulatedDelivery> deliveries,
        IReadOnlyList<DisruptionEvent> disruptions)
    {
        var stockouts = new List<StockoutRecord>();
        var events = new List<SimulationEventDto>();

        var workdayStart = Clock.WorkdayStartUtc(date);
        var workdayEnd = Clock.WorkdayEndUtc(date);

        // Filtra los incidentes activos/conocidos en el día (disruptions cuya fecha coincida con date o estén activos antes del cierre de jornada)
        var dayDisruptions = disruptions
            .Where(d => DateOnly.FromDateTime(d.OccurredAtUtc) == date || d.KnownAtUtc <= workdayEnd)
            .ToList();

        var adjustedDeliveries = new List<SimulatedDelivery>();
        foreach (var del in deliveries)
        {
            var matching = dayDisruptions
                .Where(d => d.KnownAtUtc <= del.EtaUtc &&
                            (d.SubjectCode == del.PointCode ||
                             d.SubjectCode == "ALL" ||
                             d.RegionCode == RegionCode ||
                             d.RegionCode == "REG-ALL" ||
                             d.RegionCode == "ALL"))
                .ToList();

            var delayDisruptions = matching.Where(d => d.Factor > 1.0m).ToList();
            if (delayDisruptions.Count > 0)
            {
                var maxFactor = delayDisruptions.Max(d => d.Factor);
                var originalTravelMinutes = Math.Max(0, (del.EtaUtc - workdayStart).TotalMinutes);
                var adjustedEtaUtc = workdayStart.AddMinutes(originalTravelMinutes * (double)maxFactor);
                adjustedDeliveries.Add(del with { EtaUtc = adjustedEtaUtc });

                foreach (var d in delayDisruptions)
                {
                    events.Add(new SimulationEventDto(
                        Sequence: ++_eventSequence,
                        OccurredAtUtc: d.OccurredAtUtc,
                        Kind: "disruption_applied",
                        RegionCode: RegionCode,
                        PointCode: d.SubjectCode,
                        Sku: del.Sku,
                        Requested: d.Factor,
                        Satisfied: null,
                        Reason: d.Kind));
                }
            }
            else
            {
                adjustedDeliveries.Add(del);
            }
        }

        // Crear una lista cronológica unificada de eventos que ocurren en el día
        // Cada evento tiene un tiempo UTC y un tipo (Delivery o Demand)
        var timeline = new List<(DateTime TimeUtc, string Type, object Payload)>();

        foreach (var del in adjustedDeliveries)
        {
            timeline.Add((del.EtaUtc, "Delivery", del));
        }

        foreach (var dem in demands)
        {
            timeline.Add((dem.OccurredAtUtc, "Demand", dem));
        }

        // Ordenar cronológicamente. Si coinciden en la misma marca de tiempo, primero se procesa la entrega para simular arribo
        var orderedTimeline = timeline
            .OrderBy(e => e.TimeUtc)
            .ThenBy(e => e.Type == "Delivery" ? 0 : 1)
            .ToList();

        foreach (var (timeUtc, type, payload) in orderedTimeline)
        {
            Clock.AdvanceTo(timeUtc);

            if (type == "Delivery" && payload is SimulatedDelivery delivery)
            {
                var key = (delivery.PointCode, delivery.Sku);
                _pointStock[key] = GetPointStock(delivery.PointCode, delivery.Sku) + delivery.Quantity;

                events.Add(new SimulationEventDto(
                    Sequence: ++_eventSequence,
                    OccurredAtUtc: timeUtc,
                    Kind: "delivery_received",
                    RegionCode: RegionCode,
                    PointCode: delivery.PointCode,
                    Sku: delivery.Sku,
                    Requested: delivery.Quantity,
                    Satisfied: delivery.Quantity,
                    Reason: null));
            }
            else if (type == "Demand" && payload is DemandEvent demand)
            {
                var key = (demand.PointCode, demand.Sku);
                var currentStock = GetPointStock(demand.PointCode, demand.Sku);

                if (currentStock >= demand.Requested)
                {
                    // Demanda totalmente satisfecha
                    _pointStock[key] = currentStock - demand.Requested;

                    events.Add(new SimulationEventDto(
                        Sequence: ++_eventSequence,
                        OccurredAtUtc: timeUtc,
                        Kind: "demand_satisfied",
                        RegionCode: RegionCode,
                        PointCode: demand.PointCode,
                        Sku: demand.Sku,
                        Requested: demand.Requested,
                        Satisfied: demand.Requested,
                        Reason: null));
                }
                else
                {
                    // Quiebre parcial o total
                    var satisfied = Math.Max(0m, currentStock);
                    var unserved = demand.Requested - satisfied;
                    _pointStock[key] = 0m; // Se agotó el stock

                    stockouts.Add(new StockoutRecord(
                        demand.PointCode,
                        demand.Sku,
                        timeUtc,
                        unserved));

                    events.Add(new SimulationEventDto(
                        Sequence: ++_eventSequence,
                        OccurredAtUtc: timeUtc,
                        Kind: "stockout",
                        RegionCode: RegionCode,
                        PointCode: demand.PointCode,
                        Sku: demand.Sku,
                        Requested: demand.Requested,
                        Satisfied: satisfied,
                        Reason: "insufficient_point_stock"));
                }
            }
        }

        // Cierre de la jornada a las 18:00
        Clock.AdvanceTo(Clock.WorkdayEndUtc(date));

        return new DailySimulationResult(date, RegionCode, stockouts, events);
    }
}
