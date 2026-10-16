import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { describe, expect, it, beforeEach } from 'vitest';
import { DeliveryFieldStatus, HandlingUnitStatus, MobileAssignmentDelivery } from '@istpetdev/shared-core';
import { FieldOutboxService, OutboxItem } from '../../outbox/field-outbox.service';
import { effectiveDeliveryStatus } from '../../custody-state';
import { ReceiptPage } from './receipt.page';

// #162: turnos de la PWA del conductor y estado monotónico local vs servidor (jsdom, sin navegador real).
describe('ReceiptPage custodia por turnos (#162)', () => {
  let outbox: {
    activeAssignment: ReturnType<typeof signal<unknown>>;
    outboxItems: ReturnType<typeof signal<OutboxItem[]>>;
    saveCustodyCommand: ReturnType<typeof vi.fn>;
    effectiveDeliveryStatus: (deliveryId: string, snapshotStatus: string, unitStatuses: string[]) => string;
    statusLabel: (status: string) => string;
  };

  const delivery = (status: DeliveryFieldStatus, unitStatus: HandlingUnitStatus, unitCount = 1): MobileAssignmentDelivery => ({
    deliveryId: 'd-1',
    sequence: 1,
    pointCode: 'P-1',
    pointName: 'Punto de prueba',
    address: 'Dirección',
    dueAtUtc: new Date().toISOString(),
    status,
    handlingUnits: Array.from({ length: unitCount }, (_, index) => ({
      handlingUnitId: `hu-${index}`,
      code: `HU-${index}`,
      deliveryId: 'd-1',
      status: unitStatus,
      labelUrl: 'https://label',
      lines: [{ deliveryLineId: 'dl-1', lotId: 'lot-1', sku: 'SKU-1', quantity: 10, unit: 'L' }],
      createdAtUtc: new Date().toISOString(),
    })),
  });

  const custodyItem = (deliveryStatus: DeliveryFieldStatus): OutboxItem => ({
    operationId: 'op-1',
    ownerAccountId: 'driver-1',
    commandKind: 'Deliver',
    deliveryId: 'd-1',
    expectedDeliveryVersion: 0,
    recipientName: 'Entrega',
    capturedAtUtc: new Date().toISOString(),
    status: 'Complete',
    custodyResponse: {
      deliveryId: 'd-1',
      status: deliveryStatus,
      handlingUnitIds: ['hu-0'],
      changedAtUtc: new Date().toISOString(),
      custodyEventId: 'ce-1',
    },
  });

  beforeEach(() => {
    outbox = {
      activeAssignment: signal(null),
      outboxItems: signal([]),
      saveCustodyCommand: vi.fn(async () => ({})),
      effectiveDeliveryStatus: (deliveryId, snapshotStatus, unitStatuses) => {
        const accepted = outbox.outboxItems()
          .filter((i) => i.deliveryId === deliveryId && i.custodyResponse)
          .sort((a, b) => new Date(a.capturedAtUtc).getTime() - new Date(b.capturedAtUtc).getTime())
          .pop()?.custodyResponse?.status ?? null;
        return effectiveDeliveryStatus({
          deliveryStatus: snapshotStatus,
          handlingUnits: unitStatuses.map((status) => ({ status })),
          acceptedCustodyStatus: accepted,
        });
      },
      statusLabel: (status) => status,
    };

    TestBed.configureTestingModule({
      imports: [ReceiptPage],
      providers: [
        { provide: FieldOutboxService, useValue: outbox },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ deliveryId: 'd-1' }) } } },
      ],
    });
  });

  function render(del: MobileAssignmentDelivery): { fixture: ReturnType<typeof TestBed.createComponent>; page: ReceiptPage } {
    outbox.activeAssignment.set({
      serverTimeUtc: new Date().toISOString(),
      driverAccountId: 'driver-1',
      routes: [{ routePlanId: 'r-1', revision: 1, downloadedAtUtc: new Date().toISOString(), vehicleCode: 'V-1', stopsCount: 1, deliveries: [del] }],
    });
    const fixture = TestBed.createComponent(ReceiptPage);
    const page = fixture.componentInstance;
    fixture.detectChanges();
    return { fixture, page };
  }

  it('dispatched con bultos ofrece Recoger y guarda el comando Pickup', async () => {
    const { fixture, page } = render(delivery('Dispatched', 'Dispatched'));
    expect(fixture.nativeElement.textContent).toContain('Marcar Recogido');

    await page.pickup();

    expect(outbox.saveCustodyCommand).toHaveBeenCalledWith(expect.objectContaining({ command: 'Pickup', deliveryId: 'd-1' }));
  });

  it('draft o sin bultos muestra Esperando a Bodega y no ofrece acciones indebidas', async () => {
    const { fixture, page } = render(delivery('Draft', 'Sealed', 0));
    expect(fixture.nativeElement.textContent).toContain('Esperando a Bodega');
    expect(fixture.nativeElement.textContent).not.toContain('Marcar Recogido');

    await page.pickup();

    expect(outbox.saveCustodyCommand).not.toHaveBeenCalled();
    expect(page.validationError()).toContain('no está en el estado');
  });

  it('inTransit ofrece Entregar y guarda el comando Deliver', async () => {
    const { fixture, page } = render(delivery('InTransit', 'InTransit'));
    expect(fixture.nativeElement.textContent).toContain('Marcar Entregado');

    await page.deliver();

    expect(outbox.saveCustodyCommand).toHaveBeenCalledWith(expect.objectContaining({ command: 'Deliver' }));
  });

  it('un snapshot Received tras un Deliver confirmado viejo no retrocede: muestra Recibido y sin acciones', async () => {
    outbox.outboxItems.set([custodyItem('Delivered')]);
    const { fixture, page } = render(delivery('Received', 'Received'));
    expect(fixture.nativeElement.textContent).toContain('Recibido en el punto');
    expect(fixture.nativeElement.textContent).not.toContain('Marcar Entregado');

    await page.deliver();

    expect(outbox.saveCustodyCommand).not.toHaveBeenCalled();
  });

  it('legado Delivered con bultos Received se muestra como Recibido', async () => {
    const { fixture } = render(delivery('Delivered', 'Received', 2));
    expect(fixture.nativeElement.textContent).toContain('Recibido en el punto');
    expect(fixture.nativeElement.textContent).not.toContain('Marcar Entregado');
  });

  it('el estado local pendiente se muestra como guardado en el teléfono, no como confirmación', async () => {
    outbox.outboxItems.set([{ ...custodyItem('InTransit'), status: 'SavedOnDevice', custodyResponse: undefined }]);
    const { fixture } = render(delivery('InTransit', 'InTransit'));
    expect(fixture.nativeElement.textContent).toContain('Guardado en este teléfono');
    expect(fixture.nativeElement.textContent).toContain('aún no confirmado por el servidor');
  });
});
