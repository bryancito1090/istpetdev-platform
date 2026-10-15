import { ApplicationConfig, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { Router, provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AbstractSecurityStorage, provideAuth } from 'angular-auth-oidc-client';
import { OidcSessionStorage, browserStore } from '@istpetdev/shared-core';
import { routes } from './app.routes';
import { webOidcConfig } from '@shared/config';
import { WEB_FIELD_API, HttpWebFieldApiService, SESSION_EXPIRED, apiAuthInterceptor } from '@shared/api';
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
    // D-013-S1: la sesión vive en sessionStorage; sobrevive a F5 y muere al cerrar la pestaña.
    { provide: AbstractSecurityStorage, useFactory: () => new OidcSessionStorage(browserStore('session')) },
    provideAppInitializer(() => inject(AuthSession).bootstrap()),
    {
      provide: SESSION_EXPIRED,
      useFactory: () => {
        const session = inject(AuthSession);
        const router = inject(Router);
        return () => session.expire(router.url);
      }
    },
    {
      provide: WEB_FIELD_API,
      useClass: HttpWebFieldApiService
    }
  ]
};
