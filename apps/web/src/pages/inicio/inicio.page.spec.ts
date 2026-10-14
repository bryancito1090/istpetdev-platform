import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';
import { InicioPage } from './inicio.page';

describe('InicioPage (FR-21)', () => {
  it('oculta al Auditor el enlace de escritura a Datos', () => {
    TestBed.configureTestingModule({
      imports: [InicioPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => false } },
        { provide: ApiService, useValue: { getServicePoints: () => of([]), getRisks: () => of({ items: [] }) } },
      ],
    });
    const fixture = TestBed.createComponent(InicioPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a[href="/datos"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('1. Cargar Datos Maestros');
  });
});
