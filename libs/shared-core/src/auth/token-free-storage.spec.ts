import { describe, expect, it } from 'vitest';
import { TokenFreeStorage, KeyValueStore } from './token-free-storage';

class MapStore implements KeyValueStore {
  readonly data = new Map<string, string>();
  get length() { return this.data.size; }
  key(index: number) { return Array.from(this.data.keys())[index] ?? null; }
  getItem(key: string) { return this.data.get(key) ?? null; }
  setItem(key: string, value: string) { this.data.set(key, value); }
  removeItem(key: string) { this.data.delete(key); }
}

const CONFIG = '0-cliente-web';
const fullState = JSON.stringify({
  codeVerifier: 'verificador-pkce',
  authStateControl: 'estado',
  authNonce: 'nonce',
  authnResult: { access_token: 'AT-SECRETO', id_token: 'IT-SECRETO', refresh_token: 'RT-SECRETO' },
  authzData: 'AT-SECRETO',
  reusable_refresh_token: 'RT-SECRETO',
  userData: { email: 'persona@ejemplo.test' },
});

describe('TokenFreeStorage (FR-03)', () => {
  it('la app lee todo de memoria pero sessionStorage solo recibe el flujo PKCE', () => {
    const flow = new MapStore();
    const storage = new TokenFreeStorage(flow);

    storage.write(CONFIG, fullState);

    expect(storage.read(CONFIG)).toBe(fullState);
    const persisted = flow.getItem(CONFIG) ?? '';
    expect(JSON.parse(persisted)).toEqual({ codeVerifier: 'verificador-pkce', authStateControl: 'estado', authNonce: 'nonce' });
    expect(persisted).not.toMatch(/SECRETO|access_token|refresh_token|id_token|persona@/);
  });

  it('el verificador sobrevive a la redirección; los tokens no', () => {
    const flow = new MapStore();
    new TokenFreeStorage(flow).write(CONFIG, fullState);

    const afterReload = new TokenFreeStorage(flow);

    expect(JSON.parse(afterReload.read(CONFIG) ?? '{}')).toEqual({ codeVerifier: 'verificador-pkce', authStateControl: 'estado', authNonce: 'nonce' });
  });

  it('una clave suelta de flujo se persiste y cualquier otra no', () => {
    const flow = new MapStore();
    const storage = new TokenFreeStorage(flow);

    storage.write('codeVerifier', 'v');
    storage.write('authzData', 'AT-SECRETO');

    expect(flow.getItem('codeVerifier')).toBe('v');
    expect(flow.getItem('authzData')).toBeNull();
    expect(storage.read('authzData')).toBe('AT-SECRETO');
  });

  it('remove y clear borran memoria y sessionStorage', () => {
    const flow = new MapStore();
    const storage = new TokenFreeStorage(flow);
    storage.write(CONFIG, fullState);

    storage.clear();

    expect(storage.read(CONFIG)).toBeNull();
    expect(flow.data.size).toBe(0);
  });

  it('clear en una instancia nueva borra el flujo PKCE propio y conserva datos ajenos', () => {
    const flow = new MapStore();
    new TokenFreeStorage(flow).write(CONFIG, fullState);
    flow.setItem('preferencia-app', 'compact');

    const afterRedirect = new TokenFreeStorage(flow);
    expect(JSON.parse(afterRedirect.read(CONFIG) ?? '{}')).toEqual({ codeVerifier: 'verificador-pkce', authStateControl: 'estado', authNonce: 'nonce' });

    afterRedirect.clear();

    expect(flow.getItem(CONFIG)).toBeNull();
    expect(flow.getItem('preferencia-app')).toBe('compact');
  });

  it('clear conserva JSON ajeno con claves PKCE mezcladas y borra el registro OIDC', () => {
    const flow = new MapStore();
    const oidcFlow = JSON.stringify({ codeVerifier: 'verificador', authNonce: 'nonce' });
    const appState = JSON.stringify({ authNonce: 'dato-app', preferencia: 'compact' });
    flow.setItem(CONFIG, oidcFlow);
    flow.setItem('estado-app', appState);

    new TokenFreeStorage(flow).clear();

    expect(flow.getItem(CONFIG)).toBeNull();
    expect(flow.getItem('estado-app')).toBe(appState);
  });
});
