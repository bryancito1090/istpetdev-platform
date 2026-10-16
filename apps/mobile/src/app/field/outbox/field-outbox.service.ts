import { Injectable, computed, inject, signal } from '@angular/core';
import { openDB, IDBPDatabase } from 'idb';
import {
  CreateReceiptRequest,
  CustodyTransitionResponse,
  DispatchResponse,
  ReceiptResponse,
  MobileAssignmentResponse,
  PwaSyncState,
  FieldProblemDetails,
  ReceiptLineRequest,
  RECEIPT_REJECTION_REASONS
} from '@istpetdev/shared-core';
import { FIELD_API, IFieldApi, ReceiverUnit } from '../field-api.interface';
import { generateUuidV7 } from '../uuid';
import { effectiveDeliveryStatus } from '../custody-state';

export type OutboxCommandKind = 'Receipt' | 'Pickup' | 'Deliver' | 'Dispatch';

export interface OutboxItem {
  operationId: string; // UUID v7
  ownerAccountId: string; // FR-09: cuenta que capturó; otra cuenta no la ve ni la sincroniza
  receiverContext?: { unitId: string; traceToken: string };
  commandKind?: OutboxCommandKind; // #162; ausente = Receipt (compatibilidad)
  deliveryId: string;
  expectedDeliveryVersion: number;
  recipientName: string;
  notes?: string;
  capturedAtUtc: string;
  status: PwaSyncState;
  receiptPayload?: CreateReceiptRequest;
  dispatchPayload?: { handlingUnitIds: string[] };

  // Evidencias binarias
  evidencePhotoBlob?: Blob;
  photoSha256?: string;
  evidenceSignatureBlob?: Blob;
  signatureSha256?: string;

  // Respuestas del servidor
  receiptResponse?: ReceiptResponse;
  custodyResponse?: CustodyTransitionResponse;
  dispatchResponse?: DispatchResponse;
  photoEvidenceId?: string;
  signatureEvidenceId?: string;

  // Error si hubo rechazo/conflicto de negocio o transitorio
  errorCode?: string;
  errorMessage?: string;
  problemType?: string;
  retryCount?: number;
  lastAttemptUtc?: string;
}

export interface SyncErrorItem {
  operationId: string;
  code?: string;
  message: string;
}

export interface SyncSummary {
  attempted: number;
  synced: number;
  failed: number;
  blockedBySimulation?: boolean;
  blockedByOffline?: boolean;
  blockedBySessionHeld?: boolean;
  errors: SyncErrorItem[];
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
export const DRIVER_LAST_DOWNLOAD_TIME_KEY = 'istpetdev_driver_last_download_time';

@Injectable({
  providedIn: 'root'
})
export class FieldOutboxService {
  private readonly fieldApi = inject<IFieldApi>(FIELD_API);
  private readonly running = new Map<string, Promise<OutboxItem>>();
  private sessionRevision = 0;
  private dbPromise: Promise<IDBPDatabase> | null = null;

  // Señales reactivas para la UI
  readonly outboxItems = signal<OutboxItem[]>([]);
  readonly activeAssignment = signal<MobileAssignmentResponse | null>(null);
  readonly isSyncing = signal<boolean>(false);
  readonly isSimulatingOffline = signal<boolean>(false);
  /** 015: la sesión se abrió sin señal; se guarda en el teléfono y no se envía hasta renovar la sesión. */
  readonly syncHeld = signal<boolean>(false);
  readonly activeDriverNotice = signal<{ revision: number; message: string; routePlanId: string } | null>(null);
  readonly lastDownloadTimestamp = signal<number>(this.readInitialDownloadTimestamp());
  private readonly owner = signal<string | null>(null);
  private readonly legacyPendingCount = signal<number>(0);
  readonly legacyCount = this.legacyPendingCount.asReadonly();
  readonly pendingCount = computed(
    () => this.outboxItems().filter((item) => item.status !== 'Complete').length + this.legacyPendingCount()
  );
  readonly sendableCount = computed(() => {
    return this.outboxItems().filter((i) =>
      i.status === 'SavedOnDevice' ||
      i.status === 'EvidencePending' ||
      i.status === 'Sending' ||
      i.status === 'Failed'
    ).length;
  });
  readonly reviewCount = computed(() =>
    this.outboxItems().filter((i) => i.status === 'RequiresReview' || i.status === 'Conflict').length
  );

