using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Api.Authentication;
using IstpetDev.Api.Controllers;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Application.Field;
using IstpetDev.Application.Field.Dtos;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IstpetDev.Tests.Unit.Field;

[Trait("Category", "Unit")]
public sealed class FieldUnitTests
{
    private sealed class FakeFieldService : IFieldService
    {
        public Task<HandlingUnitResponse> CreateHandlingUnitAsync(Guid deliveryId, CreateHandlingUnitRequest request, Guid actorAccountId, CancellationToken ct = default)
        {
            if (request.Lines.Count == 0)
            {
                throw new FieldUnprocessableException(FieldErrorCodes.QuantityExceeded, "Sin líneas");
            }
            return Task.FromResult(new HandlingUnitResponse(
                Guid.NewGuid(), "HU-01", deliveryId, "Prepared", "https://label", "token123", Array.Empty<HandlingUnitLineDto>(), DateTime.UtcNow));
        }

        public Task<DispatchResponse> DispatchAsync(Guid deliveryId, DispatchRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default)
        {
            return Task.FromResult(new DispatchResponse(deliveryId, "Dispatched", request.HandlingUnitIds, DateTime.UtcNow, Guid.NewGuid()));
        }

        public Task<ReceiptResponse> CreateReceiptAsync(Guid deliveryId, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default)
        {
            if (request.ExpectedDeliveryVersion == 999)
            {
                throw new FieldConflictException(FieldErrorCodes.AssignmentChanged, "Versión en conflicto");
            }
            return Task.FromResult(new ReceiptResponse(
                Guid.NewGuid(), deliveryId, DateTime.UtcNow, 2, "Delivered", "Pending", Array.Empty<ReceiptLineDto>()));
        }

        public Task<MobileAssignmentResponse> GetMobileAssignmentsAsync(Guid driverAccountId, CancellationToken ct = default)
        {
            return Task.FromResult(new MobileAssignmentResponse(DateTime.UtcNow, driverAccountId, Array.Empty<MobileAssignmentRoute>()));
        }

        public Task<DeliveryListResponse> GetDeliveriesAsync(Guid? routePlanId, string? status, int limit, string? cursor, CancellationToken ct = default)
        {
            return Task.FromResult(new DeliveryListResponse(Array.Empty<DeliveryListItem>(), null, 0));
        }

        public Task<DeliveryDetailResponse> GetDeliveryDetailAsync(Guid deliveryId, CancellationToken ct = default)
        {
            if (deliveryId == Guid.Empty)
            {
                throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Entrega no encontrada.");
            }
            var lines = new[]
            {
                new DeliveryDetailLineDto(Guid.NewGuid(), Guid.NewGuid(), "LOT-01", "MED-001", "Paracetamol 500mg", 50m, "U")
            };
            var hus = new[]
            {
                new DeliveryDetailHandlingUnitDto(Guid.NewGuid(), "HU-01", "Prepared")
            };
            return Task.FromResult(new DeliveryDetailResponse(
                deliveryId, "Draft", 1, "Hospital Docente de Ambato", Guid.NewGuid(), lines, hus));
        }

        public Task<PublicTraceUnitDto> GetPublicTraceAsync(Guid unitId, string? traceToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(traceToken) || traceToken == "expired")
            {
                throw new FieldNotFoundException(FieldErrorCodes.UnitNotFound, "Unit not found.");
            }
            return Task.FromResult(new PublicTraceUnitDto(
                unitId, "HU-01", "Prepared", Array.Empty<PublicTraceProductUnit>(), Array.Empty<PublicTraceEvent>()));
        }

        public Task<HandlingUnitTraceResponse> GetOperativeTraceAsync(Guid unitId, CancellationToken ct = default)
        {
            return Task.FromResult(new HandlingUnitTraceResponse(unitId, "HU-01", "Prepared", null, Array.Empty<HandlingUnitLineDto>(), Array.Empty<HandlingUnitTraceEvent>()));
        }

