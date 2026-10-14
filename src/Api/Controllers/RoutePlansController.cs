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
    IOperationalClock clock) : ControllerBase
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

        var responseItems = items.Select(p => new
        {
            routePlanId = p.Id,
            id = p.Id,
            code = p.Code,
            revision = p.Revision,
            status = p.Status,
            vehicleId = p.VehicleId,
            vehicleCode = vehicles.GetValueOrDefault(p.VehicleId, ""),
            km = p.TotalKm ?? 0m,
            totalDistanceKm = p.TotalKm ?? 0m,
            minutes = p.TotalMinutes ?? 0m,
            totalDurationMinutes = p.TotalMinutes ?? 0m,
            cost = p.TotalCost ?? 0m,
            plannedStartUtc = p.PlannedStartUtc,
            stopsCount = stopsCountDict.GetValueOrDefault(p.Id, 0),
            hasApproximations = false
        });

        return Ok(new
        {
            items = responseItems,
            nextCursor = (string?)null
        });
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

        var servicePointIds = stops.Select(s => s.ServicePointId).Distinct().ToList();
        var servicePoints = await db.ServicePoints
            .Where(sp => sp.OrganizationId == DemoOrganization.Id && servicePointIds.Contains(sp.Id))
            .ToDictionaryAsync(sp => sp.Id, ct);

        return Ok(new
        {
            routePlanId = plan.Id,
            id = plan.Id,
            code = plan.Code,
            revision = plan.Revision,
            status = plan.Status,
            vehicleId = plan.VehicleId,
            vehicleCode = vehicle?.Code ?? "",
            totalKm = plan.TotalKm,
            totalDistanceKm = plan.TotalKm ?? 0m,
            totalMinutes = plan.TotalMinutes,
            totalDurationMinutes = plan.TotalMinutes ?? 0m,
            totalCost = plan.TotalCost,
            plannedStartUtc = plan.PlannedStartUtc,
            stops = stops.Select(s =>
            {
                servicePoints.TryGetValue(s.ServicePointId, out var sp);
                return new
                {
                    sequenceNumber = s.SequenceNumber,
                    sequence = s.SequenceNumber,
                    servicePointId = s.ServicePointId,
                    servicePointCode = sp?.Code ?? "",
                    servicePointName = sp?.Name ?? sp?.Code ?? "",
                    status = s.Status,
                    plannedArrivalUtc = s.PlannedArrivalUtc,
                    etaUtc = s.PlannedArrivalUtc,
                    actualArrivalUtc = s.ActualArrivalUtc
                };
            })
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> CreatePlan([FromBody] PlanRequest? request = null, CancellationToken ct = default)
    {
        var req = request ?? new PlanRequest(null, null, true);
        var result = await planner.PlanAsync(req, ct);

        var planDate = req.Date ?? await clock.TodayAsync(ct);

        // Guardar cada ruta como un RoutePlan con estado Proposed
        var createdPlans = new List<RoutePlan>();
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
        }

        await db.SaveChangesAsync(ct);

        var explanation = templates.Explain("plan", result.Facts);

        return StatusCode(StatusCodes.Status201Created, new
        {
            routes = createdPlans.Select(p => new
            {
                routePlanId = p.Id,
                id = p.Id,
                code = p.Code,
                revision = p.Revision,
                status = p.Status
            }),
            unserved = result.Unserved,
            facts = result.Facts,
            explanation
        });
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> Publish([FromRoute] Guid id, [FromBody] PublishRequest? req = null, CancellationToken ct = default)
    {
        var request = new PublishRequest(id, req?.DriverAssignments, User.AccountId());
        var res = await publisher.PublishAsync(request, ct);
        return Ok(res);
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
        var res = await replanner.ReplanAsync(id, req.IncidentId, ct);
        return StatusCode(StatusCodes.Status201Created, res);
    }

    [HttpPost("{id:guid}/revert")]
    [Authorize(Roles = Roles.Planning)]
    public async Task<IActionResult> Revert([FromRoute] Guid id, CancellationToken ct = default)
    {
        var res = await replanner.RevertPlanAsync(new RevertPlanRequest(id, User.AccountId()), ct);
        return Ok(res);
    }
}

public sealed record ReplanRequestInput(Guid IncidentId);
