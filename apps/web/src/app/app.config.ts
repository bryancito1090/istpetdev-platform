import { ApplicationConfig, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AbstractSecurityStorage, provideAuth } from 'angular-auth-oidc-client';
import { TokenFreeStorage } from '@istpetdev/shared-core';
import { routes } from './app.routes';
import { webOidcConfig } from '@shared/config';
import { WEB_FIELD_API, HttpWebFieldApiService, apiAuthInterceptor } from '@shared/api';
import { AuthSession } from '@entities/session';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({
        anchorScrolling: 'enabled',
        scrollPositionRestoration: 'enabled'
      })
    ),
    provideHttpClient(withInterceptors([apiAuthInterceptor])),
    provideAuth(webOidcConfig),
    // FR-03: tokens solo en memoria; sessionStorage recibe solo las claves PKCE autorizadas.
    { provide: AbstractSecurityStorage, useFactory: () => new TokenFreeStorage() },
    provideAppInitializer(() => inject(AuthSession).bootstrap()),
    {
      provide: WEB_FIELD_API,
      useClass: HttpWebFieldApiService
    }
  ]
};
