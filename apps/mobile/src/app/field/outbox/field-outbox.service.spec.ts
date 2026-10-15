import { TestBed } from '@angular/core/testing';
import { IDBFactory } from 'fake-indexeddb';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
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
    getAssignments: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    globalThis.indexedDB = new IDBFactory();
    api = {
      createReceipt: vi.fn(() => of({ receiptId: 'r-1' })),
      uploadEvidence: vi.fn(),
      confirmEvidence: vi.fn(),
      getAssignments: vi.fn()
    };
    TestBed.configureTestingModule({
      providers: [{ provide: FIELD_API, useValue: api }]
    });
  });

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

  it('ESC-25: carrera de desactivación de cuenta (403 TOKEN_REVOKED_OR_EXPIRED) aísla la captura en RequiresReview sin perder datos', async () => {
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
    expect(currentItem?.status).toBe('RequiresReview');
    expect(currentItem?.errorCode).toBe('TOKEN_REVOKED_OR_EXPIRED');
    expect(currentItem?.errorMessage).toContain('inactiva');

    // Los datos siguen intactos en el dispositivo
    const db = await (outbox as any).getDb();
    const stored = await db.get('outbox_items', item.operationId);
    expect(stored).toBeDefined();
    expect(stored.receiptPayload.deliveryId || item.deliveryId).toBe('d-deactivated');
  });
});
