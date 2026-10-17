import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map, Observable, of, tap, throwError } from 'rxjs';
import { environment } from '../config/environment';
import {
  HealthStatusDto,
  PublicTraceResponseDto,
  ServicePoint,
  RejectedCaptureDto,
  RejectedCaptureLineDto,
  RejectedCapturesResponse,
  ReturnGuideDto,
  ReturnGuideLineDto,
  GenerateReturnGuideRequest,
  ReceiveReturnGuideRequest,
  ReceiveReturnGuideResponse
} from '@istpetdev/shared-core';
export type {
  ServicePoint,
  RejectedCaptureDto,
  RejectedCaptureLineDto,
  RejectedCapturesResponse,
  ReturnGuideDto,
  ReturnGuideLineDto,
  GenerateReturnGuideRequest,
  ReceiveReturnGuideRequest,
  ReceiveReturnGuideResponse
};

export interface RiskItemDto {
  servicePointId: string;
  servicePointCode: string;
  criticality?: string;
  productId: string;
  sku: string;
  baseUnit?: string;
  availableQuantity: number;
  inTransitQuantity?: number;
  dailyDemand: number;
  coverageDays: number;
  leadTimeDays: number;
  safetyMarginDays: number;
  suggestedQuantity: number;
  level: string;
}

export interface ImportIssueDto {
  entity: string;
  row: number;
  field?: string;
  severity: string;
  code: string;
  message: string;
  file?: string;
}

export interface EntityImportReportDto {
  entity: string;
  file?: string;
  rowsRead: number;
  created: number;
  updated: number;
  skipped: number;
  duplicates: number;
}

export interface ImportReportDto {
  batchId: string;
  dryRun: boolean;
  applied: boolean;
  errorCount: number;
  warningCount: number;
  entities: EntityImportReportDto[];
  issues: ImportIssueDto[];
}

export interface RisksResponseDto {
  items: RiskItemDto[];
  facts: any[];
}

export interface RoutePlanSummaryDto {
  id: string;
  code: string;
  revision: number;
  status: string;
  vehicleId: string;
  vehicleCode?: string;
  plannedStartUtc: string;
  totalDistanceKm: number;
  totalDurationMinutes: number;
  stopsCount: number;
  driverName?: string;
  driverAccountId?: string | null;
}

export interface DriverOptionDto {
  accountId: string;
  displayName: string;
}

export interface RouteGeometryWaypointDto {
  code: string;
  name: string;
  latitude: number;
  longitude: number;
  sequence: number;
}

/** Recorrido para el mapa: `osrm` por calles; `approximate` rectas entre paradas (sin servicio de rutas). */
export interface RouteGeometryDto {
  routePlanId: string;
  vehicleCode: string;
  source: 'osrm' | 'approximate';
  roadKm: number | null;
  coordinates: [number, number][];
  depot: RouteGeometryWaypointDto | null;
  stops: RouteGeometryWaypointDto[];
}

export interface RouteStopLineDto {
  deliveryId?: string | null;
  deliveryCode?: string | null;
  deliveryStatus?: string;
  productId: string;
  sku: string;
  productName: string;
  lotId?: string | null;
  lotCode?: string;
  lotExpiresOn?: string | null;
  plannedQuantity: number;
  dispatchedQuantity?: number;
  acceptedQuantity?: number;
  rejectedQuantity?: number;
  baseUnit: string;
  handlingUnitId?: string | null;
  handlingUnitCode?: string;
  handlingUnitStatus?: string;
}

/** #199: cantidades de una línea: lo requerido (sugerido o pedido), lo planificado en la ruta y lo pendiente. */
export interface PendingLoadLineDto {
  sku: string;
  baseUnit: string;
  requiredQuantity: number;
  plannedQuantity: number;
  pendingQuantity: number;
  pointCode?: string;
  orderNumber?: string;
}

/** #199: resume las líneas con remanente («SKU: 100 de 150 u, pendientes 50»); vacío si no queda nada pendiente. */
export function describePendingLines(lines: PendingLoadLineDto[] | null | undefined): string {
  const pending = (lines || []).filter(l => Number(l.pendingQuantity) > 0);
  if (pending.length === 0) return '';
  return pending
    .map(l => `${l.pointCode ? l.pointCode + ' ' : ''}${l.sku}: ${l.plannedQuantity} de ${l.requiredQuantity} ${l.baseUnit} planificadas, pendientes ${l.pendingQuantity}`)
    .join('; ');
}

