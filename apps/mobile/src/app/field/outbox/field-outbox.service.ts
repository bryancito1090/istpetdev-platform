import { Injectable, computed, inject, signal } from '@angular/core';
import { openDB, IDBPDatabase } from 'idb';
import {
  CreateReceiptRequest,
  ReceiptResponse,
  MobileAssignmentResponse,
  PwaSyncState,
  FieldProblemDetails
} from '@istpetdev/shared-core';
import { FIELD_API, IFieldApi } from '../field-api.interface';
import { generateUuidV7 } from '../mock/mock-field-api.service';

export interface OutboxItem {
  operationId: string; // UUID v7
  ownerAccountId: string; // FR-09: cuenta que capturó; otra cuenta no la ve ni la sincroniza
  deliveryId: string;
  expectedDeliveryVersion: number;
  recipientName: string;
  notes?: string;
  capturedAtUtc: string;
  status: PwaSyncState;
  receiptPayload: CreateReceiptRequest;
  
  // Evidencias binarias
  evidencePhotoBlob?: Blob;
  photoSha256?: string;
  evidenceSignatureBlob?: Blob;
  signatureSha256?: string;

  // Respuestas del servidor
  receiptResponse?: ReceiptResponse;
  photoEvidenceId?: string;
  signatureEvidenceId?: string;
  
  // Error si hubo rechazo/conflicto de negocio o transitorio
  errorCode?: string;
  errorMessage?: string;
  problemType?: string;
  retryCount?: number;
  lastAttemptUtc?: string;
}

export interface ClassifiedError {
  isBusiness: boolean;
  status: number;
  code: string;
  type?: string;
  message: string;
}

/**
 * Clasifica respuestas del API según la práctica offline-first:
 * - 409/422/403/400 de negocio = no reintentar (isBusiness: true)
 * - 5xx o errores de red = error transitorio, reintento con espera exponencial y variación aleatoria (isBusiness: false)
 */
export function classifySyncError(err: any): ClassifiedError {
  let status = 0;
  let code = 'UNKNOWN_ERROR';
  let type: string | undefined;
  let message = '';

  if (err) {
    if (typeof err.status === 'number') {
      status = err.status;
    }
    const body = err.error || err;
    if (body && typeof body === 'object') {
      if (typeof body.status === 'number') status = body.status;
      if (body.code) code = String(body.code);
      else if (body.title && typeof body.title === 'string' && body.title.length < 50 && !body.title.includes(' ')) {
        code = body.title;
      }
      if (body.type) type = String(body.type);
      if (body.detail) message = String(body.detail);
      else if (body.title) message = String(body.title);
      else if (body.message) message = String(body.message);
    }
    if (!message && err.message) {
      message = String(err.message);
    }
  }

  const isBusiness =
    status === 409 ||
    status === 422 ||
    status === 403 ||
    status === 400 ||
    status === 404 ||
    code === 'ASSIGNMENT_CHANGED' ||
    code === 'IDEMPOTENCY_CONFLICT' ||
    code === 'QUANTITY_EXCEEDED' ||
    code === 'UNAUTHORIZED_ROLE' ||
    code === 'UNAUTHORIZED_ASSIGNMENT' ||
    code === 'UNIT_NOT_FOUND' ||
    code === 'TOKEN_REVOKED_OR_EXPIRED';

  return {
    isBusiness,
    status,
    code,
    type,
    message: message || (isBusiness ? 'Conflicto o rechazo de negocio en el servidor.' : 'Error de red o conexión.')
  };
}

const DB_NAME = 'istpetdev-driver-outbox-db';
const DB_VERSION = 2;

/**
 * Calcula el hash SHA-256 en formato hexadecimal de un Blob en el navegador.
 */
export async function calculateBlobSha256(blob: Blob): Promise<string> {
  const buffer = await blob.arrayBuffer();
  const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map((b) => b.toString(16).padStart(2, '0')).join('');
}

const DRIVER_ACK_REVISION_KEY = 'istpetdev_driver_ack_route_revision';

@Injectable({
  providedIn: 'root'
})
export class FieldOutboxService {
  private readonly fieldApi = inject<IFieldApi>(FIELD_API);
  private dbPromise: Promise<IDBPDatabase> | null = null;

