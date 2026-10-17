import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';
import { RutasPage } from './rutas.page';

describe('Asistente: restricciones y avisos de vehículo (#244)', () => {
  it('muestra el descarte y permite seleccionar el vehículo con aviso extraurbano', () => {
    TestBed.configureTestingModule({
      imports: [RutasPage],
      providers: [provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => true } },
        { provide: ApiService, useValue: { getRoutePlans: () => of([]), getDrivers: () => of([]), getDeliveryOrders: () => of([]), getEntityData: () => of([]), getServicePoints: () => of([]), getWarehouses: () => of([]), getRejectedCaptures: () => of({ items: [] }) } },
      ],
    });
    const fixture = TestBed.createComponent(RutasPage);
    const page = fixture.componentInstance;
    page.isWizardOpen.set(true); page.wizardStep.set(4);
    page.availableVehicleMatches.set([
      { vehicleId: 'heavy', vehicleCode: 'PESADO-244', isMatch: false, discardReason: 'restriccion_horaria (camion_pesado no circula 06:30–09:30; regla sintética)', warnings: [] },
      { vehicleId: 'urban', vehicleCode: 'URBANO-244', isMatch: true, warnings: ['uso_extraurbano (aviso, no descarte)'] },
    ]);
    fixture.detectChanges();
    const cards = Array.from(fixture.nativeElement.querySelectorAll('div.cursor-not-allowed, div.cursor-pointer')) as HTMLElement[];
    const heavy = cards.find(card => card.textContent?.includes('PESADO-244'))!;
    const urban = cards.find(card => card.textContent?.includes('URBANO-244') && card.textContent?.includes('uso_extraurbano'))!;
    expect(heavy.textContent).toContain('Motivo descarte: restriccion_horaria');
    heavy.click(); expect(page.selectedVehicleId()).toBeNull();
    expect(urban.querySelector('[role="status"]')?.textContent).toContain('uso_extraurbano');
    urban.click(); expect(page.selectedVehicleId()).toBe('urban');
  });
});
