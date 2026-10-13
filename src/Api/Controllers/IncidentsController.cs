using IstpetDev.Api.Authentication;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/incidents")]
public sealed class IncidentsController(
    IApplicationDbContext db,
    IIncidentEvaluator evaluator) : ControllerBase
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

        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);

        var impact = await evaluator.EvaluateAsync(incident.Id, ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            incidentId = incident.Id,
            kind = incident.Kind,
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