export interface RouteStopHandlingUnitDto {
  id: string;
  code: string;
  status: string;
  locationId?: string;
  sealedAtUtc?: string | null;
}

export interface RouteProductSummaryDto {
  sku: string;
  productName: string;
  totalPlannedQuantity: number;
  baseUnit: string;
  stopsCount: number;
  lots: Array<{ lotCode: string; quantity: number }>;
  deliveryStatuses: string[];
}

export interface RouteDriverAssignmentDto {
  id: string;
  routePlanId: string;
  code: string;
  revision: number;
  status: string;
  vehicleCode: string;
  plannedStartUtc: string;
  totalDistanceKm: number;
  totalDurationMinutes: number;
  stopsCount: number;
  isCurrent: boolean;
}

export interface RoutePlanDetailDto {
  id: string;
  code: string;
  revision: number;
  status: string;
  vehicleId: string;
  vehicleCode: string;
  plannedStartUtc: string;
  plannedEndUtc?: string;
  totalDistanceKm: number;
  totalDurationMinutes: number;
  polyline: string;
  originWarehouseCode?: string;
  originWarehouseName?: string;
  driverAccountId?: string | null;
  driverName?: string;
  deliveriesCount?: number;
  unassignedDeliveries?: number;
  stops: Array<{
    sequenceNumber?: number;
    sequence: number;
    /** Pickup: recogida en bodega (lines = lo que se carga); Delivery: entrega. */
    kind?: 'Pickup' | 'Delivery' | string;
    servicePointId: string;
    servicePointCode: string;
    servicePointName: string;
    etaUtc: string;
    plannedArrivalUtc?: string;
    plannedDepartureUtc?: string;
    actualArrivalUtc?: string | null;
    serviceMinutes?: number;
    windowStart?: string;
    windowEnd?: string;
    status: string;
    ordersCount?: number;
    deliveryId?: string | null;
    deliveryCode?: string;
    deliveryStatus?: string;
    handlingUnits?: RouteStopHandlingUnitDto[];
    lines?: RouteStopLineDto[];
    // #199: requerida, planificada y pendiente por línea (rutas guardadas con atención parcial).
    loadQuantities?: PendingLoadLineDto[];
  }>;
  productsSummary?: RouteProductSummaryDto[];
  driverRoutes?: RouteDriverAssignmentDto[];
}

export interface CreateManualRoutePlanRequest {
  code?: string;
  vehicleId?: string;
  totalDistanceKm: number;
  totalDurationMinutes: number;
  plannedStartUtc?: string;
  polyline?: string;
  coordinates?: [number, number][];
  stops: Array<{
    servicePointId: string;
    code?: string;
    name?: string;
    sequence: number;
    plannedArrivalUtc?: string;
  }>;
}

export interface DeliveryOrderDto {
  id: string;
  orderNumber: string;
  destinationServicePointId?: string;
  destinationLocationId?: string;
  destinationCode: string;
  destinationName: string;
  isWarehouse: boolean;
  requiredDateUtc: string;
  priority: string;
  status: string;
  sourceType: string;
  notes?: string;
  createdAtUtc: string;
  lines: Array<{
    id: string;
    productId: string;
    sku: string;
    productName: string;
    requestedQuantity: number;
    plannedQuantity: number;
    deliveredQuantity: number;
  }>;
}

export interface WarehouseAnalysisDto {
  locationId: string;
  locationCode: string;
  productId: string;
  sku: string;
  productName: string;
  totalConsumedInWindow: number;
  windowDays: number;
  dailyConsumptionRate: number;
  monthlyConsumptionRate: number;
  quantityOnHand: number;
  quantityReserved: number;
  availableQuantity: number;
  coverageDays: number;
  confidenceLevel: string;
  minStock: number;
  maxStock: number;
  status: 'Normal' | 'Critico' | 'Exceso' | 'SinStock';
}

