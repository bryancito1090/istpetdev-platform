import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { AuthSession, SessionStatus } from '../model/auth-session';
import { authGuard } from './auth.guard';
import { homeRedirectGuard, roleGuard } from './role.guard';

function guard(status: SessionStatus, role: string | null, run: () => unknown) {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [provideRouter([]), { provide: AuthSession, useValue: { status: () => status, role: () => role } }],
  });
  return TestBed.runInInjectionContext(run);
}

const state = { url: '/dashboard' } as RouterStateSnapshot;

describe('authGuard (FR-08)', () => {
  it('deja pasar una sesión web válida', () => {
    expect(guard('authenticated', 'Gerencia', () => authGuard({} as ActivatedRouteSnapshot, state))).toBe(true);
  });

  it('sin sesión manda a /login con returnUrl', () => {
    const tree = guard('anonymous', null, () => authGuard({} as ActivatedRouteSnapshot, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(tree).startsWith('/login')).toBe(true);
    expect(tree.queryParams).toEqual({ returnUrl: '/dashboard' });
  });

  it.each(['wrong-platform', 'disabled', 'error'] as const)('%s va a /login sin returnUrl', (status) => {
    const tree = guard(status, null, () => authGuard({} as ActivatedRouteSnapshot, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(tree)).toBe('/login');
  });
});

describe('roleGuard', () => {
  const route = { data: { roles: ['Gerencia'] } } as unknown as ActivatedRouteSnapshot;

  it('admite el rol listado', () => {
    expect(guard('authenticated', 'Gerencia', () => roleGuard(route, state))).toBe(true);
  });

  it('devuelve al panel a cualquier otro rol', () => {
    const tree = guard('authenticated', 'Auditor', () => roleGuard(route, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(tree)).toBe('/dashboard');
  });

  it('permite /users a Gerencia y rechaza a Planificador y Auditor', () => {
    const usersRoute = { data: { roles: ['Gerencia'] } } as unknown as ActivatedRouteSnapshot;
    const usersState = { url: '/users' } as RouterStateSnapshot;

    expect(guard('authenticated', 'Gerencia', () => roleGuard(usersRoute, usersState))).toBe(true);

    const treePlanificador = guard('authenticated', 'Planificador', () => roleGuard(usersRoute, usersState)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(treePlanificador)).toBe('/dashboard');

    const treeAuditor = guard('authenticated', 'Auditor', () => roleGuard(usersRoute, usersState)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(treeAuditor)).toBe('/dashboard');
  });
});

describe('homeRedirectGuard', () => {
  it('redirige a inicio con sesión', () => {
    const authenticated = guard('authenticated', 'Auditor', () => homeRedirectGuard({} as ActivatedRouteSnapshot, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(authenticated)).toBe('/inicio');
  });

  it('redirige a login sin sesión', () => {
    const anonymous = guard('anonymous', null, () => homeRedirectGuard({} as ActivatedRouteSnapshot, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(anonymous)).toBe('/login');
  });
});
