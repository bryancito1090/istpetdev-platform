using System.Text.Json;
using IstpetDev.Application.Common;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Field;

public sealed record FieldSeedReport(bool Applied, bool AlreadyApplied, int Routes, int Receipts,
    int ReceiptsWithRejections, int ReceiptsWithAdjustments, int FieldRequests,
    decimal DispatchedUnits, decimal AcceptedUnits, decimal RejectedUnits, decimal RejectedUnitPercent,
    string Source = "Muestra sintética de demostración; cantidades derivadas del consumo y stock del entorno.");

/// <summary>Carga operativa explícita, sin reset ni migración. El ensayo completo revierte su transacción.</summary>
public sealed class FieldActivitySeed(IApplicationDbContext db, IFieldService field, IRoutePublisher publisher, IRouteEvaluator evaluator)
{
    public const string Prefix = "SYN-NOV-V1-";
    private const string ActorIssuer = "urn:istpetdev:synthetic:field-activity-v1";
    private sealed record Demand(ServicePoint Point, Product Product, decimal Daily, decimal Stock);
    private sealed record Item(Demand Demand, Location Warehouse, decimal Quantity);

    public async Task<FieldSeedReport> RunAsync(bool apply, CancellationToken ct = default)
    {
        await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
        if (transaction is null) throw new InvalidOperationException("La carga requiere una transacción propia de PostgreSQL.");
        if (await db.RoutePlans.AnyAsync(p => p.OrganizationId == DemoOrganization.Id && p.Code.StartsWith(Prefix), ct))
            return await ReportAsync(apply, true, ct);

        var points = await db.ServicePoints.Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive && p.Position != null).ToListAsync(ct);
        var products = await db.Products.Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive).ToDictionaryAsync(p => p.Id, ct);
        var locations = await db.Locations.Where(l => l.OrganizationId == DemoOrganization.Id && l.IsActive).ToListAsync(ct);
        var pointLocations = locations.Where(l => l.ServicePointId != null).ToDictionary(l => l.ServicePointId!.Value);
        var warehouses = locations.Where(l => l.Kind == LocationKinds.Warehouse && l.Position != null).ToList();
        var vehicles = await db.Vehicles.Where(v => v.OrganizationId == DemoOrganization.Id && v.IsActive).OrderBy(v => v.Code).ToListAsync(ct);
        var lots = await db.Lots.Where(l => l.OrganizationId == DemoOrganization.Id && !l.IsBlocked
            && (l.ExpiresOn == null || l.ExpiresOn > DateOnly.FromDateTime(DateTime.UtcNow))).ToDictionaryAsync(l => l.Id, ct);
        var balances = await db.StockBalances.Where(b => b.OrganizationId == DemoOrganization.Id).ToListAsync(ct);
        var consumption = await db.Consumptions.AsNoTracking().Where(c => c.OrganizationId == DemoOrganization.Id
            && c.SourceKind != "FieldReconciliation" && c.PeriodDays > 0 && c.Quantity > 0).ToListAsync(ct);
        var last = consumption.Select(c => c.OccurredOn.AddDays(c.PeriodDays)).DefaultIfEmpty().Max();
        var windowStart = last.AddDays(-14);
        var observations = consumption.Select(c => new { Consumption = c,
            Days = Math.Max(0, c.OccurredOn.AddDays(c.PeriodDays).DayNumber - Math.Max(c.OccurredOn.DayNumber, windowStart.DayNumber)) })
            .Where(c => c.Days > 0);
        var rates = observations.GroupBy(c => (c.Consumption.ServicePointId, c.Consumption.ProductId))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Consumption.Quantity * c.Days / c.Consumption.PeriodDays) / g.Sum(c => c.Days));
        var demands = points.Where(p => pointLocations.ContainsKey(p.Id)).SelectMany(point =>
            products.Values.Where(product => rates.ContainsKey((point.Id, product.Id))).Select(product =>
                new Demand(point, product, rates[(point.Id, product.Id)], balances.Where(b => b.LocationId == pointLocations[point.Id].Id
                    && lots.TryGetValue(b.LotId, out var lot) && lot.ProductId == product.Id).Sum(b => b.QuantityOnHand))))
            .Where(d => d.Daily >= 1)
            .OrderBy(d => d.Stock / d.Daily).ThenBy(d => d.Point.Code).ThenBy(d => d.Product.Sku)
            .DistinctBy(d => d.Point.Id).Take(12).ToList();
        var available = warehouses.SelectMany(w => products.Values.Select(product => new
            { Warehouse = w, Product = product, Available = balances.Where(b => b.LocationId == w.Id
                && lots.TryGetValue(b.LotId, out var lot) && lot.ProductId == product.Id)
                .Sum(b => Math.Max(0, b.QuantityOnHand * .9m - b.QuantityReserved)) }))
            .Where(x => x.Available >= 20 && vehicles.Any(v => v.DepotLocationId == x.Warehouse.Id))
            .ToList();
        var sourced = demands.Select(d => new { Demand = d, Source = available.Where(a => a.Product.Id == d.Product.Id)
            .OrderBy(a => DistanceProvider.HaversineDistanceKm(Position(a.Warehouse), Position(d.Point))).FirstOrDefault() })
            .Where(x => x.Source is not null).ToList();
        var items = sourced.Select(x => new Item(x.Demand, x.Source!.Warehouse,
            // ponytail: muestra pequeña; máximo 10% del saldo libre tras seguridad por bodega/producto.
            Math.Floor(Math.Min(x.Demand.Daily * 2, x.Source.Available * .1m /
                sourced.Count(other => other.Source!.Warehouse.Id == x.Source.Warehouse.Id && other.Demand.Product.Id == x.Demand.Product.Id)))))
            .Where(i => i.Quantity >= 2).ToList();
        if (items.Count < 6) throw new InvalidOperationException("No hay al menos seis puntos con consumo, ubicación y stock suficiente para una muestra coherente.");

        var warehouseActor = await ActorAsync("warehouse", "Bodega", ct);
        var driver = await ActorAsync("driver", "Conductor", ct);
        var receiver = await ActorAsync("receiver", "Receptor", ct);
        await db.SaveChangesAsync(ct);
        var receiptIndex = 0;
        var routeIndex = 0;
        var nextStart = new Dictionary<Guid, DateTime>();
        foreach (var group in items.GroupBy(i => i.Warehouse.Id))
        {
            var warehouse = group.First().Warehouse;
            var depotVehicles = vehicles.Where(v => v.DepotLocationId == warehouse.Id).ToList();
            foreach (var chunk in group.Chunk(6))
            {
                var vehicle = depotVehicles[routeIndex % depotVehicles.Count];
                var start = nextStart.GetValueOrDefault(vehicle.Id, DateTime.UtcNow.Date.AddHours(12)); // 07:00 Guayaquil.
                var pending = chunk.ToList();
                var ordered = new List<Item>();
                var position = Position(warehouse);
                while (pending.Count > 0)
                {
                    var next = pending.OrderBy(i => DistanceProvider.HaversineDistanceKm(position, Position(i.Demand.Point))).First();
                    ordered.Add(next); pending.Remove(next); position = Position(next.Demand.Point);
                }
                var sequence = new List<RouteStopCandidate> { new(1, RouteStopKind.Pickup, null, warehouse.Id,
                    warehouse.Code, warehouse.Name, Position(warehouse), ordered.Sum(Weight), ordered.Sum(Volume),
                    new TimeOnly(7, 0), new TimeOnly(18, 0), 10) };
                sequence.AddRange(ordered.Select((item, index) => new RouteStopCandidate(index + 2, RouteStopKind.Delivery,
                    item.Demand.Point.Id, null, item.Demand.Point.Code, item.Demand.Point.Name, Position(item.Demand.Point),
                    -Weight(item), -Volume(item), item.Demand.Point.WindowStartLocal ?? new TimeOnly(8, 0),
                    item.Demand.Point.WindowEndLocal ?? new TimeOnly(17, 0), item.Demand.Point.ServiceMinutes ?? 10,
                    [new StopLoadDto(item.Demand.Product.Id, item.Demand.Product.Sku, item.Quantity, item.Demand.Product.BaseUnit)])));
                var evaluation = await evaluator.EvaluateSequenceAsync(sequence, vehicle, start, ct);
                if (!evaluation.IsValid) throw new InvalidOperationException("El recorrido de demostración no es viable: " + string.Join("; ", evaluation.ValidationErrors));
                var plan = new RoutePlan { OrganizationId = DemoOrganization.Id, Code = Prefix + (++routeIndex).ToString("D2"),
                    VehicleId = vehicle.Id, Revision = 1, Status = "Proposed", ProposedBy = "Demostración sintética #330",
                    AlgorithmVersion = "synthetic-field-activity-v1", PlannedStartUtc = start,
                    PlannedEndUtc = evaluation.Stops.Last().PlannedDepartureUtc, TotalKm = evaluation.TotalDistanceKm,
                    TotalMinutes = evaluation.TotalDurationMinutes, TotalCost = evaluation.TotalCost,
                    InputJson = JsonSerializer.Serialize(new { synthetic = true, seed = Prefix, coordinatesSource = evaluation.CoordinatesSource,
                        methodology = "Consumo de los últimos 14 días del dataset; entrega hasta 2 días; reposición a 10 días. No son estadísticas reales.",
                        stops = sequence.Select(s => new { sequence = s.Sequence, pointCode = s.Code, kind = s.Kind,
                            loads = s.Loads ?? [] }) }) };
                db.RoutePlans.Add(plan);
                foreach (var stop in evaluation.Stops)
                    db.RouteStops.Add(new RouteStop { OrganizationId = DemoOrganization.Id, RoutePlanId = plan.Id,
                        SequenceNumber = stop.Sequence, ServicePointId = stop.ServicePointId ?? ordered[0].Demand.Point.Id, LocationId = stop.LocationId,
                        Kind = stop.Kind, PlannedArrivalUtc = stop.PlannedArrivalUtc, PlannedDepartureUtc = stop.PlannedDepartureUtc,
                        Status = stop.Kind == RouteStopKind.Pickup ? RouteStopStatus.PickedUp : RouteStopStatus.Pending });
                await db.SaveChangesAsync(ct);
                var published = await publisher.PublishAsync(new PublishRequest(plan.Id,
                    new Dictionary<string, string> { [vehicle.Code] = driver.Id.ToString() }), ct);
                foreach (var delivery in published.Deliveries)
                {
                    var demand = ordered.Single(i => i.Demand.Point.Id == delivery.ServicePointId).Demand;
                    var lines = await db.DeliveryLines.Where(l => l.OrganizationId == DemoOrganization.Id && l.DeliveryId == delivery.Id).ToListAsync(ct);
                    var hu = await field.CreateHandlingUnitAsync(delivery.Id, new CreateHandlingUnitRequest(BaseEntity.NewId(),
                        lines.Select(l => new CreateHandlingUnitLineRequest(l.Id, l.LotId, l.PlannedQuantity)).ToList()), warehouseActor.Id, ct);
                    await field.DispatchAsync(delivery.Id, new DispatchRequest(BaseEntity.NewId(), [hu.HandlingUnitId]), warehouseActor.Id, Prefix, ct);
                    await field.PickupAsync(delivery.Id, new CustodyTransitionRequest(BaseEntity.NewId()), driver.Id, Prefix, ct);
                    await field.DeliverAsync(delivery.Id, new CustodyTransitionRequest(BaseEntity.NewId()), driver.Id, Prefix, ct);
                    var index = receiptIndex++;
                    var requestLines = new List<ReceiptLineRequest>();
                    var first = true;
                    foreach (var line in lines)
                    {
                        var rejected = first && index is 1 or 5 ? Math.Min(1m, line.DispatchedQuantity) : 0m;
                        decimal? declared = null;
                        var adjustment = 0m;
                        if (first && index is 2 or 7)
                        {
                            var balance = await db.StockBalances.SingleOrDefaultAsync(b => b.OrganizationId == DemoOrganization.Id
                                && b.LocationId == pointLocations[demand.Point.Id].Id && b.LotId == line.LotId, ct);
                            var previous = balance?.QuantityOnHand ?? 0m;
                            declared = index == 2 && previous > 0 ? Math.Max(0, previous - Math.Max(1m, Math.Floor(demand.Daily * .1m))) : previous + 1m;
                            adjustment = declared.Value - previous;
                        }
                        var accepted = line.DispatchedQuantity - rejected;
                        var requested = first ? Math.Max(0, Math.Ceiling(demand.Daily * 10 - demand.Stock
                            - lines.Sum(l => l.DispatchedQuantity) + rejected - adjustment)) : 0;
                        requestLines.Add(new ReceiptLineRequest(line.Id, line.LotId, accepted, rejected, demand.Product.BaseUnit,
                            rejected > 0 ? index == 1 ? ReceiptRejectionReasons.Daño : ReceiptRejectionReasons.ErrorRotulado : null,
                            declared, requested > 0 ? requested : null));
                        first = false;
                    }
                    await field.CreateReceiverReceiptAsync(hu.HandlingUnitId, hu.Token,
                        new CreateReceiptRequest(BaseEntity.NewId(), delivery.Version, DateTime.UtcNow, requestLines), receiver.Id, Prefix, ct);
                }
                nextStart[vehicle.Id] = evaluation.Stops.Last().PlannedDepartureUtc.AddMinutes(15);
            }
        }
        var fieldOrders = await db.DeliveryOrders.Where(o => o.OrganizationId == DemoOrganization.Id && o.SourceType == "FieldRequest"
            && o.Notes != null && !o.Notes.StartsWith(Prefix)).ToListAsync(ct);
        // Solo pedidos de recibos de esta carga: el código de entrega contiene el código de su plan sintético.
        foreach (var order in fieldOrders.Where(o => o.OrderNumber.Contains(Prefix)))
            order.Notes = Prefix + " [SINTÉTICO] Reposición calculada a 10 días de consumo. " + order.Notes;
        await db.SaveChangesAsync(ct);
        var report = await ReportAsync(apply, false, ct);
        if (report.ReceiptsWithRejections < 1 || report.ReceiptsWithAdjustments < 1 || report.FieldRequests < 1)
            throw new InvalidOperationException("La muestra no cubre rechazos, diferencias y reposición; no se aplicó ningún cambio.");
        if (apply) await transaction.CommitAsync(ct);
        else await transaction.RollbackAsync(ct);
        return report;
    }

    private async Task<Account> ActorAsync(string subject, string role, CancellationToken ct)
    {
        var actor = await db.Accounts.SingleOrDefaultAsync(a => a.OrganizationId == DemoOrganization.Id && a.Issuer == ActorIssuer && a.Subject == subject, ct);
        if (actor is not null) return actor;
        actor = new Account { OrganizationId = DemoOrganization.Id, Kind = "technical", Issuer = ActorIssuer, Subject = subject,
            DisplayName = "[Demo sintética] " + role, Role = role, IsActive = true };
        db.Accounts.Add(actor); return actor;
    }

    private async Task<FieldSeedReport> ReportAsync(bool applied, bool already, CancellationToken ct)
    {
        var planIds = await db.RoutePlans.Where(p => p.OrganizationId == DemoOrganization.Id && p.Code.StartsWith(Prefix)).Select(p => p.Id).ToListAsync(ct);
        var deliveryIds = await db.Deliveries.Where(d => d.OrganizationId == DemoOrganization.Id
            && db.RouteStops.Any(s => s.Id == d.RouteStopId && planIds.Contains(s.RoutePlanId))).Select(d => d.Id).ToListAsync(ct);
        var receipts = await db.Receipts.Where(r => r.OrganizationId == DemoOrganization.Id && deliveryIds.Contains(r.DeliveryId)).Select(r => r.Id).ToListAsync(ct);
        var lines = await db.ReceiptLines.Where(l => l.OrganizationId == DemoOrganization.Id && receipts.Contains(l.ReceiptId)).ToListAsync(ct);
        var lineIds = lines.Select(l => l.Id).ToList();
        var adjustedIds = await db.InventoryMovements.Where(m => m.OrganizationId == DemoOrganization.Id && m.Kind == MovementKinds.FieldAdjustment
            && m.ReceiptLineId != null && lineIds.Contains(m.ReceiptLineId.Value)).Select(m => m.ReceiptLineId!.Value).ToListAsync(ct);
        var rejected = lines.Sum(l => l.QuantityRejected); var accepted = lines.Sum(l => l.QuantityAccepted);
        return new FieldSeedReport(applied, already, planIds.Count, receipts.Count,
            lines.Where(l => l.QuantityRejected > 0).Select(l => l.ReceiptId).Distinct().Count(),
            lines.Where(l => adjustedIds.Contains(l.Id)).Select(l => l.ReceiptId).Distinct().Count(),
            await db.DeliveryOrders.CountAsync(o => o.OrganizationId == DemoOrganization.Id && o.SourceType == "FieldRequest" && o.OrderNumber.Contains(Prefix), ct),
            accepted + rejected, accepted, rejected, accepted + rejected == 0 ? 0 : Math.Round(100 * rejected / (accepted + rejected), 2));
    }
    private static decimal Weight(Item item) => item.Quantity * item.Demand.Product.WeightPerBaseUnit;
    private static decimal Volume(Item item) => item.Quantity * item.Demand.Product.VolumePerBaseUnit;
    private static GeoPoint Position(Location l) => new((decimal)l.Position!.Y, (decimal)l.Position.X);
    private static GeoPoint Position(ServicePoint p) => new((decimal)p.Position!.Y, (decimal)p.Position.X);
}
