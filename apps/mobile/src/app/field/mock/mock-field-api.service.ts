import { Injectable } from '@angular/core';
import { Observable, from, throwError, of } from 'rxjs';
import { delay, switchMap } from 'rxjs/operators';
import {
  CreateHandlingUnitRequest,
  HandlingUnitResponse,
  DispatchRequest,
  DispatchResponse,
  MobileAssignmentResponse,
  DeliveryListResponse,
  DeliveryDetailResponse,
  DeliveryDetailLineDto,
  CreateReceiptRequest,
  ReceiptResponse,
  EvidenceUploadRequest,
  EvidenceUploadResponse,
  EvidenceConfirmRequest,
  EvidenceConfirmResponse,
  EvidenceStatus,
  DemoAdvanceRequest,
  DemoAdvanceResponse,
  PublicTraceUnitDto,
  FieldProblemDetails
} from '@istpetdev/shared-core';
import { IFieldApi, ReceiverUnit } from '../field-api.interface';
import { MockServerStore } from './mock-server.store';

/**
 * Generador simple de UUID v7 estándar (RFC 9562) en cliente sin librerías externas.
 */
export function generateUuidV7(): string {
  const now = Date.now();
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);

  // Timestamp de 48 bits
  bytes[0] = (now / 0x10000000000) & 0xff;
  bytes[1] = (now / 0x100000000) & 0xff;
  bytes[2] = (now / 0x1000000) & 0xff;
  bytes[3] = (now / 0x10000) & 0xff;
  bytes[4] = (now / 0x100) & 0xff;
  bytes[5] = now & 0xff;

  // Versión 7 (0b0111) en bits 48..51
  bytes[6] = (bytes[6] & 0x0f) | 0x70;
  // Variante RFC (0b10) en bits 64..65
  bytes[8] = (bytes[8] & 0x3f) | 0x80;

  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

@Injectable({
  providedIn: 'root'
})
export class MockFieldApiService implements IFieldApi {
  private store = new MockServerStore();

  // Interruptores de fallo e inyección para pruebas del equipo
  public latencyMs = 200;
  public simulateNetworkFailure = false;
  public simulate401Unauthorized = false;
  public simulate429RateLimit = false;
  public simulate503ServiceUnavailable = false;
  public simulateS3Failure = false;
  public simulate409Conflict = false;
  public simulate422Unprocessable = false;
  public simulate500InternalError = false;
  public simulateAccountDeactivated = false;

  private createProblemDetails(code: string, status: number, title: string, detail?: string): FieldProblemDetails {
    return {
      type: `https://istpetdev.local/errors/${code.toLowerCase()}`,
      title,
      status,
      detail: detail || title,
      code,
      correlationId: generateUuidV7()
    };
  }

  private checkFailureHooks(): void {
    if (this.simulateNetworkFailure) {
      throw new Error('NETWORK_CONNECTION_LOST: No hay conexión con el servidor.');
    }
    if (this.simulate401Unauthorized) {
      throw this.createProblemDetails('UNAUTHORIZED', 401, 'Sesión expirada o no autorizada.');
    }
    if (this.simulateAccountDeactivated) {
      throw this.createProblemDetails('TOKEN_REVOKED_OR_EXPIRED', 403, 'La cuenta del conductor está inactiva o ha sido desactivada.');
    }
    if (this.simulate409Conflict) {
      throw this.createProblemDetails('ASSIGNMENT_CHANGED', 409, 'Conflicto de asignación: versión desactualizada.');
    }
    if (this.simulate422Unprocessable) {
      throw this.createProblemDetails('QUANTITY_EXCEEDED', 422, 'Cantidades no procesables: regla de validación de negocio violada.');
    }
    if (this.simulate429RateLimit) {
      throw this.createProblemDetails('RATE_LIMIT_EXCEEDED', 429, 'Demasiadas solicitudes. Intente más tarde.');
    }
    if (this.simulate500InternalError) {
      throw this.createProblemDetails('INTERNAL_SERVER_ERROR', 500, 'Error interno del servidor.');
    }
    if (this.simulate503ServiceUnavailable) {
      throw this.createProblemDetails('SERVICE_UNAVAILABLE', 503, 'El servicio de API no está disponible temporalmente.');
    }
  }

