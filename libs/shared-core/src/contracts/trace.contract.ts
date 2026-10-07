export interface TraceEventDto {
  stage: string;
  timestamp: string;
  location: string;
}

export interface PublicTraceResponseDto {
  unitId: string;
  traceType: string;
  status: string;
  generatedAtUtc: string;
  summary: string;
  events: TraceEventDto[];
}