  private readInitialDownloadTimestamp(): number {
    try {
      const val = typeof localStorage !== 'undefined' ? localStorage.getItem(DRIVER_LAST_DOWNLOAD_TIME_KEY) : null;
      return val ? parseInt(val, 10) : 0;
    } catch {
      return 0;
    }
  }

  recordDownloadTimestamp(timestamp: number = Date.now()): void {
    try {
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(DRIVER_LAST_DOWNLOAD_TIME_KEY, timestamp.toString());
      }
    } catch {
      // Ignorar fallo de storage
    }
    this.lastDownloadTimestamp.set(timestamp);
  }

  constructor() {
    this.init();
    if (typeof window !== 'undefined') {
      window.addEventListener('online', () => { void this.syncAllPending(); });
    }
  }

  /** La fija MobileAuthService tras /me y la limpia al cerrar sesión. Nunca borra capturas. */
  async setOwner(accountId: string | null): Promise<void> {
    this.sessionRevision++;
    this.owner.set(accountId);
    this.activeAssignment.set(null);
    this.activeDriverNotice.set(null);
    if (!accountId) {
      this.lastDownloadTimestamp.set(0);
    }
    await this.refreshOutboxItems();
    if (accountId) await this.loadCachedAssignment();
  }

  /** La fija MobileAuthService: true mientras la sesión no se pueda renovar (015, FR-04). */
  setSyncHeld(held: boolean): void {
    this.syncHeld.set(held);
  }

  /** Sin esto el navegador puede borrar IndexedDB cuando falta espacio (015, FR-07). */
  private async requestPersistentStorage(): Promise<void> {
    try {
      await navigator.storage?.persist?.();
    } catch {
      // Sin soporte: la cola sigue en IndexedDB con el riesgo normal de desalojo.
    }
  }

  private async init(): Promise<void> {
    void this.requestPersistentStorage();
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
    const revision = this.sessionRevision;
    const db = await this.getDb();
    const allItems: OutboxItem[] = await db.getAll('outbox_items');
    const legacyCount = allItems.filter((item) => !item.ownerAccountId && item.status !== 'Complete').length;
    if (revision !== this.sessionRevision) return;
    this.legacyPendingCount.set(legacyCount);

    const items: OutboxItem[] = owner
      ? allItems.filter((item: OutboxItem) => item.ownerAccountId === owner)
      : [];
    // Ordenar de más reciente a más antiguo por capturedAtUtc
    items.sort((a, b) => {
      const diff = new Date(b.capturedAtUtc).getTime() - new Date(a.capturedAtUtc).getTime();
      return diff !== 0 ? diff : b.operationId.localeCompare(a.operationId);
    });
    this.outboxItems.set(items);
  }

  private assertSession(owner: string, revision: number): void {
    if (!owner || this.owner() !== owner || this.sessionRevision !== revision) {
      throw { status: 401, code: 'SESSION_REQUIRED', message: 'La sesión cambió durante la sincronización.' };
    }
  }

