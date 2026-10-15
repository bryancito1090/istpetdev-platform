import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { environment } from '@shared/config';
import { NAVIGATE_EXTERNAL } from '../lib/navigate-external';
import { AuthSession } from './auth-session';

const ME_URL = `${environment.apiUrl}/api/v1/me`;

function memoryStorage(): Storage {
  const data = new Map<string, string>();
  return {
    get length() { return data.size; },
    clear: () => data.clear(),
    getItem: key => data.get(key) ?? null,
    key: index => [...data.keys()][index] ?? null,
    removeItem: key => { data.delete(key); },
    setItem: (key, value) => { data.set(key, value); },
  } as Storage;
}

function setup(isAuthenticated: boolean, refresh: { token?: string; result?: boolean } = {}) {
  const oidc = {
    checkAuth: vi.fn(() => of({ isAuthenticated })),
    getRefreshToken: vi.fn(() => of(refresh.token ?? '')),
    checkAuthIncludingServer: vi.fn(() => of({ isAuthenticated: refresh.result ?? false })),
    authorize: vi.fn(),
    logoffLocal: vi.fn(),
  };
  const navigate = vi.fn();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(), provideHttpClientTesting(),
      { provide: OidcSecurityService, useValue: oidc },
      { provide: NAVIGATE_EXTERNAL, useValue: navigate },
    ],
  });
  return { session: TestBed.inject(AuthSession), backend: TestBed.inject(HttpTestingController), oidc, navigate };
}

async function answerMe(backend: HttpTestingController, body: object, status = 200) {
  const request = await vi.waitFor(() => backend.expectOne(ME_URL));
  request.flush(body, { status, statusText: status === 200 ? 'OK' : 'Forbidden' });
}

describe('AuthSession', () => {
  beforeEach(() => {
    Object.defineProperty(globalThis, 'localStorage', { configurable: true, value: memoryStorage() });
    Object.defineProperty(globalThis, 'sessionStorage', { configurable: true, value: memoryStorage() });
  });

  it('sin sesión de Cognito queda anónima y no llama a /me', async () => {
    const { session, backend } = setup(false);
    await session.bootstrap();
    expect(session.status()).toBe('anonymous');
    backend.expectNone(ME_URL);
  });

  it('con el access token vencido recupera la sesión con el refresh token', async () => {
    const { session, backend, oidc } = setup(false, { token: 'RT', result: true });
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-1', displayName: 'Planificación', role: 'Planificador', platform: 'web' });
    await done;
    expect(oidc.checkAuthIncludingServer).toHaveBeenCalled();
    expect(session.status()).toBe('authenticated');
  });

  it('si el refresh token ya no sirve queda anónima', async () => {
    const { session, backend } = setup(false, { token: 'RT', result: false });
    await session.bootstrap();
    expect(session.status()).toBe('anonymous');
    backend.expectNone(ME_URL);
  });

  it('sin refresh token no intenta renovar', async () => {
    const { session, oidc } = setup(false);
    await session.bootstrap();
    expect(oidc.checkAuthIncludingServer).not.toHaveBeenCalled();
  });

  it('una cuenta web queda autenticada con su rol y sin escribir localStorage', async () => {
    const { session, backend } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-1', displayName: 'Planificación', role: 'Planificador', platform: 'web' });
    await done;
    expect(session.status()).toBe('authenticated');
    expect(session.role()).toBe('Planificador');
    expect(session.canWrite()).toBe(true);
    expect(localStorage.length).toBe(0);
  });

  it('Auditor entra a la web sin permisos de escritura', async () => {
    const { session, backend } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-2', displayName: 'Auditoría', role: 'Auditor', platform: 'web' });
    await done;
    expect(session.status()).toBe('authenticated');
    expect(session.canWrite()).toBe(false);
  });

  it('un Conductor en la web queda en plataforma equivocada', async () => {
    const { session, backend } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-3', displayName: 'Conductor', role: 'Conductor', platform: 'field' });
    await done;
    expect(session.status()).toBe('wrong-platform');
  });

  it('403 de /me significa cuenta no habilitada', async () => {
    const { session, backend } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { code: 'forbidden' }, 403);
    await done;
    expect(session.status()).toBe('disabled');
    expect(session.me()).toBeNull();
  });

  it('persiste solo el returnUrl seguro y lo consume al volver', () => {
    const { session, oidc } = setup(false);
    session.login('//evil.example');
    expect(sessionStorage.length).toBe(0);
    session.login('/dashboard?vista=mapa');
    expect(localStorage.getItem('istpetdev.returnUrl')).toBe('/dashboard?vista=mapa');
    expect(localStorage.getItem('istpetdev.returnUrl')).not.toMatch(/access_token|refresh_token|id_token|bearer/i);
    expect(sessionStorage.length).toBe(0);
    expect(oidc.authorize).toHaveBeenCalledTimes(2);
    expect(session.takeReturnUrl()).toBe('/dashboard?vista=mapa');
    expect(localStorage.length).toBe(0);
  });

  it('usa el panel tras volver de Cognito si el returnUrl apuntaba a otro origen (Review Focus 1)', async () => {
    const { session, backend } = setup(true);
    session.login('https://evil.example/dashboard');
    expect(localStorage.length).toBe(0);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-1', displayName: 'Planificación', role: 'Planificador', platform: 'web' });
    await done;

    expect(session.takeReturnUrl()).toBe('/dashboard');
  });

  it('usa /dashboard como fallback si no hay returnUrl', () => {
    const { session } = setup(false);
    expect(session.takeReturnUrl()).toBe('/dashboard');
  });

  it('logout borra la sesión local y cierra la de Cognito', async () => {
    const { session, backend, oidc, navigate } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-1', displayName: 'Planificación', role: 'Planificador', platform: 'web' });
    await done;

    session.logout();

    expect(session.status()).toBe('anonymous');
    expect(oidc.logoffLocal).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(
      `${environment.oidc.cognitoDomain}/logout?client_id=${environment.oidc.clientId}&logout_uri=${encodeURIComponent(environment.oidc.postLogoutRedirectUri)}`,
    );
  });

  it('expire con la sesión iniciada vuelve a Cognito guardando la ruta', async () => {
    const { session, backend, oidc } = setup(true);
    const done = session.bootstrap();
    await answerMe(backend, { accountId: 'a-1', displayName: 'Planificación', role: 'Planificador', platform: 'web' });
    await done;

    session.expire('/routes');

    expect(session.status()).toBe('anonymous');
    expect(oidc.logoffLocal).toHaveBeenCalled();
    expect(oidc.authorize).toHaveBeenCalledTimes(1);
    expect(session.takeReturnUrl()).toBe('/routes');
  });

  it('expire sin sesión iniciada no redirige (evita bucles con /me)', async () => {
    const { session, oidc } = setup(false);
    await session.bootstrap();
    session.expire('/routes');
    expect(oidc.authorize).not.toHaveBeenCalled();
  });
});
