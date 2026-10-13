import { openDB, IDBPDatabase } from 'idb';
import { INITIAL_FIELD_DEMO_DATA, MockServerData } from './field-demo.fixture';

const DB_NAME = 'istpetdev_mock_backend_db';
const DB_VERSION = 1;
const STORE_NAME = 'server_state';
const STATE_KEY = 'current_mock_state';

/**
 * Almacenamiento local persistente en IndexedDB para simular la base de datos del servidor.
 * Permite que los cambios (despachos, recepciones, tokens, etc.) sobrevivan a recargas del navegador.
 */
export class MockServerStore {
  private dbPromise: Promise<IDBPDatabase>;

  constructor() {
    this.dbPromise = openDB(DB_NAME, DB_VERSION, {
      upgrade(db) {
        if (!db.objectStoreNames.contains(STORE_NAME)) {
          db.createObjectStore(STORE_NAME);
        }
      }
    });
  }

  async getState(): Promise<MockServerData> {
    const db = await this.dbPromise;
    const state = (await db.get(STORE_NAME, STATE_KEY)) as MockServerData | undefined;
    if (!state) {
      const initial = JSON.parse(JSON.stringify(INITIAL_FIELD_DEMO_DATA));
      await db.put(STORE_NAME, initial, STATE_KEY);
      return initial;
    }
    return state;
  }

  async saveState(state: MockServerData): Promise<void> {
    const db = await this.dbPromise;
    await db.put(STORE_NAME, state, STATE_KEY);
  }

  async resetToInitial(): Promise<MockServerData> {
    const db = await this.dbPromise;
    const initial = JSON.parse(JSON.stringify(INITIAL_FIELD_DEMO_DATA));
    await db.put(STORE_NAME, initial, STATE_KEY);
    return initial;
  }
}
