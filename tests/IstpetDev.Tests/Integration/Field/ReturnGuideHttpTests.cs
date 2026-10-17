using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using IstpetDev.Infrastructure.Persistence;
using IstpetDev.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IstpetDev.Tests.Integration.Field;

[Trait("Category", "Integration")]
public sealed class ReturnGuideHttpTests(PostgisContainer postgres) : IClassFixture<PostgisContainer>
{
    private const string Endpoint = "/api/v1/return-guides";
    private const string GuideNumber = "GRT-A1B2C3D4";
    private static readonly Guid PlanId = Guid.Parse("a1b2c3d4-0000-4000-8000-000000000280");
    private sealed record Seeded(FieldSeed.Dispatched Delivery, Guid WarehouseId, Guid VehicleId);

    private static async Task<Seeded> SeedAsync(ApiFactory api)
    {
        var delivery = await FieldSeed.DispatchedDeliveryAsync(api, 10m, "RET-280", withHandlingUnit: true);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var d = await db.Deliveries.SingleAsync(d => d.Id == delivery.DeliveryId);
        var vehicleLocation = await db.Locations.SingleAsync(l => l.Id == delivery.VehicleLocationId);
        var plan = new RoutePlan
        {
            Id = PlanId, Code = "R-280", Revision = 1, VehicleId = vehicleLocation.VehicleId!.Value,
            Status = "Published", ProposedBy = "test", PlannedStartUtc = DateTime.UtcNow, AlgorithmVersion = "test",
            PublishedByAccountId = delivery.DriverAccountId
        };
        var stop = new RouteStop { RoutePlanId = plan.Id, ServicePointId = d.ServicePointId, SequenceNumber = 1 };
        d.RouteStopId = stop.Id;
        db.AddRange(plan, stop, new StockBalance { LocationId = d.OriginLocationId, LotId = delivery.LotId, QuantityOnHand = 3m });
        await db.SaveChangesAsync();
        return new(delivery, d.OriginLocationId, plan.VehicleId);
    }

