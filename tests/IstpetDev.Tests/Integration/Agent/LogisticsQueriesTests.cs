using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace IstpetDev.Tests.Integration.Agent;

using DomainLocation = IstpetDev.Domain.Entities.Location;
using DomainServicePoint = IstpetDev.Domain.Entities.ServicePoint;

[Trait("Category", "Integration")]
public sealed class LogisticsQueriesTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private const string TechnicalClientId = "n8n-automation-client";
    private const string TechnicalScope = "istpetdev-api/automation";
    private static readonly GeometryFactory Gf = new(new PrecisionModel(), 4326);

    private static Dictionary<string, string?> CreateOverrides() => new()
    {
        ["Authentication:AllowedClientIds:1"] = TechnicalClientId,
        ["Authentication:TechnicalClientIds:0"] = TechnicalClientId,
        ["Authentication:TechnicalScope"] = TechnicalScope,
        ["Demo:Accounts:3:Subject"] = "manager-1",
        ["Demo:Accounts:3:DisplayName"] = "Gerencia de prueba",
        ["Demo:Accounts:3:Role"] = Roles.Gerencia,
        ["Demo:Accounts:5:Subject"] = "warehouse-1",
        ["Demo:Accounts:5:DisplayName"] = "Bodega de prueba",
        ["Demo:Accounts:5:Role"] = Roles.Bodega,
        ["Demo:Accounts:6:Subject"] = "auditor-1",
        ["Demo:Accounts:6:DisplayName"] = "Auditor de prueba",
        ["Demo:Accounts:6:Role"] = Roles.Auditor
    };

    private static async Task SeedTechnicalAccountAsync(
        string connection,
        string subject = TechnicalClientId,
        string role = Roles.Automatizacion,
        bool isActive = true)
    {
        await using var db = TestDb.Context(connection);
        db.Accounts.Add(new Account
        {
            Kind = "technical",
            Issuer = TestTokens.Issuer,
            Subject = subject,
            DisplayName = $"Cuenta técnica {subject}",
            Role = role,
            IsActive = isActive
        });
        await db.SaveChangesAsync();
    }

    private static HttpClient CreateAutomationClient(
        ApiFactory api,
        string subject = TechnicalClientId,
        string clientId = TechnicalClientId,
        string scope = TechnicalScope,
        string role = Roles.Automatizacion)
    {
        var client = api.CreateClient();
        var token = TestTokens.Create(
            subject: subject,
            clientId: clientId,
            scope: scope,
            role: role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Stock_query_applies_safety_stock_once_orders_fefo_excludes_expired_and_blocked_and_includes_empty_warehouses()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: CreateOverrides());
        using var planner = api.ClientFor(TestTokens.Planner);

        var resetResp = await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        Assert.Equal(HttpStatusCode.OK, resetResp.StatusCode);

        Guid productId;
        Guid mainWarehouseId;
        Guid secondWarehouseId;
        Guid emptyWarehouseId;
        Guid lotEarlyId;
        Guid lotLateId;
        Guid lotNoExpiryId;
        Guid lotExpiredTodayId;
        Guid lotBlockedId;

        await using (var db = TestDb.Context(connection))
        {
            var mainWh = await db.Locations.SingleAsync(
                l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive);
            mainWarehouseId = mainWh.Id;

            var prod = new Product
            {
                OrganizationId = DemoOrganization.Id,
                Sku = "TST-STOCK-01",
                Name = "Insumo de Prueba Stock",
                BaseUnit = "caja",
                WeightPerBaseUnit = 1.0m,
                VolumePerBaseUnit = 0.01m,
                IsActive = true
            };
            db.Products.Add(prod);
            productId = prod.Id;

            var secondWh = new DomainLocation
            {
                OrganizationId = DemoOrganization.Id,
                Code = "BOD-NORTE",
                Name = "Bodega Norte",
                Kind = LocationKinds.Warehouse,
                Position = Gf.CreatePoint(new Coordinate(-78.4800, -0.1500)),
                IsActive = true
            };
            var emptyWh = new DomainLocation
            {
                OrganizationId = DemoOrganization.Id,
                Code = "BOD-VACIA",
                Name = "Bodega Sin Saldo",
                Kind = LocationKinds.Warehouse,
                Position = Gf.CreatePoint(new Coordinate(-78.5500, -0.2800)),
                IsActive = true
            };
            var inactiveWh = new DomainLocation
            {
                OrganizationId = DemoOrganization.Id,
                Code = "BOD-INACTIVA",
                Name = "Bodega Inactiva",
                Kind = LocationKinds.Warehouse,
                Position = Gf.CreatePoint(new Coordinate(-78.5600, -0.2900)),
                IsActive = false
            };
            db.Locations.AddRange(secondWh, emptyWh, inactiveWh);
            secondWarehouseId = secondWh.Id;
            emptyWarehouseId = emptyWh.Id;

            // Fecha operativa de demo-region: 2026-10-12
            var lotLate = new Lot
            {
                OrganizationId = DemoOrganization.Id,
                ProductId = productId,
                Code = "LOT-B-2027",
                ExpiresOn = new DateOnly(2027, 3, 1),
                IsBlocked = false
            };
            var lotEarly = new Lot
            {
                OrganizationId = DemoOrganization.Id,
                ProductId = productId,
                Code = "LOT-A-2026",
                ExpiresOn = new DateOnly(2026, 11, 15),
                IsBlocked = false
            };
            var lotNoExpiry = new Lot
            {
                OrganizationId = DemoOrganization.Id,
                ProductId = productId,
                Code = "LOT-C-NOEXP",
                ExpiresOn = null,
                IsBlocked = false
            };
            var lotExpiredToday = new Lot
            {
                OrganizationId = DemoOrganization.Id,
                ProductId = productId,
                Code = "LOT-EXP-HOY",
                ExpiresOn = new DateOnly(2026, 10, 12), // Vencido en la fecha operativa (<= hoy)
                IsBlocked = false
            };
            var lotBlocked = new Lot
            {
                OrganizationId = DemoOrganization.Id,
                ProductId = productId,
                Code = "LOT-BLOCKED",
                ExpiresOn = new DateOnly(2027, 6, 1),
                IsBlocked = true
            };

            db.Lots.AddRange(lotLate, lotEarly, lotNoExpiry, lotExpiredToday, lotBlocked);
            lotEarlyId = lotEarly.Id;
            lotLateId = lotLate.Id;
            lotNoExpiryId = lotNoExpiry.Id;
            lotExpiredTodayId = lotExpiredToday.Id;
            lotBlockedId = lotBlocked.Id;

            // En mainWh:
            // - lotEarly: onHand 100, reserved 20 -> netUsable 80
            // - lotLate: onHand 60, reserved 10 -> netUsable 50
            // - lotNoExpiry: onHand 70, reserved 0 -> netUsable 70
            // - lotExpiredToday: onHand 500, reserved 0 -> excluido
            // - lotBlocked: onHand 400, reserved 0 -> excluido
            // Total utilizable en mainWh = 80 + 50 + 70 = 200
            // SafetyStock (10%) = 20 (aplicado una sola vez sobre 200)
            // AvailableQuantity = 180
            db.StockBalances.AddRange(
                new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = mainWarehouseId,
                    LotId = lotEarlyId,
                    QuantityOnHand = 100m,
                    QuantityReserved = 20m
                },
                new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = mainWarehouseId,
                    LotId = lotLateId,
                    QuantityOnHand = 60m,
                    QuantityReserved = 10m
                },
                new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = mainWarehouseId,
                    LotId = lotNoExpiryId,
                    QuantityOnHand = 70m,
                    QuantityReserved = 0m
                },
                new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = mainWarehouseId,
                    LotId = lotExpiredTodayId,
                    QuantityOnHand = 500m,
                    QuantityReserved = 0m
                },
                new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = mainWarehouseId,
                    LotId = lotBlockedId,
                    QuantityOnHand = 400m,
                    QuantityReserved = 0m
                },
                // En secondWh: onHand 50, reserved 0 -> netUsable 50, safety 5, available 45
                new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = secondWarehouseId,
                    LotId = lotLateId,
                    QuantityOnHand = 50m,
                    QuantityReserved = 0m
                });

            await db.SaveChangesAsync();
        }

        // 1. Consulta general por producto (incluye todas las bodegas activas, excluida la inactiva)
        var resp = await planner.GetAsync($"/api/v1/logistics/stock?productId={productId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var root = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-10-12", root.GetProperty("operationalDate").GetString());
        Assert.Equal(10m, root.GetProperty("safetyStockPct").GetDecimal());
        Assert.Equal(productId, root.GetProperty("productId").GetGuid());
        Assert.Equal("TST-STOCK-01", root.GetProperty("sku").GetString());
        Assert.Equal("caja", root.GetProperty("baseUnit").GetString());

        var warehouses = root.GetProperty("warehouses").EnumerateArray().ToList();
        Assert.Equal(3, warehouses.Count); // mainWh, BOD-NORTE, BOD-VACIA (BOD-INACTIVA excluida)

        var mainWhJson = warehouses.Single(w => w.GetProperty("warehouseId").GetGuid() == mainWarehouseId);
        Assert.Equal(230m, mainWhJson.GetProperty("quantityOnHand").GetDecimal()); // 100 + 60 + 70
        Assert.Equal(30m, mainWhJson.GetProperty("quantityReserved").GetDecimal()); // 20 + 10 + 0
        Assert.Equal(200m, mainWhJson.GetProperty("netUsableQuantity").GetDecimal()); // 80 + 50 + 70
        Assert.Equal(20m, mainWhJson.GetProperty("safetyStock").GetDecimal()); // 10% de 200 una sola vez
        Assert.Equal(180m, mainWhJson.GetProperty("availableQuantity").GetDecimal()); // 200 - 20

        // Verificar orden FEFO (LOT-A-2026 -> LOT-B-2027 -> LOT-C-NOEXP al final) y exclusión de vencido hoy y bloqueado
        var mainLots = mainWhJson.GetProperty("lots").EnumerateArray().ToList();
        Assert.Equal(3, mainLots.Count);
        Assert.Equal(lotEarlyId, mainLots[0].GetProperty("lotId").GetGuid());
        Assert.Equal(80m, mainLots[0].GetProperty("netUsableQuantity").GetDecimal());
        Assert.Equal(lotLateId, mainLots[1].GetProperty("lotId").GetGuid());
        Assert.Equal(50m, mainLots[1].GetProperty("netUsableQuantity").GetDecimal());
        Assert.Equal(lotNoExpiryId, mainLots[2].GetProperty("lotId").GetGuid());
        Assert.Equal(JsonValueKind.Null, mainLots[2].GetProperty("expiresOn").ValueKind);
        Assert.Equal(70m, mainLots[2].GetProperty("netUsableQuantity").GetDecimal());

        // Verificar bodega activa sin saldo con disponibilidad cero
        var emptyWhJson = warehouses.Single(w => w.GetProperty("warehouseId").GetGuid() == emptyWarehouseId);
        Assert.Equal(0m, emptyWhJson.GetProperty("quantityOnHand").GetDecimal());
        Assert.Equal(0m, emptyWhJson.GetProperty("quantityReserved").GetDecimal());
        Assert.Equal(0m, emptyWhJson.GetProperty("netUsableQuantity").GetDecimal());
        Assert.Equal(0m, emptyWhJson.GetProperty("safetyStock").GetDecimal());
        Assert.Equal(0m, emptyWhJson.GetProperty("availableQuantity").GetDecimal());
        Assert.Equal(0, emptyWhJson.GetProperty("lots").GetArrayLength());

        // 2. Filtro por warehouseId devuelve únicamente esa bodega
        var filteredResp = await planner.GetAsync($"/api/v1/logistics/stock?productId={productId}&warehouseId={secondWarehouseId}");
        Assert.Equal(HttpStatusCode.OK, filteredResp.StatusCode);
        var filteredRoot = await filteredResp.Content.ReadFromJsonAsync<JsonElement>();
        var filteredWhs = filteredRoot.GetProperty("warehouses").EnumerateArray().ToList();
        Assert.Single(filteredWhs);
        Assert.Equal(secondWarehouseId, filteredWhs[0].GetProperty("warehouseId").GetGuid());
        Assert.Equal(50m, filteredWhs[0].GetProperty("netUsableQuantity").GetDecimal());
        Assert.Equal(5m, filteredWhs[0].GetProperty("safetyStock").GetDecimal());
        Assert.Equal(45m, filteredWhs[0].GetProperty("availableQuantity").GetDecimal());
    }

    [Fact]
    public async Task Sourcing_by_orders_handles_single_and_multi_warehouse_competing_demands_shortages_and_suggest_regression()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: CreateOverrides());
        using var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        Guid productId;
        Guid nearWhId;
        Guid farWhId;
        Guid lotNearId;
        Guid lotFarId;
        Guid order1Id;
        Guid order2Id;

        await using (var db = TestDb.Context(connection))
        {
            var mainWh = await db.Locations.SingleAsync(
                l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive);
            nearWhId = mainWh.Id;

            var farWh = new DomainLocation
            {
                OrganizationId = DemoOrganization.Id,
                Code = "BOD-SUR-AUX",
                Name = "Bodega Auxiliar Sur",
                Kind = LocationKinds.Warehouse,
                Position = Gf.CreatePoint(new Coordinate(-78.5800, -0.3500)),
                IsActive = true
            };
            db.Locations.Add(farWh);
            farWhId = farWh.Id;

            var product = new Product
            {
                OrganizationId = DemoOrganization.Id,
                Sku = "TST-SRC-01",
                Name = "Kit Quirúrgico Sourcing",
                BaseUnit = "kit",
                WeightPerBaseUnit = 2.0m,
                VolumePerBaseUnit = 0.01m,
                IsActive = true
            };
            db.Products.Add(product);
            productId = product.Id;

            var lotNear = new Lot
            {
                OrganizationId = DemoOrganization.Id,
                ProductId = productId,
                Code = "LOT-NEAR-01",
                ExpiresOn = new DateOnly(2026, 12, 1),
                IsBlocked = false
            };
            var lotFar = new Lot
            {
                OrganizationId = DemoOrganization.Id,
                ProductId = productId,
                Code = "LOT-FAR-01",
                ExpiresOn = new DateOnly(2027, 2, 1),
                IsBlocked = false
            };
            db.Lots.AddRange(lotNear, lotFar);
            lotNearId = lotNear.Id;
            lotFarId = lotFar.Id;

            // nearWh tiene 100 en mano -> 10% seguridad = 10 -> 90 asignables
            // farWh tiene 50 en mano -> 10% seguridad = 5 -> 45 asignables
            // Total asignable entre ambas bodegas = 90 + 45 = 135
            db.StockBalances.AddRange(
                new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = nearWhId,
                    LotId = lotNearId,
                    QuantityOnHand = 100m,
                    QuantityReserved = 0m
                },
                new StockBalance
                {
                    OrganizationId = DemoOrganization.Id,
                    LocationId = farWhId,
                    LotId = lotFarId,
                    QuantityOnHand = 50m,
                    QuantityReserved = 0m
                });

            var sp1 = await db.ServicePoints
                .Where(sp => sp.OrganizationId == DemoOrganization.Id && sp.IsActive)
                .OrderBy(sp => sp.Code)
                .FirstAsync();
            var sp2 = await db.ServicePoints
                .Where(sp => sp.OrganizationId == DemoOrganization.Id && sp.IsActive && sp.Id != sp1.Id)
                .OrderBy(sp => sp.Code)
                .FirstAsync();

            // Pedido 1 pide 80 (cabe completo en nearWh porque asignable es 90; quedan 10 en nearWh)
            var ord1 = new DeliveryOrder
            {
                OrganizationId = DemoOrganization.Id,
                OrderNumber = "ORD-TST-001",
                DestinationServicePointId = sp1.Id,
                RequiredDateUtc = DateTime.UtcNow.AddDays(1),
                Priority = "High",
                Status = DeliveryOrderStatus.Pending,
                SourceType = "Manual"
            };
            // Pedido 2 pide 70 (toma los 10 restantes de nearWh + los 45 asignables de farWh = 55; faltante = 15)
            var ord2 = new DeliveryOrder
            {
                OrganizationId = DemoOrganization.Id,
                OrderNumber = "ORD-TST-002",
                DestinationServicePointId = sp2.Id,
                RequiredDateUtc = DateTime.UtcNow.AddDays(1),
                Priority = "Normal",
                Status = DeliveryOrderStatus.Pending,
                SourceType = "Manual"
            };
            db.DeliveryOrders.AddRange(ord1, ord2);
            order1Id = ord1.Id;
            order2Id = ord2.Id;

            db.DeliveryOrderLines.AddRange(
                new DeliveryOrderLine
                {
                    OrganizationId = DemoOrganization.Id,
                    DeliveryOrderId = order1Id,
                    ProductId = productId,
                    RequestedQuantity = 80m
                },
                new DeliveryOrderLine
                {
                    OrganizationId = DemoOrganization.Id,
                    DeliveryOrderId = order2Id,
                    ProductId = productId,
                    RequestedQuantity = 70m
                });

            await db.SaveChangesAsync();
        }

        // 1. Un solo pedido (80 <= 90 disponibles en nearWh): abastecido al 100% desde 1 bodega -> solo Opción A
        var singleResp = await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = new[] { order1Id }
        });
        Assert.Equal(HttpStatusCode.OK, singleResp.StatusCode);
        var singleJson = await singleResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Order", singleJson.GetProperty("sourceKind").GetString());
        Assert.True(singleJson.GetProperty("fullySourced").GetBoolean());
        Assert.Single(singleJson.GetProperty("options").EnumerateArray());

        var singleDemand = singleJson.GetProperty("demands")[0];
        Assert.Equal(order1Id, singleDemand.GetProperty("orderId").GetGuid());
        Assert.Equal(80m, singleDemand.GetProperty("requestedQuantity").GetDecimal());
        Assert.Equal(80m, singleDemand.GetProperty("allocatedQuantity").GetDecimal());
        Assert.Equal(0m, singleDemand.GetProperty("shortageQuantity").GetDecimal());
        Assert.True(singleDemand.GetProperty("fullySourced").GetBoolean());

        // 2. Dos pedidos compitiendo por el mismo stock (80 + 70 = 150 solicitados vs 135 disponibles totales)
        // También enviamos order1Id duplicado para comprobar deduplicación sin doble consumo
        var multiResp = await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = new[] { order1Id, order1Id, order2Id }
        });
        Assert.Equal(HttpStatusCode.OK, multiResp.StatusCode);
        var multiJson = await multiResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(multiJson.GetProperty("fullySourced").GetBoolean());

        // Como order2 se divide entre nearWh y farWh, se devuelven Opción A y Opción B
        var options = multiJson.GetProperty("options").EnumerateArray().ToList();
        Assert.Equal(2, options.Count);
        Assert.Equal("A", options[0].GetProperty("optionId").GetString());
        Assert.Equal("B", options[1].GetProperty("optionId").GetString());

        var demands = multiJson.GetProperty("demands").EnumerateArray().ToList();
        Assert.Equal(2, demands.Count);

        var d1 = demands[0];
        Assert.Equal(order1Id, d1.GetProperty("orderId").GetGuid());
        Assert.Equal(80m, d1.GetProperty("allocatedQuantity").GetDecimal());
        Assert.Equal(0m, d1.GetProperty("shortageQuantity").GetDecimal());
        Assert.True(d1.GetProperty("fullySourced").GetBoolean());

        var d2 = demands[1];
        Assert.Equal(order2Id, d2.GetProperty("orderId").GetGuid());
        Assert.Equal(70m, d2.GetProperty("requestedQuantity").GetDecimal());
        Assert.Equal(55m, d2.GetProperty("allocatedQuantity").GetDecimal()); // 10 de nearWh + 45 de farWh
        Assert.Equal(15m, d2.GetProperty("shortageQuantity").GetDecimal());
        Assert.False(d2.GetProperty("fullySourced").GetBoolean());

        var d2Allocs = d2.GetProperty("allocations").EnumerateArray().ToList();
        Assert.Equal(2, d2Allocs.Count);
        Assert.Equal(nearWhId, d2Allocs[0].GetProperty("warehouseId").GetGuid());
        Assert.Equal(10m, d2Allocs[0].GetProperty("quantity").GetDecimal());
        Assert.Equal(farWhId, d2Allocs[1].GetProperty("warehouseId").GetGuid());
        Assert.Equal(45m, d2Allocs[1].GetProperty("quantity").GetDecimal());

        // Suma asignada en toda la consulta = 80 + 55 = 135 <= 135 disponible (nunca consume los 10 + 5 de seguridad)
        decimal totalAllocatedAcrossDemands = demands.Sum(d => d.GetProperty("allocatedQuantity").GetDecimal());
        Assert.Equal(135m, totalAllocatedAcrossDemands);

        // 3. Regresión de POST /api/v1/route-plans/suggest: usa SourcingService con descuento de seguridad
        var suggestResp = await planner.PostAsJsonAsync("/api/v1/route-plans/suggest", new
        {
            orderIds = new[] { order1Id, order2Id },
            plannedDate = "2026-10-12"
        });
        Assert.Equal(HttpStatusCode.OK, suggestResp.StatusCode);
        var suggestJson = await suggestResp.Content.ReadFromJsonAsync<JsonElement>();
        var suggestOpts = suggestJson.GetProperty("options").EnumerateArray().ToList();
        Assert.Equal(2, suggestOpts.Count);
        var suggestLines = suggestOpts[0].GetProperty("sourcingLines").EnumerateArray().ToList();
        Assert.Equal(80m, suggestLines[0].GetProperty("totalAllocated").GetDecimal());
        Assert.Equal(55m, suggestLines[1].GetProperty("totalAllocated").GetDecimal());

        // #199: ambas consultas deben ofrecer solo lo que aún no está planificado.
        await using (var db = TestDb.Context(connection))
        {
            var partialLine = await db.DeliveryOrderLines.SingleAsync(l => l.DeliveryOrderId == order1Id);
            var completeLine = await db.DeliveryOrderLines.SingleAsync(l => l.DeliveryOrderId == order2Id);
            partialLine.PlannedQuantity = 60m;
            completeLine.PlannedQuantity = completeLine.RequestedQuantity;
            await db.SaveChangesAsync();
        }

        var remainingResp = await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = new[] { order1Id, order2Id }
        });
        remainingResp.EnsureSuccessStatusCode();
        var remainingJson = await remainingResp.Content.ReadFromJsonAsync<JsonElement>();
        var remainingDemand = Assert.Single(remainingJson.GetProperty("demands").EnumerateArray());
        Assert.Equal(order1Id, remainingDemand.GetProperty("orderId").GetGuid());
        Assert.Equal(20m, remainingDemand.GetProperty("requestedQuantity").GetDecimal());
        Assert.Equal(20m, remainingDemand.GetProperty("allocatedQuantity").GetDecimal());

        var remainingSuggestResp = await planner.PostAsJsonAsync("/api/v1/route-plans/suggest", new
        {
            orderIds = new[] { order1Id, order2Id },
            plannedDate = "2026-10-12"
        });
        remainingSuggestResp.EnsureSuccessStatusCode();
        var remainingSuggest = await remainingSuggestResp.Content.ReadFromJsonAsync<JsonElement>();
        var remainingSuggestLine = Assert.Single(remainingSuggest.GetProperty("options")[0].GetProperty("sourcingLines").EnumerateArray());
        Assert.Equal(20m, remainingSuggestLine.GetProperty("requestedQuantity").GetDecimal());
        Assert.Equal(20m, remainingSuggestLine.GetProperty("totalAllocated").GetDecimal());

        await using (var db = TestDb.Context(connection))
        {
            var partialLine = await db.DeliveryOrderLines.SingleAsync(l => l.DeliveryOrderId == order1Id);
            Assert.Equal(60m, partialLine.PlannedQuantity);
            partialLine.PlannedQuantity = partialLine.RequestedQuantity;
            await db.SaveChangesAsync();
        }

        var fullyPlannedResp = await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = new[] { order1Id, order2Id }
        });
        fullyPlannedResp.EnsureSuccessStatusCode();
        var fullyPlanned = await fullyPlannedResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(fullyPlanned.GetProperty("demands").EnumerateArray());
        Assert.Empty(fullyPlanned.GetProperty("options").EnumerateArray());
    }

    [Fact]
    public async Task Sourcing_by_service_points_calculates_risks_without_persisting_orders_and_preserves_multiple_products_per_point()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: CreateOverrides());
        using var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        Guid hosN01Id;
        Guid des01ProductId;
        Guid secondProductId;
        Guid safePointId;
        int ordersBefore;
        int snapshotsBefore;

        await using (var db = TestDb.Context(connection))
        {
            var hosN01 = await db.ServicePoints.SingleAsync(
                sp => sp.OrganizationId == DemoOrganization.Id && sp.Code == "HOS-N01");
            hosN01Id = hosN01.Id;

            var des01 = await db.Products.SingleAsync(
                p => p.OrganizationId == DemoOrganization.Id && p.Sku == "DES-01");
            des01ProductId = des01.Id;

            var mainWh = await db.Locations.SingleAsync(
                l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive);

            var extraProd = new Product
            {
                OrganizationId = DemoOrganization.Id,
                Sku = "TST-RISK-02",
                Name = "Guante Quirúrgico Crítico",
                BaseUnit = "caja",
                WeightPerBaseUnit = 0.5m,
                VolumePerBaseUnit = 0.005m,
                IsActive = true
            };
            db.Products.Add(extraProd);
            secondProductId = extraProd.Id;

            var extraLot = new Lot
            {
                OrganizationId = DemoOrganization.Id,
                ProductId = secondProductId,
                Code = "LOT-RISK-02",
                ExpiresOn = new DateOnly(2027, 5, 1),
                IsBlocked = false
            };
            db.Lots.Add(extraLot);

            db.StockBalances.Add(new StockBalance
            {
                OrganizationId = DemoOrganization.Id,
                LocationId = mainWh.Id,
                LotId = extraLot.Id,
                QuantityOnHand = 200m,
                QuantityReserved = 0m
            });

            // Consumo en HOS-N01 para extraProd sin stock en el punto -> Cobertura 0d (Critico) y SuggestedQuantity = 100
            db.Consumptions.Add(new Consumption
            {
                OrganizationId = DemoOrganization.Id,
                ServicePointId = hosN01Id,
                ProductId = secondProductId,
                OccurredOn = new DateOnly(2026, 10, 2),
                PeriodDays = 10,
                Quantity = 100m,
                SourceKind = "test"
            });

            // Punto adicional sin consumos -> nivel SinDatos y SuggestedQuantity == 0
            var safePoint = new DomainServicePoint
            {
                OrganizationId = DemoOrganization.Id,
                Code = "PUNTO-SIN-RIESGO",
                Name = "Punto Sin Riesgo",
                Category = "CentroSalud",
                Criticality = "normal",
                Position = Gf.CreatePoint(new Coordinate(-78.4900, -0.1900)),
                IsActive = true
            };
            db.ServicePoints.Add(safePoint);
            safePointId = safePoint.Id;

            await db.SaveChangesAsync();

            ordersBefore = await db.DeliveryOrders.CountAsync(o => o.OrganizationId == DemoOrganization.Id);
            snapshotsBefore = await db.RiskSnapshots.CountAsync(s => s.OrganizationId == DemoOrganization.Id);
        }

        // 1. Consulta por punto HOS-N01 (sin filtro de producto): debe conservar TODOS los productos en riesgo del mismo punto
        var pointResp = await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            servicePointIds = new[] { hosN01Id }
        });
        Assert.Equal(HttpStatusCode.OK, pointResp.StatusCode);
        var pointJson = await pointResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Risk", pointJson.GetProperty("sourceKind").GetString());

        var demands = pointJson.GetProperty("demands").EnumerateArray().ToList();
        Assert.True(demands.Count >= 2, "Debe conservar múltiples productos en riesgo para un mismo punto de servicio.");
        Assert.All(demands, d =>
        {
            Assert.Equal(JsonValueKind.Null, d.GetProperty("orderId").ValueKind);
            Assert.Equal(JsonValueKind.Null, d.GetProperty("orderNumber").ValueKind);
            Assert.Equal("Risk", d.GetProperty("sourceKind").GetString());
            Assert.Equal(hosN01Id, d.GetProperty("destinationId").GetGuid());
            Assert.True(d.GetProperty("requestedQuantity").GetDecimal() > 0m);
        });
        var distinctProductIds = demands.Select(d => d.GetProperty("productId").GetGuid()).Distinct().ToList();
        Assert.Equal(demands.Count, distinctProductIds.Count);
        Assert.Contains(des01ProductId, distinctProductIds);
        Assert.Contains(secondProductId, distinctProductIds);
        // Verificar orden de urgencia: Critico (secondProductId) antes que Alto (des01ProductId)
        Assert.Equal("Critico", demands[0].GetProperty("riskLevel").GetString());
        Assert.Equal(secondProductId, demands[0].GetProperty("productId").GetGuid());

        // 2. Filtro por productId con servicePointIds devuelve únicamente ese producto
        var filteredResp = await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            servicePointIds = new[] { hosN01Id },
            productId = des01ProductId
        });
        Assert.Equal(HttpStatusCode.OK, filteredResp.StatusCode);
        var filteredJson = await filteredResp.Content.ReadFromJsonAsync<JsonElement>();
        var filteredDemands = filteredJson.GetProperty("demands").EnumerateArray().ToList();
        Assert.Single(filteredDemands);
        Assert.Equal(des01ProductId, filteredDemands[0].GetProperty("productId").GetGuid());

        // Comparar que la cantidad solicitada proviene exactamente de RiskCalculator
        var risksResp = await planner.GetAsync("/api/v1/risks");
        var risksJson = await risksResp.Content.ReadFromJsonAsync<JsonElement>();
        var expectedSuggested = risksJson.GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("servicePointCode").GetString() == "HOS-N01" &&
                         i.GetProperty("sku").GetString() == "DES-01")
            .GetProperty("suggestedQuantity").GetDecimal();
        Assert.Equal(expectedSuggested, filteredDemands[0].GetProperty("requestedQuantity").GetDecimal());

        // 3. Punto sin riesgos elegibles devuelve 200 OK con demands: [] y options: []
        var noRiskResp = await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            servicePointIds = new[] { safePointId }
        });
        Assert.Equal(HttpStatusCode.OK, noRiskResp.StatusCode);
        var noRiskJson = await noRiskResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, noRiskJson.GetProperty("demands").GetArrayLength());
        Assert.Equal(0, noRiskJson.GetProperty("options").GetArrayLength());

        // Verificar que no se crearon DeliveryOrders ni RiskSnapshots
        await using (var db = TestDb.Context(connection))
        {
            Assert.Equal(ordersBefore, await db.DeliveryOrders.CountAsync(o => o.OrganizationId == DemoOrganization.Id));
            Assert.Equal(snapshotsBefore, await db.RiskSnapshots.CountAsync(s => s.OrganizationId == DemoOrganization.Id));
        }
    }

    [Fact]
    public async Task Vehicles_available_respects_operational_timezone_midnight_boundaries_published_status_and_fallback_tariff()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: CreateOverrides());
        using var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        Guid depotId;
        Guid vFreeFallbackId;
        Guid vInactiveId;
        Guid vPublishedLateLocalId;
        Guid vPublishedPrevDayLateLocalId;
        Guid vProposedSameDayId;

        await using (var db = TestDb.Context(connection))
        {
            var depot = await db.Locations.SingleAsync(
                l => l.OrganizationId == DemoOrganization.Id && l.Kind == LocationKinds.Warehouse && l.IsActive);
            depotId = depot.Id;

            var vFreeFallback = new Vehicle
            {
                OrganizationId = DemoOrganization.Id,
                Code = "VEH-FREE-FALLBACK",
                DepotLocationId = depotId,
                MaxWeight = 1800m,
                MaxVolume = null, // Volumen nullable
                CostPerKm = null, // Debe usar tarifa de respaldo DefaultCostPerKm (0.45)
                IsActive = true
            };
            var vInactive = new Vehicle
            {
                OrganizationId = DemoOrganization.Id,
                Code = "VEH-INACTIVE",
                DepotLocationId = depotId,
                MaxWeight = 2000m,
                MaxVolume = 12m,
                CostPerKm = 0.60m,
                IsActive = false
            };
            var vPublishedLateLocal = new Vehicle
            {
                OrganizationId = DemoOrganization.Id,
                Code = "VEH-PUB-22LOCAL",
                DepotLocationId = depotId,
                MaxWeight = 2000m,
                MaxVolume = 12m,
                CostPerKm = 0.55m,
                IsActive = true
            };
            var vPublishedPrevDayLateLocal = new Vehicle
            {
                OrganizationId = DemoOrganization.Id,
                Code = "VEH-PUB-PREVDAY",
                DepotLocationId = depotId,
                MaxWeight = 2000m,
                MaxVolume = 12m,
                CostPerKm = 0.52m,
                IsActive = true
            };
            var vProposedSameDay = new Vehicle
            {
                OrganizationId = DemoOrganization.Id,
                Code = "VEH-PROPOSED",
                DepotLocationId = depotId,
                MaxWeight = 2000m,
                MaxVolume = 12m,
                CostPerKm = 0.50m,
                IsActive = true
            };

            db.Vehicles.AddRange(
                vFreeFallback,
                vInactive,
                vPublishedLateLocal,
                vPublishedPrevDayLateLocal,
                vProposedSameDay);

            vFreeFallbackId = vFreeFallback.Id;
            vInactiveId = vInactive.Id;
            vPublishedLateLocalId = vPublishedLateLocal.Id;
            vPublishedPrevDayLateLocalId = vPublishedPrevDayLateLocal.Id;
            vProposedSameDayId = vProposedSameDay.Id;

            // En America/Guayaquil (UTC-5), el día 2026-10-12 abarca [2026-10-12T05:00:00Z, 2026-10-13T05:00:00Z).
            // 1. Ruta Published en 2026-10-13T03:00:00Z = 2026-10-12 22:00:00 local -> BLOQUEA en 2026-10-12 (duplicada para probar exclusión única)
            db.RoutePlans.AddRange(
                new RoutePlan
                {
                    OrganizationId = DemoOrganization.Id,
                    Code = "PLAN-PUB-LATE-1",
                    Revision = 1,
                    VehicleId = vPublishedLateLocalId,
                    Status = "Published",
                    ProposedBy = "test",
                    PlannedStartUtc = new DateTime(2026, 10, 13, 3, 0, 0, DateTimeKind.Utc)
                },
                new RoutePlan
                {
                    OrganizationId = DemoOrganization.Id,
                    Code = "PLAN-PUB-LATE-2",
                    Revision = 1,
                    VehicleId = vPublishedLateLocalId,
                    Status = "Published",
                    ProposedBy = "test",
                    PlannedStartUtc = new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc)
                },
                // 2. Ruta Published en 2026-10-12T04:30:00Z = 2026-10-11 23:30:00 local -> NO bloquea 2026-10-12 (pertenece a 2026-10-11 local)
                new RoutePlan
                {
                    OrganizationId = DemoOrganization.Id,
                    Code = "PLAN-PUB-PREVDAY",
                    Revision = 1,
                    VehicleId = vPublishedPrevDayLateLocalId,
                    Status = "Published",
                    ProposedBy = "test",
                    PlannedStartUtc = new DateTime(2026, 10, 12, 4, 30, 0, DateTimeKind.Utc)
                },
                // 3. Rutas Proposed, Cancelled y Superseded en 2026-10-12 -> NO bloquean
                new RoutePlan
                {
                    OrganizationId = DemoOrganization.Id,
                    Code = "PLAN-PROP-1",
                    Revision = 1,
                    VehicleId = vProposedSameDayId,
                    Status = "Proposed",
                    ProposedBy = "test",
                    PlannedStartUtc = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc)
                },
                new RoutePlan
                {
                    OrganizationId = DemoOrganization.Id,
                    Code = "PLAN-CANC-1",
                    Revision = 1,
                    VehicleId = vProposedSameDayId,
                    Status = "Cancelled",
                    ProposedBy = "test",
                    PlannedStartUtc = new DateTime(2026, 10, 12, 13, 0, 0, DateTimeKind.Utc)
                },
                new RoutePlan
                {
                    OrganizationId = DemoOrganization.Id,
                    Code = "PLAN-SUP-1",
                    Revision = 1,
                    VehicleId = vProposedSameDayId,
                    Status = "Superseded",
                    ProposedBy = "test",
                    PlannedStartUtc = new DateTime(2026, 10, 12, 15, 0, 0, DateTimeKind.Utc)
                });

            await db.SaveChangesAsync();
        }

        // 1. Consulta sin parámetro date -> usa reloj operativo (2026-10-12 en demo-region)
        var resp = await planner.GetAsync("/api/v1/logistics/vehicles/available");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var root = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("2026-10-12", root.GetProperty("operationalDate").GetString());
        Assert.Equal("2026-10-12", root.GetProperty("date").GetString());
        Assert.Equal("America/Guayaquil", root.GetProperty("timeZone").GetString());
        Assert.Equal(new DateTime(2026, 10, 12, 5, 0, 0, DateTimeKind.Utc), root.GetProperty("dayStartUtc").GetDateTime());
        Assert.Equal(new DateTime(2026, 10, 13, 5, 0, 0, DateTimeKind.Utc), root.GetProperty("dayEndUtc").GetDateTime());

        var vehicles = root.GetProperty("vehicles").EnumerateArray().ToList();
        var vehicleIds = vehicles.Select(v => v.GetProperty("vehicleId").GetGuid()).ToList();

        // Cada disponible aparece una sola vez y ordenado por código
        Assert.Equal(vehicleIds.Distinct().Count(), vehicleIds.Count);
        var codes = vehicles.Select(v => v.GetProperty("code").GetString()!).ToList();
        Assert.Equal(codes.OrderBy(c => c, StringComparer.Ordinal).ToList(), codes);

        Assert.Contains(vFreeFallbackId, vehicleIds);
        Assert.Contains(vPublishedPrevDayLateLocalId, vehicleIds);
        Assert.Contains(vProposedSameDayId, vehicleIds);
        Assert.DoesNotContain(vInactiveId, vehicleIds);
        Assert.DoesNotContain(vPublishedLateLocalId, vehicleIds);

        // Verificar campos de VEH-FREE-FALLBACK: tarifa fallback (0.45), unidad USD/km, volumen null y estimatedTotalCost null
        var fallbackDto = vehicles.Single(v => v.GetProperty("vehicleId").GetGuid() == vFreeFallbackId);
        Assert.Equal(1800m, fallbackDto.GetProperty("maxWeight").GetDecimal());
        Assert.Equal(JsonValueKind.Null, fallbackDto.GetProperty("maxVolume").ValueKind);
        Assert.Equal(depotId, fallbackDto.GetProperty("depotLocationId").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(fallbackDto.GetProperty("depotCode").GetString()));
        Assert.Equal(0.45m, fallbackDto.GetProperty("costPerKm").GetDecimal());
        Assert.Equal("USD/km", fallbackDto.GetProperty("costUnit").GetString());
        Assert.Equal(JsonValueKind.Null, fallbackDto.GetProperty("estimatedTotalCost").ValueKind);

        // 2. Consulta en 2026-10-11: vPublishedPrevDayLateLocalId (2026-10-12T04:30:00Z = 2026-10-11 23:30 local) está ocupado, mientras vPublishedLateLocalId está libre
        var prevDayResp = await planner.GetAsync("/api/v1/logistics/vehicles/available?date=2026-10-11");
        Assert.Equal(HttpStatusCode.OK, prevDayResp.StatusCode);
        var prevDayJson = await prevDayResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-10-12", prevDayJson.GetProperty("operationalDate").GetString());
        Assert.Equal("2026-10-11", prevDayJson.GetProperty("date").GetString());
        var prevDayVehicleIds = prevDayJson.GetProperty("vehicles").EnumerateArray()
            .Select(v => v.GetProperty("vehicleId").GetGuid())
            .ToList();
        Assert.DoesNotContain(vPublishedPrevDayLateLocalId, prevDayVehicleIds);
        Assert.Contains(vPublishedLateLocalId, prevDayVehicleIds);
    }

    [Fact]
    public async Task Validation_enforces_400_404_422_limits_and_rejects_other_organization_ids()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: CreateOverrides());
        using var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        Guid validProductId;
        Guid validPointId;
        Guid otherOrgProductId;
        Guid otherOrgOrderId;

        await using (var db = TestDb.Context(connection))
        {
            validProductId = await db.Products
                .Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive)
                .Select(p => p.Id)
                .FirstAsync();
            validPointId = await db.ServicePoints
                .Where(sp => sp.OrganizationId == DemoOrganization.Id && sp.IsActive)
                .Select(sp => sp.Id)
                .FirstAsync();

            var otherOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Code = "OTHER-ORG",
                Name = "Otra Organización",
                TimeZone = "America/Guayaquil"
            };
            db.Organizations.Add(otherOrg);

            var otherProd = new Product
            {
                OrganizationId = otherOrg.Id,
                Sku = "OTH-01",
                Name = "Producto Ajeno",
                BaseUnit = "u",
                IsActive = true
            };
            var otherOrder = new DeliveryOrder
            {
                OrganizationId = otherOrg.Id,
                OrderNumber = "ORD-OTHER-01",
                RequiredDateUtc = DateTime.UtcNow,
                Status = DeliveryOrderStatus.Pending
            };
            db.Products.Add(otherProd);
            db.DeliveryOrders.Add(otherOrder);
            otherOrgProductId = otherProd.Id;
            otherOrgOrderId = otherOrder.Id;

            await db.SaveChangesAsync();
        }

        // --- Stock validations ---
        // Falta productId o Guid.Empty -> 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.GetAsync("/api/v1/logistics/stock")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.GetAsync($"/api/v1/logistics/stock?productId={Guid.Empty}")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.GetAsync($"/api/v1/logistics/stock?productId={validProductId}&warehouseId={Guid.Empty}")).StatusCode);
        // Formato UUID inválido -> 400
        Assert.Equal(HttpStatusCode.BadRequest, (await planner.GetAsync("/api/v1/logistics/stock?productId=no-es-uuid")).StatusCode);
        // Producto o bodega inexistente o de otra organización -> 404
        Assert.Equal(HttpStatusCode.NotFound, (await planner.GetAsync($"/api/v1/logistics/stock?productId={Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await planner.GetAsync($"/api/v1/logistics/stock?productId={otherOrgProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await planner.GetAsync($"/api/v1/logistics/stock?productId={validProductId}&warehouseId={Guid.NewGuid()}")).StatusCode);

        // --- Sourcing validations ---
        // Cuerpo JSON mal formado -> 400
        var badJsonContent = new StringContent("{ invalid json", Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await planner.PostAsync("/api/v1/logistics/sourcing", badJsonContent)).StatusCode);

        // Sin selectores o con ambos selectores -> 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = new[] { Guid.NewGuid() },
            servicePointIds = new[] { validPointId }
        })).StatusCode);

        // Listas vacías o con Guid.Empty -> 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = Array.Empty<Guid>()
        })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            servicePointIds = Array.Empty<Guid>()
        })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = new[] { Guid.Empty }
        })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            servicePointIds = new[] { Guid.Empty }
        })).StatusCode);

        // productId con orderIds -> 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = new[] { Guid.NewGuid() },
            productId = validProductId
        })).StatusCode);

        // Más de 100 IDs únicos -> 422
        var overLimitIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = overLimitIds
        })).StatusCode);

        // Lista mixta (un ID válido y uno inexistente/ajeno) -> 404 (no acepta silenciosamente un subconjunto)
        Assert.Equal(HttpStatusCode.NotFound, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            servicePointIds = new[] { validPointId, Guid.NewGuid() }
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            orderIds = new[] { otherOrgOrderId }
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new
        {
            servicePointIds = new[] { validPointId },
            productId = otherOrgProductId
        })).StatusCode);

        // --- Vehicles validations ---
        Assert.Equal(HttpStatusCode.BadRequest, (await planner.GetAsync("/api/v1/logistics/vehicles/available?date=not-a-date")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await planner.GetAsync("/api/v1/logistics/vehicles/available?date=12-10-2026")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await planner.GetAsync("/api/v1/logistics/vehicles/available?date=2026-02-30")).StatusCode);
    }

    [Fact]
    public async Task Access_control_allows_planning_and_valid_m2m_and_rejects_other_roles_anonymous_and_invalid_m2m()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: CreateOverrides());
        using var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        await SeedTechnicalAccountAsync(connection);

        Guid productId;
        Guid pointId;
        await using (var db = TestDb.Context(connection))
        {
            productId = await db.Products
                .Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive)
                .Select(p => p.Id)
                .FirstAsync();
            pointId = await db.ServicePoints
                .Where(sp => sp.OrganizationId == DemoOrganization.Id && sp.IsActive)
                .Select(sp => sp.Id)
                .FirstAsync();
        }

        async Task AssertStatusForAllThreeAsync(HttpClient client, HttpStatusCode expected)
        {
            var r1 = await client.GetAsync($"/api/v1/logistics/stock?productId={productId}");
            var r2 = await client.PostAsJsonAsync("/api/v1/logistics/sourcing", new { servicePointIds = new[] { pointId } });
            var r3 = await client.GetAsync("/api/v1/logistics/vehicles/available");
            Assert.Equal(expected, r1.StatusCode);
            Assert.Equal(expected, r2.StatusCode);
            Assert.Equal(expected, r3.StatusCode);
        }

        // 1. Planificador y Gerencia -> 200 OK
        await AssertStatusForAllThreeAsync(planner, HttpStatusCode.OK);
        using var manager = api.ClientFor("manager-1");
        await AssertStatusForAllThreeAsync(manager, HttpStatusCode.OK);

        // 2. M2M válido (Automatizacion + scope istpetdev-api/automation + cliente técnico permitido) -> 200 OK
        using var automation = CreateAutomationClient(api);
        await AssertStatusForAllThreeAsync(automation, HttpStatusCode.OK);

        // 3. Restantes roles humanos (Conductor, Receptor, Bodega, Auditor) -> 403 Forbidden
        using var driver = api.ClientFor(TestTokens.Driver);
        using var receiver = api.ClientFor(TestTokens.Receiver);
        using var warehouse = api.ClientFor("warehouse-1");
        using var auditor = api.ClientFor("auditor-1");
        await AssertStatusForAllThreeAsync(driver, HttpStatusCode.Forbidden);
        await AssertStatusForAllThreeAsync(receiver, HttpStatusCode.Forbidden);
        await AssertStatusForAllThreeAsync(warehouse, HttpStatusCode.Forbidden);
        await AssertStatusForAllThreeAsync(auditor, HttpStatusCode.Forbidden);

        // 4. Anónimo -> 401 Unauthorized
        using var anon = api.ClientFor(null);
        await AssertStatusForAllThreeAsync(anon, HttpStatusCode.Unauthorized);

        // 5. M2M con scope incorrecto -> 401 Unauthorized
        using var badScopeClient = CreateAutomationClient(api, scope: "istpetdev-api/other");
        await AssertStatusForAllThreeAsync(badScopeClient, HttpStatusCode.Unauthorized);

        // 6. M2M con client_id no autorizado -> 401 Unauthorized
        using var badClientId = CreateAutomationClient(api, clientId: "unauthorized-m2m-client");
        await AssertStatusForAllThreeAsync(badClientId, HttpStatusCode.Unauthorized);

        // 7. M2M con cuenta técnica inactiva o rol incompatible -> 403 Forbidden
        await TestAccounts.SetRoleAsync(connection, TechnicalClientId, Roles.Automatizacion, active: false);
        await AssertStatusForAllThreeAsync(automation, HttpStatusCode.Forbidden);

        await TestAccounts.SetRoleAsync(connection, TechnicalClientId, Roles.Conductor, active: true);
        await AssertStatusForAllThreeAsync(automation, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Queries_are_strictly_read_only_for_logistics_state_and_record_agent_trace_only_for_automation()
    {
        var connection = postgres.NewDatabase();
        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: CreateOverrides());
        using var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);
        await SeedTechnicalAccountAsync(connection);

        Guid productId;
        Guid pointId;
        Guid orderId;
        Guid runId;

        await using (var db = TestDb.Context(connection))
        {
            productId = await db.Products
                .Where(p => p.OrganizationId == DemoOrganization.Id && p.IsActive)
                .Select(p => p.Id)
                .FirstAsync();
            pointId = await db.ServicePoints
                .Where(sp => sp.OrganizationId == DemoOrganization.Id && sp.Code == "HOS-N01")
                .Select(sp => sp.Id)
                .FirstAsync();

            var order = new DeliveryOrder
            {
                OrganizationId = DemoOrganization.Id,
                OrderNumber = "ORD-READONLY-01",
                DestinationServicePointId = pointId,
                RequiredDateUtc = DateTime.UtcNow.AddDays(1),
                Priority = "High",
                Status = DeliveryOrderStatus.Pending,
                SourceType = "Manual"
            };
            db.DeliveryOrders.Add(order);
            orderId = order.Id;

            db.DeliveryOrderLines.Add(new DeliveryOrderLine
            {
                OrganizationId = DemoOrganization.Id,
                DeliveryOrderId = orderId,
                ProductId = productId,
                RequestedQuantity = 25m
            });

            var run = new AgentRun
            {
                OrganizationId = DemoOrganization.Id,
                Kind = "Watch",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                ToolCallsJson = "[]"
            };
            db.AgentRuns.Add(run);
            runId = run.Id;

            await db.SaveChangesAsync();
        }

        async Task<string> CaptureLogisticsStateSignatureAsync()
        {
            await using var db = TestDb.Context(connection);
            var snapshot = new
            {
                Orders = await db.DeliveryOrders.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.OrderNumber, x.Status, x.Version })
                    .ToListAsync(),
                OrderLines = await db.DeliveryOrderLines.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.DeliveryOrderId, x.ProductId, x.RequestedQuantity, x.PlannedQuantity, x.DeliveredQuantity })
                    .ToListAsync(),
                Plans = await db.RoutePlans.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.Code, x.Revision, x.Status })
                    .ToListAsync(),
                Stops = await db.RouteStops.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.RoutePlanId, x.SequenceNumber, x.Status })
                    .ToListAsync(),
                Reservations = await db.RouteStockReservations.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.LotId, x.Quantity, x.Status })
                    .ToListAsync(),
                Balances = await db.StockBalances.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.LocationId, x.LotId, x.QuantityOnHand, x.QuantityReserved })
                    .ToListAsync(),
                Movements = await db.InventoryMovements.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.Kind, x.LotId, x.Quantity })
                    .ToListAsync(),
                Deliveries = await db.Deliveries.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.Code, x.Status })
                    .ToListAsync(),
                Snapshots = await db.RiskSnapshots.AsNoTracking()
                    .Where(x => x.OrganizationId == DemoOrganization.Id)
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.ServicePointId, x.ProductId, x.SuggestedQuantity })
                    .ToListAsync()
            };
            return JsonSerializer.Serialize(snapshot);
        }

        var beforeSignature = await CaptureLogisticsStateSignatureAsync();

        // 1. Llamadas humanas con X-Agent-Run-Id: 200 OK y NO agregan trazas a AgentRun
        planner.DefaultRequestHeaders.Add("X-Agent-Run-Id", runId.ToString());
        Assert.Equal(HttpStatusCode.OK, (await planner.GetAsync($"/api/v1/logistics/stock?productId={productId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new { orderIds = new[] { orderId } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await planner.PostAsJsonAsync("/api/v1/logistics/sourcing", new { servicePointIds = new[] { pointId } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await planner.GetAsync("/api/v1/logistics/vehicles/available")).StatusCode);

        await using (var db = TestDb.Context(connection))
        {
            var runAfterHuman = await db.AgentRuns.AsNoTracking().SingleAsync(r => r.Id == runId);
            Assert.Equal("[]", runAfterHuman.ToolCallsJson);
        }

        // 2. Llamadas M2M con X-Agent-Run-Id (repetidas): 200 OK y registran trazas en AgentRun
        using var automation = CreateAutomationClient(api);
        automation.DefaultRequestHeaders.Add("X-Agent-Run-Id", runId.ToString());

        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await automation.GetAsync($"/api/v1/logistics/stock?productId={productId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await automation.PostAsJsonAsync("/api/v1/logistics/sourcing", new { orderIds = new[] { orderId } })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await automation.PostAsJsonAsync("/api/v1/logistics/sourcing", new { servicePointIds = new[] { pointId } })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await automation.GetAsync("/api/v1/logistics/vehicles/available")).StatusCode);
        }

        var afterSignature = await CaptureLogisticsStateSignatureAsync();
        Assert.Equal(beforeSignature, afterSignature);

        await using (var db = TestDb.Context(connection))
        {
            var runAfterAutomation = await db.AgentRuns.AsNoTracking().SingleAsync(r => r.Id == runId);
            using var doc = JsonDocument.Parse(runAfterAutomation.ToolCallsJson);
            var traces = doc.RootElement.EnumerateArray().ToList();
            Assert.Equal(8, traces.Count);
            Assert.Contains(traces, t => t.GetProperty("method").GetString() == "GET" && t.GetProperty("path").GetString() == "/api/v1/logistics/stock" && t.GetProperty("status").GetInt32() == 200);
            Assert.Contains(traces, t => t.GetProperty("method").GetString() == "POST" && t.GetProperty("path").GetString() == "/api/v1/logistics/sourcing" && t.GetProperty("status").GetInt32() == 200);
            Assert.Contains(traces, t => t.GetProperty("method").GetString() == "GET" && t.GetProperty("path").GetString() == "/api/v1/logistics/vehicles/available" && t.GetProperty("status").GetInt32() == 200);
        }
    }

    [Fact]
    public async Task Fixed_clock_mode_and_vehicle_matcher_empty_stops_behave_consistently()
    {
        var connection = postgres.NewDatabase();
        var overrides = CreateOverrides();
        overrides["Logistics:Clock:Mode"] = "Fixed";
        overrides["Logistics:Clock:FixedDate"] = "2026-10-20";

        await using var api = new ApiFactory(connection, demoEnabled: true, overrides: overrides);
        using var planner = api.ClientFor(TestTokens.Planner);

        await planner.PostAsync("/api/v1/demo/reset?fixture=demo-region", null);

        // Con ClockMode.Fixed = 2026-10-20, /vehicles/available sin date usa 2026-10-20
        var vehResp = await planner.GetAsync("/api/v1/logistics/vehicles/available");
        Assert.Equal(HttpStatusCode.OK, vehResp.StatusCode);
        var vehJson = await vehResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-10-20", vehJson.GetProperty("operationalDate").GetString());
        Assert.Equal("2026-10-20", vehJson.GetProperty("date").GetString());

        // Y consultar otra fecha en /vehicles/available?date=2026-10-25 no altera operationalDate
        var customDateResp = await planner.GetAsync("/api/v1/logistics/vehicles/available?date=2026-10-25");
        Assert.Equal(HttpStatusCode.OK, customDateResp.StatusCode);
        var customDateJson = await customDateResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2026-10-20", customDateJson.GetProperty("operationalDate").GetString());
        Assert.Equal("2026-10-25", customDateJson.GetProperty("date").GetString());

        // Verificar que MatchVehiclesAsync sin paradas no inventa distancia ni costo hacia la coordenada de fallback
        using var scope = api.Services.CreateScope();
        var matcher = scope.ServiceProvider.GetRequiredService<IstpetDev.Application.Logistics.IVehicleMatcher>();
        var matches = await matcher.MatchVehiclesAsync(
            Array.Empty<IstpetDev.Application.Logistics.RouteStopCandidate>(),
            new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));

        Assert.NotEmpty(matches);
        Assert.All(matches, m =>
        {
            Assert.Equal(0m, m.DistanceToFirstPickupKm);
            Assert.Equal(0m, m.EstimatedTotalCost);
        });
    }
}