export interface WarehouseStockDto {
  locationId: string;
  locationCode: string;
  locationName?: string;
  productId: string;
  sku: string;
  productName: string;
  quantityOnHand: number;
  quantityReserved: number;
  availableQuantity: number;
  coverageDays?: number;
  minStock?: number;
  maxStock?: number;
  status: 'Normal' | 'Critico' | 'Exceso' | 'SinStock' | string;
}

export interface TransferSuggestionDto {
  id: string;
  sourceLocationId: string;
  sourceLocationCode: string;
  sourceLocationName: string;
  targetLocationId: string;
  targetLocationCode: string;
  targetLocationName: string;
  productId: string;
  sku: string;
  productName: string;
  suggestedQuantity: number;
  acceptedQuantity?: number;
  sourceCoverageDaysBefore: number;
  sourceCoverageDaysAfter: number;
  targetCoverageDaysBefore: number;
  targetCoverageDaysAfter: number;
  status: string;
  rationale: string;
  createdAtUtc: string;
}

export interface RouteCandidateStop {
  sequence: number;
  kind: 'Pickup' | 'Delivery';
  servicePointId?: string;
  locationId?: string;
  code: string;
  name: string;
  position: { lat: number; lon: number };
  deltaWeight: number;
  deltaVolume: number;
  windowStart: string;
  windowEnd: string;
  serviceMinutes: number;
}

export interface RouteSuggestionOptionDto {
  optionId: string;
  title: string;
  description: string;
  isRecommended: boolean;
  evaluation: {
    isValid: boolean;
    totalDistanceKm: number;
    totalDurationMinutes: number;
    totalCost: number;
    maxWeightOnRoute: number;
    maxVolumeOnRoute: number;
    stops: any[];
    validationErrors: string[];
    warnings: string[];
    coordinates: [number, number][];
    /** `osrm`: por calles; `approximate`: rectas entre paradas. */
    coordinatesSource?: 'osrm' | 'approximate';
  };
  stops: RouteCandidateStop[];
  sourcingLines: any[];
  vehicleMatches: Array<{
    vehicleId: string;
    vehicleCode: string;
    maxWeight: number;
    maxVolume?: number;
    isMatch: boolean;
    discardReason?: string;
    weightUtilizationPct: number;
    volumeUtilizationPct: number;
    estimatedCost: number;
    distanceToFirstPickupKm: number;
  }>;
}

export interface AgentDecisionDto {
  id: string;
  incidentId: string;
  proposalType: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Applied';
  rationale: string;
  recommendedOption: string;
  options: any[];
  createdAtUtc: string;
}

export interface AgentDecisionViewDto {
  agentDecisionId: string;
  vehicleCode: string;
  outcome: string;
  explanation: string;
  driverMessage: string;
  source: 'Llm' | 'Template';
  mismatchReason?: string | null;
  facts: { code: string; subject: string; value: number; unit: string }[];
}

export interface AgentRunViewDto {
  runId: string;
  kind: 'Watch' | 'Incident';
  status: 'Running' | 'Completed' | 'Fallback' | 'Failed';
  latencyMs: number;
  model?: string;
  promptVersion?: string;
  n8nExecutionId?: string;
  fallbackReason?: string;
  toolCalls: {
    method: string; path: string; status: number; milliseconds: number; atUtc: string;
    event?: string | null; executionId?: string | null; workflow?: string | null; node?: string | null; message?: string | null;
  }[];
  decisions: AgentDecisionViewDto[];
}

export interface AutonomyModeDto {
  mode: 'AutoWithLimits' | 'ProposeOnly';
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  readonly apiLiveStatus = signal<'checking' | 'online' | 'offline'>('checking');
  readonly loadErrors = signal<Record<string, string>>({});

  private clearLoadError(resource: string): void {
    this.loadErrors.update(errors => {
      const remaining = { ...errors };
      delete remaining[resource];
      return remaining;
    });
  }

  private reportLoadError(resource: string, error: unknown): void {
    console.error('No se pudo cargar ' + resource, error);
    this.loadErrors.update(errors => ({
      ...errors,
      [resource]: 'No se pudo cargar ' + resource + '. Los datos pueden estar incompletos. Reintente la consulta.'
    }));
  }

