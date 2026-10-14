import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateHandlingUnitRequest,
  HandlingUnitResponse,
  DispatchRequest,
  DispatchResponse,
  DeliveryListResponse,
  DemoAdvanceRequest,
  DemoAdvanceResponse,
  PublicTraceUnitDto
} from '@istpetdev/shared-core';
import { IWebFieldApi } from './field-api.interface';
import { environment } from '../config/environment';

@Injectable({
  providedIn: 'root'
})
export class HttpWebFieldApiService implements IWebFieldApi {
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

  getDeliveries(routePlanId?: string, status?: string, cursor?: string, limit?: number): Observable<DeliveryListResponse> {
    let params = new HttpParams();
    if (routePlanId) params = params.set('routePlanId', routePlanId);
    if (status) params = params.set('status', status);
    if (cursor) params = params.set('cursor', cursor);
    if (limit) params = params.set('limit', limit.toString());

    return this.http.get<DeliveryListResponse>(`${this.baseUrl}/api/v1/deliveries`, { params });
  }

  revokePublicToken(unitId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/v1/handling-units/${unitId}/public-token/revoke`, {});
  }

  advanceDemo(routePlanId: string, req: DemoAdvanceRequest): Observable<DemoAdvanceResponse> {
    return this.http.post<DemoAdvanceResponse>(`${this.baseUrl}/api/v1/demo/route-plans/${routePlanId}/advance`, req);
  }

  getPublicTrace(unitId: string, token: string): Observable<PublicTraceUnitDto> {
    const headers = new HttpHeaders({
      'X-Trace-Token': token
    });
    return this.http.get<PublicTraceUnitDto>(`${this.baseUrl}/api/v1/public/trace/${unitId}`, { headers });
  }
}
