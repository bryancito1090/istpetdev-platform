import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService, RoutePlanCancellationDto, RoutePlanDetailDto } from '@shared/api';
import { RutasPage } from './rutas.page';

// #199: «Cancelar ruta» en el detalle de una ruta propuesta o publicada sin despacho.
describe('RutasPage - cancelar ruta (#199)', () => {
  const detail = (status: string, deliveryStatus?: string): RoutePlanDetailDto => ({
    id: 'plan-1',
    code: 'WIZ-001',
    revision: 1,
    status,
    vehicleId: 'veh-1',
    vehicleCode: 'VEH-01',
    plannedStartUtc: '2026-10-17T13:00:00Z',
    totalDistanceKm: 10,
    totalDurationMinutes: 60,
    polyline: '',
    stops: deliveryStatus
      ? [{ sequence: 1, servicePointId: 'sp-1', servicePointCode: 'HOS-01', servicePointName: 'Hospital', etaUtc: '2026-10-17T13:30:00Z', status: 'Pending', deliveryStatus }]
      : []
  });

  const cancelled: RoutePlanCancellationDto = {
    routePlanId: 'plan-1',
    status: 'Cancelled',
    alreadyCancelled: false,
    cancelledDeliveries: 1,
    releasedReservedQuantity: 100,
    orderLines: [
      { orderId: 'ord-1', orderNumber: 'PED-001', sku: 'ALC-01', releasedQuantity: 100, plannedQuantity: 50, pendingQuantity: 100, orderStatus: 'Pending' }
    ]
  };

  const setup = (canWrite = true) => {
    const apiMock = {
      getRoutePlans: vi.fn(() => of([])),
      getRoutePlanById: vi.fn(() => of(null)),
      getRoutePlanGeometry: vi.fn(() => of(null)),
      getDrivers: vi.fn(() => of([])),
      getServicePoints: vi.fn(() => of([])),
      getWarehouses: vi.fn(() => of([])),
      getDeliveryOrders: vi.fn(() => of([])),
      getEntityData: vi.fn(() => of([])),
      getRejectedCaptures: vi.fn(() => of({ items: [], count: 0 })),
      cancelRoutePlan: vi.fn((_id: string) => of(cancelled))
    };
    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => canWrite } },
        { provide: ApiService, useValue: apiMock }
      ]
    });
    const fixture = TestBed.createComponent(RutasPage);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, apiMock };
  };

  afterEach(() => vi.restoreAllMocks());

  it('solo permite cancelar rutas propuestas o publicadas sin entregas despachadas', () => {
    const { component } = setup();
    expect(component.canCancelPlan(detail('Proposed'))).toBe(true);
    expect(component.canCancelPlan(detail('Published', 'Draft'))).toBe(true);
    expect(component.canCancelPlan(detail('Published', 'Dispatched'))).toBe(false);
    expect(component.canCancelPlan(detail('Cancelled'))).toBe(false);
    expect(component.canCancelPlan(detail('Superseded'))).toBe(false);
  });

  it('muestra el botón en el detalle de una ruta publicada y lo oculta sin permiso de escritura', () => {
    // El detalle se muestra cuando hay rutas en la lista.
    const summary = { id: 'plan-1', code: 'WIZ-001', status: 'Published', vehicleCode: 'VEH-01', totalDistanceKm: 10 } as any;
    const writer = setup();
    writer.component.routePlans.set([summary]);
    writer.component.selectedDetail.set(detail('Published', 'Draft'));
    writer.fixture.detectChanges();
    expect(writer.fixture.nativeElement.querySelector('[data-testid="btn-cancel-plan"]')).not.toBeNull();

    TestBed.resetTestingModule();
    const reader = setup(false);
    reader.component.routePlans.set([summary]);
    reader.component.selectedDetail.set(detail('Published', 'Draft'));
    reader.fixture.detectChanges();
    expect(reader.fixture.nativeElement.querySelector('[data-testid="btn-cancel-plan"]')).toBeNull();
  });

  it('pide confirmación, llama a la API y muestra lo liberado y devuelto', () => {
    const { component, apiMock } = setup();
    component.selectedDetail.set(detail('Published', 'Draft'));
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(true);

    component.cancelPlan('plan-1');

    expect(confirm).toHaveBeenCalledTimes(1);
    expect(apiMock.cancelRoutePlan).toHaveBeenCalledWith('plan-1');
    const message = component.feedbackMessage();
    expect(message?.type).toBe('success');
    expect(message?.text).toContain('1 entregas canceladas y 100 unidades liberadas');
    expect(message?.text).toContain('PED-001 ALC-01: 100 devueltas (pendiente 100)');
  });

  it('no llama a la API si no se confirma', () => {
    const { component, apiMock } = setup();
    component.selectedDetail.set(detail('Proposed'));
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    component.cancelPlan('plan-1');

    expect(apiMock.cancelRoutePlan).not.toHaveBeenCalled();
  });

  it('muestra el motivo del 409 cuando la ruta ya salió', () => {
    const { component, apiMock } = setup();
    component.selectedDetail.set(detail('Published', 'Draft'));
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    apiMock.cancelRoutePlan.mockReturnValue(throwError(() => ({
      status: 409,
      error: { title: 'cancel_conflict', detail: 'No se puede cancelar: la ruta tiene entregas despachadas, entregadas o recibidas.' }
    })));

    component.cancelPlan('plan-1');

    expect(component.feedbackMessage()).toEqual({
      type: 'error',
      text: 'No se puede cancelar: la ruta tiene entregas despachadas, entregadas o recibidas.'
    });
    expect(component.actionLoading()).toBe(false);
  });
});