  getAssignments(): Observable<MobileAssignmentResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        return state.assignment;
      })()
    ).pipe(delay(this.latencyMs));
  }

  createHandlingUnit(deliveryId: string, req: CreateHandlingUnitRequest): Observable<HandlingUnitResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        const delivery = state.deliveries.find((d) => d.deliveryId === deliveryId);
        if (!delivery) {
          throw this.createProblemDetails('NOT_FOUND', 404, `Entrega ${deliveryId} no encontrada.`);
        }

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

        state.handlingUnits[huId] = newUnit;
        state.tokens[token] = {
          unitId: huId,
          token,
          isRevoked: false,
          expiresAtUtc: new Date(Date.now() + 7 * 86400 * 1000).toISOString()
        };

        // Asociar a la asignación
        for (const route of state.assignment.routes) {
          const d = route.deliveries.find((x) => x.deliveryId === deliveryId);
          if (d) {
            d.handlingUnits.push(newUnit);
            d.status = 'Prepared';
          }
        }
        delivery.status = 'Prepared';
        delivery.packagesCount = (delivery.packagesCount || 0) + 1;

        await this.store.saveState(state);
        return newUnit;
      })()
    ).pipe(delay(this.latencyMs));
  }

  dispatch(deliveryId: string, req: DispatchRequest, idempotencyKey: string): Observable<DispatchResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();

        // 1. Verificar idempotencia
        const canonicalBody = JSON.stringify({ deliveryId, ids: [...req.handlingUnitIds].sort() });
        const existingRecord = state.idempotencyRecords[idempotencyKey];
        if (existingRecord) {
          if (existingRecord.requestFingerprint === canonicalBody) {
            // Replay exacto
            return existingRecord.responseBody as DispatchResponse;
          }
          throw this.createProblemDetails(
            'IDEMPOTENCY_CONFLICT',
            409,
            'Conflicto de idempotencia: misma clave con diferente contenido de solicitud.'
          );
        }

        const delivery = state.deliveries.find((d) => d.deliveryId === deliveryId);
        if (!delivery) {
          throw this.createProblemDetails('NOT_FOUND', 404, `Entrega ${deliveryId} no encontrada.`);
        }

        // 2. Marcar unidades despachadas
        for (const huId of req.handlingUnitIds) {
          const hu = state.handlingUnits[huId];
          if (hu) {
            hu.status = 'Dispatched';
          }
        }

        delivery.status = 'Dispatched';
        for (const route of state.assignment.routes) {
          const d = route.deliveries.find((x) => x.deliveryId === deliveryId);
          if (d) {
            d.status = 'Dispatched';
            d.handlingUnits.forEach((hu) => {
              if (req.handlingUnitIds.includes(hu.handlingUnitId)) {
                hu.status = 'Dispatched';
              }
            });
          }
        }

        const response: DispatchResponse = {
          deliveryId,
          status: 'Dispatched',
          dispatchedHandlingUnits: req.handlingUnitIds,
          dispatchedAtUtc: new Date().toISOString(),
          custodyEventId: `custody-${generateUuidV7().slice(0, 8)}`
        };

        state.idempotencyRecords[idempotencyKey] = {
          action: 'DISPATCH',
          operationId: req.operationId,
          requestFingerprint: canonicalBody,
          responseStatusCode: 200,
          responseBody: response,
          recordedAtUtc: new Date().toISOString()
        };

        await this.store.saveState(state);
        return response;
      })()
    ).pipe(delay(this.latencyMs));
  }

  createReceipt(deliveryId: string, req: CreateReceiptRequest, idempotencyKey: string): Observable<ReceiptResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();

        // 1. Verificación de Idempotencia estricta
        const canonicalBody = JSON.stringify({
          deliveryId,
          version: req.expectedDeliveryVersion,
          lines: req.lines.map((l) => ({ dl: l.deliveryLineId, a: l.quantityAccepted, r: l.quantityRejected }))
        });

        const existingRecord = state.idempotencyRecords[idempotencyKey];
        if (existingRecord) {
          if (existingRecord.requestFingerprint === canonicalBody) {
            // Replay transparente idempotente
            return existingRecord.responseBody as ReceiptResponse;
          }
          throw this.createProblemDetails(
            'IDEMPOTENCY_CONFLICT',
            409,
            'Conflicto de idempotencia: el operationId ya fue utilizado con datos distintos.'
          );
        }

        // 2. Validación de versión y asignación
        if (req.expectedDeliveryVersion > 3) {
          throw this.createProblemDetails(
            'ASSIGNMENT_CHANGED',
            409,
            'La entrega fue reasignada a otro vehículo o revisión de ruta.'
          );
        }

        // 3. Validación de cantidades (Suma no puede exceder lo despachado)
        let totalAccepted = 0;
        let totalRejected = 0;
        for (const line of req.lines) {
          totalAccepted += line.quantityAccepted;
          totalRejected += line.quantityRejected;
        }

        // Límite de prueba: 12 Litros máximo para la entrega del hospital
        if (totalAccepted + totalRejected > 12) {
          throw this.createProblemDetails(
            'QUANTITY_EXCEEDED',
            409,
            `Las cantidades (${totalAccepted + totalRejected} L) exceden el saldo pendiente despachado.`
          );
        }

        // 4. Actualización del estado
        const deliveryStatus = totalRejected > 0 ? 'Partial' : 'Delivered';
        const delivery = state.deliveries.find((d) => d.deliveryId === deliveryId);
        if (delivery) {
          delivery.status = deliveryStatus;
          delivery.evidenceStatus = 'Pending';
        }

        for (const route of state.assignment.routes) {
          const d = route.deliveries.find((x) => x.deliveryId === deliveryId);
          if (d) {
            d.status = deliveryStatus;
            d.handlingUnits.forEach((hu) => {
              hu.status = totalRejected > 0 ? 'Partial' : 'Received';
            });
          }
        }

        const receiptResponse: ReceiptResponse = {
          receiptId: `rcpt-${generateUuidV7().slice(0, 8)}`,
          deliveryId,
          acceptedAtServer: new Date().toISOString(),
          deliveryVersion: req.expectedDeliveryVersion + 1,
          deliveryStatus,
          evidenceStatus: 'Pending',
          lines: req.lines.map((l) => ({
            deliveryLineId: l.deliveryLineId,
            lotId: l.lotId,
            quantityAccepted: l.quantityAccepted,
            quantityRejected: l.quantityRejected,
            unit: l.unit
          }))
        };

        state.idempotencyRecords[idempotencyKey] = {
          action: 'RECEIPT',
          operationId: req.operationId,
          requestFingerprint: canonicalBody,
          responseStatusCode: 200,
          responseBody: receiptResponse,
          recordedAtUtc: new Date().toISOString()
        };

        await this.store.saveState(state);
        return receiptResponse;
      })()
    ).pipe(delay(this.latencyMs));
  }

  uploadEvidence(receiptId: string, req: EvidenceUploadRequest, fileBlob?: Blob): Observable<EvidenceUploadResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        if (this.simulateS3Failure) {
          throw this.createProblemDetails(
            'S3_STORAGE_UNAVAILABLE',
            503,
            'Fallo temporal de almacenamiento en S3. El recibo permanece confirmado, reintente la evidencia.'
          );
        }

        const state = await this.store.getState();
        const evidenceId = `ev-${generateUuidV7().slice(0, 8)}`;

        state.evidenceFiles[evidenceId] = {
          evidenceId,
          receiptId,
          kind: req.kind,
          mimeType: req.mimeType,
          byteSize: req.byteSize,
          sha256: req.sha256,
          status: 'Uploaded'
        };

        await this.store.saveState(state);

        return {
          evidenceId,
          uploadUrl: `https://s3.istpetdev.local/evidences/${evidenceId}`,
          expiresAtUtc: new Date(Date.now() + 15 * 60 * 1000).toISOString()
        };
      })()
    ).pipe(delay(this.latencyMs));
  }

  confirmEvidence(evidenceId: string, req: EvidenceConfirmRequest): Observable<EvidenceConfirmResponse> {
    return from(
      (async (): Promise<EvidenceConfirmResponse> => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        const ev = state.evidenceFiles[evidenceId];
        if (!ev) {
          throw this.createProblemDetails('NOT_FOUND', 404, `Evidencia ${evidenceId} no encontrada.`);
        }

        ev.status = 'Confirmed';
        ev.confirmedAtUtc = new Date().toISOString();

        await this.store.saveState(state);

        return {
          evidenceId,
          status: 'Confirmed' as EvidenceStatus,
          confirmedAtUtc: ev.confirmedAtUtc
        };
      })()
    ).pipe(delay(this.latencyMs));
  }

  getPublicTrace(unitId: string, token: string): Observable<PublicTraceUnitDto> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        const tokRecord = state.tokens[token];

        // Token inexistente, revocado o para otra unidad -> respuesta 404 controlada
        if (!tokRecord || tokRecord.isRevoked || tokRecord.unitId !== unitId) {
          throw this.createProblemDetails(
            'TOKEN_REVOKED_OR_EXPIRED',
            404,
            'Trazabilidad no disponible. El código QR es inválido o fue revocado.'
          );
        }

        const hu = state.handlingUnits[unitId];
        if (!hu) {
          throw this.createProblemDetails('NOT_FOUND', 404, 'Unidad logística no encontrada.');
        }

        // Proyección pública limitada (sin actores, cantidades privadas ni firmas)
        const trace: PublicTraceUnitDto = {
          unitId: hu.handlingUnitId,
          code: hu.code,
          status: hu.status,
          productUnits: hu.lines.map((l) => ({ sku: l.sku, unit: l.unit })),
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

        return trace;
      })()
    ).pipe(delay(this.latencyMs));
  }

  revokePublicToken(unitId: string): Observable<void> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        for (const k in state.tokens) {
          if (state.tokens[k].unitId === unitId) {
            state.tokens[k].isRevoked = true;
          }
        }
        await this.store.saveState(state);
      })()
    ).pipe(delay(this.latencyMs));
  }

  getDeliveries(routePlanId?: string, status?: string, cursor?: string, limit = 50): Observable<DeliveryListResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        let items = [...state.deliveries];
        if (routePlanId) {
          items = items.filter((d) => d.routePlanId === routePlanId);
        }
        if (status) {
          items = items.filter((d) => d.status === status);
        }
        return {
          items: items.slice(0, limit),
          total: items.length
        };
      })()
    ).pipe(delay(this.latencyMs));
  }

  getDelivery(deliveryId: string): Observable<DeliveryDetailResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        const delivery = state.deliveries.find((d) => d.deliveryId === deliveryId);
        if (!delivery) {
          throw this.createProblemDetails('UNIT_NOT_FOUND', 404, `Entrega ${deliveryId} no encontrada.`);
        }

        const handlingUnits = Object.values(state.handlingUnits)
          .filter((hu) => hu.deliveryId === deliveryId)
          .map((hu) => ({
            handlingUnitId: hu.handlingUnitId,
            code: hu.code,
            status: hu.status
          }));

        const existingLines: DeliveryDetailLineDto[] = [
          {
            deliveryLineId: `dl-${deliveryId}-01`,
            lotId: `lot-${deliveryId}-01`,
            lotCode: 'LOT-2026-MED-01',
            sku: 'MED-PARACETAMOL-500MG',
            productName: 'Paracetamol 500mg Comprimidos',
            plannedQuantity: 100,
            unit: 'caja'
          },
          {
            deliveryLineId: `dl-${deliveryId}-02`,
            lotId: `lot-${deliveryId}-02`,
            lotCode: 'LOT-2026-SUERO-02',
            sku: 'SUERO-FISIOLOGICO-500ML',
            productName: 'Solución Fisiológica 0.9% 500ml',
            plannedQuantity: 50,
            unit: 'frasco'
          }
        ];

        return {
          deliveryId: delivery.deliveryId,
          status: delivery.status,
          version: 1,
          pointName: delivery.pointName,
          routePlanId: delivery.routePlanId,
          lines: existingLines,
          handlingUnits
        };
      })()
    ).pipe(delay(this.latencyMs));
  }

  advanceDemo(routePlanId: string, req: DemoAdvanceRequest): Observable<DemoAdvanceResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        const completed: any[] = [];

        for (const route of state.assignment.routes) {
          if (route.routePlanId === routePlanId) {
            const pendingDeliveries = route.deliveries.slice(0, req.stops);
            for (const d of pendingDeliveries) {
              d.status = 'Delivered';
              d.handlingUnits.forEach((hu) => (hu.status = 'Received'));
              const rcptId = `rcpt-adv-${generateUuidV7().slice(0, 8)}`;
              completed.push({
                sequence: d.sequence,
                deliveryId: d.deliveryId,
                receiptId: rcptId,
                acceptedAtUtc: new Date().toISOString()
              });
            }
          }
        }

        await this.store.saveState(state);
        return { completedStops: completed };
      })()
    ).pipe(delay(this.latencyMs));
  }

  // Método utilitario para restablecer la base de datos simulada
  resetMockData(): Observable<void> {
    return from(this.store.resetToInitial()).pipe(
      switchMap(() => of(undefined)),
      delay(50)
    );
  }

  getReceiverUnit(_unitId: string, _token: string): Observable<ReceiverUnit> {
    return throwError(() => new Error('no disponible en mock'));
  }

  receiveAsReceiver(_unitId: string, _token: string, _req: CreateReceiptRequest, _idempotencyKey: string): Observable<ReceiptResponse> {
    return throwError(() => new Error('no disponible en mock'));
  }
}
