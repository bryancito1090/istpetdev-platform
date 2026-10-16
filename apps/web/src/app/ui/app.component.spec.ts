import { Component } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, expect, it, vi } from 'vitest';
import { ApiService } from '@shared/api';
import { environment } from '@shared/config/environment';
import { HeaderWidget } from '@widgets/header/header.widget';
import { FooterWidget } from '@widgets/footer/footer.widget';
import { SidebarWidget } from '@widgets/sidebar/sidebar.widget';
import { AuthSession } from '@entities/session';
import { AppComponent } from './app.component';

@Component({ selector: 'widget-header', standalone: true, template: '' })
class HeaderStub {}
@Component({ selector: 'widget-footer', standalone: true, template: '' })
class FooterStub {}
@Component({ selector: 'widget-sidebar', standalone: true, template: '' })
class SidebarStub {}

afterEach(() => {
  vi.restoreAllMocks();
  TestBed.resetTestingModule();
});

it('muestra el fallo de API en pantalla y retira el aviso al recuperar la consulta', () => {
  vi.spyOn(console, 'error').mockImplementation(() => {});
  TestBed.configureTestingModule({
    imports: [AppComponent],
    providers: [
      provideRouter([]),
      provideHttpClient(),
      provideHttpClientTesting(),
      {
        provide: AuthSession,
        useValue: {
          status: () => 'anonymous',
          role: () => null,
          me: () => null,
          logout: () => undefined
        }
      }
    ]
  });
  TestBed.overrideComponent(AppComponent, {
    remove: { imports: [HeaderWidget, FooterWidget, SidebarWidget] },
    add: { imports: [HeaderStub, FooterStub, SidebarStub] }
  });
  const fixture = TestBed.createComponent(AppComponent);
  const api = TestBed.inject(ApiService);
  const backend = TestBed.inject(HttpTestingController);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  api.getWarehouses().subscribe();
  backend.expectOne(environment.apiUrl + '/api/v1/service-points/warehouses')
    .flush({}, { status: 503, statusText: 'Unavailable' });
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('[role="alert"]').textContent)
    .toContain('No se pudo cargar bodegas');
  api.getWarehouses().subscribe();
  backend.expectOne(environment.apiUrl + '/api/v1/service-points/warehouses').flush([]);
  fixture.detectChanges();
  expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  backend.verify();
});
