import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateHandlingUnitRequest,
  HandlingUnitResponse,
  DispatchRequest,
  DispatchResponse,
  CustodyTransitionRequest,
  CustodyTransitionResponse,
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
  // #162: custodia del conductor. Recoger (Dispatched → InTransit) y entregar (InTransit → Delivered).
  pickup(deliveryId: string, req: CustodyTransitionRequest, idempotencyKey: string): Observable<CustodyTransitionResponse>;
  deliver(deliveryId: string, req: CustodyTransitionRequest, idempotencyKey: string): Observable<CustodyTransitionResponse>;
  createReceipt(deliveryId: string, req: CreateReceiptRequest, idempotencyKey: string): Observable<ReceiptResponse>;
  uploadEvidence(receiptId: string, req: EvidenceUploadRequest, fileBlob?: Blob): Observable<EvidenceUploadResponse>;
  confirmEvidence(evidenceId: string, req: EvidenceConfirmRequest): Observable<EvidenceConfirmResponse>;

  // Público (Consulta QR sin datos sensibles)
  getPublicTrace(unitId: string, token: string): Observable<PublicTraceUnitDto>;

  // Receptor (Mobile / Recepción validada con QR del bulto)
  getReceiverUnit(unitId: string, token: string): Observable<ReceiverUnit>;
  receiveAsReceiver(unitId: string, token: string, req: CreateReceiptRequest, idempotencyKey: string): Observable<ReceiptResponse>;
}

export interface ReceiverUnitLine {
  deliveryLineId: string;
  lotId: string;
  lotCode: string;
  sku: string;
  productName: string;
  quantity: number;
  unit: string;
}

export interface ReceiverUnitBox {
  handlingUnitId: string;
  code: string;
  status: string;
}

export interface ReceiverUnit {
  handlingUnitId: string;
  code: string;
  status: string;
  deliveryId: string;
  deliveryVersion: number;
  destinationName: string;
  /** #162: todos los bultos de la entrega; el recibo final cubre la entrega completa. */
  boxes: ReceiverUnitBox[];
  lines: ReceiverUnitLine[];
}

export const FIELD_API = new InjectionToken<IFieldApi>('FIELD_API');

