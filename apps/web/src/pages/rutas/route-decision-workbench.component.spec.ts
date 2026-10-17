import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService, PlanningDecision } from '@shared/api';
import { RouteDecisionWorkbench } from './route-decision-workbench.component';

const run = (): PlanningDecision => ({
  id: 'decision', version: 1, status: 'Open', acceptedOptionId: null, routeIds: [], orderIds: ['order'], plannedStartUtc: '2026-10-12T13:00:00Z', published: false,
  options: ['A', 'B'].map((id, i) => ({ optionId: id, title: id, description: '', isRecommended: i === 0, carrierType: 'Propia', blockedReason: null,
    routes: [], totalCost: 20 + i * 10, totalDistanceKm: 10 + i, pendingOrdersCount: 0, status: 'Available' }))
});
const accepted = (): PlanningDecision => ({ ...run(), version: 2, status: 'Accepted', acceptedOptionId: 'A', routeIds: ['route1', 'route2'],
  options: run().options.map(o => ({ ...o, status: o.optionId === 'A' ? 'Accepted' : 'Discarded' })) });

function setup(acceptResponse = new Subject<PlanningDecision>(), initialize = true) {
  const api = { getDrivers: vi.fn(() => of([])), preparePlanningDecision: vi.fn(() => of(run())),
    acceptPlanningDecision: vi.fn(() => acceptResponse), getPlanningDecision: vi.fn(() => of(accepted())) };
  TestBed.configureTestingModule({ imports: [RouteDecisionWorkbench], providers: [
    { provide: ApiService, useValue: api }, { provide: AuthSession, useValue: { me: () => ({ accountId: 'test-decision' }) } }
  ] });
  const fixture = TestBed.createComponent(RouteDecisionWorkbench);
  fixture.componentRef.setInput('orderIds', ['order']);
  if (initialize) fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, api, acceptResponse };
}

afterEach(() => { TestBed.resetTestingModule(); localStorage.removeItem('istpet.route-decision.test-decision'); });