  async loadReceiverUnit(unitId: string, traceToken: string): Promise<{ unit: ReceiverUnit; cached: boolean }> {
    const owner = this.owner();
    if (!owner) throw new Error('Inicia sesión para consultar el bulto.');
    const revision = this.sessionRevision;
    const key = `receiver:${owner}:${unitId}:${await calculateBlobSha256(new Blob([traceToken]))}`;
    const db = await this.getDb();
    this.assertSession(owner, revision);
    try {
      if (this.isSimulatingOffline() || !navigator.onLine) throw { status: 0 };
      const unit = await new Promise<ReceiverUnit>((resolve, reject) =>
        this.fieldApi.getReceiverUnit(unitId, traceToken).subscribe({ next: resolve, error: reject }));
      this.assertSession(owner, revision);
      await db.put('assignments', { driverAccountId: key, unit });
      return { unit, cached: false };
    } catch (error) {
      this.assertSession(owner, revision);
      const status = classifySyncError(error).status;
      if (status !== 0 && status < 500) throw error;
      const snapshot = await db.get('assignments', key);
      this.assertSession(owner, revision);
      if (!snapshot) throw new Error('Consulta este bulto con conexión antes de capturarlo sin señal.');
      return { unit: snapshot.unit, cached: true };
    }
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
          this.recordDownloadTimestamp();
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
    receiverContext?: { unitId: string; traceToken: string };
    lines: ReceiptLineRequest[];
    photoBlob?: Blob;
    signatureBlob?: Blob;
  }): Promise<OutboxItem> {
    const ownerAccountId = this.owner();
    if (!ownerAccountId) {
      throw new Error('Inicia sesión para registrar una recepción.');
    }

    if (params.receiverContext && params.lines.some(l => l.quantityRejected > 0)) {
      if (!params.photoBlob) throw new Error('Adjunta una foto cuando hay rechazo.');
      if (params.lines.some(l => l.quantityRejected > 0 && !RECEIPT_REJECTION_REASONS.some(r => r.code === l.rejectionReason))) {
        throw new Error('Selecciona un motivo de catálogo por cada línea rechazada.');
      }
    }
    if (params.receiverContext) {
      await this.refreshOutboxItems();
      if (this.outboxItems().some(i => i.receiverContext?.unitId === params.receiverContext!.unitId))
        throw new Error('Este bulto ya tiene una recepción en la cola de tu cuenta.');
    }
    const sessionRevision = this.sessionRevision;
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
      receiverContext: params.receiverContext,
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

    this.assertSession(ownerAccountId, sessionRevision);
    // Guardado atómico en una sola transacción
    const db = await this.getDb();
    const tx = db.transaction('outbox_items', 'readwrite');
    await tx.objectStore('outbox_items').put(outboxItem);
    await tx.done;

    await this.refreshOutboxItems();

    // Si no estamos simulando offline ni desconectados, intentamos sincronizar en background
    if (this.canSend()) {
      this.syncItem(operationId).catch(() => {
        // En caso de fallo transitorio, queda en SavedOnDevice
      });
    }

    return outboxItem;
  }

  /**
   * #162: guarda localmente un comando de custodia del conductor (recoger/entregar) antes de
   * intentar enviarlo. El ítem se sincroniza con la misma cola, idempotencia y estados que las capturas.
   */
  async saveCustodyCommand(params: { deliveryId: string; command: 'Pickup' | 'Deliver'; label: string }): Promise<OutboxItem> {
    const ownerAccountId = this.owner();
    if (!ownerAccountId) {
      throw new Error('Inicia sesión para registrar la custodia.');
    }

    await this.refreshOutboxItems();
    const pending = this.outboxItems().find((i) =>
      i.deliveryId === params.deliveryId && (i.commandKind ?? 'Receipt') === params.command && i.status !== 'Complete');
    if (pending) {
      throw new Error('Ya hay un comando de custodia pendiente para esta entrega.');
    }

    const sessionRevision = this.sessionRevision;
    const operationId = generateUuidV7();
    const capturedAtUtc = new Date().toISOString();

    const outboxItem: OutboxItem = {
      operationId,
      ownerAccountId,
      commandKind: params.command,
      deliveryId: params.deliveryId,
      expectedDeliveryVersion: 0,
      recipientName: params.label,
      capturedAtUtc,
      status: 'SavedOnDevice'
    };

    this.assertSession(ownerAccountId, sessionRevision);
    const db = await this.getDb();
    const tx = db.transaction('outbox_items', 'readwrite');
    await tx.objectStore('outbox_items').put(outboxItem);
    await tx.done;

    await this.refreshOutboxItems();

    if (!this.isSimulatingOffline() && navigator.onLine) {
      this.syncItem(operationId).catch(() => {
        // Si falla por red queda en SavedOnDevice; el usuario ve el estado real en la cola.
      });
    }

    return outboxItem;
  }

  /**
   * #230: guarda localmente un comando de despacho de bodega antes de enviarlo.
   * Permite que la bodega despache al vehículo sin conexión y se envíe automáticamente al reconectar.
   */
  async saveDispatchCommand(params: { deliveryId: string; handlingUnitIds: string[]; label: string }): Promise<OutboxItem> {
    const ownerAccountId = this.owner();
    if (!ownerAccountId) {
      throw new Error('Inicia sesión para registrar el despacho.');
    }

    await this.refreshOutboxItems();
    const pending = this.outboxItems().find((i) =>
      i.deliveryId === params.deliveryId && i.commandKind === 'Dispatch' && i.status !== 'Complete');
    if (pending) {
      throw new Error('Ya hay un despacho pendiente para esta entrega.');
    }

    const sessionRevision = this.sessionRevision;
    const operationId = generateUuidV7();
    const capturedAtUtc = new Date().toISOString();

    const outboxItem: OutboxItem = {
      operationId,
      ownerAccountId,
      commandKind: 'Dispatch',
      deliveryId: params.deliveryId,
      expectedDeliveryVersion: 0,
      recipientName: params.label,
      dispatchPayload: { handlingUnitIds: params.handlingUnitIds },
      capturedAtUtc,
      status: 'SavedOnDevice'
    };

    this.assertSession(ownerAccountId, sessionRevision);
    const db = await this.getDb();
    const tx = db.transaction('outbox_items', 'readwrite');
    await tx.objectStore('outbox_items').put(outboxItem);
    await tx.done;

    await this.refreshOutboxItems();

    if (!this.isSimulatingOffline() && navigator.onLine) {
      this.syncItem(operationId).catch(() => {
        // Si falla por red queda en SavedOnDevice
      });
    }

    return outboxItem;
  }

  /**
   * Actualiza el estado y unidades de una entrega en la asignación en memoria y caché local.
   */
  updateAssignmentDeliveryStatus(deliveryId: string, status: string, huStatus?: string): void {
    const current = this.activeAssignment();
    if (!current || !current.routes) return;
    let modified = false;
    for (const route of current.routes) {
      for (const del of route.deliveries) {
        if (del.deliveryId === deliveryId) {
          del.status = status as any;
          if (huStatus && del.handlingUnits) {
            del.handlingUnits.forEach((hu) => {
              hu.status = huStatus as any;
            });
          }
          modified = true;
        }
      }
    }
    if (modified) {
      this.activeAssignment.set({ ...current });
      void this.cacheAssignment(current);
    }
  }

  /**
   * Ejecuta la sincronización completa de un ítem de la cola outbox contra el API.
   * Flujo:
   * 1. createReceipt -> 2. uploadEvidence foto -> confirmEvidence -> 3. uploadEvidence firma -> confirmEvidence -> Complete.
   */
  syncItem(operationId: string): Promise<OutboxItem> {
    const running = this.running.get(operationId);
    if (running) return running;
    const result = this.syncItemCore(operationId).finally(() => this.running.delete(operationId));
    this.running.set(operationId, result);
    return result;
  }

  private async syncItemCore(operationId: string): Promise<OutboxItem> {
    const sessionRevision = this.sessionRevision;
    const db = await this.getDb();
    const item = await db.get('outbox_items', operationId);
    if (!item) {
      throw new Error(`Ítem ${operationId} no encontrado en outbox.`);
    }

    if (item.ownerAccountId !== this.owner()) {
      throw new Error('La captura pertenece a otra cuenta.');
    }

    if (item.status === 'Complete') return item;
    const assertSession = () => this.assertSession(item.ownerAccountId, sessionRevision);
    assertSession();

    if (!this.canSend()) {
      item.status = 'SavedOnDevice';
      await db.put('outbox_items', item);
      await this.refreshOutboxItems();
      return item;
    }

    try {
      // Paso 1: Enviar recepción, comando de custodia (#162) o despacho (#230)
      item.status = 'Sending';
      item.errorCode = undefined;
      item.errorMessage = undefined;
      await db.put('outbox_items', item);
      await this.refreshOutboxItems();

      const commandKind = item.commandKind ?? 'Receipt';
      if (commandKind === 'Dispatch') {
        const huIds = item.dispatchPayload?.handlingUnitIds ?? [];
        const dispatchResponse = await new Promise<DispatchResponse>((resolve, reject) => {
          this.fieldApi.dispatch(item.deliveryId, { operationId: item.operationId, handlingUnitIds: huIds }, item.operationId).subscribe({
            next: (res) => resolve(res),
            error: (err) => reject(err)
          });
        });
        assertSession();
        item.dispatchResponse = dispatchResponse;
        item.status = 'Complete';
        await db.put('outbox_items', item);
        await this.refreshOutboxItems();
        this.updateAssignmentDeliveryStatus(item.deliveryId, 'Dispatched', 'Dispatched');
        return item;
      }

      if (commandKind === 'Pickup' || commandKind === 'Deliver') {
        const custodyResponse = await this.sendCustodyCommand(item, commandKind);
        assertSession();
        item.custodyResponse = custodyResponse;
        item.status = 'Complete';
        await db.put('outbox_items', item);
        await this.refreshOutboxItems();
        const targetStatus = commandKind === 'Pickup' ? 'InTransit' : 'Delivered';
        this.updateAssignmentDeliveryStatus(item.deliveryId, targetStatus, targetStatus);
        return item;
      }

      const receiptResponse = item.receiptResponse ?? await this.sendReceiptRequestWithRetry(item, assertSession);
      assertSession();
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
          item.operationId,
          assertSession
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
          item.operationId,
          assertSession
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

      if (errorInfo.status === 401 || (errorInfo.status === 403 && !['UNAUTHORIZED_ASSIGNMENT', 'UNAUTHORIZED_ROLE'].includes(errorInfo.code)) || errorInfo.code === 'SESSION_REQUIRED') {
        item.status = 'SessionRequired';
        item.errorMessage = `Sesión requerida: vuelve a entrar con la cuenta que capturó. La captura se conserva. ${errorInfo.message}`;
      } else if (errorInfo.isBusiness) {
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
   * Sincroniza todos los ítems pendientes en cola secuencialmente con diagnóstico completo.
   */
  async syncAllPending(): Promise<SyncSummary> {
    if (this.isSyncing()) {
      return { attempted: 0, synced: 0, failed: 0, errors: [] };
    }

    if (this.isSimulatingOffline()) {
      return {
        attempted: 0,
        synced: 0,
        failed: 0,
        blockedBySimulation: true,
        errors: [{ operationId: '', code: 'OFFLINE_SIMULATION_ACTIVE', message: 'Desactiva el modo «Sin señal (prueba)» para sincronizar.' }]
      };
    }

    if (typeof navigator !== 'undefined' && !navigator.onLine) {
      return {
        attempted: 0,
        synced: 0,
        failed: 0,
        blockedByOffline: true,
        errors: [{ operationId: '', code: 'NO_NETWORK_CONNECTION', message: 'Sin conexión a Internet.' }]
      };
    }

    if (this.syncHeld()) {
      return {
        attempted: 0,
        synced: 0,
        failed: 0,
        blockedBySessionHeld: true,
        errors: [{ operationId: '', code: 'SESSION_HELD', message: 'Sesión iniciada sin señal; renueva sesión para sincronizar.' }]
      };
    }

    this.isSyncing.set(true);
    const summary: SyncSummary = {
      attempted: 0,
      synced: 0,
      failed: 0,
      errors: []
    };

    try {
      const items = this.outboxItems()
        .filter(
          (i) =>
            i.status === 'SavedOnDevice' ||
            i.status === 'EvidencePending' ||
            i.status === 'Sending' ||
            i.status === 'Failed'
        )
        .sort((a, b) => {
          const diff = new Date(a.capturedAtUtc).getTime() - new Date(b.capturedAtUtc).getTime();
          return diff !== 0 ? diff : a.operationId.localeCompare(b.operationId);
        });

      summary.attempted = items.length;

      for (const it of items) {
        try {
          const res = await this.syncItem(it.operationId);
          if (res.status === 'Complete') {
            summary.synced++;
          } else {
            summary.failed++;
            if (res.errorMessage || res.errorCode) {
              summary.errors.push({
                operationId: it.operationId,
                code: res.errorCode,
                message: res.errorMessage || 'Error en sincronización.'
              });
            }
          }
        } catch (err: any) {
          summary.failed++;
          const classified = classifySyncError(err);
          summary.errors.push({
            operationId: it.operationId,
            code: classified.code,
            message: classified.message
          });
        }
      }
    } finally {
      this.isSyncing.set(false);
      await this.refreshOutboxItems();
    }

    return summary;
  }

  canSend(): boolean {
    return !this.syncHeld() && !this.isSimulatingOffline() && (typeof navigator === 'undefined' || navigator.onLine);
  }

  /**
   * Reclama los ítems legados sin cuenta asociándolos a la cuenta actualmente activa.
   */
  async claimLegacyItems(): Promise<number> {
    const owner = this.owner();
    if (!owner) throw new Error('Inicia sesión para reclamar capturas anteriores.');
    const db = await this.getDb();
    const allItems: OutboxItem[] = await db.getAll('outbox_items');
    const legacy = allItems.filter((i) => !i.ownerAccountId && i.status !== 'Complete');
    for (const item of legacy) {
      item.ownerAccountId = owner;
      item.status = 'SavedOnDevice';
      await db.put('outbox_items', item);
    }
    await this.refreshOutboxItems();
    return legacy.length;
  }

  /**
   * Descarta definitivamente los ítems legados sin cuenta.
   */
  async discardLegacyItems(): Promise<number> {
    const db = await this.getDb();
    const allItems: OutboxItem[] = await db.getAll('outbox_items');
    const legacy = allItems.filter((i) => !i.ownerAccountId && i.status !== 'Complete');
    for (const item of legacy) {
      await db.delete('outbox_items', item.operationId);
    }
    await this.refreshOutboxItems();
    return legacy.length;
  }

  /**
   * Reintenta manualmente un ítem en cola.
   */
  async retryItem(operationId: string): Promise<OutboxItem> {
    const db = await this.getDb();
    const item = await db.get('outbox_items', operationId);
    if (!item) throw new Error(`Ítem ${operationId} no encontrado en outbox.`);
    if (item.ownerAccountId !== this.owner()) throw new Error('La captura pertenece a otra cuenta.');

    item.status = 'SavedOnDevice';
    item.errorCode = undefined;
    item.errorMessage = undefined;
    item.problemType = undefined;
    await db.put('outbox_items', item);
    await this.refreshOutboxItems();
    return this.syncItem(operationId);
  }

  private async sendReceiptRequestWithRetry(item: OutboxItem, assertSession: () => void, maxRetries = 3): Promise<ReceiptResponse> {
    let attempt = 0;
    let baseDelayMs = 400;

    while (true) {
      try {
        assertSession();
        return await this.sendReceiptRequest(item);
      } catch (err: any) {
        const errorInfo = classifySyncError(err);
        // Regla: 409/422/403 de negocio NUNCA se reintentan
        if (errorInfo.isBusiness || errorInfo.status === 401 || errorInfo.code === 'SESSION_REQUIRED') {
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
      const payload = item.receiptPayload;
      if (!payload) {
        reject(new Error('La captura no tiene datos de recepción.'));
        return;
      }
      const request = item.receiverContext
        ? this.fieldApi.receiveAsReceiver(item.receiverContext.unitId, item.receiverContext.traceToken, payload, item.operationId)
        : this.fieldApi.createReceipt(item.deliveryId, payload, item.operationId);
      request.subscribe({
        next: (res) => resolve(res),
        error: (err) => reject(err)
      });
    });
  }

  private sendCustodyCommand(item: OutboxItem, command: 'Pickup' | 'Deliver'): Promise<CustodyTransitionResponse> {
    return new Promise((resolve, reject) => {
      const payload = { operationId: item.operationId, occurredAtDevice: item.capturedAtUtc };
      const request = command === 'Pickup'
        ? this.fieldApi.pickup(item.deliveryId, payload, item.operationId)
        : this.fieldApi.deliver(item.deliveryId, payload, item.operationId);
      request.subscribe({
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
    operationId: string,
    assertSession: () => void
  ): Promise<{ evidenceId: string }> {
    assertSession();
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
            try { assertSession(); } catch (error) { reject(error); return; }
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
    assertSession: () => void,
    maxRetries = 3
  ): Promise<{ evidenceId: string }> {
    let attempt = 0;
    let delayMs = 500;

    while (attempt < maxRetries) {
      try {
        return await this.uploadAndConfirmEvidence(receiptId, kind, blob, sha256, operationId, assertSession);
      } catch (err) {
        const errorInfo = classifySyncError(err);
        if (errorInfo.isBusiness || errorInfo.status === 401 || errorInfo.code === 'SESSION_REQUIRED') {
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

    if (item.ownerAccountId !== this.owner()) throw new Error('La captura pertenece a otra cuenta.');
    if (newVersion !== undefined && item.receiptPayload) {
      // #162: los comandos de custodia no tienen receiptPayload; solo las capturas de recepción
      // adaptan su versión esperada.
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
   * #162: último estado de custodia confirmado por el servidor para una entrega, si existe.
   * Evita mostrar el snapshot viejo como si el servidor ya hubiera aceptado el comando local.
   */
  latestCustodyStatus(deliveryId: string): string | null {
    const accepted = this.outboxItems()
      .filter((i) => i.deliveryId === deliveryId && i.custodyResponse)
      .sort((a, b) => new Date(a.capturedAtUtc).getTime() - new Date(b.capturedAtUtc).getTime())
      .pop();
    return accepted?.custodyResponse?.status ?? null;
  }

  /**
   * #230: estado derivado de comandos de custodia locales guardados en el outbox aún no confirmados por el servidor.
   * Permite que el Conductor avance en su flujo de custodia (Pickup -> Deliver) incluso sin señal.
   */
  latestPendingCustodyStatus(deliveryId: string): string | null {
    const pending = this.outboxItems()
      .filter((i) => i.deliveryId === deliveryId && i.commandKind && i.status !== 'Complete' && i.status !== 'RequiresReview' && i.status !== 'SessionRequired')
      .sort((a, b) => {
        const timeDiff = new Date(a.capturedAtUtc).getTime() - new Date(b.capturedAtUtc).getTime();
        return timeDiff !== 0 ? timeDiff : a.operationId.localeCompare(b.operationId);
      })
      .pop();
    if (!pending) return null;
    if (pending.commandKind === 'Pickup') return 'InTransit';
    if (pending.commandKind === 'Deliver') return 'Delivered';
    if (pending.commandKind === 'Dispatch') return 'Dispatched';
    return null;
  }

  /**
   * #162/#230: estado monotónico de la entrega que combina snapshot, comandos confirmados, comandos
   * locales pendientes en outbox y el estado de los bultos; un snapshot Received/Partial nuevo no retrocede
   * por un Deliver viejo en IndexedDB.
   */
  effectiveDeliveryStatus(deliveryId: string, snapshotStatus: string, handlingUnitStatuses: string[]): string {
    return effectiveDeliveryStatus({
      deliveryStatus: snapshotStatus,
      handlingUnits: handlingUnitStatuses.map((status) => ({ status })),
      acceptedCustodyStatus: this.latestCustodyStatus(deliveryId),
      pendingCustodyStatus: this.latestPendingCustodyStatus(deliveryId),
    });
  }

  /**
   * Etiqueta humana del estado de la cola, compartida por la página de custodia y la cola (#162).
   */
  statusLabel(status: PwaSyncState): string {
    switch (status) {
      case 'SavedOnDevice': return 'Guardado en Teléfono';
      case 'Sending': return 'Enviando...';
      case 'AcceptedByServer': return 'Aceptado por Servidor';
      case 'EvidencePending': return 'Subiendo Evidencias...';
      case 'Complete': return 'Confirmado por el servidor';
      case 'Conflict': return 'Conflicto 409';
      case 'SessionRequired': return 'Sesión requerida';
      case 'RequiresReview': return 'Requiere Revisión';
      case 'Failed': return 'Error de Envío';
      default: return status;
    }
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
