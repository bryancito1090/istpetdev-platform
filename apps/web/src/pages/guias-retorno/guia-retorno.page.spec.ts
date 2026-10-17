import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService, ReturnGuideDto, ReceiveReturnGuideResponse } from '@shared/api';
import { AuthSession } from '@entities/session';
import { GuiaRetornoPage } from './guia-retorno.page';

describe('GuiaRetornoPage (Issue #247)', () => {
  const sampleGuide: ReturnGuideDto = {
    guideNumber: 'GRT-7A8B9C0D',
    routePlanId: '7a8b9c0d-1111-2222-3333-444455556666',
    vehiclePlate: 'PBX-7788',
    driverName: 'Juan Conductor',
    destinationWarehouseId: 'wh-1',
    destinationWarehouseName: 'Bodega Central Quito',
    generatedAtUtc: '2026-10-16T18:00:00Z',
    status: 'PendingWarehouse',
    lines: [
      {
        handlingUnitId: 'hu-1',
        handlingUnitCode: 'HU-RET-101',
        productId: 'prod-1',
        sku: 'MED-AMOX-500',
        productName: 'Amoxicilina 500mg',
        lotId: 'lot-1',
        lotCode: 'LOT-2026-A1',
        quantity: 12,
        unit: 'Caja',
        reason: 'Cliente ausente en destino'
      }
    ]
  };

  const sampleReceiveResponse: ReceiveReturnGuideResponse = {
    guideNumber: 'GRT-7A8B9C0D',
    status: 'ReceivedAtWarehouse',
    receivedAtUtc: '2026-10-16T18:30:00Z',
    itemsReconciled: 1,
    rebalanceOrderId: 'ord-rebalance-999'
  };

  it('renderiza la guía de retorno con metadatos, tabla de bultos y firmas', () => {
    TestBed.configureTestingModule({
      imports: [GuiaRetornoPage],
      providers: [
        {
          provide: ApiService,
          useValue: {
            getReturnGuide: vi.fn(() => of(sampleGuide)),
            receiveReturnGuide: vi.fn(() => of(sampleReceiveResponse))
          }
        },
        {
          provide: AuthSession,
          useValue: {
            role: () => 'Bodega',
            canWrite: () => true
          }
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              params: { guideNumber: 'GRT-7A8B9C0D' },
              queryParams: {}
            }
          }
        },
        {
          provide: Router,
          useValue: {
            navigate: vi.fn()
          }
        }
      ]
    });

    const fixture = TestBed.createComponent(GuiaRetornoPage);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('GUÍA DE RETORNO DE MATERIAL NO ENTREGADO');
    expect(text).toContain('GRT-7A8B9C0D');
    expect(text).toContain('PBX-7788');
    expect(text).toContain('Juan Conductor');
    expect(text).toContain('Bodega Central Quito');
    expect(text).toContain('HU-RET-101');
    expect(text).toContain('MED-AMOX-500');
    expect(text).toContain('12 Caja');
    expect(text).toContain('Cliente ausente en destino');
    expect(text).toContain('Entregado por (Conductor / Custodia en Ruta)');
    expect(text).toContain('Recibido en Bodega (Conciliación de Stock)');
  });

  it('permite realizar la recepción en bodega y muestra confirmación con pedido de reinyección', () => {
    const apiMock = {
      getReturnGuide: vi.fn(() => of(sampleGuide)),
      receiveReturnGuide: vi.fn(() => of(sampleReceiveResponse))
    };

    TestBed.configureTestingModule({
      imports: [GuiaRetornoPage],
      providers: [
        { provide: ApiService, useValue: apiMock },
        { provide: AuthSession, useValue: { role: () => 'Bodega', canWrite: () => true } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              params: { guideNumber: 'GRT-7A8B9C0D' },
              queryParams: {}
            }
          }
        },
        { provide: Router, useValue: { navigate: vi.fn() } }
      ]
    });

    const fixture = TestBed.createComponent(GuiaRetornoPage);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.receiveInWarehouse();
    fixture.detectChanges();

    expect(apiMock.receiveReturnGuide).toHaveBeenCalledWith('GRT-7A8B9C0D');
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Recepción confirmada y stock conciliado');
    expect(text).toContain('ord-rebalance-999');
  });

  it('dispara window.print al hacer clic en Imprimir Guía', () => {
    const printSpy = vi.spyOn(window, 'print').mockImplementation(() => {});

    TestBed.configureTestingModule({
      imports: [GuiaRetornoPage],
      providers: [
        {
          provide: ApiService,
          useValue: {
            getReturnGuide: vi.fn(() => of(sampleGuide))
          }
        },
        { provide: AuthSession, useValue: { role: () => 'Planificador' } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              params: { guideNumber: 'GRT-7A8B9C0D' },
              queryParams: {}
            }
          }
        },
        { provide: Router, useValue: { navigate: vi.fn() } }
      ]
    });

    const fixture = TestBed.createComponent(GuiaRetornoPage);
    fixture.detectChanges();

    fixture.componentInstance.printGuide();
    expect(printSpy).toHaveBeenCalled();
    printSpy.mockRestore();
  });
});
