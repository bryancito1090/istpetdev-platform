using System.Text.Json;
using IstpetDev.Api.Authentication;
using IstpetDev.Application.Agent;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/incidents")]
public sealed class IncidentsController(
    IApplicationDbContext db,
    IIncidentEvaluator evaluator,
    IOptions<AgentOptions> agentOptions,
    IServiceScopeFactory scopes,
    ILogger<IncidentsController> logger) : ControllerBase
{
    private static readonly GeometryFactory Gf = new(new PrecisionModel(), 4326);

    [HttpPost]
    [Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken ct = default)
    {
        Point? position = null;
        if (request.Lat.HasValue && request.Lon.HasValue)
        {
            position = Gf.CreatePoint(new Coordinate((double)request.Lon.Value, (double)request.Lat.Value));
        }

        var incident = new Incident
        {
            OrganizationId = DemoOrganization.Id,
            Kind = request.Kind,
            Severity = request.Severity ?? "ALTA",
            Description = request.Description ?? "",
            Position = position,
            RadiusKm = request.RadiusKm,
            IsSynthetic = request.IsSynthetic,
            Status = "Active",
            ReportedByAccountId = User.AccountId(),
            OccurredAtUtc = DateTime.UtcNow
        };

        // Una avería se informa con el vehículo: sin su ruta publicada, el evaluador no encuentra paradas afectadas.
        if (!string.IsNullOrWhiteSpace(request.VehicleCode))
        {
            var vehicleId = await db.Vehicles
                .Where(v => v.OrganizationId == DemoOrganization.Id && v.Code == request.VehicleCode)
                .Select(v => (Guid?)v.Id).FirstOrDefaultAsync(ct);
            if (vehicleId is null) return Problem(statusCode: 404, title: "vehicle_not_found", detail: $"No existe el vehículo {request.VehicleCode}.");
            incident.RoutePlanId = await db.RoutePlans
                .Where(p => p.OrganizationId == DemoOrganization.Id && p.VehicleId == vehicleId && p.Status == "Published")
                .OrderByDescending(p => p.Revision).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
        }

        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);

        var impact = await evaluator.EvaluateAsync(incident.Id, ct);

        Guid? runId = null;
        if (agentOptions.Value.AutoOnIncident && impact.AffectedRoutes.Count > 0)
        {
            var run = new AgentRun
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Incident",
                Status = "Running",
                TriggerJson = JsonSerializer.Serialize(new { kind = "Incident", incidentId = incident.Id }),
                StartedAtUtc = DateTime.UtcNow,
                ToolCallsJson = "[]"
            };
            db.AgentRuns.Add(run);
            await db.SaveChangesAsync(ct);
            runId = run.Id;

            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var agent = scope.ServiceProvider.GetRequiredService<IAgentRunService>();
                    await agent.ResumeAndWaitAsync(run.Id, CancellationToken.None);
                }
                catch (Exception error)
                {
                    logger.LogError(error, "Failed to complete automatic incident run {RunId}", run.Id);
                }
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            incidentId = incident.Id,
            kind = incident.Kind,
            runId,
            affectedRoutes = impact.AffectedRoutes.Select(r => new
            {
                routePlanId = r.RoutePlanId,
                vehicleCode = r.VehicleCode,
                affectedLegs = r.AffectedLegs.Select(l => new { from = l.From, to = l.To }),
                affectedStops = r.AffectedStops
            })
        });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Any)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct = default)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == id && i.OrganizationId == DemoOrganization.Id, ct);
        if (incident == null) return NotFound();

        var impact = await evaluator.EvaluateAsync(incident.Id, ct);

        return Ok(new
        {
            incidentId = incident.Id,
            kind = incident.Kind,
            status = incident.Status,
            severity = incident.Severity,
            description = incident.Description,
            occurredAtUtc = incident.OccurredAtUtc,
            radiusKm = incident.RadiusKm,
            affectedRoutes = impact.AffectedRoutes.Select(r => new
            {
                routePlanId = r.RoutePlanId,
                vehicleCode = r.VehicleCode,
                affectedLegs = r.AffectedLegs.Select(l => new { from = l.From, to = l.To }),
                affectedStops = r.AffectedStops
            })
        });
    }
}

public sealed record CreateIncidentRequest(
    string Kind,
    decimal? Lat = null,
    decimal? Lon = null,
    decimal? RadiusKm = null,
    decimal? Factor = null,
    string? VehicleCode = null,
    string? Severity = null,
    string? Description = null,
    bool IsSynthetic = true);