  // ==========================================
  // 1. TELEMETRÍA Y ESTADO DE SERVICIO
  // ==========================================

  getLiveness(): Observable<HealthStatusDto | null> {
    return this.http.get<HealthStatusDto>(`${this.baseUrl}/health/live`).pipe(
      catchError(() => of(null))
    );
  }

  getReadiness(): Observable<HealthStatusDto | null> {
    return this.http.get<HealthStatusDto>(`${this.baseUrl}/health/ready`).pipe(
      catchError(err => {
        if (err.error) return of(err.error as HealthStatusDto);
        return of(null);
      })
    );
  }

  // ==========================================
  // 3. PUNTOS DE SERVICIO (GET /service-points)
  // ==========================================

  getServicePoints(): Observable<ServicePoint[]> {
    return this.http.get<ServicePoint[]>(`${this.baseUrl}/api/v1/service-points`).pipe(
      tap(() => this.clearLoadError('puntos de servicio')),
      tap(() => this.apiLiveStatus.set('online')),
      catchError(err => {
        this.reportLoadError('puntos de servicio', err);
        return of([]);
      })
    );
  }

  getWarehouses(): Observable<ServicePoint[]> {
    return this.http.get<ServicePoint[]>(`${this.baseUrl}/api/v1/service-points/warehouses`).pipe(
      tap(() => this.clearLoadError('bodegas')),
      tap(() => this.apiLiveStatus.set('online')),
      catchError(err => {
        this.reportLoadError('bodegas', err);
        return of([]);
      })
    );
  }

  // ==========================================
  // 4. MATRIZ DE RIESGOS Y AUTONOMÍA (GET /risks)
  // ==========================================

  getRisks(level?: string, refresh = false): Observable<RisksResponseDto | null> {
    const params = new URLSearchParams();
    if (level) params.append('level', level);
    if (refresh) params.append('refresh', 'true');
    const qs = params.toString() ? `?${params.toString()}` : '';
    const url = `${this.baseUrl}/api/v1/risks${qs}`;
    return this.http.get<RisksResponseDto>(url).pipe(
      tap(() => this.clearLoadError('alertas de stock')),
      catchError(err => {
        this.reportLoadError('alertas de stock', err);
        return of(null);
      })
    );
  }

  // ==========================================
  // 5. RUTAS Y DESPACHOS VRP (GET/POST /route-plans)
  // ==========================================

  getRoutePlans(date?: string, status?: string): Observable<RoutePlanSummaryDto[]> {
    const params = new URLSearchParams();
    if (date) params.append('date', date);
    if (status) params.append('status', status);
    const queryString = params.toString() ? `?${params.toString()}` : '';

    return this.http.get<any>(`${this.baseUrl}/api/v1/route-plans${queryString}`).pipe(
      tap(() => this.clearLoadError('rutas')),
      map(res => {
        const raw = Array.isArray(res) ? res : (res?.items || []);
        return raw.map((item: any) => ({
          id: item.id || item.routePlanId,
          code: item.code,
          revision: item.revision,
          status: item.status,
          vehicleId: item.vehicleId,
          vehicleCode: item.vehicleCode,
          plannedStartUtc: item.plannedStartUtc,
          totalDistanceKm: item.totalDistanceKm ?? item.km ?? 0,
          totalDurationMinutes: item.totalDurationMinutes ?? item.minutes ?? 0,
          stopsCount: item.stopsCount ?? 0
        }));
      }),
      catchError(err => {
        this.reportLoadError('rutas', err);
        return of([]);
      })
    );
  }

