import { ApplicationConfig, inject, isDevMode, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideServiceWorker } from '@angular/service-worker';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { AbstractSecurityStorage, provideAuth } from 'angular-auth-oidc-client';
import { OidcSessionStorage, browserStore } from '@istpetdev/shared-core';
import { routes } from './app.routes';
import { MobileAuthService, SESSION_EXPIRED, authInterceptor, mobileOidcConfig } from './auth';
import { FIELD_API } from './field/field-api.interface';
import { MockFieldApiService } from './field/mock/mock-field-api.service';
import { HttpFieldApiService } from './field/http-field-api.service';
import { shouldMockFieldApi } from './config/mock-api';

export { shouldMockFieldApi };

// Por defecto conectar con la API real en localhost:5000; solo usar mock si es desarrollo y se pasa ?mock=true en la URL
const useMockFieldApi = shouldMockFieldApi(
  isDevMode(),
  typeof window !== 'undefined' ? window.location.search : '',
);

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideIonicAngular({
      mode: 'md'
    }),
    provideAuth(mobileOidcConfig),
    // D-013-S1: la sesión vive en localStorage para que el conductor no la pierda si el sistema cierra la app sin conexión.
    { provide: AbstractSecurityStorage, useFactory: () => new OidcSessionStorage(browserStore('local')) },
    provideAppInitializer(() => inject(MobileAuthService).bootstrap()),
    {
      provide: SESSION_EXPIRED,
      useFactory: () => {
        const auth = inject(MobileAuthService);
        const router = inject(Router);
        return () => auth.expire(router.url);
      }
    },
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    }),
    {
      provide: FIELD_API,
      useClass: useMockFieldApi ? MockFieldApiService : HttpFieldApiService
    }
  ]
};
