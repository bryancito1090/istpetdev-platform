import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it } from 'vitest';
import { AuthSession } from '@entities/session';
import { ApiService } from '@shared/api';
import { AlertasPage } from './alertas.page';

describe('AlertasPage (FR-21)', () => {
  it('oculta al Auditor el enlace de escritura a Datos en estado vacío', () => {
    TestBed.configureTestingModule({
      imports: [AlertasPage],
      providers: [
        provideRouter([]),
        { provide: AuthSession, useValue: { canWrite: () => false } },
        { provide: ApiService, useValue: { getRisks: () => of({ items: [] }) } },
      ],
    });
    const fixture = TestBed.createComponent(AlertasPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a[href="/datos"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No se han registrado consumos');
  });
});
