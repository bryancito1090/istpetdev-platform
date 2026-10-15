import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';
import { RutasPage } from './rutas.page';

describe('RutasPage (FR-21)', () => {
  it('oculta al Auditor las acciones para generar y publicar rutas', () => {
    const plan = {
      id: 'route-1',
      code: 'R-1',
      revision: 1,
      status: 'Proposed',
      vehicleId: 'vehicle-1',
      plannedStartUtc: '2026-10-14T12:00:00Z',
      totalDistanceKm: 0,
      totalDurationMinutes: 0,
      stopsCount: 0,
    };
    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => false } },
        {
          provide: ApiService,
          useValue: {
            getRoutePlans: vi.fn(() => of([plan])),
            getRoutePlanById: vi.fn(() => of({ ...plan, stops: [] })),
            getRoutePlanGeometry: vi.fn(() => of(null)),
            getDrivers: vi.fn(() => of([{ accountId: 'drv-1', displayName: 'Conductor 1' }])),
            getServicePoints: vi.fn(() => of([])),
            getDeliveryOrders: vi.fn(() => of([])),
            getWarehouses: vi.fn(() => of([])),
            getEntityData: vi.fn(() => of([])),
            getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.some(button => /Generar|Publicar/.test(button.textContent ?? ''))).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Pendiente de publicación');
  });

  it('permite al Planificador consultar la bandeja de capturas rechazadas por punto (Issue #114)', () => {
    const rejectedCapture = {
      receiptId: 'rcpt-1',
      deliveryId: 'del-1',
      deliveryCode: 'DEL-001',
      servicePointId: 'sp-1',
      servicePointCode: 'CS-NORTE',
      servicePointName: 'Centro de Salud Norte',
      driverId: 'drv-1',
      driverName: 'Carlos Chofer',
      receivedAtUtc: '2026-10-15T10:00:00Z',
      reason: 'Versión desactualizada o colisión de negocio (ESC-23)',
      status: 'RequiresReview',
      lines: [
        {
          productId: 'prod-1',
          productName: 'Paracetamol 500mg',
          sku: 'MED-001',
          quantityAccepted: 8,
          quantityRejected: 2,
          rejectionReason: 'Cajas deterioradas'
        }
      ]
    };
    const getRejectedCapturesMock = vi.fn(() => of({ items: [rejectedCapture], count: 1 }));

    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => true } },
        {
          provide: ApiService,
          useValue: {
            getRoutePlans: vi.fn(() => of([])),
            getRoutePlanById: vi.fn(() => of(null)),
            getRoutePlanGeometry: vi.fn(() => of(null)),
            getDrivers: vi.fn(() => of([{ accountId: 'drv-1', displayName: 'Conductor 1' }])),
            getServicePoints: vi.fn(() => of([{ id: 'sp-1', code: 'CS-NORTE', name: 'Centro de Salud Norte' }])),
            getDeliveryOrders: vi.fn(() => of([])),
            getWarehouses: vi.fn(() => of([])),
            getEntityData: vi.fn(() => of([])),
            getRejectedCaptures: getRejectedCapturesMock,
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();

    fixture.componentInstance.activeTab.set('rechazos');
    fixture.detectChanges();

    expect(getRejectedCapturesMock).toHaveBeenCalled();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Bandeja de Capturas Rechazadas');
    expect(text).toContain('Centro de Salud Norte');
    expect(text).toContain('Paracetamol 500mg');
    expect(text).toContain('Requiere Revisión');
    expect(text).toContain('DEL-001');
  });
});
