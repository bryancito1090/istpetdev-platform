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

function usersLinkFor(role: string): HTMLElement | null {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    imports: [HeaderWidget],
    providers: [
      provideRouter([]),
      { provide: AuthSession, useValue: { status: () => 'authenticated', role: () => role, me: () => null, logout: () => undefined } },
    ],
  });
  const fixture = TestBed.createComponent(HeaderWidget);
  fixture.detectChanges();
  return fixture.nativeElement.querySelector('a[routerLink="/users"]');
}

describe('HeaderWidget (FR-21)', () => {
  it('Gerencia ve el acceso a Datos', () => {
    expect(navigationFor('Gerencia')).toContain('/datos');
  });

  it('Auditor conserva solo la navegación de lectura', () => {
    expect(navigationFor('Auditor')).toEqual(['/inicio', '/alertas', '/rutas']);
  });

  it('Gerencia ve el enlace a Usuarios', () => {
    const link = usersLinkFor('Gerencia');
    expect(link).not.toBeNull();
    expect(link?.textContent?.trim()).toBe('Usuarios');
  });

  it('Planificador y Auditor NO ven el enlace a Usuarios', () => {
    expect(usersLinkFor('Planificador')).toBeNull();
    expect(usersLinkFor('Auditor')).toBeNull();
  });
});
