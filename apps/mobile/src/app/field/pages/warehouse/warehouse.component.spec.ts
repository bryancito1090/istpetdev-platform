import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { WarehouseComponent } from './warehouse.component';
import { FIELD_API } from '../../field-api.interface';
import {
  DeliveryListItem,
  DeliveryDetailResponse,
  HandlingUnitResponse
} from '@istpetdev/shared-core';

describe('WarehouseComponent (Spec 004 / Issue #121)', () => {
  let fieldApiMock: {
    getDeliveries: ReturnType<typeof vi.fn>;
    getDelivery: ReturnType<typeof vi.fn>;
    createHandlingUnit: ReturnType<typeof vi.fn>;
    dispatch: ReturnType<typeof vi.fn>;
  };

  const sampleDraftDeliveries: DeliveryListItem[] = [
    {
      deliveryId: 'del-draft-001',
      routePlanId: 'rp-01',
      pointCode: 'P01',
      pointName: 'Hospital General Docente',
      dueAtUtc: new Date().toISOString(),
      status: 'Draft',
      evidenceStatus: 'Pending',
      packagesCount: 0
    }
  ];

  const sampleDeliveryDetail: DeliveryDetailResponse = {
    deliveryId: 'del-draft-001',
    status: 'Draft',
    version: 1,
    pointName: 'Hospital General Docente',
    routePlanId: 'rp-01',
    lines: [
      {
        deliveryLineId: 'line-01',
        lotId: 'lot-01',
        lotCode: 'LOT-MED-2026',
        sku: 'MED-001',
        productName: 'Amoxicilina 500mg',
        plannedQuantity: 40,
        unit: 'caja'
      }
    ],
    handlingUnits: []
  };

  const sampleHuResponse: HandlingUnitResponse = {
    handlingUnitId: 'hu-001',
    code: 'BULTO-001',
    deliveryId: 'del-draft-001',
    status: 'Prepared',
    labelUrl: 'https://istpetdev.doicela.dev/public-trace/hu-001#t=token123',
    token: 'token123',
    lines: [],
    createdAtUtc: new Date().toISOString()
  };

  beforeEach(() => {
    fieldApiMock = {
      getDeliveries: vi.fn().mockReturnValue(of({ items: sampleDraftDeliveries, total: 1 })),
      getDelivery: vi.fn().mockReturnValue(of(sampleDeliveryDetail)),
      createHandlingUnit: vi.fn().mockReturnValue(of(sampleHuResponse)),
      dispatch: vi.fn().mockReturnValue(of({ deliveryId: 'del-draft-001', status: 'Dispatched' }))
    };

    TestBed.configureTestingModule({
      imports: [WarehouseComponent],
      providers: [
        { provide: FIELD_API, useValue: fieldApiMock }
      ]
    });
  });

  it('inicializa y carga entregas en estado Draft', () => {
    const fixture = TestBed.createComponent(WarehouseComponent);
    fixture.detectChanges();

    expect(fieldApiMock.getDeliveries).toHaveBeenCalledWith(undefined, 'Draft');
    expect(fixture.componentInstance.draftDeliveries().length).toBe(1);
    expect(fixture.componentInstance.draftDeliveries()[0].deliveryId).toBe('del-draft-001');
  });

  it('al seleccionar entrega, carga detalle de líneas y bultos', () => {
    const fixture = TestBed.createComponent(WarehouseComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.selectDelivery(sampleDraftDeliveries[0]);

    expect(fieldApiMock.getDelivery).toHaveBeenCalledWith('del-draft-001');
    expect(component.selectedDelivery()).not.toBeNull();
    expect(component.selectedDelivery()?.lines.length).toBe(1);
    expect(component.selectedDelivery()?.lines[0].sku).toBe('MED-001');
  });

  it('prepara bulto y genera código QR a partir de labelUrl', async () => {
    const fixture = TestBed.createComponent(WarehouseComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.selectedDelivery.set(sampleDeliveryDetail);

    component.createHandlingUnit(sampleDeliveryDetail);

    expect(fieldApiMock.createHandlingUnit).toHaveBeenCalledWith(
      'del-draft-001',
      expect.objectContaining({
        lines: [
          {
            deliveryLineId: 'line-01',
            lotId: 'lot-01',
            quantity: 40
          }
        ]
      })
    );

    // Esperar a la resolución asíncrona de QRCode.toDataURL
    await new Promise((r) => setTimeout(r, 50));

    expect(component.createdHandlingUnit()).not.toBeNull();
    expect(component.createdHandlingUnit()?.hu.code).toBe('BULTO-001');
    expect(component.createdHandlingUnit()?.qrDataUrl).toContain('data:image/png;base64');
  });

  it('despacha al vehículo con handlingUnitIds y actualiza la lista', () => {
    const fixture = TestBed.createComponent(WarehouseComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    const detailWithHu: DeliveryDetailResponse = {
      ...sampleDeliveryDetail,
      handlingUnits: [
        { handlingUnitId: 'hu-001', code: 'BULTO-001', status: 'Prepared' }
      ]
    };
    component.selectedDelivery.set(detailWithHu);

    component.dispatchToVehicle(detailWithHu);

    expect(fieldApiMock.dispatch).toHaveBeenCalledWith(
      'del-draft-001',
      expect.objectContaining({
        handlingUnitIds: ['hu-001']
      }),
      expect.any(String)
    );

    expect(component.selectedDelivery()).toBeNull();
    expect(fieldApiMock.getDeliveries).toHaveBeenCalledTimes(2); // init + after dispatch
  });

  describe('printLabel (Issue #161)', () => {
    const originalTitle = 'IstpetDev Móvil — Logística PWA';
    let titleDuringPrint: string[];

    beforeEach(() => {
      document.title = originalTitle;
      titleDuringPrint = [];
      vi.spyOn(window, 'print').mockImplementation(() => {
        titleDuringPrint.push(document.title);
      });
    });

    afterEach(() => {
      vi.restoreAllMocks();
      vi.useRealTimers();
    });

    function createWithHu(code: string) {
      const fixture = TestBed.createComponent(WarehouseComponent);
      fixture.componentInstance.createdHandlingUnit.set({
        hu: { ...sampleHuResponse, code },
        qrDataUrl: 'data:image/png;base64,AAA'
      });
      return fixture.componentInstance;
    }

    it('usa etiqueta-<code>-<fecha> como título del PDF durante la impresión', () => {
      createWithHu('BULTO-001').printLabel();

      expect(window.print).toHaveBeenCalledTimes(1);
      expect(titleDuringPrint[0]).toMatch(/^etiqueta-BULTO-001-\d{12}$/);
    });

    it('restaura el título original al disparar afterprint', () => {
      createWithHu('BULTO-001').printLabel();
      expect(document.title).not.toBe(originalTitle);

      window.dispatchEvent(new Event('afterprint'));

      expect(document.title).toBe(originalTitle);
    });

    it('sin afterprint el título no se restaura por tiempo (Chrome Android)', () => {
      vi.useFakeTimers();
      createWithHu('BULTO-001').printLabel();

      vi.advanceTimersByTime(5000);

      expect(document.title).toMatch(/^etiqueta-BULTO-001-\d{12}$/);
    });

    it('al destruir el componente restaura el título original', () => {
      const fixture = TestBed.createComponent(WarehouseComponent);
      fixture.componentInstance.printLabel();
      expect(document.title).not.toBe(originalTitle);

      fixture.destroy();

      expect(document.title).toBe(originalTitle);
    });

    it('dos impresiones sin afterprint y luego afterprint dejan el título original', () => {
      const component = createWithHu('BULTO-001');
      component.printLabel();
      component.createdHandlingUnit.set({
        hu: { ...sampleHuResponse, code: 'BULTO-002' },
        qrDataUrl: 'data:image/png;base64,AAA'
      });
      component.printLabel();

      window.dispatchEvent(new Event('afterprint'));

      expect(titleDuringPrint[1]).toMatch(/^etiqueta-BULTO-002-\d{12}$/);
      expect(document.title).toBe(originalTitle);
    });

    it('dos bultos distintos producen títulos distintos', () => {
      createWithHu('BULTO-001').printLabel();
      window.dispatchEvent(new Event('afterprint'));
      createWithHu('BULTO-002').printLabel();
      window.dispatchEvent(new Event('afterprint'));

      expect(titleDuringPrint).toHaveLength(2);
      expect(titleDuringPrint[0]).not.toBe(titleDuringPrint[1]);
    });

    it('usa "bulto" si no hay código', () => {
      const fixture = TestBed.createComponent(WarehouseComponent);
      fixture.componentInstance.printLabel();

      expect(titleDuringPrint[0]).toMatch(/^etiqueta-bulto-\d{12}$/);
    });
  });

  it('maneja errores 409 y 422 de forma legible para el operador', () => {
    fieldApiMock.getDeliveries.mockReturnValue(throwError(() => ({
      status: 409,
      error: { code: 'IDEMPOTENCY_CONFLICT', detail: 'Ya despachada' }
    })));

    const fixture = TestBed.createComponent(WarehouseComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.errorMessage()).toContain('ya despachada');
  });
});
