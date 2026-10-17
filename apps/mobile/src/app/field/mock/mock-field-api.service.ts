import { Injectable } from '@angular/core';
import { Observable, from, throwError, of } from 'rxjs';
import { delay, switchMap } from 'rxjs/operators';
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
  FieldProblemDetails,
  TodayRoutePlanDto,
  TodayRouteStopDto
} from '@istpetdev/shared-core';
import { IFieldApi, ReceiverUnit } from '../field-api.interface';
import { MockServerStore } from './mock-server.store';

import { generateUuidV7 } from '../uuid';

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
          status: 'Sealed',
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
          }
        }
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

        // #162: solo una entrega Draft con bultos sellados puede despacharse.
        if (delivery.status !== 'Draft') {
          throw this.createProblemDetails('INVALID_STATE_TRANSITION', 409,
            `Solo una entrega Draft puede despacharse; la entrega está en ${delivery.status}.`);
        }
        if (!req.handlingUnitIds.length) {
          throw this.createProblemDetails('NO_HANDLING_UNITS', 409,
            'El despacho debe incluir al menos un bulto preparado por Bodega.');
        }

        // 2. Marcar unidades despachadas
        for (const huId of req.handlingUnitIds) {
          const hu = state.handlingUnits[huId];
          if (!hu || (hu.status !== 'Sealed' && hu.status !== 'Prepared')) {
            throw this.createProblemDetails('HANDLING_UNIT_STATE_CONFLICT', 409,
              'Una o más cajas no están preparadas para despacho.');
          }
        }
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

  // #162: custodia del conductor en el mock: guardas de estado e idempotencia como el backend.
  pickup(deliveryId: string, req: CustodyTransitionRequest, idempotencyKey: string): Observable<CustodyTransitionResponse> {
    return this.advanceCustody(deliveryId, req, idempotencyKey, 'PICKUP', 'Dispatched', 'InTransit');
  }

  deliver(deliveryId: string, req: CustodyTransitionRequest, idempotencyKey: string): Observable<CustodyTransitionResponse> {
    return this.advanceCustody(deliveryId, req, idempotencyKey, 'DELIVER', 'InTransit', 'Delivered');
  }

  private advanceCustody(
    deliveryId: string,
    req: CustodyTransitionRequest,
    idempotencyKey: string,
    action: 'PICKUP' | 'DELIVER',
    fromStatus: 'Dispatched' | 'InTransit',
    toStatus: 'InTransit' | 'Delivered'
  ): Observable<CustodyTransitionResponse> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();

        const existingRecord = state.idempotencyRecords[idempotencyKey];
        if (existingRecord) {
          if (existingRecord.action === action && existingRecord.operationId === req.operationId) {
            return existingRecord.responseBody as CustodyTransitionResponse;
          }
          throw this.createProblemDetails('IDEMPOTENCY_CONFLICT', 409,
            'Conflicto de idempotencia: el operationId ya fue utilizado con datos distintos.');
        }

        const delivery = state.deliveries.find((d) => d.deliveryId === deliveryId);
        if (!delivery) {
          throw this.createProblemDetails('NOT_FOUND', 404, `Entrega ${deliveryId} no encontrada.`);
        }
        if (delivery.status !== fromStatus) {
          const code = delivery.status === 'Draft' ? 'NOT_DISPATCHED'
            : delivery.status === 'Received' || delivery.status === 'Partial' ? 'ALREADY_RECEIVED'
            : 'INVALID_STATE_TRANSITION';
          throw this.createProblemDetails(code, 409,
            `Transición inválida: ${action === 'PICKUP' ? 'solo una entrega despachada puede recogerse' : 'solo una entrega en ruta puede marcarse como entregada'} (estado actual ${delivery.status}).`);
        }
        const unitIds = Object.values(state.handlingUnits)
          .filter((hu) => hu.deliveryId === deliveryId)
          .map((hu) => hu.handlingUnitId);
        if (unitIds.length === 0) {
          throw this.createProblemDetails('NO_HANDLING_UNITS', 409,
            'La entrega no tiene bultos preparados por Bodega.');
        }
        for (const huId of unitIds) {
          if (state.handlingUnits[huId].status !== fromStatus) {
            throw this.createProblemDetails('HANDLING_UNIT_STATE_CONFLICT', 409,
              'Uno o más bultos no están en el estado esperado.');
          }
        }

        for (const huId of unitIds) {
          state.handlingUnits[huId].status = toStatus;
        }
        delivery.status = toStatus;
        for (const route of state.assignment.routes) {
          const d = route.deliveries.find((x) => x.deliveryId === deliveryId);
          if (d) {
            d.status = toStatus;
            d.handlingUnits.forEach((hu) => {
              if (state.handlingUnits[hu.handlingUnitId]) hu.status = toStatus;
            });
          }
        }

        const response: CustodyTransitionResponse = {
          deliveryId,
          status: toStatus,
          handlingUnitIds: unitIds,
          changedAtUtc: new Date().toISOString(),
          custodyEventId: `custody-${generateUuidV7().slice(0, 8)}`
        };
        state.idempotencyRecords[idempotencyKey] = {
          action,
          operationId: req.operationId,
          requestFingerprint: `${action}:${deliveryId}`,
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

        // #162: el conductor ya no confirma stock en destino; solo el receptor con el bulto entregado.
        const delivery = state.deliveries.find((d) => d.deliveryId === deliveryId);
        if (!delivery) {
          throw this.createProblemDetails('NOT_FOUND', 404, `Entrega ${deliveryId} no encontrada.`);
        }
        if (delivery.status === 'Draft') {
          throw this.createProblemDetails('NOT_DISPATCHED', 409, 'La entrega aún no fue despachada por Bodega.');
        }
        if (delivery.status === 'Dispatched' || delivery.status === 'InTransit') {
          throw this.createProblemDetails('NOT_DELIVERED', 409, 'El Conductor aún no entrega este bulto en el punto.');
        }
        if (delivery.status === 'Received') {
          throw this.createProblemDetails('ALREADY_RECEIVED', 409, 'La entrega ya tiene un recibo final.');
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
        const deliveryStatus = totalRejected > 0 ? 'Partial' : 'Received';
        delivery.status = deliveryStatus;
        delivery.evidenceStatus = 'Pending';

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
        const reached = (min: string): boolean => {
          const order = ['Sealed', 'Prepared', 'Dispatched', 'InTransit', 'Delivered', 'Received', 'Partial'];
          return order.indexOf(hu.status) >= order.indexOf(min);
        };
        const trace: PublicTraceUnitDto = {
          unitId: hu.handlingUnitId,
          code: hu.code,
          status: hu.status,
          productUnits: hu.lines.map((l) => ({ sku: l.sku, unit: l.unit })),
          events: [
            { stage: 'Preparada', timestamp: hu.createdAtUtc },
            ...(reached('Dispatched') ? [{ stage: 'Despachada', timestamp: new Date().toISOString() }] : []),
            ...(reached('InTransit') ? [{ stage: 'En ruta', timestamp: new Date().toISOString() }] : []),
            ...(reached('Delivered') ? [{ stage: 'Entregada', timestamp: new Date().toISOString() }] : []),
            ...(hu.status === 'Received' || hu.status === 'Partial'
              ? [{ stage: hu.status === 'Partial' ? 'Recibida Parcial' : 'Recibida', timestamp: new Date().toISOString() }]
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
          handlingUnits,
          confirmedEvidenceCount: 0
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
        let checkpointCompleted = 'Avance simulado';
        let nextCheckpoint = 'Ruta finalizada';

        for (const route of state.assignment.routes) {
          if (route.routePlanId === routePlanId) {
            const pendingDeliveries = route.deliveries.filter((d) => d.status !== 'Received' && d.status !== 'Partial');
            let toProcess = pendingDeliveries;
            if (req.mode === 'half') {
              const count = Math.ceil(pendingDeliveries.length / 2);
              toProcess = pendingDeliveries.slice(0, count);
            } else if (req.mode === 'next') {
              toProcess = pendingDeliveries.slice(0, 1);
            } else if (!req.mode && req.stops) {
              toProcess = pendingDeliveries.slice(0, req.stops);
            }

            for (const d of toProcess) {
              if (d.handlingUnits.length === 0) {
                d.handlingUnits.push({
                  handlingUnitId: `hu-${generateUuidV7().slice(0, 8)}`,
                  code: `CAJA-${Math.floor(1000 + Math.random() * 9000)}`,
                  deliveryId: d.deliveryId,
                  status: 'Received',
                  labelUrl: '',
                  token: `tok-${generateUuidV7()}`,
                  lines: [],
                  createdAtUtc: new Date().toISOString()
                });
              } else {
                d.handlingUnits.forEach((hu) => (hu.status = 'Received'));
              }
              d.status = 'Received';
              const rcptId = `rcpt-adv-${generateUuidV7().slice(0, 8)}`;
              completed.push({
                sequence: d.sequence,
                deliveryId: d.deliveryId,
                receiptId: rcptId,
                acceptedAtUtc: new Date().toISOString()
              });
            }

            const remaining = route.deliveries.filter((d) => d.status !== 'Received' && d.status !== 'Partial');
            nextCheckpoint = remaining.length === 0 ? 'Ruta finalizada' : `Parada ${remaining[0].sequence}: ${remaining[0].pointName}`;
            checkpointCompleted = req.mode === 'next' ? `Parada completada` : `${completed.length} paradas procesadas`;
          }
        }

        await this.store.saveState(state);
        return {
          completedStops: completed,
          checkpointCompleted,
          nextCheckpoint,
          totalStops: completed.length,
          completedStopsCount: completed.length,
          status: 'Success'
        };
      })()
    ).pipe(delay(this.latencyMs));
  }

  getTodayRoutePlans(): Observable<TodayRoutePlanDto[]> {
    return from(
      (async () => {
        this.checkFailureHooks();
        const state = await this.store.getState();
        const routes: TodayRoutePlanDto[] = (state.assignment.routes || []).map((r) => {
          const totalStops = r.deliveries.length;
          const completedStops = r.deliveries.filter((d) => d.status === 'Received' || d.status === 'Partial').length;
          let status = 'Published';
          if (completedStops === totalStops && totalStops > 0) {
            status = 'Completed';
          } else if (completedStops > 0) {
            status = 'InProgress';
          }
          const stops: TodayRouteStopDto[] = r.deliveries.map((d) => ({
            stopId: d.deliveryId,
            sequenceNumber: d.sequence,
            kind: d.kind || 'Delivery',
            pointName: d.pointName,
            status: (d.status === 'Received' || d.status === 'Partial') ? 'Completed' : 'Pending',
            plannedArrivalUtc: new Date().toISOString(),
            actualArrivalUtc: (d.status === 'Received' || d.status === 'Partial') ? new Date().toISOString() : undefined
          }));
          return {
            routePlanId: r.routePlanId,
            code: r.routePlanId,
            status,
            vehicleCode: r.vehicleCode,
            driverName: 'Conductor Demo',
            totalStops,
            completedStops,
            nextCheckpoint: completedStops >= totalStops ? 'Ruta finalizada' : `Parada ${completedStops + 1}`,
            plannedStartUtc: new Date().toISOString(),
            stops
          };
        });
        return routes;
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
