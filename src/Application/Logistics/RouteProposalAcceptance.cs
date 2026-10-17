using System.Text.Json.Nodes;
using IstpetDev.Application.Common;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Application.Logistics;

/// <summary>Retira borradores competidores de la misma familia de revisiones, conservando el cálculo elegido completo.</summary>
public static class RouteProposalAcceptance
{
    public const string Discarded = "Discarded";
    public const string BatchField = "proposalBatchId";

    public static async Task<IReadOnlyList<Guid>> ResolveBatchAsync(IApplicationDbContext db, RoutePlan plan, CancellationToken ct)
    {
        if (plan.SupersedesRoutePlanId.HasValue) return [plan.Id];
        var input = JsonNode.Parse(plan.InputJson)?.AsObject();
        if (Guid.TryParse(input?[BatchField]?.ToString(), out var batchId))
        {
            var text = batchId.ToString();
            // ponytail: volumen DemoCore; mover el lote a columna indexada si crece el historial.
            // InputJson es jsonb: filtrar tras materializar evita aplicar LIKE a jsonb.
            var candidates = await db.RoutePlans.AsNoTracking().Where(p => p.OrganizationId == plan.OrganizationId && p.SupersedesRoutePlanId == null)
                .Select(p => new { p.Id, p.InputJson }).ToListAsync(ct);
            return candidates.Where(p => JsonNode.Parse(p.InputJson)?[BatchField]?.ToString() == text).Select(p => p.Id).ToList();
        }
        // Compatibilidad con cálculos ya guardados: la respuesta de #287 identifica sus vehículos sin inferir por fecha.
        var receipts = await db.IdempotencyRecords.AsNoTracking().Where(r => r.OrganizationId == plan.OrganizationId
            && r.ActionCode == "CreateRoutePlans" && r.ResponseStatus == 201 && r.ResponseJson != null)
            .Select(r => r.ResponseJson!).ToListAsync(ct);
        foreach (var receipt in receipts)
        {
            if (JsonNode.Parse(receipt)?["routes"] is not JsonArray routes) continue;
            var ids = routes.Select(r => Guid.TryParse((r?["id"] ?? r?["routePlanId"])?.ToString(), out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Contains(plan.Id)) return ids;
        }
        return [plan.Id];
    }

    public static async Task<int> AcceptAsync(IApplicationDbContext db, IReadOnlyList<Guid> chosenIds, CancellationToken ct)
    {
        await using var transaction = await OrganizationTransaction.BeginAsync(db, ct);
        var chosen = await db.RoutePlans.Where(p => p.OrganizationId == DemoOrganization.Id && chosenIds.Contains(p.Id)).ToListAsync(ct);
        if (chosen.Count == 0 || chosen.Count != chosenIds.Distinct().Count() || chosen.Any(p => p.Status is not ("Proposed" or "Published")))
            throw new InvalidOperationException("La propuesta ya no está disponible para aprobación.");
        var codes = chosen.Select(p => p.Code).ToList();
        var candidates = await db.RoutePlans.Where(p => p.OrganizationId == DemoOrganization.Id && p.Status == "Proposed"
            && p.AlgorithmVersion == "2.0-deterministic" && p.SupersedesRoutePlanId == null
            && codes.Contains(p.Code) && !chosenIds.Contains(p.Id)).ToListAsync(ct);
        var competingIds = new HashSet<Guid>();
        foreach (var candidate in candidates)
            foreach (var id in await ResolveBatchAsync(db, candidate, ct)) competingIds.Add(id);
        var competitors = await db.RoutePlans.Where(p => p.OrganizationId == DemoOrganization.Id && competingIds.Contains(p.Id)
            && p.Status == "Proposed" && p.AlgorithmVersion == "2.0-deterministic" && p.SupersedesRoutePlanId == null && !chosenIds.Contains(p.Id)).ToListAsync(ct);
        if (competitors.Any(p => JsonNode.Parse(p.InputJson)?["proposalAccepted"]?.GetValue<bool>() == true))
            throw new InvalidOperationException("Ya se aprobó otra propuesta de esta familia de rutas. Genera un cálculo nuevo antes de continuar.");
        var competitorIds = competitors.Select(p => p.Id).ToList();
        // Estos borradores del motor no comprometen pedidos ni stock. No descartar operaciones con efectos activos.
        if (await db.RouteStockReservations.AnyAsync(r => competitorIds.Contains(r.RoutePlanId) && r.OrganizationId == DemoOrganization.Id, ct)
            || await db.Deliveries.AnyAsync(d => d.OrganizationId == DemoOrganization.Id && d.RouteStopId != null
                && db.RouteStops.Any(s => s.Id == d.RouteStopId && competitorIds.Contains(s.RoutePlanId)), ct))
            throw new InvalidOperationException("Una propuesta competidora ya tiene efectos operativos. Revisa su estado antes de aprobar.");
        var acceptedAt = DateTime.UtcNow;
        foreach (var plan in chosen)
        {
            var input = JsonNode.Parse(plan.InputJson)!.AsObject();
            input["proposalAccepted"] = true;
            plan.InputJson = input.ToJsonString();
        }
        foreach (var plan in competitors)
        {
            plan.Status = Discarded;
            var input = JsonNode.Parse(plan.InputJson)!.AsObject();
            input["discardedByRoutePlanId"] = chosen[0].Id;
            input["discardedAtUtc"] = acceptedAt;
            plan.InputJson = input.ToJsonString();
        }
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return competitors.Count;
    }
}