  // Señales reactivas para la UI
  readonly outboxItems = signal<OutboxItem[]>([]);
  readonly activeAssignment = signal<MobileAssignmentResponse | null>(null);
  readonly isSyncing = signal<boolean>(false);
  readonly isSimulatingOffline = signal<boolean>(false);
  readonly activeDriverNotice = signal<{ revision: number; message: string; routePlanId: string } | null>(null);
  private readonly owner = signal<string | null>(null);
  private readonly legacyPendingCount = signal<number>(0);
  readonly pendingCount = computed(
    () => this.outboxItems().filter((item) => item.status !== 'Complete').length + this.legacyPendingCount()
  );

  constructor() {
    this.init();
  }

  /** La fija MobileAuthService tras /me y la limpia al cerrar sesión. Nunca borra capturas. */
  async setOwner(accountId: string | null): Promise<void> {
    this.owner.set(accountId);
    this.activeAssignment.set(null);
    this.activeDriverNotice.set(null);
    await this.refreshOutboxItems();
    if (accountId) await this.loadCachedAssignment();
  }

  private async init(): Promise<void> {
    await this.refreshOutboxItems();
    await this.loadCachedAssignment();
  }

  private getDb(): Promise<IDBPDatabase> {
    if (!this.dbPromise) {
      this.dbPromise = openDB(DB_NAME, DB_VERSION, {
        upgrade(db, _oldVersion, _newVersion, transaction) {
          let outboxStore: any;
          if (!db.objectStoreNames.contains('outbox_items')) {
            outboxStore = db.createObjectStore('outbox_items', { keyPath: 'operationId' });
          } else {
            outboxStore = transaction.objectStore('outbox_items');
          }
          if (!outboxStore.indexNames.contains('by_owner')) {
            outboxStore.createIndex('by_owner', 'ownerAccountId');
          }
          if (!db.objectStoreNames.contains('assignments')) {
            db.createObjectStore('assignments', { keyPath: 'driverAccountId' });
          }
        }
      });
    }
    return this.dbPromise;
  }

  /**
   * Refresca la señal reactiva de la cola outbox desde IndexedDB.
   */
  async refreshOutboxItems(): Promise<void> {
    const owner = this.owner();
    const db = await this.getDb();
    const allItems: OutboxItem[] = await db.getAll('outbox_items');
    const legacyCount = allItems.filter((item) => !item.ownerAccountId && item.status !== 'Complete').length;
    this.legacyPendingCount.set(legacyCount);

    const items: OutboxItem[] = owner
      ? allItems.filter((item: OutboxItem) => item.ownerAccountId === owner)
      : [];
    // Ordenar de más reciente a más antiguo por capturedAtUtc
    items.sort((a, b) => new Date(b.capturedAtUtc).getTime() - new Date(a.capturedAtUtc).getTime());
    this.outboxItems.set(items);
  }

  /**
   * Guarda o actualiza la asignación descargada en el store local.
   */
  async cacheAssignment(assignment: MobileAssignmentResponse): Promise<void> {
    const db = await this.getDb();
    await db.put('assignments', assignment);
    this.activeAssignment.set(assignment);
    this.checkDriverNotices(assignment);
  }

  /**
   * Carga la última asignación guardada localmente para permitir inicio 100% offline.
   */
  async loadCachedAssignment(): Promise<MobileAssignmentResponse | null> {
    const owner = this.owner();
    if (!owner) return null;
    const db = await this.getDb();
    const assignment = (await db.get('assignments', owner)) as MobileAssignmentResponse | undefined;
    if (!assignment) return null;
    this.activeAssignment.set(assignment);
    this.checkDriverNotices(assignment);
    return assignment;
  }

  /**
   * Evalúa si existe un aviso de revisión de ruta (driverMessage) no reconocido aún (PA-02).
   */
  checkDriverNotices(assignment: MobileAssignmentResponse): void {
    if (!assignment || !assignment.routes) return;
    try {
      const lastAckStr = localStorage.getItem(DRIVER_ACK_REVISION_KEY);
      const lastAck = lastAckStr ? parseInt(lastAckStr, 10) : 0;

      for (const route of assignment.routes) {
        if (route.driverMessage && route.revision > lastAck) {
          this.activeDriverNotice.set({
            revision: route.revision,
            message: route.driverMessage,
            routePlanId: route.routePlanId
          });
          break;
        }
      }
    } catch {
      // Ignorar fallo de localStorage
    }
  }

