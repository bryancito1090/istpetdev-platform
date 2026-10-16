import { environment } from './environment';

/**
 * Resuelve la URL canónica de la aplicación móvil PWA (Campo) respetando el entorno de ejecución:
 * - En producción: apunta a la ruta relativa `/campo/` servida por Nginx en el mismo origen HTTPS.
 * - En desarrollo: apunta al puerto de desarrollo (`http://localhost:8100` o sincroniza el hostname
 *   si se accede a través de la red local para pruebas en dispositivos físicos).
 */
export function getMobileAppUrl(): string {
  if (environment.production) {
    return environment.mobileAppUrl || '/campo/';
  }

  if (typeof window !== 'undefined') {
    if (
      window.location.hostname === 'istpetdev.doicela.dev' ||
      (window.location.protocol === 'https:' && !window.location.hostname.endsWith('.localhost'))
    ) {
      return '/campo/';
    }
  }

  const rawUrl = environment.mobileAppUrl || 'http://localhost:8100';

  if (typeof window === 'undefined') {
    return rawUrl;
  }

  try {
    const url = new URL(rawUrl, window.location.origin);
    if (
      url.hostname === 'localhost' &&
      window.location.hostname !== 'localhost' &&
      window.location.hostname !== '127.0.0.1'
    ) {
      url.hostname = window.location.hostname;
    }
    const result = url.toString();
    return rawUrl.endsWith('/') ? result : result.replace(/\/$/, '');
  } catch {
    return rawUrl;
  }
}
