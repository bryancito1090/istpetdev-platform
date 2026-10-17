using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IstpetDev.Application.Logistics;

public sealed record EvaluatedStopDto(
    int Sequence,
    string Kind,
    Guid? ServicePointId,
    Guid? LocationId,
    string Code,
    string Name,
    GeoPoint Position,
    DateTime PlannedArrivalUtc,
    DateTime PlannedDepartureUtc,
    decimal LegDistanceKm,
    decimal LegDurationMinutes,
    decimal AccumulatedWeightOnArrival,
    decimal AccumulatedVolumeOnArrival,
    bool ViolatesPrecedence,
    bool ViolatesWindow,
    string? WarningMessage
)
{
    // #245: factor de hora pico aplicado al tramo que llega a esta parada; null fuera de hora pico.
    public decimal? TrafficFactorApplied { get; init; }
}

public sealed record RouteEvaluationResult(
    bool IsValid,
    decimal TotalDistanceKm,
    decimal TotalDurationMinutes,
    decimal TotalCost,
    decimal MaxWeightOnRoute,
    decimal MaxVolumeOnRoute,
    IReadOnlyList<EvaluatedStopDto> Stops,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<double[]> Coordinates,
    // "osrm": recorrido por calles; "approximate": rectas entre paradas (sin servicio de rutas).
    string CoordinatesSource = "approximate",
    ServiceCostBreakdown? ServiceCostBreakdown = null
);

public interface IRouteEvaluator
{
    Task<RouteEvaluationResult> EvaluateSequenceAsync(
        IReadOnlyList<RouteStopCandidate> sequence,
        Vehicle? vehicle,
        DateTime plannedStartUtc,
        CancellationToken ct = default);
}

public sealed class RouteEvaluator : IRouteEvaluator
{
    private readonly IDistanceProvider _distanceProvider;
    private readonly LogisticsOptions _options;
    private readonly IRouteGeometryService? _geometry;
    private readonly IApplicationDbContext? _db;

    public RouteEvaluator(
        IDistanceProvider distanceProvider,
        IOptions<LogisticsOptions> options,
        IRouteGeometryService? geometry = null,
        IApplicationDbContext? db = null)
    {
        _distanceProvider = distanceProvider;
        _options = options.Value;
        _geometry = geometry;
        _db = db;
    }

