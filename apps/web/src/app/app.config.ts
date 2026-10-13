import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAuth } from 'angular-auth-oidc-client';
import { routes } from './app.routes';
import { webOidcConfig } from '@shared/config';
import { WEB_FIELD_API, MockWebFieldApiService, HttpWebFieldApiService } from '@shared/api';
import { authInterceptor } from './providers/auth.interceptor';

const useMockWebFieldApi = typeof window !== 'undefined' && (
  window.location.search.includes('mock=true') ||
  window.localStorage?.getItem('istpetdev:use_mock') === 'true'
);

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
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAuth(webOidcConfig),
    {
      provide: WEB_FIELD_API,
      useClass: useMockWebFieldApi ? MockWebFieldApiService : HttpWebFieldApiService
    }
  ]
};