  getRoutePlanGeometry(id: string): Observable<RouteGeometryDto | null> {
    return this.http.get<RouteGeometryDto>(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(id)}/geometry`).pipe(
      catchError(() => of(null))
    );
  }

  getRoutePlanById(id: string): Observable<RoutePlanDetailDto | null> {
    return this.http.get<any>(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(id)}`).pipe(
      map(res => {
        if (!res) return null;
        return {
          id: res.id || res.routePlanId,
          code: res.code,
          revision: res.revision,
          status: res.status,
          vehicleId: res.vehicleId,
          vehicleCode: res.vehicleCode,
          plannedStartUtc: res.plannedStartUtc,
          plannedEndUtc: res.plannedEndUtc,
          totalDistanceKm: res.totalDistanceKm ?? res.totalKm ?? res.km ?? 0,
          totalDurationMinutes: res.totalDurationMinutes ?? res.totalMinutes ?? res.minutes ?? 0,
          polyline: res.polyline || '',
          originWarehouseName: res.originWarehouseName || '',
          driverAccountId: res.driverAccountId ?? null,
          driverName: res.driverName || '',
          deliveriesCount: res.deliveriesCount ?? 0,
          unassignedDeliveries: res.unassignedDeliveries ?? 0,
          stops: (res.stops || []).map((s: any) => ({
            sequence: s.sequence ?? s.sequenceNumber,
            servicePointId: s.servicePointId,
            servicePointCode: s.servicePointCode || '',
            servicePointName: s.servicePointName || '',
            etaUtc: s.etaUtc || s.plannedArrivalUtc,
            plannedDepartureUtc: s.plannedDepartureUtc,
            serviceMinutes: s.serviceMinutes ?? 15,
            windowStart: s.windowStart || '08:00',
            windowEnd: s.windowEnd || '17:00',
            status: s.status,
            ordersCount: s.ordersCount || 0,
            // Antes se descartaban y la página mostraba «Sin productos registrados» aunque el backend los enviara.
            kind: s.kind,
            deliveryId: s.deliveryId ?? null,
            deliveryCode: s.deliveryCode || '',
            deliveryStatus: s.deliveryStatus || '',
            handlingUnits: s.handlingUnits || [],
            lines: s.lines || [],
            loadQuantities: s.loadQuantities || []
          })),
          productsSummary: res.productsSummary || [],
          driverRoutes: res.driverRoutes || []
        } as RoutePlanDetailDto;
      }),
      catchError(err => {
        console.warn('Get route plan by ID failed', err);
        return of(null);
      })
    );
  }

  createRoutePlan(req: {
    date?: string;
    vehicleCodes?: string[];
    includeConsolidation?: boolean;
    criticalDeadlineLocal?: string;
  } = {}): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans`, req);
  }

  createManualRoutePlan(req: CreateManualRoutePlanRequest): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/manual`, req);
  }

  // --- MÉTODOS DE RUTAS POR PEDIDOS & VEHÍCULOS ---
  suggestRoutes(orderIds: string[], plannedDate?: string): Observable<{ options: RouteSuggestionOptionDto[] }> {
    return this.http.post<{ options: RouteSuggestionOptionDto[] }>(`${this.baseUrl}/api/v1/route-plans/suggest`, {
      orderIds,
      plannedDate
    });
  }

  evaluateRouteSequence(stops: RouteCandidateStop[], vehicleId?: string, plannedStartUtc?: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/evaluate`, {
      stops,
      vehicleId,
      plannedStartUtc
    });
  }

  confirmRoutePlan(req: {
    code?: string;
    vehicleId?: string;
    totalDistanceKm: number;
    totalDurationMinutes: number;
    totalCost: number;
    plannedStartUtc?: string;
    orderIds?: string[];
    stops: RouteCandidateStop[];
    polyline?: string;
    coordinates?: [number, number][];
  }): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/confirm`, req);
  }

  getEmergencyRoutes(): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/emergency`, {});
  }

  // --- MÉTODOS DE PEDIDOS (DELIVERY ORDERS) ---
  getDeliveryOrders(status?: string, priority?: string): Observable<DeliveryOrderDto[]> {
    let url = `${this.baseUrl}/api/v1/delivery-orders`;
    const params: string[] = [];
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    if (priority) params.push(`priority=${encodeURIComponent(priority)}`);
    if (params.length > 0) url += `?${params.join('&')}`;
    return this.http.get<DeliveryOrderDto[]>(url);
  }

  createDeliveryOrder(order: {
    destinationServicePointId?: string;
    destinationLocationId?: string;
    requiredDateUtc?: string;
    priority?: string;
    sourceType?: string;
    notes?: string;
    lines: Array<{ productId: string; quantity: number }>;
  }): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/delivery-orders`, order);
  }

  cancelDeliveryOrder(id: string): Observable<any> {
    return this.http.patch(`${this.baseUrl}/api/v1/delivery-orders/${encodeURIComponent(id)}/cancel`, {});
  }

  // --- MÉTODOS DE BODEGAS & REBALANCEO ---
  getWarehouseAnalysis(): Observable<WarehouseAnalysisDto[]> {
    return this.http.get<WarehouseAnalysisDto[]>(`${this.baseUrl}/api/v1/warehouses/analysis`);
  }

  getWarehouseStock(params?: { productId?: string; sku?: string; locationId?: string }): Observable<WarehouseStockDto[]> {
    const query = new URLSearchParams();
    if (params?.productId) query.append('productId', params.productId);
    if (params?.sku) query.append('sku', params.sku);
    if (params?.locationId) query.append('locationId', params.locationId);
    const qs = query.toString() ? `?${query.toString()}` : '';

    return this.http.get<WarehouseStockDto[]>(`${this.baseUrl}/api/v1/warehouses/stock${qs}`).pipe(
      catchError(() => {
        return this.http.get<any[]>(`${this.baseUrl}/api/v1/warehouses/analysis`).pipe(
          map(items => {
            let filtered = items || [];
            if (params?.productId) filtered = filtered.filter(i => i.productId === params.productId);
            if (params?.sku) filtered = filtered.filter(i => (i.sku || '').toLowerCase() === (params.sku || '').toLowerCase());
            if (params?.locationId) filtered = filtered.filter(i => i.locationId === params.locationId);
            return filtered.map(i => ({
              locationId: i.locationId,
              locationCode: i.locationCode,
              locationName: i.locationName,
              productId: i.productId,
              sku: i.sku,
              productName: i.productName,
              quantityOnHand: i.quantityOnHand ?? 0,
              quantityReserved: i.quantityReserved ?? 0,
              availableQuantity: i.availableQuantity ?? 0,
              coverageDays: i.coverageDays,
              minStock: i.minStock,
              maxStock: i.maxStock,
              status: i.status || 'Normal'
            } as WarehouseStockDto));
          }),
          catchError(() => of([]))
        );
      })
    );
  }

  getWarehousePolicies(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/api/v1/warehouses/policies`);
  }

  updateWarehousePolicy(locationId: string, productId: string, policy: {
    minStock: number;
    maxStock: number;
    leadTimeDays: number;
    targetCoverageDays: number;
    isManual: boolean;
  }): Observable<any> {
    return this.http.put(`${this.baseUrl}/api/v1/warehouses/${encodeURIComponent(locationId)}/policies/${encodeURIComponent(productId)}`, policy);
  }

  generateRebalancingSuggestions(): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/warehouses/rebalancing/generate`, {});
  }

  getTransferSuggestions(status?: string): Observable<TransferSuggestionDto[]> {
    const url = status
      ? `${this.baseUrl}/api/v1/warehouses/rebalancing/suggestions?status=${encodeURIComponent(status)}`
      : `${this.baseUrl}/api/v1/warehouses/rebalancing/suggestions`;
    return this.http.get<TransferSuggestionDto[]>(url);
  }

  acceptTransferSuggestion(id: string, acceptedQuantity?: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/warehouses/rebalancing/suggestions/${encodeURIComponent(id)}/accept`, { acceptedQuantity });
  }

  dismissTransferSuggestion(id: string, reason: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/warehouses/rebalancing/suggestions/${encodeURIComponent(id)}/dismiss`, { reason });
  }

  /** Conductores activos que el Planificador puede asignar al publicar. */
  getDrivers(): Observable<DriverOptionDto[]> {
    return this.http.get<DriverOptionDto[]>(`${this.baseUrl}/api/v1/drivers`).pipe(catchError(() => of([])));
  }

  /** #160: cambia el conductor de una ruta ya publicada (entregas pendientes). */
  assignRouteDriver(id: string, accountId: string): Observable<any> {
    return this.http.put(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(id)}/driver`, { accountId });
  }

  /** Sin conductor, las entregas se crean sin asignar y ningún conductor las ve en la PWA. */
  publishRoutePlan(id: string, assignment?: { vehicleCode: string; driverAccountId: string }): Observable<any> {
    const body = assignment ? { driverAssignments: { [assignment.vehicleCode]: assignment.driverAccountId } } : {};
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(id)}/publish`, body);
  }

  completeStop(routePlanId: string, sequence: number): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(routePlanId)}/stops/${sequence}/complete`, {});
  }

  replanRoute(id: string, reason?: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(id)}/replan`, { reason });
  }

  revertRoute(id: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(id)}/revert`, {});
  }

  // ==========================================
  // 6. DECISIONES DEL AGENTE IA (GET /agent-decisions)
  // ==========================================

  getAgentDecisions(status?: string): Observable<AgentDecisionDto[]> {
    const url = status ? `${this.baseUrl}/api/v1/agent-decisions?status=${encodeURIComponent(status)}` : `${this.baseUrl}/api/v1/agent-decisions`;
    return this.http.get<AgentDecisionDto[]>(url).pipe(
      tap(() => this.clearLoadError('decisiones del agente')),
      catchError(err => {
        this.reportLoadError('decisiones del agente', err);
        return of([]);
      })
    );
  }

  approveAgentDecision(id: string, option: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/agent-decisions/${encodeURIComponent(id)}/approve`, { option });
  }

  rejectAgentDecision(id: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/agent-decisions/${encodeURIComponent(id)}/reject`, {});
  }

  startAgentRun(kind: 'Watch' | 'Incident' = 'Watch', incidentId?: string): Observable<AgentRunViewDto> {
    return this.http.post<AgentRunViewDto>(`${this.baseUrl}/api/v1/agent/runs`, { kind, incidentId });
  }

  getAgentRun(id: string): Observable<AgentRunViewDto> {
    return this.http.get<AgentRunViewDto>(`${this.baseUrl}/api/v1/agent/runs/${encodeURIComponent(id)}`);
  }

  // ==========================================
  // 7. MODO DE AUTONOMÍA DEL SISTEMA (GET/PUT /autonomy/mode)
  // ==========================================

  getAutonomyMode(): Observable<AutonomyModeDto | null> {
    return this.http.get<AutonomyModeDto>(`${this.baseUrl}/api/v1/autonomy/mode`).pipe(
      catchError(err => {
        console.warn('Get autonomy mode failed', err);
        return of(null);
      })
    );
  }

  setAutonomyMode(mode: 'AutoWithLimits' | 'ProposeOnly'): Observable<AutonomyModeDto | null> {
    return this.http.put<AutonomyModeDto>(`${this.baseUrl}/api/v1/autonomy/mode`, { mode }).pipe(
      catchError(err => {
        console.warn('Set autonomy mode failed', err);
        return of(null);
      })
    );
  }

  // ==========================================
  // 8. INCIDENTES Y SIMULACIÓN DEMO (POST /incidents, POST /demo/incidents)
  // ==========================================

  createIncident(req: { kind: string; lat?: number; lon?: number; severity?: string; description?: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/incidents`, req);
  }

  triggerDemoIncident(preset = 'cierre-norte'): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/demo/incidents`, { preset });
  }

  resetDemo(fixture = 'demo-base'): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/demo/reset?fixture=${encodeURIComponent(fixture)}`, {});
  }

  // ==========================================
  // 9. TRAZABILIDAD PÚBLICA QR (GET /public/trace/{unitId}) Y REVOCACIÓN
  // ==========================================

  getPublicTrace(unitId: string, token: string): Observable<PublicTraceResponseDto> {
    const headers = new HttpHeaders({
      'X-Trace-Token': token
    });
    return this.http.get<PublicTraceResponseDto>(
      `${this.baseUrl}/api/v1/public/trace/${encodeURIComponent(unitId)}`,
      { headers }
    );
  }

  revokePublicToken(unitId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/api/v1/handling-units/${encodeURIComponent(unitId)}/public-token/revoke`,
      {}
    );
  }

  // ==========================================
  // 10. IMPORTACIÓN DE DATOS (POST /api/v1/imports)
  // ==========================================

  /**
   * Igual que importData, pero un rechazo sin reporte (400, 409, 413, red) llega como error con el
   * motivo del servidor, para mostrarlo en pantalla en lugar de dejarla vacía.
   */
  importDataOrExplain(formData: FormData, dryRun = true, allowPartial = false): Observable<ImportReportDto> {
    return this.http.post<ImportReportDto>(
      `${this.baseUrl}/api/v1/imports?dryRun=${dryRun}&allowPartial=${allowPartial}`,
      formData
    ).pipe(
      catchError(err => {
        if (err?.error && typeof err.error === 'object' && 'entities' in err.error) {
          return of(err.error as ImportReportDto);
        }
        const problem = err?.error && typeof err.error === 'object' ? err.error as { title?: string; detail?: string } : null;
        const message = err?.status === 0
          ? 'Sin conexión con el servidor.'
          : [problem?.title, problem?.detail].filter(Boolean).join(' ') || `La importación fue rechazada (HTTP ${err?.status ?? '?'}).`;
        return throwError(() => new Error(message));
      })
    );
  }

  importData(formData: FormData, dryRun = true, allowPartial = false): Observable<ImportReportDto | null> {
    return this.http.post<ImportReportDto>(
      `${this.baseUrl}/api/v1/imports?dryRun=${dryRun}&allowPartial=${allowPartial}`,
      formData
    ).pipe(
      catchError(err => {
        if (err.error && typeof err.error === 'object' && 'entities' in err.error) {
          return of(err.error as ImportReportDto);
        }
        console.warn('Import data failed', err);
        return of(null);
      })
    );
  }

  getEntityData(entityKey: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/api/v1/imports/data/${encodeURIComponent(entityKey)}`).pipe(
      tap(() => this.clearLoadError('datos de ' + entityKey)),
      catchError(err => {
        this.reportLoadError('datos de ' + entityKey, err);
        return of([]);
      })
    );
  }

  // ==========================================
  // 11. BANDEJA DE CAPTURAS RECHAZADAS (GET /api/v1/deliveries/rejected)
  // ==========================================

  getRejectedCaptures(servicePointId?: string): Observable<RejectedCapturesResponse> {
    const options = servicePointId ? { params: { servicePointId } } : {};
    return this.http.get<RejectedCapturesResponse>(`${this.baseUrl}/api/v1/deliveries/rejected`, options).pipe(
      tap(() => this.clearLoadError('capturas rechazadas')),
      catchError(err => {
        this.reportLoadError('capturas rechazadas', err);
        return of({ items: [], count: 0 });
      })
    );
  }

  // ==========================================
  // 12. GUÍA DE RETORNO (Issue #247: /api/v1/return-guides)
  // ==========================================

  getReturnGuide(guideNumber: string): Observable<ReturnGuideDto> {
    return this.http.get<ReturnGuideDto>(`${this.baseUrl}/api/v1/return-guides/${encodeURIComponent(guideNumber)}`).pipe(
      tap(() => this.clearLoadError(`guía de retorno ${guideNumber}`)),
      catchError(err => {
        this.reportLoadError(`guía de retorno ${guideNumber}`, err);
        throw err;
      })
    );
  }

  generateReturnGuide(routePlanId: string, notes?: string): Observable<ReturnGuideDto> {
    return this.http.post<ReturnGuideDto>(`${this.baseUrl}/api/v1/return-guides/generate`, {
      routePlanId,
      notes
    }).pipe(
      tap(() => this.clearLoadError('generación de guía de retorno')),
      catchError(err => {
        this.reportLoadError('generación de guía de retorno', err);
        throw err;
      })
    );
  }

  receiveReturnGuide(guideNumber: string, operationId?: string): Observable<ReceiveReturnGuideResponse> {
    const opId = operationId || (typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : '00000000-0000-0000-0000-000000000000');
    return this.http.post<ReceiveReturnGuideResponse>(`${this.baseUrl}/api/v1/return-guides/receive`, {
      guideNumber,
      operationId: opId,
      occurredAtDevice: new Date().toISOString()
    }).pipe(
      tap(() => this.clearLoadError(`recepción de guía de retorno ${guideNumber}`)),
      catchError(err => {
        this.reportLoadError(`recepción de guía de retorno ${guideNumber}`, err);
        throw err;
      })
    );
  }
}

