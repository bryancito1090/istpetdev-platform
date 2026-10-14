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
    logLevel: LogLevel.Warn,
  }
};
