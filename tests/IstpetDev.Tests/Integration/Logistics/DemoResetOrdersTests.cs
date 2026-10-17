using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using IstpetDev.Application.Logistics;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Logistics;

[Trait("Category", "Integration")]
public class DemoResetOrdersTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    [Fact]
    public async Task Reset_succeeds_when_database_has_delivery_orders_transfer_suggestions_and_stock_policies()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        // 1. Reset inicial con demo-region
        var firstReset = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        firstReset.EnsureSuccessStatusCode();
        var firstDoc = await firstReset.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(firstDoc.GetProperty("applied").GetBoolean());

        // 2. Insertar DeliveryOrder, DeliveryOrderLine, TransferSuggestion, RouteStopLine y WarehouseStockPolicy
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var warehouse = await db.Locations.FirstAsync(l => l.Kind == LocationKinds.Warehouse && l.OrganizationId == DemoOrganization.Id);
            var servicePoint = await db.ServicePoints.FirstAsync(sp => sp.OrganizationId == DemoOrganization.Id);
            var product = await db.Products.FirstAsync(p => p.OrganizationId == DemoOrganization.Id);
            var lot = await db.Lots.FirstAsync(l => l.ProductId == product.Id && l.OrganizationId == DemoOrganization.Id);
            var vehicle = await db.Vehicles.FirstAsync(v => v.OrganizationId == DemoOrganization.Id);

            // TransferSuggestion
            var suggestion = new TransferSuggestion
            {
                OrganizationId = DemoOrganization.Id,
                SourceLocationId = warehouse.Id,
                TargetLocationId = warehouse.Id,
                ProductId = product.Id,
                SuggestedQuantity = 50m,
                Status = TransferSuggestionStatus.Suggested
            };
            db.TransferSuggestions.Add(suggestion);

            // WarehouseStockPolicy
            var policy = new WarehouseStockPolicy
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = warehouse.Id,
                ProductId = product.Id,
                MinStock = 20m,
                MaxStock = 100m,
                LeadTimeDays = 3,
                TargetCoverageDays = 15m,
                IsManual = true,
                LastCalculatedAtUtc = DateTime.UtcNow
            };
            db.WarehouseStockPolicies.Add(policy);

            // DeliveryOrder referenciando TransferSuggestion
            var order = new DeliveryOrder
            {
                OrganizationId = DemoOrganization.Id,
                OrderNumber = "ORD-TEST-290",
                DestinationServicePointId = servicePoint.Id,
                DestinationLocationId = warehouse.Id,
                TransferSuggestionId = suggestion.Id,
                RequiredDateUtc = DateTime.UtcNow.AddDays(2),
                Status = DeliveryOrderStatus.Planned,
                SourceType = "Manual"
            };
            db.DeliveryOrders.Add(order);

            var orderLine = new DeliveryOrderLine
            {
                OrganizationId = DemoOrganization.Id,
                DeliveryOrderId = order.Id,
                ProductId = product.Id,
                RequestedQuantity = 30m,
                PlannedQuantity = 30m
            };
            db.DeliveryOrderLines.Add(orderLine);

            // RoutePlan, RouteStop, RouteStopLine
            var routePlan = new RoutePlan
            {
                OrganizationId = DemoOrganization.Id,
                Code = "RP-TEST-290",
                Revision = 1,
                VehicleId = vehicle.Id,
                PlannedStartUtc = DateTime.UtcNow,
                Status = "Draft"
            };
            db.RoutePlans.Add(routePlan);

            var routeStop = new RouteStop
            {
                OrganizationId = DemoOrganization.Id,
                RoutePlanId = routePlan.Id,
                SequenceNumber = 1,
                Kind = RouteStopKind.Delivery,
                ServicePointId = servicePoint.Id
            };
            db.RouteStops.Add(routeStop);

            var routeStopLine = new RouteStopLine
            {
                OrganizationId = DemoOrganization.Id,
                RouteStopId = routeStop.Id,
                DeliveryOrderId = order.Id,
                ProductId = product.Id,
                LotId = lot.Id,
                PlannedQuantity = 30m
            };
            db.RouteStopLines.Add(routeStopLine);

            // Delivery asociada al pedido
            var delivery = new Delivery
            {
                OrganizationId = DemoOrganization.Id,
                Code = "DEL-TEST-290",
                RouteStopId = routeStop.Id,
                DeliveryOrderId = order.Id,
                ServicePointId = servicePoint.Id,
                OriginLocationId = warehouse.Id,
                Status = DeliveryStates.Draft
            };
            db.Deliveries.Add(delivery);

            var deliveryLine = new DeliveryLine
            {
                OrganizationId = DemoOrganization.Id,
                DeliveryId = delivery.Id,
                LotId = lot.Id,
                PlannedQuantity = 30m
            };
            db.DeliveryLines.Add(deliveryLine);

            await db.SaveChangesAsync();
        }

        // 3. Ejecutar segundo reset: debe completarse con éxito (applied: true)
        var secondReset = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        var secondContent = await secondReset.Content.ReadAsStringAsync();
        Assert.True(secondReset.IsSuccessStatusCode, secondContent);

        var secondDoc = await secondReset.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(secondDoc.GetProperty("applied").GetBoolean(), "El reset debe aplicarse exitosamente cuando hay pedidos y sugerencias");

        // 4. Verificar que se hayan eliminado todas las entidades operacionales y se hayan restaurado las canónicas
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Assert.Equal(0, await db.DeliveryOrders.CountAsync(x => x.OrganizationId == DemoOrganization.Id));
            Assert.Equal(0, await db.DeliveryOrderLines.CountAsync(x => x.OrganizationId == DemoOrganization.Id));
            Assert.Equal(0, await db.TransferSuggestions.CountAsync(x => x.OrganizationId == DemoOrganization.Id));
            Assert.Equal(0, await db.WarehouseStockPolicies.CountAsync(x => x.OrganizationId == DemoOrganization.Id));
            Assert.Equal(0, await db.RouteStopLines.CountAsync(x => x.OrganizationId == DemoOrganization.Id));
            Assert.Equal(0, await db.Deliveries.CountAsync(x => x.OrganizationId == DemoOrganization.Id));
            Assert.Equal(0, await db.RoutePlans.CountAsync(x => x.OrganizationId == DemoOrganization.Id));

            // Entidades maestras restauradas desde demo-region
            Assert.Equal(1, await db.Locations.CountAsync(w => w.OrganizationId == DemoOrganization.Id && w.Kind == LocationKinds.Warehouse));
            Assert.Equal(15, await db.ServicePoints.CountAsync(sp => sp.OrganizationId == DemoOrganization.Id));
            Assert.Equal(3, await db.Products.CountAsync(p => p.OrganizationId == DemoOrganization.Id));
        }
    }

    [Fact]
    public async Task Reset_after_order_creation_and_confirmation_workflow_succeeds()
    {
        var dbConn = postgres.NewDatabase();
        await using var api = new ApiFactory(dbConn, demoEnabled: true);
        var planner = api.ClientFor(TestTokens.Planner);

        // 1. Reset inicial
        var resetResp = await planner.PostAsync("/api/v1/demo/reset", null);
        resetResp.EnsureSuccessStatusCode();

        Guid orderId;
        Guid servicePointId;
        Guid productId;
        Guid warehouseId;
        Guid vehicleId;

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sp = await db.ServicePoints.OrderBy(p => p.Code).FirstAsync();
            var p = await db.Products.OrderBy(pr => pr.Sku).FirstAsync();
            var w = await db.Locations.FirstAsync(l => l.Kind == LocationKinds.Warehouse);
            var v = await db.Vehicles.FirstAsync();

            servicePointId = sp.Id;
            productId = p.Id;
            warehouseId = w.Id;
            vehicleId = v.Id;

            // Crear un pedido
            var order = new DeliveryOrder
            {
                OrganizationId = DemoOrganization.Id,
                OrderNumber = $"ORD-CONFIRM-{Guid.NewGuid():N}"[..16],
                DestinationServicePointId = sp.Id,
                RequiredDateUtc = DateTime.UtcNow.AddDays(1),
                SourceType = "Manual",
                Status = DeliveryOrderStatus.Pending
            };
            db.DeliveryOrders.Add(order);
            db.DeliveryOrderLines.Add(new DeliveryOrderLine
            {
                OrganizationId = DemoOrganization.Id,
                DeliveryOrderId = order.Id,
                ProductId = p.Id,
                RequestedQuantity = 5m
            });
            await db.SaveChangesAsync();
            orderId = order.Id;

            // Crear sugerencia de stock policy via advisor
            var policyAdvisor = scope.ServiceProvider.GetRequiredService<IStockPolicyAdvisor>();
            await policyAdvisor.ApplyPolicyAsync(w.Id, p.Id, 10m, 50m, 2, 10m, true);
        }

        // 2. Confirmar plan de ruta para el pedido usando endpoint de confirmación
        var confirmResp = await planner.PostAsJsonAsync("/api/v1/route-plans/confirm", new
        {
            code = $"WIZ-{Guid.NewGuid():N}"[..12],
            vehicleId,
            totalDistanceKm = 10m,
            totalDurationMinutes = 30m,
            totalCost = 15m,
            orderIds = new[] { orderId },
            stops = new object[]
            {
                new { sequence = 1, kind = "Pickup", locationId = (Guid?)warehouseId, code = "BOD-01", name = "Bodega Principal",
                      position = new { lat = -0.2m, lon = -78.5m }, deltaWeight = 0m, deltaVolume = 0m, windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15 },
                new { sequence = 2, kind = "Delivery", servicePointId = (Guid?)servicePointId, code = "P-01", name = "Punto 01",
                      position = new { lat = -0.21m, lon = -78.49m }, deltaWeight = 0m, deltaVolume = 0m, windowStart = "07:00:00", windowEnd = "19:00:00", serviceMinutes = 15 }
            },
            lines = new[] { new { deliveryOrderId = orderId, productId, plannedQuantity = 5m } }
        });
        Assert.True(confirmResp.IsSuccessStatusCode, await confirmResp.Content.ReadAsStringAsync());

        // 3. Ejecutar reset nuevamente: debe pasar limpiamente
        var resetAgainResp = await planner.PostAsync("/api/v1/demo/reset", null);
        var resetAgainContent = await resetAgainResp.Content.ReadAsStringAsync();
        Assert.True(resetAgainResp.IsSuccessStatusCode, resetAgainContent);

        var doc = await resetAgainResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(doc.GetProperty("applied").GetBoolean());

        // 4. Comprobar que no queden pedidos ni pólizas residuales
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await db.DeliveryOrders.CountAsync(x => x.OrganizationId == DemoOrganization.Id));
            Assert.Equal(0, await db.WarehouseStockPolicies.CountAsync(x => x.OrganizationId == DemoOrganization.Id));
        }
    }
}
