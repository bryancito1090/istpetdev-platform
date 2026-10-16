using System.Globalization;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IstpetDev.Api.Controllers;

[ApiController]
[Route("api/v1/logistics")]
[Authorize(Roles = Roles.Planning + "," + Roles.Automatizacion)]
public sealed class LogisticsQueriesController(
    IApplicationDbContext db,
    ISourcingService sourcingService,
    IVehicleMatcher vehicleMatcher,
    IRiskCalculator riskCalculator) : ControllerBase
{
    public const int MaxQueryIds = 100;

    [HttpGet("stock")]
    public async Task<IActionResult> GetStock(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        if (!productId.HasValue || productId.Value == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "invalid_product_id",
                detail: "El parámetro productId es obligatorio y no puede ser Guid.Empty.");
        }

        if (warehouseId.HasValue && warehouseId.Value == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "invalid_warehouse_id",
                detail: "El parámetro warehouseId no puede ser Guid.Empty.");
        }

        if (warehouseId.HasValue)
        {
            var warehouseExists = await db.Locations
                .AsNoTracking()
                .AnyAsync(
                    l => l.OrganizationId == DemoOrganization.Id &&
                         l.Id == warehouseId.Value &&
                         l.Kind == LocationKinds.Warehouse &&
                         l.IsActive,
                    ct);

            if (!warehouseExists)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "warehouse_not_found",
                    detail: $"No existe una bodega activa con ID '{warehouseId.Value}' en la organización.");
            }
        }

        var result = await sourcingService.GetWarehouseStockAsync(productId.Value, warehouseId, ct);
        if (result is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "product_not_found",
                detail: $"No existe un producto con ID '{productId.Value}' en la organización.");
        }

        return Ok(result);
    }

    [HttpPost("sourcing")]
    public async Task<IActionResult> QuerySourcing(
        [FromBody] LogisticsSourcingQueryRequest? req,
        CancellationToken ct = default)
    {
        if (req is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "invalid_request",
                detail: "Se requiere un cuerpo JSON válido.");
        }

        bool hasOrders = req.OrderIds is not null;
        bool hasServicePoints = req.ServicePointIds is not null;

        if (hasOrders == hasServicePoints)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "invalid_sourcing_selector",
                detail: "Debe especificar exactamente uno de orderIds o servicePointIds.");
        }

        if (hasOrders)
        {
            if (req.ProductId.HasValue)
            {
                return Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "product_filter_not_allowed",
                    detail: "El filtro productId solo se admite cuando se consulta por servicePointIds.");
            }

            if (req.OrderIds!.Count == 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "empty_order_ids",
                    detail: "La lista orderIds no puede estar vacía.");
            }

            if (req.OrderIds.Any(id => id == Guid.Empty))
            {
                return Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "invalid_order_id",
                    detail: "La lista orderIds no puede contener Guid.Empty.");
            }

            var distinctOrderIds = req.OrderIds.Distinct().ToList();
            if (distinctOrderIds.Count > MaxQueryIds)
            {
                return Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "limit_exceeded",
                    detail: $"Se permite un máximo de {MaxQueryIds} IDs únicos por consulta.");
            }

            var orders = await db.DeliveryOrders
                .AsNoTracking()
                .Where(o => o.OrganizationId == DemoOrganization.Id && distinctOrderIds.Contains(o.Id))
                .ToListAsync(ct);

            if (orders.Count != distinctOrderIds.Count)
            {
                var foundIds = orders.Select(o => o.Id).ToHashSet();
                var missing = distinctOrderIds.First(id => !foundIds.Contains(id));
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "order_not_found",
                    detail: $"El pedido '{missing}' no existe en la organización.");
            }

            var lines = await db.DeliveryOrderLines
                .AsNoTracking()
                .Where(l => l.OrganizationId == DemoOrganization.Id && distinctOrderIds.Contains(l.DeliveryOrderId))
                .ToListAsync(ct);

            var warehouses = await db.Locations
                .AsNoTracking()
                .Where(l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive)
                .ToDictionaryAsync(l => l.Id, ct);

            var servicePoints = await db.ServicePoints
                .AsNoTracking()
                .Where(sp => sp.OrganizationId == DemoOrganization.Id)
                .ToDictionaryAsync(sp => sp.Id, ct);

            var ordersById = orders.ToDictionary(o => o.Id);
            var demandInputs = new List<SourcingDemandInput>();

            foreach (var orderId in distinctOrderIds)
            {
                var ord = ordersById[orderId];

                (Guid Id, string Code, string Name, bool IsWh, GeoPoint Pos) dest;
                if (ord.DestinationServicePointId.HasValue && servicePoints.TryGetValue(ord.DestinationServicePointId.Value, out var sp))
                {
                    var pos = sp.Position != null
                        ? new GeoPoint((decimal)sp.Position.Y, (decimal)sp.Position.X)
                        : new GeoPoint(-0.2295m, -78.5243m);
                    dest = (sp.Id, sp.Code, sp.Name, false, pos);
                }
                else if (ord.DestinationLocationId.HasValue && warehouses.TryGetValue(ord.DestinationLocationId.Value, out var wh))
                {
                    var pos = wh.Position != null
                        ? new GeoPoint((decimal)wh.Position.Y, (decimal)wh.Position.X)
                        : new GeoPoint(-0.2295m, -78.5243m);
                    dest = (wh.Id, wh.Code, wh.Name, true, pos);
                }
                else
                {
                    dest = (Guid.Empty, "UNK", "Destino Desconocido", false, new GeoPoint(-0.2295m, -78.5243m));
                }

                var ordLines = lines
                    .Where(l => l.DeliveryOrderId == ord.Id)
                    .OrderBy(l => l.ProductId)
                    .ThenBy(l => l.Id)
                    .ToList();

                foreach (var line in ordLines)
                {
                    demandInputs.Add(new SourcingDemandInput(
                        ord.Id,
                        ord.OrderNumber,
                        "Order",
                        dest.Id,
                        dest.IsWh,
                        dest.Code,
                        dest.Name,
                        dest.Pos,
                        line.ProductId,
                        line.RequestedQuantity
                    ));
                }
            }

            var orderResult = await sourcingService.PlanDemandSourcingAsync(demandInputs, "Order", ct);
            return Ok(orderResult);
        }

        // Consulta por puntos de servicio a partir de riesgos en memoria (sin crear pedidos ni snapshots)
        if (req.ServicePointIds!.Count == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "empty_service_point_ids",
                detail: "La lista servicePointIds no puede estar vacía.");
        }

        if (req.ServicePointIds.Any(id => id == Guid.Empty))
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "invalid_service_point_id",
                detail: "La lista servicePointIds no puede contener Guid.Empty.");
        }

        if (req.ProductId.HasValue && req.ProductId.Value == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "invalid_product_id",
                detail: "El parámetro productId no puede ser Guid.Empty.");
        }

        var distinctPointIds = req.ServicePointIds.Distinct().ToList();
        if (distinctPointIds.Count > MaxQueryIds)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "limit_exceeded",
                detail: $"Se permite un máximo de {MaxQueryIds} IDs únicos por consulta.");
        }

        var pointsById = await db.ServicePoints
            .AsNoTracking()
            .Where(sp => sp.OrganizationId == DemoOrganization.Id && distinctPointIds.Contains(sp.Id))
            .ToDictionaryAsync(sp => sp.Id, ct);

        if (pointsById.Count != distinctPointIds.Count)
        {
            var missing = distinctPointIds.First(id => !pointsById.ContainsKey(id));
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "service_point_not_found",
                detail: $"El punto de servicio '{missing}' no existe en la organización.");
        }

        if (req.ProductId.HasValue)
        {
            var productExists = await db.Products
                .AsNoTracking()
                .AnyAsync(p => p.OrganizationId == DemoOrganization.Id && p.Id == req.ProductId.Value, ct);

            if (!productExists)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "product_not_found",
                    detail: $"El producto '{req.ProductId.Value}' no existe en la organización.");
            }
        }

        var allRisks = await riskCalculator.CalculateAsync(persistSnapshots: false, ct);
        var pointSet = distinctPointIds.ToHashSet();

        var eligibleRisks = allRisks
            .Where(r => pointSet.Contains(r.ServicePointId) &&
                        (!req.ProductId.HasValue || r.ProductId == req.ProductId.Value) &&
                        r.SuggestedQuantity > 0m &&
                        r.Level is RiskLevel.Critico or RiskLevel.Alto or RiskLevel.Vigilar)
            .OrderBy(r => RiskLevelRank(r.Level))
            .ThenBy(r => (r.CoverageDays ?? 0m) - r.LeadTimeDays)
            .ThenBy(r => CriticalityRank(r.Criticality))
            .ThenBy(r => r.PointCode, StringComparer.Ordinal)
            .ThenBy(r => r.Sku, StringComparer.Ordinal)
            .ThenBy(r => r.ServicePointId)
            .ThenBy(r => r.ProductId)
            .ToList();

        var riskDemands = new List<SourcingDemandInput>(eligibleRisks.Count);
        foreach (var risk in eligibleRisks)
        {
            if (!pointsById.TryGetValue(risk.ServicePointId, out var sp)) continue;

            var pos = sp.Position != null
                ? new GeoPoint((decimal)sp.Position.Y, (decimal)sp.Position.X)
                : new GeoPoint(-0.2295m, -78.5243m);

            riskDemands.Add(new SourcingDemandInput(
                OrderId: null,
                OrderNumber: null,
                SourceKind: "Risk",
                DestinationId: sp.Id,
                IsDestinationWarehouse: false,
                DestinationCode: sp.Code,
                DestinationName: sp.Name,
                DestinationPosition: pos,
                ProductId: risk.ProductId,
                RequestedQuantity: risk.SuggestedQuantity,
                RiskLevel: risk.Level.ToString(),
                CoverageDays: risk.CoverageDays
            ));
        }

        var riskResult = await sourcingService.PlanDemandSourcingAsync(riskDemands, "Risk", ct);
        return Ok(riskResult);
    }

    [HttpGet("vehicles/available")]
    public async Task<IActionResult> GetAvailableVehicles(
        [FromQuery] string? date = null,
        CancellationToken ct = default)
    {
        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateOnly.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "invalid_date",
                    detail: "El parámetro date debe tener formato ISO yyyy-MM-dd.");
            }

            parsedDate = d;
        }

        var result = await vehicleMatcher.GetAvailableVehiclesAsync(parsedDate, ct);
        return Ok(result);
    }

    private static int RiskLevelRank(RiskLevel level) => level switch
    {
        RiskLevel.Critico => 0,
        RiskLevel.Alto => 1,
        RiskLevel.Vigilar => 2,
        _ => 3
    };

    private static int CriticalityRank(string criticality) => criticality.ToLowerInvariant() switch
    {
        "alta" => 0,
        "media" => 1,
        "normal" => 2,
        _ => 3
    };
}

public sealed record LogisticsSourcingQueryRequest(
    IReadOnlyList<Guid>? OrderIds = null,
    IReadOnlyList<Guid>? ServicePointIds = null,
    Guid? ProductId = null
);
