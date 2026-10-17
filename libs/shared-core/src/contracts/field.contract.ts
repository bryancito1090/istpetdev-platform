/**
 * Contratos y DTOs oficiales del Spec 004:
 * Campo — QR, custodia, despacho, recepción y PWA offline.
 * Compartidos entre apps/web, apps/mobile y la librería shared-core.
 */

// ==========================================
// Enums y Estados de Dominio
// ==========================================

// #162: el bulto nace Sealed (preparado por Bodega) y avanza Dispatched → InTransit → Delivered
// → Received/Partial. 'Prepared' se conserva como alias legado anterior a #162.
export type HandlingUnitStatus = 'Sealed' | 'Prepared' | 'Dispatched' | 'InTransit' | 'Delivered' | 'Received' | 'Partial';

export type DeliveryFieldStatus =
  | 'Draft'
  | 'Planned'
  | 'Prepared'
  | 'Dispatched'
  | 'InTransit'
  | 'Delivered'
  | 'Received'
  | 'Partial'
  | 'Cancelled';

export type EvidenceStatus = 'Pending' | 'Uploaded' | 'Confirmed' | 'Failed';

export type PwaSyncState =
  | 'Downloaded'
  | 'Captured'
  | 'SavedOnDevice'
  | 'Sending'
  | 'AcceptedByServer'
  | 'Conflict'
  | 'RequiresReview'
  | 'SessionRequired'
  | 'EvidencePending'
  | 'Complete'
  | 'Failed';

export type FieldErrorCode =
  | 'IDEMPOTENCY_CONFLICT'
  | 'ASSIGNMENT_CHANGED'
  | 'QUANTITY_EXCEEDED'
  | 'UNAUTHORIZED_ROLE'
  | 'UNAUTHORIZED_ASSIGNMENT'
  | 'UNIT_NOT_FOUND'
  | 'STORAGE_QUOTA_EXCEEDED'
  | 'TOKEN_REVOKED_OR_EXPIRED'
  | 'NOT_DISPATCHED'
  | 'NOT_DELIVERED'
  | 'ALREADY_RECEIVED'
  | 'INVALID_STATE_TRANSITION'
  | 'HANDLING_UNIT_STATE_CONFLICT'
  | 'NO_HANDLING_UNITS'
  | 'RECEIPT_REQUIRES_RECEIVER'
  | 'INCOMPLETE_HANDLING_UNITS'
  | 'INSUFFICIENT_VEHICLE_STOCK'
  | 'RECEIPT_COVERAGE_MISMATCH'
  | 'INVALID_OPERATION_ID';

// Problem Details estandarizado (RFC 7807)
export interface FieldProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  code: FieldErrorCode | string;
  correlationId: string;
}

// ==========================================
// T2: Preparación de Unidades (Handling Units) y QR
// ==========================================

export interface HandlingUnitLineDto {
  deliveryLineId: string;
  lotId: string;
  sku: string;
  quantity: number;
  unit: string;
  name?: string;
}

export interface CreateHandlingUnitRequest {
  operationId: string; // UUID v7
  lines: Array<{
    deliveryLineId: string;
    lotId: string;
    quantity: number;
  }>;
}

export interface HandlingUnitResponse {
  handlingUnitId: string;
  code: string;
  deliveryId: string;
  status: HandlingUnitStatus;
  labelUrl: string;
  token?: string; // Token claro solo visible al emitir/reimprimir
  lines: HandlingUnitLineDto[];
  createdAtUtc: string;
}

// ==========================================
// T3: Despacho Atómico de Bodega
// ==========================================

export interface DispatchRequest {
  operationId: string; // UUID v7 coincidente con Idempotency-Key
  handlingUnitIds: string[];
  occurredAtDevice?: string;
}

export interface DispatchResponse {
  deliveryId: string;
  status: DeliveryFieldStatus;
  dispatchedHandlingUnits: string[];
  dispatchedAtUtc: string;
  custodyEventId: string;
}

// ==========================================
// #162: Custodia del Conductor (recogida y entrega física)
// ==========================================

export interface CustodyTransitionRequest {
  operationId: string; // UUID v7 coincidente con Idempotency-Key
  occurredAtDevice?: string;
}

export interface CustodyTransitionResponse {
  deliveryId: string;
  status: DeliveryFieldStatus;
  handlingUnitIds: string[];
  changedAtUtc: string;
  custodyEventId: string;
}

// ==========================================
// T8: Asignaciones del Conductor (Snapshot Móvil)
// ==========================================

export interface MobileAssignmentDelivery {
  deliveryId: string;
  sequence: number;
  kind?: 'Pickup' | 'Delivery';
  locationId?: string;
  pointCode: string;
  pointName: string;
  address: string;
  dueAtUtc: string;
  status: DeliveryFieldStatus;
  handlingUnits: HandlingUnitResponse[];
}

