using System.Text.Json;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Application.Logistics.Planning;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/route-plans")]
public sealed class RoutePlansController(
    IApplicationDbContext db,
    IRoutePlanner planner,
    IRoutePublisher publisher,
    IReplanner replanner,
    IExplanationTemplates templates,
    IOperationalClock clock,
    ISourcingService sourcingService,
    IVehicleMatcher vehicleMatcher,
    IRouteEvaluator routeEvaluator,
    IEmergencyPlanner emergencyPlanner) : ControllerBase
{
    // FR-23: Lectura para el panel 005 (no crea propuestas)
    [HttpGet]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> ListRoutePlans(
        [FromQuery] DateOnly? date = null,
        [FromQuery] string? status = null,
        [FromQuery] string? vehicleCode = null,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        var query = db.RoutePlans.Where(p => p.OrganizationId == DemoOrganization.Id);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (date.HasValue)
        {
            var startUtc = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endUtc = date.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(p => p.PlannedStartUtc >= startUtc && p.PlannedStartUtc <= endUtc);
        }

        var vehicles = await db.Vehicles.Where(v => v.OrganizationId == DemoOrganization.Id).ToDictionaryAsync(v => v.Id, v => v.Code, ct);

        if (!string.IsNullOrWhiteSpace(vehicleCode))
        {
            var vehicleId = vehicles.FirstOrDefault(x => x.Value == vehicleCode).Key;
            query = query.Where(p => p.VehicleId == vehicleId);
        }

        var items = await query
            .OrderByDescending(p => p.PlannedStartUtc)
            .ThenBy(p => p.Code)
            .ThenByDescending(p => p.Revision)
            .Take(limit)
            .ToListAsync(ct);

        var planIds = items.Select(p => p.Id).ToList();
        var stopsCountDict = await db.RouteStops
            .Where(s => planIds.Contains(s.RoutePlanId))
            .GroupBy(s => s.RoutePlanId)
            .Select(g => new { RoutePlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.RoutePlanId, g => g.Count, ct);

        // Drivers asignados por ruta (#164)
        var planStopIds = await db.RouteStops.AsNoTracking()
            .Where(s => planIds.Contains(s.RoutePlanId))
            .Select(s => new { s.Id, s.RoutePlanId })
            .ToListAsync(ct);
        var stopToPlan = planStopIds.ToDictionary(s => s.Id, s => s.RoutePlanId);
        var allStopIds = planStopIds.Select(s => s.Id).ToList();

        var deliveriesWithDriver = await db.Deliveries.AsNoTracking()
            .Where(d => d.RouteStopId.HasValue && allStopIds.Contains(d.RouteStopId.Value) && d.AssignedAccountId.HasValue && d.OrganizationId == DemoOrganization.Id)
            .Select(d => new { StopId = d.RouteStopId!.Value, DriverId = d.AssignedAccountId!.Value })
            .ToListAsync(ct);

        var allDriverIds = deliveriesWithDriver.Select(d => d.DriverId).Distinct().ToList();
        var allDrivers = await db.Accounts.AsNoTracking()
            .Where(a => allDriverIds.Contains(a.Id) && a.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(a => a.Id, a => a.DisplayName, ct);

        var driversByPlan = deliveriesWithDriver
            .GroupBy(d => stopToPlan[d.StopId])
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new { Id = x.DriverId, Name = allDrivers.GetValueOrDefault(x.DriverId, "") })
                      .Where(x => !string.IsNullOrEmpty(x.Name))
                      .GroupBy(x => x.Id)
                      .Select(grp => grp.First())
                      .ToList()
            );

        var responseItems = items.Select(p =>
        {
            var planDrivers = driversByPlan.GetValueOrDefault(p.Id, new());
            return new
            {
                routePlanId = p.Id,
                id = p.Id,
                code = p.Code,
                revision = p.Revision,
                status = p.Status,
                vehicleId = p.VehicleId,
                vehicleCode = vehicles.GetValueOrDefault(p.VehicleId, ""),
                driverAccountId = planDrivers.Count == 1 ? planDrivers[0].Id : (Guid?)null,
                driverName = string.Join(", ", planDrivers.Select(d => d.Name)),
                km = p.TotalKm ?? 0m,
                totalDistanceKm = p.TotalKm ?? 0m,
                minutes = p.TotalMinutes ?? 0m,
                totalDurationMinutes = p.TotalMinutes ?? 0m,
                cost = p.TotalCost ?? 0m,
                plannedStartUtc = p.PlannedStartUtc,
                stopsCount = stopsCountDict.GetValueOrDefault(p.Id, 0),
                hasApproximations = false
            };
        });

        return Ok(new
        {
            items = responseItems,
            nextCursor = (string?)null
        });
    }

    // Recorrido por calles para el mapa (OSRM route); rectas aproximadas si OSRM no está disponible.
    [HttpGet("{id:guid}/geometry")]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> GetGeometry([FromRoute] Guid id, [FromServices] IRouteGeometryService geometry, CancellationToken ct = default)
    {
        var result = await geometry.GetAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct = default)
    {
        var plan = await db.RoutePlans.FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == DemoOrganization.Id, ct);
        if (plan == null) return NotFound();

        var stops = await db.RouteStops
            .Where(s => s.RoutePlanId == plan.Id)
            .OrderBy(s => s.SequenceNumber)
            .ToListAsync(ct);

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == plan.VehicleId, ct);

        // #160 & #164: bodega, vehículo, conductor y entregas asociadas a la ruta.
        var stopIdsOfPlan = stops.Select(s => s.Id).ToList();
        var planDeliveries = await db.Deliveries.AsNoTracking()
            .Where(d => d.RouteStopId.HasValue && stopIdsOfPlan.Contains(d.RouteStopId.Value) && d.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var originId = planDeliveries.Select(d => (Guid?)d.OriginLocationId).FirstOrDefault() ?? vehicle?.DepotLocationId;
        var origin = originId is { } oid ? await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == oid, ct) : null;
        var driverIds = planDeliveries.Where(d => d.AssignedAccountId.HasValue).Select(d => d.AssignedAccountId!.Value).Distinct().ToList();
        var drivers = await db.Accounts.AsNoTracking().Where(a => driverIds.Contains(a.Id) && a.OrganizationId == DemoOrganization.Id).Select(a => new { a.Id, a.DisplayName }).ToListAsync(ct);

        var deliveryIds = planDeliveries.Select(d => d.Id).ToList();
        var planDeliveryLines = await db.DeliveryLines.AsNoTracking()
            .Where(dl => deliveryIds.Contains(dl.DeliveryId) && dl.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);

        var planHandlingUnits = await db.HandlingUnits.AsNoTracking()
            .Where(hu => hu.DeliveryId.HasValue && deliveryIds.Contains(hu.DeliveryId.Value) && hu.OrganizationId == DemoOrganization.Id)
            .ToListAsync(ct);
        var husByDelivery = planHandlingUnits.GroupBy(hu => hu.DeliveryId!.Value).ToDictionary(g => g.Key, g => g.ToList());
        var huById = planHandlingUnits.ToDictionary(hu => hu.Id, hu => hu);

        var lotIds = planDeliveryLines.Select(dl => dl.LotId).Distinct().ToList();
        var lots = await db.Lots.AsNoTracking()
            .Where(l => lotIds.Contains(l.Id) && l.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(l => l.Id, ct);

        var allProductIds = new HashSet<Guid>(lots.Values.Select(l => l.ProductId));

        // Cargas planificadas de InputJson por si la ruta no tiene entregas creadas aún (propuesta)
        var plannedLoadsBySeq = new Dictionary<int, List<(Guid ProductId, string Sku, decimal Quantity, string BaseUnit)>>();
        string? polyline = null;
        List<double[]>? coordinates = null;
        IReadOnlyList<Guid> orderIds = [];
        try
        {
            if (!string.IsNullOrWhiteSpace(plan.InputJson))
            {
                using var doc = JsonDocument.Parse(plan.InputJson);
                var root = doc.RootElement;
                orderIds = OrderStopLoads.OrderIds(root);
                if (root.TryGetProperty("polyline", out var polyElem))
                    polyline = polyElem.GetString();
                if (root.TryGetProperty("coordinates", out var coordsElem) && coordsElem.ValueKind == JsonValueKind.Array)
                {
                    coordinates = JsonSerializer.Deserialize<List<double[]>>(coordsElem.GetRawText());
                }
                if (root.TryGetProperty("stops", out var sProp) && sProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var sElem in sProp.EnumerateArray())
                    {
                        if (sElem.TryGetProperty("sequence", out var seqElem))
                        {
                            int seq = seqElem.GetInt32();
                            var loadsList = new List<(Guid, string, decimal, string)>();
                            if (sElem.TryGetProperty("loads", out var loadsElem) && loadsElem.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var lElem in loadsElem.EnumerateArray())
                                {
                                    var pId = lElem.TryGetProperty("productId", out var p1) ? p1.GetGuid() : (lElem.TryGetProperty("ProductId", out var p2) ? p2.GetGuid() : Guid.Empty);
                                    var sku = lElem.TryGetProperty("sku", out var s1) ? s1.GetString() : (lElem.TryGetProperty("Sku", out var s2) ? s2.GetString() : "");
                                    var qty = lElem.TryGetProperty("quantity", out var q1) ? q1.GetDecimal() : (lElem.TryGetProperty("Quantity", out var q2) ? q2.GetDecimal() : 0m);
                                    var unit = lElem.TryGetProperty("baseUnit", out var b1) ? b1.GetString() : (lElem.TryGetProperty("BaseUnit", out var b2) ? b2.GetString() : "U");
                                    if (pId != Guid.Empty) allProductIds.Add(pId);
                                    loadsList.Add((pId, sku ?? "", qty, unit ?? "U"));
                                }
                            }
                            plannedLoadsBySeq[seq] = loadsList;
                        }
                    }
                }
            }
        }
        catch { }

        // Rutas del asistente: guardan los pedidos y no las cargas; se muestran las líneas pendientes de cada punto.
        if (orderIds.Count > 0)
        {
            var fromOrders = new Dictionary<int, List<StopLoad>>();
            await OrderStopLoads.FillAsync(db, orderIds,
                stops.Where(s => !plannedLoadsBySeq.TryGetValue(s.SequenceNumber, out var l) || l.Count == 0), fromOrders, ct);
            foreach (var (seq, loads) in fromOrders)
            {
                plannedLoadsBySeq[seq] = loads.Select(l => (l.ProductId, l.Sku, l.Quantity, l.BaseUnit)).ToList();
                foreach (var load in loads) allProductIds.Add(load.ProductId);
            }
        }

        var pickupLocationIds = stops.Where(s => s.Kind == RouteStopKind.Pickup && s.LocationId.HasValue).Select(s => s.LocationId!.Value).ToList();
        var pickupLocations = await db.Locations.AsNoTracking().Where(l => pickupLocationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);

        var products = await db.Products.AsNoTracking()
            .Where(p => allProductIds.Contains(p.Id) && p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, ct);

        var servicePointIds = stops.Select(s => s.ServicePointId).Distinct().ToList();
        var servicePoints = await db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id && servicePointIds.Contains(sp.Id))
            .ToDictionaryAsync(sp => sp.Id, ct);

        // Mapeo detallado de paradas con productos, lotes, bultos y entregas (#164)
        var stopLinesById = stops.ToDictionary(s => s.Id, s =>
        {
            var stopDeliveries = planDeliveries.Where(d => d.RouteStopId == s.Id).ToList();
            var stopLines = new List<object>();
            if (s.Kind == RouteStopKind.Pickup) return stopLines;
            if (stopDeliveries.Count > 0)
            {
                foreach (var d in stopDeliveries)
                {
                    var dLines = planDeliveryLines.Where(dl => dl.DeliveryId == d.Id).ToList();
                    var dHus = husByDelivery.GetValueOrDefault(d.Id, new List<HandlingUnit>());
                    foreach (var dl in dLines)
                    {
                        lots.TryGetValue(dl.LotId, out var lot);
                        Product? prod = null;
                        if (lot != null) products.TryGetValue(lot.ProductId, out prod);
                        var hu = dl.HandlingUnitId.HasValue ? huById.GetValueOrDefault(dl.HandlingUnitId.Value) : dHus.FirstOrDefault();

                        stopLines.Add(new
                        {
                            deliveryId = (Guid?)d.Id,
                            deliveryCode = d.Code,
                            deliveryStatus = d.Status,
                            productId = lot?.ProductId ?? Guid.Empty,
                            sku = prod?.Sku ?? "",
                            productName = prod?.Name ?? prod?.Sku ?? "Producto",
                            lotId = (Guid?)dl.LotId,
                            lotCode = lot?.Code ?? "",
                            lotExpiresOn = lot?.ExpiresOn?.ToString("yyyy-MM-dd"),
                            plannedQuantity = dl.PlannedQuantity,
                            dispatchedQuantity = dl.DispatchedQuantity,
                            acceptedQuantity = dl.AcceptedQuantity,
                            rejectedQuantity = dl.RejectedQuantity,
                            baseUnit = prod?.BaseUnit ?? "U",
                            handlingUnitId = hu?.Id,
                            handlingUnitCode = hu?.Code ?? "",
                            handlingUnitStatus = hu?.Status ?? ""
                        });
                    }
                }
            }
            else if (plannedLoadsBySeq.TryGetValue(s.SequenceNumber, out var loads))
            {
                foreach (var load in loads)
                {
                    products.TryGetValue(load.ProductId, out var prod);
                    stopLines.Add(new
                    {
                        deliveryId = (Guid?)null,
                        deliveryCode = (string?)null,
                        deliveryStatus = "Proposed",
                        productId = load.ProductId,
                        sku = !string.IsNullOrWhiteSpace(load.Sku) ? load.Sku : prod?.Sku ?? "",
                        productName = prod?.Name ?? load.Sku,
                        lotId = (Guid?)null,
                        lotCode = "",
                        lotExpiresOn = (string?)null,
                        plannedQuantity = load.Quantity,
                        dispatchedQuantity = 0m,
                        acceptedQuantity = 0m,
                        rejectedQuantity = 0m,
                        baseUnit = !string.IsNullOrWhiteSpace(load.BaseUnit) ? load.BaseUnit : prod?.BaseUnit ?? "U",
                        handlingUnitId = (Guid?)null,
                        handlingUnitCode = "",
                        handlingUnitStatus = ""
                    });
                }
            }
            return stopLines;
        });

        // En una recogida en bodega se carga lo que se entrega hasta la siguiente recogida.
        var pickupLoads = new Dictionary<Guid, List<dynamic>>();
        List<dynamic>? loading = null;
        foreach (var s in stops)
        {
            if (s.Kind == RouteStopKind.Pickup) { loading = pickupLoads[s.Id] = []; continue; }
            loading?.AddRange(stopLinesById[s.Id].Cast<dynamic>());
        }
        foreach (var (pickupId, delivered) in pickupLoads)
        {
            stopLinesById[pickupId] = delivered
                .GroupBy(l => ((Guid)l.productId, (string)l.sku))
                .Select(g => (object)new
                {
                    deliveryId = (Guid?)null,
                    deliveryCode = (string?)null,
                    deliveryStatus = "Cargar",
                    productId = g.Key.Item1,
                    sku = g.Key.Item2,
                    productName = (string)g.First().productName,
                    lotId = (Guid?)null,
                    lotCode = string.Join(", ", g.Select(l => (string)l.lotCode).Where(c => !string.IsNullOrEmpty(c)).Distinct()),
                    lotExpiresOn = (string?)null,
                    plannedQuantity = g.Sum(l => (decimal)l.plannedQuantity),
                    dispatchedQuantity = g.Sum(l => (decimal)l.dispatchedQuantity),
                    acceptedQuantity = 0m,
                    rejectedQuantity = 0m,
                    baseUnit = (string)g.First().baseUnit,
                    handlingUnitId = (Guid?)null,
                    handlingUnitCode = "",
                    handlingUnitStatus = ""
                })
                .ToList();
        }

        var stopDetailList = stops.Select(s =>
        {
            servicePoints.TryGetValue(s.ServicePointId, out var sp);
            var pickupLocation = s.LocationId is { } locId ? pickupLocations.GetValueOrDefault(locId) : null;
            var stopDeliveries = planDeliveries.Where(d => d.RouteStopId == s.Id).ToList();
            var primaryDelivery = stopDeliveries.FirstOrDefault();
            var stopHus = stopDeliveries.SelectMany(d => husByDelivery.GetValueOrDefault(d.Id, new List<HandlingUnit>())).GroupBy(hu => hu.Id).Select(g => g.First()).ToList();
            var stopLines = stopLinesById[s.Id];

            return new
            {
                sequenceNumber = s.SequenceNumber,
                kind = s.Kind.ToString(),
                sequence = s.SequenceNumber,
                servicePointId = s.ServicePointId,
                servicePointCode = pickupLocation?.Code ?? sp?.Code ?? "",
                servicePointName = pickupLocation?.Name ?? sp?.Name ?? sp?.Code ?? "",
                status = s.Status,
                plannedArrivalUtc = s.PlannedArrivalUtc,
                plannedDepartureUtc = s.PlannedDepartureUtc,
                etaUtc = s.PlannedArrivalUtc,
                actualArrivalUtc = s.ActualArrivalUtc,
                serviceMinutes = sp?.ServiceMinutes ?? 15,
                windowStart = sp?.WindowStartLocal.HasValue == true ? sp.WindowStartLocal.Value.ToString("HH:mm") : "08:00",
                windowEnd = sp?.WindowEndLocal.HasValue == true ? sp.WindowEndLocal.Value.ToString("HH:mm") : "17:00",
                lat = sp?.Position != null ? (double?)sp.Position.Y : null,
                lon = sp?.Position != null ? (double?)sp.Position.X : null,
                deliveryId = primaryDelivery?.Id,
                deliveryCode = primaryDelivery?.Code ?? "",
                deliveryStatus = primaryDelivery?.Status ?? (plan.Status == "Proposed" ? "Proposed" : s.Status),
                handlingUnits = stopHus.Select(hu => new
                {
                    id = hu.Id,
                    code = hu.Code,
                    status = hu.Status,
                    locationId = hu.LocationId,
                    sealedAtUtc = hu.SealedAtUtc
                }),
                lines = stopLines
            };
        }).ToList();

        // Resumen agregado por producto de la ruta (#164)
        var deliveryStopsDetail = stopDetailList.Where(s => s.kind != nameof(RouteStopKind.Pickup)).ToList();
        var allLines = deliveryStopsDetail.SelectMany(s => s.lines.Cast<dynamic>()).ToList();
        var productsSummary = allLines
            .GroupBy(l => (string)l.sku)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g =>
            {
                var first = g.First();
                var lotsGrouped = g
                    .Where(x => !string.IsNullOrEmpty((string)x.lotCode))
                    .GroupBy(x => (string)x.lotCode)
                    .Select(lg => new { lotCode = lg.Key, quantity = lg.Sum(x => (decimal)x.plannedQuantity) })
                    .ToList();

                var stopsCount = deliveryStopsDetail.Count(s => s.lines.Cast<dynamic>().Any(l => (string)l.sku == g.Key));
                var statuses = g.Select(x => (string)x.deliveryStatus).Where(st => !string.IsNullOrEmpty(st)).Distinct().ToList();

                return new
                {
                    sku = g.Key,
                    productName = (string)first.productName,
                    totalPlannedQuantity = g.Sum(x => (decimal)x.plannedQuantity),
                    baseUnit = (string)first.baseUnit,
                    stopsCount,
                    lots = lotsGrouped,
                    deliveryStatuses = statuses
                };
            })
            .OrderBy(p => p.sku)
            .ToList();

        // Resumen de rutas que lleva el conductor (#164)
        var driverRoutesList = new List<object>();
        if (drivers.Count > 0)
        {
            var assignedDriverId = drivers[0].Id;
            var driverDeliveries = await db.Deliveries.AsNoTracking()
                .Where(d => d.AssignedAccountId == assignedDriverId && d.RouteStopId.HasValue && d.OrganizationId == DemoOrganization.Id)
                .Select(d => d.RouteStopId!.Value)
                .ToListAsync(ct);

            var driverPlanIds = await db.RouteStops.AsNoTracking()
                .Where(s => driverDeliveries.Contains(s.Id) && s.OrganizationId == DemoOrganization.Id)
                .Select(s => s.RoutePlanId)
                .Distinct()
                .ToListAsync(ct);

            var driverPlans = await db.RoutePlans.AsNoTracking()
                .Where(p => driverPlanIds.Contains(p.Id) && p.OrganizationId == DemoOrganization.Id)
                .OrderByDescending(p => p.PlannedStartUtc)
                .ToListAsync(ct);

            var driverVehicleIds = driverPlans.Select(p => p.VehicleId).Distinct().ToList();
            var driverVehicles = await db.Vehicles.AsNoTracking()
                .Where(v => driverVehicleIds.Contains(v.Id) && v.OrganizationId == DemoOrganization.Id)
                .ToDictionaryAsync(v => v.Id, v => v.Code, ct);

            var driverStops = await db.RouteStops.AsNoTracking()
                .Where(s => driverPlanIds.Contains(s.RoutePlanId) && s.OrganizationId == DemoOrganization.Id)
                .Select(s => new { s.Id, s.RoutePlanId })
                .ToListAsync(ct);

            var driverStopsCounts = driverStops
                .GroupBy(s => s.RoutePlanId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var dp in driverPlans)
            {
                driverRoutesList.Add(new
                {
                    id = dp.Id,
                    routePlanId = dp.Id,
                    code = dp.Code,
                    revision = dp.Revision,
                    status = dp.Status,
                    vehicleCode = driverVehicles.GetValueOrDefault(dp.VehicleId, ""),
                    plannedStartUtc = dp.PlannedStartUtc,
                    totalDistanceKm = dp.TotalKm ?? 0m,
                    totalDurationMinutes = dp.TotalMinutes ?? 0m,
                    stopsCount = driverStopsCounts.GetValueOrDefault(dp.Id, 0),
                    isCurrent = dp.Id == plan.Id
                });
            }
        }

        return Ok(new
        {
            routePlanId = plan.Id,
            id = plan.Id,
            code = plan.Code,
            revision = plan.Revision,
            status = plan.Status,
            vehicleId = plan.VehicleId,
            vehicleCode = vehicle?.Code ?? "",
            originWarehouseCode = origin?.Code ?? "",
            originWarehouseName = origin?.Name ?? "",
            driverAccountId = drivers.Count == 1 ? drivers[0].Id : (Guid?)null,
            driverName = string.Join(", ", drivers.Select(d => d.DisplayName)),
            deliveriesCount = planDeliveries.Count,
            unassignedDeliveries = planDeliveries.Count(d => d.AssignedAccountId == null),
            totalKm = plan.TotalKm,
            totalDistanceKm = plan.TotalKm ?? 0m,
            totalMinutes = plan.TotalMinutes,
            totalDurationMinutes = plan.TotalMinutes ?? 0m,
            totalCost = plan.TotalCost,
            plannedStartUtc = plan.PlannedStartUtc,
            plannedEndUtc = plan.PlannedEndUtc ?? plan.PlannedStartUtc.AddMinutes((double)(plan.TotalMinutes ?? 0m)),
            polyline = polyline ?? "",
            coordinates = coordinates,
            stops = stopDetailList,
            productsSummary,
            driverRoutes = driverRoutesList
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> CreatePlan([FromBody] PlanRequest? request = null, CancellationToken ct = default)
    {
        var req = request ?? new PlanRequest(null, null, true);
        var result = await planner.PlanAsync(req, ct);

        var planDate = req.Date ?? await clock.TodayAsync(ct);
        var agentRunId = await GetAgentRunIdAsync("Watch", ct);

        // Guardar cada ruta como un RoutePlan con estado Proposed
        var createdPlans = new List<RoutePlan>();
        var agentDecisions = new Dictionary<Guid, Guid>();
        foreach (var r in result.Routes)
        {
            var baseCode = $"PLAN-{planDate:yyyyMMdd}-{r.VehicleCode}";

            // Verificar si ya existe una revisión para este código y calcular la siguiente revisión
            var existingMaxRevision = await db.RoutePlans
                .Where(p => p.OrganizationId == DemoOrganization.Id && p.Code == baseCode)
                .MaxAsync(p => (int?)p.Revision, ct) ?? 0;

            var revision = existingMaxRevision + 1;

            var plan = new RoutePlan
            {
                OrganizationId = DemoOrganization.Id,
                Code = baseCode,
                Revision = revision,
                VehicleId = r.VehicleId,
                Status = "Proposed",
                ProposedBy = User.AccountId()?.ToString() ?? "System",
                PlannedStartUtc = r.Stops.FirstOrDefault()?.ArrivalUtc ?? DateTime.UtcNow,
                PlannedEndUtc = r.Stops.LastOrDefault()?.DepartureUtc ?? DateTime.UtcNow,
                TotalKm = r.Km,
                TotalMinutes = r.Minutes,
                TotalCost = r.Cost,
                AlgorithmVersion = "2.0-deterministic",
                InputJson = JsonSerializer.Serialize(new
                {
                    stops = r.Stops.Select(s => new
                    {
                        sequence = s.Sequence,
                        pointCode = s.PointCode,
                        isConsolidation = s.IsConsolidation,
                        loads = s.Loads
                    }),
                    legs = r.Legs
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            };
            db.RoutePlans.Add(plan);

            int seq = 1;
            foreach (var s in r.Stops)
            {
                db.RouteStops.Add(new RouteStop
                {
                    OrganizationId = DemoOrganization.Id,
                    RoutePlanId = plan.Id,
                    ServicePointId = s.ServicePointId,
                    SequenceNumber = seq++,
                    PlannedArrivalUtc = s.ArrivalUtc,
                    PlannedDepartureUtc = s.DepartureUtc,
                    Status = "Pending"
                });
            }

            createdPlans.Add(plan);

            if (agentRunId.HasValue)
            {
                var routeFacts = result.Facts.ToList();
                routeFacts.Add(new Fact("revision", r.VehicleCode, plan.Revision, "revision"));
                routeFacts.Add(new Fact("stops_count", r.VehicleCode, r.Stops.Count, "stops"));
                routeFacts.Add(new Fact("total_km", r.VehicleCode, Math.Round(r.Km, 2), "km"));
                routeFacts.Add(new Fact("total_minutes", r.VehicleCode, Math.Round(r.Minutes, 2), "min"));
                routeFacts.Add(new Fact("total_cost", r.VehicleCode, Math.Round(r.Cost, 2), "USD"));
                for (int i = 1; i <= r.Stops.Count + 4; i++)
                {
                    routeFacts.Add(new Fact("stop_sequence", $"stop_{i}", i, "sequence"));
                }
                foreach (var s in r.Stops)
                {
                    routeFacts.Add(new Fact("arrival_time", s.PointCode, (decimal)s.ArrivalUtc.TimeOfDay.TotalHours, "hora"));
                    routeFacts.Add(new Fact("arrival_time", s.PointCode, (decimal)s.ArrivalUtc.TimeOfDay.TotalMinutes, "min"));
                }

                var decision = new AgentDecision
                {
                    OrganizationId = DemoOrganization.Id,
                    Kind = "WATCH_ROUTE_PROPOSAL",
                    Status = "Proposed",
                    TriggerJson = JsonSerializer.Serialize(new { runId = agentRunId.Value, kind = "Watch" }),
                    ProposalJson = JsonSerializer.Serialize(new
                    {
                        proposedRoutePlanId = plan.Id,
                        revision = plan.Revision,
                        facts = routeFacts
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true }),
                    Rationale = templates.Explain("plan", routeFacts),
                    CorrelationId = agentRunId.Value,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.AgentDecisions.Add(decision);
                agentDecisions[plan.Id] = decision.Id;
            }
        }

        await db.SaveChangesAsync(ct);

        var allFacts = result.Facts.ToList();
        foreach (var p in createdPlans)
        {
            var r = result.Routes.FirstOrDefault(x => x.VehicleId == p.VehicleId);
            var vCode = r?.VehicleCode ?? "";
            allFacts.Add(new Fact("revision", vCode, p.Revision, "revision"));
            allFacts.Add(new Fact("stops_count", vCode, r?.Stops.Count ?? 0, "stops"));
        }

        var explanation = templates.Explain("plan", allFacts);

        return StatusCode(StatusCodes.Status201Created, new
        {
            routes = createdPlans.Select(p => new
            {
                routePlanId = p.Id,
                id = p.Id,
                code = p.Code,
                revision = p.Revision,
                status = p.Status,
                agentDecisionId = agentDecisions.TryGetValue(p.Id, out var decisionId) ? decisionId : (Guid?)null
            }),
            unserved = result.Unserved,
            facts = allFacts,
            explanation
        });
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> Publish([FromRoute] Guid id, [FromBody] PublishRequest? req = null, CancellationToken ct = default)
    {
        var request = new PublishRequest(id, req?.DriverAssignments, User.AccountId());
        try
        {
            var res = await publisher.PublishAsync(request, ct);
            return Ok(res);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "invalid_driver", detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "publish_conflict", detail: ex.Message);
        }
    }

    // #160: reasignar el conductor de una ruta ya publicada (entregas pendientes).
    [HttpPut("{id:guid}/driver")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> AssignDriver([FromRoute] Guid id, [FromBody] AssignDriverRequest req, CancellationToken ct = default)
    {
        try
        {
            var updated = await publisher.AssignDriverAsync(id, req.AccountId, ct);
            return Ok(new { routePlanId = id, driverAccountId = req.AccountId, updatedDeliveries = updated });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "invalid_driver", detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "assign_conflict", detail: ex.Message);
        }
    }

    [HttpPost("{id:guid}/stops/{sequence:int}/complete")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Conductor)]
    public async Task<IActionResult> CompleteStop(
        [FromRoute] Guid id,
        [FromRoute] int sequence,
        [FromBody] CompleteStopRequest? req = null,
        CancellationToken ct = default)
    {
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        if (userRole == Roles.Conductor)
        {
            var userAccountId = User.AccountId();
            var stopEntity = await db.RouteStops.FirstOrDefaultAsync(s => s.RoutePlanId == id && s.SequenceNumber == sequence && s.OrganizationId == DemoOrganization.Id, ct);
            if (stopEntity == null) return NotFound();

            var hasAssignedDelivery = await db.Deliveries.AnyAsync(d => d.RouteStopId == stopEntity.Id && d.AssignedAccountId == userAccountId && d.OrganizationId == DemoOrganization.Id, ct);
            if (!hasAssignedDelivery)
            {
                return Forbid();
            }
        }

        var request = new CompleteStopRequest(id, sequence, req?.ActualArrivalUtc, req?.ActualDepartureUtc);
        var stop = await publisher.CompleteStopAsync(request, ct);
        return Ok(stop);
    }

    [HttpPost("{id:guid}/replan")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> Replan([FromRoute] Guid id, [FromBody] ReplanRequestInput req, CancellationToken ct = default)
    {
        ReplanResult res;
        try { res = await replanner.ReplanAsync(id, req.IncidentId, ct); }
        catch (ReplanConflictException error) { return Conflict(new ProblemDetails { Status = 409, Title = error.Message }); }
        var agentRunId = await GetAgentRunIdAsync("Incident", ct);
        if (agentRunId.HasValue && res.AgentDecisionId.HasValue)
        {
            var decision = await db.AgentDecisions.FirstOrDefaultAsync(d => d.Id == res.AgentDecisionId.Value, ct);
            if (decision is not null)
            {
                decision.CorrelationId = agentRunId.Value;
                await db.SaveChangesAsync(ct);
            }
        }
        return StatusCode(StatusCodes.Status201Created, res);
    }

    private async Task<Guid?> GetAgentRunIdAsync(string kind, CancellationToken ct)
    {
        if (!User.IsInRole(Roles.Automatizacion) ||
            !Request.Headers.TryGetValue("X-Agent-Run-Id", out var value) ||
            !Guid.TryParse(value, out var runId)) return null;

        return await db.AgentRuns.AnyAsync(
            r => r.Id == runId && r.OrganizationId == DemoOrganization.Id && r.Status == "Running" && r.Kind == kind,
            ct)
            ? runId
            : null;
    }

    [HttpPost("{id:guid}/revert")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> Revert([FromRoute] Guid id, CancellationToken ct = default)
    {
        var res = await replanner.RevertPlanAsync(new RevertPlanRequest(id, User.AccountId()), ct);
        return Ok(res);
    }

    [HttpPost("suggest")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> SuggestRoutes([FromBody] SuggestRoutesRequest req, CancellationToken ct = default)
    {
        if (req == null || req.OrderIds == null || req.OrderIds.Count == 0)
        {
            return BadRequest(new { message = "Debe proporcionar al menos un pedido para sugerir rutas." });
        }

        var sourcingOptions = await sourcingService.PlanSourcingAsync(req.OrderIds, ct);
        var today = req.PlannedDate ?? await clock.TodayAsync(ct);
        var plannedStartUtc = today.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);

        var warehouses = await db.Locations
            .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse)
            .ToDictionaryAsync(l => l.Id, ct);

        var products = await db.Products
            .Where(p => p.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(p => p.Id, ct);

        var optionsResult = new List<object>();

        foreach (var opt in sourcingOptions)
        {
            // Construir secuencia de paradas preliminar (primero Pickups en bodegas, luego Deliveries)
            var candidates = new List<RouteStopCandidate>();
            int seq = 1;

            // Agrupar recogidas por bodega
            var pickupsByWarehouse = opt.Lines
                .SelectMany(l => l.Allocations.Select(a => new { Line = l, Allocation = a }))
                .GroupBy(x => x.Allocation.WarehouseId);

            foreach (var whGroup in pickupsByWarehouse)
            {
                var wh = warehouses.GetValueOrDefault(whGroup.Key);
                var whPos = wh?.Position != null ? new GeoPoint((decimal)wh.Position.Y, (decimal)wh.Position.X) : new GeoPoint(-0.2295m, -78.5243m);

                decimal totalWeight = 0m;
                decimal totalVol = 0m;
                foreach (var item in whGroup)
                {
                    if (products.TryGetValue(item.Line.ProductId, out var prod))
                    {
                        totalWeight += item.Allocation.Quantity * prod.WeightPerBaseUnit;
                        totalVol += item.Allocation.Quantity * prod.VolumePerBaseUnit;
                    }
                }

                candidates.Add(new RouteStopCandidate(
                    seq++,
                    RouteStopKind.Pickup,
                    null,
                    whGroup.Key,
                    wh?.Code ?? "WH",
                    wh?.Name ?? "Bodega",
                    whPos,
                    totalWeight,
                    totalVol,
                    new TimeOnly(7, 0),
                    new TimeOnly(19, 0),
                    20
                ));
            }

            // Entregas por destino
            var deliveriesByDest = opt.Lines.GroupBy(l => l.DestinationId);
            foreach (var destGroup in deliveriesByDest)
            {
                var firstLine = destGroup.First();
                decimal totalWeight = 0m;
                decimal totalVol = 0m;

                foreach (var line in destGroup)
                {
                    if (products.TryGetValue(line.ProductId, out var prod))
                    {
                        totalWeight += line.TotalAllocated * prod.WeightPerBaseUnit;
                        totalVol += line.TotalAllocated * prod.VolumePerBaseUnit;
                    }
                }

                candidates.Add(new RouteStopCandidate(
                    seq++,
                    RouteStopKind.Delivery,
                    !firstLine.IsDestinationWarehouse ? firstLine.DestinationId : null,
                    firstLine.IsDestinationWarehouse ? firstLine.DestinationId : null,
                    firstLine.DestinationCode,
                    firstLine.DestinationName,
                    firstLine.DestinationPosition,
                    -totalWeight,
                    -totalVol,
                    new TimeOnly(8, 0),
                    new TimeOnly(17, 0),
                    15
                ));
            }

            // Evaluar secuencia con RouteEvaluator
            var eval = await routeEvaluator.EvaluateSequenceAsync(candidates, null, plannedStartUtc, ct);

            // Emparejar vehículos con VehicleMatcher
            var vehicleMatches = await vehicleMatcher.MatchVehiclesAsync(candidates, plannedStartUtc, ct);

            optionsResult.Add(new
            {
                optionId = opt.OptionId,
                title = opt.Title,
                description = opt.Description,
                isRecommended = opt.IsRecommended,
                evaluation = eval,
                stops = candidates,
                sourcingLines = opt.Lines,
                vehicleMatches = vehicleMatches.Select(m => new
                {
                    vehicleId = m.Vehicle.Id,
                    vehicleCode = m.Vehicle.Code,
                    maxWeight = m.Vehicle.MaxWeight,
                    maxVolume = m.Vehicle.MaxVolume,
                    isMatch = m.IsMatch,
                    discardReason = m.DiscardReason,
                    weightUtilizationPct = m.WeightUtilizationPct,
                    volumeUtilizationPct = m.VolumeUtilizationPct,
                    estimatedCost = m.EstimatedTotalCost,
                    distanceToFirstPickupKm = m.DistanceToFirstPickupKm
                })
            });
        }

        return Ok(new
        {
            options = optionsResult
        });
    }

    [HttpPost("evaluate")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> EvaluateRoute([FromBody] EvaluateRouteRequest req, CancellationToken ct = default)
    {
        if (req == null || req.Stops == null || req.Stops.Count == 0)
        {
            return BadRequest(new { message = "La secuencia no contiene paradas." });
        }

        Vehicle? vehicle = null;
        if (req.VehicleId.HasValue)
        {
            vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == req.VehicleId.Value, ct);
        }

        var startUtc = req.PlannedStartUtc ?? DateTime.UtcNow;
        var eval = await routeEvaluator.EvaluateSequenceAsync(req.Stops, vehicle, startUtc, ct);

        return Ok(eval);
    }

    [HttpPost("confirm")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> ConfirmRoutePlan([FromBody] ConfirmRoutePlanRequest req, CancellationToken ct = default)
    {
        if (req == null || req.Stops == null || req.Stops.Count == 0)
        {
            return BadRequest(new { message = "La ruta debe contener al menos una parada." });
        }

        var today = await clock.TodayAsync(ct);
        var vehicles = await db.Vehicles.Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive).ToListAsync(ct);
        var vehicle = (req.VehicleId.HasValue ? vehicles.FirstOrDefault(v => v.Id == req.VehicleId.Value) : null)
                      ?? vehicles.FirstOrDefault();

        var vehicleId = vehicle?.Id ?? (vehicles.Count > 0 ? vehicles[0].Id : Guid.NewGuid());
        var vehicleCode = vehicle?.Code ?? "C-01";

        var baseCode = string.IsNullOrWhiteSpace(req.Code)
            ? $"PLAN-{today:yyyyMMdd}-{vehicleCode}"
            : req.Code;

        var existingMaxRevision = await db.RoutePlans
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.Code == baseCode)
            .MaxAsync(p => (int?)p.Revision, ct) ?? 0;

        var revision = existingMaxRevision + 1;
        var now = DateTime.UtcNow;

        var plan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = baseCode,
            Revision = revision,
            VehicleId = vehicleId,
            Status = "Proposed",
            ProposedBy = User.AccountId()?.ToString() ?? "Planner",
            PlannedStartUtc = req.PlannedStartUtc ?? now,
            PlannedEndUtc = (req.PlannedStartUtc ?? now).AddMinutes((double)req.TotalDurationMinutes),
            TotalKm = req.TotalDistanceKm,
            TotalMinutes = req.TotalDurationMinutes,
            TotalCost = req.TotalCost > 0 ? req.TotalCost : Math.Round(req.TotalDistanceKm * (vehicle?.CostPerKm ?? 0.45m), 2),
            AlgorithmVersion = "order-based-routing-1.0",
            InputJson = JsonSerializer.Serialize(new
            {
                orderBased = true,
                orderIds = req.OrderIds,
                polyline = req.Polyline,
                coordinates = req.Coordinates,
                stops = req.Stops.Select(s => new
                {
                    sequence = s.Sequence,
                    kind = s.Kind,
                    code = s.Code,
                    name = s.Name,
                    servicePointId = s.ServicePointId,
                    locationId = s.LocationId
                })
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        };

        db.RoutePlans.Add(plan);

        // Crear RouteStops
        var defaultSp = await db.ServicePoints.Where(p => p.OrganizationId == DemoOrganization.Id).FirstOrDefaultAsync(ct);
        var defaultSpId = defaultSp?.Id ?? Guid.NewGuid();

        for (int i = 0; i < req.Stops.Count; i++)
        {
            var s = req.Stops[i];
            var arrival = (req.PlannedStartUtc ?? now).AddMinutes(i * 20);

            var stop = new RouteStop
            {
                OrganizationId = DemoOrganization.Id,
                RoutePlanId = plan.Id,
                ServicePointId = s.ServicePointId ?? defaultSpId,
                LocationId = s.LocationId,
                Kind = s.Kind ?? RouteStopKind.Delivery,
                SequenceNumber = s.Sequence > 0 ? s.Sequence : (i + 1),
                PlannedArrivalUtc = arrival,
                PlannedDepartureUtc = arrival.AddMinutes(15),
                Status = RouteStopStatus.Pending
            };
            db.RouteStops.Add(stop);
        }

        // Marcar pedidos como planificados
        if (req.OrderIds != null && req.OrderIds.Count > 0)
        {
            var ordersToUpdate = await db.DeliveryOrders
                .Where(o => o.OrganizationId == DemoOrganization.Id && req.OrderIds.Contains(o.Id))
                .ToListAsync(ct);

            foreach (var ord in ordersToUpdate)
            {
                ord.Status = DeliveryOrderStatus.Planned;
            }
        }

        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = plan.Id,
            routePlanId = plan.Id,
            code = plan.Code,
            revision = plan.Revision,
            status = plan.Status
        });
    }

    [HttpPost("emergency")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> EmergencyRoutes(CancellationToken ct = default)
    {
        // 1. Detectar demandas críticas en puntos de servicio y bodegas
        var demands = await emergencyPlanner.DetectEmergencyDemandsAsync(ct);

        if (demands.Count == 0)
        {
            return Ok(new
            {
                message = "No se detectaron puntos de entrega ni bodegas con nivel crítico de desabastecimiento.",
                demands = Array.Empty<object>(),
                orders = Array.Empty<object>()
            });
        }

        // 2. Crear pedidos de emergencia
        var orders = await emergencyPlanner.CreateEmergencyOrdersAsync(demands, ct);
        var orderIds = orders.Select(o => o.Id).ToList();

        // 3. Obtener sugerencias de abastecimiento y rutas para estos pedidos de emergencia
        var sourcingOptions = await sourcingService.PlanSourcingAsync(orderIds, ct);

        return Ok(new
        {
            message = $"Se generaron {orders.Count} pedidos de emergencia cubriendo {demands.Count} requerimientos críticos.",
            demands,
            orders = orders.Select(o => new { o.Id, o.OrderNumber, o.Priority, o.SourceType }),
            options = sourcingOptions
        });
    }

    [HttpPost("manual")]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> CreateManualPlan([FromBody] CreateManualPlanRequest req, CancellationToken ct = default)
    {
        if (req == null || req.Stops == null || req.Stops.Count == 0)
        {
            return BadRequest(new { message = "La ruta debe contener al menos una parada." });
        }

        var today = await clock.TodayAsync(ct);
        var vehicles = await db.Vehicles.Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive).ToListAsync(ct);
        var vehicle = (req.VehicleId.HasValue ? vehicles.FirstOrDefault(v => v.Id == req.VehicleId) : null)
                      ?? vehicles.FirstOrDefault();
        var vehicleId = vehicle?.Id ?? (vehicles.Count > 0 ? vehicles[0].Id : Guid.NewGuid());
        var vehicleCode = vehicle?.Code ?? "C-01";

        var baseCode = string.IsNullOrWhiteSpace(req.Code) 
            ? $"PLAN-{today:yyyyMMdd}-{vehicleCode}" 
            : req.Code;

        var existingMaxRevision = await db.RoutePlans
            .Where(p => p.OrganizationId == DemoOrganization.Id && p.Code == baseCode)
            .MaxAsync(p => (int?)p.Revision, ct) ?? 0;

        var revision = existingMaxRevision + 1;
        var now = DateTime.UtcNow;

        var plan = new RoutePlan
        {
            OrganizationId = DemoOrganization.Id,
            Code = baseCode,
            Revision = revision,
            VehicleId = vehicleId,
            Status = "Proposed",
            ProposedBy = User.AccountId()?.ToString() ?? "Planner",
            PlannedStartUtc = req.PlannedStartUtc ?? now,
            PlannedEndUtc = (req.PlannedStartUtc ?? now).AddMinutes((double)req.TotalDurationMinutes),
            TotalKm = req.TotalDistanceKm,
            TotalMinutes = req.TotalDurationMinutes,
            TotalCost = Math.Round(req.TotalDistanceKm * 0.45m, 2),
            AlgorithmVersion = "interactive-osrm-1.0",
            InputJson = JsonSerializer.Serialize(new
            {
                manual = true,
                polyline = req.Polyline,
                coordinates = req.Coordinates,
                stops = req.Stops.Select(s => new
                {
                    sequence = s.Sequence,
                    servicePointId = s.ServicePointId,
                    name = s.Name,
                    code = s.Code
                })
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        };

        db.RoutePlans.Add(plan);

        var allServicePoints = await db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id)
            .ToDictionaryAsync(sp => sp.Id, ct);
        var firstSp = allServicePoints.Values.FirstOrDefault();

        int seq = 1;
        foreach (var s in req.Stops)
        {
            var spId = s.ServicePointId;
            if (spId == Guid.Empty || !allServicePoints.ContainsKey(spId))
            {
                var match = allServicePoints.Values.FirstOrDefault(p => p.Code == s.Code);
                if (match != null)
                {
                    spId = match.Id;
                }
                else if (firstSp != null)
                {
                    spId = firstSp.Id;
                }
            }

            if (spId != Guid.Empty)
            {
                var arrival = s.PlannedArrivalUtc ?? now.AddMinutes(seq * 15);
                db.RouteStops.Add(new RouteStop
                {
                    OrganizationId = DemoOrganization.Id,
                    RoutePlanId = plan.Id,
                    ServicePointId = spId,
                    SequenceNumber = seq++,
                    PlannedArrivalUtc = arrival,
                    PlannedDepartureUtc = arrival.AddMinutes(15),
                    Status = "Pending"
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = plan.Id,
            routePlanId = plan.Id,
            code = plan.Code,
            revision = plan.Revision,
            status = plan.Status,
            routes = new[]
            {
                new
                {
                    id = plan.Id,
                    routePlanId = plan.Id,
                    code = plan.Code,
                    revision = plan.Revision,
                    status = plan.Status
                }
            }
        });
    }
}

public sealed record ReplanRequestInput(Guid IncidentId);

public sealed record SuggestRoutesRequest(
    IReadOnlyList<Guid> OrderIds,
    DateOnly? PlannedDate
);

public sealed record EvaluateRouteRequest(
    IReadOnlyList<RouteStopCandidate> Stops,
    Guid? VehicleId,
    DateTime? PlannedStartUtc
);

public sealed record AssignDriverRequest(Guid AccountId);

public sealed record ConfirmRoutePlanRequest(
    string? Code,
    Guid? VehicleId,
    decimal TotalDistanceKm,
    decimal TotalDurationMinutes,
    decimal TotalCost,
    DateTime? PlannedStartUtc,
    IReadOnlyList<Guid>? OrderIds,
    IReadOnlyList<RouteStopCandidate> Stops,
    string? Polyline,
    IReadOnlyList<double[]>? Coordinates
);

public sealed record CreateManualPlanRequest(
    string? Code,
    Guid? VehicleId,
    decimal TotalDistanceKm,
    decimal TotalDurationMinutes,
    DateTime? PlannedStartUtc,
    IReadOnlyList<ManualStopDto> Stops,
    string? Polyline,
    IReadOnlyList<double[]>? Coordinates
);

public sealed record ManualStopDto(
    Guid ServicePointId,
    string? Code,
    string? Name,
    int Sequence,
    DateTime? PlannedArrivalUtc
);

