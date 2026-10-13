import { Injectable } from '@angular/core';
import { Observable, from, of } from 'rxjs';
import { delay, switchMap } from 'rxjs/operators';
import { openDB, IDBPDatabase } from 'idb';
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
import { IWebFieldApi } from '../field-api.interface';

const DB_NAME = 'istpetdev_mock_backend_db';
const DB_VERSION = 1;
const STORE_NAME = 'server_state';
const STATE_KEY = 'current_mock_state';

function generateUuidV7(): string {
  const now = Date.now();
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  bytes[0] = (now / 0x10000000000) & 0xff;
  bytes[1] = (now / 0x100000000) & 0xff;
  bytes[2] = (now / 0x1000000) & 0xff;
  bytes[3] = (now / 0x10000) & 0xff;
  bytes[4] = (now / 0x100) & 0xff;
  bytes[5] = now & 0xff;
  bytes[6] = (bytes[6] & 0x0f) | 0x70;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

@Injectable({
  providedIn: 'root'
})
export class MockWebFieldApiService implements IWebFieldApi {
  private dbPromise: Promise<IDBPDatabase>;
  public latencyMs = 200;

  constructor() {
    this.dbPromise = openDB(DB_NAME, DB_VERSION, {
      upgrade(db) {
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          db.createObjectStore(STORE_NAME);
        }
      }
    });
  }

  private async getState(): Promise<any> {
    const db = await this.dbPromise;
    let state = await db.get(STORE_NAME, STATE_KEY);
    if (!state) {
      // Estado inicial si se abre primero la web
      state = {
        deliveries: [
          {
            deliveryId: 'del-hosp-001',
            routePlanId: 'rp-quito-sur-001',
            pointCode: 'PTO-HOSP-01',
            pointName: 'Hospital General San Francisco',
            dueAtUtc: new Date(Date.now() + 4 * 3600 * 1000).toISOString(),
            status: 'Prepared',
            evidenceStatus: 'Pending',
            packagesCount: 1
          },
          {
            deliveryId: 'del-clin-002',
            routePlanId: 'rp-quito-sur-001',
            pointCode: 'PTO-CLIN-02',
            pointName: 'Clínica Metropolitana Norte',
            dueAtUtc: new Date(Date.now() + 6 * 3600 * 1000).toISOString(),
            status: 'Planned',
            evidenceStatus: 'Pending',
            packagesCount: 0
          }
        ],
        handlingUnits: {
          'hu-caja-001': {
            handlingUnitId: 'hu-caja-001',
            code: 'CAJA-MED-001',
            deliveryId: 'del-hosp-001',
            status: 'Prepared',
            labelUrl: 'https://istpetdev.local/campo/label/hu-caja-001',
            token: 'tok-sec-demo-hosp-001',
            lines: [
              {
                deliveryLineId: 'dl-001-1',
                lotId: 'LOT-DESINF-A1',
                sku: 'SKU-AMONIO-CUAT-4L',
                quantity: 12,
                unit: 'L'
              }
            ],
            createdAtUtc: new Date().toISOString()
          }
        },
        tokens: {
          'tok-sec-demo-hosp-001': {
            unitId: 'hu-caja-001',
            token: 'tok-sec-demo-hosp-001',
            isRevoked: false,
            expiresAtUtc: new Date(Date.now() + 7 * 86400 * 1000).toISOString()
          }
        },
        idempotencyRecords: {}
      };
      await db.put(STORE_NAME, state, STATE_KEY);
    }
    return state;
  }

  private async saveState(state: any): Promise<void> {
    const db = await this.dbPromise;
    await db.put(STORE_NAME, state, STATE_KEY);
  }

  createHandlingUnit(deliveryId: string, req: CreateHandlingUnitRequest): Observable<HandlingUnitResponse> {
    return from(
      (async () => {
        const state = await this.getState();
        const huId = `hu-${generateUuidV7().slice(0, 8)}`;
        const code = `CAJA-${Math.floor(1000 + Math.random() * 9000)}`;
        const token = `tok-${generateUuidV7()}`;

        const newUnit: HandlingUnitResponse = {
          handlingUnitId: huId,
          code,
          deliveryId,
          status: 'Prepared',
          labelUrl: `https://istpetdev.local/campo/label/${huId}`,
          token,
          lines: req.lines.map((l) => ({
            deliveryLineId: l.deliveryLineId,
            lotId: l.lotId,
            sku: 'SKU-AMONIO-CUAT-4L',
            quantity: l.quantity,
            unit: 'L'
          })),
          createdAtUtc: new Date().toISOString()
        };

        state.handlingUnits = state.handlingUnits || {};
        state.handlingUnits[huId] = newUnit;

        state.tokens = state.tokens || {};
        state.tokens[token] = {
          unitId: huId,
          token,
          isRevoked: false,
          expiresAtUtc: new Date(Date.now() + 7 * 86400 * 1000).toISOString()
        };

        const delivery = state.deliveries?.find((d: any) => d.deliveryId === deliveryId);
        if (delivery) {
          delivery.status = 'Prepared';
          delivery.packagesCount = (delivery.packagesCount || 0) + 1;
        }

        await this.saveState(state);
        return newUnit;
      })()
    ).pipe(delay(this.latencyMs));
  }

  dispatch(deliveryId: string, req: DispatchRequest, idempotencyKey: string): Observable<DispatchResponse> {
    return from(
      (async () => {
        const state = await this.getState();
        state.idempotencyRecords = state.idempotencyRecords || {};

        const canonicalBody = JSON.stringify({ deliveryId, ids: [...req.handlingUnitIds].sort() });
        const existing = state.idempotencyRecords[idempotencyKey];
        if (existing) {
          if (existing.fingerprint === canonicalBody) {
            return existing.response as DispatchResponse;
          }
          throw { status: 409, code: 'IDEMPOTENCY_CONFLICT', title: 'Conflicto de idempotencia' };
        }

        for (const id of req.handlingUnitIds) {
          if (state.handlingUnits?.[id]) {
            state.handlingUnits[id].status = 'Dispatched';
          }
        }

        const delivery = state.deliveries?.find((d: any) => d.deliveryId === deliveryId);
        if (delivery) {
          delivery.status = 'Dispatched';
        }

        const response: DispatchResponse = {
          deliveryId,
          status: 'Dispatched',
          dispatchedHandlingUnits: req.handlingUnitIds,
          dispatchedAtUtc: new Date().toISOString(),
          custodyEventId: `custody-${generateUuidV7().slice(0, 8)}`
        };

        state.idempotencyRecords[idempotencyKey] = {
          fingerprint: canonicalBody,
          response
        };

        await this.saveState(state);
        return response;
      })()
    ).pipe(delay(this.latencyMs));
  }

  getDeliveries(routePlanId?: string, status?: string, cursor?: string, limit = 50): Observable<DeliveryListResponse> {
    return from(
      (async () => {
        const state = await this.getState();
        let items = state.deliveries || [];
        if (routePlanId) {
          items = items.filter((d: any) => d.routePlanId === routePlanId);
        }
        if (status) {
          items = items.filter((d: any) => d.status === status);
        }
        return {
          items: items.slice(0, limit),
          total: items.length
        };
      })()
    ).pipe(delay(this.latencyMs));
  }

  revokePublicToken(unitId: string): Observable<void> {
    return from(
      (async () => {
        const state = await this.getState();
        if (state.tokens) {
          for (const k in state.tokens) {
            if (state.tokens[k].unitId === unitId) {
              state.tokens[k].isRevoked = true;
            }
          }
        }
        await this.saveState(state);
      })()
    ).pipe(delay(this.latencyMs));
  }

  getPublicTrace(unitId: string, token: string): Observable<PublicTraceUnitDto> {
    return from(
      (async () => {
        const state = await this.getState();
        const tokRecord = state.tokens?.[token];

        if (!tokRecord || tokRecord.isRevoked || tokRecord.unitId !== unitId) {
          throw { status: 404, code: 'TOKEN_REVOKED_OR_EXPIRED', title: 'Token no válido o revocado.' };
        }

        const hu = state.handlingUnits?.[unitId];
        if (!hu) {
          throw { status: 404, code: 'NOT_FOUND', title: 'Unidad no encontrada.' };
        }

        return {
          unitId: hu.handlingUnitId,
          code: hu.code,
          status: hu.status,
          productUnits: hu.lines.map((l: any) => ({ sku: l.sku, unit: l.unit })),
          events: [
            { stage: 'Preparado en Bodega Central', timestamp: hu.createdAtUtc },
            ...(hu.status === 'Dispatched' || hu.status === 'Received' || hu.status === 'Partial'
              ? [{ stage: 'Despachado en Vehículo Logístico', timestamp: new Date().toISOString() }]
              : []),
            ...(hu.status === 'Received'
              ? [{ stage: 'Entregado en Punto de Destino', timestamp: new Date().toISOString() }]
              : [])
          ]
        };
      })()
    ).pipe(delay(this.latencyMs));
  }

  advanceDemo(routePlanId: string, req: DemoAdvanceRequest): Observable<DemoAdvanceResponse> {
    return from(
      (async () => {
        const state = await this.getState();
        const completed: any[] = [];
        const pending = (state.deliveries || []).slice(0, req.stops);
        for (const d of pending) {
          d.status = 'Delivered';
          const rcptId = `rcpt-adv-${generateUuidV7().slice(0, 8)}`;
          completed.push({
            sequence: 1,
            deliveryId: d.deliveryId,
            receiptId: rcptId,
            acceptedAtUtc: new Date().toISOString()
          });
        }
        await this.saveState(state);
        return { completedStops: completed };
      })()
    ).pipe(delay(this.latencyMs));
  }
}
