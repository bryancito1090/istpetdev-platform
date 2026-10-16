using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

// #162: la cadena de custodia Bodega → Conductor → Receptor se respeta por turnos, roles,
// estados y transacciones. Cada transición queda en CustodyEvent y la traza pública la refleja.
[Trait("Category", "Integration")]
public class CustodyChainHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private const string BodegaSubject = "warehouse-162";
    private const string Receiver2Subject = "receiver-162-b";

    private sealed record Seeded(
        Guid DeliveryId,
        Guid DeliveryLineId,
        Guid LotId,
        Guid PointLocationId,
        Guid VehicleLocationId,
        Guid WarehouseLocationId,
        Guid DriverAccountId,
        Guid BodegaAccountId,
        Guid ReceiverAccountId);

    private static async Task<Guid> EnsureAccountAsync(ApiFactory api, string subject, string role)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existing = await db.Accounts.FirstOrDefaultAsync(a => a.Issuer == TestTokens.Issuer && a.Subject == subject);
        if (existing is not null) return existing.Id;
        var account = new Account { Issuer = TestTokens.Issuer, Subject = subject, Kind = "human", DisplayName = subject, Role = role, IsActive = true };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    // Entrega Draft con línea, saldo de bodega reservado y parada de ruta lista para despacho.
    private static async Task<Seeded> SeedDraftAsync(ApiFactory api, string suffix, decimal quantity = 12m)
    {
        var bodegaId = await EnsureAccountAsync(api, BodegaSubject, Roles.Bodega);
        var receiverId = await EnsureAccountAsync(api, TestTokens.Receiver, Roles.Receptor);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var suffixCode = $"{suffix}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        var product = new Product { Sku = $"SKU-162-{suffixCode}", Name = "Producto 162", BaseUnit = "L", IsActive = true };
        var lot = new Lot { ProductId = product.Id, Code = $"LOTE-162-{suffixCode}" };
        var point = new IstpetDev.Domain.Entities.ServicePoint { Code = $"P-162-{suffixCode}", Name = "Punto 162", Criticality = "normal", IsActive = true };
        var warehouse = new Location { Code = $"B-162-{suffixCode}", Name = "Bodega 162", Kind = LocationKinds.Warehouse, IsActive = true };
        var pointLocation = new Location { Code = $"P-162-{suffixCode}", Name = "Punto 162", Kind = LocationKinds.ServicePoint, ServicePointId = point.Id, IsActive = true };
        var vehicle = new Vehicle { Code = $"V-162-{suffixCode}", DepotLocationId = warehouse.Id, MaxWeight = 1000, IsActive = true };
        var vehicleLocation = new Location { Code = $"V-162-{suffixCode}", Name = "Vehículo 162", Kind = LocationKinds.Vehicle, VehicleId = vehicle.Id, IsActive = true };
        var plan = new RoutePlan { Code = $"R-162-{suffixCode}", Revision = 1, VehicleId = vehicle.Id, Status = "Published", ProposedBy = "test", PlannedStartUtc = DateTime.UtcNow, AlgorithmVersion = "test" };
        var stop = new RouteStop { RoutePlanId = plan.Id, ServicePointId = point.Id, SequenceNumber = 1 };
        var delivery = new Delivery
        {
            Code = $"D-162-{suffixCode}",
            RouteStopId = stop.Id,
            OriginLocationId = warehouse.Id,
            ServicePointId = point.Id,
            Status = DeliveryStates.Draft,
            AssignedAccountId = driver.Id,
        };
        var line = new DeliveryLine { DeliveryId = delivery.Id, LotId = lot.Id, PlannedQuantity = quantity };
        var warehouseBalance = new StockBalance { LocationId = warehouse.Id, LotId = lot.Id, QuantityOnHand = quantity, QuantityReserved = quantity };
        var vehicleBalance = new StockBalance { LocationId = vehicleLocation.Id, LotId = lot.Id, QuantityOnHand = 0m };
        var reservation = new RouteStockReservation { RoutePlanId = plan.Id, DeliveryId = delivery.Id, DeliveryLineId = line.Id, LotId = lot.Id, StockBalanceId = warehouseBalance.Id, Quantity = quantity };

        db.AddRange(product, lot, point, warehouse, pointLocation, vehicle, vehicleLocation, plan, stop, delivery, line,
            warehouseBalance, vehicleBalance, reservation);
        await db.SaveChangesAsync();

        return new Seeded(delivery.Id, line.Id, lot.Id, pointLocation.Id, vehicleLocation.Id, warehouse.Id, driver.Id, bodegaId, receiverId);
    }

    // Bulto sellado o despachado en base para probar guardas de estado.
    private static async Task<Guid> SeedUnitAsync(ApiFactory api, Guid deliveryId, Guid deliveryLineId, Guid lotId, string status, decimal quantity)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unit = new HandlingUnit { Code = $"HU-{Guid.NewGuid().ToString("N")[..8]}", DeliveryId = deliveryId, LocationId = (await db.Deliveries.SingleAsync(d => d.Id == deliveryId)).OriginLocationId, Status = status };
        db.Add(unit);
        db.Add(new HandlingUnitLine { HandlingUnitId = unit.Id, DeliveryLineId = deliveryLineId, LotId = lotId, Quantity = quantity });
        await db.SaveChangesAsync();
        return unit.Id;
    }

    // Prepara, despacha, recoge y entrega con los servicios reales; devuelve el bulto y su token QR.
    private static async Task<(Guid UnitId, string Token)> PrepareDispatchPickupDeliverAsync(ApiFactory api, Seeded s, decimal quantity = 12m)
    {
        using var scope = api.Services.CreateScope();
        var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
        var op = BaseEntity.NewId();
        var prep = await field.CreateHandlingUnitAsync(s.DeliveryId,
            new CreateHandlingUnitRequest(op, [new CreateHandlingUnitLineRequest(s.DeliveryLineId, s.LotId, quantity)]), s.BodegaAccountId);
        await field.DispatchAsync(s.DeliveryId, new DispatchRequest(BaseEntity.NewId(), [prep.HandlingUnitId]), s.BodegaAccountId, $"disp-{Guid.NewGuid()}");
        await field.PickupAsync(s.DeliveryId, new CustodyTransitionRequest(BaseEntity.NewId()), s.DriverAccountId, $"pick-{Guid.NewGuid()}");
        await field.DeliverAsync(s.DeliveryId, new CustodyTransitionRequest(BaseEntity.NewId()), s.DriverAccountId, $"delv-{Guid.NewGuid()}");
        return (prep.HandlingUnitId, prep.Token!);
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object body, Guid? idempotencyKey = null, string? traceToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
        if (traceToken is not null) request.Headers.Add("X-Trace-Token", traceToken);
        return await client.SendAsync(request);
    }

    private static object ReceiptBody(Guid operationId, Guid deliveryLineId, Guid lotId, decimal accepted, decimal rejected = 0m, string? reason = null) => new
    {
        operationId,
        expectedDeliveryVersion = 0,
        occurredAtDevice = DateTime.UtcNow,
        lines = new[] { new { deliveryLineId, lotId, quantityAccepted = accepted, quantityRejected = rejected, unit = "L", rejectionReason = reason } },
    };

    private static readonly string[] FullJourneyStages = ["Preparada", "Despachada", "En ruta", "Entregada", "Recibida"];

    [Fact]
    public async Task Full_journey_respects_each_role_turn_and_moves_stock_once()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "FULL");
        using var bodega = api.ClientFor(BodegaSubject);
        using var driver = api.ClientFor(TestTokens.Driver);
        using var receiver = api.ClientFor(TestTokens.Receiver);

        // Bodega prepara la caja: no mueve stock todavía.
        var prepOp = BaseEntity.NewId();
        var prep = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/handling-units",
            new { operationId = prepOp, lines = new[] { new { deliveryLineId = s.DeliveryLineId, lotId = s.LotId, quantity = 12m } } });
        Assert.Equal(HttpStatusCode.Created, prep.StatusCode);
        using var prepJson = JsonDocument.Parse(await prep.Content.ReadAsStringAsync());
        var unitId = prepJson.RootElement.GetProperty("handlingUnitId").GetGuid();
        var unitToken = prepJson.RootElement.GetProperty("token").GetString()!;
        Assert.Equal("Sealed", prepJson.RootElement.GetProperty("status").GetString());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.WarehouseLocationId, s.LotId));
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));

        // Antes del despacho: ni el receptor ni el conductor pueden avanzar la custodia.
        var earlyReceive = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
            ReceiptBody(BaseEntity.NewId(), s.DeliveryLineId, s.LotId, 12m), BaseEntity.NewId(), unitToken);
        Assert.Equal(HttpStatusCode.Conflict, earlyReceive.StatusCode);
        Assert.Contains(FieldErrorCodes.NotDispatched, await earlyReceive.Content.ReadAsStringAsync());

        var earlyPickup = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.Conflict, earlyPickup.StatusCode);
        Assert.Contains(FieldErrorCodes.NotDispatched, await earlyPickup.Content.ReadAsStringAsync());

        // Bodega despacha: el stock se mueve una sola vez bodega → vehículo.
        var dispatch = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/dispatch",
            new { operationId = BaseEntity.NewId(), handlingUnitIds = new[] { unitId } });
        Assert.Equal(HttpStatusCode.OK, dispatch.StatusCode);
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.WarehouseLocationId, s.LotId));
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));

        // Antes de la entrega física el receptor sigue bloqueado.
        var beforeDelivery = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
            ReceiptBody(BaseEntity.NewId(), s.DeliveryLineId, s.LotId, 12m), BaseEntity.NewId(), unitToken);
        Assert.Equal(HttpStatusCode.Conflict, beforeDelivery.StatusCode);
        Assert.Contains(FieldErrorCodes.NotDelivered, await beforeDelivery.Content.ReadAsStringAsync());

        // Conductor: recoger y entregar. Sin movimientos de stock; solo custodia.
        var pickup = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.OK, pickup.StatusCode);
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));

        var deliver = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/deliver", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.OK, deliver.StatusCode);
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));

        var repeatPickup = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.Conflict, repeatPickup.StatusCode);
        Assert.Contains(FieldErrorCodes.InvalidStateTransition, await repeatPickup.Content.ReadAsStringAsync());

        // Receptor: único recibo final; mover lo aceptado al punto.
        var receiptOp = BaseEntity.NewId();
        var receiptBody = ReceiptBody(receiptOp, s.DeliveryLineId, s.LotId, 12m);
        var receipt = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt", receiptBody, receiptOp, unitToken);
        Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);
        var receiptText = await receipt.Content.ReadAsStringAsync();
        Assert.Contains("\"deliveryStatus\":\"Received\"", receiptText);
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.LotId));
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));

        // Replay de la misma operación: misma respuesta y sin efectos nuevos.
        var replay = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt", receiptBody, receiptOp, unitToken);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(receiptText, await replay.Content.ReadAsStringAsync());

        // Clave nueva sobre una entrega terminal: sin doble stock.
        var secondOp = BaseEntity.NewId();
        var second = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
            ReceiptBody(secondOp, s.DeliveryLineId, s.LotId, 12m), secondOp, unitToken);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains(FieldErrorCodes.AlreadyReceived, await second.Content.ReadAsStringAsync());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.LotId));

        // El endpoint anterior del conductor ya no confirma stock en destino.
        var driverReceipt = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/receipts",
            ReceiptBody(BaseEntity.NewId(), s.DeliveryLineId, s.LotId, 12m), BaseEntity.NewId());
        Assert.Equal(HttpStatusCode.Conflict, driverReceipt.StatusCode);
        Assert.Contains(FieldErrorCodes.ReceiptRequiresReceiver, await driverReceipt.Content.ReadAsStringAsync());

        // Traza pública: Despachada → En ruta → Entregada → Recibida.
        using var receiverTrace = api.CreateClient();
        var traceRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/public/trace/{unitId}");
        traceRequest.Headers.Add("X-Trace-Token", unitToken);
        var trace = await receiverTrace.SendAsync(traceRequest);
        Assert.Equal(HttpStatusCode.OK, trace.StatusCode);
        using var traceJson = JsonDocument.Parse(await trace.Content.ReadAsStringAsync());
        var stages = traceJson.RootElement.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("stage").GetString()).ToList();
        Assert.Equal(FullJourneyStages, stages);

        // La parada se completa por el recibo real.
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deliveryRow = await db.Deliveries.SingleAsync(d => d.Id == s.DeliveryId);
        var stopStatus = await db.RouteStops.Where(st => st.Id == deliveryRow.RouteStopId).Select(st => st.Status).SingleAsync();
        Assert.Equal("Completed", stopStatus);
        Assert.Single(await db.Receipts.Where(r => r.DeliveryId == s.DeliveryId).ToListAsync());
    }

    [Fact]
    public async Task Receivers_with_distinct_keys_cannot_duplicate_stock()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "RACE");
        var (unitId, unitToken) = await PrepareDispatchPickupDeliverAsync(api, s);
        await EnsureAccountAsync(api, Receiver2Subject, Roles.Receptor);
        using var receiverA = api.ClientFor(TestTokens.Receiver);
        using var receiverB = api.ClientFor(Receiver2Subject);

        var opA = BaseEntity.NewId();
        var opB = BaseEntity.NewId();
        var responses = await Task.WhenAll(
            PostJsonAsync(receiverA, $"/api/v1/receiver/handling-units/{unitId}/receipt", ReceiptBody(opA, s.DeliveryLineId, s.LotId, 12m), opA, unitToken),
            PostJsonAsync(receiverB, $"/api/v1/receiver/handling-units/{unitId}/receipt", ReceiptBody(opB, s.DeliveryLineId, s.LotId, 12m), opB, unitToken));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        var rejected = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        // El bloqueo de organización serializa: el segundo ve la entrega ya recibida sin doble stock.
        Assert.Contains(FieldErrorCodes.AlreadyReceived, await rejected.Content.ReadAsStringAsync());
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.LotId));
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await db.Receipts.Where(r => r.DeliveryId == s.DeliveryId).ToListAsync());
    }

    [Fact]
    public async Task Partial_receipt_keeps_rejected_in_the_vehicle_and_blocks_fully_accounted_retries()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "PART");
        var (unitId, unitToken) = await PrepareDispatchPickupDeliverAsync(api, s);
        using var receiver = api.ClientFor(TestTokens.Receiver);

        var op = BaseEntity.NewId();
        var first = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
            ReceiptBody(op, s.DeliveryLineId, s.LotId, 9m, 3m, "Daño"), op, unitToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Contains("\"deliveryStatus\":\"Partial\"", await first.Content.ReadAsStringAsync());
        Assert.Equal(9m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.LotId));
        Assert.Equal(3m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(HandlingUnitStates.Partial, (await db.HandlingUnits.SingleAsync(h => h.Id == unitId)).Status);
            var receiptId = await db.Receipts.Where(r => r.DeliveryId == s.DeliveryId).Select(r => r.Id).SingleAsync();
            var rejectedLine = await db.ReceiptLines.SingleAsync(l => l.ReceiptId == receiptId);
            Assert.Equal(3m, rejectedLine.QuantityRejected);
        }

        // La línea ya está conciliada por completo: no se repiten efectos con otra clave.
        var retryOp = BaseEntity.NewId();
        var retry = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
            ReceiptBody(retryOp, s.DeliveryLineId, s.LotId, 3m), retryOp, unitToken);
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Contains(FieldErrorCodes.AlreadyReceived, await retry.Content.ReadAsStringAsync());
        Assert.Equal(9m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.LotId));
    }

    [Fact]
    public async Task Legacy_delivered_with_an_existing_receipt_is_already_received()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "LEGACY");
        var unitId = await SeedUnitAsync(api, s.DeliveryId, s.DeliveryLineId, s.LotId, HandlingUnitStates.Delivered, 12m);

        // Estado legado anterior a #162: Delivered significaba recepción final registrada.
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var delivery = await db.Deliveries.SingleAsync(d => d.Id == s.DeliveryId);
            delivery.Status = DeliveryStates.Delivered;
            var receipt = new Receipt { DeliveryId = s.DeliveryId, ActorAccountId = s.DriverAccountId, OperationId = BaseEntity.NewId(), ReceivedAtServer = DateTime.UtcNow };
            db.Add(receipt);
            db.Add(new ReceiptLine { ReceiptId = receipt.Id, DeliveryLineId = s.DeliveryLineId, QuantityAccepted = 12m });
            await db.SaveChangesAsync();
        }

        using var receiver = api.ClientFor(TestTokens.Receiver);
        using var scope2 = api.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var token = "token-legacy-0123456789abcdef0123456789abcdef";
        db2.Add(new QrAccessToken { HandlingUnitId = unitId, TokenHash = FieldSeed.HashToken(token), ExpiresAtUtc = DateTime.UtcNow.AddDays(7) });
        await db2.SaveChangesAsync();

        var op = BaseEntity.NewId();
        var response = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
            ReceiptBody(op, s.DeliveryLineId, s.LotId, 12m), op, token);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(FieldErrorCodes.AlreadyReceived, await response.Content.ReadAsStringAsync());
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.LotId));
    }

    [Fact]
    public async Task Missing_or_foreign_assignment_forbids_driver_transitions()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "ASSIGN");
        await SeedUnitAsync(api, s.DeliveryId, s.DeliveryLineId, s.LotId, HandlingUnitStates.Dispatched, 12m);

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var delivery = await db.Deliveries.SingleAsync(d => d.Id == s.DeliveryId);
            delivery.Status = DeliveryStates.Dispatched;
            delivery.AssignedAccountId = null; // sin conductor asignado
            await db.SaveChangesAsync();
        }

        using var driver = api.ClientFor(TestTokens.Driver);
        var withoutAssignment = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.Forbidden, withoutAssignment.StatusCode);
        Assert.Contains(FieldErrorCodes.UnauthorizedAssignment, await withoutAssignment.Content.ReadAsStringAsync());

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var delivery = await db.Deliveries.SingleAsync(d => d.Id == s.DeliveryId);
            var otherDriver = await db.Accounts.SingleAsync(a => a.Subject == "driver-fixture-duplicate");
            delivery.AssignedAccountId = otherDriver.Id;
            await db.SaveChangesAsync();
        }

        var foreign = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.Contains(FieldErrorCodes.UnauthorizedAssignment, await foreign.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Role_and_unit_state_guards_block_invalid_transitions()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "GUARD");
        using var bodega = api.ClientFor(BodegaSubject);

        // Despacho sin bultos sobre una entrega Draft.
        var emptyDispatch = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/dispatch",
            new { operationId = BaseEntity.NewId(), handlingUnitIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.Conflict, emptyDispatch.StatusCode);
        Assert.Contains(FieldErrorCodes.NoHandlingUnits, await emptyDispatch.Content.ReadAsStringAsync());

        // Preparar requiere Draft.
        using (var preScope = api.Services.CreateScope())
        {
            var preDb = preScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await preDb.Deliveries.SingleAsync(d => d.Id == s.DeliveryId)).Status = DeliveryStates.Dispatched;
            await preDb.SaveChangesAsync();
        }
        var latePrep = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/handling-units",
            new { operationId = BaseEntity.NewId(), lines = new[] { new { deliveryLineId = s.DeliveryLineId, lotId = s.LotId, quantity = 1m } } });
        Assert.Equal(HttpStatusCode.Conflict, latePrep.StatusCode);
        Assert.Contains(FieldErrorCodes.InvalidStateTransition, await latePrep.Content.ReadAsStringAsync());

        // Recogida sin bultos preparados.
        using var driver = api.ClientFor(TestTokens.Driver);
        var zeroUnits = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.Conflict, zeroUnits.StatusCode);
        Assert.Contains(FieldErrorCodes.NoHandlingUnits, await zeroUnits.Content.ReadAsStringAsync());

        // Entrega antes de recoger.
        await SeedUnitAsync(api, s.DeliveryId, s.DeliveryLineId, s.LotId, HandlingUnitStates.Dispatched, 12m);
        var earlyDeliver = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/deliver", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.Conflict, earlyDeliver.StatusCode);
        Assert.Contains(FieldErrorCodes.InvalidStateTransition, await earlyDeliver.Content.ReadAsStringAsync());

        // Bulto fuera de estado: dos bultos, uno sellado.
        await SeedUnitAsync(api, s.DeliveryId, s.DeliveryLineId, s.LotId, HandlingUnitStates.Sealed, 1m);
        var mismatch = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        Assert.Contains(FieldErrorCodes.HandlingUnitStateConflict, await mismatch.Content.ReadAsStringAsync());

        // Otros roles no operan la custodia del conductor.
        using var receiver = api.ClientFor(TestTokens.Receiver);
        var receiverPickup = await PostJsonAsync(receiver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() });
        Assert.Equal(HttpStatusCode.Forbidden, receiverPickup.StatusCode);
    }

    private sealed record MultiSeeded(
        Guid DeliveryId,
        Guid PointLocationId,
        Guid VehicleLocationId,
        Guid WarehouseLocationId,
        Guid DriverAccountId,
        Guid BodegaAccountId,
        Guid ReceiverAccountId,
        IReadOnlyList<(Guid LineId, Guid LotId)> Lines);

    // Entrega Draft con varias líneas; sameLot comparte un único lote entre las líneas.
    private static async Task<MultiSeeded> SeedMultiLineAsync(ApiFactory api, string suffix, bool sameLot, decimal quantityPerLine = 10m, int lineCount = 2)
    {
        var bodegaId = await EnsureAccountAsync(api, BodegaSubject, Roles.Bodega);
        var receiverId = await EnsureAccountAsync(api, TestTokens.Receiver, Roles.Receptor);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var code = $"{suffix}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        var product = new Product { Sku = $"SKU-162-{code}", Name = "Producto 162", BaseUnit = "L", IsActive = true };
        var point = new IstpetDev.Domain.Entities.ServicePoint { Code = $"P-162-{code}", Name = "Punto 162", Criticality = "normal", IsActive = true };
        var warehouse = new Location { Code = $"B-162-{code}", Name = "Bodega 162", Kind = LocationKinds.Warehouse, IsActive = true };
        var pointLocation = new Location { Code = $"P-162-{code}", Name = "Punto 162", Kind = LocationKinds.ServicePoint, ServicePointId = point.Id, IsActive = true };
        var vehicle = new Vehicle { Code = $"V-162-{code}", DepotLocationId = warehouse.Id, MaxWeight = 1000, IsActive = true };
        var vehicleLocation = new Location { Code = $"V-162-{code}", Name = "Vehículo 162", Kind = LocationKinds.Vehicle, VehicleId = vehicle.Id, IsActive = true };
        var plan = new RoutePlan { Code = $"R-162-{code}", Revision = 1, VehicleId = vehicle.Id, Status = "Published", ProposedBy = "test", PlannedStartUtc = DateTime.UtcNow, AlgorithmVersion = "test" };
        var stop = new RouteStop { RoutePlanId = plan.Id, ServicePointId = point.Id, SequenceNumber = 1 };
        var delivery = new Delivery
        {
            Code = $"D-162-{code}",
            RouteStopId = stop.Id,
            OriginLocationId = warehouse.Id,
            ServicePointId = point.Id,
            Status = DeliveryStates.Draft,
            AssignedAccountId = driver.Id,
        };

        db.AddRange(product, point, warehouse, pointLocation, vehicle, vehicleLocation, plan, stop, delivery);

        var lotsCreated = new List<Lot>();
        var lines = new List<(Guid LineId, Guid LotId)>();
        for (var i = 0; i < lineCount; i++)
        {
            var lot = sameLot ? lotsCreated.FirstOrDefault() : null;
            if (lot is null)
            {
                lot = new Lot { ProductId = product.Id, Code = $"LOTE-162-{code}-{i}" };
                db.Add(lot);
                lotsCreated.Add(lot);
            }
            var line = new DeliveryLine { DeliveryId = delivery.Id, LotId = lot.Id, PlannedQuantity = quantityPerLine };
            db.Add(line);
            lines.Add((line.Id, lot.Id));
        }

        foreach (var lot in lotsCreated)
        {
            var total = quantityPerLine * lines.Count(l => l.LotId == lot.Id);
            db.Add(new StockBalance { LocationId = warehouse.Id, LotId = lot.Id, QuantityOnHand = total, QuantityReserved = total });
        }

        await db.SaveChangesAsync();
        return new MultiSeeded(delivery.Id, pointLocation.Id, vehicleLocation.Id, warehouse.Id, driver.Id, bodegaId, receiverId, lines);
    }

    private static async Task<(int Receipts, int ReceiptLines, int Movements, int CustodyEvents)> EffectCountsAsync(ApiFactory api, Guid deliveryId)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var receiptIds = db.Receipts.Where(r => r.DeliveryId == deliveryId).Select(r => r.Id);
        var lineIds = await db.DeliveryLines.Where(dl => dl.DeliveryId == deliveryId).Select(dl => dl.Id).ToListAsync();
        return (
            await receiptIds.CountAsync(),
            await db.ReceiptLines.CountAsync(l => receiptIds.Contains(l.ReceiptId)),
            await db.InventoryMovements.CountAsync(m => m.DeliveryLineId.HasValue && lineIds.Contains(m.DeliveryLineId.Value)),
            await db.CustodyEvents.CountAsync(e => e.DeliveryId == deliveryId));
    }

    private static async Task<(Guid Hu1, Guid Hu2, string Token1, string Token2)> PrepareTwoBoxesAsync(ApiFactory api, MultiSeeded s)
    {
        using var scope = api.Services.CreateScope();
        var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
        var hu1 = await field.CreateHandlingUnitAsync(s.DeliveryId, new CreateHandlingUnitRequest(BaseEntity.NewId(),
            [new CreateHandlingUnitLineRequest(s.Lines[0].LineId, s.Lines[0].LotId, 10m)]), s.BodegaAccountId);
        var hu2 = await field.CreateHandlingUnitAsync(s.DeliveryId, new CreateHandlingUnitRequest(BaseEntity.NewId(),
            [new CreateHandlingUnitLineRequest(s.Lines[1].LineId, s.Lines[1].LotId, 10m)]), s.BodegaAccountId);
        return (hu1.HandlingUnitId, hu2.HandlingUnitId, hu1.Token!, hu2.Token!);
    }

    private static async Task AdvanceToDeliveredAsync(ApiFactory api, MultiSeeded s, Guid hu1, Guid hu2)
    {
        using var scope = api.Services.CreateScope();
        var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
        await field.DispatchAsync(s.DeliveryId, new DispatchRequest(BaseEntity.NewId(), [hu1, hu2]), s.BodegaAccountId, $"disp-{Guid.NewGuid()}");
        await field.PickupAsync(s.DeliveryId, new CustodyTransitionRequest(BaseEntity.NewId()), s.DriverAccountId, $"pick-{Guid.NewGuid()}");
        await field.DeliverAsync(s.DeliveryId, new CustodyTransitionRequest(BaseEntity.NewId()), s.DriverAccountId, $"delv-{Guid.NewGuid()}");
    }

    [Fact]
    public async Task Dispatch_requires_all_boxes_and_full_planned_coverage_without_effects()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "DCOV");
        using var bodega = api.ClientFor(BodegaSubject);

        Guid huHalf;
        Guid huOther;
        using (var scope = api.Services.CreateScope())
        {
            var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
            huHalf = (await field.CreateHandlingUnitAsync(s.DeliveryId, new CreateHandlingUnitRequest(BaseEntity.NewId(),
                [new CreateHandlingUnitLineRequest(s.DeliveryLineId, s.LotId, 6m)]), s.BodegaAccountId)).HandlingUnitId;
            huOther = (await field.CreateHandlingUnitAsync(s.DeliveryId, new CreateHandlingUnitRequest(BaseEntity.NewId(),
                [new CreateHandlingUnitLineRequest(s.DeliveryLineId, s.LotId, 6m)]), s.BodegaAccountId)).HandlingUnitId;
        }

        var before = await EffectCountsAsync(api, s.DeliveryId);

        // IDs extra ajenos al conjunto de la entrega: sin efectos.
        var extra = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/dispatch",
            new { operationId = BaseEntity.NewId(), handlingUnitIds = new[] { huHalf, huOther, Guid.NewGuid() } });
        Assert.Equal(HttpStatusCode.Conflict, extra.StatusCode);
        Assert.Contains(FieldErrorCodes.IncompleteHandlingUnits, await extra.Content.ReadAsStringAsync());
        Assert.Equal(before, await EffectCountsAsync(api, s.DeliveryId));
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.WarehouseLocationId, s.LotId));

        // Subconjunto: falta uno de los dos bultos de la entrega.
        var subset = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/dispatch",
            new { operationId = BaseEntity.NewId(), handlingUnitIds = new[] { huHalf } });
        Assert.Equal(HttpStatusCode.Conflict, subset.StatusCode);
        Assert.Contains(FieldErrorCodes.IncompleteHandlingUnits, await subset.Content.ReadAsStringAsync());
        Assert.Equal(before, await EffectCountsAsync(api, s.DeliveryId));

        // Con ambos bultos cubre exactamente la línea: un solo movimiento por línea y lote.
        var ok = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/dispatch",
            new { operationId = BaseEntity.NewId(), handlingUnitIds = new[] { huHalf, huOther } });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.WarehouseLocationId, s.LotId));
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));

        using var scope2 = api.Services.CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.InventoryMovements.CountAsync(m => m.Kind == "Dispatch"));
    }

    [Fact]
    public async Task Dispatch_requires_full_planned_coverage_per_line()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "DPLAN");
        using var bodega = api.ClientFor(BodegaSubject);

        Guid huPartial;
        using (var scope = api.Services.CreateScope())
        {
            var field = scope.ServiceProvider.GetRequiredService<IFieldService>();
            huPartial = (await field.CreateHandlingUnitAsync(s.DeliveryId, new CreateHandlingUnitRequest(BaseEntity.NewId(),
                [new CreateHandlingUnitLineRequest(s.DeliveryLineId, s.LotId, 7m)]), s.BodegaAccountId)).HandlingUnitId;
        }

        var before = await EffectCountsAsync(api, s.DeliveryId);
        var incomplete = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/dispatch",
            new { operationId = BaseEntity.NewId(), handlingUnitIds = new[] { huPartial } });
        Assert.Equal(HttpStatusCode.Conflict, incomplete.StatusCode);
        Assert.Contains(FieldErrorCodes.IncompleteHandlingUnits, await incomplete.Content.ReadAsStringAsync());
        Assert.Equal(before, await EffectCountsAsync(api, s.DeliveryId));
        Assert.Equal(12m, await FieldSeed.OnHandAsync(api, s.WarehouseLocationId, s.LotId));
    }

    [Fact]
    public async Task Dispatch_of_two_lines_sharing_a_lot_creates_one_vehicle_balance()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedMultiLineAsync(api, "SAME", sameLot: true, quantityPerLine: 10m, lineCount: 2);
        var (hu1, hu2, _, _) = await PrepareTwoBoxesAsync(api, s);
        using var bodega = api.ClientFor(BodegaSubject);

        var dispatch = await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/dispatch",
            new { operationId = BaseEntity.NewId(), handlingUnitIds = new[] { hu1, hu2 } });
        Assert.Equal(HttpStatusCode.OK, dispatch.StatusCode);

        var lotId = s.Lines[0].LotId;
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var vehicleBalances = await db.StockBalances.Where(b => b.LocationId == s.VehicleLocationId && b.LotId == lotId).ToListAsync();
        Assert.Single(vehicleBalances);
        Assert.Equal(20m, vehicleBalances[0].QuantityOnHand);
        Assert.Equal(2, await db.InventoryMovements.CountAsync(m => m.Kind == "Dispatch")); // uno por línea y lote
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.WarehouseLocationId, lotId));
    }

    [Fact]
    public async Task Multi_box_delivery_is_received_once_with_full_coverage()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedMultiLineAsync(api, "MULTI", sameLot: true, quantityPerLine: 10m, lineCount: 2);
        var (hu1, hu2, token1, _) = await PrepareTwoBoxesAsync(api, s);
        await AdvanceToDeliveredAsync(api, s, hu1, hu2);

        // El receptor escanea un bulto y ve TODOS los bultos y TODAS las líneas de la entrega.
        using var receiver = api.ClientFor(TestTokens.Receiver);
        var get = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/receiver/handling-units/{hu1}");
        get.Headers.Add("X-Trace-Token", token1);
        var getResponse = await receiver.SendAsync(get);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, json.RootElement.GetProperty("boxes").GetArrayLength());
        var linesJson = json.RootElement.GetProperty("lines").EnumerateArray().ToList();
        Assert.Equal(2, linesJson.Count);
        Assert.All(linesJson, l => Assert.Equal(10m, l.GetProperty("quantity").GetDecimal()));

        var op = BaseEntity.NewId();
        var body = new
        {
            operationId = op,
            expectedDeliveryVersion = 0,
            occurredAtDevice = DateTime.UtcNow,
            lines = s.Lines.Select(l => new { deliveryLineId = l.LineId, lotId = l.LotId, quantityAccepted = 10m, quantityRejected = 0m, unit = "L" }).ToArray()
        };
        var receipt = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{hu1}/receipt", body, op, token1);
        Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);

        Assert.Equal(20m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.Lines[0].LotId));
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.Lines[0].LotId));
        var counts = await EffectCountsAsync(api, s.DeliveryId);
        Assert.Equal(1, counts.Receipts);
        Assert.Equal(2, counts.ReceiptLines);
        // Dos bultos × (despacho, recogida, entrega, recibo) = 8 eventos de custodia.
        Assert.Equal(8, counts.CustodyEvents);
    }

    [Fact]
    public async Task Receipt_requires_exact_full_coverage_without_effects()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedMultiLineAsync(api, "COV", sameLot: true, quantityPerLine: 10m, lineCount: 2);
        var (hu1, hu2, token1, _) = await PrepareTwoBoxesAsync(api, s);
        await AdvanceToDeliveredAsync(api, s, hu1, hu2);
        using var receiver = api.ClientFor(TestTokens.Receiver);

        var before = await EffectCountsAsync(api, s.DeliveryId);
        var url = $"/api/v1/receiver/handling-units/{hu1}/receipt";

        async Task<HttpResponseMessage> AttemptAsync(object[] lines)
        {
            var op = BaseEntity.NewId();
            var body = new { operationId = op, expectedDeliveryVersion = 0, occurredAtDevice = DateTime.UtcNow, lines };
            return await PostJsonAsync(receiver, url, body, op, token1);
        }

        object Line(int index, decimal accepted, decimal rejected = 0m, Guid? lotOverride = null, Guid? lineOverride = null) => new
        {
            deliveryLineId = lineOverride ?? s.Lines[index].LineId,
            lotId = lotOverride ?? s.Lines[index].LotId,
            quantityAccepted = accepted,
            quantityRejected = rejected,
            unit = "L"
        };

        var omission = await AttemptAsync([Line(0, 10m)]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, omission.StatusCode);
        Assert.Contains(FieldErrorCodes.ReceiptCoverageMismatch, await omission.Content.ReadAsStringAsync());

        var duplicate = await AttemptAsync([Line(0, 10m), Line(0, 10m)]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicate.StatusCode);
        Assert.Contains(FieldErrorCodes.ReceiptCoverageMismatch, await duplicate.Content.ReadAsStringAsync());

        var foreign = await AttemptAsync([Line(0, 10m), Line(1, 10m, lineOverride: Guid.NewGuid())]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, foreign.StatusCode);
        Assert.Contains(FieldErrorCodes.ReceiptCoverageMismatch, await foreign.Content.ReadAsStringAsync());

        var wrongLot = await AttemptAsync([Line(0, 10m, lotOverride: Guid.NewGuid()), Line(1, 10m)]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongLot.StatusCode);
        Assert.Contains(FieldErrorCodes.ReceiptCoverageMismatch, await wrongLot.Content.ReadAsStringAsync());

        var negative = await AttemptAsync([Line(0, -1m), Line(1, 10m)]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, negative.StatusCode);
        Assert.Contains(FieldErrorCodes.QuantityExceeded, await negative.Content.ReadAsStringAsync());

        var underReport = await AttemptAsync([Line(0, 9m), Line(1, 10m)]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, underReport.StatusCode);
        Assert.Contains(FieldErrorCodes.QuantityExceeded, await underReport.Content.ReadAsStringAsync());

        var overReport = await AttemptAsync([Line(0, 11m), Line(1, 10m)]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, overReport.StatusCode);
        Assert.Contains(FieldErrorCodes.QuantityExceeded, await overReport.Content.ReadAsStringAsync());

        Assert.Equal(before, await EffectCountsAsync(api, s.DeliveryId));
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.Lines[0].LotId));

        // Cobertura exacta completa: un solo recibo.
        var ok = await AttemptAsync([Line(0, 10m), Line(1, 10m)]);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var counts = await EffectCountsAsync(api, s.DeliveryId);
        Assert.Equal(1, counts.Receipts);
        Assert.Equal(2, counts.ReceiptLines);
    }

    [Fact]
    public async Task Receipt_rejects_insufficient_vehicle_stock_without_effects()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "VSTOCK");
        var (unitId, unitToken) = await PrepareDispatchPickupDeliverAsync(api, s);

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var vehicleBalance = await db.StockBalances.SingleAsync(b => b.LocationId == s.VehicleLocationId && b.LotId == s.LotId);
            vehicleBalance.QuantityOnHand = 5m; // menos que lo aceptado (12)
            await db.SaveChangesAsync();
        }

        using var receiver = api.ClientFor(TestTokens.Receiver);
        var before = await EffectCountsAsync(api, s.DeliveryId);
        var op = BaseEntity.NewId();
        var response = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
            ReceiptBody(op, s.DeliveryLineId, s.LotId, 12m), op, unitToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(FieldErrorCodes.InsufficientVehicleStock, await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await EffectCountsAsync(api, s.DeliveryId));
        Assert.Equal(0m, await FieldSeed.OnHandAsync(api, s.PointLocationId, s.LotId));
        Assert.Equal(5m, await FieldSeed.OnHandAsync(api, s.VehicleLocationId, s.LotId));
    }
}
