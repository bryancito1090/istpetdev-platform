using System;
using System.Threading;
using System.Threading.Tasks;
using IstpetDev.Application.Field.Dtos;

namespace IstpetDev.Application.Field;

public interface IFieldService
{
    Task<HandlingUnitResponse> CreateHandlingUnitAsync(
        Guid deliveryId,
        CreateHandlingUnitRequest request,
        Guid actorAccountId,
        CancellationToken ct = default);

    Task<DispatchResponse> DispatchAsync(
        Guid deliveryId,
        DispatchRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        CancellationToken ct = default);

    Task<ReceiptResponse> CreateReceiptAsync(
        Guid deliveryId,
        CreateReceiptRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        CancellationToken ct = default);

    Task<MobileAssignmentResponse> GetMobileAssignmentsAsync(
        Guid driverAccountId,
        CancellationToken ct = default);

    Task<DeliveryListResponse> GetDeliveriesAsync(
        Guid? routePlanId,
        string? status,
        int limit,
        string? cursor,
        CancellationToken ct = default);

    Task<PublicTraceUnitDto> GetPublicTraceAsync(
        Guid unitId,
        string? traceToken,
        CancellationToken ct = default);

    Task<HandlingUnitTraceResponse> GetOperativeTraceAsync(
        Guid unitId,
        CancellationToken ct = default);

    Task RevokePublicTokenAsync(
        Guid unitId,
        Guid actorAccountId,
        CancellationToken ct = default);

    Task<DemoAdvanceResponse> AdvanceDemoAsync(
        Guid routePlanId,
        DemoAdvanceRequest request,
        Guid actorAccountId,
        CancellationToken ct = default);

    Task<EvidenceUploadResponse> CreateEvidenceUploadAsync(
        Guid receiptId,
        EvidenceUploadRequest request,
        Guid actorAccountId,
        CancellationToken ct = default);

    Task<EvidenceConfirmResponse> ConfirmEvidenceAsync(
        Guid evidenceId,
        EvidenceConfirmRequest request,
        Guid actorAccountId,
        CancellationToken ct = default);
}
