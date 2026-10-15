import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateHandlingUnitRequest,
  HandlingUnitResponse,
  DispatchRequest,
  DispatchResponse,
  MobileAssignmentResponse,
  DeliveryListResponse,
  DeliveryDetailResponse,
  CreateReceiptRequest,
  ReceiptResponse,
  EvidenceUploadRequest,
  EvidenceUploadResponse,
  EvidenceConfirmRequest,
  EvidenceConfirmResponse,
  DemoAdvanceRequest,
  DemoAdvanceResponse,
  PublicTraceUnitDto
} from '@istpetdev/shared-core';

/**
 * Puerto abstracto FieldApi (Arquitectura Hexagonal / Puertos y Adaptadores).
 * Permite desacoplar totalmente la UI y los servicios de cliente del backend,
 * posibilitando la alternancia transparente entre MockFieldApi y HttpFieldApi.
 */
export interface IFieldApi {
  // Bodega (Web / Operaciones de Despacho)
  createHandlingUnit(deliveryId: string, req: CreateHandlingUnitRequest): Observable<HandlingUnitResponse>;
  dispatch(deliveryId: string, req: DispatchRequest, idempotencyKey: string): Observable<DispatchResponse>;
  getDeliveries(routePlanId?: string, status?: string, cursor?: string, limit?: number): Observable<DeliveryListResponse>;
  getDelivery(deliveryId: string): Observable<DeliveryDetailResponse>;
  revokePublicToken(unitId: string): Observable<void>;
  advanceDemo(routePlanId: string, req: DemoAdvanceRequest): Observable<DemoAdvanceResponse>;

  // Conductor (Mobile / Captura y Sincronización)
  getAssignments(): Observable<MobileAssignmentResponse>;
  createReceipt(deliveryId: string, req: CreateReceiptRequest, idempotencyKey: string): Observable<ReceiptResponse>;
  uploadEvidence(receiptId: string, req: EvidenceUploadRequest, fileBlob?: Blob): Observable<EvidenceUploadResponse>;
  confirmEvidence(evidenceId: string, req: EvidenceConfirmRequest): Observable<EvidenceConfirmResponse>;

  // Público (Consulta QR sin datos sensibles)
  getPublicTrace(unitId: string, token: string): Observable<PublicTraceUnitDto>;
}

export const FIELD_API = new InjectionToken<IFieldApi>('FIELD_API');
