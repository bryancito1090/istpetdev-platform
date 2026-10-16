import { Blob as NodeBlob } from 'node:buffer';
import { TestBed } from '@angular/core/testing';
import { IDBFactory } from 'fake-indexeddb';
import { of, throwError, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { FIELD_API } from '../field-api.interface';
import { FieldOutboxService } from './field-outbox.service';

const receipt = (deliveryId: string) => ({
  deliveryId,
  expectedDeliveryVersion: 1,
  recipientName: 'Receptor de prueba',
  lines: []
});

describe('FieldOutboxService por cuenta (H2, FR-09)', () => {
  let api: {
    createReceipt: ReturnType<typeof vi.fn>;
    uploadEvidence: ReturnType<typeof vi.fn>;
    confirmEvidence: ReturnType<typeof vi.fn>;
    receiveAsReceiver: ReturnType<typeof vi.fn>;
    getReceiverUnit: ReturnType<typeof vi.fn>;
    getAssignments: ReturnType<typeof vi.fn>;
    pickup: ReturnType<typeof vi.fn>;
    deliver: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    vi.stubGlobal('Blob', NodeBlob);
    globalThis.indexedDB = new IDBFactory();
    api = {
      createReceipt: vi.fn(() => of({ receiptId: 'r-1' })),
      uploadEvidence: vi.fn(),
      confirmEvidence: vi.fn(),
      getAssignments: vi.fn(),
      receiveAsReceiver: vi.fn(() => of({ receiptId: 'receiver-receipt' })),
      getReceiverUnit: vi.fn(),
      pickup: vi.fn(() => of({ deliveryId: 'd-1', status: 'InTransit', handlingUnitIds: [], changedAtUtc: new Date().toISOString(), custodyEventId: 'ce-1' })),
      deliver: vi.fn(() => of({ deliveryId: 'd-2', status: 'Delivered', handlingUnitIds: [], changedAtUtc: new Date().toISOString(), custodyEventId: 'ce-2' }))
    };
    TestBed.configureTestingModule({
      providers: [{ provide: FIELD_API, useValue: api }]
    });
  });

  afterEach(() => vi.unstubAllGlobals());

  it('cada cuenta ve y sincroniza solo sus capturas, y cerrar sesión no las borra', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('cuenta-a');
    await outbox.saveReceiptAtomic(receipt('d-1'));
    await outbox.saveReceiptAtomic(receipt('d-2'));
    expect(outbox.pendingCount()).toBe(2);

    await outbox.setOwner(null);
    expect(outbox.outboxItems()).toEqual([]);

    await outbox.setOwner('cuenta-b');
    outbox.isSimulatingOffline.set(false);
    await outbox.syncAllPending();
    expect(outbox.outboxItems()).toEqual([]);
    expect(api.createReceipt).not.toHaveBeenCalled();

    await outbox.setOwner('cuenta-a');
    expect(outbox.pendingCount()).toBe(2);
    expect(outbox.outboxItems().every((item) => item.ownerAccountId === 'cuenta-a')).toBe(true);
  });

  it('015: con la sincronización en espera guarda en el teléfono y no envía hasta liberarla', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    await outbox.setOwner('cuenta-a');
    outbox.setSyncHeld(true);

    const item = await outbox.saveReceiptAtomic(receipt('d-1'));
    await outbox.syncAllPending();
    expect(api.createReceipt).not.toHaveBeenCalled();
    expect(outbox.outboxItems()[0].status).toBe('SavedOnDevice');

    outbox.setSyncHeld(false);
    await outbox.syncAllPending();
    expect(api.createReceipt).toHaveBeenCalledTimes(1);
    expect((await outbox.syncItem(item.operationId)).status).toBe('Complete');
  });

  it('015: pide almacenamiento persistente para que el navegador no borre la cola', async () => {
    const persist = vi.fn(async () => true);
    Object.defineProperty(navigator, 'storage', { value: { persist }, configurable: true });
    try {
      TestBed.inject(FieldOutboxService);
      await vi.waitFor(() => expect(persist).toHaveBeenCalled());
    } finally {
      delete (navigator as { storage?: unknown }).storage;
    }
  });

  it('no captura sin cuenta y no sincroniza una captura ajena', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await expect(outbox.saveReceiptAtomic(receipt('d-1'))).rejects.toThrow('Inicia sesión para registrar una recepción.');

    await outbox.setOwner('cuenta-a');
    const item = await outbox.saveReceiptAtomic(receipt('d-1'));
    await outbox.setOwner('cuenta-b');
    outbox.isSimulatingOffline.set(false);

    await expect(outbox.syncItem(item.operationId)).rejects.toThrow('La captura pertenece a otra cuenta.');
    expect(api.createReceipt).not.toHaveBeenCalled();
  });

  it('verifica que ningún OutboxItem guarda tokens (Review Focus 4)', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('cuenta-test');
    const item = await outbox.saveReceiptAtomic(receipt('d-sec-1'));

    const jsonStr = JSON.stringify(item).toLowerCase();
    expect(jsonStr).not.toContain('access_token');
    expect(jsonStr).not.toContain('id_token');
    expect(jsonStr).not.toContain('refresh_token');
    expect(jsonStr).not.toContain('bearer');
    expect(jsonStr).not.toContain('token');

    const db = await (outbox as any).getDb();
    const storedItem = await db.get('outbox_items', item.operationId);
    const storedJson = JSON.stringify(storedItem).toLowerCase();
    expect(storedJson).not.toContain('access_token');
    expect(storedJson).not.toContain('id_token');
    expect(storedJson).not.toContain('refresh_token');
    expect(storedJson).not.toContain('bearer');
    expect(storedJson).not.toContain('token');
  });

  it('registros previos/legados sin ownerAccountId se conservan, cuentan en pendingCount y no se sincronizan con otra cuenta', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);

    // Insertar directamente un registro legado (sin ownerAccountId) simulando datos de DB_VERSION 1
    const db = await (outbox as any).getDb();
    const legacyItem = {
      operationId: 'op-legacy-123',
      deliveryId: 'd-legacy',
      expectedDeliveryVersion: 1,
      recipientName: 'Receptor Legado',
      capturedAtUtc: new Date().toISOString(),
      status: 'SavedOnDevice',
      receiptPayload: {
        operationId: 'op-legacy-123',
        expectedDeliveryVersion: 1,
        occurredAtDevice: new Date().toISOString(),
        lines: []
      }
    };
    await db.put('outbox_items', legacyItem);

    // Cuenta B inicia sesión
    await outbox.setOwner('cuenta-b');

    // Cuenta en pendingCount general para no perderse
    expect(outbox.pendingCount()).toBeGreaterThanOrEqual(1);

    // No aparece en outboxItems de cuenta-b
    expect(outbox.outboxItems().some((i) => i.operationId === 'op-legacy-123')).toBe(false);

    // syncAllPending de cuenta-b no sincroniza el ítem legado
    outbox.isSimulatingOffline.set(false);
    await outbox.syncAllPending();
    expect(api.createReceipt).not.toHaveBeenCalled();

    // Intento directo de syncItem falla
    await expect(outbox.syncItem('op-legacy-123')).rejects.toThrow('La captura pertenece a otra cuenta.');

    // El registro se conserva en la base de datos intacto (CE-04: no se pierde ninguna captura)
    const stored = await db.get('outbox_items', 'op-legacy-123');
    expect(stored).toBeDefined();
    expect(stored.operationId).toBe('op-legacy-123');
  });

  it('ESC-23: elemento con error de negocio (409/422) pasa a RequiresReview y NO bloquea el resto de la cola', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('conductor-1');

    // Registrar 3 capturas: la primera falla con 409 (colisión HZ-04 / conflicto), las otras dos son válidas
    const item1 = await outbox.saveReceiptAtomic(receipt('d-collision-hz04'));
    const item2 = await outbox.saveReceiptAtomic(receipt('d-success-2'));
    const item3 = await outbox.saveReceiptAtomic(receipt('d-success-3'));

    api.createReceipt.mockImplementation((deliveryId: string) => {
      if (deliveryId === 'd-collision-hz04') {
        const error: any = new Error('Conflicto de negocio HZ-04');
        error.status = 409;
        error.error = {
          status: 409,
          code: 'ASSIGNMENT_CHANGED',
          title: 'ASSIGNMENT_CHANGED',
          type: 'https://istpetdev.local/errors/assignment_changed',
          detail: 'Colisión de asignación o versión en el servidor.'
        };
        return {
          subscribe: ({ error: errCb }: any) => errCb(error)
        };
      }
      return of({ receiptId: `rcpt-${deliveryId}`, deliveryId, deliveryVersion: 2, status: 'Delivered' });
    });

    outbox.isSimulatingOffline.set(false);
    await outbox.syncAllPending();

    const items = outbox.outboxItems();
    const failedItem = items.find((i) => i.deliveryId === 'd-collision-hz04');
    const okItem2 = items.find((i) => i.deliveryId === 'd-success-2');
    const okItem3 = items.find((i) => i.deliveryId === 'd-success-3');

    // El elemento que falló se aisló como «RequiresReview»
    expect(failedItem).toBeDefined();
    expect(failedItem?.status).toBe('RequiresReview');
    expect(failedItem?.errorCode).toBe('ASSIGNMENT_CHANGED');
    expect(failedItem?.problemType).toBe('https://istpetdev.local/errors/assignment_changed');

    // ESC-23: NO bloqueó la cola; las capturas 2 y 3 se sincronizaron con éxito
    expect(okItem2?.status).toBe('Complete');
    expect(okItem3?.status).toBe('Complete');
  });

  it('ESC-25: carrera de desactivación de cuenta (403 TOKEN_REVOKED_OR_EXPIRED) pausa la captura en SessionRequired sin perder datos', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('conductor-deactivated');

    const item = await outbox.saveReceiptAtomic(receipt('d-deactivated'));

    // Servidor responde 403 Forbidden por cuenta inactiva
    api.createReceipt.mockImplementation(() => {
      const error: any = new Error('Cuenta inactiva');
      error.status = 403;
      error.error = {
        status: 403,
        code: 'TOKEN_REVOKED_OR_EXPIRED',
        type: 'https://istpetdev.local/errors/token_revoked_or_expired',
        detail: 'La cuenta del conductor está inactiva o ha sido desactivada.'
      };
      return {
        subscribe: ({ error: errCb }: any) => errCb(error)
      };
    });

    outbox.isSimulatingOffline.set(false);
    await expect(outbox.syncItem(item.operationId)).rejects.toBeDefined();

    const currentItem = outbox.outboxItems().find((i) => i.operationId === item.operationId);
    expect(currentItem?.status).toBe('SessionRequired');
    expect(currentItem?.errorCode).toBe('TOKEN_REVOKED_OR_EXPIRED');
    expect(currentItem?.errorMessage).toContain('inactiva');

    // Los datos siguen intactos en el dispositivo
    const db = await (outbox as any).getDb();
    const stored = await db.get('outbox_items', item.operationId);
    expect(stored).toBeDefined();
    expect(stored.receiptPayload.deliveryId || item.deliveryId).toBe('d-deactivated');
  });
  const receiverCapture = (id = 'unit-1') => ({ ...receipt('d-receiver'),
    receiverContext: { unitId: id, traceToken: 'qr-capability' },
    lines: [{ deliveryLineId: 'line-1', lotId: 'lot-1', unit: 'L', quantityAccepted: 6,
      quantityRejected: 4, rejectionReason: 'Daño', remainingQuantity: 3, nextRequestedQuantity: 20 }]
  });

  it('ESC-06: rechazo exige foto y motivo; persiste foto y cantidades con la identidad del Receptor', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('receiver');
    await expect(outbox.saveReceiptAtomic(receiverCapture())).rejects.toThrow('foto');
    const photo = new Blob(['image-bytes'], { type: 'image/jpeg' });
    const capture = receiverCapture();
    await expect(outbox.saveReceiptAtomic({ ...capture, photoBlob: photo,
      lines: [{ ...capture.lines[0], rejectionReason: 'texto libre' }] })).rejects.toThrow('catálogo');
    const saved = await outbox.saveReceiptAtomic({ ...capture, photoBlob: photo });
    const db = await (outbox as any).getDb();
    const persisted = await db.get('outbox_items', saved.operationId);
    expect(persisted.ownerAccountId).toBe('receiver');
    expect(await persisted.evidencePhotoBlob.text()).toBe('image-bytes');
    expect(persisted.receiptPayload.lines[0]).toMatchObject({ quantityAccepted: 6, quantityRejected: 4,
      remainingQuantity: 3, nextRequestedQuantity: 20, rejectionReason: 'Daño' });
    await expect(outbox.saveReceiptAtomic({ ...capture, photoBlob: photo })).rejects.toThrow('ya tiene');
  });

  it('ESC-04: sincroniza por endpoint Receptor; un fallo de foto no vuelve a asentar recepción', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('receiver');
    const saved = await outbox.saveReceiptAtomic({ ...receiverCapture(), photoBlob: new Blob(['photo']) });
    api.uploadEvidence.mockReturnValue(throwError(() => ({ status: 422, error: { detail: 'Foto inválida' } })));
    outbox.isSimulatingOffline.set(false);
    await expect(outbox.syncItem(saved.operationId)).rejects.toBeDefined();
    expect(api.receiveAsReceiver).toHaveBeenCalledWith('unit-1', 'qr-capability', saved.receiptPayload, saved.operationId);
    expect(api.createReceipt).not.toHaveBeenCalled();
    api.uploadEvidence.mockReturnValue(of({ evidenceId: 'photo-id' }));
    api.confirmEvidence.mockReturnValue(of({ status: 'Confirmed' }));
    const done = await outbox.syncItem(saved.operationId);
    expect(done.status).toBe('Complete');
    expect(api.receiveAsReceiver).toHaveBeenCalledTimes(1);
    expect(api.uploadEvidence.mock.calls[0][1].operationId).toBe(api.uploadEvidence.mock.calls[1][1].operationId);
    await outbox.syncItem(saved.operationId);
    expect(api.receiveAsReceiver).toHaveBeenCalledTimes(1);
  });

  it('snapshot offline del Receptor queda aislado por cuenta y por QR; 403 nunca usa caché', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    await outbox.setOwner('receiver-a');
    api.getReceiverUnit.mockReturnValue(of({ handlingUnitId: 'unit-1', status: 'Dispatched' }));
    expect((await outbox.loadReceiverUnit('unit-1', 'qr')).cached).toBe(false);
    outbox.isSimulatingOffline.set(true);
    expect((await outbox.loadReceiverUnit('unit-1', 'qr')).cached).toBe(true);
    await expect(outbox.loadReceiverUnit('unit-1', 'wrong')).rejects.toThrow('conexión');
    await outbox.setOwner('receiver-b');
    await expect(outbox.loadReceiverUnit('unit-1', 'qr')).rejects.toThrow('conexión');
    await outbox.setOwner('receiver-a');
    outbox.isSimulatingOffline.set(false);
    api.getReceiverUnit.mockReturnValue(throwError(() => ({ status: 403 })));
    await expect(outbox.loadReceiverUnit('unit-1', 'qr')).rejects.toMatchObject({ status: 403 });
  });

  it('cambio de identidad durante recepción detiene envío de foto y no expone capturas ajenas', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('receiver-a');
    const saved = await outbox.saveReceiptAtomic({ ...receiverCapture(), photoBlob: new Blob(['photo']) });
    const response = new Subject<any>();
    api.receiveAsReceiver.mockReturnValue(response);
    outbox.isSimulatingOffline.set(false);
    const syncing = outbox.syncItem(saved.operationId);
    const rejected = expect(syncing).rejects.toBeDefined();
    await vi.waitFor(() => expect(api.receiveAsReceiver).toHaveBeenCalledTimes(1));
    await outbox.setOwner('receiver-b');
    response.next({ receiptId: 'receipt-a' });
    await rejected;
    expect(api.uploadEvidence).not.toHaveBeenCalled();
    expect(outbox.outboxItems()).toEqual([]);
    await expect(outbox.resolveRequiresReview(saved.operationId, 2)).rejects.toThrow('otra cuenta');
    await outbox.setOwner('receiver-a');
    expect(outbox.outboxItems()[0].status).toBe('SessionRequired');
  });

  it('ESC-15: 401 no se reintenta y dos sincronizaciones simultáneas comparten petición', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('receiver');
    const saved = await outbox.saveReceiptAtomic({ ...receiverCapture(), lines: [] });
    api.receiveAsReceiver.mockReturnValue(throwError(() => ({ status: 401 })));
    outbox.isSimulatingOffline.set(false);
    const results = await Promise.allSettled([outbox.syncItem(saved.operationId), outbox.syncItem(saved.operationId)]);
    expect(results.every(r => r.status === 'rejected')).toBe(true);
    expect(api.receiveAsReceiver).toHaveBeenCalledTimes(1);
    expect(outbox.outboxItems()[0].status).toBe('SessionRequired');
    await outbox.syncAllPending();
    expect(api.receiveAsReceiver).toHaveBeenCalledTimes(1);
  });

  // #162: los comandos de custodia usan la misma cola durable y solo se muestran confirmados si el servidor responde.
  it('el comando de recogida se guarda pendiente y solo queda confirmado tras la respuesta del servidor', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('driver-a');
    const item = await outbox.saveCustodyCommand({ deliveryId: 'd-1', command: 'Pickup', label: 'Recogida' });
    expect(item.status).toBe('SavedOnDevice');
    expect(item.commandKind).toBe('Pickup');
    expect(outbox.pendingCount()).toBe(1);
    expect(api.pickup).not.toHaveBeenCalled();

    outbox.isSimulatingOffline.set(false);
    await outbox.syncItem(item.operationId);

    const synced = outbox.outboxItems().find((i) => i.operationId === item.operationId)!;
    expect(synced.status).toBe('Complete');
    expect(synced.custodyResponse?.status).toBe('InTransit');
    expect(api.pickup).toHaveBeenCalledWith('d-1', expect.objectContaining({ operationId: item.operationId }), item.operationId);
    expect(outbox.pendingCount()).toBe(0);
    expect(outbox.latestCustodyStatus('d-1')).toBe('InTransit');
  });

  it('un 409 del servidor deja el comando de custodia en revisión sin perderlo', async () => {
    const outbox = TestBed.inject(FieldOutboxService);
    outbox.isSimulatingOffline.set(true);
    await outbox.setOwner('driver-a');
    const item = await outbox.saveCustodyCommand({ deliveryId: 'd-2', command: 'Deliver', label: 'Entrega' });
    api.deliver.mockReturnValue(throwError(() => ({ status: 409, error: { code: 'INVALID_STATE_TRANSITION', detail: 'Transición inválida' } })));
    outbox.isSimulatingOffline.set(false);

    await expect(outbox.syncItem(item.operationId)).rejects.toBeTruthy();

    const stored = outbox.outboxItems().find((i) => i.operationId === item.operationId)!;
    expect(stored.status).toBe('RequiresReview');
    expect(stored.errorCode).toBe('INVALID_STATE_TRANSITION');
    expect(stored.custodyResponse).toBeUndefined();
  });

});
