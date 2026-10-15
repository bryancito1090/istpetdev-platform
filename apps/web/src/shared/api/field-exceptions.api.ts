import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../config/environment';

/** Pedido de reposición pedido en campo por el receptor (#116). */
export interface FieldRequestOrderLineDto {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  requestedQuantity: number;
  plannedQuantity: number;
  deliveredQuantity: number;
}

export interface FieldRequestOrderDto {
  id: string;
  orderNumber: string;
  destinationCode: string;
  destinationName: string;
  requiredDateUtc: string;
  priority: string;
  status: string;
  sourceType: string;
  notes: string;
  createdAtUtc: string;
  version: number;
  lines: FieldRequestOrderLineDto[];
}

export interface ReceiptExceptionLineDto {
  receiptLineId: string;
  sku: string;
  productName: string;
  lotCode: string;
  quantityAccepted: number;
  quantityRejected: number;
  rejectionReason?: string | null;
  remainingDeclared?: number | null;
  /** Con signo: negativo = consumo derivado; positivo = conteo mayor que el saldo. */
  adjustmentQuantity?: number | null;
  adjustmentReason?: string | null;
}

/** Metadatos de evidencia. El archivo todavía no se almacena (la URL de subida del backend es un stub). */
export interface ReceiptEvidenceDto {
  evidenceId: string;
  kind: string;
  status: string;
  mimeType: string;
  byteSize?: number | null;
  sha256?: string | null;
  confirmedAtUtc?: string | null;
}

export interface ReceiptExceptionDto {
  receiptId: string;
  deliveryId: string;
  deliveryCode: string;
  servicePointId: string;
  servicePointCode: string;
  servicePointName: string;
  actorName: string;
  receivedAtServer: string;
  occurredAtDevice?: string | null;
  hasRejections: boolean;
  hasAdjustments: boolean;
  lines: ReceiptExceptionLineDto[];
  evidence: ReceiptEvidenceDto[];
}

export interface ReceiptExceptionsResponse {
  items: ReceiptExceptionDto[];
  total: number;
}

@Injectable({ providedIn: 'root' })
export class FieldExceptionsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1`;

  listFieldRequests(limit = 100): Observable<FieldRequestOrderDto[]> {
    const params = new HttpParams().set('sourceType', 'FieldRequest').set('limit', limit);
    return this.http.get<FieldRequestOrderDto[]>(`${this.baseUrl}/delivery-orders`, { params });
  }

  /** Envía la versión vista: si otro usuario o proceso cambió el pedido, el servidor responde 409. */
  cancelOrder(id: string, expectedVersion: number): Observable<{ id: string; status: string; version: number }> {
    const params = new HttpParams().set('expectedVersion', expectedVersion);
    return this.http.patch<{ id: string; status: string; version: number }>(
      `${this.baseUrl}/delivery-orders/${encodeURIComponent(id)}/cancel`, {}, { params });
  }

  listReceiptExceptions(servicePointId?: string, limit = 50): Observable<ReceiptExceptionsResponse> {
    let params = new HttpParams().set('limit', limit);
    if (servicePointId) params = params.set('servicePointId', servicePointId);
    return this.http.get<ReceiptExceptionsResponse>(`${this.baseUrl}/receipts/exceptions`, { params });
  }
}