  /**
   * Marca el aviso de revisión de ruta como reconocido para no insistir (PA-02).
   */
  acknowledgeNotice(revision: number): void {
    try {
      localStorage.setItem(DRIVER_ACK_REVISION_KEY, revision.toString());
    } catch {
      // Ignorar fallo de storage
    }
    this.activeDriverNotice.set(null);
  }

  /**
   * Descarga la asignación activa desde el API (o mock) y la persiste localmente.
   */
  async downloadRouteAssignment(): Promise<MobileAssignmentResponse> {
    return new Promise((resolve, reject) => {
      this.fieldApi.getAssignments().subscribe({
        next: async (assignment: MobileAssignmentResponse) => {
          await this.cacheAssignment(assignment);
          resolve(assignment);
        },
        error: (err: unknown) => reject(err)
      });
    });
  }

  /**
   * Guarda una captura de recepción completa (datos + foto + firma) en IndexedDB
   * en una ÚNICA transacción atómica antes de cualquier intento de sincronización.
   * Cumple con S-05 y NFR-01.
   */
  async saveReceiptAtomic(params: {
    deliveryId: string;
    expectedDeliveryVersion: number;
    recipientName: string;
    notes?: string;
    lines: {
      deliveryLineId: string;
      lotId: string;
      quantityAccepted: number;
      quantityRejected: number;
      unit: string;
      remainingQuantity?: number;
      nextRequestedQuantity?: number;
    }[];
    photoBlob?: Blob;
    signatureBlob?: Blob;
  }): Promise<OutboxItem> {
    const ownerAccountId = this.owner();
    if (!ownerAccountId) {
      throw new Error('Inicia sesión para registrar una recepción.');
    }

    const operationId = generateUuidV7();
    const capturedAtUtc = new Date().toISOString();

    let photoSha256: string | undefined;
    if (params.photoBlob) {
      photoSha256 = await calculateBlobSha256(params.photoBlob);
    }

    let signatureSha256: string | undefined;
    if (params.signatureBlob) {
      signatureSha256 = await calculateBlobSha256(params.signatureBlob);
    }

    const payload: CreateReceiptRequest = {
      operationId,
      expectedDeliveryVersion: params.expectedDeliveryVersion,
      occurredAtDevice: capturedAtUtc,
      lines: params.lines
    };

    const outboxItem: OutboxItem = {
      operationId,
      ownerAccountId,
      deliveryId: params.deliveryId,
      expectedDeliveryVersion: params.expectedDeliveryVersion,
      recipientName: params.recipientName,
      notes: params.notes,
      capturedAtUtc,
      status: 'SavedOnDevice',
      receiptPayload: payload,
      evidencePhotoBlob: params.photoBlob,
      photoSha256,
      evidenceSignatureBlob: params.signatureBlob,
      signatureSha256
    };

    // Guardado atómico en una sola transacción
    const db = await this.getDb();
    const tx = db.transaction('outbox_items', 'readwrite');
    await tx.objectStore('outbox_items').put(outboxItem);
    await tx.done;

    await this.refreshOutboxItems();

    // Si no estamos simulando offline ni desconectados, intentamos sincronizar en background
    if (!this.isSimulatingOffline() && navigator.onLine) {
      this.syncItem(operationId).catch(() => {
        // En caso de fallo transitorio, queda en SavedOnDevice
      });
    }

    return outboxItem;
  }

