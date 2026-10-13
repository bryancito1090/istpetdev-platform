import { ApplicationConfig, isDevMode, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideServiceWorker } from '@angular/service-worker';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { provideAuth } from 'angular-auth-oidc-client';
import { routes } from './app.routes';
import { authInterceptor, mobileOidcConfig } from './auth';
import { FIELD_API } from './field/field-api.interface';
import { MockFieldApiService } from './field/mock/mock-field-api.service';
import { HttpFieldApiService } from './field/http-field-api.service';

const useMockFieldApi = typeof window !== 'undefined' && (
  window.location.search.includes('mock=true') ||
  window.localStorage?.getItem('istpetdev:use_mock') === 'true'
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
