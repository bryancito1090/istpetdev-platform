export interface KeyValueStore {
  readonly length: number;
  key(index: number): string | null;
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

/**
 * Almacén de la sesión OIDC (decisión D-013-S1 del spec 013).
 * La web usa `sessionStorage`: la sesión sobrevive a F5 y muere al cerrar la pestaña.
 * La PWA usa `localStorage`: la sesión del conductor sobrevive a que el sistema cierre la app sin conexión.
 * El token de acceso dura 15 minutos y el de refresco rota en cada uso; nunca se usa IndexedDB ni cookies.
 * `clear()` solo borra las claves que escribió o leyó la librería OIDC, no las de la app.
 */
export class OidcSessionStorage {
  private readonly memory = new Map<string, string>();
  private readonly ownKeys = new Set<string>();

  constructor(private readonly store: KeyValueStore | null) {}

  read(key: string): string | null {
    const value = this.memory.get(key) ?? this.store?.getItem(key) ?? null;
    if (value !== null) this.ownKeys.add(key);
    return value;
  }

  write(key: string, value: string): void {
    this.ownKeys.add(key);
    this.memory.set(key, value);
    this.store?.setItem(key, value);
  }

  remove(key: string): void {
    this.ownKeys.delete(key);
    this.memory.delete(key);
    this.store?.removeItem(key);
  }

  clear(): void {
    for (const key of this.ownKeys) this.store?.removeItem(key);
    this.ownKeys.clear();
    this.memory.clear();
  }
}

/** Devuelve el almacén del navegador o `null` si no existe o está bloqueado (modo privado estricto, SSR, pruebas). */
export function browserStore(kind: 'session' | 'local'): KeyValueStore | null {
  try {
    return kind === 'session' ? globalThis.sessionStorage ?? null : globalThis.localStorage ?? null;
  } catch {
    return null;
  }
}
