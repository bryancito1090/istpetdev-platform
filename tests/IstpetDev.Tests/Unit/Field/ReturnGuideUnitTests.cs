using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IstpetDev.Tests.Unit.Field;

[Trait("Category", "Unit")]
public sealed class ReturnGuideUnitTests
{
    private sealed class FakeReturnGuideFieldService : IFieldService
    {
        public bool ThrowOnGenerate { get; set; }
        public bool ThrowOnReceive { get; set; }

        public Task<ReturnGuideDto> GenerateReturnGuideAsync(Guid routePlanId, Guid actorAccountId, CancellationToken ct = default)
        {
            if (ThrowOnGenerate)
            {
                throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Plan de ruta no encontrado.");
            }

            var lines = new List<ReturnGuideLineDto>
            {
                new(
                    Guid.NewGuid(),
                    "HU-RET-01",
                    Guid.NewGuid(),
                    "MED-AMOX-500",
                    "Amoxicilina 500mg",
                    Guid.NewGuid(),
                    "LOT-2026-X1",
                    10m,
                    "Caja",
                    "No entregado en ruta: cliente ausente"
                ),
                new(
                    Guid.NewGuid(),
                    "HU-RET-02",
                    Guid.NewGuid(),
                    "MED-PARAC-500",
                    "Paracetamol 500mg",
                    Guid.NewGuid(),
                    "LOT-2026-Y2",
                    5m,
                    "Caja",
                    "Rechazado por receptor: empaque dañado"
                )
            };

            var guideNumber = $"GRT-{routePlanId.ToString()[..8].ToUpperInvariant()}";
            return Task.FromResult(new ReturnGuideDto(
                guideNumber,
                routePlanId,
                "PBX-1234",
                "Carlos Chofer",
                Guid.NewGuid(),
                "Bodega Central Quito",
                DateTime.UtcNow,
                "PendingWarehouse",
                lines
            ));
        }

        public Task<ReturnGuideDto> GetReturnGuideAsync(string guideNumber, Guid actorAccountId, CancellationToken ct = default)
        {
            if (guideNumber == "GRT-NOTFOUND")
            {
                throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, $"Guía {guideNumber} no encontrada.");
            }

            var lines = new List<ReturnGuideLineDto>
            {
                new(
                    Guid.NewGuid(),
                    "HU-RET-01",
                    Guid.NewGuid(),
                    "MED-AMOX-500",
                    "Amoxicilina 500mg",
                    Guid.NewGuid(),
                    "LOT-2026-X1",
                    10m,
                    "Caja",
                    "Cliente ausente"
                )
            };

            return Task.FromResult(new ReturnGuideDto(
                guideNumber,
                Guid.NewGuid(),
                "PBX-1234",
                "Carlos Chofer",
                Guid.NewGuid(),
                "Bodega Central Quito",
                DateTime.UtcNow,
                "PendingWarehouse",
                lines
            ));
        }

        public Task<ReceiveReturnGuideResponse> ReceiveReturnGuideAsync(ReceiveReturnGuideRequest request, Guid actorAccountId, CancellationToken ct = default)
        {
            if (ThrowOnReceive)
            {
                throw new FieldConflictException(FieldErrorCodes.NoHandlingUnits, "La guía no contiene bultos.");
            }

            return Task.FromResult(new ReceiveReturnGuideResponse(
                request.GuideNumber,
                "ReceivedAtWarehouse",
                DateTime.UtcNow,
                2,
                Guid.NewGuid()
            ));
        }

