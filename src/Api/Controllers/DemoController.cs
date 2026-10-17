using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Demo;
using IstpetDev.Application.Logistics.Incidents;
using IstpetDev.Api.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/demo")]
[Authorize(Roles = Roles.Planning)]
[EnableRateLimiting("imports")]
public sealed class DemoController(
    IDemoResetService reset,
    IstpetDev.Application.Field.IFieldService fieldService,
    IApplicationDbContext db,
    IIncidentEvaluator evaluator,
    IOptions<DemoOptions> options) : ControllerBase
{
    private static readonly GeometryFactory Gf = new(new PrecisionModel(), 4326);

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromQuery] string? fixture = null, [FromBody] ResetRequest? req = null, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled) return NotFound();
        var selected = fixture ?? req?.Fixture;
        var report = await reset.ResetAsync(selected, cancellationToken, User.AccountId());
        return report.Applied ? Ok(report) : UnprocessableEntity(report);
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> TriggerPresetIncident([FromBody] PresetIncidentRequest request, CancellationToken ct = default)
    {
        if (!options.Value.Enabled) return NotFound();

        var incident = new Incident
        {
            OrganizationId = DemoOrganization.Id,
            Kind = "CierreVial",
            Severity = "ALTA",
            Description = "Cierre vial en Panamericana Norte (km 12) por trabajos de mantenimiento",
            Position = Gf.CreatePoint(new Coordinate(-78.4850, -0.1250)),
            RadiusKm = 5.0m,
            IsSynthetic = true,
            Status = "Active",
            ReportedByAccountId = User.AccountId(),
            OccurredAtUtc = DateTime.UtcNow
        };

        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);

        var impact = await evaluator.EvaluateAsync(incident.Id, ct);

        return Ok(new
        {
            preset = request.Preset,
            status = "Triggered",
            incidentId = incident.Id,
            affectedRoutes = impact.AffectedRoutes.Select(r => new
            {
                routePlanId = r.RoutePlanId,
                vehicleCode = r.VehicleCode,
                affectedLegs = r.AffectedLegs.Select(l => new { from = l.From, to = l.To }),
                affectedStops = r.AffectedStops
            })
        });
    }

    /// <summary>
    /// FR-16: Avanza N paradas de forma real (preparación, despacho y recepción). Solo demo y rol Planificación/Gerencia.
    /// </summary>
    [HttpPost("route-plans/{id:guid}/advance")]
    public async Task<IActionResult> Advance(
        Guid id,
        [FromBody] IstpetDev.Application.Field.Dtos.DemoAdvanceRequest request,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled) return NotFound();

        try
        {
            var actorAccountId = User.AccountId() ?? DemoOrganization.Id;
            var result = await fieldService.AdvanceDemoAsync(id, request, actorAccountId, cancellationToken);
            return Ok(result);
        }
        catch (IstpetDev.Application.Field.FieldException ex)
        {
            return Problem(detail: ex.Message, statusCode: ex.StatusCode, title: ex.Code);
        }
    }
}

public sealed record PresetIncidentRequest(string Preset);
public sealed record ResetRequest(string? Fixture = null);
