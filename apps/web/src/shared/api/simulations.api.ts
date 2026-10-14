import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../config/environment';

export interface SimulationComparisonRequest {
  datasetVersion?: string;
  seeds?: number[];
  includeAdverse?: boolean;
  policyVersions?: Record<string, string>;
}

export interface SimulationComparisonResponse {
  comparisonId: string;
  status: string;
  createdAtUtc: string;
}

export interface SimulationComparisonSummary {
  comparisonId: string;
  status: string;
  datasetVersion: string;
  createdAtUtc: string;
  completedAtUtc?: string;
  progressPercentage: number;
  origin: string;
}

export interface SimulationRunSummaryDto {
  id: string;
  seed: number;
  scenarioCode: string;
  policyCode: string;
  status: string;
  progressPercentage: number;
  failureReason?: string;
}

export interface AggregatedMetricSummary {
  policy: string;
  metricCode: string;
  min?: number;
  median?: number;
  max?: number;
  adverseValue?: number;
}

export interface SimulationMetricDto {
  code: string;
  policyCode: string;
  sku?: string;
  numerator: number;
  denominator?: number;
  value?: number;
  unit: string;
  notApplicableReason?: string;
}

export interface SimulationComparisonDetail {
  comparisonId: string;
  status: string;
  datasetVersion: string;
  progressPercentage: number;
  createdAtUtc: string;
  completedAtUtc?: string;
  origin: string;
  hostName?: string;
  commitHash?: string;
  failureReason?: string;
  assumptions: string[];
  runs: SimulationRunSummaryDto[];
  aggregated: AggregatedMetricSummary[];
  metrics: SimulationMetricDto[];
}

@Injectable({
  providedIn: 'root'
})
export class SimulationsApiService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/v1/simulations/comparisons`;

  createComparison(request: SimulationComparisonRequest): Observable<SimulationComparisonResponse> {
    return this.http.post<SimulationComparisonResponse>(this.baseUrl, request);
  }

  listComparisons(limit: number = 50): Observable<SimulationComparisonSummary[]> {
    return this.http.get<SimulationComparisonSummary[]>(`${this.baseUrl}?limit=${limit}`);
  }

  getComparison(id: string): Observable<SimulationComparisonDetail> {
    return this.http.get<SimulationComparisonDetail>(`${this.baseUrl}/${id}`);
  }

  exportComparison(id: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/export`, {
      responseType: 'blob'
    });
  }

  importComparison(file: File): Observable<{ comparisonId: string; message: string; origin: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ comparisonId: string; message: string; origin: string }>(
      `${this.baseUrl}/import`,
      formData
    );
  }
}
