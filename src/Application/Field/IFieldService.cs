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

    // #162: el conductor asignado marca la recogida (Dispatched → InTransit) y la entrega física
    // (InTransit → Delivered). No mueven stock; solo custodia.
    Task<CustodyTransitionResponse> PickupAsync(
        Guid deliveryId,
        CustodyTransitionRequest request,
        Guid actorAccountId,
        string idempotencyKey,
        CancellationToken ct = default);

    Task<CustodyTransitionResponse> DeliverAsync(
        Guid deliveryId,
        CustodyTransitionRequest request,
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

    Task<DeliveryDetailResponse> GetDeliveryDetailAsync(
        Guid deliveryId,
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

    Task<ReceiverUnitResponse> GetReceiverUnitAsync(Guid unitId, string? traceToken, CancellationToken ct = default);
    Task<ReceiptResponse> CreateReceiverReceiptAsync(Guid unitId, string? traceToken, CreateReceiptRequest request, Guid actorAccountId, string idempotencyKey, CancellationToken ct = default);
    RejectionReasonsCatalogResponse GetRejectionReasonsCatalog();

    // Issue #247: Guía de Retorno de lo no entregado
    Task<ReturnGuideDto> GenerateReturnGuideAsync(Guid routePlanId, Guid actorAccountId, CancellationToken ct = default);
    Task<ReturnGuideDto> GetReturnGuideAsync(string guideNumber, CancellationToken ct = default);
    Task<ReceiveReturnGuideResponse> ReceiveReturnGuideAsync(ReceiveReturnGuideRequest request, Guid actorAccountId, CancellationToken ct = default);
}
