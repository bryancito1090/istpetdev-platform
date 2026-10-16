import { LogLevel, PassedInitialConfig } from 'angular-auth-oidc-client';
import { environment } from './environment';

/** Authorization Code + PKCE S256 contra Cognito (ADR-10, ADR-18, FR-01 de 013). */
export const webOidcConfig: PassedInitialConfig = {
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
    disableRefreshTokenOfflineAccessScopeWarning: true,
    logLevel: LogLevel.Warn,
    customParamsAuthRequest: {
      lang: 'es'
    },
    authWellknownEndpoints: {
      issuer: environment.oidc.authority,
      tokenEndpoint: `${environment.oidc.cognitoDomain}/oauth2/token`,
      authorizationEndpoint: `${environment.oidc.cognitoDomain}/oauth2/authorize`,
      jwksUri: `${environment.oidc.authority}/.well-known/jwks.json`,
      userInfoEndpoint: `${environment.oidc.cognitoDomain}/oauth2/userInfo`,
      endSessionEndpoint: `${environment.oidc.cognitoDomain}/logout`,
      revocationEndpoint: `${environment.oidc.cognitoDomain}/oauth2/revoke`
    }
  }
};