describe('Confirmación de alternativas', () => {
  it('explica la entrada sin pedidos y permite ir a seleccionarlos sin una consulta vacía', () => {
    const { component, fixture, api } = setup(new Subject<PlanningDecision>(), false);
    fixture.componentRef.setInput('orderIds', []);
    fixture.detectChanges();
    const requested = vi.fn(); component.requestOrders.subscribe(requested);
    expect(api.preparePlanningDecision).not.toHaveBeenCalled();
    expect(api.getPlanningDecision).not.toHaveBeenCalled();
    expect(component.error()).toBe('');
    expect(fixture.nativeElement.textContent).not.toContain('Consultar estado');
    (fixture.nativeElement.querySelector('[data-testid="decision-empty-state"] button') as HTMLButtonElement).click();
    expect(requested).toHaveBeenCalledOnce();
  });
  it('reintenta consultar el mismo identificador si falla abrir una propuesta guardada', () => {
    const { fixture, api } = setup(new Subject<PlanningDecision>(), false);
    fixture.componentRef.setInput('orderIds', []);
    fixture.componentRef.setInput('decisionId', 'decision');
    api.getPlanningDecision.mockReturnValueOnce(throwError(() => ({ status: 503 })) as never);
    fixture.detectChanges();
    const retry = [...fixture.nativeElement.querySelectorAll('button')].find((button: unknown) => (button as HTMLButtonElement).textContent?.includes('Reintentar consulta')) as HTMLButtonElement;
    retry.click();
    expect(api.getPlanningDecision).toHaveBeenNthCalledWith(1, 'decision');
    expect(api.getPlanningDecision).toHaveBeenNthCalledWith(2, 'decision');
  });
  it('retira una referencia guardada caducada y ofrece seleccionar pedidos', () => {
    const { fixture, api, component } = setup(new Subject<PlanningDecision>(), false);
    localStorage.setItem('istpet.route-decision.test-decision', 'expired-decision');
    fixture.componentRef.setInput('orderIds', []);
    api.getPlanningDecision.mockReturnValueOnce(throwError(() => ({ status: 404 })) as never);
    fixture.detectChanges();
    expect(api.getPlanningDecision).toHaveBeenCalledWith('expired-decision');
    expect(localStorage.getItem('istpet.route-decision.test-decision')).toBeNull();
    expect(component.recoveryId()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Seleccionar pedidos');
    expect(fixture.nativeElement.textContent).not.toContain('Reintentar consulta');
  });
  it('identifica la opción elegida en el botón de aprobación y conserva la revisión previa', () => {
    const { component, fixture, api } = setup();
    const button = () => fixture.nativeElement.querySelector('.decision-footer .primary-button') as HTMLButtonElement;
    expect(button().textContent).toContain('Revisar y aprobar opción A');
    button().click(); fixture.detectChanges();
    expect(button().textContent).toContain('Aprobar opción A');
    expect(api.acceptPlanningDecision).not.toHaveBeenCalled();
    component.selectOption('B'); fixture.detectChanges();
    expect(button().textContent).toContain('Revisar y aprobar opción B');
  });
  it('asigna conductores al vehículo activo encima del mapa y conserva la asignación al cambiar', async () => {
    const { fixture, component } = setup(new Subject<PlanningDecision>(), false);
    fixture.componentRef.setInput('initialAssignments', { 'VEH-01': 'driver-1' });
    fixture.detectChanges();
    const routes = ['VEH-01', 'VEH-02'].map((vehicleCode, i) => ({ id: `vehicle-${i}`, vehicleId: `v${i}`, vehicleCode,
      maxWeight: 1000, maxVolume: 10, orderIds: [], lines: [], stops: [0, 1].map(sequence => ({ sequence,
        name: `Parada ${sequence}`, code: `P-${sequence}`, kind: sequence ? 'Delivery' as const : 'Pickup' as const, position: { lat: 0, lon: sequence }, deltaWeight: 0, deltaVolume: 0, windowStart: '08:00', windowEnd: '18:00', serviceMinutes: 10 })),
      evaluation: { isValid: true, totalDistanceKm: 10, totalDurationMinutes: 20, totalCost: 30,
        maxWeightOnRoute: 20, maxVolumeOnRoute: 1, stops: [], validationErrors: [], warnings: [], coordinates: [[0, 0], [0, 1]] as [number, number][] } }));
    component.decision.update(value => ({ ...value!, options: value!.options.map(option => ({ ...option, routes })) }));
    component.drivers.set([{ accountId: 'driver-1', displayName: 'Conductor uno' }, { accountId: 'driver-2', displayName: 'Conductor dos' }]);
    component.focusVehicle('vehicle-0'); fixture.detectChanges(); await Promise.resolve(); fixture.detectChanges();
    const toolbar = fixture.nativeElement.querySelector('.fleet-list') as HTMLElement;
    const map = fixture.nativeElement.querySelector('.operation-map') as HTMLElement;
    expect(toolbar.compareDocumentPosition(map) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect((toolbar.querySelector('select') as HTMLSelectElement).value).toBe('driver-1');
    component.focusVehicle('vehicle-1'); component.assignDriver('VEH-02', 'driver-2'); fixture.detectChanges(); await Promise.resolve(); fixture.detectChanges();
    expect((toolbar.querySelector('select') as HTMLSelectElement).value).toBe('driver-2');
    component.focusVehicle('vehicle-0'); fixture.detectChanges(); await Promise.resolve(); fixture.detectChanges();
    expect((toolbar.querySelector('select') as HTMLSelectElement).value).toBe('driver-1');
    expect(component.assignments()).toEqual({ 'VEH-01': 'driver-1', 'VEH-02': 'driver-2' });
  });
  it('no descarta antes de la respuesta y bloquea el doble clic', () => {
    const { component, api, acceptResponse } = setup();
    component.accept();
    component.accept();
    expect(api.acceptPlanningDecision).toHaveBeenCalledTimes(1);
    expect(component.decision()?.options.every(o => o.status === 'Available')).toBe(true);
    acceptResponse.next(accepted());
    expect(component.decision()?.routeIds).toEqual(['route1', 'route2']);
    expect(component.decision()?.options[1].status).toBe('Discarded');
    expect(localStorage.getItem('istpet.route-decision.test-decision')).toBe('decision');
  });
  it('recupera la elección del servidor si se pierde la respuesta', () => {
    const { component, api } = setup();
    api.acceptPlanningDecision.mockReturnValue(throwError(() => new Error('network')) as never);
    component.accept();
    expect(api.getPlanningDecision).toHaveBeenCalledWith('decision');
    expect(component.decision()?.status).toBe('Accepted');
    expect(component.busy()).toBe(false);
  });
  it('enfocar un vehículo no cambia la alternativa elegida', () => {
    const { component, fixture } = setup();
    component.selectOption('B');
    component.focusMarker({ id: 'vehicle-route:2', latitude: 0, longitude: 0, title: 'Entrega' });
    fixture.detectChanges();
    expect(component.focusedRouteId()).toBe('vehicle-route');
    expect(component.selected()?.optionId).toBe('B');
    expect(fixture.nativeElement.textContent).toContain('Mayor costo');
  });
});
