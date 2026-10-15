import 'fake-indexeddb/auto';
import { vi } from 'vitest';

const mockIonicStandalone = vi.mock;
mockIonicStandalone('@ionic/angular/standalone', async () => {
  const { Component } = await import('@angular/core');
  @Component({ selector: 'ion-dummy', template: '', standalone: true })
  class IonDummyComponent {}

  class MockAlertController {
    create = vi.fn();
  }

  return {
    IonHeader: IonDummyComponent,
    IonToolbar: IonDummyComponent,
    IonTitle: IonDummyComponent,
    IonContent: IonDummyComponent,
    IonCard: IonDummyComponent,
    IonCardHeader: IonDummyComponent,
    IonCardTitle: IonDummyComponent,
    IonCardSubtitle: IonDummyComponent,
    IonCardContent: IonDummyComponent,
    IonButton: IonDummyComponent,
    IonBadge: IonDummyComponent,
    IonButtons: IonDummyComponent,
    IonInput: IonDummyComponent,
    IonSpinner: IonDummyComponent,
    AlertController: MockAlertController
  };
});

function createMemoryStorage(): Storage {
  const data = new Map<string, string>();
  const storage: Storage = {
    get length() { return data.size; },
    clear() { data.clear(); },
    getItem(key: string) { return data.get(key) ?? null; },
    key(index: number) { return [...data.keys()][index] ?? null; },
    removeItem(key: string) { data.delete(key); },
    setItem(key: string, value: string) { data.set(key, String(value)); },
  };
  return new Proxy(storage, {
    ownKeys() { return [...data.keys()]; },
    getOwnPropertyDescriptor(_target, prop) {
      if (typeof prop === 'string' && data.has(prop)) {
        return { configurable: true, enumerable: true, value: data.get(prop) };
      }
      return undefined;
    },
    get(target, prop, receiver) {
      if (typeof prop === 'string' && data.has(prop)) return data.get(prop);
      return Reflect.get(target, prop, receiver);
    },
    set(target, prop, value, receiver) {
      if (typeof prop === 'string') {
        data.set(prop, String(value));
        return true;
      }
      return Reflect.set(target, prop, value, receiver);
    },
    deleteProperty(target, prop) {
      if (typeof prop === 'string') return data.delete(prop);
      return Reflect.deleteProperty(target, prop);
    }
  });
}

Object.defineProperty(globalThis, 'localStorage', { configurable: true, value: createMemoryStorage() });
Object.defineProperty(globalThis, 'sessionStorage', { configurable: true, value: createMemoryStorage() });
