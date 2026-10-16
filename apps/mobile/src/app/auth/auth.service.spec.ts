import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '../config/environment';
import { FieldOutboxService } from '../field/outbox/field-outbox.service';
import { MobileAuthService } from './auth.service';
import { NAVIGATE_EXTERNAL } from './navigate-external';

const ME_URL = `${environment.apiUrl}/api/v1/me`;
const LAST_ACCOUNT_KEY = 'istpetdev.lastFieldAccount';
const CONDUCTOR = { accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' };

function seedLastAccount(subject = 'sub-c1') {
  localStorage.setItem(LAST_ACCOUNT_KEY, JSON.stringify({ ...CONDUCTOR, subject }));
}

function setup(isAuthenticated: boolean, refresh: { token?: string; result?: boolean; unreachable?: boolean; sub?: string } = {}) {
  const oidc = {
    getPayloadFromIdToken: vi.fn(() => of(refresh.sub === undefined ? { sub: 'sub-c1' } : { sub: refresh.sub })),
    checkAuth: vi.fn(() => of({ isAuthenticated })),
    getRefreshToken: vi.fn(() => of(refresh.token ?? '')),
    checkAuthIncludingServer: vi.fn(() => refresh.unreachable
      ? throwError(() => new Error('Failed to fetch'))
      : of({ isAuthenticated: refresh.result ?? false })),
    authorize: vi.fn(),
    logoffLocal: vi.fn(),
  };
  const navigate = vi.fn();
  const outbox = { setOwner: vi.fn(async () => undefined), setSyncHeld: vi.fn(), syncAllPending: vi.fn(async () => undefined) };
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
    // 015: solo queda la última cuenta (sin tokens) para poder abrir la app sin señal.
    expect(Object.keys(localStorage)).toEqual([LAST_ACCOUNT_KEY]);
    expect(sessionStorage.length).toBe(0);
  });

  it('al reabrir la app con el access token vencido recupera la sesión con el refresh token', async () => {
    const ctx = setup(false, { token: 'RT', result: true });
    const done = ctx.auth.bootstrap();
    (await vi.waitFor(() => ctx.backend.expectOne(ME_URL))).flush({ accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    await done;
    expect(ctx.oidc.checkAuthIncludingServer).toHaveBeenCalled();
    expect(ctx.auth.status()).toBe('authenticated');
  });

  it('si el refresh token ya no sirve queda anónima', async () => {
    const ctx = setup(false, { token: 'RT', result: false });
    await ctx.auth.bootstrap();
    expect(ctx.auth.status()).toBe('anonymous');
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
    expect(localKeys).toEqual([LAST_ACCOUNT_KEY]);
    expect(localStorage.getItem(LAST_ACCOUNT_KEY)!.toLowerCase()).not.toMatch(/token|bearer/);
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

  it('expire vuelve a Cognito sin soltar el outbox de la cuenta', async () => {
    const { auth, oidc, outbox } = await boot(true, { accountId: 'c-1', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    outbox.setOwner.mockClear();

    auth.expire('/home');

    expect(auth.status()).toBe('anonymous');
    expect(oidc.authorize).toHaveBeenCalledTimes(1);
    expect(outbox.setOwner).not.toHaveBeenCalled();
  });

  describe('arranque sin señal (015)', () => {
    afterEach(() => vi.restoreAllMocks());

    it('guarda la última cuenta de campo tras /me', async () => {
      await boot(true, CONDUCTOR);
      expect(JSON.parse(localStorage.getItem(LAST_ACCOUNT_KEY)!)).toEqual({ ...CONDUCTOR, subject: 'sub-c1' });
    });

    it('no guarda una cuenta de otra plataforma', async () => {
      await boot(true, { accountId: 'g-1', displayName: 'Gerencia', role: 'Gerencia', platform: 'web' });
      expect(localStorage.getItem(LAST_ACCOUNT_KEY)).toBeNull();
    });

    it('con el token vencido y Cognito inalcanzable entra sin conexión con la última cuenta', async () => {
      seedLastAccount();
      const ctx = setup(false, { token: 'RT', unreachable: true });
      await ctx.auth.bootstrap();
      expect(ctx.auth.status()).toBe('authenticated');
      expect(ctx.auth.offline()).toBe(true);
      expect(ctx.auth.currentRole()).toBe('Conductor');
      expect(ctx.outbox.setSyncHeld).toHaveBeenCalledWith(true);
      expect(ctx.outbox.setOwner).toHaveBeenCalledWith('c-1');
    });

    it('si el navegador sabe que no hay red no espera a Cognito', async () => {
      vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(false);
      seedLastAccount();
      const ctx = setup(false, { token: 'RT', result: true });
      await ctx.auth.bootstrap();
      expect(ctx.oidc.checkAuthIncludingServer).not.toHaveBeenCalled();
      expect(ctx.auth.offline()).toBe(true);
    });

    it('con el token vigente pero la API sin respuesta entra sin conexión', async () => {
      seedLastAccount();
      const ctx = setup(true);
      const done = ctx.auth.bootstrap();
      (await vi.waitFor(() => ctx.backend.expectOne(ME_URL))).error(new ProgressEvent('error'));
      await done;
      expect(ctx.auth.status()).toBe('authenticated');
      expect(ctx.auth.offline()).toBe(true);
    });

    it('un 503 de /me también entra sin conexión', async () => {
      seedLastAccount();
      const { auth } = await boot(true, { title: 'down' }, 503);
      expect(auth.offline()).toBe(true);
    });

    it('no usa la cuenta guardada de otra persona de Cognito en el mismo teléfono', async () => {
      seedLastAccount('sub-a');
      const ctx = setup(false, { token: 'RT', unreachable: true, sub: 'sub-b' });
      await ctx.auth.bootstrap();
      expect(ctx.auth.status()).toBe('error');
      expect(ctx.auth.offline()).toBe(false);
      expect(ctx.outbox.setOwner).not.toHaveBeenCalled();
    });

    it('no usa una cuenta guardada sin sujeto de Cognito', async () => {
      localStorage.setItem(LAST_ACCOUNT_KEY, JSON.stringify(CONDUCTOR));
      const ctx = setup(false, { token: 'RT', unreachable: true });
      await ctx.auth.bootstrap();
      expect(ctx.auth.status()).toBe('error');
    });

    it('sin red y sin cuenta guardada pide señal', async () => {
      const ctx = setup(false, { token: 'RT', unreachable: true });
      await ctx.auth.bootstrap();
      expect(ctx.auth.status()).toBe('error');
      expect(ctx.outbox.setOwner).not.toHaveBeenCalled();
    });

    it('sin sesión OIDC local no usa la cuenta guardada', async () => {
      seedLastAccount();
      const ctx = setup(false);
      await ctx.auth.bootstrap();
      expect(ctx.auth.status()).toBe('anonymous');
    });

    it('si Cognito rechaza el refresh token no usa la cuenta guardada', async () => {
      seedLastAccount();
      const ctx = setup(false, { token: 'RT', result: false });
      await ctx.auth.bootstrap();
      expect(ctx.auth.status()).toBe('anonymous');
      expect(ctx.auth.offline()).toBe(false);
    });

    it('al volver la señal renueva la sesión, sale del modo sin conexión y envía la cola', async () => {
      seedLastAccount();
      const ctx = setup(false, { token: 'RT', unreachable: true });
      await ctx.auth.bootstrap();
      ctx.oidc.checkAuthIncludingServer.mockReturnValue(of({ isAuthenticated: true }));

      window.dispatchEvent(new Event('online'));
      (await vi.waitFor(() => ctx.backend.expectOne(ME_URL))).flush(CONDUCTOR);

      await vi.waitFor(() => expect(ctx.outbox.syncAllPending).toHaveBeenCalled());
      expect(ctx.auth.offline()).toBe(false);
      expect(ctx.auth.status()).toBe('authenticated');
      expect(ctx.outbox.setSyncHeld).toHaveBeenLastCalledWith(false);
    });

    it('al volver la señal con el refresh token rechazado queda anónima y conserva la cola', async () => {
      seedLastAccount();
      const ctx = setup(false, { token: 'RT', unreachable: true });
      await ctx.auth.bootstrap();
      ctx.oidc.checkAuthIncludingServer.mockReturnValue(of({ isAuthenticated: false }));
      ctx.outbox.setOwner.mockClear();

      await ctx.auth.reconnect();

      expect(ctx.auth.status()).toBe('anonymous');
      expect(ctx.auth.offline()).toBe(false);
      expect(ctx.outbox.setOwner).not.toHaveBeenCalled();
      expect(ctx.outbox.syncAllPending).not.toHaveBeenCalled();
    });

    it('cerrar sesión borra la última cuenta', async () => {
      const { auth } = await boot(true, CONDUCTOR);
      await auth.logout();
      expect(localStorage.getItem(LAST_ACCOUNT_KEY)).toBeNull();
    });
  });
});