    private static async Task<string> SnapshotAsync(ApiFactory api)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return JsonSerializer.Serialize(new
        {
            Units = await db.HandlingUnits.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Events = await db.CustodyEvents.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Balances = await db.StockBalances.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Movements = await db.InventoryMovements.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Orders = await db.DeliveryOrders.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            OrderLines = await db.DeliveryOrderLines.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Replays = await db.IdempotencyRecords.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
        });
    }

    private static async Task AssertReceivedOnceAsync(ApiFactory api, Seeded seed, decimal returned = 10m)
    {
        Assert.Equal(3m + returned, await FieldSeed.OnHandAsync(api, seed.WarehouseId, seed.Delivery.LotId));
        Assert.Equal(10m - returned, await FieldSeed.OnHandAsync(api, seed.Delivery.VehicleLocationId, seed.Delivery.LotId));
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.All(await db.HandlingUnits.ToListAsync(), h =>
        {
            Assert.Equal(HandlingUnitStates.Returned, h.Status);
            Assert.Equal(seed.WarehouseId, h.LocationId);
        });
        Assert.Single(await db.CustodyEvents.Where(e => e.EventKind == CustodyEventKinds.Returned).ToListAsync());
        Assert.Equal(returned, (await db.InventoryMovements.SingleAsync(m => m.Kind == MovementKinds.Return)).Quantity);
        var order = await db.DeliveryOrders.SingleAsync(o => o.SourceType == "ReturnGuide");
        Assert.Equal("High", order.Priority);
        Assert.Equal(returned, (await db.DeliveryOrderLines.SingleAsync(l => l.DeliveryOrderId == order.Id)).RequestedQuantity);
        Assert.Single(await db.IdempotencyRecords.Where(r => r.ActionCode == "ReceiveReturnGuide").ToListAsync());
    }

    [Theory]
    [InlineData(Roles.Conductor)]
    [InlineData(Roles.Bodega)]
    [InlineData(Roles.Planificador)]
    [InlineData(Roles.Gerencia)]
    [InlineData(Roles.Auditor)]
    public async Task Get_is_read_only_for_every_authorized_role(string role)
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = role == Roles.Conductor ? api.ClientFor(TestTokens.Driver) : api.ClientFor(TestTokens.Planner);
        if (role != Roles.Conductor)
            await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, role);
        await SeedAsync(api);
        var before = await SnapshotAsync(api);

        for (var i = 0; i < 2; i++)
        {
            var response = await client.GetAsync($"{Endpoint}/{GuideNumber}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var guide = await response.Content.ReadFromJsonAsync<ReturnGuideDto>();
            Assert.Equal(PlanId, guide!.RoutePlanId);
            Assert.Equal("PendingWarehouse", guide.Status);
            Assert.Equal(10m, Assert.Single(guide.Lines).Quantity);
        }
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Fact]
    public async Task Driver_cannot_get_or_generate_a_guide_from_another_route()
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        await SeedAsync(api);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Conductor);
        using var otherDriver = api.ClientFor(TestTokens.Planner);
        using var assignedDriver = api.ClientFor(TestTokens.Driver);
        var before = await SnapshotAsync(api);

        Assert.Equal(HttpStatusCode.Forbidden, (await otherDriver.GetAsync($"{Endpoint}/{GuideNumber}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherDriver.PostAsJsonAsync($"{Endpoint}/generate", new GenerateReturnGuideRequest(PlanId))).StatusCode);
        Assert.Equal(before, await SnapshotAsync(api));

        Assert.Equal(HttpStatusCode.OK, (await assignedDriver.GetAsync($"{Endpoint}/{GuideNumber}")).StatusCode);
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Fact]
    public async Task Receiver_cannot_get_a_guide_and_auditor_cannot_write()
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var receiver = api.ClientFor(TestTokens.Receiver);
        await SeedAsync(api);
        var before = await SnapshotAsync(api);
        Assert.Equal(HttpStatusCode.Forbidden, (await receiver.GetAsync($"{Endpoint}/{GuideNumber}")).StatusCode);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Auditor);
        using var auditor = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.Forbidden, (await auditor.PostAsJsonAsync($"{Endpoint}/generate", new GenerateReturnGuideRequest(PlanId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await auditor.PostAsJsonAsync($"{Endpoint}/receive", new ReceiveReturnGuideRequest(GuideNumber, Guid.NewGuid(), null))).StatusCode);
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Theory]
    [InlineData("GRT-A1B2")]
    [InlineData("GRT-a1b2c3d4")]
    [InlineData("GRT-A1B2C3D40")]
    [InlineData("GRT-A1B2C3DG")]
    [InlineData("A1B2C3D4")]
    [InlineData("GRT-")]
    public async Task Invalid_number_is_rejected_without_writes(string number)
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        await SeedAsync(api);
        var before = await SnapshotAsync(api);
        var get = await client.GetAsync($"{Endpoint}/{number}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, get.StatusCode);
        Assert.Contains(FieldErrorCodes.InvalidGuideNumber, await get.Content.ReadAsStringAsync());
        var receive = await client.PostAsJsonAsync($"{Endpoint}/receive", new ReceiveReturnGuideRequest(number, Guid.NewGuid(), null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, receive.StatusCode);
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Fact]
    public async Task Exact_number_with_no_plan_returns_404()
    {
        using var api = new ApiFactory(postgres.NewDatabase());
        using var client = api.ClientFor(TestTokens.Planner);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Endpoint}/{GuideNumber}")).StatusCode);
    }

    [Fact]
    public async Task Colliding_plan_prefix_returns_409_for_get_generate_and_receive()
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        var seed = await SeedAsync(api);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.RoutePlans.Add(new RoutePlan
            {
                Id = Guid.Parse("a1b2c3d4-1111-4111-8111-111111111280"), Code = "R-280-COLLISION", Revision = 1,
                VehicleId = seed.VehicleId, Status = "Published", ProposedBy = "test", PlannedStartUtc = DateTime.UtcNow, AlgorithmVersion = "test"
            });
            await db.SaveChangesAsync();
        }
        var before = await SnapshotAsync(api);
        var responses = new[]
        {
            await client.GetAsync($"{Endpoint}/{GuideNumber}"),
            await client.PostAsJsonAsync($"{Endpoint}/generate", new GenerateReturnGuideRequest(PlanId)),
            await client.PostAsJsonAsync($"{Endpoint}/receive", new ReceiveReturnGuideRequest(GuideNumber, Guid.NewGuid(), null))
        };
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains(FieldErrorCodes.AmbiguousGuideNumber, await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Same_operation_replays_identical_response_and_new_operation_conflicts(bool concurrent)
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        var seed = await SeedAsync(api);
        var request = new ReceiveReturnGuideRequest(GuideNumber, Guid.NewGuid(), DateTime.UtcNow);
        var firstTask = client.PostAsJsonAsync($"{Endpoint}/receive", request);
        if (!concurrent) await firstTask;
        var secondTask = client.PostAsJsonAsync($"{Endpoint}/receive", request);
        var responses = await Task.WhenAll(firstTask, secondTask);
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Equal(await responses[0].Content.ReadAsStringAsync(), await responses[1].Content.ReadAsStringAsync());
        await AssertReceivedOnceAsync(api, seed);
        var before = await SnapshotAsync(api);
        var next = await client.PostAsJsonAsync($"{Endpoint}/receive", request with { OperationId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, next.StatusCode);
        Assert.Contains(FieldErrorCodes.AlreadyReceived, await next.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotAsync(api));
        var get = await client.GetFromJsonAsync<ReturnGuideDto>($"{Endpoint}/{GuideNumber}");
        Assert.Equal("ReceivedAtWarehouse", get!.Status);
        Assert.Single(get.Lines);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Endpoint}/generate", new GenerateReturnGuideRequest(PlanId))).StatusCode);
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Fact]
    public async Task Fresh_operation_cannot_receive_same_guide_twice()
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        var seed = await SeedAsync(api);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Endpoint}/receive",
            new ReceiveReturnGuideRequest(GuideNumber, Guid.NewGuid(), null))).StatusCode);
        var before = await SnapshotAsync(api);
        var repeated = await client.PostAsJsonAsync($"{Endpoint}/receive", new ReceiveReturnGuideRequest(GuideNumber, Guid.NewGuid(), null));
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        Assert.Contains(FieldErrorCodes.AlreadyReceived, await repeated.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotAsync(api));
        await AssertReceivedOnceAsync(api, seed);
    }

    [Fact]
    public async Task Concurrent_different_operations_receive_only_once()
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        var seed = await SeedAsync(api);
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            client.PostAsJsonAsync($"{Endpoint}/receive", new ReceiveReturnGuideRequest(GuideNumber, Guid.NewGuid(), null))));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        await AssertReceivedOnceAsync(api, seed);
    }

    [Fact]
    public async Task Mixed_guide_reconciles_only_units_not_already_returned()
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        var seed = await SeedAsync(api);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existingLine = await db.HandlingUnitLines.SingleAsync();
            existingLine.Quantity = 6m;
            var returned = new HandlingUnit { Code = "HU-ALREADY-RETURNED", DeliveryId = seed.Delivery.DeliveryId, LocationId = seed.WarehouseId, Status = HandlingUnitStates.Returned };
            db.AddRange(returned, new HandlingUnitLine { HandlingUnitId = returned.Id, DeliveryLineId = seed.Delivery.DeliveryLineId, LotId = seed.Delivery.LotId, Quantity = 4m });
            await db.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync($"{Endpoint}/receive", new ReceiveReturnGuideRequest(GuideNumber, Guid.NewGuid(), null));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, (await response.Content.ReadFromJsonAsync<ReceiveReturnGuideResponse>())!.ItemsReconciled);
        await AssertReceivedOnceAsync(api, seed, 6m);
    }

    [Fact]
    public async Task Empty_operation_and_changed_payload_cannot_move_stock()
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        var seed = await SeedAsync(api);
        var request = new ReceiveReturnGuideRequest(GuideNumber, Guid.Empty, null);
        var before = await SnapshotAsync(api);
        var invalid = await client.PostAsJsonAsync($"{Endpoint}/receive", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Contains(FieldErrorCodes.InvalidOperationId, await invalid.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotAsync(api));
        request = request with { OperationId = Guid.NewGuid() };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Endpoint}/receive", request)).StatusCode);
        before = await SnapshotAsync(api);
        var changed = await client.PostAsJsonAsync($"{Endpoint}/receive", request with { OccurredAtDevice = DateTime.UtcNow });
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Contains(FieldErrorCodes.IdempotencyConflict, await changed.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotAsync(api));
        await AssertReceivedOnceAsync(api, seed);
    }

    [Theory]
    [InlineData(HandlingUnitStates.Returned, DeliveryStates.NotDelivered, FieldErrorCodes.AlreadyReceived)]
    [InlineData(HandlingUnitStates.Received, DeliveryStates.Received, FieldErrorCodes.NoHandlingUnits)]
    public async Task Legacy_returned_or_empty_guide_conflicts_without_a_replay_record(string unitState, string deliveryState, string error)
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        var seed = await SeedAsync(api);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.HandlingUnits.SingleAsync()).Status = unitState;
            (await db.Deliveries.SingleAsync(d => d.Id == seed.Delivery.DeliveryId)).Status = deliveryState;
            await db.SaveChangesAsync();
        }
        var before = await SnapshotAsync(api);
        var response = await client.PostAsJsonAsync($"{Endpoint}/receive", new ReceiveReturnGuideRequest(GuideNumber, Guid.NewGuid(), null));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(error, await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Fact]
    public async Task Operation_used_for_another_action_cannot_receive_a_guide()
    {
        var connection = postgres.NewDatabase();
        using var api = new ApiFactory(connection);
        using var client = api.ClientFor(TestTokens.Planner);
        var actorId = await TestAccounts.SetRoleAsync(connection, TestTokens.Planner, Roles.Bodega);
        await SeedAsync(api);
        var operationId = Guid.NewGuid();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                AccountId = actorId, OperationId = operationId, ActionCode = "Dispatch", RequestHash = "previous-action",
                ResponseStatus = 200, ResponseJson = "{}", CompletedAtUtc = DateTime.UtcNow, RetainUntilUtc = DateTime.UtcNow.AddDays(7)
            });
            await db.SaveChangesAsync();
        }
        var before = await SnapshotAsync(api);
        var response = await client.PostAsJsonAsync($"{Endpoint}/receive", new ReceiveReturnGuideRequest(GuideNumber, operationId, null));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(FieldErrorCodes.IdempotencyConflict, await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await SnapshotAsync(api));
    }

    [Fact]
    public async Task Generate_persists_custody_once_and_get_remains_read_only()
    {
        using var api = new ApiFactory(postgres.NewDatabase());
        using var client = api.ClientFor(TestTokens.Planner);
        await SeedAsync(api);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Endpoint}/generate", new GenerateReturnGuideRequest(PlanId))).StatusCode);
        var generated = await SnapshotAsync(api);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Endpoint}/{GuideNumber}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Endpoint}/generate", new GenerateReturnGuideRequest(PlanId))).StatusCode);
        Assert.Equal(generated, await SnapshotAsync(api));
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(HandlingUnitStates.NotDelivered, (await db.HandlingUnits.SingleAsync()).Status);
        Assert.Single(await db.CustodyEvents.ToListAsync());
    }
}
