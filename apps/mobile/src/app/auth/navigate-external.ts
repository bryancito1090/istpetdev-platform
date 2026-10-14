import { InjectionToken } from '@angular/core';

/** Salida a una URL fuera de la app (cierre de Cognito). Inyectable para las pruebas. */
export const NAVIGATE_EXTERNAL = new InjectionToken<(url: string) => void>('NAVIGATE_EXTERNAL', {
  providedIn: 'root',
  factory: () => (url: string) => window.location.assign(url),
});
