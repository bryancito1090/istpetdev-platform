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
            getServicePoints: vi.fn(() => of([])),
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
});
