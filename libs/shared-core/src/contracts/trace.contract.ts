export interface TraceEventDto {
  stage: string;
  timestamp: string;
}

export interface PublicTraceProductUnitDto {
  sku: string;
  unit: string;
}

export interface PublicTraceResponseDto {
  unitId: string;
  code?: string;
  status: string;
  productUnits?: PublicTraceProductUnitDto[];
  events: TraceEventDto[];
}
