import { LogLevel, PassedInitialConfig } from 'angular-auth-oidc-client';
import { environment } from '../config/environment';

/** Authorization Code + PKCE S256 contra Cognito para la PWA (ADR-10, ADR-18, FR-01 de 013). */
export const mobileOidcConfig: PassedInitialConfig = {
  config: {
    authority: environment.oidc.authority,
    clientId: environment.oidc.clientId,
    redirectUrl: environment.oidc.redirectUrl,
    postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
    scope: 'openid email profile istpetdev-api/access',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 60,
    ignoreNonceAfterRefresh: true,
    // localStorage se comparte entre pestañas: el bloqueo evita que dos pestañas gasten el mismo refresh token rotado.
    useRefreshTokenLock: true,
    disableRefreshTokenOfflineAccessScopeWarning: true,
    logLevel: LogLevel.Warn,
    customParamsAuthRequest: {
      lang: 'es'
    },
    authWellknownEndpoints: {
      issuer: environment.oidc.authority,
      tokenEndpoint: `${environment.oidc.cognitoDomain}/oauth2/token`,
      authorizationEndpoint: `${environment.oidc.cognitoDomain}/oauth2/authorize?lang=es`,
      jwksUri: `${environment.oidc.authority}/.well-known/jwks.json`,
      userInfoEndpoint: `${environment.oidc.cognitoDomain}/oauth2/userInfo`,
      endSessionEndpoint: `${environment.oidc.cognitoDomain}/logout`,
      revocationEndpoint: `${environment.oidc.cognitoDomain}/oauth2/revoke`
    }
  },
};
