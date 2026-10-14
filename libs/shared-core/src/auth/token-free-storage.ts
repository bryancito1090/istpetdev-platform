/** Claves del flujo PKCE y configuración OIDC pública que deben sobrevivir a la redirección a Cognito. Ninguna es un token. */
export const AUTH_FLOW_KEYS: readonly string[] = [
  'codeVerifier',
  'authStateControl',
  'authNonce',
  'authWellKnownEndPoints',
  'storageCodeFlowInProgress'
];

export interface KeyValueStore {
  readonly length: number;
  key(index: number): string | null;
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

/**
 * Almacenamiento seguro de sesión OIDC según el estándar IETF OAuth 2.0 BCP para Browser-Based Apps.
 * Los tokens y el estado del flujo PKCE residen en `sessionStorage` (aislados por pestaña y eliminados al cerrar la pestaña),
 * garantizando inmunidad a CSRF y supervivencia a recargas (F5) o navegación directa por barra de direcciones.
 * Nunca se almacena en `localStorage` ni IndexedDB.
 */
export class TokenFreeStorage {
  private readonly memory = new Map<string, string>();

  constructor(private readonly flowStore: KeyValueStore | null = typeof sessionStorage === 'undefined' ? null : sessionStorage) {}

  read(key: string): string | null {
    return this.memory.get(key) ?? this.flowStore?.getItem(key) ?? null;
  }

  write(key: string, value: string): void {
    this.memory.set(key, value);
    this.flowStore?.setItem(key, value);
  }

  remove(key: string): void {
    this.memory.delete(key);
    this.flowStore?.removeItem(key);
  }

  clear(): void {
    if (this.flowStore) {
      const keysToRemove: string[] = [];
      for (let index = 0; index < this.flowStore.length; index++) {
        const key = this.flowStore.key(index);
        if (key !== null) keysToRemove.push(key);
      }
      for (const key of keysToRemove) this.flowStore.removeItem(key);
    }
    this.memory.clear();
  }
}

// La librería guarda un objeto JSON por configuración; también se admite una clave suelta.
function flowStateOf(key: string, value: string): string | null {
  if (AUTH_FLOW_KEYS.includes(key)) return value;
  let parsed: unknown;
  try {
    parsed = JSON.parse(value);
  } catch {
    return null;
  }
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return null;
  const flow = Object.fromEntries(Object.entries(parsed).filter(([name]) => AUTH_FLOW_KEYS.includes(name)));
  return Object.keys(flow).length > 0 ? JSON.stringify(flow) : null;
}

function isFlowStorageEntry(store: KeyValueStore, key: string): boolean {
  const value = store.getItem(key);
  if (value === null) return false;
  if (AUTH_FLOW_KEYS.includes(key)) return true;

  let parsed: unknown;
  try {
    parsed = JSON.parse(value);
  } catch {
    return false;
  }
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return false;
  const keys = Object.keys(parsed);
  return keys.length > 0 && keys.every((name) => AUTH_FLOW_KEYS.includes(name));
}
