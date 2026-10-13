import { InjectionToken } from '@angular/core';
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

/**
 * Puerto de API para operaciones de Campo y Bodega en apps/web.
 */
export interface IWebFieldApi {
  createHandlingUnit(deliveryId: string, req: CreateHandlingUnitRequest): Observable<HandlingUnitResponse>;
  dispatch(deliveryId: string, req: DispatchRequest, idempotencyKey: string): Observable<DispatchResponse>;
  getDeliveries(routePlanId?: string, status?: string, cursor?: string, limit?: number): Observable<DeliveryListResponse>;
  revokePublicToken(unitId: string): Observable<void>;
  advanceDemo(routePlanId: string, req: DemoAdvanceRequest): Observable<DemoAdvanceResponse>;
  getPublicTrace(unitId: string, token: string): Observable<PublicTraceUnitDto>;
}

export const WEB_FIELD_API = new InjectionToken<IWebFieldApi>('WEB_FIELD_API');