        // Métodos no usados en estas pruebas
        public Task<HandlingUnitResponse> CreateHandlingUnitAsync(Guid deliveryId, CreateHandlingUnitRequest request, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<DispatchResponse> DispatchAsync(Guid deliveryId, DispatchRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<CustodyTransitionResponse> PickupAsync(Guid deliveryId, CustodyTransitionRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<CustodyTransitionResponse> DeliverAsync(Guid deliveryId, CustodyTransitionRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<MobileAssignmentResponse> GetMobileAssignmentsAsync(Guid driverAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<DeliveryListResponse> GetDeliveriesAsync(Guid? routePlanId, string? status, int limit, string? cursor, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<DeliveryDetailResponse> GetDeliveryDetailAsync(Guid deliveryId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PublicTraceUnitDto> GetPublicTraceAsync(Guid unitId, string? traceToken, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<HandlingUnitTraceResponse> GetOperativeTraceAsync(Guid unitId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RevokePublicTokenAsync(Guid unitId, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<DemoAdvanceResponse> AdvanceDemoAsync(Guid routePlanId, DemoAdvanceRequest request, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<TodayRoutePlanDto>> GetTodayRoutePlansAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TodayRoutePlanDto>>(Array.Empty<TodayRoutePlanDto>());
        public Task<EvidenceUploadResponse> CreateEvidenceUploadAsync(Guid receiptId, EvidenceUploadRequest request, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<EvidenceConfirmResponse> ConfirmEvidenceAsync(Guid evidenceId, EvidenceConfirmRequest request, Guid actorAccountId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ReceiverUnitResponse> GetReceiverUnitAsync(Guid unitId, string? traceToken, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ReceiptResponse> CreateReceiverReceiptAsync(Guid unitId, string? traceToken, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default) => throw new NotImplementedException();
        public RejectionReasonsCatalogResponse GetRejectionReasonsCatalog() => throw new NotImplementedException();
    }

    private static ControllerContext CreateContext(string role, Guid? accountId = null)
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, role),
                    new Claim(AccountRoleClaimsTransformation.AccountIdClaim, (accountId ?? Guid.NewGuid()).ToString())
                }, "TestAuth"))
            }
        };
    }

    [Fact]
    public async Task GenerateGuide_returns_200_with_return_lines_and_proper_fields()
    {
        var fake = new FakeReturnGuideFieldService();
        var controller = new ReturnGuidesController(fake)
        {
            ControllerContext = CreateContext(Roles.Conductor)
        };

        var planId = Guid.NewGuid();
        var result = await controller.GenerateGuide(new GenerateReturnGuideRequest(planId), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var guide = Assert.IsType<ReturnGuideDto>(okResult.Value);

        Assert.StartsWith("GRT-", guide.GuideNumber);
        Assert.Equal(planId, guide.RoutePlanId);
        Assert.Equal("PBX-1234", guide.VehiclePlate);
        Assert.Equal("Carlos Chofer", guide.DriverName);
        Assert.Equal("Bodega Central Quito", guide.DestinationWarehouseName);
        Assert.Equal(2, guide.Lines.Count);
        Assert.Equal("HU-RET-01", guide.Lines[0].HandlingUnitCode);
        Assert.Equal(10m, guide.Lines[0].Quantity);
        Assert.Equal("No entregado en ruta: cliente ausente", guide.Lines[0].Reason);
    }

    [Fact]
    public async Task GenerateGuide_when_service_throws_returns_problem_details()
    {
        var fake = new FakeReturnGuideFieldService { ThrowOnGenerate = true };
        var controller = new ReturnGuidesController(fake)
        {
            ControllerContext = CreateContext(Roles.Planificador)
        };

        var result = await controller.GenerateGuide(new GenerateReturnGuideRequest(Guid.NewGuid()), CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(FieldErrorCodes.UnitNotFound, problem.Title);
    }

    [Fact]
    public async Task GetGuide_by_number_returns_200_with_guide_data()
    {
        var fake = new FakeReturnGuideFieldService();
        var controller = new ReturnGuidesController(fake)
        {
            ControllerContext = CreateContext(Roles.Bodega)
        };

        var result = await controller.GetGuide("GRT-12345678", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var guide = Assert.IsType<ReturnGuideDto>(okResult.Value);

        Assert.Equal("GRT-12345678", guide.GuideNumber);
        Assert.Single(guide.Lines);
        Assert.Equal("MED-AMOX-500", guide.Lines[0].Sku);
    }

    [Fact]
    public async Task GetGuide_not_found_returns_404()
    {
        var fake = new FakeReturnGuideFieldService();
        var controller = new ReturnGuidesController(fake)
        {
            ControllerContext = CreateContext(Roles.Planificador)
        };

        var result = await controller.GetGuide("GRT-NOTFOUND", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task ReceiveGuide_at_warehouse_returns_200_with_rebalance_order()
    {
        var fake = new FakeReturnGuideFieldService();
        var controller = new ReturnGuidesController(fake)
        {
            ControllerContext = CreateContext(Roles.Bodega)
        };

        var opId = Guid.NewGuid();
        var req = new ReceiveReturnGuideRequest("GRT-12345678", opId, DateTime.UtcNow);
        var result = await controller.ReceiveGuide(req, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var resp = Assert.IsType<ReceiveReturnGuideResponse>(okResult.Value);

        Assert.Equal("GRT-12345678", resp.GuideNumber);
        Assert.Equal("ReceivedAtWarehouse", resp.Status);
        Assert.Equal(2, resp.ItemsReconciled);
        Assert.NotEqual(Guid.Empty, resp.RebalanceOrderId);
    }

    [Fact]
    public async Task ReceiveGuide_when_conflict_returns_409()
    {
        var fake = new FakeReturnGuideFieldService { ThrowOnReceive = true };
        var controller = new ReturnGuidesController(fake)
        {
            ControllerContext = CreateContext(Roles.Bodega)
        };

        var req = new ReceiveReturnGuideRequest("GRT-EMPTY", Guid.NewGuid(), DateTime.UtcNow);
        var result = await controller.ReceiveGuide(req, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(FieldErrorCodes.NoHandlingUnits, problem.Title);
    }
}
