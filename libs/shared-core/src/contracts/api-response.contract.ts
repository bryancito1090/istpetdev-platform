export interface ApiResponse<T> {
  data?: T;
  error?: string;
  correlationId?: string;
}

export interface PingResponseDto {
  status: string;
  version: string;
  service: string;
  timestamp: string;
  environment: string;
}

export interface HealthStatusDto {
  status: 'Live' | 'Healthy' | 'Degraded' | 'Unhealthy';
  timestamp: string;
  service?: string;
  entries?: Array<{
    name: string;
    status: string;
    description?: string;
    durationMs?: number;
  }>;
}