  /**
   * Ejecuta la sincronización completa de un ítem de la cola outbox contra el API.
   * Flujo:
   * 1. createReceipt -> 2. uploadEvidence foto -> confirmEvidence -> 3. uploadEvidence firma -> confirmEvidence -> Complete.
   */
  async syncItem(operationId: string): Promise<OutboxItem> {
    const db = await this.getDb();
    const item = await db.get('outbox_items', operationId);
    if (!item) {
      throw new Error(`Ítem ${operationId} no encontrado en outbox.`);
    }

    if (item.ownerAccountId !== this.owner()) {
      throw new Error('La captura pertenece a otra cuenta.');
    }

    if (this.isSimulatingOffline() || !navigator.onLine) {
      item.status = 'SavedOnDevice';
      await db.put('outbox_items', item);
      await this.refreshOutboxItems();
      return item;
    }

    try {
      // Paso 1: Enviar recepción
      item.status = 'Sending';
      item.errorCode = undefined;
      item.errorMessage = undefined;
      await db.put('outbox_items', item);
      await this.refreshOutboxItems();

      const receiptResponse = await this.sendReceiptRequestWithRetry(item);
      item.receiptResponse = receiptResponse;
      item.status = 'EvidencePending';
      await db.put('outbox_items', item);
      await this.refreshOutboxItems();

      const receiptId = receiptResponse.receiptId;

      // Paso 2: Subir foto de evidencia con reintentos y retroceso exponencial con jitter (PA-02, F5)
      if (item.evidencePhotoBlob && item.photoSha256 && !item.photoEvidenceId) {
        const photoUpload = await this.uploadAndConfirmEvidenceWithRetry(
          receiptId,
          'photo',
          item.evidencePhotoBlob,
          item.photoSha256,
          generateUuidV7()
        );
        item.photoEvidenceId = photoUpload.evidenceId;
        await db.put('outbox_items', item);
      }

      // Paso 3: Subir firma con reintentos y retroceso exponencial con jitter (PA-02, F5)
      if (item.evidenceSignatureBlob && item.signatureSha256 && !item.signatureEvidenceId) {
        const sigUpload = await this.uploadAndConfirmEvidenceWithRetry(
          receiptId,
          'signature',
          item.evidenceSignatureBlob,
          item.signatureSha256,
          generateUuidV7()
        );
        item.signatureEvidenceId = sigUpload.evidenceId;
        await db.put('outbox_items', item);
      }

      // Paso 4: Finalizado con éxito
      item.status = 'Complete';
      await db.put('outbox_items', item);
      await this.refreshOutboxItems();
      return item;
    } catch (err: any) {
      const errorInfo = classifySyncError(err);
      item.lastAttemptUtc = new Date().toISOString();
      item.errorCode = errorInfo.code;
      item.errorMessage = errorInfo.message;
      item.problemType = errorInfo.type;

      if (errorInfo.isBusiness) {
        // Tarea 1 & ESC-23: Errores de negocio (409/422/403) NO se reintentan;
        // pasan a estado «requiere revisión» con el motivo y la cola sigue.
        item.status = 'RequiresReview';
      } else {
        // Errores transitorios (5xx o red): tras agotar reintentos con variación,
        // permanece en SavedOnDevice para posterior intento.
        item.retryCount = (item.retryCount ?? 0) + 1;
        item.status = 'SavedOnDevice';
      }

      await db.put('outbox_items', item);
      await this.refreshOutboxItems();
      throw err;
    }
  }

  /**
   * Sincroniza todos los ítems pendientes en cola secuencialmente.
   */
  async syncAllPending(): Promise<void> {
    if (this.isSyncing() || this.isSimulatingOffline() || !navigator.onLine) {
      return;
    }

    this.isSyncing.set(true);
    try {
      const items = this.outboxItems().filter(
        (i) => i.status === 'SavedOnDevice' || i.status === 'EvidencePending' || i.status === 'Sending'
      );

      for (const it of items) {
        try {
          await this.syncItem(it.operationId);
        } catch {
          // Si uno falla, continúa con los demás si es posible
        }
      }
    } finally {
      this.isSyncing.set(false);
      await this.refreshOutboxItems();
    }
  }

  private async sendReceiptRequestWithRetry(item: OutboxItem, maxRetries = 3): Promise<ReceiptResponse> {
    let attempt = 0;
    let baseDelayMs = 400;

    while (true) {
      try {
        return await this.sendReceiptRequest(item);
      } catch (err: any) {
        const errorInfo = classifySyncError(err);
        // Regla: 409/422/403 de negocio NUNCA se reintentan
        if (errorInfo.isBusiness) {
          throw err;
        }

        attempt++;
        if (attempt >= maxRetries) {
          throw err;
        }

        // Variación aleatoria (jitter de 80% a 120%)
        const jitter = 0.8 + Math.random() * 0.4;
        const delayMs = Math.round(Math.min(3000, baseDelayMs * Math.pow(2, attempt - 1)) * jitter);
        await new Promise((resolve) => setTimeout(resolve, delayMs));
      }
    }
  }

