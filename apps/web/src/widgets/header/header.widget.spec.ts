import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { AuthSession } from '@entities/session';
import { HeaderWidget } from './header.widget';

function navigationFor(role: string) {
  TestBed.configureTestingModule({
    imports: [HeaderWidget],
    providers: [
      provideRouter([]),
      { provide: AuthSession, useValue: { status: () => 'authenticated', role: () => role, me: () => null, logout: () => undefined } },
    ],
  });
  const fixture = TestBed.createComponent(HeaderWidget);
  fixture.detectChanges();
  return fixture.componentInstance.navItems().map(item => item.path);
}

describe('HeaderWidget (FR-21)', () => {
  it('Gerencia ve el acceso a Datos', () => {
    expect(navigationFor('Gerencia')).toContain('/datos');
  });

  it('Auditor conserva solo la navegación de lectura', () => {
    expect(navigationFor('Auditor')).toEqual(['/inicio', '/alertas', '/rutas']);
  });
});
