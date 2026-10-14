import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '../config/environment';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { MobileAuthService } from './auth.service';
import { NAVIGATE_EXTERNAL } from './navigate-external';

const ME_URL = `${environment.apiUrl}/api/v1/me`;

function setup(isAuthenticated: boolean) {
  const oidc = { checkAuth: vi.fn(() => of({ isAuthenticated })), authorize: vi.fn(), logoffLocal: vi.fn() };
  const navigate = vi.fn();
  const outbox = { setOwner: vi.fn(async () => undefined) };
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]), provideHttpClient(), provideHttpClientTesting(),
      { provide: OidcSecurityService, useValue: oidc },
      { provide: NAVIGATE_EXTERNAL, useValue: navigate },
      { provide: FieldOutboxService, useValue: outbox },
    ],
  });
  return { auth: TestBed.inject(MobileAuthService), backend: TestBed.inject(HttpTestingController), oidc, navigate, outbox };
}

async function boot(isAuthenticated: boolean, me?: object, status = 200) {
  const ctx = setup(isAuthenticated);
  const done = ctx.auth.bootstrap();
  if (me) (await vi.waitFor(() => ctx.backend.expectOne(ME_URL))).flush(me, { status, statusText: 'x' });
  await done;
  return ctx;
}

describe('MobileAuthService', () => {
  beforeEach(() => { localStorage.clear(); sessionStorage.clear(); });

  it('un Conductor queda autenticado sin escribir tokens en ningún almacenamiento', async () => {
    const { auth } = await boot(true, { accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    expect(auth.status()).toBe('authenticated');
    expect(auth.currentRole()).toBe('Conductor');
    expect(localStorage.length + sessionStorage.length).toBe(0);
  });

  it('Gerencia en la PWA queda en plataforma equivocada', async () => {
    const { auth } = await boot(true, { accountId: 'g-1', displayName: 'Gerencia', role: 'Gerencia', platform: 'web' });
    expect(auth.status()).toBe('wrong-platform');
    expect(auth.currentRole()).toBeNull();
  });

  it('403 de /me significa cuenta no habilitada', async () => {
    const { auth } = await boot(true, { code: 'forbidden' }, 403);
    expect(auth.status()).toBe('disabled');
  });

  it('sin sesión queda anónima y el returnUrl por defecto es /home', async () => {
    const { auth } = await boot(false);
    expect(auth.status()).toBe('anonymous');
    expect(auth.takeReturnUrl()).toBe('/home');
  });

  it('logout cierra la sesión de Cognito', async () => {
    const { auth, oidc, navigate } = await boot(true, { accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    await auth.logout();
    expect(auth.status()).toBe('anonymous');
    expect(oidc.logoffLocal).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(expect.stringContaining(`${environment.oidc.cognitoDomain}/logout?client_id=${environment.oidc.clientId}`));
  });

  it('returnUrl hacia otro origen o sospechoso termina en /home (Review Focus 1)', async () => {
    const { auth } = await boot(false);

    auth.login('https://evil.example/home');
    expect(auth.takeReturnUrl()).toBe('/home');

    auth.login('//evil.example');
    expect(auth.takeReturnUrl()).toBe('/home');

    auth.login('/\\evil.example');
    expect(auth.takeReturnUrl()).toBe('/home');

    auth.login('/login');
    expect(auth.takeReturnUrl()).toBe('/home');

    auth.login('/auth/callback');
    expect(auth.takeReturnUrl()).toBe('/home');

    auth.login('/scan');
    expect(auth.takeReturnUrl()).toBe('/scan');
  });

  it('verifica que tokens nunca quedan en localStorage ni sessionStorage tras autenticación (Review Focus 4)', async () => {
    const { auth } = await boot(true, { accountId: 'c-2', displayName: 'Conductor 2', role: 'Conductor', platform: 'field' });
    expect(auth.status()).toBe('authenticated');

    const localKeys = Object.keys(localStorage);
    const sessionKeys = Object.keys(sessionStorage);
    expect(localKeys.some((k) => k.toLowerCase().includes('token'))).toBe(false);
    expect(sessionKeys.some((k) => k.toLowerCase().includes('token'))).toBe(false);
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it('bootstrap fija el dueño del outbox si la plataforma es field', async () => {
    const { outbox } = await boot(true, { accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    expect(outbox.setOwner).toHaveBeenCalledWith('c-1');
  });

  it('bootstrap pasa null al outbox si la plataforma no es field', async () => {
    const { outbox } = await boot(true, { accountId: 'g-1', displayName: 'Gerencia', role: 'Gerencia', platform: 'web' });
    expect(outbox.setOwner).toHaveBeenCalledWith(null);
  });

  it('logout limpia el dueño del outbox antes de salir', async () => {
    const { auth, outbox } = await boot(true, { accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    await auth.logout();
    expect(outbox.setOwner).toHaveBeenCalledWith(null);
  });
});