  private sendReceiptRequest(item: OutboxItem): Promise<ReceiptResponse> {
    return new Promise((resolve, reject) => {
      this.fieldApi.createReceipt(item.deliveryId, item.receiptPayload, item.operationId).subscribe({
        next: (res) => resolve(res),
        error: (err) => reject(err)
      });
    });
  }

  private uploadAndConfirmEvidence(
    receiptId: string,
    kind: 'photo' | 'signature',
    blob: Blob,
    sha256: string,
    operationId: string
  ): Promise<{ evidenceId: string }> {
    return new Promise((resolve, reject) => {
      // 1. Solicitar URL / registro de carga
      this.fieldApi
        .uploadEvidence(
          receiptId,
          {
            operationId,
            kind,
            mimeType: blob.type || (kind === 'signature' ? 'image/png' : 'image/jpeg'),
            byteSize: blob.size,
            sha256
          },
          blob
        )
        .subscribe({
          next: (uploadRes) => {
            // 2. Confirmar evidencia con el servidor
            this.fieldApi
              .confirmEvidence(uploadRes.evidenceId, {
                operationId: generateUuidV7(),
                sha256,
                byteSize: blob.size
              })
              .subscribe({
                next: () => resolve({ evidenceId: uploadRes.evidenceId }),
                error: (err) => reject(err)
              });
          },
          error: (err) => reject(err)
        });
    });
  }

  /**
   * Sube y confirma evidencia binaria implementando reintentos con retroceso exponencial y jitter (PA-02, F5).
   */
  private async uploadAndConfirmEvidenceWithRetry(
    receiptId: string,
    kind: 'photo' | 'signature',
    blob: Blob,
    sha256: string,
    operationId: string,
    maxRetries = 3
  ): Promise<{ evidenceId: string }> {
    let attempt = 0;
    let delayMs = 500;

    while (attempt < maxRetries) {
      try {
        return await this.uploadAndConfirmEvidence(receiptId, kind, blob, sha256, operationId);
      } catch (err) {
        const errorInfo = classifySyncError(err);
        if (errorInfo.isBusiness) {
          throw err;
        }
        attempt++;
        if (attempt >= maxRetries) {
          throw err;
        }
        const jitter = 0.8 + Math.random() * 0.4;
        await new Promise((resolve) => setTimeout(resolve, Math.round(delayMs * jitter)));
        delayMs *= 2;
      }
    }
    throw new Error(`Excedido límite de reintentos para subir evidencia ${kind}.`);
  }

  /**
   * Resuelve o reintenta manualmente una captura en estado 'RequiresReview' o 'Conflict'.
   * Si se especifica una nueva versión, se adapta la versión de la entrega (FR-12).
   */
  async resolveRequiresReview(operationId: string, newVersion?: number): Promise<OutboxItem> {
    const db = await this.getDb();
    const item = await db.get('outbox_items', operationId);
    if (!item) {
      throw new Error(`Ítem ${operationId} no encontrado en la cola outbox.`);
    }

    if (newVersion !== undefined) {
      item.expectedDeliveryVersion = newVersion;
      item.receiptPayload.expectedDeliveryVersion = newVersion;
    }

    item.status = 'SavedOnDevice';
    item.errorCode = undefined;
    item.errorMessage = undefined;
    item.problemType = undefined;
    item.retryCount = 0;

    await db.put('outbox_items', item);
    await this.refreshOutboxItems();

    // Reintentar sincronización inmediata
    return this.syncItem(operationId);
  }

  /**
   * Compatibilidad hacia atrás para resolución de 409 de versión.
   */
  async resolveConflictWithNewVersion(operationId: string, newVersion: number): Promise<OutboxItem> {
    return this.resolveRequiresReview(operationId, newVersion);
  }

  /**
   * Alterna la simulación de offline para probar comportamientos sin conexión.
   */
  toggleOfflineSimulation(): boolean {
    const nextState = !this.isSimulatingOffline();
    this.isSimulatingOffline.set(nextState);
    if (!nextState) {
      this.syncAllPending();
    }
    return nextState;
  }
}