export interface MobileAssignmentRoute {
  routePlanId: string;
  revision: number;
  downloadedAtUtc: string;
  driverMessage?: string;
  vehicleCode: string;
  vehiclePlate?: string;
  vehicleDescription?: string;
  stopsCount: number;
  totalDistanceKm?: number;
  estimatedDurationMinutes?: number;
  deliveries: MobileAssignmentDelivery[];
  /** #160: bodega desde la que sale la ruta. */
  originWarehouseName?: string | null;
  /** Puntos de recogida de bodega que forman parte de la ruta. */
  pickupStops?: MobileAssignmentPickupStop[];
}

export interface MobileAssignmentPickupStop {
  stopId: string;
  sequence: number;
  locationId?: string | null;
  pointCode: string;
  pointName: string;
  address: string;
  dueAtUtc: string;
}

export interface MobileAssignmentResponse {
  serverTimeUtc: string;
  driverAccountId: string;
  routes: MobileAssignmentRoute[];
}

// ==========================================
// FR-19: Listado Paginado para el Panel Web
// ==========================================

export interface DeliveryListItem {
  deliveryId: string;
  routePlanId: string;
  pointCode: string;
  pointName: string;
  dueAtUtc: string;
  status: DeliveryFieldStatus;
  evidenceStatus: EvidenceStatus;
  packagesCount: number;
}

export interface DeliveryListResponse {
  items: DeliveryListItem[];
  nextCursor?: string;
  total?: number;
}

// ==========================================
// Spec 004 / Issue #121: Delivery Detail
// ==========================================

export interface DeliveryDetailLineDto {
  deliveryLineId: string;
  lotId: string;
  lotCode: string;
  sku: string;
  productName: string;
  plannedQuantity: number;
  unit: string;
}

export interface DeliveryDetailHandlingUnitDto {
  handlingUnitId: string;
  code: string;
  status: string;
}

export interface DeliveryDetailResponse {
  deliveryId: string;
  status: string;
  version: number;
  pointName: string;
  routePlanId: string;
  lines: DeliveryDetailLineDto[];
  handlingUnits: DeliveryDetailHandlingUnitDto[];
  /** #160: vehículo y conductor al que va la entrega, y bodega de salida. */
  vehicleCode?: string | null;
  driverName?: string | null;
  originWarehouseName?: string | null;
  /** #163: cantidad de evidencias confirmadas en almacenamiento (S3). */
  confirmedEvidenceCount?: number;
}

// ==========================================
// T4 & T5: Recepción Offline e Idempotente
// ==========================================

export interface ReceiptLineRequest {
  deliveryLineId: string;
  lotId: string;
  quantityAccepted: number;
  quantityRejected: number;
  unit: string;
  rejectionReason?: string;
  remainingQuantity?: number;
  nextRequestedQuantity?: number;
}

export interface CreateReceiptRequest {
  operationId: string; // UUID v7 (Idempotency-Key)
  expectedDeliveryVersion: number;
  occurredAtDevice: string;
  lines: ReceiptLineRequest[];
}

export interface ReceiptLineDto {
  deliveryLineId: string;
  lotId: string;
  quantityAccepted: number;
  quantityRejected: number;
  unit: string;
  remainingQuantity?: number;
  nextRequestedQuantity?: number;
}

export interface ReceiptResponse {
  receiptId: string;
  deliveryId: string;
  acceptedAtServer: string;
  deliveryVersion: number;
  deliveryStatus: DeliveryFieldStatus;
  evidenceStatus: EvidenceStatus;
  lines: ReceiptLineDto[];
}

// ==========================================
// T6: Evidencias Privadas (Foto y Firma)
// ==========================================

export interface EvidenceUploadRequest {
  operationId: string;
  kind: 'photo' | 'signature';
  mimeType: string;
  byteSize: number;
  sha256: string;
}

export interface EvidenceUploadResponse {
  evidenceId: string;
  uploadUrl: string;
  requiredHeaders?: Record<string, string>;
  expiresAtUtc: string;
}

export interface EvidenceConfirmRequest {
  operationId: string;
  sha256: string;
  byteSize: number;
}

export interface EvidenceConfirmResponse {
  evidenceId: string;
  status: EvidenceStatus;
  confirmedAtUtc: string;
}

// ==========================================
// FR-16: Avance de Demo (advance)
// ==========================================

export interface DemoAdvanceRequest {
  operationId: string;
  stops?: number;
  mode?: 'complete' | 'half' | 'next';
  withIncidents?: boolean;
  noveltyRate?: number;
}

export interface CompletedStopDto {
  sequence: number;
  deliveryId: string;
  receiptId: string;
  acceptedAtUtc: string;
  hadIncidents?: boolean;
  quantityRejected?: number | null;
  rejectionReason?: string | null;
}

export interface DemoAdvanceResponse {
  completedStops: CompletedStopDto[];
  checkpointCompleted?: string;
  nextCheckpoint?: string;
  totalStops?: number;
  completedStopsCount?: number;
  status?: string;
}

