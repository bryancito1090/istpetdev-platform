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

describe('TokenFreeStorage (OAuth 2.0 BCP en sessionStorage)', () => {
  it('persiste la sesion efimera en sessionStorage aislada por pestaña', () => {
    const flow = new MapStore();
    const storage = new TokenFreeStorage(flow);

    storage.write(CONFIG, fullState);

    expect(storage.read(CONFIG)).toBe(fullState);
    expect(flow.getItem(CONFIG)).toBe(fullState);
  });

  it('la sesion completa sobrevive a la recarga de pagina dentro de la pestaña', () => {
    const flow = new MapStore();
    new TokenFreeStorage(flow).write(CONFIG, fullState);

    const afterReload = new TokenFreeStorage(flow);

    expect(afterReload.read(CONFIG)).toBe(fullState);
  });

  it('permite escribir claves sueltas en sessionStorage', () => {
    const flow = new MapStore();
    const storage = new TokenFreeStorage(flow);

    storage.write('codeVerifier', 'v');
    storage.write('authzData', 'AT-SECRETO');

    expect(flow.getItem('codeVerifier')).toBe('v');
    expect(flow.getItem('authzData')).toBe('AT-SECRETO');
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
});
