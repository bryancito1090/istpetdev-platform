import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { AlertController } from '@ionic/angular/standalone';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { HomePage } from './home.page';
import { MobileAuthService } from '../auth';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { FIELD_API } from '../field/field-api.interface';

describe('Receptor: recepción parcial y evidencia #115', () => {
  let page: HomePage;
  let outbox: { saveReceiptAtomic: ReturnType<typeof vi.fn>; loadReceiverUnit: ReturnType<typeof vi.fn>; pendingCount: ReturnType<typeof signal<number>> };
  const unit = { handlingUnitId: 'hu', code: 'BULTO', status: 'Dispatched', deliveryId: 'delivery', deliveryVersion: 3,
    destinationName: 'Hospital', lines: [{ deliveryLineId: 'line', lotId: 'lot', lotCode: 'L-01', sku: 'SKU', productName: 'Insumo', quantity: 10, unit: 'L' }] };
  beforeEach(async () => {
    outbox = { pendingCount: signal(0), saveReceiptAtomic: vi.fn(async () => ({})),
      loadReceiverUnit: vi.fn(async () => ({ unit, cached: true })) };
    TestBed.configureTestingModule({ imports: [HomePage], providers: [
      { provide: MobileAuthService, useValue: { currentRole: signal('Receptor'), me: signal({ displayName: 'Receptor' }) } },
      { provide: FieldOutboxService, useValue: outbox }, { provide: FIELD_API, useValue: {} },
      { provide: Router, useValue: { navigate: vi.fn() } }, { provide: AlertController, useValue: {} }
    ] });
    page = TestBed.createComponent(HomePage).componentInstance;
    await page.loadReceiverUnit('hu', 'qr');
  });

  it('valida motivo y foto antes de guardar 6 aceptadas y 4 rechazadas', async () => {
    page.receiverLines[0].quantityAccepted = 6;
    await page.confirmReceiverAction();
    expect(page.receiverError()).toContain('motivo');
    page.receiverLines[0].rejectionReason = 'Daño';
    await page.confirmReceiverAction();
    expect(page.receiverError()).toContain('foto');
    expect(outbox.saveReceiptAtomic).not.toHaveBeenCalled();
    page.receiverPhoto.set(new File(['image'], 'photo.jpg', { type: 'image/jpeg' }));
    page.receiverLines[0].remainingQuantity = 2;
    page.receiverLines[0].nextRequestedQuantity = 15;
    await page.confirmReceiverAction();
    expect(outbox.saveReceiptAtomic).toHaveBeenCalledWith(expect.objectContaining({
      expectedDeliveryVersion: 3, receiverContext: { unitId: 'hu', traceToken: 'qr' },
      lines: [expect.objectContaining({ quantityAccepted: 6, quantityRejected: 4,
        rejectionReason: 'Daño', remainingQuantity: 2, nextRequestedQuantity: 15 })]
    }));
    expect(page.receiverConfirmedMessage()).toContain('teléfono');
    await page.confirmReceiverAction();
    expect(outbox.saveReceiptAtomic).toHaveBeenCalledTimes(1);
  });

  it('conforme no exige foto y cantidades negativas, vacías o superiores se rechazan', async () => {
    for (const quantity of [-1, 11, NaN, null]) {
      page.receiverLines[0].quantityAccepted = quantity as number;
      await page.confirmReceiverAction();
      expect(page.receiverError()).toContain('cantidades');
    }
    expect(outbox.saveReceiptAtomic).not.toHaveBeenCalled();
    page.receiverLines[0].quantityAccepted = 10;
    await page.confirmReceiverAction();
    expect(outbox.saveReceiptAtomic).toHaveBeenCalledWith(expect.objectContaining({
      lines: [expect.objectContaining({ quantityAccepted: 10, quantityRejected: 0, rejectionReason: undefined })]
    }));
  });

  it('seleccionar un archivo inválido elimina la foto anterior', () => {
    page.receiverPhoto.set(new File(['old'], 'old.jpg', { type: 'image/jpeg' }));
    page.onReceiverPhoto({ target: { files: [new File(['pdf'], 'evidence.pdf', { type: 'application/pdf' })] } } as unknown as Event);
    expect(page.receiverPhoto()).toBeNull();
    expect(page.receiverError()).toContain('imagen');
  });
});