        public Task RevokePublicTokenAsync(Guid unitId, Guid actorAccountId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task<DemoAdvanceResponse> AdvanceDemoAsync(Guid routePlanId, DemoAdvanceRequest request, Guid actorAccountId, CancellationToken ct = default)
        {
            return Task.FromResult(new DemoAdvanceResponse(Array.Empty<CompletedStopDto>()));
        }

        public Task<EvidenceUploadResponse> CreateEvidenceUploadAsync(Guid receiptId, EvidenceUploadRequest request, Guid actorAccountId, CancellationToken ct = default)
        {
            return Task.FromResult(new EvidenceUploadResponse(Guid.NewGuid(), "https://s3/up", null, DateTime.UtcNow.AddMinutes(15)));
        }

        public Task<EvidenceConfirmResponse> ConfirmEvidenceAsync(Guid evidenceId, EvidenceConfirmRequest request, Guid actorAccountId, CancellationToken ct = default)
        {
            return Task.FromResult(new EvidenceConfirmResponse(evidenceId, "Confirmed", DateTime.UtcNow));
        }

        public Task<ReceiverUnitResponse> GetReceiverUnitAsync(Guid unitId, string? traceToken, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<ReceiptResponse> CreateReceiverReceiptAsync(Guid unitId, string? traceToken, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void Qr_token_generation_produces_valid_sha256_hash()
    {
        var rawBytes = RandomNumberGenerator.GetBytes(16);
        var clearToken = Convert.ToHexString(rawBytes).ToLowerInvariant();

        Assert.Equal(32, clearToken.Length);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(clearToken));
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        Assert.Equal(64, hashHex.Length);
    }

    [Fact]
    public async Task DeliveriesController_handles_conflict_exception_as_problem_details_409()
    {
        var fake = new FakeFieldService();
        var controller = new DeliveriesController(fake);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, Roles.Conductor),
                    new Claim(AccountRoleClaimsTransformation.AccountIdClaim, Guid.NewGuid().ToString())
                }, "TestAuth"))
            }
        };

        var req = new CreateReceiptRequest(Guid.NewGuid(), 999, DateTime.UtcNow, Array.Empty<ReceiptLineRequest>());
        var result = await controller.CreateReceipt(Guid.NewGuid(), req, null, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, objectResult.StatusCode);
    }

    [Fact]
    public async Task DeliveriesController_dispatch_returns_200_with_idempotency_key()
    {
        var fake = new FakeFieldService();
        var controller = new DeliveriesController(fake);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, Roles.Bodega),
                    new Claim(AccountRoleClaimsTransformation.AccountIdClaim, Guid.NewGuid().ToString())
                }, "TestAuth"))
            }
        };

        var opId = Guid.NewGuid();
        var req = new DispatchRequest(opId, new[] { Guid.NewGuid() });
        var result = await controller.Dispatch(Guid.NewGuid(), req, opId.ToString(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var resp = Assert.IsType<DispatchResponse>(okResult.Value);
        Assert.Equal("Dispatched", resp.Status);
    }

    [Fact]
    public async Task EvidenceController_creates_upload_url_and_confirms()
    {
        var fake = new FakeFieldService();
        var controller = new EvidenceController(fake);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, Roles.Conductor),
                    new Claim(AccountRoleClaimsTransformation.AccountIdClaim, Guid.NewGuid().ToString())
                }, "TestAuth"))
            }
        };

        var upReq = new EvidenceUploadRequest(Guid.NewGuid(), "photo", "image/jpeg", 1024, "sha");
        var upRes = await controller.RequestUpload(Guid.NewGuid(), upReq, CancellationToken.None);
        var okUp = Assert.IsType<OkObjectResult>(upRes);
        var upResp = Assert.IsType<EvidenceUploadResponse>(okUp.Value);
        Assert.StartsWith("https://s3", upResp.UploadUrl);

        var confReq = new EvidenceConfirmRequest(Guid.NewGuid(), "sha", 1024);
        var confRes = await controller.ConfirmEvidence(upResp.EvidenceId, confReq, CancellationToken.None);
        var okConf = Assert.IsType<OkObjectResult>(confRes);
        var confResp = Assert.IsType<EvidenceConfirmResponse>(okConf.Value);
        Assert.Equal("Confirmed", confResp.Status);
    }

    [Fact]
    public async Task DeliveriesController_get_detail_returns_200_with_lines_and_handling_units()
    {
        var fake = new FakeFieldService();
        var controller = new DeliveriesController(fake);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Role, Roles.Bodega),
                    new Claim(AccountRoleClaimsTransformation.AccountIdClaim, Guid.NewGuid().ToString())
                }, "TestAuth"))
            }
        };

        var delId = Guid.NewGuid();
        var result = await controller.GetDeliveryDetail(delId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var detail = Assert.IsType<DeliveryDetailResponse>(okResult.Value);
        Assert.Equal(delId, detail.DeliveryId);
        Assert.Single(detail.Lines);
        Assert.Equal("MED-001", detail.Lines[0].Sku);
        Assert.Equal("Hospital Docente de Ambato", detail.PointName);
    }
}