export interface TodayRouteStopDto {
  stopId: string;
  sequenceNumber: number;
  kind: string;
  pointName: string;
  status: string;
  plannedArrivalUtc?: string;
  actualArrivalUtc?: string;
}

export interface TodayRoutePlanDto {
  routePlanId: string;
  code: string;
  vehicleCode: string;
  driverName?: string;
  driverAccountId?: string;
  status: string;
  totalStops: number;
  completedStops: number;
  nextCheckpoint: string;
  plannedStartUtc: string;
  stops: TodayRouteStopDto[];
}

// ==========================================
// T7: Trazabilidad Pública Limitada (QR)
// ==========================================

export interface PublicTraceProductUnit {
  sku: string;
  unit: string;
}

export interface PublicTraceEvent {
  stage: string;
  timestamp: string;
}

export interface PublicTraceUnitDto {
  unitId: string;
  code: string;
  status: HandlingUnitStatus;
  productUnits: PublicTraceProductUnit[];
  events: PublicTraceEvent[];
}

// ==========================================
// Issue #114 / ESC-23 & ESC-25: Problem Details y Bandeja de Rechazos
// ==========================================

export const ProblemTypes = {
  Conflict: 'https://istpetdev.local/errors/conflict',
  Validation: 'https://istpetdev.local/errors/validation',
  TokenRevoked: 'https://istpetdev.local/errors/token_revoked',
  InternalError: 'https://istpetdev.local/errors/internal_error',
} as const;

export interface RejectedCaptureLineDto {
  productId: string;
  productName: string;
  sku: string;
  quantityAccepted: number;
  quantityRejected: number;
  rejectionReason?: string | null;
}

export interface RejectedCaptureDto {
  receiptId: string;
  deliveryId: string;
  deliveryCode: string;
  servicePointId: string;
  servicePointCode: string;
  servicePointName: string;
  /** @deprecated campo legado: desde #162 la recepción la registra el Receptor, no el Conductor. */
  driverId: string;
  /** @deprecated usar registeredByName; se conserva por compatibilidad del contrato. */
  driverName: string;
  /** #162: nombre de quien registró la recepción (el Receptor). */
  registeredByName?: string;
  receivedAtUtc: string;
  reason: string;
  status: string;
  lines: RejectedCaptureLineDto[];
}

export interface RejectedCapturesResponse {
  items: RejectedCaptureDto[];
  count: number;
}

/** Catálogo V1, códigos compatibles con #117. El contrato conserva rejectionReason. */
export const RECEIPT_REJECTION_CATALOG_VERSION = 'V1';
export const RECEIPT_REJECTION_REASONS = [
  { code: 'Faltante', label: 'Faltante' },
  { code: 'Sobrante', label: 'Sobrante' },
  { code: 'Daño', label: 'Daño' },
  { code: 'PrecintoViolado', label: 'Precinto violado' },
  { code: 'LoteNoCoincide', label: 'Lote no coincide' },
  { code: 'CadenaDeFrio', label: 'Cadena de frío' }
] as const;

// ==========================================
// Issue #247: Guía de Retorno de lo No Entregado
// ==========================================

export interface ReturnGuideLineDto {
  handlingUnitId: string;
  handlingUnitCode: string;
  productId: string;
  sku: string;
  productName: string;
  lotId: string;
  lotCode: string;
  quantity: number;
  unit: string;
  reason: string;
}

export interface ReturnGuideDto {
  guideNumber: string;
  routePlanId: string;
  vehiclePlate: string;
  driverName: string;
  destinationWarehouseId: string;
  destinationWarehouseName: string;
  generatedAtUtc: string;
  status: string;
  lines: ReturnGuideLineDto[];
}

export interface GenerateReturnGuideRequest {
  routePlanId: string;
  notes?: string;
}

export interface ReceiveReturnGuideRequest {
  guideNumber: string;
  operationId: string;
  occurredAtDevice?: string;
}

export interface ReceiveReturnGuideResponse {
  guideNumber: string;
  status: string;
  receivedAtUtc: string;
  itemsReconciled: number;
  rebalanceOrderId: string;
}

// ==========================================
// Incidencias en Ruta (Conductor / Campo)
// ==========================================

export interface ReportIncidentRequest {
  kind: string;
  vehicleCode?: string;
  description?: string;
  severity?: string;
  lat?: number;
  lon?: number;
  radiusKm?: number;
  slowdownFactor?: number;
  factor?: number;
  isSynthetic?: boolean;
}

export interface ReportIncidentResponse {
  incidentId: string;
  kind: string;
  slowdownFactor?: number | null;
  runId?: string | null;
  affectedRoutes?: Array<{
    routePlanId: string;
    vehicleCode: string;
    affectedLegs: Array<{ from: { lat: number; lon: number }; to: { lat: number; lon: number } }>;
    affectedStops: Array<{ pointCode: string; sequenceNumber: number }>;
  }>;
}


