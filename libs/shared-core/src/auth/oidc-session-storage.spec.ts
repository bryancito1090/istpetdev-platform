import { describe, expect, it } from 'vitest';
import { OidcSessionStorage, KeyValueStore } from './oidc-session-storage';

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
  authnResult: { access_token: 'AT', id_token: 'IT', refresh_token: 'RT' },
});

describe('OidcSessionStorage', () => {
  it('guarda la sesion OIDC en el almacen del navegador', () => {
    const store = new MapStore();
    const storage = new OidcSessionStorage(store);

    storage.write(CONFIG, fullState);

    expect(storage.read(CONFIG)).toBe(fullState);
    expect(store.getItem(CONFIG)).toBe(fullState);
  });

  it('la sesion sobrevive a la recarga de la pagina', () => {
    const store = new MapStore();
    new OidcSessionStorage(store).write(CONFIG, fullState);

    const afterReload = new OidcSessionStorage(store);

    expect(afterReload.read(CONFIG)).toBe(fullState);
  });

  it('remove borra la clave de memoria y del almacen', () => {
    const store = new MapStore();
    const storage = new OidcSessionStorage(store);
    storage.write(CONFIG, fullState);

    storage.remove(CONFIG);

    expect(storage.read(CONFIG)).toBeNull();
    expect(store.getItem(CONFIG)).toBeNull();
  });

  it('clear solo borra las claves de la sesion OIDC y respeta las de la app', () => {
    const store = new MapStore();
    store.setItem('istpetdev.returnUrl', '/rutas');
    store.setItem('preferencia-ajena', 'x');
    new OidcSessionStorage(store).write(CONFIG, fullState);

    const afterReload = new OidcSessionStorage(store);
    afterReload.read(CONFIG);
    afterReload.clear();

    expect(afterReload.read(CONFIG)).toBeNull();
    expect(store.getItem(CONFIG)).toBeNull();
    expect(store.getItem('istpetdev.returnUrl')).toBe('/rutas');
    expect(store.getItem('preferencia-ajena')).toBe('x');
  });

  it('sin almacen del navegador funciona solo en memoria', () => {
    const storage = new OidcSessionStorage(null);

    storage.write(CONFIG, fullState);

    expect(storage.read(CONFIG)).toBe(fullState);
  });
});
