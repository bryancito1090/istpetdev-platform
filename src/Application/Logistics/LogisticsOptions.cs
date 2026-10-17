namespace IstpetDev.Application.Logistics;

public sealed class LogisticsOptions
{
    public const string SectionName = "Logistics";

    public ServiceCostOptions? ServiceCost { get; set; }
    public ClockOptions Clock { get; set; } = new();
    public string WorkdayStart { get; set; } = "07:00";
    public int WorkdayHours { get; set; } = 10;
    public int FallbackUtcOffsetHours { get; set; } = -5;
    public int DemandWindowDays { get; set; } = 14;
    public decimal DefaultLeadTimeDays { get; set; } = 2m;
    public decimal SafetyMarginDays { get; set; } = 3m;
    public decimal ConsolidationExtraDays { get; set; } = 3m;
    public decimal TargetCoverageDays { get; set; } = 10m;
    public decimal SafetyStockUnits { get; set; } = 0m;
    public decimal WarehouseSafetyStockPct { get; set; } = 10m;
    public int StaleStockDays { get; set; } = 7;
    // Días que cubre el consumo derivado del stock en percha declarado en una recepción (#111).
    public int FieldReconciliationPeriodDays { get; set; } = 7;
    public string DefaultWindowStart { get; set; } = "08:00";
    public string DefaultWindowEnd { get; set; } = "17:00";
    public int DefaultServiceMinutes { get; set; } = 15;
    public decimal DefaultCostPerKm { get; set; } = 0.45m;
    public decimal CostPerHour { get; set; } = 6.00m;
    public int MaxDetourMinutes { get; set; } = 20;
    public decimal ApproxDetourFactor { get; set; } = 1.3m;
    public decimal ApproxSpeedKmh { get; set; } = 40m;
    public RebalancingOptions Rebalancing { get; set; } = new();
    public OsrmOptions Osrm { get; set; } = new();
    public PolicyOptions Policy { get; set; } = new();
}

public sealed class RebalancingOptions
{
    public int ConsumptionHistoryDays { get; set; } = 90;
    public decimal MinTransferThresholdPct { get; set; } = 10m; // 10% del stock objetivo
    public decimal MinCoverageDifferenceDays { get; set; } = 15m; // Mínimo 15 días de diferencia
    public int CooldownDaysAfterDismiss { get; set; } = 7;
    public decimal DefaultTargetCoverageDays { get; set; } = 30m;
}

public sealed class ClockOptions
{
    public ClockMode Mode { get; set; } = ClockMode.Dataset;
    public DateOnly? FixedDate { get; set; }
}

public sealed class OsrmOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 3;
    // Solo para dibujar el recorrido por calles en el mapa (servicio route de OSRM). No cambia las
    // distancias del plan, que siguen saliendo de BaseUrl, la caché de tramos o la aproximación.
    public string RouteGeometryUrl { get; set; } = string.Empty;
    public int RouteGeometryTimeoutSeconds { get; set; } = 8;
}

public sealed class PolicyOptions
{
    public const string SectionName = "Agent:Policy";

    public string Mode { get; set; } = "AutoWithLimits";
    public decimal MaxCostIncreasePct { get; set; } = 15m;
    public decimal MaxDelayMinutes { get; set; } = 60m;
    public bool AutoPublishInitialPlans { get; set; } = false;
}
