import { ApplicationConfig, inject, isDevMode, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideServiceWorker } from '@angular/service-worker';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { AbstractSecurityStorage, provideAuth } from 'angular-auth-oidc-client';
import { TokenFreeStorage } from '@istpetdev/shared-core';
import { routes } from './app.routes';
import { MobileAuthService, authInterceptor, mobileOidcConfig } from './auth';
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
    // FR-03: tokens solo en memoria; reemplaza el almacenamiento por defecto de la librería.
    { provide: AbstractSecurityStorage, useFactory: () => new TokenFreeStorage() },
    provideAppInitializer(() => inject(MobileAuthService).bootstrap()),
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
