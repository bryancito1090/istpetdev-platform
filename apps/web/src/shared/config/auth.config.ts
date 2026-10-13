import { PassedInitialConfig, LogLevel } from 'angular-auth-oidc-client';

/**
 * Configuración OIDC con flujo PKCE (S256) para la aplicación Web.
 * Cumple con ADR-10, Spec 004 (T8) y placeholders S-09 desacoplados.
 */
export const webOidcConfig: PassedInitialConfig = {
  config: {
    authority: 'https://identity.istpetdev.local',
    redirectUrl: typeof window !== 'undefined' ? `${window.location.origin}/login` : 'http://localhost:4200/login',
    postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4200',
    clientId: 'istpetdev-web-portal',
    scope: 'openid profile email roles logistics:warehouse logistics:field',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    logLevel: LogLevel.Warn,
    secureRoutes: ['https://api.istpetdev.local/', '/api/']
  }
};
