/**
 * Contratos y DTOs oficiales del Spec 004:
 * Campo — QR, custodia, despacho, recepción y PWA offline.
 * Compartidos entre apps/web, apps/mobile y la librería shared-core.
 */

// ==========================================
// Enums y Estados de Dominio
// ==========================================

export type HandlingUnitStatus = 'Prepared' | 'Dispatched' | 'Received' | 'Partial';

export type DeliveryFieldStatus = 'Draft' | 'Planned' | 'Prepared' | 'Dispatched' | 'Delivered' | 'Partial';

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
  | 'Complete';

export type FieldErrorCode =
  | 'IDEMPOTENCY_CONFLICT'
  | 'ASSIGNMENT_CHANGED'
  | 'QUANTITY_EXCEEDED'
  | 'UNAUTHORIZED_ROLE'
  | 'UNAUTHORIZED_ASSIGNMENT'
  | 'UNIT_NOT_FOUND'
  | 'STORAGE_QUOTA_EXCEEDED'
  | 'TOKEN_REVOKED_OR_EXPIRED';

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
  stopsCount: number;
  totalDistanceKm?: number;
  estimatedDurationMinutes?: number;
  deliveries: MobileAssignmentDelivery[];
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
  stops: number;
}

export interface CompletedStopDto {
  sequence: number;
  deliveryId: string;
  receiptId: string;
  acceptedAtUtc: string;
}

export interface DemoAdvanceResponse {
  completedStops: CompletedStopDto[];
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
  driverId: string;
  driverName: string;
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