    public async Task<RouteEvaluationResult> EvaluateSequenceAsync(
        IReadOnlyList<RouteStopCandidate> sequence,
        Vehicle? vehicle,
        DateTime plannedStartUtc,
        CancellationToken ct = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (sequence.Count == 0)
        {
            return new RouteEvaluationResult(false, 0, 0, 0, 0, 0, Array.Empty<EvaluatedStopDto>(), new[] { "La ruta no contiene paradas." }, Array.Empty<string>(), Array.Empty<double[]>());
        }

        var serviceCost = await ServiceCostCalculator.LoadAsync(_options, sequence, _db, ct);
        var routeHelperCount = serviceCost.HelperCount(sequence);
        sequence = sequence.Select(s => s with { ServiceMinutes = serviceCost.ServiceMinutes(s, vehicle, routeHelperCount) }).ToArray();

        // 1. Validar precedencia: Ninguna entrega puede ocurrir antes de que se recoja en la bodega correspondiente
        // Se asegura que la primera parada sea una Recogida (Pickup)
        if (sequence[0].Kind != RouteStopKind.Pickup)
        {
            errors.Add("Infracción de precedencia: La primera parada debe ser obligatoriamente una Recogida (Pickup) en bodega.");
        }

        // 2. Calcular matriz de distancias
        var allPoints = sequence.Select(s => s.Position).Distinct().ToList();
        var distanceMatrix = await _distanceProvider.MatrixAsync(allPoints, ct);

        // 3. Simular itinerario
        var evaluatedStops = new List<EvaluatedStopDto>();
        var coordinates = new List<double[]>();

        DateTime currentTime = plannedStartUtc;
        decimal totalKm = 0m;
        decimal totalMinutes = 0m;
        decimal currentWeight = 0m;
        decimal currentVolume = 0m;
        decimal maxWeight = 0m;
        decimal maxVolume = 0m;

        for (int i = 0; i < sequence.Count; i++)
        {
            var stop = sequence[i];
            decimal legKm = 0m;
            decimal legMin = 0m;
            decimal speedFactor = 1m;

            if (i > 0)
            {
                var prevStop = sequence[i - 1];
                var key = (prevStop.Position, stop.Position);
                if (distanceMatrix.TryGetValue(key, out var leg))
                {
                    legKm = leg.Km;
                    legMin = leg.Minutes;
                }
                else
                {
                    legKm = DistanceProvider.HaversineDistanceKm(prevStop.Position, stop.Position) * _options.ApproxDetourFactor;
                    legMin = Math.Round((legKm / _options.ApproxSpeedKmh) * 60m, 1);
                }

                // #245: hora pico según la hora local de salida del tramo
                speedFactor = TrafficBands.SpeedFactorAt(_options.TrafficBands,
                    TimeOnly.FromDateTime(currentTime.AddHours(_options.FallbackUtcOffsetHours)));
                if (speedFactor != 1m) legMin = Math.Round(TrafficBands.Apply(legMin, speedFactor), 1);

                currentTime = currentTime.AddMinutes((double)legMin);
                totalKm += legKm;
                totalMinutes += legMin;
            }

            // Registrar coordenadas para trazado
            coordinates.Add(new double[] { (double)stop.Position.Lat, (double)stop.Position.Lon });

            // Verificar ventana horaria
            var localTime = TimeOnly.FromDateTime(currentTime.AddHours(_options.FallbackUtcOffsetHours));
            bool violatesWindow = false;
            string? warning = null;

            if (stop.Kind == RouteStopKind.Delivery)
            {
                if (localTime < stop.WindowStart)
                {
                    // Esperar a que abra
                    var waitMinutes = (stop.WindowStart.ToTimeSpan() - localTime.ToTimeSpan()).TotalMinutes;
                    currentTime = currentTime.AddMinutes(waitMinutes);
                    totalMinutes += (decimal)waitMinutes;
                }
                else if (localTime > stop.WindowEnd)
                {
                    violatesWindow = true;
                    var late = Math.Round((localTime.ToTimeSpan() - stop.WindowEnd.ToTimeSpan()).TotalMinutes, 0);
                    warning = $"Llegada tardía: {late} min después del cierre ({stop.WindowEnd:HH:mm})";
                    warnings.Add($"{stop.Code}: {warning}");
                }
            }

            // Aplicar carga en la parada
            currentWeight += stop.DeltaWeight;
            currentVolume += stop.DeltaVolume;

            if (currentWeight < 0)
            {
                errors.Add($"Infracción de inventario en parada #{stop.Sequence} ({stop.Code}): Se intenta entregar más mercancía de la que se ha recogido.");
            }

            if (currentWeight > maxWeight) maxWeight = currentWeight;
            if (currentVolume > maxVolume) maxVolume = currentVolume;

            // Tiempo de servicio en parada
            currentTime = currentTime.AddMinutes(stop.ServiceMinutes);
            totalMinutes += stop.ServiceMinutes;

            evaluatedStops.Add(new EvaluatedStopDto(
                stop.Sequence,
                stop.Kind,
                stop.ServicePointId,
                stop.LocationId,
                stop.Code,
                stop.Name,
                stop.Position,
                currentTime.AddMinutes(-stop.ServiceMinutes),
                currentTime,
                legKm,
                legMin,
                currentWeight,
                currentVolume,
                currentWeight < 0,
                violatesWindow,
                warning
            )
            { TrafficFactorApplied = TrafficBands.Reported(speedFactor) });
        }

        // 4. Validar capacidades de vehículo si está asignado
        if (vehicle != null)
        {
            if (vehicle.MaxWeight > 0 && maxWeight > vehicle.MaxWeight)
            {
                errors.Add($"El peso máximo en ruta ({maxWeight:F0} kg) excede la capacidad del vehículo {vehicle.Code} ({vehicle.MaxWeight:F0} kg).");
            }
            if (vehicle.MaxVolume.HasValue && vehicle.MaxVolume.Value > 0 && maxVolume > vehicle.MaxVolume.Value)
            {
                errors.Add($"El volumen máximo en ruta ({maxVolume:F1} m³) excede la capacidad del vehículo {vehicle.Code} ({vehicle.MaxVolume.Value:F1} m³).");
            }
        }

        // #244: la evaluación/confirmación también aplica la regla; cambiar el vehículo en el cliente no la omite.
        if (vehicle is not null && VehicleRestrictions.Configured(_options))
        {
            var regions = await VehicleRestrictions.RegionsAsync(_db, sequence, ct);
            var timeZone = _db is null ? null : await _db.Organizations.AsNoTracking()
                .Where(o => o.Id == DemoOrganization.Id).Select(o => o.TimeZone).FirstOrDefaultAsync(ct);
            var startLocal = VehicleRestrictions.LocalTime(plannedStartUtc, timeZone, _options.FallbackUtcOffsetHours);
            var endLocal = VehicleRestrictions.LocalTime(currentTime, timeZone, _options.FallbackUtcOffsetHours);
            var reason = VehicleRestrictions.DiscardReason(_options, vehicle, startLocal, endLocal, regions);
            if (reason is not null) errors.Add(reason);
            warnings.AddRange(VehicleRestrictions.Warnings(_options, vehicle, regions));
        }

        // 5. Validar jornada laboral máxima permitida
        if (_options.WorkdayHours > 0 && totalMinutes > _options.WorkdayHours * 60)
        {
            errors.Add($"La duración total de la ruta ({totalMinutes:F0} min) excede la jornada máxima permitida ({_options.WorkdayHours * 60} min).");
        }

        var breakdown = serviceCost.Calculate(totalKm, totalMinutes, vehicle, maxWeight, sequence);
        decimal totalCost = breakdown.Total;

        bool isValid = errors.Count == 0;

        // El mapa del asistente (rutas por pedidos y de emergencia) dibuja el recorrido por calles si OSRM
        // responde; los km y el costo de la evaluación no cambian.
        var coordinatesSource = "approximate";
        if (_geometry is not null && coordinates.Count > 1 && await _geometry.RoadPathAsync(coordinates, ct) is { } road)
        {
            coordinates = road.Coordinates.ToList();
            coordinatesSource = "osrm";
        }

        return new RouteEvaluationResult(
            isValid,
            Math.Round(totalKm, 1),
            Math.Round(totalMinutes, 0),
            totalCost,
            maxWeight,
            maxVolume,
            evaluatedStops,
            errors,
            warnings,
            coordinates,
            coordinatesSource,
            breakdown
        );
    }
}
