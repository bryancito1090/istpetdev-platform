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

// #162 (revisión): idempotencia por operación, acción distinta y matriz de transiciones inválidas
// con cero efectos en recibos, saldos, ledger y eventos.
[Trait("Category", "Integration")]
public class CustodyGuardsHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private const string BodegaSubject = "warehouse-162-guards";
    private sealed record Seeded(
        Guid DeliveryId,
        Guid DeliveryLineId,
        Guid LotId,
        Guid PointLocationId,
        Guid VehicleLocationId,
        Guid WarehouseLocationId,
        Guid DriverAccountId,
        Guid BodegaAccountId);

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

    private static async Task<Seeded> SeedDraftAsync(ApiFactory api, string suffix, decimal quantity = 12m)
    {
        var bodegaId = await EnsureAccountAsync(api, BodegaSubject, Roles.Bodega);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var driver = await db.Accounts.SingleAsync(a => a.Subject == TestTokens.Driver);
        var code = $"{suffix}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        var product = new Product { Sku = $"SKU-162-{code}", Name = "Producto 162", BaseUnit = "L", IsActive = true };
        var lot = new Lot { ProductId = product.Id, Code = $"LOTE-162-{code}" };
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
        var line = new DeliveryLine { DeliveryId = delivery.Id, LotId = lot.Id, PlannedQuantity = quantity };
        var depotBalance = new StockBalance { LocationId = warehouse.Id, LotId = lot.Id, QuantityOnHand = quantity, QuantityReserved = quantity };
        var reservation = new RouteStockReservation { RoutePlanId = plan.Id, DeliveryId = delivery.Id, DeliveryLineId = line.Id, LotId = lot.Id, StockBalanceId = depotBalance.Id, Quantity = quantity };

        db.AddRange(product, lot, point, warehouse, pointLocation, vehicle, vehicleLocation, plan, stop, delivery, line, depotBalance, reservation);
        await db.SaveChangesAsync();
        return new Seeded(delivery.Id, line.Id, lot.Id, pointLocation.Id, vehicleLocation.Id, warehouse.Id, driver.Id, bodegaId);
    }

    private static async Task<(Guid UnitId, string Token)> SeedUnitAndStateAsync(
        ApiFactory api, Seeded s, string deliveryStatus, string unitStatus, bool withReceipt)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var delivery = await db.Deliveries.SingleAsync(d => d.Id == s.DeliveryId);
        delivery.Status = deliveryStatus;
        if (deliveryStatus is DeliveryStates.Dispatched or DeliveryStates.InTransit or DeliveryStates.Delivered or DeliveryStates.Received or DeliveryStates.Partial)
        {
            delivery.DispatchedAtUtc ??= DateTime.UtcNow;
        }

        var unit = new HandlingUnit { Code = $"HU-{Guid.NewGuid().ToString("N")[..8]}", DeliveryId = s.DeliveryId, LocationId = s.VehicleLocationId, Status = unitStatus };
        var token = $"token-{Guid.NewGuid():N}-0123456789abcdef";
        db.Add(unit);
        db.Add(new HandlingUnitLine { HandlingUnitId = unit.Id, DeliveryLineId = s.DeliveryLineId, LotId = s.LotId, Quantity = 12m });
        db.Add(new QrAccessToken { HandlingUnitId = unit.Id, TokenHash = FieldSeed.HashToken(token), ExpiresAtUtc = DateTime.UtcNow.AddDays(7) });

        if (deliveryStatus is DeliveryStates.Dispatched or DeliveryStates.InTransit or DeliveryStates.Delivered or DeliveryStates.Received or DeliveryStates.Partial)
        {
            var line = await db.DeliveryLines.SingleAsync(l => l.Id == s.DeliveryLineId);
            line.DispatchedQuantity = 12m;
            if (withReceipt)
            {
                line.AcceptedQuantity = 12m;
            }

            var vehicleBalance = await db.StockBalances.FirstOrDefaultAsync(b => b.LocationId == s.VehicleLocationId && b.LotId == s.LotId);
            if (vehicleBalance is null)
            {
                db.Add(new StockBalance { LocationId = s.VehicleLocationId, LotId = s.LotId, QuantityOnHand = 12m });
            }
        }

        if (withReceipt)
        {
            var receipt = new Receipt { DeliveryId = s.DeliveryId, ActorAccountId = s.DriverAccountId, OperationId = BaseEntity.NewId(), ReceivedAtServer = DateTime.UtcNow };
            db.Add(receipt);
            db.Add(new ReceiptLine { ReceiptId = receipt.Id, DeliveryLineId = s.DeliveryLineId, QuantityAccepted = 12m });
        }

        await db.SaveChangesAsync();
        return (unit.Id, token);
    }

    private static async Task<(int Receipts, int ReceiptLines, int Movements, int CustodyEvents, decimal Point, decimal Vehicle)> SnapshotAsync(ApiFactory api, Seeded s)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var receiptIds = db.Receipts.Where(r => r.DeliveryId == s.DeliveryId).Select(r => r.Id);
        var lineIds = await db.DeliveryLines.Where(dl => dl.DeliveryId == s.DeliveryId).Select(dl => dl.Id).ToListAsync();
        var point = await db.StockBalances.Where(b => b.LocationId == s.PointLocationId && b.LotId == s.LotId).Select(b => b.QuantityOnHand).SingleOrDefaultAsync();
        var vehicle = await db.StockBalances.Where(b => b.LocationId == s.VehicleLocationId && b.LotId == s.LotId).Select(b => b.QuantityOnHand).SingleOrDefaultAsync();
        return (
            await receiptIds.CountAsync(),
            await db.ReceiptLines.CountAsync(l => receiptIds.Contains(l.ReceiptId)),
            await db.InventoryMovements.CountAsync(m => m.DeliveryLineId.HasValue && lineIds.Contains(m.DeliveryLineId.Value)),
            await db.CustodyEvents.CountAsync(e => e.DeliveryId == s.DeliveryId),
            point,
            vehicle);
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object body, Guid? idempotencyKey = null, string? traceToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
        if (traceToken is not null) request.Headers.Add("X-Trace-Token", traceToken);
        return await client.SendAsync(request);
    }

    private static object ReceiptBody(Guid operationId, Seeded s, decimal accepted = 12m, decimal rejected = 0m) => new
    {
        operationId,
        expectedDeliveryVersion = 0,
        occurredAtDevice = DateTime.UtcNow,
        lines = new[] { new { deliveryLineId = s.DeliveryLineId, lotId = s.LotId, quantityAccepted = accepted, quantityRejected = rejected, unit = "L" } },
    };

    [Fact]
    public async Task Reusing_an_operation_uuid_in_another_action_delivery_or_body_is_rejected_without_effects()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var first = await SeedDraftAsync(api, "IDEM-A");
        var second = await SeedDraftAsync(api, "IDEM-B");
        await SeedUnitAndStateAsync(api, first, DeliveryStates.Dispatched, HandlingUnitStates.Dispatched, withReceipt: false);
        await SeedUnitAndStateAsync(api, second, DeliveryStates.Dispatched, HandlingUnitStates.Dispatched, withReceipt: false);
        using var driver = api.ClientFor(TestTokens.Driver);

        var operationId = BaseEntity.NewId();
        var pickupBody = new { operationId, occurredAtDevice = DateTime.UtcNow };
        var pickup = await PostJsonAsync(driver, $"/api/v1/deliveries/{first.DeliveryId}/pickup", pickupBody, operationId);
        Assert.Equal(HttpStatusCode.OK, pickup.StatusCode);

        // La misma UUID en otra acción (deliver) se rechaza con 409 explícito, sin 23505/500.
        var deliverSameOp = await PostJsonAsync(driver, $"/api/v1/deliveries/{first.DeliveryId}/deliver", new { operationId, occurredAtDevice = DateTime.UtcNow }, operationId);
        Assert.Equal(HttpStatusCode.Conflict, deliverSameOp.StatusCode);
        Assert.Contains(FieldErrorCodes.IdempotencyConflict, await deliverSameOp.Content.ReadAsStringAsync());

        // La misma UUID y acción en otra entrega se rechaza.
        var pickupOtherDelivery = await PostJsonAsync(driver, $"/api/v1/deliveries/{second.DeliveryId}/pickup", pickupBody, operationId);
        Assert.Equal(HttpStatusCode.Conflict, pickupOtherDelivery.StatusCode);
        Assert.Contains(FieldErrorCodes.IdempotencyConflict, await pickupOtherDelivery.Content.ReadAsStringAsync());

        // La misma UUID con cuerpo distinto se rechaza.
        var changedBody = new { operationId, occurredAtDevice = DateTime.UtcNow.AddMinutes(5) };
        var pickupChangedBody = await PostJsonAsync(driver, $"/api/v1/deliveries/{first.DeliveryId}/pickup", changedBody, operationId);
        Assert.Equal(HttpStatusCode.Conflict, pickupChangedBody.StatusCode);
        Assert.Contains(FieldErrorCodes.IdempotencyConflict, await pickupChangedBody.Content.ReadAsStringAsync());

        // Replay exacto de la operación original: 200 con la respuesta previa.
        var replay = await PostJsonAsync(driver, $"/api/v1/deliveries/{first.DeliveryId}/pickup", pickupBody, operationId);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Contains("\"status\":\"InTransit\"", await replay.Content.ReadAsStringAsync());

        var beforeFirst = await SnapshotAsync(api, first);
        await PostJsonAsync(driver, $"/api/v1/deliveries/{first.DeliveryId}/deliver", new { operationId, occurredAtDevice = DateTime.UtcNow }, operationId);
        Assert.Equal(beforeFirst, await SnapshotAsync(api, first));
        Assert.Equal(1, beforeFirst.CustodyEvents); // solo el evento de recogida
    }

    [Fact]
    public async Task Empty_operation_guid_is_rejected_with_422()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "EMPTY");
        var (unitId, unitToken) = await SeedUnitAndStateAsync(api, s, DeliveryStates.Dispatched, HandlingUnitStates.Dispatched, withReceipt: false);
        using var driver = api.ClientFor(TestTokens.Driver);
        using var receiver = api.ClientFor(TestTokens.Receiver);

        var pickup = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = Guid.Empty });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, pickup.StatusCode);
        Assert.Contains(FieldErrorCodes.InvalidOperationId, await pickup.Content.ReadAsStringAsync());

        var receipt = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
            ReceiptBody(Guid.Empty, s), Guid.Empty, unitToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, receipt.StatusCode);
        Assert.Contains(FieldErrorCodes.InvalidOperationId, await receipt.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Exact_replay_after_advancing_the_state_returns_stored_responses_without_new_effects()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "REPLAY");
        var (unitId, unitToken) = await SeedUnitAndStateAsync(api, s, DeliveryStates.Dispatched, HandlingUnitStates.Dispatched, withReceipt: false);
        using var driver = api.ClientFor(TestTokens.Driver);
        using var receiver = api.ClientFor(TestTokens.Receiver);

        var pickupOp = BaseEntity.NewId();
        var pickupBody = new { operationId = pickupOp, occurredAtDevice = DateTime.UtcNow };
        var pickup = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", pickupBody, pickupOp);
        Assert.Equal(HttpStatusCode.OK, pickup.StatusCode);

        var deliverOp = BaseEntity.NewId();
        var deliver = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/deliver", new { operationId = deliverOp, occurredAtDevice = DateTime.UtcNow }, deliverOp);
        Assert.Equal(HttpStatusCode.OK, deliver.StatusCode);

        // Versión real que el receptor capturó con el bulto ya entregado, antes del primer recibo.
        long versionBeforeReceipt;
        using (var versionScope = api.Services.CreateScope())
        {
            var versionDb = versionScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            versionBeforeReceipt = (await versionDb.Deliveries.SingleAsync(d => d.Id == s.DeliveryId)).Version;
        }

        var receiptOp = BaseEntity.NewId();
        var receiptBody = ReceiptBody(receiptOp, s);
        var receipt = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt", receiptBody, receiptOp, unitToken);
        Assert.Equal(HttpStatusCode.OK, receipt.StatusCode);
        var receiptText = await receipt.Content.ReadAsStringAsync();

        var afterAdvance = await SnapshotAsync(api, s);

        // Replay del pickup después de avanzar: responde lo almacenado y no agrega eventos.
        var pickupReplay = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", pickupBody, pickupOp);
        Assert.Equal(HttpStatusCode.OK, pickupReplay.StatusCode);
        Assert.Contains("\"status\":\"InTransit\"", await pickupReplay.Content.ReadAsStringAsync());

        // Replay del recibo con la entrega ya terminal: misma respuesta previa.
        var receiptReplay = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt", receiptBody, receiptOp, unitToken);
        Assert.Equal(HttpStatusCode.OK, receiptReplay.StatusCode);
        Assert.Equal(receiptText, await receiptReplay.Content.ReadAsStringAsync());

        // Clave nueva con la versión real capturada ANTES del primer recibo: ALREADY_RECEIVED,
        // no ASSIGNMENT_CHANGED (el guard terminal precede a la versión).
        var newOp = BaseEntity.NewId();
        var staleVersionBody = new
        {
            operationId = newOp,
            expectedDeliveryVersion = versionBeforeReceipt,
            occurredAtDevice = DateTime.UtcNow,
            lines = new[] { new { deliveryLineId = s.DeliveryLineId, lotId = s.LotId, quantityAccepted = 12m, quantityRejected = 0m, unit = "L" } },
        };
        var already = await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt", staleVersionBody, newOp, unitToken);
        Assert.Equal(HttpStatusCode.Conflict, already.StatusCode);
        var alreadyText = await already.Content.ReadAsStringAsync();
        Assert.Contains(FieldErrorCodes.AlreadyReceived, alreadyText);
        Assert.DoesNotContain(FieldErrorCodes.AssignmentChanged, alreadyText);

        Assert.Equal(afterAdvance, await SnapshotAsync(api, s));
        Assert.Equal(3, afterAdvance.CustodyEvents); // recogida + entrega + recibo
        Assert.Equal(1, afterAdvance.Receipts);
    }

    [Fact]
    public async Task Legacy_idempotency_hash_replays_without_new_effects()
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, "LEGACY-IDEM");
        using var driver = api.ClientFor(TestTokens.Driver);

        // Registro de la primera entrega: hash = sha256(claveCliente + operationId)
        var operationId = BaseEntity.NewId();
        var key = operationId.ToString();
        var storedResponse = new CustodyTransitionResponse(s.DeliveryId, DeliveryStates.InTransit, [], DateTime.UtcNow, Guid.NewGuid());
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                OrganizationId = DemoOrganization.Id,
                AccountId = s.DriverAccountId,
                OperationId = operationId,
                ActionCode = "Pickup",
                RequestHash = FieldSeed.HashToken(key + operationId.ToString()),
                ResponseStatus = 200,
                ResponseJson = JsonSerializer.Serialize(storedResponse, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                CompletedAtUtc = DateTime.UtcNow,
                RetainUntilUtc = DateTime.UtcNow.AddDays(7),
            });
            await db.SaveChangesAsync();
        }

        var before = await SnapshotAsync(api, s);
        var replay = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId }, operationId);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Contains("\"status\":\"InTransit\"", await replay.Content.ReadAsStringAsync());

        // Sin efectos: la entrega sigue Draft y no hay eventos de custodia.
        Assert.Equal(before, await SnapshotAsync(api, s));
        using var checkScope = api.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(DeliveryStates.Draft, (await checkDb.Deliveries.SingleAsync(d => d.Id == s.DeliveryId)).Status);

        // Regresión: un registro legado de OTRA entrega no puede devolver 200 para esta entrega.
        var other = await SeedDraftAsync(api, "LEGACY-OTHER");
        var crossOp = BaseEntity.NewId();
        var crossKey = crossOp.ToString();
        var crossResponse = new CustodyTransitionResponse(other.DeliveryId, DeliveryStates.InTransit, [], DateTime.UtcNow, Guid.NewGuid());
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                OrganizationId = DemoOrganization.Id,
                AccountId = s.DriverAccountId,
                OperationId = crossOp,
                ActionCode = "Pickup",
                RequestHash = FieldSeed.HashToken(crossKey + crossOp.ToString()),
                ResponseStatus = 200,
                ResponseJson = JsonSerializer.Serialize(crossResponse, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                CompletedAtUtc = DateTime.UtcNow,
                RetainUntilUtc = DateTime.UtcNow.AddDays(7),
            });
            await db.SaveChangesAsync();
        }

        var beforeCross = await SnapshotAsync(api, s);
        var mismatched = await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = crossOp }, crossOp);
        Assert.Equal(HttpStatusCode.Conflict, mismatched.StatusCode);
        Assert.Contains(FieldErrorCodes.IdempotencyConflict, await mismatched.Content.ReadAsStringAsync());
        Assert.Equal(beforeCross, await SnapshotAsync(api, s));
        Assert.Equal(DeliveryStates.Draft, (await checkDb.Deliveries.SingleAsync(d => d.Id == s.DeliveryId)).Status);

        // En la entrega correcta el mismo registro legado sí es replay, sin efectos.
        var beforeOther = await SnapshotAsync(api, other);
        var matched = await PostJsonAsync(driver, $"/api/v1/deliveries/{other.DeliveryId}/pickup", new { operationId = crossOp }, crossOp);
        Assert.Equal(HttpStatusCode.OK, matched.StatusCode);
        Assert.Equal(beforeOther, await SnapshotAsync(api, other));
        Assert.Equal(DeliveryStates.Draft, (await checkDb.Deliveries.SingleAsync(d => d.Id == other.DeliveryId)).Status);
    }

    // #162: la matriz se genera desde el origen permitido de cada acción; recorre todos los
    // estados canónicos y excluye solo la combinación válida.
    private static readonly (string Action, string Origin)[] AllowedOrigins =
    [
        ("prepare", DeliveryStates.Draft),
        ("dispatch", DeliveryStates.Draft),
        ("pickup", DeliveryStates.Dispatched),
        ("deliver", DeliveryStates.InTransit),
        ("receipt", DeliveryStates.Delivered),
    ];

    private static readonly string[] CanonicalStates =
    [
        DeliveryStates.Draft,
        DeliveryStates.Dispatched,
        DeliveryStates.InTransit,
        DeliveryStates.Delivered,
        DeliveryStates.Received,
        DeliveryStates.Partial,
        "Cancelled",
    ];

    private static string UnitStatusFor(string state) => state switch
    {
        DeliveryStates.Draft => HandlingUnitStates.Sealed,
        DeliveryStates.Dispatched => HandlingUnitStates.Dispatched,
        DeliveryStates.InTransit => HandlingUnitStates.InTransit,
        DeliveryStates.Delivered => HandlingUnitStates.Delivered,
        DeliveryStates.Received => HandlingUnitStates.Received,
        DeliveryStates.Partial => HandlingUnitStates.Partial,
        _ => HandlingUnitStates.Delivered,
    };

    public static TheoryData<string, string, string, bool> InvalidTransitions()
    {
        var data = new TheoryData<string, string, string, bool>();
        foreach (var (action, origin) in AllowedOrigins)
        {
            foreach (var state in CanonicalStates)
            {
                if (state == origin) continue;
                data.Add(state, UnitStatusFor(state), action, state is DeliveryStates.Received or DeliveryStates.Partial);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public async Task Invalid_transition_matrix_rejects_with_stable_code_and_zero_effects(
        string deliveryStatus, string unitStatus, string action, bool withReceipt)
    {
        await using var api = new ApiFactory(postgres.NewDatabase());
        var s = await SeedDraftAsync(api, $"MATRIX-{action}");
        var (unitId, unitToken) = await SeedUnitAndStateAsync(api, s, deliveryStatus, unitStatus, withReceipt);
        using var bodega = api.ClientFor(BodegaSubject);
        using var driver = api.ClientFor(TestTokens.Driver);
        using var receiver = api.ClientFor(TestTokens.Receiver);

        var before = await SnapshotAsync(api, s);
        HttpResponseMessage response = action switch
        {
            "prepare" => await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/handling-units",
                new { operationId = BaseEntity.NewId(), lines = new[] { new { deliveryLineId = s.DeliveryLineId, lotId = s.LotId, quantity = 1m } } }),
            "dispatch" => await PostJsonAsync(bodega, $"/api/v1/deliveries/{s.DeliveryId}/dispatch",
                new { operationId = BaseEntity.NewId(), handlingUnitIds = new[] { unitId } }),
            "pickup" => await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/pickup", new { operationId = BaseEntity.NewId() }),
            "deliver" => await PostJsonAsync(driver, $"/api/v1/deliveries/{s.DeliveryId}/deliver", new { operationId = BaseEntity.NewId() }),
            _ => await PostJsonAsync(receiver, $"/api/v1/receiver/handling-units/{unitId}/receipt",
                ReceiptBody(BaseEntity.NewId(), s), BaseEntity.NewId(), unitToken),
        };

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        var expectedCode = (action, deliveryStatus) switch
        {
            ("prepare", _) => FieldErrorCodes.InvalidStateTransition,
            ("dispatch", _) => FieldErrorCodes.InvalidStateTransition,
            ("pickup" or "deliver", DeliveryStates.Draft) => FieldErrorCodes.NotDispatched,
            ("pickup" or "deliver", DeliveryStates.Received or DeliveryStates.Partial) => FieldErrorCodes.AlreadyReceived,
            ("pickup" or "deliver", _) => FieldErrorCodes.InvalidStateTransition,
            ("receipt", DeliveryStates.Draft) => FieldErrorCodes.NotDispatched,
            ("receipt", DeliveryStates.Dispatched or DeliveryStates.InTransit) => FieldErrorCodes.NotDelivered,
            ("receipt", DeliveryStates.Received or DeliveryStates.Partial) => FieldErrorCodes.AlreadyReceived,
            _ => FieldErrorCodes.InvalidStateTransition,
        };
        Assert.Contains(expectedCode, text);
        Assert.Equal(before, await SnapshotAsync(api, s));

        using var checkScope = api.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(deliveryStatus, (await checkDb.Deliveries.SingleAsync(d => d.Id == s.DeliveryId)).Status);
    }
}
