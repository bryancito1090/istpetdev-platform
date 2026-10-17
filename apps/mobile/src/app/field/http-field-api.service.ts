import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, switchMap, of, throwError } from 'rxjs';
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
  PublicTraceUnitDto,
  TodayRoutePlanDto
} from '@istpetdev/shared-core';
import { environment } from '../config/environment';
import { IFieldApi, ReceiverUnit } from './field-api.interface';

@Injectable({
  providedIn: 'root'
})
export class HttpFieldApiService implements IFieldApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  createHandlingUnit(deliveryId: string, req: CreateHandlingUnitRequest): Observable<HandlingUnitResponse> {
    return this.http.post<HandlingUnitResponse>(`${this.baseUrl}/api/v1/deliveries/${deliveryId}/handling-units`, req);
  }

  dispatch(deliveryId: string, req: DispatchRequest, idempotencyKey: string): Observable<DispatchResponse> {
    const headers = new HttpHeaders({
      'Idempotency-Key': idempotencyKey
    });
    return this.http.post<DispatchResponse>(`${this.baseUrl}/api/v1/deliveries/${deliveryId}/dispatch`, req, { headers });
  }

  pickup(deliveryId: string, req: CustodyTransitionRequest, idempotencyKey: string): Observable<CustodyTransitionResponse> {
    const headers = new HttpHeaders({
      'Idempotency-Key': idempotencyKey
    });
    return this.http.post<CustodyTransitionResponse>(`${this.baseUrl}/api/v1/deliveries/${deliveryId}/pickup`, req, { headers });
  }

  deliver(deliveryId: string, req: CustodyTransitionRequest, idempotencyKey: string): Observable<CustodyTransitionResponse> {
    const headers = new HttpHeaders({
      'Idempotency-Key': idempotencyKey
    });
    return this.http.post<CustodyTransitionResponse>(`${this.baseUrl}/api/v1/deliveries/${deliveryId}/deliver`, req, { headers });
  }

  getDeliveries(routePlanId?: string, status?: string, cursor?: string, limit?: number): Observable<DeliveryListResponse> {
    let params = new HttpParams();
    if (routePlanId) params = params.set('routePlanId', routePlanId);
    if (status) params = params.set('status', status);
    if (cursor) params = params.set('cursor', cursor);
    if (limit) params = params.set('limit', limit.toString());

    return this.http.get<DeliveryListResponse>(`${this.baseUrl}/api/v1/deliveries`, { params });
  }

  getDelivery(deliveryId: string): Observable<DeliveryDetailResponse> {
    return this.http.get<DeliveryDetailResponse>(`${this.baseUrl}/api/v1/deliveries/${deliveryId}`);
  }

  revokePublicToken(unitId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/v1/handling-units/${unitId}/public-token/revoke`, {});
  }

  advanceDemo(routePlanId: string, req: DemoAdvanceRequest): Observable<DemoAdvanceResponse> {
    return this.http.post<DemoAdvanceResponse>(`${this.baseUrl}/api/v1/demo/route-plans/${routePlanId}/advance`, req);
  }

  getTodayRoutePlans(): Observable<TodayRoutePlanDto[]> {
    return this.http.get<TodayRoutePlanDto[]>(`${this.baseUrl}/api/v1/demo/route-plans/today`);
  }

  getAssignments(): Observable<MobileAssignmentResponse> {
    return this.http.get<MobileAssignmentResponse>(`${this.baseUrl}/api/v1/mobile/assignments`);
  }

  createReceipt(deliveryId: string, req: CreateReceiptRequest, idempotencyKey: string): Observable<ReceiptResponse> {
    const headers = new HttpHeaders({
      'Idempotency-Key': idempotencyKey
    });
    return this.http.post<ReceiptResponse>(`${this.baseUrl}/api/v1/deliveries/${deliveryId}/receipts`, req, { headers });
  }

  uploadEvidence(receiptId: string, req: EvidenceUploadRequest, fileBlob?: Blob): Observable<EvidenceUploadResponse> {
    return this.http.post<EvidenceUploadResponse>(`${this.baseUrl}/api/v1/receipts/${receiptId}/evidence-uploads`, req).pipe(
      switchMap((resp) => {
        if (!fileBlob || !resp.uploadUrl || resp.uploadUrl.includes('istpetdev.local')) {
          return throwError(() => ({ status: 503, message: 'Almacenamiento de evidencias no disponible.' }));
        }
        const headers = new HttpHeaders(resp.requiredHeaders || { 'Content-Type': req.mimeType });
        return this.http.put(resp.uploadUrl, fileBlob, { headers }).pipe(
          switchMap(() => of(resp))
        );
      })
    );
  }

  confirmEvidence(evidenceId: string, req: EvidenceConfirmRequest): Observable<EvidenceConfirmResponse> {
    return this.http.post<EvidenceConfirmResponse>(`${this.baseUrl}/api/v1/evidence/${evidenceId}/confirm`, req);
  }

  getPublicTrace(unitId: string, token: string): Observable<PublicTraceUnitDto> {
    const headers = new HttpHeaders({
      'X-Trace-Token': token
    });
    return this.http.get<PublicTraceUnitDto>(`${this.baseUrl}/api/v1/public/trace/${unitId}`, { headers });
  }

  getReceiverUnit(unitId: string, token: string): Observable<ReceiverUnit> {
    const headers = new HttpHeaders({
      'X-Trace-Token': token
    });
    return this.http.get<ReceiverUnit>(`${this.baseUrl}/api/v1/receiver/handling-units/${unitId}`, { headers });
  }

  receiveAsReceiver(unitId: string, token: string, req: CreateReceiptRequest, idempotencyKey: string): Observable<ReceiptResponse> {
    const headers = new HttpHeaders({
      'X-Trace-Token': token,
      'Idempotency-Key': idempotencyKey
    });
    return this.http.post<ReceiptResponse>(`${this.baseUrl}/api/v1/receiver/handling-units/${unitId}/receipt`, req, { headers });
  }
}

