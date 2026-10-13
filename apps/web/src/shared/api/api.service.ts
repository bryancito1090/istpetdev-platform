import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { environment } from '../config/environment';
import { PingResponseDto, HealthStatusDto, PublicTraceResponseDto, ServicePoint } from '@istpetdev/shared-core';

export interface AuthLoginResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: {
    id: string;
    email: string;
    name: string;
    role: string;
    roleLabel: string;
    facility: string;
  };
}

export interface CurrentUserDto {
  id: string;
  subject: string;
  role: string;
  roleLabel: string;
  clientId?: string;
}

export interface RiskItemDto {
  servicePointId: string;
  servicePointCode: string;
  productId: string;
  sku: string;
  availableQuantity: number;
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
}

export interface RoutePlanDetailDto {
  id: string;
  code: string;
  revision: number;
  status: string;
  vehicleId: string;
  vehicleCode: string;
  plannedStartUtc: string;
  totalDistanceKm: number;
  totalDurationMinutes: number;
  polyline: string;
  stops: Array<{
    sequence: number;
    servicePointId: string;
    servicePointCode: string;
    servicePointName: string;
    etaUtc: string;
    status: string;
    ordersCount: number;
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
  readonly lastPing = signal<PingResponseDto | null>(null);

  // ==========================================
  // 1. TELEMETRÍA Y ESTADO DE SERVICIO
  // ==========================================

  getPing(): Observable<PingResponseDto | null> {
    return this.http.get<PingResponseDto>(`${this.baseUrl}/api/v1/ping`).pipe(
      map(res => {
        this.apiLiveStatus.set('online');
        this.lastPing.set(res);
        return res;
      }),
      catchError(err => {
        console.warn('API ping failed', err);
        this.apiLiveStatus.set('offline');
        this.lastPing.set(null);
        return of(null);
      })
    );
  }

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
  // 2. AUTENTICACIÓN Y SESIÓN (POST /auth/login, GET /auth/me)
  // ==========================================

  login(credentials: { email?: string; password?: string; role?: string }): Observable<AuthLoginResponse | null> {
    return this.http.post<AuthLoginResponse>(`${this.baseUrl}/api/v1/auth/login`, credentials).pipe(
      tap(() => this.apiLiveStatus.set('online')),
      catchError(err => {
        console.warn('API login failed', err);
        return of(null);
      })
    );
  }

  getCurrentUser(): Observable<CurrentUserDto | null> {
    return this.http.get<CurrentUserDto>(`${this.baseUrl}/api/v1/auth/me`).pipe(
      catchError(err => {
        console.warn('Get current user failed', err);
        return of(null);
      })
    );
  }

  logout(): Observable<boolean> {
    return this.http.post<{ success: boolean }>(`${this.baseUrl}/api/v1/auth/logout`, {}).pipe(
      map(res => res.success),
      catchError(() => of(true))
    );
  }

  // ==========================================
  // 3. PUNTOS DE SERVICIO (GET /service-points)
  // ==========================================

  getServicePoints(): Observable<ServicePoint[]> {
    return this.http.get<ServicePoint[]>(`${this.baseUrl}/api/v1/service-points`).pipe(
      tap(() => this.apiLiveStatus.set('online')),
      catchError(err => {
        console.warn('Get service points failed', err);
        return of([]);
      })
    );
  }

  // ==========================================
  // 4. MATRIZ DE RIESGOS Y AUTONOMÍA (GET /risks)
  // ==========================================

  getRisks(level?: string): Observable<RisksResponseDto | null> {
    const url = level ? `${this.baseUrl}/api/v1/risks?level=${encodeURIComponent(level)}` : `${this.baseUrl}/api/v1/risks`;
    return this.http.get<RisksResponseDto>(url).pipe(
      catchError(err => {
        console.warn('Get risks failed', err);
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

    return this.http.get<RoutePlanSummaryDto[]>(`${this.baseUrl}/api/v1/route-plans${queryString}`).pipe(
      catchError(err => {
        console.warn('Get route plans failed', err);
        return of([]);
      })
    );
  }

  getRoutePlanById(id: string): Observable<RoutePlanDetailDto | null> {
    return this.http.get<RoutePlanDetailDto>(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(id)}`).pipe(
      catchError(err => {
        console.warn('Get route plan by ID failed', err);
        return of(null);
      })
    );
  }

  createRoutePlan(req: { date?: string; vehicleCode?: string }): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans`, req);
  }

  publishRoutePlan(id: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/api/v1/route-plans/${encodeURIComponent(id)}/publish`, {});
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
      catchError(err => {
        console.warn('Get agent decisions failed', err);
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
  // 9. TRAZABILIDAD PÚBLICA QR (GET /public/trace/{unitId})
  // ==========================================

  getPublicTrace(unitId: string): Observable<PublicTraceResponseDto | null> {
    return this.http.get<PublicTraceResponseDto>(`${this.baseUrl}/api/v1/public/trace/${encodeURIComponent(unitId)}`).pipe(
      catchError(err => {
        console.warn('Public trace query failed', err);
        return of(null);
      })
    );
  }

  // ==========================================
  // 10. IMPORTACIÓN DE DATOS (POST /api/v1/imports)
  // ==========================================

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
}
