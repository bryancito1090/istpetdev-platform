import { PassedInitialConfig, LogLevel } from 'angular-auth-oidc-client';

/**
 * Configuración OIDC con flujo PKCE (S256) para el entorno móvil/PWA.
 * S-09: Placeholders de autoridad OIDC configurados de forma desacoplada
 * para permitir sustitución directa por variables de entorno sin modificar lógica.
 */
export const mobileOidcConfig: PassedInitialConfig = {
  config: {
    authority: 'https://identity.istpetdev.local',
    redirectUrl: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4200',
    postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4200',
    clientId: 'istpetdev-mobile-pwa',
    scope: 'openid profile email roles logistics:field',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    logLevel: LogLevel.Warn,
    secureRoutes: ['https://api.istpetdev.local/', '/api/']
  }
};
