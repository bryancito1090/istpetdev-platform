import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map, Observable, of } from 'rxjs';
import { environment } from '../config/environment';
import { PingResponseDto, HealthStatusDto, PublicTraceResponseDto, ServicePoint } from '@istpetdev/shared-core';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  readonly apiLiveStatus = signal<'checking' | 'online' | 'offline'>('checking');
  readonly lastPing = signal<PingResponseDto | null>(null);

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

  getPublicTrace(unitId: string): Observable<PublicTraceResponseDto | null> {
    return this.http.get<PublicTraceResponseDto>(`${this.baseUrl}/api/v1/public/trace/${encodeURIComponent(unitId)}`).pipe(
      catchError(err => {
        console.warn('Public trace query failed', err);
        return of(null);
      })
    );
  }

  getServicePoints(): Observable<ServicePoint[]> {
    return this.http.get<ServicePoint[]>(`${this.baseUrl}/api/v1/service-points`).pipe(
      catchError(err => {
        console.warn('Get service points failed', err);
        return of([]);
      })
    );
  }
}
